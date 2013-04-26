using System.Windows.Forms;
using System.Windows.Input;
using ChorusDialogMockup.SyncDialog.Settings;
using Tokay;

namespace ChorusDialogMockup.SyncDialog.Start
{
	public class SyncStartViewModel : ObservableObject
	{
		private string _commitMessage;
		private readonly ICommand _useUSBFlashDriveCommand;
		private readonly ICommand _useInternetCommand;
		private readonly ICommand _useChorusHubCommand;
		private readonly ICommand _showSettingsDialogCommand;
		private string _usbFlashDriveStatusMessage;
		private string _internetStatusMessage;
		private string _chorusHubStatusMessage;
		private readonly ChorusSendReceiveSettingsModel _sendReceiveSettings = new ChorusSendReceiveSettingsModel();
		private readonly Timer _simulateDoneCheckingTimer;
		private bool _interentIsAvailable;
		private bool _usbAvailable;

		public SyncStartViewModel(SyncViewModel syncViewModel)
		{
			_simulateDoneCheckingTimer = new Timer { Interval = 2000, Enabled = true };
			_simulateDoneCheckingTimer.Tick += SimulateDoneCheckingTimerTick;

			_useUSBFlashDriveCommand = new RelayCommand(() => syncViewModel.StartSync(), () => UsbAvailable);
			_useInternetCommand = new RelayCommand(() => syncViewModel.StartSync(), () => InternetAvailable);
			_useChorusHubCommand = new RelayCommand(() => syncViewModel.StartSync(), () => false);
			_showSettingsDialogCommand = new RelayCommand(() => LaunchSettingsDialog());
			_usbFlashDriveStatusMessage = "Checking...";
			_internetStatusMessage = "Checking...";
			_chorusHubStatusMessage = "Checking...";
		}

		public bool UsbAvailable
		{
			get { return _usbAvailable; }
			set { Set(() => UsbAvailable, ref _usbAvailable, value); }
		}

		public bool InternetAvailable
		{
			get { return _interentIsAvailable; }
			set { Set(() => InternetAvailable, ref _interentIsAvailable, value); }
		}

		private void SimulateDoneCheckingTimerTick(object sender, System.EventArgs e)
		{
			_simulateDoneCheckingTimer.Enabled = false;
			USBFlashDriveStatusMessage = "Found at z:/";
			InternetStatusMessage = "Ready";
			ChorusHubStatusMessage = "Not found";

			UsbAvailable = true;
			InternetAvailable = true;
		}

		private void LaunchSettingsDialog()
		{
			var chorusSendReceiveSettingsDialog = new ChorusSendReceiveSettingsDialog {Model = _sendReceiveSettings};
			chorusSendReceiveSettingsDialog.Show();
		}

		public string CommitMessage
		{
			get { return _commitMessage; }
			set { Set(() => CommitMessage, ref _commitMessage, value); }
		}

		public ICommand UseUSBFlashDriveCommand
		{
			get { return _useUSBFlashDriveCommand; }
		}

		public string USBFlashDriveStatusMessage
		{
			get { return _usbFlashDriveStatusMessage; }
			set { Set(() => USBFlashDriveStatusMessage, ref _usbFlashDriveStatusMessage, value); }
		}

		public ICommand UseInternetCommand
		{
			get { return _useInternetCommand; }
		}

		public ICommand ShowSettingsDialogCommand
		{
			get { return _showSettingsDialogCommand; }
		}

		public string InternetStatusMessage
		{
			get { return _internetStatusMessage; }
			set { Set(() => InternetStatusMessage, ref _internetStatusMessage, value); }
		}

		public ICommand UseChorusHubCommand
		{
			get { return _useChorusHubCommand; }
		}

		public string ChorusHubStatusMessage
		{
			get { return _chorusHubStatusMessage; }
			set { Set(() => ChorusHubStatusMessage, ref _chorusHubStatusMessage, value); }
		}
	}
}
