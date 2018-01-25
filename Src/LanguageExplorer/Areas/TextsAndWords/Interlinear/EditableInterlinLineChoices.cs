// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
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
		/// <param name="index"></param>
		/// <returns></returns>
		public override bool OkToChangeWritingSystem(int index)
		{
			var flid = this[index].Flid;
			if (flid == kflidLexPos)
			{
				return true; // We now allow the user to select the ws for Lex Grammatical Info.
			}
			if (flid != kflidWord && flid != kflidMorphemes && flid != kflidLexEntries)
			{
				return true; // Not a field we care about.
			}
			if (this[index].WritingSystem != m_wsDefVern)
			{
				return true; // Not a Ws we care about.
			}
			if (IndexOf(flid) != index)
			{
				return true; // Not the instance we care about.
			}
			return false;
		}

		public override bool OkToRemove(InterlinLineSpec spec, out string message)
		{
			if (!base.OkToRemove(spec, out message))
			{
				return false;
			}
			if (spec.Flid == kflidWord && spec.WritingSystem == m_wsDefVern && ItemsWithFlids(new int[] {kflidWord}, new int[] {m_wsDefVern}).Count < 2)
			{
				message = ITextStrings.ksNeedWordLine;
				return false;
			}
			if (FindDependents(spec).Count > 0)
			{
				// Enhance JohnT: get the names and include them in the message.
				message = ITextStrings.ksHidesDependentLinesAlso;
				// OK to go ahead if the user wishes, return true.
			}
			return true;
		}


		/// <summary>
		/// Overridden to prevent removing the Words line and to remove dependents of the line being removed
		/// (after warning the user).
		/// </summary>
		public override void Remove(InterlinLineSpec spec)
		{
			var dependents = new List<InterlinLineSpec>();
			dependents = FindDependents(spec);
			foreach (var depSpec in dependents)
			{
				m_specs.Remove(depSpec);
			}
			base.Remove(spec);
		}

		private List<InterlinLineSpec> FindDependents(InterlinLineSpec spec)
		{
			var dependents = new List<InterlinLineSpec>();
			return dependents;
		}
	}
}