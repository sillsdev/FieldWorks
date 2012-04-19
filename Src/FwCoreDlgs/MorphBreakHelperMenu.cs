using System;
using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Context menu to help build morpheme breaks
	/// </summary>
	public class MorphBreakHelperMenu : HelperMenu
	{
		private readonly FdoCache m_cache;
		private readonly StringTable m_stringTable;

		/// <summary>
		/// Constructor for Morph Break Helper Context Menu
		/// </summary>
		/// <param name="textbox">the textbox to insert regex characters into</param>
		/// <param name="helpTopicProvider">usually IHelpTopicProvider.App</param>
		/// <param name="cache">cache</param>
		/// <param name="stringTable">stringTable</param>
		public MorphBreakHelperMenu(FwTextBox textbox, IHelpTopicProvider helpTopicProvider, FdoCache cache, StringTable stringTable)
			: base(textbox, helpTopicProvider)
		{
			m_cache = cache;
			m_stringTable = stringTable;
			Init();
		}

		private IMoMorphType m_mmtStem;
		private IMoMorphType m_mmtPrefix;
		private IMoMorphType m_mmtSuffix;
		private IMoMorphType m_mmtInfix;
		private IMoMorphType m_mmtBoundStem;
		private IMoMorphType m_mmtProclitic;
		private IMoMorphType m_mmtEnclitic;
		private IMoMorphType m_mmtSimulfix;
		private IMoMorphType m_mmtSuprafix;

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Added to MenuItems collection and disposed there.")]
		private void Init()
		{
			m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetMajorMorphTypes(
				out m_mmtStem, out m_mmtPrefix, out m_mmtSuffix, out m_mmtInfix,
				out m_mmtBoundStem, out m_mmtProclitic, out m_mmtEnclitic,
				out m_mmtSimulfix, out m_mmtSuprafix);

			string sStemExample = m_stringTable.GetString("EditMorphBreaks-stemExample", "DialogStrings");
			string sAffixExample = m_stringTable.GetString("EditMorphBreaks-affixExample", "DialogStrings");

			string lbl_stemExample = String.Format(sStemExample,
				m_mmtStem.Prefix == null ? "" : m_mmtStem.Prefix,
				m_mmtStem.Postfix == null ? "" : m_mmtStem.Postfix);
			string lbl_boundStemExample = String.Format(sStemExample,
				m_mmtBoundStem.Prefix == null ? "" : m_mmtBoundStem.Prefix,
				m_mmtBoundStem.Postfix == null ? "" : m_mmtBoundStem.Postfix);
			string lbl_prefixExample = String.Format(sAffixExample,
				m_mmtPrefix.Prefix == null ? "" : " " + m_mmtPrefix.Prefix,
				m_mmtPrefix.Postfix == null ? "" : m_mmtPrefix.Postfix + " ");
			string lbl_suffixExample = String.Format(sAffixExample,
				m_mmtSuffix.Prefix == null ? "" : " " + m_mmtSuffix.Prefix,
				m_mmtSuffix.Postfix == null ? "" : m_mmtSuffix.Postfix + " ");
			string lbl_infixExample = String.Format(sAffixExample,
				m_mmtInfix.Prefix == null ? "" : " " + m_mmtInfix.Prefix,
				m_mmtInfix.Postfix == null ? "" : m_mmtInfix.Postfix + " ");
			string lbl_procliticExample = String.Format(sAffixExample,
				m_mmtProclitic.Prefix == null ? "" : " " + m_mmtProclitic.Prefix,
				m_mmtProclitic.Postfix == null ? "" : m_mmtProclitic.Postfix + " ");
			string lbl_encliticExample = String.Format(sAffixExample,
				m_mmtEnclitic.Prefix == null ? "" : " " + m_mmtEnclitic.Prefix,
				m_mmtEnclitic.Postfix == null ? "" : m_mmtEnclitic.Postfix + " ");
			string lbl_simulfixExample = String.Format(sAffixExample,
				m_mmtSimulfix.Prefix == null ? "" : " " + m_mmtSimulfix.Prefix,
				m_mmtSimulfix.Postfix == null ? "" : m_mmtSimulfix.Postfix + " ");
			string lbl_suprafixExample = String.Format(sAffixExample,
				m_mmtSuprafix.Prefix == null ? "" : " " + m_mmtSuprafix.Prefix,
				m_mmtSuprafix.Postfix == null ? "" : m_mmtSuprafix.Postfix + " ");

			MenuItems.Add(String.Format(FwCoreDlgs.ksStemMenuCmd, lbl_stemExample), new EventHandler(stem));
			MenuItems.Add(String.Format(FwCoreDlgs.ksPrefixMenuCmd, lbl_prefixExample), new EventHandler(prefix));
			MenuItems.Add(String.Format(FwCoreDlgs.ksSuffixMenuCmd, lbl_suffixExample), new EventHandler(suffix));
			MenuItems.Add(String.Format(FwCoreDlgs.ksInfixMenuCmd, lbl_infixExample), new EventHandler(infix));
			MenuItems.Add(String.Format(FwCoreDlgs.ksBdStemMenuCmd, lbl_boundStemExample), new EventHandler(boundStem));
			MenuItems.Add(String.Format(FwCoreDlgs.ksProcliticMenuCmd, lbl_procliticExample), new EventHandler(proclitic));
			MenuItems.Add(String.Format(FwCoreDlgs.ksEncliticMenuCmd, lbl_encliticExample), new EventHandler(enclitic));
			MenuItems.Add(String.Format(FwCoreDlgs.ksSimulfixMenuCmd, lbl_simulfixExample), new EventHandler(simulfix));
			MenuItems.Add(String.Format(FwCoreDlgs.ksSuprafixMenuCmd, lbl_suprafixExample), new EventHandler(suprafix));

			MenuItems.Add("-");
			MenuItems.Add(FwCoreDlgs.ksMorphemeBreakHelp, new EventHandler(showHelp));
		}

		// Determines what to insert based on the presence of prefix and postfix tokens
		private void morphBreakInsert(IMoMorphType morphType)
		{
			if (!String.IsNullOrEmpty(morphType.Prefix) && !String.IsNullOrEmpty(morphType.Postfix))
				GroupText(prefixSpace() + morphType.Prefix, morphType.Postfix + postfixSpace(), true);
			else if (!String.IsNullOrEmpty(morphType.Prefix))
				InsertText(prefixSpace() + morphType.Prefix, false);
			else if (!String.IsNullOrEmpty(morphType.Postfix))
				InsertText(morphType.Postfix + postfixSpace(), false, true);
		}

		// This one is different because for stems, spaces must always be included on both sides where needed
		private void morphBreakInsertStem(IMoMorphType morphType)
		{
			GroupText(prefixSpace() + morphType.Prefix, morphType.Postfix + postfixSpace(), true);
		}

		// Don't include a postfix space if we're at the end of the textbox
		private string postfixSpace()
		{
			int selLen = m_textbox.SelectionLength;
			int selStart = m_textbox.SelectionStart;
			int len = m_textbox.Text.Length;

			if (selStart + selLen == len)
				return "";
			else
				return " ";
		}

		// Don't include a prefix space if we're at the beginning of the textbox
		private string prefixSpace()
		{
			int selStart = m_textbox.SelectionStart;

			if (selStart == 0)
				return string.Empty;
			else
				return " ";
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
