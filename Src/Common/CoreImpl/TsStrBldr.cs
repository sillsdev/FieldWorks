// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This class represents a formatted string builder.
	/// </summary>
	public class TsStrBldr : TsStrBase, ITsStrBldr
	{
		private readonly StringBuilder m_text;
		private readonly List<TsRun> m_runs;

		/// <summary>
		/// Initializes a new instance of the <see cref="TsStrBldr"/> class.
		/// </summary>
		public TsStrBldr()
			: this(null, TsRun.EmptyRun)
		{
		}

		internal TsStrBldr(string text, TsRun run)
			: this(text, run.ToEnumerable())
		{
		}

		internal TsStrBldr(string text, IEnumerable<TsRun> runs)
		{
			m_text = new StringBuilder(text ?? string.Empty);
			m_runs = runs.ToList();
		}

		/// <summary>
		/// Gets the current text.
		/// </summary>
		public override string Text
		{
			get { return m_text.Length == 0 ? null : m_text.ToString(); }
		}

		internal override IList<TsRun> Runs
		{
			get { return m_runs; }
		}

		/// <summary>
		/// Replaces the specified range of characters with the specified string.
		/// </summary>
		public void Replace(int ichMin, int ichLim, string bstrIns, ITsTextProps ttp)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Replaces the specified range of characters with the specified <see cref="ITsString"/>.
		/// </summary>
		public void ReplaceTsString(int ichMin, int ichLim, ITsString tssIns)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Replaces the specified range of characters with the specified string.
		/// This method is only used by Views.
		/// </summary>
		public void ReplaceRgch(int ichMin, int ichLim, string rgchIns, int cchIns, ITsTextProps ttp)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the text properties for the specified range of characters.
		/// </summary>
		public void SetProperties(int ichMin, int ichLim, ITsTextProps ttp)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the integer property values for the range of characters.
		/// If the variation and value are both -1, then the integer property is removed.
		/// </summary>
		public void SetIntPropValues(int ichMin, int ichLim, int tpt, int nVar, int nVal)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Set the string property value for the range of characters.
		/// If the varation is -1 and the value is empty, then the string property is removed.
		/// </summary>
		public void SetStrPropValue(int ichMin, int ichLim, int tpt, string bstrVal)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates an <see cref="ITsString"/> from the current state.
		/// </summary>
		public ITsString GetString()
		{
			return new TsString(Text, Runs);
		}

		/// <summary>
		/// Clears everything from the formatted string builder (return to state when just created).
		/// </summary>
		public void Clear()
		{
			m_text.Clear();
			m_runs.Clear();
			m_runs.Add(TsRun.EmptyRun);
		}
	}
}
