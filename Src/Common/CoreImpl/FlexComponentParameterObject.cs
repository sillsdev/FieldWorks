using System;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Class that contains the three interfaces used in the IFlexComponent interface.
	/// </summary>
	public class FlexComponentParameterObject
	{
		/// <summary />
		public FlexComponentParameterObject(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		/// <summary>
		/// Get the property table.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		/// <summary>
		/// Get the publisher
		/// </summary>
		public IPublisher Publisher { get; private set; }

		/// <summary>
		/// Get the subscriber
		/// </summary>
		public ISubscriber Subscriber { get; private set; }
	}
}