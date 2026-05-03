using System.Windows.Forms;

namespace DzfpPdfPrinter
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            var startMinimized = args.Contains("-minimized");
            var autoStartMonitor = args.Contains("-autostart");
            Application.Run(new MainForm(startMinimized, autoStartMonitor));
        }
    }
}
