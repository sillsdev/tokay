using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gecko;

namespace TestApp
{
    public partial class ShowGoogleForm : Form
    {
        private GeckoWebBrowser _browser;

        public ShowGoogleForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _browser = new Gecko.GeckoWebBrowser();
            _browser.Dock= DockStyle.Fill;
            Controls.Add(_browser);
            Debug.WriteLine(_browser.ProductVersion);
            _browser.Navigate("http://google.com");
          
        }
    }
}
