using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TokaySharp;

namespace ChorusDialogMockup
{
	public class ChorusSendReceiveSettingsModel : ObservableObject
	{
		private string _userName;
		private string _internetUrl;
		private string _internetProjectId;
		private string _internetLogin;
		private string _internetPassword;
		private readonly ICommand _okCommand;
		private IChorusSendReceiveSettingsView _view;

		public ChorusSendReceiveSettingsModel()
		{
			_okCommand = new RelayCommand(() => OkClicked());

		}

		public void SetView(IChorusSendReceiveSettingsView view)
		{
			_view = view;
		}
		private object OkClicked()
		{
			Debug.WriteLine("User name is: " + UserName);
			Debug.WriteLine("Internet settings are Url: {0}; Project {1}; Login {2}; Passsword {3}",
				InternetUrl, InternetProjectId, InternetLogin, InternetPassword);
			if (_view != null)
				_view.Close();
			return null;
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

		public ICommand OkCommand
		{
			get { return _okCommand; }
		}
	}

	/// <summary>
	/// Interface implemented for real by the actual dialog, and potentially also by test double.
	/// Defines what the model knows about the dialog directly.
	/// </summary>
	public interface IChorusSendReceiveSettingsView
	{
		void Close();
	}
}
