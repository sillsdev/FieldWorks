// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CreateReversalIndexDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CreateReversalIndexDlg : Form, IFWDisposable
	{
		private int m_hvoRevIdx;
		private FdoCache m_cache;
		private readonly HashSet<LanguageSubtag> m_revIdx = new HashSet<LanguageSubtag>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CreateReversalIndexDlg"/> class.
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
			foreach (IReversalIndex ri in cache.LanguageProject.LexDbOA.ReversalIndexesOC)
				revIdxWs.Add(m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem));
			// Include only the analysis writing systems chosen by the user.  See LT-7514 and LT-7239.
			Set<int> activeWs = new Set<int>(cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Select(wsObj => wsObj.Handle));
			m_cbWritingSystems.Sorted = true;
			m_cbWritingSystems.DisplayMember = "Name";
			WritingSystem selectedWs = null;
			foreach (WritingSystem ws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				if (revIdxWs.Contains(ws.Handle))
				{
					AddLanguageForExistingRevIdx(ws);
					continue;
				}
				if (!activeWs.Contains(ws.Handle))
					continue;
				m_cbWritingSystems.Items.Add(ws);
				if (selectedWs == null && !m_revIdx.Contains(ws.Language))
					selectedWs = ws;
			}
			if (selectedWs != null)
				m_cbWritingSystems.SelectedItem = selectedWs;
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
		/// Store the language portion of the identifier for the writing system of each
		/// existing reversal index.  This is to facilitate choosing an initial value to
		/// display in the combo box.
		/// </summary>
		/// <param name="ws">The ws.</param>
		private void AddLanguageForExistingRevIdx(WritingSystem ws)
		{
			LanguageSubtag sLang = ws.Language;
			// LT-4937 : only add if not already present.
			// This is really a Set.
			if (!m_revIdx.Contains(sLang))
				m_revIdx.Add(sLang);
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
						var wsObj = m_cbWritingSystems.SelectedItem as WritingSystem;
						if (wsObj != null)
						{
							UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoCreateReversalIndex, LexEdStrings.ksRedoCreateReversalIndex,
								m_cache.ActionHandlerAccessor,
								() =>
								{
									var riRepo = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
									m_hvoRevIdx = riRepo.FindOrCreateIndexForWs(wsObj.Handle).Hvo;
								});
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