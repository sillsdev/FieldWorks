// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SpellCheckHelper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to facilitate adding spell-check items to menus and handling their events.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SpellCheckHelper
	{
		/// <summary>The Cache</summary>
		protected FdoCache m_cache;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SpellCheckHelper(FdoCache cache)
		{
			m_cache = cache;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and shows a context menu with spelling suggestions.
		/// </summary>
		/// <param name="pt">The location on the screen of the word for which we want spelling
		/// suggestions (usually the mouse position)</param>
		/// <param name="rootsite">The focused rootsite</param>
		/// <returns><c>true</c> if a menu was created and shown (with at least one item);
		/// <c>false</c> otherwise</returns>
		/// -----------------------------------------------------------------------------------
		public bool ShowContextMenu(Point pt, SimpleRootSite rootsite)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			MakeSpellCheckMenuOptions(pt, rootsite, menu);
			if (menu.Items.Count == 0)
				return false;
			menu.Show(rootsite, pt);
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// If the mousePos is part of a word that is not properly spelled, add to the menu
		/// options for correcting it.
		/// </summary>
		/// <param name="pt">The location on the screen of the word for which we want spelling
		/// suggestions (usually the mouse position)</param>
		/// <param name="rootsite">The focused rootsite</param>
		/// <param name="menu">to add items to.</param>
		/// <returns>the number of menu items added (not counting a possible separator line)</returns>
		/// -----------------------------------------------------------------------------------
		public virtual int MakeSpellCheckMenuOptions(Point pt, SimpleRootSite rootsite,
			ContextMenuStrip menu)
		{
			int hvoObj, tag, wsAlt, wsText;
			string word;
			ISpellEngine dict;
			bool nonSpellingError;
			ICollection<SpellCorrectMenuItem> suggestions = GetSuggestions(pt, rootsite,
				out hvoObj, out tag, out wsAlt, out wsText, out word, out dict, out nonSpellingError);
			if (suggestions == null)
				return 0;
			// no detectable spelling problem.

			// Note that items are inserted in order starting at the beginning, rather than
			// added to the end.  This is to support TE-6901.
			// If the menu isn't empty, add a separator.
			if (menu.Items.Count > 0)
				menu.Items.Insert(0, new ToolStripSeparator());

			// Make the menu option.
			ToolStripMenuItem itemExtras = null;
			int cSuggestions = 0;
			int iMenuItem = 0;
			IVwRootBox rootb = rootsite.RootBox;
			foreach (SpellCorrectMenuItem subItem in suggestions)
			{
				subItem.Click += spellingMenuItemClick;
				if (cSuggestions++ < RootSiteEditingHelper.kMaxSpellingSuggestionsInRootMenu)
				{
					Font createdFont = null;
					try
					{
					Font font = subItem.Font;
					if (wsText != 0)
					{
							font = createdFont = EditingHelper.GetFontForNormalStyle(wsText, rootb.Stylesheet,
							rootb.DataAccess.WritingSystemFactory);
						//string familyName = rootb.DataAccess.WritingSystemFactory.get_EngineOrNull(wsText).DefaultBodyFont;
						//font = new Font(familyName, font.Size, FontStyle.Bold);
					}

					subItem.Font = new Font(font, FontStyle.Bold);

					menu.Items.Insert(iMenuItem++, subItem);
				}
					finally
					{
						if (createdFont != null)
							createdFont.Dispose();
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
				ToolStripMenuItem noSuggestItems = new ToolStripMenuItem(RootSiteStrings.ksNoSuggestions);
				menu.Items.Insert(iMenuItem++, noSuggestItems);
				noSuggestItems.Enabled = false;
			}
			ToolStripMenuItem itemAdd = new AddToDictMenuItem(dict, word, rootb,
				hvoObj, tag, wsAlt, wsText, RootSiteStrings.ksAddToDictionary, m_cache);
			if (nonSpellingError)
				itemAdd.Enabled = false;
			menu.Items.Insert(iMenuItem++, itemAdd);
			itemAdd.Image = SIL.FieldWorks.Resources.ResourceHelper.SpellingIcon;
			itemAdd.Click += spellingMenuItemClick;
			return iMenuItem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handler for the Click event for items on the spelling context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void spellingMenuItemClick(object sender, EventArgs e)
		{
			SpellCorrectMenuItem item = sender as SpellCorrectMenuItem;
			if (item == null)
			{
				AddToDictMenuItem dict = sender as AddToDictMenuItem;
				Debug.Assert(dict != null, "invalid sender of spell check item");
				dict.AddWordToDictionary();
			}
			else
			{
				item.DoIt();
			}
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="we store a reference to SpellCorrectMenuItem for later use. REVIEW: we never dispose it.")]
		public ICollection<SpellCorrectMenuItem> GetSuggestions(Point mousePos,
			SimpleRootSite rootsite, out int hvoObj, out int tag, out int wsAlt, out int wsText,
			out string word, out ISpellEngine dict, out bool nonSpellingError)
		{
			hvoObj = tag = wsAlt = wsText = 0; // make compiler happy for early returns
			word = null;
			dict = null;
			nonSpellingError = true;

			IVwRootBox rootb = rootsite != null ? rootsite.RootBox : null;
			if (rootb == null)
				return null;

			// Get a selection at the indicated point.
			IVwSelection sel = rootsite.GetSelectionAtPoint(mousePos, false);

			// Get the selected word and verify that it is a single run within a single
			// editable string.
			if (sel != null)
				sel = sel.GrowToWord();
			if (sel == null || !sel.IsRange || sel.SelType != VwSelType.kstText || !SelectionHelper.IsEditable(sel))
				return null;
			ITsString tss;
			bool fAssocPrev;
			int ichAnchor;
			sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out wsAlt);
			int ichEnd, hvoObjE, tagE, wsE;
			sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjE, out tagE, out wsE);
			if (hvoObj != hvoObjE || tag != tagE || wsAlt != wsE)
				return null;

			int ichMin = Math.Min(ichEnd, ichAnchor);
			int ichLim = Math.Max(ichEnd, ichAnchor);

			ILgWritingSystemFactory wsf = rootsite.RootBox.DataAccess.WritingSystemFactory;

			// May need to enlarge the word beyond what GrowToWord does, if there is adjacent wordforming material.
			int ichMinAdjust = AdjustWordBoundary(wsf, tss, ichMin, -1, 0) + 1; // further expanded start of word.
			int ichLimAdjust = AdjustWordBoundary(wsf, tss, ichLim - 1, 1, tss.Length); // further expanded lim of word.
			// From the ends we can strip stuff with different spell-checking properties.
			IVwStylesheet styles = rootsite.RootBox.Stylesheet;
			int spellProps = SpellCheckProps(tss, ichMin, styles);
			while (ichMinAdjust < ichMin && SpellCheckProps(tss, ichMinAdjust, styles) != spellProps)
				ichMinAdjust++;
			while (ichLimAdjust > ichLim && SpellCheckProps(tss, ichLimAdjust - 1, styles) != spellProps)
				ichLimAdjust--;
			ichMin = ichMinAdjust;
			ichLim = ichLimAdjust;

			ITsStrFactory tsf = TsStrFactoryClass.Create();

			// Now we have the specific range we will check. Get the actual string.
			ITsStrBldr bldr = tss.GetBldr();
			if (ichLim < bldr.Length)
				bldr.ReplaceTsString(ichLim, bldr.Length, null);
			if (ichMin > 0)
				bldr.ReplaceTsString(0, ichMin, null);
			ITsString tssWord = bldr.GetString();

			// See whether we need the special blue underline, which is used mainly for adjacent words in different writing systems.
			List<int> wss = TsStringUtils.GetWritingSystems(tssWord);
			if (wss.Count > 1)
				return MakeWssSuggestions(tssWord, wss, rootb, hvoObj, tag, wsAlt, ichMin, ichLim);
			ITsString keepOrcs; // holds any ORCs we found in the original word that we need to keep rather than reporting.
			IList<SpellCorrectMenuItem> result = MakeEmbeddedNscSuggestion(ref tssWord, styles, rootb,
				hvoObj, tag, wsAlt, ichMin, ichLim, out keepOrcs);
			if (result.Count > 0)
				return result;

			// Determine whether it is a spelling problem.
			wsText = TsStringUtils.GetWsOfRun(tssWord, 0);
			dict = SpellingHelper.GetSpellChecker(wsText, wsf);
			if (dict == null)
				return null;
			word = tssWord.get_NormalizedForm(FwNormalizationMode.knmNFC).Text;
			if (word == null)
				return null; // don't think this can happen, but...
			if (dict.Check(word))
				return null; // not mis-spelled.

			// Get suggestions. Make sure to return an empty collection rather than null, even if no suggestions,
			// to indicate an error.
			ICollection<string> suggestions = dict.Suggest(word);
			foreach (string suggest in suggestions)
			{
				ITsString replacement = tsf.MakeStringRgch(suggest, suggest.Length, wsText);
				if (keepOrcs != null)
				{
					ITsStrBldr bldrRep = keepOrcs.GetBldr();
					bldrRep.ReplaceTsString(0, 0, replacement);
					replacement = bldrRep.GetString();
				}
				result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, suggest,
					replacement));
			}
			nonSpellingError = false; // it IS a spelling problem.
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a start character position that is within a word, and an delta that is +/- 1,
		/// return the index of the first non-wordforming (and non-number) character in that direction,
		/// or -1 if the start of the string is reached, or string.Length if the end is reached.
		/// For our purposes here, ORC (0xfffc) is considered word-forming.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int AdjustWordBoundary(ILgWritingSystemFactory wsf, ITsString tss, int ichStart,
			int delta, int lim)
		{
			string text = tss.Text;
			int ich;
			for (ich = ichStart + delta; !BeyondLim(ich, delta, lim); ich += delta)
			{
				ILgCharacterPropertyEngine cpe = TsStringUtils.GetCharPropEngineAtOffset(tss, wsf, ich);
				char ch = text[ich];
				if (!cpe.get_IsWordForming(ch) && !cpe.get_IsNumber(ch) && ch != 0xfffc)
					break;
			}
			return ich;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determins whether ich has passed the limit
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool BeyondLim(int ich, int delta, int lim)
		{
			return (delta < 0) ? (ich < lim) : (ich >= lim);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer the spelling status of the indicated character in the string, unless it is an
		/// ORC, in which case, for each ORC we answer a different value (that is not any of the
		/// valid spelling statuses).
		/// Enhance JohnT: we don't want to consider embedded-picture ORCs to count as
		/// different; we may strip them out before we start checking the word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int SpellCheckProps(ITsString tss, int ich, IVwStylesheet styles)
		{
			// For our purposes here, ORC (0xfffc) is considered to have a different spelling status from everything else,
			// even from every other ORC in the string. This means we always offer to insert spaces adjacent to them.
			if (ich < tss.Length && tss.GetChars(ich, ich + 1)[0] == 0xfffc)
			{
				return -50 - ich;
			}
			ITsTextProps props = tss.get_PropertiesAt(ich);
			string style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			int var, val;
			if (styles != null && !string.IsNullOrEmpty(style))
			{
				ITsTextProps styleProps = styles.GetStyleRgch(style.Length, style);
				if (styleProps != null)
				{
					val = styleProps.GetIntPropValues((int)FwTextPropType.ktptSpellCheck, out var);
					if (var != -1)
						return val; // style overrides
				}
			}
			val = props.GetIntPropValues((int)FwTextPropType.ktptSpellCheck, out var);
			if (var == -1)
				return 0; // treat unspecified the same as default.
			else
				return val;
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="we store a reference to SpellCorrectMenuItem for later use. REVIEW: we never dispose it.")]
		private IList<SpellCorrectMenuItem> MakeEmbeddedNscSuggestion(ref ITsString tssWord, IVwStylesheet styles, IVwRootBox rootb,
			int hvoObj, int tag, int wsAlt, int ichMin, int ichLim, out ITsString tssKeepOrcs)
		{
			List<SpellCorrectMenuItem> result = new List<SpellCorrectMenuItem>();
			// Make an item with inserted spaces.
			ITsStrBldr bldr = tssWord.GetBldr();
			int spCur = SpellCheckProps(tssWord, 0, styles);
			int offset = 0;
			bool foundDiff = false;
			bool fHasOrc = false;
			ITsStrBldr bldrWord = null;
			ITsStrBldr bldrKeepOrcs = null;
			int bldrWordOffset = 0;
			// Start at 0 even though we already got its props, because it just might be an ORC.
			for (int ich = 0; ich < tssWord.Length; ich++)
			{
				if (tssWord.GetChars(ich, ich + 1) == "\xfffc")
				{
					ITsTextProps ttp = tssWord.get_PropertiesAt(ich);
					string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (objData.Length == 0 || objData[0] != Convert.ToChar((int)FwObjDataTypes.kodtGuidMoveableObjDisp))
					{
						fHasOrc = true;
						int ichInsert = ich + offset;
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
							bldrKeepOrcs = TsStrBldrClass.Create();
						}
						bldrWord.Replace(ich - bldrWordOffset, ich - bldrWordOffset + 1, "", null);
						bldrKeepOrcs.Replace(bldrKeepOrcs.Length, bldrKeepOrcs.Length, "\xfffc", ttp);
						bldrWordOffset++;
					}
				}
				else // not an orc, see if props changed.
				{
					int spNew = SpellCheckProps(tssWord, ich, styles);
					if (spNew != spCur)
					{
						int ichInsert = ich + offset;
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
				return result;
			ITsString suggest = bldr.GetString();
			// There might still be an ORC in the string, in the pathological case of a picture anchor and embedded verse number
			// in the same word(!). Leave it in the replacement, but not in the menu item.
			string menuItemText = suggest.Text.Replace("\xfffc", "");
			if (fHasOrc)
				menuItemText = RootSiteStrings.ksInsertMissingSpaces;
			result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, menuItemText, suggest));
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of suggestions for what to do when a "word" consists of multiple
		/// writing systems
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="we store a reference to SpellCorrectMenuItem for later use. REVIEW: we never dispose it.")]
		private ICollection<SpellCorrectMenuItem> MakeWssSuggestions(ITsString tssWord,
			List<int> wss, IVwRootBox rootb, int hvoObj, int tag, int wsAlt,
			int ichMin, int ichLim)
		{
			List<SpellCorrectMenuItem> result = new List<SpellCorrectMenuItem>(wss.Count + 1);

			// Make an item with inserted spaces.
			ITsStrBldr bldr = tssWord.GetBldr();
			int wsFirst = TsStringUtils.GetWsOfRun(tssWord, 0);
			int offset = 0;
			for (int irun = 1; irun < tssWord.RunCount; irun++)
			{
				int wsNew = TsStringUtils.GetWsOfRun(tssWord, irun);
				if (wsNew != wsFirst)
				{
					int ichInsert = tssWord.get_MinOfRun(irun) + offset;
					bldr.Replace(ichInsert, ichInsert, " ", null);
					wsFirst = wsNew;
					offset++;
				}
			}
			ITsString suggest = bldr.GetString();
			string menuItemText = suggest.Text;
			result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, menuItemText, suggest));

			// And items for each writing system.
			foreach (int ws in wss)
			{
				bldr = tssWord.GetBldr();
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
				suggest = bldr.GetString();
				ILgWritingSystemFactory wsf = rootb.DataAccess.WritingSystemFactory;
				ILgWritingSystem engine = wsf.get_EngineOrNull(ws);
				string wsName = engine.LanguageName;
				string itemText = string.Format(RootSiteStrings.ksMlStringIsMono, tssWord.Text, wsName);
				result.Add(new SpellCorrectMenuItem(rootb, hvoObj, tag, wsAlt, ichMin, ichLim, itemText, suggest));
			}

			return result;
		}
	}

	// A helper class for MakeSpellCheckColleague. See comments there.
	///// <summary>
	///// A temporary colleague that contains enough information to correct a spelling error
	///// in a particular string.
	///// </summary>
	//internal class SpellCorrectColleague : IxCoreColleague
	//{
	//    IVwRootBox m_rootb;
	//    ICollection<string> m_suggestions;
	//    int m_hvoObj;
	//    int m_tag;
	//    int m_wsAlt;
	//    int m_wsText;
	//    int m_ichMin;
	//    int m_ichLim;
	//    string m_word; // supposedly incorrect word.
	//    Dictionary m_dict;
	//    EditingHelper m_helper;

	//    public SpellCorrectColleague(IVwRootBox rootb, ICollection<string> suggestions,
	//        int hvoObj, int tag, int wsAlt, int wsText, int ichMin, int ichLim, string word, Dictionary dict, EditingHelper helper)
	//    {
	//        m_rootb = rootb;
	//        m_suggestions = suggestions;
	//        m_hvoObj = hvoObj;
	//        m_tag = tag;
	//        m_wsAlt = wsAlt;
	//        m_wsText = wsText;
	//        m_ichMin = ichMin;
	//        m_ichLim = ichLim;
	//        m_word = word;
	//        m_dict = dict;
	//        m_helper = helper;
	//    }

	//    #region methods called by reflection (mediator.Broadcast)

	//    public bool OnDisplayPossibleCorrections(string wsList, UIListDisplayProperties display)
	//    {
	//        XCore.List items = display.List;
	//        XmlDocument doc = new XmlDocument();

	//        foreach (string suggestion in m_suggestions)
	//        {
	//            XmlNode paramNode = doc.CreateElement("param");
	//            XmlAttribute att = doc.CreateAttribute("correction");
	//            att.Value = suggestion;
	//            paramNode.Attributes.Append(att);
	//            items.Add(suggestion, suggestion, null, paramNode);
	//        }
	//        if (m_suggestions.Count == 0)
	//        {
	//            XmlNode paramNode = doc.CreateElement("param"); // dummy
	//            items.Add(SimpleRootSiteStrings.ksNoSuggestions, SimpleRootSiteStrings.ksNoSuggestions, null, paramNode);
	//        }
	//        return true;
	//    }

	//    /// <summary>
	//    /// We want to display the Correct Spelling item (and submenu) if we this colleague
	//    /// exists at all.
	//    /// </summary>
	//    /// <param name="arg"></param>
	//    /// <param name="display"></param>
	//    /// <returns></returns>
	//    public bool OnDisplayCorrectSpelling(object arg, UIItemDisplayProperties display)
	//    {
	//        display.Visible = true;
	//        display.Enabled = true;
	//        return true;
	//    }

	//    /// <summary>
	//    /// Mediator-called method to do spelling correction.
	//    /// </summary>
	//    /// <param name="arg"></param>
	//    /// <returns></returns>
	//    public bool OnCorrectSpelling(object arg)
	//    {
	//        XmlNode paramNode = arg as XmlNode;
	//        if (paramNode.Attributes["correction"] == null)
	//            return true; // "No suggestions" item.
	//        string correction = paramNode.Attributes["correction"].Value;
	//        m_rootb.DataAccess.BeginUndoTask(SimpleRootSiteStrings.ksUndoCorrectSpelling, SimpleRootSiteStrings.ksRedoSpellingChange);
	//        ITsStrBldr bldr = m_rootb.DataAccess.get_MultiStringAlt(m_hvoObj, m_tag, m_wsAlt).GetBldr();
	//        bldr.Replace(m_ichMin, m_ichLim, correction, null);
	//        m_rootb.DataAccess.SetMultiStringAlt(m_hvoObj, m_tag, m_wsAlt, bldr.GetString());
	//        m_rootb.DataAccess.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
	//        m_rootb.DataAccess.EndUndoTask();
	//        return true;
	//    }

	//    /// <summary>
	//    /// Mediator-called method to add 'incorrect' word to dictionary.
	//    /// </summary>
	//    /// <param name="arg"></param>
	//    /// <returns></returns>
	//    public bool OnAddToSpellDict(object arg)
	//    {
	//        m_helper.AddToSpellDict(m_dict, m_word, m_wsText);
	//        m_rootb.DataAccess.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
	//        return true;
	//    }
	//    #endregion methods called by reflection (mediator.Broadcast)

	//    #region IxCoreColleague Members

	//    /// <summary>
	//    /// No addtional ones, but include yourself.
	//    /// </summary>
	//    /// <returns></returns>
	//    public IxCoreColleague[] GetMessageTargets()
	//    {
	//        return new IxCoreColleague[] { this };
	//    }

	//    /// <summary>
	//    /// Required interface method, but nothing to do.
	//    /// </summary>
	//    /// <param name="mediator"></param>
	//    /// <param name="configurationParameters"></param>
	//    public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
	//    {

	//    }

	//    #endregion
	//}

	/// <summary>
	/// Menu item subclass containing the information needed to correct a spelling error.
	/// </summary>
	public class SpellCorrectMenuItem : ToolStripMenuItem
	{
		IVwRootBox m_rootb;
		int m_hvoObj;
		int m_tag;
		int m_wsAlt; // 0 if not multilingual--not yet implemented.
		int m_ichMin; // where to make the change.
		int m_ichLim; // end of string to replace
		ITsString m_tssReplacement;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SpellCorrectMenuItem(IVwRootBox rootb, int hvoObj, int tag, int wsAlt, int ichMin, int ichLim, string text, ITsString tss)
			: base(text)
		{
			m_rootb = rootb;
			m_hvoObj = hvoObj;
			m_tag = tag;
			m_wsAlt = wsAlt;
			m_ichMin = ichMin;
			m_ichLim = ichLim;
			m_tssReplacement = tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DoIt()
		{
			m_rootb.DataAccess.BeginUndoTask(RootSiteStrings.ksUndoCorrectSpelling, RootSiteStrings.ksRedoSpellingChange);
			ITsString tssInput;
			if (m_wsAlt == 0)
				tssInput = m_rootb.DataAccess.get_StringProp(m_hvoObj, m_tag);
			else
				tssInput = m_rootb.DataAccess.get_MultiStringAlt(m_hvoObj, m_tag, m_wsAlt);
			ITsStrBldr bldr = tssInput.GetBldr();
			bldr.ReplaceTsString(m_ichMin, m_ichLim, m_tssReplacement);
			if (m_wsAlt == 0)
				m_rootb.DataAccess.SetString(m_hvoObj, m_tag, bldr.GetString());
			else
				m_rootb.DataAccess.SetMultiStringAlt(m_hvoObj, m_tag, m_wsAlt, bldr.GetString());
			m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
			m_rootb.DataAccess.EndUndoTask();
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Menu item subclass containing the information needed to add an item to a dictionary.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class AddToDictMenuItem : ToolStripMenuItem
	{
		private readonly ISpellEngine m_dict;
		private readonly string m_word;
		private readonly IVwRootBox m_rootb;
		private readonly int m_hvoObj;
		private readonly int m_tag;
		private readonly int m_wsAlt; // 0 if not multilingual--not yet implemented.
		private readonly int m_wsText; // ws of actual word
		private readonly FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal AddToDictMenuItem(ISpellEngine dict, string word, IVwRootBox rootb,
			int hvoObj, int tag, int wsAlt, int wsText, string text, FdoCache cache)
			: base(text)
		{
			m_rootb = rootb;
			m_dict = dict;
			m_word = word;
			m_hvoObj = hvoObj;
			m_tag = tag;
			m_wsAlt = wsAlt;
			m_wsText = wsText;
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the current word to the dictionary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddWordToDictionary()
		{
			m_rootb.DataAccess.BeginUndoTask(RootSiteStrings.ksUndoAddToSpellDictionary,
				RootSiteStrings.ksRedoAddToSpellDictionary);
			if (m_rootb.DataAccess.GetActionHandler() != null)
				m_rootb.DataAccess.GetActionHandler().AddAction(new UndoAddToSpellDictAction(m_wsText, m_word, m_rootb,
				m_hvoObj, m_tag, m_wsAlt));
			AddToSpellDict(m_dict, m_word, m_wsText);
			m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
			m_rootb.DataAccess.EndUndoTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This information is useful for an override of MakeSpellCheckMenuOptions in TeEditingHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Word
		{
			get { return m_word; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The writing system of the actual mis-spelled word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int WritingSystem
		{
			get { return m_wsText; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the word to the spelling dictionary.
		/// Overrides to also add to the wordform inventory.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="word"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		private void AddToSpellDict(ISpellEngine dict, string word, int ws)
		{
			dict.SetStatus(word, true);

			if (m_cache == null)
				return; // bizarre, but means we just can't do it.

			// If it's in a current vernacular writing system, we want to update the WFI as well.
			if (!m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Any(wsObj => wsObj.Handle == ws))
				return;
			// Now get matching wordform (create if needed).
			var servLoc = m_cache.ServiceLocator;
			var wf = servLoc.GetInstance<IWfiWordformRepository>().GetMatchingWordform(ws, word);
			if (wf == null)
			{
				// Create it. (Caller has already started the UOW.)
				wf = servLoc.GetInstance<IWfiWordformFactory>().Create(
								m_cache.TsStrFactory.MakeString(word, ws));
			}
			wf.SpellingStatus = (int)SpellingStatusStates.correct;
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Supports undoing and redoing adding an item to a dictionary
	/// </summary>
	/// ------------------------------------------------------------------------------------
	class UndoAddToSpellDictAction : IUndoAction
	{
		private readonly int m_wsText;
		private readonly string m_word;
		private readonly int m_hvoObj;
		private readonly int m_tag;
		private readonly int m_wsAlt;
		private readonly IVwRootBox m_rootb;

		public UndoAddToSpellDictAction(int wsText, string word, IVwRootBox rootb,
			int hvoObj, int tag, int wsAlt)
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

		public bool IsDataChange
		{
			get { return true; }
		}

		public bool IsRedoable
		{
			get { return true; }
		}

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
