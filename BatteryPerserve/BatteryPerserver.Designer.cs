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
            this.button_DefaultBatteryRange = new System.Windows.Forms.Button();
            this.BatteryMax = new System.Windows.Forms.NumericUpDown();
            this.BatteryMin = new System.Windows.Forms.NumericUpDown();
            this.label_BatteryMax = new System.Windows.Forms.Label();
            this.label_BatteryMin = new System.Windows.Forms.Label();
            this.label_BatteryRange = new System.Windows.Forms.Label();
            this.checkBox_OptimizeChargeTime = new System.Windows.Forms.CheckBox();
            this.Battery_OptimizeChargeTime = new System.Windows.Forms.DateTimePicker();
            this.checkBox_NormalChargeTime = new System.Windows.Forms.CheckBox();
            this.Battery_NormalChargeTime = new System.Windows.Forms.DateTimePicker();
            this.Panel_Connection = new System.Windows.Forms.Panel();
            this.label_ComSelection = new System.Windows.Forms.Label();
            this.button_OptmizeBattery = new System.Windows.Forms.Button();
            this.Panel_Battery_Info.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryMax)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryMin)).BeginInit();
            this.Panel_Connection.SuspendLayout();
            this.SuspendLayout();
            // 
            // Com_Selection
            // 
            this.Com_Selection.FormattingEnabled = true;
            this.Com_Selection.Location = new System.Drawing.Point(3, 28);
            this.Com_Selection.Name = "Com_Selection";
            this.Com_Selection.Size = new System.Drawing.Size(120, 95);
            this.Com_Selection.TabIndex = 1;
            // 
            // Com_Con_Dis
            // 
            this.Com_Con_Dis.Location = new System.Drawing.Point(129, 30);
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
            this.Panel_Battery_Info.Controls.Add(this.button_OptmizeBattery);
            this.Panel_Battery_Info.Controls.Add(this.button_DefaultBatteryRange);
            this.Panel_Battery_Info.Controls.Add(this.BatteryMax);
            this.Panel_Battery_Info.Controls.Add(this.BatteryMin);
            this.Panel_Battery_Info.Controls.Add(this.label_BatteryMax);
            this.Panel_Battery_Info.Controls.Add(this.label_BatteryMin);
            this.Panel_Battery_Info.Controls.Add(this.label_BatteryRange);
            this.Panel_Battery_Info.Controls.Add(this.checkBox_OptimizeChargeTime);
            this.Panel_Battery_Info.Controls.Add(this.Battery_OptimizeChargeTime);
            this.Panel_Battery_Info.Controls.Add(this.checkBox_NormalChargeTime);
            this.Panel_Battery_Info.Controls.Add(this.Battery_NormalChargeTime);
            this.Panel_Battery_Info.Controls.Add(this.Battery_Info);
            this.Panel_Battery_Info.Controls.Add(this.Battery_LineStatus);
            this.Panel_Battery_Info.Controls.Add(this.Battery_Percentage);
            this.Panel_Battery_Info.Location = new System.Drawing.Point(241, 12);
            this.Panel_Battery_Info.Name = "Panel_Battery_Info";
            this.Panel_Battery_Info.Size = new System.Drawing.Size(378, 208);
            this.Panel_Battery_Info.TabIndex = 7;
            // 
            // button_DefaultBatteryRange
            // 
            this.button_DefaultBatteryRange.Location = new System.Drawing.Point(314, 28);
            this.button_DefaultBatteryRange.Name = "button_DefaultBatteryRange";
            this.button_DefaultBatteryRange.Size = new System.Drawing.Size(58, 19);
            this.button_DefaultBatteryRange.TabIndex = 18;
            this.button_DefaultBatteryRange.Text = "Defaults";
            this.button_DefaultBatteryRange.UseVisualStyleBackColor = true;
            this.button_DefaultBatteryRange.Click += new System.EventHandler(this.button_DefaultBatteryRange_Click);
            // 
            // BatteryMax
            // 
            this.BatteryMax.Location = new System.Drawing.Point(269, 28);
            this.BatteryMax.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.BatteryMax.Name = "BatteryMax";
            this.BatteryMax.Size = new System.Drawing.Size(39, 20);
            this.BatteryMax.TabIndex = 17;
            this.BatteryMax.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // BatteryMin
            // 
            this.BatteryMin.Location = new System.Drawing.Point(183, 27);
            this.BatteryMin.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.BatteryMin.Name = "BatteryMin";
            this.BatteryMin.Size = new System.Drawing.Size(44, 20);
            this.BatteryMin.TabIndex = 16;
            this.BatteryMin.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // label_BatteryMax
            // 
            this.label_BatteryMax.AutoSize = true;
            this.label_BatteryMax.Location = new System.Drawing.Point(233, 30);
            this.label_BatteryMax.Name = "label_BatteryMax";
            this.label_BatteryMax.Size = new System.Drawing.Size(30, 13);
            this.label_BatteryMax.TabIndex = 15;
            this.label_BatteryMax.Text = "Max:";
            // 
            // label_BatteryMin
            // 
            this.label_BatteryMin.AutoSize = true;
            this.label_BatteryMin.Location = new System.Drawing.Point(150, 30);
            this.label_BatteryMin.Name = "label_BatteryMin";
            this.label_BatteryMin.Size = new System.Drawing.Size(27, 13);
            this.label_BatteryMin.TabIndex = 14;
            this.label_BatteryMin.Text = "Min:";
            // 
            // label_BatteryRange
            // 
            this.label_BatteryRange.AutoSize = true;
            this.label_BatteryRange.Location = new System.Drawing.Point(150, 10);
            this.label_BatteryRange.Name = "label_BatteryRange";
            this.label_BatteryRange.Size = new System.Drawing.Size(113, 13);
            this.label_BatteryRange.TabIndex = 13;
            this.label_BatteryRange.Text = "Optimal Battery Range";
            // 
            // checkBox_OptimizeChargeTime
            // 
            this.checkBox_OptimizeChargeTime.AutoSize = true;
            this.checkBox_OptimizeChargeTime.Location = new System.Drawing.Point(6, 83);
            this.checkBox_OptimizeChargeTime.Name = "checkBox_OptimizeChargeTime";
            this.checkBox_OptimizeChargeTime.Size = new System.Drawing.Size(118, 17);
            this.checkBox_OptimizeChargeTime.TabIndex = 10;
            this.checkBox_OptimizeChargeTime.Text = "Optimize Charge at:";
            this.checkBox_OptimizeChargeTime.UseVisualStyleBackColor = true;
            this.checkBox_OptimizeChargeTime.CheckedChanged += new System.EventHandler(this.checkBox_OptimizeChargeTime_CheckedChanged);
            // 
            // Battery_OptimizeChargeTime
            // 
            this.Battery_OptimizeChargeTime.CustomFormat = "h:mm:tt";
            this.Battery_OptimizeChargeTime.Enabled = false;
            this.Battery_OptimizeChargeTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.Battery_OptimizeChargeTime.Location = new System.Drawing.Point(6, 103);
            this.Battery_OptimizeChargeTime.Name = "Battery_OptimizeChargeTime";
            this.Battery_OptimizeChargeTime.ShowUpDown = true;
            this.Battery_OptimizeChargeTime.Size = new System.Drawing.Size(72, 20);
            this.Battery_OptimizeChargeTime.TabIndex = 9;
            this.Battery_OptimizeChargeTime.Value = new System.DateTime(2019, 5, 15, 9, 0, 0, 0);
            // 
            // checkBox_NormalChargeTime
            // 
            this.checkBox_NormalChargeTime.AutoSize = true;
            this.checkBox_NormalChargeTime.Location = new System.Drawing.Point(130, 83);
            this.checkBox_NormalChargeTime.Name = "checkBox_NormalChargeTime";
            this.checkBox_NormalChargeTime.Size = new System.Drawing.Size(111, 17);
            this.checkBox_NormalChargeTime.TabIndex = 8;
            this.checkBox_NormalChargeTime.Text = "Normal Charge at:";
            this.checkBox_NormalChargeTime.UseVisualStyleBackColor = true;
            this.checkBox_NormalChargeTime.CheckedChanged += new System.EventHandler(this.checkBox1_NormalChargeTime_CheckedChanged);
            // 
            // Battery_NormalChargeTime
            // 
            this.Battery_NormalChargeTime.CustomFormat = "h:mm:tt";
            this.Battery_NormalChargeTime.Enabled = false;
            this.Battery_NormalChargeTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.Battery_NormalChargeTime.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Battery_NormalChargeTime.Location = new System.Drawing.Point(130, 103);
            this.Battery_NormalChargeTime.Name = "Battery_NormalChargeTime";
            this.Battery_NormalChargeTime.ShowUpDown = true;
            this.Battery_NormalChargeTime.Size = new System.Drawing.Size(73, 20);
            this.Battery_NormalChargeTime.TabIndex = 7;
            this.Battery_NormalChargeTime.Value = new System.DateTime(2019, 5, 14, 22, 0, 0, 0);
            // 
            // Panel_Connection
            // 
            this.Panel_Connection.Controls.Add(this.label_ComSelection);
            this.Panel_Connection.Controls.Add(this.Com_Selection);
            this.Panel_Connection.Controls.Add(this.Com_Con_Dis);
            this.Panel_Connection.Location = new System.Drawing.Point(12, 12);
            this.Panel_Connection.Name = "Panel_Connection";
            this.Panel_Connection.Size = new System.Drawing.Size(214, 180);
            this.Panel_Connection.TabIndex = 8;
            // 
            // label_ComSelection
            // 
            this.label_ComSelection.AutoSize = true;
            this.label_ComSelection.Location = new System.Drawing.Point(3, 10);
            this.label_ComSelection.Name = "label_ComSelection";
            this.label_ComSelection.Size = new System.Drawing.Size(148, 13);
            this.label_ComSelection.TabIndex = 3;
            this.label_ComSelection.Text = "Select the corresponding com";
            // 
            // button_OptmizeBattery
            // 
            this.button_OptmizeBattery.BackColor = System.Drawing.Color.Red;
            this.button_OptmizeBattery.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.button_OptmizeBattery.Location = new System.Drawing.Point(4, 54);
            this.button_OptmizeBattery.Name = "button_OptmizeBattery";
            this.button_OptmizeBattery.Size = new System.Drawing.Size(55, 23);
            this.button_OptmizeBattery.TabIndex = 19;
            this.button_OptmizeBattery.Text = "OFF";
            this.button_OptmizeBattery.UseVisualStyleBackColor = false;
            this.button_OptmizeBattery.Click += new System.EventHandler(this.button_OptmizeBattery_Click);
            // 
            // BatteryPerserver
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.Panel_Connection);
            this.Controls.Add(this.Panel_Battery_Info);
            this.Name = "BatteryPerserver";
            this.Text = "Battery Saver";
            this.Panel_Battery_Info.ResumeLayout(false);
            this.Panel_Battery_Info.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryMax)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BatteryMin)).EndInit();
            this.Panel_Connection.ResumeLayout(false);
            this.Panel_Connection.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListBox Com_Selection;
        private System.Windows.Forms.Button Com_Con_Dis;
        private System.Windows.Forms.Label Battery_Info;
        private System.Windows.Forms.TextBox Battery_Percentage;
        private System.Windows.Forms.TextBox Battery_LineStatus;
        private System.Windows.Forms.Panel Panel_Battery_Info;
        private System.Windows.Forms.DateTimePicker Battery_NormalChargeTime;
        private System.Windows.Forms.CheckBox checkBox_NormalChargeTime;
        private System.Windows.Forms.DateTimePicker Battery_OptimizeChargeTime;
        private System.Windows.Forms.CheckBox checkBox_OptimizeChargeTime;
        private System.Windows.Forms.Button button_DefaultBatteryRange;
        private System.Windows.Forms.NumericUpDown BatteryMax;
        private System.Windows.Forms.NumericUpDown BatteryMin;
        private System.Windows.Forms.Label label_BatteryMax;
        private System.Windows.Forms.Label label_BatteryMin;
        private System.Windows.Forms.Label label_BatteryRange;
        private System.Windows.Forms.Panel Panel_Connection;
        private System.Windows.Forms.Label label_ComSelection;
        private System.Windows.Forms.Button button_OptmizeBattery;
    }
}

