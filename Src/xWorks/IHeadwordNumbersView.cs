// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.CoreImpl.WritingSystems;
using SIL.CoreImpl.KernelInterfaces;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This is the view interface used by the HeadwordNumbersDlg to facilitate unit testing the controller
	/// without any UI.
	/// </summary>
	public interface IHeadwordNumbersView
	{
		event EventHandler RunStylesDialog;
		event EventHandler CustomDigitsChanged;
		bool HomographBefore { get; set; }
		bool ShowHomograph { get; set; }
		bool ShowHomographOnCrossRef { get; set; }
		bool ShowSenseNumber { get; set; }
		bool OkButtonEnabled { get; set; }
		string Description { get; set; }
		string HomographWritingSystem { get; set; }
		IEnumerable<CoreWritingSystemDefinition> AvailableWritingSystems { set; }
		IEnumerable<string> CustomDigits { get; set; }
		void SetWsFactoryForCustomDigits(ILgWritingSystemFactory factory);
	}
}