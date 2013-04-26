using System.Windows.Forms;
using Tokay;

namespace ChorusDialogMockup.SyncDialog.Settings
{
	public partial class ChorusSendReceiveSettingsDialog : Form
	{
		private readonly TokayControl _tokay;

		public ChorusSendReceiveSettingsModel Model { get; set; }

		public ChorusSendReceiveSettingsDialog()
        {
            InitializeComponent();

			_tokay = new TokayControl(GetObject, FileLocator.GetFileDistributedWithApplication("ChorusSendReceiveSettings.html"));
			Controls.Add(_tokay);
			_tokay.Dock = DockStyle.Fill;
			Model = new ChorusSendReceiveSettingsModel();
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
				case "ChorusSendReceiveSettingsModel":
					return Model;
			}

			return null;
		}
	}
}
