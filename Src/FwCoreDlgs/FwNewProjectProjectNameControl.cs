// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	[Browsable(true)]
	public partial class FwNewProjectProjectNameControl : UserControl
	{
		private FwNewLangProjectModel _model;
		/// <summary/>
		public FwNewProjectProjectNameControl(FwNewLangProjectModel model)
		{
			InitializeComponent();
			_model = model;
			m_txtName.Text = _model.ProjectName;
			m_txtName.TextChanged += ProjectNameTextChanged;
			_errorImage.Visible = false;
			_projectNameErrorLabel.Visible = false;
		}

		private void ProjectNameTextChanged(object sender, System.EventArgs e)
		{
			if (_model != null)
			{
				_model.ProjectName = m_txtName.Text;
			}
		}

		/// <summary/>
		public void Bind(FwNewLangProjectModel model)
		{
			_model = model;
			// Do not re-bind the project name contents to avoid UX trouble (flickering and text selection issues)
			_projectNameErrorLabel.Visible = !model.IsProjectNameValid;
			_errorImage.Visible = !model.IsProjectNameValid;
			_projectNameErrorLabel.Text = model.InvalidProjectNameMessage;
		}
	}
}
