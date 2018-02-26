// Copyright (c) 2016-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class that contains the three interfaces used in the IFlexComponent interface.
	/// </summary>
	public class FlexComponentParameters
	{
		/// <summary />
		public FlexComponentParameters(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
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