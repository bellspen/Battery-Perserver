namespace BatteryPerserve
{
    partial class WifiProfiles
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel_wifi_profiles = new System.Windows.Forms.Panel();
            this.listBox_wifi_profiles = new System.Windows.Forms.ListBox();
            this.label_wifi_profiles = new System.Windows.Forms.Label();
            this.panel_wifi_profiles.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel_wifi_profiles
            // 
            this.panel_wifi_profiles.Controls.Add(this.label_wifi_profiles);
            this.panel_wifi_profiles.Controls.Add(this.listBox_wifi_profiles);
            this.panel_wifi_profiles.Location = new System.Drawing.Point(12, 12);
            this.panel_wifi_profiles.Name = "panel_wifi_profiles";
            this.panel_wifi_profiles.Size = new System.Drawing.Size(519, 214);
            this.panel_wifi_profiles.TabIndex = 0;
            // 
            // listBox_wifi_profiles
            // 
            this.listBox_wifi_profiles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listBox_wifi_profiles.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox_wifi_profiles.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.listBox_wifi_profiles.FormattingEnabled = true;
            this.listBox_wifi_profiles.ItemHeight = 24;
            this.listBox_wifi_profiles.Items.AddRange(new object[] {
            "Cant find any wifi profiles"});
            this.listBox_wifi_profiles.Location = new System.Drawing.Point(3, 32);
            this.listBox_wifi_profiles.Name = "listBox_wifi_profiles";
            this.listBox_wifi_profiles.Size = new System.Drawing.Size(284, 172);
            this.listBox_wifi_profiles.TabIndex = 0;
            this.listBox_wifi_profiles.DoubleClick += new System.EventHandler(this.listBox_wifi_profiles_DoubleClick);
            // 
            // label_wifi_profiles
            // 
            this.label_wifi_profiles.AutoSize = true;
            this.label_wifi_profiles.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_wifi_profiles.ForeColor = System.Drawing.SystemColors.Highlight;
            this.label_wifi_profiles.Location = new System.Drawing.Point(3, 0);
            this.label_wifi_profiles.Name = "label_wifi_profiles";
            this.label_wifi_profiles.Size = new System.Drawing.Size(512, 25);
            this.label_wifi_profiles.TabIndex = 1;
            this.label_wifi_profiles.Text = "Select a WIFI profile to send to the Battery Optimizer";
            // 
            // WifiProfiles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(540, 289);
            this.Controls.Add(this.panel_wifi_profiles);
            this.Name = "WifiProfiles";
            this.Text = "Select a WIFI Profile";
            this.panel_wifi_profiles.ResumeLayout(false);
            this.panel_wifi_profiles.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel_wifi_profiles;
        private System.Windows.Forms.Label label_wifi_profiles;
        private System.Windows.Forms.ListBox listBox_wifi_profiles;
    }
}