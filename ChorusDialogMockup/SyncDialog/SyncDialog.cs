using System.Windows.Forms;
using Knockout.Net;

namespace ChorusDialogMockup.SyncDialog
{
    public partial class SyncDialog : Form
    {
		private readonly KOControl _ko;
	    private readonly SyncViewModel _syncViewModel;

	    public SyncDialog()
        {
            InitializeComponent();

			_ko = new KOControl(GetObject, FileLocator.GetFileDistributedWithApplication("SyncView.html"));
			Controls.Add(_ko);
			_ko.Dock = DockStyle.Fill;
			_syncViewModel = new SyncViewModel();
			_ko.CloseDialogRequested += _ko_CloseDialogRequested;
		}

		private void _ko_CloseDialogRequested(object sender, CloseDialogRequestedEventArgs e)
		{
			DialogResult = e.DialogResult;
			Close();
		}


		private object GetObject(string name)
		{
			switch (name)
			{
				case "SyncViewModel":
					return _syncViewModel;
			}

			return null;
		}
    }
}
