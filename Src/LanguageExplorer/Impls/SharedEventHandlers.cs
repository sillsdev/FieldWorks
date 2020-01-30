// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.Code;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Class that provides a place to store shared event handlers. Those can be:
	/// <list type="number">
	/// <item>
	/// <term>"Global"</term>
	/// <description>Shared across multiple areas, tool, user controls
	/// [Note: By nature truly global things won't need to be shared, but this accounts for things that cross area boundaries.]</description>
	/// </item>
	/// <item>
	/// <term>Area</term>
	/// <description>Shared across all tools and/or user controls within a given area.</description>
	/// </item>
	/// <item>
	/// <term>Tool</term>
	/// <description>Shared across all UserControl instances within a given tool.</description>
	/// </item>
	/// </list>
	/// </summary>
	internal sealed class SharedEventHandlers : ISharedEventHandlers
	{
		private readonly Dictionary<string, EventHandler> _sharedEventHandlers = new Dictionary<string, EventHandler>();
		private readonly Dictionary<string, Func<Tuple<bool, bool>>> _sharedStatusCheckers = new Dictionary<string, Func<Tuple<bool, bool>>>();
		private readonly Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _sharedEventHandlerTuples = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();

		#region Implementation of ISharedEventHandlers

		/// <inheritdoc />
		public void Add(Command key, Tuple<EventHandler, Func<Tuple<bool, bool>>> handlerAndFunction)
		{
			Require.That(!_sharedEventHandlerTuples.ContainsKey(key), $"'{key}' handler and status checker are already present.");
			Guard.AgainstNull(handlerAndFunction, nameof(handlerAndFunction));

			// Don't fret (for now) if someone registered it as a string, and has not yet switched to the Command enum.
			// Just store it in _sharedEventHandlerTuples.
			if (handlerAndFunction.Item2 == null)
			{
				handlerAndFunction = new Tuple<EventHandler, Func<Tuple<bool, bool>>>(handlerAndFunction.Item1, DefaultStatusChecker);
			}
			_sharedEventHandlerTuples.Add(key, handlerAndFunction);
		}

		/// <inheritdoc />
		public void Remove(Command key)
		{
			Require.That(_sharedEventHandlerTuples.ContainsKey(key), $"'{key}' handler and status checker are not present.");

			_sharedEventHandlerTuples.Remove(key);
		}

		/// <inheritdoc />
		public Tuple<EventHandler, Func<Tuple<bool, bool>>> Get(Command key)
		{
			Require.That(_sharedEventHandlerTuples.ContainsKey(key), $"'{key}' handler and status checker are not present.");

			return _sharedEventHandlerTuples[key];
		}

		/// <inheritdoc />
		public EventHandler GetEventHandler(Command key)
		{
			return Get(key).Item1;
		}

		/// <inheritdoc />
		public bool TryGetEventHandler(Command key, out Tuple<EventHandler, Func<Tuple<bool, bool>>> handlerAndFunction)
		{
			return _sharedEventHandlerTuples.TryGetValue(key, out handlerAndFunction);
		}

		/// <inheritdoc />
		public bool TryGetEventHandler(Command key, out EventHandler eventHandler)
		{
			Tuple<EventHandler, Func<Tuple<bool, bool>>> handlerAndFunction;
			if (TryGetEventHandler(key, out handlerAndFunction))
			{
				eventHandler = handlerAndFunction.Item1;
				return true;
			}
			eventHandler = null;
			return false;
		}

		#endregion

		private static Tuple<bool, bool> DefaultStatusChecker()
		{
			return new Tuple<bool, bool>(false, false);
		}

		private static bool TryConvertToCommand(string key, out Command command)
		{
			return Enum.TryParse(key, out command);
		}
	}
}