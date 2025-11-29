using Ionic.Zip;
using Octokit;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace E3NextUI
{
    public partial class Update : Form
    {
        public GitHubClient client;
        public int latestID;
        public Update()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(this.textBoxInstallPath.Text))
            {
                 lblStatus.Text = "Please enter a name or cancel";
                return;
            }

            string exePath = textBoxInstallPath.Text;
            var assets = client.Repository.Release.GetAllAssets("RekkasGit", "E3Next", latestID).Result;

			foreach (var asset in assets)
			{
				if (asset.Name == "e3.zip")
				{
					var zipFile = assets[0];
					lblStatus.Text = "Status: Downloading ....";
					var resp = client.Connection.Get<byte[]>(new Uri(zipFile.BrowserDownloadUrl), new Dictionary<string, string>(), null).Result;
					var data = resp.Body;

					lblStatus.Text = "Status: Extracting ....";
					using (System.IO.Stream stream = new System.IO.MemoryStream(data))
					{
						using (ZipFile zip = ZipFile.Read(stream))
						{
							zip.ExtractAll(exePath, ExtractExistingFileAction.OverwriteSilently);

						}
					}
				}
			}
			var mb = new MessageBox();
            mb.Owner = this;
            mb.StartPosition = FormStartPosition.CenterParent;
         
            mb.Text = "Upgrade E3";
            mb.lblMessage.Text = "Done! Please restart E3/Everquest.";
            mb.buttonOkayOnly.Visible = true;
            mb.buttonOK.Visible = false;
            mb.buttonCancel.Visible = false;
            mb.ShowDialog();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
