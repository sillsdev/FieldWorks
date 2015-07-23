// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface for major FLEx components
	/// </summary>
	public interface IMajorFlexComponent
	{
		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		string Name { get; }

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		void Deactivate(PropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar);

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		void Activate(PropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar);

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		void PrepareToRefresh();

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		void FinishRefresh();

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		/// <param name="propertyTable">The table that is about to be persisted.</param>
		void EnsurePropertiesAreCurrent(PropertyTable propertyTable);
	}
}