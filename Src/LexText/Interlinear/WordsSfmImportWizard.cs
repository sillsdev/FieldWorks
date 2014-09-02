using System;
using System.Collections.Generic;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.IText
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
			Text = String.Format(Text, ITextStrings.ksWordsAndGlosses);
		}

		protected override string SfmImportSettingsFileName
		{
			get
			{
				return "WordsSfmImport.map";
			}
		}

		/// <summary>
		/// Don't care about checking the baseline in this dialog.
		/// TODO: Maybe check that their are words to import?
		/// </summary>
		/// <returns></returns>
		protected override bool ValidateReadyToImport()
		{
			return true;
		}

		protected override IEnumerable<InterlinDestination> GetDestinationsFilter()
		{
			return new[] { InterlinDestination.Ignored, InterlinDestination.Wordform, InterlinDestination.WordGloss };
		}

		protected override Sfm2FlexTextBase<InterlinearMapping> GetSfmConverter()
		{
			return new Sfm2FlexTextWordsFrag();
		}

		protected override void DoStage2Conversion(byte[] stage1, IThreadedProgress dlg)
		{
		   var stage2Converter = new LinguaLinksImport(m_cache, null, null);
			// Until we have a better idea, assume we're half done with the import when we've produced the intermediate.
			// TODO: we could do progress based on number of words to import.
			dlg.Position += 50;
			stage2Converter.ImportWordsFrag(() => new MemoryStream(stage1), LinguaLinksImport.ImportAnalysesLevel.WordGloss);
		}




	}
}
