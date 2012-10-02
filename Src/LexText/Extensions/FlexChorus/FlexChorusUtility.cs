using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

using XCore;

using LiftIO;
using LiftIO.Merging;

namespace SIL.FieldWorks.LexText.FlexChorus
{
	/// <summary>
	/// This class hooks the Chorus Merge dialog into the utilities dialog in the tools
	/// menu of FieldWorks Language Explorer.
	/// </summary>
	class FlexChorusUtility : IUtility
	{
		private UtilityDlg m_dlg;
		FdoCache m_cache;

		/// <summary>
		/// Constructor.
		/// </summary>
		public FlexChorusUtility()
		{
		}

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility Members

		/// <summary>
		/// Set the UtilityDlg.
		/// </summary>
		/// <remarks>
		/// This must be set, before calling any other property or method.
		/// </remarks>
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_dlg == null);

				m_dlg = value;
				m_cache = (FdoCache)m_dlg.Mediator.PropertyTable.GetValue("cache");
			}
		}

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return "Chorus/LIFT/WeSay interoperation utility";
			}
		}

		/// <summary>
		/// Load 0 or more items in the dialog's list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		/// <summary>
		/// Notify the utility has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = "When you want to run the Chorus/LIFT/WeSay interoperation utility";
			m_dlg.WhatDescription = "What on earth this utility is";
			m_dlg.RedoDescription = "This cannot be undone!";
		}

		/// <summary>
		/// Have this utility do what it does.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			using (FlexChorusDlg dlg = new FlexChorusDlg(m_cache))
			{
				dlg.ShowDialog();
			}
		}

		#endregion
	}
}
