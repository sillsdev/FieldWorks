// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrImportSetTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

using NUnit.Framework;
using NMock;
using NMock.Constraints;

using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Common.COMInterfaces;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region MappingInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MappingInfo
	{
		/// <summary></summary>
		public string beginMarker;
		/// <summary></summary>
		public string endMarker;
		/// <summary></summary>
		public bool isInline;
		/// <summary></summary>
		public string dataEncoding;
		/// <summary></summary>
		public MarkerDomain domain;
		/// <summary></summary>
		public string styleName;
		/// <summary></summary>
		public string ws;
		/// <summary></summary>
		public bool isExcluded;
		/// <summary></summary>
		public MappingTargetType mappingTarget;

		/// ----------------------------------------------------------------------------------------
		/// <summary>Constructor</summary>
		/// ----------------------------------------------------------------------------------------
		public MappingInfo(string beginMarker, string endMarker, bool isInline,
			string dataEncoding, MarkerDomain domain,string styleName, string ws,
			bool isExcluded, MappingTargetType mappingTarget)
		{
			this.beginMarker = beginMarker;
			this.endMarker = endMarker;
			this.isInline = isInline;
			this.dataEncoding = dataEncoding;
			this.domain = domain;
			this.styleName = styleName;
			this.ws = ws;
			this.isExcluded = isExcluded;
			this.mappingTarget = mappingTarget;
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ScrImportSet class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrImportSetTests : ScrInMemoryFdoTestBase
	{
		#region data members
		private ScrImportSet m_importSettings;
		#endregion

		#region Setup & Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scrInMemoryCache.InitializeScripture();
			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_importSettings = new ScrImportSet();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(m_importSettings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private byte[] ImportSettings
		{
			get
			{
				int hvo = Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0];
				return Cache.GetBinaryProperty(hvo, (int)ScrImportSet.ScrImportSetTags.kflidImportSettings);
			}
		}

		#endregion

		#region Convert Blob Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ConvertFromBlobSettings method for an "ECProject" blob, version 0x104
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertFromBlobSettings_Other_Version0x104()
		{
			CheckDisposed();

			// Create a blob of settings.
			ArrayList mappingList = new ArrayList();
			mappingList.Add(new MappingInfo(@"\btp", null, false, null, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, null, false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"\btp_fr", null, false, null, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, "fr", false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"\c", null, false, null, MarkerDomain.Default, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"|i{", @"}", true, null, MarkerDomain.Default, "Emphasis", null, false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"\ex", null, false, null, MarkerDomain.Default, null, null, true, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"\f", null, false, null, MarkerDomain.Default, ScrStyleNames.NormalFootnoteParagraph, null, false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"\fig", null, false, null, MarkerDomain.Default, null, null, false, MappingTargetType.Figure));
			mappingList.Add(new MappingInfo(@"\h", null, false, null, MarkerDomain.Default, null, null, false, MappingTargetType.TitleShort));
			mappingList.Add(new MappingInfo(@"\id", null, false, null, MarkerDomain.Default, null, null, false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"\rem", null, false, null, MarkerDomain.Note, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"\v", null, false, null, MarkerDomain.Default, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle));
			mappingList.Add(new MappingInfo(@"|x{", "}", true, null, MarkerDomain.Default | MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, false, MappingTargetType.TEStyle));

			ArrayList fileList = new ArrayList(2);
			fileList.Add(@"c:\my data\first file.sf");
			fileList.Add(@"c:\my data\tom's file.sf");

			m_importSettings.ImportType = (int)TypeOfImport.Other;

			DynamicMock mockStyleSheet = new DynamicMock(typeof(IVwStylesheet));
			mockStyleSheet.SetupResultForParams("GetContext", ContextValues.Note, ScrStyleNames.NormalFootnoteParagraph);
			mockStyleSheet.SetupResultForParams("GetContext", ContextValues.General, new IsAnything()); // We don't care about anything else for these tests

			ReflectionHelper.SetField(m_importSettings, "m_stylesheet", (IVwStylesheet)mockStyleSheet.MockInstance);

			// Call ConvertFromBlobSettings, which should cause the blob to be converted.
			ReflectionHelper.CallMethod(m_importSettings, "ConvertFromBlobSettings",
				GetEcProjectSettingsArray(mappingList, fileList));

			// Make sure that the blob and deprecated footnote settings are eliminated and
			// that the settings are correct in the database.
			Assert.AreEqual(0, ImportSettings.Length);
			Assert.AreEqual(12, m_importSettings.ScriptureMappingsOC.Count);
			Assert.AreEqual(0, m_importSettings.NoteMappingsOC.Count);

			IEnumerator<IScrMarkerMapping> mappings = m_importSettings.ScriptureMappingsOC.GetEnumerator();
			//mappings.Reset(); // Throws NotSupportedException exception on generic enumerator
			VerifyNextMapping(mappings, @"\btp", null, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\btp_fr", null, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, "fr", false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\c", null, MarkerDomain.Default /* NOT Default! */, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"|i{", @"}", MarkerDomain.Default, "Emphasis", null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\ex", null, MarkerDomain.Default, null, null, true, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\f", null, MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\fig", null, MarkerDomain.Default, null, null, false, MappingTargetType.Figure);
			VerifyNextMapping(mappings, @"\h", null, MarkerDomain.Default, null, null, false, MappingTargetType.TitleShort);
			VerifyNextMapping(mappings, @"\id", null, MarkerDomain.Default, null, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\rem", null, MarkerDomain.Note, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\v", null, MarkerDomain.Default /* NOT Default! */, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"|x{", "}", MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, false, MappingTargetType.TEStyle);
			Assert.IsFalse(mappings.MoveNext(), "This is just a sanity check to make sure we remembered to check all the mappings in this test.");

			Assert.AreEqual(1, m_importSettings.ScriptureSourcesOC.Count);
			Assert.AreEqual(0, m_importSettings.BackTransSourcesOC.Count);
			Assert.AreEqual(0, m_importSettings.NoteSourcesOC.Count);

			ScrImportSFFiles scriptureSource = new ScrImportSFFiles(Cache, m_importSettings.ScriptureSourcesOC.HvoArray[0]);
			Assert.AreEqual((int)FileFormatType.Other, scriptureSource.FileFormat);
			Assert.AreEqual(2, scriptureSource.FilesOC.Count);
			int[] cmFileHvos = scriptureSource.FilesOC.HvoArray;
			CmFile file = new CmFile(Cache, cmFileHvos[0]);
			Assert.AreEqual((string)fileList[0], file.AbsoluteInternalPath);
			file = new CmFile(Cache, cmFileHvos[1]);
			Assert.AreEqual((string)fileList[1], file.AbsoluteInternalPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ConvertFromBlobSettings method for a "Paratext" blob, version 0x104,
		/// with no Notes project specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertFromBlobSettings_Paratext_Version0x104_NoNotesProj()
		{
			CheckDisposed();

			// Create a blob of settings.
			ArrayList vernMappingList = new ArrayList();
			vernMappingList.Add(new MappingInfo(@"\c", null, false, null, MarkerDomain.Default, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\ex", null, false, null, MarkerDomain.Default, null, null, true, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\f", @"\f*", true, null, MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\fig", null, false, null, MarkerDomain.Default, null, null, false, MappingTargetType.Figure));
			vernMappingList.Add(new MappingInfo(@"\h", null, false, null, MarkerDomain.Default, null, null, false, MappingTargetType.TitleShort));
			vernMappingList.Add(new MappingInfo(@"\i", @"\i*", true, null, MarkerDomain.Default, "Emphasis", null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\id", null, false, null, MarkerDomain.Default, null, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\p", null, false, null, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\rem", null, false, null, MarkerDomain.Note, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\v", null, false, null, MarkerDomain.Default, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\x", @"\x*", true, null, MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, false, MappingTargetType.TEStyle));

			ArrayList backMappingList = new ArrayList();
			backMappingList.Add(new MappingInfo(@"\c", null, false, null, MarkerDomain.BackTrans, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle));
			backMappingList.Add(new MappingInfo(@"\f", @"\f*", true, null, MarkerDomain.BackTrans | MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, false, MappingTargetType.TEStyle));
			backMappingList.Add(new MappingInfo(@"\fig", null, false, null, MarkerDomain.BackTrans, "Dude, this is a mistake!", null, false, MappingTargetType.TEStyle));
			backMappingList.Add(new MappingInfo(@"\h", null, false, null, MarkerDomain.BackTrans, null, null, false, MappingTargetType.TitleShort));
			backMappingList.Add(new MappingInfo(@"\id", null, false, null, MarkerDomain.BackTrans, null, null, false, MappingTargetType.TEStyle));
			backMappingList.Add(new MappingInfo(@"\p", null, false, null, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, null, false, MappingTargetType.TEStyle));
			backMappingList.Add(new MappingInfo(@"\v", null, false, null, MarkerDomain.BackTrans, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle));
			backMappingList.Add(new MappingInfo(@"\x", @"\x*", true, null, MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, false, MappingTargetType.TEStyle));
			backMappingList.Add(new MappingInfo(@"\utw", @"\utw*", true, null, MarkerDomain.BackTrans, ScrStyleNames.UntranslatedWord, null, false, MappingTargetType.TEStyle));

			m_importSettings.ImportType = (int)TypeOfImport.Paratext6;

			// Call ConvertFromBlobSettings, which should cause the blob to be converted.
			ReflectionHelper.CallMethod(m_importSettings, "ConvertFromBlobSettings",
				GetParatextSettingsArray(vernMappingList, backMappingList, null, "ABC", "XYZ", null));

			// Make sure that the blob and deprecated footnote settings are eliminated and
			// that the settings are correct in the database.
			Assert.AreEqual(0, ImportSettings.Length);
			Assert.AreEqual(12, m_importSettings.ScriptureMappingsOC.Count);
			Assert.AreEqual(0, m_importSettings.NoteMappingsOC.Count);

			IEnumerator<IScrMarkerMapping> mappings = m_importSettings.ScriptureMappingsOC.GetEnumerator();
			//mappings.Reset(); // Throws NotSupportedException exception on generic enumerator
			VerifyNextMapping(mappings, @"\c", null, MarkerDomain.Default /* NOT Default! */, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\ex", null, MarkerDomain.Default, null, null, true, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\f", @"\f*", MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\fig", null, MarkerDomain.Default, null, null, false, MappingTargetType.Figure);
			VerifyNextMapping(mappings, @"\h", null, MarkerDomain.Default, null, null, false, MappingTargetType.TitleShort);
			VerifyNextMapping(mappings, @"\i", @"\i*", MarkerDomain.Default, "Emphasis", null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\id", null, MarkerDomain.Default, null, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\p", null, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\rem", null, MarkerDomain.Note, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\v", null, MarkerDomain.Default /* NOT Default! */, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\x", @"\x*", MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\utw", @"\utw*", MarkerDomain.BackTrans, ScrStyleNames.UntranslatedWord, null, false, MappingTargetType.TEStyle);
			Assert.IsFalse(mappings.MoveNext(), "This is just a sanity check to make sure we remembered to check all the mappings in this test.");

			Assert.AreEqual(1, m_importSettings.ScriptureSourcesOC.Count);
			Assert.AreEqual(1, m_importSettings.BackTransSourcesOC.Count);
			Assert.AreEqual(0, m_importSettings.NoteSourcesOC.Count);

			ScrImportP6Project scriptureSource = new ScrImportP6Project(Cache, m_importSettings.ScriptureSourcesOC.HvoArray[0]);
			Assert.AreEqual("ABC", scriptureSource.ParatextID);
			ScrImportP6Project backTransSource = new ScrImportP6Project(Cache, m_importSettings.BackTransSourcesOC.HvoArray[0]);
			Assert.AreEqual("XYZ", backTransSource.ParatextID);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ConvertFromBlobSettings method for a "Paratext" blob, version 0x104,
		/// with a Notes project specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertFromBlobSettings_Paratext_Version0x104_NotesProj()
		{
			CheckDisposed();

			// Create a blob of settings.
			ArrayList vernMappingList = new ArrayList();
			vernMappingList.Add(new MappingInfo(@"\c", null, false, null, MarkerDomain.Default, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\id", null, false, null, MarkerDomain.Default, null, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\p", null, false, null, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\rem", null, false, null, MarkerDomain.Note, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle));
			vernMappingList.Add(new MappingInfo(@"\v", null, false, null, MarkerDomain.Default, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle));

			ArrayList notesMappingList = new ArrayList();
			notesMappingList.Add(new MappingInfo(@"\c", null, false, null, MarkerDomain.Note, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle));
			notesMappingList.Add(new MappingInfo(@"\id", null, false, null, MarkerDomain.Note, null, null, false, MappingTargetType.TEStyle));
			notesMappingList.Add(new MappingInfo(@"\rem", null, false, null, MarkerDomain.Note, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle));
			notesMappingList.Add(new MappingInfo(@"\v", null, false, null, MarkerDomain.Note, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle));
			notesMappingList.Add(new MappingInfo(@"\xo", @"\xo*", true, null, MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, false, MappingTargetType.TEStyle));

			m_importSettings.ImportType = (int)TypeOfImport.Paratext6;

			// Call ConvertFromBlobSettings, which should cause the blob to be converted.
			ReflectionHelper.CallMethod(m_importSettings, "ConvertFromBlobSettings",
				GetParatextSettingsArray(vernMappingList, null, notesMappingList, "ABC", null, "XYZ"));

			// Make sure that the blob and deprecated footnote settings are eliminated and
			// that the settings are correct in the database.
			Assert.AreEqual(0, ImportSettings.Length);
			Assert.AreEqual(5, m_importSettings.ScriptureMappingsOC.Count);
			Assert.AreEqual(5, m_importSettings.NoteMappingsOC.Count);

			IEnumerator<IScrMarkerMapping> mappings = m_importSettings.ScriptureMappingsOC.GetEnumerator();
			//mappings.Reset(); // Throws NotSupportedException exception on generic enumerator
			VerifyNextMapping(mappings, @"\c", null, MarkerDomain.Default /* NOT Default! */, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\id", null, MarkerDomain.Default, null, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\p", null, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\rem", null, MarkerDomain.Note, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\v", null, MarkerDomain.Default /* NOT Default! */, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle);
			Assert.IsFalse(mappings.MoveNext(), "This is just a sanity check to make sure we remembered to check all the mappings in this test.");

			mappings = m_importSettings.NoteMappingsOC.GetEnumerator();
			//mappings.Reset(); // Throws NotSupportedException exception on generic enumerator
			VerifyNextMapping(mappings, @"\c", null, MarkerDomain.Default, ScrStyleNames.ChapterNumber, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\id", null, MarkerDomain.Default, null, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\rem", null, MarkerDomain.Default, ScrStyleNames.Remark, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\v", null, MarkerDomain.Default, ScrStyleNames.VerseNumber, null, false, MappingTargetType.TEStyle);
			VerifyNextMapping(mappings, @"\xo", @"\xo*", MarkerDomain.Default, ScrStyleNames.CrossRefFootnoteParagraph, null, false, MappingTargetType.TEStyle);
			Assert.IsFalse(mappings.MoveNext(), "This is just a sanity check to make sure we remembered to check all the mappings in this test.");

			Assert.AreEqual(1, m_importSettings.ScriptureSourcesOC.Count);
			Assert.AreEqual(0, m_importSettings.BackTransSourcesOC.Count);
			Assert.AreEqual(1, m_importSettings.NoteSourcesOC.Count);

			ScrImportP6Project scriptureSource = new ScrImportP6Project(Cache, m_importSettings.ScriptureSourcesOC.HvoArray[0]);
			Assert.AreEqual("ABC", scriptureSource.ParatextID);
			ScrImportP6Project notesSource = new ScrImportP6Project(Cache, m_importSettings.NoteSourcesOC.HvoArray[0]);
			Assert.AreEqual("XYZ", notesSource.ParatextID);
		}
		#endregion

		#region Save/load Mappings Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test saving and reloading mappings in both Scripture and Notes lists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndLoadMappings()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\a", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\b", @"\b*", false, MappingTargetType.TEStyle, MarkerDomain.Default, "Key Word", null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\f", @"\f*", false, MappingTargetType.TEStyle, MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\ex", null, true, MappingTargetType.TEStyle, MarkerDomain.Default, null, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\greek", @"\greek*", false, MappingTargetType.TEStyle, MarkerDomain.Default, "Doxology", "gr"));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\c", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.ChapterNumber, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\v", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.VerseNumber, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\id", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, null, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\vt", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, "Default Paragraph Characters", null));

			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\bta", null, false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\btb", @"\btb*", false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, "Key Word", null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\btf", @"\btf*", false, MappingTargetType.TEStyle, MarkerDomain.BackTrans | MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, null));

			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\bta_es", null, false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, "es"));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\btb_es", @"\btb_es*", false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, "Key Word", "es"));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\btf_es", @"\btf_es*", false, MappingTargetType.TEStyle, MarkerDomain.BackTrans | MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, "es", null));

			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\rem", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_inMemoryCache.m_translatorNoteDefn));

			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\rem", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.Remark, null, m_inMemoryCache.m_consultantNoteDefn));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\m", @"\m*", false, MappingTargetType.TEStyle, MarkerDomain.Note, "Emphasis", null, null));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\c", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.ChapterNumber, null));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\v", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.VerseNumber, null));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\id", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, null, null));

			// save to the DB
			m_importSettings.SaveSettings();

			// create a new settings object and reload it from the DB
			ScrImportSet newSettingsObj = new ScrImportSet(Cache,
				Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

			// verify the settings
			IEnumerable scrMappings = newSettingsObj.Mappings(MappingSet.Main);
			Assert.IsNotNull(scrMappings);

			Hashtable htCheck = new Hashtable();
			foreach (ImportMappingInfo mapping in scrMappings)
			{
				htCheck[mapping.BeginMarker] = true;
				switch(mapping.BeginMarker)
				{
					case @"\a":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\b":
						Assert.AreEqual(@"\b*", mapping.EndMarker);
						Assert.AreEqual("Key Word", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\f":
						Assert.AreEqual(@"\f*", mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Footnote, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(true, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\ex":
						Assert.IsNull(mapping.EndMarker);
						Assert.IsNull(mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsTrue(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\greek":
						Assert.AreEqual(@"\greek*", mapping.EndMarker);
						Assert.AreEqual("Doxology", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("gr", mapping.IcuLocale);
						break;

					case @"\fig":
						Assert.IsNull(mapping.EndMarker);
						Assert.IsNull(mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.Figure, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\c":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.ChapterNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\v":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.VerseNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\vt":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual("Default Paragraph Characters", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\id":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(null, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\bta":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\btb":
						Assert.AreEqual(@"\btb*", mapping.EndMarker);
						Assert.AreEqual("Key Word", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\btf":
						Assert.AreEqual(@"\btf*", mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans | MarkerDomain.Footnote, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(true, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						break;

					case @"\bta_es":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("es", mapping.IcuLocale);
						break;

					case @"\btb_es":
						Assert.AreEqual(@"\btb_es*", mapping.EndMarker);
						Assert.AreEqual("Key Word", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("es", mapping.IcuLocale);
						break;

					case @"\btf_es":
						Assert.AreEqual(@"\btf_es*", mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans | MarkerDomain.Footnote, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(true, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("es", mapping.IcuLocale);
						break;

					case @"\rem":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.Remark, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Note, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(false, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						Assert.AreEqual(m_inMemoryCache.m_translatorNoteDefn, mapping.NoteType);
						break;

					default:
						Assert.Fail();
						break;
				}
			}
			Assert.AreEqual(17, htCheck.Count);

			IEnumerable notesMappings = newSettingsObj.Mappings(MappingSet.Notes);
			Assert.IsNotNull(notesMappings);

			htCheck = new Hashtable();
			foreach (ImportMappingInfo mapping in notesMappings)
			{
				htCheck[mapping.BeginMarker] = true;
				switch(mapping.BeginMarker)
				{
					case @"\rem":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.Remark, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						Assert.AreEqual(m_inMemoryCache.m_consultantNoteDefn, mapping.NoteType);
						break;

					case @"\m":
						Assert.AreEqual(@"\m*", mapping.EndMarker);
						Assert.AreEqual("Emphasis", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						Assert.IsNull(mapping.NoteType);
						break;

					case @"\c":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.ChapterNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						Assert.IsNull(mapping.NoteType);
						break;

					case @"\v":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.VerseNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						Assert.IsNull(mapping.NoteType);
						break;

					case @"\id":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(null, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.IcuLocale);
						Assert.IsNull(mapping.NoteType);
						break;

					default:
						Assert.Fail();
						break;
				}
			}
			Assert.AreEqual(5, htCheck.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test saving and reloading mappings with modifications
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndLoadMappings_DeleteModify()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\aa", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\cc", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\ee", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null));
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\qq", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null));

			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\aa", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_inMemoryCache.m_consultantNoteDefn));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\cc", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_inMemoryCache.m_consultantNoteDefn));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\ee", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_inMemoryCache.m_consultantNoteDefn));

			// save to the DB, create a new settings object and reload it
			m_importSettings.SaveSettings();
			ScrImportSet newSettingsObj = new ScrImportSet(Cache,
				Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

			// Sanity check to verify the settings
			IEnumerable scrMappings = newSettingsObj.Mappings(MappingSet.Main);
			Assert.IsNotNull(scrMappings);
			Assert.AreEqual(4, ((ScrMappingList)scrMappings).Count);

			IEnumerable notesMappings = newSettingsObj.Mappings(MappingSet.Notes);
			Assert.IsNotNull(notesMappings);
			Assert.AreEqual(3, ((ScrMappingList)notesMappings).Count);

			// Replace "cc" in Main and "ee" in Notes
			newSettingsObj.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\cc", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));
			newSettingsObj.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\ee", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null , m_inMemoryCache.m_translatorNoteDefn));

			// Modify existing "ee" in Main and "aa" in Notes
			ImportMappingInfo main_ee = newSettingsObj.MappingForMarker(@"\ee", MappingSet.Main);
			main_ee.StyleName = ScrStyleNames.NormalFootnoteParagraph;
			ImportMappingInfo notes_aa = newSettingsObj.MappingForMarker(@"\aa", MappingSet.Notes);
			notes_aa.StyleName = "Emphasis";

			// Delete "qq" in Main and "cc" in Notes
			ImportMappingInfo main_qq = newSettingsObj.MappingForMarker(@"\qq", MappingSet.Main);
			ImportMappingInfo notes_cc = newSettingsObj.MappingForMarker(@"\cc", MappingSet.Notes);
			newSettingsObj.DeleteMapping(MappingSet.Main, main_qq);
			newSettingsObj.DeleteMapping(MappingSet.Notes, notes_cc);

			// Save the settings, create a new settings object and reload it.
			newSettingsObj.SaveSettings();
			newSettingsObj = new ScrImportSet(Cache,
				Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

			// Make sure that all the changes were saved and restored
			ScrMappingList scrMappingList = (ScrMappingList)newSettingsObj.Mappings(MappingSet.Main);
			Assert.IsNotNull(scrMappingList);
			Assert.AreEqual(3, scrMappingList.Count);

			ScrMappingList notesMappingList = (ScrMappingList)newSettingsObj.Mappings(MappingSet.Notes);
			Assert.IsNotNull(notesMappingList);
			Assert.AreEqual(2, notesMappingList.Count);

			ImportMappingInfo mapping = scrMappingList[0];
			Assert.AreEqual(@"\aa", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
			Assert.AreEqual(ScrStyleNames.NormalParagraph, mapping.StyleName);

			mapping = scrMappingList[1];
			Assert.AreEqual(@"\cc", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
			Assert.AreEqual(MappingTargetType.Figure, mapping.MappingTarget);

			mapping = scrMappingList[2];
			Assert.AreEqual(@"\ee", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
			Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, mapping.StyleName);

			mapping = notesMappingList[0];
			Assert.AreEqual(@"\aa", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
			Assert.AreEqual("Emphasis", mapping.StyleName);

			mapping = notesMappingList[1];
			Assert.AreEqual(@"\ee", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
			Assert.AreEqual(m_inMemoryCache.m_translatorNoteDefn.Guid.ToString(), mapping.NoteType.Guid.ToString());
		}
		#endregion

		#region File Ordering Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the sorting of files by canonical reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SortingProjectFiles()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			string[] files = new string[4];
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				files[0] = fileMaker.CreateFile("MAT", new string[] {@"\p", @"\c 1", @"\v 1", @"\v 2"});
				files[1] = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});
				files[2] = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 2", @"\v 1"});
				files[3] = fileMaker.CreateFile("ACT", new string[] {@"\p", @"\c 1", @"\v 1"});

				foreach (string fileName in files)
					m_importSettings.AddFile(fileName, ImportDomain.Main, null, 0);

				// Get the list of files back from the project and make sure they are in the
				// correct order.
				ImportFileSource source = m_importSettings.GetImportFiles(ImportDomain.Main);
				IEnumerator enumerator = source.GetEnumerator();
				Assert.IsTrue(enumerator.MoveNext());
				ScrImportFileInfo info = (ScrImportFileInfo)enumerator.Current;
				Assert.AreEqual(files[2], info.FileName, "Genesis 2 file was out of order");

				Assert.IsTrue(enumerator.MoveNext());
				info = (ScrImportFileInfo)enumerator.Current;
				Assert.AreEqual(files[1], info.FileName, "Genesis 3 file was out of order");

				Assert.IsTrue(enumerator.MoveNext());
				info = (ScrImportFileInfo)enumerator.Current;
				Assert.AreEqual(files[0], info.FileName, "Matthew file was out of order");

				Assert.IsTrue(enumerator.MoveNext());
				info = (ScrImportFileInfo)enumerator.Current;
				Assert.AreEqual(files[3], info.FileName, "Acts file was out of order");
				Assert.IsFalse(enumerator.MoveNext());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the file lists for a Paratext 5 project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndReloadProjectFiles_P5()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			TempSFFileMaker fileMaker = new TempSFFileMaker();
			string scrFile = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 1", @"\v 1", @"\v 2"});
			string btFile = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});
			string annotationFile = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});

			m_importSettings.AddFile(scrFile, ImportDomain.Main, null, 0);
			m_importSettings.AddFile(btFile, ImportDomain.BackTrans, "es", 0);
			m_importSettings.AddFile(annotationFile, ImportDomain.Annotations, "de", m_inMemoryCache.m_consultantNoteDefn.Hvo);

			// Save the settings and reload
			m_importSettings.SaveSettings();
			m_importSettings = new ScrImportSet(Cache,
				Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

			// Test to see that the files were saved and loaded correctly
			Assert.AreEqual(TypeOfImport.Paratext5, m_importSettings.ImportTypeEnum);
			ImportFileSource source = m_importSettings.GetImportFiles(ImportDomain.Main);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
				Assert.AreEqual(scrFile.ToUpper(), info.FileName.ToUpper());

			source = m_importSettings.GetImportFiles(ImportDomain.BackTrans);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(btFile.ToUpper(), info.FileName.ToUpper());
				Assert.AreEqual("es", info.IcuLocale);
			}

			source = m_importSettings.GetImportFiles(ImportDomain.Annotations);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(annotationFile.ToUpper(), info.FileName.ToUpper());
				Assert.AreEqual("de", info.IcuLocale);
				Assert.AreEqual(m_inMemoryCache.m_consultantNoteDefn.Hvo, info.NoteTypeHvo);
			}

			// While we're at it, let's test CheckForOverlappingFilesInRange. This should
			// do nothing since there are no overlaps -- it will throw an exception if an
			// overlap is detected.
			m_importSettings.CheckForOverlappingFilesInRange(
				new ScrReference(1, 1, 1, Paratext.ScrVers.English),
				new ScrReference(66, 21, 8, Paratext.ScrVers.English));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the settings after changing import type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndReloadSources_SaveSeparateSourcesWhenImportTypeChanges()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			TempSFFileMaker fileMaker = new TempSFFileMaker();
			string scrFile = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 1", @"\v 1", @"\v 2"});
			string btFile = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});
			string annotationFile = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});

			m_importSettings.AddFile(scrFile, ImportDomain.Main, null, 0);
			m_importSettings.AddFile(btFile, ImportDomain.BackTrans, "es", 0);
			m_importSettings.AddFile(annotationFile, ImportDomain.Annotations, "de", m_inMemoryCache.m_consultantNoteDefn.Hvo);

			// Save the settings and reload
			m_importSettings.SaveSettings();
			m_importSettings = new ScrImportSet(Cache,
				Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
				m_importSettings.ParatextScrProj = "KAM";
				m_importSettings.SaveSettings();

				m_importSettings = new ScrImportSet(Cache,
					Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

				m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}

			// Test to see that the files survived the type change
			Assert.AreEqual(TypeOfImport.Paratext5, m_importSettings.ImportTypeEnum);
			ImportFileSource source = m_importSettings.GetImportFiles(ImportDomain.Main);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
				Assert.AreEqual(scrFile.ToUpper(), info.FileName.ToUpper());

			source = m_importSettings.GetImportFiles(ImportDomain.BackTrans);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(btFile.ToUpper(), info.FileName.ToUpper());
				Assert.AreEqual("es", info.IcuLocale);
			}

			source = m_importSettings.GetImportFiles(ImportDomain.Annotations);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(annotationFile.ToUpper(), info.FileName.ToUpper());
				Assert.AreEqual("de", info.IcuLocale);
				Assert.AreEqual(m_inMemoryCache.m_consultantNoteDefn.Hvo, info.NoteTypeHvo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the file lists for an "other" SF project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndReloadProjectFiles_Other()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			string[] filesScr = new string[5];
			TempSFFileMaker fileMaker = new TempSFFileMaker();
			// vernacular files
			filesScr[0] = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 1", @"\v 1", @"\v 2"});
			filesScr[1] = fileMaker.CreateFile("EXO", new string[] {@"\p", @"\c 3", @"\v 1"});
			filesScr[2] = fileMaker.CreateFile("LEV", new string[] {@"\p", @"\c 2", @"\v 1"});
			filesScr[3] = fileMaker.CreateFile("NUM", new string[] {@"\p", @"\c 1", @"\v 1"});
			filesScr[4] = fileMaker.CreateFile("DEU", new string[] {@"\p", @"\c 1", @"\v 1", @"\v 2"});

			// BT files
			string[] filesBT = new string[3];
			filesBT[0] = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});
			filesBT[1] = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});
			filesBT[2] = fileMaker.CreateFile("MAT", new string[] {@"\p", @"\c 1", @"\v 1"});

			// notes files
			string[] filesNotes = new string[3];
			filesNotes[0] = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});
			filesNotes[1] = fileMaker.CreateFile("GEN", new string[] {@"\p", @"\c 3", @"\v 1"});
			filesNotes[2] = fileMaker.CreateFile("DEU", new string[] {@"\p", @"\c 1", @"\v 1"});

			// add Scripture files
			foreach (string fileName in filesScr)
				m_importSettings.AddFile(fileName, ImportDomain.Main, null, 0);

			// make one of the file names upper case to test case sensitivity
			m_importSettings.AddFile(filesScr[2].ToUpper(), ImportDomain.Main, null, 0);

			// add BT file with different ICU locales
			m_importSettings.AddFile(filesBT[0], ImportDomain.BackTrans, "en", 0);
			m_importSettings.AddFile(filesBT[1], ImportDomain.BackTrans, "es", 0);
			m_importSettings.AddFile(filesBT[2], ImportDomain.BackTrans, "es", 0);

			// add Notes files with different ICU locales and note type
			m_importSettings.AddFile(filesNotes[0], ImportDomain.Annotations, null, m_inMemoryCache.m_translatorNoteDefn.Hvo);
			m_importSettings.AddFile(filesNotes[1], ImportDomain.Annotations, "es", m_inMemoryCache.m_translatorNoteDefn.Hvo);
			m_importSettings.AddFile(filesNotes[2], ImportDomain.Annotations, "de", m_inMemoryCache.m_consultantNoteDefn.Hvo);

			// Save the settings and reload
			m_importSettings.SaveSettings();
			m_importSettings = new ScrImportSet(Cache,
				Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

			// Test to see that the vernacular file list has the right number of files
			Assert.AreEqual(TypeOfImport.Other, m_importSettings.ImportTypeEnum);
			CheckFileListContents(filesScr, ImportDomain.Main);
			CheckFileListContents(filesBT, ImportDomain.BackTrans);
			CheckFileListContents(filesNotes, ImportDomain.Annotations);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the Scripture and BT Paratext 6 projects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndReloadParatext6Projects()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				// add Scripture files
				m_importSettings.ParatextScrProj = "KAM";
				m_importSettings.ParatextBTProj = "TEV";

				// Save the settings and reload
				m_importSettings.SaveSettings();
				m_importSettings = null;

				m_importSettings = new ScrImportSet(Cache,
					Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

				// Test to see that the projects are set correctly
				Assert.AreEqual(TypeOfImport.Paratext6, m_importSettings.ImportTypeEnum);
				Assert.AreEqual("KAM", m_importSettings.ParatextScrProj);
				Assert.AreEqual("TEV", m_importSettings.ParatextBTProj);

				ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);

				Assert.AreEqual(176, mappingList.Count, "This should be the combined number of unique markers between KAM and TEV.");

				Assert.AreEqual(MarkerDomain.Default, mappingList[@"\c"].Domain);
				Assert.AreEqual(MarkerDomain.Default, mappingList[@"\v"].Domain);
				Assert.AreEqual(MappingTargetType.Figure, mappingList[@"\fig"].MappingTarget);
				Assert.AreEqual(MarkerDomain.BackTrans, mappingList[@"\pdi"].Domain);
				Assert.IsNull(mappingList[@"\pdi"].StyleName);

				mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Notes);
				Assert.AreEqual(0, mappingList.Count);
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}
		#endregion

		#region AddFile tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test adding a file that is locked
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_Locked()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string filename = fileMaker.CreateFile("EPH",
					new string[] {@"\c 1", @"\v 1"}, Encoding.Unicode, true);

				// Lock the file
				using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
				{
					ScrImportFileInfo info = m_importSettings.AddFile(filename, ImportDomain.Main, null, 0);
					Assert.AreEqual(Encoding.ASCII, info.FileEncoding);
					Assert.AreEqual(1, m_importSettings.GetImportFiles(ImportDomain.Main).Count);
					StringCollection notFound;
					Assert.IsFalse(m_importSettings.ImportProjectIsAccessible(out notFound));
					Assert.AreEqual(1, notFound.Count);
					Assert.AreEqual(filename, (string)notFound[0]);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_UnicodeBOM()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string filename = fileMaker.CreateFile("EPH",
					new string[] {@"\c 1", @"\v 1"}, Encoding.Unicode, true);
				ScrImportFileInfo info = m_importSettings.AddFile(filename, ImportDomain.Main, null, 0);
				Assert.AreEqual(Encoding.Unicode, info.FileEncoding);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-3754
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException))]
		public void TestAddFile_Empty()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			TempSFFileMaker fileMaker = new TempSFFileMaker();
			string filename = fileMaker.CreateFileNoID(new string[] {});
			m_importSettings.AddFile(filename, ImportDomain.Main, null, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_UnicodeNoBOM()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string filename = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"},
					Encoding.Unicode, false);
				ScrImportFileInfo info = m_importSettings.AddFile(filename, ImportDomain.Main, null, 0);
				Assert.AreEqual(Encoding.Unicode, info.FileEncoding);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_UTF8BOM()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string filename = fileMaker.CreateFile("EPH",
					new string[] {@"\c 1", @"\v 1"}, Encoding.UTF8, true);
				ScrImportFileInfo info = m_importSettings.AddFile(filename, ImportDomain.Main, null, 0);
				Assert.AreEqual(Encoding.UTF8, info.FileEncoding);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_UTF8NoBOM()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string filename = fileMaker.CreateFile("EPH",
					new string[] {"\\ud 12/Aug/2002",
									 "\\mt \u0782\u0785\u07a7\u0794",
									 "\\c 1",
									 "\\s \u0787\u0786\u078c\u07a6 \u0794\u0786\u078c",
									 "\\p",
									 "\\v 1",
									 "\\vt \u078c\u0789\u0789\u0782\u0780\u07a2"},
					Encoding.UTF8, false);
				ScrImportFileInfo info = m_importSettings.AddFile(filename, ImportDomain.Main, null, 0);
				Assert.AreEqual(Encoding.UTF8, info.FileEncoding);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_ASCII()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string fileName = fileMaker.CreateFile("EPH",
					new string[] {@"\mt Ephesians", @"\c 1", @"\v 1"});
				ScrImportFileInfo info = m_importSettings.AddFile(fileName, ImportDomain.Main, null, 9);
				Assert.AreEqual(Encoding.ASCII, info.FileEncoding);
				Assert.AreEqual(1, m_importSettings.GetImportFiles(ImportDomain.Main).Count);

				// see that all the mappings were created properly
				Assert.AreEqual(4, m_importSettings.GetMappingListForDomain(ImportDomain.Main).Count);
				ImportMappingInfo mapping = m_importSettings.MappingForMarker(@"\id", MappingSet.Main);
				Assert.AreEqual(@"\id", mapping.BeginMarker);
				Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
				Assert.IsNull(mapping.StyleName);

				mapping = m_importSettings.MappingForMarker(@"\mt", MappingSet.Main);
				Assert.AreEqual(@"\mt", mapping.BeginMarker);
				Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
				Assert.AreEqual("Title Main", mapping.StyleName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_BigEndianUnicodeBOM()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string fileName = fileMaker.CreateFile("EPH",
					new string[] {@"\c 1", @"\v 1"}, Encoding.BigEndianUnicode, true);
				ScrImportFileInfo info = m_importSettings.AddFile(fileName, ImportDomain.Main, null, 0);
				Assert.AreEqual(Encoding.BigEndianUnicode, info.FileEncoding);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_BigEndianUnicodeNoBOM()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string fileName = fileMaker.CreateFile("EPH",
					new string[] {@"\c 1", @"\v 1"}, Encoding.BigEndianUnicode, false);
				ScrImportFileInfo info = m_importSettings.AddFile(fileName, ImportDomain.Main, null, 0);
				Assert.AreEqual(Encoding.BigEndianUnicode, info.FileEncoding);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to add non-existent file to the SF Project.  This should be allowed because
		/// there may be network files that are not currently available. We want to remember
		/// the file names because they may be accessible in the future.
		/// Jira task number is TE-510
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddNonexistentFileToProject()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			m_importSettings.AddFile(@"q:\wugga\bugga\slugga.hhh", ImportDomain.Main, null, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-505
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddFile_AddingExistingFile()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				// create 5 temporary files and add them to the settings
				string fileName = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"}, Encoding.UTF8, false);
				m_importSettings.AddFile(fileName, ImportDomain.Main, null, 0);

				string fileName2 = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"}, Encoding.UTF8, false);
				m_importSettings.AddFile(fileName2, ImportDomain.Main, null, 0);

				string fileName3 = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"}, Encoding.UTF8, false);
				m_importSettings.AddFile(fileName3, ImportDomain.Main, null, 0);

				string fileName4 = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"}, Encoding.UTF8, false);
				m_importSettings.AddFile(fileName4, ImportDomain.Main, null, 0);

				string fileName5 = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"}, Encoding.UTF8, false);
				m_importSettings.AddFile(fileName5, ImportDomain.Main, null, 0);
				// make sure the file count is correct
				Assert.IsNotNull(m_importSettings.GetImportFiles(ImportDomain.Main).Count);

				// re-add the same 5 files
				m_importSettings.AddFile(fileName, ImportDomain.Main, null, 0);
				m_importSettings.AddFile(fileName2, ImportDomain.Main, null, 0);
				m_importSettings.AddFile(fileName3, ImportDomain.Main, null, 0);
				m_importSettings.AddFile(fileName4, ImportDomain.Main, null, 0);
				m_importSettings.AddFile(fileName5, ImportDomain.Main, null, 0);

				// make sure the file count is still correct
				Assert.IsNotNull(m_importSettings.GetImportFiles(ImportDomain.Main).Count);
			}
		}
		#endregion

		#region Mapping-in-use tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to load a Paratext 6 project and distinguish between markers in use
		/// in the files and those that only come for them STY file, as well as making sure that
		/// the mappings are not in use when rescanning.
		/// Jiras task is TE-2439
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkMappingsInUse_Paratext6()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
			m_importSettings.SetMapping(MappingSet.Main, new ImportMappingInfo("\\hahaha",
				"\\*hahaha", false, MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				null, null, true, ImportDomain.Main));
			m_importSettings.SetMapping(MappingSet.Main, new ImportMappingInfo("\\bthahaha",
				"\\*bthahaha", false, MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				"en", null, true, ImportDomain.Main));

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				// set Scripture project
				m_importSettings.ParatextScrProj = "TEV";

				ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);

				Assert.IsTrue(mappingList[@"\c"].IsInUse);
				Assert.IsTrue(mappingList[@"\p"].IsInUse);
				Assert.IsFalse(mappingList[@"\ipi"].IsInUse);
				Assert.IsFalse(mappingList[@"\hahaha"].IsInUse,
					"In-use flag should have been cleared before re-scanning when the P6 project changed.");
				Assert.IsTrue(mappingList[@"\bthahaha"].IsInUse,
					"In-use flag should not have been cleared before re-scanning when the P6 project changed because it was in use by the BT.");
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that in use flags are cleared for all mappings when all files are removed from
		/// a list.
		/// Jiras task is TE-4435
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkMappingsInUse_FlagsClearedWhenAllFilesRemoved()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			Unpacker.UnPackSfTestProjects();
			try
			{
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "01GEN.sfm", ImportDomain.Main, null, 0);

				m_importSettings.SetMapping(MappingSet.Main, new ImportMappingInfo("\\tom",
					"\\*tom", false, MappingTargetType.TEStyle, MarkerDomain.Default, "bogleMarker",
					null, null, true, ImportDomain.Main));

				m_importSettings.RemoveFile(Unpacker.SfProjectTestFolder + "01GEN.sfm", ImportDomain.Main, null, 0);

				ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);

				foreach (ImportMappingInfo mapping in mappingList)
					Assert.IsFalse(mapping.IsInUse);
			}
			finally
			{
				Unpacker.RemoveSfTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that in use flags are cleared for all inline Scripture mappings when switching
		/// from  Paratext 5 to other.
		/// Jiras task is TE-4435
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkMappingsInUse_SwitchFromParatext6ToOther()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				// set Scripture project
				m_importSettings.ParatextScrProj = "TEV";
				m_importSettings.ParatextNotesProj = "KAM";

				m_importSettings.ImportTypeEnum = TypeOfImport.Other;

				ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);
				foreach (ImportMappingInfo mapping in mappingList)
					Assert.IsFalse(mapping.IsInUse, "Expected false, received true 6 to other. First assertion");

				mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Notes);
				foreach (ImportMappingInfo mapping in mappingList)
					Assert.IsFalse(mapping.IsInUse, "Expected false, received true 6 to other. Second assertion");
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that in use flags are cleared for all inline Scripture mappings when switching
		/// from  Paratext 5 to other.
		/// Jiras task is TE-4435
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkMappingsInUse_SwitchFromParatext5ToOther_Main()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string fileName = fileMaker.CreateFile("MAT", new string[] {@"\p", @"\c 1", @"\v 1 \em Wow!\em*"});

				m_importSettings.AddFile(fileName, ImportDomain.Main, null, 0);
				m_importSettings.ImportTypeEnum = TypeOfImport.Other;

				ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);

				ImportMappingInfo inlineMapping = mappingList[@"\em"];
				Assert.IsFalse(inlineMapping.IsInUse);
				ImportMappingInfo mapping = mappingList[@"\c"];
				Assert.IsTrue(mapping.IsInUse);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that in use flags are cleared for all inline annotation mappings when switching
		/// from  Paratext 5 to other Annotations.
		/// Jiras task is TE-4435
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkMappingsInUse_SwitchFromParatext5ToOther_Annotations()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			using (TempSFFileMaker fileMaker = new TempSFFileMaker())
			{
				string fileName = fileMaker.CreateFile("MAT", new string[] { @"\p", @"\c 1", @"\v 1", @"\rem \em Wow!\em*" });

				m_importSettings.AddFile(fileName, ImportDomain.Annotations, "en", m_inMemoryCache.m_consultantNoteDefn.Hvo);
				m_importSettings.ImportTypeEnum = TypeOfImport.Other;

				ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Notes);

				ImportMappingInfo inlineMapping = mappingList[@"\em"];
				Assert.IsFalse(inlineMapping.IsInUse, "Expected false, received true 5 To Ann");
				ImportMappingInfo mapping = mappingList[@"\rem"];
				Assert.IsTrue(mapping.IsInUse, "Expected true, received false 5 To Ann");
			}
		}
		#endregion

		#region Project setting tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see what happens if you try to set a Paratext project as the project for
		/// multiple domains.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AttemptToUseSameParatextProjectTwice()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
				m_importSettings.ParatextBTProj = "KAM";
				m_importSettings.ParatextNotesProj = "KAM";
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that settings are discarded and initialization is done correctly when changing
		/// project type from P6 to P5
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SwitchFromParatext6ToParatext5Project()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
				m_importSettings.ParatextBTProj = "KAM";
				m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
// REVIEW: Should changing project tye automatically wipe out existing stuff. Makes logical sense in
// a way, but if the user makes a istake and switches right back, should their settings be lost?
// What about mappings?
//				Assert.IsNull(m_importSettings.ParatextBTProj);
				m_importSettings.AddFile("IDontExistButAtLeastIDontCrash.txt",
					ImportDomain.Main, null, 0);
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the HasNonInterleavedBT property for a Paratext5 project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasNonInterleavedBT_Paratext5()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			Assert.IsFalse(m_importSettings.HasNonInterleavedBT);
			m_importSettings.AddFile("IDontExistButAtLeastIDontCrash.txt",
				ImportDomain.BackTrans, "es", 0);
			Assert.IsTrue(m_importSettings.HasNonInterleavedBT);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the HasNonInterleavedNotes property for a Paratext5 project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasNonInterleavedNotes_Paratext5()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			Assert.IsFalse(m_importSettings.HasNonInterleavedBT);
			m_importSettings.AddFile("IDontExistButAtLeastIDontCrash.txt",
				ImportDomain.Annotations, "es", m_inMemoryCache.m_translatorNoteDefn.Hvo);
			Assert.IsTrue(m_importSettings.HasNonInterleavedNotes);
		}
		#endregion

		#region BasicSettingsExist tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the BasicSettingsExist property works for Unknown import project
		/// types.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicSettingsExist_Unknown()
		{
			CheckDisposed();

			Assert.IsFalse(m_importSettings.BasicSettingsExist, "BasicSettingsExist should return false if Type of Import is Unknown.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the BasicSettingsExist property works for Paratext 6 projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicSettingsExist_Paratext6()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
				Assert.IsFalse(m_importSettings.BasicSettingsExist, "No project settings set yet");

				m_importSettings.ParatextBTProj = "KAM";
				m_importSettings.ParatextNotesProj = "TEV";
				m_importSettings.SaveSettings();
				Assert.IsFalse(m_importSettings.BasicSettingsExist, "Basic settings don't exist if the ParatextScrProj is not set");

				m_importSettings.ParatextBTProj = string.Empty;
				m_importSettings.ParatextScrProj = "KAM";
				Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist if the ParatextScrProj and ParatextNotesProj are set");

				m_importSettings.ParatextNotesProj = string.Empty;
				Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist if the ParatextScrProj is set");

				m_importSettings.ImportTypeEnum = TypeOfImport.Unknown;
				Assert.IsFalse(m_importSettings.BasicSettingsExist, "BasicSettingsExist should return false if Type of Import is Unknown.");
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the BasicSettingsExist property works for Other projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicSettingsExist_Other()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			Assert.IsFalse(m_importSettings.BasicSettingsExist, "No files added yet.");

			m_importSettings.AddFile(@"c:\BtFile.sf", ImportDomain.BackTrans, null, 0);
			Assert.IsFalse(m_importSettings.BasicSettingsExist, "Settings don't exist if only BT files have been added.");

			m_importSettings.AddFile(@"c:\NotesFile.sf", ImportDomain.Annotations, null, m_inMemoryCache.m_consultantNoteDefn.Hvo);
			Assert.IsFalse(m_importSettings.BasicSettingsExist, "Settings don't exist if only BT and notes files have been added.");

			m_importSettings.AddFile(@"c:\ScrFile.sf", ImportDomain.Main, null, 0);
			Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist once we add a Scripture file.");

			m_importSettings.RemoveFile(@"c:\BtFile.sf", ImportDomain.BackTrans, null, 0);
			m_importSettings.RemoveFile(@"c:\NotesFile.sf", ImportDomain.Annotations, null, m_inMemoryCache.m_consultantNoteDefn.Hvo);
			Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist if we only have Scripture files.");

			m_importSettings.ImportTypeEnum = TypeOfImport.Unknown;
			Assert.IsFalse(m_importSettings.BasicSettingsExist, "BasicSettingsExist should return false if Type of Import is Unknown.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the BasicSettingsExist property works for Paratext 5 projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicSettingsExist_Paretxt5()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			Assert.IsFalse(m_importSettings.BasicSettingsExist, "No files added yet.");

			m_importSettings.AddFile(@"c:\ScrFile.sf", ImportDomain.Main, null, 0);
			Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist once we add a Scripture file.");
		}
		#endregion

		#region Attempting to load Paratext project with missing files
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test attempting to load a Paratext project when the Paratext SSF references an
		/// encoding file that does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadParatextMappings_MissingEncodingFile()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
			Unpacker.UnPackMissingFileParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();

			try
			{
				m_importSettings.ParatextScrProj = "NEC";

				ScrMappingList mappings = ReflectionHelper.GetField(m_importSettings,
					"m_scrMappingsList") as ScrMappingList;

				Assert.AreEqual(null, m_importSettings.ParatextScrProj,
					"The Paratext project should not be set because the project NEC is missing" +
					"an encoding file.");
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextMissingFileTestProject();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test attempting to load a Paratext project when the Paratext SSF references a
		/// style file that does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadParatextMappings_MissingStyleFile()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
			Unpacker.UnPackMissingFileParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();

			try
			{
				m_importSettings.ParatextScrProj = "NSF";
				Assert.AreEqual(null, m_importSettings.ParatextScrProj,
					"The Paratext project should not be set because the project NSF is missing" +
					"a style file.");
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextMissingFileTestProject();
			}
		}
		#endregion

		#region ImportProjectIsAccessible tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for Paratext 6 projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportProjectIsAccessible_Paratext6()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				m_importSettings.ParatextScrProj = "KAM";
				m_importSettings.ParatextBTProj = "TEV";

				StringCollection filesNotFound;
				Assert.IsTrue(m_importSettings.ImportProjectIsAccessible(out filesNotFound));

				// Now blow away the KAM.ssf (i.e. vernacular project file) settings file and
				// check if it is still accessible. It should not be.
				File.Delete(Unpacker.PtProjectTestFolder + "KAM.ssf");

				Assert.IsFalse(m_importSettings.ImportProjectIsAccessible(out filesNotFound));

				// Also check that the file that's inaccessible is correct.
				Assert.AreEqual(1, filesNotFound.Count);
				Assert.IsTrue(((string)filesNotFound[0]).IndexOf("KAM") != -1);
			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for Paratext 5 projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportProjectIsAccessible_Paratext5()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			ImportProjectIsAccessible_helper();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for Other projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportProjectIsAccessible_Other()
		{
			CheckDisposed();

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			ImportProjectIsAccessible_helper();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify the next mapping in the collection that was created in the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyNextMapping(IEnumerator<IScrMarkerMapping> mappings, string beginMarker, string endMarker,
			MarkerDomain domain, string styleName, string ws, bool excluded, MappingTargetType target)
		{
			Assert.IsTrue(mappings.MoveNext());
			ScrMarkerMapping mapping = (ScrMarkerMapping)(ScrMarkerMapping)mappings.Current;

			Assert.AreEqual(beginMarker, mapping.BeginMarker, "Begin marker did not match for marker " + beginMarker);
			Assert.AreEqual(endMarker, mapping.EndMarker, "End marker did not match for marker " + beginMarker);
			Assert.AreEqual(domain, (MarkerDomain)mapping.Domain, "Domain did not match for marker " + beginMarker);
			if (styleName != null)
				Assert.AreEqual(styleName, mapping.StyleRA.Name, "Style name did not match for marker " + beginMarker);
			else
				Assert.AreEqual(0, mapping.StyleRAHvo, "Style should not have been set for marker " + beginMarker);
			Assert.AreEqual(ws, mapping.ICULocale, "ICU Locale did not match for marker " + beginMarker);
			Assert.AreEqual(excluded, mapping.Excluded, "Excluded state was wrong for marker " + beginMarker);
			Assert.AreEqual(target, (MappingTargetType)mapping.Target, "Mapping target type did not match for marker " + beginMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a binary blob representing "ECProject" values
		/// </summary>
		/// <returns>a byte array containing the settings</returns>
		/// ------------------------------------------------------------------------------------
		private byte[] GetEcProjectSettingsArray(ArrayList mappingList, ArrayList fileList)
		{
			BinarySettings blob = new BinarySettings("ECProject");

			// stuff at the start of the file
			blob.AddString(@"\id");
			blob.AddString(string.Empty); // data encoding
			blob.AddString(string.Empty); // marker encoding
			blob.AddString(string.Empty); // binary directory
			blob.AddString(string.Empty); // SSF file name
			blob.AddString(string.Empty); // STY file name

			// Marker mappings
			OutputMappings(blob, false, mappingList);

			// file list
			blob.AddInt(fileList.Count);
			foreach (string fileName in fileList)
			{
				blob.AddString(fileName);
				blob.AddIntShort(1);	// file encoding (dummy value)
				blob.AddIntShort(1);	// file encoding source (dummy value)
				blob.AddIntShort(100);	// percent certain
			}
			return blob.Finalize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the settings from a byte array or gets the settings into a byte array.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private byte[] GetParatextSettingsArray(ArrayList vernMappingList, ArrayList backMappingList,
			ArrayList notesMappingList, string vernProj, string backProj, string notesProj)
		{
			BinarySettings blob = new BinarySettings("PTProject");

			// Mapping set count (3 sets for vern, back, and notes)
			blob.AddInt(3);

			// We store the mappings in three different parts of the blob because the source P6 project
			// often determines the domain. It's possible for users to use the same markers in different
			// projects to mean different things. The domain + the marker serve as the "key".

			OutputMappings(blob, true, vernMappingList);
			OutputMappings(blob, true, backMappingList);
			OutputMappings(blob, true, notesMappingList);

			blob.AddString(vernProj);
			blob.AddString(backProj);
			blob.AddString(notesProj);

			return blob.Finalize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a list of mappings to the blob
		/// </summary>
		/// <param name="blob">The settings blob</param>
		/// <param name="fParatext">true if this is for Paratext</param>
		/// <param name="mappings">A collection of mappings that belong together in the blob (there
		/// are three such globs in a well-formed blob)</param>
		/// ------------------------------------------------------------------------------------
		private void OutputMappings(BinarySettings blob, bool fParatext, ArrayList mappings)
		{
			if (mappings == null)
			{
				blob.AddInt(0);
				return;
			}
			blob.AddInt(mappings.Count);
			foreach (MappingInfo mapping in mappings)
			{
				blob.AddString(mapping.beginMarker);
				blob.AddString(mapping.endMarker);
				blob.AddIntShort(mapping.isInline ? 1 : 0);	// inline
				blob.AddString(string.Empty);		// marker encoding
				blob.AddString(mapping.dataEncoding);
				blob.AddIntShort((int)mapping.domain);
				blob.AddString(mapping.styleName);
				blob.AddString(mapping.ws); // writing system
				blob.AddIntShort(1);	// confirmed
				// Looks like we accidentally got these two parameters in opposite order for
				// Paratext and ECProject - ugh!
				if (fParatext)
				{
					blob.AddIntShort((int)mapping.mappingTarget);
					blob.AddIntShort(mapping.isExcluded ? 1 : 0);
				}
				else
				{
					blob.AddIntShort(mapping.isExcluded ? 1 : 0);
					blob.AddIntShort((int)mapping.mappingTarget);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that the source contains the list of files given
		/// </summary>
		/// <param name="files"></param>
		/// <param name="domain"></param>
		/// ------------------------------------------------------------------------------------
		private void CheckFileListContents(string[] files, ImportDomain domain)
		{
			ImportFileSource source = m_importSettings.GetImportFiles(domain);
			bool[] found = new bool[files.Length];
			foreach (ScrImportFileInfo info in source)
			{
				bool foundFile = false;
				for (int index = 0; index < files.Length; index++)
				{
					if (info.FileName.ToUpper() == files[index].ToUpper())
					{
						Assert.IsFalse(found[index], "Found the " + domain + " file twice: " + files[index]);
						found[index] = foundFile = true;
						break;
					}
				}
				Assert.IsTrue(foundFile, "The file in " + domain + " domain was not found in the list of expected files: " + info.FileName);
			}
			for (int index = 0; index < found.Length; index++)
				Assert.IsTrue(found[index], "File not found in " + domain + " source: " + files[index]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for projects other than
		/// Paratext 6.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ImportProjectIsAccessible_helper()
		{
			Unpacker.UnPackSfTestProjects();
			try
			{
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "01GEN.sfm", ImportDomain.Main, null, 0);
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "02EXO.sfm", ImportDomain.Main, null, 0);
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "03LEV.sfm", ImportDomain.Main, null, 0);
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "39MAL.sfm", ImportDomain.BackTrans, null, 0);
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "32JON.sfm", ImportDomain.BackTrans, "es", 0);
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "41MAT.sfm", ImportDomain.Annotations, null, m_inMemoryCache.m_consultantNoteDefn.Hvo);
				m_importSettings.AddFile(Unpacker.SfProjectTestFolder + "67REV.sfm", ImportDomain.Annotations, null, m_inMemoryCache.m_translatorNoteDefn.Hvo);

				StringCollection filesNotFound;
				Assert.IsTrue(m_importSettings.ImportProjectIsAccessible(out filesNotFound));
				Assert.AreEqual(0, filesNotFound.Count);
				m_importSettings.SaveSettings();

				// Blow away some project files: should still return true, but should
				// report missing files.
				File.Delete(Unpacker.SfProjectTestFolder + "02EXO.sfm");
				File.Delete(Unpacker.SfProjectTestFolder + "03LEV.sfm");
				File.Delete(Unpacker.SfProjectTestFolder + "39MAL.sfm");
				File.Delete(Unpacker.SfProjectTestFolder + "32JON.sfm");
				File.Delete(Unpacker.SfProjectTestFolder + "67REV.sfm");

				m_importSettings = new ScrImportSet(Cache,
					Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

				Assert.IsTrue(m_importSettings.ImportProjectIsAccessible(out filesNotFound));
				Assert.AreEqual(5, filesNotFound.Count);

				foreach (string file in filesNotFound)
				{
					string sFile = file.ToLower();
					Assert.IsTrue(sFile.EndsWith("02exo.sfm") || sFile.EndsWith("03lev.sfm") ||
						sFile.EndsWith("39mal.sfm") || sFile.EndsWith("32jon.sfm") ||
						sFile.EndsWith("67rev.sfm"));
				}

				m_importSettings.SaveSettings();

				// Blow away the rest of the project files: should return false and report
				// missing files.
				File.Delete(Unpacker.SfProjectTestFolder + "01GEN.sfm");
				File.Delete(Unpacker.SfProjectTestFolder + "41MAT.sfm");

				m_importSettings = new ScrImportSet(Cache,
					Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.HvoArray[0]);

				Assert.IsFalse(m_importSettings.ImportProjectIsAccessible(out filesNotFound));
				Assert.AreEqual(7, filesNotFound.Count);

				foreach (string file in filesNotFound)
				{
					string sFile = file.ToLower();
					Assert.IsTrue(sFile.EndsWith("01gen.sfm") || sFile.EndsWith("02exo.sfm") ||
						sFile.EndsWith("03lev.sfm") || sFile.EndsWith("39mal.sfm") ||
						sFile.EndsWith("41mat.sfm") || sFile.EndsWith("32jon.sfm") ||
						sFile.EndsWith("67rev.sfm"));
				}
			}
			finally
			{
				Unpacker.RemoveSfTestProjects();
			}
		}
		#endregion
	}
}
