// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;

namespace LanguageExplorer.Works
{
#if RANDYTODO
	// My current idea is to repurpose DataTreeMenuHandler (nee DTMenuHandler)
	// In a way it will hold the actual menu click events that are then assigned to the context menus,
	// when they are displayed. That way the same event handler can be used for main menu items, toolbar buttons,
	// and those context menus that show up in a DataTree.
	//
	// If I can pull it off, this class will hold a Dictionary that has a string key (original command id from the old xml files?)
	// and a Tuple with four chunks: 1. The event handler Action, 2. an optional main menu item, 3. an optional toolbar button, and 4. a context menu item
	// # 1, #2 (if any), and #3 (if any) are set up up by a given tool.
	//
	// #4 comes and goes and is created either by a Slice that wants to display context menu relevant to that slice, and/or a record view
	// That has a context menu to display up top in its pane bar. The event is wired up to a context menu item and then unwired, when the menu goes away.
	//
	// The class of event args that will be needed to support all of this is yet to be determined.
	//
	// As a 'spike' to see if the plan will work, I hope to find a suitable command that was used in all of the possible places the core event
	// could be used in: main menu, toolbar, pane bar, one or two locations in the slices.
	// Surely, at least one such original command was used in the lex edit tool, such as insert entry or sense.
#endif
	/// <summary>
	/// DataTreeMenuHandler provides context menus to the data tree.  When the user (or test code)
	/// selects issues commands, this class also invokes the corresponding methods on the data tree.
	/// You may create subclasses to do smart things with menus.
	/// </summary>
	internal sealed class DataTreeMenuHandler : IFlexComponent, IDisposable
	{
		/// <summary />
		internal DataTreeMenuHandler(RecordClerk recordClerk, DataTree dataTree)
		{
			if (recordClerk == null) throw new ArgumentNullException(nameof(recordClerk));
			if (dataTree == null) throw new ArgumentNullException(nameof(dataTree));

			RecordClerk = recordClerk;
			DataTree = dataTree;
			IsDisposed = false;
		}

		internal Dictionary<string, ContextMenuStrip> ContextMenus { get; } = new Dictionary<string, ContextMenuStrip>();

		private Dictionary<string, Dictionary<string, Tuple<ToolStripMenuItem, EventHandler>>> MenuItems { get; } = new Dictionary<string, Dictionary<string, Tuple<ToolStripMenuItem, EventHandler>>>();

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
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

		/// <summary>
		/// Get the DataTree control.
		/// </summary>
		internal DataTree DataTree { get; }

		/// <summary>
		/// Get the clerk.
		/// </summary>
		internal RecordClerk RecordClerk { get; }

		/// <summary>
		/// Register an individual item, which should also have its main ContextMenuStrip registered at some point.
		/// </summary>
		/// <param name="contextmenuId">Id of the context menu the ToolStripMenuItem belongs to.</param>
		/// <param name="id">Command identifier. This will be one of the old style command ids.</param>
		/// <param name="toolStripMenuItem">The tuple that holds the ToolStripMenuItem and its 'Click' event handler.</param>
		internal void RegisterMenu(string contextmenuId, string id, Tuple<ToolStripMenuItem, EventHandler> toolStripMenuItem)
		{
			Dictionary<string, Tuple<ToolStripMenuItem, EventHandler>> contextMenuItems;
			if (!MenuItems.TryGetValue(contextmenuId, out contextMenuItems))
			{
				contextMenuItems = new Dictionary<string, Tuple<ToolStripMenuItem, EventHandler>>();
				MenuItems.Add(contextmenuId, contextMenuItems);
			}
			contextMenuItems.Add(id, toolStripMenuItem);
		}

		/// <summary>
		/// Invoked by a DataTree (which is in turn invoked by the slice)
		/// when the context menu for a slice is needed.
		/// </summary>
		public ContextMenuStrip ShowSliceContextMenu(object sender, SliceMenuRequestArgs e)
		{
			var elementToSearchIn = e.Slice.CallerNode ?? e.Slice.ConfigurationNode;
			var menuId = XmlUtils.GetOptionalAttributeValue(elementToSearchIn, e.HotLinksOnly ? "hotlinks" : "menu", string.Empty);

			//an empty/missing hotlinks/menu attribute means no menu
			if (string.IsNullOrWhiteSpace(menuId))
			{
				return null;
			}

			ContextMenuStrip contextMenuStrip;
			return ContextMenus.TryGetValue(menuId, out contextMenuStrip) ? contextMenuStrip : null;
		}

#region IDisposable
		~DataTreeMenuHandler()
		{
			Dispose(false);
		}
		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		private bool IsDisposed { get; set; }

		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (IsDisposed)
			{
				return; // No need to do it twice.
			}
			if (disposing)
			{
				// Disconnect all event handlers from menus and dispose the menus.
				foreach (var menuDictionary in MenuItems.Values)
				{
					foreach (var menuTuple in menuDictionary.Values)
					{
						menuTuple.Item1.Click -= menuTuple.Item2;
						menuTuple.Item1.Dispose();
					}
					menuDictionary.Clear();
				}
				MenuItems.Clear();

				foreach (var contextMenuStrip in ContextMenus.Values)
				{
					contextMenuStrip.Dispose();
				}
				ContextMenus.Clear();
			}
			IsDisposed = true;
		}
#endregion
	}
}
