// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This is a subclass of InterlinLineChoices used for editable interlinear views. It has more
	/// restrictions on allowed orders.
	/// </summary>
	internal class EditableInterlinLineChoices : InterlinLineChoices
	{
		public EditableInterlinLineChoices(ILangProject proj, int defaultVernacularWs, int defaultAnalysisWs)
			: base(proj, defaultVernacularWs, defaultAnalysisWs)
		{
		}

		public new static InterlinLineChoices DefaultChoices(ILangProject proj, int vern, int analysis)
		{
			InterlinLineChoices result = new EditableInterlinLineChoices(proj, vern, analysis);
			result.SetStandardState();
			return result;
		}

		/// <summary>
		/// Answer true if it is OK to change the writing system of the specified field.
		/// This is not allowed if it is one of the special fields and is the first
		/// occurrence of the default writing system.
		/// </summary>
		public override bool OkToChangeWritingSystem(int index)
		{
			var flid = this[index].Flid;
			return flid == kflidLexPos || flid != kflidWord && flid != kflidMorphemes && flid != kflidLexEntries || this[index].WritingSystem != m_wsDefVern || IndexOf(flid) != index;
		}

		public override bool OkToRemove(InterlinLineSpec spec, out string message)
		{
			if (!base.OkToRemove(spec, out message))
			{
				return false;
			}
			if (spec.Flid == kflidWord && spec.WritingSystem == m_wsDefVern && ItemsWithFlids(new int[] { kflidWord }, new int[] { m_wsDefVern }).Count < 2)
			{
				message = ITextStrings.ksNeedWordLine;
				return false;
			}
			return true;
		}
	}
}