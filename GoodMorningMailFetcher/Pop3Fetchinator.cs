using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenPop.Mime;
using OpenPop.Pop3;

namespace GoodMorningMailFetcher
{
    public class Pop3Fetchinator
    {
        /// <summary>
        /// Fetch all messages from a POP3 server.
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <returns>All Messages on the POP3 server</returns>
        public static List<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server
                client.Authenticate(username, password);
                client.Reset();

                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();

                // We want to download all messages
                List<Message> allMessages = new List<Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                }

                client.Reset();

                // Now return the fetched messages
                return allMessages;
            }
        }

        /// <summary>
        /// Delete a specific message from a server
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <param name="messageNumber">
        /// The number of the message to delete.
        /// Must be in range [1, messageCount] where messageCount is the number of messages on the server.
        /// </param>
        public static void DeleteMessageOnServer(string hostname, int port, bool useSsl, string username, string password, int messageNumber)
        {
            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server
                client.Authenticate(username, password);

                // Mark the message as deleted
                // Notice that it is only MARKED as deleted
                // POP3 requires you to "commit" the changes
                // which is done by sending a QUIT command to the server
                // You can also reset all marked messages, by sending a RSET command.
                client.DeleteMessage(messageNumber);

                // When a QUIT command is sent to the server, the connection between them are closed.
                // When the client is disposed, the QUIT command will be sent to the server
                // just as if you had called the Disconnect method yourself.
            }
        }
        
        // EXAMPLES
        /// <summary>
        /// Example showing:
        ///  - how to a find plain text version in a Message
        ///  - how to save MessageParts to file
        /// </summary>
        /// <param name="message">The message to examine for plain text</param>
        public static void FindPlainTextInMessage(Message message)
        {
            MessagePart plainText = message.FindFirstPlainTextVersion();
            if (plainText != null)
            {
                // Save the plain text to a file, database or anything you like
                plainText.Save(new FileInfo("plainText.txt"));
            }
        }

        /// <summary>
        /// Example showing:
        ///  - how to find a html version in a Message
        ///  - how to save MessageParts to file
        /// </summary>
        /// <param name="message">The message to examine for html</param>
        public static void FindHtmlInMessage(Message message)
        {
            MessagePart html = message.FindFirstHtmlVersion();
            if (html != null)
            {
                // Save the plain text to a file, database or anything you like
                html.Save(new FileInfo("html.txt"));
            }
        }

        /// <summary>
        /// Example showing:
        ///  - how to find a MessagePart with a specified MediaType
        ///  - how to get the body of a MessagePart as a string
        /// </summary>
        /// <param name="message">The message to examine for xml</param>
        public static void FindXmlInMessage(Message message)
        {
            MessagePart xml = message.FindFirstMessagePartWithMediaType("text/xml");
            if (xml != null)
            {
                // Get out the XML string from the email
                string xmlString = xml.GetBodyAsText();

                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

                // Load in the XML read from the email
                doc.LoadXml(xmlString);

                // Save the xml to the filesystem
                doc.Save("test.xml");
            }
        }
    }
}
