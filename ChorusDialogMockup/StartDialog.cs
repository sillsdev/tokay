using System.Windows.Forms;
using Knockout.Net;

namespace ChorusDialogMockup
{
    public partial class StartDialog : Form
    {
		private readonly KOControl _ko;
	    private ChorusStartDialogViewModel _chorusStartDialogViewModel;

	    public StartDialog()
        {
            InitializeComponent();

			_ko = new KOControl(GetObject, FileLocator.GetFileDistributedWithApplication("html/ChorusStartupView.html"));
			Controls.Add(_ko);
			_ko.Dock = DockStyle.Fill;
			_chorusStartDialogViewModel = new ChorusStartDialogViewModel();
		}


		private object GetObject(string name)
		{
			switch (name)
			{
				case "ChorusStartDialogViewModel":
					return _chorusStartDialogViewModel;
			}

			return null;
		}
    }
}
