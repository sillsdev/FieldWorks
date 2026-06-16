// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Code;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Implementation of IPublisher interface.
	/// </summary>
	internal sealed class Publisher : IPublisher
	{
		private ISubscriber _subscriber;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Publisher(ISubscriber subscriber)
		{
			_subscriber = subscriber;
			EndOfActionManager = new EndOfActionManager();
		}

		/// <summary>
		/// Publish the message using the given data.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		/// <param name="data">The data to send to subscribers. This may be null.</param>
		/// <param name="scope">The delivery scope. When non-null, only subscribers with the same
		/// scope (or none) are invoked; when null, every subscriber is. See <see cref="IPubSubScope"/>.</param>
		private void PublishMessage(string message, object data, IPubSubScope scope)
		{
			Guard.AgainstNullOrEmptyString(message, nameof(message));

			// Check if 'message' was subscribed to.
			if (_subscriber.Subscriptions.TryGetValue(message, out var subscribers))
			{
				foreach (var subscription in subscribers.ToList())
				{
					// A scoped publish is delivered only to subscribers in the same scope (e.g. the
					// same main window). A null scope on either end means process-wide delivery.
					if (scope == null || subscription.Value == null || ReferenceEquals(scope, subscription.Value))
					{
						// NB: It is possible that the action's object is disposed,
						// but we'll not fret about making sure it isn't disposed,
						// but we will expect the subscribers to be well-behaved and unsubscribe,
						// when they get disposed.
						subscription.Key(data);
					}
				}
			}

			// Check if 'message' contains a prefix that was subscribed to.
			// Note: ToArray() is important to avoid new entries being added while iterating over the collection.
			foreach (KeyValuePair<string, Dictionary<Action<string, object>, IPubSubScope>> entry in _subscriber.PrefixSubscriptions.ToArray())
			{
				if (message.StartsWith(entry.Key))
				{
					foreach (var subscription in entry.Value.ToList())
					{
						// Same delivery rule as specific subscriptions: a null scope on either end
						// means process-wide; otherwise the scopes must be the same window.
						if (scope == null || subscription.Value == null || ReferenceEquals(scope, subscription.Value))
						{
							subscription.Key(message, data);
						}
					}
				}
			}
		}

		#region Implementation of IPublisher
		/// <inheritdoc />
		public EndOfActionManager EndOfActionManager { get; }

		/// <inheritdoc />
		public void Publish(PublisherParameterObject publisherParameterObject)
		{
			Guard.AgainstNull(publisherParameterObject, nameof(publisherParameterObject));

			PublishMessage(publisherParameterObject.Message, publisherParameterObject.Data, publisherParameterObject.Scope);
		}

		/// <inheritdoc />
		public void PublishAtEndOfAction(PublisherParameterObject publisherParameterObject)
		{
			Guard.AgainstNull(publisherParameterObject, nameof(publisherParameterObject));

			EndOfActionManager.AddEvent(publisherParameterObject);
		}

		/// <inheritdoc />
		public void Publish(IList<PublisherParameterObject> publisherParameterObjects)
		{
			Guard.AgainstNull(publisherParameterObjects, nameof(publisherParameterObjects));
			Require.That(publisherParameterObjects.Count > 1, $"'{nameof(publisherParameterObjects)}' must contain at least two elements.");

			foreach (var publisherParameterObject in publisherParameterObjects)
			{
				PublishMessage(publisherParameterObject.Message, publisherParameterObject.Data, publisherParameterObject.Scope);
			}
		}
		#endregion
	}
}