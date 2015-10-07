// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	public class AffixRuleFormulaSlice : RuleFormulaSlice
	{
		public AffixRuleFormulaSlice()
		{
		}

		AffixRuleFormulaControl AffixRuleFormulaControl
		{
			get
			{
				return Control as AffixRuleFormulaControl;
			}
		}

		public override void FinishInit()
		{
			CheckDisposed();
			Control = new AffixRuleFormulaControl(m_configurationNode);
		}

#if RANDYTODO
		public bool OnDisplayMappingSetFeatures(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnMappingSetFeatures(object args)
		{
			CheckDisposed();
			AffixRuleFormulaControl.SetMappingFeatures();
			return true;
		}

#if RANDYTODO
		public bool OnDisplayMappingSetNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnMappingSetNaturalClass(object args)
		{
			CheckDisposed();
			AffixRuleFormulaControl.SetMappingNaturalClass();
			return true;
		}

#if RANDYTODO
		public bool OnDisplayMappingJumpToNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsNCIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnMappingJumpToNaturalClass(object args)
		{
			CheckDisposed();
			var mapping = (IMoModifyFromInput)RuleFormulaControl.CurrentObject;
			var commands = new List<string>
										{
											"AboutToFollowLink",
											"FollowLink"
										};
			var parms = new List<object>
										{
											null,
											new FwLinkArgs("naturalClassEdit", mapping.ModificationRA.Guid)
										};
			Publisher.Publish(commands, parms);
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayMappingJumpToPhoneme(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsPhonemeCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public virtual bool OnMappingJumpToPhoneme(object args)
		{
			CheckDisposed();
			var mapping = (IMoInsertPhones)RuleFormulaControl.CurrentObject;
			var commands = new List<string>
										{
											"AboutToFollowLink",
											"FollowLink"
										};
			var parms = new List<object>
										{
											null,
											new FwLinkArgs("phonemeEdit", mapping.ContentRS[0].Guid)
										};
			Publisher.Publish(commands, parms);
			return true;
		}
	}
}
