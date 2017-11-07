// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Lists
{
	[Export(AreaServices.ListsAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class ListsArea : IArea
	{
		[ImportMany(AreaServices.ListsAreaMachineName)]
		private IEnumerable<ITool> _myTools;
		private const string MyUiName = "Lists";
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		[Import]
		private IPropertyTable _propertyTable;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_propertyTable.SetDefault(PropertyNameForToolName, AreaServices.ListsAreaDefaultToolMachineName, SettingsGroup.LocalSettings, true, false);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			PersistedOrDefaultTool.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			PersistedOrDefaultTool.FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			_propertyTable.SetProperty(AreaServices.InitialArea, MachineName, SettingsGroup.LocalSettings, true, false);

			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.ListsAreaMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => MyUiName;
		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultTool => _myTools.First(tool => tool.MachineName == _propertyTable.GetValue(PropertyNameForToolName, AreaServices.ListsAreaDefaultToolMachineName));

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					AreaServices.DomainTypeEditMachineName,
					AreaServices.AnthroEditMachineName,
					AreaServices.ComplexEntryTypeEditMachineName,
					AreaServices.ConfidenceEditMachineName,
					AreaServices.ChartmarkEditMachineName,
					AreaServices.CharttempEditMachineName,
					AreaServices.EducationEditMachineName,
					AreaServices.RoleEditMachineName,
					AreaServices.FeatureTypesAdvancedEditMachineName,
					AreaServices.GenresEditMachineName,
					AreaServices.LexRefEditMachineName,
					AreaServices.LocationsEditMachineName,
					AreaServices.PublicationsEditMachineName,
					AreaServices.MorphTypeEditMachineName,
					AreaServices.PeopleEditMachineName,
					AreaServices.PositionsEditMachineName,
					AreaServices.RestrictionsEditMachineName,
					AreaServices.SemanticDomainEditMachineName,
					AreaServices.SenseTypeEditMachineName,
					AreaServices.StatusEditMachineName,
					AreaServices.TextMarkupTagsEditMachineName,
					AreaServices.TranslationTypeEditMachineName,
					AreaServices.UsageTypeEditMachineName,
					AreaServices.VariantEntryTypeEditMachineName,
					AreaServices.RecTypeEditMachineName,
					AreaServices.TimeOfDayEditMachineName,
					AreaServices.ReversalToolReversalIndexPOSMachineName
				};

#if RANDYTODO
				// TODO: Add user-defined tools in some kind of generic list area that can work with user-defined lists.
				// TODO: That generic list tools will *not* be located by reflection in a plugin assembly like all other tools,
				// TODO: but it/they will be created by this area, as/if needed for each user-defined tool.
#endif
				return myToolsInOrder.Select(toolName => _myTools.First(tool => tool.MachineName == toolName)).ToList();
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Lists.ToBitmap();

		#endregion
	}
}
