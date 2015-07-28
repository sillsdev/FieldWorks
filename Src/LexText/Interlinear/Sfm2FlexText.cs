// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Sfm2Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.FDO.DomainServices;
using SilEncConverters40;

namespace SIL.FieldWorks.IText
{
   /// <summary>
	/// This class is responsible for converting SFM files to the FlexText interlinear XML format that we
	/// know how to import.
	/// The importer is designed to import a single text per operation. Therefore, we produce the output
	/// from a single stream as a dictionary from text name to a stream from which the FlexText for a text
	/// of that name can be read.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_writer gets disposed in Convert method")]
	internal class Sfm2FlexText : Sfm2FlexTextBase<InterlinearMapping>
	{
		// true when we have already added a "words" element to the current "phrase". Meaningless if there is not a phrase open at all.
		private bool m_phraseHasWords;
		private bool m_textHasContent;
		// set emptied when we open a new phrase recording non-repeatable item-type/writing system combinations that have already occurred.
		private HashSet<Tuple<InterlinDestination, string>> m_itemsInThisPhrase = new HashSet<Tuple<InterlinDestination, string>>();
		// set emptied when we open a new text recording non-repeatable item-type/writing system combinations that have already occurred.
		private HashSet<Tuple<InterlinDestination, string>> m_itemsInThisText = new HashSet<Tuple<InterlinDestination, string>>();

		internal Sfm2FlexText() : base (new List<string>(new [] { "document", "interlinear-text",
			"paragraphs", "paragraph", "phrases", "phrase", "words", "word" }))
		{
		}

		protected override void WriteToDocElement(byte[] data, InterlinearMapping mapping)
		{
			switch (mapping.Destination)
			{
					// Todo: many cases need more checks for correct state.
				default: // Ignored
					break;
				case InterlinDestination.Source:
					MakeRootItem(mapping, data, "source");
					break;
				case InterlinDestination.Abbreviation:
					MakeRootItem(mapping, data, "title-abbreviation");
					break;
				case InterlinDestination.Title:
					MakeRootItem(mapping, data, "title");
					break;
				case InterlinDestination.Comment:
					MakeRootItem(mapping, data, "comment");
					break;
				case InterlinDestination.ParagraphBreak:
					HandleParaBreak();
					break;
				case InterlinDestination.Reference:
					HandleReference(mapping, data);
					break;
				case InterlinDestination.Baseline:
					HandleBaseline(mapping, data);
					break;
				case InterlinDestination.FreeTranslation:
					MakeRepeatableItem(mapping, data, "gls", "phrase", m_itemsInThisPhrase);
					break;
				case InterlinDestination.LiteralTranslation:
					MakeRepeatableItem(mapping, data, "lit", "phrase", m_itemsInThisPhrase);
					break;
				case InterlinDestination.Note:
					MakeItem(mapping, data, "note", "phrase");
					break;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="data"></param>
		/// <param name="itemType"></param>
		protected override void MakeRootItem(InterlinearMapping mapping, byte[] data, string itemType)
		{
			if (m_textHasContent)
				WriteStartElementIn("interlinear-text", "document");
			MakeRepeatableItem(mapping, data, itemType, "interlinear-text", m_itemsInThisText);
		}

		protected override void WriteStartElement(string marker)
		{
			base.WriteStartElement(marker);
			if (marker == "phrase")
			{
				m_phraseHasWords = false;
				m_itemsInThisPhrase.Clear();
			}
			else if (marker == "words")
			{
				m_phraseHasWords = true;
			}
			else if (marker == "interlinear-text")
			{
				m_textHasContent = false;
				m_itemsInThisText.Clear();
			}
		}

		private void HandleBaseline(InterlinearMapping mapping, byte[] data)
		{
			m_textHasContent = true;
			var text = GetString(data, mapping).Trim();

			var ws = m_wsf.get_Engine(mapping.WritingSystem).Handle; // don't use GetWsFromStr, fails if not a known WS
			var tss = m_tsf.MakeString(text, ws);
			var wordmaker = new WordMaker(tss, m_wsf);
			int ichLast = 0;
			int ichMin, ichLim;
			while (true)
			{
				var word = wordmaker.NextWord(out ichMin, out ichLim);
				if (word == null)
					ichMin = text.Length;
				if (ichLast < ichMin)
				{
					var punct = text.Substring(ichLast, ichMin - ichLast).Trim();
					if (punct.Length > 0)
					MakeWord(mapping, punct, "punct");
				}
				ichLast = ichLim;
				if (word != null)
					MakeWord(mapping, word.Text, "txt");
				else
					break;
			}
		}

		private void HandleParaBreak()
		{
			WriteStartElementIn("paragraph", "paragraphs");
		}

		private void HandleReference(InterlinearMapping mapping, byte[] data)
		{
			WriteStartElementIn("phrase", "phrases");
			MakeItem(mapping, data, "reference-label", "phrase");
		}

		private void MakeWord(InterlinearMapping mapping, string text, string itemType)
		{
			if (m_phraseHasWords && m_openElements.LastOrDefault() == "phrase")
			{
				// A phrase is currently open and already has a (completed) <words> element;
				// we need a new phrase.
				// We can accomplish this simply by terminating the current one;
				//the new one is automatically opened by WriteStartElementIn
				WriteEndElement();
			}
			WriteStartElementIn("word", "words");
			MakeItem(mapping, text, itemType, "word");
			WriteEndElement();
		}
	}
}
