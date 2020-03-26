// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	internal interface IWizardStep
	{
		/// <summary/>
		string StepName { get; set; }

		/// <summary/>
		bool IsOptional { get; set; }

		/// <summary/>
		bool IsCurrent { get; set; }

		/// <summary/>
		bool IsComplete { get; set; }
	}
}