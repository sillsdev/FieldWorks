// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
		/// <param name="subscriber"></param>
		internal Publisher(ISubscriber subscriber) : this()
		{
			_subscriber = subscriber;
		}

		#region Implementation of IPublisher

		/// <summary>
		/// Publish the message using the new value.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		/// <param name="newValue">The new value to send to subscribers. This may be null.</param>
		public void Publish(string message, object newValue)
		{
			PublishMessage(message, newValue);
		}

		/// <summary>
		/// Publish an ordered sequence of messages, each of which has a newValue (which may be null).
		/// </summary>
		/// <param name="messages">Ordered list of messages to publish. Each message has a matching new value (shich may be null).</param>
		/// <param name="newValues">Ordered list of new values. Each value matches a message.</param>
		/// <exception cref="ArgumentNullException">Thrown if either <paramref name="messages"/> or <paramref name="newValues"/> are null.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the <paramref name="messages"/> and <paramref name="newValues"/> lists are not the same size.</exception>
		public void Publish(IList<string> messages, IList<object> newValues)
		{
			Guard.AgainstNull(messages, nameof(messages));
			Guard.AgainstNull(newValues, nameof(newValues));
			Require.That(messages.Count == newValues.Count, "'messages' and 'newValues' counts are not the same.");

			for (var idx = 0; idx < messages.Count; ++idx)
			{
				PublishMessage(messages[idx], newValues[idx]);
			}
		}

		private string _lastMessage;
		private object _lastNewValue;
		/// <summary>
		/// Publish the message using the new value.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		/// <param name="newValue">The new value to send to subscribers. This may be null.</param>
		private void PublishMessage(string message, object newValue)
		{
			Guard.AgainstNullOrEmptyString(message, nameof(message));

			try
			{
				if (_lastMessage == message && _lastNewValue.ToString() == newValue.ToString())
				{
					Console.WriteLine($@"Why, pray tell, do we need to redo the very same message ({message}) with the very same new value?");
				}
				else
				{
					Console.WriteLine($@"About to publish: '{message}'.");
				}
				using (Detect.Reentry(this, "Publish").AndThrow())
				{
					_lastMessage = message;
					_lastNewValue = newValue;
					HashSet<Action<object>> subscribers;
					if (!_subscriber.Subscriptions.TryGetValue(message, out subscribers))
					{
						Console.WriteLine($@"Nobody likes me ({message}), everybody hates me, guess I'll go eat some worms....");
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
				Console.WriteLine($@"Finished publishing: '{message}'.");
			}
			finally
			{
				_lastMessage = null;
				_lastNewValue = null;
			}
		}

		#endregion
	}
}