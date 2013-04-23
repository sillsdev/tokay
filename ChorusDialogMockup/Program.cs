using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Knockout.Net;

namespace ChorusDialogMockup
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			KOControl.InitializeGeckoFx();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new StartDialog());
        }
    }
}
