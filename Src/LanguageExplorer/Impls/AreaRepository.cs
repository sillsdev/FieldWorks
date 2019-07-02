// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using LanguageExplorer.Areas;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Repository for IArea implementations.
	/// </summary>
	[Export(typeof(IAreaRepository))]
	internal sealed class AreaRepository : IAreaRepository
	{
		private const string DefaultAreaMachineName = AreaServices.InitialAreaMachineName;
		[ImportMany]
		private IEnumerable<IArea> _areas;
		[Import]
		private IPropertyTable _propertyTable;
		private Dictionary<string, IArea> _dictionaryOfAllAreas;

		#region Implementation of IAreaRepository

		/// <summary>
		/// Get the most recently persisted area, or the default area if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted area or the default area.</returns>
		public IArea PersistedOrDefaultArea => GetArea(_propertyTable.GetValue(AreaServices.InitialArea, DefaultAreaMachineName, SettingsGroup.LocalSettings));

		/// <summary>
		/// Get the IArea that has the machine friendly "Name" for <paramref name="machineName"/>.
		/// </summary>
		/// <returns>The IArea for the given Name, or null if not in the system.</returns>
		public IArea GetArea(string machineName)
		{
			return _areas.FirstOrDefault(area => area.MachineName == machineName); // May be null.
		}

		/// <summary>
		/// Return all areas in this order (if installed):
		/// Lexicon - required
		/// Text and Words
		/// Grammar
		/// Notebook
		/// Lists
		/// User defined areas (unspecified order, but after the fully supported areas)
		/// </summary>
		/// <returns>The areas in correct order for display in sidebar.</returns>
		public IReadOnlyDictionary<string, IArea> AllAreasInOrder
		{
			get
			{
				if (_dictionaryOfAllAreas == null)
				{
					_dictionaryOfAllAreas = new Dictionary<string, IArea>();
					var myBuiltinAreasInOrder = new List<string>
					{
						AreaServices.LexiconAreaMachineName,
						AreaServices.TextAndWordsAreaMachineName,
						AreaServices.GrammarAreaMachineName,
						AreaServices.NotebookAreaMachineName,
						AreaServices.ListsAreaMachineName
					};
					foreach (var areaMachineName in myBuiltinAreasInOrder)
					{
						var currentBuiltinArea = _areas.First(area => area.MachineName == areaMachineName);
						_dictionaryOfAllAreas.Add(currentBuiltinArea.UiName, currentBuiltinArea);
					}
					// Add user-defined areas in unspecified order, but after the fully supported areas.
					foreach (var userDefinedArea in _areas.Where(area => !myBuiltinAreasInOrder.Contains(area.MachineName)))
					{
						_dictionaryOfAllAreas.Add(userDefinedArea.UiName, userDefinedArea);
					}
				}
				return _dictionaryOfAllAreas;
			}
		}

		#endregion
	}
}