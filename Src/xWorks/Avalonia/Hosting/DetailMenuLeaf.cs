// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// One command leaf of a context menu as its configuration defines it: the resolved
	/// <see cref="XCore.Command"/> and the menu text. Built by the bridge's walk of the menu XML,
	/// so no xCore choice object and no mediator query stands behind it.
	/// </summary>
	public sealed class DetailMenuLeaf
	{
		/// <param name="command">The command the item's <c>command</c> attribute names.</param>
		/// <param name="label">The localized menu text, the item's own label when it has one,
		/// else the command's; the accelerator marker is kept.</param>
		public DetailMenuLeaf(Command command, string label)
		{
			Command = command ?? throw new ArgumentNullException(nameof(command));
			Label = label ?? string.Empty;
		}

		/// <summary>The resolved command, for answers that call its target's object UI.</summary>
		public Command Command { get; }

		/// <summary>The command id, e.g. <c>CmdDataTree-Help</c>.</summary>
		public string CommandId => Command.Id;

		/// <summary>The message the command sends, e.g. <c>DataTreeDelete</c>.</summary>
		public string Message => Command.Message;

		/// <summary>The localized menu text with its accelerator marker.</summary>
		public string Label { get; }
	}
}
