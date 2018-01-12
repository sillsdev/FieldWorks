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
		private string m_sMsg;
		private int m_lineNumber;

		public ImportMessage(string sMsg, int lineNumber)
		{
			m_sMsg = sMsg;
			m_lineNumber = lineNumber;
		}

		public string Message
		{
			get { return m_sMsg; }
		}

		public int LineNumber
		{
			get { return m_lineNumber; }
		}

		#region IComparable Members
		public int CompareTo(object obj)
		{
			ImportMessage that = obj as ImportMessage;
			if (that == null)
				return 1;
			if (this.Message == that.Message)
				return this.LineNumber.CompareTo(that.LineNumber);
			else
				return this.Message.CompareTo(that.Message);
		}
		#endregion
	}
}