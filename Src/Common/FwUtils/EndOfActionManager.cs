// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using SIL.Code;


namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Manages the events that need to be executed at the end of an action. The action could be
	/// a command, button click, menu change...
	/// </summary>
	public sealed class EndOfActionManager
	{
		private bool m_initialized = false;
		// The events are grouped by message, and then by scope within each message. At most a message
		// is queued once per scope (last one wins). Note: Null is a valid scope, it follows the same
		// rules; queued once and last one wins, but null scope is unique in that the message is delivered
		// to all subscribers regardless of their scope (null or not null).
		private readonly Dictionary<string, List<PublisherParameterObject>> m_events = new Dictionary<string, List<PublisherParameterObject>>();

		/// <summary>
		/// Returns the EndOfAction event that is currently being published.
		/// Returns null if we are not currently publishing an EndOfAction event.
		/// </summary>
		public string CurrentEndOfActionEvent { get; private set; }

		internal void AddEvent(PublisherParameterObject publisherParameterObject)
		{
			// If not already initialized, then add the EndOfAction event to the idle queue.
			if (!m_initialized)
			{
				m_initialized = true;
				FwUtils.IdleQueue.Add(new IdleQueueTask(IdleQueuePriority.High, IdleEndOfAction, null));
			}

			if (CurrentEndOfActionEvent != null)
			{
				// Confirm that the event being added comes after the currently executing event.
				var currentEventIndex = EndOfActionOrder.Order.IndexOf(CurrentEndOfActionEvent);
				var newEventIndex = EndOfActionOrder.Order.IndexOf(publisherParameterObject.Message);

				if (newEventIndex <= currentEventIndex)
				{
					throw new Exception(String.Format(
						"While executing an EndOfAction event, cannot add the current or an earlier event. Current Event: {0}    Added Event: {1}",
						CurrentEndOfActionEvent,
						publisherParameterObject.Message));
				}
			}

			// The whole parameter object is kept so the delivery scope survives until the idle publish.
			// Group by both message and scope: overwrite an already-queued publish only if it has the
			// same scope; publishes of the same message from different scopes are all delivered.
			if (!m_events.TryGetValue(publisherParameterObject.Message, out var queued))
			{
				queued = new List<PublisherParameterObject>();
				m_events.Add(publisherParameterObject.Message, queued);
			}
			var sameScopeIndex = queued.FindIndex(e => ReferenceEquals(e.Scope, publisherParameterObject.Scope));
			if (sameScopeIndex >= 0)
			{
				queued[sameScopeIndex] = publisherParameterObject;
			}
			else
			{
				queued.Add(publisherParameterObject);
			}
		}

		/// Should be private, but is public to support testing.
		public bool IdleEndOfAction(object _)
		{
			// Execute the 'End of Action' events in a fixed order.
			try
			{
				foreach (var orderedEvent in EndOfActionOrder.Order)
				{
					if (m_events.TryGetValue(orderedEvent, out var queuedEvents))
					{
						m_events.Remove(orderedEvent);
						CurrentEndOfActionEvent = orderedEvent;
						foreach (var publisherParameterObject in queuedEvents)
						{
							FwUtils.Publisher.Publish(publisherParameterObject);
						}
						CurrentEndOfActionEvent = null;
					}

					if (m_events.Count == 0)
					{
						break;
					}
				}

				if (m_events.Count == 1)
				{
					throw new Exception(String.Format("This 'End Of Action' event was not assigned an order: {0}", m_events.First().Key));
				}
				else if (m_events.Count > 1)
				{
					throw new Exception(String.Format("{0} 'End Of Action' events were not assigned an order.", m_events.Count));
				}
			}
			finally
			{
				m_initialized = false;
				CurrentEndOfActionEvent = null;
			}

			return true; // Always handle the event in the IdleQueue.
		}
	}
}