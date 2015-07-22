// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Interface for major UI components
	/// </summary>
	public interface IMajorFlexUiComponent
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
		/// This is called on the outgoing Area, when the user switches to a new Area.
		/// </remarks>
		void Deactivate();

		/// <summary>
		/// Activate the area.
		/// </summary>
		/// <remarks>
		/// This is called on the area that is becoming active.
		///
		/// One of its tools will become active.
		/// </remarks>
		void Activate();

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
		/// <param name="fdoServiceLocator">The main system service locator.</param>
		/// <param name="propertyTable">The table that is about to be persisted.</param>
		void EnsurePropertiesAreCurrent(IFdoServiceLocator fdoServiceLocator, PropertyTable propertyTable);
	}
}