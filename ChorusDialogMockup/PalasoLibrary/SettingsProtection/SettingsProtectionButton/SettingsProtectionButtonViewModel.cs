using System.Windows.Input;
using Tokay;

namespace ChorusDialogMockup.PalasoLibrary.SettingsProtection.SettingsProtectionButton
{
	public class SettingsProtectionButtonViewModel : ObservableObject
	{
		private readonly ICommand _launchSettingsProtectionDialog;

		public SettingsProtectionButtonViewModel()
		{
			_launchSettingsProtectionDialog = new RelayCommand(() => LaunchSettingsDialog(), () => true);
		}

		private void LaunchSettingsDialog()
		{
			var dlg = new  ChorusDialogMockup.PalasoLibrary.SettingsProtection.SettingsProtectionDialog.SettingsProtectionDialog();
			dlg.Show();
		}

		public ICommand LaunchSettingsProtectionDialog
		{
			get { return _launchSettingsProtectionDialog; }
		}
	}
}
