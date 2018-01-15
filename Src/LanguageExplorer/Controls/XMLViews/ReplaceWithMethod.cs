// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Implement FakeDoIt by replacing whatever matches the pattern with the result.
	/// </summary>
	internal class ReplaceWithMethod : DoItMethod
	{

		IVwPattern m_pattern;
		ITsString m_replacement;
		IVwTxtSrcInit m_textSourceInit;
		IVwTextSource m_ts;

		internal ReplaceWithMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XElement spec, IVwPattern pattern, ITsString replacement)
			: base(cache, sda, accessor, spec)
		{
			m_pattern = pattern;
			m_replacement = replacement;
			m_pattern.ReplaceWith = m_replacement;
			m_textSourceInit = VwStringTextSourceClass.Create();
			m_ts = m_textSourceInit as IVwTextSource;
		}

		/// <summary>
		/// We can do a replace if the pattern matches.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override bool OkToChange(int hvo)
		{
			if (!base.OkToChange(hvo))
			{
				return false;
			}
			var tss = OldValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
			m_textSourceInit.SetString(tss);
			int ichMin, ichLim;
			m_pattern.FindIn(m_ts, 0, tss.Length, true, out ichMin, out ichLim, null);
			return ichMin >= 0;
		}

		/// <summary>
		/// Actually produce the replacement string.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override ITsString NewValue(int hvo)
		{
			var tss = OldValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
			m_textSourceInit.SetString(tss);
			var ichStartSearch = 0;
			ITsStrBldr tsb = null;
			var delta = 0; // Amount added to length of string (negative if shorter).
			var cch = tss.Length;
			// Some tricky stuff going on here. We allow a match ONCE where ichStartSearch = cch,
			// because some patterns match an empty string, and (once) we want to allow that.
			// But we must not allow it repeatedly, because we can get an infinite sequence
			// of replacements if ichLim comes out of FindIn equal to ichStartSearch.
			// Also, normally we want a pattern which matched, say, chars 1 and 2 to also
			// be able to match chars 3 and 4. But we don't want one which matched 1 and 2
			// to also match the empty position between 2 and 3. Or, for example, ".*" to
			// match the whole input, and then again the empty string at the end.
			// To achieve this, we allow each match to start where the last one ended,
			// but a match of zero length that ends exactly where a previous match
			// ended is discarded.
			var ichLimLastMatch = -1;
			for ( ; ichStartSearch <= cch; )
			{
				int ichMin, ichLim;
				m_pattern.FindIn(m_ts, ichStartSearch, cch, true, out ichMin, out ichLim, null);
				if (ichMin < 0)
				{
					break;
				}
				if (ichLim == ichLimLastMatch)
				{
					ichStartSearch = ichLim + 1;
					continue;
				}
				ichLimLastMatch = ichLim;
				if (tsb == null)
				{
					tsb = tss.GetBldr();
				}
				var tssRep = m_pattern.ReplacementText;
				tsb.ReplaceTsString(ichMin + delta, ichLim + delta, tssRep);
				delta += tssRep.Length - (ichLim - ichMin);
				ichStartSearch = ichLim;
			}
			return tsb?.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
		}

		/// <summary>
		/// This is very like the base Doit, but we can save a duplicate pattern search
		/// by calling the BASE version of OkToChange rather than our own version, which
		/// tests for at least one match. We DO need to call the base version, e.g., so
		/// we don't change wordforms which shouldn't change because they are in use.
		/// </summary>
		/// <param name="hvo"></param>
		public override void Doit(int hvo)
		{
			if (!base.OkToChange(hvo))
			{
				return;
			}
			var tss = NewValue(hvo);
			if (tss != null)
			{
				SetNewValue(hvo, tss);
			}
		}
	}
}