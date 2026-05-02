using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace DzfpPdfPrinter
{
    public class FileMonitorService : IDisposable
    {
        private FileSystemWatcher? _watcher;
        private readonly string _processedFilesPath;
        private readonly HashSet<string> _processedFiles = new();
        private readonly ConcurrentQueue<string> _printQueue = new();
        private readonly ConcurrentDictionary<string, int> _failedAttempts = new();
        private readonly HashSet<string> _failedFiles = new();
        private System.Threading.Timer? _printTimer;
        private bool _isRunning;
        private string? _monitorDirectory;
        private string? _printerName;
        private const int MaxRetryCount = 3;
        private DateTime _lastErrorLogTime = DateTime.MinValue;
        private const int ErrorLogIntervalMs = 5000;

        public event Action<string, string>? OnFileDetected;
        public event Action<string>? OnFilePrinted;
        public event Action<string, string>? OnError;
        public event Action<string>? OnFileFailed;

        public FileMonitorService()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DzfpPdfPrinter");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _processedFilesPath = Path.Combine(appData, "processed_files.txt");
            LoadProcessedFiles();
        }

        public void StartMonitoring(string directory, string? printerName = null)
        {
            if (_isRunning)
            {
                StopMonitoring();
            }

            _monitorDirectory = directory;
            _printerName = printerName;
            
            if (!Directory.Exists(directory))
            {
                OnError?.Invoke("目录不存在", directory);
                return;
            }

            ScanExistingFiles();

            _watcher = new FileSystemWatcher(directory)
            {
                Filter = "*.pdf",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite
            };

            _watcher.Created += OnFileCreated;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Error += OnWatcherError;

            _printTimer = new System.Threading.Timer(ProcessPrintQueue, null, 1000, 2000);
            _isRunning = true;
        }

        public void StopMonitoring()
        {
            _isRunning = false;
            
            if (_watcher != null)
            {
                _watcher.Created -= OnFileCreated;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.Error -= OnWatcherError;
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _printTimer?.Dispose();
            _printTimer = null;
        }

        private void ScanExistingFiles()
        {
            if (string.IsNullOrEmpty(_monitorDirectory) || !Directory.Exists(_monitorDirectory))
                return;

            try
            {
                var files = Directory.GetFiles(_monitorDirectory, "dzfp_*.pdf");
                foreach (var file in files)
                {
                    MarkAsProcessed(file);
                }
            }
            catch (Exception ex)
            {
                LogError("扫描现有文件失败", ex.Message);
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (IsDzfpFile(e.FullPath) && !IsProcessed(e.FullPath) && !IsFailed(e.FullPath))
            {
                Thread.Sleep(500);
                EnqueueForPrinting(e.FullPath);
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (IsDzfpFile(e.FullPath) && !IsProcessed(e.FullPath) && !IsFailed(e.FullPath))
            {
                Thread.Sleep(500);
                EnqueueForPrinting(e.FullPath);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            LogError("文件监控错误", e.GetException()?.Message ?? "未知错误");
        }

        private void EnqueueForPrinting(string filePath)
        {
            _failedAttempts.TryRemove(filePath, out _);
            _printQueue.Enqueue(filePath);
            OnFileDetected?.Invoke(Path.GetFileName(filePath), filePath);
        }

        private void ProcessPrintQueue(object? state)
        {
            while (_printQueue.TryDequeue(out var filePath))
            {
                if (!IsDzfpFile(filePath) || IsProcessed(filePath) || IsFailed(filePath))
                    continue;

                try
                {
                    PdfPrinter.PrintPdf(filePath, _printerName);
                    MarkAsProcessed(filePath);
                    OnFilePrinted?.Invoke(Path.GetFileName(filePath));
                    
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    HandlePrintFailure(filePath, ex.Message);
                }
            }
        }

        private void HandlePrintFailure(string filePath, string errorMessage)
        {
            var normalizedPath = filePath.ToLowerInvariant();
            var currentAttempt = _failedAttempts.AddOrUpdate(normalizedPath, 1, (_, existing) => existing + 1);

            if (currentAttempt < MaxRetryCount)
            {
                LogError($"打印失败 ({currentAttempt}/{MaxRetryCount}): {Path.GetFileName(filePath)}", errorMessage);
                
                var delay = Math.Min(1000 * currentAttempt * currentAttempt, 30000);
                Task.Delay(delay).ContinueWith(_ =>
                {
                    if (!IsFailed(normalizedPath))
                    {
                        _printQueue.Enqueue(filePath);
                    }
                });
            }
            else
            {
                MarkAsFailed(filePath);
                LogError($"打印最终失败 (已重试{MaxRetryCount}次): {Path.GetFileName(filePath)}", errorMessage);
                OnFileFailed?.Invoke(Path.GetFileName(filePath));
            }
        }

        private void LogError(string title, string message)
        {
            var now = DateTime.Now;
            if ((now - _lastErrorLogTime).TotalMilliseconds >= ErrorLogIntervalMs)
            {
                _lastErrorLogTime = now;
                OnError?.Invoke(title, message);
            }
        }

        private bool IsDzfpFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return fileName.StartsWith("dzfp_", StringComparison.OrdinalIgnoreCase) && 
                   filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsProcessed(string filePath)
        {
            var normalizedPath = filePath.ToLowerInvariant();
            lock (_processedFiles)
            {
                return _processedFiles.Contains(normalizedPath);
            }
        }

        private bool IsFailed(string filePath)
        {
            var normalizedPath = filePath.ToLowerInvariant();
            lock (_failedFiles)
            {
                return _failedFiles.Contains(normalizedPath);
            }
        }

        private void MarkAsProcessed(string filePath)
        {
            var normalizedPath = filePath.ToLowerInvariant();
            lock (_processedFiles)
            {
                _processedFiles.Add(normalizedPath);
            }
            SaveProcessedFiles();
        }

        private void MarkAsFailed(string filePath)
        {
            var normalizedPath = filePath.ToLowerInvariant();
            lock (_failedFiles)
            {
                _failedFiles.Add(normalizedPath);
            }
            _failedAttempts.TryRemove(normalizedPath, out _);
        }

        private void LoadProcessedFiles()
        {
            if (!File.Exists(_processedFilesPath))
                return;

            try
            {
                var lines = File.ReadAllLines(_processedFilesPath);
                lock (_processedFiles)
                {
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _processedFiles.Add(line.Trim().ToLowerInvariant());
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveProcessedFiles()
        {
            try
            {
                lock (_processedFiles)
                {
                    File.WriteAllLines(_processedFilesPath, _processedFiles);
                }
            }
            catch { }
        }

        public void ClearProcessedHistory()
        {
            lock (_processedFiles)
            {
                _processedFiles.Clear();
            }
            
            lock (_failedFiles)
            {
                _failedFiles.Clear();
            }

            _failedAttempts.Clear();
            
            if (File.Exists(_processedFilesPath))
            {
                try
                {
                    File.Delete(_processedFilesPath);
                }
                catch { }
            }
        }

        public void RetryFailedFiles()
        {
            List<string> filesToRetry;
            
            lock (_failedFiles)
            {
                filesToRetry = _failedFiles.ToList();
                _failedFiles.Clear();
            }

            foreach (var file in filesToRetry)
            {
                if (File.Exists(file))
                {
                    EnqueueForPrinting(file);
                }
            }
        }

        public int GetProcessedCount()
        {
            lock (_processedFiles)
            {
                return _processedFiles.Count;
            }
        }

        public int GetFailedCount()
        {
            lock (_failedFiles)
            {
                return _failedFiles.Count;
            }
        }

        public bool IsMonitoring => _isRunning;

        public void Dispose()
        {
            StopMonitoring();
            GC.SuppressFinalize(this);
        }
    }
}
