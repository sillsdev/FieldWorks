// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using L10NSharp.UI;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	partial class ConstituentChart
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposing)
			{
				components?.Dispose();
				m_toolTip?.Dispose();
				_sharedEventHandlers.Remove(Command.CmdRepeatLastMoveLeft);
				_sharedEventHandlers.Remove(Command.CmdRepeatLastMoveRight);
				_uiWidgetController?.RemoveUserControlHandlers(this);
				// TODO: _dataMenu
			}

			components = null;
			_sharedEventHandlers = null;
			m_toolTip = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
			_uiWidgetController = null;

			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// ConstituentChart
			//
			this.AccessibleDescription = "Main Chart object includes ribbon and column headers and column buttons.";
			this.AccessibleName = "ConstituentChart";
			this.Name = "ConstituentChart";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
