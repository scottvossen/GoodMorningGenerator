using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;

namespace GoodMorningGenerator
{
    public class MailGenerator
    {
        public MailMessage GenerateMail()
        {
            var subject = "Good Morning " + Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SUBJECT_TAG);
            var attachment = GetRandomAttachment();
            var body = GetMessage(attachment);

            if (attachment == null)
                return null;

            var mailMan = new MailService();
            var mail = mailMan.CreateMail(subject, body, attachment);
            return mail;
        }

        /// <summary>
        /// Gets the random attachment.
        /// </summary>
        /// <returns>a random attachment with priority given to those which haven't been used.</returns>
        private Attachment GetRandomAttachment()
        {
            Attachment attach = null;
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "Images");

            if (files.Length > 0)
            {
                files.Shuffle();
                
                // Use the first attachment that hasn't been used or default to the first one in the random list.
                var file = files.FirstOrDefault(f => !Logger.Instance.MailEntryExists(f)) ?? files.FirstOrDefault();
                if (file != null)
                {
                    attach = new Attachment(file);
                }
            }

            return attach;
        }

        private string GetMessage(Attachment attachment)
        {
            if (attachment == null)
                return string.Empty;

            if (Configuration.Instance.Messages.ContainsKey(attachment.Name))
            {
                // use the message associated with this file
                return Configuration.Instance.Messages[attachment.Name];
            }

            // Select a random generic message
            var genericMessages = Configuration.Instance.GetGenericMessages();
            
            if (genericMessages == null || genericMessages.Length <= 0)
            {
                genericMessages = new [] { "LOVE!" };
            }

            return genericMessages[(new Random()).Next(genericMessages.Length)];
        }
    }
}
