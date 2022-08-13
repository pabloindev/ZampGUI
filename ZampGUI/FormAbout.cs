﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZampLib.Business;

namespace ZampGUI
{
    public partial class FormAbout : Form
    {
        public ConfigVar cv;
        public string HOME { get; set; }
        public string paypal_donate_url { get; set; }
        public string sourceforge_zampgui_url { get; set; }
        public string github_url { get; set; }

        public FormAbout(ConfigVar cv)
        {
            InitializeComponent();
            this.cv = cv;
            this.HOME = ZampLib.ZampGUILib.getval_from_appsetting("HOME");
            JObject jobj = cv.getReqInfo_from_WebSite(HOME);

            paypal_donate_url = jobj.Value<string>("paypal_donate_url");
            sourceforge_zampgui_url = jobj.Value<string>("sourceforge_zampgui_url");
            github_url = jobj.Value<string>("github_url");
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(HOME);
        }

        private void pictureBoxHome_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(HOME);
        }

        private void pictureBoxEmail_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(HOME + "/contactme.php");
        }

        private void pictureBoxGithub_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(github_url);
        }

        private void pictureBoxSourceforge_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(sourceforge_zampgui_url);
        }

        private void pictureBoxDonatePaypal_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(paypal_donate_url);
        }
    }
}
