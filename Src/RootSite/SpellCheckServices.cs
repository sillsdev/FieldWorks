// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Class to facilitate adding spell-check items to menus and handling their events.
	/// </summary>
	internal static class SpellCheckServices
	{
		/// <summary>
		/// Creates and shows a context menu with spelling suggestions.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="pt">The location on the screen of the word for which we want spelling
		/// suggestions (usually the mouse position)</param>
		/// <param name="rootsite">The focused rootsite</param>
		/// <returns><c>true</c> if a menu was created and shown (with at least one item);
		/// <c>false</c> otherwise</returns>
		internal static bool ShowContextMenu(LcmCache cache, Point pt, SimpleRootSite rootsite)
		{
			TearDownContextMenu(rootsite);
			var menu = new ContextMenuStrip();
			rootsite.ContextMenuStrip = menu;
			MakeSpellCheckMenuOptions(cache, pt, rootsite, menu);
			if (menu.Items.Count == 0)
			{
				TearDownContextMenu(rootsite);
				return false;
			}
			menu.Show(rootsite, pt);
			return true;
		}

		private static void TearDownContextMenu(Control rootsite)
		{
			if (rootsite.ContextMenuStrip != null)
			{
				UnwireEventHandlers(rootsite.ContextMenuStrip);
				rootsite.ContextMenuStrip.Dispose();
				rootsite.ContextMenuStrip = null;
			}
		}

		/// <summary>
		/// Unwire event handlers added to submenus
		/// </summary>
		internal static void UnwireEventHandlers(ContextMenuStrip menu)
		{
			foreach (var submenu in menu.Items)
			{
				if (submenu is AddToDictMenuItem || submenu is SpellCorrectMenuItem)
				{
					((ToolStripMenuItem)submenu).Click -= SpellingMenuItemClick;
				}
			}
		}

		/// <summary>
		/// If the mousePos is part of a word that is not properly spelled, add to the menu
		/// options for correcting it.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="pt">The location on the screen of the word for which we want spelling
		/// suggestions (usually the mouse position)</param>
		/// <param name="rootsite">The focused rootsite</param>
		/// <param name="menu">to add items to.</param>
		/// <returns>the number of menu items added (not counting a possible separator line)</returns>
		internal static int MakeSpellCheckMenuOptions(LcmCache cache, Point pt, SimpleRootSite rootsite, ContextMenuStrip menu)
		{
			var suggestions = GetSuggestions(pt, rootsite, out var hvoObj, out var tag, out var wsAlt, out var wsText, out var word, out var dict, out var nonSpellingError);
			if (suggestions == null)
			{
				return 0;
			}
			// no detectable spelling problem.

			// Note that items are inserted in order starting at the beginning, rather than
			// added to the end.  This is to support TE-6901.
			// If the menu isn't empty, add a separator.
			if (menu.Items.Count > 0)
			{
				menu.Items.Insert(0, new ToolStripSeparator());
			}
			// Make the menu option.
			ToolStripMenuItem itemExtras = null;
			var cSuggestions = 0;
			var iMenuItem = 0;
			var rootb = rootsite.RootBox;
			foreach (var subItem in suggestions)
			{
				subItem.Click += SpellingMenuItemClick;
				if (cSuggestions++ < RootSiteEditingHelper.kMaxSpellingSuggestionsInRootMenu)
				{
					Font createdFont = null;
					try
					{
						var font = subItem.Font;
						if (wsText != 0)
						{
							font = createdFont = FontHeightAdjuster.GetFontForNormalStyle(wsText, rootb.Stylesheet,
							rootb.DataAccess.WritingSystemFactory);
						}
						subItem.Font = new Font(font, FontStyle.Bold);
						menu.Items.Insert(iMenuItem++, subItem);
					}
					finally
					{
						createdFont?.Dispose();
					}
				}
				else
				{
					if (itemExtras == null)
					{
						itemExtras = new ToolStripMenuItem(RootSiteStrings.ksAdditionalSuggestions);
						menu.Items.Insert(iMenuItem++, itemExtras);
					}
					itemExtras.DropDownItems.Add(subItem);
				}
			}
			if (suggestions.Count == 0)
			{
				var noSuggestItems = new ToolStripMenuItem(RootSiteStrings.ksNoSuggestions);
				menu.Items.Insert(iMenuItem++, noSuggestItems);
				noSuggestItems.Enabled = false;
			}
			ToolStripMenuItem itemAdd = new AddToDictMenuItem(dict, word, rootb, hvoObj, tag, wsAlt, wsText, RootSiteStrings.ksAddToDictionary, cache);
			if (nonSpellingError)
			{
				itemAdd.Enabled = false;
			}
			menu.Items.Insert(iMenuItem++, itemAdd);
			itemAdd.Image = Resources.ResourceHelper.SpellingIcon;
			itemAdd.Click += SpellingMenuItemClick;
			return iMenuItem;
		}

		/// <summary>
		/// Handler for the Click event for items on the spelling context menu.
		/// </summary>
		private static void SpellingMenuItemClick(object sender, EventArgs e)
		{
			if (!(sender is SpellCorrectMenuItem item))
			{
				var dict = sender as AddToDictMenuItem;
				Debug.Assert(dict != null, "invalid sender of spell check item");
				dict.AddWordToDictionary();
			}
			else
			{
				item.DoIt();
			}
		}

		/// <summary>
		/// Get a list of suggested corrections if the selection is a spelling or similar error.
		/// Returns null if there is no problem at the selection location.
		/// Note that it may also return an empty list; this has a distinct meaning, namely,
		/// that there IS a problem, but we have no useful suggestions for what to change it to.
		/// nonSpellingError is set true when the error is not simply a mis-spelled word in a
		/// single writing system; currently this should disable or hide the commands to add
		/// the word to the dictionary or change multiple occurrences.
		/// The input arguments indicate where the user clicked and allow us to find the
		/// text he might be trying to correct. The other output arguments indicate which WS
		/// (wasAlt -- 0 for simple string) of which property (tag) of which object (hvoObj)
		/// is affected by the change, the ws of the mis-spelled word, and the corresponding
		/// spelling engine. Much of this information is already known to the
		/// SpellCorrectMenuItems returned, but some clients use it in creating other menu options.
		/// </summary>
		private static ICollection<SpellCorrectMenuItem> GetSuggestions(Point mousePos,
			SimpleRootSite rootsite, out int hvoObj, out int tag, out int wsAlt, out int wsText,
			out string word, out ISpellEngine dict, out bool nonSpellingError)
		{
			hvoObj = tag = wsAlt = wsText = 0; // make compiler happy for early returns
			word = null;
			dict = null;
			nonSpellingError = true;

			var rootb = rootsite?.RootBox;
			if (rootb == null)
			{
				return null;
			}
			// Get a selection at the indicated point.
			var sel = rootsite.GetSelectionAtPoint(mousePos, false);

			// Get the selected word and verify that it is a single run within a single
			// editable string.
			sel = sel?.GrowToWord();
			if (sel == null || !sel.IsRange || sel.SelType != VwSelType.kstText || !SelectionHelper.IsEditable(sel))
			{
				return null;
			}
			sel.TextSelInfo(false, out var tss, out var ichAnchor, out _, out hvoObj, out tag, out wsAlt);
			sel.TextSelInfo(true, out tss, out var ichEnd, out _, out var hvoObjE, out var tagE, out var wsE);
			if (hvoObj != hvoObjE || tag != tagE || wsAlt != wsE)
			{
				return null;
			}
			var ichMin = Math.Min(ichEnd, ichAnchor);
			var ichLim = Math.Max(ichEnd, ichAnchor);
			var wsf = rootsite.RootBox.DataAccess.WritingSystemFactory;

			// May need to enlarge the word beyond what GrowToWord does, if there is adjacent wordforming material.
			var ichMinAdjust = AdjustWordBoundary(wsf, tss, false, ichMin, 0) + 1; // further expanded start of word.
			// further expanded lim of word.
			var ichLimAdjust = AdjustWordBoundary(wsf, tss, true, ichLim - 1, tss.Length);
			// From the ends we can strip stuff with different spell-checking properties.
			var styles = rootsite.RootBox.Stylesheet;
			var spellProps = SpellCheckProps(tss, ichMin, styles);
			while (ichMinAdjust < ichMin && SpellCheckProps(tss, ichMinAdjust, styles) != spellProps)
			{
				ichMinAdjust++;
			}
			while (ichLimAdjust > ichLim && SpellCheckProps(tss, ichLimAdjust - 1, styles) != spellProps)
			{
				ichLimAdjust--;
			}
			ichMin = ichMinAdjust;
			ichLim = ichLimAdjust;

			// Now we have the specific range we will check. Get the actual string.
			var bldr = tss.GetBldr();
			if (ichLim < bldr.Length)
			{
				bldr.ReplaceTsString(ichLim, bldr.Length, null);
			}
			if (ichMin > 0)
			{
				bldr.ReplaceTsString(0, ichMin, null);
			}
			var tssWord = bldr.GetString();
			// See whether we need the special blue underline, which is used mainly for adjacent words in different writing systems.
			var wss = TsStringUtils.GetWritingSystems(tssWord);
			if (wss.Count > 1)
			{
				return MakeWssSuggestions(tssWord, wss, rootb, hvoObj, tag, wsAlt, ichMin, ichLim);
			}
			var result = MakeEmbeddedNscSuggestion(ref tssWord, styles, rootb, hvoObj, tag, wsAlt, ichMin, ichLim, out var orcsTokeep);
			if (result.Count > 0)
			{
				return result;
			}
			// Determine whether it is a spelling problem.
			wsText = TsStringUtils.GetWsOfRun(tssWord, 0);
			dict = SpellingHelper.GetSpellChecker(wsText, wsf);
			if (dict == null)
			{
				return null;
			}
			word = tssWord.get_NormalizedForm(FwNormalizationMode.knmNFC).Text;
			if (word == null)
			{
				return null; // don't think this can happen, but...
			}
			if (dict.Check(word))
			{
				return null; // not mis-spelled.
			}
			// Get suggestions. Make sure to return an empty collection rather than null, even if no suggestions,
			// to indicate an error.
			var suggestions = dict.Suggest(word);
			foreach (var suggest in suggestions)
			{
				var replacement = TsStringUtils.MakeString(suggest, wsText);
				if (orcsTokeep != null)
				{
					var bldrRep = orcsTokeep.GetBldr();
					bldrRep.ReplaceTsString(0, 0, replacement);
					replacement = bldrRep.GetString();
				}
				result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, suggest, replacement));
			}
			nonSpellingError = false; // it IS a spelling problem.
			return result;
		}

		/// <summary>
		/// Given a start character position that is within a word, and a direction
		/// return the index of the first non-wordforming (and non-number) character in that direction,
		/// or -1 if the start of the string is reached, or string.Length if the end is reached.
		/// For our purposes here, ORC (0xfffc) is considered word-forming.
		/// </summary>
		private static int AdjustWordBoundary(ILgWritingSystemFactory wsf, ITsString tss, bool forward, int ichStart, int lim)
		{
			int ich;
			for (ich = NextCharIndex(tss, forward, ichStart); !BeyondLim(forward, ich, lim); ich = NextCharIndex(tss, forward, ich))
			{
				var ch = tss.CharAt(ich);
				var ws = wsf.get_EngineOrNull(tss.get_WritingSystemAt(ich));
				if (!ws.get_IsWordForming(ch) && !Icu.Character.IsNumeric(ch) && ch != 0xfffc)
				{
					break;
				}
			}
			return ich;
		}

		/// <summary>
		/// Determines whether ich has passed the limit
		/// </summary>
		private static bool BeyondLim(bool forward, int ich, int lim)
		{
			return forward ? ich >= lim : ich < lim;
		}

		private static int NextCharIndex(ITsString tss, bool forward, int ich)
		{
			return forward ? tss.NextCharIndex(ich) : tss.PrevCharIndex(ich);
		}

		/// <summary>
		/// Answer the spelling status of the indicated character in the string, unless it is an
		/// ORC, in which case, for each ORC we answer a different value (that is not any of the
		/// valid spelling statuses).
		/// Enhance JohnT: we don't want to consider embedded-picture ORCs to count as
		/// different; we may strip them out before we start checking the word.
		/// </summary>
		private static int SpellCheckProps(ITsString tss, int ich, IVwStylesheet styles)
		{
			// For our purposes here, ORC (0xfffc) is considered to have a different spelling status from everything else,
			// even from every other ORC in the string. This means we always offer to insert spaces adjacent to them.
			if (ich < tss.Length && tss.GetChars(ich, ich + 1)[0] == 0xfffc)
			{
				return -50 - ich;
			}
			var props = tss.get_PropertiesAt(ich);
			var style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			int var, val;
			if (styles != null && !string.IsNullOrEmpty(style))
			{
				var styleProps = styles.GetStyleRgch(style.Length, style);
				if (styleProps != null)
				{
					val = styleProps.GetIntPropValues((int)FwTextPropType.ktptSpellCheck, out var);
					if (var != -1)
					{
						return val; // style overrides
					}
				}
			}
			val = props.GetIntPropValues((int)FwTextPropType.ktptSpellCheck, out var);
			return var == -1 ? 0 : val;
		}

		/// <summary>
		/// Make 'suggestion' menu items for the case where the 'word' contains embedded stuff that we don't spell-check
		/// (typically CV numbers) or ORCs. The current suggestion inserts spaces adjacent to the embedded stuff.
		/// If possible, we show what the result will look like; this isn't possible for orcs, so we just display "insert missing spaces".
		/// Enhance JohnT: we want two more menu items, one offering to move all the problem stuff to the start, one to the end.
		/// This is tricky to word; we will need an override that subclasses implement to provide a user-friendly description
		/// of a caller.
		/// There is a special case for certain ORCs which are not visible (picture anchors, basically). These should not
		/// affect the menu options offered, but they must not be replaced by a new spelling. If these are found, we update
		/// tssWord to something that does not contain them, and retain the orcs to append to any substitutions (returned in tssKeepOrcs).
		/// </summary>
		private static IList<SpellCorrectMenuItem> MakeEmbeddedNscSuggestion(ref ITsString tssWord, IVwStylesheet styles, IVwRootBox rootb,
			int hvoObj, int tag, int wsAlt, int ichMin, int ichLim, out ITsString tssKeepOrcs)
		{
			var result = new List<SpellCorrectMenuItem>();
			// Make an item with inserted spaces.
			var bldr = tssWord.GetBldr();
			var spCur = SpellCheckProps(tssWord, 0, styles);
			var offset = 0;
			var foundDiff = false;
			var fHasOrc = false;
			ITsStrBldr bldrWord = null;
			ITsStrBldr bldrKeepOrcs = null;
			var bldrWordOffset = 0;
			// Start at 0 even though we already got its props, because it just might be an ORC.
			for (var ich = 0; ich < tssWord.Length; ich++)
			{
				if (tssWord.GetChars(ich, ich + 1) == "\xfffc")
				{
					var ttp = tssWord.get_PropertiesAt(ich);
					var objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (objData.Length == 0 || objData[0] != Convert.ToChar((int)FwObjDataTypes.kodtGuidMoveableObjDisp))
					{
						fHasOrc = true;
						var ichInsert = ich + offset;
						bldr.Replace(ichInsert, ichInsert, " ", null);
						spCur = -50 - ich; // Same trick as SpellCheckProps to ensure won't match anything following.
						offset++;
						foundDiff = true;
					}
					else
					{
						// An ORC we want to ignore, but not lose. We will strip it out of the word we will
						// actually spell-check if we don't find other ORC problems, but save it to be
						// inserted at the end of any correction word. We might still use
						// our own "insert missing spaces" option, too, if we find another ORC of a different type.
						// In that case, this ORC just stays as part of the string, without spaces inserted.
						if (bldrWord == null)
						{
							bldrWord = tssWord.GetBldr();
							bldrKeepOrcs = TsStringUtils.MakeStrBldr();
						}
						bldrWord.Replace(ich - bldrWordOffset, ich - bldrWordOffset + 1, "", null);
						bldrKeepOrcs.Replace(bldrKeepOrcs.Length, bldrKeepOrcs.Length, "\xfffc", ttp);
						bldrWordOffset++;
					}
				}
				else // not an orc, see if props changed.
				{
					var spNew = SpellCheckProps(tssWord, ich, styles);
					if (spNew != spCur)
					{
						var ichInsert = ich + offset;
						bldr.Replace(ichInsert, ichInsert, " ", null);
						spCur = spNew;
						offset++;
						foundDiff = true;
					}
				}
			}
			if (bldrWord != null)
			{
				tssWord = bldrWord.GetString();
				tssKeepOrcs = bldrKeepOrcs.GetString();
			}
			else
			{
				tssKeepOrcs = null;
			}
			if (!foundDiff)
			{
				return result;
			}
			var suggest = bldr.GetString();
			// There might still be an ORC in the string, in the pathological case of a picture anchor and embedded verse number
			// in the same word(!). Leave it in the replacement, but not in the menu item.
			var menuItemText = suggest.Text.Replace("\xfffc", "");
			if (fHasOrc)
			{
				menuItemText = RootSiteStrings.ksInsertMissingSpaces;
			}
			result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, menuItemText, suggest));
			return result;
		}

		/// <summary>
		/// Gets a collection of suggestions for what to do when a "word" consists of multiple
		/// writing systems
		/// </summary>
		private static ICollection<SpellCorrectMenuItem> MakeWssSuggestions(ITsString tssWord, List<int> wss, IVwRootBox rootb, int hvoObj, int tag, int wsAlt, int ichMin, int ichLim)
		{
			var result = new List<SpellCorrectMenuItem>(wss.Count + 1);
			// Make an item with inserted spaces.
			var bldr = tssWord.GetBldr();
			var wsFirst = TsStringUtils.GetWsOfRun(tssWord, 0);
			var offset = 0;
			for (var irun = 1; irun < tssWord.RunCount; irun++)
			{
				var wsNew = TsStringUtils.GetWsOfRun(tssWord, irun);
				if (wsNew != wsFirst)
				{
					var ichInsert = tssWord.get_MinOfRun(irun) + offset;
					bldr.Replace(ichInsert, ichInsert, " ", null);
					wsFirst = wsNew;
					offset++;
				}
			}
			var suggest = bldr.GetString();
			var menuItemText = suggest.Text;
			result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, menuItemText, suggest));
			// And items for each writing system.
			foreach (var ws in wss)
			{
				bldr = tssWord.GetBldr();
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
				suggest = bldr.GetString();
				var wsf = rootb.DataAccess.WritingSystemFactory;
				var engine = wsf.get_EngineOrNull(ws);
				var wsName = engine.LanguageName;
				var itemText = string.Format(RootSiteStrings.ksMlStringIsMono, tssWord.Text, wsName);
				result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, itemText, suggest));
			}

			return result;
		}

		/// <summary>
		/// Menu item subclass containing the information needed to add an item to a dictionary.
		/// </summary>
		private sealed class AddToDictMenuItem : ToolStripMenuItem
		{
			private readonly ISpellEngine m_dict;
			private readonly IVwRootBox m_rootb;
			private readonly int m_hvoObj;
			private readonly int m_tag;
			private readonly int m_wsAlt; // 0 if not multilingual--not yet implemented.
			private readonly LcmCache m_cache;

			/// <summary />
			internal AddToDictMenuItem(ISpellEngine dict, string word, IVwRootBox rootb, int hvoObj, int tag, int wsAlt, int wsText, string text, LcmCache cache)
				: base(text)
			{
				m_rootb = rootb;
				m_dict = dict;
				Word = word;
				m_hvoObj = hvoObj;
				m_tag = tag;
				m_wsAlt = wsAlt;
				WritingSystem = wsText;
				m_cache = cache;
			}

			/// <summary />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
				base.Dispose(disposing);
			}

			/// <summary>
			/// Add the current word to the dictionary.
			/// </summary>
			internal void AddWordToDictionary()
			{
				m_rootb.DataAccess.BeginUndoTask(RootSiteStrings.ksUndoAddToSpellDictionary, RootSiteStrings.ksRedoAddToSpellDictionary);
				if (m_rootb.DataAccess.GetActionHandler() != null)
				{
					m_rootb.DataAccess.GetActionHandler().AddAction(new UndoAddToSpellDictAction(WritingSystem, Word, m_rootb, m_hvoObj, m_tag, m_wsAlt));
				}
				AddToSpellDict(m_dict, Word, WritingSystem);
				m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
				m_rootb.DataAccess.EndUndoTask();
			}

			/// <summary>
			/// This information is useful for an override of MakeSpellCheckMenuOptions in TeEditingHelper.
			/// </summary>
			private string Word { get; }

			/// <summary>
			/// The writing system of the actual mis-spelled word.
			/// </summary>
			private int WritingSystem { get; }

			/// <summary>
			/// Add the word to the spelling dictionary.
			/// Overrides to also add to the wordform inventory.
			/// </summary>
			private void AddToSpellDict(ISpellEngine dict, string word, int ws)
			{
				dict.SetStatus(word, true);
				if (m_cache == null)
				{
					return; // bizarre, but means we just can't do it.
				}
				// If it's in a current vernacular writing system, we want to update the WFI as well.
				if (m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.All(wsObj => wsObj.Handle != ws))
				{
					return;
				}
				// Now get matching wordform (create if needed).
				var servLoc = m_cache.ServiceLocator;
				var wf = servLoc.GetInstance<IWfiWordformRepository>().GetMatchingWordform(ws, word) ?? servLoc.GetInstance<IWfiWordformFactory>().Create(TsStringUtils.MakeString(word, ws));
				wf.SpellingStatus = (int)SpellingStatusStates.correct;
			}

			/// <summary>
			/// Supports undoing and redoing adding an item to a dictionary
			/// </summary>
			private sealed class UndoAddToSpellDictAction : IUndoAction
			{
				private readonly int m_wsText;
				private readonly string m_word;
				private readonly int m_hvoObj;
				private readonly int m_tag;
				private readonly int m_wsAlt;
				private readonly IVwRootBox m_rootb;

				internal UndoAddToSpellDictAction(int wsText, string word, IVwRootBox rootb, int hvoObj, int tag, int wsAlt)
				{
					m_wsText = wsText;
					m_word = word;
					m_hvoObj = hvoObj;
					m_tag = tag;
					m_wsAlt = wsAlt;
					m_rootb = rootb;
				}

				#region IUndoAction Members

				public void Commit()
				{
				}

				public bool IsDataChange => true;

				public bool IsRedoable => true;

				public bool Redo()
				{
					SpellingHelper.SetSpellingStatus(m_word, m_wsText, m_rootb.DataAccess.WritingSystemFactory, true);
					m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
					return true;
				}

				public bool SuppressNotification
				{
					set { }
				}

				public bool Undo()
				{
					SpellingHelper.SetSpellingStatus(m_word, m_wsText, m_rootb.DataAccess.WritingSystemFactory, false);
					m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
					return true;
				}

				#endregion
			}
		}
	}
}