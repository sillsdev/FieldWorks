using System;
using System.Security;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices; // DllImport
using System.Security.Principal; // WindowsImpersonationContext
using System.Windows.Forms;

namespace BackupScheduler
{
	public class UserAccount
	{
		private string FullUserName;
		private string Name;
		private string Domain;
		private bool fInitialized;

		public string AccountName
		{
			get { return FullUserName; }
		}

		public string AccountNameAlt
		{
			get { return Domain + "." + Name; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public UserAccount()
		{
			fInitialized = false;
			Name = null;
			Domain = null;
			FullUserName = null;

			// Get user's logon domain\name string:
			FullUserName = WindowsIdentity.GetCurrent().Name.ToString();

			// Split into separate domain and user name strings:
			char[] Separator = { '\\' };
			string[] UserNameElements = FullUserName.Split(Separator);

			// Sanity check:
			if (UserNameElements.Length > 2)
				throw new SystemException("User account has more than 2 elements.");

			// Identify domain name (if present) and user name:
			if (UserNameElements.Length == 2)
			{
				Domain = UserNameElements[0];
				Name = UserNameElements[1];
			}
			else
				Name = UserNameElements[0];

			fInitialized = true;
		}

		/// <summary>
		/// Test if user has blank/no password.
		/// </summary>
		public bool IsUserPasswordBlank()
		{
			if (!fInitialized)
				throw new SystemException("Uninitialized UserAccount class");

			// To test for user having no password, we try to log on as the user
			// using a blank password. The log on won'task work, but GetLastError()
			// returns a special value if the blank password is correct.

			// NOTE: On systems connected to a network domain, this can result
			// in a user's account being locked for a period, if they run this
			// part of the utility too many times, because each attempt is regarded
			// as a failed logon attempt. However, there appears to be no other way
			// to test for blank passowrds.

			// Attempt to log on as user using blank password:
			IntPtr _userHandle = new IntPtr(0);
			bool fLoggedOn = LogonUser(Name, Domain, (IntPtr)0, LOGON32_LOGON_INTERACTIVE,
				LOGON32_PROVIDER_DEFAULT, ref _userHandle);

			if (!fLoggedOn)
			{
				uint Error = GetLastError();
				if (Error == ERROR_ACCOUNT_RESTRICTION)
					return true;

				return false;
			}
			// Should not get here, but if we do, then we successfully logged on with a blank
			// password, so hand back the logon token
			CloseHandle(_userHandle);
			return true;
		}

		/// <summary>
		/// Test given password to see if it works for the user.
		/// </summary>
		public bool IsPasswordCorrect(SecureString Password)
		{
			if (!fInitialized)
				throw new SystemException("Uninitialized UserAccount class");

			// To test a password, we try to log on as the user using that password.
			// If the logon is successful, then the password is correct.
			IntPtr _userHandle = new IntPtr(0);
			IntPtr pwd = Marshal.SecureStringToCoTaskMemUnicode(Password);
			bool fLoggedOn = LogonUser(Name, Domain, pwd, LOGON32_LOGON_INTERACTIVE,
				LOGON32_PROVIDER_DEFAULT, ref _userHandle);
			uint error = GetLastError();
			Marshal.ZeroFreeCoTaskMemUnicode(pwd);

			if (!fLoggedOn)
			{
				switch (error)
				{
					case ERROR_LOGON_FAILURE:
						MessageBox.Show(Properties.Resources.ErrorInvalidPassword,
							Properties.Resources.ErrorMsgTitle, MessageBoxButtons.OK,
							MessageBoxIcon.Stop);
						break;
					case ERROR_PASSWORD_MUST_CHANGE:
						MessageBox.Show(Properties.Resources.ERROR_PASSWORD_MUST_CHANGE +
							Properties.Resources.ErrorValidatingPassword,
							Properties.Resources.ErrorMsgTitle, MessageBoxButtons.OK,
							MessageBoxIcon.Stop);
						throw new System.Exception("Password error");
					case ERROR_DOMAIN_CONTROLLER_NOT_FOUND:
						MessageBox.Show(Properties.Resources.ERROR_DOMAIN_CONTROLLER_NOT_FOUND +
							Properties.Resources.ErrorValidatingPassword,
							Properties.Resources.ErrorMsgTitle, MessageBoxButtons.OK,
							MessageBoxIcon.Stop);
						throw new System.Exception("Password error");
					case ERROR_ACCOUNT_LOCKED_OUT:
						MessageBox.Show(Properties.Resources.ERROR_ACCOUNT_LOCKED_OUT +
							Properties.Resources.ErrorValidatingPassword,
							Properties.Resources.ErrorMsgTitle, MessageBoxButtons.OK,
							MessageBoxIcon.Stop);
						throw new System.Exception("Password error");
					default:
						MessageBox.Show(
							string.Format(Properties.Resources.ErrorUnknownPasswordProblem,
							error),
							Properties.Resources.ErrorMsgTitle, MessageBoxButtons.OK,
							MessageBoxIcon.Stop);
						break;
				}
				return false; // Password incorrect
			}

			// We successfully logged on, so hand back the logon token:
			CloseHandle(_userHandle);
			return true;
		}

		#region Interop imports/constants
		public const int LOGON32_LOGON_INTERACTIVE = 2;
		public const int LOGON32_LOGON_SERVICE = 3;
		public const int LOGON32_PROVIDER_DEFAULT = 0;
		public const System.UInt32 ERROR_LOGON_FAILURE = 1326;
		public const System.UInt32 ERROR_ACCOUNT_RESTRICTION = 1327;
		public const System.UInt32 ERROR_PASSWORD_MUST_CHANGE = 1907;
		public const System.UInt32 ERROR_DOMAIN_CONTROLLER_NOT_FOUND = 1908;
		public const System.UInt32 ERROR_ACCOUNT_LOCKED_OUT = 1909;

		[DllImport("advapi32.dll", CharSet = CharSet.Auto)]
		public static extern bool LogonUser(String lpszUserName, String lpszDomain,
			IntPtr lpszPassword, // Formerly a String, changed to enable SecureString passwords.
			int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public extern static bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public extern static System.UInt32 GetLastError();
		#endregion	}
	}
}
