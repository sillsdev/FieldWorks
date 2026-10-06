// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// An editor that holds typed text outside any edit session until its edit finishes.
	/// <see cref="DataTree.HasUnsubmittedText"/> asks each one, so a host holds an external
	/// refresh rather than saving half-typed text and rebuilding under the caret.
	/// </summary>
	public interface IUnstagedTextHolder
	{
		/// <summary>
		/// Whether the editor holds text the domain has not been offered.
		/// </summary>
		bool HasUnstagedText { get; }
	}
}
