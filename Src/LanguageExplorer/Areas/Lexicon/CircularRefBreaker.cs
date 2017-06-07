// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Reporting;

namespace LanguageExplorer.Areas.Lexicon
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
			get { return LanguageExplorerResources.ksBreakCircularRefs; }
		}

		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LanguageExplorerResources.ksTryIfProgramGoesPoof;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatAreCircularRefs;
			m_dlg.RedoDescription = LanguageExplorerResources.ksGenericUtilityCannotUndo;
		}

		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropertyTable.GetValue<FdoCache>("cache");
			Process(cache);
			// Show the message returned from running the circular reference breaker service.
			MessageBox.Show(Report, LanguageExplorerResources.ksCircularRefsFixed);
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
