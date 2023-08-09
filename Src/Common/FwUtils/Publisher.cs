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
		}

		/// <summary>
		/// Publish the message using the new value.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		/// <param name="newValue">The new value to send to subscribers. This may be null.</param>
		private void PublishMessage(string message, object newValue)
		{
			Guard.AgainstNullOrEmptyString(message, nameof(message));

			// Check if 'message' was subscribed to.
			if (_subscriber.Subscriptions.TryGetValue(message, out var subscribers))
			{
				foreach (var subscriberAction in subscribers.ToList())
				{
					// NB: It is possible that the action's object is disposed,
					// but we'll not fret about making sure it isn't disposed,
					// but we will expect the subscribers to be well-behaved and unsubscribe,
					// when they get disposed.
					subscriberAction(newValue);
				}
			}

			// Check if 'message' contains a prefix that was subscribed to.
			foreach (KeyValuePair<string, HashSet<Action<string, object>>> entry in _subscriber.PrefixSubscriptions)
			{
				if (message.StartsWith(entry.Key))
				{
					foreach (var subscriberAction in entry.Value.ToList())
					{
						subscriberAction(message, newValue);
					}
				}
			}
		}

		#region Implementation of IPublisher

		/// <inheritdoc />
		public void Publish(PublisherParameterObject publisherParameterObject)
		{
			Guard.AgainstNull(publisherParameterObject, nameof(publisherParameterObject));

			PublishMessage(publisherParameterObject.Message, publisherParameterObject.NewValue);
		}

		/// <inheritdoc />
		public void Publish(IList<PublisherParameterObject> publisherParameterObjects)
		{
			Guard.AgainstNull(publisherParameterObjects, nameof(publisherParameterObjects));
			Require.That(publisherParameterObjects.Count > 1, $"'{nameof(publisherParameterObjects)}' must contain at least two elements.");

			foreach (var publisherParameterObject in publisherParameterObjects)
			{
				PublishMessage(publisherParameterObject.Message, publisherParameterObject.NewValue);
			}
		}
		#endregion
	}
}