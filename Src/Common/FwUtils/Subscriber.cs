// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Implementation of ISubscriber interface.
	/// </summary>
	internal sealed class Subscriber : ISubscriber
	{
		private readonly Dictionary<string, HashSet<Action<object>>> _subscriptions =
			new Dictionary<string, HashSet<Action<object>>>();
		private readonly Dictionary<string, HashSet<Action<string, object>>> _prefixSubscriptions =
			new Dictionary<string, HashSet<Action<string, object>>>();

		#region Implementation of ISubscriber

		/// <summary>
		/// An object subscribes to message <paramref name="message"/> using
		/// the method <paramref name="messageHandler"/>, which method that takes one parameter of type "object".
		/// </summary>
		/// <param name="message">The message being subscribed to receive.</param>
		/// <param name="messageHandler">The method on subscriber to call, when <paramref name="message"/>
		/// has been published</param>
		public void Subscribe(string message, Action<object> messageHandler)
		{
			if (!_subscriptions.TryGetValue(message, out var subscribers))
			{
				subscribers = new HashSet<Action<object>>();
				_subscriptions.Add(message, subscribers);
			}
			// NB: If an ill-behaved subscribing object registers the same delegate more than once
			// for the same message, then only one registration will 'take'.
			subscribers.Add(messageHandler);
		}

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
		public void PrefixSubscribe(string messagePrefix, Action<string, object> messageHandler)
		{
			if (!_prefixSubscriptions.TryGetValue(messagePrefix, out var prefixSubscribers))
			{
				prefixSubscribers = new HashSet<Action<string, object>>();
				_prefixSubscriptions.Add(messagePrefix, prefixSubscribers);
			}
			// NB: If an ill-behaved subscribing object registers the same delegate more than once
			// for the same message, then only one registration will 'take'.
			prefixSubscribers.Add(messageHandler);
		}

		/// <summary>
		/// Register end of interest (unsubscribe) of an object in receiving <paramref name="message"/>
		/// when/if published.
		/// </summary>
		/// <param name="message">The message that is no longer of interest to subscriber</param>
		/// <param name="messageHandler">The action that is no longer interested in <paramref name="message"/>.</param>
		public void Unsubscribe(string message, Action<object> messageHandler)
		{
			if (!_subscriptions.TryGetValue(message, out var subscribers))
			{
				return;
			}
			subscribers.Remove(messageHandler);
			if (subscribers.Count == 0)
			{
				// Nobody left that cares about 'message', so remove the message from the system.
				_subscriptions.Remove(message);
			}
		}

		/// <summary>
		/// Register end of interest (unsubscribe) of an object in receiving <paramref name="messagePrefix"/>
		/// when/if published.
		/// </summary>
		/// <param name="messagePrefix">The message prefix that is no longer of interest to subscriber</param>
		/// <param name="messageHandler">The action that is no longer interested in <paramref name="messagePrefix"/>.</param>
		public void PrefixUnsubscribe(string messagePrefix, Action<string, object> messageHandler)
		{
			if (!_prefixSubscriptions.TryGetValue(messagePrefix, out var prefixSubscribers))
			{
				return;
			}
			prefixSubscribers.Remove(messageHandler);
			if (prefixSubscribers.Count == 0)
			{
				// Nobody left that cares about 'messagePrefix', so remove the message from the system.
				_prefixSubscriptions.Remove(messagePrefix);
			}
		}

		/// <summary>
		/// Get all current subscriptions.
		/// </summary>
		public IReadOnlyDictionary<string, HashSet<Action<object>>> Subscriptions =>
			new ReadOnlyDictionary<string, HashSet<Action<object>>>(_subscriptions);

		/// <summary>
		/// Get all the current prefix subscriptions. If a message starts with one of these
		/// prefixes then the prefix subscribers will get notified.
		/// </summary>
		public IReadOnlyDictionary<string, HashSet<Action<string, object>>> PrefixSubscriptions =>
			new ReadOnlyDictionary<string, HashSet<Action<string, object>>>(_prefixSubscriptions);

		#endregion
	}
}