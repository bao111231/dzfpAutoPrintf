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
            Application.Run(new MainForm(startMinimized));
        }
    }
}
