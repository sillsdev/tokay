using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
