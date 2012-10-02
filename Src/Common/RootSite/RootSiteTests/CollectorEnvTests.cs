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
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	// -----------------------------------------------------------------------------------------
	/// <summary>
	/// TODO: Replace DummyCollectorEnv with a Rhino Mock
	/// </summary>
	// -----------------------------------------------------------------------------------------
	public class DummyCollectorEnv : CollectorEnv
	{
		private int m_index;
		internal string[] m_expectedStringContents;

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
		public class DummyCollectorEnvVc : FwBaseVc
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
				int frWs = vwenv.DataAccess.WritingSystemFactory.GetWsFromStr("fr");
				int hvoOuter, tag, ihvo;
				ITsString tss;
				switch (frag)
				{
					case 1: // A ScrBook; display the title.
						vwenv.AddObjProp(ScrBookTags.kflidTitle, this, 2);
						break;
					case 2: // An StText; display the paragraphs.
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
						tss = TsStringHelper.MakeTSS(
							"Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo,
							frWs);
						vwenv.AddString(tss);
						vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 3);
						break;
					case 3: // StTxtPara, display details of our outer object
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
						tss = TsStringHelper.MakeTSS(
							"Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo,
							frWs);
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
		private IStText m_text;
		DummyCollectorEnv m_collectorEnv;
		IScrBook m_book;
		#endregion

		#region Setup/Teardown/Initialize

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_vc = new DummyCollectorEnvVc();
			m_book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1);
			m_collectorEnv = new DummyCollectorEnv(Cache.DomainDataByFlid, m_book.Hvo);

			m_text = AddTitleToMockedBook(m_book, "Who cares?");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_vc = null;
			m_collectorEnv = null;

			m_scr.ScriptureBooksOS.Remove(m_book);
			m_book = null;

			base.TestTearDown();
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
			m_collectorEnv.m_expectedStringContents = new[]
				{
					"Hvo = " + m_book.Hvo + "; Tag = " + ScrBookTags.kflidTitle + "; Ihvo = " + 0,
					"Hvo = " + m_text.Hvo + "; Tag = " + StTextTags.kflidParagraphs + "; Ihvo = " + 0,
				};
			m_vc.Display(m_collectorEnv, m_book.Hvo, 1);
		}
		#endregion
	}
}
