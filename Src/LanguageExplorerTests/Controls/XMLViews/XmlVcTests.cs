// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExplorer;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorerTests.Controls.XMLViews
{
	/// <summary>
	/// Test (some aspects of) XmlVc
	/// </summary>
	[TestFixture]
	public sealed class XmlVcTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private RealDataCache m_sda;
		/// <summary>Writing System Manager (reset for each test)</summary>
		private WritingSystemManager m_wsManager;
		private int m_hvoLexDb; // root
		private int m_hvoKick; // one entry.
		private int kflidLexDb_Entries;
		private int kflidEntry_Form;
		private int kflidEntry_Summary;
		private int m_wsAnal;
		private int m_wsVern;
		internal const int kclsidLexDb = 1; // consistent with TextCacheModel.xml in resource file
		internal const int kclsidEntry = 7; // consistent with TextCacheModel.xml in resource file
		private LayoutCache m_layouts;

		/// <summary />
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_sda = new RealDataCache
			{
				MetaDataCache = MetaDataCache.CreateMetaDataCache("TextCacheModel_LanguageExplorer.xml")
			};
			Debug.Assert(m_wsManager == null);
			m_wsManager = Cache.ServiceLocator.WritingSystemManager;
			m_sda.WritingSystemFactory = m_wsManager;
			m_wsAnal = Cache.DefaultAnalWs;
			m_wsVern = Cache.DefaultVernWs;
			m_hvoLexDb = m_sda.MakeNewObject(kclsidLexDb, 0, -1, -1);
			kflidLexDb_Entries = m_sda.MetaDataCache.GetFieldId("LexDb", "Entries", false);
			kflidEntry_Form = m_sda.MetaDataCache.GetFieldId("Entry", "Form", false);
			kflidEntry_Summary = m_sda.MetaDataCache.GetFieldId("Entry", "Summary", false);
			m_hvoKick = m_sda.MakeNewObject(kclsidEntry, m_hvoLexDb, kflidLexDb_Entries, 0);
			m_sda.SetMultiStringAlt(m_hvoKick, kflidEntry_Form, m_wsVern, TsStringUtils.MakeString("kick", m_wsVern));
			m_sda.SetString(m_hvoKick, kflidEntry_Summary, TsStringUtils.MakeString("strike with foot", m_wsAnal));
			var keyAttrs = new Dictionary<string, string[]>
			{
				["layout"] = new[] { "class", "type", "name", "choiceGuid" },
				["group"] = new[] { "label" },
				["part"] = new[] { "ref" }
			};
			var layoutInventory = new Inventory("*.fwlayout", "/LayoutInventory/*", keyAttrs, "test", "nowhere");
			layoutInventory.LoadElements(XmlViewsResources.Layouts_xml, 1);
			keyAttrs = new Dictionary<string, string[]>
			{
				["part"] = new[] { "id" }
			};
			var partInventory = new Inventory("*Parts.xml", "/PartInventory/bin/*", keyAttrs, "test", "nowhere");
			partInventory.LoadElements(XmlViewsResources.Parts_xml, 1);
			m_layouts = new LayoutCache(m_sda.MetaDataCache, layoutInventory, partInventory);
		}

		/// <summary />
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			try
			{
				// GrowToWord causes a Char Property Engine to be created, and the test runner
				// fails if we don't shut the factory down.
				m_sda.Dispose();
				m_sda = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} FixtureTeardown method.", err);
			}
			finally
			{
				base.FixtureTeardown();
			}
		}

		/// <summary>
		/// Test that displaying a string property produces marking indicating the XML configuration.
		/// </summary>
		[Test]
		public void StringPropIsMarked()
		{
			using (var view = new XmlView(m_hvoLexDb, "root", true, m_sda))
			{
				var vc = new XmlVc("root", true, view, null, m_sda)
				{
					IdentifySource = true
				};
				vc.SetCache(Cache);
				vc.LayoutCache = m_layouts;
				vc.DataAccess = m_sda;
				var testEnv = new MockEnv() { DataAccess = m_sda, OpenObject = m_hvoLexDb };
				vc.Display(testEnv, m_hvoLexDb, XmlVc.kRootFragId);
				VerifySourceIdentified(testEnv.EventHistory, m_hvoKick, kflidEntry_Form, m_wsVern, "Entry:basic:Headword:HeadwordL");
				VerifyLabel(testEnv.EventHistory, m_hvoKick, kflidEntry_Form, m_wsVern, 1, ")", "Entry:basic:Headword:HeadwordL");

				VerifyLabel(testEnv.EventHistory, m_hvoKick, kflidEntry_Form, m_wsVern, -3, "head(", "Entry:basic:Headword:HeadwordL");
				VerifySourceIdentified(testEnv.EventHistory, m_hvoKick, kflidEntry_Summary, "Entry:basic:Summary:Sum.");
			}
		}

		private static void VerifySourceIdentified(List<object> events, int hvo, int tag, int ws, string expected)
		{
			for (var i = 1; i < events.Count; i++)
			{
				var ssp = events[i] as StringAltMemberAdded;
				if (ssp == null || ssp.Hvo != hvo || ssp.Tag != tag || ssp.Ws != ws)
				{
					continue;
				}
				var sps = events[i - 1] as StringPropSet;
				Assert.That(sps, Is.Not.Null);
				Assert.That(sps.ttp, Is.EqualTo((int)FwTextPropType.ktptBulNumTxtBef));
				Assert.That(sps.val, Is.EqualTo(expected));
				break;
			}
		}

		private void VerifySourceIdentified(List<object> events, int hvo, int tag, string expected)
		{
			for (var i = 1; i < events.Count; i++)
			{
				var spa = events[i] as StringPropAdded;
				if (spa == null || spa.Hvo != hvo || spa.Tag != tag)
				{
					continue;
				}
				var sps = events[i - 1] as StringPropSet;
				Assert.That(sps, Is.Not.Null);
				Assert.That(sps.ttp, Is.EqualTo((int)FwTextPropType.ktptBulNumTxtBef));
				Assert.That(sps.val, Is.EqualTo(expected));
				break;
			}
		}

		private static void VerifyLabel(List<object> events, int hvo, int tag, int ws, int offset, string expectContent, string expectSource)
		{
			for (var i = 1; i < events.Count; i++)
			{
				var ssp = events[i] as StringAltMemberAdded;
				if (ssp == null || ssp.Hvo != hvo || ssp.Tag != tag || ssp.Ws != ws)
				{
					continue;
				}
				var sa = events[i + offset] as StringAdded;
				Assert.That(sa, Is.Not.Null);
				Assert.That(sa.Content.Text, Is.EqualTo(expectContent));
				var ttp = sa.Content.get_Properties(0);
				Assert.That(ttp.GetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef), Is.EqualTo(expectSource));
				break;
			}
		}

		[Test]
		public void SenseOutlineIsObtainedUsingVirtual()
		{
			// For this test we need a real entry and sense.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			var sda = new MockDecorator(Cache);
			var vc = new XmlVc("root", true, null, null, sda);
			vc.SetCache(Cache);
			var sut = new XmlVcDisplayVec(vc, new MockEnv(), entry.Hvo, LexEntryTags.kflidSenses, 1);

			Assert.That(sut.CalculateAndFormatSenseLabel(sense.Hvo, 0, "%O)"), Is.EqualTo("77)"), "CalculateAndFormatSenseLabel should have used the decorator method");
			Assert.That(sda.Tag, Is.EqualTo(Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "LexSenseOutline", false)), "CalculateAndFormatSenseLabel should have used the right property");
		}

		private sealed class MockDecorator : DomainDataByFlidDecoratorBase
		{
			private LcmCache m_cache;
			public MockDecorator(LcmCache cache) : base(cache.DomainDataByFlid as ISilDataAccessManaged)
			{
				m_cache = cache;
			}

			public int Tag;

			public override ITsString get_StringProp(int hvo, int tag)
			{
				Tag = tag;
				return TsStringUtils.MakeString("77", m_cache.DefaultUserWs);
			}
		}

		private sealed class StringAltMemberAdded
		{
			public int Hvo;
			public int Tag;
			public int Ws;
			public IVwViewConstructor Vc;
		}

		private sealed class StringAdded
		{
			public ITsString Content;
		}

		private sealed class StringPropSet
		{
			public int ttp;
			public string val;
		}

		private sealed class StringPropAdded
		{
			public int Hvo;
			public int Tag;
			public IVwViewConstructor Vc;
		}

		private sealed class MockEnv : IVwEnv
		{
			private List<int> m_openObjects = new List<int>();

			/// <summary />
			public void AddObjProp(int tag, IVwViewConstructor vwvc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddObjVec(int tag, IVwViewConstructor vwvc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
			{
				var cobj = DataAccess.get_VecSize(OpenObject, tag);
				for (var i = 0; i < cobj; i++)
				{
					var hvoItem = DataAccess.get_VecItem(OpenObject, tag, i);
					OpenTheObject(hvoItem, i);
					vc.Display(this, hvoItem, frag);
					CloseTheObject();
				}
			}

			private void OpenTheObject(int hvo, int index)
			{
				m_openObjects.Add(OpenObject);
				OpenObject = hvo;
			}

			private void CloseTheObject()
			{
				OpenObject = m_openObjects.Last();
				m_openObjects.RemoveAt(m_openObjects.Count - 1);
			}

			/// <summary />
			public void AddReversedObjVecItems(int tag, IVwViewConstructor vwvc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddObj(int hvo, IVwViewConstructor vwvc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddLazyVecItems(int tag, IVwViewConstructor vwvc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddLazyItems(int[] rghvo, int chvo, IVwViewConstructor vwvc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddProp(int tag, IVwViewConstructor vc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddDerivedProp(int[] rgtag, int ctag, IVwViewConstructor vwvc, int frag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void NoteDependency(int[] rghvo, int[] rgtag, int chvo)
			{
			}

			/// <summary />
			public void NoteStringValDependency(int hvo, int tag, int ws, ITsString tssVal)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddStringProp(int tag, IVwViewConstructor vc)
			{
				EventHistory.Add(new StringPropAdded() { Hvo = OpenObject, Tag = tag, Vc = vc });
			}

			/// <summary />
			public void AddUnicodeProp(int tag, int ws, IVwViewConstructor vwvc)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddIntProp(int tag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddIntPropPic(int tag, IVwViewConstructor vc, int frag, int nMin, int nMax)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddStringAltMember(int tag, int ws, IVwViewConstructor vc)
			{
				EventHistory.Add(new StringAltMemberAdded() { Hvo = OpenObject, Tag = tag, Ws = ws, Vc = vc });
			}

			/// <summary />
			public void AddStringAlt(int tag)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddStringAltSeq(int tag, int[] rgenc, int cws)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Add literal text that is not a property and not editable.
			/// </summary>
			/// <param name="_ss"/>
			public void AddString(ITsString ss)
			{
				EventHistory.Add(new StringAdded { Content = ss });
			}

			/// <summary />
			public void AddTimeProp(int tag, uint flags)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Get the object currently being displayed. (This is the object whose properties
			///              will be used by the various Add methods.) Compare <c>OpenObject</c>.
			/// </summary>
			/// <returns/>
			public int CurrentObject()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void GetOuterObject(int ichvoLevel, out int hvo, out int tag, out int ihvo)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddWindow(IVwEmbeddedWindow ew, int dmpAscent, bool fJustifyRight, bool fAutoShow)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Add the special little grey box used to separate items in Data Entry lists.
			/// </summary>
			public void AddSeparatorBar()
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Insert a simple rectangular box with the specified color, height, and width.
			/// </summary>
			/// <param name="rgb"/><param name="dmpWidth">desired box width, or 1 to fill the available space. </param><param name="dmpHeight"/><param name="dmpBaselineOffset">positive to raise the box; 0 aligns bottom with baseline </param>
			public void AddSimpleRect(int rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenDiv()
			{
			}

			/// <summary />
			public void CloseDiv()
			{
			}

			/// <summary />
			public bool IsParagraphOpen()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenParagraph()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenTaggedPara()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenMappedPara()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenMappedTaggedPara()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenOverridePara(int cOverrideProperties, DispPropOverride[] rgOverrideProperties)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseParagraph()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenInnerPile()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseInnerPile()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenSpan()
			{
			}

			/// <summary />
			public void CloseSpan()
			{
			}

			/// <summary />
			public void OpenTable(int cCols, VwLength vlWidth, int mpBorder, VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule, int mpSpacing, int mpPadding, bool fSelectOneCol)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseTable()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenTableRow()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseTableRow()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenTableCell(int nRowSpan, int nColSpan)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseTableCell()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenTableHeaderCell(int nRowSpan, int nColSpan)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseTableHeaderCell()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void MakeColumns(int nColSpan, VwLength vlWidth)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void MakeColumnGroup(int nColSpan, VwLength vlWidth)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenTableHeader()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseTableHeader()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenTableFooter()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseTableFooter()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void OpenTableBody()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void CloseTableBody()
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void set_IntProperty(int tpt, int tpv, int nValue)
			{
				throw new NotSupportedException();
			}

			public List<object> EventHistory = new List<object>();

			/// <summary />
			public void set_StringProperty(int sp, string bstrValue)
			{
				EventHistory.Add(new StringPropSet() { ttp = sp, val = bstrValue });
			}

			/// <summary />
			public void get_StringWidth(ITsString tss, ITsTextProps ttp, out int dmpx, out int dmpy)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddPictureWithCaption(IPicture pict, int tag, ITsTextProps ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor vwvc)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void AddPicture(IPicture pict, int tag, int dxmpWidth, int dympHeight)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void SetParagraphMark(VwBoundaryMark boundaryMark)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public void EmptyParagraphBehavior(int behavior)
			{
				throw new NotSupportedException();
			}

			/// <summary />
			public int OpenObject { get; set; }

			/// <summary />
			public int EmbeddingLevel
			{
				get { throw new NotSupportedException(); }
			}

			/// <summary />
			public ISilDataAccess DataAccess { get; set; }

			/// <summary />
			public ITsTextProps Props
			{
				set { throw new NotSupportedException(); }
			}
		}
	}
}