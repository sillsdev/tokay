using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TokaySharp;

namespace ChorusDialogMockup
{
	public partial class ChorusSendReceiveSettingsDialog : Form
	{
		private readonly KOControl _ko;

		public ChorusSendReceiveSettingsModel Model { get; set; }

		public ChorusSendReceiveSettingsDialog()
        {
            InitializeComponent();

			_ko = new KOControl(GetObject, FileLocator.GetFileDistributedWithApplication("ChorusSendReceiveSettings.html"));
			Controls.Add(_ko);
			_ko.Dock = DockStyle.Fill;
			Model = new ChorusSendReceiveSettingsModel();
		}


		private object GetObject(string name)
		{
			switch (name)
			{
				case "ChorusSendReceiveSettingsModel":
					return Model;
			}

			return null;
		}
	}
}
