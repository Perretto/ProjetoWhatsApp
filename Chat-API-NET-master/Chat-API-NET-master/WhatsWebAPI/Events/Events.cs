using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using WhatsAppApi;
using WhatsAppApi.Account;
using WhatsAppApi.Helper;
using WhatsAppApi.Register;
using WhatsAppApi.Response;

namespace WhatsWebAPI.Events
{
    public class Events
    {
        public static void Instance_OnPrintDebug(object value)
        {
            Console.WriteLine(value);
        }

        public static void wa_OnGetPrivacySettings(Dictionary<ApiBase.VisibilityCategory, ApiBase.VisibilitySetting> settings)
        {
            throw new NotImplementedException();
        }

        public static void wa_OnGetStatus(string from, string type, string name, string status)
        {
            Console.WriteLine(String.Format("Got status from {0}: {1}", from, status));
        }

        public static string getDatFileName(string pn)
        {
            string filename = string.Format("{0}.next.dat", pn);
            return Path.Combine(Directory.GetCurrentDirectory(), filename);
        }

        public static void wa_OnGetSyncResult(int index, string sid, Dictionary<string, string> existingUsers, string[] failedNumbers)
        {
            Console.WriteLine("Sync result for {0}:", sid);
            foreach (KeyValuePair<string, string> item in existingUsers)
            {
                Console.WriteLine("Existing: {0} (username {1})", item.Key, item.Value);
            }
            foreach (string item in failedNumbers)
            {
                Console.WriteLine("Non-Existing: {0}", item);
            }
        }

        public static void wa_OnGetGroups(WaGroupInfo[] groups)
        {
            Console.WriteLine("Got groups:");
            foreach (WaGroupInfo info in groups)
            {
                Console.WriteLine("\t{0} {1}", info.subject, info.id);
            }
        }

        public static void wa_OnGetPhotoPreview(string from, string id, byte[] data)
        {
            Console.WriteLine("Got preview photo for {0}", from);
            File.WriteAllBytes(string.Format("preview_{0}.jpg", from), data);
        }

        public static void wa_OnGetPhoto(string from, string id, byte[] data)
        {
            Console.WriteLine("Got full photo for {0}", from);
            File.WriteAllBytes(string.Format("{0}.jpg", from), data);
        }

        public static void wa_OnGetMessageVcard(ProtocolTreeNode vcardNode, string from, string id, string name, byte[] data)
        {
            Console.WriteLine("Got vcard \"{0}\" from {1}", name, from);
            File.WriteAllBytes(string.Format("{0}.vcf", name), data);
        }

        public static void wa_OnGetMessageLocation(ProtocolTreeNode locationNode, string from, string id, double lon, double lat, string url, string name, byte[] preview, string userName)
        {
            Console.WriteLine("Got location from {0} username {1} ({1}, {2})", from, userName, lat, lon);
            if (!string.IsNullOrEmpty(name))
            {
                Console.WriteLine("\t{0}", name);
            }
            File.WriteAllBytes(string.Format("{0}{1}.jpg", lat, lon), preview);
        }

        public static void wa_OnGetMessageVideo(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got video from {0} name{1}", from, name, fileName);
            OnGetMedia(fileName, url, preview);
        }

        public static void OnGetMedia(string file, string url, byte[] data)
        {
            //save preview
            File.WriteAllBytes(string.Format("preview_{0}.jpg", file), data);
            //download
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileAsync(new Uri(url), file, null);
            }
        }

        public static void wa_OnGetMessageAudio(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got audio from {0} name{1}", from, name, fileName);
            OnGetMedia(fileName, url, preview);
        }

        public static void wa_OnGetMessageImage(ProtocolTreeNode mediaNode, string from, string id, string fileName, int size, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got image from {0} name{1}", from, name, fileName);
            OnGetMedia(fileName, url, preview);
        }

        public static void wa_OnGetPaused(string from)
        {
            Console.WriteLine("{0} stopped typing", from);
        }

        public static void wa_OnGetTyping(string from)
        {
            Console.WriteLine("{0} is typing...", from);
        }

        public static void wa_OnGetLastSeen(string from, DateTime lastSeen)
        {
            Console.WriteLine("{0} last seen on {1}", from, lastSeen.ToString());
        }

        public static void wa_OnGetMessageReadedClient(string from, string id)
        {
            Console.WriteLine("Message {0} to {1} readed by client", id, from);
        }

        public static void wa_OnGetMessageReceivedServer(string from, string id)
        {
            Console.WriteLine("Message {0} to {1} received by server", id, from);
        }

        public static void wa_OnGetMessageReceivedClient(string from, string id)
        {
            Console.WriteLine("Message {0} to {1} received by client", id, from);
        }

        public static void wa_OnGetGroupParticipants(string gjid, string[] jids)
        {
            Console.WriteLine("Got participants from {0}:", gjid);
            foreach (string jid in jids)
            {
                Console.WriteLine("\t{0}", jid);
            }
        }

        public static void wa_OnGetPresence(string from, string type)
        {
            //Console.WriteLine("Presence from {0}: {1}", from, type);
        }

        public static void wa_OnNotificationPicture(string type, string jid, string id)
        {
            //TODO
            //throw new NotImplementedException();
        }

        public static void wa_OnGetMessage(ProtocolTreeNode node, string from, string id, string name, string message, bool receipt_sent)
        {


            Console.WriteLine("Message from {0} {1}: {2}", name, from, message);
        }

        public static void wa_OnLoginFailed(string data)
        {
            Console.WriteLine("Login failed. Reason: {0}", data);
        }

        public static void wa_OnLoginSuccess(string phoneNumber, byte[] data)
        {
            //Console.WriteLine("Login success. Next password:");
            string sdata = Convert.ToBase64String(data);
            //Console.WriteLine(sdata);
            try
            {
                File.WriteAllText(getDatFileName(phoneNumber), sdata);
            }
            catch (Exception) { }
        }


        public static void ProcessChat(WhatsApp wa, string dst)
        {
            var thRecv = new Thread(t =>
            {
                try
                {
                    while (wa != null)
                    {
                        wa.PollMessages();
                        Thread.Sleep(100);
                        continue;
                    }

                }
                catch (ThreadAbortException)
                {
                }
            }) { IsBackground = true };
            thRecv.Start();

            WhatsUserManager usrMan = new WhatsUserManager();
            var tmpUser = usrMan.CreateUser(dst, "User");

            while (true)
            {
                string line = Console.ReadLine();
                if (line == null && line.Length == 0)
                    continue;

                string command = line.Trim();
                switch (command)
                {
                    case "/query":
                        //var dst = dst//trim(strstr($line, ' ', FALSE));
                        Console.WriteLine("[] Interactive conversation with {0}:", tmpUser);
                        break;
                    case "/accountinfo":
                        Console.WriteLine("[] Account Info: {0}", wa.GetAccountInfo().ToString());
                        break;
                    case "/lastseen":
                        Console.WriteLine("[] Request last seen {0}", tmpUser);
                        wa.SendQueryLastOnline(tmpUser.GetFullJid());
                        break;
                    case "/exit":
                        wa = null;
                        thRecv.Abort();
                        return;
                    case "/start":
                        wa.SendComposing(tmpUser.GetFullJid());
                        break;
                    case "/pause":
                        wa.SendPaused(tmpUser.GetFullJid());
                        break;
                    default:
                        Console.WriteLine("[] Send message to {0}: {1}", tmpUser, line);
                        wa.SendMessage(tmpUser.GetFullJid(), line);
                        break;
                }
            }
        }
    }
}