// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TsRunPart.cs
// Responsibility: FW Team

using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	#region TsStringSegment class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores a TsString and an indication of an interesting range within it.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TsStringSegment
	{
		/// <summary>
		/// The character position where the segment starts
		/// </summary>
		public readonly int IchMin;
		/// <summary>
		/// The character limit where the segment ends
		/// </summary>
		public readonly int IchLim;
		/// <summary>
		/// TsString from which this segment derives
		/// </summary>
		protected readonly ITsString m_tssBase;
		/// <summary>
		/// Cached TsString representing the substring that corresponds to this segment
		/// </summary>
		protected ITsString m_tssSeg;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringSegment"/> class for the
		/// specified run of the specified TsString.
		/// </summary>
		/// <param name="tssBase">TsString from which this segment derives</param>
		/// <param name="irun">The index of the run to represent.</param>
		/// ------------------------------------------------------------------------------------
		internal TsStringSegment(ITsString tssBase, int irun)
		{
			m_tssBase = tssBase;
			IchMin = tssBase.get_MinOfRun(irun);
			IchLim = tssBase.get_LimOfRun(irun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringSegment"/> class.
		/// </summary>
		/// <param name="tssBase">TsString from which this segment derives</param>
		/// <param name="ichMin">The character position where the segment starts</param>
		/// <param name="ichLim">The character limit where the segment ends</param>
		/// ------------------------------------------------------------------------------------
		public TsStringSegment(ITsString tssBase, int ichMin, int ichLim)
		{
			m_tssBase = tssBase;
			IchMin = ichMin;
			IchLim = ichLim;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TsString representing the substring that corresponds to this segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString String
		{
			get { return m_tssSeg ?? (m_tssSeg = m_tssBase.GetSubstring(IchMin, IchLim)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representing the substring that corresponds to this segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Text
		{
			get
			{
				// For efficiency, we use the cached tss if we have it; otherwise, just get the
				// text and avoid the overhead of creating a TsString
				return (m_tssSeg != null) ? m_tssSeg.Text : m_tssBase.Text.Substring(IchMin, IchLim - IchMin);
			}
		}
	}
	#endregion

	#region TsRunPart class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a part of a run of a TsString.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public sealed class TsRunPart : TsStringSegment
	{
		/// <summary>
		/// Cached Text properties for this part
		/// </summary>
		private ITsTextProps m_props;
		private readonly int m_irun;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsRunPart"/> class for the specified
		/// run of the specified TsString.
		/// </summary>
		/// <param name="tssBase">The TsString that this part is a piece of.</param>
		/// <param name="irun">The index of the run to represent.</param>
		/// ------------------------------------------------------------------------------------
		internal TsRunPart(ITsString tssBase, int irun) : base(tssBase, irun)
		{
			m_irun = irun;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsRunPart"/> class representing a
		/// part of the specified TsString.
		/// </summary>
		/// <param name="partBase">The part (typically corresponding to a full run) this part is
		/// a piece of.</param>
		/// <param name="ichMin">The character position where the part starts.</param>
		/// <param name="ichLim">The character limit where the part ends.</param>
		/// ------------------------------------------------------------------------------------
		private TsRunPart(TsRunPart partBase, int ichMin, int ichLim) : base(partBase.m_tssBase, ichMin, ichLim)
		{
			m_irun = partBase.m_irun;
			m_props = partBase.Props; // Use property accessor for efficiency
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text properties for this part
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsTextProps Props
		{
			get { return m_props ?? (m_props = m_tssBase.get_Properties(m_irun)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all of the words in this part where a word is defined as separated by whitespace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<TsRunPart> Words()
		{
			string tssText = m_tssBase.Text;
			int index = IchMin;
			int wordStart = -1;
			while (index < IchLim)
			{
				if (!char.IsWhiteSpace(tssText[index]))
				{
					if (wordStart == -1)
						wordStart = index;
				}
				else if (wordStart != -1)
				{
					yield return new TsRunPart(this, wordStart, index);
					wordStart = -1;
				}
				index++;
			}

			if (wordStart != -1)
				yield return new TsRunPart(this, wordStart, index);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lasts the word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsRunPart LastWord()
		{
			string tssText = m_tssBase.Text;
			int index = IchLim - 1;
			int wordLim = -1;
			while (index >= IchMin)
			{
				if (!char.IsWhiteSpace(tssText[index]))
				{
					if (wordLim == -1)
						wordLim = index + 1;
				}
				else if (wordLim != -1)
					return new TsRunPart(this, index + 1, wordLim);
				index--;
			}

			return (wordLim != -1) ? new TsRunPart(this, index + 1, wordLim) : null;
		}
	}
	#endregion
}
