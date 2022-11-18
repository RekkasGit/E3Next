
namespace E3NextUI
{
    partial class MessageBox
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
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.lblMessage = new System.Windows.Forms.Label();
            this.buttonOkayOnly = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOK.Location = new System.Drawing.Point(96, 66);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(77, 27);
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "Yes";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.Location = new System.Drawing.Point(197, 66);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(77, 27);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "No";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // lblMessage
            // 
            this.lblMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMessage.ForeColor = System.Drawing.Color.CornflowerBlue;
            this.lblMessage.Location = new System.Drawing.Point(12, 19);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(358, 27);
            this.lblMessage.TabIndex = 2;
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonOkayOnly
            // 
            this.buttonOkayOnly.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOkayOnly.Location = new System.Drawing.Point(154, 66);
            this.buttonOkayOnly.Name = "buttonOkayOnly";
            this.buttonOkayOnly.Size = new System.Drawing.Size(77, 27);
            this.buttonOkayOnly.TabIndex = 3;
            this.buttonOkayOnly.Text = "OK";
            this.buttonOkayOnly.UseVisualStyleBackColor = true;
            this.buttonOkayOnly.Visible = false;
            this.buttonOkayOnly.Click += new System.EventHandler(this.buttonOkayOnly_Click);
            // 
            // MessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 119);
            this.ControlBox = false;
            this.Controls.Add(this.buttonOkayOnly);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Name = "MessageBox";
            this.Text = "Yes/No?";
            this.ResumeLayout(false);

        }

        #endregion
        public System.Windows.Forms.Label lblMessage;
        public System.Windows.Forms.Button buttonOK;
        public System.Windows.Forms.Button buttonCancel;
        public System.Windows.Forms.Button buttonOkayOnly;
    }
}