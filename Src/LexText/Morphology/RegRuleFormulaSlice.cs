using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class represents a <c>PhRegularRule</c> slice.
	/// </summary>
	public class RegRuleFormulaSlice : RuleFormulaSlice
	{
		public RegRuleFormulaSlice()
		{
		}

		public override void FinishInit()
		{
			CheckDisposed();
			Control = new RegRuleFormulaControl(m_configurationNode);
		}

		RegRuleFormulaControl RegRuleFormulaControl
		{
			get
			{
				return Control as RegRuleFormulaControl;
			}
		}

		public bool OnDisplayContextSetOccurrence(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = RegRuleFormulaControl.CanModifyContextOccurrence;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnContextSetOccurrence(object args)
		{
			CheckDisposed();

			XCore.Command cmd = args as XCore.Command;
			if (cmd.Parameters.Count > 0)
			{
				string minStr = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "min");
				string maxStr = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "max");
				int min = Int32.Parse(minStr);
				int max = Int32.Parse(maxStr);
				RegRuleFormulaControl.SetContextOccurrence(min, max);
			}
			else
			{
				int min, max;
				RegRuleFormulaControl.GetContextOccurrence(out min, out max);
				using (ContextOccurrenceDlg dlg = new ContextOccurrenceDlg(min, max))
				{
					if (dlg.ShowDialog((XCore.XWindow)Mediator.PropertyTable.GetValue("window")) == DialogResult.OK)
						RegRuleFormulaControl.SetContextOccurrence(dlg.Minimum, dlg.Maximum);
				}
			}
			return true;
		}

		public bool OnDisplayContextSetVariables(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = RuleFormulaControl.IsFeatsNCContextCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnContextSetVariables(object args)
		{
			CheckDisposed();
			RegRuleFormulaControl.SetContextVariables();
			return true;
		}
	}
}
