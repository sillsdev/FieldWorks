// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using XCore;

namespace SIL.VariantGenerator
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
		public string Label => VariantGeneratorDll_Strings.ksTitle;

		UtilityDlg IUtility.Dialog
		{
			set => m_dlg = value;
		}

		void IUtility.LoadUtilities()
		{
			m_dlg.Utilities.Items.Add(this);
		}

		void IUtility.OnSelection()
		{
			m_dlg.WhenDescription = VariantGeneratorDll_Strings.ksWhenDescription;
			m_dlg.WhatDescription = VariantGeneratorDll_Strings.ksWhatDescription;
			m_dlg.RedoDescription = VariantGeneratorDll_Strings.ksRedoDescription;
			;
		}

		void IUtility.Process()
		{
			LcmCache cache = m_dlg.PropTable.GetValue<LcmCache>("cache");
			PropertyTable propTable = m_dlg.PropTable;
			Mediator mediator = m_dlg.Mediator;
			try
			{
				var varGenForm = new VariantGenForm(cache, propTable, mediator);
				varGenForm.Show();
				//m_dlg.Close();
			}
			catch (Exception e)
			{
				// probably first time and user canceled file creation
				Console.WriteLine(e.Message);
			}
			finally
			{
				m_dlg.Close();
			}
		}
		#endregion IUtility implementation
	}
}
