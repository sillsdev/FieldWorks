// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrImportSetTests.cs
// Responsibility: TE Team

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SILUBS.SharedScrUtils;

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

	#region DummyScrImportFileInfoFactory
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates mocked <see cref="IScrImportFileInfo"/> objects instead of the real thing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyScrImportFileInfoFactory : ScrImportFileInfoFactory
	{
		internal Dictionary<string, IScrImportFileInfo> m_mockedScrImportFinfos =
			new Dictionary<string, IScrImportFileInfo>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a mocked <see cref="IScrImportFileInfo"/>. This is used to build an
		/// in-memory list of files.
		/// </summary>
		/// <param name="fileName">Name of the file whose info this represents</param>
		/// <param name="mappingList">Sorted list of mappings to which newly found mappings
		/// should (and will) be added</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="wsId">The writing system identifier of the source to which this file
		/// belongs (null for Scripture source)</param>
		/// <param name="noteType">The CmAnnotationDefn of the source to which
		/// this file belongs (only used for Note sources)</param>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		public override IScrImportFileInfo Create(string fileName, ScrMappingList mappingList,
			ImportDomain domain, string wsId, ICmAnnotationDefn noteType, bool scanInlineBackslashMarkers)
		{
			IScrImportFileInfo info = MockRepository.GenerateStub<IScrImportFileInfo>();
			m_mockedScrImportFinfos[fileName] = info;
			info.Stub(x => x.FileName).Return(fileName);
			info.Stub(x => x.WsId).Return(wsId);
			info.Stub(x => x.NoteType).Return(noteType);
			return info;
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ScrImportSet class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_ptHelper gets disposed in FixtureTeardown()")]
	public class ScrImportSetTests : ScrInMemoryFdoTestBase
	{
		#region data members
		private IScrImportSet m_importSettings;
		private ICmAnnotationDefn m_translatorNoteDefn;
		private ICmAnnotationDefn m_consultantNoteDefn;
		private MockFileOS m_fileOs;
		#endregion

		#region Setup & Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setup stuff
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create(ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			m_scr.ImportSettingsOC.Add(m_importSettings);
			m_translatorNoteDefn = Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().TranslatorAnnotationDefn;
			m_consultantNoteDefn = Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().ConsultantAnnotationDefn;

			m_fileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_fileOs);
		}

		/// <summary/>
		public override void TestTearDown()
		{
			FileUtils.Manager.Reset();
			base.TestTearDown();
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
				new ImportMappingInfo(@"\rem", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_translatorNoteDefn));

			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\rem", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.Remark, null, m_consultantNoteDefn));
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
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

			// verify the settings
			IEnumerable scrMappings = m_importSettings.Mappings(MappingSet.Main);
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
						Assert.IsNull(mapping.WsId);
						break;

					case @"\b":
						Assert.AreEqual(@"\b*", mapping.EndMarker);
						Assert.AreEqual("Key Word", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\f":
						Assert.AreEqual(@"\f*", mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Footnote, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(true, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\ex":
						Assert.IsNull(mapping.EndMarker);
						Assert.IsNull(mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsTrue(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\greek":
						Assert.AreEqual(@"\greek*", mapping.EndMarker);
						Assert.AreEqual("Doxology", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("gr", mapping.WsId);
						break;

					case @"\fig":
						Assert.IsNull(mapping.EndMarker);
						Assert.IsNull(mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.Figure, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\c":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.ChapterNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\v":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.VerseNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\vt":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual("Default Paragraph Characters", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\id":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(null, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\bta":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\btb":
						Assert.AreEqual(@"\btb*", mapping.EndMarker);
						Assert.AreEqual("Key Word", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\btf":
						Assert.AreEqual(@"\btf*", mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans | MarkerDomain.Footnote, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(true, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						break;

					case @"\bta_es":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("es", mapping.WsId);
						break;

					case @"\btb_es":
						Assert.AreEqual(@"\btb_es*", mapping.EndMarker);
						Assert.AreEqual("Key Word", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("es", mapping.WsId);
						break;

					case @"\btf_es":
						Assert.AreEqual(@"\btf_es*", mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.BackTrans | MarkerDomain.Footnote, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(true, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.AreEqual("es", mapping.WsId);
						break;

					case @"\rem":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.Remark, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Note, mapping.Domain);
						Assert.AreEqual(false, mapping.IsExcluded);
						Assert.AreEqual(false, mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						Assert.AreEqual(m_translatorNoteDefn, mapping.NoteType);
						break;

					default:
						Assert.Fail();
						break;
				}
			}
			Assert.AreEqual(17, htCheck.Count);

			IEnumerable notesMappings = m_importSettings.Mappings(MappingSet.Notes);
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
						Assert.IsNull(mapping.WsId);
						Assert.AreEqual(m_consultantNoteDefn, mapping.NoteType);
						break;

					case @"\m":
						Assert.AreEqual(@"\m*", mapping.EndMarker);
						Assert.AreEqual("Emphasis", mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsTrue(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						Assert.IsNull(mapping.NoteType);
						break;

					case @"\c":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.ChapterNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						Assert.IsNull(mapping.NoteType);
						break;

					case @"\v":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(ScrStyleNames.VerseNumber, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain); //??
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
						Assert.IsNull(mapping.NoteType);
						break;

					case @"\id":
						Assert.IsNull(mapping.EndMarker);
						Assert.AreEqual(null, mapping.StyleName);
						Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
						Assert.IsFalse(mapping.IsExcluded);
						Assert.IsFalse(mapping.IsInline);
						Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
						Assert.IsNull(mapping.WsId);
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
				new ImportMappingInfo(@"\aa", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_consultantNoteDefn));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\cc", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_consultantNoteDefn));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\ee", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null, m_consultantNoteDefn));

			// save to the DB
			m_importSettings.SaveSettings();
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

			// Sanity check to verify the settings
			IEnumerable scrMappings = m_importSettings.Mappings(MappingSet.Main);
			Assert.IsNotNull(scrMappings);
			Assert.AreEqual(4, ((ScrMappingList)scrMappings).Count);

			IEnumerable notesMappings = m_importSettings.Mappings(MappingSet.Notes);
			Assert.IsNotNull(notesMappings);
			Assert.AreEqual(3, ((ScrMappingList)notesMappings).Count);

			// Replace "cc" in Main and "ee" in Notes
			m_importSettings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\cc", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));
			m_importSettings.SetMapping(MappingSet.Notes,
				new ImportMappingInfo(@"\ee", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null , m_translatorNoteDefn));

			// Modify existing "ee" in Main and "aa" in Notes
			ImportMappingInfo main_ee = m_importSettings.MappingForMarker(@"\ee", MappingSet.Main);
			main_ee.StyleName = ScrStyleNames.NormalFootnoteParagraph;
			ImportMappingInfo notes_aa = m_importSettings.MappingForMarker(@"\aa", MappingSet.Notes);
			notes_aa.StyleName = "Emphasis";

			// Delete "qq" in Main and "cc" in Notes
			ImportMappingInfo main_qq = m_importSettings.MappingForMarker(@"\qq", MappingSet.Main);
			ImportMappingInfo notes_cc = m_importSettings.MappingForMarker(@"\cc", MappingSet.Notes);
			m_importSettings.DeleteMapping(MappingSet.Main, main_qq);
			m_importSettings.DeleteMapping(MappingSet.Notes, notes_cc);

			// save to the DB
			m_importSettings.SaveSettings();
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

			// Make sure that all the changes were saved and restored
			ScrMappingList scrMappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);
			Assert.IsNotNull(scrMappingList);
			Assert.AreEqual(3, scrMappingList.Count);

			ScrMappingList notesMappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Notes);
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
			Assert.AreEqual(m_translatorNoteDefn.Guid.ToString(), mapping.NoteType.Guid.ToString());
		}
		#endregion

		#region Save And "Reload" Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the file lists for a Paratext 5 project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndReloadProjectFiles_P5()
		{
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			string scrFile = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 1", @"\v 1", @"\v 2");
			string btFile = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
			string annotationFile = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");

			m_importSettings.AddFile(scrFile, ImportDomain.Main, null, null);
			m_importSettings.AddFile(btFile, ImportDomain.BackTrans, "es", null);
			m_importSettings.AddFile(annotationFile, ImportDomain.Annotations, "de", m_consultantNoteDefn);

			// save to the DB
			m_importSettings.SaveSettings();
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

			// Test to see that the files were saved and loaded correctly
			Assert.AreEqual(TypeOfImport.Paratext5, m_importSettings.ImportTypeEnum);
			ImportFileSource source = m_importSettings.GetImportFiles(ImportDomain.Main);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
				Assert.AreEqual(scrFile, info.FileName);

			source = m_importSettings.GetImportFiles(ImportDomain.BackTrans);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(btFile, info.FileName);
				Assert.AreEqual("es", info.WsId);
			}

			source = m_importSettings.GetImportFiles(ImportDomain.Annotations);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(annotationFile, info.FileName);
				Assert.AreEqual("de", info.WsId);
				Assert.AreEqual(m_consultantNoteDefn, info.NoteType);
			}

			// While we're at it, let's test CheckForOverlappingFilesInRange. This should
			// do nothing since there are no overlaps -- it will throw an exception if an
			// overlap is detected.
			m_importSettings.CheckForOverlappingFilesInRange(
				new ScrReference(1, 1, 1, ScrVers.English),
				new ScrReference(66, 21, 8, ScrVers.English));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the settings after changing import type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveAndReloadSources_SaveSeparateSourcesWhenImportTypeChanges()
		{
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			string scrFile = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 1", @"\v 1", @"\v 2");
			string btFile = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
			string annotationFile = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");

			m_importSettings.AddFile(scrFile, ImportDomain.Main, null, null);
			m_importSettings.AddFile(btFile, ImportDomain.BackTrans, "es", null);
			m_importSettings.AddFile(annotationFile, ImportDomain.Annotations, "de", m_consultantNoteDefn);

			// save to the DB
			m_importSettings.SaveSettings();
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;
			m_importSettings.ParatextScrProj = "KAM";
			m_importSettings.SaveSettings();
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			// Test to see that the files survived the type change
			Assert.AreEqual(TypeOfImport.Paratext5, m_importSettings.ImportTypeEnum);
			ImportFileSource source = m_importSettings.GetImportFiles(ImportDomain.Main);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
				Assert.AreEqual(scrFile, info.FileName);

			source = m_importSettings.GetImportFiles(ImportDomain.BackTrans);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(btFile, info.FileName);
				Assert.AreEqual("es", info.WsId);
			}

			source = m_importSettings.GetImportFiles(ImportDomain.Annotations);
			Assert.AreEqual(1, source.Count);
			foreach (ScrImportFileInfo info in source)
			{
				Assert.AreEqual(annotationFile, info.FileName);
				Assert.AreEqual("de", info.WsId);
				Assert.AreEqual(m_consultantNoteDefn, info.NoteType);
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			string[] filesScr = new string[5];
			// vernacular files
			filesScr[0] = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 1", @"\v 1", @"\v 2");
			filesScr[1] = m_fileOs.MakeSfFile("EXO", @"\p", @"\c 3", @"\v 1");
			filesScr[2] = m_fileOs.MakeSfFile("LEV", @"\p", @"\c 2", @"\v 1");
			filesScr[3] = m_fileOs.MakeSfFile("NUM", @"\p", @"\c 1", @"\v 1");
			filesScr[4] = m_fileOs.MakeSfFile("DEU", @"\p", @"\c 1", @"\v 1", @"\v 2");

			// BT files
			string[] filesBT = new string[3];
			filesBT[0] = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
			filesBT[1] = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
			filesBT[2] = m_fileOs.MakeSfFile("MAT", @"\p", @"\c 1", @"\v 1");

			// notes files
			string[] filesNotes = new string[3];
			filesNotes[0] = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
			filesNotes[1] = m_fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
			filesNotes[2] = m_fileOs.MakeSfFile("DEU", @"\p", @"\c 1", @"\v 1");

			// add Scripture files
			foreach (string fileName in filesScr)
				m_importSettings.AddFile(fileName, ImportDomain.Main, null, null);

			// make one of the file names upper case to test case sensitivity
			m_importSettings.AddFile(filesScr[2].ToUpper(), ImportDomain.Main, null, null);

			// add BT file with different ICU locales
			m_importSettings.AddFile(filesBT[0], ImportDomain.BackTrans, "en", null);
			m_importSettings.AddFile(filesBT[1], ImportDomain.BackTrans, "es", null);
			m_importSettings.AddFile(filesBT[2], ImportDomain.BackTrans, "es", null);

			// add Notes files with different ICU locales and note type
			m_importSettings.AddFile(filesNotes[0], ImportDomain.Annotations, null, m_translatorNoteDefn);
			m_importSettings.AddFile(filesNotes[1], ImportDomain.Annotations, "es", m_translatorNoteDefn);
			m_importSettings.AddFile(filesNotes[2], ImportDomain.Annotations, "de", m_consultantNoteDefn);

			// save to the DB
			m_importSettings.SaveSettings();
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);

			// add Scripture files
			m_importSettings.ParatextScrProj = "KAM";
			m_importSettings.ParatextBTProj = "TEV";

			mappingList.Add(new ImportMappingInfo(@"\c", null, ScrStyleNames.ChapterNumber));
			mappingList.Add(new ImportMappingInfo(@"\v", null, ScrStyleNames.VerseNumber));
			mappingList.Add(new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));
			mappingList.Add(new ImportMappingInfo(@"\utw", MarkerDomain.BackTrans, ScrStyleNames.UntranslatedWord, null, null));

			// save to the DB
			m_importSettings.SaveSettings();
			// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
			m_importSettings.RevertToSaved();

			// Test to see that the projects are set correctly
			Assert.AreEqual(TypeOfImport.Paratext6, m_importSettings.ImportTypeEnum);
			Assert.AreEqual("KAM", m_importSettings.ParatextScrProj);
			Assert.AreEqual("TEV", m_importSettings.ParatextBTProj);

			Assert.AreEqual(4, mappingList.Count);

			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\c"].Domain);
			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\v"].Domain);
			Assert.AreEqual(MappingTargetType.Figure, mappingList[@"\fig"].MappingTarget);
			Assert.AreEqual(MarkerDomain.BackTrans, mappingList[@"\utw"].Domain);
			Assert.AreEqual(ScrStyleNames.UntranslatedWord, mappingList[@"\utw"].StyleName);

			mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Notes);
			Assert.AreEqual(0, mappingList.Count);
		}
		#endregion

		#region AddFile tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AddFile uses the ScrImportFileInfoFactory to create an IScrImportFileInfo
		/// object and adds it to the correct list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFile_Main()
		{
			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			DummyScrImportFileInfoFactory factory = new DummyScrImportFileInfoFactory();
			ReflectionHelper.SetField(m_importSettings, "m_scrImpFinfoFact", factory);

			m_importSettings.AddFile("file1", ImportDomain.Main, null, null);
			Assert.IsTrue(factory.m_mockedScrImportFinfos.ContainsKey("file1"));
			Assert.AreEqual(factory.m_mockedScrImportFinfos["file1"],
				((ScrSfFileList)ReflectionHelper.GetField(m_importSettings, "m_scrFileInfoList"))[0]);
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Other;
			m_importSettings.AddFile(@"q:\wugga\bugga\slugga.hhh", ImportDomain.Main, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-505
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFile_AddingExistingFile()
		{
			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			string fileName1 = m_fileOs.MakeSfFile("EPH", @"\c 1", @"\v 1");
			string fileName2 = m_fileOs.MakeSfFile("EPH", @"\c 1", @"\v 1");
			string fileName3 = m_fileOs.MakeSfFile("EPH", @"\c 1", @"\v 1");
			string fileName4 = m_fileOs.MakeSfFile("EPH", @"\c 1", @"\v 1");
			string fileName5 = m_fileOs.MakeSfFile("EPH", @"\c 1", @"\v 1");

			m_importSettings.AddFile(fileName1, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName2, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName3, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName4, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName5, ImportDomain.Main, null, null);

			// make sure the file count is correct
			Assert.IsNotNull(m_importSettings.GetImportFiles(ImportDomain.Main).Count);

			// re-add the same 5 files
			m_importSettings.AddFile(fileName1, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName2, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName3, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName4, ImportDomain.Main, null, null);
			m_importSettings.AddFile(fileName5, ImportDomain.Main, null, null);

			// make sure the file count is still correct
			Assert.IsNotNull(m_importSettings.GetImportFiles(ImportDomain.Main).Count);
		}
		#endregion

		#region Mapping-in-use tests
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			string fileName = m_fileOs.MakeSfFile("GEN", @"\c 1", @"\v 1");

			m_importSettings.AddFile(fileName, ImportDomain.Main, null, null);

			m_importSettings.SetMapping(MappingSet.Main, new ImportMappingInfo("\\tom",
					"\\*tom", false, MappingTargetType.TEStyle, MarkerDomain.Default, "bogleMarker",
					null, null, true, ImportDomain.Main));

			m_importSettings.RemoveFile(fileName, ImportDomain.Main, null, null);

			ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);

			foreach (ImportMappingInfo mapping in mappingList)
				Assert.IsFalse(mapping.IsInUse);
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			ScrMappingList mappingListMain = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);
			ScrMappingList mappingListNotes = (ScrMappingList)m_importSettings.Mappings(MappingSet.Notes);

			// set Scripture project
			m_importSettings.ParatextScrProj = "TEV";
			m_importSettings.ParatextNotesProj = "KAM";

			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			foreach (ImportMappingInfo mapping in mappingListMain)
				Assert.IsFalse(mapping.IsInUse, "Unexpected InUse flag on mapping in main mapping list");

			foreach (ImportMappingInfo mapping in mappingListNotes)
				Assert.IsFalse(mapping.IsInUse, "Unexpected InUse flag on mapping in annotations mapping list");
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			string fileName = m_fileOs.MakeSfFile("MAT", @"\p", @"\c 1", @"\v 1 \em Wow!\em*");

			m_importSettings.AddFile(fileName, ImportDomain.Main, null, null);
			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Main);

			ImportMappingInfo inlineMapping = mappingList[@"\em"];
			Assert.IsFalse(inlineMapping.IsInUse);
			ImportMappingInfo mapping = mappingList[@"\c"];
			Assert.IsTrue(mapping.IsInUse);
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			string filename = m_fileOs.MakeSfFile("MAT", @"\p", @"\c 1", @"\v 1", @"\rem \em Wow!\em*");

			m_importSettings.AddFile(filename, ImportDomain.Annotations, "en", m_consultantNoteDefn);
			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			ScrMappingList mappingList = (ScrMappingList)m_importSettings.Mappings(MappingSet.Notes);

			ImportMappingInfo inlineMapping = mappingList[@"\em"];
			Assert.IsFalse(inlineMapping.IsInUse, "Expected false, received true 5 To Ann");
			ImportMappingInfo mapping = mappingList[@"\rem"];
			Assert.IsTrue(mapping.IsInUse, "Expected true, received false 5 To Ann");
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			m_importSettings.ParatextBTProj = "KAM";
			m_importSettings.ParatextNotesProj = "KAM";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that settings are not discarded and when changing project type from P6 to P5
		/// (See comments in TE-2002)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SwitchFromParatext6ToParatext5Project()
		{
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			ScrMappingList mappings = m_importSettings.GetMappingListForDomain(ImportDomain.Main);

			m_importSettings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\q", null, "Qute"));

			m_importSettings.ParatextBTProj = "KAM";
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			m_importSettings.AddFile("IDontExistButAtLeastIDontCrash.txt", ImportDomain.Main, null, null);

			Assert.AreEqual("KAM", m_importSettings.ParatextBTProj, "Project ID should not get wiped out");
			Assert.AreEqual("Qute", mappings[@"\q"].StyleName, "Mappings should not get wiped out");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the HasNonInterleavedBT property for a Paratext5 project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasNonInterleavedBT_Paratext5()
		{
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			Assert.IsFalse(m_importSettings.HasNonInterleavedBT);
			m_importSettings.AddFile("IDontExistButAtLeastIDontCrash.txt",
				ImportDomain.BackTrans, "es", null);
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			Assert.IsFalse(m_importSettings.HasNonInterleavedBT);
			m_importSettings.AddFile("IDontExistButAtLeastIDontCrash.txt",
				ImportDomain.Annotations, "es", m_translatorNoteDefn);
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
			Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist if only the ParatextScrProj is set");

			m_importSettings.ImportTypeEnum = TypeOfImport.Unknown;
			Assert.IsFalse(m_importSettings.BasicSettingsExist, "BasicSettingsExist should return false if Type of Import is Unknown.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the BasicSettingsExist property works for Other projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicSettingsExist_Other()
		{
			m_importSettings.ImportTypeEnum = TypeOfImport.Other;

			Assert.IsFalse(m_importSettings.BasicSettingsExist, "No files added yet.");

			m_importSettings.AddFile(@"c:\BtFile.sf", ImportDomain.BackTrans, null, null);
			Assert.IsFalse(m_importSettings.BasicSettingsExist, "Settings don't exist if only BT files have been added.");

			m_importSettings.AddFile(@"c:\NotesFile.sf", ImportDomain.Annotations, null, m_consultantNoteDefn);
			Assert.IsFalse(m_importSettings.BasicSettingsExist, "Settings don't exist if only BT and notes files have been added.");

			m_importSettings.AddFile(@"c:\ScrFile.sf", ImportDomain.Main, null, null);
			Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist once we add a Scripture file.");

			m_importSettings.RemoveFile(@"c:\BtFile.sf", ImportDomain.BackTrans, null, null);
			m_importSettings.RemoveFile(@"c:\NotesFile.sf", ImportDomain.Annotations, null, m_consultantNoteDefn);
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
			m_importSettings.ImportTypeEnum = TypeOfImport.Paratext5;

			Assert.IsFalse(m_importSettings.BasicSettingsExist, "No files added yet.");

			m_importSettings.AddFile(@"c:\ScrFile.sf", ImportDomain.Main, null, null);
			Assert.IsTrue(m_importSettings.BasicSettingsExist, "Basic settings exist once we add a Scripture file.");
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
			IScrMarkerMapping mapping = mappings.Current;

			Assert.AreEqual(beginMarker, mapping.BeginMarker, "Begin marker did not match for marker " + beginMarker);
			Assert.AreEqual(endMarker, mapping.EndMarker, "End marker did not match for marker " + beginMarker);
			Assert.AreEqual(domain, (MarkerDomain)mapping.Domain, "Domain did not match for marker " + beginMarker);
			if (styleName != null)
				Assert.AreEqual(styleName, mapping.StyleRA.Name, "Style name did not match for marker " + beginMarker);
			else
				Assert.IsNull(mapping.StyleRA, "Style should not have been set for marker " + beginMarker);
			Assert.AreEqual(ws, mapping.WritingSystem, "Writing system did not match for marker " + beginMarker);
			Assert.AreEqual(excluded, mapping.Excluded, "Excluded state was wrong for marker " + beginMarker);
			Assert.AreEqual(target, (MappingTargetType)mapping.Target, "Mapping target type did not match for marker " + beginMarker);
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
		#endregion
	}
}
