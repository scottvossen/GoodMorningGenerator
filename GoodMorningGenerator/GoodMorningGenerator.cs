using System;
using System.Globalization;
using System.Net.Mail;

namespace GoodMorningGenerator
{
    /// <summary>
    /// The Good Morning Generator generates good morning emails for my KB.
    /// </summary>
    internal class GoodMorningGenerator
    {
        static void Main(string[] args)
        {
            var mailMan = new MailService();
            var generator = new MailGenerator();
            var mail = generator.GenerateMail();

            if (mail != null && mailMan.SendMail(mail))
            {
                Logger.Instance.LogMailEntry(mail);

                // Increment the subject setting
                Configuration.Instance.SetSetting(Configuration.INTERNAL_SETTINGS_SUBJECT_TAG, 
                                                  GetNewSubjectSetting().ToString(CultureInfo.CurrentCulture));
                Configuration.Instance.SaveSettings();
            }
        }

        private static int GetNewSubjectSetting()
        {
            int emailNum;
            if (Int32.TryParse(Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SUBJECT_TAG), out emailNum))
            {
                return emailNum + 1;
            }
            return -1;
        }
    }
}
