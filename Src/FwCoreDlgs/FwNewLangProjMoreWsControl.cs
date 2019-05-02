// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Control to explain and allow the user to do more customization of the vernacular and analysis writing systems
	/// </summary>
	public partial class FwNewLangProjMoreWsControl : UserControl
	{
		private readonly FwNewLangProjectModel _model;

		/// <summary/>
		public FwNewLangProjMoreWsControl(FwNewLangProjectModel model = null)
		{
			InitializeComponent();
			_model = model;
		}

		private void ConfigureVernacularClick(object sender, System.EventArgs e)
		{
			var wsSetupModel = new FwWritingSystemSetupModel(_model.WritingSystemContainer,
				FwWritingSystemSetupModel.ListType.Vernacular,
				_model.WritingSystemManager);
			using (var wsDlg = new FwWritingSystemSetupDlg(wsSetupModel))
			{
				wsDlg.ShowDialog(this);
			}
		}

		private void ConfigureAnalysisClick(object sender, System.EventArgs e)
		{
			var wsSetupModel = new FwWritingSystemSetupModel(_model.WritingSystemContainer, FwWritingSystemSetupModel.ListType.Analysis,
				_model.WritingSystemManager);
			using (var wsDlg = new FwWritingSystemSetupDlg(wsSetupModel))
			{
				wsDlg.ShowDialog(this);
			}
		}
	}
}
