// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface that works with ISubscriber to implement
	/// a topic based Pub/Sub system.
	/// </summary>
	public interface IPublisher
	{
		/// <summary>
		/// Publish the message in the parameter object using the new value in the parameter object.
		/// </summary>
		/// <param name="publisherParameterObject">The new message and value to send to subscribers. This may not be null.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="publisherParameterObject"/> is null.</exception>
		void Publish(PublisherParameterObject publisherParameterObject);

		/// <summary>
		/// Publish an ordered sequence of messages, each of which has a newValue (which may be null).
		/// </summary>
		/// <param name="publisherParameterObjects">Ordered list of message/value parameter objects to publish. Each message has a matching new value (which may be null).</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="publisherParameterObjects"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown if there are fewer than 2 instances of <paramref name="publisherParameterObjects"/>.
		/// That is, users should only call this method when they have multiple things to publish.</exception>
		void Publish(IList<PublisherParameterObject> publisherParameterObjects);
	}
}
