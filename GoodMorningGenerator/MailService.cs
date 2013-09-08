using System;
using System.Net;
using System.Net.Mail;

namespace GoodMorningGenerator
{
    public class MailService
    {
        public MailMessage CreateMail(string subject, string body, Attachment attachment = null)
        {
            var mail = new MailMessage();
            mail.Subject = subject;
            mail.Body = body;
            mail.From = new MailAddress(Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SENDER_EMAIL),
                                        Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SENDER_NAME));

            foreach (var address in Configuration.Instance.Addresses)
            {
                mail.To.Add(address);
            }

            if (attachment != null)
            {
                mail.Attachments.Add(attachment);
            }

            return mail;
        }

        public bool SendMail(MailMessage message)
        {
            try
            {
                // Documentation:
                // http://stackoverflow.com/questions/32260/sending-email-in-net-through-gmail
                // http://coding-issues.blogspot.in/2012/11/sending-email-with-attachments-from-c.html

                var smtpServer = new SmtpClient
                                     {
                                         Host = Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SMTP_HOST),
                                         Port = 587,
                                         EnableSsl = true,
                                         //DeliveryMethod = SmtpDeliveryMethod.Network,
                                         //UseDefaultCredentials = false,
                                         Credentials = new NetworkCredential(
                                             Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SENDER_EMAIL),
                                             Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SENDER_PSWD))
                                     };
                smtpServer.Send(message);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
