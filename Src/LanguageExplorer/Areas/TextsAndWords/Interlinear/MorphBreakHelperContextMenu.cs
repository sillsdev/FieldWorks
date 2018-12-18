// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Context menu to help build morpheme breaks
	/// </summary>
	public class MorphBreakHelperContextMenu : HelperContextMenu
	{
		private readonly LcmCache m_cache;
		private IMoMorphType m_mmtStem;
		private IMoMorphType m_mmtPrefix;
		private IMoMorphType m_mmtSuffix;
		private IMoMorphType m_mmtInfix;
		private IMoMorphType m_mmtBoundStem;
		private IMoMorphType m_mmtProclitic;
		private IMoMorphType m_mmtEnclitic;
		private IMoMorphType m_mmtSimulfix;
		private IMoMorphType m_mmtSuprafix;

		/// <summary />
		/// <param name="textbox">the textbox to insert regex characters into</param>
		/// <param name="helpTopicProvider">usually IHelpTopicProvider.App</param>
		/// <param name="cache">cache</param>
		public MorphBreakHelperContextMenu(FwTextBox textbox, IHelpTopicProvider helpTopicProvider, LcmCache cache)
			: base(textbox, helpTopicProvider)
		{
			m_cache = cache;
			Init();
		}

		private void Init()
		{
			m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetMajorMorphTypes(out m_mmtStem, out m_mmtPrefix, out m_mmtSuffix, out m_mmtInfix, out m_mmtBoundStem, out m_mmtProclitic, out m_mmtEnclitic, out m_mmtSimulfix, out m_mmtSuprafix);

			var sStemExample = StringTable.Table.GetString("EditMorphBreaks-stemExample", "DialogStrings");
			var sAffixExample = StringTable.Table.GetString("EditMorphBreaks-affixExample", "DialogStrings");
			var lbl_stemExample = string.Format(sStemExample, m_mmtStem.Prefix ?? string.Empty, m_mmtStem.Postfix ?? string.Empty);
			var lbl_boundStemExample = string.Format(sStemExample, m_mmtBoundStem.Prefix ?? string.Empty, m_mmtBoundStem.Postfix ?? string.Empty);
			var lbl_prefixExample = string.Format(sAffixExample, m_mmtPrefix.Prefix == null ? string.Empty : " " + m_mmtPrefix.Prefix, m_mmtPrefix.Postfix == null ? string.Empty : m_mmtPrefix.Postfix + " ");
			var lbl_suffixExample = string.Format(sAffixExample, m_mmtSuffix.Prefix == null ? string.Empty : " " + m_mmtSuffix.Prefix, m_mmtSuffix.Postfix == null ? string.Empty : m_mmtSuffix.Postfix + " ");
			var lbl_infixExample = string.Format(sAffixExample, m_mmtInfix.Prefix == null ? string.Empty : " " + m_mmtInfix.Prefix, m_mmtInfix.Postfix == null ? string.Empty : m_mmtInfix.Postfix + " ");
			var lbl_procliticExample = string.Format(sAffixExample, m_mmtProclitic.Prefix == null ? string.Empty : " " + m_mmtProclitic.Prefix, m_mmtProclitic.Postfix == null ? string.Empty : m_mmtProclitic.Postfix + " ");
			var lbl_encliticExample = string.Format(sAffixExample, m_mmtEnclitic.Prefix == null ? string.Empty : " " + m_mmtEnclitic.Prefix, m_mmtEnclitic.Postfix == null ? string.Empty : m_mmtEnclitic.Postfix + " ");
			var lbl_simulfixExample = string.Format(sAffixExample, m_mmtSimulfix.Prefix == null ? string.Empty : " " + m_mmtSimulfix.Prefix, m_mmtSimulfix.Postfix == null ? string.Empty : m_mmtSimulfix.Postfix + " ");
			var lbl_suprafixExample = string.Format(sAffixExample, m_mmtSuprafix.Prefix == null ? string.Empty : " " + m_mmtSuprafix.Prefix, m_mmtSuprafix.Postfix == null ? string.Empty : m_mmtSuprafix.Postfix + " ");

			MenuItems.Add(string.Format(FwCoreDlgs.ksStemMenuCmd, lbl_stemExample), stem);
			MenuItems.Add(string.Format(FwCoreDlgs.ksPrefixMenuCmd, lbl_prefixExample), prefix);
			MenuItems.Add(string.Format(FwCoreDlgs.ksSuffixMenuCmd, lbl_suffixExample), suffix);
			MenuItems.Add(string.Format(FwCoreDlgs.ksInfixMenuCmd, lbl_infixExample), infix);
			MenuItems.Add(string.Format(FwCoreDlgs.ksBdStemMenuCmd, lbl_boundStemExample), boundStem);
			MenuItems.Add(string.Format(FwCoreDlgs.ksProcliticMenuCmd, lbl_procliticExample), proclitic);
			MenuItems.Add(string.Format(FwCoreDlgs.ksEncliticMenuCmd, lbl_encliticExample), enclitic);
			MenuItems.Add(string.Format(FwCoreDlgs.ksSimulfixMenuCmd, lbl_simulfixExample), simulfix);
			MenuItems.Add(string.Format(FwCoreDlgs.ksSuprafixMenuCmd, lbl_suprafixExample), suprafix);

			MenuItems.Add("-");
			MenuItems.Add(FwCoreDlgs.ksMorphemeBreakHelp, showHelp);
		}

		// Determines what to insert based on the presence of prefix and postfix tokens
		private void morphBreakInsert(IMoMorphType morphType)
		{
			if (!string.IsNullOrEmpty(morphType.Prefix) && !string.IsNullOrEmpty(morphType.Postfix))
			{
				GroupText(prefixSpace() + morphType.Prefix, morphType.Postfix + postfixSpace(), true);
			}
			else if (!string.IsNullOrEmpty(morphType.Prefix))
			{
				InsertText(prefixSpace() + morphType.Prefix, false);
			}
			else if (!string.IsNullOrEmpty(morphType.Postfix))
			{
				InsertText(morphType.Postfix + postfixSpace(), false, true);
			}
		}

		// This one is different because for stems, spaces must always be included on both sides where needed
		private void morphBreakInsertStem(IMoMorphType morphType)
		{
			GroupText(prefixSpace() + morphType.Prefix, morphType.Postfix + postfixSpace(), true);
		}

		// Don't include a postfix space if we're at the end of the textbox
		private string postfixSpace()
		{
			return m_textbox.SelectionStart + m_textbox.SelectionLength == m_textbox.Text.Length ? string.Empty : " ";
		}

		// Don't include a prefix space if we're at the beginning of the textbox
		private string prefixSpace()
		{
			return m_textbox.SelectionStart == 0 ? string.Empty : " ";
		}

		// Event handlers
		private void stem(object sender, EventArgs e)
		{
			morphBreakInsertStem(m_mmtStem);
		}

		private void prefix(object sender, EventArgs e)
		{
			morphBreakInsert(m_mmtPrefix);
		}

		private void suffix(object sender, EventArgs e)
		{
			morphBreakInsert(m_mmtSuffix);
		}

		private void infix(object sender, EventArgs e)
		{
			morphBreakInsert(m_mmtInfix);
		}

		private void boundStem(object sender, EventArgs e)
		{
			morphBreakInsertStem(m_mmtBoundStem);
		}

		private void proclitic(object sender, EventArgs e)
		{
			morphBreakInsert(m_mmtProclitic);
		}

		private void enclitic(object sender, EventArgs e)
		{
			morphBreakInsert(m_mmtEnclitic);
		}

		private void simulfix(object sender, EventArgs e)
		{
			morphBreakInsert(m_mmtSimulfix);
		}

		private void suprafix(object sender, EventArgs e)
		{
			morphBreakInsert(m_mmtSuprafix);
		}

		private void showHelp(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", "khtpEditMorphBreaks");
		}
	}
}