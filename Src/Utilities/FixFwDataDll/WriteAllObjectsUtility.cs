using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;

namespace SIL.FieldWorks.FixData
{
	public class WriteAllObjectsUtility : IUtility
	{
		private UtilityDlg _utilityDlg;

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		public string Label
		{
			get { return "Write Everything"; }
		}

		public UtilityDlg Dialog
		{
			set
			{
				_utilityDlg = value;
			}
		}

		public void LoadUtilities()
		{
			_utilityDlg.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			_utilityDlg.WhenDescription = "Run this whenever you want to write out all CmObjects in the system. This will fix S/R failures if basic attributes were somehow lost in the fwdata file.";
			_utilityDlg.WhatDescription = "This utility writes all CmObjects out fresh, as if they had all been modified.";
			_utilityDlg.RedoDescription = "This operation cannot be undone, since it makes no changes.";
		}

		public void Process()
		{
			var cache = _utilityDlg.PropTable.GetValue<FdoCache>("cache");
			cache.ExportEverythingAsModified();
		}
	}
}
