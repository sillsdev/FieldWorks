// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Factory that creates a pub/sub system via two interfaces.
	/// </summary>
	public static class PubSubSystemFactory
	{
		/// <summary>
		/// Create the interface implementation(s) and return them.
		/// This is one new pub/sub set of interfaces for each call to this factory,
		/// which is not shared with a previous or future call to this method.
		/// </summary>
		/// <param name="publisher"></param>
		/// <param name="subscriber"></param>
		public static void CreatePubSubSystem(out IPublisher publisher, out ISubscriber subscriber)
		{
			var pubSubSystem = new PubSubSystem();
			publisher = pubSubSystem;
			subscriber = pubSubSystem;
		}

		/// <summary>
		/// Implementation of Pub/Sub system, where each part is its own interface,
		/// to not confuse clients, who will only see the interfaces
		/// (other than the factory that creates this class).
		/// </summary>
		private sealed class PubSubSystem : IPublisher, ISubscriber
		{
			private readonly Dictionary<string, HashSet<Action<object>>> _subscriptions = new Dictionary<string, HashSet<Action<object>>>();

			#region Implementation of IPublisher

			/// <summary>
			/// Publish the message using the new value.
			/// </summary>
			/// <param name="message">The message to publish.</param>
			/// <param name="newValue">The new value to send to subscribers. This may be null.</param>
			public void Publish(string message, object newValue)
			{
				HashSet<Action<object>> subscribers;
				if (!_subscriptions.TryGetValue(message, out subscribers))
				{
					return;
				}
				foreach (var subscriberAction in subscribers)
				{
					// NB: It is possible that the action's object is disposed,
					// but we'll not fret about making sure it isn't disposed,
					// but we will expect the subscribers to be well-behaved and unsubscribe,
					// when they get disposed.
					subscriberAction(newValue);
				}
			}

			#endregion

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

			#endregion
		}
	}
}