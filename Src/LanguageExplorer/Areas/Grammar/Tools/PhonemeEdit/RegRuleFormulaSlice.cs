// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace LanguageExplorer.Areas.Grammar.Tools.PhonemeEdit
{
	/// <summary>
	/// This class represents a <c>PhRegularRule</c> slice.
	/// </summary>
	internal sealed class RegRuleFormulaSlice : RuleFormulaSlice
	{
		internal RegRuleFormulaSlice(ISharedEventHandlers sharedEventHandlers)
			: base(sharedEventHandlers)
		{
		}

		public override void FinishInit()
		{
			Control = new RegRuleFormulaControl(_sharedEventHandlers, ConfigurationNode);
		}

		private RegRuleFormulaControl RegRuleFormulaControl => Control as RegRuleFormulaControl;

#if RANDYTODO
		public bool OnDisplayContextSetOccurrence(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			bool enable = RegRuleFormulaControl.CanModifyContextOccurrence;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnContextSetOccurrence(object args)
		{
			var cmd = (XCore.Command) args;
			if (cmd.Parameters.Count > 0)
			{
				string minStr = XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "min");
				string maxStr = XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "max");
				int min = Int32.Parse(minStr);
				int max = Int32.Parse(maxStr);
				RegRuleFormulaControl.SetContextOccurrence(min, max);
			}
			else
			{
				int min, max;
				RegRuleFormulaControl.GetContextOccurrence(out min, out max);
				using (var dlg = new OccurrenceDlg(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), min, max, false))
				{
					if (dlg.ShowDialog(m_propertyTable.GetValue<IFwMainWnd>("window")) == DialogResult.OK)
						RegRuleFormulaControl.SetContextOccurrence(dlg.Minimum, dlg.Maximum);
				}
			}
			return true;
		}

		public bool OnDisplayContextSetVariables(object commandObject, ref UIItemDisplayProperties display)
		{
			bool enable = RuleFormulaControl.IsFeatsNCContextCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif
	}
}