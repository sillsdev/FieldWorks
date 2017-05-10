// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl.Text
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
			: this(null, TsRun.EmptyRun.ToEnumerable())
		{
		}

		internal TsStrBldr(string text, TsTextProps textProps)
			: this(text, new TsRun(text == null ? 0 : text.Length, textProps).ToEnumerable())
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
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			ReplaceRgch(ichMin, ichLim, bstrIns, bstrIns == null ? 0 : bstrIns.Length, ttp);
		}

		/// <summary>
		/// Replaces the specified range of characters with the specified <see cref="ITsString"/>.
		/// </summary>
		public void ReplaceTsString(int ichMin, int ichLim, ITsString tssIns)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			if (tssIns == null)
			{
				if (ichMin != ichLim)
					ReplaceInternal(ichMin, ichLim, string.Empty, new TsRun[0]);
			}
			else
			{
				if (tssIns.Length > 0 || ichMin != ichLim || Length == 0)
					ReplaceInternal(ichMin, ichLim, tssIns.Text ?? string.Empty, ((TsString) tssIns).Runs);
			}
		}

		/// <summary>
		/// Replaces the specified range of characters with the specified string.
		/// This method is only used by Views.
		/// </summary>
		public void ReplaceRgch(int ichMin, int ichLim, string rgchIns, int cchIns, ITsTextProps ttp)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);
			ThrowIfCharOffsetOutOfRange("cchIns", cchIns, rgchIns == null ? 0 : rgchIns.Length);

			// Don't do anything if there is nothing to insert or delete and we are not empty.
			if (cchIns == 0 && ichMin == ichLim && Length > 0)
				return;

			TsTextProps textProps;
			if (ttp == null)
			{
				// If ichMin equals ichLim, use the previous characters properties.
				int charIndex = ichMin == ichLim && ichMin > 0 ? ichMin - 1 : ichMin;
				textProps = m_runs[get_RunAt(charIndex)].TextProps;
			}
			else
			{
				textProps = (TsTextProps) ttp;
			}

			TsRun run = new TsRun(cchIns, textProps);
			ReplaceInternal(ichMin, ichLim, rgchIns == null ? string.Empty : rgchIns.Substring(0, cchIns), new[] {run});
		}

		private void ReplaceInternal(int ichMin, int ichLim, string insertText, IList<TsRun> insertRuns)
		{
			// This is the only case where we can end up with an empty run.
			if (insertText.Length == 0)
			{
				// If we're deleting everything and we were passed a run, use it.
				if (ichMin == 0 && ichLim == Length)
				{
					m_text.Clear();
					// If we were given some run properties, let them become those of the empty string.
					TsTextProps props = insertRuns.Count > 0 ? insertRuns[0].TextProps : m_runs[0].TextProps;
					m_runs.Clear();
					m_runs.Add(new TsRun(0, props));
					return;
				}

				// We're not deleting everything, therefore any run we were passed describes
				// no characters and should be ignored.
				if (insertRuns.Count > 0)
					insertRuns = new TsRun[0];
			}

			int minRunIndex = get_RunAt(ichMin);
			int limRunIndex = get_RunAt(ichLim);

			m_text.Remove(ichMin, ichLim - ichMin);
			m_text.Insert(ichMin, insertText);

			// dich is the amount that indices >= ichLim should be adjusted by after the replace.
			int dich = insertText.Length - ichLim + ichMin;

			SetPropertiesInternal(ichMin, ichLim, minRunIndex, limRunIndex, dich, insertRuns);
		}

		private void SetPropertiesInternal(int ichMin, int ichLim, int minRunIndex, int limRunIndex, int dich, IList<TsRun> insertRuns)
		{
			// Ensure ichMin is on a run boundary.
			if (ichMin > GetRunIchMin(minRunIndex))
			{
				// Insertion is within a single run.
				if (minRunIndex == limRunIndex)
				{
					// Split the run.
					m_runs.Insert(minRunIndex, new TsRun(ichMin, m_runs[limRunIndex].TextProps));
					limRunIndex++;
				}
				else
				{
					// Adjust the boundary, even when not splitting.
					m_runs[minRunIndex] = new TsRun(ichMin, m_runs[minRunIndex].TextProps);
				}
				minRunIndex++;
			}

			m_runs.RemoveRange(minRunIndex, limRunIndex - minRunIndex);
			m_runs.InsertRange(minRunIndex, insertRuns);

			limRunIndex = minRunIndex + insertRuns.Count;
			if (ichMin > 0)
			{
				for (int i = minRunIndex; i < limRunIndex; i++)
					m_runs[i] = new TsRun(m_runs[i].IchLim + ichMin, m_runs[i].TextProps);
			}

			if (dich != 0)
			{
				for (int i = limRunIndex; i < m_runs.Count; i++)
					m_runs[i] = new TsRun(m_runs[i].IchLim + dich, m_runs[i].TextProps);
			}

			// See if we can combine on the left.
			if (minRunIndex > 0)
			{
				if (m_runs[minRunIndex].TextProps.Equals(m_runs[minRunIndex - 1].TextProps))
				{
					m_runs.RemoveAt(minRunIndex - 1);
					limRunIndex--;
				}
			}

			// See if we can combine on the right.
			if (limRunIndex > 0)
			{
				// Empty right run, delete.
				if (m_runs[limRunIndex].IchLim == m_runs[limRunIndex - 1].IchLim)
				{
					m_runs.RemoveAt(limRunIndex);
				}
				else if (m_runs[limRunIndex - 1].TextProps.Equals(m_runs[limRunIndex].TextProps))
				{
					m_runs.RemoveAt(limRunIndex - 1);
					// limRunIndex is no longer valid after the above delete.
				}
			}
		}

		/// <summary>
		/// Sets the text properties for the specified range of characters.
		/// </summary>
		public void SetProperties(int ichMin, int ichLim, ITsTextProps ttp)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);
			ThrowIfParamNull("ttp", ttp);

			var textProps = (TsTextProps) ttp;

			if (ichMin == ichLim)
			{
				if (Length == 0)
					m_runs[0] = new TsRun(0, textProps);
				return;
			}

			int minRunIndex = get_RunAt(ichMin);
			int limRunIndex = get_RunAt(ichLim);

			TsRun run = new TsRun(ichLim - ichMin, textProps);
			SetPropertiesInternal(ichMin, ichLim, minRunIndex, limRunIndex, 0, new[] { run });
		}

		/// <summary>
		/// Sets the integer property values for the range of characters.
		/// If the variation and value are both -1, then the integer property is removed.
		/// </summary>
		public void SetIntPropValues(int ichMin, int ichLim, int tpt, int nVar, int nVal)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			SetPropertyValue(ichMin, ichLim, textProps => EditIntProperty(textProps, tpt, nVar, nVal));
		}

		private static TsTextProps EditIntProperty(TsTextProps textProps, int tpt, int nVar, int nVal)
		{
			ITsPropsBldr tpb = textProps.GetBldr();
			tpb.SetIntPropValues(tpt, nVar, nVal);
			return (TsTextProps) tpb.GetTextProps();
		}

		/// <summary>
		/// Set the string property value for the range of characters.
		/// If the value is null or empty, then the string property is removed.
		/// </summary>
		public void SetStrPropValue(int ichMin, int ichLim, int tpt, string bstrVal)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			SetPropertyValue(ichMin, ichLim, textProps => EditStrProperty(textProps, tpt, bstrVal));
		}

		private static TsTextProps EditStrProperty(TsTextProps textProps, int tpt, string strVal)
		{
			ITsPropsBldr tpb = textProps.GetBldr();
			tpb.SetStrPropValue(tpt, strVal);
			return (TsTextProps) tpb.GetTextProps();
		}

		private void SetPropertyValue(int ichMin, int ichLim, Func<TsTextProps, TsTextProps> editTextPropsFunc)
		{
			if (ichMin == ichLim)
			{
				if (Length == 0)
					m_runs[0] = new TsRun(0, editTextPropsFunc(m_runs[0].TextProps));
				return;
			}

			int minRunIndex = get_RunAt(ichMin);
			if (GetRunIchMin(minRunIndex) < ichMin)
			{
				m_runs.Insert(minRunIndex, new TsRun(ichMin, m_runs[minRunIndex].TextProps));
				minRunIndex++;
			}

			int limRunIndex = get_RunAt(ichLim - 1);
			if (ichLim < m_runs[limRunIndex].IchLim)
				m_runs.Insert(limRunIndex, new TsRun(ichLim, m_runs[limRunIndex].TextProps));

			for (int i = limRunIndex; i >= minRunIndex; i--)
			{
				m_runs[i] = new TsRun(m_runs[i].IchLim, editTextPropsFunc(m_runs[i].TextProps));
				if (i < m_runs.Count - 1 && m_runs[i].TextProps.Equals(m_runs[i + 1].TextProps))
					m_runs.RemoveAt(i);
			}

			if (minRunIndex > 0 && m_runs[minRunIndex].TextProps.Equals(m_runs[minRunIndex - 1].TextProps))
				m_runs.RemoveAt(minRunIndex - 1);
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
