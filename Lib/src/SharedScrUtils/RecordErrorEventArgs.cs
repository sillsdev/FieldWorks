// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RecordErrorEventArgs.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class of object to be passed to the RecodError delegate, containing information about
	/// the location and nature of the checking inconsistency.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RecordErrorEventArgs : EventArgs
	{
		private TextTokenSubstring m_tts;
		private Guid m_checkId;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RecordErrorEventArgs"/> class.
		/// </summary>
		/// <param name="tts">The TextTokenSubstring.</param>
		/// <param name="checkId">The GUID identifying the check.</param>
		/// ------------------------------------------------------------------------------------
		public RecordErrorEventArgs(TextTokenSubstring tts, Guid checkId)
		{
			m_tts = tts;
			m_checkId = checkId;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TextTokenSubstring.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextTokenSubstring Tts
		{
			get { return m_tts; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the GUID identifying the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId
		{
			get { return m_checkId; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Callback to record an error occuring at the specified location within
	/// the token. The message will have already been localized by calling the
	/// IChecksDataSource.GetLocalizedString() method.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public delegate void RecordErrorHandler(RecordErrorEventArgs args);
}
