using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GoodMorningGenerator
{
    class Logger
    {
        #region Constants

        private const string LOG_FILE_NAME = "log.xml";
        private const string DATE_TIME_FORMAT = @"G";
        
        #endregion

        #region XML TAGS

        private const string LOG_ROOT_ELEM = "GMG_Log";
        private const string LOG_ENTRY_ELEM = "Log";
        private const string LOG_DATE_ATTR = "date";

        #endregion

        #region Private Members

        private static readonly Logger instance = new Logger();
        private readonly List<LogEntry> logs = new List<LogEntry>();

        #endregion

        #region Life Cycle

        private Logger()
        {
            Read();
        }

        #endregion

        #region Public Properties
        
        public static Logger Instance { get { return instance; } }

        #endregion
        
        #region Public Methods

        public void Log(string message)
        {
            logs.Add(new LogEntry(DateTime.Now, message));
            Save();
        }

        public void LogMailEntry(MailMessage mail)
        {
            logs.Add(new LogEntry(DateTime.Now, mail.Attachments.First().Name) { IsMailEntry = true });
            Save();
        }

        public bool MailEntryExists(string attachmentName)
        {
            var results = logs.Exists(entry => entry.IsMailEntry && entry.Value.Equals(attachmentName));
            return results;
        }

        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Configs the found.
        /// </summary>
        /// <returns></returns>
        private bool LogExists()
        {
            return File.Exists(LOG_FILE_NAME);
        }

        private void Read()
        {
            if (!LogExists())
            {
                return;
            }

            // Read the config file
            var log = XDocument.Load(LOG_FILE_NAME);
            var logEntries = from item in log.Descendants(LOG_ENTRY_ELEM)
                                select new
                                {
                                    date = (item.Attribute(LOG_DATE_ATTR) != null) ?
                                        item.Attribute(LOG_DATE_ATTR).Value : string.Empty,
                                    intValue = !string.IsNullOrEmpty(item.Value) ?
                                        item.Value : string.Empty,
                                };

            // Load the log list
            logs.Clear();
            foreach (var entry in logEntries)
            {
                DateTime date;
                var isValidDate = DateTime.TryParse(entry.date, out date);
                var maxDaysToLog = Convert.ToInt32(
                    Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_MAX_DAYS_TO_LOG));
                var lastDayToLog = DateTime.Now.Subtract(new TimeSpan(maxDaysToLog, 0, 0, 0));
                var logEntry = new LogEntry(isValidDate ? date : DateTime.Now, entry.intValue);
                
                // don't log out of date mail entries
                if (logEntry.IsMailEntry && logEntry.Date < lastDayToLog)
                    continue;

                logs.Add(new LogEntry(isValidDate ? date : DateTime.Now, entry.intValue));
            }
        }

        private void Save()
        {
            Write(logs);
        }

        private void Write(IEnumerable<LogEntry> entries)
        {
            var writerSettings = new XmlWriterSettings { Indent = true };

            var writer = XmlWriter.Create(LOG_FILE_NAME, writerSettings);
            writer.WriteStartDocument();
            writer.WriteComment("These are log entries for the Good Morning Generator.");
            writer.WriteStartElement(LOG_ROOT_ELEM);
            
            // Entries
            foreach (var entry in entries)
            {
                writer.WriteStartElement(LOG_ENTRY_ELEM);
                writer.WriteAttributeString(LOG_DATE_ATTR, entry.Date.ToString(DATE_TIME_FORMAT));
                writer.WriteValue(entry.InternalValue);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

        #endregion
    }

    public class LogEntry
    {
        private const string MAIL_ENTRY_PREFIX = "[MAILED_ITEM] ";
        private string value = string.Empty;
        
        public LogEntry(DateTime date, string value)
        {
            Date = date;
            IsMailEntry = value.StartsWith(MAIL_ENTRY_PREFIX);
            InternalValue = IsMailEntry ? value.Substring(MAIL_ENTRY_PREFIX.Length) : value;
        }

        public DateTime Date { get; set; }
        public bool IsMailEntry { get; set; }
        public string Value { get { return value; } set { this.value = value; } }
        public string InternalValue
        {
            get { return IsMailEntry ? MAIL_ENTRY_PREFIX + value : value; }
            set { this.value = value; }
        }
    }
}
