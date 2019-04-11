using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.Windows.Forms.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	public partial class FwNewLangProjWritingSystemsControl : UserControl
	{
		private FwNewLangProjectModel _model;

		private FwWritingSystemSetupModel.ListType _listType;

		/// <summary/>
		public FwNewLangProjWritingSystemsControl() : this(null, FwWritingSystemSetupModel.ListType.Vernacular)
		{
			// for designer
		}

		/// <summary/>
		public FwNewLangProjWritingSystemsControl(FwNewLangProjectModel model, FwWritingSystemSetupModel.ListType type)
		{
			InitializeComponent();
			if (type != FwWritingSystemSetupModel.ListType.Vernacular &&
				type != FwWritingSystemSetupModel.ListType.Analysis)
			{
				throw new ArgumentException("Unsupported list type", nameof(type));
			}
			_model = model;
			_listType = type;
			m_defaultAnalysisAndVernSameIcon.Visible = false;
			m_defaultAnalysisAndVernSame.Visible = false;
			if (type == FwWritingSystemSetupModel.ListType.Analysis)
			{
				m_lblWsTypeHeader.Text = FwCoreDlgs.NewProjectWizard_AnalysisHeader;
				m_lblExplainWsTypeUsage.Text = FwCoreDlgs.NewLangProjWizard_AnalysisWritingSystemExplanation;
				if (model.WritingSystemContainer.CurrentAnalysisWritingSystems.Count > 0)
				{
					m_defaultWsLabel.Text = model.WritingSystemContainer.CurrentAnalysisWritingSystems[0].DisplayLabel;
				}

				if (model.WritingSystemContainer.CurrentVernacularWritingSystems.First() ==
					model.WritingSystemContainer.CurrentAnalysisWritingSystems.First())
				{
					m_defaultAnalysisAndVernSameIcon.Visible = true;
					m_defaultAnalysisAndVernSame.Visible = true;
				}
			}
			else
			{
				if (model.WritingSystemContainer.CurrentVernacularWritingSystems.Count > 0)
				{
					m_defaultWsLabel.Text = model.WritingSystemContainer.CurrentVernacularWritingSystems[0].DisplayLabel;
				}
			}
		}

		private void ChooseLanguageClick(object sender, System.EventArgs e)
		{
			using (var langPicker = new LanguageLookupDialog())
			{
				if (_listType == FwWritingSystemSetupModel.ListType.Vernacular)
				{
					langPicker.SearchText = _model.ProjectName;
				}
				var result = langPicker.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					_model.SetDefaultWs(langPicker.SelectedLanguage);
				}
			}
		}
	}
}
