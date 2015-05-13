using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FwCoreDlgs;
using System.Windows.Forms;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for HomographResetter.
	/// </summary>
	public class HomographResetter : IUtility
	{
		private UtilityDlg m_dlg;

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
				return LexEdStrings.ksReassignHomographs;
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
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LexEdStrings.ksWhenToReassignHomographs;
			m_dlg.WhatDescription = LexEdStrings.ksWhatIsReassignHomographs;
			m_dlg.RedoDescription = LexEdStrings.ksGenericUtilityCannotUndo;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropTable.GetValue<FdoCache>("cache");
			var homographWsId = cache.LanguageProject.HomographWs;
			var homographWs = cache.ServiceLocator.WritingSystems.AllWritingSystems.Where(ws => ws.Id == homographWsId);
			var homographWsLabel = homographWs.First().DisplayLabel;
			var defaultVernacularWs = cache.LanguageProject.DefaultVernacularWritingSystem;
			var defaultVernacularWsId = defaultVernacularWs.Id;
			var changeWs = false;
			if (homographWsId != defaultVernacularWsId)
			{
				var caution = string.Format(LexEdStrings.ksReassignHomographsCaution, homographWsLabel, defaultVernacularWs.DisplayLabel);
				if (MessageBox.Show(caution, LexEdStrings.ksReassignHomographs, MessageBoxButtons.YesNo) == DialogResult.Yes)
					changeWs = true;
			}
			if (changeWs)
			{
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
					LexEdStrings.ksUndoHomographWs, LexEdStrings.ksRedoHomographWs,
					cache.ActionHandlerAccessor,
					() =>
					{
						cache.LanguageProject.HomographWs = defaultVernacularWsId;
					});
			}
			cache.LanguageProject.LexDbOA.ResetHomographNumbers(new ProgressBarWrapper(m_dlg.ProgressBar));
		}

		/// <summary>
		/// Load 0 or more items in the list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);

		}

		#endregion IUtility implementation
	}
}
