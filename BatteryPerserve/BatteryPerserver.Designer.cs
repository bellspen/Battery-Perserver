namespace BatteryPerserve
{
    partial class BatteryPerserver
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
            //Stop Power Watching
            Watch_Pwr = false;
            Pwr_Watching.Join();
            if (SP0.IsOpen)
                ClosePort();


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
            this.Com_Selection = new System.Windows.Forms.ListBox();
            this.Com_Con_Dis = new System.Windows.Forms.Button();
            this.Battery_Info = new System.Windows.Forms.Label();
            this.Battery_Percentage = new System.Windows.Forms.TextBox();
            this.Battery_LineStatus = new System.Windows.Forms.TextBox();
            this.Panel_Battery_Info = new System.Windows.Forms.Panel();
            this.Battery_Normal_Charge_Time = new System.Windows.Forms.DateTimePicker();
            this.Panel_Battery_Info.SuspendLayout();
            this.SuspendLayout();
            // 
            // Com_Selection
            // 
            this.Com_Selection.FormattingEnabled = true;
            this.Com_Selection.Location = new System.Drawing.Point(13, 75);
            this.Com_Selection.Name = "Com_Selection";
            this.Com_Selection.Size = new System.Drawing.Size(120, 95);
            this.Com_Selection.TabIndex = 1;
            // 
            // Com_Con_Dis
            // 
            this.Com_Con_Dis.Location = new System.Drawing.Point(139, 75);
            this.Com_Con_Dis.Name = "Com_Con_Dis";
            this.Com_Con_Dis.Size = new System.Drawing.Size(75, 23);
            this.Com_Con_Dis.TabIndex = 2;
            this.Com_Con_Dis.Text = "Connect";
            this.Com_Con_Dis.UseVisualStyleBackColor = true;
            this.Com_Con_Dis.Click += new System.EventHandler(this.Com_Con_Dis_Click);
            // 
            // Battery_Info
            // 
            this.Battery_Info.AutoSize = true;
            this.Battery_Info.Location = new System.Drawing.Point(3, 11);
            this.Battery_Info.Name = "Battery_Info";
            this.Battery_Info.Size = new System.Drawing.Size(95, 13);
            this.Battery_Info.TabIndex = 4;
            this.Battery_Info.Text = "Battery Information";
            // 
            // Battery_Percentage
            // 
            this.Battery_Percentage.Location = new System.Drawing.Point(6, 27);
            this.Battery_Percentage.Name = "Battery_Percentage";
            this.Battery_Percentage.ReadOnly = true;
            this.Battery_Percentage.Size = new System.Drawing.Size(32, 20);
            this.Battery_Percentage.TabIndex = 5;
            this.Battery_Percentage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Battery_LineStatus
            // 
            this.Battery_LineStatus.Location = new System.Drawing.Point(44, 27);
            this.Battery_LineStatus.Name = "Battery_LineStatus";
            this.Battery_LineStatus.ReadOnly = true;
            this.Battery_LineStatus.Size = new System.Drawing.Size(100, 20);
            this.Battery_LineStatus.TabIndex = 6;
            // 
            // Panel_Battery_Info
            // 
            this.Panel_Battery_Info.Controls.Add(this.Battery_Normal_Charge_Time);
            this.Panel_Battery_Info.Controls.Add(this.Battery_Info);
            this.Panel_Battery_Info.Controls.Add(this.Battery_LineStatus);
            this.Panel_Battery_Info.Controls.Add(this.Battery_Percentage);
            this.Panel_Battery_Info.Location = new System.Drawing.Point(244, 75);
            this.Panel_Battery_Info.Name = "Panel_Battery_Info";
            this.Panel_Battery_Info.Size = new System.Drawing.Size(228, 154);
            this.Panel_Battery_Info.TabIndex = 7;
            // 
            // Battery_Normal_Charge_Time
            // 
            this.Battery_Normal_Charge_Time.CustomFormat = "h:m:tt";
            this.Battery_Normal_Charge_Time.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.Battery_Normal_Charge_Time.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Battery_Normal_Charge_Time.Location = new System.Drawing.Point(6, 69);
            this.Battery_Normal_Charge_Time.Name = "Battery_Normal_Charge_Time";
            this.Battery_Normal_Charge_Time.ShowUpDown = true;
            this.Battery_Normal_Charge_Time.Size = new System.Drawing.Size(100, 20);
            this.Battery_Normal_Charge_Time.TabIndex = 7;
            this.Battery_Normal_Charge_Time.Value = new System.DateTime(2019, 5, 14, 17, 45, 0, 0);
            // 
            // BatteryPerserver
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.Panel_Battery_Info);
            this.Controls.Add(this.Com_Con_Dis);
            this.Controls.Add(this.Com_Selection);
            this.Name = "BatteryPerserver";
            this.Text = "Battery Saver";
            this.Panel_Battery_Info.ResumeLayout(false);
            this.Panel_Battery_Info.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListBox Com_Selection;
        private System.Windows.Forms.Button Com_Con_Dis;
        private System.Windows.Forms.Label Battery_Info;
        private System.Windows.Forms.TextBox Battery_Percentage;
        private System.Windows.Forms.TextBox Battery_LineStatus;
        private System.Windows.Forms.Panel Panel_Battery_Info;
        private System.Windows.Forms.DateTimePicker Battery_Normal_Charge_Time;
    }
}

