// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.Controls;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal class BulkCopyTabPageSettings : BulkEditTabPageSettings
	{
		string m_sourceField = string.Empty;
		string m_nonEmptyTargetMode = string.Empty;
		string m_nonEmptyTargetSeparator = string.Empty;

		/// <summary />
		protected override int ExpectedTab => (int) BulkEditBarTabs.BulkCopy;

		/// <summary />
		private string SourceField
		{
			get
			{
				if (string.IsNullOrEmpty(m_sourceField) && CanLoadFromBulkEditBar() && SourceCombo != null)
				{
					m_sourceField = SourceCombo.Text;
				}
				return m_sourceField ?? (m_sourceField = string.Empty);
			}
		}

		/// <summary />
		protected virtual FwOverrideComboBox SourceCombo => m_bulkEditBar.BulkCopySourceCombo;

		/// <summary />
		protected virtual NonEmptyTargetControl NonEmptyTargetControl => m_bulkEditBar.BcNonEmptyTargetControl;

		/// <summary />
		private string NonEmptyTargetWriteMode
		{
			get
			{
				if (string.IsNullOrEmpty(m_nonEmptyTargetMode) && CanLoadFromBulkEditBar())
				{
					m_nonEmptyTargetMode = NonEmptyTargetControl.NonEmptyMode.ToString();
				}
				return m_nonEmptyTargetMode ?? (m_nonEmptyTargetMode = string.Empty);
			}
		}

		/// <summary />
		private string NonEmptyTargetSeparator
		{
			get
			{
				if (string.IsNullOrEmpty(m_nonEmptyTargetSeparator) && CanLoadFromBulkEditBar())
				{
					m_nonEmptyTargetSeparator = NonEmptyTargetControl.Separator;
				}
				return m_nonEmptyTargetSeparator ?? (m_nonEmptyTargetSeparator = string.Empty);
			}
		}

		/// <summary />
		protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
		{
			InitizializeTab(bulkEditBar);
			base.SetupBulkEditBarTab(bulkEditBar);
			if (SourceCombo != null)
			{
				SourceCombo.Text = SourceField;
				if (SourceCombo.SelectedIndex == -1)
				{
					// by default select the first item.
					if (SourceCombo.Items.Count > 0)
					{
						SourceCombo.SelectedIndex = 0;
					}
				}
			}
			NonEmptyTargetControl.NonEmptyMode = (NonEmptyTargetOptions) Enum.Parse(typeof(NonEmptyTargetOptions), NonEmptyTargetWriteMode);
			NonEmptyTargetControl.Separator = NonEmptyTargetSeparator;
		}

		/// <summary>
		/// the target combo for a particular tab page.
		/// </summary>
		protected override FwOverrideComboBox TargetComboForTab => m_bulkEditBar.BulkCopyTargetCombo;

		/// <summary>
		/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
		/// during initialization, since they do not fire automatically until after everything is setup.
		/// </summary>
		protected override void InvokeTargetComboSelectedIndexChanged()
		{
			m_bulkEditBar.m_bulkCopyTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
		}

		/// <summary />
		protected virtual void InitizializeTab(BulkEditBar bulkEditBar)
		{
			bulkEditBar.InitBulkCopyTab();
		}
	}
}