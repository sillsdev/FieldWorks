// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface that gets FLEx's components to initialize with some basic elements.
	/// </summary>
	/// <remarks>
	/// Initializes a FLEx component with the major interfaces, and can then provide them for others
	/// via the various 'provider' interfaces. There should be a 'provider' interface
	/// for each elements that is passed in the "InitializeFlexComponent" method.
	/// </remarks>
	public interface IFlexComponent : IPropertyTableProvider, IPublisherProvider, ISubscriberProvider
	{
		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber);
	}
}