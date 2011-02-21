/**
Copyright 2011, Cong Nguyen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Tesseract.OCR.AppEntry
{
    public partial class DlgOptions : Form
    {
        public DlgOptions()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (txtDataPath.Text == "" || !Directory.Exists(txtDataPath.Text))
                {
                    MessageBox.Show("Data path is invalid!", "Error");

                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }

        public string DataPath
        {
            get { return txtDataPath.Text; }
            set { txtDataPath.Text = value; }
        }

        public string Language
        {
            get { return cmbLanguage.Text; }
        }

        public eOcrEngineMode OcrEngineMode
        {
            get { return eOcrEngineMode.DEFAULT; }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {                    
                    txtDataPath.Text = dlg.SelectedPath.Trim(new char[] {'\\'}) + @"\";
                }
            }
        }
    }
}
