// Copyright (c) 2018-2019 SIL International
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
		public void Add(string key, EventHandler sharedEventHandler)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));
			Guard.AgainstNull(sharedEventHandler, nameof(sharedEventHandler));

			Command command;
			if (TryConvertToCommand(key, out command))
			{
				Require.That(!_sharedEventHandlerTuples.ContainsKey(command), $"'{key}' already present.");
				_sharedEventHandlerTuples.Add(command, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(sharedEventHandler, null));
			}
			else
			{
				Require.That(!_sharedEventHandlers.ContainsKey(key), $"'{key}' already present.");
				_sharedEventHandlers.Add(key, sharedEventHandler);
			}
		}

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
		public void Remove(string key)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));

			Command command;
			if (TryConvertToCommand(key, out command))
			{
				Require.That(_sharedEventHandlerTuples.ContainsKey(command), $"'{key}' not present.");
				_sharedEventHandlerTuples.Remove(command);
			}
			else
			{
				Require.That(_sharedEventHandlers.ContainsKey(key), $"'{key}' not present.");
				_sharedEventHandlers.Remove(key);
			}
		}

		/// <inheritdoc />
		public void Remove(Command key)
		{
			Require.That(_sharedEventHandlerTuples.ContainsKey(key), $"'{key}' handler and status checker are not present.");

			_sharedEventHandlerTuples.Remove(key);
		}

		/// <inheritdoc />
		public EventHandler Get(string key)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));

			Command command;
			if (TryConvertToCommand(key, out command))
			{
				Require.That(_sharedEventHandlerTuples.ContainsKey(command), $"'{key}' not present.");
				return _sharedEventHandlerTuples[command].Item1;
			}
			Require.That(_sharedEventHandlers.ContainsKey(key), $"'{key}' not present.");
			return _sharedEventHandlers[key];
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

		public Func<Tuple<bool, bool>> SeeAndDo => () => new Tuple<bool, bool>(true, true);

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