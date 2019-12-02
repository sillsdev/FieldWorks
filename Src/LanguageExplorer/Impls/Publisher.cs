// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Implementation of IPublisher interface.
	/// </summary>
	[Export(typeof(IPublisher))]
	internal sealed class Publisher : IPublisher, IDisposable
	{
		private IdleQueue _idleQueue;
		[Import]
		private ISubscriber _subscriber;

		internal Publisher()
		{
			_idleQueue = new IdleQueue
			{
				IsPaused = true
			};
		}

		/// <summary>
		/// Constructor for tests only!
		/// </summary>
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
		/// <param name="messages">Ordered list of messages to publish. Each message has a matching new value (which may be null).</param>
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

		/// <summary>
		/// Publish the message using the new value.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		/// <param name="newValue">The new value to send to subscribers. This may be null.</param>
		private void PublishMessage(string message, object newValue)
		{
			Guard.AgainstNullOrEmptyString(message, nameof(message));

			using (var detector = Detect.Reentry(this, "Publish"))
			{
				if (detector.DidReenter)
				{
					_idleQueue.Add(new IdleQueueTask(IdleQueuePriority.High, DeferredPublish, new DeferredMessage(message, newValue)));
					_idleQueue.IsPaused = false;
				}
				else
				{
					_idleQueue.IsPaused = true;
					HashSet<Action<object>> subscribers;
					if (!_subscriber.Subscriptions.TryGetValue(message, out subscribers))
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
			}
		}

		private bool DeferredPublish(object arg)
		{
			var deferredMessage = (DeferredMessage)arg;
			Publish(deferredMessage.Message, deferredMessage.NewValue);
			return true;
		}
		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. _isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~Publisher()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				_idleQueue?.Dispose();
			}
			_idleQueue = null;

			// Dispose unmanaged resources here, whether disposing is true or false.

			_isDisposed = true;
		}
		#endregion

		private sealed class DeferredMessage
		{
			internal string Message { get; }
			internal object NewValue { get; }

			internal DeferredMessage(string message, object newValue)
			{
				Message = message;
				NewValue = newValue;
			}
		}
	}
}