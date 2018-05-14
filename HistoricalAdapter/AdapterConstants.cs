using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoricalAdapter
{
    public class AdapterConstants
    {

        public AdapterConstants()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        /// HISTORICAL ADAPTER CONSTANTS
        /// </summary>
        public const string HISTORICAL_REPORT_FOLDER = "Historical Reports";
        public const string HISTORICAL_AGENT_CUSTOM_CALL_REPORT = "Agent Custom Call Report";
        public const string HISTORICAL_AGENT_LOGIN_LOGOUT = "Agent Login-Logout";
        public const string HISTORICAL_AGENT_OCCUPANCY_BY_GROUP = "Agent Occupancy by Group";
        public const string HISTORICAL_AGENT_PRODUCTIVITY_BY_CALL_TYPE = "Agent Productivity by Call Type";
        public const string HISTORICAL_AGENT_SKILL_LOGIN_TIME1 = "Agent Skill Login Time1";
        public const string HISTORICAL_AGENT_NOT_READY_TIME = "Agent-Not-Ready-Time";
        public const string HISTORICAL_CUSTOM_CALL_SEGMENT_REPORT = "Call SegmentReportCustom";
        public const string HISTORICAL_CUSTOM_AGENT_OCCUPANCY = "Custom Agent Occupancy";
        public const string HISTORICAL_CUSTOM_ACD_AGENT_GROUP = "Custom-ACD-Agent-Group-Report";
        public const string HISTORICAL_CUSTOM_AGENT_SKILL_LOGIN_TIME = "Custom-Agent-Skill-Login-Time";
        public const string HISTORICAL_CUSTOM_AGENT_STATE_REPORT = "Custom-Agent-State-Report";
        public const string HISTORICAL_CUSTOM_CALL_LOG = "Custom-Call-Log";

        /// <summary>
        /// HISTORICAL ADAPTER OUTPUT FILE NAMES
        /// </summary>
        public const string FILE_HISTORICAL_TCS_DATA= "Historical TCS Data";
        public const string FILE_HISTORICAL_AGENT_PRDUCTIVITY = "Historical Agent Productivity";
    }
}
