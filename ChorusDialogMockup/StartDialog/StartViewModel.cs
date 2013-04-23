using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Input;
using Knockout.Net;

namespace ChorusDialogMockup.StartDialog
{
	public class StartViewModel : ObservableObject
	{
		private string _commitMessage;
		private readonly ICommand _useUSBFlashDriveCommand;
		private readonly ICommand _useInternetCommand;
		private readonly ICommand _useChorusHubCommand;
		private readonly ICommand _showSettingsDialogCommand;
		private string _usbFlashDriveStatusMessage;
		private string _internetStatusMessage;
		private string _chorusHubStatusMessage;
		private ChorusSendReceiveSettingsModel _sendReceiveSettings = new ChorusSendReceiveSettingsModel();
		private Timer _simulateDoneCheckingTimer;
		private bool _interentIsAvailable;

		public StartViewModel()
		{
			_simulateDoneCheckingTimer = new System.Windows.Forms.Timer { Interval = 2000, Enabled = true };
			_simulateDoneCheckingTimer.Tick += SimulateDoneCheckingTimerTick;

			_useUSBFlashDriveCommand = new RelayCommand(() => Debug.WriteLine("Use Flash Drive Clicked"), () => InternetAvailable);


			_useInternetCommand = new RelayCommand(() => Debug.WriteLine("Use Internet Clicked"), () => InternetAvailable);
			_useChorusHubCommand = new RelayCommand(()=>Debug.WriteLine("Use Chorus Hub Clicked"), ()=>false);
			_showSettingsDialogCommand = new RelayCommand(() => LaunchSettingsDialog());
			_usbFlashDriveStatusMessage = "Checking...";
			_internetStatusMessage = "Checking...";
			_chorusHubStatusMessage = "Checking...";
		}



		public bool InternetAvailable
		{
			get { return false; }
			set { Set(() => InternetAvailable, ref _interentIsAvailable, value); }
		}

		private void SimulateDoneCheckingTimerTick(object sender, System.EventArgs e)
		{
			_simulateDoneCheckingTimer.Enabled = false;
			USBFlashDriveStatusMessage = "Found at z:/";
			InternetStatusMessage = "Ready";
			ChorusHubStatusMessage = "Not found";

			InternetAvailable = true;
		}


		private object LaunchSettingsDialog()
		{
			var chorusSendReceiveSettingsDialog = new ChorusSendReceiveSettingsDialog();
			chorusSendReceiveSettingsDialog.Model = _sendReceiveSettings;
			chorusSendReceiveSettingsDialog.Show();
			return null; // what object is it supposed to return??
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
