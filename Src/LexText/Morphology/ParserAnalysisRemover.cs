// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParserAnalysisRemover.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
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
			var cache = (FdoCache) m_dlg.Mediator.PropertyTable.GetValue("cache");
			IWfiAnalysis[] analyses = cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().ToArray();
			if (analyses.Length == 0)
				return;

			// Set up progress bar.
			m_dlg.ProgressBar.Minimum = 0;
			m_dlg.ProgressBar.Maximum = analyses.Length;
			m_dlg.ProgressBar.Step = 1;

			// stop parser if it's running.
			m_dlg.Mediator.SendMessage("StopParser", null);

			NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
			{
				foreach (IWfiAnalysis analysis in analyses)
				{
					ICmAgentEvaluation[] parserEvals = analysis.EvaluationsRC.Where(evaluation => !evaluation.Human).ToArray();
					foreach (ICmAgentEvaluation parserEval in parserEvals)
						analysis.EvaluationsRC.Remove(parserEval);

					IWfiWordform wordform = analysis.Wordform;
					if (analysis.EvaluationsRC.Count == 0)
						wordform.AnalysesOC.Remove(analysis);

					if (parserEvals.Length > 0)
						wordform.Checksum = 0;

					m_dlg.ProgressBar.PerformStep();
				}
			});
		}

		#endregion IUtility implementation
	}
}
