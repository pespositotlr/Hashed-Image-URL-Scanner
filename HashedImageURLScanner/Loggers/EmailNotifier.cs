using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;

namespace HashedImageURLScanner.Loggers
{
    public class EmailNotifier
    {
        private bool _isSendToEmailAddress;
        private MailAddress _fromAddress;
        private MailAddress _toAddress;
        private SmtpClient _smtpClient;

        public EmailNotifier(IConfigurationRoot config)
        {
            _isSendToEmailAddress = Convert.ToBoolean(config["isSendToEmailAddress"]);

            if (!_isSendToEmailAddress)
                return;

            _fromAddress = new MailAddress(config["emailAddress"]);
            _toAddress = new MailAddress(config["emailAddress"]);
            _smtpClient = new SmtpClient(config["smtpClient"]);
            _smtpClient.UseDefaultCredentials = false;
            _smtpClient.EnableSsl = true;
            _smtpClient.Credentials = new NetworkCredential(config["emailAddress"], config["emailAddressPassword"]);
            _smtpClient.Port = Convert.ToInt32(config["smptPort"]);
            _smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
        }

        public void SendNotificationEmailError(string subjectArea, string errorMessage)
        {
            if (!_isSendToEmailAddress)
                return;

            string subject = "Error message in Website Status Checker " + subjectArea;
            string body = "Error message in Website Status Checker " + subjectArea + ": " + errorMessage + " The current time is: " + DateTime.Now.ToString();
            SendNotificationEmail(subject, body);
        }

        public void SendNotificationEmailBookFound(string bookId, string currentEpisodeId)
        {
            if (!_isSendToEmailAddress)
                return;

            string subject = "Successfully found " + currentEpisodeId;
            string body = "Successfully found episode " + currentEpisodeId + " of bookId " + bookId + ". It's currently being downloaded. The current time is: " + DateTime.Now.ToString();
            SendNotificationEmail(subject, body);
        }
        public void SendNotificationUrlFound(string url)
        {
            if (!_isSendToEmailAddress)
                return;

            string subject = "Successfully found requested url";
            string body = "Successfully found the url: " + url + " -- It's currently being downloaded. The current time is: " + DateTime.Now.ToString();
            SendNotificationEmail(subject, body);
        }

        private void SendNotificationEmail(string subject, string body)
        {
            MailMessage msgMail = new MailMessage();

            msgMail.From = _fromAddress;
            msgMail.To.Add(_toAddress);
            msgMail.Subject = subject;
            msgMail.Body = body;
            msgMail.IsBodyHtml = false;

            try
            {
                _smtpClient.Send(msgMail);
            }
            catch
            {
                throw;
            }

            msgMail.Dispose();
        }
    }
}
