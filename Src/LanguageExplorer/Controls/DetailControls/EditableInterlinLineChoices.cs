// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This is a subclass of InterlinLineChoices used for editable interlinear views. It has more
	/// restrictions on allowed orders.
	/// </summary>
	internal sealed class EditableInterlinLineChoices : InterlinLineChoices
	{
		internal EditableInterlinLineChoices(ILangProject proj, int defaultVernacularWs, int defaultAnalysisWs)
			: base(proj, defaultVernacularWs, defaultAnalysisWs)
		{
		}

		internal new static InterlinLineChoices DefaultChoices(ILangProject proj, int vern, int analysis)
		{
			InterlinLineChoices result = new EditableInterlinLineChoices(proj, vern, analysis);
			result.SetStandardState();
			return result;
		}

		internal override bool OkToRemove(InterlinLineSpec spec, out string message)
		{
			if (!base.OkToRemove(spec, out message))
			{
				return false;
			}
			if (spec.Flid == kflidWord && spec.WritingSystem == m_wsDefVern && ItemsWithFlids(new int[] { kflidWord }, new int[] { m_wsDefVern }).Count < 2)
			{
				message = LanguageExplorerResources.ksNeedWordLine;
				return false;
			}
			return true;
		}
	}
}