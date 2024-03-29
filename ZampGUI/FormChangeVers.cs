﻿using ZampLib;
using ZampLib.Business;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ZampGUI
{
    public partial class FormChangeVers : Form
    {
        public ConfigVar cv;
        int k = 0;
        string _env;

        public FormChangeVers(ConfigVar cv)
        {
            InitializeComponent();
            this.cv = cv;


            this._env = ZampGUILib.getval_from_appsetting("env");

            int index_sel_mariadb = 0;
            k = 0;
            foreach(var c in this.cv.MariaDB_list)
            {
                comboBoxDB_Vers.Items.Add(c);
                if (c == cv.MariaDB_current)
                    index_sel_mariadb = k;
                k++;
            }

            int index_sel_php = 0;
            k = 0;
            foreach (var c in this.cv.PHP_list)
            {
                comboBoxPHP_Vers.Items.Add(c);
                if (c == cv.PHP_current)
                    index_sel_php = k;
                k++;
            }

            comboBoxDB_Vers.SelectedIndex = index_sel_mariadb; //(comboBoxDB_Vers.Items.Count > 0) ? 0 : -1;
            comboBoxPHP_Vers.SelectedIndex = index_sel_php;// (comboBoxPHP_Vers.Items.Count > 0) ? 0 : -1;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            JObject jobj = ZampGUILib.getJson_Env();

            //jobj[_env]["MariaDB_scelta"] = comboBoxDB_Vers.SelectedItem.ToString();
            jobj[_env]["PHP_scelta"] = comboBoxPHP_Vers.SelectedItem.ToString();

            aggiorna_apache_httpd_conf_php(comboBoxPHP_Vers.SelectedItem.ToString(), comboBoxDB_Vers.SelectedItem.ToString());

            ZampGUILib.setJson_Env(jobj);

            //cv = new ConfigVar();
            this.Close();
        }

        private void aggiorna_apache_httpd_conf_php(string PHP_scelta, string DB_scelta)
        {
            //aggiorno il file zampgui.conf
            //string text = File.ReadAllText(cv.Apache_zampgui_conf);

            //text = System.Text.RegularExpressions.Regex.Replace(text, "^PHPIniDir .*$", "PHPIniDir \"${ZAMPROOT}/Apps/" + PHP_scelta + "\"", System.Text.RegularExpressions.RegexOptions.Multiline);
            ////text = System.Text.RegularExpressions.Regex.Replace(text, "^SetEnv MYSQL_HOME .*$", "SetEnv MYSQL_HOME \"${ZAMPROOT}\\\\Apps\\\\" + DB_scelta + "\\\\bin\"", System.Text.RegularExpressions.RegexOptions.Multiline);
            //if (PHP_scelta.Contains("php-8"))
            //{
            //    text = text.Replace("LoadFile \"${PHPROOT}/php7ts.dll\"", "LoadFile \"${PHPROOT}/php8ts.dll\"");
            //    text = text.Replace("LoadModule php7_module \"${PHPROOT}/php7apache2_4.dll\"", "LoadModule php_module \"${PHPROOT}/php8apache2_4.dll\"");
            //}
            //else if (PHP_scelta.Contains("php-7"))
            //{
            //    text = text.Replace("LoadFile \"${PHPROOT}/php8ts.dll\"", "LoadFile \"${PHPROOT}/php7ts.dll\"");
            //    text = text.Replace("LoadModule php_module \"${PHPROOT}/php8apache2_4.dll\"", "LoadModule php7_module \"${PHPROOT}/php7apache2_4.dll\"");
            //}
            //File.WriteAllText(cv.Apache_zampgui_conf, text);






            //aggiorno il file httpd.conf
            string num = PHP_scelta.Substring(4, 3).Replace(".", "");
            string text = File.ReadAllText(cv.Apache_httpd_conf);
            text = System.Text.RegularExpressions.Regex.Replace(text, "^Define PHPROOT.*$", "Define PHPROOT \"${ZAMPROOT}/Apps/" + PHP_scelta + "\"", System.Text.RegularExpressions.RegexOptions.Multiline);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^Include conf/extra/zampgui_\d\d.conf", "#$&", System.Text.RegularExpressions.RegexOptions.Multiline);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^#Include conf/extra/zampgui_" + num + ".conf", "Include conf/extra/zampgui_" + num + ".conf", System.Text.RegularExpressions.RegexOptions.Multiline);

            File.WriteAllText(cv.Apache_httpd_conf, text);
        }
    }
}
