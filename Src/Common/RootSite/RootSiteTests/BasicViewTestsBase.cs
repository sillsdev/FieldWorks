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
using System.Diagnostics;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.DomainServices;

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
		protected int m_flidContainingTexts;
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
			return new DummyBasicView(m_hvoRoot, m_flidContainingTexts);
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			KeyboardHelper.Release();
			base.FixtureTeardown();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			var styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

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
		public override void  TestTearDown()
		{
			// FdoTestBase::TestTeardown uses m_actionHandler which seems to
			// require its associated RootBox to have a valid root site.
			// This m_basicView needs to be disposed after FdoTestBase::TestTeardown is called.
			base.TestTearDown();

			if (m_basicView != null)
				m_basicView.Dispose();
			m_basicView = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// <param name="display"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowForm(DummyBasicViewVc.DisplayType display)
		{
#if !__MonoCS__
			int height = 307 - 25;
#else
			// TODO-Linux: This value works better, given mono differences. Possibly look into this further.
			int height = 300;
#endif
			ShowForm(display, height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// <param name="display"></param>
		/// <param name="height"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowForm(DummyBasicViewVc.DisplayType display, int height)
		{
			Assert.IsTrue(m_flidContainingTexts != 0, "Need to initialize m_flidContainingTexts");

			m_basicView.DisplayType = display;

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = height;
			m_basicView.MakeRoot(m_hvoRoot, m_flidContainingTexts, m_frag);
			m_basicView.CallLayout();
		}
	}
}
