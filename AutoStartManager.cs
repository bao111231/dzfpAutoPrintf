using Microsoft.Win32;

namespace DzfpPdfPrinter
{
    public static class AutoStartManager
    {
        private const string AppName = "DzfpPdfPrinter";
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        public static bool IsEnabled
        {
            get
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                    if (key?.GetValue(AppName) != null)
                    {
                        return true;
                    }
                    
                    var shortcutPath = GetStartupShortcutPath();
                    return System.IO.File.Exists(shortcutPath);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool EnableAutoStart(string exePath)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                if (key == null)
                {
                    Registry.CurrentUser.CreateSubKey(RegistryPath);
                }

                using var runKey = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                runKey?.SetValue(AppName, $"\"{exePath}\" -minimized -autostart", RegistryValueKind.String);
                
                SaveAutoStartSetting(true);
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"启用开机自启动失败: {ex.Message}", ex);
            }
        }

        public static bool DisableAutoStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                if (key?.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                }

                RemoveStartupShortcut();
                
                SaveAutoStartSetting(false);
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"禁用开机自启动失败: {ex.Message}", ex);
            }
        }

        private static void CreateStartupShortcut(string exePath)
        {
            try
            {
                var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var shortcutPath = System.IO.Path.Combine(startupFolder, $"{AppName}.lnk");

                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                dynamic shell = System.Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                
                shortcut.TargetPath = exePath;
                shortcut.Arguments = "-minimized";
                shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath);
                shortcut.Description = "DZFP PDF 自动打印工具 - 开机自动启动";
                shortcut.WindowStyle = 7; 
                shortcut.IconLocation = $"{exePath},0";
                
                shortcut.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建快捷方式失败: {ex.Message}");
            }
        }

        private static void RemoveStartupShortcut()
        {
            try
            {
                var shortcutPath = GetStartupShortcutPath();
                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }
            }
            catch { }
        }

        private static string GetStartupShortcutPath()
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return System.IO.Path.Combine(startupFolder, $"{AppName}.lnk");
        }

        private static void SaveAutoStartSetting(bool enabled)
        {
            try
            {
                var appData = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "DzfpPdfPrinter"
                );
                
                if (!System.IO.Directory.Exists(appData))
                {
                    System.IO.Directory.CreateDirectory(appData);
                }

                var settingsPath = System.IO.Path.Combine(appData, "autostart.txt");
                var content = enabled ? "enabled" : "disabled";
                System.IO.File.WriteAllText(settingsPath, content);
            }
            catch { }
        }

        public static bool LoadAutoStartSetting()
        {
            try
            {
                var appData = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "DzfpPdfPrinter"
                );
                
                var settingsPath = System.IO.Path.Combine(appData, "autostart.txt");
                
                if (System.IO.File.Exists(settingsPath))
                {
                    var content = System.IO.File.ReadAllText(settingsPath).Trim().ToLowerInvariant();
                    return content == "enabled";
                }
            }
            catch { }

            return false;
        }

        public static string GetStatusInfo()
        {
            var info = new List<string>();
            
            info.Add("=== 开机自启动状态 ===");
            info.Add("");
            
            var registryEnabled = false;
            var shortcutExists = false;
            
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                registryEnabled = key?.GetValue(AppName) != null;
            }
            catch { }
            
            try
            {
                shortcutExists = System.IO.File.Exists(GetStartupShortcutPath());
            }
            catch { }
            
            if (registryEnabled || shortcutExists)
            {
                info.Add("状态: ✓ 已启用");
                
                if (registryEnabled)
                {
                    info.Add("注册表项: ✓ 已设置");
                }
                
                if (shortcutExists)
                {
                    info.Add("启动快捷方式: ✓ 已创建");
                    info.Add($"  路径: {GetStartupShortcutPath()}");
                }
            }
            else
            {
                info.Add("状态: ✗ 未启用");
                info.Add("");
                info.Add("提示:");
                info.Add("- 勾选'开机自启动'复选框即可启用");
                info.Add("- 程序将在系统托盘中运行");
            }

            info.Add("");
            info.Add("说明:");
            info.Add("- 使用注册表方式启动");
            info.Add("- 启动参数: -minimized -autostart");
            info.Add("- 程序将在系统托盘中运行并自动开始监控");

            return string.Join(System.Environment.NewLine, info);
        }
    }
}
