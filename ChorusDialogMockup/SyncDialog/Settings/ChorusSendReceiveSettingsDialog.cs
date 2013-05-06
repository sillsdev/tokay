using System.Windows.Forms;
using Tokay;

namespace ChorusDialogMockup.SyncDialog.Settings
{
	public partial class ChorusSendReceiveSettingsDialog : Form
	{
		private readonly TokayControl _tokay;
		private ModelSandboxer<ChorusSendReceiveSettingsModel> m_modelSandboxer;

		public ChorusSendReceiveSettingsModel Model
		{
			get { return m_modelSandboxer.WorkingModel; }
			set { m_modelSandboxer = new ModelSandboxer<ChorusSendReceiveSettingsModel>(value); }
		}

		public ChorusSendReceiveSettingsDialog()
        {
            InitializeComponent();

			_tokay = new TokayControl(FileLocator.DirectoryOfApplicationOrSolution, GetObject, FileLocator.GetFileDistributedWithApplication("ChorusSendReceiveSettings.html"));
			Controls.Add(_tokay);
			_tokay.Dock = DockStyle.Fill;
			Model = new ChorusSendReceiveSettingsModel();
			_tokay.CloseDialogRequested += _tokay_CloseDialogRequested;
		}

		private void _tokay_CloseDialogRequested(object sender, CloseDialogRequestedEventArgs e)
		{
			DialogResult = e.DialogResult;
			// Review: should this logic be in the {View}Model rather than here?
			if (DialogResult == DialogResult.OK)
				m_modelSandboxer.CopyBackToOriginal();
			m_modelSandboxer.OriginalModel.Closing(DialogResult);
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
