// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SIL.CoreImpl;

namespace LanguageExplorer.Impls
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
		private readonly Dictionary<string, ITool> _tools = new Dictionary<string, ITool>();
		/// <summary>
		/// Constructor.
		/// </summary>
		internal ToolRepository()
		{
			var langExAssembly = Assembly.GetExecutingAssembly();
			var toolTypes = (langExAssembly.GetTypes().Where(typeof(ITool).IsAssignableFrom)).ToList();
			foreach (var toolType in toolTypes)
			{
				var constInfo = toolType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
				if (constInfo == null)
				{
					continue; // It does need at least one public or non-public default constructor.
				}
				var currentTool = (ITool)constInfo.Invoke(BindingFlags.NonPublic, null, null, null);
				_tools.Add(currentTool.MachineName, currentTool);
			}
		}

		#region Implementation of IToolRepository

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the given area.</returns>
		public ITool GetPersistedOrDefaultToolForArea(IArea area)
		{
			// The persisted tool could be obsolete or simply not present,
			// so we'll use the default tool of the given area, if the stored one cannot be found.
			// That default tool must be available, even if there are no other tools.
			var storedOrDefaultToolName = PropertyTable.GetValue(BasePersistedToolName + area.MachineName,
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
			_tools.TryGetValue(machineName, out retval);
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
				if (_tools.TryGetValue(expectedTool, out extantTool))
				{
					retval.Add(extantTool);
				}
			}

			// Add any user-defined tools that are installed.
			retval.AddRange(_tools.Values.Where(userDefinedTool => userDefinedTool.AreaMachineName == areaMachineName && !retval.Contains(userDefinedTool)));

			return retval;
		}

		#endregion

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			foreach (var tool in _tools.Values)
			{
				tool.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			}
		}

		#endregion
	}
}