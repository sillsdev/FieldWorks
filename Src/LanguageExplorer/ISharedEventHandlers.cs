// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
#if RANDYTODO
	// TODO: At some point, the string based keys in ISharedEventHandlers will go away, in favor of the Command enum based methods.
#endif
	/// <summary>
	/// Interface for sharing event handlers
	/// </summary>
	internal interface ISharedEventHandlers
	{
		/// <summary>
		/// Add a handler with the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <param name="sharedEventHandler">The handler.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/>is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an element with the same <paramref name="key"/> already exists.</exception>
		void Add(string key, EventHandler sharedEventHandler);

		/// <summary>
		/// Add a handler tuple with the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <param name="handlerAndFunction">The handler tuple.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/>is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an element with the same <paramref name="key"/> already exists.</exception>
		void Add(Command key, Tuple<EventHandler, Func<Tuple<bool, bool>>> handlerAndFunction);

		/// <summary>
		/// Remove the handler for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an element with the given <paramref name="key"/> is not present.</exception>
		void Remove(string key);

		/// <summary>
		/// Remove the handler tuple for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an element with the given <paramref name="key"/> is not present.</exception>
		void Remove(Command key);

		/// <summary>
		/// Get the handler for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <returns>The handler.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an element with the given <paramref name="key"/> is not present.</exception>
		EventHandler Get(string key);

		/// <summary>
		/// Get the handler tuple for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <returns>The handler.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an element with the given <paramref name="key"/> is not present.</exception>
		Tuple<EventHandler, Func<Tuple<bool, bool>>> Get(Command key);

		/// <summary>
		/// Get the handler tuple for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <returns>The handler.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an element with the given <paramref name="key"/> is not present.</exception>
		EventHandler GetEventHandler(Command key);

		/// <summary>
		/// Try to get an event handler for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <param name="handlerAndFunction">The handler tuple, or null if <paramref name="key"/> has not been registered.</param>
		/// <returns>'true', if the  <paramref name="key"/> has been registered, otherwise 'false'.</returns>
		bool TryGetEventHandler(Command key, out Tuple<EventHandler, Func<Tuple<bool, bool>>> handlerAndFunction);

		/// <summary>
		/// Try to get an event handler for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key">A unique name for the handler.</param>
		/// <param name="eventHandler">The event handler handler, or null if <paramref name="key"/> has not been registered.</param>
		/// <returns>'true', if the  <paramref name="key"/> has been registered, otherwise 'false'.</returns>
		bool TryGetEventHandler(Command key, out EventHandler eventHandler);
	}
}