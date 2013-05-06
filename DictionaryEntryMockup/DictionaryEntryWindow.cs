using System.Windows.Forms;
using Tokay;

namespace DictionaryEntryMockup
{
	public partial class DictionaryEntryWindow : Form
	{
		private TokayControl _tokay;
		private DictionaryModel _model;
		public DictionaryEntryWindow()
		{
			InitializeComponent();
			_tokay = new TokayControl(FileLocator.DirectoryOfApplicationOrSolution, GetObject, FileLocator.GetFileDistributedWithApplication("DictionaryView.html"));
			Controls.Add(_tokay);
			_tokay.Dock = DockStyle.Fill;
			_model = new DictionaryModel();
			_tokay.CloseDialogRequested += _tokay_CloseDialogRequested;
		}

		private void _tokay_CloseDialogRequested(object sender, CloseDialogRequestedEventArgs e)
		{
			DialogResult = e.DialogResult;
			Close();
		}


		private object GetObject(string name)
		{
			switch (name)
			{
				case "DictionaryModel":
					return _model;
			}

			return null;
		}
	}
}
