using System;
using System.Windows.Forms;

namespace VirtualDesktopManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new Form1(new Configuration()))
            {
                Application.Run(form);
            }
        }
    }
}
