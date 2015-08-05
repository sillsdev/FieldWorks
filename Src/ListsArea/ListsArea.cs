// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;

namespace ListsAreaPlugin
{
	public class ListsArea : IArea
	{
		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, MenuStrip menuStrip,
			ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, MenuStrip menuStrip,
			ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void FinishRefresh()
		{
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		/// <param name="propertyTable">The table that is about to be persisted.</param>
		public void EnsurePropertiesAreCurrent(IPropertyTable propertyTable)
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName
		{
			get { return "lists"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Lists"; }
		}

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public List<ITool> AllToolsInOrder { get; private set; }

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon
		{
			get { return ListAreaResources.Lists.ToBitmap(); }
		}

		#endregion
	}
}
