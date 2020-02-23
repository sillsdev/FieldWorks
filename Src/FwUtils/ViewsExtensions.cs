// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Add some extensions to some views related code.
	/// </summary>
	public static class ViewsExtensions
	{
		public static bool CanLookupLexicon(this IVwSelection me)
		{
			// Enable the command if the selection exists and we actually have a word.
			GetWordLimitsOfSelection(me, out var ichMin, out var ichLim, out _, out _, out _, out _);
			return ichLim > ichMin;
		}

		public static bool CanInsert(this IVwSelection me, LcmCache cache)
		{
			var enabled = false;
			// Enable the command if the selection exists, we actually have a word, and it's in
			// the default vernacular writing system.
			me.GetWordLimitsOfSelection(out var ichMin, out var ichLim, out _, out _, out var ws, out var tss);
			if (ws == 0)
			{
				ws = tss.GetWsFromString(ichMin, ichLim);
			}
			if (ichLim > ichMin && ws == cache.DefaultVernWs)
			{
				enabled = true;
			}
			return enabled;
		}

		public static void GetWordLimitsOfSelection(this IVwSelection me, out int ichMin, out int ichLim, out int hvo, out int tag, out int ws, out ITsString tss)
		{
			ichMin = ichLim = hvo = tag = ws = 0;
			tss = null;
			var sel2 = me.EndBeforeAnchor ? me.EndPoint(true) : me.EndPoint(false);
			var  wordsel = sel2?.GrowToWord();
			if (wordsel == null)
			{
				return;
			}
			wordsel.TextSelInfo(false, out tss, out ichMin, out _, out hvo, out tag, out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out _, out hvo, out tag, out ws);
		}

		public static bool GetSelectedWordPos(this IVwSelection me, out int hvo, out int tag, out int ws, out int ichMin, out int ichLim)
		{
			var sel2 = me.EndBeforeAnchor ? me.EndPoint(true) : me.EndPoint(false);
			var wordsel = sel2?.GrowToWord();
			if (wordsel == null)
			{
				hvo = tag = ws = 0;
				ichMin = ichLim = -1;
				return false;
			}
			wordsel.TextSelInfo(false, out _, out ichMin, out _, out hvo, out tag, out ws);
			wordsel.TextSelInfo(true, out _, out ichLim, out _, out hvo, out tag, out ws);
			return ichLim > 0;
		}
	}
}