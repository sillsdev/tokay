using Knockout.Net;

namespace ChorusDialogMockup.SyncDialog
{
	public enum SyncState
	{
		Start,
		Syncing
	}

	public class SyncViewModel : ObservableObject
	{
		private SyncState _state;
		private readonly SyncStartViewModel _start;
		private readonly SyncStatusViewModel _status;

		public SyncViewModel()
		{
			_start = new SyncStartViewModel(this);
			_status = new SyncStatusViewModel();
		}

		public SyncState State
		{
			get { return _state; }
			set { Set(() => State, ref _state, value); }
		}

		public void StartSync()
		{
			_status.StartSync();
			State = SyncState.Syncing;
		}

		public SyncStartViewModel Start
		{
			get { return _start; }
		}

		public SyncStatusViewModel Status
		{
			get { return _status; }
		}

		public int Value
		{
			get { return 50; }
		}
	}
}
