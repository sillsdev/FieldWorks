using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.Utils;

namespace SIL.CoreImpl.Impls
{
	// TODO: Upgrade to .Net 4.5 and use MEF 2.0 instead of reflection. (Ok, MEF will use Reflection, but that is fine.)
	// TODO: MEF will need to create the ToolRepository, and thus the tools, on a per window scope.
	// TODO: MEF in .Net 4 can't do scoping beyond singleton or per call.
	/// <summary>
	/// Repository for ITool implementations.
	/// </summary>
	internal sealed class ToolRepository : IToolRepository
	{
		private const string BasePersistedToolName = "ToolForAreaNamed_";
		private readonly Dictionary<string, ITool> m_tools = new Dictionary<string, ITool>();
		/// <summary>
		/// Constructor.
		/// </summary>
		internal ToolRepository()
		{
			// Use Reflection (for now) to get all implementations of ITool.
			var baseDir = DirectoryUtils.DirectoryOfExecutingAssembly();
			// We use 'installedToolPluginAssemblies' for the var name, since theory has it the user can
			// select to not install some optional tools.
			var installedToolPluginAssemblies = new List<Assembly>();
			installedToolPluginAssemblies.AddRange(Directory
				.GetFiles(baseDir, "*ToolPlugin.dll", SearchOption.TopDirectoryOnly)
				.Select(toolPluginDllPathname => Assembly.LoadFrom(Path.Combine(baseDir, toolPluginDllPathname))));

			foreach (var pluginAssembly in installedToolPluginAssemblies)
			{
				var toolTypes = (pluginAssembly.GetTypes().Where(typeof(ITool).IsAssignableFrom)).ToList();
				foreach (var toolType in toolTypes)
				{
					var constInfo = toolType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
					if (constInfo == null)
						continue; // It does need at least one public or non-public default constructor.
					var currentTool = (ITool)constInfo.Invoke(BindingFlags.Public | BindingFlags.NonPublic, null, null, null);
					m_tools.Add(currentTool.MachineName, currentTool);
				}
			}
		}

		#region Implementation of IToolRepository

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the given area.</returns>
		public ITool GetPersistedOrDefaultToolForArea(IPropertyTable propertyTable, IArea area)
		{
			// The persisted tool could be obsolete or simply not present,
			// so we'll use the defauld tool of the given area, if the stored one cannot be found.
			// That default tool must be available, even if there are no other tools.
			var storedOrDefaultToolName = propertyTable.GetValue(BasePersistedToolName + area.MachineName,
				SettingsGroup.LocalSettings, area.DefaultToolMachineName);
			return GetTool(storedOrDefaultToolName);
		}

		/// <summary>
		/// Get the ITool that has the machine friendly "Name" for <paramref name="machineName"/>.
		/// </summary>
		/// <returns>The ITool for the given Name, or null if not in the system.</returns>
		public ITool GetTool(string machineName)
		{
			ITool retval;
			m_tools.TryGetValue(machineName, out retval);
			return retval; // May be null.
		}

		/// <summary>
		/// Return all installed tools in no particular order for the given area.
		/// </summary>
		/// <returns></returns>
		public IList<ITool> AllToolsForAreaInOrder(IList<string> expectedToolsInOrder, string areaMachineName)
		{
			var retval = new List<ITool>(expectedToolsInOrder.Count);
			foreach (var expectedTool in expectedToolsInOrder)
			{
				ITool extantTool;
				if (m_tools.TryGetValue(expectedTool, out extantTool))
				{
					retval.Add(extantTool);
				}
			}

			// Add any user-defined tools that are installed.
			retval.AddRange(m_tools.Values.Where(userDefinedTool => userDefinedTool.AreaMachineName == areaMachineName && !retval.Contains(userDefinedTool)));

			return retval;
		}

		#endregion
	}
}