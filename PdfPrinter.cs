using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace DzfpPdfPrinter
{
    public static class PdfPrinter
    {
        private static string? _sumatraPdfPath;
        private static string? _customSumatraPath;
        private const string SettingsFile = "pdf_tools_settings.txt";

        public enum PrintScaleMode
        {
            FitToPage,
            ShrinkToFit,
            None
        }

        private static PrintScaleMode _currentScaleMode = PrintScaleMode.FitToPage;

        public static bool PrintPdf(string filePath, string? printerName = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("文件不存在", filePath);
            }

            var errors = new List<string>();

            try
            {
                if (TryPrintWithShellExecute(filePath, printerName, out var error1))
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(error1)) errors.Add(error1);
            }
            catch (Exception ex)
            {
                errors.Add($"ShellExecute: {ex.Message}");
            }

            try
            {
                if (TryPrintWithSumatraPDF(filePath, printerName, out var error2))
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(error2)) errors.Add(error2);
            }
            catch (Exception ex)
            {
                errors.Add($"SumatraPDF: {ex.Message}");
            }

            try
            {
                if (TryPrintWithAdobeReader(filePath, printerName, out var error3))
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(error3)) errors.Add(error3);
            }
            catch (Exception ex)
            {
                errors.Add($"Adobe Reader: {ex.Message}");
            }

            throw new Exception(
                $"所有打印方式均失败:\n{string.Join("\n", errors)}\n\n" +
                "建议: 安装 Adobe Reader 或 SumatraPDF，或手动指定 PDF 工具路径",
                null
            );
        }

        private static bool TryPrintWithShellExecute(string filePath, string? printerName, out string? error)
        {
            error = null;

            try
            {
                var verb = string.IsNullOrEmpty(printerName) ? "print" : "printto";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = verb,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                if (!string.IsNullOrEmpty(printerName) && verb == "printto")
                {
                    startInfo.Arguments = $"\"{printerName}\"";
                }

                using var process = Process.Start(startInfo);
                
                if (process != null)
                {
                    process.WaitForExit(30000);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return false;
        }

        private static bool TryPrintWithSumatraPDF(string filePath, string? printerName, out string? error)
        {
            error = null;

            try
            {
                var sumatraPath = FindSumatraPDF();
                if (string.IsNullOrEmpty(sumatraPath))
                {
                    error = "未找到 SumatraPDF";
                    return false;
                }

                var scaleSetting = GetScaleModeSetting();
                
                var args = new List<string> 
                { 
                    "-print-to-default", 
                    "-silent",
                    $"-print-settings \"{scaleSetting}\"",
                    $"\"{filePath}\"" 
                };

                if (!string.IsNullOrEmpty(printerName))
                {
                    args[0] = $"-print-to \"{printerName}\"";
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = sumatraPath,
                    Arguments = string.Join(" ", args),
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(sumatraPath)
                };

                using var process = Process.Start(startInfo);

                if (process != null)
                {
                    process.WaitForExit(30000);
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return false;
        }

        private static bool TryPrintWithAdobeReader(string filePath, string? printerName, out string? error)
        {
            error = null;

            try
            {
                var adobePath = FindAdobeReader();
                if (string.IsNullOrEmpty(adobePath))
                {
                    error = "未找到 Adobe Reader";
                    return false;
                }

                var args = $"/t \"{filePath}\"";
                if (!string.IsNullOrEmpty(printerName))
                {
                    args = $"/t \"{filePath}\" \"{printerName}\"";
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = adobePath,
                    Arguments = args,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                
                if (process != null)
                {
                    process.WaitForExit(60000);
                    
                    Thread.Sleep(2000);
                    
                    KillAcrobatProcess();
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return false;
        }

        public static string? FindSumatraPDF()
        {
            if (_customSumatraPath != null && File.Exists(_customSumatraPath))
            {
                return _customSumatraPath;
            }

            if (_sumatraPdfPath != null && File.Exists(_sumatraPdfPath))
            {
                return _sumatraPdfPath;
            }

            LoadCustomPath();

            if (_customSumatraPath != null && File.Exists(_customSumatraPath))
            {
                _sumatraPdfPath = _customSumatraPath;
                return _sumatraPdfPath;
            }

            var possiblePaths = new[]
            {
                @"C:\Program Files\SumatraPDF\SumatraPDF.exe",
                @"C:\Program Files (x86)\SumatraPDF\SumatraPDF.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SumatraPDF", "SumatraPDF.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SumatraPDF", "SumatraPDF.exe"),
                @"C:\Users\" + Environment.UserName + @"\AppData\Local\SumatraPDF\SumatraPDF.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SumatraPDF", "SumatraPDF.exe"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _sumatraPdfPath = path;
                    SaveCustomPath(path);
                    return path;
                }
            }

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\SumatraPDF.exe");
                if (key?.GetValue("") is string registryPath && File.Exists(registryPath))
                {
                    _sumatraPdfPath = registryPath;
                    SaveCustomPath(registryPath);
                    return registryPath;
                }
            }
            catch { }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\SumatraPDF.exe");
                if (key?.GetValue("") is string registryPath && File.Exists(registryPath))
                {
                    _sumatraPdfPath = registryPath;
                    SaveCustomPath(registryPath);
                    return registryPath;
                }
            }
            catch { }

            var searchPaths = SearchForSumatraPDF();
            if (searchPaths.Count > 0)
            {
                _sumatraPdfPath = searchPaths[0];
                SaveCustomPath(searchPaths[0]);
                return searchPaths[0];
            }

            _sumatraPdfPath = null;
            return null;
        }

        private static List<string> SearchForSumatraPDF()
        {
            var found = new List<string>();

            try
            {
                var drives = Environment.GetLogicalDrives();
                foreach (var drive in drives)
                {
                    try
                    {
                        var programDirs = new[]
                        {
                            Path.Combine(drive, "Program Files"),
                            Path.Combine(drive, "Program Files (x86)")
                        };

                        foreach (var dir in programDirs)
                        {
                            if (Directory.Exists(dir))
                            {
                                var sumatraDir = Path.Combine(dir, "SumatraPDF");
                                if (Directory.Exists(sumatraDir))
                                {
                                    var exePath = Path.Combine(sumatraDir, "SumatraPDF.exe");
                                    if (File.Exists(exePath) && !found.Contains(exePath))
                                    {
                                        found.Add(exePath);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            try
            {
                var desktopShortcuts = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "*.lnk"
                );

                var shortcutDir = Path.GetDirectoryName(desktopShortcuts);
                if (shortcutDir != null && Directory.Exists(shortcutDir))
                {
                    var shortcuts = Directory.GetFiles(shortcutDir, "*Sumatra*.lnk", SearchOption.TopDirectoryOnly);
                    foreach (var shortcut in shortcuts)
                    {
                        try
                        {
                            var target = ResolveShortcut(shortcut);
                            if (!string.IsNullOrEmpty(target) && target.EndsWith("SumatraPDF.exe", StringComparison.OrdinalIgnoreCase) 
                                && File.Exists(target) && !found.Contains(target))
                            {
                                found.Add(target);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return found;
        }

        private static string? ResolveShortcut(string shortcutPath)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return null;

                dynamic shell = Activator.CreateInstance(shellType)!;
                var shortcut = shell.CreateShortcut(shortcutPath);
                return shortcut.TargetPath.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static void SetPrintScaleMode(PrintScaleMode mode)
        {
            _currentScaleMode = mode;
            SaveScaleModeSetting(mode);
        }

        public static PrintScaleMode GetPrintScaleMode() => _currentScaleMode;

        private static string GetScaleModeSetting()
        {
            LoadScaleModeSetting();
            
            switch (_currentScaleMode)
            {
                case PrintScaleMode.FitToPage:
                    return "fit";
                case PrintScaleMode.ShrinkToFit:
                    return "shrink";
                case PrintScaleMode.None:
                    default:
                    return "none";
            }
        }

        private static void SaveScaleModeSetting(PrintScaleMode mode)
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                
                List<string> lines = new List<string>();
                if (File.Exists(settingsPath))
                {
                    var existingLines = File.ReadAllLines(settingsPath);
                    foreach (var line in existingLines)
                    {
                        if (!line.StartsWith("ScaleMode="))
                        {
                            lines.Add(line);
                        }
                    }
                }

                lines.Add($"ScaleMode={mode}");
                File.WriteAllLines(settingsPath, lines);
            }
            catch { }
        }

        private static void LoadScaleModeSetting()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("ScaleMode="))
                        {
                            var value = line.Substring("ScaleMode=".Length).Trim();
                            if (Enum.TryParse<PrintScaleMode>(value, out var mode))
                            {
                                _currentScaleMode = mode;
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        public static void SetCustomSumatraPath(string? path)
        {
            _customSumatraPath = path;
            
            if (!string.IsNullOrEmpty(path))
            {
                _sumatraPdfPath = path;
                SaveCustomPath(path);
            }
            else
            {
                _sumatraPdfPath = null;
                ClearCustomPath();
            }
        }

        public static string? GetCustomSumatraPath() => _customSumatraPath ?? _sumatraPdfPath;

        private static void LoadCustomPath()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("SumatraPath="))
                        {
                            var path = line.Substring("SumatraPath=".Length).Trim();
                            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                            {
                                _customSumatraPath = path;
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private static void SaveCustomPath(string path)
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                var lines = new List<string> { $"SumatraPath={path}" };
                File.WriteAllLines(settingsPath, lines);
            }
            catch { }
        }

        private static void ClearCustomPath()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                if (File.Exists(settingsPath))
                {
                    File.Delete(settingsPath);
                }
            }
            catch { }
        }

        private static string GetSettingsFilePath()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "DzfpPdfPrinter"
            );
            
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            return Path.Combine(appData, SettingsFile);
        }

        private static string? FindAdobeReader()
        {
            var possiblePaths = new[]
            {
                @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                @"C:\Program Files\Adobe\Acrobat DC\Acrobat\AcroRd32.exe",
                @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\AcroRd32.exe",
                @"C:\Program Files\Adobe\Reader 11.0\Reader\AcroRd32.exe",
                @"C:\Program Files (x86)\Adobe\Reader 11.0\Reader\AcroRd32.exe",
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\.pdf");
                if (key?.GetValue("") is string progId && !string.IsNullOrEmpty(progId))
                {
                    using var commandKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Classes\{progId}\shell\open\command");
                    if (commandKey?.GetValue("") is string commandPath)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(commandPath, @"""([^""]+)""");
                        if (match.Success && File.Exists(match.Groups[1].Value))
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private static void KillAcrobatProcess()
        {
            try
            {
                var processes = Process.GetProcessesByName("AcroRd32");
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.CloseMainWindow();
                            
                            if (!process.WaitForExit(3000))
                            {
                                process.Kill();
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static string GetAvailablePrintersInfo()
        {
            var info = new List<string>();
            
            info.Add("=== PDF 打印工具状态 ===");
            info.Add("");
            
            var sumatraPath = FindSumatraPDF();
            if (sumatraPath != null)
            {
                info.Add($"SumatraPDF: ✓ 已安装");
                info.Add($"  路径: {sumatraPath}");
            }
            else
            {
                info.Add("SumatraPDF: ✗ 未检测到");
                info.Add("  可能原因:");
                info.Add("  - 安装在非标准目录");
                info.Add("  - 使用便携版（非安装版）");
                info.Add("");
                info.Add("  解决方案: 点击'浏览'按钮手动指定路径");
            }
            
            info.Add("");
            
            var hasAdobe = FindAdobeReader();
            info.Add($"Adobe Reader: {(hasAdobe != null ? "✓ 已安装" : "✗ 未安装")}");

            info.Add("");
            info.Add("=== 推荐工具 ===");
            info.Add("SumatraPDF (免费、轻量、开源)");
            info.Add("下载: https://www.sumatrapdfreader.org/free-pdf-reader.html");

            return string.Join(Environment.NewLine, info);
        }
    }
}
