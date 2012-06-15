

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class SfmWordsMappingDlg : SfmToTextsAndWordesMappingBaseDlg
	{
		public SfmWordsMappingDlg()
		{
			InitializeComponent();
			/*
			 * TODO: new helpTopicID
			 */
			m_helpTopicID = "khtpField-InterlinearSfmImportWizard-Step2";
		}

		protected override string GetDestinationName(InterlinDestination destEnum)
		{
			return GetDestinationNameFromResource(destEnum, LexTextControls.ResourceManager);
		}
	}
}
