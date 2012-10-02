using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FdoUi;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
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

		public bool OnDisplayMappingSetFeatures(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnMappingSetFeatures(object args)
		{
			CheckDisposed();
			AffixRuleFormulaControl.SetMappingFeatures();
			return true;
		}

		public bool OnDisplayMappingSetNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnMappingSetNaturalClass(object args)
		{
			CheckDisposed();
			AffixRuleFormulaControl.SetMappingNaturalClass();
			return true;
		}

		public bool OnDisplayMappingJumpToNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsNCIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnMappingJumpToNaturalClass(object args)
		{
			CheckDisposed();
			int hvo = RuleFormulaControl.CurrentHvo;
			IMoModifyFromInput mapping = new MoModifyFromInput(m_cache, hvo);
			Mediator.PostMessage("FollowLink", FwLink.Create("naturalClassedit",
				m_cache.GetGuidFromId(mapping.ModificationRAHvo), m_cache.ServerName, m_cache.DatabaseName));
			return true;
		}

		public virtual bool OnDisplayMappingJumpToPhoneme(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsPhonemeCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public virtual bool OnMappingJumpToPhoneme(object args)
		{
			CheckDisposed();
			int hvo = RuleFormulaControl.CurrentHvo;
			IMoInsertPhones mapping = new MoInsertPhones(m_cache, hvo);
			Mediator.PostMessage("FollowLink", FwLink.Create("phonemeEdit",
				m_cache.GetGuidFromId(mapping.ContentRS[0].Hvo), m_cache.ServerName, m_cache.DatabaseName));
			return true;
		}
	}
}
