// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	}
}
