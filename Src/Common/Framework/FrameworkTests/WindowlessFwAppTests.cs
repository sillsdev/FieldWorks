// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WindowlessFwAppTests.cs
// Responsibility: TomB
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.UIAdapters;

namespace SIL.FieldWorks.Common.Framework
{
	#region InvisibleFwMainWnd
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy override of FwMainWnd that never gets shown
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class InvisibleFwMainWnd : FwMainWnd
	{
		#region data members
		/// <summary>Mocked Editing Helper</summary>
		public EditingHelper m_mockedEditingHelper;
		#endregion

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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mockedEditingHelper = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the mocked editing helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();
				return m_mockedEditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		public override void PreSynchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful (false results in RefreshAll)</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Synchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}
	}
	#endregion

	#region WindowslessFwAppTests class
	/// <summary>
	/// Summary description for WindowlessFwAppTests.
	/// </summary>
	[TestFixture]
	public class WindowlessFwAppTests : ScrInMemoryFdoTestBase
	{
		private InvisibleFwMainWnd m_mainWnd;
		private readonly TMItemProperties m_itemProps = new TMItemProperties();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_mainWnd = new InvisibleFwMainWnd();
			m_itemProps.ParentForm = m_mainWnd;
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			m_mainWnd.Dispose();
			base.FixtureTeardown();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_mainWnd.m_mockedEditingHelper = MockRepository.GenerateStub<EditingHelper>();
		}

		#region tests for enabling/disabling Format Apply Style menu
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is enabled when there is an active editing
		/// helper and a current selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_EnabledWhenCurrentSelection()
		{
			SelectionHelper mockedSelectionHelper = MockRepository.GenerateMock<SelectionHelper>();
			IVwSelection mockedSelection = MockRepository.GenerateMock<IVwSelection>();
			mockedSelection.Expect(sel => sel.IsEditable).Return(true);
			mockedSelection.Stub(sel => sel.CanFormatChar).Return(true);
			mockedSelection.Stub(sel => sel.CanFormatPara).Return(true);
			mockedSelectionHelper.Expect(sh => sh.Selection).Return(mockedSelection);
			m_mainWnd.m_mockedEditingHelper.Expect(ed => ed.CurrentSelection).Return(mockedSelectionHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsTrue(m_itemProps.Enabled);
			mockedSelectionHelper.VerifyAllExpectations();
			mockedSelection.VerifyAllExpectations();
			m_mainWnd.m_mockedEditingHelper.VerifyAllExpectations();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is no active editing helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenNoEditingHelper()
		{
			m_mainWnd.m_mockedEditingHelper = null;

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is no current selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenNoSelection()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(ed => ed.CurrentSelection).Return(null);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is an active editing
		/// helper and a current selection but the current selection doesn't allow formatting
		/// of either paragraph or character styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenNeitherParaNorCharCanBeFormatted()
		{
			SelectionHelper mockedSelectionHelper = MockRepository.GenerateMock<SelectionHelper>();
			IVwSelection mockedSelection = MockRepository.GenerateStub<IVwSelection>();
			mockedSelection.Expect(sel => sel.IsEditable).Return(true);
			mockedSelection.Stub(sel => sel.CanFormatChar).Return(false);
			mockedSelection.Stub(sel => sel.CanFormatPara).Return(false);
			mockedSelectionHelper.Expect(sh => sh.Selection).Return(mockedSelection);
			m_mainWnd.m_mockedEditingHelper.Expect(ed => ed.CurrentSelection).Return(mockedSelectionHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is an active editing
		/// helper and a current selection but the current selection isn't editable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenSelectionIsUneditable()
		{
			SelectionHelper mockedSelectionHelper = MockRepository.GenerateMock<SelectionHelper>();
			IVwSelection mockedSelection = MockRepository.GenerateStub<IVwSelection>();
			mockedSelection.Expect(sel => sel.IsEditable).Return(false);
			mockedSelectionHelper.Expect(sh => sh.Selection).Return(mockedSelection);
			m_mainWnd.m_mockedEditingHelper.Expect(ed => ed.CurrentSelection).Return(mockedSelectionHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}
		#endregion
	}
	#endregion
}
