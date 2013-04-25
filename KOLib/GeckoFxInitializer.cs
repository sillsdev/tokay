using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TokaySharp
{
    public class GeckoFxInitializer
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetDllDirectory(string lpPathName);
    
        public static void SetUpXulRunner()
        {
	        if (Gecko.Xpcom.IsInitialized)
		        return;

            string xulRunnerPath = Path.Combine(DirectoryOfApplicationOrSolution, "xulrunner");
            if (!Directory.Exists(xulRunnerPath))
            {
                //if this is a programmer, go look in the lib directory
                xulRunnerPath = Path.Combine(DirectoryOfApplicationOrSolution,
                                             Path.Combine("lib", "xulrunner"));
            }
            //Review: an early tester found that wrong xpcom was being loaded. The following solution is from http://www.geckofx.org/viewtopic.php?id=74&action=new
            SetDllDirectory(xulRunnerPath);

	        try
	        {
				Gecko.Xpcom.Initialize(xulRunnerPath);
	        }
	        catch (Exception)
	        {
#if DEBUG
				MessageBox.Show("Make sure the directory holding XulRunner 18 is at lib/XulRunner. We'll open a browser there for you now.");
				Process.Start("http://ftp.mozilla.org/pub/mozilla.org/xulrunner/releases/18.0.2/runtimes/");
		        Environment.FailFast("Exiting Tokay");
#endif

		        throw;
	        }
	        
        }

        /// <summary>
        /// Gives the directory of either the project folder (if running from visual studio), or
        /// the installation folder.  Helpful for finding templates and things; by using this,
        /// you don't have to copy those files into the build directory during development.
        /// It assumes your build directory has "output" as part of its path.
        /// </summary>
        /// <returns></returns>
        public static string DirectoryOfApplicationOrSolution
        {
            get
            {
                string path = DirectoryOfTheApplicationExecutable;
                char sep = Path.DirectorySeparatorChar;
                int i = path.ToLower().LastIndexOf(sep + "output" + sep);

                if (i > -1)
                {
                    path = path.Substring(0, i + 1);
                }
                return path;
            }
        }

        public static string DirectoryOfTheApplicationExecutable
        {
            get
            {
                string path;
                bool unitTesting = Assembly.GetEntryAssembly() == null;
                if (unitTesting)
                {
                    path = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
                    path = Uri.UnescapeDataString(path);
                }
                else
                {
                    path = Assembly.GetEntryAssembly().Location;
                }
                return Directory.GetParent(path).FullName;
            }
        }
    }  
}