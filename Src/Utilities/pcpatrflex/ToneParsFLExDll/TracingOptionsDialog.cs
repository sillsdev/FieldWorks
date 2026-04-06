// Copyright (c) 2019-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.DisambiguateInFLExDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIL.ToneParsFLEx
{
	public partial class TracingOptionsDialog : Form
	{
		public TracingOptionsDialog()
		{
			InitializeComponent();
			cbSegmentParsingTrace.Checked = ToneParsInvokerOptions.Instance.SegmentParsingTrace;
			cbMorphemeLinkingTrace.Checked = ToneParsInvokerOptions.Instance.MorphemeLinkingTrace;
			cbMoraParsingTrace.Checked = ToneParsInvokerOptions.Instance.MoraParsingTrace;
			cbSyllableParsingTrace.Checked = ToneParsInvokerOptions.Instance.SyllableParsingTrace;
			cbTBUAssignmentTrace.Checked = ToneParsInvokerOptions.Instance.TBUAssignmentTrace;
			cbMorphemeToneAssignmentTrace.Checked = ToneParsInvokerOptions
				.Instance
				.MorphemeToneAssignmentTrace;
			cbDomainAssignmentTrace.Checked = ToneParsInvokerOptions.Instance.DomainAssignmentTrace;
			cbTierAssignmentTrace.Checked = ToneParsInvokerOptions.Instance.TierAssignmentTrace;
			cbRuleTrace.Checked = ToneParsInvokerOptions.Instance.RuleTrace;
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			ToneParsInvokerOptions.Instance.SegmentParsingTrace = cbSegmentParsingTrace.Checked;
			ToneParsInvokerOptions.Instance.MorphemeLinkingTrace = cbMorphemeLinkingTrace.Checked;
			ToneParsInvokerOptions.Instance.MoraParsingTrace = cbMoraParsingTrace.Checked;
			ToneParsInvokerOptions.Instance.SyllableParsingTrace = cbSyllableParsingTrace.Checked;
			ToneParsInvokerOptions.Instance.TBUAssignmentTrace = cbTBUAssignmentTrace.Checked;
			ToneParsInvokerOptions.Instance.MorphemeToneAssignmentTrace =
				cbMorphemeToneAssignmentTrace.Checked;
			ToneParsInvokerOptions.Instance.DomainAssignmentTrace = cbDomainAssignmentTrace.Checked;
			ToneParsInvokerOptions.Instance.TierAssignmentTrace = cbTierAssignmentTrace.Checked;
			ToneParsInvokerOptions.Instance.RuleTrace = cbRuleTrace.Checked;
		}
	}
}
