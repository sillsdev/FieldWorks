using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel.Infrastructure;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class serves to remove all analyses that are only approved by the user.
	/// Analyses that have a parser evaluation (approved or disapproved) remain afterwards.
	/// </summary>
	public class UserAnalysisRemover : IUtility
	{
		#region Data members

		private UtilityDlg m_dlg;
		const string kPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='RemoveUserAnalyses']/";

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
				return StringTable.Table.GetStringWithXPath("Label", kPath);
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
			m_dlg.WhenDescription = StringTable.Table.GetStringWithXPath("WhenDescription", kPath);
			m_dlg.WhatDescription = StringTable.Table.GetStringWithXPath("WhatDescription", kPath);
			m_dlg.RedoDescription = StringTable.Table.GetStringWithXPath("RedoDescription", kPath);
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropTable.GetValue<LcmCache>("cache");
			IWfiAnalysis[] analyses = cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().ToArray();
			if (analyses.Length == 0)
				return;

			// Set up progress bar.
			m_dlg.ProgressBar.Minimum = 0;
			m_dlg.ProgressBar.Maximum = analyses.Length;
			m_dlg.ProgressBar.Step = 1;

			NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
			{
				foreach (IWfiAnalysis analysis in analyses)
				{
					ICmAgentEvaluation[] humanEvals = analysis.EvaluationsRC.Where(evaluation => evaluation.Human).ToArray();
					foreach (ICmAgentEvaluation humanEval in humanEvals)
						analysis.EvaluationsRC.Remove(humanEval);

					IWfiWordform wordform = analysis.Wordform;
					if (analysis.EvaluationsRC.Count == 0)
						wordform.AnalysesOC.Remove(analysis);

					if (humanEvals.Length > 0)
					{
						wordform.Checksum = 0;
						if (analysis.Cache != null)
							analysis.MoveConcAnnotationsToWordform();
					}

					m_dlg.ProgressBar.PerformStep();
				}
			});

			m_dlg.Mediator.SendMessage("RefreshInterlin", null);

		}

		#endregion IUtility implementation
	}
}
