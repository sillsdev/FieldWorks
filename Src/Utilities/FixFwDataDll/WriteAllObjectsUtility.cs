// Copyright (c) 2015-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.FixData
{
	public class WriteAllObjectsUtility : IUtility
	{
		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		public string Label => Strings.WriteEverything;

		public UtilityDlg Dialog
		{
			private get;
			set;
		}

		public void LoadUtilities()
		{
			Dialog.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			Dialog.WhenDescription = Strings.WriteEverythingUseThisWhen;
			Dialog.WhatDescription = Strings.WriteEverythingThisUtilityAttemptsTo;
			Dialog.RedoDescription = Strings.WriteEverythingCannotUndo;
		}

		public void Process()
		{
			var cache = Dialog.PropTable.GetValue<LcmCache>("cache");
			cache.ExportEverythingAsModified();
		}
	}
}
