// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;

namespace TextAndWordsAreaPlugin
{
	/// <summary>
	/// IArea implementation for the area: "textAndWords".
	/// </summary>
	public class TextAndWordsArea : IArea
	{
		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(PropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, MenuStrip menuStrip,
			ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(PropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, MenuStrip menuStrip,
			ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
		}

		#endregion

		#region Implementation of IMajorFlexComponent

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
		public void EnsurePropertiesAreCurrent(PropertyTable propertyTable)
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
			get { return "textAndWords"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Texts & Words"; }
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
			get { return TextAndWordsResources.Text_And_Words.ToBitmap(); }
		}

		#endregion
	}
}
