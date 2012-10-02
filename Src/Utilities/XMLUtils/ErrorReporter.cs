using System;
using System.Web.Mail;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for ErrorReporter.
	/// </summary>
	public class ErrorReporter
	{
		public ErrorReporter()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public void SendEmail()
		{
			MailMessage myMail = new MailMessage();
			myMail.From = "j.hatton@sil.org.pg";
			myMail.To = "hatton@ebible.org";
			myMail.Subject = "UtilMailMessage001";
			myMail.Priority = MailPriority.Normal;
			myMail.BodyFormat = MailFormat.Html;
			myMail.Body = "<html><body>UtilMailMessage001 - success</body></html>";
			SmtpMail.Send(myMail);
		}
	}
}
