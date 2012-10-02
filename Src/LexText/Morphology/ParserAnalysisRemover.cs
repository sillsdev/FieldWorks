// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParserAnalysisRemover.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class serves to remove all analyses that are only approved by the parser.
	/// Analyses that have a human evaluation (approved or disapproved) remain afterwards.
	/// </summary>
	public class ParserAnalysisRemover : IUtility
	{
		#region Data members

		private UtilityDlg m_dlg;
		const string kPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='RemoveParserAnalyses']/";

		#endregion Data members

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return m_dlg.Mediator.StringTbl.GetStringWithXPath("Label", kPath);
			}
		}

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
			}
		}

		/// <summary>
		/// Load 0 or more items in the list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);

		}

		/// <summary>
		/// Notify the utility that has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = m_dlg.Mediator.StringTbl.GetStringWithXPath("WhenDescription", kPath);
			m_dlg.WhatDescription = m_dlg.Mediator.StringTbl.GetStringWithXPath("WhatDescription", kPath);
			m_dlg.RedoDescription = m_dlg.Mediator.StringTbl.GetStringWithXPath("RedoDescription", kPath);
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = (FdoCache)m_dlg.Mediator.PropertyTable.GetValue("cache");
			var analyses = cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances();
			if (analyses.Count() == 0)
				return;

			var humanAgent = cache.ServiceLocator.GetInstance<ICmAgentRepository>().GetObject(new Guid("9303883A-AD5C-4CCF-97A5-4ADD391F8DCB"));
			var humanApproves = humanAgent.ApprovesOA;
			var humanDisapproves = humanAgent.DisapprovesOA;

			// Set up progress bar.
			m_dlg.ProgressBar.Minimum = 0;
			m_dlg.ProgressBar.Maximum = analyses.Count();
			m_dlg.ProgressBar.Step = 1;

			// stop parser if it's running.
			m_dlg.Mediator.SendMessage("StopParser", null);

			var affectedWordforms = new HashSet<IWfiWordform>();
			NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
			{
				foreach (var currentAnalysis in analyses)
				{
					var parserEvals = new List<ICmAgentEvaluation>(currentAnalysis.EvaluationsRC
						.Where(evaluation => evaluation != humanApproves && evaluation != humanDisapproves));

					foreach (var parserEval in parserEvals)
						currentAnalysis.EvaluationsRC.Remove(parserEval);

					// By this point the only eval(s) will be human, so don't zap the analysis.
					// The wordform has been (will be) affected if parserEvals.Count > 0 or currentAnalysis.EvaluationsRC.Count == 0.
					var wordformIsAffected = parserEvals.Count > 0 || currentAnalysis.EvaluationsRC.Count == 0;
					var currentWordform = currentAnalysis.Wordform;
					if (currentAnalysis.EvaluationsRC.Count == 0)
						currentWordform.AnalysesOC.Remove(currentAnalysis);
					if (wordformIsAffected)
						affectedWordforms.Add(currentWordform);

					m_dlg.ProgressBar.PerformStep();
				}
				// Reset progress bar for wordforms.
				m_dlg.ProgressBar.Value = 0;
				m_dlg.ProgressBar.Minimum = 0;
				m_dlg.ProgressBar.Maximum = affectedWordforms.Count;
				m_dlg.ProgressBar.Step = 1;
				foreach (var affectedWordform in affectedWordforms)
				{
					affectedWordform.Checksum = 0;
					using (var wfui = new WfiWordformUi(affectedWordform))
					{
						wfui.UpdateWordsToolDisplay(affectedWordform.Hvo, false, false, true, true);
					}
					m_dlg.ProgressBar.PerformStep();
				}
			});
		}

		#endregion IUtility implementation
	}
}
