// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SCTextSegment.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Text;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for SCTextSegment.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SCTextSegment: ISCTextSegment
	{
		private BCVRef m_firstRef;
		private BCVRef m_lastRef;
		private string m_filename;
		private int m_lineNumber;
		private string m_marker;
		private string m_text;
		private string m_literalVerse;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SCTextSegment"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SCTextSegment(string text, string marker, string literalVerse,
			BCVRef firstRef, BCVRef lastRef, string filename, int lineNumber)
		{
			m_marker = marker;
			m_text = text;
			m_firstRef = new BCVRef(firstRef);
			m_lastRef = new BCVRef(lastRef);
			m_filename = filename;
			m_lineNumber = lineNumber;
			m_literalVerse = literalVerse;
		}

		#region ISCTextSegment Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the first reference of the segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef FirstReference
		{
			get { return new BCVRef(m_firstRef); }
			set { m_firstRef = new BCVRef(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return the last reference of the segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef LastReference
		{
			get { return new BCVRef(m_lastRef); }
			set { m_lastRef = new BCVRef(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the filename from which the segment was read.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CurrentFileName
		{
			get
			{
				return m_filename;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the line number from which the segment was read
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentLineNumber
		{
			get
			{
				return m_lineNumber;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the literal verse text for a verse number segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LiteralVerseNum
		{
			get { return m_literalVerse; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the text of the segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Text
		{
			get { return m_text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return the marker for a segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Marker
		{
			get { return m_marker; }
		}
		#endregion
	}
}
