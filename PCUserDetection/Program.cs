using System;
using System.Windows.Forms;

namespace PCUserDetection
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // PerMonitorV2 keeps the window sharp on high DPI displays instead of
            // letting Windows scale a 96 DPI bitmap of it.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UserFaceDetector());
        }
    }
}
