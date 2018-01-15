// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.Controls;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Same as BulkCopy except for the Process combo box and different controls.
	/// </summary>
	internal sealed class ProcessTabPageSettings : BulkCopyTabPageSettings
	{
		string m_process = string.Empty;

		/// <summary />
		protected override int ExpectedTab => (int) BulkEditBarTabs.Process;

		/// <summary>
		///
		/// </summary>
		protected override FwOverrideComboBox SourceCombo => m_bulkEditBar.TransduceSourceCombo;

		/// <summary>
		///
		/// </summary>
		protected override NonEmptyTargetControl NonEmptyTargetControl => m_bulkEditBar.TrdNonEmptyTargetControl;

		/// <summary>
		/// the target combo for a particular tab page.
		/// </summary>
		protected override FwOverrideComboBox TargetComboForTab => m_bulkEditBar.TransduceTargetCombo;

		/// <summary />
		private string Process
		{
			get
			{
				if (string.IsNullOrEmpty(m_process) && CanLoadFromBulkEditBar() && m_bulkEditBar.TransduceProcessorCombo != null)
				{
					m_process = m_bulkEditBar.TransduceProcessorCombo.Text;
				}
				return m_process ?? (m_process = string.Empty);
			}
		}

		/// <summary>
		///
		/// </summary>
		protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
		{
			base.SetupBulkEditBarTab(bulkEditBar);

			// now handle the process combo.
			if (m_bulkEditBar.TransduceProcessorCombo != null)
			{
				m_bulkEditBar.TransduceProcessorCombo.Text = Process;
			}
		}

		/// <summary>
		/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
		/// during initialization, since they do not fire automatically until after everything is setup.
		/// </summary>
		protected override void InvokeTargetComboSelectedIndexChanged()
		{
			m_bulkEditBar.m_transduceTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bulkEditBar"></param>
		protected override void InitizializeTab(BulkEditBar bulkEditBar)
		{
			bulkEditBar.InitTransduce();
		}
	}
}