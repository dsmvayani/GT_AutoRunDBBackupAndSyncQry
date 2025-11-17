using GTVeriSys_Net;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;

namespace GT_AutoRunDBBackupAndSyncQry
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        GTVeriSys_Net.GTVS GTVeriSys_Net = new GTVeriSys_Net.GTVS();

        private async void UploadDataBtn_Click(object sender, EventArgs e)
        {
            UploadDataBtn.Enabled = false;
            string oldText = UploadDataBtn.Text;

            bool uploading = true;

            // 🔹 Start simple animation (dots increasing)
            var animationTask = Task.Run(async () =>
            {
                int dotCount = 0;
                while (uploading)
                {
                    dotCount = (dotCount + 1) % 4;
                    string dots = new string('.', dotCount);
                    Invoke(new Action(() => UploadDataBtn.Text = "Uploading" + dots));
                    await Task.Delay(500);
                }
            });

            try
            {
                await Task.Run(() =>
                {
                    RunBackupAndUpload();
                });
            }
            finally
            {
                // 🔹 Stop animation
                uploading = false;
                await animationTask;

                UploadDataBtn.Text = oldText;
                UploadDataBtn.Enabled = true;
            }
        }


        private void RunBackupAndUpload()
        {
            // 🔹 Force TLS 1.2 (for secure connection)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // 🔹 Disable certificate validation (optional but helps on old PCs)
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DBBackups");
            Directory.CreateDirectory(backupFolder);
            string logFile = Path.Combine(backupFolder, "UploadLog.txt");

            try
            {
                string connStr = ConfigurationManager.AppSettings["DataUploadConnectionString"];

                string dbName;
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    dbName = conn.Database;
                    DBNameText.Invoke(new Action(() => DBNameText.Text = dbName));
                }

                string backupFile = Path.Combine(backupFolder, $"{dbName}.bak");
                string zipFile = Path.Combine(backupFolder, $"{dbName}.zip");

                // 1️⃣ SQL backup
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string edition;
                    using (SqlCommand cmd = new SqlCommand("SELECT SERVERPROPERTY('Edition')", conn))
                    {
                        edition = cmd.ExecuteScalar()?.ToString() ?? "";
                    }

                    bool supportsCompression = edition.Contains("Enterprise") ||
                                               edition.Contains("Standard") ||
                                               edition.Contains("Developer");

                    string sql = supportsCompression
                        ? $@"BACKUP DATABASE [{dbName}] TO DISK = '{backupFile}' WITH INIT, FORMAT, COMPRESSION"
                        : $@"BACKUP DATABASE [{dbName}] TO DISK = '{backupFile}' WITH INIT, FORMAT";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }
                }

                // 2️⃣ Zip backup file
                if (File.Exists(zipFile)) File.Delete(zipFile);
                using (FileStream zipToOpen = new FileStream(zipFile, FileMode.Create))
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(backupFile, Path.GetFileName(backupFile));
                }

                // 3️⃣ FTP details
                string ftpHost = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPHost"], "Y");
                string ftpPort = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPPort"], "Y");
                string ftpUser = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPUser"], "Y");
                string ftpPass = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPPassword"], "Y");
                string ftpPath = ConfigurationManager.AppSettings["FTPPath"];

                string ftpUrl = $"ftp://{ftpHost}:{ftpPort}{ftpPath}/{Path.GetFileName(zipFile)}";

                // 🔹 Common FTP request configuration method
                FtpWebRequest CreateFtpRequest(string url, string method)
                {
                    var req = (FtpWebRequest)WebRequest.Create(url);
                    req.Method = method;
                    req.Credentials = new NetworkCredential(ftpUser, ftpPass);

                    // ✅ These 3 lines fix most random FTP issues:
                    req.UseBinary = true;
                    req.UsePassive = true;  // Enable passive mode (firewall-friendly)
                    req.Proxy = null;       // Disable proxy interference

                    req.KeepAlive = false;
                    req.EnableSsl = true;   // Use FTPS (if your server supports it)
                    return req;
                }

                // 4️⃣ Delete old file
                try
                {
                    var delRequest = CreateFtpRequest(ftpUrl, WebRequestMethods.Ftp.DeleteFile);
                    using (var delResponse = (FtpWebResponse)delRequest.GetResponse()) { }
                }
                catch
                {
                    // Ignore if file doesn’t exist
                }

                // 5️⃣ Upload new file
                var uploadRequest = CreateFtpRequest(ftpUrl, WebRequestMethods.Ftp.UploadFile);
                byte[] fileContents = File.ReadAllBytes(zipFile);
                uploadRequest.ContentLength = fileContents.Length;

                using (Stream requestStream = uploadRequest.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)uploadRequest.GetResponse())
                {
                    string successMsg = $"Backup and upload successful! File: {Path.GetFileName(zipFile)}";
                    MessageBox.Show(successMsg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    File.AppendAllText(logFile, $"[{DateTime.Now}] SUCCESS - {successMsg}{Environment.NewLine}");
                }

                // 6️⃣ Clean up
                if (File.Exists(backupFile)) File.Delete(backupFile);
                if (File.Exists(zipFile)) File.Delete(zipFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.AppendAllText(logFile, $"[{DateTime.Now}] ERROR - {ex.Message}{Environment.NewLine}");
            }
        }




        private async void DataSyncBtn_Click(object sender, EventArgs e)
        {
            // Button disable aur text change
            DataSyncBtn.Enabled = false;
            string oldText = DataSyncBtn.Text;
            DataSyncBtn.Text = "Loading...";
            DataSyncBtn.ForeColor = Color.White;

            try
            {
                await Task.Run(() =>
                {
                    RunDataSyncFunc(); // Main kaam alag method me
                    SendBranchReportEmail();
                });

                // ✅ Jab saare databases ho jaye tab ek hi dialog box
                //MessageBox.Show("🎉 Data Sync Completed Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DataSyncLabel.Text = "🎉 Data Sync Completed Successfully!";
            }
            finally
            {
                // Wapas button ko normal state me lao
                DataSyncBtn.Text = oldText;
                DataSyncBtn.Enabled = true;
            }
        }
        private void RunDataSyncFunc()
        {
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataSyncLog.txt");

            try
            {
                // Step 1: Read config values
                string[] paths = ConfigurationManager.AppSettings["DataSyncMultipleDataBaseBackupPath"]
                                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(p => p.Trim())
                                    .ToArray();

                string connectionString = ConfigurationManager.AppSettings["DataSyncConnectionString"];

                foreach (var archivePath in paths)
                {
                    string dbName = Path.GetFileNameWithoutExtension(archivePath); // fallback name
                    try
                    {
                        if (!File.Exists(archivePath))
                        {
                            File.AppendAllText(logFile, $"[{DateTime.Now}] ❌ File not found: {archivePath}{Environment.NewLine}");
                            continue;
                        }

                        string extractPath = Path.GetDirectoryName(archivePath);
                        string bakFile = "";

                        // Step 2: Extract .bak from .rar/.zip
                        using (var archive = ArchiveFactory.Open(archivePath))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (!entry.IsDirectory && entry.Key.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                                {
                                    bakFile = Path.Combine(extractPath, Path.GetFileName(entry.Key));
                                    entry.WriteToFile(bakFile, new ExtractionOptions() { Overwrite = true });
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(bakFile))
                        {
                            File.AppendAllText(logFile, $"[{DateTime.Now}] ❌ No .bak file found in {archivePath}{Environment.NewLine}");
                            continue;
                        }

                        // Step 3: Get DB name from .bak
                        dbName = Path.GetFileNameWithoutExtension(bakFile);

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();

                            // Step 4: Get logical names
                            string fileListSql = $"RESTORE FILELISTONLY FROM DISK = '{bakFile}'";
                            SqlCommand fileListCmd = new SqlCommand(fileListSql, conn);
                            SqlDataReader reader = fileListCmd.ExecuteReader();

                            string logicalData = "";
                            string logicalLog = "";

                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string logicalName = reader["LogicalName"].ToString();
                                    string type = reader["Type"].ToString();

                                    if (type == "D") logicalData = logicalName;
                                    else if (type == "L") logicalLog = logicalName;
                                }
                            }
                            reader.Close();

                            if (string.IsNullOrEmpty(logicalData) || string.IsNullOrEmpty(logicalLog))
                            {
                                File.AppendAllText(logFile, $"[{DateTime.Now}] ❌ Logical file names not found for DB: {dbName}{Environment.NewLine}");
                                continue;
                            }

                            // Step 5: Build restore query
                            string dataFile = Path.Combine(extractPath, $"{dbName}.mdf");
                            string logFilePath = Path.Combine(extractPath, $"{dbName}_log.ldf");

                            string restoreSql = $@"
                    RESTORE DATABASE [{dbName}]
                    FROM DISK = '{bakFile}'
                    WITH REPLACE,
                    MOVE '{logicalData}' TO '{dataFile}',
                    MOVE '{logicalLog}' TO '{logFilePath}'";

                            SqlCommand restoreCmd = new SqlCommand(restoreSql, conn);
                            restoreCmd.ExecuteNonQuery();

                            File.AppendAllText(logFile, $"[{DateTime.Now}] ✅ Database '{dbName}' restored successfully{Environment.NewLine}");
                        }

                        // ✅ Restore ke turant baad stored procedures execute karo
                        ExecuteSyncProcedures(connectionString, dbName, logFile);
                    }
                    catch (Exception exDb)
                    {
                        // agar ek DB me problem ho, baki process continue rahe
                        File.AppendAllText(logFile, $"[{DateTime.Now}] ❌ Database '{dbName}' restore failed - {exDb.Message}{Environment.NewLine}");
                    }
                }

                // Saare DB process hone ke baad ek line log me
                File.AppendAllText(logFile, $"[{DateTime.Now}] 🎉 Data Sync Completed{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"[{DateTime.Now}] ❌ Fatal Error in DataSync - {ex.Message}{Environment.NewLine}");
            }
        }
        private void ExecuteSyncProcedures(string connectionString, string databaseName, string logFile)
        {
            string[] procedures =
            {
                "SYNC_UploadVoucherBranchBasedSyncLinkServerSP",
                "SYNC_UploadSaleInovoicesBranchBasedSyncLinkServer_BD_SP",
                "SYNC_UploadSalesReturnBranchBasedSyncLinkServer_BD_SP",
                "SYNC_UploadTransactionBranchBasedSyncLinkServerSP"
            };

            foreach (var sp in procedures)
            {
                using (SqlConnection conn = new SqlConnection(connectionString.Replace("master", databaseName)))
                {
                    try
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(sp, conn);
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.CommandTimeout = 600;

                        cmd.ExecuteNonQuery();

                        File.AppendAllText(logFile, $"[{DateTime.Now}] ✅ DB: {databaseName} | SP: {sp} | Success{Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(logFile, $"[{DateTime.Now}] ❌ DB: {databaseName} | SP: {sp} | Failed - {ex.Message}{Environment.NewLine}");
                    }
                }
            }
        }
        public void SendBranchReportEmail()
        {
            try
            {
                // 🔹 Step 1: Config values uthao
                string connectionString = ConfigurationManager.AppSettings["EmailDatabase"];
                string toEmails = ConfigurationManager.AppSettings["ToSendingEmail"];
                string fromEmail = ConfigurationManager.AppSettings["Email"];
                string password = ConfigurationManager.AppSettings["Password"];
                int port = int.Parse(ConfigurationManager.AppSettings["PortNo"]);

                // 🔹 Step 2: SQL Query
                string sql = @"
                    SELECT
                        w.WarehouseCode as BranchCode,
                        w.Warehouse as Branch,
                        p.BillNo,
                        Format(Cast(p.BillDate as Date) ,'dd-MMM-yyyy') as BillDate
                    FROM INV_PointofSalesMasterTAB p
                    INNER JOIN (
                        SELECT WarehouseCode, MAX(BillDate) AS LastBillDate
                        FROM INV_PointofSalesMasterTAB
                        GROUP BY WarehouseCode
                    ) x ON p.WarehouseCode = x.WarehouseCode AND p.BillDate = x.LastBillDate
                    LEFT JOIN GEN_WarehouseTAB w 
                        ON p.WarehouseCode = w.WarehouseCode;
                ";

                DataTable dt = new DataTable();

                // 🔹 Step 3: Run query and fill datatable
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                    throw new Exception("⚠️ No data found in SQL query!");

                // 🔹 Step 4: Convert DataTable to HTML
                StringBuilder sb = new StringBuilder();
                sb.Append("<h3>📊 Branch Report</h3>");
                sb.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

                // Table header
                sb.Append("<tr style='background-color:#f2f2f2;'>");
                foreach (DataColumn col in dt.Columns)
                    sb.AppendFormat("<th>{0}</th>", col.ColumnName);
                sb.Append("</tr>");

                // Table rows
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (var item in row.ItemArray)
                        sb.AppendFormat("<td>{0}</td>", item);
                    sb.Append("</tr>");
                }
                sb.Append("</table>");

                // 🔹 Step 5: Send Email
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Report");

                // multiple emails add karo
                foreach (string email in toEmails.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    mail.To.Add(email.Trim());
                }

                mail.Subject = "Daily Branch Report";
                mail.Body = sb.ToString();
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", port))
                {
                    smtp.Credentials = new NetworkCredential(fromEmail, password);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }

                Console.WriteLine("✅ Report email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in SendBranchReportEmail: " + ex.Message);
            }
        }





        private async void DataDownloadBtn_Click(object sender, EventArgs e)
        {
            // Button disable aur text change
            DataDownloadBtn.Enabled = false;
            string oldText = UploadDataBtn.Text;
            DataDownloadBtn.Text = "Loading...";


            try
            {
                await Task.Run(() =>
                {
                    DownloadData(); // Main kaam alag method me
                });
            }
            finally
            {
                // Wapas button ko normal state me lao
                DataDownloadBtn.Text = oldText;
                DataDownloadBtn.Enabled = true;
            }
            
        }
        private void DownloadData()
        {
            try
            {
                // Step 1: FTP details
                string ftpHost = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPHost"], "Y");
                string ftpPort = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPPort"], "Y");
                string ftpUser = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPUser"], "Y");
                string ftpPass = GTVeriSys_Net.nDecode(ConfigurationManager.AppSettings["FTPPassword"], "Y");
                string FTPMultiplePath = ConfigurationManager.AppSettings["FTPMultiplePath"];
                string FTPIgnoreFileName = ConfigurationManager.AppSettings["FTPIgnoreFileName"];
                string DownloadPath = ConfigurationManager.AppSettings["DownloadPath"];


                string basePath = Path.Combine(DownloadPath, "DataDownload_" + DateTime.Now.ToString("ddMMyyyy_hhmmtt"));
                Directory.CreateDirectory(basePath);


                // Step 3: split multiple paths
                string[] paths = FTPMultiplePath.Split(',');
                string[] ignoreFiles = FTPIgnoreFileName.Split(','); // ignore list

                foreach (string remotePath in paths)
                {
                    string ftpFullPath = $"ftp://{ftpHost}:{ftpPort}{remotePath}";

                    // get file list from ftp
                    FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create(ftpFullPath);
                    listRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                    listRequest.Credentials = new NetworkCredential(ftpUser, ftpPass);

                    List<string> files = new List<string>();
                    using (FtpWebResponse listResponse = (FtpWebResponse)listRequest.GetResponse())
                    using (StreamReader reader = new StreamReader(listResponse.GetResponseStream()))
                    {
                        string line = reader.ReadLine();
                        while (!string.IsNullOrEmpty(line))
                        {
                            files.Add(line);
                            line = reader.ReadLine();
                        }
                    }

                    // download each file
                    foreach (string file in files)
                    {
                        // agar file ignore list mai hai to skip kar do
                        if (ignoreFiles.Contains(file, StringComparer.OrdinalIgnoreCase))
                            continue;

                        string localFilePath = Path.Combine(basePath, file);
                        string ftpFileUrl = $"{ftpFullPath}/{file}";

                        // download
                        FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create(ftpFileUrl);
                        downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                        downloadRequest.Credentials = new NetworkCredential(ftpUser, ftpPass);

                        using (FtpWebResponse downloadResponse = (FtpWebResponse)downloadRequest.GetResponse())
                        using (Stream ftpStream = downloadResponse.GetResponseStream())
                        using (FileStream fileStream = new FileStream(localFilePath, FileMode.Create))
                        {
                            ftpStream.CopyTo(fileStream);
                        }

                        // download ke baad delete
                        FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(ftpFileUrl);
                        deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                        deleteRequest.Credentials = new NetworkCredential(ftpUser, ftpPass);
                        using (FtpWebResponse deleteResponse = (FtpWebResponse)deleteRequest.GetResponse())
                        {
                            // delete success, kuch karna ho tu kar sakte ho
                        }
                    }
                }

                MessageBox.Show($"✅ Data downloaded & cleaned successfully in: {basePath}", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error while downloading: " + ex.Message);
            }
        }

        private new System.Windows.Forms.Timer syncTimer;
        private bool syncTriggeredToday = false;


        private void Form1_Shown(object sender, EventArgs e)
        {
            string isReqUploadDataBtn = ConfigurationManager.AppSettings["IsReqUploadDataBtn"];
            string isReqDataSyncBtn = ConfigurationManager.AppSettings["IsReqDataSyncBtn"];
            string isReqDataDownloadBtn = ConfigurationManager.AppSettings["IsReqDataDownloadBtn"];

            string SyncButtonClickTime = ConfigurationManager.AppSettings["SyncButtonClickTime"];

            UploadDataBtn.Visible = isReqUploadDataBtn == "1";
            DataSyncBtn.Visible = isReqDataSyncBtn == "1";
            DataDownloadBtn.Visible = isReqDataDownloadBtn == "1";

            if (isReqDataDownloadBtn == "1")
                DataDownloadBtn.PerformClick();

            // Start timer for auto sync
            if (isReqDataSyncBtn == "1")
            {
                syncTimer = new System.Windows.Forms.Timer();
                syncTimer.Interval = 1000; // 1 second
                syncTimer.Tick += (s, ev) => CheckAutoSyncTime(SyncButtonClickTime);
                syncTimer.Start();
            }
        }

        private void CheckAutoSyncTime(string configTime)
        {
            if (!DateTime.TryParseExact(
                    configTime,
                    "h:mm tt",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime targetDateTime))
            {
                return;
            }

            TimeSpan targetTime = targetDateTime.TimeOfDay;
            TimeSpan now = DateTime.Now.TimeOfDay;

            // Run only once per day
            if (!syncTriggeredToday &&
                now.Hours == targetTime.Hours &&
                now.Minutes == targetTime.Minutes)
            {
                syncTriggeredToday = true;
                DataSyncBtn.PerformClick();
            }

            // Reset flag at midnight so next day it runs again
            if (now.Hours == 0 && now.Minutes == 0)
            {
                syncTriggeredToday = false;
            }
        }


    }
}
