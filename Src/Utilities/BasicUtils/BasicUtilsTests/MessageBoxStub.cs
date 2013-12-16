// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MessageBoxStub.cs
// Responsibility: EberhardB
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stub for tests that display message boxes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MessageBoxStub: IMessageBox
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This implementation displays the message in the Console and returns the first
		/// button as dialog result.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DialogResult Show(IWin32Window owner, string text, string caption,
			MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			// When running tests, displaying a message box is usually not what we want so we
			// just write to the Console.
			// If we later change our mind we have to check Environment.UserInteractive. If it
			// is false we have to use MessageBoxOptions.ServiceNotification or
			// DefaultDesktopOnly so that it works when running from a service (build machine).
			Console.WriteLine("**** {0}: {1}{3}{2}", caption, text, buttons, Environment.NewLine);

			return TranslateButtons(buttons);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This implementation displays the message in the Console and returns the first
		/// button as dialog result.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DialogResult Show(IWin32Window owner, string text, string caption,
			MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton,
			MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param)
		{
			Console.WriteLine("**** {0}: {1}{3}{2}", caption, text, buttons, Environment.NewLine);

			return TranslateButtons(buttons);
		}

		private DialogResult TranslateButtons(MessageBoxButtons buttons)
		{
			switch (buttons)
			{
				case MessageBoxButtons.OK:
				case MessageBoxButtons.OKCancel:
					return DialogResult.OK;
				case MessageBoxButtons.YesNo:
				case MessageBoxButtons.YesNoCancel:
					return DialogResult.Yes;
				case MessageBoxButtons.RetryCancel:
					return DialogResult.Retry;
				case MessageBoxButtons.AbortRetryIgnore:
					return DialogResult.Abort;
				default:
					return DialogResult.OK;
			}
		}
	}
}
