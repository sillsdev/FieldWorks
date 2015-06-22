// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.Impls
{
	// TODO: Upgrade to .Net 4.5 and use MEF 2.0 instead of reflection. (Ok, MEF will use Reflection, but that is fine.)
	// TODO: MEF will need to create the AreaRepository, and thus the areas, on a per window scope.
	// TODO: MEF in .Net 4 can't do scoping beyond singleton or per call.
	/// <summary>
	/// Repository for IArea implementations.
	/// </summary>
	internal sealed class AreaRepository : IAreaRepository
	{
		private readonly Dictionary<string, IArea> m_areas;

		internal AreaRepository()
		{
			m_areas = new Dictionary<string, IArea>();
			// Use Reflection (for now) to get all implementations of IArea.
			var frameworkAssembly = Assembly.GetExecutingAssembly();
			var baseDir = Path.GetDirectoryName(FileUtils.StripFilePrefix(frameworkAssembly.CodeBase));
			// We use 'installedAreaPluginAssemblies' for the var name, since thoery has it the user can
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
					m_areas.Add(currentArea.Name, currentArea);
				}
			}
		}

		#region Implementation of IAreaRepository

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

		#endregion
	}
}