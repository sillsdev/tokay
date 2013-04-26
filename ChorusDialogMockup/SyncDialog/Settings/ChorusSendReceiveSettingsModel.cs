using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Input;
using Tokay;

namespace ChorusDialogMockup.SyncDialog.Settings
{
	public class ChorusSendReceiveSettingsModel : ObservableObject, ICopyDataTo<ChorusSendReceiveSettingsModel>
	{
		private string _userName;
		private string _internetUrl;
		private string _internetProjectId;
		private string _internetLogin;
		private string _internetPassword;
		private bool _internetEnabled;
		private bool _chorusHubEnabled;
		private bool _showPasswordText;

		public void Closing(DialogResult result)
		{
			Debug.WriteLine("Closing settings dialog with result " + result);
			Debug.WriteLine("User name is: " + UserName);
			Debug.WriteLine("Internet settings are Url: {0}; Project {1}; Login {2}; Passsword {3}",
				InternetUrl, InternetProjectId, InternetLogin, InternetPassword);
		}

		public string InternetUrl
		{
			get { return _internetUrl; }
			set { Set(() => InternetUrl, ref _internetUrl, value); }
		}
		public string InternetProjectId
		{
			get { return _internetProjectId; }
			set { Set(() => InternetProjectId, ref _internetProjectId, value); }
		}
		public string InternetLogin
		{
			get { return _internetLogin; }
			set { Set(() => InternetLogin, ref _internetLogin, value); }
		}
		public string InternetPassword
		{
			get { return _internetPassword; }
			set { Set(() => InternetPassword, ref _internetPassword, value); }
		}

		public string UserName
		{
			get { return _userName; }
			set { Set(() => UserName, ref _userName, value); }
		}

		// Should we offer the user Internet S/R at all?
		public bool InternetEnabled
		{
			get { return _internetEnabled; }
			set { Set(() => InternetEnabled, ref _internetEnabled, value); }
		}

		// Should we offer the user Chorus Hub S/R at all?
		public bool ChorusHubEnabled
		{
			get { return _chorusHubEnabled; }
			set { Set(() => ChorusHubEnabled, ref _chorusHubEnabled, value); }
		}

		public bool ShowPasswordText
		{
			get { return _showPasswordText; }
			set { Set(() => ShowPasswordText, ref _showPasswordText, value); }
		}

		public void CopyTo(ChorusSendReceiveSettingsModel destination)
		{
			destination.InternetEnabled = InternetEnabled;
			destination.InternetLogin = InternetLogin;
			destination.InternetPassword = InternetPassword;
			destination.InternetProjectId = InternetProjectId;
			destination.InternetUrl = InternetUrl;
			destination.ChorusHubEnabled = ChorusHubEnabled;
			destination.ShowPasswordText = ShowPasswordText;
		}
	}
}
