// Copyright (c) 2015-2018 SIL International
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
		private IEnumerable<IArea> m_areas;
		[Import]
		private IPropertyTable _propertyTable;

		#region Implementation of IAreaRepository

		/// <summary>
		/// Get the most recently persisted area, or the default area if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted area or the default area.</returns>
		public IArea PersistedOrDefaultArea => GetArea(_propertyTable.GetValue(AreaServices.InitialArea, SettingsGroup.LocalSettings, DefaultAreaMachineName));

		/// <summary>
		/// Get the IArea that has the machine friendly "Name" for <paramref name="machineName"/>.
		/// </summary>
		/// <returns>The IArea for the given Name, or null if not in the system.</returns>
		public IArea GetArea(string machineName)
		{
			return m_areas.FirstOrDefault(area => area.MachineName == machineName); // May be null.
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
		/// <returns>The areas in correct order for display in sidbar.</returns>
		public IList<IArea> AllAreasInOrder
		{
			get
			{
				var knownAreas = new List<string>
				{
					AreaServices.LexiconAreaMachineName,
					AreaServices.TextAndWordsAreaMachineName,
					AreaServices.GrammarAreaMachineName,
					AreaServices.NotebookAreaMachineName,
					AreaServices.ListsAreaMachineName
				};
				var retval = new List<IArea>(knownAreas.Select(knownAreaName => GetArea(knownAreaName)));

				// Add user-defined areas in unspecified order, but after the fully supported areas.
				retval.AddRange(m_areas.Where(userDefinedArea => !knownAreas.Contains(userDefinedArea.MachineName)));

				return retval;
			}
		}

		#endregion
	}
}