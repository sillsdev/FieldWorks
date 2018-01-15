// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.Controls;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal sealed class ListChoiceTabPageSettings : BulkEditTabPageSettings
	{

		string m_changeTo = string.Empty;

		/// <summary />
		protected override int ExpectedTab => (int) BulkEditBarTabs.ListChoice;

		/// <summary />
		private string ChangeTo
		{
			get
			{
				if (string.IsNullOrEmpty(m_changeTo) && CanLoadFromBulkEditBar() && m_bulkEditBar.ListChoiceControl != null)
				{
					m_changeTo = m_bulkEditBar.ListChoiceControl.Text;
				}
				return m_changeTo ?? (m_changeTo = string.Empty);
			}
		}

		/// <summary />
		protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
		{
			// first initialize the controls, since otherwise, we overwrite our selection.
			// now we can setup the target field
			m_bulkEditBar.InitListChoiceTab();

			base.SetupBulkEditBarTab(bulkEditBar);
			if (m_bulkEditBar.ListChoiceControl != null)
			{
				if (HasExpectedTargetSelected())
				{
					m_bulkEditBar.ListChoiceControl.Text = ChangeTo;
				}
				if (m_bulkEditBar.CurrentItem.BulkEditControl is ITextChangedNotification)
				{
					(m_bulkEditBar.CurrentItem.BulkEditControl as ITextChangedNotification).ControlTextChanged();
				}
				else
				{
					// couldn't restore target selection, so revert to defaults.
					// (LT-9940 default is ChangeTo, not "")
					m_bulkEditBar.ListChoiceControl.Text = ChangeTo;
				}
			}
			else
			{
				// at least show dummy control.
				m_bulkEditBar.ListChoiceChangeToCombo.Visible = true;
			}
		}

		/// <summary>
		/// the target combo for a particular tab page.
		/// </summary>
		protected override FwOverrideComboBox TargetComboForTab => m_bulkEditBar.ListChoiceTargetCombo;

		/// <summary>
		/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
		/// during initialization, since they do not fire automatically until after everything is setup.
		/// </summary>
		protected override void InvokeTargetComboSelectedIndexChanged()
		{
			m_bulkEditBar.m_listChoiceTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
		}

		/// <summary>
		/// Update Preview/Clear and Apply Button states.
		/// </summary>
		protected override void SetupApplyPreviewButtons()
		{
			base.SetupApplyPreviewButtons();
			m_bulkEditBar.EnablePreviewApplyForListChoice();
		}
	}
}