// MessageBox.cs
// User: Jean-Marc Giffin at 10:21 A 11/07/2008
// Wrapper class from System.Windows.Forms -> GTK

using System;
using Gtk;

namespace System.Windows.Forms
{
	public class MessageBox
	{
		public MessageBox()
		{
		}

		public static DialogResult Show(string text)
		{
			return Show(null, text, "", MessageBoxButtons.OK, MessageBoxIcon.None,
						MessageBoxDefaultButton.Button1, MessageBoxOptions.None);
		}

		public static DialogResult Show(Window window, string text)
		{
			return Show(window, text, "", MessageBoxButtons.OK, MessageBoxIcon.None,
						MessageBoxDefaultButton.Button1, MessageBoxOptions.None);
		}

		public static DialogResult Show(string text, string caption)
		{
			return Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None,
						MessageBoxDefaultButton.Button1, MessageBoxOptions.None);
		}

		public static DialogResult Show(Window window, string text, string caption)
		{
			return Show(window, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None,
						MessageBoxDefaultButton.Button1, MessageBoxOptions.None);
		}

		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
		{
			return Show(null, text, caption, buttons, MessageBoxIcon.None,
						MessageBoxDefaultButton.Button1, MessageBoxOptions.None);
		}

		public static DialogResult Show(Window window, string text, string caption,
										MessageBoxButtons buttons)
		{
			return Show(window, text, caption, buttons, MessageBoxIcon.None,
						MessageBoxDefaultButton.Button1, MessageBoxOptions.None);
		}

		public static DialogResult Show(string text, string caption,
										MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			return Show(null, text, caption, buttons, icon, MessageBoxDefaultButton.Button1,
						MessageBoxOptions.None);
		}

		public static DialogResult Show(Window window, string text, string caption,
										MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			return Show(null, text, caption, buttons, icon, MessageBoxDefaultButton.Button1,
						MessageBoxOptions.None);
		}

		public static DialogResult Show(string text, string caption,
										MessageBoxButtons buttons, MessageBoxIcon icon,
										MessageBoxDefaultButton defaultButton)
		{
			return Show(null, text, caption, buttons, icon, defaultButton,
						MessageBoxOptions.None);
		}

		public static DialogResult Show(Window window, string text, string caption,
										MessageBoxButtons buttons, MessageBoxIcon icon,
										MessageBoxDefaultButton defaultButton)
		{
			return Show(window, text, caption, buttons, icon, defaultButton,
						MessageBoxOptions.None);
		}

		public static DialogResult Show(string text, string caption,
										MessageBoxButtons buttons, MessageBoxIcon icon,
										MessageBoxDefaultButton defaultButton,
										MessageBoxOptions options)
		{
			return Show(null, text, caption, buttons, icon, defaultButton, options);
		}

		public static DialogResult Show(Window window, string text, string caption,
										MessageBoxButtons buttons, MessageBoxIcon icon,
										MessageBoxDefaultButton defaultButton,
										MessageBoxOptions options)
		{
			ButtonsType bt;
			switch (buttons)
			{
				case MessageBoxButtons.YesNo:
					bt = ButtonsType.YesNo;
					break;
				case MessageBoxButtons.OK:
					bt = ButtonsType.Ok;
					break;
				case MessageBoxButtons.OKCancel:
					bt = ButtonsType.OkCancel;
					break;
				default:
					bt = ButtonsType.None;
					break;
			}
			MessageType mt;
			switch (icon)
			{
				case MessageBoxIcon.Information:
					mt = MessageType.Info;
					break;
				case MessageBoxIcon.Warning:
					mt = MessageType.Warning;
					break;
				case MessageBoxIcon.Question:
					mt = MessageType.Question;
					break;
				case MessageBoxIcon.Exclamation:
					mt = MessageType.Info;
					break;
				case MessageBoxIcon.Stop:
					mt = MessageType.Warning;
					break;
				case MessageBoxIcon.Error:
					mt = MessageType.Error;
					break;
				case MessageBoxIcon.Asterisk:
					mt = MessageType.Info;
					break;
				case MessageBoxIcon.Hand:
					mt = MessageType.Warning;
					break;
				default:
					mt = MessageType.Other;
					break;
			}
			MessageDialog md = new MessageDialog(window, DialogFlags.DestroyWithParent, mt, bt, text);
			md.Title = caption;

			switch (buttons)
			{
				case MessageBoxButtons.AbortRetryIgnore:
					md.AddButton("Abort", (int)DialogResult.Abort);
					md.AddButton("Ignore", (int)DialogResult.Ignore);
					md.AddButton("Retry", (int)DialogResult.Retry);
					break;
				case MessageBoxButtons.RetryCancel:
					md.AddButton("Cancel", (int)DialogResult.Cancel);
					md.AddButton("Retry", (int)DialogResult.Retry);
					break;
				case MessageBoxButtons.YesNoCancel:
					md.AddButton("Cancel", (int)DialogResult.Cancel);
					md.AddButton("No", (int)DialogResult.No);
					md.AddButton("Yes", (int)DialogResult.Yes);
					break;
			}
			DialogResult dr = (DialogResult)md.Run();
			md.Destroy();
			return dr;
		}
	}

	public enum MessageBoxButtons
	{
		AbortRetryIgnore,
		OK,
		OKCancel,
		RetryCancel,
		YesNo,
		YesNoCancel
	}

	public enum MessageBoxIcon
	{
		Asterisk,
		Error,
		Exclamation,
		Hand,
		Information,
		None,
		Question,
		Stop,
		Warning
	}

	public enum MessageBoxDefaultButton
	{
		Button1,
		Button2,
		Button3
	}

	public enum MessageBoxOptions
	{
		None,
		DefaultDesktopOnly,
		RightAlign,
		RtlReading,
		ServiceNotification
	}
}
