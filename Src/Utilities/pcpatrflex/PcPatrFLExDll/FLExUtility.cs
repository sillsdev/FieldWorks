// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;

namespace SIL.PcPatrFLEx
{
	class FLExUtility : IUtility

	{
		protected UtilityDlg m_dlg;

		#region IUtility implementation

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label => "Use PC-PATR with FLEx";

		UtilityDlg IUtility.Dialog { set => m_dlg = value; }

		void IUtility.LoadUtilities()
		{
			m_dlg.Utilities.Items.Add(this);
		}

		void IUtility.OnSelection()
		{
			m_dlg.WhenDescription = "Run this when you have a PC-PATR grammar and have a reasonable amount of morphological ambiguity in interlinear texts.";
			m_dlg.WhatDescription = "Run this to use a PC-PATR grammar to try and disambiguate interlinear texts.";
			m_dlg.RedoDescription = "You cannot use 'Undo' to cancel the effect of this utility.You would need to go back to a previously saved version of the database(i.e., make a backup of your database before running this utility so you can restore to it if the results are not what you want).";
		}

		void IUtility.Process()
		{
			var pcpatrFlexForm = new PcPatrFLExForm();
			pcpatrFlexForm.Cache = m_dlg.PropTable.GetValue<LcmCache>("cache");
			pcpatrFlexForm.PrepareForm();
			//pcpatrFlexForm.FillTextsListBox();
			pcpatrFlexForm.Show();
			m_dlg.Close();
		}
		#endregion IUtility implementation
	}
}
