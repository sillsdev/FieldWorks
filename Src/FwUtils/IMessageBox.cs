// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for message box methods. This helps in not showing modal message boxes when
	/// running tests.
	/// </summary>
	public interface IMessageBox
	{
		/// <summary>
		/// Displays a message box in front of the specified object and with the specified text,
		/// caption, buttons, and icon.
		/// </summary>
		DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon);

		/// <summary>
		/// Displays a message box with the specified text, caption, buttons, icon, default
		/// button, options, and Help button, using the specified Help file, HelpNavigator, and
		/// Help topic.
		/// </summary>
		DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
			MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param);
	}
}