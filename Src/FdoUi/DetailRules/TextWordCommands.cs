// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// Find in Dictionary and Add to Dictionary on a word of structured text (messages
	/// LexiconLookup and AddToLexicon): whether each applies to the word at the start of the
	/// selection, and the form Add to Dictionary offers.
	/// </summary>
	public static class TextWordCommands
	{
		/// <summary>The one writing system of the span <paramref name="ichMin"/> to
		/// <paramref name="ichLim"/>; 0 when the span is empty or mixes writing
		/// systems.</summary>
		public static int WritingSystemOf(ITsString tss, int ichMin, int ichLim)
		{
			if (tss == null || tss.Length == 0 || ichMin >= ichLim)
				return 0;
			var firstRun = tss.get_RunAt(ichMin);
			var lastRun = tss.get_RunAt(ichLim - 1);
			var ws = tss.get_WritingSystem(firstRun);
			for (var run = firstRun + 1; run <= lastRun; run++)
			{
				if (tss.get_WritingSystem(run) != ws)
					return 0;
			}
			return ws;
		}

		/// <summary>Whether Find in Dictionary applies: the word is not empty.</summary>
		public static bool CanLookUp(int ichMin, int ichLim) => ichLim > ichMin;

		/// <summary>Whether Add to Dictionary applies: the word is not empty and is in the
		/// project's default vernacular writing system.</summary>
		/// <param name="ws">The word's writing system as the selection reports it; 0 to take
		/// it from <paramref name="tss"/>.</param>
		public static bool CanAddToLexicon(LcmCache cache, ITsString tss, int ichMin, int ichLim, int ws)
		{
			if (ws == 0)
				ws = WritingSystemOf(tss, ichMin, ichLim);
			return ichLim > ichMin && ws == cache.DefaultVernWs;
		}

		/// <summary>The word cut from <paramref name="tss"/>, with its runs: the form Add to
		/// Dictionary offers.</summary>
		public static ITsString FormOf(ITsString tss, int ichMin, int ichLim)
		{
			var builder = tss.GetBldr();
			if (ichLim < builder.Length)
				builder.Replace(ichLim, builder.Length, null, null);
			if (ichMin > 0)
				builder.Replace(0, ichMin, null, null);
			return builder.GetString();
		}
	}
}
