// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This basically just wraps the MessageBox class to allow replacing it with a test stub
	/// during unit tests.
	/// </summary>
	public static class MessageBoxUtils
	{
		private static IMessageBox s_MsgBox = new MessageBoxAdapter();

		/// <summary>
		/// Sets the MessageBox adapter.
		/// </summary>
		public static void SetMessageBoxAdapter(IMessageBox adapter)
		{
			s_MsgBox = adapter;
		}

		/// <summary>
		/// Resets the MessageBox adapter to the default adapter which will display a
		/// message box.
		/// </summary>
		public static void Reset()
		{
			s_MsgBox = new MessageBoxAdapter();
		}

		#region MessageBoxAdapter class

		private sealed class MessageBoxAdapter : IMessageBox
		{
			/// <summary>
			/// Displays a message box in front of the specified object and with the specified
			/// text, caption, buttons, and icon.
			/// </summary>
			public DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
			{
				return MessageBox.Show(owner, text, caption, buttons, icon);
			}

			/// <summary>
			/// Displays a message box with the specified text, caption, buttons, icon, default
			/// button, options, and Help button, using the specified Help file, HelpNavigator,
			/// and Help topic.
			/// </summary>
			public DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
				MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param)
			{
				return MessageBox.Show(owner, text, caption, buttons, icon, defaultButton, options, helpFilePath, navigator, param);
			}

		}

		#endregion

		/// <summary>
		/// Displays a message box with the specified text, caption, buttons, icon, default
		/// button, options, and Help button, using the specified Help file, HelpNavigator, and
		/// Help topic.
		/// </summary>
		public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
			MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param)
		{
			return s_MsgBox.Show(owner, text, caption, buttons, icon, defaultButton, options, helpFilePath, navigator, param);
		}

		/// <summary>
		/// Displays a message box with the specified text, caption, buttons, icon, default
		/// button, options, and Help button, using the specified Help file, HelpNavigator, and
		/// Help topic.
		/// </summary>
		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
			MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param)
		{
			// TODO-Linux: Help is not implemented in Mono
			return Show(null, text, caption, buttons, icon, defaultButton, options, helpFilePath, navigator, param);
		}

		/// <summary>
		/// Displays a message box in front of the specified object and with the specified text,
		/// caption, buttons, and icon.
		/// </summary>
		public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			return s_MsgBox.Show(owner, text, caption, buttons, icon);
		}

		/// <summary>
		/// Displays a message box with the specified text, caption, buttons, and icon.
		/// </summary>
		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			return Show(null, text, caption, buttons, icon);
		}

		/// <summary>
		/// Displays a message box in front of the specified object and with the specified text,
		/// caption and buttons.
		/// </summary>
		public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons)
		{
			return Show(owner, text, caption, buttons, MessageBoxIcon.Information);
		}

		/// <summary>
		/// Displays a message box in front of the specified object and with the specified text,
		/// and caption.
		/// </summary>
		public static DialogResult Show(IWin32Window owner, string text, string caption)
		{
			return Show(owner, text, caption, MessageBoxButtons.OK);
		}

		/// <summary>
		/// Displays a message box with the specified text, caption, and buttons.
		/// </summary>
		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
		{
			return Show(null, text, caption, buttons, MessageBoxIcon.Information);
		}

		/// <summary>
		/// Displays a message box with the specified text and caption.
		/// </summary>
		public static DialogResult Show(string text, string caption)
		{
			return Show(text, caption, MessageBoxButtons.OK);
		}

		/// <summary>
		/// Displays a message box with the specified text.
		/// </summary>
		public static DialogResult Show(string text)
		{
			return Show(text, string.Empty);
		}
	}
}