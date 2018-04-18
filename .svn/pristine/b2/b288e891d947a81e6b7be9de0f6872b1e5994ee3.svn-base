/*
    Copyright (C) 2018 RAM Mutual Insurance Company

    This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
    FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License along with this program. If not, see http://www.gnu.org/licenses/.  
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAMPdfFileService
{
    public static class EventLogManager
    {
        private static readonly EventLog RAMPdfFileServiceLog = new EventLog("RAMPdfFileServiceLog", Environment.MachineName, "RAMPdfFileServiceSource");

        public static void WriteInformation(string message)
        {
            RAMPdfFileServiceLog.WriteEntry(message, EventLogEntryType.Information, 1);
        }

        public static void WriteWarning(string message)
        {
            RAMPdfFileServiceLog.WriteEntry(message, EventLogEntryType.Warning, 2);
        }

        public static void WriteError(string message)
        {
            RAMPdfFileServiceLog.WriteEntry(message, EventLogEntryType.Error, 3);
        }

        public static void WriteError(Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append("Message : " + ex.Message + Environment.NewLine);
            sb.Append("Source :" + ex.Source + Environment.NewLine);
            sb.Append("Stack Trace :" + ex.StackTrace + Environment.NewLine);

            if (ex.InnerException != null)
                sb.Append("Inner Exception :" + ex.InnerException.Message + Environment.NewLine);

            WriteError(sb.ToString());
        }
    }
}
