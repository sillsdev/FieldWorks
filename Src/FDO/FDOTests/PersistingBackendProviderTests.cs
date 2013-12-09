using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.IO;
using FwRemoteDatabaseConnector;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.CoreTests.PersistingLayerTests
{
	/// <summary>
	/// This class defines all the tests to be run by the
	/// persisting backend providers (BEP), embedded and client-server.
	///
	/// (Client-server only BEP tests are defined in
	/// PersistingClientServerBackendProviderTestBase, which derives
	/// from this class.)
	///
	/// The embedded BEP test classes that actually store data
	/// derive from this base class, in order to create the required BEP.
	///
	/// All tests that are common to both embedded and client-server BEPs are defined on this class,
	/// however, so all BEPs can be tested the exact same way.
	/// I can do this, because BEPs only support the four basic
	/// "CRUD" operations (Create, Read, Update, and Delete).
	///
	/// Test CmObject properties.
	/// 1. Guid,
	/// 2. Owner.
	/// 3. OwningFlid
	///
	/// Test the most basic generated properties.
	/// We test these flid types here:
	/// CellarPropertyType.Boolean: Done
	/// CellarPropertyType.Integer: Done
	/// CellarPropertyType.Time: Done
	/// CellarPropertyType.Guid: Done
	/// CellarPropertyType.GenDate: Done
	/// CellarPropertyType.Binary: Done
	/// CellarPropertyType.Unicode: Done
	/// CellarPropertyType.String: Done
	/// CellarPropertyType.MultiString: Done
	/// CellarPropertyType.MultiUnicode: Done
	/// CellarPropertyType.Numeric: (Not used in model.)
	/// CellarPropertyType.Float: (Not used in model.)
	/// CellarPropertyType.Image: (Not used in model.)
	///
	/// </summary>
	public abstract class PersistingBackendProviderTestBase : FdoTestBase
	{
		private int m_customCertifiedFlid;
		private int m_customITsStringFlid;
		private int m_customMultiUnicodeFlid;
		private int m_customAtomicReferenceFlid;
		//private int m_customReferenceSequenceFlid; // This is a 'gonna be', not a 'has been'.
		/// <summary></summary>
		protected BackendBulkLoadDomain m_loadType = BackendBulkLoadDomain.All;

		/// <summary>Name use for renaming DB tests</summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetCurrentProcess() returns a reference")]
		protected string NewProjectName
		{
			get { return "Something.Completely.Different" + Process.GetCurrentProcess().Id; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call the base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			var servLoc = Cache.ServiceLocator;
			var mdc = servLoc.GetInstance<IFwMetaDataCacheManaged>();
			if (m_internalRestart)
			{
				try
				{
					m_customCertifiedFlid = mdc.GetFieldId("WfiWordform", "Certified", false);
					m_customITsStringFlid = mdc.GetFieldId("WfiWordform", "NewTsStringProp", false);
					m_customMultiUnicodeFlid = mdc.GetFieldId("WfiWordform", "MultiUnicodeProp", false);
					m_customAtomicReferenceFlid = mdc.GetFieldId("WfiWordform", "NewAtomicRef", false);
					//m_customReferenceSequenceFlid = mdc.GetFieldId("WfiWordform", "NewRefSeq", false);
				}
				catch (FDOInvalidFieldException ex)
				{
					// ignore. We get this exception if we call RestartCache(false) because
					// we then don't persist the custom fields. If we don't ignore this
					// exception we get Lonely Tests, i.e. OnDemandLoadTest will fail if run on
					// its own.
					m_customCertifiedFlid = 0;
					m_customITsStringFlid = 0;
					m_customMultiUnicodeFlid = 0;
					m_customAtomicReferenceFlid = 0;
				}
			}
			else
			{
				m_customCertifiedFlid = mdc.AddCustomField("WfiWordform", "Certified", CellarPropertyType.Boolean, 0);
				mdc.UpdateCustomField(m_customCertifiedFlid, "myHelpId", 0, "my label");
				m_customITsStringFlid = mdc.AddCustomField("WfiWordform", "NewTsStringProp", CellarPropertyType.String, 0);
				m_customMultiUnicodeFlid = mdc.AddCustomField("WfiWordform", "MultiUnicodeProp", CellarPropertyType.MultiUnicode, 0);
				m_customAtomicReferenceFlid = mdc.AddCustomField("WfiWordform", "NewAtomicRef", CellarPropertyType.ReferenceAtomic, CmPersonTags.kClassId);
				//m_customReferenceSequenceFlid = mdc.AddCustomField("WfiWordform", "NewRefSeq", CellarPropertyType.ReferenceSequence, CmPersonTags.kClassId);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call the base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			m_customCertifiedFlid = 0;
			m_customITsStringFlid = 0;
			m_customMultiUnicodeFlid = 0;
			m_customAtomicReferenceFlid = 0;
			//m_customReferenceSequenceFlid = 0;

			base.FixtureTeardown();
		}

		/// <summary>
		/// Make sure the basic data types are right.
		/// </summary>
		[Test]
		public void BasicDataTypes()
		{
			Cache.DomainDataByFlid.BeginNonUndoableTask();

			var lp = Cache.LanguageProject;

			// 'Remember' some guids.
			// Atomic property.
			var lpGuid = lp.Guid;
			// Owning collection property.
			var aaGuids = new HashSet<Guid>();
			foreach (var aa in lp.AnalyzingAgentsOC)
				aaGuids.Add(aa.Guid);
			// Owning sequence property.
			var pssGuid = lp.TranslationTagsOA.PossibilitiesOS[0].Guid;

			// CellarPropertyType.Boolean:
			lp.TranslationTagsOA.IsClosed = true;
			var isClosed = lp.TranslationTagsOA.IsClosed;
			var isSorted = lp.TranslationTagsOA.IsSorted;
			// CellarPropertyType.Integer:
			lp.TranslationTagsOA.Depth = 5;
			var depth = lp.TranslationTagsOA.Depth;
			// CellarPropertyType.Time
			var dateCreated = lp.DateCreated;
			// CellarPropertyType.Guid
			ICmPossibilityList possList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LanguageProject.TimeOfDayOA = possList;
			possList.ListVersion = Guid.NewGuid();
			var versionGuid = possList.ListVersion;
			var possGuid = possList.Guid;
			// CellarPropertyType.GenDate
			lp.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var eve = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			lp.PeopleOA.PossibilitiesOS.Add(eve);
			eve.DateOfBirth = new GenDate(GenDate.PrecisionType.Before, 1, 1, 3000, true);
			var eveDOB = eve.DateOfBirth;
			// CellarPropertyType.Binary:
			IUserConfigAcct acct = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>().Create();
			Cache.LanguageProject.UserAccountsOC.Add(acct);
			var acctGuid = acct.Guid;
			var byteArrayValue = new byte[] { 1, 2, 3 };
			acct.Sid = byteArrayValue;
			// CellarPropertyType.Unicode:
			const string newEthCode = "ZPI";
			lp.EthnologueCode = newEthCode;
			// CellarPropertyType.String:
			var le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var irOriginalValue = Cache.TsStrFactory.MakeString("<import & residue>",
																Cache.WritingSystemFactory.UserWs);
			le.ImportResidue = irOriginalValue;
			var xmlOriginalValue = TsStringUtils.GetXmlRep(irOriginalValue, Cache.WritingSystemFactory, Cache.WritingSystemFactory.UserWs, true);
			var irOriginalBlankValue = Cache.TsStrFactory.MakeString("    ",
																Cache.WritingSystemFactory.UserWs);
			le.Comment.set_String(Cache.WritingSystemFactory.UserWs, irOriginalBlankValue);
			var xmlOriginalBlankValue = TsStringUtils.GetXmlRep(irOriginalBlankValue, Cache.WritingSystemFactory, Cache.WritingSystemFactory.UserWs, true);
			// ITsTextProps
			var userWs = Cache.WritingSystemFactory.UserWs;
			var bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Arial");
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
			var style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			lp.StylesOC.Add(style);
			const string styleName = "Name With <this> & <that>";
			style.Name = styleName;
			style.Rules = bldr.GetTextProps();
			var rOriginalvalue = style.Rules;
			string xmlOriginalValueRules = TsStringUtils.GetXmlRep(rOriginalvalue, Cache.WritingSystemFactory);

			// Use same test, since the multiString and MultiUnicode props
			// all use the same mechanism (on MultiAccessor) to read/write
			// the data.
			// CellarPropertyType.MultiString:
			// CellarPropertyType.MultiUnicode:
			var tsf = Cache.TsStrFactory;
			var englishWsHvo = Cache.WritingSystemFactory.GetWsFromStr("en");
			var nameEnValue = tsf.MakeString("Stateful FDO Test Project", englishWsHvo);
			TsStringUtils.GetXmlRep(nameEnValue, Cache.WritingSystemFactory, 0, true);
			// Set LP's Name.
			//lp.Name.set_String(
			//    englishWsHvo,
			//    nameEnValue);
			//var nameEsValue = tsf.MakeString("Proyecto de prueba: FDO", spanishWsHvo);
			//streamWrapper = TsStreamWrapperClass.Create();
			//streamWrapper.WriteTssAsXml(nameEsValue,
			//     Cache.WritingSystemFactory,
			//     0,
			//     spanishWsHvo, true);
			//var esXML = streamWrapper.Contents;
			//lp.Name.set_String(
			//    spanishWsHvo,
			//    nameEsValue);

			// Check that ScriptureReferenceSystem is persisted & reloaded.
			var srs = Cache.ServiceLocator.GetInstance<IScrRefSystemFactory>().Create();

			// Restart BEP.
			RestartCache(true);
			lp = Cache.LanguageProject;

			// Atomic property.
			var lpGuidRestored = lp.Guid;
			Assert.IsTrue(lpGuid == lpGuidRestored, "Object Guid not saved/restored properly.");

			// Owning collection property.
			Assert.AreEqual(aaGuids.Count, lp.AnalyzingAgentsOC.Count, "Wrong number of analyzing agents.");
			foreach (var aa in lp.AnalyzingAgentsOC)
				Assert.IsTrue(aaGuids.Contains(aa.Guid), "Analyzing Agent not found.");

			// Owning sequence property.
			Assert.IsTrue(lp.TranslationTagsOA.PossibilitiesOS[0].Guid == pssGuid, "Translation type Guid not the same.");

			// Check bool.
			Assert.IsTrue(lp.TranslationTagsOA.IsClosed, "Wrong boolean value restored (for true).");
			Assert.AreEqual(isSorted, lp.TranslationTagsOA.IsSorted, "Wrong boolean value restored (for false).");
			// Check int.
			Assert.AreEqual(depth, lp.TranslationTagsOA.Depth, "Wrong integer value restored.");
			// Check time.
			Assert.AreEqual(dateCreated.Year, lp.DateCreated.Year, "Wrong year part of time value restored.");
			Assert.AreEqual(dateCreated.Month, lp.DateCreated.Month, "Wrong month part of time value restored.");
			Assert.AreEqual(dateCreated.Day, lp.DateCreated.Day, "Wrong day part of time value restored.");
			Assert.AreEqual(dateCreated.Hour, lp.DateCreated.Hour, "Wrong hour part of time value restored.");
			Assert.AreEqual(dateCreated.Minute, lp.DateCreated.Minute, "Wrong minute part of time value restored.");
			Assert.AreEqual(dateCreated.Second, lp.DateCreated.Second, "Wrong second part of time value restored.");
			Assert.AreEqual(dateCreated.Millisecond, lp.DateCreated.Millisecond, "Wrong millisecond time value restored.");
			// Check Guid.
			possList = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(possGuid);
			Assert.AreEqual(versionGuid, possList.ListVersion, "Wrong Guid value restored.");
			// Check GenDate
			eve = (ICmPerson)lp.PeopleOA.PossibilitiesOS[0];
			Assert.AreEqual(eveDOB, eve.DateOfBirth, "Wrong DOB value restored.");
			// Check Binary.
			acct = Cache.ServiceLocator.GetInstance<IUserConfigAcctRepository>().GetObject(acctGuid);
			Assert.AreEqual(byteArrayValue, acct.Sid, "Wrong Binary value restored.");
			// Check Unicode
			Assert.AreEqual(newEthCode, lp.EthnologueCode, "Wrong Unicode value restored.");
			// Check string (ITsString)
			var irRestoredValue = lp.LexDbOA.Entries.ToArray()[0].ImportResidue;
			var xmlRestoredValue = TsStringUtils.GetXmlRep(irRestoredValue, Cache.WritingSystemFactory, Cache.WritingSystemFactory.UserWs, true);
			Assert.AreEqual(xmlOriginalValue, xmlRestoredValue, "Wrong ITsString value restored.");
			var irRestoredBlankValue = lp.LexDbOA.Entries.ToArray()[0].Comment.get_String(Cache.WritingSystemFactory.UserWs);
			var xmlRestoredBlankValue = TsStringUtils.GetXmlRep(irRestoredBlankValue, Cache.WritingSystemFactory, Cache.WritingSystemFactory.UserWs, true);
			Assert.AreEqual(xmlOriginalBlankValue, xmlRestoredBlankValue, "Wrong ITsString value restored for blank string.");
			// ITsTextProps
			var rRestoredValue = lp.StylesOC.ToArray()[0].Rules;
			var xmlRestoredValueRules = TsStringUtils.GetXmlRep(rRestoredValue, Cache.WritingSystemFactory);
			Assert.AreEqual(xmlOriginalValueRules, xmlRestoredValueRules, "Wrong ITsTextProps value restored.");

			var styleRestoredName = lp.StylesOC.ToArray()[0].Name;
			Assert.AreEqual(styleName, styleRestoredName, "Wrong style name restored.");

			//englishWsHvo = Cache.WritingSystemFactory.GetWsFromStr("en");
			//irRestoredValue = lp.Name.get_String(englishWsHvo);
			//streamWrapper = TsStreamWrapperClass.Create();
			//streamWrapper.WriteTssAsXml(irRestoredValue,
			//     Cache.WritingSystemFactory,
			//     0,
			//     englishWsHvo, true);
			//Assert.AreEqual(enXML, streamWrapper.Contents,
			//    "Wrong ITsString value (Name) restored.");
			//spanishWsHvo = Cache.WritingSystemFactory.GetWsFromStr("es");
			//irRestoredValue = lp.Name.get_String(spanishWsHvo);
			//streamWrapper = TsStreamWrapperClass.Create();
			//streamWrapper.WriteTssAsXml(irRestoredValue,
			//     Cache.WritingSystemFactory,
			//     0,
			//     spanishWsHvo, true);
			//Assert.AreEqual(esXML, streamWrapper.Contents,
			//    "Wrong ITsString value (Name) restored.");

			Assert.AreEqual(srs.Guid, Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().Singleton.Guid, "Wrong ScrRefSystem Guid.");
		}

		/// <summary>
		/// Make sure BackendProvider does an 'on demand' load.
		/// </summary>
		[Test]
		public void OnDemandLoadTest()
		{
			var originalLoadType = m_loadType;
			try
			{
				m_loadType = BackendBulkLoadDomain.None;
				RestartCache(false);

				var lp = Cache.LanguageProject;
				var lexDb = lp.LexDbOA;
				Assert.IsNotNull(lexDb, "Null Lex DB.");
			}
			finally
			{
				m_loadType = originalLoadType;
				RestartCache(false);
			}
		}

		/// <summary>
		/// Add new custom field, persist it, and reload it.
		/// </summary>
		[Test]
		public void CustomFieldTest()
		{
			// No changes, but a task is needed for the restart to work.
			Cache.DomainDataByFlid.BeginNonUndoableTask();

			// Restart BEP to force a commit.
			RestartCache(true);

			// Make sure the custom fields were reloaded,
			// which means they had to also have been saved.
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var flid = mdc.GetFieldId("WfiWordform", "Certified", false);
			Assert.IsTrue(mdc.IsCustom(flid));
			Assert.That(mdc.GetFieldLabel(flid), Is.EqualTo("my label"));
			flid = mdc.GetFieldId("WfiWordform", "NewAtomicRef", false);
			Assert.IsTrue(mdc.IsCustom(flid));
		}

		/// <summary>
		/// Add new custom field data, persist it, and reload it.
		/// </summary>
		[Test]
		public void CustomFieldDataTest()
		{
			var servLoc = Cache.ServiceLocator;
			var sda = servLoc.GetInstance<ISilDataAccessManaged>();
			var lp = Cache.LanguageProject;

			sda.BeginNonUndoableTask();
			var wf = servLoc.GetInstance<IWfiWordformFactory>().Create();
			var wfGuid = wf.Guid;
			// Set custom boolean property.
			sda.SetBoolean(wf.Hvo, m_customCertifiedFlid, true);
			// Set custom ITsString property.
			var tsf = Cache.TsStrFactory;
			var userWs = Cache.WritingSystemFactory.UserWs;
			var newStringValue = tsf.MakeString("New ITsString", userWs);
			sda.SetString(wf.Hvo, m_customITsStringFlid, newStringValue);
			// Set custom MultiUnicode property.
			var newUnicodeTsStringValue = tsf.MakeString("New unicode ITsString", userWs);
			sda.SetMultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs, newUnicodeTsStringValue);
			// Set atomic reference custom property.
			var possListFactory = servLoc.GetInstance<ICmPossibilityListFactory>();
			lp.PeopleOA = possListFactory.Create();
			var personFactory = servLoc.GetInstance<ICmPersonFactory>();
			var person = personFactory.Create();
			lp.PeopleOA.PossibilitiesOS.Add(person);
			var personGuid = person.Guid;
			sda.SetObjProp(wf.Hvo, m_customAtomicReferenceFlid, person.Hvo);

			// Restart BEP.
			RestartCache(true);

			servLoc = Cache.ServiceLocator;
			userWs = Cache.WritingSystemFactory.UserWs;
			sda = servLoc.GetInstance<ISilDataAccessManaged>();
			wf = servLoc.GetInstance<IWfiWordformRepository>().GetObject(wfGuid);
			Assert.IsTrue(sda.get_BooleanProp(wf.Hvo, m_customCertifiedFlid), "Custom prop is not 'true'.");
			var tss = sda.get_StringProp(wf.Hvo, m_customITsStringFlid);
			Assert.AreEqual(tss.Text, newStringValue.Text, "Wrong TsString in custom property.");
			tss = sda.get_MultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs);
			Assert.AreEqual(tss.Text, newUnicodeTsStringValue.Text, "MultiUnicode custom property is not newUnicodeTsStringValue.");
			person = servLoc.GetInstance<ICmPersonRepository>().GetObject(personGuid);
			Assert.AreEqual(person.Hvo, sda.get_ObjectProp(wf.Hvo, m_customAtomicReferenceFlid), "Wrong atomic ref custom value.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rename the database to something else, and back again.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RenameDatabaseTest()
		{
			string sOrigName = Cache.ProjectId.Name;
			string newProjectDir = Path.Combine(DirectoryFinder.ProjectsDirectory, NewProjectName);
			if (Cache.ProjectId.Type != FDOBackendProviderType.kMemoryOnly && Directory.Exists(newProjectDir))
			{
				// make sure database doesn't exist before running the test
				Directory.Delete(newProjectDir, true);
			}
			try
			{
				Assert.IsTrue(Cache.RenameDatabase(NewProjectName),
					"Renaming database file(s) to something completely different failed");
				Assert.AreEqual(NewProjectName, Cache.ProjectId.Name,
					"Renaming database file(s) changed the DatabaseName property incorrectly");
				CheckAdditionalStuffAfterFirstRename();
				Assert.IsTrue(Cache.RenameDatabase(sOrigName),
					"Renaming database file(s) back to the original name(s) failed");
				Assert.AreEqual(sOrigName, Cache.ProjectId.Name,
					"Renaming database file(s) back changed the DatabaseName property incorrectly");
			}
			finally
			{
				if (Cache.ProjectId.Type != FDOBackendProviderType.kMemoryOnly && Directory.Exists(newProjectDir))
				{
					// make sure new database gets deleted even when test fails
					try
					{
						Directory.Delete(newProjectDir, true);
					}
					catch (IOException)
					{
						// Ignore any errors trying to clean up. We don't want to fail the test
						// for that
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows for individual BEPs to check additional stuff after first rename for the
		/// <see cref="RenameDatabaseTest"/> test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CheckAdditionalStuffAfterFirstRename()
		{
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for testing the FdoCache with the FDOBackendProviderType.kMemoryOnly
	/// backend provider. Technically, this one need not be tested here, since it doesn't
	/// actually persist anything, but in order to have the test stable be complete for
	/// all BEPs, I run them anyway.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class MemoryOnlyTests : PersistingBackendProviderTestBase
	{
		/// <summary>
		/// Override to create and load a very basic cache.
		/// </summary>
		/// <returns>An FdoCache that has only the basic data in it.</returns>
		protected override FdoCache CreateCache()
		{
			return BootstrapSystem(new TestProjectId(FDOBackendProviderType.kMemoryOnly, "MemoryOnly.mem"), m_loadType);
		}

		/// <summary>
		/// Override to do nothing.
		/// </summary>
		/// <param name="doCommit">'True' to end the task and commit the changes. 'False' to skip the commit.</param>
		protected override void RestartCache(bool doCommit)
		{
			if (doCommit)
				Cache.DomainDataByFlid.EndNonUndoableTask();
		}
	}


	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for testing the FdoCache with the FDOBackendProviderType.kDb4oClientServer
	/// backend provider.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class Db4oCSTests : PersistingBackendProviderTestBase
	{
		private String _projectDir;
		private String _randomProjectName;
		/// <summary>
		/// Override to create and load a very basic cache.
		/// </summary>
		/// <returns>An FdoCache that has only the basic data in it.</returns>
		protected override FdoCache CreateCache()
		{
			if (String.IsNullOrEmpty(_randomProjectName))
			{
				//Create a unique folder and project name for this run of the tests. This is done in case
				//the build machine is running multiple builds of Flex in parallel. We don't want the multiple
				//parallel renditions to clobber each other's text folders
				//This project/folder is used for all the texts in this class. Then deleted after they are all run.
				while (true)
				{
					_randomProjectName = "TestLangProjCS" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
					var projectDir = Path.Combine(DirectoryFinder.ProjectsDirectory, _randomProjectName);
					if (!Directory.Exists(projectDir))
					{
						_projectDir = projectDir;
						break;
					}
				}
			}
			return BootstrapSystem(new TestProjectId(FDOBackendProviderType.kDb4oClientServer, _randomProjectName), m_loadType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call the base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			RemotingServer.Start();
			base.FixtureSetup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call the base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			//Only delete the folder when we are really done with it. This method is called by RestartCache.
			if (!m_internalRestart && Directory.Exists(_projectDir))
				Directory.Delete(_projectDir, true);
			RemotingServer.Stop();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check path after first rename for the RenameDatabaseTest test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CheckAdditionalStuffAfterFirstRename()
		{
			Assert.AreEqual(Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, NewProjectName),
				FdoFileHelper.GetDb4oDataFileName(NewProjectName)), Cache.ProjectId.Path);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for testing the FdoCache with the FDOBackendProviderType.kXML
	/// backend provider.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class XMLTests : PersistingBackendProviderTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to create and load a very basic cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache CreateCache()
		{
			const string projName = "TestLangProj-test";
			string filename = Path.Combine(DirectoryFinder.ProjectsDirectory,
				Path.Combine(projName, FdoFileHelper.GetXmlDataFileName(projName)));
			if (!m_internalRestart)
			{
				if (File.Exists(filename))
					File.Delete(filename);
			}

			return BootstrapSystem(new TestProjectId(FDOBackendProviderType.kXMLWithMemoryOnlyWsMgr, filename), m_loadType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test verifies that an XML file can only be opened by one cache at a time.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(IOException))]
		[Ignore("There is a timing problem with this test - project file may not exist becuase background thread is doing commit.")]
		public void OnlyOneCacheAllowed()
		{
			string filename = FdoFileHelper.GetXmlDataFileName("TestLangProj");
			Assert.True(File.Exists(filename), "Test XML file not found");
			using (FdoCache cache = OpenExistingFile(filename))
				Assert.Fail("Able to open XML file that is already open");
		}

		private FdoCache OpenExistingFile(string filename)
		{
			return FdoCache.CreateCacheFromExistingData(
				new TestProjectId(FDOBackendProviderType.kXMLWithMemoryOnlyWsMgr, filename), "en", new DummyProgressDlg(), new DummyFdoUI());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check full path after first rename for the RenameDatabaseTest test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CheckAdditionalStuffAfterFirstRename()
		{
			Assert.AreEqual(Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, NewProjectName),
				FdoFileHelper.GetXmlDataFileName(NewProjectName)), Cache.ProjectId.Path);
		}

		/// <summary>
		/// Tests that a fwdata file that didn't get finished (due to a power outage?) will
		/// throw an Exception.
		/// </summary>
		[Test]
		[ExpectedException(typeof(StartupException))]
		public void CorruptedXMLFileTest()
		{
			var testDataPath = Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/TestData");
			var projName = Path.Combine(testDataPath, "CorruptedXMLFileTest.fwdata");

			// MockXMLBackendProvider implements IDisposable therefore we need the "using".
			// Otherwise the build agent might complain, and in the worst case the test might hang.
			using (var xmlBep = new MockXMLBackendProvider(Cache, projName))
			{
				// Should throw an XMLException, but this will be caught and because there's
				// no .bak file, an StartupException will be thrown instead.
				xmlBep.Startup();
			}
		}

		/// <summary>
		/// Tests that the right thing happens when StartupExtantLanguageProject() throws
		/// an UnauthorizedAccessException in Linux.
		/// In "real life" this happens if the user forgets to logout and back in
		/// after the initial install of FieldWorks.
		/// </summary>
		[Test]
		[Platform(Include = "Linux", Reason = "Linux specific")]
		[ExpectedException(typeof(UnauthorizedAccessException))]
		public void StartupExtantTest()
		{
			string testFileName = String.Empty;
			try
			{
				var testDataPath = Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/BackupRestore/BackupTestProject");
				var projName = Path.Combine(testDataPath, "BackupTestProject.fwdata");
				testFileName = Path.GetTempFileName();
				// If we leave the extension as .tmp, we get a sharing violation when the
				// MockXMLBackendProvider ctor tries to write to the same name with .tmp extension
				testFileName = Path.ChangeExtension(testFileName, "fwdata");
				File.Copy(projName, testFileName, true);

				// MockXMLBackendProvider implements IDisposable therefore we need the "using".
				// Otherwise the build agent might complain, and in the worst case the test might hang.
				using (var xmlBep = new MockXMLBackendProvider(Cache, testFileName))
				{
					// Should throw an UnauthorizedAccessException.
					xmlBep.StartupExtant();
				}
			}
			finally
			{
				File.Delete(testFileName);
			}
		}

		/// <summary>
		/// Tests that a fwdata file that was edited and has an extra newline at the end will not
		/// throw an Exception.
		/// </summary>
		[Test]
		public void SlightlyCorruptedXMLFileTest()
		{
			var testDataPath = Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/TestData");
			var projName = Path.Combine(testDataPath, "SlightlyCorruptedXMLFile.fwdata");

			// MockXMLBackendProvider implements IDisposable therefore we need the "using".
			// Otherwise the build agent might complain, and in the worst case the test might hang.
			using (var xmlBep = new MockXMLBackendProvider(Cache, projName))
			{
				// Should not throw an XMLException. The code that detects a corrupt file shouldn't
				// care about an extra character or two at the end of the file after the last tag.
				xmlBep.Startup();
			}
		}

		/// <summary>
		/// Tests that a fwdata file with different rt element that have duplicate guids will report this error.
		/// </summary>
		[Test]
		public void XMLFileWithDuplicateGuidsTest()
		{
			var testDataPath = Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/TestData");
			var projName = Path.Combine(testDataPath, "DuplicateGuids.fwdata");

			// MockXMLBackendProvider implements IDisposable therefore we need the "using".
			// Otherwise the build agent might complain, and in the worst case the test might hang.
			using (var xmlBep = new MockXMLBackendProvider(Cache, projName))
			{
				// Should not throw an XMLException. The code that detects a corrupt file shouldn't
				// care about an extra character or two at the end of the file after the last tag.
				xmlBep.Startup();
				Assert.That(xmlBep.ListOfDuplicateGuids.Count, Is.GreaterThan(0), "The loading of this test project should result in finding duplicate guids.");

				Assert.That(xmlBep.ListOfDuplicateGuids.Contains("cc60cb18-5067-442b-a740-d3b913b2610a, classname: PartOfSpeech"), "The guid cc60cb18-5067-442b-a740-d3b913b2610a should have been found as a duplicate.");
				Assert.That(xmlBep.ListOfDuplicateGuids.Contains("30d07580-5052-4d91-bc24-469b8b2d7df9, classname: PartOfSpeech"), "The guid 30d07580-5052-4d91-bc24-469b8b2d7df9 should have been found as a duplicate.");
				Assert.That(xmlBep.ListOfDuplicateGuids.Contains("6df1c8ee-5530-4180-99e8-be2afab0f60d, classname: PartOfSpeech"), "The guid 6df1c8ee-5530-4180-99e8-be2afab0f60d should have been found as a duplicate.");
				Assert.That(xmlBep.ListOfDuplicateGuids.Contains("a4a759b4-5a10-4d7a-80a3-accf5bd840b1, classname: PartOfSpeech"), "The guid a4a759b4-5a10-4d7a-80a3-accf5bd840b1 should have been found as a duplicate.");
				Assert.That(xmlBep.ListOfDuplicateGuids.Contains("a5311f3b-ff98-47d2-8ece-b1aca03a8bbd, classname: PartOfSpeech"), "The guid a5311f3b-ff98-47d2-8ece-b1aca03a8bbd should have been found as a duplicate.");
				Assert.That(xmlBep.ListOfDuplicateGuids.Contains("f1ac9eab-5f8c-41cf-b234-e53405aaaac5, classname: PartOfSpeech"), "The guid f1ac9eab-5f8c-41cf-b234-e53405aaaac5 should have been found as a duplicate.");
			}
		}
	}

	/// <summary>
	/// Minimal XMLBackendProvider for testing the loading of an xml file.
	/// </summary>
	internal class MockXMLBackendProvider : XMLBackendProvider
	{

		public string Project { get; set; }
		public FdoCache Cache { get; private set; }

		/// <summary>
		/// Create a minimal XMLBackendProvider for testing the loading of an xml file.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="projName"></param>
		public MockXMLBackendProvider(FdoCache cache, string projName):
			base(cache, new IdentityMap((IFwMetaDataCacheManaged)cache.MetaDataCache),
			new CmObjectSurrogateFactory(cache), (IFwMetaDataCacheManagedInternal)cache.MetaDataCache,
			new FdoDataMigrationManager(), new DummyFdoUI())
		{
			Project = projName;
			Cache = cache;
		}

		/// <summary/>
		public void Startup()
		{
			ProjectId = new TestProjectId(FDOBackendProviderType.kXML, Project);
			StartupInternal(ModelVersion);
		}

		/// <summary />
		public void StartupExtant()
		{
			ProjectId = new TestProjectId(FDOBackendProviderType.kXML, Project);
			// This will throw an UnauthorizedAccessException because of the
			// StartupInternalWithDataMigrationIfNeeded() override below
			StartupExtantLanguageProject(ProjectId, false, new DummyProgressDlg());
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_identityMap != null)
					m_identityMap.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// We don't want to display any dialogs when running tests.
		/// </summary>
		internal override void ReportDuplicateGuidsIfTheyExist()
		{
		}

		protected override void StartupInternalWithDataMigrationIfNeeded(IThreadedProgress progressDlg)
		{
			throw new UnauthorizedAccessException();
		}
	}
}
