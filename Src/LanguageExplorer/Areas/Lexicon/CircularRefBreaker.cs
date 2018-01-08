// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.UtilityTools;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
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
	internal sealed class CircularRefBreaker : IUtility
	{
		private UtilityDlg m_dlg;
		private int m_count;
		private int m_circular;
		private string m_report;

		/// <summary>
		/// Testing only.
		/// </summary>
		internal CircularRefBreaker()
		{
		}

		/// <summary />
		internal CircularRefBreaker(UtilityDlg utilityDlg)
		{
			m_dlg = utilityDlg;
		}

		/// <summary>
		/// Override method to return the Label property.  This is really needed.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		#region Implement IUtility

		/// <summary />
		public string Label => LanguageExplorerResources.ksBreakCircularRefs;

		/// <summary />
		public void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksTryIfProgramGoesPoof;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatAreCircularRefs;
			m_dlg.RedoDescription = LanguageExplorerResources.ksGenericUtilityCannotUndo;
		}

		/// <summary />
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>("cache");
			Process(cache);
			// Show the message returned from running the circular reference breaker service.
			MessageBox.Show(Report, LanguageExplorerResources.ksCircularRefsFixed);
			Logger.WriteEvent(Report);
		}
		#endregion

		/// <summary>
		/// Number of LexEntryRef objects processed
		/// </summary>
		public int Count => m_count;

		/// <summary>
		/// Number of circular references found and fixed
		/// </summary>
		public int Circular => m_circular;

		/// <summary>
		/// Final report to display to the user
		/// </summary>
		public string Report => m_report;

		public void Process(LcmCache cache)
		{
			// Run service that does the work of fixing the circular references.
			CircularRefBreakerService.ReferenceBreaker(cache, out m_count, out m_circular, out m_report);
			Debug.WriteLine(Report);
		}
	}
}
