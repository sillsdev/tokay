using System.Windows.Forms;
using Knockout.Net;

namespace TestApp
{
	public partial class MainForm : Form
	{
		private readonly KOControl _ko;
		private readonly MainViewModel _mainViewModel;

		public MainForm()
		{
			InitializeComponent();

			_ko = new KOControl(GetObject, FileLocator.GetFileDistributedWithApplication("MainView.html"));
			Controls.Add(_ko);
			_ko.Dock = DockStyle.Fill;
			_mainViewModel = new MainViewModel();
		}

		private object GetObject(string name)
		{
			switch (name)
			{
				case "Main":
					return _mainViewModel;
			}

			return null;
		}
	}
}
