// Copyright (c) 2012-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public partial class WordsSfmImportWizard : InterlinearSfmImportWizard
	{
		public WordsSfmImportWizard()
		{
			InitializeComponent();
			HelpTopicIdPrefix = "khtpField-WordsAndGlossesSfmImportWizard-Step";
			SetInitialHelpTopicID();
		}

		protected override void SetDialogTitle()
		{
			Text = string.Format(Text, ITextStrings.ksWordsAndGlosses);
		}

		protected override string SfmImportSettingsFileName => "WordsSfmImport.map";

		/// <summary>
		/// Don't care about checking the baseline in this dialog.
		/// TODO: Maybe check that there are words to import?
		/// </summary>
		protected override bool ValidateReadyToImport()
		{
			return true;
		}

		protected override IEnumerable<InterlinDestination> GetDestinationsFilter()
		{
			return new[] { InterlinDestination.Ignored, InterlinDestination.Wordform, InterlinDestination.WordGloss };
		}

		internal override Sfm2FlexTextBase<InterlinearMapping> GetSfmConverter()
		{
			return new Sfm2FlexTextWordsFrag();
		}

		protected override void DoStage2Conversion(byte[] stage1, IThreadedProgress dlg)
		{
			var stage2Converter = new LinguaLinksImport(m_cache, null, null);
			// Until we have a better idea, assume we're half done with the import when we've produced the intermediate.
			// TODO: we could do progress based on number of words to import.
			dlg.Position += 50;
			stage2Converter.ImportWordsFrag(() => new MemoryStream(stage1), ImportAnalysesLevel.WordGloss);
		}

		/// <summary>
		/// This converts Sfm to a subset of the FlexText xml standard that only deals with Words, their Glosses and their Morphology.
		/// This frag is special case (non-conforming) in that it can have multiple glosses in the same writing system.
		/// </summary>
		private sealed class Sfm2FlexTextWordsFrag : Sfm2FlexTextBase<InterlinearMapping>
		{
			private HashSet<Tuple<InterlinDestination, string>> m_txtItemsAddedToWord = new HashSet<Tuple<InterlinDestination, string>>();

			public Sfm2FlexTextWordsFrag()
				: base(new List<string>(new[] { "document", "word" }))
			{ }

			protected override void WriteToDocElement(byte[] data, InterlinearMapping mapping)
			{
				switch (mapping.Destination)
				{
					// Todo: many cases need more checks for correct state.
					default: // Ignored
						break;
					case InterlinDestination.Wordform:
						var key = new Tuple<InterlinDestination, string>(mapping.Destination, mapping.WritingSystem);
						// don't add more than one "txt" to word parent element
						if (m_txtItemsAddedToWord.Contains(key) && ParentElementIsOpen("word"))
						{
							WriteEndElement();
							m_txtItemsAddedToWord.Clear();
						}
						MakeItem(mapping, data, "txt", "word");
						m_txtItemsAddedToWord.Add(key);
						break;
					case InterlinDestination.WordGloss:
						// (For AdaptIt Knowledge Base sfm) it is okay to add more than one "gls" with same writing system to word parent element
						// this is a special case and probably doesn't strictly conform to FlexText standard.
						MakeItem(mapping, data, "gls", "word");
						break;
				}
			}
		}
	}
}