namespace MopidyTray
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.state = new System.Windows.Forms.ListView();
            this.colKey = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.labelCommand = new System.Windows.Forms.Label();
            this.buttonCommand = new System.Windows.Forms.Button();
            this.comboCommand = new System.Windows.Forms.ComboBox();
            this.checkShowNotifications = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.Text = "MopidyTray";
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            // 
            // state
            // 
            this.state.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.state.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colKey,
            this.colValue});
            this.state.FullRowSelect = true;
            this.state.Location = new System.Drawing.Point(12, 12);
            this.state.Name = "state";
            this.state.Size = new System.Drawing.Size(697, 416);
            this.state.SmallImageList = this.imageList;
            this.state.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.state.TabIndex = 4;
            this.state.UseCompatibleStateImageBehavior = false;
            this.state.View = System.Windows.Forms.View.Details;
            this.state.DoubleClick += new System.EventHandler(this.state_DoubleClick);
            // 
            // colKey
            // 
            this.colKey.Text = "Key";
            this.colKey.Width = 100;
            // 
            // colValue
            // 
            this.colValue.Text = "Value";
            this.colValue.Width = 200;
            // 
            // imageList
            // 
            this.imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageList.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // labelCommand
            // 
            this.labelCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCommand.AutoSize = true;
            this.labelCommand.Location = new System.Drawing.Point(12, 438);
            this.labelCommand.Name = "labelCommand";
            this.labelCommand.Size = new System.Drawing.Size(68, 15);
            this.labelCommand.TabIndex = 0;
            this.labelCommand.Text = "Command:";
            // 
            // buttonCommand
            // 
            this.buttonCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCommand.Location = new System.Drawing.Point(501, 433);
            this.buttonCommand.Name = "buttonCommand";
            this.buttonCommand.Size = new System.Drawing.Size(77, 23);
            this.buttonCommand.TabIndex = 2;
            this.buttonCommand.Text = "Send";
            this.buttonCommand.UseVisualStyleBackColor = true;
            this.buttonCommand.Click += new System.EventHandler(this.buttonCommand_Click);
            // 
            // comboCommand
            // 
            this.comboCommand.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboCommand.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::MopidyTray.Properties.Settings.Default, "Command", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboCommand.FormattingEnabled = true;
            this.comboCommand.Location = new System.Drawing.Point(86, 435);
            this.comboCommand.Name = "comboCommand";
            this.comboCommand.Size = new System.Drawing.Size(409, 21);
            this.comboCommand.TabIndex = 1;
            this.comboCommand.Text = global::MopidyTray.Properties.Settings.Default.Command;
            // 
            // checkShowNotifications
            // 
            this.checkShowNotifications.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkShowNotifications.AutoSize = true;
            this.checkShowNotifications.Checked = global::MopidyTray.Properties.Settings.Default.ShowNotifications;
            this.checkShowNotifications.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::MopidyTray.Properties.Settings.Default, "ShowNotifications", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkShowNotifications.Location = new System.Drawing.Point(584, 436);
            this.checkShowNotifications.Name = "checkShowNotifications";
            this.checkShowNotifications.Size = new System.Drawing.Size(125, 19);
            this.checkShowNotifications.TabIndex = 3;
            this.checkShowNotifications.Text = "Show notifications";
            this.checkShowNotifications.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AcceptButton = this.buttonCommand;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 465);
            this.Controls.Add(this.comboCommand);
            this.Controls.Add(this.buttonCommand);
            this.Controls.Add(this.labelCommand);
            this.Controls.Add(this.checkShowNotifications);
            this.Controls.Add(this.state);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Mopidy client";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ListView state;
        private System.Windows.Forms.ColumnHeader colKey;
        private System.Windows.Forms.ColumnHeader colValue;
        private System.Windows.Forms.CheckBox checkShowNotifications;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.Label labelCommand;
        private System.Windows.Forms.Button buttonCommand;
        private System.Windows.Forms.ComboBox comboCommand;
    }
}

