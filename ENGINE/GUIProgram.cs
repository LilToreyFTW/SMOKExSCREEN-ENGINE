using System;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    internal static class GUIProgram
    {
        [STAThread]
        static void Main()
        {
            // Catch all unhandled UI thread exceptions — show dialog instead of silent crash
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                    "SmokeScreen ENGINE — Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            // Catch background thread exceptions
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(
                    $"A fatal error occurred:\n\n{ex?.Message ?? e.ExceptionObject?.ToString()}",
                    "SmokeScreen ENGINE — Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            ApplicationConfiguration.Initialize();
            Application.Run(new HubForm());
        }
    }
}
