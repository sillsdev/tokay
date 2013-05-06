using System.Windows.Forms;
using Tokay;

namespace ChorusDialogMockup.SyncDialog
{
    public partial class SyncDialog : Form
    {
		private readonly TokayControl _tokay;
	    private readonly SyncViewModel _syncViewModel;

	    public SyncDialog()
        {
            InitializeComponent();

			_tokay = new TokayControl(FileLocator.DirectoryOfApplicationOrSolution, GetObject, FileLocator.GetFileDistributedWithApplication("SyncView.html"));
			Controls.Add(_tokay);
			_tokay.Dock = DockStyle.Fill;
			_syncViewModel = new SyncViewModel();
			_tokay.CloseDialogRequested += _tokay_CloseDialogRequested;
		}

		private void _tokay_CloseDialogRequested(object sender, CloseDialogRequestedEventArgs e)
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
