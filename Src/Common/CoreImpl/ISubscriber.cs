// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface that works with IPublisher to implement
	/// a topic based Pub/Sub system.
	/// </summary>
	public interface ISubscriber
	{
		/// <summary>
		/// An object subscribes to message <paramref name="message"/> using
		/// the method <paramref name="messageHandler"/>, which method that takes one parameter of type "object".
		/// </summary>
		/// <param name="message">The message being subscribed to receive.</param>
		/// <param name="messageHandler">The method on subscriber to call, when <paramref name="message"/>
		/// has been published</param>
		void Subscribe(string message, Action<object> messageHandler);

		/// <summary>
		/// Register end of interest (unsubscribe) of an object in receiving <paramref name="message"/>
		/// when/if published.
		/// </summary>
		/// <param name="message">The message that is no longer of interest to subscriber</param>
		/// <param name="messageHandler">The action that is no longer interested in <paramref name="message"/>.</param>
		void Unsubscribe(string message, Action<object> messageHandler);
	}
}