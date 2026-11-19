// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;
using XCore;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Notebook answers of the detail view: the record and subrecord commands of its
	/// menus and pane bar, and the text commands of a structured-text row, which act on the
	/// word or span at its selection.
	/// </summary>
	public partial class RecordEditView
	{
		/// <summary>The tool whose record and text commands this view answers on the main
		/// menu, toolbar and pane bar.</summary>
		internal const string NotebookEditTool = "notebookEdit";

		/// <summary>The message of the record list's inserts, such as Insert Subrecord.</summary>
		internal const string InsertItemInVectorMessage = "InsertItemInVector";

		/// <summary>The message of a subrecord's Move Up.</summary>
		internal const string MoveRecordUpMessage = "MoveItemUpInVector";

		/// <summary>The message of a subrecord's Move Down.</summary>
		internal const string MoveRecordDownMessage = "MoveItemDownInVector";

		/// <summary>The message of a subrecord's Promote.</summary>
		internal const string PromoteSubrecordMessage = "PromoteSubitemInVector";

		/// <summary>The message of a subrecord's Demote.</summary>
		internal const string DemoteSubrecordMessage = "DemoteSubitemInVector";

		/// <summary>The message of the pane bar's Demote Record.</summary>
		internal const string DemoteRecordMessage = "DemoteItemInVector";

		/// <summary>The message of Find in Dictionary.</summary>
		internal const string LexiconLookupMessage = "LexiconLookup";

		/// <summary>The message of Add to Dictionary, Insert > Entry in Notebook.</summary>
		internal const string AddToLexiconMessage = "AddToLexicon";

		/// <summary>The message of Cut.</summary>
		internal const string EditCutMessage = "EditCut";

		/// <summary>The message of Copy.</summary>
		internal const string EditCopyMessage = "EditCopy";

		/// <summary>The message of Paste.</summary>
		internal const string EditPasteMessage = "EditPaste";

		private FwTsStringClipboard m_textClipboard;

		// The word at the start of the last selection asked about: the toolbar asks on every
		// idle pass, mostly about the same selection.
		private DetailTextSelectionSnapshot m_wordSelection;
		private ITsString m_wordText;
		private int m_wordMin;
		private int m_wordLim;

		/// <summary>
		/// Picks the record to take a demoted one among two or more candidates, or null to
		/// cancel; true for a subrecord: the product asks with
		/// <see cref="DemotedRecordOwnerChooser"/>; a headless test answers for the user.
		/// </summary>
		internal Func<IReadOnlyList<IRnGenericRec>, bool, IRnGenericRec> DemotedRecordOwnerChosen { get; set; }

		/// <summary>
		/// Shows the entry for a word of a paragraph, given its writing system and span, or
		/// offers to create one: the product opens it as Find in Dictionary does elsewhere; a
		/// headless test records the call.
		/// </summary>
		internal Action<IStTxtPara, int, int, int> WordLookupLaunch { get; set; }

		/// <summary>
		/// Offers to add a word to the lexicon with the given form: the product opens
		/// <see cref="InsertEntryDlg"/>; a headless test records the call.
		/// </summary>
		internal Action<ITsString> AddWordLaunch { get; set; }

		/// <summary>
		/// Supplies the current row's structured-text selection: the product asks the host for
		/// the last one its editor reported; a headless test names one.
		/// </summary>
		internal Func<DetailTextSelectionSnapshot> CurrentTextSelectionSource { get; set; }

		/// <summary>The clipboard the text commands use.</summary>
		internal FwTsStringClipboard TextClipboard
			=> m_textClipboard ?? (m_textClipboard = new FwTsStringClipboard(Cache.WritingSystemFactory));

		/// <summary>
		/// The record list's own Insert item, such as Insert Subrecord: shown and run as the
		/// view's record list decides, whichever row the menu came from.
		/// </summary>
		internal DetailMenuItem ClerkInsertItem(DetailMenuLeaf leaf)
		{
			var clerk = Clerk;
			if (clerk == null)
				return null;
			var display = new UIItemDisplayProperties(null, leaf.Label, false, null, true);
			clerk.OnDisplayInsertItemInVector(leaf.Command, ref display);
			var label = XCoreMenuBridge.StripAccelerator(string.IsNullOrEmpty(display.Text) ? leaf.Label : display.Text);
			if (!display.Enabled)
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => InsertThroughClerk(leaf.Command));
		}

		/// <summary>Runs a record-list insert: pending edits settle first, and the record list
		/// shows the new record.</summary>
		/// <returns>True when the record list inserted one.</returns>
		internal bool InsertThroughClerk(Command command)
		{
			SettleDetailEdits();
			var clerk = Clerk;
			return clerk != null && clerk.OnInsertItemInVector(command);
		}

		/// <summary>
		/// The Promote or Demote item for the row's subrecord: absent on a row with no object,
		/// a ghost row and a row whose object the command does not act on; disabled when
		/// <see cref="RecordNesting"/> refuses the move.
		/// </summary>
		/// <param name="demote">True for Demote, false for Promote.</param>
		internal DetailMenuItem RecordNestingItem(DetailMenuLeaf leaf, DetailField field, bool demote)
		{
			var target = RowObjectOf(field);
			if (target == null || field.GhostPrompt != null || !CommandActsOn(target, leaf.Command))
				return null;
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (!(demote ? RecordNesting.CanDemoteSubrecord(target) : RecordNesting.CanPromote(target)))
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => NestRecord(field, demote));
		}

		/// <summary>
		/// Demotes the row's subrecord under the sibling the user picks, or promotes it: pending
		/// edits settle first, the move is one undoable step, and the re-show focuses the
		/// record. A record promoted to the top level is shown on its own.
		/// </summary>
		/// <param name="demote">True for Demote, false for Promote.</param>
		/// <returns>True when the record moved.</returns>
		internal bool NestRecord(DetailField field, bool demote)
		{
			SettleDetailEdits();
			if (!(RowObjectOf(field) is IRnGenericRec record))
			{
				Logger.WriteEvent("Promote or Demote did nothing: the row's object " + field?.ObjectHvo + " is no record.");
				return false;
			}
			if (demote)
			{
				var newOwner = RecordNesting.ChooseOwner(RecordNesting.SubrecordDemoteOwners(record),
					candidates => ChooseDemotedRecordOwner(candidates, subrecord: true));
				if (newOwner == null || !RecordNesting.Demote(record, newOwner))
					return false;
			}
			else if (!RecordNesting.Promote(record))
			{
				return false;
			}
			m_avaloniaEntryForm?.FocusObjectOnNextShow(record.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			// Last: the jump's filter prompt can replace the view under this call.
			if (!demote && RecordNesting.IsTopLevel(record))
			{
				Publisher.Publish(new PublisherParameterObject(EventConstants.JumpToRecord, record.Hvo,
					m_propertyTable.GetWindow()));
			}
			return true;
		}

		/// <summary>
		/// The pane bar's Demote Record: enabled when the shown record is a top-level record
		/// with another one to go under.
		/// </summary>
		internal DetailMenuItem DemoteRecordItem(DetailMenuLeaf leaf)
		{
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (!RecordNesting.CanDemoteRecord(ShownRecordTarget(leaf.Command)))
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => DemoteShownRecord(leaf.Command));
		}

		/// <summary>
		/// Makes the shown record the first subrecord of the record the user picks: pending
		/// edits settle first, the move is one undoable step, and the re-show focuses it.
		/// </summary>
		/// <returns>True when the record moved.</returns>
		internal bool DemoteShownRecord(Command command)
		{
			SettleDetailEdits();
			if (!(ShownRecordTarget(command) is IRnGenericRec record) || !RecordNesting.CanDemoteRecord(record))
			{
				Logger.WriteEvent("Demote Record did nothing: the shown record cannot be demoted.");
				return false;
			}
			var newOwner = RecordNesting.ChooseOwner(RecordNesting.RecordDemoteOwners(record),
				candidates => ChooseDemotedRecordOwner(candidates, subrecord: false));
			if (newOwner == null || !RecordNesting.Demote(record, newOwner))
				return false;
			m_avaloniaEntryForm?.FocusObjectOnNextShow(record.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		private IRnGenericRec ChooseDemotedRecordOwner(IReadOnlyList<IRnGenericRec> candidates, bool subrecord)
		{
			if (DemotedRecordOwnerChosen != null)
				return DemotedRecordOwnerChosen(candidates, subrecord);
			return subrecord
				? DemotedRecordOwnerChooser.ChooseForSubrecord(Cache, m_mediator, m_propertyTable, candidates)
				: DemotedRecordOwnerChooser.ChooseForRecord(Cache, m_mediator, m_propertyTable, candidates);
		}

		/// <summary>
		/// Find in Dictionary or Add to Dictionary on the main menu or toolbar, for the current
		/// row's structured-text selection: absent when it has none.
		/// </summary>
		/// <param name="addToLexicon">True for Add to Dictionary.</param>
		internal DetailMenuItem MainMenuTextWordItem(DetailMenuLeaf leaf, bool addToLexicon)
		{
			var selection = (CurrentTextSelectionSource ?? (() => m_avaloniaEntryForm?.CurrentTextSelection))();
			return selection == null ? null
				: TextWordItem(XCoreMenuBridge.StripAccelerator(leaf.Label), selection, addToLexicon);
		}

		/// <summary>
		/// A structured-text row's text-menu item for <paramref name="leaf"/>, acting on
		/// <paramref name="selection"/>; disabled without one.
		/// </summary>
		internal DetailMenuItem StructuredTextItem(DetailMenuLeaf leaf, DetailTextSelectionSnapshot selection)
		{
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (selection == null)
				return DetailMenuItem.Disabled(label);
			switch (leaf.Message)
			{
				case LexiconLookupMessage:
					return TextWordItem(label, selection, addToLexicon: false);
				case AddToLexiconMessage:
					return TextWordItem(label, selection, addToLexicon: true);
				case EditCopyMessage:
					return selection.SelectedText.Length == 0 ? DetailMenuItem.Disabled(label)
						: new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
							execute: () => CopyText(selection));
				case EditCutMessage:
					return selection.SelectedText.Length == 0 || !selection.CanEdit ? DetailMenuItem.Disabled(label)
						: new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
							execute: () => ReplaceSelectedText(selection, cut: true));
				case EditPasteMessage:
					return !selection.CanEdit || !TextClipboard.ContainsText() ? DetailMenuItem.Disabled(label)
						: new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
							execute: () => ReplaceSelectedText(selection, cut: false));
				default:
					throw new InvalidOperationException("No text command answers '" + leaf.Message + "'.");
			}
		}

		/// <summary>
		/// Find in Dictionary or Add to Dictionary for the word at the start of
		/// <paramref name="selection"/>: enabled when <see cref="TextWordCommands"/> lets the
		/// command take the word, judged on the text as edited so far.
		/// </summary>
		/// <param name="addToLexicon">True for Add to Dictionary.</param>
		internal DetailMenuItem TextWordItem(string label, DetailTextSelectionSnapshot selection, bool addToLexicon)
		{
			if (!ReferenceEquals(selection, m_wordSelection))
			{
				m_wordText = DetailRichTextAdapter.ToTsString(selection.Text, Cache.WritingSystemFactory,
					Cache.DefaultAnalWs);
				FindWordAt(m_wordText, Math.Min(selection.Anchor, selection.End), out m_wordMin, out m_wordLim);
				m_wordSelection = selection;
			}
			if (!WordCommandApplies(m_wordText, m_wordMin, m_wordLim, addToLexicon))
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => RunTextWordCommand(selection, addToLexicon));
		}

		/// <summary>
		/// Runs Find in Dictionary or Add to Dictionary on the word at the start of the
		/// selection: pending edits settle first, and the command does nothing unless the saved
		/// paragraph holds the text the selection was made in.
		/// </summary>
		/// <returns>True when the command ran.</returns>
		internal bool RunTextWordCommand(DetailTextSelectionSnapshot selection, bool addToLexicon)
		{
			SettleDetailEdits();
			var paragraph = SavedParagraphOf(selection);
			if (paragraph == null)
				return false;
			var tss = paragraph.Contents;
			SavedSpan(selection, tss.Text, out var spanMin, out _);
			FindWordAt(tss, spanMin, out var ichMin, out var ichLim);
			if (!WordCommandApplies(tss, ichMin, ichLim, addToLexicon))
				return false;
			if (addToLexicon)
				(AddWordLaunch ?? AddWord)(TextWordCommands.FormOf(tss, ichMin, ichLim));
			else
				(WordLookupLaunch ?? LookUpWord)(paragraph, TextWordCommands.WritingSystemOf(tss, ichMin, ichLim), ichMin, ichLim);
			return true;
		}

		/// <summary>Puts the selected text on the clipboard, with its writing systems and
		/// styles.</summary>
		internal void CopyText(DetailTextSelectionSnapshot selection)
		{
			var tss = DetailRichTextAdapter.ToTsString(selection.Text, Cache.WritingSystemFactory, Cache.DefaultAnalWs);
			SelectedSpan(selection, tss.Length, out var ichMin, out var ichLim);
			TextClipboard.SetText(TextClipboard.FromTsString(tss.GetSubstring(ichMin, ichLim)));
		}

		/// <summary>
		/// Cuts the selected text to the clipboard, or puts the clipboard's text in its place:
		/// pending edits settle first, the change is one undoable step on the saved paragraph,
		/// and the re-show focuses the row. Text after a line break on the clipboard is not
		/// pasted, since a paragraph holds no line break.
		/// </summary>
		/// <param name="cut">True for Cut, false for Paste.</param>
		/// <returns>True when the paragraph changed.</returns>
		internal bool ReplaceSelectedText(DetailTextSelectionSnapshot selection, bool cut)
		{
			SettleDetailEdits();
			var paragraph = SavedParagraphOf(selection);
			if (paragraph == null || !selection.CanEdit)
				return false;
			var contents = paragraph.Contents;
			SavedSpan(selection, contents.Text, out var ichMin, out var ichLim);
			ITsString replacement = null;
			if (cut)
			{
				if (ichLim == ichMin)
					return false;
				TextClipboard.SetText(TextClipboard.FromTsString(contents.GetSubstring(ichMin, ichLim)));
			}
			else
			{
				replacement = PastedText(contents, ichMin);
				if (replacement == null)
					return false;
			}
			ResourceHelper.MakeUndoRedoLabels(cut ? "kstidEditCut" : "kstidEditPaste", out var undo, out var redo);
			UndoableUnitOfWorkHelper.Do(undo, redo, Cache.ActionHandlerAccessor, () =>
			{
				var builder = contents.GetBldr();
				if (replacement == null)
					builder.Replace(ichMin, ichLim, null, null);
				else
					builder.ReplaceTsString(ichMin, ichLim, replacement);
				paragraph.Contents = builder.GetString();
			});
			m_avaloniaEntryForm?.FocusObjectOnNextShow(selection.Field.ObjectHvo, selection.Field.Field);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		// The clipboard's text up to any line break, with its own writing systems and styles,
		// or as plain text in those of the text before ichMin; null when there is none.
		private ITsString PastedText(ITsString contents, int ichMin)
		{
			var payload = TextClipboard.GetText();
			if (payload == null)
				return null;
			var tss = TextClipboard.ToTsString(payload);
			if (tss == null)
			{
				var props = contents.get_PropertiesAt(Math.Max(0, Math.Min(ichMin - 1, contents.Length - 1)));
				var builder = contents.GetBldr();
				builder.Replace(0, builder.Length, payload.PlainText, props);
				tss = builder.GetString();
			}
			var text = tss.Text ?? string.Empty;
			var lineBreak = text.IndexOfAny(new[] { '\r', '\n', (char)0x2028, (char)0x2029 });
			if (lineBreak >= 0)
				tss = tss.GetSubstring(0, lineBreak);
			return tss.Length == 0 ? null : tss;
		}

		// The word Find in Dictionary takes around ich, the selection's first point.
		private void FindWordAt(ITsString tss, int ich, out int ichMin, out int ichLim)
		{
			var styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			Func<string, ITsTextProps> styleProperties = null;
			if (styleSheet != null)
				styleProperties = name => styleSheet.GetStyleRgch(name.Length, name);
			TextWordBoundaries.Find(tss, ich, Cache.WritingSystemFactory, styleProperties, out ichMin, out ichLim);
		}

		private bool WordCommandApplies(ITsString tss, int ichMin, int ichLim, bool addToLexicon)
			=> addToLexicon
				? TextWordCommands.CanAddToLexicon(Cache, tss, ichMin, ichLim, 0)
				: TextWordCommands.CanLookUp(ichMin, ichLim);

		private static void SelectedSpan(DetailTextSelectionSnapshot selection, int length, out int ichMin,
			out int ichLim)
		{
			ichMin = Math.Max(0, Math.Min(Math.Min(selection.Anchor, selection.End), length));
			ichLim = Math.Max(ichMin, Math.Min(Math.Max(selection.Anchor, selection.End), length));
		}

		// The selection's span in savedText, which holds the selection's text, perhaps in
		// another Unicode normalization form.
		private static void SavedSpan(DetailTextSelectionSnapshot selection, string savedText, out int ichMin,
			out int ichLim)
		{
			var text = selection.PlainText;
			SelectedSpan(selection, text.Length, out var min, out var lim);
			ichMin = EquivalentOffset(text, min, savedText ?? string.Empty);
			ichLim = EquivalentOffset(text, lim, savedText ?? string.Empty);
		}

		// The point in target matching offset in source, the same text perhaps differently
		// normalized: where target's decomposed length reaches that of source up to offset.
		private static int EquivalentOffset(string source, int offset, string target)
		{
			if (string.Equals(source, target, StringComparison.Ordinal))
				return offset;
			var nfd = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD);
			var wanted = nfd.Normalize(source.Substring(0, offset)).Length;
			var reached = 0;
			var ich = 0;
			while (ich < target.Length && reached < wanted)
			{
				// A decomposed length adds up code point by code point.
				var cch = char.IsSurrogatePair(target, ich) ? 2 : 1;
				reached += nfd.Normalize(target.Substring(ich, cch)).Length;
				ich += cch;
			}
			return ich;
		}

		// Whether two strings hold the same text, perhaps in different normalization forms.
		private static bool SameText(string first, string second)
		{
			if (string.Equals(first, second, StringComparison.Ordinal))
				return true;
			var nfd = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD);
			return string.Equals(nfd.Normalize(first), nfd.Normalize(second), StringComparison.Ordinal);
		}

		// The saved paragraph the selection was made in; null, and logged, when it is gone or
		// now holds other text.
		private IStTxtPara SavedParagraphOf(DetailTextSelectionSnapshot selection)
		{
			var owner = RowObjectOf(selection.Field);
			IStTxtPara paragraph = null;
			if (owner != null && !string.IsNullOrEmpty(selection.Field.Field))
			{
				var flid = Cache.MetaDataCacheAccessor.GetFieldId2(owner.ClassID, selection.Field.Field, true);
				var textHvo = Cache.DomainDataByFlid.get_ObjectProp(owner.Hvo, flid);
				if (textHvo != 0 && Cache.ServiceLocator.GetObject(textHvo) is IStText text
					&& selection.ParagraphIndex >= 0 && selection.ParagraphIndex < text.ParagraphsOS.Count)
				{
					paragraph = text.ParagraphsOS[selection.ParagraphIndex] as IStTxtPara;
				}
			}
			if (paragraph == null || !SameText(paragraph.Contents?.Text ?? string.Empty, selection.PlainText))
			{
				Logger.WriteEvent("Text command did nothing: the paragraph changed or is gone.");
				return null;
			}
			return paragraph;
		}

		private void LookUpWord(IStTxtPara paragraph, int ws, int ichMin, int ichLim)
			=> LexEntryUi.DisplayOrCreateEntry(Cache, paragraph.Hvo, StTxtParaTags.kflidContents, ws, ichMin, ichLim,
				this, m_mediator, m_propertyTable, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
				"UserHelpFile");

		private void AddWord(ITsString form)
		{
			using (var dlg = new InsertEntryDlg())
			{
				dlg.SetDlgInfo(Cache, form, m_mediator, m_propertyTable);
				dlg.ShowDialog(this);
			}
		}
	}
}
