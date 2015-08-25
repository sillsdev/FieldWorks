// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface that works with ISubscriber to implement
	/// a topic based Pub/Sub system.
	/// </summary>
	public interface IPublisher
	{
		/// <summary>
		/// Publish the message using the new value.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		/// <param name="newValue">The new value to send to subscribers. This may be null.</param>
		void Publish(string message, object newValue);

		/// <summary>
		/// Publish an ordered sequence of messages, each of which has a newValue (which may be null).
		/// </summary>
		/// <param name="messages">Ordered list of messages to publish. Each message has a matching new value (shich may be null).</param>
		/// <param name="newValues">Ordered list of new values. Each value matches a message.</param>
		/// <exception cref="ArgumentNullException">Thrown if either <paramref name="messages"/> or <paramref name="newValues"/> are null.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the <paramref name="messages"/> and <paramref name="newValues"/> lists are not the same size.</exception>
		void Publish(IList<string> messages, IList<object> newValues);
	}
}
