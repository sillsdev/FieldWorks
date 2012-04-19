using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Palaso.WritingSystems;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	[TestFixture]
	[SetCulture("en-US")]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "Unit tests, gets disposed in FixtureTearDown()")]
	public class PalasoWritingSystemManagerTests : FwCOMTestBase // can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private DebugProcs m_DebugProcs;

		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			// This needs to be set for ICU
			RegistryHelper.CompanyName = "SIL";
			Icu.InitIcuDataDir();
			m_DebugProcs = new DebugProcs();
		}

		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			m_DebugProcs.Dispose();
			m_DebugProcs = null;
		}

		private static string PrepareTempStore(string name)
		{
			string path = Path.Combine(Path.GetTempPath(), name);
			if (Directory.Exists(path))
				Directory.Delete(path, true);
			Directory.CreateDirectory(path);
			return path;
		}

		/// <summary>
		/// Tests serialization and deserialization of writing systems.
		/// </summary>
		[Test]
		public void SerializeDeserialize()
		{
			string storePath = PrepareTempStore("Store");

			// serialize
			var wsManager = new PalasoWritingSystemManager(new LocalFileWritingSystemStore(storePath));
			var ws = wsManager.Set("en-US");
			ws.SpellCheckingId = "en-US";
			ws.MatchedPairs = "matched pairs";
			ws.LCID = 0x409;
			ws.ValidChars = "valid characters";
			ws.LegacyMapping = "legacy mapping";
			wsManager.Save();

			// deserialize
			wsManager = new PalasoWritingSystemManager(new LocalFileWritingSystemStore(storePath));
			Assert.IsTrue(wsManager.Exists("en-US"));
			ws = wsManager.Get("en-US");
			Assert.AreEqual("Eng", ws.Abbreviation);
			Assert.AreEqual("English", ws.LanguageSubtag.Name);
			Assert.AreEqual("en-US", ws.SpellCheckingId);
			Assert.AreEqual("United States", ws.RegionSubtag.Name);
			Assert.AreEqual("matched pairs", ws.MatchedPairs);
			Assert.AreEqual(0x409 , ws.LCID);
			Assert.AreEqual("valid characters", ws.ValidChars);
			Assert.AreEqual("legacy mapping", ws.LegacyMapping);
			Assert.AreEqual("eng", ws.ISO3);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the global store.
		/// </summary>
		[Test]
		public void GlobalStore()
		{
			string storePath1 = PrepareTempStore("Store1");
			string storePath2 = PrepareTempStore("Store2");
			string globalStorePath = PrepareTempStore("GlobalStore");

			var globalStore = new GlobalFileWritingSystemStore(globalStorePath);
			var wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			var ws = wsManager.Set("en-US");
			ws.SpellCheckingId = "id1";
			wsManager.Save();
			Assert.IsTrue(File.Exists(Path.Combine(storePath1, "en-US.ldml")));
			Assert.IsTrue(File.Exists(Path.Combine(globalStorePath, "en-US.ldml")));

			Thread.Sleep(1000);

			DateTime lastModified = File.GetLastWriteTime(Path.Combine(globalStorePath, "en-US.ldml"));
			wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, globalStore), globalStore);
			ws = wsManager.Set("en-US");
			ws.SpellCheckingId = "id2";
			wsManager.Save();
			Assert.Less(lastModified, File.GetLastWriteTime(Path.Combine(globalStorePath, "en-US.ldml")));

			lastModified = File.GetLastWriteTime(Path.Combine(storePath1, "en-US.ldml"));
			wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			ws = wsManager.Get("en-US");
			Assert.AreEqual("id1", ws.SpellCheckingId);
			IEnumerable<IWritingSystem> sharedWss = wsManager.CheckForNewerGlobalWritingSystems();
			Assert.AreEqual(1, sharedWss.Count());
			IWritingSystem sharedWs = sharedWss.First();
			Assert.AreEqual("en-US", sharedWs.Id);
			wsManager.Replace(sharedWs);
			wsManager.Save();

			ws = wsManager.Get("en-US");
			Assert.AreEqual("id2", ws.SpellCheckingId);
			Assert.Less(lastModified, File.GetLastWriteTime(Path.Combine(storePath1, "en-US.ldml")));
		}

		/// <summary>
		/// Tests the global store.
		/// </summary>
		[Test]
		public void GlobalStore_WritingSystemsToIgnore()
		{
			string storePath1 = PrepareTempStore("Store1");
			string storePath2 = PrepareTempStore("Store2");
			string globalStorePath = PrepareTempStore("GlobalStore");

			var globalStore = new GlobalFileWritingSystemStore(globalStorePath);
			var wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			var ws = wsManager.Set("en-US");
			ws.SpellCheckingId = "id1";
			wsManager.Save();

			Thread.Sleep(1000);

			wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, globalStore), globalStore);
			ws = wsManager.Set("en-US");
			ws.SpellCheckingId = "id2";
			wsManager.Save();

			wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			IEnumerable<IWritingSystem> sharedWss = wsManager.CheckForNewerGlobalWritingSystems();
			Assert.AreEqual(1, sharedWss.Count());
			Assert.AreEqual("en-US", sharedWss.First().Id);
			ws = wsManager.Get("en-US");
			Assert.AreEqual("id1", ws.SpellCheckingId);
			wsManager.Save();

			wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			sharedWss = wsManager.CheckForNewerGlobalWritingSystems();
			Assert.AreEqual(0, sharedWss.Count());
			wsManager.Save();

			Thread.Sleep(1000);

			wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, globalStore), globalStore);
			ws = wsManager.Get("en-US");
			ws.LegacyMapping = "encoding converter";
			wsManager.Save();

			wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			ws = wsManager.Get("en-US");
			Assert.IsNullOrEmpty(ws.LegacyMapping);
			sharedWss = wsManager.CheckForNewerGlobalWritingSystems();
			Assert.AreEqual(1, sharedWss.Count());
			IWritingSystem sharedWs = sharedWss.First();
			Assert.AreEqual("en-US", sharedWs.Id);
			wsManager.Replace(sharedWs);
			wsManager.Save();
			ws = wsManager.Get("en-US");
			Assert.AreEqual("encoding converter", ws.LegacyMapping);
		}

		/// <summary>
		/// Tests the get_Engine method.
		/// </summary>
		[Test]
		public void get_Engine()
		{
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem enWs = wsManager.Set("en-US");
			ILgWritingSystem enLgWs = wsManager.get_Engine("en-US");
			Assert.AreSame(enWs, enLgWs);

			Assert.IsFalse(wsManager.Exists("en-Latn-US"));
			// this should create a new writing system, since it doesn't exist
			ILgWritingSystem enUsLgWs = wsManager.get_Engine("en-Latn-US");
			Assert.IsTrue(wsManager.Exists("en-Latn-US"));
			Assert.IsTrue(wsManager.Exists(enUsLgWs.Handle));
			IWritingSystem enUsWs = wsManager.Get("en-Latn-US");
			Assert.AreSame(enUsWs, enUsLgWs);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_EngineOrNull method.
		/// </summary>
		[Test]
		public void get_EngineOrNull()
		{
			var wsManager = new PalasoWritingSystemManager();
			Assert.IsNull(wsManager.get_EngineOrNull(1));
			IWritingSystem ws = wsManager.Set("en-US");
			Assert.AreSame(ws, wsManager.get_EngineOrNull(ws.Handle));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the GetWsFromStr method.
		/// </summary>
		[Test]
		public void GetWsFromStr()
		{
			var wsManager = new PalasoWritingSystemManager();
			Assert.AreEqual(0, wsManager.GetWsFromStr("en-US"));
			IWritingSystem ws = wsManager.Set("en-US");
			Assert.AreEqual(ws.Handle, wsManager.GetWsFromStr("en-US"));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the GetStrFromWs method.
		/// </summary>
		[Test]
		public void GetStrFromWs()
		{
			var wsManager = new PalasoWritingSystemManager();
			Assert.IsNull(wsManager.GetStrFromWs(1));
			IWritingSystem ws = wsManager.Set("en-US");
			Assert.AreEqual("en-US", wsManager.GetStrFromWs(ws.Handle));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the Create method.
		/// </summary>
		[Test]
		public void Create()
		{
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem enWs = wsManager.Create("en-Latn-US-fonipa");
			Assert.AreEqual("Eng", enWs.Abbreviation);
			Assert.AreEqual("English", enWs.LanguageSubtag.Name);
			Assert.AreEqual("Latin", enWs.ScriptSubtag.Name);
			Assert.AreEqual("United States", enWs.RegionSubtag.Name);
			Assert.AreEqual("International Phonetic Alphabet", enWs.VariantSubtag.Name);
			// On Linux InstalledInputLanguages or DefaultInputLanguage doesn't do anything sensible.
			// see: https://bugzilla.novell.com/show_bug.cgi?id=613014
			Assert.AreEqual(MiscUtils.IsUnix ? 0x409 : InputLanguage.DefaultInputLanguage.Culture.LCID, enWs.LCID);

			IWritingSystem chWs = wsManager.Create("zh-CN");
			Assert.AreEqual("Man", chWs.Abbreviation);
			Assert.AreEqual("Mandarin Chinese", chWs.LanguageSubtag.Name);
			Assert.AreEqual("China", chWs.RegionSubtag.Name);
			Assert.AreEqual("Charis SIL", chWs.DefaultFontName);
			Assert.AreEqual(WritingSystemDefinition.SortRulesType.OtherLanguage, chWs.SortUsing);
			Assert.AreEqual("zh-CN", chWs.SortRules);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_RendererFromChrp method with a normal font.
		/// </summary>
		[Test]
		public void get_RendererFromChrp_Uniscribe()
		{
			using (GraphicsManager gm = new GraphicsManager(new Form()))
			{
				gm.Init(1.0f);
				try
				{
					var wsManager = new PalasoWritingSystemManager();
					IWritingSystem ws = wsManager.Set("en-US");
					var chrp = new LgCharRenderProps { ws = ws.Handle, szFaceName = new ushort[32] };
					MarshalEx.StringToUShort("Arial", chrp.szFaceName);
					IRenderEngine engine = wsManager.get_RendererFromChrp(gm.VwGraphics, ref chrp);
					Assert.IsNotNull(engine);
					Assert.AreSame(wsManager, engine.WritingSystemFactory);
					Assert.IsInstanceOf(typeof(UniscribeEngine), engine);
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}

		/// <summary>
		/// Tests the get_RendererFromChrp method with a Graphite font.
		/// </summary>
		[Test]
		public void get_RendererFromChrp_Graphite()
		{
			using (GraphicsManager gm = new GraphicsManager(new Form()))
			{
				gm.Init(1.0f);
				try
				{
					var wsManager = new PalasoWritingSystemManager();
					// by default Graphite is disabled
					IWritingSystem ws = wsManager.Set("en-US");
					var chrp = new LgCharRenderProps { ws = ws.Handle, szFaceName = new ushort[32] };
					MarshalEx.StringToUShort("Charis SIL", chrp.szFaceName);
					IRenderEngine engine = wsManager.get_RendererFromChrp(gm.VwGraphics, ref chrp);
					Assert.IsNotNull(engine);
					Assert.AreSame(wsManager, engine.WritingSystemFactory);
					Assert.IsInstanceOf(typeof(UniscribeEngine), engine);

					ws.IsGraphiteEnabled = true;
					engine = wsManager.get_RendererFromChrp(gm.VwGraphics, ref chrp);
					Assert.IsNotNull(engine);
					Assert.AreSame(wsManager, engine.WritingSystemFactory);
					Assert.IsInstanceOf(typeof(FwGrEngine), engine);
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}

		/// <summary>
		/// Tests the InterpretChrp method.
		/// </summary>
		[Test]
		public void InterpretChrp()
		{
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem ws = wsManager.Create("en-US");
			var chrp = new LgCharRenderProps
				{
					ws = ws.Handle,
					szFaceName = new ushort[32],
					dympHeight = 10000,
					ssv = (int) FwSuperscriptVal.kssvSuper
				};
			MarshalEx.StringToUShort("<default font>", chrp.szFaceName);
			ws.InterpretChrp(ref chrp);

			Assert.AreEqual(ws.DefaultFontName, MarshalEx.UShortToString(chrp.szFaceName));
			Assert.AreEqual(10000 / 3, chrp.dympOffset);
			Assert.AreEqual((10000 * 2) / 3, chrp.dympHeight);
			Assert.AreEqual((int) FwSuperscriptVal.kssvOff, chrp.ssv);

			chrp.ssv = (int) FwSuperscriptVal.kssvSub;
			chrp.dympHeight = 10000;
			chrp.dympOffset = 0;
			ws.InterpretChrp(ref chrp);

			Assert.AreEqual(-(10000 / 5), chrp.dympOffset);
			Assert.AreEqual((10000 * 2) / 3, chrp.dympHeight);
			Assert.AreEqual((int)FwSuperscriptVal.kssvOff, chrp.ssv);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_CharPropEngine method
		/// </summary>
		[Test]
		public void get_CharPropEngine()
		{
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem ws = wsManager.Set("zh-CN");
			ws.ValidChars = "<?xml version=\"1.0\" encoding=\"utf-16\"?>"
							+ "<ValidCharacters><WordForming>e\uFFFCf\uFFFCg\uFFFCh\uFFFC'</WordForming>"
							+ "<Numeric>4\uFFFC5</Numeric>"
							+ "<Other>,\uFFFC!\uFFFC*</Other>"
							+ "</ValidCharacters>";
			ILgCharacterPropertyEngine cpe = wsManager.get_CharPropEngine(ws.Handle);
			Assert.IsNotNull(cpe);
			Assert.IsTrue(cpe.get_IsWordForming('\''));
			Assert.IsFalse(cpe.get_IsWordForming('"'));
			Assert.AreEqual(0x0804, cpe.Locale);

			ws.ValidChars = null;
			cpe = wsManager.get_CharPropEngine(ws.Handle);
			Assert.IsNotNull(cpe);
			Assert.IsFalse(cpe.get_IsWordForming('\''));
			Assert.IsFalse(cpe.get_IsWordForming('"'));
			Assert.AreEqual(0x0804, cpe.Locale);
			wsManager.Save();
		}

		[Test]
		public void GetOrSetWorksRepeatedlyOnIdNeedingModification()
		{
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem ws;
			Assert.That(wsManager.GetOrSet("x-kal", out ws), Is.False);
			Assert.That(ws.Id, Is.EqualTo("qaa-x-kal"));
			IWritingSystem ws2;
			Assert.That(wsManager.GetOrSet("x-kal", out ws2), Is.True);
			Assert.That(ws2, Is.EqualTo(ws));

			// By the way it should work the same for one where it does not have to modify the ID.
			Assert.That(wsManager.GetOrSet("fr", out ws), Is.False);
			Assert.That(wsManager.GetOrSet("fr", out ws), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetValidLangCodeForNewLang method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetValidLangCodeForNewLang()
		{
			string storePath = PrepareTempStore("Store");
			string globalStorePath = PrepareTempStore("GlobalStore");

			EnsureDirectoryIsEmpty(storePath);
			EnsureDirectoryIsEmpty(globalStorePath);

			var globalStore = new GlobalFileWritingSystemStore(globalStorePath);
			var wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath, globalStore), globalStore);

			Assert.AreEqual("qip", wsManager.GetValidLangTagForNewLang("Qipkey"));
			Assert.AreEqual("sn", wsManager.GetValidLangTagForNewLang("Sn"));
			Assert.AreEqual("eba", wsManager.GetValidLangTagForNewLang("\u00CBbashlish")); // \u00CB == E with diacritic
			Assert.AreEqual("eee", wsManager.GetValidLangTagForNewLang("\u00CB\u00CB\u00CBlish"));
			// \u00CB == E with diacritic
			Assert.AreEqual("aaa", wsManager.GetValidLangTagForNewLang("U"));

			LanguageSubtag subtag = new LanguageSubtag("qip", "Qipkey", true, null);
			IWritingSystem newWs = wsManager.Create(subtag, null, null, null);
			wsManager.Set(newWs);
			Assert.AreEqual("aaa", wsManager.GetValidLangTagForNewLang("Qipsing"), "code for 'qip' should already be taken");

			subtag = new LanguageSubtag("aaa", "Qipsing", true, null);
			newWs = wsManager.Create(subtag, null, null, null);
			wsManager.Set(newWs);
			Assert.AreEqual("aab", wsManager.GetValidLangTagForNewLang("Qipwest"),
				"code for 'qip' should already be taken twice");

			// ENHANCE: Ideally, we would want to test incrementing the middle and first character,
			// but that would require at least 677 (26^2 + 1) writing systems be created.
		}

		[Test]
		public void CreateAudioWritingSystemScriptFirst()
		{
			string storePath = PrepareTempStore("Store");
			string globalStorePath = PrepareTempStore("GlobalStore");

			EnsureDirectoryIsEmpty(storePath);
			EnsureDirectoryIsEmpty(globalStorePath);

			var globalStore = new GlobalFileWritingSystemStore(globalStorePath);
			var wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath, globalStore), globalStore);

			IWritingSystem newWs = wsManager.Create(new LanguageSubtag("qaa", "Unknown", true, null), null, null, null);

			Assert.DoesNotThrow(()=>
			{
				newWs.ScriptSubtag = new ScriptSubtag("Zxxx", "Audio", false);
				newWs.VariantSubtag = new VariantSubtag("x-audio", "Audio", false, null);
			});
		}

		[Test]
		[Ignore("If the system changed so that this and the test above could both work we could remove a lot of complexity")]
		public void CreateAudioWritingSystemVariantFirst()
		{

			string storePath = PrepareTempStore("Store");
			string globalStorePath = PrepareTempStore("GlobalStore");

			EnsureDirectoryIsEmpty(storePath);
			EnsureDirectoryIsEmpty(globalStorePath);

			var globalStore = new GlobalFileWritingSystemStore(globalStorePath);
			var wsManager = new PalasoWritingSystemManager(
				new LocalFileWritingSystemStore(storePath, globalStore), globalStore);

			IWritingSystem newWs = wsManager.Create(new LanguageSubtag("qaa", "Unknown", true, null), null, null, null);

			Assert.DoesNotThrow(() =>
			{
				newWs.VariantSubtag = new VariantSubtag("x-audio", "Audio", false, null);
				newWs.ScriptSubtag = new ScriptSubtag("Zxxx", "Audio", false);
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that the specified directory is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void EnsureDirectoryIsEmpty(string dir)
		{
			if (Directory.Exists(dir))
			{
				foreach (string file in Directory.GetFiles(dir))
					File.Delete(file); // If we can't delete, let the exception go through so the test will fail in a useful way
			}
		}
	}
}
