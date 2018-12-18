// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class of object to be passed to the RecordError delegate, containing information about
	/// the location and nature of the checking inconsistency.
	/// </summary>
	public class RecordErrorEventArgs : EventArgs
	{
		/// <summary />
		public RecordErrorEventArgs(TextTokenSubstring tts, Guid checkId)
		{
			Tts = tts;
			CheckId = checkId;
		}

		/// <summary>
		/// Gets the TextTokenSubstring.
		/// </summary>
		public TextTokenSubstring Tts { get; }

		/// <summary>
		/// Gets the GUID identifying the check.
		/// </summary>
		public Guid CheckId { get; }
	}

	/// <summary>
	/// Callback to record an error occuring at the specified location within
	/// the token. The message will have already been localized by calling the
	/// IChecksDataSource.GetLocalizedString() method.
	/// </summary>
	public delegate void RecordErrorHandler(RecordErrorEventArgs args);
}
