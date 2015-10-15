using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;

namespace SIL.FieldWorks.IText
{
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="The creator/owner of this class is responsible to dispose the passed in dialog")]
	public class ParseIsCurrentFixer : IUtility
	{
		public string Label
		{
			get { return ITextStrings.ksClearParseIsCurrent; }
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
			m_dlg.WhenDescription = ITextStrings.ksUseClearParseIsCurrentWhen;
			m_dlg.WhatDescription = ITextStrings.ksClearParseIsCurrentDoes;
			m_dlg.RedoDescription = ITextStrings.ksParseIsCurrentWarning;
		}

		/// <summary>
		/// This actually makes the fix.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropTable.GetValue<FdoCache>("cache");
			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoMergeWordforms, ITextStrings.ksRedoMergeWordforms,
				cache.ActionHandlerAccessor,
				() => ClearFlags(cache, m_dlg.ProgressBar));

		}

		void ClearFlags(FdoCache cache, ProgressBar progressBar)
		{
			var paras = cache.ServiceLocator.GetInstance<IStTxtParaRepository>().AllInstances().ToArray();
			progressBar.Minimum = 0;
			progressBar.Maximum = paras.Length;
			progressBar.Step = 1;
			foreach (var para in paras)
			{
				progressBar.PerformStep();
				para.ParseIsCurrent = false;
			}
		}
	}
}
