// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	internal sealed class AffixRuleFormulaSlice : RuleFormulaSlice
	{
		public AffixRuleFormulaSlice()
		{
		}

		AffixRuleFormulaControl AffixRuleFormulaControl => (AffixRuleFormulaControl)Control;

		public override void FinishInit()
		{
			Control = new AffixRuleFormulaControl(ConfigurationNode);
		}

#if RANDYTODO
		public bool OnDisplayMappingSetFeatures(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnMappingSetFeatures(object args)
		{
			AffixRuleFormulaControl.SetMappingFeatures();
			return true;
		}

#if RANDYTODO
		public bool OnDisplayMappingSetNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnMappingSetNaturalClass(object args)
		{
			AffixRuleFormulaControl.SetMappingNaturalClass();
			return true;
		}

#if RANDYTODO
		public bool OnDisplayMappingJumpToNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			bool enable = AffixRuleFormulaControl.IsNCIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnMappingJumpToNaturalClass(object args)
		{
			var mapping = (IMoModifyFromInput)RuleFormulaControl.CurrentObject;
			LinkHandler.JumpToTool(Publisher, new FwLinkArgs(AreaServices.NaturalClassEditMachineName, mapping.ModificationRA.Guid));
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayMappingJumpToPhoneme(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			bool enable = AffixRuleFormulaControl.IsPhonemeCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnMappingJumpToPhoneme(object args)
		{
			var mapping = (IMoInsertPhones)RuleFormulaControl.CurrentObject;
			LinkHandler.JumpToTool(Publisher, new FwLinkArgs(AreaServices.PhonemeEditMachineName, mapping.ContentRS[0].Guid));
			return true;
		}
	}
}