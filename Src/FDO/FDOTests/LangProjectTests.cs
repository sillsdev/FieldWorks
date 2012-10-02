// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LangProjectTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for <see cref="SIL.FieldWorks.FDO.LangProj.LangProject"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LangProjectTests : BaseTest
	{
		private InMemoryFdoCache m_mock;
		private ILangProject m_lp;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			CheckDisposed();
			if (m_mock != null)
				m_mock.Dispose();

			m_mock = InMemoryFdoCache.CreateInMemoryFdoCache();
			m_mock.InitializeWritingSystemEncodings();
			m_mock.InitializeLangProject();
			m_lp = m_mock.Cache.LangProject;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Teardown()
		{
			CheckDisposed();
			m_mock.Dispose();
			m_mock = null;
			m_lp = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all the existing ICU language definition files so they won't interfere with
		/// the operation of these tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			// Backup any existing XML files in the Languages folder
			string langFilesPath = Path.Combine(DirectoryFinder.FWDataDirectory, "Languages");
			foreach (string langFile in Directory.GetFiles(langFilesPath, "*.xml"))
			{
				string backupFile = langFile + ".BAK";
				if (!File.Exists(backupFile))
					File.Move(langFile, backupFile);
			}
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mock != null)
					m_mock.Dispose();
				string langFilesPath = Path.Combine(DirectoryFinder.FWDataDirectory, "Languages");
				// Delete any language files created by these tests.
				foreach (string filename in Directory.GetFiles(langFilesPath, "*.xml"))
					File.Delete(filename);
				// Restore any backed up XML files in the Languages folder to their original names
				foreach (string filename in Directory.GetFiles(langFilesPath, "*.xml.BAK"))
					File.Move(filename, filename.Replace(".xml.BAK", ".xml"));
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mock = null;
			m_lp = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the (currently only) version of the <see cref="LangProject.GetAllNamedWritingSystems()"/>
		/// method that returns writing systems either in the cache or in a file list that is
		/// passed in. This avoids the necessity of making actual files to test the method,
		/// yet it only takes one line of code to generate the list of pathnames, so not much
		/// functionality is omitted from the test.
		/// Note (JohnT): I haven't entirely figured out why, but the correct running of this
		/// test seems to be somewhat dependent on previous tests. The in-memory test data
		/// we create does not have 'French' as the English name of the 'fr' writing system, so
		/// (somehow) we get it from DistFiles/Languages/fr.xml. Some sequence of tests
		/// doesn't create this file properly, and the name of 'fr' is merely set to 'fr'.
		/// Then the test fails checking the name of the french writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NamedWritingSystemsWithDefaultListOfWritingSystems_DbOnly()
		{
			CheckDisposed();
			// Request the list of all writing systems in the database.
			Set<NamedWritingSystem> set = LangProject.GetAllNamedWritingSystems(m_lp, "en", new string[0]);

			Assert.AreEqual(7, set.Count, "Wrong number of writing systems.");
			Assert.IsTrue(set.Contains(new NamedWritingSystem("English", "en")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("Spanish", "es")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("French", "fr")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("English IPA", "en-IPA")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("German", "de")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("Kalaba", "xkal")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("Urdu", "ur")));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the getting the list of writing systems with a file list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NamedWritingSystemsWithDefaultListOfWritingSystems_WithFileList()
		{
			CheckDisposed();
			string arabicXml = DirectoryFinder.FWDataDirectory + @"\languages\ar_IQ.xml";
			if (!System.IO.File.Exists(arabicXml))
			{
				ILgWritingSystemFactory qwsf = m_mock.Cache.LanguageWritingSystemFactoryAccessor;
				// This particular test needs to actually use InstallLanguage
				bool saveBypass = qwsf.BypassInstall;
				qwsf.BypassInstall = false;
				IWritingSystem qws = qwsf.get_Engine("ar_IQ");
				qws.InstallLanguage(true);
				qwsf.BypassInstall = saveBypass;
				Assert.IsTrue(System.IO.File.Exists(arabicXml), "This test is useless if we can't get the Arabic XML file to get created.");
			}
			// Request the set of all writing systems in the database. Further request
			// English again, to check for removal of duplicates, Arabic (to check for
			// a valid ws that isn't in the database), and nonsense (to check one that's not
			// in ICU is omitted).
			Set<NamedWritingSystem> set = LangProject.GetAllNamedWritingSystems(m_lp, "en", new string[]
				{DirectoryFinder.FWDataDirectory + @"\languages\en.xml",
				arabicXml,
				DirectoryFinder.FWDataDirectory + @"\languages\SillyXYZNonsense.xml"});

			Assert.AreEqual(8, set.Count);
			Assert.IsTrue(set.Contains(new NamedWritingSystem("English", "en")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("Spanish", "es")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("French", "fr")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("English IPA", "en-IPA")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("German", "de")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("Kalaba", "xkal")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("Urdu", "ur")));
			Assert.IsTrue(set.Contains(new NamedWritingSystem("Arabic (Iraq)", "ar_IQ")));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="LangProject.GetWritingSystemName"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetWritingSystemName()
		{
			CheckDisposed();
			Assert.AreEqual("Spanish", m_lp.GetWritingSystemName("es"));
		}

// This test is removed for now along with the method that it tests. It was not used except
// as part of the implementation of the no-argument method, which is now done another way.
// If reinstated, both this code and the real method need updating to reflect the new
// version of the NamedWritingSystem class.
//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Tests the version of the <see cref="LangProject.GetAllNamedWritingSystems"/>
//		/// method that uses a default list of preferred writing systems to find the best name,
//		/// namely analysis first, then vernacular (if no analysis name is found). We use
//		/// French as default analysis language and English as default vernacular.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		[Test]
//		public void NamedWritingSystemsWithDefaultListOfWritingSystems_FrenchEnglish()
//		{
//			m_mock.SetDefaultUserWritingSystem((int)InMemoryFdoCache.Hvo.Fr);
//			m_mock.SetDefaultAnalysisWritingSystem(1, (int)InMemoryFdoCache.Hvo.Fr);
//			m_mock.SetDefaultVernacularWritingSystem(1, (int)InMemoryFdoCache.Hvo.En);
//
//			// Request the list of all writing systems, getting the French name where
//			// available; otherwise English
//			LangProject lp = new LangProject(m_mock.Cache, hvoLangProj);
//			ArrayList rgws = lp.GetAllNamedWritingSystems();
//
//			Assert.AreEqual(4, rgws.Count);
//			Assert.AreEqual("English", ((NamedWritingSystem)rgws[0]).Name);
//			Assert.AreEqual("espagnol", ((NamedWritingSystem)rgws[1]).Name);
//			Assert.AreEqual("francais", ((NamedWritingSystem)rgws[2]).Name);
//			Assert.AreEqual("English IPA", ((NamedWritingSystem)rgws[3]).Name);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.En, ((NamedWritingSystem)rgws[0]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Es, ((NamedWritingSystem)rgws[1]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Fr, ((NamedWritingSystem)rgws[2]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Ipa, ((NamedWritingSystem)rgws[3]).Ws.Hvo);
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Test getting named writing systems while passing in a list of prefered writing
//		/// systems. In this test we pass in English and French.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		[Test]
//		public void NamedWritingSystems_PreferedList_EnFr()
//		{
//			ArrayList preferedWsHvos = new ArrayList(2);
//			preferedWsHvos.Add(InMemoryFdoCache.Hvo.En);
//			preferedWsHvos.Add(InMemoryFdoCache.Hvo.Fr);
//
//			LangProject lp = new LangProject(m_mock.Cache, hvoLangProj);
//			ArrayList rgws = lp.GetAllNamedWritingSystems(preferedWsHvos);
//
//			Assert.AreEqual(4, rgws.Count);
//			Assert.AreEqual("English", ((NamedWritingSystem)rgws[0]).Name);
//			Assert.AreEqual("Spanish", ((NamedWritingSystem)rgws[1]).Name);
//			Assert.AreEqual("francais", ((NamedWritingSystem)rgws[2]).Name);
//			Assert.AreEqual("English IPA", ((NamedWritingSystem)rgws[3]).Name);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.En, ((NamedWritingSystem)rgws[0]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Es, ((NamedWritingSystem)rgws[1]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Fr, ((NamedWritingSystem)rgws[2]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Ipa, ((NamedWritingSystem)rgws[3]).Ws.Hvo);
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Test getting named writing systems while passing in a list of prefered writing
//		/// systems. In this test we pass in French, IPA, Spanish and English.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		[Test]
//		public void NamedWritingSystems_PreferedList_FrIpaEsEn()
//		{
//			ArrayList preferedWsHvos = new ArrayList(4);
//			preferedWsHvos.Add(InMemoryFdoCache.Hvo.Fr);
//			preferedWsHvos.Add(InMemoryFdoCache.Hvo.Ipa);
//			preferedWsHvos.Add(InMemoryFdoCache.Hvo.Es);
//			preferedWsHvos.Add(InMemoryFdoCache.Hvo.En);
//
//			LangProject lp = new LangProject(m_mock.Cache, hvoLangProj);
//			ArrayList rgws = lp.GetAllNamedWritingSystems(preferedWsHvos);
//
//			Assert.AreEqual(4, rgws.Count);
//			Assert.AreEqual("inglés", ((NamedWritingSystem)rgws[0]).Name);
//			Assert.AreEqual("espagnol", ((NamedWritingSystem)rgws[1]).Name);
//			Assert.AreEqual("francais", ((NamedWritingSystem)rgws[2]).Name);
//			Assert.AreEqual("aipie", ((NamedWritingSystem)rgws[3]).Name);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.En, ((NamedWritingSystem)rgws[0]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Es, ((NamedWritingSystem)rgws[1]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Fr, ((NamedWritingSystem)rgws[2]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Ipa, ((NamedWritingSystem)rgws[3]).Ws.Hvo);
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Test getting named writing systems while passing in a list of prefered writing
//		/// systems. In this test we pass in only French.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		[Test]
//		public void NamedWritingSystems_PreferedList_FrOnly()
//		{
//			ArrayList preferedWsHvos = new ArrayList(1);
//			preferedWsHvos.Add(InMemoryFdoCache.Hvo.Fr);
//
//			LangProject lp = new LangProject(m_mock.Cache, hvoLangProj);
//			ArrayList rgws = lp.GetAllNamedWritingSystems(preferedWsHvos);
//
//			Assert.AreEqual(2, rgws.Count);
//			Assert.AreEqual("espagnol", ((NamedWritingSystem)rgws[0]).Name);
//			Assert.AreEqual("francais", ((NamedWritingSystem)rgws[1]).Name);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Es, ((NamedWritingSystem)rgws[0]).Ws.Hvo);
//			Assert.AreEqual((int)InMemoryFdoCache.Hvo.Fr, ((NamedWritingSystem)rgws[1]).Ws.Hvo);
//		}
	}
}
