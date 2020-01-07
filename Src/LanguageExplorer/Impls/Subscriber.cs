// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Implementation of ISubscriber interface.
	/// </summary>
	[Export(typeof(ISubscriber))]
	internal sealed class Subscriber : ISubscriber
	{
		private readonly Dictionary<string, HashSet<Action<object>>> _subscriptions = new Dictionary<string, HashSet<Action<object>>>();

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
			HashSet<Action<object>> subscribers;
			if (!_subscriptions.TryGetValue(message, out subscribers))
			{
				subscribers = new HashSet<Action<object>>();
				_subscriptions.Add(message, subscribers);
			}
			// NB: If an ill-behaved subscribing object registers the same delegate more than once
			// for the same message, then only one registration will 'take'.
			subscribers.Add(messageHandler);
		}

		/// <summary>
		/// Register end of interest (unsubscribe) of an object in receiving <paramref name="message"/>
		/// when/if published.
		/// </summary>
		/// <param name="message">The message that is no longer of interest to subscriber</param>
		/// <param name="messageHandler">The action that is no longer interested in <paramref name="message"/>.</param>
		public void Unsubscribe(string message, Action<object> messageHandler)
		{
			HashSet<Action<object>> subscribers;
			if (!_subscriptions.TryGetValue(message, out subscribers))
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
		/// Get all current subscriptions.
		/// </summary>
		public IReadOnlyDictionary<string, HashSet<Action<object>>> Subscriptions => new ReadOnlyDictionary<string, HashSet<Action<object>>>(_subscriptions);

		#endregion
	}
}