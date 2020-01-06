// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Control to explain and allow the user to do more customization of the vernacular and analysis writing systems
	/// </summary>
	public partial class FwNewLangProjMoreWsControl : UserControl
	{
		private readonly FwNewLangProjectModel _model;
		private readonly IHelpTopicProvider _helpTopicProvider;

		/// <summary/>
		public FwNewLangProjMoreWsControl(FwNewLangProjectModel model = null, IHelpTopicProvider helpTopicProvider = null)
		{
			InitializeComponent();
			_model = model;
			_helpTopicProvider = helpTopicProvider;
		}

		private void ConfigureVernacularClick(object sender, System.EventArgs e)
		{
			var wsSetupModel = new FwWritingSystemSetupModel(_model.WritingSystemContainer,
				FwWritingSystemSetupModel.ListType.Vernacular,
				_model.WritingSystemManager);
			using (var wsDlg = new FwWritingSystemSetupDlg(wsSetupModel, _helpTopicProvider))
			{
				wsDlg.ShowDialog(this);
			}
		}

		private void ConfigureAnalysisClick(object sender, System.EventArgs e)
		{
			var wsSetupModel = new FwWritingSystemSetupModel(_model.WritingSystemContainer, FwWritingSystemSetupModel.ListType.Analysis,
				_model.WritingSystemManager);
			using (var wsDlg = new FwWritingSystemSetupDlg(wsSetupModel, _helpTopicProvider))
			{
				wsDlg.ShowDialog(this);
			}
		}
	}
}
