namespace E3NextConfigEditor
{
	partial class SplashScreen
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.splashLabel = new System.Windows.Forms.Label();
			this.e3nextPictureBox = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.e3nextPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// splashLabel
			// 
			this.splashLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.splashLabel.ForeColor = System.Drawing.Color.LightSkyBlue;
			this.splashLabel.Location = new System.Drawing.Point(196, 62);
			this.splashLabel.Name = "splashLabel";
			this.splashLabel.Size = new System.Drawing.Size(529, 24);
			this.splashLabel.TabIndex = 0;
			this.splashLabel.Text = "Loading Data...";
			this.splashLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// e3nextPictureBox
			// 
			this.e3nextPictureBox.ImageLocation = "";
			this.e3nextPictureBox.Location = new System.Drawing.Point(12, 12);
			this.e3nextPictureBox.Name = "e3nextPictureBox";
			this.e3nextPictureBox.Size = new System.Drawing.Size(178, 141);
			this.e3nextPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.e3nextPictureBox.TabIndex = 1;
			this.e3nextPictureBox.TabStop = false;
			// 
			// SplashScreen
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(750, 165);
			this.Controls.Add(this.e3nextPictureBox);
			this.Controls.Add(this.splashLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "SplashScreen";
			this.Text = "SplashScreen";
			((System.ComponentModel.ISupportInitialize)(this.e3nextPictureBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.Label splashLabel;
		public System.Windows.Forms.PictureBox e3nextPictureBox;
	}
}