// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace XCore
{
	/// <summary>
	/// Answers commands and boolean properties of menus, toolbars and pane bars directly, ahead
	/// of the mediator. What an authority answers, it answers in full: the mediator is asked
	/// neither how to show it nor to run it.
	/// </summary>
	public interface ICommandAuthority
	{
		/// <summary>
		/// Whether this authority answers <paramref name="command"/>; when it does, it has set
		/// whether <paramref name="display"/> is enabled and visible.
		/// </summary>
		bool UpdateDisplay(Command command, UIItemDisplayProperties display);

		/// <summary>
		/// Whether this authority answers <paramref name="command"/>; when it does, it has run
		/// the command, or done nothing when the command cannot run now.
		/// </summary>
		bool Execute(Command command);

		/// <summary>
		/// Whether this authority answers the boolean property <paramref name="propertyName"/>;
		/// when it does, it has set whether <paramref name="display"/> is enabled, visible and
		/// checked.
		/// </summary>
		bool UpdatePropertyDisplay(string propertyName, UIItemDisplayProperties display);

		/// <summary>
		/// Whether this authority toggles the boolean property <paramref name="propertyName"/>
		/// itself; when it does, it has toggled it.
		/// </summary>
		bool ToggleProperty(string propertyName);
	}

	/// <summary>
	/// The command authorities registered with one property table, asked most recently
	/// registered first; the first that answers decides.
	/// </summary>
	public sealed class CommandAuthorities : ICommandAuthority
	{
		/// <summary>The property holding a property table's registered authorities.</summary>
		public const string PropertyName = "CommandAuthorities";

		private readonly List<ICommandAuthority> m_authorities = new List<ICommandAuthority>();

		/// <summary>
		/// The authorities registered with <paramref name="propertyTable"/>; null when none ever
		/// was, or the table is null or disposed.
		/// </summary>
		public static CommandAuthorities Of(PropertyTable propertyTable)
		{
			if (propertyTable == null || propertyTable.IsDisposed)
				return null;
			return propertyTable.GetValue<object>(PropertyName) as CommandAuthorities;
		}

		/// <summary>
		/// Registers <paramref name="authority"/> with <paramref name="propertyTable"/> ahead of
		/// those registered before it.
		/// </summary>
		/// <returns>Disposing it unregisters the authority.</returns>
		public static IDisposable Register(PropertyTable propertyTable, ICommandAuthority authority)
		{
			if (propertyTable == null)
				throw new ArgumentNullException(nameof(propertyTable));
			if (authority == null)
				throw new ArgumentNullException(nameof(authority));
			var authorities = Of(propertyTable);
			if (authorities == null)
			{
				authorities = new CommandAuthorities();
				propertyTable.SetProperty(PropertyName, authorities, false);
				propertyTable.SetPropertyPersistence(PropertyName, false);
			}
			authorities.m_authorities.Insert(0, authority);
			return new Registration(authorities, authority);
		}

		/// <inheritdoc />
		public bool UpdateDisplay(Command command, UIItemDisplayProperties display)
			=> FirstAnswer(a => a.UpdateDisplay(command, display));

		/// <inheritdoc />
		public bool Execute(Command command) => FirstAnswer(a => a.Execute(command));

		/// <inheritdoc />
		public bool UpdatePropertyDisplay(string propertyName, UIItemDisplayProperties display)
			=> FirstAnswer(a => a.UpdatePropertyDisplay(propertyName, display));

		/// <inheritdoc />
		public bool ToggleProperty(string propertyName) => FirstAnswer(a => a.ToggleProperty(propertyName));

		private bool FirstAnswer(Func<ICommandAuthority, bool> answers)
		{
			// By index, because an answer may run a command that unregisters an authority.
			for (var i = 0; i < m_authorities.Count; i++)
			{
				if (answers(m_authorities[i]))
					return true;
			}
			return false;
		}

		private sealed class Registration : IDisposable
		{
			private CommandAuthorities m_owner;
			private readonly ICommandAuthority m_authority;

			public Registration(CommandAuthorities owner, ICommandAuthority authority)
			{
				m_owner = owner;
				m_authority = authority;
			}

			public void Dispose()
			{
				m_owner?.m_authorities.Remove(m_authority);
				m_owner = null;
			}
		}
	}
}
