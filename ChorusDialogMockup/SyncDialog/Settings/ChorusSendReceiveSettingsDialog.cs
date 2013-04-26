using System.Windows.Forms;
using Tokay;

namespace ChorusDialogMockup.SyncDialog.Settings
{
	public partial class ChorusSendReceiveSettingsDialog : Form
	{
		private readonly KOControl _ko;

		public ChorusSendReceiveSettingsModel Model { get; set; }

		public ChorusSendReceiveSettingsDialog()
        {
            InitializeComponent();

			_ko = new KOControl(GetObject, FileLocator.GetFileDistributedWithApplication("ChorusSendReceiveSettings.html"));
			Controls.Add(_ko);
			_ko.Dock = DockStyle.Fill;
			Model = new ChorusSendReceiveSettingsModel();
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
				case "ChorusSendReceiveSettingsModel":
					return Model;
			}

			return null;
		}
	}
}
