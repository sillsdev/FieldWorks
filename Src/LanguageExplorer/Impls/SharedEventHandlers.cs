// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.Code;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Class that provides a place to store shared event handlers. Those can be globally shared, area-wide shared, or tool specific shared handlers.
	/// </summary>
	/// <remarks>
	/// I (RBR) would expect there to be more at the area/tool levels than global, since global by nature may not need to be shared.
	/// That may really be shared between some areas or between a tool in one area and a tool in another area.
	/// </remarks>
	internal sealed class SharedEventHandlers : ISharedEventHandlers
	{
		private readonly Dictionary<string, EventHandler> _sharedEventHandlers = new Dictionary<string, EventHandler>();
		private readonly Dictionary<string, Func<Tuple<bool, bool>>> _sharedStatusCheckers = new Dictionary<string, Func<Tuple<bool, bool>>>();

		#region Implementation of ISharedEventHandlers

		/// <inheritdoc />
		public void Add(string key, EventHandler sharedEventHandler)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));
			Require.That(!_sharedEventHandlers.ContainsKey(key), $"'{key}' already present.");
			Guard.AgainstNull(sharedEventHandler, nameof(sharedEventHandler));

			_sharedEventHandlers.Add(key, sharedEventHandler);
		}

		/// <inheritdoc />
		public void Remove(string key)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));
			Require.That(_sharedEventHandlers.ContainsKey(key), $"'{key}' not present.");

			_sharedEventHandlers.Remove(key);
		}

		/// <inheritdoc />
		public EventHandler Get(string key)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));
			Require.That(_sharedEventHandlers.ContainsKey(key), $"'{key}' not present.");

			return _sharedEventHandlers[key];
		}

		/// <inheritdoc />
		public bool TryGetEventHandler(string key, out EventHandler eventHandler)
		{
			return _sharedEventHandlers.TryGetValue(key, out eventHandler);
		}

		/// <inheritdoc />
		public void AddStatusChecker(string key, Func<Tuple<bool, bool>> visibilityStatus)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));
			Require.That(!_sharedStatusCheckers.ContainsKey(key), $"'{key}' already present.");
			Guard.AgainstNull(visibilityStatus, nameof(visibilityStatus));
			_sharedStatusCheckers.Add(key, visibilityStatus);
		}

		/// <inheritdoc />
		public Func<Tuple<bool, bool>> GetStatusChecker(string key)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));

			return _sharedStatusCheckers.ContainsKey(key) ? _sharedStatusCheckers[key] : DefaultStatusChecker;
		}

		/// <inheritdoc />
		public void RemoveStatusChecker(string key)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));
			Require.That(_sharedStatusCheckers.ContainsKey(key), $"'{key}' not present.");

			_sharedStatusCheckers.Remove(key);
		}
		#endregion

		private static Tuple<bool, bool> DefaultStatusChecker()
		{
			return new Tuple<bool, bool>(false, false);
		}
	}
}