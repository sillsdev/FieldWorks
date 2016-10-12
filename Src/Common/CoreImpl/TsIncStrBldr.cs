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
	/// This class represents an incremental formatted string builder.
	/// </summary>
	public class TsIncStrBldr : ITsIncStrBldr
	{
		private readonly StringBuilder m_text;
		private readonly List<TsRun> m_runs;

		/// <summary>
		/// Initializes a new instance of the <see cref="TsIncStrBldr"/> class.
		/// </summary>
		public TsIncStrBldr()
			: this(null, TsRun.EmptyRun.ToEnumerable())
		{
		}

		internal TsIncStrBldr(string text, TsTextProps textProps)
			: this(text, new TsRun(text == null ? 0 : text.Length, textProps).ToEnumerable())
		{
		}

		internal TsIncStrBldr(string text, IEnumerable<TsRun> runs)
		{
			m_text = new StringBuilder(text ?? string.Empty);
			m_runs = runs.ToList();
		}

		/// <summary>
		/// Gets the current text.
		/// </summary>
		public string Text
		{
			get { return m_text.Length == 0 ? null : m_text.ToString(); }
		}

		/// <summary>
		/// Appends the specified string to end of the current text.
		/// </summary>
		public void Append(string bstrIns)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Appends the specified <see cref="ITsString"/> to end of the current text.
		/// </summary>
		public void AppendTsString(ITsString tssIns)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Appends the specified string to the end of the current text.
		/// This method is only used by Views.
		/// </summary>
		public void AppendRgch(string rgchIns, int cchIns)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Set an integer property to be applied to any subsequent append operations. The type is not checked for validity,
		/// but its value affects how the variation and value are interpreted.
		/// If the variation and value are -1, the integer property is deleted.
		/// </summary>
		public void SetIntPropValues(int tpt, int nVar, int nVal)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Set a string property to be applied to any subsequent append operations. The type is not checked for validity.
		/// If variation is -1 and value is empty, the string property is deleted.
		/// </summary>
		public void SetStrPropValue(int tpt, string bstrVal)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates an <see cref="ITsString"/> instance from the current state.
		/// </summary>
		public ITsString GetString()
		{
			return new TsString(Text, m_runs);
		}

		/// <summary>
		/// Clears the current state.
		/// </summary>
		public void Clear()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets a string property by passing a byte array and length.
		/// This method is only used by Views.
		/// </summary>
		public void SetStrPropValueRgch(int tpt, byte[] rgchVal, int nValLength)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Clears all the properties for the next run.
		/// </summary>
		public void ClearProps()
		{
			throw new NotImplementedException();
		}
	}
}
