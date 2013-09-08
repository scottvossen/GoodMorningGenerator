using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Xml;
using System.Xml.Linq;

namespace GoodMorningGenerator
{
    /// <summary>
    /// Manages the GoodMorningGenerator configuration file. [Not thread safe]
    /// </summary>
    public class Configuration
    {
        #region Constants

        private const string CONFIG_FILE_NAME       = "config.xml";
        private const string DEFAULT_SENDER_USER    = "goodmorninghelper";
        private const string DEFAULT_SENDER_NAME    = "Good Morning Helper";
        private const string DEFAULT_SENDER_EMAIL   = Sensitive.DEFAULT_SENDER_EMAIL;
        private const string DEFAULT_SENDER_PSWD    = Sensitive.DEFAULT_SENDER_PSWD;
        private const string GENERIC_MESSAGE_PREFIX = "_GENERIC_MESSAGE_";
        private const int DEFAULT_MAX_DAYS_TO_LOG   = 100;

        internal const string INTERNAL_SETTINGS_SMTP_HOST       = "smtp_host";
        internal const string INTERNAL_SETTINGS_SUBJECT_TAG     = "subject_tag";
        internal const string INTERNAL_SETTINGS_SENDER_USER     = "sender_user";
        internal const string INTERNAL_SETTINGS_SENDER_NAME     = "sender_name";
        internal const string INTERNAL_SETTINGS_SENDER_EMAIL    = "sender_email";
        internal const string INTERNAL_SETTINGS_SENDER_PSWD     = "sender_pswd";
        internal const string INTERNAL_SETTINGS_MAX_DAYS_TO_LOG = "max_days_to_log";
        
        #endregion

        #region XML TAGS

        private const string CONFIG_ROOT_ELEM        = "GMG_Config";
        private const string SETTINGS_ELEM           = "Settings";
        private const string SETTING_ELEM            = "Setting";
        private const string SETTING_KEY_ATTR        = "key";
        private const string SETTING_VAL_ATTR        = "value";
        private const string ADDRESSES_ELEM          = "Addresses";
        private const string ADDRESS_ELEM            = "Address";
        private const string ADDRESS_NAME_ATTR       = "name";
        private const string ADDRESS_EMAIL_ATTR      = "email";
        private const string MESSAGES_ELEM           = "Messages";
        private const string MESSAGE_ELEM            = "Message";
        private const string MESSAGE_ASSOC_FILE_ATTR = "assocFile";

        #endregion

        #region Private Members

        private static readonly Configuration instance = new Configuration();
        private Dictionary<string, string> settings;
        private List<MailAddress> addresses;
        private Dictionary<string, string> messages;
        private readonly List<InternalSetting> internalSettings;

        #endregion

        #region Lifce Cycle

        private Configuration()
        {
            settings = new Dictionary<string, string>();
            addresses = new List<MailAddress>();
            messages = new Dictionary<string, string>();
            internalSettings = GetInternalSettings();
            
            if (!ConfigFound())
            {
                GenerateConfigFile();
            }

            ReadConfig();
        }

        #endregion

        #region Public Properties

        public static Configuration Instance { get { return instance; } }

        public List<MailAddress> Addresses
        {
            get { return addresses ?? (addresses = new List<MailAddress>()); }
        }

        public Dictionary<string, string> Messages { get { return messages; } }

        #endregion

        #region Public Methods

        public string GetSetting(string name)
        {
            string value;
            settings.TryGetValue(name, out value);
            return value;
        }

        public void SetSetting(string name, string value)
        {
            settings[name] = value;
        }

        public void SaveSettings()
        {
            WriteConfigFile(settings, addresses, messages);
        }

        public string[] GetGenericMessages()
        {
            return (from message in messages 
                    where message.Key.StartsWith(GENERIC_MESSAGE_PREFIX) 
                    select message.Value).ToArray();
        }
        
        #endregion

        #region Helper Methods
        
        private bool ConfigFound()
        {
            return File.Exists(CONFIG_FILE_NAME);
        }

        private void ReadConfig()
        {
            // Read the config file
            var config = XDocument.Load(CONFIG_FILE_NAME);
            var configSettings = from item in config.Descendants(SETTING_ELEM)
                                 select new
                                 {
                                     key = (item.Attribute(SETTING_KEY_ATTR) != null) ?
                                         item.Attribute(SETTING_KEY_ATTR).Value : string.Empty,
                                     value = (item.Attribute(SETTING_VAL_ATTR) != null) ?
                                         item.Attribute(SETTING_VAL_ATTR).Value : string.Empty,
                                 };
            var configAddresses = from item in config.Descendants(ADDRESS_ELEM)
                                  select new
                                  {
                                      name = (item.Attribute(ADDRESS_NAME_ATTR) != null) ?
                                              item.Attribute(ADDRESS_NAME_ATTR).Value : string.Empty,
                                      email = (item.Attribute(ADDRESS_EMAIL_ATTR) != null) ?
                                              item.Attribute(ADDRESS_EMAIL_ATTR).Value : string.Empty,
                                  };
            var configMessages = from item in config.Descendants(MESSAGE_ELEM)
                                 select new
                                 {
                                     assocFile = (item.Attribute(MESSAGE_ASSOC_FILE_ATTR) != null) ?
                                                 item.Attribute(MESSAGE_ASSOC_FILE_ATTR).Value : string.Empty,
                                     value = item.Value,
                                 };

            // Load the settings dictionary
            settings = new Dictionary<string, string>();
            var defaultInternalSettings = internalSettings.ToDictionary(
                                                            internalSetting => internalSetting.Key,
                                                            internalSetting => internalSetting.DefaultValue);
            foreach (var setting in configSettings)
            {
                // override invalid or missing internal settings with an appropriate internal default
                if (defaultInternalSettings.ContainsKey(setting.key) && string.IsNullOrEmpty(setting.value))
                {
                    settings.Add(setting.key, defaultInternalSettings[setting.key]);
                }
                else
                {
                    settings.Add(setting.key, setting.value);
                }
            }

            // Load the addresses list
            addresses = new List<MailAddress>();
            foreach (var address in configAddresses.Where(address => !string.IsNullOrEmpty(address.email) && !string.IsNullOrEmpty(address.name)))
            {
                addresses.Add(new MailAddress(address.email, address.name));
            }

            // Load the messages dictionary
            messages = new Dictionary<string, string>();
            var count = 1;
            foreach (var message in configMessages)
            {
                if (string.IsNullOrEmpty(message.value))
                    continue;

                var assocFile = string.IsNullOrEmpty(message.assocFile)
                                       ? GENERIC_MESSAGE_PREFIX + count++
                                       : message.assocFile;
                
                messages.Add(assocFile, message.value);
            }
        }
        
        private void GenerateConfigFile()
        {
            // Initial settings. Note that some settings may be hidden from the user (such as default sender password)
            var defaultSettings = internalSettings.ToDictionary(
                                                   setting => setting.Key, 
                                                   setting => setting.GetValueForConfigEntry());
            
            var defaultAddresses = new List<MailAddress>
                                       {
                                           new MailAddress(DEFAULT_SENDER_EMAIL,            DEFAULT_SENDER_NAME),
                                       };
            var defaultMessages = new Dictionary<string, string>
                                      {
                                          { "scott-vossen.jpg", Environment.NewLine +
                                                          "Good morning sweetie! I hope your day goes well... I'll talk to you later." + 
                                                          Environment.NewLine +
                                                          "With all my heart," + 
                                                          Environment.NewLine +
                                                          "Scottie" + 
                                                          Environment.NewLine }
                                      };

            WriteConfigFile(defaultSettings, defaultAddresses, defaultMessages);
        }

        private void WriteConfigFile(Dictionary<string, string> settings, IEnumerable<MailAddress> addresses, Dictionary<string, string> messages)
        {
            var writerSettings = new XmlWriterSettings { Indent = true };

            var writer = XmlWriter.Create(CONFIG_FILE_NAME, writerSettings);
            writer.WriteStartDocument();
            writer.WriteComment("These settings control the Good Morning Email Generator.");
            writer.WriteStartElement(CONFIG_ROOT_ELEM);

            // Settings
            writer.WriteStartElement(SETTINGS_ELEM);
            foreach (var setting in settings)
            {
                writer.WriteStartElement(SETTING_ELEM);
                writer.WriteAttributeString(SETTING_KEY_ATTR, setting.Key);

                var internalSetting = internalSettings.Find(internalStg => 
                    internalStg.Key.Equals(setting.Key, StringComparison.Ordinal));
                var value = setting.Value;

                if (internalSetting != null)
                {
                    // override missing internal settings or those which need to be hidden
                    var missingInternalSetting = string.IsNullOrEmpty(setting.Value);
                    var valueShouldBeHidden = internalSetting.HideDefaultFromUser &&
                                              value.Equals(internalSetting.DefaultValue, StringComparison.Ordinal);

                    if (missingInternalSetting || valueShouldBeHidden)
                        value = internalSetting.DefaultValue;
                }
                
                writer.WriteAttributeString(SETTING_VAL_ATTR, value);
                //writer.WriteAttributeString(SETTING_VAL_ATTR, 
                //                            defaultInternalSettings.ContainsKey(setting.Key)
                //                            ? defaultInternalSettings[setting.Key]
                //                            : setting.Value ?? string.Empty);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            // Addresses
            writer.WriteStartElement(ADDRESSES_ELEM);
            foreach (var address in addresses)
            {
                writer.WriteStartElement(ADDRESS_ELEM);
                writer.WriteAttributeString(ADDRESS_NAME_ATTR, address.DisplayName);
                writer.WriteAttributeString(ADDRESS_EMAIL_ATTR, address.Address);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            // Messages
            writer.WriteStartElement(MESSAGES_ELEM);
            foreach (var message in messages)
            {
                writer.WriteStartElement(MESSAGE_ELEM);
                writer.WriteAttributeString(MESSAGE_ASSOC_FILE_ATTR, message.Key);
                writer.WriteValue(message.Value);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

        private List<InternalSetting> GetInternalSettings()
        {
            return new List<InternalSetting>
                                   {
                                       new InternalSetting
                                           {
                                               Key = INTERNAL_SETTINGS_SMTP_HOST,
                                               DefaultValue = "smtp.gmail.com"
                                           },
                                       new InternalSetting
                                           {
                                               Key = INTERNAL_SETTINGS_SUBJECT_TAG, 
                                               DefaultValue = "1"
                                           },
                                       new InternalSetting
                                           {
                                               Key = INTERNAL_SETTINGS_SENDER_USER,
                                               DefaultValue = DEFAULT_SENDER_USER,
                                               HideDefaultFromUser = true
                                           },
                                       new InternalSetting
                                           {
                                               Key = INTERNAL_SETTINGS_SENDER_NAME,
                                               DefaultValue = DEFAULT_SENDER_NAME,
                                               HideDefaultFromUser = true
                                           },
                                       new InternalSetting
                                           {
                                               Key = INTERNAL_SETTINGS_SENDER_EMAIL,
                                               DefaultValue = DEFAULT_SENDER_EMAIL,
                                               HideDefaultFromUser = true
                                           },
                                       new InternalSetting
                                           {
                                               Key = INTERNAL_SETTINGS_SENDER_PSWD,
                                               DefaultValue = DEFAULT_SENDER_PSWD,
                                               HideDefaultFromUser = true
                                           },
                                       new InternalSetting
                                           {
                                               Key = INTERNAL_SETTINGS_MAX_DAYS_TO_LOG,
                                               DefaultValue = DEFAULT_MAX_DAYS_TO_LOG.ToString(),
                                           },
                                   };
        } 

        #endregion

        private class InternalSetting
        {
            public string Key { get; set; }
            public string DefaultValue { get; set; }
            public bool HideDefaultFromUser { get; set; }
            
            public string GetValueForConfigEntry()
            {
                return HideDefaultFromUser ? string.Empty : DefaultValue;
            }
        }
    }
}
