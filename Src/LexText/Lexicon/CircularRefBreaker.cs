using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FDO;
using System;
using System.Text;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Go through all the PrimaryLexeme lists of complex form LexEntryRefs searching for and fixing any circular references.
	/// If a circular reference is found, the entry with the longer headword is removed as a component (and primary lexeme)
	/// of the other one.
	/// </summary>
	/// <remarks>
	/// This fixes https://jira.sil.org/browse/LT-16362.
	/// </remarks>
	class CircularRefBreaker : IUtility
	{
		private UtilityDlg m_dlg;

		private int m_count;
		private int m_circular;
		private string m_report;
		/// <summary>
		/// Number of LexEntryRef objects processed
		/// </summary>
		public int Count { get{return m_count;} }
		/// <summary>
		/// Number of circular references found and fixed
		/// </summary>
		public int Circular { get{return m_circular;} }
		/// <summary>
		/// Final report to display to the user
		/// </summary>
		public string Report { get{return m_report;} }

		/// <summary>
		/// Override method to return the Label property.  This is really needed.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		#region Implement IUtility
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_dlg == null);	// must be set only once

				m_dlg = value;
			}
		}

		public string Label
		{
			get { return LexEdStrings.ksBreakCircularRefs; }
		}

		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LexEdStrings.ksTryIfProgramGoesPoof;
			m_dlg.WhatDescription = LexEdStrings.ksWhatAreCircularRefs;
			m_dlg.RedoDescription = LexEdStrings.ksGenericUtilityCannotUndo;
		}

		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropTable.GetValue<FdoCache>("cache");

			Process(cache);
			// Show the message returned from running the circular reference breaker service.
			MessageBox.Show(Report, LexEdStrings.ksCircularRefsFixed);
			Logger.WriteEvent(Report);
		}
#endregion

		public void Process(FdoCache cache)
		{
			// Run service that does the work of fixing the circular references.
			CircularRefBreakerService.ReferenceBreaker(cache, out m_count, out m_circular, out m_report);
			Debug.WriteLine(Report);
		}
	}
}
