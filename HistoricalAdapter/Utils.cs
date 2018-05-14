using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using KBCsv;

namespace HistoricalAdapter
{
    public class Utils
    {
        #region "TCS Report"
        public static string GetDate(object SkillGroup, DataTable rptTable, string timeZone)
        {
            if (SkillGroup != null)
            {
                try
                {
                    string filterExpression = "(SKILL = '" + SkillGroup + "'" + ")";
                    DataRow row = rptTable.Select(filterExpression).First();
                    DateTime zoneTime = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(row["TIMESTAMP"]).ToUniversalTime(), TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    return zoneTime.ToShortDateString();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetDate", ex);
                }
            }
            return System.DateTime.Now.ToShortDateString();
        }

        public static string GetIntervalTime(object SkillGroup, DataTable rptTable)
        {
            if (SkillGroup != null)
            {
                try
                {
                    string filterExpression = "(SKILL = '" + SkillGroup + "'" + ")";
                    DataRow row = rptTable.Select(filterExpression).First();
                    return row["HALFHOUR"].ToString();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetIntervalTime", ex);
                }
            }
            return string.Empty;
        }

        public static int GetNCO(object Skill, DataTable rptTable)
        {
            int NCOValue = 0;
            //CALL ID
            //NCO = (Count CALL ID where CALL ID Is unique)

            if (Skill != null)
            {
                try
                {
                    string filterExpression = "(SKILL = '" + Skill + "'" + ")";
                    //NCOValue = rptTable.Select(filterExpression).Distinct().Count();
                    NCOValue = (from DataRow dRow in rptTable.Select(filterExpression)
                                        select new { col1 = dRow["CALLID"]}).Distinct().Count();
                    
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetNCO", ex);
                }
            }
            return NCOValue;
        }

        public static int GetContactsHandled(object Skill, DataTable rptTable)
        {
            int contactHandled = 0;
            //NCH = (Count CALL ID where CALL ID is Unique and where SKILL not[None] AND AGENT Not[None] and DISPOSITION does not contain ‘Transfer’)
            
            if (Skill != null)
            {
                try
                {
                    string filterExpression = "((AGENT <> '[None]') AND (DISPOSITION <> 'Transfer') AND (SKILL = '" + Skill + "'" + "))";
                    //contactHandled = rptTable.Select(filterExpression).Distinct().Count();
                    contactHandled = (from DataRow dRow in rptTable.Select(filterExpression)
                                select new { col1 = dRow["CALLID"] }).Distinct().Count();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetContactsHandled", ex);
                }
            }
            return contactHandled;
        }

        public static double GetAverageTalkTime(object Skill, int ContactHandled, DataTable rptTable)
        {
            double AvgTalkTime = 0;
            //TALK TIME LESS HOLD AND PARK
            //ATT = (Sum TALK TIME LESS HOLD AND PARK) / (NCH)
            TimeSpan totalTime = new TimeSpan();

            if (ContactHandled > 0)
            {
                if (Skill != null)
                {
                    string filterExpression = "(SKILL = '" + Skill + "'" + ")";
                    DataRow[] rows = rptTable.Select(filterExpression);

                    foreach (DataRow row in rows)
                    {
                        if (row["TALKTIMELESSHOLDANDPARK"]  != null)
                        {
                            try
                            {
                                TimeSpan tTime = TimeSpan.Parse(row["TALKTIMELESSHOLDANDPARK"].ToString());
                                totalTime += tTime;
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
                AvgTalkTime= Math.Round(totalTime.TotalSeconds/ContactHandled, 2); ;
            }
            return AvgTalkTime;
        }


        public static double GetAverageAfterCallWorkTime(object Skill, int ContactHandled, DataTable rptTable)
        {
            double AvgWorkTime = 0;
            TimeSpan totalTime = new TimeSpan();
            //AFTER CALL WORK TIME
            //ACWT = (Sum AFTER CALL WORK TIME)/ (NCH)

            if (ContactHandled > 0)
            {
                if (Skill != null)
                {
                    string filterExpression = "(SKILL = '" + Skill + "'" + ")";
                    DataRow[] rows = rptTable.Select(filterExpression);

                    foreach (DataRow row in rows)
                    {
                        if (row["AFTERCALLWORKTIME"] != null)
                        {
                            try
                            {
                                TimeSpan tTime = TimeSpan.Parse(row["AFTERCALLWORKTIME"].ToString());
                                totalTime += tTime;
                            }
                            catch (Exception ex)
                            {
                            }
                        }      
                    }
                    AvgWorkTime = Math.Round(totalTime.TotalSeconds/ContactHandled,2);
                }
            }
            return AvgWorkTime;
        }

        public static double GetAverageDelayTime(object Skill, int ContactHandled, DataTable rptTable)
        {
            double AvgDelayTime = 0;
            TimeSpan totalTime = new TimeSpan();
            //SPEED OF ANSWER
            //For Each SKILL ASA = (Sum SPEED OF ANSWER) / (NCH)
            //Five9 default domain setting for speed of answer will be applied

            if (ContactHandled > 0)
            {
                if (Skill != null)
                {

                    string filterExpression = "(SKILL = '" + Skill + "'" + ")";
                    DataRow[] rows = rptTable.Select(filterExpression);

                    foreach (DataRow row in rows)
                    {
                        if (row["SPEEDOFANSWER"] != null)
                        {
                            try
                            {
                                TimeSpan tTime = TimeSpan.Parse(row["SPEEDOFANSWER"].ToString());
                                totalTime += tTime;
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                    AvgDelayTime = Math.Round(totalTime.TotalSeconds / ContactHandled,2);
                }
            }
            return AvgDelayTime;
        }


        public static long GetPercentServiceLevel(object Skill, int NCO, DataTable rptTable)
        {
            long ServicePercentage = 0;
            int CallIDCount = 0;
            //CALL ID, SERVICE LEVEL
            //%SL = (((Count CALL ID where SERVICE LEVEL = 1)/(NCO)) * 100)
            //Five9 default domain setting for Service level will be applied
            
            if (Skill != null)
            {
                try
                {
                    string filterExpression = "((SERVICELEVEL = '1') AND (SKILL = '" + Skill + "'" + "))";
                    CallIDCount = (from DataRow dRow in rptTable.Select(filterExpression)
                                      select new { col1 = dRow["CALLID"] }).Distinct().Count();

                    ServicePercentage = ((CallIDCount*100)/NCO);
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetPercentServiceLevel", ex);
                }
            }
            return ServicePercentage;
        }


        public static double GetAveragePositionsStaffed(object Skill, DataTable skillTable, DataTable stateTable)
        {
            double AverageValue = 0;
            //AVAILABLE TIME (LOGIN LESS NOT READY
            //APS = AVAILABLE TIME (LOGIN LESS NOT READY)/ 1800secs

            TimeSpan agentLoginTime = new TimeSpan();
            TimeSpan agentNotReadyTime = new TimeSpan();

            if (Skill != null)
            {
                string filterExpression = "((SKILLLOGGEDIN = '" + Skill + "'" + "))";
                IEnumerable<DataRow> rows = skillTable.Select(filterExpression).Distinct();
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        if ((row["LOGINTIME"] != null)&& (!string.IsNullOrEmpty(row["LOGINTIME"].ToString())))
                        {
                            try
                            {
                                TimeSpan lTime = TimeSpan.Parse(row["LOGINTIME"].ToString());
                                agentLoginTime += lTime;
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Instance.Error("Utils.cs", "GetAveragePositionsStaffed", ex);
                            }
                        }
                    }
                }

                string expression = "((SKILL = '" + Skill + "'" + "))";
                IEnumerable<DataRow> dataRows = stateTable.Select(expression).Distinct();
                if (dataRows != null)
                {
                    foreach (var dataRow in dataRows)
                    {
                        if ((dataRow["NOTREADYTIME"] != null)&& (!string.IsNullOrEmpty(dataRow["NOTREADYTIME"].ToString())))
                        {
                            try
                            {
                                TimeSpan time = TimeSpan.Parse(dataRow["NOTREADYTIME"].ToString());
                                agentNotReadyTime += time;
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Instance.Error("Utils.cs", "GetAveragePositionsStaffed", ex);
                            }
                        }
                    }
                }
                try
                {
                    AverageValue=Math.Round((agentLoginTime.TotalSeconds-agentNotReadyTime.TotalSeconds)/1800,2);
                }
                catch (Exception ex)
                {
                }
            }
            return AverageValue;
        }

        public static int GetActualAbandons(object Skill, DataTable rptTable)
        {
            int ActualAbandons = 0;
            //CALL ID, DISPOSITION
            //ABD = (Count CALL ID where DISPOSITION = ‘Abandon’)

            if (Skill != null)
            {
                try
                {
                    string filterExpression = "((DISPOSITION = 'Abandon') AND (SKILL = '" + Skill + "'" + "))";
                    ActualAbandons = (from DataRow dRow in rptTable.Select(filterExpression)
                                   select new { col1 = dRow["CALLID"] }).Distinct().Count();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetActualAbandons", ex);
                }
            }
            return ActualAbandons;
        }
        #endregion

        #region "Agent Productivity Report"
        public static string GetAgentID(object IDstr)
        {
            string AgentID = " ";
            if (IDstr!=null)
            {
                if (7 >= IDstr.ToString().Length)
                {
                    AgentID = IDstr.ToString();
                }
                else
                {
                    AgentID = IDstr.ToString().Substring(Math.Max(0, IDstr.ToString().Length - 7));
                }
            }
            return AgentID;
        }

        public static string GetACDGroup(object GroupID)
        {
            string ACDGroup = " ";
            if (GroupID!=null)
            {
                if (GroupID.ToString().Length > 30)
                {
                    ACDGroup = GroupID.ToString().Substring(0, 29);
                }
                else
                {
                    ACDGroup = GroupID.ToString();
                }
            }
            return ACDGroup;
        }

        public static string GetSignInTime(object AgentID, object Skill, DataTable rptTable)
        {
            string LoginTime = "0";
            //TIMESTAMP
            //When State and Reason Code both are "Not Ready" then the timestamp will be the Login time

            if ((AgentID != null) && (Skill != null))
            {
                string filterExpression = "((STATE = 'Not Ready') AND (REASONCODE = 'Not Ready') AND (AGENTID = '" + AgentID + "'" + "))";
                IEnumerable<DataRow> rows = rptTable.Select(filterExpression).Distinct();
                foreach (var row in rows)
                {
                    if ((row["TIMESTAMP"] != null) && (!string.IsNullOrEmpty(row["TIMESTAMP"].ToString())))
                    {
                        try
                        {
                            DateTime lTime = System.Convert.ToDateTime(row["TIMESTAMP"]);
                            //TimeSpan lTime = TimeSpan.Parse(row["TIMESTAMP"].ToString());
                            LoginTime= lTime.ToString("HHmmss");
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Instance.Error("Utils.cs", "GetSignInTime", ex);
                        }
                    }
                }
            }
            return LoginTime;
        }

        public static string GetSignOutTime(object AgentID, object Skill, DataTable rptTable)
        {
            string LogoutTime = "0";
            //TIMESTAMP
            //When State "Logout" and ReasonCode "EndShift" then the timestamp will be the Login time

            if ((AgentID != null) && (Skill != null))
            {
                string filterExpression = "((STATE = 'Logout') AND( (REASONCODE = 'Logout') OR (REASONCODE = 'End Shift') )AND (AGENTID = '" + AgentID + "'" + "))";
                IEnumerable<DataRow> rows = rptTable.Select(filterExpression).Distinct();
                foreach (var row in rows)
                {
                    if ((row["TIMESTAMP"] != null) && (!string.IsNullOrEmpty(row["TIMESTAMP"].ToString())))
                    {
                        try
                        {
                            DateTime lTime = System.Convert.ToDateTime(row["TIMESTAMP"]);
                            //TimeSpan lTime = TimeSpan.Parse(row["TIMESTAMP"].ToString());
                            LogoutTime = lTime.ToString("HHmmss");
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Instance.Error("Utils.cs", "GetSignOutTime", ex);
                        }
                    }
                }
            }
            return LogoutTime;
        }
        
        public static int GetNCHCount(object AgentID, object Skill, DataTable rptTable)
        {
            int contactHandled = 0;
            //CALL ID, SKILL
            //NCH = For each SKIL (Count CALLID where CALL ID Is unique)

            if ((AgentID != null) && (Skill != null))
            {
                try
                {
                    string filterExpression = "((AGENTID = '" + AgentID + "'" + ")  AND (SKILL = '" + Skill + "'" + "))";
                    contactHandled = (from DataRow dRow in rptTable.Select(filterExpression)
                                      select new { col1 = dRow["CALLID"] }).Distinct().Count();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetNCHCount", ex);
                }
            }
            return contactHandled;
        }

        
        public static double GetAverageTalkTime(object AgentID, object Skill, DataTable rptTable, int NCH)
        {
            double AvgTalkTime = 0;
            TimeSpan totalTime = new TimeSpan();
            //TALK TIME LESS HOLD AND PARK
            //ATT = (Sum TALK TIME LESS HOLD AND PARK) / (NCH)

            if ((AgentID != null) && (Skill != null))
            {

                try
                {
                    string filterExpression = "((AGENTID = '" + AgentID + "'" + ")  AND (SKILL = '" + Skill + "'" + "))";
                    var rows = (from DataRow dRow in rptTable.Select(filterExpression)
                                select new { col1 = dRow["AGENTID"], col2 = dRow["SKILL"], col3 = dRow["TALKTIMELESSHOLDANDPARK"] }).Distinct();

                    foreach (var row in rows)
                    {
                        if (row.col3 != null)
                        {
                            try
                            {
                                TimeSpan lTime = TimeSpan.Parse(row.col3.ToString());
                                totalTime += lTime;
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Instance.Error("Utils.cs", "GetAverageTalkTime", ex);
                            }
                        }
                    }
                    AvgTalkTime = Math.Round(totalTime.TotalSeconds / NCH,2);
                }
                catch (Exception ex)
                {
                }
            }
            return AvgTalkTime;
        }

        public static double GetContactWorkTime(object AgentID, object Skill, DataTable rptTable, int NCH)
        {
            double AvgTalkTime = 0;
            TimeSpan totalTime = new TimeSpan();
            //AFTER CALL WORK TIME
            //AWT = (Sum AFTER CALL WORK TIME)/ (NCH)

            if ((AgentID != null) && (Skill != null))
            {
                try
                {
                    string filterExpression = "((AGENTID = '" + AgentID + "'" + ")  AND (SKILL = '" + Skill + "'" + "))";
                    var rows = (from DataRow dRow in rptTable.Select(filterExpression)
                                select new { col1 = dRow["AGENTID"], col2 = dRow["SKILL"], col3 = dRow["AFTERCALLWORKTIME"] }).Distinct();

                    foreach (var row in rows)
                    {
                        if (row.col3 != null)
                        {
                            try
                            {
                                TimeSpan lTime = TimeSpan.Parse(row.col3.ToString());
                                totalTime += lTime;
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Instance.Error("Utils.cs", "GetContactWorkTime", ex);
                            }
                        }
                    }
                    AvgTalkTime = Math.Round(totalTime.TotalSeconds / NCH, 2);
                }
                catch (Exception ex)
                {
                }
            }
            return AvgTalkTime;
        }
        
        public static int GetOutboundContactNumber(object AgentID, object Skill, DataTable rptTable)
        {
            int OutboundContactNumber = 0;
            //CALL ID, CALL TYPE
            //NOC = (Count CALL ID where CALL TYPE= ‘Manual’)

            if ((AgentID != null) && (Skill != null))
            {
                try
                {
                    string filterExpression = "((CALLTYPE= 'Manual') AND (AGENTID = '" + AgentID + "'" + "))";
                    OutboundContactNumber = (from DataRow dRow in rptTable.Select(filterExpression)
                                      select new { col1 = dRow["CALLID"] }).Distinct().Count();
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetOutboundContactNumber", ex);
                }
            }
            return OutboundContactNumber;
        }


        public static double GetAverageOutboundTalkTime(object AgentID, object Skill, DataTable rptTable, int NCH)
        {
            double AvgTalkTime = 0;
            //CALL TYPE, TALK TIME LESS HOLD & PARK
            //AOTT = Sum TALK TIME LESS HOLD AND PARK where CALL TYPE= ‘Manual’)/ (NCH)
            TimeSpan totalTime = new TimeSpan();

            if ((AgentID != null) && (Skill != null))
            {

                try
                {
                    string filterExpression = "((CALLTYPE= 'Manual') AND (AGENTID = '" + AgentID + "'" + "))";
                    var rows = (from DataRow dRow in rptTable.Select(filterExpression)
                                select new { col1 = dRow["AGENTID"], col2 = dRow["SKILL"], col3 = dRow["TALKTIMELESSHOLDANDPARK"] }).Distinct();

                    foreach (var row in rows)
                    {
                        if (row.col3 != null)
                        {
                            try
                            {
                                TimeSpan lTime = TimeSpan.Parse(row.col3.ToString());
                                totalTime += lTime;
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Instance.Error("Utils.cs", "GetAverageOutboundTalkTime", ex);
                            }
                        }
                    }
                    AvgTalkTime = Math.Round(totalTime.TotalSeconds / NCH, 2);
                }
                catch (Exception ex)
                {
                }
            }
            return AvgTalkTime;
        }

        public static double GetAverageOutboundWorkTime(object AgentID, object Skill, DataTable rptTable, int NCH)
        {
            double AvgWorkTime = 0;
            //AFTER CALL WORK TIME, CALL TYPE
            //AOWT = (Sum AFTER CALL WORK TIME where CALL TYPE= ‘Manual’)/ (NCH)

            TimeSpan totalTime = new TimeSpan();

            if ((AgentID != null) && (Skill != null))
            {
                try
                {
                    string filterExpression = "((CALLTYPE= 'Manual') AND (AGENTID = '" + AgentID + "'" + "))";
                    var rows = (from DataRow dRow in rptTable.Select(filterExpression)
                                select new { col1 = dRow["AGENTID"], col2 = dRow["SKILL"], col3 = dRow["AFTERCALLWORKTIME"] }).Distinct();

                    foreach (var row in rows)
                    {
                        if (row.col3 != null)
                        {
                            try
                            {
                                TimeSpan lTime = TimeSpan.Parse(row.col3.ToString());
                                totalTime += lTime;
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Instance.Error("Utils.cs", "GetAverageOutboundWorkTime", ex);
                            }
                        }
                    }
                    AvgWorkTime = Math.Round(totalTime.TotalSeconds / NCH, 2);
                }
                catch (Exception ex)
                {
                }
            }
            return AvgWorkTime;
        }

        public static string GetLoginTime(object Agent, object Skill, DataTable skillTable)
        {
            string LoginTime = string.Empty;

            TimeSpan totalLoginTime = new TimeSpan();
            if ((Agent != null) && (Skill != null))
            {
                string filterExpression = "((AGENT = '" + Agent + "'" + ")  AND (SKILLLOGGEDIN = '" + Skill + "'" + "))";
                var rows = (from DataRow dRow in skillTable.Select(filterExpression)
                            select new { col1 = dRow["SKILLLOGGEDIN"], col2 = dRow["AGENT"], col3 = dRow["LOGINTIME"] }).Distinct();

                foreach (var row in rows)
                {
                    if (row.col3 != null)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(row.col3.ToString()))
                            {
                                TimeSpan lTime = TimeSpan.Parse(row.col3.ToString());
                                totalLoginTime += lTime;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Instance.Error("Utils.cs", "GetLoginTime", ex);
                        }
                    }
                }
            }

            return totalLoginTime.ToString();
        }

        public static string GetNotReadyTime(object Agent, object Skill, DataTable rptTable)
        {
            string NotReadyTime = string.Empty;

            TimeSpan totalNotReadyTime = new TimeSpan();
            if ((Agent != null) && (Skill != null))
            {
                string expression = "((AGENT = '" + Agent + "'" + "))";
                var dRows = (from DataRow dRow in rptTable.Select(expression)
                             select new { col1 = dRow["SKILL"], col2 = dRow["AGENT"], col3 = dRow["NOTREADYTIME"] }).Distinct();

                foreach (var r in dRows)
                {
                    if (r.col3 != null)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(r.col3.ToString()))
                            {
                                TimeSpan lTime = TimeSpan.Parse(r.col3.ToString());
                                totalNotReadyTime += lTime;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Instance.Error("Utils.cs", "GetNotReadyTime", ex);
                        }
                    }
                }
            }

            return totalNotReadyTime.ToString();
        }

        public static string GetWaitTime(object Agent, object Skill, DataTable rptTable)
        {
            string NotReadyTime = string.Empty;
            TimeSpan totalWaitTime = new TimeSpan();

            if ((Agent != null) && (Skill != null))
            {
                string expression = "((AGENT = '" + Agent + "'" + "))";
                var dRows = (from DataRow dRow in rptTable.Select(expression)
                             select new { col1 = dRow["SKILL"], col2 = dRow["AGENT"], col3 = dRow["WAITTIME"] }).Distinct();

                foreach (var r in dRows)
                {
                    if (r.col3 != null)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(r.col3.ToString()))
                            {
                                TimeSpan lTime = TimeSpan.Parse(r.col3.ToString());
                                totalWaitTime += lTime;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Instance.Error("Utils.cs", "GetWaitTime", ex);
                        }
                    }
                }
            }

            return totalWaitTime.ToString();
        }


        public static double GetAvailableTime(object LoginTime, object NotReadyTime)
        {
            double AvailableTime = 0;
            //LOGIN TIME, NOT READY TIME
            //AVL = (Sum LOGIN TIME) - (Sum NOT READY TIME)

            TimeSpan totalLoginTime = new TimeSpan();
            TimeSpan totalNotReadyTime = new TimeSpan();

            try
            {
                if (LoginTime != null)
                {
                    totalLoginTime = TimeSpan.Parse(LoginTime.ToString());
                }

                if (NotReadyTime != null)
                {
                    totalNotReadyTime = TimeSpan.Parse(NotReadyTime.ToString());
                }

                AvailableTime = Math.Round((totalLoginTime.TotalSeconds - totalNotReadyTime.TotalSeconds));

            }
            catch (Exception ex)
            {
            }
            if (AvailableTime < 1)
            {
                AvailableTime = 0;
            }

            return AvailableTime;
        }

        public static double GetUnAvailableTime(object NotReadyTime)
        {
            double UnAvailableTime = 0;
            
            TimeSpan totalNotReadyTime = new TimeSpan();

            try
            {
                if (NotReadyTime != null)
                {
                    totalNotReadyTime = TimeSpan.Parse(NotReadyTime.ToString());
                }

                UnAvailableTime = totalNotReadyTime.TotalSeconds;

            }
            catch (Exception ex)
            {
            }
            if (UnAvailableTime < 1)
            {
                UnAvailableTime = 0;
            }

            return UnAvailableTime;
        }

        public static double GetPluggedInPercentage(object LoginTime, object WaitTime)
        {
            double PercentTime = 0;
            //LOGIN TIME, WAIT TIME
            //PIP = ((((Sum LOGIN TIME) - (Sum WAIT TIME)) / (Sum LOGIN TIME)) *100)

            TimeSpan totalLoginTime = new TimeSpan();
            TimeSpan totalWaitTime = new TimeSpan();

            if ((LoginTime != null)&& (WaitTime != null))
            {
                if (LoginTime != null)
                {
                    totalLoginTime = TimeSpan.Parse(LoginTime.ToString());
                }

                if (WaitTime != null)
                {
                    totalWaitTime = TimeSpan.Parse(WaitTime.ToString());
                }

                try
                {
                    PercentTime = (((totalLoginTime.TotalSeconds - totalWaitTime.TotalSeconds) * 100) / totalLoginTime.TotalSeconds);
                    PercentTime= Math.Round(PercentTime,2);
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("Utils.cs", "GetPluggedInPercentage", ex);
                }
            }
            
            return PercentTime;
        }
        #endregion
    }
}
