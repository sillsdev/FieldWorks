// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.Code;

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
		public IPropertyTable PropertyTable { get; }

		/// <summary>
		/// Get the publisher
		/// </summary>
		public IPublisher Publisher { get; }

		/// <summary>
		/// Get the subscriber
		/// </summary>
		public ISubscriber Subscriber { get; }

		/// <summary>
		/// Check to make sure an IFlexComponent can be initialized.
		/// </summary>
		public static void CheckInitializationValues(FlexComponentParameters sourceFlexComponentParameters, FlexComponentParameters targetFlexComponentParameters)
		{
			Guard.AgainstNull(sourceFlexComponentParameters, nameof(sourceFlexComponentParameters));
			Guard.AgainstNull(targetFlexComponentParameters, nameof(targetFlexComponentParameters));

			// The three source values must not be null.
			if (sourceFlexComponentParameters.PropertyTable == null)
			{
				throw new InvalidOperationException("No source property table.");
			}
			if (sourceFlexComponentParameters.Publisher == null)
			{
				throw new InvalidOperationException("No source publisher.");
			}
			if (sourceFlexComponentParameters.Subscriber == null)
			{
				throw new InvalidOperationException("No source subscriber.");
			}

			// Three target values must be null.
			const string danger_Will_Robinson = "Thou shalt not initialize an IFlexComponent instance more than one time!";
			if (targetFlexComponentParameters.PropertyTable != null)
			{
				throw new InvalidOperationException($"Target property table must be null. {danger_Will_Robinson}");
			}
			if (targetFlexComponentParameters.Publisher != null)
			{
				throw new InvalidOperationException($"Target publisher must be null.  {danger_Will_Robinson}");
			}
			if (targetFlexComponentParameters.Subscriber != null)
			{
				throw new InvalidOperationException($"Target subscriber must be null.  {danger_Will_Robinson}");
			}
		}
	}
}