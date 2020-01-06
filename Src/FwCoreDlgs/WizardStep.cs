// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	public partial class WizardStep : UserControl
	{
		/// <summary/>
		public WizardStep()
		{
			InitializeComponent();
		}

		/// <summary/>
		public void Bind(IWizardStep step, bool isFirst, bool isLast)
		{
			_stepName.Text = step.StepName;
			_optionalIndicator.Visible = step.IsOptional;

			if (step.IsComplete)
			{
				_statusImage.Image = Properties.Resources.WizardStepComplete;
			}
			else
			{
				_statusImage.Image = Properties.Resources.WizardNotComplete;
			}

			_lineToNextImage.Image = Properties.Resources.WizardConnectToStep;
			_lineToPreviousImage.Image = Properties.Resources.WizardConnectToStep;

			if (isFirst)
			{
				_lineToPreviousImage.Visible = false;
			}
			if (isLast)
			{
				_lineToNextImage.Visible = false;
			}

			if (step.IsCurrent)
			{
				_stepName.Font = new Font(_stepName.Font, FontStyle.Bold | FontStyle.Underline);
			}
			else
			{
				_stepName.Font = new Font(_stepName.Font, FontStyle.Regular);
			}
		}
	}
}
