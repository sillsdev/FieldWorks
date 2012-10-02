/*
 *    OptionsDlgController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.IO;
using Gtk;
using Glade;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class OptionsDlgController : DialogController
	{
		[Widget] private Gtk.Dialog kridOptionsDlg;
		[Widget] private Gtk.RadioButton kctidArrVis;
		[Widget] private Gtk.RadioButton kctidArrLog;
		[Widget] private Gtk.CheckButton kctidGraphiteLog;
		private OptionsDlgModel model_;

		public OptionsDlgController(OptionsDlgModel model) : base("kridOptionsDlg", model)
		{
			if (model == null)
				throw new Exception("Model is null.");
			model_ = model;
			Console.WriteLine("OptionsDlgController.ctor invoked");
			kctidArrLog.Active = false;
			kctidArrVis.Active = false;
			if (model_.ArrowKeyBehaviour == OptionsDlgModel.ArrowKeyBehaviourKind.Visual) {
				kctidArrVis.Active = true;
			}
			else if (model_.ArrowKeyBehaviour == OptionsDlgModel.ArrowKeyBehaviourKind.Logical) {
				kctidArrLog.Active = true;
			}
			else {
				throw new Exception("Invalid program state: ArrowKeyBehaviour undefined.");
			}
			kctidGraphiteLog.Active = model_.OutputGraphiteDebug;
		}

		protected override void Commit() {
			if (kctidArrVis.Active)
				model_.ArrowKeyBehaviour = OptionsDlgModel.ArrowKeyBehaviourKind.Visual;
			else if (kctidArrLog.Active)
				model_.ArrowKeyBehaviour = OptionsDlgModel.ArrowKeyBehaviourKind.Logical;
			else
				throw new Exception("Invalid radio button state.");
			model_.OutputGraphiteDebug = kctidGraphiteLog.Active;
		}
	}
}
