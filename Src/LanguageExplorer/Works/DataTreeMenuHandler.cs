// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel.Core.Cellar;
using SIL.FieldWorks.Common.Controls.FileDialog;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
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
	internal sealed class DataTreeMenuHandler : IFlexComponent
	{
		/// <summary />
		internal DataTreeMenuHandler(RecordClerk recordClerk, DataTree dataTree)
		{
			if (recordClerk == null) throw new ArgumentNullException(nameof(recordClerk));
			if (dataTree == null) throw new ArgumentNullException(nameof(dataTree));

			RecordClerk = recordClerk;
			DataTree = dataTree;
		}

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
		/// Invoked by a DataTree (which is in turn invoked by the slice)
		/// when the context menu for a slice is needed.
		/// </summary>
		public ContextMenu ShowSliceContextMenu(object sender, SliceMenuRequestArgs e)
		{
			Slice slice = e.Slice;
			return MakeSliceContextMenu(slice, e.HotLinksOnly);
		}

		private ContextMenu MakeSliceContextMenu(Slice slice, bool fHotLinkOnly)//, bool retrieveDoNotShow)
		{
			var configuration = slice.ConfigurationNode;
			var caller = slice.CallerNode;
			string menuId = null;
			if (caller != null)
				menuId = ShowContextMenu2Id(caller, fHotLinkOnly);
			if (string.IsNullOrEmpty(menuId))
				menuId = ShowContextMenu2Id(configuration, fHotLinkOnly);

			var window = PropertyTable.GetValue<IFwMainWnd>("window");

			//an empty menu attribute means no menu
			if (menuId != null && menuId.Length == 0)
				return null;

			/*			//a missing menu attribute means "figure out a default"
						if (menuId == null)
						{
							//todo: this is probably too simplistic
							//we are trying to select out just atomic objects
							//of this will currently also select "place keeping" nodes
							if(slice.IsObjectNode)
								//					configuration.HasChildNodes /*<-- that's dumb
								//					&& configuration.SelectSingleNode("seq")== null
								//					&& !(e.Slice.Object.Hvo == slice.Container.RootObjectHvo))
							{
								menuId="mnuDataTree-Object";
							}
							else //we could not figure out a default menu for this item, so fall back on the auto menu
							{	//todo: this must not be used in the final product!
								// return m_dataTree.GetAutoMenu(sender, e);
								return null;
							}
						}
						if (menuId == "")
							return null;	//explicitly stated that there should not be a menu

			*/
#if RANDYTODO
			//ChoiceGroup group;
			if(fHotLinkOnly)
			{
				return	window.GetWindowsFormsContextMenu(menuId);
			}
			else
			{
				//string[] menus = new string[2];
				List<string> menus = new List<string>();
				menus.Add(menuId);
				if (slice is MultiStringSlice)
					menus.Add("mnuDataTree-MultiStringSlice");
				else
					menus.Add("mnuDataTree-Object");
				window.ShowContextMenu(menus.ToArray(),
					new Point(Cursor.Position.X, Cursor.Position.Y),
					null, // Don't care about a temporary colleague
					null); // or MessageSequencer
				return null;
			}
#else
			return null; // TODO: Fix this.
#endif
		}

		private string ShowContextMenu2Id(XElement caller, bool fHotLinkOnly)
		{
			if (fHotLinkOnly)
			{
				string result = XmlUtils.GetOptionalAttributeValue(caller, "hotlinks");
				if (!string.IsNullOrEmpty(result))
					return result;
			}
			return XmlUtils.GetOptionalAttributeValue(caller, "menu");
		}
	}
}
