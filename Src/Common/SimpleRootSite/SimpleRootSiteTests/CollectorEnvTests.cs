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
// File: CollectorEnvTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.Common.RootSites
{
	public class DummyCollectorEnv : CollectorEnv
	{
		private int m_index;
		public string[] m_expectedStringContents;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyCollectorEnv"/> class.
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="rootHvo">The root hvo.</param>
		/// ------------------------------------------------------------------------------------
		public DummyCollectorEnv(ISilDataAccess sda, int rootHvo) : base(null, sda, rootHvo)
		{
			m_index = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the string.
		/// </summary>
		/// <param name="tss">The TSS.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddString(ITsString tss)
		{
			Assert.AreEqual(m_expectedStringContents[m_index++], tss.Text);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of the CollectorEnv class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CollectorEnvTests : ScrInMemoryFdoTestBase
	{
		#region Dummy View Constructor
		///  ----------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy view constructor.
		/// </summary>
		///  ----------------------------------------------------------------------------------------
		public class DummyCollectorEnvVc : VwBaseVc
		{
			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="hvo"></param>
			/// <param name="frag"></param>
			/// ------------------------------------------------------------------------------------
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				int hvoOuter, tag, ihvo;
				ITsString tss;
				switch (frag)
				{
					case 1: // A ScrBook; display the title.
						vwenv.AddObjProp((int)ScrBook.ScrBookTags.kflidTitle, this, 2);
						break;
					case 2: // An StText; display the paragraphs.
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
						tss = TsStringHelper.MakeTSS(
							"Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo,
							InMemoryFdoCache.s_wsHvos.Fr);
						vwenv.AddString(tss);
						vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this, 3);
						break;
					case 3: // StTxtPara, display details of our outer object
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
						tss = TsStringHelper.MakeTSS(
							"Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo,
							InMemoryFdoCache.s_wsHvos.Fr);
						vwenv.AddString(tss);
						break;
					default:
						throw new ApplicationException("Unexpected frag in DummyCollectorEnvVc");
				}
			}
		}
		#endregion // Dummy View Constructor

		#region Data members
		private DummyCollectorEnvVc m_vc;
		private StText m_text;
		DummyCollectorEnv m_collectorEnv;
		#endregion

		#region Setup/Teardown/Initialize
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_text = m_scrInMemoryCache.AddTitleToMockedBook(Int32.MaxValue, "Who cares?");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_vc = new DummyCollectorEnvVc();
			m_collectorEnv = new DummyCollectorEnv(Cache.MainCacheAccessor, Int32.MaxValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_vc.Dispose();
			m_vc = null;
			m_collectorEnv = null;

			base.Exit();
		}
		#endregion

		#region tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetOuterObject method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetOuterObject()
		{
			CheckDisposed();

			m_collectorEnv.m_expectedStringContents = new string[]
				{
					"Hvo = " + Int32.MaxValue + "; Tag = " + (int)ScrBook.ScrBookTags.kflidTitle + "; Ihvo = " + 0,
					"Hvo = " + m_text.Hvo + "; Tag = " + (int)StText.StTextTags.kflidParagraphs + "; Ihvo = " + 0,
				};
			m_vc.Display(m_collectorEnv, Int32.MaxValue, 1);
		}
		#endregion
	}
}
