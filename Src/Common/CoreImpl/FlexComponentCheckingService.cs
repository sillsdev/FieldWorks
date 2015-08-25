// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Check to make sure an IFlexComponent can be initialized.
	/// </summary>
	public static class FlexComponentCheckingService
	{
		/// <summary>
		/// Check to make sure an IFlexComponent can be initialized.
		/// </summary>
		public static void CheckInitializationValues(IPropertyTable sourcePropertyTable, IPublisher sourcePublisher, ISubscriber sourceSubscriber,
			IPropertyTable targetPropertyTable, IPublisher targetPublisher, ISubscriber targetSubscriber)
		{
			// The three source values must not be null.
			if (sourcePropertyTable == null) throw new ArgumentNullException("sourcePropertyTable");
			if (sourcePublisher == null) throw new ArgumentNullException("sourcePublisher");
			if (sourceSubscriber == null) throw new ArgumentNullException("sourceSubscriber");

			// Three target values must be null.
			if (targetPropertyTable != null) throw new ArgumentException("'targetPropertyTable' must be null");
			if (targetPublisher != null) throw new ArgumentException("'targetPublisher' must be null");
			if (targetSubscriber != null) throw new ArgumentException("'targetSubscriber' must be null");
		}
	}
}
