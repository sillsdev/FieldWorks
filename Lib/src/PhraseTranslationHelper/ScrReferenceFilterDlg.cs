// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrReferenceFilterDlg.cs
// ---------------------------------------------------------------------------------------------
using System.Linq;
using System.Windows.Forms;
using SILUBS.SharedScrUtils;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog to present user with options for generating an LCF file to use for generating a
	/// printable script to do comprehension checking.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ScrReferenceFilterDlg : Form
	{
		#region Data members
		private readonly ScrReference m_firstAvailableRef;
		private readonly ScrReference m_lastAvailableRef;
		#endregion

		#region Constructor and initialization methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScrReferenceFilterDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ScrReferenceFilterDlg(ScrReference initialFromRef, ScrReference initialToRef,
			int[] canonicalBookIds)
		{
			InitializeComponent();
			scrPsgFrom.Initialize(initialFromRef, canonicalBookIds);
			scrPsgTo.Initialize(initialToRef, canonicalBookIds);
			m_firstAvailableRef = new ScrReference(canonicalBookIds[0], 1, 1, initialFromRef.Versification);
			m_lastAvailableRef = new ScrReference(canonicalBookIds.Last(), 1, 1, initialToRef.Versification);
			m_lastAvailableRef = m_lastAvailableRef.LastReferenceForBook;
			if (initialFromRef == m_firstAvailableRef && initialToRef == m_lastAvailableRef)
				btnClearFilter.Enabled = false;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the From reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference FromRef
		{
			get { return scrPsgFrom.ScReference; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the To reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference ToRef
		{
			get { return scrPsgTo.ScReference; }
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles change in the from passage
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		private void scrPsgFrom_PassageChanged(ScrReference newReference)
		{
			if (newReference != ScrReference.Empty && newReference > scrPsgTo.ScReference)
				scrPsgTo.ScReference = scrPsgFrom.ScReference;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles change in the from passage
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		private void scrPsgTo_PassageChanged(ScrReference newReference)
		{
			if (newReference != ScrReference.Empty && newReference < scrPsgFrom.ScReference)
				scrPsgFrom.ScReference = scrPsgTo.ScReference;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnClearFilter control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnClearFilter_Click(object sender, System.EventArgs e)
		{
			scrPsgFrom.ScReference = m_firstAvailableRef;
			scrPsgTo.ScReference = m_lastAvailableRef;
		}
		#endregion
	}
}