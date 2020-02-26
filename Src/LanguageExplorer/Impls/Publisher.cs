// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Implementation of IPublisher interface.
	/// </summary>
	[Export(typeof(IPublisher))]
	internal sealed class Publisher : IPublisher
	{
		[Import]
		private ISubscriber _subscriber;

		internal Publisher()
		{
		}

		/// <summary>
		/// Constructor for tests only!
		/// </summary>
		internal Publisher(ISubscriber subscriber)
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

			if (!_subscriber.Subscriptions.TryGetValue(message, out var subscribers))
			{
				return;
			}
			foreach (var subscriberAction in subscribers.ToList())
			{
				// NB: It is possible that the action's object is disposed,
				// but we'll not fret about making sure it isn't disposed,
				// but we will expect the subscribers to be well-behaved and unsubscribe,
				// when they get disposed.
				subscriberAction(newValue);
			}
		}

		#region Implementation of IPublisher

		/// <inheritdoc />
		void IPublisher.Publish(PublisherParameterObject publisherParameterObject)
		{
			Guard.AgainstNull(publisherParameterObject, nameof(publisherParameterObject));

			PublishMessage(publisherParameterObject.Message, publisherParameterObject.NewValue);
		}

		/// <inheritdoc />
		void IPublisher.Publish(IList<PublisherParameterObject> publisherParameterObjects)
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