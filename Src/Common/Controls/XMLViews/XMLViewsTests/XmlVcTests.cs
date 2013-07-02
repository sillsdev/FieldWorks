using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using SIL.Utils.ComTypes;
using XCore;

namespace XMLViewsTests
{
	/// <summary>
	/// Test (some aspects of) XmlVc
	/// </summary>
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_sda gets disposed in FixtureTeardown()")]
	public class XmlVcTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private RealDataCache m_sda;
		/// <summary>Writing System Manager (reset for each test)</summary>
		protected IWritingSystemManager m_wsManager;

		private int m_hvoLexDb; // root
		private int m_hvoKick; // one entry.

		private int kflidLexDb_Entries;
		private int kflidEntry_Form;
		private int kflidEntry_Summary;

		private int m_wsAnal;
		private int m_wsVern;

		internal const int kclsidLexDb = 1; // consistent with TextCacheModel.xml in resource file
		internal const int kclsidEntry = 7; // consistent with TextCacheModel.xml in resource file

		private ITsStrFactory m_tsf;

		private LayoutCache m_layouts;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			SetupTestModel(Resources.TextCacheModel_xml);

			m_sda = new RealDataCache();
			m_sda.MetaDataCache = MetaDataCache.CreateMetaDataCache("TestModel.xml");
			//m_cache.ParaContentsFlid = kflidParaContents;
			//m_cache.ParaPropertiesFlid = kflidParaProperties;
			//m_cache.TextParagraphsFlid = kflidTextParas;

			Debug.Assert(m_wsManager == null);
			m_wsManager = Cache.ServiceLocator.GetInstance<IWritingSystemManager>();
			m_sda.WritingSystemFactory = m_wsManager;

			m_wsAnal = Cache.DefaultAnalWs;

			m_wsVern = Cache.DefaultVernWs;

			//IWritingSystem deWs;
			//m_wsManager.GetOrSet("de", out deWs);
			//m_wsDeu = deWs.Handle;

			//m_wsManager.UserWs = m_wsEng;
			//m_wsUser = m_wsManager.UserWs;

			m_tsf = TsStrFactoryClass.Create();

			m_hvoLexDb = m_sda.MakeNewObject(kclsidLexDb, 0, -1, -1);

			kflidLexDb_Entries = m_sda.MetaDataCache.GetFieldId("LexDb", "Entries", false);
			kflidEntry_Form = m_sda.MetaDataCache.GetFieldId("Entry", "Form", false);
			kflidEntry_Summary = m_sda.MetaDataCache.GetFieldId("Entry", "Summary", false);

			m_hvoKick = m_sda.MakeNewObject(kclsidEntry, m_hvoLexDb, kflidLexDb_Entries, 0);
			m_sda.SetMultiStringAlt(m_hvoKick, kflidEntry_Form, m_wsVern, m_tsf.MakeString("kick", m_wsVern));
			m_sda.SetString(m_hvoKick, kflidEntry_Summary, m_tsf.MakeString("strike with foot", m_wsAnal));

			var keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new[] { "class", "type", "name", "choiceGuid" };
			keyAttrs["group"] = new[] { "label" };
			keyAttrs["part"] = new[] { "ref" };
			var layoutInventory = new Inventory("*.fwlayout", "/LayoutInventory/*", keyAttrs, "test", "nowhere");
			layoutInventory.LoadElements(Resources.Layouts_xml, 1);

			keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new[] { "id" };

			var partInventory = new Inventory("*Parts.xml", "/PartInventory/bin/*", keyAttrs, "test", "nowhere");
			partInventory.LoadElements(Resources.Parts_xml, 1);

			m_layouts = new LayoutCache(m_sda.MetaDataCache, layoutInventory, partInventory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			FileUtils.Manager.Reset();

			// GrowToWord causes a Char Property Engine to be created, and the test runner
			// fails if we don't shut the factory down.
			m_sda.Dispose();
			m_sda = null;

			base.FixtureTeardown();
		}

		public static void SetupTestModel(string cacheModelfile)
		{
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xsd", Encoding.UTF8))
			{
				fw.Write(SIL.FieldWorks.CacheLightTests.Properties.Resources.TestModel_xsd);
				fw.Close();
			}
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xml", Encoding.UTF8))
			{
				fw.Write(cacheModelfile);
				fw.Close();
			}
		}

		/// <summary>
		/// Test that displaying a string property produces marking indicating the XML configuration.
		/// </summary>
		[Test]
		public void StringPropIsMarked()
		{
			using (var view = new XmlView(m_hvoLexDb, "root", null, true, m_sda))
			{
				var vc = new XmlVc(null, "root", true, view, null, m_sda);
				vc.IdentifySource = true;
				vc.SetCache(Cache);
				vc.m_layouts = m_layouts;
				vc.DataAccess = m_sda;
				var testEnv = new MockEnv() {DataAccess = m_sda, OpenObject = m_hvoLexDb};
				vc.Display(testEnv, m_hvoLexDb, XmlVc.kRootFragId);
				VerifySourceIdentified(testEnv.EventHistory, m_hvoKick, kflidEntry_Form, m_wsVern, "Entry:basic:Headword:HeadwordL");
				VerifyLabel(testEnv.EventHistory, m_hvoKick, kflidEntry_Form, m_wsVern, 1, ")", "Entry:basic:Headword:HeadwordL");

				VerifyLabel(testEnv.EventHistory, m_hvoKick, kflidEntry_Form, m_wsVern, -3, "head(", "Entry:basic:Headword:HeadwordL");
				VerifySourceIdentified(testEnv.EventHistory, m_hvoKick, kflidEntry_Summary, "Entry:basic:Summary:Sum.");
			}
		}

		private void VerifySourceIdentified(List<Object> events, int hvo, int tag, int ws, string expected)
		{
			for (int i = 1; i < events.Count; i++)
			{
				var ssp = events[i] as MockEnv.StringAltMemberAdded;
				if (ssp == null || ssp.Hvo != hvo || ssp.Tag != tag || ssp.Ws != ws)
					continue;
				var sps = events[i - 1] as MockEnv.StringPropSet;
				Assert.That(sps, Is.Not.Null);
				Assert.That(sps.ttp, Is.EqualTo((int)FwTextPropType.ktptBulNumTxtBef));
				Assert.That(sps.val, Is.EqualTo(expected));
				break;
			}
		}

		private void VerifySourceIdentified(List<Object> events, int hvo, int tag, string expected)
		{
			for (int i = 1; i < events.Count; i++)
			{
				var spa = events[i] as MockEnv.StringPropAdded;
				if (spa == null || spa.Hvo != hvo || spa.Tag != tag)
					continue;
				var sps = events[i - 1] as MockEnv.StringPropSet;
				Assert.That(sps, Is.Not.Null);
				Assert.That(sps.ttp, Is.EqualTo((int)FwTextPropType.ktptBulNumTxtBef));
				Assert.That(sps.val, Is.EqualTo(expected));
				break;
			}
		}

		private void VerifyLabel(List<Object> events, int hvo, int tag, int ws, int offset, string expectContent, string expectSource)
		{
			for (int i = 1; i < events.Count; i++)
			{
				var ssp = events[i] as MockEnv.StringAltMemberAdded;
				if (ssp == null || ssp.Hvo != hvo || ssp.Tag != tag || ssp.Ws != ws)
					continue;
				var sa = events[i + offset] as MockEnv.StringAdded;
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
			var vc = new XmlVc(null, "root", true, null, null, sda);
			vc.SetCache(Cache);
			var sut = new XmlVcDisplayVec(vc, new MockEnv(), entry.Hvo, LexEntryTags.kflidSenses, 1);

			Assert.That(sut.CalculateAndFormatSenseLabel(new int[] {sense.Hvo}, 0, "%O)"), Is.EqualTo("77)"),
				"CalculateAndFormatSenseLabel should have used the decorator method");
			Assert.That(sda.Tag, Is.EqualTo(Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "LexSenseOutline", false)),
				"CalculateAndFormatSenseLabel should have used the right property");
		}
	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "Cache is a reference and will be disposed in parent class")]
	class MockDecorator : DomainDataByFlidDecoratorBase
	{
		private FdoCache m_cache;
		public MockDecorator(FdoCache cache) : base(cache.DomainDataByFlid as ISilDataAccessManaged)
		{
			m_cache = cache;
		}

		public int Tag;

		public override ITsString get_StringProp(int hvo, int tag)
		{
			Tag = tag;
			return m_cache.TsStrFactory.MakeString("77", m_cache.DefaultUserWs);
		}
	}

	class MockEnv : IVwEnv
	{
		/// <summary>
		/// Display an (atomic) object prop. Calls <c>IVwViewConstructor.Display</c> for the object
		///              that is the value of the (atomic) object property. Passes 0 as the HVO of the
		///              Display method if the property is empty.
		/// </summary>
		/// <param name="tag">Identifies the property; used to retrieve the value using
		///              <c>ISilDataAccess.get_ObjectProp</c></param><param name="_vwvc">View constructor that will be sent the Display message to display the
		///              object. Usually the caller is a view constructor and will pass 'this', but it
		///              is possible for one view constructor to make use of another one. </param><param name="frag">A value to pass on to the Display method, indicating what kind of
		///              display of the object is desired. </param>
		public void AddObjProp(int tag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Display a vector property. Calls <c>IVwViewConstructor.DisplayVec</c>, passing the
		///              current HVO, the tag specified here, and the frag specied here, to the view
		///              constructor specified here.
		/// </summary>
		/// <param name="tag"/><param name="_vwvc"/><param name="frag"/>
		public void AddObjVec(int tag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Display a vector property. Calls <c>IVwViewConstructor.Display</c> for each item in
		///              the vector, passing the HVO of each item (obtained by passing the HVO and tag to
		///              <c>ISilDataAccess.get_VecItem</c>) and the frag specied here, to the view
		///              constructor specified here (usually the caller of the method).
		/// </summary>
		/// <param name="tag"/><param name="_vwvc"/><param name="frag"/>
		public void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			//OpenProp(tag);
			int cobj = DataAccess.get_VecSize(OpenObject, tag);

			for (int i = 0; i < cobj; i++)
			{
				int hvoItem = DataAccess.get_VecItem(OpenObject, tag, i);
					OpenTheObject(hvoItem, i);
					vc.Display(this, hvoItem, frag);
					CloseTheObject();
				//if (Finished)
				//    break;
			}

			//CloseProp();
		}

		private List<int> m_openObjects = new List<int>();

		private void OpenTheObject(int hvo, int index)
		{
			m_openObjects.Add(OpenObject);
			OpenObject = hvo;
		}

		void CloseTheObject()
		{
			OpenObject = m_openObjects.Last();
			m_openObjects.RemoveAt(m_openObjects.Count - 1);
		}

		/// <summary>
		/// Display a vector property in reverse order. Calls <c>IVwViewConstructor.Display</c> for
		///              each item in the vector, passing the HVO of each item (obtained by passing the HVO
		///              and tag to <c>ISilDataAccess.get_VecItem</c>) and the frag specied here, to the view
		///              constructor specified here (usually the caller of the method).
		/// </summary>
		/// <param name="tag"/><param name="_vwvc"/><param name="frag"/>
		public void AddReversedObjVecItems(int tag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Display another object, not one of your own properties. Calls
		///              <c>IVwViewConstructor.Display</c>, passing the HVO and frag specified here, on the
		///              view constructor specified here.
		///              Consider using NoteDependency if this part of the display should change when
		///              certain propertiese.g., those you used to find the objectchange.
		/// </summary>
		/// <param name="hvo"/><param name="_vwvc"/><param name="frag"/>
		public void AddObj(int hvo, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Display a vector property using laziness.
		///              Nothing is added to the display immediately, but at some point the views code will call the
		///              <c>IVwViewConstructor.EstimateHeight</c> method to find out how high one or more items are.
		///              At that time or later, it may call your <c>IVwViewConstructor.LoadData</c> method,
		///              followed by the <c>IVwViewConstructor.Display</c> method,
		///              for one or more items in the property, as needed.
		///              Note: the current flow object MUST be a Div (or the root).
		/// </summary>
		/// <param name="tag"/><param name="_vwvc"/><param name="frag"/>
		public void AddLazyVecItems(int tag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Display a list of items using laziness. Typically, this is used to implement
		///              DisplayVec, either of the complete property contents, or a filtered subset.
		///              Nothing is added to the display immediately, but at some point the views code will call the
		///              <c>IVwViewConstructor.EstimateHeight</c> method to find out how high one or more items are.
		///              At that time or later, it may call your <c>IVwViewConstructor.LoadData</c> method,
		///              followed by the <c>IVwViewConstructor.Display</c> method,
		///              for one or more items in the list, as needed.
		///              Note: this method has not been tested and I suspect the implementation is incomplete.
		/// </summary>
		/// <param name="_rghvo"/><param name="chvo"/><param name="_vwvc"/><param name="frag"/>
		public void AddLazyItems(int[] _rghvo, int chvo, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// :&gt; Generic basic property displays, managed by the VC.
		///              Display a property, with the client handling the process of formatting it as a
		///              string. (Calls <c>IVwViewConstructor.DisplayVariant</c>, and the resulting
		///              string is inserted into the display.) If the user edits the data, then when focus leaves
		///              the display of the property, the system calls its <c>IViewConstructor.UpdateProp</c>
		///              method. That method is responsible to make an appropriate change to the underlying
		///              data, or veto it with an appropriate error message.
		///              The variant is obtained by calling <c>ISilDataAccess.get_Prop</c>. Current implementations
		///              of ISilDataAcess can only provide variants for 4byte and 8byte integers and
		///              simple string (ITsString, nonalternation) properties. This could fairly easily be
		///              extended if needed.
		///              This may eventually also be used for string alts. The Alternation object will get
		///              passed as an IUnknown variant.
		///              If pvwvc is not prepared to receive an UpdateProp call when the string is edited,
		///              be sure to disable editing of this property.
		/// </summary>
		/// <param name="tag"/><param name="_vwvc"/><param name="frag"/>
		public void AddProp(int tag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is similar to <c>AddProp</c>, but used where it is necessary to follow a path of
		///              properties to get to the desired object/property. The first tag in prgtag
		///              indicates an atomic object property of the current open object, the
		///              next an atomic object property of the one obtained from the first property,
		///              and so on until the last indicates the property that is represented by
		///              the string. This last tag (and the corresponding object) is what gets
		///              passed to UpdateProp(). If any of the attrs along the path is null, nothing gets added.
		///              DisplayVariant is used to display the property (which must be basic).
		///              <h3>Note</h3> This method is not yet implemented.
		/// </summary>
		/// <param name="_rgtag"/><param name="ctag"/><param name="_vwvc"/><param name="frag"/>
		public void AddDerivedProp(int[] _rgtag, int ctag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inform the view of special dependencies. The current flow object (and anything else
		///              that is part of the same object in the same higher level property) needs to be
		///              regenerated if any of the listed properties changes.
		/// </summary>
		/// <param name="_rghvo"/><param name="_rgtag"/><param name="chvo"/>
		public void NoteDependency(int[] _rghvo, int[] _rgtag, int chvo)
		{
		}

		/// <summary>
		/// Inform the view of special dependencies. The current flow object (and anything else
		///              that is part of the same object in the same higher level property) needs to be
		///              regenerated if there is a change (from the time where this method is called)
		///              in whether it is true that ptssVal is equal to the specified property.
		///              It is a multilingual string property if ws is nonzero, otherwise a plain string.
		///              Note that this can be VERY much more efficient than using NoteDependency for such
		///              conditions, which will regenerate every time the property changes.
		/// </summary>
		/// <param name="hvo"/><param name="tag"/><param name="ws"/><param name="_tssVal"/>
		public void NoteStringValDependency(int hvo, int tag, int ws, ITsString _tssVal)
		{
			throw new NotImplementedException();
		}

		public class StringPropAdded
		{
			public int Hvo;
			public int Tag;
			public IVwViewConstructor Vc;
		}

		/// <summary>
		/// :&gt; Inserting basic object property displays into the view.
		///              The view looks up the value of the indicated property on the current open
		///              object and displays it. The property must be a (non alternation) string property.
		/// </summary>
		public void AddStringProp(int tag, IVwViewConstructor vc)
		{
			EventHistory.Add(new StringPropAdded() {Hvo = OpenObject, Tag = tag, Vc = vc});
		}

		/// <summary>
		/// Add a Unicode string property, as if it were a string in the specified WS.
		/// </summary>
		/// <param name="tag"/><param name="ws"/><param name="_vwvc"/>
		public void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// The view looks up the value of the indicated property on the current open
		///              object and displays it as a decimal string. The property must be an integer property.
		/// </summary>
		/// <param name="tag"/>
		public void AddIntProp(int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// The view looks up the value of the indicated property in the current open
		///              object and passes it to the view constructor's DisplayPicture method.
		///              If nMax is greater than nMin, automatic cycling is performed: clicking on one
		///              of the picutes will not make a selection, but will merely increment the
		///              specified property (and rotate from nMax to nMin).
		/// </summary>
		/// <param name="tag"/><param name="_vc"/><param name="frag"/><param name="nMin"/><param name="nMax"/>
		public void AddIntPropPic(int tag, IVwViewConstructor _vc, int frag, int nMin, int nMax)
		{
			throw new NotImplementedException();
		}

		public class StringAltMemberAdded
		{
			public int Hvo;
			public int Tag;
			public int Ws;
			public IVwViewConstructor Vc;
		}
		/// <summary>
		/// :&gt; ENHANCE JohnT: we should have individual methods and default formats
		///             :&gt; for more of the basic types.
		///             :&gt; String alternations.
		///              Add a single, isolated alternative (of the indicated property of the open object).
		/// </summary>
		public void AddStringAltMember(int tag, int ws, IVwViewConstructor vc)
		{
			EventHistory.Add(new StringAltMemberAdded() { Hvo = OpenObject, Tag = tag, Ws = ws, Vc = vc});
		}

		/// <summary>
		/// Add all the alternatives present in the indicated property of the current object,
		///              in a default format labelled by writing system tag markers.
		///              If the current flow object is a paragraph, the items are separated with a
		///              space. If the current flow object is a pile (Div or InnerPile), each
		///              alternative (with identifying tag) is a row.
		///              ENHANCE JohnT: what controls the order of the encodings? As found?
		///              Alphabetical by enc? Numeric by enc? Need to resolve this before
		///              implementing.
		///              <h3>Note</h3> This method is not yet implemented.
		/// </summary>
		/// <param name="tag"/>
		public void AddStringAlt(int tag)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Display the indicated multialternative string property of the current object,
		///              showing the alternaties specified by the given list of encodings, with ws tags.
		///              An ws appears (as an empty string) even if the corresponding alternative is absent.
		///              If the current flow object is a paragraph, the items are separated with a
		///              space. If the current flow object is a pile (Div or InnerPile), each
		///              alternative (with identifying tag) is a row.
		///              <h3>Note</h3> This method is not yet implemented.
		/// </summary>
		/// <param name="tag"/><param name="_rgenc"/><param name="cws"/>
		public void AddStringAltSeq(int tag, int[] _rgenc, int cws)
		{
			throw new NotImplementedException();
		}

		public class StringAdded
		{
			public ITsString Content;
		}
		/// <summary>
		/// Add literal text that is not a property and not editable.
		/// </summary>
		/// <param name="_ss"/>
		public void AddString(ITsString _ss)
		{
			EventHistory.Add(new StringAdded {Content = _ss});
		}

		/// <summary>
		/// Add an SilTime property, formatted according to the default user locale.
		///              The flags argument is currently interpreted as by
		///              ::GetDateFormat, but we may decide to restrict the full range of these
		///              options to achieve easier portability. Passing DATE_SHORTDATE
		///              is safe.
		/// </summary>
		/// <param name="tag"/><param name="flags"/>
		public void AddTimeProp(int tag, uint flags)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the object currently being displayed. (This is the object whose properties
		///              will be used by the various Add methods.) Compare <c>OpenObject</c>.
		/// </summary>
		/// <returns/>
		public int CurrentObject()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get info about outer objects, inside whose display the display of the
		///              current object is embedded.
		///              The outermost object is returned at level 0. Whatever property of that
		///              object the next object is embedded in is returned in ptag.
		///              The index of the nextlevel object in that property (if it is a vector
		///              property) is returned in pihvo; if not a vector property, that value
		///              is always zero. The Level argument may range from 0 to EmbeddingLevel() 1.
		///              Level 0 is one of the toplevel objects passed to SetRootObjects.
		/// </summary>
		/// <param name="ichvoLevel"/><param name="_hvo"/><param name="_tag"/><param name="_ihvo"/>
		public void GetOuterObject(int ichvoLevel, out int _hvo, out int _tag, out int _ihvo)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Add a display region whose contents are controlled by the client, which must
		///              implement <c>IVwEmbeddedWindow</c>. The mechanism is designed to allow either an
		///              embedded window (which gets moves as the view is laid out) or just a region of
		///              the larger window which the client draws into.
		///              ENHANCE JohnT: can this be implemented reliably? What if the root box is displayed
		///              in two places, so we can't just move the embedded Window? (Could we say that it
		///              appears in the primary split? Which one is primary?)
		/// </summary>
		/// <param name="_ew"/><param name="dmpAscent">Distance from top of embedded box to baseline for text alignment </param><param name="fJustifyRight">True if embedded box is last in para and should be rightaligned,
		///              as we do with "..." buttons in data entry. (However, that facility is currently
		///              implemented a different way). </param><param name="fAutoShow">True if true view will ensure visibility before drawing.
		///              <h3>Note</h3> This method is not yet implemented. </param>
		public void AddWindow(IVwEmbeddedWindow _ew, int dmpAscent, bool fJustifyRight, bool fAutoShow)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Add the special little grey box used to separate items in Data Entry lists.
		/// </summary>
		public void AddSeparatorBar()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Insert a simple rectangular box with the specified color, height, and width.
		/// </summary>
		/// <param name="rgb"/><param name="dmpWidth">desired box width, or 1 to fill the available space. </param><param name="dmpHeight"/><param name="dmpBaselineOffset">positive to raise the box; 0 aligns bottom with baseline </param>
		public void AddSimpleRect(int rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// :&gt; Delimit layout flow objects
		///              Start a group of paragraphs sharing properties.
		/// </summary>
		public void OpenDiv()
		{
		}

		/// <summary>
		/// End a group of paragraphs sharing properties.
		/// </summary>
		public void CloseDiv()
		{
		}

		/// <summary>
		/// Start a normal paragraph.
		/// </summary>
		public void OpenParagraph()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a paragraph that supports display of tagging, if an overlay is installed in the
		///              root box.
		/// </summary>
		public void OpenTaggedPara()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a paragraph that supports certain kinds of onethefly text substitution.
		///              Currently the only type supported is an embedded object character with text
		///              property ktptObjData's first character set to kodtNameGuidHot. The system finds
		///              a name for the specified object using <c>IVwViewConstructor.GetStrForGuid</c>, and displays
		///              that.
		/// </summary>
		public void OpenMappedPara()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a paragraph that supports both tagging and mapping.
		/// </summary>
		public void OpenMappedTaggedPara()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a paragraph that is intended to be a line in a concordance display.
		///              A key word is identified, typically the item being concorded on, and its position is
		///              indicated. Typically, the keyword is bold, and gets aligned with a specified position,
		///              dmpAlign. Alignment will be left, center, or right, according to the alignment of the
		///              paragraph as a whole.
		///              (Nonleft alignment is not yet implemented.)
		/// </summary>
		/// <param name="ichMinItem">Indicate the position of the item being concorded. Depending
		///              on the flags, this item is typically made bold and aligned with dmpAlign </param><param name="ichLimItem">Indicate the position of the item being concorded. Depending
		///              on the flags, this item is typically made bold and aligned with dmpAlign </param><param name="cpoFlags">indicates whether to bold the key word, and whether to align it.
		///              Eventually other flag bits may be supported. </param><param name="dmpAlign">distance from left of paragraph to align keywords. </param>
		public void OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a paragraph that supports overrides
		/// </summary>
		/// <param name="cOverrideProperties">Number of overriden properties </param><param name="_rgOverrideProperties">Array of override properties </param>
		public void OpenOverridePara(int cOverrideProperties, DispPropOverride[] _rgOverrideProperties)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End a paragraph (of any type).
		/// </summary>
		public void CloseParagraph()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a pile of paras embedded in another para, for interlinear.
		/// </summary>
		public void OpenInnerPile()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End a pile of paras embedded in another para, for interlinear.
		/// </summary>
		public void CloseInnerPile()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a group of subpara objects sharing properties
		/// </summary>
		public void OpenSpan()
		{
		}

		/// <summary>
		/// End a group of subpara objects sharing properties
		/// </summary>
		public void CloseSpan()
		{
		}

		/// <summary>
		/// :&gt; ENHANCE JohnT: VwLength is used only in three places now. Would it be better to
		///             :&gt; just have an int plus a FwTextPropVar?
		///              Start a table.
		/// </summary>
		/// <param name="cCols">The number of columns the table will have. </param><param name="vlWidth">The width of whole table. If the unit is percent, it is relative
		///              to the available width for laying out the table. </param><param name="mpBorder">The thickness of the border drawn around the whole table.
		///              This can be overridden by individual cells which explicitly set a border on the
		///              relevant side. </param><param name="vwalign">Default alignment for text in the table (not implemented, I think) </param><param name="frmpos">Indicates which sides of the table to draw a border all around it. </param><param name="vwrule">Indicates where to draw lines between cells. </param><param name="mpSpacing">between cells </param><param name="mpPadding">between cell border and contents </param><param name="fSelectOneCol">true to keep the selection in one column, false to use normal
		///              selections. </param>
		public void OpenTable(int cCols, VwLength vlWidth, int mpBorder, VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule, int mpSpacing, int mpPadding, bool fSelectOneCol)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End a table.
		/// </summary>
		public void CloseTable()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a row in a table.
		/// </summary>
		public void OpenTableRow()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End a row in a table.
		/// </summary>
		public void CloseTableRow()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a cell in a table.
		/// </summary>
		/// <param name="nRowSpan">Number of rows in the table occupied by the cell </param><param name="nColSpan">Number of columns in the table occupied by the cell. </param>
		public void OpenTableCell(int nRowSpan, int nColSpan)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End a cell in a table.
		/// </summary>
		public void CloseTableCell()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a header cell in a table. Params as in <c>OpenCell</c>.
		/// </summary>
		/// <param name="nRowSpan"/><param name="nColSpan"/>
		public void OpenTableHeaderCell(int nRowSpan, int nColSpan)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End a header cell in a table.
		/// </summary>
		public void CloseTableHeaderCell()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Specify the width of columns in a table. Should be called after <c>OpenTable</c> and
		///              before <c>OpenTableBody</c> or <c>OpenTableHeader</c>. Each call sets the width of a
		///              specified number of columns. This should be called enough times (in combination
		///              with MakeColumnGroup) to account for all the columns in the table.
		/// </summary>
		/// <param name="nColSpan">Number of columns which have this same width (NOT an index!). </param><param name="vlWidth">Percent of the overall space available for laying out the table. </param>
		public void MakeColumns(int nColSpan, VwLength vlWidth)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is like <c>MakeColumns</c>, except that a "group" of columns ends after the
		///              last one whose width is specified using this method. Groups are significant
		///              for some options regarding drawing rules between cells; see the vwrule parameter
		///              of <c>OpenTable</c>.
		/// </summary>
		/// <param name="nColSpan"/><param name="vlWidth"/>
		public void MakeColumnGroup(int nColSpan, VwLength vlWidth)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start the header section of a table. This is like a body section except that
		///              (eventually) in a long printout it will be repeated at the start of each new page.
		/// </summary>
		public void OpenTableHeader()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End the header section of a table.
		/// </summary>
		public void CloseTableHeader()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start the footer section of a table. Note that this is specified before the body,
		///              so that (eventually) we can repeat it at the bottom of each page in a long table,
		///              which requires us to know what it is even before we have all the information
		///              about the body.
		/// </summary>
		public void OpenTableFooter()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End the footer section of a table.
		/// </summary>
		public void CloseTableFooter()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Start a body section of a table. There may be multiple body sections; each defines
		///              a "group" of rows. (Groups are significant
		///              for some options regarding drawing rules between cells; see the vwrule parameter
		///              of <c>OpenTable</c>).
		/// </summary>
		public void OpenTableBody()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// End a body section of a table.
		/// </summary>
		public void CloseTableBody()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Specify an integer style property to be applied to the next flow object opened.
		///              Ignored if a close flow object operation precedes the next open.
		///              Adding anything basic (such as a string or int object property, string literal,
		///              string alternation, or string generated using AddProp/DisplayVariant)
		///              means the property applies as if to a span containing just that text.
		/// </summary>
		/// <param name="tpt">A value from the FwTextPropType enumeration, identifying the property
		///              to be set. It should be a property identified as having an integer value.
		///              Note: to allow for forwards compatibility, we unrecognized properties are simply
		///              ignored. </param><param name="tpv">A value from the FwTextPropVar enumeration, indicating how to
		///              interpret the value. For example, it may be a member of another enumeration,
		///              a length in millipoints, a percent of some other distance, a color, etc. </param><param name="nValue"/>
		public void set_IntProperty(int tpt, int tpv, int nValue)
		{
			throw new NotImplementedException();
		}

		public List<Object> EventHistory = new List<object>();

		public class StringPropSet
		{
			public int ttp;
			public string val;
		}

		/// <summary>
		/// Specify a string style property to be applied to the next flow object opened.
		///              Ignored if a close flow object operation precedes the next open.
		///              Adding anything basic (such as a string or int object property, string literal,
		///              string alternation, or string generated using AddProp/DisplayVariant)
		///              means the property applies as if to a span containing just that text.
		/// </summary>
		/// <param name="sp">A value from VwStyleProperty denoting property to set. </param><param name="bstrValue"/>
		public void set_StringProperty(int sp, string bstrValue)
		{
			EventHistory.Add(new StringPropSet() {ttp = sp, val = bstrValue});
		}

		/// <summary>
		/// :&gt; More informative routines to help with fancy layouts.
		///              Gives the height and width required to lay out the given string,
		///              using current display properties plus those produced by pttp (which
		///              may be null). In other words, this is the amount of space that would
		///              be occupied if one currently called putref_Props(pttp); AddString(ptss);
		///              (assuming infinite available width).
		///              EberhardB: This used to be [propget] and named without get_. However, this results
		///              in a TLBIMP warning because we don't specify a [retval], so I renamed it.
		/// </summary>
		/// <param name="_tss"/><param name="_ttp"/><param name="dmpx"/><param name="dmpy"/>
		public void get_StringWidth(ITsString _tss, ITsTextProps _ttp, out int dmpx, out int dmpy)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Insert a picture along with the specified ws alternative of the caption.
		///              Mark it as if it came from the property tag, though we don't actually read data from
		///              there (the caller can pass any value that might be useful later to identify something
		///              about the picture the user clicked). If the tag argument is not useful, pass ktagNotAnAttr.
		///              It is planned that a positive dxmpHeight is a maximum height, negative is an exact
		///              height, and 0 means don't specify height (use natural height or determine from
		///              width and aspect ratio). Similarly for width.
		///              Currently only max height is implemented. Always pass zero for width.
		/// </summary>
		/// <param name="_pict"/><param name="tag"/><param name="_ttpCaption"/><param name="hvoCmFile"/><param name="ws"/><param name="dxmpWidth"/><param name="dympHeight"/><param name="_vwvc"/>
		public void AddPictureWithCaption(IPicture _pict, int tag, ITsTextProps _ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor _vwvc)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Insert a picture, optionally limiting the height and width.
		///              Mark it as if it came from the property tag, though we don't actually read data from
		///              there (the caller can pass any value that might be useful later to identify something
		///              about the picture the user clicked). If the tag argument is not useful, pass ktagNotAnAttr.
		///              It is planned that a positive dxmpHeight is a maximum height, negative is an exact
		///              height, and 0 means don't specify height (use natural height or determine from
		///              width and aspect ratio). Similarly for width.
		///              Currently only max height is implemented. Always pass zero for width.
		/// </summary>
		/// <param name="_pict"/><param name="tag"/><param name="dxmpWidth"/><param name="dympHeight"/>
		public void AddPicture(IPicture _pict, int tag, int dxmpWidth, int dympHeight)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Setting a Unicode character that indicates a boundary (e.g. for a paragraph or section).
		/// </summary>
		/// <param name="boundaryMark"/>
		public void SetParagraphMark(VwBoundaryMark boundaryMark)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called while a paragraph is open, this controls how the view will behave if no
		///              content is added to the paragraph before it is closed.
		///              Currently the argument must be 1; the only reason to have the argument at
		///              all is in the interests of forward compatibility if we think of more behaviors.
		///              The default behavior is that the paragraph behaves as if it contained a readonly
		///              empty string.
		///              The behavior when this method is called (with argument 1) is to make the
		///              empty paragraph as nearly as possible invisible.
		/// </summary>
		/// <param name="behavior"/>
		public void EmptyParagraphBehavior(int behavior)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Current open object; null if VC has called AddObjVec and has not yet
		///              called AddObj for any of the vector items. Compare <c>CurrentObject</c>,
		///              which gives the object owning the vector in that circumstance.
		/// </summary>
		/// <returns>
		/// A System.Int32
		/// </returns>
		public int OpenObject { get; set; }

		/// <summary>
		/// Get the current embedding level: the number of layers of object we
		///              are displaying. The number of outer objects (see <c>GetOuterObject</c>
		///              is one less than this.
		/// </summary>
		/// <returns>
		/// A System.Int32
		/// </returns>
		public int EmbeddingLevel
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Get the data access object in use. This allows the view constructor to get at
		///              other properties of the object.
		/// </summary>
		/// <returns>
		/// A ISilDataAccess
		/// </returns>
		public ISilDataAccess DataAccess { get; set; }

		/// <summary>
		/// Specify a group of properties to be applied to the next flow object opened.
		///              All the properties in the TsTextProps are applied in one operation.
		///              This is considerably more efficient than <c>put_IntProperty</c> or
		///              <c>put_StringProperty</c>, especially if the same set of values is used repeatedly.
		///              Ignored if a close flow object operation precedes the next open.
		///              Adding anything basic (such as a string or int object property, string literal,
		///              string alternation, or string generated using AddProp/DisplayVariant)
		///              means the property applies as if to a span containing just that text.
		/// </summary>
		/// <returns>
		/// A ITsTextProps
		/// </returns>
		public ITsTextProps Props
		{
			set { throw new NotImplementedException(); }
		}
	}
}
