// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.Code;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Class that provides a place to store shared event handlers. Those can be globally, area-wide, tool-wide, or Control shared handlers.
	/// </summary>
	/// <remarks>
	/// I (RBR) would expect there to be more at the area/tool levels than global, since global by nature may not need to be shared.
	/// That may really be shared between some areas or between a tool in one area and a tool in another area.
	/// </remarks>
	internal sealed class CommandBasedSharedEventHandlers : ICommandBasedSharedEventHandlers
	{
		private readonly Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _sharedTuples = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();

		#region Implementation of ICommandBasedSharedEventHandlers

		/// <inheritdoc />
		void ICommandBasedSharedEventHandlers.Add(Command key, Tuple<EventHandler, Func<Tuple<bool, bool>>> handlerAndFunction)
		{
			Require.That(!_sharedTuples.ContainsKey(key), $"'{key}' already present.");
			Guard.AgainstNull(handlerAndFunction, nameof(handlerAndFunction));

			if (handlerAndFunction.Item2 == null)
			{
				handlerAndFunction = new Tuple<EventHandler, Func<Tuple<bool, bool>>>(handlerAndFunction.Item1, DefaultStatusChecker);
			}
			_sharedTuples.Add(key, handlerAndFunction);
		}

		/// <inheritdoc />
		void ICommandBasedSharedEventHandlers.Remove(Command key)
		{
			Require.That(_sharedTuples.ContainsKey(key), $"'{key}' not present.");

			_sharedTuples.Remove(key);
		}

		/// <inheritdoc />
		Tuple<EventHandler, Func<Tuple<bool, bool>>> ICommandBasedSharedEventHandlers.Get(Command key)
		{
			Require.That(_sharedTuples.ContainsKey(key), $"'{key}' not present.");

			return _sharedTuples[key];
		}

		/// <inheritdoc />
		bool ICommandBasedSharedEventHandlers.TryGetEventHandler(Command key, out Tuple<EventHandler, Func<Tuple<bool, bool>>> handlerAndFunction)
		{
			return _sharedTuples.TryGetValue(key, out handlerAndFunction);
		}
		#endregion

		private static Tuple<bool, bool> DefaultStatusChecker()
		{
			return new Tuple<bool, bool>(false, false);
		}
	}
}