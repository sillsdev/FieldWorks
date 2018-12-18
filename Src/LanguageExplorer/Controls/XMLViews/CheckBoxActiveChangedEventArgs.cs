// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class is the arguments for a ClickCopyEventHandler.
	/// </summary>
	public class CheckBoxActiveChangedEventArgs : CheckBoxChangedEventArgs
	{
		/// <summary />
		public CheckBoxActiveChangedEventArgs(int[] hvosChanged, string undoMessage, string redoMessage)
			: base(hvosChanged)
		{
			UndoMessage = undoMessage;
			RedoMessage = redoMessage;
		}


		/// <summary />
		public string UndoMessage { get; }

		/// <summary />
		public string RedoMessage { get; }
	}

	/// <summary />
	public delegate void CheckBoxActiveChangedEventHandler(object sender, CheckBoxActiveChangedEventArgs e);
}