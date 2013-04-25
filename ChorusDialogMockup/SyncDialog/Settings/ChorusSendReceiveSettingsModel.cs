using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TokaySharp;

namespace ChorusDialogMockup
{
	public class ChorusSendReceiveSettingsModel : ObservableObject
	{
		private string _userName;

		public string UserName
		{
			get { return _userName; }
			set { Set(() => UserName, ref _userName, value); }
		}
	}
}
