// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System.Web.Mail;

namespace NMockSample.Order
{
	/// <summary>
	/// Notify people of events using SMTP emails.
	/// </summary>
	public class Notifier
	{
		private string from = "nobody";
		private string admin = "admin";

		public virtual void NotifyAdmin(string msg)
		{
			MailMessage mail = new MailMessage();
			mail.From = from;
			mail.To = admin;
			mail.Subject = msg;
			mail.Body = msg;
			SmtpMail.Send(mail);
		}

		public virtual void NotifyUser(string user, string msg)
		{
			MailMessage mail = new MailMessage();
			mail.From = from;
			mail.To = user;
			mail.Subject = msg;
			mail.Body = msg;
			SmtpMail.Send(mail);
		}
	}
}
