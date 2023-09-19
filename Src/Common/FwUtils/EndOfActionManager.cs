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
		private readonly Dictionary<string, object> m_events = new Dictionary<string, object>();

		internal void AddEvent(PublisherParameterObject publisherParameterObject)
		{
			// If not already initialized, then add the EndOfAction event to the idle queue.
			if (!m_initialized)
			{
				m_initialized = true;
				FwUtils.IdleQueue.Add(new IdleQueueTask(IdleQueuePriority.High, IdleEndOfAction, null));
			}

			// Add the dictionary entry if the key is not present. Overwrite the value if the key is present.
			m_events[publisherParameterObject.Message] = publisherParameterObject.NewValue;
		}

		/// Should be private, but is public to support testing.
		public bool IdleEndOfAction(object _)
		{
			// Execute the 'End of Action' events in a fixed order.
			try
			{
				foreach (var orderedEvent in EndOfActionOrder.Order)
				{
					if (m_events.TryGetValue(orderedEvent, out object data))
					{
						FwUtils.Publisher.Publish(
							new PublisherParameterObject(orderedEvent, data));
						m_events.Remove(orderedEvent);
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
			}

			return true; // Always handle the event in the IdleQueue.
		}
	}
}