// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The complete native answer for every leaf of the context-menu ids it owns: display
	/// (visible, enabled, checked, label) and execution together, computed from the Avalonia
	/// row alone. Nothing on the mediator, the hidden DataTree command adapter included, takes
	/// part in an owned id.
	/// </summary>
	public interface IDetailMenuAuthority
	{
		/// <summary>Whether this authority answers every leaf under the given menu id.</summary>
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
	}
}
