// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.LCModel.Core.Text
{
	/// <summary>
	/// This class represents an incremental formatted string builder.
	/// </summary>
	public class TsIncStrBldr : ITsIncStrBldr
	{
		private readonly StringBuilder m_text;
		private readonly List<TsRun> m_runs;
		private ITsPropsBldr m_propsBldr;

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
			m_propsBldr = m_runs.Count == 0 ? new TsPropsBldr() : m_runs[m_runs.Count - 1].TextProps.GetBldr();
			if (m_text.Length == 0)
				m_runs.Clear();
		}

		/// <summary>
		/// Gets the current text.
		/// </summary>
		public string Text
		{
			get { return m_text.Length == 0 ? null : m_text.ToString(); }
		}

		internal IList<TsRun> Runs
		{
			get { return m_runs; }
		}

		internal ITsPropsBldr PropsBldr
		{
			get { return m_propsBldr; }
		}

		/// <summary>
		/// Appends the specified string to end of the current text.
		/// </summary>
		public void Append(string bstrIns)
		{
			if (string.IsNullOrEmpty(bstrIns))
				return;

			m_text.Append(bstrIns);

			var newRun = new TsRun(m_text.Length, (TsTextProps) m_propsBldr.GetTextProps());
			if (m_runs.Count > 0 && m_runs[m_runs.Count - 1].TextProps.Equals(newRun.TextProps))
				m_runs[m_runs.Count - 1] = newRun;
			else
				m_runs.Add(newRun);
		}

		/// <summary>
		/// Appends the specified <see cref="ITsString"/> to end of the current text.
		/// </summary>
		public void AppendTsString(ITsString tssIns)
		{
			if (tssIns == null)
				throw new ArgumentNullException("tssIns");

			var tss = (TsString) tssIns;

			ITsPropsBldr propsBldr = tss.Runs[tss.Runs.Count - 1].TextProps.GetBldr();
			if (tss.Length == 0)
			{
				// No characters to insert - just update the current properties.
				m_propsBldr = propsBldr;
				return;
			}

			int len = m_text.Length;
			m_text.Append(tss.Text ?? string.Empty);

			// Determine if the last run of the bldr should be merged with the first run of the
			// append string.
			if (tss.RunCount > 0 && m_runs.Count > 0 && m_runs[m_runs.Count - 1].TextProps.Equals(tss.Runs[0].TextProps))
				m_runs.RemoveAt(m_runs.Count - 1);

			// copy runs
			foreach (TsRun run in tss.Runs)
				m_runs.Add(new TsRun(run.IchLim + len, run.TextProps));

			// Update the current properties.
			m_propsBldr = propsBldr;
		}

		/// <summary>
		/// Appends the specified string to the end of the current text.
		/// This method is only used by Views.
		/// </summary>
		public void AppendRgch(string rgchIns, int cchIns)
		{
			if (cchIns < 0 || cchIns > (rgchIns == null ? 0 : rgchIns.Length))
				throw new ArgumentOutOfRangeException("cchIns");

			Append(rgchIns == null ? string.Empty : rgchIns.Substring(0, cchIns));
		}

		/// <summary>
		/// Set an integer property to be applied to any subsequent append operations. The type is not checked for validity,
		/// but its value affects how the variation and value are interpreted.
		/// If the variation and value are -1, the integer property is deleted.
		/// </summary>
		public void SetIntPropValues(int tpt, int nVar, int nVal)
		{
			m_propsBldr.SetIntPropValues(tpt, nVar, nVal);
		}

		/// <summary>
		/// Set a string property to be applied to any subsequent append operations. The type is not checked for validity.
		/// If variation is -1 and value is empty, the string property is deleted.
		/// </summary>
		public void SetStrPropValue(int tpt, string bstrVal)
		{
			m_propsBldr.SetStrPropValue(tpt, bstrVal);
		}

		/// <summary>
		/// Sets a string property by passing a byte array and length.
		/// This method is only used by Views.
		/// </summary>
		public void SetStrPropValueRgch(int tpt, byte[] rgchVal, int nValLength)
		{
			m_propsBldr.SetStrPropValueRgch(tpt, rgchVal, nValLength);
		}

		/// <summary>
		/// Creates an <see cref="ITsString"/> instance from the current state.
		/// </summary>
		public ITsString GetString()
		{
			if (m_runs.Count == 0)
				return new TsString(null, (TsTextProps) m_propsBldr.GetTextProps());

			return new TsString(Text, m_runs);
		}

		/// <summary>
		/// Clears the current state.
		/// </summary>
		public void Clear()
		{
			m_text.Clear();
			m_runs.Clear();
		}

		/// <summary>
		/// Clears all the properties for the next run.
		/// </summary>
		public void ClearProps()
		{
			m_propsBldr.Clear();
		}
	}
}
