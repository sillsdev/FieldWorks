// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The native authority for a structured-text row's text menu: Cut, Copy, Paste, Find in
	/// Dictionary and Add to Dictionary, each acting on the paragraph and selection the
	/// request snapshotted.
	/// </summary>
	internal sealed class StructuredTextMenuAuthority : IDetailMenuAuthority
	{
		internal const string MenuId = "mnuStTextChoices";

		private static readonly HashSet<string> s_messages = new HashSet<string>(StringComparer.Ordinal)
		{
			RecordEditView.EditCutMessage, RecordEditView.EditCopyMessage, RecordEditView.EditPasteMessage,
			RecordEditView.LexiconLookupMessage, RecordEditView.AddToLexiconMessage
		};

		private readonly DetailMenuRequest _request;
		private readonly Func<DetailMenuLeaf, DetailTextSelectionSnapshot, DetailMenuItem> _answer;

		/// <param name="request">The request; its paragraph snapshot is what each item acts
		/// on, and a request without one shows them disabled.</param>
		/// <param name="answer">Builds the item for a leaf and the snapshot.</param>
		public StructuredTextMenuAuthority(DetailMenuRequest request,
			Func<DetailMenuLeaf, DetailTextSelectionSnapshot, DetailMenuItem> answer)
		{
			_request = request ?? throw new ArgumentNullException(nameof(request));
			_answer = answer ?? throw new ArgumentNullException(nameof(answer));
		}

		public bool Owns(string menuId) => string.Equals(menuId, MenuId, StringComparison.Ordinal);

		public DetailMenuItem Build(string menuId, DetailMenuLeaf leaf)
		{
			if (leaf == null)
				throw new ArgumentNullException(nameof(leaf));
			if (!s_messages.Contains(leaf.Message))
			{
				throw new InvalidOperationException(string.Format(
					"Menu '{0}' has a leaf '{1}' this authority does not answer.", menuId, leaf.CommandId));
			}
			return _answer(leaf, _request.TextSnapshot);
		}

		// The text menu carries no list-populated submenu.
		public IReadOnlyList<DetailMenuItem> BuildList(string menuId, string listId)
			=> throw new InvalidOperationException(string.Format(
				"Menu '{0}' has a list submenu '{1}' this authority does not answer.", menuId, listId));
	}
}
