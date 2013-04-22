using System;
using System.IO;
using System.Windows.Forms;
using Knockout.Net;

namespace TestApp
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
			//Gecko.Xpcom.Initialize("..\\lib\\xulrunner");
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
