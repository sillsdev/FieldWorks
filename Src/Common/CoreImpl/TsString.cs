// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using SIL.Extensions;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This class represents a formatted string. The string is divided into different runs. Each run
	/// has its own properties that define its style and writing system.
	/// </summary>
	public class TsString : TsStrBase, ITsString, IEquatable<ITsString>, IEquatable<TsString>
	{
		private readonly string m_text;
		private readonly TsRun[] m_runs;

		internal TsString(int ws)
			: this(null, ws)
		{
		}

		internal TsString(string text, int ws)
			: this(text, new TsTextProps(ws))
		{
		}

		internal TsString(string text, TsTextProps textProps)
			: this(text, new TsRun(text == null ? 0 : text.Length, textProps).ToEnumerable())
		{
		}

		internal TsString(string text, IEnumerable<TsRun> runs)
		{
			m_text = text == string.Empty ? null : text;
			m_runs = runs.ToArray();
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		public override string Text
		{
			get { return m_text; }
		}

		internal override IList<TsRun> Runs
		{
			get { return m_runs; }
		}

		/// <summary>
		/// Gets the starting offset of the specified run.
		/// </summary>
		public int get_MinOfRun(int irun)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			return GetRunIchMin(irun);
		}

		/// <summary>
		/// Get the limit (end offset + 1) of the specified run.
		/// </summary>
		public int get_LimOfRun(int irun)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			return m_runs[irun].IchLim;
		}

		/// <summary>
		/// Locks the text for reading. This method is only needed by Views.
		/// </summary>
		public void LockText(out string prgch, out int cch)
		{
			prgch = m_text ?? string.Empty;
			cch = Text == null ? 0 : Text.Length;
		}

		/// <summary>
		/// Unlocks the text. This method is only needed by Views.
		/// </summary>
		public void UnlockText(string rgch)
		{
			if ((m_text ?? string.Empty) != rgch)
				throw new ArgumentException("The text cannot be changed.");
		}

		/// <summary>
		/// Locks the run for reading. This method is only needed by Views.
		/// </summary>
		public void LockRun(int irun, out string prgch, out int cch)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			prgch = get_RunText(irun);
			cch = prgch.Length;
		}

		/// <summary>
		/// Unlocks the run.
		/// </summary>
		public void UnlockRun(int irun, string rgch)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			if (get_RunText(irun) != rgch)
				throw new ArgumentException("The run text cannot be changed.");
		}

		/// <summary>
		/// Gets a string builder. The builder allows a copy of the string to be modified using
		/// a series of replace operations. There is no connection between the builder and the
		/// string. Data is copied to the builder.
		/// </summary>
		/// <returns></returns>
		public ITsStrBldr GetBldr()
		{
			return new TsStrBldr(Text, Runs);
		}

		/// <summary>
		/// Gets an incremental string builder. The builder allows a copy of the string to be
		/// modified using a series of append operations. There is no connection between the builder
		/// and the string. Data is copied to the builder.
		/// </summary>
		/// <returns></returns>
		public ITsIncStrBldr GetIncBldr()
		{
			return new TsIncStrBldr(Text, Runs);
		}

		/// <summary>
		/// Determines if the specified string is equal to this string.
		/// </summary>
		public bool Equals(ITsString tss)
		{
			var other = tss as TsString;
			return other != null && Equals(other);
		}

		/// <summary>
		/// Gets the substring for the specified range returned as an <see cref="ITsString"/>.
		/// </summary>
		public ITsString GetSubstring(int ichMin, int ichLim)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			if (ichMin == 0 && ichLim == Length)
				return this;

			string newText = GetChars(ichMin, ichLim);

			var newRuns = new List<TsRun>();
			int irun = get_RunAt(ichMin);
			for (int i = irun; i < m_runs.Length; i++)
			{
				TsRun run = m_runs[i];
				bool lastRun = ichLim <= run.IchLim;
				newRuns.Add(new TsRun((lastRun ? ichLim : run.IchLim) - ichMin, run.TextProps));
				if (lastRun)
					break;
			}

			return new TsString(newText, newRuns.ToArray());
		}

		/// <summary>
		/// Return whether the string is already in the specified normal form.
		/// Note that a string may be considered to be in NFC
		/// even though its text (the plain character sequence) is not.
		/// This is because we don't collapse otherwise collapsible pairs if they
		/// have different style properties.
		/// </summary>
		public bool get_IsNormalizedForm(FwNormalizationMode nm)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Return an equivalent string in the specified normal form.
		/// This may be the same object as the recipient, if it is already in
		/// that normal form.
		/// Note that <see cref="TsString"/> instances normalized to NFC may not have text
		/// that is so normalized. This is because we don't collapse otherwise collapsible
		/// pairs if they have different style properties.
		/// </summary>
		public ITsString get_NormalizedForm(FwNormalizationMode nm)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Return an equivalent string in NFD.
		/// This may be the same object as the recipient, if it is already in that normal form.
		///
		/// The values pointed to by the array of offsets to fix are each offsets into
		/// the string. The code attempts to adjust them to corresponding offsets in the output
		/// string. An exact correspondence is not always achieved; if the offset is in the middle
		/// of a diacritic sequence, it may be moved to the start of the following base character
		/// (or the end of the string).
		/// </summary>
		public void NfdAndFixOffsets(out ITsString ptssRet, int[] rgpichOffsetsToFix, int cichOffsetsToFix)
		{
			throw new NotImplementedException();
		}

		// TODO: XML serialization should not be coupled with the data object, this should be done elsewhere

		/// <summary>
		/// Writes this instance to the specified stream in the FW XML format.
		/// </summary>
		public void WriteAsXml(IStream strm, ILgWritingSystemFactory wsf, int cchIndent, int ws, bool fWriteObjData)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the FW XML representation of this instance.
		/// </summary>
		public string GetXmlString(ILgWritingSystemFactory wsf, int cchIndent, int ws, bool fWriteObjData)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Writes this <see cref="TsString"/> to the specified stream in the FW XML format.
		/// </summary>
		public void WriteAsXmlExtended(IStream strm, ILgWritingSystemFactory wsf, int cchIndent, int ws, bool fWriteObjData, bool fUseRfc4646)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		public bool Equals(TsString other)
		{
			return other != null && m_text == other.m_text && m_runs.SequenceEqual(other.m_runs);
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
		/// </summary>
		public override bool Equals(object obj)
		{
			var other = obj as TsString;
			return other != null && Equals(other);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + (m_text == null ? 0 : m_text.GetHashCode());
			code = code * 31 + m_runs.GetSequenceHashCode();
			return code;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return Text;
		}
	}
}
