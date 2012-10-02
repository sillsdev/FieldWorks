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
// File: BasicViewTestsBase.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BasicViewTestsBase : ScrInMemoryFdoTestBase
	{
		/// <summary>The draft form</summary>
		protected DummyBasicView m_basicView;
		/// <summary></summary>
		protected int m_hvoRoot;
		/// <summary>Derived class needs to initialize this with something useful</summary>
		protected int m_flidContainingTexts = 0;
		/// <summary>Fragment for view constructor</summary>
		protected int m_frag = 1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual DummyBasicView CreateDummyBasicView()
		{
			return new DummyBasicView();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			Debug.Assert(m_basicView == null, "m_basicView is not null.");
			//if (m_basicView != null)
			//	m_basicView.Dispose();
			m_basicView = CreateDummyBasicView();
			m_basicView.Cache = Cache;
			m_basicView.Visible = false;
			m_basicView.StyleSheet = styleSheet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the view
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_basicView.Dispose();
			m_basicView = null;

			base.Exit();
		}

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
				if (m_basicView != null)
					m_basicView.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_basicView = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// <param name="display"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowForm(DummyBasicViewVc.DisplayType display)
		{
			Assert.IsTrue(m_flidContainingTexts != 0, "Need to initialize m_flidContainingTexts");

			m_basicView.DisplayType = display;

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = 307-25;
			m_basicView.MakeRoot(m_hvoRoot, m_flidContainingTexts, m_frag);
			m_basicView.CallLayout();
		}
	}
}
