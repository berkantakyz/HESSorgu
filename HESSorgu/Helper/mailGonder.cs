using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;

namespace HESSorgu.Helper
{
    public class mailGonder
    {
        private static string strSmtp = "yourMailSmtp";
        private static string strSender = "mailSender";
        private static string strDisplay = "HES KODU SORGULAMA SONUCLARI";

        public bool newMail(string subject, string body, List<Mails> mailList, string file_path)
        {
            if (mailList.Count == 0) return false;
            try
            {
                SmtpClient smtp = new SmtpClient(strSmtp);

                MailMessage msg = new MailMessage();

                #region Attachment

                Attachment data = new Attachment(file_path, MediaTypeNames.Application.Octet);

                ContentDisposition disposition = data.ContentDisposition;
                disposition.CreationDate = System.IO.File.GetCreationTime(file_path);
                disposition.ModificationDate = System.IO.File.GetLastWriteTime(file_path);
                disposition.ReadDate = System.IO.File.GetLastAccessTime(file_path);

                msg.Attachments.Add(data);

                #endregion

                msg.Sender = new MailAddress(strSender);

                msg.From = new MailAddress(strSender, strDisplay, System.Text.Encoding.Unicode);

                foreach (var item in mailList)
                {
                    if (item.type.Trim() == "to")
                    {
                        msg.To.Add(item.mail);
                    }

                    else if (item.type.Trim() == "cc")
                    {
                        msg.CC.Add(item.type);
                    }

                    else if (item.type.Trim() == "bcc")
                    {
                        msg.Bcc.Add(item.mail);
                    }
                }

                msg.Subject = subject;
                msg.Body += body;

                smtp.Send(msg);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}