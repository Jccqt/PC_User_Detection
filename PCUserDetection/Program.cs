using System;
using System.Threading;
using System.Windows.Forms;

namespace PCUserDetection
{
    internal static class Program
    {
        /// <summary>The title every dialog in the app is shown under.</summary>
        private const string Title = "PC User Detection";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Anything that gets past a try/catch elsewhere lands in one of these
            // three places, and without a handler each one is a bare .NET crash
            // dialog full of stack frames. Catching them turns that into a
            // sentence the person can act on, and in the one case that is not
            // fatal keeps the window up.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // PerMonitorV2 keeps the window sharp on high DPI displays instead of
            // letting Windows scale a 96 DPI bitmap of it.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new UserFaceDetector());
            }
            catch (Exception ex)
            {
                // The window is built before the message loop starts, so a
                // failure in the constructor never reaches ThreadException.
                Report("The app could not be started.", ex);
            }
        }

        /// <summary>
        /// Reports an exception that escaped an event handler on the UI thread.
        /// The message loop carries on afterwards, so the window stays up and
        /// the failed action is the only thing lost.
        /// </summary>
        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Report("Something went wrong, and that step was not completed.", e.Exception);
        }

        /// <summary>
        /// Reports an exception from any other thread. The runtime is already on
        /// its way down by the time this runs and cannot be talked out of it, so
        /// this only explains the closure rather than preventing it.
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // ExceptionObject is typed as object because a throw is not required
            // to carry an Exception, though in practice it almost always does.
            Exception ex = e.ExceptionObject as Exception;

            Report("Something went wrong, and the app has to close.", ex);
        }

        /// <summary>
        /// Shows one failure the way the rest of the app shows its own: a plain
        /// sentence, with the underlying message kept for whoever it means
        /// something to.
        /// </summary>
        private static void Report(string summary, Exception ex)
        {
            string detail = ex == null ? "No further detail is available." : ex.Message;

            try
            {
                MessageBox.Show(summary + "\n\n" + detail,
                    Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // A machine too far gone to put a dialog on screen is not one
                // this can report to, and throwing from here would replace the
                // crash dialog it exists to avoid.
            }
        }
    }
}
