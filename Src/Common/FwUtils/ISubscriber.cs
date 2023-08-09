// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
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
		/// An object subscribes to messages that begin with <paramref name="messagePrefix"/> using
		/// the method <paramref name="messageHandler"/>. The method takes two parameters, the
		/// first is the specific message, and the second is of type "object".
		/// It is important to note that prefix subscribers are called after
		/// specific subscribers.  Specific subscribers are checked first, then we
		/// check if there are any prefix subscribers.
		/// </summary>
		/// <param name="messagePrefix">The message prefix being subscribed to receive.</param>
		/// <param name="messageHandler">The method on subscriber to call, when a message that
		/// begins with <paramref name="messagePrefix"/> has been published.</param>
		void PrefixSubscribe(string messagePrefix, Action<string, object> messageHandler);

		/// <summary>
		/// Register end of interest (unsubscribe) of an object in receiving <paramref name="message"/>
		/// when/if published.
		/// </summary>
		/// <param name="message">The message that is no longer of interest to subscriber</param>
		/// <param name="messageHandler">The action that is no longer interested in <paramref name="message"/>.</param>
		void Unsubscribe(string message, Action<object> messageHandler);

		/// <summary>
		/// Register end of interest (unsubscribe) of an object in receiving <paramref name="messagePrefix"/>
		/// when/if published.
		/// </summary>
		/// <param name="messagePrefix">The message prefix that is no longer of interest to subscriber</param>
		/// <param name="messageHandler">The action that is no longer interested in <paramref name="messagePrefix"/>.</param>
		void PrefixUnsubscribe(string messagePrefix, Action<string, object> messageHandler);

		/// <summary>
		/// Get all current subscriptions.
		/// </summary>
		IReadOnlyDictionary<string, HashSet<Action<object>>> Subscriptions { get; }

		/// <summary>
		/// Get all the current prefix subscriptions. If a message starts with one of these
		/// prefixes then the prefix subscribers will get notified.
		/// </summary>
		IReadOnlyDictionary<string, HashSet<Action<string, object>>> PrefixSubscriptions { get; }
	}
}