using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexEntryInflTypeConverter.
	/// </summary>
	public class LexEntryInflTypeConverter : IUtility
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
				return LexEdStrings.ksConvertIrregularlyInflectedFormVariants;
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
			m_dlg.WhenDescription = LexEdStrings.ksWhenToConvertIrregularlyInflectedFormVariants;
			m_dlg.WhatDescription = LexEdStrings.ksWhatIsConvertIrregularlyInflectedFormVariants;
			m_dlg.RedoDescription = LexEdStrings.ksCannotRedoConvertIrregularlyInflectedFormVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = (FdoCache)m_dlg.Mediator.PropertyTable.GetValue("cache");
			// NOTE: need to implement this: cache.LanguageProject.LexDbOA.ConvertLexEntryInflTypes(m_dlg.ProgressBar);
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
