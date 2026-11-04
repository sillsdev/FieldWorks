// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.Reporting;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The detail view's own answers to the commands and boolean properties of the main menu,
	/// toolbars and pane bar: command answers by the message each command sends, property
	/// answers by property name. It answers only while its view is live.
	/// </summary>
	internal sealed class DetailCommandAuthority : ICommandAuthority
	{
		private readonly Func<bool> _isLive;
		private readonly Dictionary<string, Func<DetailMenuLeaf, DetailMenuItem>> _commands
			= new Dictionary<string, Func<DetailMenuLeaf, DetailMenuItem>>(StringComparer.Ordinal);
		private readonly Dictionary<string, PropertyAnswer> _properties
			= new Dictionary<string, PropertyAnswer>(StringComparer.Ordinal);

		/// <param name="isLive">Whether the view answers now; while it does not, every
		/// command and property is left to whoever is asked next.</param>
		public DetailCommandAuthority(Func<bool> isLive)
		{
			_isLive = isLive ?? throw new ArgumentNullException(nameof(isLive));
		}

		/// <summary>The messages this authority answers.</summary>
		internal IEnumerable<string> AnsweredMessages => _commands.Keys;

		/// <summary>The boolean properties this authority answers.</summary>
		internal IEnumerable<string> AnsweredProperties => _properties.Keys;

		/// <summary>
		/// Registers the answer for every command that sends <paramref name="message"/>: a null
		/// item hides the command, a disabled one disables it, and an enabled one's Execute runs
		/// it. One answer per message.
		/// </summary>
		internal void Answer(string message, Func<DetailMenuLeaf, DetailMenuItem> answer)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("A message is required.", nameof(message));
			_commands.Add(message, answer ?? throw new ArgumentNullException(nameof(answer)));
		}

		/// <summary>Registers the answer for a boolean property.</summary>
		/// <param name="propertyName">The property the item toggles.</param>
		/// <param name="isEnabled">Whether the item is enabled.</param>
		/// <param name="isChecked">Whether the item is checked; null for the property's own
		/// value.</param>
		/// <param name="toggle">Toggles the item; null to leave toggling to the item, which
		/// flips the property.</param>
		internal void AnswerProperty(string propertyName, Func<bool> isEnabled, Func<bool> isChecked = null,
			Action toggle = null)
		{
			if (string.IsNullOrEmpty(propertyName))
				throw new ArgumentException("A property name is required.", nameof(propertyName));
			_properties.Add(propertyName,
				new PropertyAnswer(isEnabled ?? throw new ArgumentNullException(nameof(isEnabled)), isChecked, toggle));
		}

		public bool UpdateDisplay(Command command, UIItemDisplayProperties display)
		{
			if (!TryGetAnswer(command, out var answer))
				return false;
			var item = answer(LeafOf(command));
			display.Enabled = item?.IsEnabled == true;
			if (item == null)
				display.Visible = false;
			return true;
		}

		public bool Execute(Command command)
		{
			if (!TryGetAnswer(command, out var answer))
				return false;
			var item = answer(LeafOf(command));
			if (item?.IsEnabled == true && item.Execute != null)
				item.Execute();
			else
				Logger.WriteEvent(command.Id + " did nothing: it cannot run now.");
			return true;
		}

		public bool UpdatePropertyDisplay(string propertyName, UIItemDisplayProperties display)
		{
			if (!TryGetAnswer(propertyName, out var answer))
				return false;
			display.Enabled = answer.IsEnabled();
			if (answer.IsChecked != null)
				display.Checked = answer.IsChecked();
			return true;
		}

		public bool ToggleProperty(string propertyName)
		{
			if (!TryGetAnswer(propertyName, out var answer) || answer.Toggle == null)
				return false;
			answer.Toggle();
			return true;
		}

		private bool TryGetAnswer(Command command, out Func<DetailMenuLeaf, DetailMenuItem> answer)
		{
			answer = null;
			return command != null && _isLive() && _commands.TryGetValue(command.Message ?? string.Empty, out answer);
		}

		private bool TryGetAnswer(string propertyName, out PropertyAnswer answer)
		{
			answer = null;
			return propertyName != null && _isLive() && _properties.TryGetValue(propertyName, out answer);
		}

		private static DetailMenuLeaf LeafOf(Command command) => new DetailMenuLeaf(command, command.Label);

		private sealed class PropertyAnswer
		{
			public PropertyAnswer(Func<bool> isEnabled, Func<bool> isChecked, Action toggle)
			{
				IsEnabled = isEnabled;
				IsChecked = isChecked;
				Toggle = toggle;
			}

			public Func<bool> IsEnabled { get; }
			public Func<bool> IsChecked { get; }
			public Action Toggle { get; }
		}
	}
}
