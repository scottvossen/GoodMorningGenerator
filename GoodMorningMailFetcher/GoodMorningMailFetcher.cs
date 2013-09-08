using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GoodMorningGenerator;

namespace GoodMorningMailFetcher
{
    /// <summary>
    /// The Good Morning Mail Fetcher gets existing Good Morning emails and enters them into the 
    /// config file to be used in conjunction with the Good Morning Generator.
    /// </summary>
    public class GoodMorningMailFetcher
    {
        internal const string GMAIL_POP3_HOST = "pop.gmail.com";
        internal const int GMAIL_POP3_PORT = 995;

        internal const string GMAIL_IMAP_HOST = "imap.gmail.com";
        internal const int GMAIL_IMAP_PORT = 993;

        static void Main(string[] args)
        {
            var user = Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SENDER_USER);
            var pswd = Configuration.Instance.GetSetting(Configuration.INTERNAL_SETTINGS_SENDER_PSWD);

            var pop3Messages = Pop3Fetchinator.FetchAllMessages(GMAIL_POP3_HOST, GMAIL_POP3_PORT, true, user, pswd);
            Console.WriteLine("Pop3 Found {0} files!", pop3Messages.Count);

            var imapMessages = ImapFetchinator.FetchAllMessages(GMAIL_IMAP_HOST, GMAIL_IMAP_PORT, true, user, pswd);
            Console.WriteLine("IMAP Found {0} files!", imapMessages.Count);

            Console.WriteLine("Enter to exit");
            Console.Read();
        }
    }
}
