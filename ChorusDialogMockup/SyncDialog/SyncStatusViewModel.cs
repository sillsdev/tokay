using System;
using System.Windows.Forms;
using Knockout.Net;

namespace ChorusDialogMockup.SyncDialog
{
	public class SyncStatusViewModel : ObservableObject
	{
		private int _progress;
		private readonly Timer _simulateProgressTimer;

		public SyncStatusViewModel()
		{
			_simulateProgressTimer = new Timer {Interval = 1000};
			_simulateProgressTimer.Tick += _simulateProgressTimer_Tick;
		}

		public void StartSync()
		{
			_simulateProgressTimer.Enabled = true;
		}

		private void _simulateProgressTimer_Tick(object sender, EventArgs e)
		{
			Progress += 10;
			if (Progress == 100)
				_simulateProgressTimer.Enabled = false;
		}

		public int Progress
		{
			get { return _progress; }
			set { Set(() => Progress, ref _progress, value); }
		}

		public int Value
		{
			get { return 10; }
		}
	}
}
