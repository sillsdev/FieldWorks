// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// this just saves the target field
	/// </summary>
	internal sealed class BulkReplaceTabPageSettings : BulkEditTabPageSettings
	{
		/// <summary />
		protected override void SaveSettings(BulkEditBar bulkEditBar)
		{
			base.SaveSettings(bulkEditBar);

			// now temporarily save some nonserializable objects so that they will
			// persist for the duration of the app, but not after closing the app.
			// 1) the Find & Replace pattern
			var keyFindPattern = BuildFindPatternKey(bulkEditBar);
			// store the Replace string into the Pattern
			m_bulkEditBar.Pattern.ReplaceWith = m_bulkEditBar.TssReplace;
			var patternSettings = new VwPatternSerializableSettings(m_bulkEditBar.Pattern);
			var patternAsXml = XmlSerializationHelper.SerializeToString(patternSettings);
			bulkEditBar.PropertyTable.SetProperty(keyFindPattern, patternAsXml, true, false);
		}

		/// <summary>
		/// Check that we've changed to BulkEditBar to ExpectedTab,
		/// and then set BulkEditBar to those tab settings
		/// </summary>
		protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
		{
			bulkEditBar.InitFindReplaceTab();
			base.SetupBulkEditBarTab(bulkEditBar);

			// now setup nonserializable objects
			bulkEditBar.SetupNonserializableObjects(Pattern);
		}

		/// <summary />
		protected override FwOverrideComboBox TargetComboForTab => m_bulkEditBar.FindReplaceTargetCombo;

		/// <summary>
		/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
		/// during initialization, since they do not fire automatically until after everything is setup.
		/// </summary>
		protected override void InvokeTargetComboSelectedIndexChanged()
		{
			m_bulkEditBar.m_findReplaceTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
		}

		private static string BuildFindPatternKey(BulkEditBar bulkEditBar)
		{
			var toolId = GetBulkEditBarToolId(bulkEditBar);
			var currentTabPageName = GetCurrentTabPageName(bulkEditBar);
			var keyFindPattern = $"{toolId}_{currentTabPageName}_FindAndReplacePattern";
			return keyFindPattern;
		}

		#region NonSerializable properties

		IVwPattern m_pattern;

		internal IVwPattern Pattern
		{
			get
			{
				if (m_pattern != null || !CanLoadFromBulkEditBar())
				{
					return m_pattern;
				}
				// first see if we can load the value from BulkEditBar
				if (m_bulkEditBar.Pattern != null)
				{
					m_pattern = m_bulkEditBar.Pattern;
				}
				else
				{
					// next see if we can restore the pattern from deserializing settings stored in the property table.
					var patternAsXml = m_bulkEditBar.PropertyTable.GetValue<string>(BuildFindPatternKey(m_bulkEditBar));
					var settings = (VwPatternSerializableSettings) SIL.FieldWorks.Common.FwUtils.XmlSerializationHelper.DeserializeXmlString(patternAsXml, typeof(VwPatternSerializableSettings));
					if (settings != null)
					{
						m_pattern = settings.NewPattern;
					}
					if (m_pattern == null)
					{
						m_pattern = VwPatternClass.Create();
					}
				}
				return m_pattern;
			}
		}
		#endregion NonSerializable properties
	}
}