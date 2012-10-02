// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SyncedDraftViewTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NMock;
using NMock.Constraints;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for synchronized DraftViews. These tests use mock objects and
	/// so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SyncedDraftViewTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;
		private IgnorePropChanged m_IgnorePropChanged;
		private RootSiteGroup m_group;

		#region Setup and Teardown

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_draftForm != null)
					m_draftForm.Dispose();
				if (m_IgnorePropChanged != null)
					m_IgnorePropChanged.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftForm = null;
			m_draftView = null; // Comes from m_draftForm, which is to dispose it.
			m_group = null;
			m_IgnorePropChanged = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_group = m_draftForm.CreateSyncDraftView(Cache);
			m_IgnorePropChanged = new IgnorePropChanged(Cache,
				PropChangedHandling.SuppressChangeWatcher);

			m_draftView = m_draftForm.DraftView;
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_draftView = null;
			m_draftForm.Close();
			m_draftForm = null;
			m_group = null;
			m_IgnorePropChanged.Dispose();
			m_IgnorePropChanged = null;

			// Restore prompt setting
			m_IgnorePropChanged.Dispose();
			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			CreateExodusData();
			CreateLeviticusData();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests laying out a view when there is a border on the bottom of titles. (TE-4141)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This is a test for TE-4141, which we hope JohnT can help us fix.")]
		public void WithBorderOnBottomOfTitle()
		{
			CheckDisposed();

			IStStyle titleMain = m_scr.FindStyle(ScrStyleNames.MainBookTitle);
			Assert.IsNotNull(titleMain);
			ITsPropsBldr propBldr = titleMain.Rules.GetBldr();
			propBldr.SetIntPropValues((int)FwTextPropType.ktptBorderBottom,
				(int)FwTextPropVar.ktpvMilliPoint, 20);
			titleMain.Rules = propBldr.GetTextProps();
			m_draftForm.StyleSheet.Init(Cache, m_scr.Hvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			m_group.RefreshDisplay();
			m_draftView.PerformLayout();
		}
	}
}
