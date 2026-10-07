// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The complete native answer for every leaf of the context-menu ids it owns: display
	/// (visible, enabled, checked, label) and execution together, computed from the Avalonia
	/// row alone. Nothing on the mediator, the hidden DataTree command adapter included, takes
	/// part in an owned id. An owned submenu shows, with its configured label, whenever any of
	/// its leaves does; no colleague can hide or relabel it.
	/// </summary>
	public interface IDetailMenuAuthority
	{
		/// <summary>
		/// Whether this authority answers every leaf under the given menu id, submenus
		/// included. Asked only for the ids a menu is built from; a nested menu is never
		/// offered on its own.
		/// </summary>
		bool Owns(string menuId);

		/// <summary>
		/// The rendered item for a leaf under an owned menu id, or null when the leaf is hidden.
		/// </summary>
		/// <param name="menuId">The owned menu id the leaf belongs to.</param>
		/// <param name="leaf">The xCore leaf; its label is the menu text, its
		/// <see cref="ChoiceBase.HelpId"/> the command id.</param>
		/// <exception cref="System.InvalidOperationException">The leaf's command is not one the
		/// authority answers: an owned id must be answered in full, or a leaf would leak as
		/// visible-but-disabled.</exception>
		DetailMenuItem Build(string menuId, ChoiceBase leaf);

		/// <summary>
		/// The items of a list-populated submenu under an owned menu id, in display order, each
		/// already carrying its label, state and execute action. Empty hides the submenu.
		/// </summary>
		/// <param name="menuId">The owned menu id the submenu belongs to.</param>
		/// <param name="listId">The submenu's <c>list</c> attribute: the id xCore would
		/// otherwise have asked the mediator to populate.</param>
		/// <exception cref="System.InvalidOperationException">The list is not one this authority
		/// answers: an owned id must be answered in full.</exception>
		IReadOnlyList<DetailMenuItem> BuildList(string menuId, string listId);
	}
}
