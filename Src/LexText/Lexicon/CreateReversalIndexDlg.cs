// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CreateReversalIndexDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;

using System.Runtime.InteropServices;	// for the dllimport and related

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CreateReversalIndexDlg : Form, IFWDisposable
	{
		private int m_hvoRevIdx = 0;
		private FdoCache m_cache;
		private StringCollection m_revIdx = new StringCollection();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CreateReversalIndexDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CreateReversalIndexDlg()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		public void Init(FdoCache cache)
		{
			CheckDisposed();

			Init(cache, true);
		}

		public void Init(FdoCache cache, bool enableCancel)
		{
			CheckDisposed();

			m_cache = cache;
			m_btnCancel.Visible = enableCancel;
			Set<int> revIdxWs = new Set<int>(4);
			foreach (IReversalIndex ri in cache.LangProject.LexDbOA.ReversalIndexesOC)
				revIdxWs.Add(ri.WritingSystemRAHvo);
			// Include only the analysis writing systems chosen by the user.  See LT-7514 and LT-7239.
			Set<int> activeWs = new Set<int>(8);
			foreach (int ws in cache.LangProject.AnalysisWssRC.HvoArray)
				activeWs.Add(ws);
			m_cbWritingSystems.Sorted = true;
			m_cbWritingSystems.DisplayMember = "Name";
			NamedWritingSystem nwsSelected = null;
			foreach (NamedWritingSystem nws in cache.LangProject.GetDbNamedWritingSystems())
			{
				if (revIdxWs.Contains(nws.Hvo))
				{
					AddLanguageForExistingRevIdx(nws.IcuLocale);
					continue;
				}
				if (!activeWs.Contains(nws.Hvo))
					continue;
				m_cbWritingSystems.Items.Add(nws);
				if (nwsSelected == null && !LanguageMatchesExistingRevIdx(nws.IcuLocale))
					nwsSelected = nws;
			}
			if (nwsSelected != null)
				m_cbWritingSystems.SelectedItem = nwsSelected;
			if (m_cbWritingSystems.Items.Count > 0 && m_cbWritingSystems.SelectedIndex < 0)
				m_cbWritingSystems.SelectedIndex = 0;
			if (!enableCancel && m_cbWritingSystems.Items.Count == 0)
				throw new ApplicationException("Cancel is disabled, but there are none to choose, so the user has no way to get out of this dialog.");
		}

		/// <summary>
		/// This is valid (greater than zero) only after the user has clicked OK.
		/// </summary>
		public int NewReversalIndexHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoRevIdx;
			}
		}

		/// <summary>
		/// This is valid only after Init(...) has been called.
		/// </summary>
		public int PossibilityCount
		{
			get
			{
				CheckDisposed();
				return m_cbWritingSystems.Items.Count;
			}
		}

		#region private implementation methods
		/// <summary>
		/// Store the language portion of the ICU Locale for the writing system of each
		/// existing reversal index.  This is to facilitate choosing an initial value to
		/// display in the combo box.
		/// </summary>
		/// <param name="sIcuLocale"></param>
		private void AddLanguageForExistingRevIdx(string sIcuLocale)
		{
			string sLang = MiscUtils.ExtractLanguageCode(sIcuLocale);
			// LT-4937 : only add if not already present.
			// This is really a Set.
			if (!m_revIdx.Contains(sLang))
				m_revIdx.Add(sLang);
		}

		/// <summary>
		/// Check whether the language portion of this ICU Locale matches one for a writing
		/// system already used for an existing reversal index.
		/// </summary>
		/// <param name="sIcuLocale"></param>
		/// <returns></returns>
		private bool LanguageMatchesExistingRevIdx(string sIcuLocale)
		{
			string sLang = MiscUtils.ExtractLanguageCode(sIcuLocale);
			return m_revIdx.Contains(sLang);
		}

		#endregion

		private void CreateReversalIndexDlg_FormClosing(object sender, FormClosingEventArgs e)
		{
			switch (DialogResult)
			{
				default:
					{
						Debug.Assert(false, "Unexpected DialogResult.");
						break;
					}
				case DialogResult.Cancel:
					{
						if (!m_btnCancel.Visible)
						{
							e.Cancel = true;
							MessageBox.Show(LexEdStrings.ksMustSelectOne);
						}
						break;
					}
				case DialogResult.OK:
					{
						NamedWritingSystem nws = m_cbWritingSystems.SelectedItem as NamedWritingSystem;
						if (nws != null)
						{
							ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, nws.Hvo);
							IReversalIndex newIdx = m_cache.LangProject.LexDbOA.ReversalIndexesOC.Add(new ReversalIndex());
							newIdx.WritingSystemRA = lgws;
							// Copy any and all alternatives from lgws.Name to newIdx.Name
							// LT-4907 dies here.
							foreach (ILgWritingSystem lgwsLoop in m_cache.LanguageEncodings)
							{
								string lgsNameAlt = lgws.Name.GetAlternative(lgwsLoop.Hvo);
								if (lgsNameAlt != null && lgsNameAlt.Length > 0)
									newIdx.Name.SetAlternative(lgsNameAlt, lgws.Hvo);
							}
							m_hvoRevIdx = newIdx.Hvo;
						}
						break;
					}
			}
		}

		/// <summary>
		/// Fix so that 120 DPI fonts don't push the buttons off the bottom of
		/// the dialog.  (See LT-5080.)
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			int dy = ClientSize.Height - (m_btnOK.Location.Y + m_btnOK.Height);
			if (dy < 0)
				Height += 14 - dy;
		}
	}
}