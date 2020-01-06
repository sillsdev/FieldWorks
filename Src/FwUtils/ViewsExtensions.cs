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
			int ichMin;
			int ichLim;
			int hvoDummy;
			int tagDummy;
			int wsDummy;
			ITsString tssDummy;
			GetWordLimitsOfSelection(me, out ichMin, out ichLim, out hvoDummy, out tagDummy, out wsDummy, out tssDummy);
			return ichLim > ichMin;
		}

		public static bool CanInsert(this IVwSelection me, LcmCache cache)
		{
			var enabled = false;
			// Enable the command if the selection exists, we actually have a word, and it's in
			// the default vernacular writing system.
			int ichMin;
			int ichLim;
			int hvoDummy;
			int tagDummy;
			int ws;
			ITsString tss;
			me.GetWordLimitsOfSelection(out ichMin, out ichLim, out hvoDummy, out tagDummy, out ws, out tss);
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
			bool fAssocPrev;
			wordsel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
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
			ITsString tss;
			bool fAssocPrev;
			wordsel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
			return ichLim > 0;
		}
	}
}