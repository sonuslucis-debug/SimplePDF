using System;
using System.Windows.Forms;

namespace SimplePdfReader
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string? initialFile = args.Length > 0 ? args[0] : null;
            Application.Run(new MainForm(initialFile));
        }
    }
}
