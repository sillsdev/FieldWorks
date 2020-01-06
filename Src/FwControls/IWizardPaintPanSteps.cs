// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	internal interface IWizardPaintPanSteps
	{
		string[] StepNames { get; }
		int LastStepNumber { get; }
		int CurrentStepNumber { get; }
		Font StepsFont { get; }
		Color TextColor { get; }
		Panel PanSteps { get; }
	}
}