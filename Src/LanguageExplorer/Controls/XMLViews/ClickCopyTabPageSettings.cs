// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.Controls;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal sealed class ClickCopyTabPageSettings : BulkEditTabPageSettings
	{
		string m_copyMode = string.Empty;
		string m_nonEmptyTargetMode = string.Empty;
		string m_nonEmptyTargetSeparator = string.Empty;

		/// <summary />
		protected override int ExpectedTab => (int) BulkEditBarTabs.ClickCopy;

		/// <summary>
		/// the target combo for a particular tab page.
		/// </summary>
		protected override FwOverrideComboBox TargetComboForTab => m_bulkEditBar.ClickCopyTargetCombo;

		private enum SourceCopyOptions
		{
			CopyWord = 0,
			StringReorderedAtClicked
		}

		/// <summary />
		private string SourceCopyMode
		{
			get
			{
				if (!string.IsNullOrEmpty(m_copyMode) || !CanLoadFromBulkEditBar())
				{
					return m_copyMode ?? (m_copyMode = string.Empty);
				}
				if (m_bulkEditBar.ClickCopyWordButton.Checked)
				{
					m_copyMode = SourceCopyOptions.CopyWord.ToString();
				}
				else if (m_bulkEditBar.ClickCopyReorderButton.Checked)
				{
					m_copyMode = SourceCopyOptions.StringReorderedAtClicked.ToString();
				}

				return m_copyMode ?? (m_copyMode = string.Empty);
			}
		}

		/// <summary />
		private string NonEmptyTargetWriteMode
		{
			get
			{
				if (!string.IsNullOrEmpty(m_nonEmptyTargetMode) || !CanLoadFromBulkEditBar())
				{
					return m_nonEmptyTargetMode ?? (m_nonEmptyTargetMode = string.Empty);
				}
				if (m_bulkEditBar.ClickCopyAppendButton.Checked)
				{
					m_nonEmptyTargetMode = NonEmptyTargetOptions.Append.ToString();
				}
				else if (m_bulkEditBar.ClickCopyOverwriteButton.Checked)
				{
					m_nonEmptyTargetMode = NonEmptyTargetOptions.Overwrite.ToString();
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
					m_nonEmptyTargetSeparator = m_bulkEditBar.ClickCopySepBox.Text;
				}
				return m_nonEmptyTargetSeparator ?? (m_nonEmptyTargetSeparator = string.Empty);
			}
		}

		/// <summary />
		protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
		{
			bulkEditBar.InitClickCopyTab();
			base.SetupBulkEditBarTab(bulkEditBar);
			var sourceCopyMode = (SourceCopyOptions) Enum.Parse(typeof(SourceCopyOptions), SourceCopyMode);
			switch (sourceCopyMode)
			{
				case SourceCopyOptions.StringReorderedAtClicked:
					m_bulkEditBar.ClickCopyReorderButton.Checked = true;
					break;
				case SourceCopyOptions.CopyWord:
				default:
					m_bulkEditBar.ClickCopyWordButton.Checked = true;
					break;
			}

			var nonEmptyTargetMode = (NonEmptyTargetOptions) Enum.Parse(typeof(NonEmptyTargetOptions), NonEmptyTargetWriteMode);
			switch (nonEmptyTargetMode)
			{
				case NonEmptyTargetOptions.Overwrite:
					m_bulkEditBar.ClickCopyOverwriteButton.Checked = true;
					break;
				case NonEmptyTargetOptions.Append:
				default:
					m_bulkEditBar.ClickCopyAppendButton.Checked = true;
					break;
			}
			m_bulkEditBar.ClickCopySepBox.Text = NonEmptyTargetSeparator;
		}

		/// <summary>
		/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
		/// during initialization, since they do not fire automatically until after everything is setup.
		/// </summary>
		protected override void InvokeTargetComboSelectedIndexChanged()
		{
			m_bulkEditBar.m_clickCopyTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
		}

		/// <summary>
		/// Update Preview/Clear and Apply Button states.
		/// </summary>
		protected override void SetupApplyPreviewButtons()
		{
			m_bulkEditBar.SetupApplyPreviewButtons(false, false);
		}

		/// <summary>
		/// when switching contexts, we should commit any pending click copy changes.
		/// </summary>
		protected override void SaveSettings(BulkEditBar bulkEditBar)
		{
			// first commit any pending changes.
			// switching from click copy, so commit any pending changes.
			m_bulkEditBar.CommitClickChanges(this, EventArgs.Empty);
			base.SaveSettings(bulkEditBar);
		}
	}
}