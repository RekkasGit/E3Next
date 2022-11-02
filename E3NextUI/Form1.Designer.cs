
namespace E3NextUI
{
    partial class E3UI
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
            this.labelPlayer = new System.Windows.Forms.Label();
            this.labelPlayerName = new System.Windows.Forms.Label();
            this.labelHP = new System.Windows.Forms.Label();
            this.labelHPTotal = new System.Windows.Forms.Label();
            this.richTextBoxConsole = new System.Windows.Forms.RichTextBox();
            this.richTextBoxMQConsole = new System.Windows.Forms.RichTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelPlayer
            // 
            this.labelPlayer.AutoSize = true;
            this.labelPlayer.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlayer.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelPlayer.Location = new System.Drawing.Point(13, 13);
            this.labelPlayer.Name = "labelPlayer";
            this.labelPlayer.Size = new System.Drawing.Size(74, 24);
            this.labelPlayer.TabIndex = 0;
            this.labelPlayer.Text = "Player:";
            // 
            // labelPlayerName
            // 
            this.labelPlayerName.AutoSize = true;
            this.labelPlayerName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlayerName.ForeColor = System.Drawing.Color.Crimson;
            this.labelPlayerName.Location = new System.Drawing.Point(93, 16);
            this.labelPlayerName.Name = "labelPlayerName";
            this.labelPlayerName.Size = new System.Drawing.Size(93, 20);
            this.labelPlayerName.TabIndex = 1;
            this.labelPlayerName.Text = "YourName";
            // 
            // labelHP
            // 
            this.labelHP.AutoSize = true;
            this.labelHP.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHP.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelHP.Location = new System.Drawing.Point(43, 49);
            this.labelHP.Name = "labelHP";
            this.labelHP.Size = new System.Drawing.Size(44, 24);
            this.labelHP.TabIndex = 2;
            this.labelHP.Text = "HP:";
            // 
            // labelHPTotal
            // 
            this.labelHPTotal.AutoSize = true;
            this.labelHPTotal.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHPTotal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.labelHPTotal.Location = new System.Drawing.Point(93, 48);
            this.labelHPTotal.Name = "labelHPTotal";
            this.labelHPTotal.Size = new System.Drawing.Size(25, 25);
            this.labelHPTotal.TabIndex = 3;
            this.labelHPTotal.Text = "0";
            // 
            // richTextBoxConsole
            // 
            this.richTextBoxConsole.BackColor = System.Drawing.Color.Black;
            this.richTextBoxConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxConsole.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxConsole.ForeColor = System.Drawing.Color.Lime;
            this.richTextBoxConsole.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxConsole.Name = "richTextBoxConsole";
            this.richTextBoxConsole.ReadOnly = true;
            this.richTextBoxConsole.Size = new System.Drawing.Size(763, 236);
            this.richTextBoxConsole.TabIndex = 4;
            this.richTextBoxConsole.Text = "";
            // 
            // richTextBoxMQConsole
            // 
            this.richTextBoxMQConsole.BackColor = System.Drawing.Color.Black;
            this.richTextBoxMQConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxMQConsole.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxMQConsole.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.richTextBoxMQConsole.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxMQConsole.Name = "richTextBoxMQConsole";
            this.richTextBoxMQConsole.Size = new System.Drawing.Size(383, 236);
            this.richTextBoxMQConsole.TabIndex = 5;
            this.richTextBoxMQConsole.Text = "";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitContainer1.Location = new System.Drawing.Point(0, 296);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.richTextBoxMQConsole);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxConsole);
            this.splitContainer1.Size = new System.Drawing.Size(1150, 236);
            this.splitContainer1.SplitterDistance = 383;
            this.splitContainer1.TabIndex = 6;
            // 
            // E3UI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1150, 532);
            this.ControlBox = false;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.labelHPTotal);
            this.Controls.Add(this.labelHP);
            this.Controls.Add(this.labelPlayerName);
            this.Controls.Add(this.labelPlayer);
            this.Name = "E3UI";
            this.Text = "E3UI";
            this.Load += new System.EventHandler(this.E3UI_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelPlayer;
        private System.Windows.Forms.Label labelPlayerName;
        private System.Windows.Forms.Label labelHP;
        private System.Windows.Forms.Label labelHPTotal;
        private System.Windows.Forms.RichTextBox richTextBoxConsole;
        private System.Windows.Forms.RichTextBox richTextBoxMQConsole;
        private System.Windows.Forms.SplitContainer splitContainer1;
    }
}

