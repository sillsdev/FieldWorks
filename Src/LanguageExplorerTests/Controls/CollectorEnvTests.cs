// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Controls
{
	/// <summary>
	/// Tests of the CollectorEnv class
	/// </summary>
	[TestFixture]
	public class CollectorEnvTests : ScrInMemoryLcmTestBase
	{
		private DummyCollectorEnvVc m_vc;
		private IStText m_text;
		DummyCollectorEnv m_collectorEnv;
		IScrBook m_book;

		#region Setup/Teardown/Initialize

		/// <summary />
		public override void TestSetup()
		{
			base.TestSetup();

			m_vc = new DummyCollectorEnvVc();
			m_book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1);
			m_collectorEnv = new DummyCollectorEnv(Cache.DomainDataByFlid, m_book.Hvo);

			m_text = AddTitleToMockedBook(m_book, "Who cares?");
		}

		/// <summary />
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
		/// <summary>
		/// Tests the GetOuterObject method.
		/// </summary>
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

		/// <summary>
		/// Dummy view constructor.
		/// </summary>
		private sealed class DummyCollectorEnvVc : FwBaseVc
		{
			/// <summary />
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				var frWs = vwenv.DataAccess.WritingSystemFactory.GetWsFromStr("fr");
				int hvoOuter, tag, ihvo;
				ITsString tss;
				switch (frag)
				{
					case 1: // A ScrBook; display the title.
						vwenv.AddObjProp(ScrBookTags.kflidTitle, this, 2);
						break;
					case 2: // An StText; display the paragraphs.
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
						tss = TsStringUtils.MakeString("Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo, frWs);
						vwenv.AddString(tss);
						vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 3);
						break;
					case 3: // StTxtPara, display details of our outer object
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
						tss = TsStringUtils.MakeString("Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo, frWs);
						vwenv.AddString(tss);
						break;
					default:
						throw new ApplicationException("Unexpected frag in DummyCollectorEnvVc");
				}
			}
		}

		/// <summary />
		private sealed class DummyCollectorEnv : CollectorEnv
		{
			private int m_index;
			internal string[] m_expectedStringContents;

			/// <summary />
			public DummyCollectorEnv(ISilDataAccess sda, int rootHvo) : base(null, sda, rootHvo)
			{
				m_index = 0;
			}

			/// <summary />
			public override void AddString(ITsString tss)
			{
				Assert.AreEqual(m_expectedStringContents[m_index++], tss.Text);
			}
		}
	}
}
