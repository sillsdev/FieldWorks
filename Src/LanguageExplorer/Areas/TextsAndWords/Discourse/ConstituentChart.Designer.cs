// Copyright (c) 2008-2019 SIL International
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
				_sharedEventHandlers.Remove("CmdRepeatLastMoveLeft");
				_sharedEventHandlers.RemoveStatusChecker("CmdRepeatLastMoveLeft");
				_sharedEventHandlers.Remove("CmdRepeatLastMoveRight");
				_sharedEventHandlers.RemoveStatusChecker("CmdRepeatLastMoveRight");
				if (_fileMenu != null)
				{
					_fileMenu.DropDownItems.Remove(_exportMenu);
					_exportMenu.Click -= ExportDiscourseChart_Click;
					_exportMenu.Dispose();
				}
				// TODO: _dataMenu
			}

			components = null;
			_sharedEventHandlers = null;
			m_toolTip = null;
			_fileMenu = null;
			_exportMenu = null;
			_dataMenu = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

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
