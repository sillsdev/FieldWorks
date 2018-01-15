// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.Controls;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// this just saves the target field
	/// </summary>
	internal sealed class DeleteTabPageSettings : BulkEditTabPageSettings
	{
		/// <summary />
		protected override int ExpectedTab => (int) BulkEditBarTabs.Delete;

		/// <summary>
		///
		/// </summary>
		protected override FwOverrideComboBox TargetComboForTab => m_bulkEditBar.DeleteWhatCombo;

		/// <summary>
		/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
		/// during initialization, since they do not fire automatically until after everything is setup.
		/// </summary>
		protected override void InvokeTargetComboSelectedIndexChanged()
		{
			m_bulkEditBar.m_deleteWhatCombo_SelectedIndexChanged(this, EventArgs.Empty);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bulkEditBar"></param>
		protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
		{
			bulkEditBar.InitDeleteTab();
			base.SetupBulkEditBarTab(bulkEditBar);
		}
	}
}