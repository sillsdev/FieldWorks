using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.IText
{
	public partial class WordsSfmImportWizard : InterlinearSfmImportWizard
	{
		public WordsSfmImportWizard()
		{
			InitializeComponent();
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


	}
}
