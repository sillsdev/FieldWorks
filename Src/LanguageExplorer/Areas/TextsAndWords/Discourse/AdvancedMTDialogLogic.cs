// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class AdvancedMTDialogLogic: IDisposable
	{
		private IConstChartWordGroup m_wordGroup;

		public AdvancedMTDialogLogic(LcmCache cache, bool fPrepose, CChartSentenceElements ccSentElem)
		{
			Cache = cache;
			Prepose = fPrepose;
			SentElem = ccSentElem;

			DlgRibbon = new DialogInterlinRibbon(Cache) {Dock = DockStyle.Fill};
		}

		#region Disposable stuff
#if DEBUG
		~AdvancedMTDialogLogic()
		{
			Dispose(false);
		}
#endif

		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				DlgRibbon.Dispose();
			}

			IsDisposed = true;
		}
		#endregion

		private void CacheWordGroupOccurrencesForRibbon()
		{
			var words = m_wordGroup.GetOccurrences();
			DlgRibbon.CacheRibbonItems(words);
		}

		#region GetProperties

		/// <summary>
		/// Gets the LCM cache.
		/// </summary>
		public LcmCache Cache { get; }

		///// <summary>
		///// Gets the dialog Ribbon.
		///// </summary>
		public DialogInterlinRibbon DlgRibbon { get; }

		/// <summary>
		/// Gets the Dialog parameter object.
		/// </summary>
		internal CChartSentenceElements SentElem { get; }

		/// <summary>
		/// Gets the Prepose/Postpose flag.
		/// </summary>
		internal bool Prepose { get; }

		#endregion

		internal void Init()
		{
			Debug.Assert(SentElem != null && SentElem.AffectedWordGroups.Count > 0, "No WordGroup Hvo set.");

			// Collect eligible rows
			var crows = CollectRowsToCombo();

			// Next collect all columns and create ColumnMenuItem List for ComboBox
			if (crows == 1)
			{
				CollectColumnsToCombo(GetColumnChoices(SentElem.GetOriginRow));
			}
			else
			{
				CollectAllColumnsToCombo();
			}

			// TODO GordonM: Eventually we want to check and see if AffectedWordGroups has more than one
			// and put them all in the Ribbon!
			// Review: Perhaps we need to build a temporary/dummy WordGroup with all the occurrences of
			// the AffectedWordGroups in it for dialog Ribbon display purposes.
			m_wordGroup = SentElem.AffectedWordGroups[0];
			SetRibbon();
		}

		/// <summary>
		/// Collect the eligible rows (as passed in by the Chart Logic) into the ComboBox items array.
		/// Returns the number of rows collected.
		/// </summary>
		private int CollectRowsToCombo()
		{
			Debug.Assert(SentElem.EligibleRows != null);
			var crows = SentElem.EligibleRows.Length;
			var rows = new RowMenuItem[crows];
			for (var i = 0; i < crows; i++)
			{
				rows[i] = new RowMenuItem(SentElem.EligibleRows[i]);
			}
			SentElem.ComboRows = rows;
			return crows;
		}

		/// <summary>
		/// Collect all columns from hvo array and create List of ColumnMenuItems for ComboBox.
		/// </summary>
		private void CollectAllColumnsToCombo()
		{
			CollectColumnsToCombo(SentElem.AllChartCols);
		}

		/// <summary>
		/// Create a List of ColumnMenuItems for ComboBox based on an array of column hvos.
		/// </summary>
		internal void CollectColumnsToCombo(ICmPossibility[] colArray)
		{
			var ccols = colArray.Length;
			var cols = new ColumnMenuItem[ccols];
			for (var i = 0; i < ccols; i++)
			{
				cols[i] = new ColumnMenuItem(colArray[i]);
			}
			SentElem.ComboCols = cols;
		}

		/// <summary>
		/// Figure out what columns should be available, given a selected row.
		/// </summary>
		internal ICmPossibility[] GetColumnChoices(IConstChartRow row)
		{
			if (row.Hvo != SentElem.GetOriginRow.Hvo)
			{
				CollectAllColumnsToCombo();
				return SentElem.AllChartCols;
			}
			var ccols = SentElem.AllChartCols.Length;
			var icurCol = SentElem.GetOriginColumnIndex;
			if (Prepose)
			{
				//   Collect columns following clicked one
				ccols = ccols - icurCol - 1;
				icurCol++;
			}
			else
			{
				//   Collect columns preceding clicked one
				ccols = icurCol;
				icurCol = 0;
			}
			var result = new ICmPossibility[Math.Max(0, ccols)];
			for (var i = 0; i < ccols; i++)
			{
				result[i] = SentElem.AllChartCols[i + icurCol];
			}
			return result;
		}

		/// <summary>
		/// Sets the text ribbon to display the occurrences in the current WordGroup.
		/// </summary>
		internal void SetRibbon()
		{
			// TODO GordonM: make it work for an array of WordGroups?
			CacheWordGroupOccurrencesForRibbon();
			DlgRibbon.SetRoot(m_wordGroup.Hvo);
		}

		/// <summary>
		/// Takes the list of AffectedWordGroups fed into the dialog and the list of user selected words
		/// and updates the AffectedWordGroups list in the parameter object
		/// </summary>
		internal void SetAffectedWordGroups(AnalysisOccurrence[] selectedWords)
		{
			var selWords = new HashSet<AnalysisOccurrence>(selectedWords);
			var affectedWordGrps = SentElem.AffectedWordGroups
				.Select(wordGroup => new {wordGroup, wordGrpPoints = wordGroup.GetOccurrences()})
				.Where(@t => selWords.Intersect(@t.wordGrpPoints).Any())
				.Select(@t => @t.wordGroup).ToList();
			SentElem.AffectedWordGroups = affectedWordGrps;
		}
	}
}