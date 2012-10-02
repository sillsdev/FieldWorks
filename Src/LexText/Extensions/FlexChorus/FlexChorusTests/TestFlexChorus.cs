using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Data.SqlClient;
using System.Text;
using System.Xml;

using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;

using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.LexText.FlexChorus;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Ling;

namespace FlexChorusTests
{
	/// <summary>
	/// This class provides tests for various methods in the FlexChorusDlg class.
	/// </summary>
	[TestFixture]
	public class TestFlexChorus : FdoTestBase
	{
		internal const string ksguidDelete1 = "d2cf2ec8-dc28-408d-9b72-fc0329e9b9d5";
		internal const string ksguidDelete2 = "f098081b-98bc-44e0-afab-6daa713f586d";
		internal const string ksguidModify = "00ff7964-5ce4-4199-8e7c-fb15ad34b7f7";
		internal const string ksguidNewEntry = "57e4edde-8c0c-441a-bac5-cb0fab50b5a5";

		FdoCache m_cache = null;

		[SetUp]
		public void Setup()
		{
			string sBackupFile = UnzipBackup();
			RestoreDbBackup(sBackupFile, "FlexChorusTest");
			// now, create the data cache for the test project.
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("c", MiscUtils.LocalServerName);
			cacheOptions.Add("db", "FlexChorusTest");
			m_cache = FdoCache.Create(cacheOptions);
			Debug.Assert(m_cache != null);
			m_cache.EnableBulkLoadingIfPossible(true);
			InstallVirtuals(@"Language Explorer\Configuration\Main.xml",
				new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." });
		}

		/// <summary>
		/// Adding a zip file to source control is much cheaper than adding a full-sized
		/// SQL Server .bak file.  So we may need to unzip it the first time we run the
		/// test.
		/// </summary>
		/// <returns>name of the unzipped backup (.bak) file</returns>
		private string UnzipBackup()
		{
			string sCodeDir = DirectoryFinder.FWCodeDirectory;
			int ich = sCodeDir.LastIndexOf(Path.DirectorySeparatorChar);
			if (ich > 0)
				sCodeDir = sCodeDir.Substring(0, ich);
			sCodeDir = Path.Combine(sCodeDir, "Src\\LexText\\Extensions\\FlexChorus\\FlexChorusTests");
			string sBackupZipFile = Path.Combine(sCodeDir, "FlexChorusTestBak.zip");
			string sBackupFile = Path.Combine(sCodeDir, "FlexChorusTest.bak");
			if (File.Exists(sBackupFile))
				return sBackupFile;		// it's already been unzipped!
			FileStream f = new FileStream(sBackupZipFile, FileMode.Open);
			ZipInputStream zipStream = new ZipInputStream(f);
			ZipEntry zipEntry = zipStream.GetNextEntry();
			if (zipEntry != null)
			{
				string directoryName = Path.GetDirectoryName(zipEntry.Name);
				string fileName = Path.GetFileName(zipEntry.Name);
				Debug.Assert(String.IsNullOrEmpty(directoryName));
				Debug.Assert(fileName.ToLowerInvariant() == "flexchorustest.bak");
				FileInfo fi = new FileInfo(sBackupFile);
				Debug.Assert(fi != null);
				FileStream fileStreamWriter = null;
				try
				{
					fileStreamWriter = fi.Create();
					int size = 8192;
					byte[] data = new byte[8192];
					while (true)
					{
						size = zipStream.Read(data, 0, data.Length);
						if (size > 0)
							fileStreamWriter.Write(data, 0, size);
						else
							break;
					}
				}
				finally
				{
					if (fileStreamWriter != null)
						fileStreamWriter.Close();
					fileStreamWriter = null;
				}
				fi.LastWriteTime = zipEntry.DateTime;
			}
			return sBackupFile;
		}

		// The following three methods were adapted from the sources for DB.
		static public string CSqlN(string sValue)
		{
			// Need N to interpret Unicode names properly.
			return "N'" + sValue.Replace("'", "''") + "'";
		}

		static public string DbFolder
		{
			get
			{
				string sDbFolder = null;
				sDbFolder = "%ALLUSERSPROFILE%\\Application Data\\SIL\\FieldWorks\\Data\\";
				sDbFolder = Environment.ExpandEnvironmentVariables(sDbFolder);
				Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\SIL\\FieldWorks");
				if (oKey != null)
				{
					object oFolder = oKey.GetValue("DbDir");
					if (oFolder != null)
						sDbFolder = oFolder.ToString();
				}
				if (!sDbFolder.EndsWith("\\"))
					sDbFolder += "\\";
				return sDbFolder;
			}
		}

		private void RestoreDbBackup(string sBackupFilename, string sDatabaseName)
		{
			string sConnectionString =
				"Server=" + MiscUtils.LocalServerName + "; Database=master; User ID = sa;" +
				"Password=inscrutable; Pooling=false;";
			using (SqlConnection oConn = new SqlConnection(sConnectionString))
			{
				oConn.Open();
				// Under some rare situations, the restore fails if the database already exists, even with move
				// and replace. It may have something to do with duplicate logical names, although that's not
				// consistent. So to be safe, we'll delete the database first if it is present.
				string ssql = "If exists (select name from sysdatabases where name = '" + sDatabaseName + "') " +
					"begin " +
					  "drop database [" + sDatabaseName + "] " +
					"end";
				using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
				{
					oCommand.ExecuteNonQuery();
				}

				// Get the list of the logical files in the backup file and reset the path for each one.
				ssql = "restore filelistonly from disk = " + CSqlN(sBackupFilename);
				using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
				using (SqlDataReader oReader = oCommand.ExecuteReader())
				{
					ssql = "restore database [" + sDatabaseName + "] from disk = " + CSqlN(sBackupFilename) + " with replace";
					int iLogFile = 0;
					while (oReader.Read())
					{
						string sFilename = DbFolder + sDatabaseName;
						if (oReader["Type"].ToString().ToUpper() == "D")
						{
							sFilename += ".mdf";
						}
						else
						{
							string sExt = (oReader["PhysicalName"].ToString().IndexOf("_log") > -1) ? "_log.ldf" : ".ldf";
							if (iLogFile++ == 0)
								sFilename += "_log.ldf";
							else
								sFilename += iLogFile + "_log.ldf";
						}
						ssql += ", move " + CSqlN(oReader["LogicalName"].ToString()) + " to " + CSqlN(sFilename);
					}
				}

				using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
				{
					// Default is 30 which is too short when restoring a big database from SQL Server 2000
					// on slower machines. 0 is unlimited time.
					oCommand.CommandTimeout = 0;
					oCommand.ExecuteNonQuery();
				}
			}
		}

		[TearDown]
		public void Teardown()
		{
			if (m_cache != null)
			{
				m_cache.Dispose();
				m_cache = null;
			}
		}

		[Test]
		public void TestFlexChorusMethods()
		{
			int cOrigEntries = m_cache.LangProject.LexDbOA.Entries.Count();
			string sLiftFile = Path.GetTempFileName();
			m_cache.EnableBulkLoadingIfPossible(true);
			TestFlexChorusDlg dlg = new TestFlexChorusDlg(m_cache);
			TestProgress prog = new TestProgress();
			string sLiftFile2 = dlg.Export(sLiftFile, prog);
			Assert.AreEqual(sLiftFile2, sLiftFile);
			string sLiftFile3 = dlg.Merge(sLiftFile2, prog);
			Assert.IsNotNull(sLiftFile3);
			Assert.AreNotEqual(sLiftFile3, sLiftFile);
			string sLogFile = dlg.Import(sLiftFile3, prog);
			Assert.AreEqual(m_cache.LangProject.LexDbOA.Entries.Count(), cOrigEntries - 1);
			CheckDeletedEntries();
			CheckModifiedEntry();
			CheckCreatedEntry();
			Assert.IsNotNull(sLogFile);

			// Clean up the temp directory and other debris.
			File.Delete(sLiftFile);
			File.Delete(Path.ChangeExtension(sLiftFile, ".lift-ranges"));	// if it exists...
			File.Delete(sLiftFile3);
			File.Delete(sLogFile);
		}

		private void CheckDeletedEntries()
		{
			int hvo = m_cache.GetIdFromGuid(TestFlexChorus.ksguidDelete1);
			Assert.AreEqual(hvo, 0);
			hvo = m_cache.GetIdFromGuid(TestFlexChorus.ksguidDelete2);
			Assert.AreEqual(hvo, 0);
		}

		private void CheckModifiedEntry()
		{
			int hvo = m_cache.GetIdFromGuid(TestFlexChorus.ksguidModify);
			Assert.AreNotEqual(hvo, 0);
			int cSenses = m_cache.GetVectorSize(hvo, (int)LexEntry.LexEntryTags.kflidSenses);
			Assert.AreEqual(cSenses, 3);
		}

		private void CheckCreatedEntry()
		{
			int hvo = m_cache.GetIdFromGuid(TestFlexChorus.ksguidNewEntry);
			Assert.AreNotEqual(hvo, 0);
			int cSenses = m_cache.GetVectorSize(hvo, (int)LexEntry.LexEntryTags.kflidSenses);
			Assert.AreEqual(cSenses, 1);
		}

		protected override FdoCache Cache
		{
			get { return m_cache; }
		}
	}

	/// <summary>
	/// This subclasses FlexChorusDlg in order to provide access to its
	/// protected methods.
	/// </summary>
	internal class TestFlexChorusDlg : FlexChorusDlg
	{
		internal TestFlexChorusDlg(FdoCache cache)
			: base(cache)
		{
		}

		/// <summary>
		/// Call the base class's ExportLexicon method.
		/// </summary>
		internal string Export(string sLiftFile, TestProgress prog)
		{
			return base.ExportLexicon(prog, sLiftFile) as string;
		}

		/// <summary>
		/// Fake a merge operation by deleting an entry, adding an entry, and
		/// modifying an entry in the given LIFT file.
		/// </summary>
		internal string Merge(string sLiftFile, TestProgress prog)
		{
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(sLiftFile);

			XmlNode xnLift = xdoc.SelectSingleNode("lift");
			XmlNodeList xnlEntries = xnLift.SelectNodes("entry");
			Assert.AreEqual(m_cache.LangProject.LexDbOA.Entries.Count(), xnlEntries.Count);

			// Effectively delete the first entry listed by removing its contents and adding a
			// "dateDeleted" attribute.
			XmlNode xnDelete = xnlEntries[0];
			Assert.AreEqual(xnDelete.Attributes["guid"].Value.ToLowerInvariant(), TestFlexChorus.ksguidDelete1);
			XmlNodeList xnlDeleted = xnDelete.ChildNodes;
			foreach (XmlNode xn in xnlDeleted)
				xnDelete.RemoveChild(xn);
			AddXmlAttribute(xnDelete, "dateDeleted",
				DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK"));

			// Totally delete the second entry listed.
			Assert.AreEqual(xnlEntries[1].Attributes["guid"].Value.ToLowerInvariant(), TestFlexChorus.ksguidDelete2);
			xnLift.RemoveChild(xnlEntries[1]);

			// Modify an entry by deleting 3 senses and adding 1 sense.
			ModifyAnEntry(xnlEntries);

			// Add an entirely new entry (stolen from TestLangProj).
			CreateNewEntry(xnLift);

			string sNewLiftFile = Path.GetTempFileName();
			using (TextWriter w = new StreamWriter(sNewLiftFile))
			{
				w.Write(xdoc.OuterXml);
			}
			return sNewLiftFile;
		}

		private static void ModifyAnEntry(XmlNodeList xnlEntries)
		{
			XmlNode xnModify = null;
			foreach (XmlNode xn in xnlEntries)
			{
				if (xn.Attributes["guid"].Value.ToLowerInvariant() == TestFlexChorus.ksguidModify)
				{
					xnModify = xn;
					break;
				}
			}
			Assert.IsNotNull(xnModify);
			XmlNodeList xnlSenses = xnModify.SelectNodes("sense");
			Assert.AreEqual(xnlSenses.Count, 5);
			xnModify.RemoveChild(xnlSenses[4]);
			xnlSenses[3].Attributes["order"].Value = "2";
			xnModify.RemoveChild(xnlSenses[2]);
			xnModify.RemoveChild(xnlSenses[1]);
			XmlNode xnNewSense = AddXmlElement(xnModify, "sense");
			AddXmlAttribute(xnNewSense, "id", "tonal quality in music_" + Guid.NewGuid().ToString().ToLowerInvariant());
			AddXmlAttribute(xnNewSense, "order", "3");
			XmlNode xnGramInfo = AddXmlElement(xnNewSense, "grammatical-info");
			AddXmlAttribute(xnGramInfo, "value", "Noun");
			XmlNode xnDefinition = AddXmlElement(xnNewSense, "definition");
			XmlNode xnForm = AddXmlElement(xnDefinition, "form");
			AddXmlAttribute(xnForm, "lang", "en");
			XmlNode xnText = AddXmlElement(xnForm, "text");
			AddXmlText(xnText, "tonal quality in music");
			xnModify.Attributes["dateModified"].Value = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK");
		}

		private static void CreateNewEntry(XmlNode xnLift)
		{
			XmlNode xnNewEntry = AddXmlElement(xnLift, "entry");
			AddXmlAttribute(xnNewEntry, "dateCreated", "2003-10-29T21:06:25Z");
			AddXmlAttribute(xnNewEntry, "dateModified", "2007-01-17T19:20:07Z");
			AddXmlAttribute(xnNewEntry, "guid", TestFlexChorus.ksguidNewEntry);
			AddXmlAttribute(xnNewEntry, "id", "pus_" + TestFlexChorus.ksguidNewEntry);

			XmlNode xnLexUnit = AddXmlElement(xnNewEntry, "lexical-unit");
			XmlNode xnForm = AddXmlElement(xnLexUnit, "form");
			AddXmlAttribute(xnForm, "lang", "x-kal");
			XmlNode xnText = AddXmlElement(xnForm, "text");
			AddXmlText(xnText, "pus");

			XmlNode xnTrait = AddXmlElement(xnNewEntry, "trait");
			AddXmlAttribute(xnTrait, "name", "morph-type");
			AddXmlAttribute(xnTrait, "value", "root");

			XmlNode xnNewSense = AddXmlElement(xnNewEntry, "sense");
			AddXmlAttribute(xnNewSense, "id", "green_344ffa99-0484-4670-8811-983bf89ce654");
			XmlNode xnGramInfo = AddXmlElement(xnNewSense, "grammatical-info");
			AddXmlAttribute(xnGramInfo, "value", "Adjective");
			XmlNode xnGloss = AddXmlElement(xnNewSense, "gloss");
			AddXmlAttribute(xnGloss, "lang", "en");
			xnText = AddXmlElement(xnGloss, "text");
			AddXmlText(xnText, "green");
		}

		private static XmlNode AddXmlElement(XmlNode xnode, string sName)
		{
			XmlNode xnNew = xnode.OwnerDocument.CreateElement(sName);
			xnode.AppendChild(xnNew);
			return xnNew;
		}

		private static void AddXmlAttribute(XmlNode xnode, string sName, string sValue)
		{
			XmlAttribute xa = xnode.OwnerDocument.CreateAttribute(sName);
			xa.Value = sValue;
			xnode.Attributes.Append(xa);
		}

		private static void AddXmlText(XmlNode xnode, string sText)
		{
			XmlText xtext = xnode.OwnerDocument.CreateTextNode(sText);
			xnode.AppendChild(xtext);
		}

		/// <summary>
		/// Call the base class's ImportLexicon method.
		/// </summary>
		internal string Import(string sLiftFile, TestProgress prog)
		{
			return base.ImportLexicon(prog, sLiftFile) as string;
		}
	}

	/// <summary>
	/// This is almost the simplest possible implementation of IAdvInd4, suitable
	/// for testing purposes only.  It displays nothing on the screen.
	/// </summary>
	internal class TestProgress : IAdvInd4
	{
		int m_nPos = 0;
		int m_nMin = 0;
		int m_nMax = 100;
		int m_nStepSize = 1;
		string m_sTitle = null;
		string m_sMessage = null;

		#region IAdvInd4 Members

		public void GetRange(out int _nMin, out int _nMax)
		{
			_nMin = m_nMin;
			_nMax = m_nMax;
		}

		public string Message
		{
			get { return m_sMessage; }
			set { m_sMessage = value; }
		}

		public int Position
		{
			get { return m_nPos; }
			set { m_nPos = value; }
		}

		public void SetRange(int nMin, int nMax)
		{
			m_nMin = nMin;
			m_nMax = nMax;
		}

		public void Step(int nStepAmt)
		{
			m_nPos += nStepAmt * m_nStepSize;
		}

		public int StepSize
		{
			get { return m_nStepSize; }
			set { m_nStepSize = value; }
		}

		public string Title
		{
			get { return m_sTitle; }
			set { m_sTitle = value; }
		}

		#endregion
	}

}
