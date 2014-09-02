// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>Class for core dialogs that can be implemented using a simple MessageBox</summary>
	public static class MessageBoxes
	{
		/// <summary>
		/// Report failure to make target a component of parent. If startedFromComplex is true, the user is looking
		/// at parent, and tried to make target a component. Otherwise, the user is looking at target, and
		/// tried to make parent a complex form.
		/// </summary>
		public static void ReportLexEntryCircularReference(ILexEntry parent, ICmObject target, bool startedFromComplex)
		{
			var itemString = target is ILexEntry ? FwCoreDlgs.ksEntry : FwCoreDlgs.ksSense;
			var msgTemplate = startedFromComplex ? FwCoreDlgs.ksComponentIsComponent : FwCoreDlgs.ksComplexFormIsComponent;
			var startedFrom = startedFromComplex ? parent.HeadWord.Text : target.ShortName;
			var msg = String.Format(msgTemplate, itemString, startedFrom);
			MessageBox.Show(Form.ActiveForm, msg, FwCoreDlgs.ksWhichIsComponent, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}