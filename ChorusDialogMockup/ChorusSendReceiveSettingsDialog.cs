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
	public partial class ChorusSendReceiveSettingsDialog : Form
	{
		private readonly KOControl _ko;
		private ChorusSendReceiveSettingsModel _model;

		public ChorusSendReceiveSettingsDialog()
        {
            InitializeComponent();

			_ko = new KOControl(GetObject, FileLocator.GetFileDistributedWithApplication("html/SendReceiveSettings.html"));
			Controls.Add(_ko);
			_ko.Dock = DockStyle.Fill;
			_model = new ChorusSendReceiveSettingsModel();
		}


		private object GetObject(string name)
		{
			switch (name)
			{
				case "ChorusSendReceiveSettingsModel":
					return _model;
			}

			return null;
		}
	}
}
