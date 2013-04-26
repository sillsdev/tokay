using System.Windows.Forms;
using Tokay;

namespace TestApp
{
	public partial class MainForm : Form
	{
		private readonly TokayControl _tokay;
		private readonly MainViewModel _mainViewModel;

		public MainForm()
		{
			InitializeComponent();

			_tokay = new TokayControl(GetObject, FileLocator.GetFileDistributedWithApplication("MainView.html"));
			Controls.Add(_tokay);
			_tokay.Dock = DockStyle.Fill;
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
