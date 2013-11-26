using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.IText;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is a wrapper that makes WfiWordformServices.MergeDuplicateAnalyses available in Tools/Utilities.
	/// An entry in UtilityCatalogInclude (in DistFiles/Language Explorer/Configuration) causes an instance to
	/// be created by reflection when the dialog is initialized.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "The creator/owner of this class is responsible to dispose the passed in dialog")]
	public class DuplicateAnalysisFixer : IUtility
	{
		public string Label
		{
			get { return ITextStrings.ksMergeDuplicateAnalyses; }
		}

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		private UtilityDlg m_dlg;

		/// <summary>
		/// Sets the utility dialog. NOTE: The caller is responsible for disposing the dialog!
		/// </summary>
		public UtilityDlg Dialog
		{
			set { m_dlg = value; }
		}

		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = ITextStrings.ksUseMergeAnalysesWhen;
			m_dlg.WhatDescription = ITextStrings.ksMergeAnalysesAttemptsTo;
			m_dlg.RedoDescription = ITextStrings.ksMergeAnalysesWarning;
		}

		/// <summary>
		/// This actually makes the fix.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = (FdoCache) m_dlg.Mediator.PropertyTable.GetValue("cache");
			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoMergeAnalyses, ITextStrings.ksRedoMergeAnalyses,
				cache.ActionHandlerAccessor,
				() => WfiWordformServices.MergeDuplicateAnalyses(cache, new ProgressBarWrapper(m_dlg.ProgressBar)));
		}
	}
}
