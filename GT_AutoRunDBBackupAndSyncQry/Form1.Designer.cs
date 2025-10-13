namespace GT_AutoRunDBBackupAndSyncQry
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            UploadDataBtn = new Button();
            DataSyncBtn = new Button();
            label1 = new Label();
            DataSyncLabel = new Label();
            label2 = new Label();
            DataDownloadBtn = new Button();
            DBNameText = new Label();
            SuspendLayout();
            // 
            // UploadDataBtn
            // 
            UploadDataBtn.BackColor = Color.FromArgb(0, 123, 255);
            UploadDataBtn.FlatAppearance.BorderSize = 0;
            UploadDataBtn.FlatStyle = FlatStyle.Flat;
            UploadDataBtn.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            UploadDataBtn.ForeColor = Color.White;
            UploadDataBtn.Location = new Point(72, 66);
            UploadDataBtn.Name = "UploadDataBtn";
            UploadDataBtn.Size = new Size(237, 45);
            UploadDataBtn.TabIndex = 0;
            UploadDataBtn.Text = "⬆ Upload Data";
            UploadDataBtn.UseVisualStyleBackColor = false;
            UploadDataBtn.Click += UploadDataBtn_Click;
            // 
            // DataSyncBtn
            // 
            DataSyncBtn.BackColor = Color.SeaGreen;
            DataSyncBtn.FlatAppearance.BorderSize = 0;
            DataSyncBtn.FlatStyle = FlatStyle.Flat;
            DataSyncBtn.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            DataSyncBtn.ForeColor = Color.White;
            DataSyncBtn.Location = new Point(72, 75);
            DataSyncBtn.Name = "DataSyncBtn";
            DataSyncBtn.Size = new Size(237, 45);
            DataSyncBtn.TabIndex = 1;
            DataSyncBtn.Text = "🔄 Data Sync";
            DataSyncBtn.UseVisualStyleBackColor = false;
            DataSyncBtn.Click += DataSyncBtn_Click;
            // 
            // label1
            // 
            label1.BackColor = Color.Transparent;
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = Color.DarkBlue;
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(375, 50);
            label1.TabIndex = 1;
            label1.Text = "📂 Data Upload";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // DataSyncLabel
            // 
            DataSyncLabel.Font = new Font("Segoe UI", 10F, FontStyle.Italic, GraphicsUnit.Point);
            DataSyncLabel.ForeColor = Color.DarkGreen;
            DataSyncLabel.Location = new Point(30, 210);
            DataSyncLabel.Name = "DataSyncLabel";
            DataSyncLabel.Size = new Size(320, 40);
            DataSyncLabel.TabIndex = 0;
            DataSyncLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.FlatStyle = FlatStyle.Flat;
            label2.Location = new Point(285, 161);
            label2.Name = "label2";
            label2.Size = new Size(75, 15);
            label2.TabIndex = 2;
            label2.Text = "Version: 1.0.1";
            // 
            // DataDownloadBtn
            // 
            DataDownloadBtn.BackColor = Color.Red;
            DataDownloadBtn.FlatAppearance.BorderSize = 0;
            DataDownloadBtn.FlatStyle = FlatStyle.Flat;
            DataDownloadBtn.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            DataDownloadBtn.ForeColor = Color.White;
            DataDownloadBtn.Location = new Point(72, 85);
            DataDownloadBtn.Name = "DataDownloadBtn";
            DataDownloadBtn.Size = new Size(237, 45);
            DataDownloadBtn.TabIndex = 3;
            DataDownloadBtn.Text = "📥 Data Download";
            DataDownloadBtn.UseVisualStyleBackColor = false;
            DataDownloadBtn.Click += DataDownloadBtn_Click;
            // 
            // DBNameText
            // 
            DBNameText.AutoSize = true;
            DBNameText.Location = new Point(98, 123);
            DBNameText.Name = "DBNameText";
            DBNameText.Size = new Size(0, 15);
            DBNameText.TabIndex = 4;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(375, 182);
            Controls.Add(DBNameText);
            Controls.Add(DataDownloadBtn);
            Controls.Add(label2);
            Controls.Add(DataSyncLabel);
            Controls.Add(label1);
            Controls.Add(DataSyncBtn);
            Controls.Add(UploadDataBtn);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "GT_ARDBBSQ";
            Shown += Form1_Shown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button UploadDataBtn;
        private Button DataSyncBtn;
        private Label label1;
        private Label DataSyncLabel;
        private Label label2;
        private Button button1;
        private Button DataDownloadBtn;
        private Label DBNameText;
    }
}
