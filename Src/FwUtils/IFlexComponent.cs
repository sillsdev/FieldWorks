// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface that gets FLEx's components to initialize with some basic elements.
	/// </summary>
	/// <remarks>
	/// Initializes a FLEx component with the major interfaces, and can then provide them for others
	/// via the various 'provider' interface properties. There should be a 'provider' interface
	/// for each element that is passed in the "InitializeFlexComponent" method.
	/// </remarks>
	public interface IFlexComponent : IPropertyTableProvider, IPublisherProvider, ISubscriberProvider
	{
		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		void InitializeFlexComponent(FlexComponentParameters flexComponentParameters);
	}
}