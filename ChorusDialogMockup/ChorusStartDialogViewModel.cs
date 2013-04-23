using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Knockout.Net;

namespace ChorusDialogMockup
{
	public class ChorusStartDialogViewModel : ObservableObject
	{
		private readonly ICommand _useUSBFlashDriveCommand;
		

		public ChorusStartDialogViewModel()
		{
			_useUSBFlashDriveCommand = new RelayCommand(()=>Debug.WriteLine("Use Flash Drive Clicked"));
		}

		public ICommand UseUSBFlashDriveCommand
		{
			get { return _useUSBFlashDriveCommand; }
		}
	}
}
