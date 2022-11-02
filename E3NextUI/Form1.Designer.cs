
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
            this.richTextBoxConsole.ForeColor = System.Drawing.Color.Lime;
            this.richTextBoxConsole.Location = new System.Drawing.Point(17, 131);
            this.richTextBoxConsole.Name = "richTextBoxConsole";
            this.richTextBoxConsole.ReadOnly = true;
            this.richTextBoxConsole.Size = new System.Drawing.Size(771, 307);
            this.richTextBoxConsole.TabIndex = 4;
            this.richTextBoxConsole.Text = "";
            // 
            // E3UI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.ControlBox = false;
            this.Controls.Add(this.richTextBoxConsole);
            this.Controls.Add(this.labelHPTotal);
            this.Controls.Add(this.labelHP);
            this.Controls.Add(this.labelPlayerName);
            this.Controls.Add(this.labelPlayer);
            this.Name = "E3UI";
            this.Text = "E3UI";
            this.Load += new System.EventHandler(this.E3UI_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelPlayer;
        private System.Windows.Forms.Label labelPlayerName;
        private System.Windows.Forms.Label labelHP;
        private System.Windows.Forms.Label labelHPTotal;
        private System.Windows.Forms.RichTextBox richTextBoxConsole;
    }
}

