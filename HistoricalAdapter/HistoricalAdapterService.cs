using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

using KBCsv;
using HistoricalAdapter.Five9AdminService;
using WebDAVClient;

namespace HistoricalAdapter
{
    public partial class HistoricalAdapterService : ServiceBase
    {
        #region "Private Members"
        System.Timers.Timer tCurrent;
        System.Timers.Timer tDaily;
        
        WsAdminClient adminClient = null;
        AuthHeaderInserter inserter = null;
        string userName = null;
        string password = null;
        string filePath = null;
        int NUMDaysToKeepLOCALOUTPUT = 0;

        string WFMWebDavLocation = null;
        string WFMWebDavBasePath = null;
        string WFMWebDavFilePath = null;
        string WFMWebDavUserName = null;
        string WFMWebDavPassword = null;

        int TCSDataProcessorIntervalTime = 0;
        string HistoricalReportTimeZone = null;
        string ProductivityOutputTime = null;
        string ProductivityReportStartTime = null;
        string ProductivityReportEndTime = null;
        #endregion

        #region "Event Handlers"
        public HistoricalAdapterService()
        {
            InitializeComponent(); 
        }

        protected override void OnStart(string[] args)
        {
            AppLogger.Instance.Warn("HistoricalAdapterService.cs", "START SERVICE", "Service has been started.");
            SetConfigurations();

            tCurrent = new System.Timers.Timer();
            tCurrent.Interval = (TCSDataProcessorIntervalTime * 50 * 1000);// milliseconds
            tCurrent.AutoReset = true;
            tCurrent.Enabled = true;
            tCurrent.Start();
            tCurrent.Elapsed += new System.Timers.ElapsedEventHandler(WorkProcess);

            tDaily = new System.Timers.Timer();
            TimeSpan time = TimeSpan.Parse(ProductivityOutputTime);
            double seconds = (time - System.DateTime.Now.TimeOfDay).TotalSeconds;
            if (seconds > 1)
            {
                tDaily.Interval = (seconds * 1000);// milliseconds
            }
            else
            {
                tDaily.Interval = (24 * 60 * 60 * 1000); // milliseconds
            }
            tDaily.AutoReset = true;
            tDaily.Enabled = true;
            tDaily.Start();
            tDaily.Elapsed += new System.Timers.ElapsedEventHandler(DailyProcess);
            
            HistoricalTCSDataProcessing();
            //AgentProductivityDataProcessing(); // For Testing.
        }

        protected override void OnStop()
        {
            tCurrent.Enabled = false;
            tCurrent.Stop();
            tDaily.Enabled = false;
            tDaily.Stop();

            AppLogger.Instance.Warn("HistoricalAdapterService.cs","STOP SERVICE","Service has been stopped.");
        }

        public void WorkProcess(object sender, System.Timers.ElapsedEventArgs e)
        {
            HistoricalTCSDataProcessing();

            //Start Daily Timer or the first time
            if (!tDaily.Enabled)
            {
                TimeSpan start = new TimeSpan(12, 0, 0); //12 o'clock
                TimeSpan end = new TimeSpan(1, 0, 0); //1 o'clock
                TimeSpan now = DateTime.Now.TimeOfDay;

                if ((now > start) && (now < end))
                {
                    tDaily.Interval = (24 * 60 * 60 * 1000); // milliseconds
                    tDaily.AutoReset = true;
                    tDaily.Enabled = true;
                    tDaily.Start();
                    tDaily.Elapsed += new System.Timers.ElapsedEventHandler(DailyProcess);

                    //Process Agent Productivity Report
                    AgentProductivityDataProcessing();
                }
            }
        }

        public void DailyProcess(object sender, System.Timers.ElapsedEventArgs e)
        {
            tDaily.Interval = (24 * 60 * 60 * 1000); // milliseconds
            tDaily.AutoReset = true;
            tDaily.Enabled = true;

            AgentProductivityDataProcessing();
        }
        #endregion

        #region "Private Methods"
         private void HistoricalTCSDataProcessing()
        {
            var callLogTable = new DataTable();
            var skillTable = new DataTable();
            var stateTable = new DataTable();
            DataTable tcsDataTable = GetTCSDataTable();

            inserter = new AuthHeaderInserter();
            inserter.Username = userName;
            inserter.Password = password;

            adminClient = new WsAdminClient();
            adminClient.Endpoint.EndpointBehaviors.Add(new AuthHeaderBehavior(inserter));

            // All reports require a start and end date!
            customReportCriteria reportCriteria = new customReportCriteria();
            reportCriteria.time = new reportTimeCriteria();
            

            if ((System.DateTime.Now.Minute >= 0) && (System.DateTime.Now.Minute < 30))
            {
                reportCriteria.time.start = System.DateTime.Now.AddMinutes(-(30 + DateTime.Now.Minute)).AddSeconds(-DateTime.Now.Second);
                reportCriteria.time.end = System.DateTime.Now.AddMinutes(-(System.DateTime.Now.Minute + 1)).AddSeconds(59 - DateTime.Now.Second);
            }
            else
            {
                reportCriteria.time.start = System.DateTime.Now.AddMinutes(-System.DateTime.Now.Minute).AddSeconds(-DateTime.Now.Second);
                reportCriteria.time.end = System.DateTime.Now.AddMinutes(-(System.DateTime.Now.Minute - 29)).AddSeconds(59 - DateTime.Now.Second);
            }
            reportCriteria.time.startSpecified = true;
            reportCriteria.time.endSpecified = true;

            callLogTable = GetReportData(AdapterConstants.HISTORICAL_REPORT_FOLDER, AdapterConstants.HISTORICAL_CUSTOM_CALL_LOG, reportCriteria);
            skillTable = GetReportData(AdapterConstants.HISTORICAL_REPORT_FOLDER, AdapterConstants.HISTORICAL_CUSTOM_AGENT_SKILL_LOGIN_TIME, reportCriteria);
            stateTable = GetReportData(AdapterConstants.HISTORICAL_REPORT_FOLDER, AdapterConstants.HISTORICAL_CUSTOM_AGENT_STATE_REPORT, reportCriteria);

            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                DateTime timeFilter = TimeZoneInfo.ConvertTimeFromUtc(reportCriteria.time.start.ToUniversalTime(), zone);
                string filterExpression = "(HALFHOUR = '" + timeFilter.ToString("HH") + ":" + timeFilter.ToString("mm") + "'" + ")";
                callLogTable = callLogTable.Select(filterExpression).CopyToDataTable();

                DateTime zoneTime = TimeZoneInfo.ConvertTimeFromUtc(reportCriteria.time.start.ToUniversalTime(), TimeZoneInfo.FindSystemTimeZoneById(HistoricalReportTimeZone));
                string HalfHour = zoneTime.ToString("HH") + ":" + zoneTime.ToString("mm");

                DataTable dtable = callLogTable.DefaultView.ToTable(true, "SKILL");
                DataRow dataRow;

                foreach (DataRow row in dtable.Rows)
                {
                    if ((row["SKILL"] != null) && (row["SKILL"].ToString() != "[None]"))
                    {
                        dataRow = tcsDataTable.NewRow();

                        dataRow["Date"] = Utils.GetDate(row["SKILL"], callLogTable, HistoricalReportTimeZone);
                        dataRow["Interval End"] = HalfHour;
                        //dataRow["Interval End"] = Utils.GetIntervalTime(row["SKILL"], callLogTable);
                        dataRow["Identifier"] = "TCSDATA";

                        dataRow["Contact Group ID"] = row["SKILL"];
                        int NCO = Utils.GetNCO(row["SKILL"], callLogTable);
                        dataRow["Contacts Offered (NCO)"] = NCO;
                        int NCH = Utils.GetContactsHandled(row["SKILL"], callLogTable);
                        dataRow["Contacts Handled (NCH)"] = NCH;
                        dataRow["Average Talk Time (ATT)"] = Utils.GetAverageTalkTime(row["SKILL"], NCH, callLogTable);
                        dataRow["Average After Contact Work Time (ACWT)"] = Utils.GetAverageAfterCallWorkTime(row["SKILL"], NCH, callLogTable);
                        dataRow["Average Delay (ASA)"] = Utils.GetAverageDelayTime(row["SKILL"], NCH, callLogTable);
                        dataRow["Percent Service Level (%SL)"] = Utils.GetPercentServiceLevel(row["SKILL"], NCO, callLogTable);
                        dataRow["Average Positions Staffed (APS)"] = Utils.GetAveragePositionsStaffed(row["SKILL"], skillTable, stateTable);
                        dataRow["Actual Abandons (ABD)"] = Utils.GetActualAbandons(row["SKILL"], callLogTable);

                        tcsDataTable.Rows.Add(dataRow);
                    }
                }
                GenerateITFOutputFile(tcsDataTable, AdapterConstants.FILE_HISTORICAL_TCS_DATA);
                //CSV Output
                //GenerateOutputFile(tcsDataTable, AdapterConstants.FILE_HISTORICAL_TCS_DATA);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("HistoricalAdapterService.cs", "HistoricalTCSDataProcessing", ex);
            }
        }

        private void AgentProductivityDataProcessing()
        {
            DataTable rptTable = new DataTable();
            var skillTable = new DataTable();
            DataTable agentDataTable = GetAgentDataTable();

            inserter = new AuthHeaderInserter();
            inserter.Username = userName;
            inserter.Password = password;

            adminClient = new WsAdminClient();
            adminClient.Endpoint.EndpointBehaviors.Add(new AuthHeaderBehavior(inserter));

            // All reports require a start and end date!
            customReportCriteria reportCriteria = new customReportCriteria();
            reportCriteria.time = new reportTimeCriteria();
            reportCriteria.time.start = GetProductivityReportStartTime();
            reportCriteria.time.end = GetProductivityReportEndTime();
            reportCriteria.time.startSpecified = true;
            reportCriteria.time.endSpecified = true;

            rptTable = GetReportData(AdapterConstants.HISTORICAL_REPORT_FOLDER, AdapterConstants.HISTORICAL_CUSTOM_AGENT_STATE_REPORT, reportCriteria);
            skillTable = GetReportData(AdapterConstants.HISTORICAL_REPORT_FOLDER, AdapterConstants.HISTORICAL_CUSTOM_AGENT_SKILL_LOGIN_TIME, reportCriteria);

            try
            {
                string[] distinctColumns = { "AGENTID", "AGENT", "SKILL" };
                DataTable dtable = rptTable.DefaultView.ToTable(true, distinctColumns);

                DataRow dataRow;

                foreach (DataRow row in dtable.Rows)
                {
                    if ((row["SKILL"] != null) && (row["SKILL"].ToString() != "[None]"))
                    {
                        dataRow = agentDataTable.NewRow();

                        dataRow["ID"] = Utils.GetAgentID(row["AGENTID"]);  //Last 7 Character
                        dataRow["ACD Group"] = Utils.GetACDGroup(row["SKILL"]); //Max 30 Character
                        dataRow["SIT"] = Utils.GetSignInTime(row["AGENTID"], row["SKILL"], rptTable);
                        dataRow["SOT"] = Utils.GetSignOutTime(row["AGENTID"], row["SKILL"], rptTable);

                        int NCH = Utils.GetNCHCount(row["AGENTID"], row["SKILL"], rptTable);
                        dataRow["NCH"] = NCH;

                        dataRow["ATT"] = Utils.GetAverageTalkTime(row["AGENTID"], row["SKILL"], rptTable, NCH);
                        dataRow["AWT"] = Utils.GetContactWorkTime(row["AGENTID"], row["SKILL"], rptTable, NCH);

                        string LoginTime = Utils.GetLoginTime(row["AGENT"], row["SKILL"], skillTable);
                        string NotReadyTime = Utils.GetNotReadyTime(row["AGENT"], row["SKILL"], rptTable);
                        string WaitTime = Utils.GetWaitTime(row["AGENT"], row["SKILL"], rptTable);

                        dataRow["PIP"] = Utils.GetPluggedInPercentage(LoginTime, WaitTime);
                        dataRow["NOC"] = Utils.GetOutboundContactNumber(row["AGENTID"], row["SKILL"], rptTable);
                        dataRow["AOTT"] = Utils.GetAverageOutboundTalkTime(row["AGENTID"], row["SKILL"], rptTable, NCH);
                        dataRow["AOWT"] = Utils.GetAverageOutboundWorkTime(row["AGENTID"], row["SKILL"], rptTable, NCH);
                        dataRow["AVL"] = Utils.GetAvailableTime(LoginTime, NotReadyTime);
                        dataRow["UNAVL"] = Utils.GetUnAvailableTime(NotReadyTime);

                        agentDataTable.Rows.Add(dataRow);
                    }
                }

                GenerateITFOutputFile(agentDataTable, AdapterConstants.FILE_HISTORICAL_AGENT_PRDUCTIVITY);
                //CSV Output
                //GenerateOutputFile(agentDataTable, AdapterConstants.FILE_HISTORICAL_AGENT_PRDUCTIVITY);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("HistoricalAdapterService.cs", "AgentProductivityDataProcessing", ex);
            }
        }

        private DataTable GetReportData(string folderName, string reportName, customReportCriteria criteria)
        {
            DataTable reportTable = new DataTable();

            try
            {
                string reportID = adminClient.runReport(folderName, reportName, criteria);

                bool isReportRunning = true;
                while (isReportRunning)
                {
                    try
                    {
                        isReportRunning = adminClient.isReportRunning(reportID, 600);
                    }
                    catch (Exception ex)
                    {
                        isReportRunning = true;
                    }
                }

                if (!isReportRunning)
                {
                    string reportData = adminClient.getReportResultCsv(reportID);
                    reportTable = FillDataTable(reportData);
                }
                AppLogger.Instance.Warn("HistoricalAdapterService.cs", "GetReportData","Successfully Processed Report : " + reportName);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("HistoricalAdapterService.cs", "GetReportData", ex);
            }

            return reportTable;
        }

        private DataTable FillDataTable(string reportData)
        {
            DataTable dt = new DataTable();

            DataRow dRow;
            try
            {
                using (var reader = CsvReader.FromCsvString(reportData))
                {
                    HeaderRecord hRecord = reader.ReadHeaderRecord();
                    for (int i = 0; i < hRecord.Count; i++)
                    {
                        dt.Columns.Add(hRecord[i].Replace(" ", ""));
                    }

                    while (reader.HasMoreRecords)
                    {
                        var dataRecord = reader.ReadDataRecord();
                        dRow = dt.NewRow();

                        for (int i = 0; i < hRecord.Count; i++)
                        {
                            dRow[hRecord[i].Replace(" ", "")] = dataRecord[i];
                        }
                        dt.Rows.Add(dRow);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return dt;
        }

        private DataTable GetTCSDataTable()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("Date");
            dt.Columns.Add("Interval End");
            dt.Columns.Add("Identifier");
            dt.Columns.Add("Contact Group ID");
            dt.Columns.Add("Contacts Offered (NCO)");
            dt.Columns.Add("Contacts Handled (NCH)");
            dt.Columns.Add("Average Talk Time (ATT)");
            dt.Columns.Add("Average After Contact Work Time (ACWT)");
            dt.Columns.Add("Average Delay (ASA)");
            dt.Columns.Add("Percent Service Level (%SL)");
            dt.Columns.Add("Average Positions Staffed (APS)");
            dt.Columns.Add("Actual Abandons (ABD)");

            return dt;
        }

        private DataTable GetAgentDataTable()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("ID");
            dt.Columns.Add("ACD Group");
            dt.Columns.Add("SIT");
            dt.Columns.Add("SOT");
            dt.Columns.Add("NCH");
            dt.Columns.Add("ATT");
            dt.Columns.Add("AWT");
            dt.Columns.Add("PIP");
            dt.Columns.Add("NOC");
            dt.Columns.Add("AOTT");
            dt.Columns.Add("AOWT");
            dt.Columns.Add("AVL");
            dt.Columns.Add("UNAVL");

            return dt;
        }

        private void GenerateOutputFile(DataTable dTable, string fileName)
        {
            ClearOldFiles();
            using (var stream = File.CreateText(@filePath + fileName + "_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv"))
            {
                string csvHeaderRow = string.Empty;
                foreach (DataColumn column in dTable.Columns)
                {
                    csvHeaderRow += column.ColumnName + ",";
                }
                stream.WriteLine(csvHeaderRow.Substring(0, (csvHeaderRow.Length - 1)));

                foreach (DataRow dRow in dTable.Rows)
                {
                    string csvRow = string.Empty;

                    foreach (DataColumn col in dTable.Columns)
                    {
                        csvRow += dRow.Field<string>(col.ColumnName) + ",";
                    }
                    stream.WriteLine(csvRow.Substring(0, (csvRow.Length - 1)));
                }
            }
        }

        private void GenerateITFOutputFile(DataTable dTable, string reportName)
        {
            ClearOldFiles();
            string fileName = reportName + "_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".itf";
            using (var stream = File.CreateText(@filePath + fileName))
            {
                if (reportName.Equals(AdapterConstants.FILE_HISTORICAL_AGENT_PRDUCTIVITY))
                {
                    stream.WriteLine("\t\t\t\t\t\t\t Agent Productivity Report");
                    stream.WriteLine("\t\t\t\t\t\t\t\t Date:" + System.DateTime.Now.AddDays(-1).ToShortDateString());
                    stream.WriteLine();
                }

                foreach (DataRow dRow in dTable.Rows)
                {
                    string dataRow = string.Empty;

                    foreach (DataColumn col in dTable.Columns)
                    {
                        dataRow += dRow.Field<string>(col.ColumnName) + "\t";
                    }
                    stream.WriteLine(dataRow.Substring(0, (dataRow.Length - 1)));
                }

                if (reportName.Equals(AdapterConstants.FILE_HISTORICAL_AGENT_PRDUCTIVITY))
                {
                    stream.WriteLine("$END OF ASPECT");
                }
            }
            WebDavUpload(WFMWebDavLocation,WFMWebDavBasePath, WFMWebDavFilePath, WFMWebDavUserName,WFMWebDavPassword,@filePath,fileName).Wait();
        }

        private static async Task WebDavUpload(string webDavLocation, string webDavBasePath, string webDavFilePath, string webDavUserName, string webDavPassword,  string filePath, string fileName)
        {
            try
            {
                //Password encryption
                //var password = EncDec.Decrypt("PASSWORD", webDavPassword);

                // Basic authentication required
                IClient c = new Client(new NetworkCredential { UserName = webDavUserName, Password = webDavPassword })
                {
                    Server = webDavLocation,
                    BasePath = webDavBasePath
                };

                // List items in the root folder
                var files = await c.List();

                // Find first folder in the root folder
                var folder = files.FirstOrDefault(f => f.Href.EndsWith(webDavFilePath));

                ////Your .itf file path
                //var tempFileName = Path.GetTempFileName();

                //// Update file back to webdav
                //var tempName = Path.GetRandomFileName();
                
                using (var fileStream = File.OpenRead(filePath))
                {
                    var fileUploaded = await c.Upload(folder.Href, fileStream, fileName);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("HistoricalAdapterService.cs", "WFM Upload", ex);
            }
        }

        private void ClearOldFiles()
        {
            string[] outputFiles = Directory.GetFiles(@filePath);
            foreach (string file in outputFiles)
            {
                try
                {
                    string[] fName = file.Split('_');
                    DateTime fDate = DateTime.ParseExact(fName[1].Trim().Substring(0,14), "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                 
                    if (fDate < System.DateTime.Now.AddDays(-NUMDaysToKeepLOCALOUTPUT))
                    {
                        File.Delete(@file);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("HistoricalAdapterService.cs","ClearOldFiles",ex);
                }
            }
        }

        private DateTime GetProductivityReportStartTime()
        {
            if (!string.IsNullOrEmpty(ProductivityReportStartTime))
            {
                try
                {
                    TimeSpan time = TimeSpan.Parse(ProductivityReportStartTime);
                    return System.Convert.ToDateTime(DateTime.Now.AddDays(-1).Date.Add(time));
                }
                catch (Exception ex)
                {
                }
            }
            return System.DateTime.Now.AddHours(-24);
        }

        private DateTime GetProductivityReportEndTime()
        {
            if (!string.IsNullOrEmpty(ProductivityReportEndTime))
            {
                try
                {
                    TimeSpan time = TimeSpan.Parse(ProductivityReportEndTime);
                    return System.Convert.ToDateTime(DateTime.Now.AddDays(-1).Date.Add(time));
                }
                catch (Exception ex)
                {
                }
            }
            return System.DateTime.Now;
        }
        
        private void SetConfigurations()
        {
            var appSettings = System.Configuration.ConfigurationSettings.AppSettings;

            userName = appSettings["Five9UserName"].ToString();
            password = appSettings["Five9Password"].ToString();
            filePath = appSettings["OutputFilePath"].ToString();
            NUMDaysToKeepLOCALOUTPUT = System.Convert.ToInt16(appSettings["NUMDaysToKeepLOCALOUTPUT"].ToString());

            WFMWebDavLocation = appSettings["WFMWebDavLocation"].ToString();
            WFMWebDavBasePath = appSettings["WFMWebDavBasePath"].ToString();
            WFMWebDavFilePath = appSettings["WFMWebDavFilePath"].ToString();
            WFMWebDavUserName = appSettings["WFMWebDavUserName"].ToString();
            WFMWebDavPassword = appSettings["WFMWebDavPassword"].ToString();

            HistoricalReportTimeZone = appSettings["HistoricalReportTimeZone"].ToString();
            if (string.IsNullOrEmpty(HistoricalReportTimeZone))
            {
                HistoricalReportTimeZone = "Pacific Standard Time";
            }
            TCSDataProcessorIntervalTime = System.Convert.ToInt16(appSettings["TCSDataProcessorIntervalTime"].ToString());
            ProductivityOutputTime= appSettings["ProductivityOutputTime"].ToString();
            ProductivityReportStartTime = appSettings["ProdReportStartTime"].ToString();
            ProductivityReportEndTime = appSettings["ProdReportEndTime"].ToString();
        }
        #endregion
    }
}
