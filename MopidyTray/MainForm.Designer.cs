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
            this.ButtonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.PrevButton = new System.Windows.Forms.Button();
            this.PlayButton = new System.Windows.Forms.Button();
            this.PauseButton = new System.Windows.Forms.Button();
            this.NextButton = new System.Windows.Forms.Button();
            this.textURL = new System.Windows.Forms.TextBox();
            this.labelURL = new System.Windows.Forms.Label();
            this.buttonURL = new System.Windows.Forms.Button();
            this.ButtonPanel.SuspendLayout();
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
            this.state.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.state.Location = new System.Drawing.Point(16, 54);
            this.state.Margin = new System.Windows.Forms.Padding(4);
            this.state.Name = "state";
            this.state.Size = new System.Drawing.Size(933, 602);
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
            this.labelCommand.Location = new System.Drawing.Point(16, 673);
            this.labelCommand.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelCommand.Name = "labelCommand";
            this.labelCommand.Size = new System.Drawing.Size(81, 20);
            this.labelCommand.TabIndex = 0;
            this.labelCommand.Text = "Command:";
            // 
            // buttonCommand
            // 
            this.buttonCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCommand.Location = new System.Drawing.Point(668, 667);
            this.buttonCommand.Margin = new System.Windows.Forms.Padding(4);
            this.buttonCommand.Name = "buttonCommand";
            this.buttonCommand.Size = new System.Drawing.Size(103, 36);
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
            this.comboCommand.Location = new System.Drawing.Point(115, 669);
            this.comboCommand.Margin = new System.Windows.Forms.Padding(4);
            this.comboCommand.Name = "comboCommand";
            this.comboCommand.Size = new System.Drawing.Size(544, 28);
            this.comboCommand.TabIndex = 1;
            this.comboCommand.Text = global::MopidyTray.Properties.Settings.Default.Command;
            // 
            // checkShowNotifications
            // 
            this.checkShowNotifications.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkShowNotifications.AutoSize = true;
            this.checkShowNotifications.Checked = global::MopidyTray.Properties.Settings.Default.ShowNotifications;
            this.checkShowNotifications.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::MopidyTray.Properties.Settings.Default, "ShowNotifications", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkShowNotifications.Location = new System.Drawing.Point(800, 674);
            this.checkShowNotifications.Margin = new System.Windows.Forms.Padding(4);
            this.checkShowNotifications.Name = "checkShowNotifications";
            this.checkShowNotifications.Size = new System.Drawing.Size(150, 24);
            this.checkShowNotifications.TabIndex = 3;
            this.checkShowNotifications.Text = "Show notifications";
            this.checkShowNotifications.UseVisualStyleBackColor = true;
            // 
            // ButtonPanel
            // 
            this.ButtonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonPanel.AutoSize = true;
            this.ButtonPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ButtonPanel.Controls.Add(this.PrevButton);
            this.ButtonPanel.Controls.Add(this.PlayButton);
            this.ButtonPanel.Controls.Add(this.PauseButton);
            this.ButtonPanel.Controls.Add(this.NextButton);
            this.ButtonPanel.Enabled = false;
            this.ButtonPanel.Location = new System.Drawing.Point(620, 602);
            this.ButtonPanel.Name = "ButtonPanel";
            this.ButtonPanel.Size = new System.Drawing.Size(324, 54);
            this.ButtonPanel.TabIndex = 5;
            // 
            // PrevButton
            // 
            this.PrevButton.AccessibleDescription = "Go to the previous track";
            this.PrevButton.AccessibleName = "Previous";
            this.PrevButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.PrevButton.Location = new System.Drawing.Point(3, 3);
            this.PrevButton.Name = "PrevButton";
            this.PrevButton.Size = new System.Drawing.Size(75, 48);
            this.PrevButton.TabIndex = 0;
            this.PrevButton.Text = "◀▮";
            this.PrevButton.UseVisualStyleBackColor = true;
            this.PrevButton.Click += new System.EventHandler(this.PrevButton_Click);
            // 
            // PlayButton
            // 
            this.PlayButton.AccessibleDescription = "Play";
            this.PlayButton.AccessibleName = "Play";
            this.PlayButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.PlayButton.Location = new System.Drawing.Point(84, 3);
            this.PlayButton.Name = "PlayButton";
            this.PlayButton.Size = new System.Drawing.Size(75, 48);
            this.PlayButton.TabIndex = 1;
            this.PlayButton.Text = "▶";
            this.PlayButton.UseVisualStyleBackColor = true;
            this.PlayButton.Click += new System.EventHandler(this.PlayButton_Click);
            // 
            // PauseButton
            // 
            this.PauseButton.AccessibleDescription = "Pause";
            this.PauseButton.AccessibleName = "Pause";
            this.PauseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.PauseButton.Location = new System.Drawing.Point(165, 3);
            this.PauseButton.Name = "PauseButton";
            this.PauseButton.Size = new System.Drawing.Size(75, 48);
            this.PauseButton.TabIndex = 3;
            this.PauseButton.Text = "▮▮";
            this.PauseButton.UseVisualStyleBackColor = true;
            this.PauseButton.Click += new System.EventHandler(this.PauseButton_Click);
            // 
            // NextButton
            // 
            this.NextButton.AccessibleDescription = "Go to the next track";
            this.NextButton.AccessibleName = "Next";
            this.NextButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.NextButton.Location = new System.Drawing.Point(246, 3);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(75, 48);
            this.NextButton.TabIndex = 2;
            this.NextButton.Text = "▮▶";
            this.NextButton.UseVisualStyleBackColor = true;
            this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
            // 
            // textURL
            // 
            this.textURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textURL.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::MopidyTray.Properties.Settings.Default, "HostUri", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textURL.Location = new System.Drawing.Point(115, 16);
            this.textURL.Name = "textURL";
            this.textURL.Size = new System.Drawing.Size(726, 27);
            this.textURL.TabIndex = 1;
            this.textURL.Text = global::MopidyTray.Properties.Settings.Default.HostUri;
            // 
            // labelURL
            // 
            this.labelURL.AutoSize = true;
            this.labelURL.Location = new System.Drawing.Point(16, 19);
            this.labelURL.Name = "labelURL";
            this.labelURL.Size = new System.Drawing.Size(93, 20);
            this.labelURL.TabIndex = 0;
            this.labelURL.Text = "Mopidy URL:";
            // 
            // buttonURL
            // 
            this.buttonURL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonURL.Location = new System.Drawing.Point(847, 11);
            this.buttonURL.Name = "buttonURL";
            this.buttonURL.Size = new System.Drawing.Size(103, 36);
            this.buttonURL.TabIndex = 2;
            this.buttonURL.Text = "Connect";
            this.buttonURL.UseVisualStyleBackColor = true;
            this.buttonURL.Click += new System.EventHandler(this.buttonURL_Click);
            // 
            // MainForm
            // 
            this.AcceptButton = this.buttonCommand;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(962, 716);
            this.Controls.Add(this.buttonURL);
            this.Controls.Add(this.labelURL);
            this.Controls.Add(this.textURL);
            this.Controls.Add(this.ButtonPanel);
            this.Controls.Add(this.comboCommand);
            this.Controls.Add(this.buttonCommand);
            this.Controls.Add(this.labelCommand);
            this.Controls.Add(this.checkShowNotifications);
            this.Controls.Add(this.state);
            this.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "Mopidy client";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.ButtonPanel.ResumeLayout(false);
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
        private System.Windows.Forms.FlowLayoutPanel ButtonPanel;
        private System.Windows.Forms.Button PrevButton;
        private System.Windows.Forms.Button PlayButton;
        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.Button PauseButton;
        private System.Windows.Forms.TextBox textURL;
        private System.Windows.Forms.Label labelURL;
        private System.Windows.Forms.Button buttonURL;
    }
}

