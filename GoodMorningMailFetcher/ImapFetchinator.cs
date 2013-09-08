using System;
using System.Collections;
using Joshi.Utils.Imap;

namespace GoodMorningMailFetcher
{
	public class ImapFetchinator
	{
        public static ArrayList FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
		{
			var imap = new Imap();
			imap.Login(hostname, Convert.ToUInt16(port), username, password, useSsl);
			imap.SelectFolder("INBOX");

            var messages = new ArrayList();
            var criteria = new string[] { "Good Morning" };
            imap.ExamineFolder("INBOX");
            imap.SearchMessage(criteria, false, messages);

            imap.LogOut();
            return messages;
		}

		/* Mail.dll -> works awesome, but it's proprietary */
		/*
		public static List<IMail> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
		{
			using (var imap = new Imap())
			{
				if (useSsl)
				{
					imap.ConnectSSL(hostname, port);
				}
				else
				{
					imap.Connect(hostname);
				}

				imap.Login(username, password);
				imap.SelectInbox();
				
				var uids = imap.Search(Flag.All);
				var mailMan = new MailBuilder();
				var messages = new List<IMail>();

				foreach (var uid in uids)
				{
					var eml = imap.GetMessageByUID(uid);
					messages.Add(mailMan.CreateFromEml(eml));
				}

				imap.Close();
				return messages;
			}
		}

		public static void GetAttachments(List<IMail> messages)
		{
			foreach (var message in messages)
			{
				foreach (var mime in message.Attachments)
				{
					const string attachDir = @"C:\attachments\";
					Directory.CreateDirectory(attachDir);
					mime.Save(attachDir + mime.SafeFileName);
				}
			}
		}
		*/
	}
}
