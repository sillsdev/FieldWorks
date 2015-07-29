using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test (currently only parts of) WritingSystemServices
	/// </summary>
	[TestFixture]
	public class WritingSystemServicesTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void UpdateWritingSystemListField_DoesNothingIfNotFound()
		{
			Cache.LangProject.AnalysisWss = "fr en qaa-x-kal";
			WritingSystemServices.UpdateWritingSystemListField(Cache, Cache.LangProject, LangProjectTags.kflidAnalysisWss, "de",
				"de-NO");
			Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("fr en qaa-x-kal"));
		}

		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void UpdateWritingSystemListField_ReplacesNonDuplicateCode()
		{
			Cache.LangProject.AnalysisWss = "fr en qaa-x-kal";
			WritingSystemServices.UpdateWritingSystemListField(Cache, Cache.LangProject, LangProjectTags.kflidAnalysisWss, "fr",
				"de-NO");
			Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("de-NO en qaa-x-kal"));
		}

		/// <summary/>
		[Test]
		public void GetMagicStringAlt_TestFirstAnaly()
		{
			IWritingSystem frWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "fr", false, false, out frWs);
			var frId = frWs.Handle;
			IWritingSystem enWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en", false, false, out enWs);
			var enId = enWs.Handle;
			IWritingSystem ptWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "pt", false, false, out ptWs);
			var ptId = ptWs.Handle;
			Cache.LangProject.CurrentAnalysisWritingSystems.Clear();
			Cache.LangProject.AddToCurrentAnalysisWritingSystems(frWs);
			Cache.LangProject.AddToCurrentAnalysisWritingSystems(enWs);
			Cache.LangProject.AnalysisWritingSystems.Add(ptWs);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Definition.set_String(frId, Cache.TsStrFactory.MakeString("fr", frId));
			sense.Definition.set_String(enId, Cache.TsStrFactory.MakeString("en", enId));
			sense.Definition.set_String(ptId, Cache.TsStrFactory.MakeString("pt", ptId));
			int wsId;
			//SUT magic gets first analysis when there is one.
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstAnal, sense.Hvo, sense.Definition.Flid, false, out wsId);
			Assert.AreEqual(wsId, frId, "Did not pull first analysis language first.");
			//SUT magic gets second analysis when the first is empty
			sense.Definition.set_String(frId, Cache.TsStrFactory.MakeString("", frId)); //wipe french
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstAnal, sense.Hvo, sense.Definition.Flid, false, out wsId);
			Assert.AreEqual(wsId, enId, "Did not pull second analysis language when first was empty.");
			//SUT magic gets non current analysis when all current analysis languages are empty
			sense.Definition.set_String(enId, Cache.TsStrFactory.MakeString("", enId)); //wipe english
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstAnal, sense.Hvo, sense.Definition.Flid, false, out wsId);
			Assert.AreEqual(wsId, ptId, "Did not pull from non current analysis language when all current languages were empty.");
		}

		/// <summary/>
		[Test]
		public void GetMagicStringAlt_TestFirstVernOrAnaly()
		{
			IWritingSystem senWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "sen", false, false, out senWs);
			var senId = senWs.Handle;
			IWritingSystem mluWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "mlu", false, false, out mluWs);
			var mluId = mluWs.Handle;
			IWritingSystem sekWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "sek", false, false, out sekWs);
			var sekId = sekWs.Handle;
			Cache.LangProject.CurrentVernacularWritingSystems.Clear();
			Cache.LangProject.AddToCurrentVernacularWritingSystems(mluWs);
			Cache.LangProject.AddToCurrentVernacularWritingSystems(senWs);
			Cache.LangProject.VernacularWritingSystems.Add(sekWs);
			IWritingSystem frWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "fr", false, false, out frWs);
			var frId = frWs.Handle;
			IWritingSystem enWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en", false, false, out enWs);
			var enId = enWs.Handle;
			IWritingSystem ptWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "pt", false, false, out ptWs);
			var ptId = ptWs.Handle;
			Cache.LangProject.CurrentAnalysisWritingSystems.Clear();
			Cache.LangProject.AddToCurrentAnalysisWritingSystems(frWs);
			Cache.LangProject.AddToCurrentAnalysisWritingSystems(enWs);
			Cache.LangProject.AnalysisWritingSystems.Add(ptWs);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Definition.set_String(frId, Cache.TsStrFactory.MakeString("fr", frId));
			sense.Definition.set_String(enId, Cache.TsStrFactory.MakeString("en", enId));
			sense.Definition.set_String(ptId, Cache.TsStrFactory.MakeString("pt", ptId));
			entry.CitationForm.set_String(mluId, Cache.TsStrFactory.MakeString("To'abaita", mluId));
			entry.CitationForm.set_String(senId, Cache.TsStrFactory.MakeString("Sena", senId));
			entry.CitationForm.set_String(sekId, Cache.TsStrFactory.MakeString("Sekani", sekId));
			int wsId;
			//SUT magic gets first analysis when there is one.
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstAnal, sense.Hvo, sense.Definition.Flid, false, out wsId);
			Assert.AreEqual(wsId, frId, "Did not pull first analysis language first.");
			//SUT magic gets second analysis when the first is empty
			sense.Definition.set_String(frId, Cache.TsStrFactory.MakeString("", frId)); //wipe french
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstAnal, sense.Hvo, sense.Definition.Flid, false, out wsId);
			Assert.AreEqual(wsId, enId, "Did not pull second analysis language when first was empty.");
			//SUT magic gets non current analysis when all current analysis languages are empty
			sense.Definition.set_String(enId, Cache.TsStrFactory.MakeString("", enId)); //wipe english
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstAnal, sense.Hvo, sense.Definition.Flid, false, out wsId);
			Assert.AreEqual(wsId, ptId, "Did not pull from non current analysis language when all current languages were empty.");
			//SUT magic gets first vernacular when there is one.
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstVern, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, mluId, "Did not pull first vernacular language first.");
			//SUT magic gets second vernacular when the first is empty
			entry.CitationForm.set_String(mluId, Cache.TsStrFactory.MakeString("", mluId)); //wipe Sena
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstVern, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, senId, "Did not pull second vernacular language when first was empty.");
			//SUT magic gets non current vernacular when all current analysis languages are empty
			entry.CitationForm.set_String(senId, Cache.TsStrFactory.MakeString("", senId)); //wipe To'abaita
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstVern, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, sekId, "Did not pull from non current vernacular language when all current languages were empty.");
		}

		/// <summary/>
		[Test]
		public void GetMagicStringAlt_TestFirstVern()
		{
			IWritingSystem mluWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "mlu", false, false, out mluWs);
			var mluId = mluWs.Handle;
			IWritingSystem senWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "sen", false, false, out senWs);
			var senId = senWs.Handle;
			IWritingSystem sekWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "sek", false, false, out sekWs);
			var sekId = sekWs.Handle;
			Cache.LangProject.CurrentVernacularWritingSystems.Clear();
			Cache.LangProject.AddToCurrentVernacularWritingSystems(mluWs);
			Cache.LangProject.AddToCurrentVernacularWritingSystems(senWs);
			Cache.LangProject.VernacularWritingSystems.Add(sekWs);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.set_String(mluId, Cache.TsStrFactory.MakeString("To'abaita", mluId));
			entry.CitationForm.set_String(senId, Cache.TsStrFactory.MakeString("Sena", senId));
			entry.CitationForm.set_String(sekId, Cache.TsStrFactory.MakeString("Sekani", sekId));
			int wsId;
			//SUT magic gets first vernacular when there is one.
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstVern, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, mluId, "Did not pull first vernacular language first.");
			//SUT magic gets second vernacular when the first is empty
			entry.CitationForm.set_String(mluId, Cache.TsStrFactory.MakeString("", mluId)); //wipe Sena
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstVern, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, senId, "Did not pull second vernacular language when first was empty.");
			//SUT magic gets non current vernacular when all current vernacular languages are empty
			entry.CitationForm.set_String(senId, Cache.TsStrFactory.MakeString("", senId)); //wipe To'abaita
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstVernOrAnal, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, sekId, "Did not pull from non current vernacular language when all current languages were empty.");
		}

		/// <summary/>
		[Test]
		public void GetMagicStringAlt_TestFirstPronunciation()
		{
			IWritingSystem mluWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "mlu", false, false, out mluWs);
			var mluId = mluWs.Handle;
			IWritingSystem senWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "sen", false, false, out senWs);
			var senId = senWs.Handle;
			Cache.LangProject.CurrentPronunciationWritingSystems.Clear();
			Cache.LangProject.CurrentPronunciationWritingSystems.Add(mluWs);
			Cache.LangProject.CurrentPronunciationWritingSystems.Add(senWs);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.set_String(mluId, Cache.TsStrFactory.MakeString("To'abaita", mluId));
			entry.CitationForm.set_String(senId, Cache.TsStrFactory.MakeString("Sena", senId));
			int wsId;
			//SUT magic gets first pronuciation when there is one.
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstPronunciation, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, mluId, "Did not pull first pronuciation language first.");
			//SUT magic gets second pronuciation when the first is empty
			entry.CitationForm.set_String(mluId, Cache.TsStrFactory.MakeString("", mluId)); //wipe Sena
			WritingSystemServices.GetMagicStringAlt(Cache, Cache.MainCacheAccessor,
																 WritingSystemServices.kwsFirstPronunciation, entry.Hvo, entry.CitationForm.Flid, false, out wsId);
			Assert.AreEqual(wsId, senId, "Did not pull second pronuciation language when first was empty.");
		}

		/// <summary>
		/// For LT-12274. x-unk should convert to qaa-x-unk.
		/// </summary>
		[Test]
		public void FindOrCreateSomeWritingSystem_Converts_x_unk_To_qaa_x_unk()
		{
			IWritingSystem ws;
			Assert.That(WritingSystemServices.FindOrCreateSomeWritingSystem(Cache, null, "x-unk", true, false, out ws), Is.False);
			Assert.That(ws.Id, Is.EqualTo("qaa-x-unk"));
		}

		/// <summary>
		/// For LT-12274. "Fr-Tech 30Oct" should convert to qaa-x-Fr-Tech30Oc.
		/// (Fr-x-Tech-30Oct or Fr-Qaaa-x-Tech-30Oct might be better, but this is last-resort handling for a code we don't really understand;
		/// main thing is the result is a valid code that is recognizably derived from the original.
		/// </summary>
		[Test]
		public void FindOrCreateSomeWritingSystem_Converts_Fr_Tech_30Oct_To_qaa_x_Fr_Tech_30Oct()
		{
			IWritingSystem ws;
			Assert.That(WritingSystemServices.FindOrCreateSomeWritingSystem(Cache, null, "Fr-Tech 30Oct", true, false, out ws), Is.False);
			Assert.That(ws.Id, Is.EqualTo("qaa-x-Fr-Tech30Oc")); //8 characters is the maximum allowed for a part.
		}

		/// <summary>
		/// Special case for a plain x.
		/// </summary>
		[Test]
		public void FindOrCreateSomeWritingSystem_Converts_x_To_qaa_x_qaa()
		{
			IWritingSystem ws;
			Assert.That(WritingSystemServices.FindOrCreateSomeWritingSystem(Cache, null, "x", true, false, out ws), Is.False);
			Assert.That(ws.Id, Is.EqualTo("qaa-x-qaa"));
		}
		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void UpdateWritingSystemListField_RemovesMergedCodeAfterMergeWith()
		{
			Cache.LangProject.AnalysisWss = "fr en fr-NO";
			WritingSystemServices.UpdateWritingSystemListField(Cache, Cache.LangProject, LangProjectTags.kflidAnalysisWss, "fr-NO",
				"fr");
			Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("fr en"));
		}

		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void UpdateWritingSystemListField_RemovesMergedCodeBeforeMergeWith()
		{
			Cache.LangProject.AnalysisWss = "fr-NO en fr";
			WritingSystemServices.UpdateWritingSystemListField(Cache, Cache.LangProject, LangProjectTags.kflidAnalysisWss, "fr-NO",
				"fr");
			Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("en fr"));
		}

		/// <summary>
		/// Test that UpdateWritingSystemTag marks things as dirty if they use a problem WS.
		/// </summary>
		[Test]
		public void UpdateWritingSystemTag_MarksObjectsAsDirty()
		{
			var entry0 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IWritingSystem newWs;
			var ws = WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-NO", true, false, out newWs);
			// A string property NOT using the WS we will change.
			entry0.ImportResidue = Cache.TsStrFactory.MakeString("hello", Cache.DefaultAnalWs);
			// A multilingual one using the WS.
			entry0.LiteralMeaning.set_String(Cache.DefaultAnalWs, Cache.TsStrFactory.MakeString("whatever", Cache.DefaultAnalWs));

			var entry1 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense1 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry1.SensesOS.Add(sense1);
			// Sense1 should be dirty: it has a gloss in the changing WS.
			sense1.Gloss.set_String(newWs.Handle, Cache.TsStrFactory.MakeString("whatever", newWs.Handle));

			// Entry2 should be dirty: it has a string property with a run in the changing WS.
			var entry2 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var bldr = Cache.TsStrFactory.MakeString("abc ", Cache.DefaultAnalWs).GetBldr();
			bldr.ReplaceTsString(bldr.Length, bldr.Length, Cache.TsStrFactory.MakeString("def", newWs.Handle));
			var stringWithNewWs = bldr.GetString();
			entry2.ImportResidue = stringWithNewWs;

			// Sense3 should be dirty: it has a multistring string property with a run in the changing WS.
			var entry3 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense3 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry3.SensesOS.Add(sense3);
			sense3.Definition.set_String(Cache.DefaultAnalWs, stringWithNewWs);

			Cache.LangProject.AnalysisWss = "en en-SU";

			m_actionHandler.EndUndoTask();
			var undoManager = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
			undoManager.Save(); // makes everything non-dirty.

			var newbies = new HashSet<ICmObjectId>();
			var dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			var goners = new HashSet<ICmObjectId>();
			Assert.That(dirtballs.Count, Is.EqualTo(0)); // After save nothing should be dirty.

			var uowServices = Cache.ServiceLocator.GetInstance<IUnitOfWorkService>();

			uowServices.GatherChanges(newbies, dirtballs, goners);

			UndoableUnitOfWorkHelper.Do("doit", "undoit", m_actionHandler,
				() => WritingSystemServices.UpdateWritingSystemId(Cache, newWs, "en-SU"));

			newbies = new HashSet<ICmObjectId>();
			dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			goners = new HashSet<ICmObjectId>();

			uowServices.GatherChanges(newbies, dirtballs, goners);

			Assert.That(dirtballs.Contains((ICmObjectOrSurrogate)sense1));
			Assert.That(!dirtballs.Contains((ICmObjectOrSurrogate)entry0)); // make sure the implementation doesn't just dirty everything.
			Assert.That(dirtballs.Contains((ICmObjectOrSurrogate)entry2));
			Assert.That(dirtballs.Contains((ICmObjectOrSurrogate)sense3));
			Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("en en-NO"), "should have updated WS lists");
		}

		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void MergeWritingSystem_ConvertsMultiStrings()
		{
			var entry1 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IWritingSystem fromWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-NO", true, false, out fromWs);
			IWritingSystem toWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-SO", true, false, out toWs);
			EnsureAnalysisWs(new [] {fromWs, toWs});
			var sense1 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry1.SensesOS.Add(sense1);
			// Sense1 should be dirty: it has a gloss in the changing WS.
			sense1.Gloss.set_String(fromWs.Handle, Cache.TsStrFactory.MakeString("whatever", fromWs.Handle));
			m_actionHandler.EndUndoTask();
			UndoableUnitOfWorkHelper.Do("doit", "undoit", m_actionHandler,
				() => WritingSystemServices.MergeWritingSystems(Cache, fromWs, toWs));
			Assert.That(sense1.Gloss.get_String(toWs.Handle).Text, Is.EqualTo("whatever"));
		}

		/// <summary>
		/// Make sure these writing systems are actually noted as current analysis writing systems.
		/// (The way we create them using FindOrCreateWritingSystem normally makes this so. But
		/// sometimes if an earlier test already created one FindOrCreate doesn't have to create,
		/// and then it no longer ensures it is in the list.)
		/// </summary>
		void EnsureAnalysisWs(IWritingSystem[] wss)
		{
			foreach (var ws in wss)
			{
				if (!Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Contains(ws))
					Cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(ws);
			}
		}

		/// <summary>
		/// A style definition that has an override for the fromWs writing system should now have one
		/// for the toWs (unless there was already one for toWs).
		/// </summary>
		[Test]
		public void MergeWritingSystem_ConvertsStyleDefinition()
		{
			IWritingSystem fromWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-NO", true, false, out fromWs);
			IWritingSystem toWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-SO", true, false, out toWs);
			EnsureAnalysisWs(new [] { fromWs, toWs });

			var style1 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style1);
			var fontOverrides = new Dictionary<int, FontInfo>();
			var fontOverride = new FontInfo();
			fontOverride.m_italic.ExplicitValue = true;
			fontOverrides[fromWs.Handle] = fontOverride;
			var bldr = TsPropsBldrClass.Create();
			BaseStyleInfo.SaveFontOverridesToBuilder(fontOverrides, bldr);
			style1.Rules = bldr.GetTextProps();
			m_actionHandler.EndUndoTask();
			UndoableUnitOfWorkHelper.Do("doit", "undoit", m_actionHandler,
				() => WritingSystemServices.MergeWritingSystems(Cache, fromWs, toWs));
			var styleInfo = new BaseStyleInfo(style1);
			var overrideInfo = styleInfo.OverrideCharacterStyleInfo(toWs.Handle);
			Assert.IsNotNull(overrideInfo);
			Assert.That(overrideInfo.Italic.Value, Is.True);
		}

		/// <summary>
		/// A style definition that has an override for the fromWs AND toWs should not change the
		/// one for toWs.
		/// </summary>
		[Test]
		public void MergeWritingSystemWithStyleDefnForToWs_DoesNotConvertStyleDefinition()
		{
			IWritingSystem fromWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-NO", true, false, out fromWs);
			IWritingSystem toWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-SO", true, false, out toWs);
			EnsureAnalysisWs(new[] { fromWs, toWs });

			var style1 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style1);
			var fontOverrides = new Dictionary<int, FontInfo>();
			var fontOverride = new FontInfo();
			fontOverride.m_italic.ExplicitValue = true;
			fontOverrides[fromWs.Handle] = fontOverride;
			fontOverride = new FontInfo();
			fontOverride.m_bold.ExplicitValue = true;
			fontOverrides[toWs.Handle] = fontOverride;
			var bldr = TsPropsBldrClass.Create();
			BaseStyleInfo.SaveFontOverridesToBuilder(fontOverrides, bldr);
			style1.Rules = bldr.GetTextProps();
			m_actionHandler.EndUndoTask();
			UndoableUnitOfWorkHelper.Do("doit", "undoit", m_actionHandler,
				() => WritingSystemServices.MergeWritingSystems(Cache, fromWs, toWs));
			var styleInfo = new BaseStyleInfo(style1);
			var overrideInfo = styleInfo.OverrideCharacterStyleInfo(toWs.Handle);
			Assert.IsNotNull(overrideInfo);
			Assert.That(overrideInfo.Bold.Value, Is.True);
			Assert.That(overrideInfo.Italic.ValueIsSet, Is.False);
		}

		/// <summary>
		/// If old objects contain LiftResidue data with a lang attribute that matches the fromWs,
		/// change it to the toWs.
		/// Enhance JohnT: possibly we need to do something about merging corresponding data if
		/// the same parent element contains an alternative in the toWs.
		/// </summary>
		[Test]
		public void MergeWritingSystem_ConvertsLiftResidue()
		{
			IWritingSystem fromWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-NO", true, false, out fromWs);
			IWritingSystem toWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "en-SO", true, false, out toWs);
			EnsureAnalysisWs(new[] { fromWs, toWs });

			var entry1 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry1.LiftResidue =
				"<lift-residue id=\"aj1_8ef13061-21ae-480f-b3a9-6b694e1ec3c4\" dateCreated=\"2005-11-09T02:57:45Z\" dateModified=\"2010-07-03T08:15:00Z\"><field type=\"Source Language\">"
				+"<form lang=\"en\"><text>Proto-Tai</text></form>"
				+"<form lang=\"en-NO\"><text>￥ﾎﾟ￥ﾧﾋ￥ﾏﾰ￨ﾯﾭ</text></form>"
				+"</field>"
				+"</lift-residue>";

			m_actionHandler.EndUndoTask();
			UndoableUnitOfWorkHelper.Do("doit", "undoit", m_actionHandler,
				() => WritingSystemServices.MergeWritingSystems(Cache, fromWs, toWs));
			Assert.That(entry1.LiftResidue.Contains("lang=\"en-SO\""));
		}


		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void CollatorSort_DoesNotThrow()
		{
			Assert.DoesNotThrow(() =>
				{
					IWritingSystem fromWs;
					WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "sen", false, true, out fromWs);
					Cache.LangProject.DefaultVernacularWritingSystem = fromWs;
					fromWs.Collator.GetSortKey("boom");
				});
		}
	}
}
