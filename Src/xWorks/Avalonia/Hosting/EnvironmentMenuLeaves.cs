// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// What the host does for the environment commands that need more than the row's editor.
	/// </summary>
	internal interface IEnvironmentMenuHost
	{
		/// <summary>Lets the user choose a natural class; null when they choose none.</summary>
		IPhNaturalClass ChooseNaturalClass();

		/// <summary>Shows why an environment is not well formed.</summary>
		void ShowEnvironmentError(string message);
	}

	/// <summary>
	/// The leaves the environment menus share. The five inserts type into the row's current
	/// text editor at its caret, enabled by <see cref="EnvironmentInsertRules"/> over the text
	/// and selection the request snapshotted; without an editor they are disabled. Describe
	/// Error reports what the domain found wrong with an item. Leaves are routed by the
	/// command's message rather than its id: the three menus are configured in three files,
	/// each declaring its own command ids for the same five messages.
	/// </summary>
	internal static class EnvironmentMenuLeaves
	{
		internal const string InsertSlashMessage = "InsertSlash";
		internal const string InsertBarMessage = "InsertEnvironmentBar";
		internal const string InsertNaturalClassMessage = "InsertNaturalClass";
		internal const string InsertOptionalItemMessage = "InsertOptionalItem";
		internal const string InsertHashMarkMessage = "InsertHashMark";
		internal const string ShowErrorMessage = "ShowEnvironmentError";

		/// <summary>Whether a command message is one of the five inserts.</summary>
		internal static bool IsInsertMessage(string message)
		{
			switch (message)
			{
				case InsertSlashMessage:
				case InsertBarMessage:
				case InsertNaturalClassMessage:
				case InsertOptionalItemMessage:
				case InsertHashMarkMessage:
					return true;
				default:
					return false;
			}
		}

		/// <summary>The insert leaf for a message.</summary>
		/// <param name="message">One of the five insert messages.</param>
		/// <param name="label">The menu text.</param>
		/// <param name="request">The request whose editor snapshot decides enablement and whose
		/// editor the insert types into.</param>
		/// <param name="host">Supplies the natural-class chooser.</param>
		/// <exception cref="InvalidOperationException">The message is not an insert.</exception>
		internal static DetailMenuItem BuildInsert(string message, string label, DetailMenuRequest request,
			IEnvironmentMenuHost host)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			var text = request.EditorText;
			var anchor = request.EditorSelectionAnchor;
			var end = request.EditorSelectionEnd;
			var hasEditor = request.HasTextEditor;
			switch (message)
			{
				case InsertSlashMessage:
					return Insert(label, hasEditor && EnvironmentInsertRules.CanInsertSlash(text), request,
						EnvironmentInsertRules.Slash, 0);
				case InsertBarMessage:
					return Insert(label, hasEditor && EnvironmentInsertRules.CanInsertBar(text, anchor, end), request,
						EnvironmentInsertRules.Bar, 0);
				case InsertHashMarkMessage:
					return Insert(label, hasEditor && EnvironmentInsertRules.CanInsertHashMark(text, anchor, end),
						request, EnvironmentInsertRules.HashMark, 0);
				case InsertOptionalItemMessage:
					// The caret lands between the parentheses, where the item goes.
					return Insert(label, hasEditor && EnvironmentInsertRules.CanInsertItem(text, anchor, end), request,
						EnvironmentInsertRules.OptionalItem, 1);
				case InsertNaturalClassMessage:
					if (!hasEditor || !EnvironmentInsertRules.CanInsertItem(text, anchor, end))
						return DetailMenuItem.Disabled(label);
					return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
						execute: () => InsertNaturalClass(request, host));
				default:
					throw new InvalidOperationException(string.Format(
						"'{0}' is not an environment insert message.", message));
			}
		}

		/// <summary>
		/// The Describe Error leaf for an item: enabled when the domain reports the item
		/// malformed, showing its explanation.
		/// </summary>
		/// <param name="label">The menu text.</param>
		/// <param name="item">The clicked item; null disables the leaf.</param>
		/// <param name="host">Shows the explanation.</param>
		internal static DetailMenuItem BuildDescribeError(string label, DetailChoiceOption item,
			IEnvironmentMenuHost host)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (item == null || !item.HasValidationMessage)
				return DetailMenuItem.Disabled(label);
			var message = item.ValidationMessage;
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => host.ShowEnvironmentError(message));
		}

		private static DetailMenuItem Insert(string label, bool enabled, DetailMenuRequest request, string text,
			int caretBack)
		{
			if (!enabled)
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => request.ReplaceEditorSelection(text, caretBack));
		}

		// The chooser's abbreviation, bracketed, as the WinForms slices type it.
		private static void InsertNaturalClass(DetailMenuRequest request, IEnvironmentMenuHost host)
		{
			var naturalClass = host.ChooseNaturalClass();
			if (naturalClass == null)
				return;
			var abbreviation = naturalClass.Abbreviation.BestAnalysisVernacularAlternative.Text;
			request.ReplaceEditorSelection(EnvironmentInsertRules.NaturalClass(abbreviation), 0);
		}
	}
}
