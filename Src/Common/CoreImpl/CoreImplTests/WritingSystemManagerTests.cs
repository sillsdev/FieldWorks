using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Keyboarding;
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
		private DebugProcs m_debugProcs;

		private class TestWritingSystemManager : WritingSystemManager
		{
			public TestWritingSystemManager(IFwWritingSystemStore store) : base(store) {}

			public string TestUnionSettingsKeyboardsWithLocalStore()
			{
				return UnionSettingsKeyboardsWithLocalStore();
			}
		}

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

#if WS_FIX
		/// <summary>
		/// Tests serialization and deserialization of writing systems.
		/// </summary>
		[Test]
		public void SerializeDeserialize()
		{
			string storePath = PrepareTempStore("Store");

			// serialize
			var wsManager = new WritingSystemManager(new LocalFileWritingSystemStore(storePath));
			WritingSystem ws = wsManager.Set("en-US");
			ws.SpellCheckDictionary = new SpellCheckDictionaryDefinition("en-US", SpellCheckDictionaryFormat.Hunspell);
			ws.MatchedPairs.Add(new MatchedPair("(", ")", true));
			ws.WindowsLcid = 0x409.ToString(CultureInfo.InvariantCulture);
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}});
			ws.LegacyMapping = "legacy mapping";
			wsManager.Save();

			// deserialize
			wsManager = new WritingSystemManager(new LocalFileWritingSystemStore(storePath));
			Assert.IsTrue(wsManager.Exists("en-US"));
			ws = wsManager.Get("en-US");
			Assert.AreEqual("Eng", ws.Abbreviation);
			Assert.AreEqual("English", ws.Language.Name);
			Assert.AreEqual("en-US", ws.SpellCheckDictionary.LanguageTag);
			Assert.AreEqual("United States", ws.Region.Name);
			Assert.That(ws.MatchedPairs, Is.EqualTo(new[] {new MatchedPair("(", ")", true)}));
			Assert.AreEqual(0x409.ToString(CultureInfo.InvariantCulture), ws.WindowsLcid);
			Assert.That(ws.CharacterSets.Count, Is.EqualTo(1));
			Assert.That(ws.CharacterSets[0].ValueEquals(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}}), Is.True);
			Assert.AreEqual("legacy mapping", ws.LegacyMapping);
			Assert.AreEqual("eng", ws.ISO3);
			wsManager.Save();
		}

		/// <summary>
		/// Tests to make sure that the special fw extensions of ldml aren't duplicated when round tripping.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void FieldWorksExtensionsAreNotDuplicatedOnRoundTrip()
		{
			var storePath = PrepareTempStore("Store");

			// serialize
			var wsManager = new WritingSystemManager(new LocalFileWritingSystemStore(storePath));
			var ws = wsManager.Set("en-US");
			ws.SpellCheckingId = "en-US";
			ws.MatchedPairs = "matched pairs";
			ws.WindowsLcid = 0x409.ToString(CultureInfo.InvariantCulture);
			ws.ValidChars = "valid characters";
			ws.LegacyMapping = "legacy mapping";
			wsManager.Save();

			// deserialize
			wsManager = new WritingSystemManager(new LocalFileWritingSystemStore(storePath));
			Assert.IsTrue(wsManager.Exists("en-US"), "broken before SUT.");
			ws = wsManager.Get("en-US");
			Assert.AreEqual("valid characters", ws.ValidChars, "broken before SUT");
			//change the valid chars data and save back out, this was duplicating in LT-15048
			ws.ValidChars = "more valid characters";
			wsManager.Save();

			var xmlDoc = new XmlDocument();
			xmlDoc.Load(Path.Combine(storePath, "en-US.ldml"));
			Assert.AreEqual(1, xmlDoc.SelectNodes("//special/*[local-name()='validChars']").Count, "Special fw elements were duplicated");
			Assert.AreEqual(1, xmlDoc.SelectNodes("//special/*[local-name()='validChars' and @value='more valid characters']").Count, "special fw changes not saved");
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
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			var ws = wsManager.Set("en-US");
			ws.SpellCheckDictionary = new SpellCheckDictionaryDefinition("fr", SpellCheckDictionaryFormat.Hunspell);
			wsManager.Save();
			Assert.IsTrue(File.Exists(Path.Combine(storePath1, "en-US.ldml")));
			Assert.IsTrue(File.Exists(Path.Combine(globalStorePath, "en-US.ldml")));

			Thread.Sleep(1000);

			DateTime lastModified = File.GetLastWriteTime(Path.Combine(globalStorePath, "en-US.ldml"));
			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, globalStore), globalStore);
			ws = wsManager.Set("en-US");
			ws.SpellCheckDictionary = new SpellCheckDictionaryDefinition("es", SpellCheckDictionaryFormat.Hunspell);
			wsManager.Save();
			Assert.Less(lastModified, File.GetLastWriteTime(Path.Combine(globalStorePath, "en-US.ldml")));

			lastModified = File.GetLastWriteTime(Path.Combine(storePath1, "en-US.ldml"));
			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			ws = wsManager.Get("en-US");
			Assert.AreEqual("fr", ws.SpellCheckDictionary.LanguageTag);
			WritingSystem[] sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(1, sharedWss.Length);
			WritingSystem sharedWs = sharedWss[0];
			Assert.AreEqual("en-US", sharedWs.Id);
			wsManager.Replace(sharedWs);
			wsManager.Save();

			ws = wsManager.Get("en-US");
			Assert.AreEqual("es", ws.SpellCheckDictionary.LanguageTag);
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
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			var ws = wsManager.Set("en-US");
			ws.SpellCheckDictionary = new SpellCheckDictionaryDefinition("fr", SpellCheckDictionaryFormat.Hunspell);
			wsManager.Save();

			Thread.Sleep(1000);

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, globalStore), globalStore);
			ws = wsManager.Set("en-US");
			ws.SpellCheckDictionary = new SpellCheckDictionaryDefinition("es", SpellCheckDictionaryFormat.Hunspell);
			wsManager.Save();

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			WritingSystem[] sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(1, sharedWss.Length);
			Assert.AreEqual("en-US", sharedWss[0].Id);
			ws = wsManager.Get("en-US");
			Assert.AreEqual("fr", ws.SpellCheckDictionary.LanguageTag);
			wsManager.Save();

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(0, sharedWss.Length);
			wsManager.Save();

			Thread.Sleep(1000);

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath2, globalStore), globalStore);
			ws = wsManager.Get("en-US");
			ws.LegacyMapping = "encoding converter";
			wsManager.Save();

			wsManager = new WritingSystemManager(
				new LocalFileWritingSystemStore(storePath1, globalStore), globalStore);
			ws = wsManager.Get("en-US");
			Assert.IsNullOrEmpty(ws.LegacyMapping);
			sharedWss = wsManager.CheckForNewerGlobalWritingSystems().ToArray();
			Assert.AreEqual(1, sharedWss.Length);
			WritingSystem sharedWs = sharedWss[0];
			Assert.AreEqual("en-US", sharedWs.Id);
			wsManager.Replace(sharedWs);
			wsManager.Save();
			ws = wsManager.Get("en-US");
			Assert.AreEqual("encoding converter", ws.LegacyMapping);
		}
#endif

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
			ILgWritingSystem enUsLgWs = wsManager.get_Engine("en-Latn-US");
			Assert.IsTrue(wsManager.Exists("en-Latn-US"));
			Assert.IsTrue(wsManager.Exists(enUsLgWs.Handle));
			WritingSystem enUsWs = wsManager.Get("en-Latn-US");
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
			Assert.That(chWs.DefaultCollation.ValueEquals(new InheritedCollationDefinition("standard") {BaseLanguageTag = "zh-CN", BaseType = "standard"}), Is.True);
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
			Assert.That(ws.Id, Is.EqualTo("qaa-x-kal"));
			WritingSystem ws2;
			Assert.That(wsManager.GetOrSet("x-kal", out ws2), Is.True);
			Assert.That(ws2, Is.EqualTo(ws));

			// By the way it should work the same for one where it does not have to modify the ID.
			Assert.That(wsManager.GetOrSet("fr", out ws), Is.False);
			Assert.That(wsManager.GetOrSet("fr", out ws), Is.True);
		}

		[Test]
		public void LocalKeyboardsUnionLocalStore()
		{
			// Populate Settings.Default.LocalKeyboards
			Properties.Settings.Default.LocalKeyboards = "<keyboards>"
											+ "<keyboard ws=\"en\" layout=\"US\" locale=\"en-US\" />"
											+ "<keyboard ws=\"zh-CN-pinyin\" layout=\"US\" locale=\"en-US\" />"
											+ "<keyboard ws=\"zh-CN\" layout=\"Chinese (Simplified) - US Keyboard\" locale=\"zh-CN\" />"
											+ "</keyboards>";

			// Set up a local store with one conflicting and one additional keyboard
			var localStore = new LocalFileWritingSystemStore(PrepareTempStore("Store"));
			var ws = new WritingSystem
			{
				Language = new LanguageSubtag("en", "en", false, null),
				LocalKeyboard = Keyboard.Controller.CreateKeyboard("en-UK_United States-Dvorak", KeyboardFormat.Unknown, Enumerable.Empty<string>())
			};
			localStore.Set(ws);
			ws = new WritingSystem
			{
				Language = new LanguageSubtag("ko", "ko", false, null),
				LocalKeyboard = Keyboard.Controller.CreateKeyboard("ta-IN_US", KeyboardFormat.Unknown, Enumerable.Empty<string>())
			};
			localStore.Set(ws);
			var wsm = new TestWritingSystemManager(localStore);

			// SUT
			var resultXml = wsm.TestUnionSettingsKeyboardsWithLocalStore();

			// Parse resulting string into XElements
			XElement root = XElement.Parse(resultXml);
			var keyboardSettings = new Dictionary<string, XElement>();
			foreach (XElement kbd in root.Elements("keyboard"))
			{
				keyboardSettings[kbd.Attribute("ws").Value] = kbd;
			}

			Assert.AreEqual(4, keyboardSettings.Count, "Incorrect number of keyboards in Union");
			// the same
			Assert.AreEqual("US", keyboardSettings["zh-CN-pinyin"].Attribute("layout").Value, "Pinyin keyboard layout should not have changed");
			Assert.AreEqual("en-US", keyboardSettings["zh-CN-pinyin"].Attribute("locale").Value, "Pinyin keyboard locale should not have changed");
			Assert.AreEqual("Chinese (Simplified) - US Keyboard", keyboardSettings["zh-CN"].Attribute("layout").Value, "Chinese keyboard layout should not have changed");
			Assert.AreEqual("zh-CN", keyboardSettings["zh-CN"].Attribute("locale").Value, "Chinese keyboard locale should not have changed");
			// new or changed
			Assert.AreEqual("United States-Dvorak", keyboardSettings["en"].Attribute("layout").Value, "English keyboard layout should have changed");
			Assert.AreEqual("en-UK", keyboardSettings["en"].Attribute("locale").Value, "English keyboard locale should have changed");
			Assert.AreEqual("US", keyboardSettings["ko"].Attribute("layout").Value, "Korean keyboard layout should have been created");
			Assert.AreEqual("ta-IN", keyboardSettings["ko"].Attribute("locale").Value, "Korean keyboard locale should have been created");
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

			var subtag = new LanguageSubtag("qip", "Qipkey", true, null);
			WritingSystem newWs = wsManager.Create(subtag, null, null, Enumerable.Empty<VariantSubtag>());
			wsManager.Set(newWs);
			Assert.AreEqual("aaa", wsManager.GetValidLangTagForNewLang("Qipsing"), "code for 'qip' should already be taken");

			subtag = new LanguageSubtag("aaa", "Qipsing", true, null);
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

			WritingSystem newWs = wsManager.Create(WellKnownSubtags.UnlistedLanguage, null, null, null);

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
