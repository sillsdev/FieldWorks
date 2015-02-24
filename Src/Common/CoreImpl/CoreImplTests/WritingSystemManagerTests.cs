using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.LexiconUtils;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	[TestFixture]
	[SetCulture("en-US")]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "Unit tests, gets disposed in FixtureTearDown()")]
	public class WritingSystemManagerTests // can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private class TestSettingsStore : ISettingsStore
		{
			private XElement m_settings;

			public XElement GetSettings()
			{
				return m_settings;
			}

			public void SaveSettings(XElement settingsElem)
			{
				m_settings = settingsElem;
			}
		}

		private DebugProcs m_debugProcs;

		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			// This needs to be set for ICU
			RegistryHelper.CompanyName = "SIL";
			Icu.InitIcuDataDir();
			m_debugProcs = new DebugProcs();
		}

		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			m_debugProcs.Dispose();
			m_debugProcs = null;
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

			var projectSettingsStore = new TestSettingsStore();
			var userSettingsStore = new TestSettingsStore();
			// serialize
			var wsManager = new WritingSystemManager(new LocalFileWritingSystemStore(storePath, new ICustomDataMapper[]
			{
				new ProjectSettingsWritingSystemDataMapper(projectSettingsStore),
				new UserSettingsWritingSystemDataMapper(userSettingsStore)
			}, null));
			WritingSystem ws = wsManager.Set("en-US");
			ws.SpellCheckingID = "en_US";
			ws.MatchedPairs.Add(new MatchedPair("(", ")", true));
			ws.WindowsLcid = 0x409.ToString(CultureInfo.InvariantCulture);
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}});
			ws.LegacyMapping = "legacy mapping";
			wsManager.Save();

			// deserialize
			wsManager = new WritingSystemManager(new LocalFileWritingSystemStore(storePath, new ICustomDataMapper[]
			{
				new ProjectSettingsWritingSystemDataMapper(projectSettingsStore),
				new UserSettingsWritingSystemDataMapper(userSettingsStore)
			}, null));
			Assert.IsTrue(wsManager.Exists("en-US"));
			ws = wsManager.Get("en-US");
			Assert.AreEqual("Eng", ws.Abbreviation);
			Assert.AreEqual("English", ws.Language.Name);
			Assert.AreEqual("en_US", ws.SpellCheckingID);
			Assert.AreEqual("United States", ws.Region.Name);
			Assert.That(ws.MatchedPairs, Is.EqualTo(new[] {new MatchedPair("(", ")", true)}));
			Assert.AreEqual(0x409.ToString(CultureInfo.InvariantCulture), ws.WindowsLcid);
			Assert.That(ws.CharacterSets.Count, Is.EqualTo(1));
			Assert.That(ws.CharacterSets[0].ValueEquals(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}}), Is.True);
			Assert.AreEqual("legacy mapping", ws.LegacyMapping);
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
			var wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			var ws = wsManager.Set("en-US");
			ws.RightToLeftScript = true;
			wsManager.Save();
			Assert.IsTrue(File.Exists(Path.Combine(storePath1, "en-US.ldml")));
			Assert.IsTrue(File.Exists(Path.Combine(globalStorePath, "en-US.ldml")));

			Thread.Sleep(1000);

			DateTime lastModified = File.GetLastWriteTime(Path.Combine(globalStorePath, "en-US.ldml"));
			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			ws = wsManager.Set("en-US");
			ws.RightToLeftScript = false;
			wsManager.Save();
			Assert.Less(lastModified, File.GetLastWriteTime(Path.Combine(globalStorePath, "en-US.ldml")));

			lastModified = File.GetLastWriteTime(Path.Combine(storePath1, "en-US.ldml"));
			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			ws = wsManager.Get("en-US");
			Assert.That(ws.RightToLeftScript, Is.True);
			WritingSystem[] sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(1, sharedWss.Length);
			WritingSystem sharedWs = sharedWss[0];
			Assert.AreEqual("en-US", sharedWs.ID);
			wsManager.Replace(sharedWs);
			wsManager.Save();

			ws = wsManager.Get("en-US");
			Assert.That(ws.RightToLeftScript, Is.False);
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
			var wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			var ws = wsManager.Set("en-US");
			ws.RightToLeftScript = true;
			wsManager.Save();

			Thread.Sleep(1000);

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			ws = wsManager.Set("en-US");
			ws.RightToLeftScript = false;
			wsManager.Save();

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			WritingSystem[] sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(1, sharedWss.Length);
			Assert.AreEqual("en-US", sharedWss[0].ID);
			ws = wsManager.Get("en-US");
			Assert.That(ws.RightToLeftScript, Is.True);
			wsManager.Save();

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(0, sharedWss.Length);
			wsManager.Save();

			Thread.Sleep(1000);

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			ws = wsManager.Get("en-US");
			ws.CharacterSets.Add(new CharacterSetDefinition("main"));
			wsManager.Save();

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);
			ws = wsManager.Get("en-US");
			Assert.That(ws.CharacterSets, Is.Empty);
			sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(1, sharedWss.Length);
			WritingSystem sharedWs = sharedWss[0];
			Assert.AreEqual("en-US", sharedWs.ID);
			wsManager.Replace(sharedWs);
			wsManager.Save();
			ws = wsManager.Get("en-US");
			Assert.That(ws.CharacterSets.Count, Is.EqualTo(1));
			Assert.That(ws.CharacterSets[0].Type, Is.EqualTo("main"));
		}

		/// <summary>
		/// Tests the get_Engine method.
		/// </summary>
		[Test]
		public void get_Engine()
		{
			var wsManager = new WritingSystemManager();
			WritingSystem enWs = wsManager.Set("en-US");
			ILgWritingSystem enLgWs = wsManager.get_Engine("en-US");
			Assert.AreSame(enWs, enLgWs);

			Assert.IsFalse(wsManager.Exists("en-Latn-US"));
			// this should create a new writing system, since it doesn't exist
			ILgWritingSystem enUsLgWs = wsManager.get_Engine("en-US-fonipa");
			Assert.IsTrue(wsManager.Exists("en-US-fonipa"));
			Assert.IsTrue(wsManager.Exists(enUsLgWs.Handle));
			WritingSystem enUsWs = wsManager.Get("en-US-fonipa");
			Assert.AreSame(enUsWs, enUsLgWs);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_EngineOrNull method.
		/// </summary>
		[Test]
		public void get_EngineOrNull()
		{
			var wsManager = new WritingSystemManager();
			Assert.IsNull(wsManager.get_EngineOrNull(1));
			WritingSystem ws = wsManager.Set("en-US");
			Assert.AreSame(ws, wsManager.get_EngineOrNull(ws.Handle));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the GetWsFromStr method.
		/// </summary>
		[Test]
		public void GetWsFromStr()
		{
			var wsManager = new WritingSystemManager();
			Assert.AreEqual(0, wsManager.GetWsFromStr("en-US"));
			WritingSystem ws = wsManager.Set("en-US");
			Assert.AreEqual(ws.Handle, wsManager.GetWsFromStr("en-US"));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the GetStrFromWs method.
		/// </summary>
		[Test]
		public void GetStrFromWs()
		{
			var wsManager = new WritingSystemManager();
			Assert.IsNull(wsManager.GetStrFromWs(1));
			WritingSystem ws = wsManager.Set("en-US");
			Assert.AreEqual("en-US", wsManager.GetStrFromWs(ws.Handle));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the Create method.
		/// </summary>
		[Test]
		public void Create()
		{
			var wsManager = new WritingSystemManager();
			WritingSystem enWs = wsManager.Create("en-Latn-US-fonipa");
			Assert.AreEqual("Eng", enWs.Abbreviation);
			Assert.AreEqual("English", enWs.Language.Name);
			Assert.AreEqual("Latin", enWs.Script.Name);
			Assert.AreEqual("United States", enWs.Region.Name);
			Assert.AreEqual("International Phonetic Alphabet", enWs.Variants[0].Name);
			Assert.That(string.IsNullOrEmpty(enWs.WindowsLcid), Is.True);

			WritingSystem chWs = wsManager.Create("zh-CN");
			Assert.AreEqual("Chi", chWs.Abbreviation);
			Assert.AreEqual("Chinese", chWs.Language.Name);
			Assert.AreEqual("China", chWs.Region.Name);
			Assert.AreEqual("Charis SIL", chWs.DefaultFontName);
			Assert.That(chWs.DefaultCollation.ValueEquals(new InheritedCollationDefinition("standard") {BaseIetfLanguageTag = "zh-CN", BaseType = "standard"}), Is.True);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_RendererFromChrp method with a normal font.
		/// </summary>
		[Test]
		public void get_RendererFromChrp_Uniscribe()
		{
			using (var gm = new GraphicsManager(new Form()))
			{
				gm.Init(1.0f);
				try
				{
					var wsManager = new WritingSystemManager();
					WritingSystem ws = wsManager.Set("en-US");
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
			using (var gm = new GraphicsManager(new Form()))
			{
				gm.Init(1.0f);
				try
				{
					var wsManager = new WritingSystemManager();
					// by default Graphite is disabled
					WritingSystem ws = wsManager.Set("en-US");
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
					Assert.IsInstanceOf(typeof(GraphiteEngine), engine);
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
			var wsManager = new WritingSystemManager();
			WritingSystem ws = wsManager.Create("en-US");
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
			var wsManager = new WritingSystemManager();
			WritingSystem ws = wsManager.Set("zh-CN");
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"e", "f", "g", "h", "'"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {",", "!", "*"}});
			ILgCharacterPropertyEngine cpe = wsManager.get_CharPropEngine(ws.Handle);
			Assert.IsNotNull(cpe);
			Assert.IsTrue(cpe.get_IsWordForming('\''));
			Assert.IsFalse(cpe.get_IsWordForming('"'));
			Assert.AreEqual(0x0804, cpe.Locale);

			ws.CharacterSets.Clear();
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
			var wsManager = new WritingSystemManager();
			WritingSystem ws;
			Assert.That(wsManager.GetOrSet("x-kal", out ws), Is.False);
			Assert.That(ws.ID, Is.EqualTo("qaa-x-kal"));
			WritingSystem ws2;
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
			var wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);

			Assert.AreEqual("qip", wsManager.GetValidLangTagForNewLang("Qipkey"));
			Assert.AreEqual("sn", wsManager.GetValidLangTagForNewLang("Sn"));
			Assert.AreEqual("eba", wsManager.GetValidLangTagForNewLang("\u00CBbashlish")); // \u00CB == E with diacritic
			Assert.AreEqual("eee", wsManager.GetValidLangTagForNewLang("\u00CB\u00CB\u00CBlish"));
			// \u00CB == E with diacritic
			Assert.AreEqual("aaa", wsManager.GetValidLangTagForNewLang("U"));

			var subtag = new LanguageSubtag("qip", "Qipkey");
			WritingSystem newWs = wsManager.Create(subtag, null, null, Enumerable.Empty<VariantSubtag>());
			wsManager.Set(newWs);
			Assert.AreEqual("aaa", wsManager.GetValidLangTagForNewLang("Qipsing"), "code for 'qip' should already be taken");

			subtag = new LanguageSubtag("aaa", "Qipsing");
			newWs = wsManager.Create(subtag, null, null, Enumerable.Empty<VariantSubtag>());
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
			var wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);

			WritingSystem newWs = wsManager.Create(WellKnownSubtags.UnlistedLanguage, null, null, Enumerable.Empty<VariantSubtag>());

			Assert.DoesNotThrow(() =>
			{
				newWs.Script = WellKnownSubtags.AudioScript;
				newWs.Variants.Add(WellKnownSubtags.AudioPrivateUse);
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
			var wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath, Enumerable.Empty<ICustomDataMapper>(), globalStore), globalStore);

			WritingSystem newWs = wsManager.Create(WellKnownSubtags.UnlistedLanguage, null, null, Enumerable.Empty<VariantSubtag>());

			Assert.DoesNotThrow(() =>
			{
				newWs.Variants.Add(WellKnownSubtags.AudioPrivateUse);
				newWs.Script = WellKnownSubtags.AudioScript;
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
