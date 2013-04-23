using System.Windows.Forms;
using Knockout.Net;

namespace ChorusDialogMockup.StartDialog
{
    public partial class StartDialog : Form
    {
		private readonly KOControl _ko;
	    private readonly StartViewModel _startViewModel;

	    public StartDialog()
        {
            InitializeComponent();

			_ko = new KOControl(GetObject, FileLocator.GetFileDistributedWithApplication("StartView.html"));
			Controls.Add(_ko);
			_ko.Dock = DockStyle.Fill;
			_startViewModel = new StartViewModel();
		}


		private object GetObject(string name)
		{
			switch (name)
			{
				case "StartViewModel":
					return _startViewModel;
			}

			return null;
		}
    }
}
