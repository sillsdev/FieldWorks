// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary>
	/// IArea implementation for the grammar area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class GrammarArea : IArea
	{
		[ImportMany(AreaServices.GrammarAreaMachineName)]
		private IEnumerable<ITool> _myTools;
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";

		[Import]
		private IPropertyTable _propertyTable;
		private Dictionary<string, ITool> _dictionaryOfAllTools;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by the active tool,
			// and any of the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveAreaHandlers();
			var activeTool = ActiveTool;
			ActiveTool = null;
			activeTool?.Deactivate(majorFlexComponentParameters);
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_propertyTable.SetDefault(PropertyNameForToolName, AreaServices.GrammarAreaDefaultToolMachineName, true);
			// Do nothing registration, but required, before a grammar tool can be registered.
			majorFlexComponentParameters.UiWidgetController.AddHandlers(new AreaUiWidgetParameterObject(this));
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
			_propertyTable.SetProperty(AreaServices.InitialArea, MachineName, true, settingsGroup: SettingsGroup.LocalSettings);

			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.GrammarAreaMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.GrammarAreaUiName);

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultTool => _dictionaryOfAllTools.Values.First(tool => tool.MachineName == _propertyTable.GetValue(PropertyNameForToolName, AreaServices.GrammarAreaDefaultToolMachineName));

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IReadOnlyDictionary<string, ITool> AllToolsInOrder
		{
			get
			{
				if (_dictionaryOfAllTools == null)
				{
					_dictionaryOfAllTools = new Dictionary<string, ITool>();
					var myBuiltinToolsInOrder = new List<string>
					{
						AreaServices.PosEditMachineName,
						AreaServices.CategoryBrowseMachineName,
						AreaServices.CompoundRuleAdvancedEditMachineName,
						AreaServices.PhonemeEditMachineName,
						AreaServices.PhonologicalFeaturesAdvancedEditMachineName,
						AreaServices.BulkEditPhonemesMachineName,
						AreaServices.NaturalClassEditMachineName,
						AreaServices.EnvironmentEditMachineName,
						AreaServices.PhonologicalRuleEditMachineName,
						AreaServices.AdhocCoprohibitionRuleEditMachineName,
						AreaServices.FeaturesAdvancedEditMachineName,
						AreaServices.ProdRestrictEditMachineName,
						AreaServices.GrammarSketchMachineName,
						AreaServices.LexiconProblemsMachineName
					};
					foreach (var toolName in myBuiltinToolsInOrder)
					{
						var currentBuiltinTool = _myTools.First(tool => tool.MachineName == toolName);
						_dictionaryOfAllTools.Add(currentBuiltinTool.UiName, currentBuiltinTool);
					}
					// Add user-defined tools in unspecified order, but after the fully supported tools.
					foreach (var userDefinedTool in _myTools.Where(tool => !myBuiltinToolsInOrder.Contains(tool.MachineName)))
					{
						_dictionaryOfAllTools.Add(userDefinedTool.UiName, userDefinedTool);
					}
				}
				return _dictionaryOfAllTools;
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Grammar.ToBitmap();

		/// <summary>
		/// Set the active tool for the area, or null, if no tool is active.
		/// </summary>
		public ITool ActiveTool { get; set; }

		#endregion
	}
}