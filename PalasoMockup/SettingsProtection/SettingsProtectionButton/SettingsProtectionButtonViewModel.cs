using System.Windows.Input;
using Tokay;

namespace PalasoMockup.SettingsProtection.SettingsProtectionButton
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
			var dlg = new  SettingsProtectionDialog.SettingsProtectionDialog();
			dlg.Show();
		}

		public ICommand LaunchSettingsProtectionDialog
		{
			get { return _launchSettingsProtectionDialog; }
		}
	}
}
