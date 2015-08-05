// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.Utils;

namespace SIL.CoreImpl.Impls
{
	// TODO: Upgrade to .Net 4.5 and use MEF 2.0 instead of reflection. (Ok, MEF will use Reflection, but that is fine.)
	// TODO: MEF will need to create the AreaRepository, and thus the areas, on a per window scope.
	// TODO: MEF in .Net 4 can't do scoping beyond singleton or per call.
	/// <summary>
	/// Repository for IArea implementations.
	/// </summary>
	internal sealed class AreaRepository : IAreaRepository
	{
		private const string DefaultAreaMachineName = "lexicon";
		private readonly Dictionary<string, IArea> m_areas;

		internal AreaRepository()
		{
			m_areas = new Dictionary<string, IArea>();
			// Use Reflection (for now) to get all implementations of IArea.
			var coreImplAssembly = Assembly.GetExecutingAssembly();
			var baseDir = Path.GetDirectoryName(FileUtils.StripFilePrefix(coreImplAssembly.CodeBase));
			// We use 'installedAreaPluginAssemblies' for the var name, since theory has it the user can
			// select to not install some optional areas.
			var installedAreaPluginAssemblies = new List<Assembly>();
			installedAreaPluginAssemblies.AddRange(Directory
				.GetFiles(baseDir, "*AreaPlugin.dll", SearchOption.TopDirectoryOnly)
				.Select(areaPluginDllPathname => Assembly.LoadFrom(Path.Combine(baseDir, areaPluginDllPathname))));

			foreach (var pluginAssembly in installedAreaPluginAssemblies)
			{
				var areaTypes = (pluginAssembly.GetTypes().Where(typeof(IArea).IsAssignableFrom)).ToList();
				foreach (var areaType in areaTypes)
				{
					var constInfo = areaType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
					if (constInfo == null)
						continue; // It does need at least one public or non-public default constructor.
					var currentArea = (IArea) constInfo.Invoke(BindingFlags.Public | BindingFlags.NonPublic, null, null, null);
					m_areas.Add(currentArea.MachineName, currentArea);
				}
			}
		}

		#region Implementation of IAreaRepository

		/// <summary>
		/// Get the most recently persisted area, or the default area if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted area or the default area.</returns>
		public IArea GetPersistedOrDefaultArea(IPropertyTable propertyTable)
		{
			// The persisted area could be obsolete or simply not present,
			// so we'll use "lexicon", if the stored one cannot be found.
			// The "lexicon" area must be available, even if there are no other areas.
			return GetArea(propertyTable.GetValue("InitialArea", DefaultAreaMachineName)) ?? GetArea(DefaultAreaMachineName);
		}

		/// <summary>
		/// Get the IArea that has the machine friendly "Name" for <paramref name="machineName"/>.
		/// </summary>
		/// <param name="machineName"></param>
		/// <returns>The IArea for the given Name, or null if not in the system.</returns>
		public IArea GetArea(string machineName)
		{
			IArea retval;
			m_areas.TryGetValue(machineName, out retval);
			return retval; // May be null.
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
		/// <returns></returns>
		public IList<IArea> AllAreasInOrder()
		{
			var knownAreas = new List<string>
			{
				"lexicon",
				"textAndWords",
				"grammar",
				"notebook",
				"lists"
			};
			var retval = new List<IArea>(m_areas.Count);
			if (m_areas.ContainsKey(knownAreas[0]))
				retval.Add(m_areas[knownAreas[0]]);
			if (m_areas.ContainsKey(knownAreas[1]))
				retval.Add(m_areas[knownAreas[1]]);
			if (m_areas.ContainsKey(knownAreas[2]))
				retval.Add(m_areas[knownAreas[2]]);
			if (m_areas.ContainsKey(knownAreas[3]))
				retval.Add(m_areas[knownAreas[3]]);
			if (m_areas.ContainsKey(knownAreas[4]))
				retval.Add(m_areas[knownAreas[4]]);

			// Add user-defined areas in unspecified order, but after the fully supported areas.
			retval.AddRange(m_areas.Values.Where(userDefinedArea => !knownAreas.Contains(userDefinedArea.MachineName)));


			return retval;
		}

		#endregion
	}
}