// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// This class encapsulates the information for a log message.
	/// </summary>
	public class ImportMessage : IComparable
	{
		public ImportMessage(string sMsg, int lineNumber)
		{
			Message = sMsg;
			LineNumber = lineNumber;
		}

		public string Message { get; }

		public int LineNumber { get; }

		#region IComparable Members
		public int CompareTo(object obj)
		{
			var that = obj as ImportMessage;
			if (that == null)
			{
				return 1;
			}
			return Message == that.Message ? LineNumber.CompareTo(that.LineNumber) : Message.CompareTo(that.Message);
		}
		#endregion
	}
}