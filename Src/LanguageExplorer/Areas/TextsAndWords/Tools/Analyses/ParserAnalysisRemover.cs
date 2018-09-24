// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using LanguageExplorer.UtilityTools;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.Analyses
{
	/// <summary>
	/// This class serves to remove all analyses that are only approved by the parser.
	/// Analyses that have a human evaluation (approved or disapproved) remain afterwards.
	/// </summary>
	internal sealed class ParserAnalysisRemover : IUtility
	{
		#region Data members

		private UtilityDlg m_dlg;
		const string kPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='RemoveParserAnalyses']/";

		/// <summary />
		internal ParserAnalysisRemover(UtilityDlg utilityDlg)
		{
			if (utilityDlg == null)
			{
				throw new ArgumentNullException(nameof(utilityDlg));
			}
			m_dlg = utilityDlg;
		}

		#endregion Data members

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label => StringTable.Table.GetStringWithXPath("Label", kPath);

		/// <summary>
		/// Notify the utility that has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			m_dlg.WhenDescription = StringTable.Table.GetStringWithXPath("WhenDescription", kPath);
			m_dlg.WhatDescription = StringTable.Table.GetStringWithXPath("WhatDescription", kPath);
			m_dlg.RedoDescription = StringTable.Table.GetStringWithXPath("RedoDescription", kPath);
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			var analyses = cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().ToArray();
			if (analyses.Length == 0)
			{
				return;
			}

			// Set up progress bar.
			m_dlg.ProgressBar.Minimum = 0;
			m_dlg.ProgressBar.Maximum = analyses.Length;
			m_dlg.ProgressBar.Step = 1;

			// stop parser if it's running.
			m_dlg.Publisher.Publish("StopParser", null);

			NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
			{
				foreach (var analysis in analyses)
				{
					var parserEvals = analysis.EvaluationsRC.Where(evaluation => !evaluation.Human).ToArray();
					foreach (var parserEval in parserEvals)
					{
						analysis.EvaluationsRC.Remove(parserEval);
					}

					var wordform = analysis.Wordform;
					if (analysis.EvaluationsRC.Count == 0)
					{
						wordform.AnalysesOC.Remove(analysis);
					}

					if (parserEvals.Length > 0)
					{
						wordform.Checksum = 0;
					}

					m_dlg.ProgressBar.PerformStep();
				}
			});
		}

		#endregion IUtility implementation
	}
}