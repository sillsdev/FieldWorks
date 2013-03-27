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
// File: TeImportTest.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using NUnit.Framework;

using Rhino.Mocks;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;

using SILUBS.SharedScrUtils;

#if __MonoCS__
#pragma warning disable 419 // ambiguous reference; mono bug #639867
#endif

namespace SIL.FieldWorks.TE.ImportTests
{
	#region DummyTeImporter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class for the <see cref="TeImporter"/> so we can test it.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyTeImporter : TeSfmImporter
	{
		/// <summary></summary>
		public static ICmAnnotationDefn s_consultantNoteDefn = null;
		/// <summary></summary>
		public static ICmAnnotationDefn s_translatorNoteDefn = null;
		/// <summary>Tests can set this to simulate importing a sequence of segments</summary>
		public List<string> m_SegmentMarkers = null;
		private static IParatextAdapter s_mockParatextAdapter;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor to use when using an in-memory cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyTeImporter(IScrImportSet settings, FdoTestBase testBase,
			FwStyleSheet styleSheet) :
			base(settings, testBase.Cache, styleSheet, new DummyUndoImportManager(testBase),
				new TeImportNoUi())
		{
		}
		#endregion

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_importCallbacks != null)
					m_importCallbacks.Dispose();
			}
			m_importCallbacks = null;
			base.Dispose(disposing);
		}

		#region Static data setup methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate settings as if user had used the Import Wizard to choose a SF project
		/// and set up mappings appropriate for TOB data files.
		/// </summary>
		/// <param name="settings">The settings object to be populated</param>
		/// ------------------------------------------------------------------------------------
		static public void MakeSFImportTestSettings(IScrImportSet settings)
		{
			settings.ImportTypeEnum = TypeOfImport.Other;

			// add a bogus file to the project
			settings.AddFile(DriveUtil.BootDrive + @"IDontExist.txt", ImportDomain.Main, null, null);

			// Set up the mappings
			SetUpMappings(settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate settings as if user had used the Import Wizard to choose a Paratext
		/// project and set up mappings.
		/// </summary>
		/// <param name="settings">The settings object to be populated</param>
		/// ------------------------------------------------------------------------------------
		static public void MakeParatextTestSettings(IScrImportSet settings)
		{
			// Setup mocked Paratext projects
			s_mockParatextAdapter = MockRepository.GenerateMock<IParatextAdapter>();
			ReflectionHelper.SetField(settings, "m_paratextAdapter", s_mockParatextAdapter);
			s_mockParatextAdapter.Stub(x => x.LoadProjectMappings(Arg<string>.Is.Anything,
				Arg<ScrMappingList>.Is.Anything, Arg<ImportDomain>.Is.Anything)).Return(true);

			settings.ImportTypeEnum = TypeOfImport.Paratext6;
			settings.ParatextScrProj = "TEV";
			settings.ParatextBTProj = "KAM";
			settings.ParatextNotesProj = "HMM";

			SetUpMappings(settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up appropriate mappings for the TEV and TOB data files.
		/// </summary>
		/// <param name="settings">The settings object to be set up</param>
		/// ------------------------------------------------------------------------------------
		static public void SetUpMappings(IScrImportSet settings)
		{
			// Map styles without end markers
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\b", null, "Stanza Break"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\c", null, ScrStyleNames.ChapterNumber));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\inp", null, "Block Quote Paragraph"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ip", null, "Intro Paragraph"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\is", null, "Intro Section Head"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ip_", null, "Background Paragraph"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\is_", null, "UnknownTEStyle"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\imt", null, "Intro Title Main"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ist", null, "Intro Title Secondary"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\iot", null, "Intro Section Head"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\io1", null, "Intro List Item1"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\io2", null, "Intro List Item2"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\li1", null, "List Item1"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\li2", null, "List Item2"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\li3", null, "List Item3"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\p", null, "Paragraph"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\m", null, "Paragraph Continuation"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\mr", null, "Section Range Paragraph"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ms", null, "Section Head Major"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\mt", null, "Title Main"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\mt", null, "Title Main"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\mt1", null, "Title Main"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\q", null, "Line1"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\q2", null, "Line2"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\q3", null, "Line3"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\qc", null, "Doxology"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\r", null, "Parallel Passage Reference"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\s", null, ScrStyleNames.SectionHead));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\s2", null, "Section Head Minor"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\v", null, ScrStyleNames.VerseNumber));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\vt", null, "Default Paragraph Characters"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\h", null, false, MappingTargetType.TitleShort, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\fig", null, false, MappingTargetType.Figure, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\cap", null, false, MappingTargetType.FigureCaption, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\cat", null, false, MappingTargetType.FigureFilename, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figcap", null, false, MappingTargetType.FigureCaption, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figcat", null, false, MappingTargetType.FigureFilename, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figcopy", null, false, MappingTargetType.FigureCopyright, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figdesc", null, false, MappingTargetType.FigureDescription, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figdesc_es", null, false, MappingTargetType.FigureDescription, MarkerDomain.Default, null, "es"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figlaypos", null, false, MappingTargetType.FigureLayoutPosition, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figrefrng", null, false, MappingTargetType.FigureRefRange, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\figscale", null, false, MappingTargetType.FigureScale, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\vern", MarkerDomain.Note, "Default Paragraph Characters", "qaa-x-kal", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\rt", MarkerDomain.Note, "Default Paragraph Characters", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\de", MarkerDomain.Default, "Emphasis", "de", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\em", @"\em*", "Emphasis"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\mt2", null, "Title Secondary"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\mt2_", null, "Book Title Secondary")); // Not a real style
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\st", null, "Title Secondary"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\st2", null, "Title Tertiary"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\st3", null, "Title Tertiary"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\quot", null, "Quoted Text"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\h_", null, "Header & Footer"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\bogus", null, true, MappingTargetType.TEStyle, MarkerDomain.Default, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\fr", MarkerDomain.Footnote, ScrStyleNames.FootnoteTargetRef, null, null));
			// TE-7718 - Added ending marker for \ft so that test data could include it - the ending marker
			// is added by Paratext if user sets selected text to the "ft" style.
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\ft*", MarkerDomain.Footnote, "Default Paragraph Characters", null, null));

			if (settings.HasNonInterleavedNotes)
			{
				settings.SetMapping(MappingSet.Notes, new ImportMappingInfo(@"\c", null, ScrStyleNames.ChapterNumber));
				settings.SetMapping(MappingSet.Notes, new ImportMappingInfo(@"\v", null, ScrStyleNames.VerseNumber));
				settings.SetMapping(MappingSet.Notes, new ImportMappingInfo(@"\rem", MarkerDomain.Default, ScrStyleNames.Remark, "en", null));
				settings.SetMapping(MappingSet.Notes, new ImportMappingInfo(@"\crem", MarkerDomain.Default, ScrStyleNames.Remark, "en", s_consultantNoteDefn));
				// need one character style for testing problem reported in TE-5071
				settings.SetMapping(MappingSet.Notes, new ImportMappingInfo(@"\em", @"\em*", "Emphasis"));
			}

			// Map styles with end markers; Note that although we use some of these markers in import tests
			// which purport to be "Other" SF, these tests really simulate segments as they would be mapped
			// coming from P6, since TE's UI for "Other" currently doesn't allow the user to indicate end
			// markers for normal backslash markers, only for in-line markers (which don't typically start
			// with a backslash in Shoebox/Toolbox data).

			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\bk", @"\bk*", ScrStyleNames.BookTitleInText));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\kw", @"\kw*", "Key Word"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\uw", @"\uw*", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "Untranslated Word", "qaa-x-kal"));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\gls", @"\gls*", "Gloss"));

			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\rem", MarkerDomain.Note,
				ScrStyleNames.Remark, "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\crem", MarkerDomain.Note,
				ScrStyleNames.Remark, "en", s_consultantNoteDefn));

			if (settings.ImportTypeEnum == TypeOfImport.Other)
			{
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\f", null, false, MappingTargetType.TEStyle,
					MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, null));
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\x", null, false, MappingTargetType.TEStyle,
					MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, null));
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\fte", null,
					"Default Paragraph Characters")); // Footnote end
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|fm ", "|fm*", MarkerDomain.Footnote,
					ScrStyleNames.FootnoteMarker, null, null));
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|ft ", "|ft*", MarkerDomain.Footnote,
					"Default Paragraph Characters", null, null));
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|fr ", "|fr*", MarkerDomain.Footnote,
					ScrStyleNames.FootnoteTargetRef, null, null));
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\fq", null, MarkerDomain.Footnote,
					"Quoted Text", null, null));
			}
			else
			{
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\f", @"\f*", false, MappingTargetType.TEStyle,
					MarkerDomain.Footnote, ScrStyleNames.NormalFootnoteParagraph, null, null));
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\x", @"\x*", false, MappingTargetType.TEStyle,
					MarkerDomain.Footnote, ScrStyleNames.CrossRefFootnoteParagraph, null, null));
				settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\fq", @"\fq*", MarkerDomain.Footnote,
					"Quoted Text", null, null));
			}

			// Map styles for Back-Translation domain
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btip", MarkerDomain.BackTrans, "Intro Paragraph", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btvt", MarkerDomain.BackTrans, "Default Paragraph Characters", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btv", MarkerDomain.BackTrans, "Verse Number", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btvt_default", MarkerDomain.BackTrans, "Default Paragraph Characters", null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btvt_de", MarkerDomain.BackTrans, "Default Paragraph Characters", "de", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btvt_es", MarkerDomain.BackTrans, "Default Paragraph Characters", "es", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btvt_default", MarkerDomain.BackTrans, "Default Paragraph Characters", null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btis", MarkerDomain.BackTrans, "Intro Section Head", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btp", MarkerDomain.BackTrans, "Paragraph", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btmt", MarkerDomain.BackTrans, "Title Main", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btst", MarkerDomain.BackTrans, "Title Secondary", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btq", MarkerDomain.BackTrans, "Line1", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btq2", MarkerDomain.BackTrans, "Line2", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btr", MarkerDomain.BackTrans, "Parallel Passage Reference", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\bts", MarkerDomain.BackTrans, "Section Head", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\bts2", MarkerDomain.BackTrans, "Section Head Minor", "en", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btvw", MarkerDomain.BackTrans, "Untranslated Word", "qaa-x-kal", null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btft", MarkerDomain.Footnote | MarkerDomain.BackTrans,
				"Default Paragraph Characters", null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btf", MarkerDomain.Footnote | MarkerDomain.BackTrans,
				ScrStyleNames.NormalFootnoteParagraph, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btfig", null, false,
				MappingTargetType.Figure, MarkerDomain.BackTrans, null, null)); // Figure is treated just like FigureCaption in the BT domain
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btcap", null, false,
				MappingTargetType.FigureCaption, MarkerDomain.BackTrans, null, null));
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btfigcopy", null, false,
				MappingTargetType.FigureCopyright, MarkerDomain.BackTrans, null, null));
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.m_importDomain"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportDomain CurrentImportDomain
		{
			get
			{
				CheckDisposed();
				return m_importDomain;
			}
			set
			{
				CheckDisposed();
				m_importDomain = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.m_CurrParaFootnotes"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<FootnoteInfo> CurrParaFootnotes
		{
			get
			{
				CheckDisposed();
				return m_CurrParaFootnotes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeImporter.m_currSection"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection CurrentSection
		{
			get
			{
				CheckDisposed();
				return m_currSection;
			}
			set
			{
				CheckDisposed();
				m_currSection = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.GetVerseRefAsString"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new string GetVerseRefAsString(int wsBt)
		{
			CheckDisposed();

			return base.GetVerseRefAsString(wsBt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.m_settings"/> property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrImportSet Settings
		{
			get
			{
				CheckDisposed();
				return m_settings;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeImporter.m_nBookNumber"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BookNumber
		{
			get
			{
				CheckDisposed();
				return m_nBookNumber;
			}
			set
			{
				CheckDisposed();
				m_nBookNumber = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.m_nChapter"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Chapter
		{
			get
			{
				CheckDisposed();
				return m_nChapter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the length of the <see cref="NormalParaStrBldr"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParaBldrLength
		{
			get
			{
				CheckDisposed();

				return NormalParaStrBldr.Length;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="StTxtParaBldr.StringBuilder"/> property for the builder that
		/// is being used to construct normal (non-footnote) paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsStrBldr NormalParaStrBldr
		{
			get
			{
				CheckDisposed();

				if (m_fInFootnote)
					return m_SavedParaBldr.StringBuilder;
				else
					return m_ParaBldr.StringBuilder;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="StTxtParaBldr.StringBuilder"/> property for the builder that
		/// is being used to construct footnote paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsStrBldr FootnoteParaStrBldr
		{
			get
			{
				CheckDisposed();

				if (m_fInFootnote)
					return m_ParaBldr.StringBuilder;
				else
					return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Dictionary of <see cref="ITsStrBldr"/>
		/// used to construct back-trans paragraph strings (non-footnote paragraphs).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<int, ITsStrBldr> BtStrBldrs
		{
			get
			{
				CheckDisposed();

				return m_BTStrBldrs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeImporter.m_CurrFootnote"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote CurrentFootnote
		{
			get
			{
				CheckDisposed();
				return m_CurrFootnote;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="DummyScrObjWrapper.TextSegment"/> property.
		/// This is used by tests that need to set scReference values for the current segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ISCTextSegment TextSegment
		{
			get
			{
				CheckDisposed();
				return ((DummyScrObjWrapper)m_SOWrapper).TextSegment;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeImporter.m_scrBook"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrBook ScrBook
		{
			get
			{
				CheckDisposed();
				return m_scrBook;
			}
			set
			{
				CheckDisposed();
				m_scrBook = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoTitle
		{
			get
			{
				CheckDisposed();
				return m_scrBook.TitleOA.Hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStText SectionHeading
		{
			get
			{
				CheckDisposed();
				return m_currSection.HeadingOA;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStText SectionContent
		{
			get
			{
				CheckDisposed();
				return m_currSection.ContentOA;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.m_styleProxies"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, ImportStyleProxy> HtStyleProxy
		{
			get
			{
				CheckDisposed();
				return m_styleProxies;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeImporter.m_undoManager"/> variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UndoImportManager UndoInfo
		{
			get
			{
				CheckDisposed();

				return m_undoManager;
			}
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that StText field for an annotation was initialized properly.
		/// </summary>
		/// <param name="text">given StText</param>
		/// <param name="fieldName">name of field</param>
		/// ------------------------------------------------------------------------------------
		public void VerifyInitializedNoteText(IStText text, string fieldName)
		{
			VerifyAnnotationText(text, fieldName, null, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that StText field for an annotation was initialized properly.
		/// </summary>
		/// <param name="text">given StText</param>
		/// <param name="fieldName">name of field</param>
		/// <param name="expectedContents">Expected contents of (first and only) para (may be
		/// null)</param>
		/// <param name="expectedWs">Expected writing system ID (ignored if expectedContents is
		/// null)</param>
		/// ------------------------------------------------------------------------------------
		public void VerifyAnnotationText(IStText text, string fieldName,
			string expectedContents, int expectedWs)
		{
			Assert.AreEqual(1, text.ParagraphsOS.Count, fieldName + " should have 1 para");
			IStTxtPara para = (IStTxtPara)text.ParagraphsOS[0];
			Assert.IsNotNull(para.StyleRules, fieldName + " should have a para style.");
			// We do not care about style for annotations because they get changed when displayed.
			//Assert.AreEqual(ScrStyleNames.Remark,
			//    para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
			//    fieldName + " should use Remark style.");
			if (expectedContents == null)
				Assert.IsNull(para.Contents.Text, fieldName + " should have 1 empty para.");
			else
			{
				ITsString tss = para.Contents;
				Assert.AreEqual(1, tss.RunCount);
				AssertEx.RunIsCorrect(tss, 0, expectedContents, null, expectedWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote has been created in the DB with a single default run having
		/// the specified text.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// <param name="sFootnoteSegment">Expected footnote contents</param>
		/// <param name="iAutoNumberedFootnoteIndex">zero-based index of this footnote
		/// in list of all auto-numbered footnotes. This could the be the third footnote
		/// overall, but only the second auto-numbered footnote.</param>
		/// <param name="sMarker">One of: <c>"a"</c>, for automatic alpha sequence; <c>"*"</c>,
		/// for a literal marker (we always use "*" in these tests); or <c>string.Empty</c>,
		/// for no marker</param>
		/// <param name="sParaStyleName">Name of the paragraph style</param>
		/// <param name="runCount">Number of runs expected in the footnote para</param>
		/// ------------------------------------------------------------------------------------
		public ITsString VerifySimpleFootnote(int iFootnoteIndex, string sFootnoteSegment,
			int iAutoNumberedFootnoteIndex, string sMarker, string sParaStyleName, int runCount)
		{
			IStFootnote footnote = GetFootnote(iFootnoteIndex);
			//if (sMarker == "a")
			//    sMarker = new string((char)((int)'a' + (iAutoNumberedFootnoteIndex % 26)), 1);
			if (sMarker != "a")
			{
				if (sMarker == null)
					Assert.IsNull(footnote.FootnoteMarker.Text);
				else
				{
					AssertEx.RunIsCorrect(footnote.FootnoteMarker, 0,
						sMarker, ScrStyleNames.FootnoteMarker, m_wsVern);
				}
			}
			IFdoOwningSequence<IStPara> footnoteParas = footnote.ParagraphsOS;
			Assert.AreEqual(1, footnoteParas.Count);
			IStTxtPara para = (IStTxtPara)footnoteParas[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(sParaStyleName), para.StyleRules);
			ITsString tss = para.Contents;
			Assert.AreEqual(runCount, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, sFootnoteSegment, null, m_wsVern);
			return tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates the user stopping an import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void StopImport()
		{
			CheckDisposed();

			try
			{
				base.StopImport();
			}
			catch (Common.Controls.CancelException)
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph with the specified style id, containing the specified verse number,
		/// if specified, and having the correct sequence number. Sets all appropriate state
		/// variables before returning.
		/// </summary>
		/// <param name="style">style of paragraph to find</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="iPara">0-based index of paragraph</param>
		/// <returns>The corrersponding IStTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara FindCorrespondingVernParaForSegment(IStStyle style,
			BCVRef targetRef, int iPara)
		{
			CheckDisposed();

			m_iNextBtPara = iPara;
			bool fDummy;
			return base.FindCorrespondingVernParaForSegment(style, targetRef, out fDummy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the requested footnote.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// ------------------------------------------------------------------------------------
		public IStFootnote GetFootnote(int iFootnoteIndex)
		{
			CheckDisposed();

			IFdoOwningSequence<IScrFootnote> footnotes = ScrBook.FootnotesOS;
			Assert.IsTrue(iFootnoteIndex < footnotes.Count, "iFootnoteIndex is out of range");
			return footnotes[iFootnoteIndex];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.Initialize"/> method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void Initialize()
		{
			CheckDisposed();

			base.Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the TeImporter.Import method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void Import()
		{
			CheckDisposed();

			base.Import();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.FindCorrespondingFootnote(string)"/> method
		/// </summary>
		/// <param name="ws">The writing system of the current BT</param>
		/// <param name="styleId">style of footnote to find</param>
		/// <returns>found footnote, or null if corrersponding footnote of styleId is not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindCorrespondingFootnote(int ws, string styleId)
		{
			CheckDisposed();

			base.m_wsCurrBtPara = ws;
			return base.FindCorrespondingFootnote(styleId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.ProcessSegment"/> method
		/// </summary>
		/// <param name="sText">text to import</param>
		/// <param name="sMarker">standard format marker</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessSegment(string sText, string sMarker)
		{
			CheckDisposed();

			m_sSegmentText = sText;
			m_sMarker = sMarker;
			base.ProcessSegment();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.FinalizeImport"/> method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void FinalizeImport()
		{
			CheckDisposed();

			base.FinalizeImport();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.AddTextToPara(string, ITsTextProps)"/> method
		/// </summary>
		/// <param name="sText">Text to be appended to the paragraph being built</param>
		/// <param name="pttpProps">Properties (should contain only a named style) for the run
		/// of text to be added.</param>
		/// ------------------------------------------------------------------------------------
		public new void AddTextToPara(string sText, ITsTextProps pttpProps)
		{
			CheckDisposed();

			base.AddTextToPara(sText, pttpProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeSfmImporter.AddImportStyleProxyForMapping"/> method
		/// </summary>
		/// <param name="mapping">The mapping for which the proxy entry is to be created</param>
		/// <param name="styleProxies">Dictionary to add the proxy to</param>
		/// ------------------------------------------------------------------------------------
		public new void AddImportStyleProxyForMapping(ImportMappingInfo mapping,
			Dictionary<string, ImportStyleProxy> styleProxies)
		{
			CheckDisposed();

			base.AddImportStyleProxyForMapping(mapping, styleProxies);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a dummy scripture object wrapper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="DummyScrObjWrapper is assigned to SOWrapper and disposed in Dispose()")]
		protected override void InitScriptureObject()
		{
			// The tests that use this DummyTeImporter do not utilize a scr obj to read real
			// data files. We'll use a dummy version of the ScrObjWrapper instead.
			DummyScrObjWrapper sow = new DummyScrObjWrapper();
			SOWrapper = sow;
			sow.m_SegmentMarkers = m_SegmentMarkers;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the DummyScrObjWrapper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyScrObjWrapper DummySoWrapper
		{
			get
			{
				CheckDisposed();
				return (DummyScrObjWrapper)SOWrapper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the FinalizePrevSection() method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void CallFinalizePrevSection(IScrSection section,
			ImportDomain prevImportDomain, bool fInScriptureText)
		{
			m_currSection = section;
			m_prevImportDomain = prevImportDomain;
			m_fInScriptureText = fInScriptureText;
			m_fCurrentSectionIsIntro = section.IsIntro;
			m_sectionHeading = section.HeadingOA;
			m_sectionContent = section.ContentOA;
			FinalizePrevSection();
		}
		#endregion

	}
	#endregion

	#region DummyUndoImportManager
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Override of the base UndoImportManager class for tests that use the in-memory cache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyUndoImportManager : UndoImportManager
	{
		private FdoTestBase m_testBase;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyUndoImportManager"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyUndoImportManager(FdoTestBase testBase) :
			base(testBase.Cache)
		{
			m_testBase = testBase;
		}
	}
	#endregion

	#region SOWNoSegException
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Special exception to facilitate testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SOWNoSegException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SOWNoSegException"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SOWNoSegException()
			: base("Dummy ScrObjWrapper has no more segments to return!")
		{
		}
	}
	#endregion

	#region DummyScrObjWrapper
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class for the <see cref="TeImporter"/> so we can test it.
	/// The tests that use the DummyTeImporter do not utilize a scr obj to read real
	/// data files. We'll provide this dummy version of the ScrObjWrapper instead, to minimize
	/// our overhead.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyScrObjWrapper : ScrObjWrapper
	{
		/// <summary>
		/// Set this to simulate an import stream having a specific ICU Locale
		/// </summary>
		public int m_CurrentWs = -1;
		/// <summary>Optional list of segment markers to process in a simulated import</summary>
		public List<string> m_SegmentMarkers;
		private int m_iSegment = 0;
		/// <summary>HVO of the current annotation type</summary>
		private int m_currentAnnotationTypeHvo = 0;
		internal bool m_fIncludeMyPicturesFolderInExternalFolders = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the wrapper's scripture text segment object. This is used by tests that need
		/// to set scReference values for the current segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ISCTextSegment TextSegment
		{
			get
			{
				if (m_scTextSegment == null)
				{
					m_scTextSegment = new SCTextSegment(string.Empty, string.Empty, string.Empty,
						ScrReference.Empty, ScrReference.Empty, null, 0);
				}
				return m_scTextSegment;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the first reference of the current segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override BCVRef SegmentFirstRef
		{
			get
			{
				// For P6 import tests, we cram the references into the TESO text segment because
				// it makes it possible to simulate the Paratext import without loading the P6
				// project.
				if (TypeOfImport == TypeOfImport.Paratext6)
					return m_scTextSegment.FirstReference;
				else
					return base.SegmentFirstRef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the last reference of the current segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override BCVRef SegmentLastRef
		{
			get
			{
				// For P6 import tests, we cram the references into the TESO text segment because
				// it makes it possible to simulate the Paratext import without loading the P6
				// project.
				if (TypeOfImport == TypeOfImport.Paratext6)
					return m_scTextSegment.LastReference;
				else
					return base.SegmentLastRef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For testing, this is a no-op
		/// </summary>
		/// <param name="paratextProjectId">3-letter Paratext project ID</param>
		/// ------------------------------------------------------------------------------------
		protected override void LoadParatextProject(string paratextProjectId)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since the tests don't actually have a real text enumerator or real files, return
		/// a surreal writing sytem.
		/// </summary>
		/// <param name="defaultWs"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int CurrentWs(int defaultWs)
		{
			return m_CurrentWs == -1 ? defaultWs : m_CurrentWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the type of the current annotation (to facilitate testing)
		/// </summary>
		/// <param name="hvoAnnotationType">HVO of the type of the current annotation.</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentAnnotationType(int hvoAnnotationType)
		{
			m_currentAnnotationTypeHvo = hvoAnnotationType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the current annotation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int CurrentAnnotationType
		{
			get { return m_currentAnnotationTypeHvo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of folders to search when importing a picture that does not have a full
		/// path specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override List<string> ExternalPictureFolders
		{
			get
			{
				List<string> externalFolders = new List<string>(new string[] { Path.GetTempPath() });
				if (m_fIncludeMyPicturesFolderInExternalFolders)
				{
					string sMyPicsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
					if (!string.IsNullOrEmpty(sMyPicsFolder))
					{
						externalFolders.Add(sMyPicsFolder);
					}
				}
				return externalFolders;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Advance the scripture text object enumerator to the next segment. This override
		/// allows us to test the condition where an unexpected exception occurs during import
		/// processing. If m_SegmentMarkers is set to a list of n segments, then this method will
		/// throw an exception when asked for segment n+1.
		/// </summary>
		/// <param name="sText">Set to the text of the current segment</param>
		/// <param name="sMarker">Set to the marker of the current segment tag</param>
		/// <param name="domain">Set to the domain of the stream being processed</param>
		/// <returns>
		/// True if successful. False if there are no more segments.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool GetNextSegment(out string sText, out string sMarker, out ImportDomain domain)
		{
			if (m_SegmentMarkers == null)
				return base.GetNextSegment(out sText, out sMarker, out domain);
			if (m_SegmentMarkers.Count > m_iSegment)
			{
				sText = "MAT Dummy text";
				sMarker = m_SegmentMarkers[m_iSegment++];
				domain = ImportDomain.Main;
				BCVRef segmentRef = new BCVRef(40, 0, 0);
				m_scTextSegment = new SCTextSegment(sText, sMarker, string.Empty, segmentRef,
					segmentRef, "dummy.sfm", m_iSegment);
				return true;
			}
			throw new SOWNoSegException();
		}
	}
	#endregion

	#region TE Import Tests (in-memory cache)
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// TeImportTestInMemory tests TeImport using in-memory cache
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class TeImportTestInMemory : TeImportTestsBase
	{
		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes import settings (mappings and options) for "Other" type of import.
		/// Individual test fixtures can override for other types of import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeImportSettings()
		{
			base.InitializeImportSettings();
			// Set up a couple additional mappings needed by FootnoteWithCurlyBraceEndMarkers
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|f{", "}", MarkerDomain.Footnote,
				ScrStyleNames.NormalFootnoteParagraph, null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|fr{", "}", MarkerDomain.Footnote,
				ScrStyleNames.FootnoteTargetRef, null, null));
		}
		#endregion

		#region Importer Individual Method Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeSfmImporter.AddImportStyleProxyForMapping"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddImportStyleProxyForMapping_Normal()
		{
			m_importer.HtStyleProxy.Clear();
			ImportMappingInfo mapping = new ImportMappingInfo(@"\hello", MarkerDomain.Default,
				ScrStyleNames.MainBookTitle, "de", null);
			int wsExpected = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("de");
			m_importer.AddImportStyleProxyForMapping(mapping, m_importer.HtStyleProxy);
			ImportStyleProxy proxy = ((ImportStyleProxy)m_importer.HtStyleProxy[@"\hello"]);
			Assert.AreEqual(StyleType.kstParagraph, proxy.StyleType);
			ITsPropsFactory pillowtex = TsPropsFactoryClass.Create();
			int cb = proxy.ParaProps.Length;
			ITsTextProps proxyParaProps = pillowtex.DeserializePropsRgb(proxy.ParaProps, ref cb);
			string sHowDifferent;
			if (!TsTextPropsHelper.PropsAreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle),
				proxyParaProps, out sHowDifferent))
			{
				Assert.Fail(sHowDifferent);
			}
			Assert.AreEqual(wsExpected, proxy.TsTextProps.GetWs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pass a mapping to AddImportStyleProxyForMapping that has a writing system set to a
		/// value that doesn't match any existing ICU Locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddImportStyleProxyForMapping_InvalidWritingSystem()
		{
			m_importer.HtStyleProxy.Clear();
			ImportMappingInfo mapping = new ImportMappingInfo(@"\bye", MarkerDomain.Note,
				ScrStyleNames.MainBookTitle, "blah", null);
			m_importer.AddImportStyleProxyForMapping(mapping, m_importer.HtStyleProxy);
			ImportStyleProxy proxy = ((ImportStyleProxy)m_importer.HtStyleProxy[@"\bye"]);
			ITsPropsFactory chrysler = TsPropsFactoryClass.Create();
			int cb = proxy.ParaProps.Length;
			ITsTextProps proxyParaProps = chrysler.DeserializePropsRgb(proxy.ParaProps, ref cb);
			string sHowDifferent;
			if (!TsTextPropsHelper.PropsAreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle),
				proxyParaProps, out sHowDifferent))
			{
				Assert.Fail(sHowDifferent);
			}
			Assert.AreEqual(m_wsAnal, proxy.TsTextProps.GetWs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeSfmImporter.AddImportStyleProxyForMapping"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddImportStyleProxyForMapping_Inline()
		{
			m_importer.HtStyleProxy.Clear();
			ImportMappingInfo mapping = new ImportMappingInfo("|b{", "}", MarkerDomain.Default,
				"Really bold text", "de", null);
			int wsExpected = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("de");
			m_importer.AddImportStyleProxyForMapping(mapping, m_importer.HtStyleProxy);
			ImportStyleProxy proxy = ((ImportStyleProxy)m_importer.HtStyleProxy[mapping.BeginMarker]);
			Assert.AreEqual(StyleType.kstCharacter, proxy.StyleType);
			ITsPropsFactory pillowtex = TsPropsFactoryClass.Create();
			ITsTextProps proxyTextProps = proxy.TsTextProps;
			string sHowDifferent;
			if (!TsTextPropsHelper.PropsAreEqual(StyleUtils.CharStyleTextProps("Really bold text", wsExpected),
				proxyTextProps, out sHowDifferent))
			{
				Assert.Fail(sHowDifferent);
			}
			Assert.AreEqual(ContextValues.General, m_styleSheet.FindStyle("Really bold text").Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the <see cref="TeSfmImporter.PrevRunIsVerseNumber"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PrevRunIsVerseNumber()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			ITsPropsBldr props = TsPropsBldrClass.Create();

			// This will do nothing except make sure it doesn't throw an exception
			Assert.IsFalse(m_importer.PrevRunIsVerseNumber(null));
			Assert.IsFalse(m_importer.PrevRunIsVerseNumber(bldr));

			// Last marker is Verse Number. Should return true for previous run.
			props.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse Number");
			bldr.Replace(0, 0, "Run 1", props.GetTextProps());
			Assert.IsTrue(m_importer.PrevRunIsVerseNumber(bldr));

			// Second marker is not Verse Number. Should return false for previous run.
			props.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.NormalParagraph);
			bldr.Replace(5, 5, "Run 2", props.GetTextProps());
			Assert.IsFalse(m_importer.PrevRunIsVerseNumber(bldr));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the <see cref="TeSfmImporter.AddTextToPara(string, ITsTextProps)"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddTextToPara()
		{
			// First run
			m_importer.AddTextToPara("What do we want to add?", m_ttpAnalWS);
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount,
				"There should only be one run.");
			Assert.AreEqual("What do we want to add?", m_importer.NormalParaStrBldr.get_RunText(0));
			Assert.AreEqual(m_ttpAnalWS, m_importer.NormalParaStrBldr.get_Properties(0),
				"First run should be anal.");
			Assert.AreEqual(23, m_importer.ParaBldrLength);

			// Add a second run
			m_importer.AddTextToPara("Some vernacular penguins", m_ttpVernWS);
			Assert.AreEqual(2, m_importer.NormalParaStrBldr.RunCount,
				"There should be two runs.");
			Assert.AreEqual("What do we want to add?", m_importer.NormalParaStrBldr.get_RunText(0));
			Assert.AreEqual("Some vernacular penguins", m_importer.NormalParaStrBldr.get_RunText(1));
			Assert.AreEqual(m_ttpVernWS, m_importer.NormalParaStrBldr.get_Properties(1),
				"Second run should be vern.");
			Assert.AreEqual(47, m_importer.ParaBldrLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the <see cref="TeSfmImporter.GetVerseRefAsString"/> method when Arabic
		/// numerals are desired.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseRefTest_NonScriptDigits()
		{
			// These values are arbitrary.
			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 0);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 0);
			Assert.AreEqual("0", m_importer.GetVerseRefAsString(Cache.DefaultAnalWs));

			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 1);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 1);
			Assert.AreEqual("1", m_importer.GetVerseRefAsString(Cache.DefaultAnalWs));

			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 12);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 13);
			Assert.AreEqual("12-13", m_importer.GetVerseRefAsString(Cache.DefaultAnalWs));

			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 14, 2);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 14, 2);
			Assert.AreEqual("14b", m_importer.GetVerseRefAsString(Cache.DefaultAnalWs));

			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 15, 3);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 17, 4);
			Assert.AreEqual("15c-17d", m_importer.GetVerseRefAsString(Cache.DefaultAnalWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the <see cref="TeSfmImporter.GetVerseRefAsString"/> method when script
		/// numerals are desired.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseRefTest_ScriptDigits()
		{
			m_scr.ScriptDigitZero = 0x0c66;
			m_scr.UseScriptDigits = true;

			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 0);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 0);
			Assert.AreEqual("\u0c66", m_importer.GetVerseRefAsString(0));

			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 1);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 1);
			Assert.AreEqual("\u0c67", m_importer.GetVerseRefAsString(0));

			m_importer.TextSegment.FirstReference = new BCVRef(3, 4, 12);
			m_importer.TextSegment.LastReference = new BCVRef(3, 4, 13);
			Assert.AreEqual("\u0c67\u0c68-\u0c67\u0c69", m_importer.GetVerseRefAsString(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the <see cref="TeSfmImporter.FindCorrespondingVernParaForSegment"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCorrespondingVernParaForSegment()
		{
			CreateExodusData();
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection lastSection = exodus.SectionsOS[exodus.SectionsOS.Count - 1];
			int hvoLastPara =
				lastSection.ContentOA.ParagraphsOS[lastSection.ContentOA.ParagraphsOS.Count - 1].Hvo;
			m_importer.ScrBook = exodus; // Need to do this to keep importer from trying to make the book.
			IStTxtPara para = m_importer.FindCorrespondingVernParaForSegment(
				m_styleSheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(2, 1, 7), lastSection.ContentOA.ParagraphsOS.Count - 1);
			Assert.AreEqual(hvoLastPara, para.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.RemoveControlCharacters method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveControlCharactersTests()
		{
			string s = "abcd" + '\u001e';
			string result = (string)ReflectionHelper.CallStaticMethod("TeImportExport.dll", "SIL.FieldWorks.TE.TeSfmImporter", "RemoveControlCharacters",
				new object[]{s});
			Assert.AreEqual("abcd", result);

			s = "abcd" + '\u0009';
			result = (string)ReflectionHelper.CallStaticMethod("TeImportExport.dll", "SIL.FieldWorks.TE.TeSfmImporter", "RemoveControlCharacters",
				new object[] { s });
			Assert.AreEqual("abcd ", result);
		}

		#region Tests of EnsurePictureFilePathIsRooted method
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the path is
		/// indeed rooted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsurePictureFilePathIsRooted_Rooted()
		{
			string fileName = MiscUtils.IsUnix ? "P0|/tmp/mypic.jpg|P2|P3|P4"
				: @"P0|c:\temp\mypic.jpg|P2|P3|P4";
			Assert.AreEqual(fileName,
				ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted",
				fileName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the text
		/// representation of the picture is bogus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsurePictureFilePathIsRooted_BogusTextRep_NoLeadingVerticalBar()
		{
			Assert.AreEqual(Path.Combine(Path.GetTempPath(), "Bogus"),
				ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted", "Bogus"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the text
		/// representation of the picture is bogus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsurePictureFilePathIsRooted_BogusTextRep_NoTrailingVerticalBar()
		{
			Assert.AreEqual("|Bogus.jpg",
				ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted", "|Bogus.jpg"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the file
		/// is not rooted but it is found to exist in the first external folder in
		/// SOWrapper.ExternalPictureFolders.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsurePictureFilePathIsRooted_NotRooted_FoundInFirstExternalFolder()
		{
			DummyScrObjWrapper sow = (DummyScrObjWrapper)ReflectionHelper.GetProperty(m_importer, "SOWrapper");
			sow.m_fIncludeMyPicturesFolderInExternalFolders = true;

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				Assert.IsTrue(Path.IsPathRooted(filemaker.Filename));
				Assert.AreEqual("P0|" + filemaker.Filename + "|P2|P3|P4",
					ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted", "P0|junk.jpg|P2|P3|P4"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the file
		/// is not rooted but it is found to exist in the second external folder in
		/// SOWrapper.ExternalPictureFolders.
		/// </summary>
		/// <exception cref="T:NUnit.Framework.IgnoreException"> Thrown if the My Pictures folder
		/// is not set.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsurePictureFilePathIsRooted_NotRooted_FoundInSecondExternalFolder()
		{
			DummyScrObjWrapper sow = (DummyScrObjWrapper)ReflectionHelper.GetProperty(m_importer, "SOWrapper");
			sow.m_fIncludeMyPicturesFolderInExternalFolders = true;
			if (sow.ExternalPictureFolders.Count < 2)
				Assert.Ignore("Test requires My Pictures folder to be set.");

			using (DummyFileMaker filemaker = new DummyFileMaker(Path.Combine(sow.ExternalPictureFolders[1], "j~u~n~k.jpg"), false))
			{
				Assert.IsTrue(Path.IsPathRooted(filemaker.Filename));
				Assert.AreEqual("P0|" + filemaker.Filename + "|P2|P3|P4",
					ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted", "P0|j~u~n~k.jpg|P2|P3|P4"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the file
		/// is "rooted" (starts with a backslash) but does not have a drive letter specified
		/// and is found in the specified folder (relative to the current drive letter).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "Linux paths behave differently.")]
		public void EnsurePictureFilePathIsRooted_RootedButNoDriveLetter_FoundRelativeToCurrentDrive()
		{
			DummyScrObjWrapper sow = (DummyScrObjWrapper)ReflectionHelper.GetProperty(m_importer, "SOWrapper");
			sow.m_fIncludeMyPicturesFolderInExternalFolders = true;

			using (DummyFileMaker filemaker = new DummyFileMaker(Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "j~u~n~k.jpg"), false))
			{
				String str1 = "P0|" + filemaker.Filename + "|P2|P3|P4";
				String str2 = ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted", @"P0|\j~u~n~k.jpg|P2|P3|P4");
				Assert.AreEqual(str1.ToLowerInvariant(), str2.ToLowerInvariant());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the file is
		/// "rooted" (starts with a backslash) but does not have a drive letter specified and is
		/// found in the specified folder (relative to the current drive letter).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "Linux paths behave differently.")]
		public void EnsurePictureFilePathIsRooted_RootedButNoDriveLetter_FoundInFirstExternalFolder()
		{
			DummyScrObjWrapper sow = (DummyScrObjWrapper)ReflectionHelper.GetProperty(m_importer, "SOWrapper");
			sow.m_fIncludeMyPicturesFolderInExternalFolders = true;

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				Assert.AreEqual("P0|" + filemaker.Filename + "|P2|P3|P4",
					ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted", @"P0|\junk.jpg|P2|P3|P4"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the TeSfmImporter.EnsurePictureFilePathIsRooted method when the file is not
		/// rooted and cannot be found in any of the external folders in
		/// SOWrapper.ExternalPictureFolders.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsurePictureFilePathIsRooted_NotRooted_NotFoundInExternalFolders()
		{
			DummyScrObjWrapper sow = (DummyScrObjWrapper)ReflectionHelper.GetProperty(m_importer, "SOWrapper");
			sow.m_fIncludeMyPicturesFolderInExternalFolders = true;

			foreach (string sFolder in sow.ExternalPictureFolders)
			{
				string sPath = Path.Combine(sFolder, "wunkybunkymunky.xyz");
				Assert.IsFalse(FileUtils.FileExists(sPath), "Test is invalid because " + sPath + "exists.");
			}

			Assert.AreEqual("P0|" + Path.Combine(sow.ExternalPictureFolders[0], "wunkybunkymunky.xyz") + "|P2|P3|P4",
					ReflectionHelper.GetStrResult(m_importer, "EnsurePictureFilePathIsRooted", "P0|wunkybunkymunky.xyz|P2|P3|P4"));
		}
		#endregion
		#endregion

		#region UseMappedLanguage
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that ProcessSegment creates a run whose writing system corresponds to the
		/// writing system set in the mapping
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment_UseMappedLanguage()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("This is an English para", @"\p");
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			int wsExpected = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("qaa-x-kal");
			VerifyBldrRun(0, "This is an English para", null, wsExpected);
		}
		#endregion

		#region ProcessSegment - Basic tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a minimal, normal sequence of scripture text segments through ProcessSegment,
		/// and verify the results.
		/// We will process this marker sequence:
		///    id mt is ip
		///    c1 s s r p v1 x x* f f* v2-3 q q2 v4
		///    c2 s q v1-10 s p v11
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegmentBasic()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("EXO", @"\id");
			Assert.AreEqual(2, m_importer.BookNumber);
			Assert.AreEqual(1, m_importer.ScrBook.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(0, m_importer.ScrBook.SectionsOS.Count);
			Assert.IsTrue(m_importer.HvoTitle > 0);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual(string.Empty, book.IdText);
			Assert.IsTrue(book.TitleOA.IsValidObject); //empty title
			Assert.AreEqual(book.TitleOA.Hvo, m_importer.HvoTitle);
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(0, book.SectionsOS.Count); // empty seq of sections
			Assert.AreEqual("EXO", book.BookId);
			//	Assert.AreEqual(2, book.CanonOrd);

			// ************** process a main title *********************
			m_importer.ProcessSegment("Main Title!", @"\mt");
			Assert.AreEqual("Main Title!", m_importer.ScrBook.Name.VernacularDefaultWritingSystem.Text);

			// begin first section (intro material)
			// ************** process an intro section head, test MakeSection() method ************
			m_importer.ProcessSegment("Background Material", @"\is");
			Assert.AreEqual(2, m_importer.BookNumber);
			Assert.IsNotNull(m_importer.CurrentSection);
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Background Material", null);
			Assert.AreEqual(19, m_importer.ParaBldrLength);
			// verify completed title was added to the DB
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			IStTxtPara title = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle), title.StyleRules);
			Assert.AreEqual(1, title.Contents.RunCount);
			AssertEx.RunIsCorrect(title.Contents, 0, "Main Title!", null, DefaultVernWs);
			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 0);

			// ************** process an intro paragraph, test MakeParagraph() method **********
			m_importer.ProcessSegment("Intro paragraph text", @"\ip");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Intro paragraph text", null);
			Assert.AreEqual(20, m_importer.ParaBldrLength);
			// verify completed intro section head was added to DB
			Assert.AreEqual(1, book.SectionsOS.Count);
			Assert.AreEqual(1, book.SectionsOS[0].HeadingOA.ParagraphsOS.Count);
			IStTxtPara heading = (IStTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"),
				heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Background Material", null, DefaultVernWs);

			// begin second section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			// note: new section and para are established, but chapter number is not put in
			//  para now (it's saved for drop-cap location later)
			// verify state of NormalParaStrBldr
			Assert.AreEqual(0, m_importer.ParaBldrLength);
			Assert.AreEqual(1, m_importer.Chapter);
			// verify contents of completed paragraph
			Assert.AreEqual(1, book.SectionsOS[0].ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Paragraph"), para.StyleRules);
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "Intro paragraph text", null, DefaultVernWs);
			// verify refs of completed section
			Assert.AreEqual(2001000, book.SectionsOS[0].VerseRefMin);
			Assert.AreEqual(2001000, book.SectionsOS[0].VerseRefMax);
			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 1);

			// ************** process a section head (for 1:1-4) *********************
			m_importer.ProcessSegment("Section Head One", @"\s");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head One", null);
			Assert.AreEqual(16, m_importer.ParaBldrLength);

			// ************** process second line of section head *********************
			m_importer.ProcessSegment("Yadda yadda Line two!", @"\s");
			// verify state of NormalParaStrBldr
			char sBrkChar = StringUtils.kChHardLB;
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head One" + sBrkChar + "Yadda yadda Line two!", null);
			Assert.AreEqual(38, m_importer.ParaBldrLength);

			// ************** process a section head reference *********************
			m_importer.ProcessSegment("Section Head Ref Line", @"\r");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head Ref Line", null);

			// in second section (1:1-4), begin first content paragraph
			// ************** process a \p paragraph marker *********************
			m_importer.ProcessSegment("", @"\p");
			// note: chapter number should be inserted now
			int expectedBldrLength = 1; // The chapter number takes one character
			int expectedRunCount = 1;
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number");
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
			// verify completed section head was added to DB (for 1:1-4)
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(2, book.SectionsOS[1].HeadingOA.ParagraphsOS.Count);
			// Check 1st heading para
			heading = (IStTxtPara)book.SectionsOS[1].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Section Head One" +
				sBrkChar + "Yadda yadda Line two!", null, DefaultVernWs);
			// Check 2nd heading para
			heading = (IStTxtPara)book.SectionsOS[1].HeadingOA.ParagraphsOS[1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Parallel Passage Reference"), heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Section Head Ref Line", null, DefaultVernWs);

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			string sSegmentText = "Verse one text";
			expectedBldrLength += 1 + sSegmentText.Length; // Length of verse # + length of verse text
			expectedRunCount += 2;
			m_importer.ProcessSegment(sSegmentText, @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number");
			VerifyBldrRun(1, "1", "Verse Number");
			VerifyBldrRun(2, sSegmentText, null);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);

			// ************** process verse text with character style *********************
			sSegmentText = " text with char style";
			expectedBldrLength += sSegmentText.Length;
			expectedRunCount++;
			m_importer.ProcessSegment(sSegmentText, @"\kw");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(expectedRunCount - 1, sSegmentText, "Key Word");
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);

			// ************** process text after the character style *********************
			sSegmentText = " text after char style";
			expectedBldrLength += sSegmentText.Length;
			expectedRunCount++;
			m_importer.ProcessSegment(sSegmentText, @"\kw*");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);

			// ********** process a footnote (use default Scripture settings) *************
			string sFootnoteSegment = "My footnote text";
			expectedBldrLength++; // Just adding the footnote marker: "a"
			expectedRunCount++;
			m_importer.ProcessSegment(sFootnoteSegment, @"\f");
			// verify state of FootnoteParaStrBldr
			Assert.AreEqual(1, m_importer.FootnoteParaStrBldr.RunCount);

			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
			// check the ORC (Object Replacement Character)
			VerifyBldrFootnoteOrcRun(expectedRunCount - 1, 0);

			// ************** process text after a footnote *********************
			sSegmentText = " more verse text";
			expectedBldrLength += sSegmentText.Length;
			expectedRunCount++;
			m_importer.ProcessSegment(sSegmentText, @"\vt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
			// Verify creation of footnote object
			VerifySimpleFootnote(0, sFootnoteSegment);

			// ************** process verse two-three text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			sSegmentText = "Verse two-three text";
			expectedBldrLength += 3 + sSegmentText.Length;
			expectedRunCount += 2;
			m_importer.ProcessSegment(sSegmentText, @"\v");
			// verify state of NormalParaStrBldr
			//TODO: when ready, modify these lines to verify that chapter number was properly added to para
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(expectedRunCount - 2, "2-3", "Verse Number");
			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);

			// in second section (verse text), begin second paragraph
			// ************** process a \q paragraph marker with text *********************
			int expectedParaRunCount = expectedRunCount;
			sSegmentText = "First line of poetry";
			expectedRunCount = 1;
			expectedBldrLength = sSegmentText.Length;
			m_importer.ProcessSegment(sSegmentText, @"\q");

			Assert.AreEqual(2, m_importer.BookNumber);

			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);

			// verify that the verse text first paragraph is in the db correctly
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(1, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 2, "Verse one text", null, DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 3, " text with char style", "Key Word", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 4, " text after char style", null, DefaultVernWs);
			//			AssertEx.RunIsCorrect(para.Contents, 5,
			//				" text after char style{footnote text} more verse text", null, DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 6, " more verse text", null, DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 7, "2-3", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 8, "Verse two-three text", null, DefaultVernWs);

			// in second section (verse text), begin third paragraph
			// ************** process a \q2 paragraph marker (for a new verse) ****************
			expectedParaRunCount = expectedRunCount;
			m_importer.ProcessSegment("", @"\q2");
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify state of NormalParaStrBldr
			Assert.AreEqual(0, m_importer.ParaBldrLength);
			// verify that the verse text second paragraph is in the db correctly
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(2, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[1]; //second para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, sSegmentText, null, DefaultVernWs);

			// ************** process verse four text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 4);
			sSegmentText = "second line of poetry";
			expectedBldrLength = sSegmentText.Length + 1;
			expectedRunCount = 2;
			m_importer.ProcessSegment(sSegmentText, @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(expectedRunCount - 2, "4", "Verse Number");
			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
			m_importer.ProcessSegment("", @"\c");
			// note: new para is established, but chapter number is not put in
			// para now (it's saved for drop-cap location later)
			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength, "nothing should have been added");
			Assert.AreEqual(2, m_importer.Chapter);
			// verify that we have not yet established a third section
			Assert.AreEqual(2, book.SectionsOS.Count);

			// begin third section
			// ************** process a section head (for 2:1-10) *********************
			m_importer.ProcessSegment("Section Head Two", @"\s");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head Two", null);
			Assert.AreEqual(16, m_importer.ParaBldrLength);

			// verify that the second section third paragraph is in the db correctly
			Assert.AreEqual(3, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[2]; //third para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line2"), para.StyleRules);
			Assert.AreEqual(2, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "4", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 1, "second line of poetry", null, DefaultVernWs);

			// verify refs of completed scripture text section (1:1-4)
			Assert.AreEqual(2001001, book.SectionsOS[1].VerseRefMin);
			Assert.AreEqual(2001004, book.SectionsOS[1].VerseRefMax);
			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 2);

			// in third section, begin first paragraph
			// ************** process a \q paragraph marker (for a new verse) ****************
			m_importer.ProcessSegment("", @"\q");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "2", "Chapter Number");
			Assert.AreEqual(1, m_importer.ParaBldrLength);
			// verify completed section head was added to DB
			Assert.AreEqual(3, book.SectionsOS.Count);
			Assert.AreEqual(1, book.SectionsOS[2].HeadingOA.ParagraphsOS.Count);
			heading = (IStTxtPara)book.SectionsOS[2].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Section Head Two", null, DefaultVernWs);

			// ************** process verse 5-10 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 10);
			m_importer.ProcessSegment("verse one to ten text", @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(1, "1-10", "Verse Number");
			VerifyBldrRun(2, "verse one to ten text", null);
			Assert.AreEqual(26, m_importer.ParaBldrLength);

			// begin fourth section
			// ************** process a section head (for 2:11) *********************
			m_importer.ProcessSegment("Section Head Four", @"\s");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head Four", null);
			Assert.AreEqual(17, m_importer.ParaBldrLength);
			// verify that the third section first paragraph is in the db correctly
			Assert.AreEqual(4, book.SectionsOS.Count);
			Assert.AreEqual(1, book.SectionsOS[2].ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)book.SectionsOS[2].ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "2", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 1, "1-10", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 2, "verse one to ten text", null, DefaultVernWs);
			// verify refs of completed scripture text section (2:5-10)
			Assert.AreEqual(2002001, book.SectionsOS[2].VerseRefMin);
			Assert.AreEqual(2002010, book.SectionsOS[2].VerseRefMax);
			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 3);

			// TODO:  p v11

			// ************** finalize **************
			m_importer.FinalizeImport();
			Assert.AreEqual(2, m_importer.BookNumber);
		}
		#endregion

		#region Verse test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1847 Verify that importing empty verses (e.g. from Paratext) results in a space
		/// between verse numbers with the default paragraph style.
		///
		/// We will process this marker sequence:
		///    id
		///    c1 v1 v2 v3
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EmptyVerses()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// begin second section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// note: chapter number should be inserted now
			// verify state of NormalParaStrBldr
			int expectedBldrLength = 1; // The chapter number takes one character
			int expectedRunCount = 1;

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			expectedBldrLength += 1; // Length of verse #
			expectedRunCount += 1;
			m_importer.ProcessSegment("", @"\v");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			expectedBldrLength += 2; // Length of verse # w/ preceeding space
			expectedRunCount += 2; // a space should be added automatically between verse numbers
			m_importer.ProcessSegment("", @"\v");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			expectedBldrLength += 2; // Length of verse # w/ preceeding space
			expectedRunCount += 2; // a space should be added automatically between verse numbers
			m_importer.ProcessSegment("", @"\v");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number");
			VerifyBldrRun(1, "1", "Verse Number");
			VerifyBldrRun(2, " ", null);
			VerifyBldrRun(3, "2", "Verse Number");
			VerifyBldrRun(4, " ", null);
			VerifyBldrRun(5, "3", "Verse Number");
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);

			// ************** finalize **************
			m_importer.FinalizeImport();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1847 Verify that importing verses out of order results in the correct section
		/// reference range.
		///
		/// We will process this marker sequence:
		///    id
		///    c1 v10 v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VersesOutOfOrder()
		{
			// ************** process an \id segment *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// begin second section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("1", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 10);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 10);
			m_importer.ProcessSegment("10 verse ten", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("2 verse two", @"\v");

			m_importer.FinalizeImport();

			IScrSection section = m_importer.ScrBook.SectionsOS[0];

			Assert.AreEqual(2001002, section.VerseRefMin);
			Assert.AreEqual(2001010, section.VerseRefMax);
			Assert.AreEqual(2001010, section.VerseRefStart);
			Assert.AreEqual(2001002, section.VerseRefEnd);

		}
		#endregion

		#region Excluded test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-5542 Test importing when a style is mapped to default paragraph characters and
		/// is excluded.
		///
		/// We will process this marker sequence:
		///    \id \c1 \v1 \vt \v2 \vt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultParaChars_Excluded()
		{
			m_importer.Settings.SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\vt", null, true, MappingTargetType.TEStyle,
				MarkerDomain.Default, "Default Paragraph Characters", null));
			m_importer.Initialize();

			// ************** process an \id segment *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// begin second section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("1", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("verse 1", @"\vt");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("verse 2", @"\vt");

			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11 2", para.Contents.Text, "No verse text should get imported");
		}
		#endregion

		#region ProcessSegment - Advanced tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send more complex (but reasonable) sequence of scripture text segments through
		/// ProcessSegment, and verify the results.
		/// We will process this marker sequence:
		///    id h mt2 mt st3
		///    c1 s q v5-10 s p{text} v12 bogus p v6
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegmentAdvanced()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// *************** process a \h (Title Short) marker ****************
			m_importer.ProcessSegment("This is a header  ", @"\h");
			// verify header was added to DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("This is a header", book.Name.VernacularDefaultWritingSystem.Text);

			// ************** process a subtitle (mt2) *********************
			m_importer.ProcessSegment("The Gospel According to", @"\mt2");
			Assert.AreEqual("This is a header", book.Name.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(0, book.SectionsOS.Count);

			// ************** process a main title (mt) *********************
			m_importer.ProcessSegment("Waldo", @"\mt");
			Assert.AreEqual("This is a header", m_importer.ScrBook.Name.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(0, book.SectionsOS.Count);

			// ************** process a subtitle (st3) *********************
			m_importer.ProcessSegment("Dude!", @"\st3");
			Assert.AreEqual("This is a header", m_importer.ScrBook.Name.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(0, book.SectionsOS.Count);

			// begin first section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			VerifyNewSectionExists(book, 0);
			// note: chapter number is not put in para now (it's saved for drop-cap location later)
			// verify state of NormalParaStrBldr
			Assert.AreEqual(0, m_importer.ParaBldrLength);
			Assert.AreEqual(1, m_importer.Chapter);

			// verify completed title was added to the DB
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			IStTxtPara title = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle), title.StyleRules);
			Assert.AreEqual(3, title.Contents.RunCount);
			AssertEx.RunIsCorrect(title.Contents, 0, "The Gospel According to", "Title Secondary", DefaultVernWs);
			char sBrkChar = StringUtils.kChHardLB;
			AssertEx.RunIsCorrect(title.Contents, 1, sBrkChar + "Waldo" + sBrkChar, null, DefaultVernWs);
			AssertEx.RunIsCorrect(title.Contents, 2, "Dude!", "Title Tertiary", DefaultVernWs);

			// ************** process a section head (for 1:5-10) *********************
			m_importer.ProcessSegment("Section Head One", @"\s");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head One", null);
			Assert.AreEqual(16, m_importer.ParaBldrLength);
			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 0);

			// in first section (1:5-10), begin first content paragraph
			// ************** process a \q (poetry paragraph marker) *********************
			m_importer.ProcessSegment("", @"\q");
			// note: chapter number should be inserted now
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number");
			Assert.AreEqual(1, m_importer.ParaBldrLength);
			// verify completed section head was added to DB
			//book = (ScrBook)ScrBook.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
			Assert.AreEqual(1, book.SectionsOS[0].HeadingOA.ParagraphsOS.Count);
			IStTxtPara heading = (IStTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Section Head One", null, DefaultVernWs);

			// ************** process verse 5-10 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 5);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 10);
			m_importer.ProcessSegment("verse five to ten text", @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(1, "5-10", "Verse Number");
			VerifyBldrRun(2, "verse five to ten text", null);
			Assert.AreEqual(27, m_importer.ParaBldrLength);

			// begin second section
			// ************** process a section head (for 1:10-2:6) *********************
			m_importer.ProcessSegment("Section Head Two", @"\s");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head Two", null);
			Assert.AreEqual(16, m_importer.ParaBldrLength);
			// verify that the first section first paragraph is in the db correctly
			//book = (ScrBook)ScrBook.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
			IScrSection prevSection = book.SectionsOS[0];
			Assert.AreEqual(1, prevSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)prevSection.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 1, "5-10", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 2, "verse five to ten text", null, DefaultVernWs);
			// verify refs of completed scripture text section (2:5-10)
			Assert.AreEqual(2001005, prevSection.VerseRefMin);
			Assert.AreEqual(2001010, prevSection.VerseRefMax);
			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 1);

			// send a marker that is to be excluded from the import
			// ************** process a \bogus marker *********************
			m_importer.ProcessSegment("Exclude me, dude!", @"\bogus");
			// Nothing should have changed
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head Two", null);
			Assert.AreEqual(16, m_importer.ParaBldrLength);

			// in second section (1:10-2:6), begin first content paragraph
			// ************** process a \p paragraph marker *********************
			m_importer.ProcessSegment("End of verse 10", @"\p");
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "End of verse 10", null);
			Assert.AreEqual(15, m_importer.ParaBldrLength);

			// verify completed section head was added to DB
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(1, book.SectionsOS[1].HeadingOA.ParagraphsOS.Count);
			heading = (IStTxtPara)book.SectionsOS[1].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Section Head Two", null, DefaultVernWs);

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 12);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 12);
			m_importer.ProcessSegment("Verse twelve text", @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "End of verse 10", null);
			VerifyBldrRun(1, "12", "Verse Number");
			VerifyBldrRun(2, "Verse twelve text", null);
			Assert.AreEqual(34, m_importer.ParaBldrLength);

			// TODO:  c2 p v6

			// ************** finalize **************
			m_importer.FinalizeImport();
			Assert.AreEqual(2, m_importer.BookNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process very large paragraphs. Should be split at chapter breaks if they are really
		/// huge.
		/// </summary>
		/// <remarks>Jira # is TE-2753</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessHugeParagraphs_SplitAtChapterBreak()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// begin first section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// Begin first content paragraph
			// ** process a paragraph over 5000 characters that has an embedded chapter number **
			m_importer.ProcessSegment("", @"\q");
			string sContents = String.Empty.PadLeft(1000, 'a');
			for (int verse = 1; verse <= 6; verse++)
			{
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, verse);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, verse);
				m_importer.ProcessSegment(sContents, @"\v");
			}
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
			m_importer.ProcessSegment(sContents, @"\v");

			// Chapter break should have caused a forced paragraph break.
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "2", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, sContents, null);
			Assert.AreEqual(1002, m_importer.ParaBldrLength);
			// verify completed paragraph was added to DB
			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			Assert.AreEqual(13, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", DefaultVernWs);
			for (int verse = 1; verse <= 6; verse++)
			{
				AssertEx.RunIsCorrect(para.Contents, verse * 2 - 1, verse.ToString(), "Verse Number", DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, verse * 2, sContents, null, DefaultVernWs);
			}

			// ************** finalize **************
			m_importer.FinalizeImport();
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1]; //second para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process very large paragraphs. Should be split at verse breaks if they are really
		/// huge.
		/// </summary>
		/// <remarks>Jira # is TE-2753</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessHugeParagraphs_SplitAtVerseBreak()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// begin first section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// Begin first content paragraph
			// ** process a paragraph over 20000 characters that has verse breaks **
			m_importer.ProcessSegment("", @"\q");
			string sContents = String.Empty.PadLeft(1000, 'a');
			for (int verse = 1; verse <= 21; verse++)
			{
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, verse);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, verse);
				m_importer.ProcessSegment(sContents, @"\v");
			}

			// Last verse break should have caused a forced paragraph break.
			Assert.AreEqual(2, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "21", ScrStyleNames.VerseNumber);
			VerifyBldrRun(1, sContents, null);
			Assert.AreEqual(1002, m_importer.ParaBldrLength);
			// verify completed paragraph (with first 19 verses) was added to DB
			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			Assert.AreEqual(41, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", DefaultVernWs);
			for (int verse = 1; verse <= 20; verse++)
			{
				AssertEx.RunIsCorrect(para.Contents, verse * 2 - 1, verse.ToString(), "Verse Number", DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, verse * 2, sContents, null, DefaultVernWs);
			}

			// ************** finalize **************
			m_importer.FinalizeImport();
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1]; //second para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process section heads that begin with major section heading and have a chapter number
		/// before the regular section heading.
		/// </summary>
		/// <remarks>Jira # is TE-6558, BT was failing because vernacular import created two sections
		/// for the complex heading</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment_ComplexSectionHeading()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process a segmments for major section heading
			m_importer.ProcessSegment("Major Section Reference", @"\mr");
			m_importer.ProcessSegment("Major Section Heading", @"\ms");

			// begin first section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// *************** process the first section heading
			m_importer.ProcessSegment("Section Heading", @"\s");
			m_importer.ProcessSegment("Section Reference", @"\r");

			// Begin first content paragraph
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("The first verse", @"\v");

			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual(1, book.SectionsOS.Count, "Should only have one section");
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(4, section.HeadingOA.ParagraphsOS.Count, "Heading should have 4 paragraphs");

			// ************** finalize **************
			m_importer.FinalizeImport();
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process very large paragraphs. Should be split at punctuation if they are really
		/// huge.
		/// </summary>
		/// <remarks>Jira # is TE-2753</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessHugeParagraphs_SplitAtPunctuation()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// begin first section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// Begin first content paragraph
			// ** process a paragraph over 20000 characters that has a period in it. **
			string sPara1 = ".".PadLeft(20000, 'a');
			string sPara2 = " And then the disciples got tired of the letter a.";
			m_importer.ProcessSegment(sPara1 + sPara2, @"\q");

			// Period should have caused a forced paragraph break.
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, sPara2, null);
			// verify completed paragraph was added to DB
			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			Assert.AreEqual(2, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 1, sPara1, null, DefaultVernWs);

			// ************** finalize **************
			m_importer.FinalizeImport();
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1]; //second para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expect an exception during import when intro material is interspersed with
		/// Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			 ExpectedMessage = "Book introduction within Scripture text.(\\r)?\\n(\\r)?\\n" +
			 "\\\\ip Bad intro para(\\r)?\\nAttempting to read EXO  Chapter: 1  Verse: 1",
			 MatchType = MessageMatch.Regex)]
		public void FailWhenImplicitIntroSectionFollowsScripture()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("A", @"\s");
			m_importer.ProcessSegment("My para", @"\p");
			m_importer.ProcessSegment("B", @"\s");
			m_importer.ProcessSegment("Bad intro para", @"\ip");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expect an exception during import when intro material is interspersed with
		/// Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
		   ExpectedMessage = "Book introduction within Scripture text.(\\r)?\\n(\\r)?\\n" +
		   "\\\\ip Bad intro para(\\r)?\\nAttempting to read EXO  Chapter: 1  Verse: 1",
		   MatchType = MessageMatch.Regex)]
		public void FailWhenIntroParaFollowsScripture()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("A", @"\s");
			m_importer.ProcessSegment("My para", @"\p");
			m_importer.ProcessSegment("Bad intro para", @"\ip");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expect an exception during import when intro material is interspersed with
		/// Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "Book introduction within Scripture text.(\\r)?\\n(\\r)?\\n" +
			"\\\\is B(\\r)?\\nAttempting to read EXO  Chapter: 1  Verse: 1",
			MatchType = MessageMatch.Regex)]
		public void FailWhenIntroSectionFollowsEmptyScriptureSection()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("A", @"\s");
			m_importer.ProcessSegment("B", @"\is");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expect an exception during import when intro material is interspersed with
		/// Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "Book introduction within Scripture text.(\\r)?\\n(\\r)?\\n" +
			"\\\\is B(\\r)?\\nAttempting to read EXO  Chapter: 1  Verse: 1",
			MatchType = MessageMatch.Regex)]
		public void FailWhenIntroSectionFollowsNotImportedNormalScriptureSection()
		{
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportAnnotations = true;
			m_importer.Settings.ImportBookIntros = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("A", @"\s");
			m_importer.ProcessSegment("My para", @"\p");
			m_importer.ProcessSegment("B", @"\is");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expect an exception during import when intro material is interspersed with
		/// Scripture and scripture is not imported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "Book introduction within Scripture text.(\\r)?\\n(\\r)?\\n" +
			"\\\\is B(\\r)?\\nAttempting to read EXO  Chapter: 1  Verse: 1",
			MatchType = MessageMatch.Regex)]
		public void FailWhenIntroSectionFollowsNormalScriptureSection()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("A", @"\s");
			m_importer.ProcessSegment("My para", @"\p");
			m_importer.ProcessSegment("B", @"\is");
		}
		#endregion

		#region ProcessSegment - Implicit Scripture Section Start
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a sequence of scripture text segments with intro sections marked as plain \s
		/// segments and an implicit Scripture section start.
		/// We will process this marker sequence:
		///    id mt s ip p v1 p v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment_ImplicitScrSectionStart()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process a main title (mt) *********************
			m_importer.ProcessSegment("Implicit Scripture Section Test", @"\mt");

			// begin first section (intro material)
			// ************** process an intro section head ************
			m_importer.ProcessSegment("Background Material", @"\s");

			// ************** process an intro paragraph **********
			m_importer.ProcessSegment("Intro paragraph text", @"\ip");

			// ************** process a Scripture paragraph **********
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse 1 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("verse one text", @"\v");

			// ************** process a Scripture paragraph **********
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse 2 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("verse two text", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Now check stuff
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual(2, book.SectionsOS.Count);
			IScrSection section1 = book.SectionsOS[0];
			Assert.AreEqual(02001000, section1.VerseRefMin);
			Assert.AreEqual(02001000, section1.VerseRefMax);
			IScrSection section2 = book.SectionsOS[1];
			Assert.AreEqual(02001001, section2.VerseRefMin);
			Assert.AreEqual(02001002, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a sequence of text segments with only an ID line. This is impossible when
		/// importing using our SF processing code, but it can happen using the Paratext 6
		/// import path because a bogus chapter number effectively kills the import. TE-9024.
		/// We will process this marker sequence:
		///    id
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment_DieAfterId()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Now check stuff
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(0, book.TitleOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, book.TitleOA[0].StyleName);
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section1 = book.SectionsOS[0];
			Assert.AreEqual(02001000, section1.VerseRefMin);
			Assert.AreEqual(02001000, section1.VerseRefMax);
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.HeadingOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead, section1.HeadingOA[0].StyleName);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.ContentOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroParagraph, section1.ContentOA[0].StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a sequence of text segments with only an ID line. This is impossible when
		/// importing using our SF processing code, but it can happen using the Paratext 6
		/// import path because a bogus chapter number effectively kills the import. TE-9024.
		/// We will process this marker sequence:
		///    id
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment_DieAfterId_TwoBooks()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(3, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(3, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Now check stuff
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			Assert.AreEqual(1, exodus.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(0, exodus.TitleOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, exodus.TitleOA[0].StyleName);
			Assert.AreEqual(1, exodus.SectionsOS.Count);
			IScrSection section1 = exodus.SectionsOS[0];
			Assert.AreEqual(02001000, section1.VerseRefMin);
			Assert.AreEqual(02001000, section1.VerseRefMax);
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.HeadingOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead, section1.HeadingOA[0].StyleName);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.ContentOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroParagraph, section1.ContentOA[0].StyleName);

			IScrBook leviticus = m_importer.ScrBook;
			Assert.AreEqual(1, leviticus.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(0, leviticus.TitleOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, leviticus.TitleOA[0].StyleName);
			Assert.AreEqual(1, leviticus.SectionsOS.Count);
			section1 = leviticus.SectionsOS[0];
			Assert.AreEqual(03001000, section1.VerseRefMin);
			Assert.AreEqual(03001000, section1.VerseRefMax);
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.HeadingOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead, section1.HeadingOA[0].StyleName);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.ContentOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroParagraph, section1.ContentOA[0].StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a sequence of text segments with only a title. This is impossible when
		/// importing using our SF processing code, but it can happen using the Paratext 6
		/// import path because a bogus chapter number effectively kills the import. TE-9024.
		/// We will process this marker sequence:
		///    id mt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment_DieAfterTitle()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process a main title (mt) *********************
			m_importer.ProcessSegment("Exodus", @"\mt");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Now check stuff
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual("Exodus", book.TitleOA[0].Contents.Text);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, book.TitleOA[0].StyleName);
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section1 = book.SectionsOS[0];
			Assert.AreEqual(02001000, section1.VerseRefMin);
			Assert.AreEqual(02001000, section1.VerseRefMax);
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.HeadingOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead, section1.HeadingOA[0].StyleName);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, section1.ContentOA[0].Contents.Length);
			Assert.AreEqual(ScrStyleNames.IntroParagraph, section1.ContentOA[0].StyleName);
		}
		#endregion

		#region DetectUnmappedMarkersInImport
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a sequence of scripture text segments containing unmapped markers and
		/// verify the results.
		/// We will process this marker sequence:
		///    id is ipnew
		///    c1 s p v1 pempty pnew{text}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetectUnmappedMarkersInImport()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// begin first section (intro material)
			// ************** process an intro section head, test MakeSection() method ************
			m_importer.ProcessSegment("Intro Section Head", @"\is");
			IScrBook book = m_importer.ScrBook;
			VerifyNewSectionExists(book, 0);

			// ************** process an intro paragraph, test MakeParagraph() method **********
			m_importer.ProcessSegment("Intro paragraph text", @"\ipnew");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Intro paragraph text", null);
			Assert.AreEqual(20, m_importer.ParaBldrLength);
			// verify completed intro section head was added to DB
			Assert.AreEqual(1, book.SectionsOS[0].HeadingOA.ParagraphsOS.Count);
			IStTxtPara heading = (IStTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"),
				heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Intro Section Head", null, DefaultVernWs);

			// begin second section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			// note: chapter number is not put in para now (it's saved for drop-cap location later)
			// verify state of NormalParaStrBldr
			Assert.AreEqual(0, m_importer.ParaBldrLength);
			Assert.AreEqual(1, m_importer.Chapter);

			// Make sure previous segment was added properly
			Assert.AreEqual(1, book.SectionsOS[0].ContentOA.ParagraphsOS.Count);
			IStTxtPara content = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("\\ipnew"), content.StyleRules);
			Assert.AreEqual(1, content.Contents.RunCount);
			AssertEx.RunIsCorrect(content.Contents, 0, "Intro paragraph text", null, DefaultVernWs);

			// ************** process a section head (for 1:1) *********************
			m_importer.ProcessSegment("Section Head One", @"\s");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head One", null);
			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 1);

			// verify refs of completed non-Scripture text section (1:0)
			Assert.AreEqual(2001000, book.SectionsOS[0].VerseRefMin);
			Assert.AreEqual(2001000, book.SectionsOS[0].VerseRefMax);

			// in first section (1:1), begin first content paragraph
			// ************** process a \p (for 1:1) *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse 1 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("verse one text", @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number");
			VerifyBldrRun(1, "1", "Verse Number");
			VerifyBldrRun(2, "verse one text", null);
			Assert.AreEqual(16, m_importer.ParaBldrLength);

			// in first section (1:1), begin second (empty) content paragraph
			// ************** process a \pempty  *********************
			m_importer.ProcessSegment("", @"\pempty");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(0, m_importer.ParaBldrLength);
			// Make sure new style was not added
			foreach (IStStyle style in m_scr.StylesOC)
			{
				Assert.IsTrue(style.Name != "pempty");
			}
			// verify completed paragraph was added to DB
			//IScrBook book = (IScrBook)CmObject.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
			Assert.AreEqual(1, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
			content = (IStTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), content.StyleRules);
			Assert.AreEqual(3, content.Contents.RunCount);

			// in first section (1:1), begin another content paragraph
			// ************** process a \pempty  *********************
			m_importer.ProcessSegment("some text", @"\pnew");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			Assert.AreEqual(9, m_importer.ParaBldrLength);
			// verify previous paragraph was discarded because it was empty
			//IScrBook book = (IScrBook)CmObject.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
			Assert.AreEqual(1, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify state of NormalParaStrBldr
			Assert.AreEqual(0, m_importer.ParaBldrLength);
			// verify previous paragraph was added to the DB
			//IScrBook book = (IScrBook)CmObject.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
			Assert.AreEqual(2, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
			content = (IStTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("\\pnew"), content.StyleRules);
			Assert.AreEqual(1, content.Contents.RunCount);

			// verify refs of completed scripture text section (1:1)
			Assert.AreEqual(2001001, book.SectionsOS[1].VerseRefMin);
			Assert.AreEqual(2001001, book.SectionsOS[1].VerseRefMax);
		}
		#endregion

		#region ProcessSegment02 Chapter Section Paragraph Verse sequences
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send unusual sequences of scripture text segments through ProcessSegment, and
		/// verify proper results.
		/// Our focus here: unusual sequences of chapter, section, para, verse
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment02_CSRPVsequences()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// TE-1796: This test wasn't doing anything, so it seems like a good place
			// to test that chapter numbers don't force a paragraph break.

			// Start section
			// ************** process a section head (for 1:1-2:1) *********************
			m_importer.ProcessSegment("Section Head One", @"\s");

			// begin first paragraph
			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			// verify state of NormalParaStrBldr
			//			Assert.AreEqual(1, m_importer.ParaBldrLength);
			Assert.AreEqual(1, m_importer.Chapter);
//			VerifyBldrRun(0, "1", "Chapter Number");

			// ************** process verse 1 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("one", @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number");
			VerifyBldrRun(1, "1", "Verse Number");
			VerifyBldrRun(2, "one", null);

			// ************** process another chapter in same para *********************
			// Note: preceding text run doesn't end with a space; must add one.
			int cchBldr = m_importer.ParaBldrLength;
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(2, m_importer.Chapter);
			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(0, section.ContentOA.ParagraphsOS.Count);

			// ************** process verse 1 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
			m_importer.ProcessSegment("two ", @"\v");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(cchBldr + 7, m_importer.ParaBldrLength);
			VerifyBldrRun(2, "one ", null);
			VerifyBldrRun(3, "2", "Chapter Number");
			VerifyBldrRun(4, "1", "Verse Number");
			VerifyBldrRun(5, "two ", null);

			// ************** Life's short -- process one more chapter in same para *********************
			// Note: preceding text ends with a space; must NOT add one.
			cchBldr = m_importer.ParaBldrLength;
			m_importer.TextSegment.FirstReference = new BCVRef(2, 3, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 3, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(3, m_importer.Chapter);

			// ************** process verse 1 text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 3, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 3, 1);
			m_importer.ProcessSegment("three", @"\v");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(cchBldr + 7, m_importer.ParaBldrLength);
			VerifyBldrRun(5, "two ", null);
			VerifyBldrRun(6, "3", "Chapter Number");
			VerifyBldrRun(7, "1", "Verse Number");
			VerifyBldrRun(8, "three", null);

			// ************** finalize **************
			m_importer.FinalizeImport();

			section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(02001001, section.VerseRefMin);
			Assert.AreEqual(02003001, section.VerseRefMax);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11one 21two 31three", para.Contents.Text);
			// test c3 v9 s v11 -missing p's should not cause a problem

			// test c1 v4 c2 s q v5-10 -section ref should be 2:5-10

			// test c1 v4 c2 s qtext v5-10 -REVIEW:section ref should be 2:1-10

			// test c1 p v4 s qtext c3 p v5-10 -section ref should be 1:4-3:10

			// s p c v   permutations
		}
		#endregion

		#region ProcessSegment03 StartOfBook
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test processing the ID line
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment03_StartOfBook()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("EXO This is the book of Exodus", @"\id");

			Assert.IsNotNull(m_importer.ScrBook);
			Assert.AreEqual(2, m_importer.ScrBook.CanonicalNum);
			Assert.AreEqual("This is the book of Exodus", m_importer.ScrBook.IdText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a book title with no text and confirm that there is an empty paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BookTitle_EmptyPara()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			Assert.AreEqual(1, m_importer.ScrBook.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(0, m_importer.ScrBook.SectionsOS.Count);
			Assert.IsTrue(m_importer.HvoTitle > 0);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.IsTrue(book.TitleOA.IsValidObject); //empty title
			Assert.AreEqual(book.TitleOA.Hvo, m_importer.HvoTitle);
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(0, book.SectionsOS.Count); // empty seq of sections
			Assert.AreEqual("EXO", book.BookId);
			//	Assert.AreEqual(2, book.CanonOrd);

			// ************** process a main title *********************
			m_importer.ProcessSegment("", @"\mt");

			// begin first section (intro material)
			// ************** process an intro section head, test MakeSection() method ************
			m_importer.ProcessSegment("Background Material", @"\is");
			Assert.AreEqual(null, m_importer.ScrBook.Name.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(1, m_importer.ScrBook.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(2, m_importer.BookNumber);
			Assert.IsNotNull(m_importer.CurrentSection);
		}

		#endregion

		#region ProcessSegment04 Character Styles
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send unusual sequences of scripture text segments through ProcessSegment, and
		/// verify proper results.
		/// Our focus here: unusual sequences for character styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment04_CharStyles()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a character style, no end marker, terminated by another char style
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("text with char style", @"\kw");
			m_importer.ProcessSegment("text with another char style", @"\gls");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(2, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "text with char style", "Key Word");
			VerifyBldrRun(1, "text with another char style", "Gloss");
			Assert.AreEqual(48, m_importer.ParaBldrLength);

			// ******** test a character style, no end marker, terminated by text marked
			//  with the same char style
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("text with char style", @"\kw");
			m_importer.ProcessSegment(" text marked with same char style", @"\kw");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "text with char style text marked with same char style",
				"Key Word");
			Assert.AreEqual(53, m_importer.ParaBldrLength);

			// ******** test a character style, no end marker, terminated by a footnote
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("text with char style", @"\kw");
			string sFootnoteSegment = "footnote text";
			m_importer.ProcessSegment(sFootnoteSegment, @"\f");
			m_importer.ProcessSegment(" ", @"\vt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "text with char style", "Key Word");

			VerifySimpleFootnote(0, sFootnoteSegment);

			// ******** test a character style, no end marker, terminated by a paragraph
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("text with char style", @"\kw");
			m_importer.ProcessSegment("text in next para", @"\q");
			// verify the first paragraph, from the db
			IStText text = m_importer.SectionContent;
			IStTxtPara para = (IStTxtPara)text.ParagraphsOS[text.ParagraphsOS.Count - 1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "text with char style", "Key Word", DefaultVernWs);
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "text in next para", null);

			// ******** test a character style, no end marker, terminated by a section head
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("text with char style", @"\kw");
			m_importer.ProcessSegment("Section Head", @"\s");
			// verify the first paragraph, from the db
			// use StText var "text" from prior section, since we just made a new section
			//text = new StText(Cache, m_importer.HvoSectionContent); //no!
			para = (IStTxtPara)text.ParagraphsOS[text.ParagraphsOS.Count - 1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "text with char style", "Key Word", DefaultVernWs);
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Section Head", null);

			// ******** test a character style, no end marker, terminated by a chapter
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("text with char style", @"\kw");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 5, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 5, 0);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(5, m_importer.Chapter);
			m_importer.ProcessSegment("", @"\p");
			// verify the first paragraph, from the db
			text = m_importer.SectionContent;
			para = (IStTxtPara)text.ParagraphsOS[text.ParagraphsOS.Count - 1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "text with char style", "Key Word", DefaultVernWs);
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.ParaBldrLength);

			// ******** test a character style, no end marker, terminated by a verse
			m_importer.ProcessSegment("text with char style", @"\kw");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 5, 8);
			m_importer.TextSegment.LastReference = new BCVRef(2, 5, 10);
			m_importer.ProcessSegment("verse text", @"\v");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(4, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "5", "Chapter Number");
			VerifyBldrRun(1, "text with char style", "Key Word");
			VerifyBldrRun(2, "8-10", "Verse Number");
			VerifyBldrRun(3, "verse text", null);
		}
		#endregion

		#region ProcessSegment05 Footnotes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send unusual sequences of scripture text segments through ProcessSegment, and
		/// verify proper results.
		/// Our focus here: unusual sequences for footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment05_Footnotes()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.ProcessSegment("poetry text ", @"\q");
			m_importer.ProcessSegment("footnote text ", @"\f");
			m_importer.ProcessSegment("another footnote text ", @"\f");
			m_importer.ProcessSegment("more poetry text ", @"\vt");
			// verify footnotes, from the db
			VerifySimpleFootnote(0, "footnote text");
			VerifySimpleFootnote(1, "another footnote text");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(4, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "poetry text", null);
			VerifyBldrFootnoteOrcRun(1, 0);
			VerifyBldrFootnoteOrcRun(2, 1);
			VerifyBldrRun(3, " more poetry text ", null);

			// ******** test a footnote, no end marker, terminated by a paragraph
			m_importer.ProcessSegment("poetry text ", @"\q");
			m_importer.ProcessSegment("footnote text 2 ", @"\f");
			// make sure footnote orc is in the builder
			VerifyBldrFootnoteOrcRun(1, 2);
			m_importer.ProcessSegment("this is a paragraph ", @"\p");
			// verify the first paragraph, from the db
			IStText text = m_importer.SectionContent;
			IStTxtPara para = (IStTxtPara)text.ParagraphsOS[text.ParagraphsOS.Count - 1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			ITsString tssPara = para.Contents;
			Assert.AreEqual(2, tssPara.RunCount);
			AssertEx.RunIsCorrect(tssPara, 0, "poetry text", null, DefaultVernWs);
			// Verify the Orc in para run 1
			VerifyFootnoteMarkerOrcRun(tssPara, 1);
			//verify the footnote, from the db
			VerifySimpleFootnote(2, "footnote text 2");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "this is a paragraph ", null);

			// ******** test a footnote, no end marker, terminated by a section head
			m_importer.ProcessSegment("poetry text", @"\q");
			m_importer.ProcessSegment("fishnote text", @"\f");
			m_importer.ProcessSegment("so long and thanks for all the fish", @"\s");
			// verify the previous paragraph, from the db
			// use StText var "text" from prior section, since we just made a new section
			//text = new StText(Cache, m_importer.HvoSectionContent); //no!
			para = (IStTxtPara)text.ParagraphsOS[text.ParagraphsOS.Count - 1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			tssPara = para.Contents;
			Assert.AreEqual(2, tssPara.RunCount);
			AssertEx.RunIsCorrect(tssPara, 0, "poetry text", null, DefaultVernWs);
			// Verify the Orc in para run 1
			VerifyFootnoteMarkerOrcRun(tssPara, 1);
			//verify the footnote, from the db
			VerifySimpleFootnote(3, "fishnote text");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "so long and thanks for all the fish", null);

			// ******** test a footnote, no end marker, terminated by a chapter
			m_importer.ProcessSegment("poetry text", @"\q");
			m_importer.ProcessSegment("footnote text 4", @"\f");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 6, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 6, 0);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(6, m_importer.Chapter);
			m_importer.ProcessSegment("poetry text", @"\q");
			// verify the previous paragraph, from the db
			text = m_importer.SectionContent;
			para = (IStTxtPara)text.ParagraphsOS[text.ParagraphsOS.Count - 1];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			tssPara = para.Contents;
			Assert.AreEqual(2, tssPara.RunCount);
			AssertEx.RunIsCorrect(tssPara, 0, "poetry text", null, DefaultVernWs);
			// Verify the Orc in para run 1
			VerifyFootnoteMarkerOrcRun(tssPara, 1);
			//verify the footnote, from the db
			VerifySimpleFootnote(4, "footnote text 4");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(12, m_importer.ParaBldrLength);

			// ******** test a character style, no end marker, terminated by a verse
			m_importer.ProcessSegment("footnote text 5", @"\f");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 6, 9);
			m_importer.TextSegment.LastReference = new BCVRef(2, 6, 11);
			m_importer.ProcessSegment("verse text", @"\v");
			//verify the footnote, from the db
			VerifySimpleFootnote(5, "footnote text 5");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(5, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "6", "Chapter Number");
			VerifyBldrRun(1, "poetry text", null);
			VerifyBldrFootnoteOrcRun(2, 5);
			VerifyBldrRun(3, "9-11", "Verse Number");
			VerifyBldrRun(4, "verse text", null);

			// ******** test a footnote, char style text within
			m_importer.ProcessSegment("poetry text ", @"\q");
			m_importer.ProcessSegment("beginning of footnote", @"\f");
			m_importer.ProcessSegment("text with char style", @"\kw");
			m_importer.ProcessSegment("remainder of footnote ", @"\kw*");
			m_importer.ProcessSegment(" ", @"\vt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "poetry text", null);
			int iFootnoteIndex = 6;
			VerifyBldrFootnoteOrcRun(1, iFootnoteIndex);
			m_importer.FinalizeImport();
			// verify the footnotes in the DB
			IStFootnote footnote = GetFootnote(iFootnoteIndex);
			AssertEx.RunIsCorrect(footnote.FootnoteMarker, 0,
				"g", ScrStyleNames.FootnoteMarker, m_wsVern);
			IFdoOwningSequence<IStPara> footnoteParas = footnote.ParagraphsOS;
			Assert.AreEqual(1, footnoteParas.Count);
			para = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual
				(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph),
				para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(((IStTxtPara)footnoteParas[0]).Contents, 0,
				"beginning of footnote", null, m_wsVern);
			AssertEx.RunIsCorrect(((IStTxtPara)footnoteParas[0]).Contents, 1,
				"text with char style", "Key Word", m_wsVern);
			AssertEx.RunIsCorrect(((IStTxtPara)footnoteParas[0]).Contents, 2,
				"remainder of footnote", null, m_wsVern);

			// test a footnote, char style text within, no char style end marker, term by f*
			// test a footnote, char style text within, no end markers, terminated by p
			// test a footnote within a section head paragraph
			// test a footnote terminated by id
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test footnotes on section head with a back translation of the section head
		///		\id MRK
		///		\mt Gospel of Mark
		///		\c 1
		///		\s new section
		///		\ft footnote1
		///		\bts section head BT
		///		\v 1
		///		\vt This is my first verse text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment05_FootnoteOnSectionWithBT()
		{
			m_settings.ImportBackTranslation = true;

			// delete the book of Mark if it exists
			IScrBook mark = m_scr.FindBook(41);
			if (mark != null)
				m_scr.ScriptureBooksOS.Remove(mark);

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.ProcessSegment("footnote1", @"\f");
			m_importer.ProcessSegment("BT for section", @"\bts");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("verse one text", @"\vt");
			m_importer.FinalizeImport();

			// Verify the imported data
			mark = m_importer.UndoInfo.ImportedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual("footnote1", para.Contents.Text);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11verse one text", para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section" + StringUtils.kChObject, para.Contents.Text);

			// verify the BT text
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			Assert.AreEqual("BT for section", trans.Translation.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test footnotes on section head after a back translation of the section head
		///		\id MRK
		///		\mt Gospel of Mark
		///		\c 1
		///		\s new section
		///		\bts BT for section
		///		\f footnote1
		///		\v 1
		///		\vt This is my first verse text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment05_FootnoteAfterBT()
		{
			m_settings.ImportBackTranslation = true;

			// delete the book of Mark if it exists
			IScrBook mark = m_scr.FindBook(41);
			if (mark != null)
				m_scr.ScriptureBooksOS.Remove(mark);

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.ProcessSegment("BT for section", @"\bts");
			m_importer.ProcessSegment("footnote1", @"\f");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("verse one text", @"\vt");
			m_importer.FinalizeImport();

			// Verify the imported data
			mark = m_importer.UndoInfo.ImportedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			Assert.AreEqual(1, footnote.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual("footnote1", para.Contents.Text);

			// verify the section head text
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section" + StringUtils.kChObject, para.Contents.Text);

			// verify the section content text
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11verse one text", para.Contents.Text);

			// verify the BT text for the section head
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			Assert.AreEqual("BT for section", trans.Translation.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test inline footnotes followed by a character style to make sure the footnote
		/// gets terminated properly
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt verse one text
		///		\f this is a footnote
		///		\vt some more verse text
		///		\em emphasis
		///		\em* done
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment05_FootnoteFollowedByVerseText()
		{
			m_settings.ImportBackTranslation = true;

			// delete the book of Mark if it exists
			IScrBook mark = m_scr.FindBook(41);
			if (mark != null)
				m_scr.ScriptureBooksOS.Remove(mark);

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("verse one text.", @"\vt");
			m_importer.ProcessSegment("this is a footnote", @"\f");
			m_importer.ProcessSegment("some more verse text ", @"\vt");
			m_importer.ProcessSegment("emphasis ", @"\em");
			m_importer.ProcessSegment("done", @"\em*");
			m_importer.FinalizeImport();

			// Verify the imported data
			mark = m_importer.UndoInfo.ImportedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			Assert.AreEqual(1, footnote.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual("this is a footnote", para.Contents.Text);

			// verify the section head text
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);

			// verify the section content text
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11verse one text." + StringUtils.kChObject + "some more verse text emphasis done", para.Contents.Text);
		}
		#endregion

		#region ProcessSegment06 Initial marker maps to character style
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send unusual sequences of scripture text segments through ProcessSegment, and
		/// verify proper results.
		/// Our focus here: initial marker after \id line maps to character style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment06_StartWithCharStyle()
		{
			// initialize - process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a character style
			m_importer.ProcessSegment("text", @"\quot");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "text", "Quoted Text");
			Assert.AreEqual(4, m_importer.ParaBldrLength);
			// ******** terminate character run by returning to default paragraph characters
			m_importer.ProcessSegment(" continuation", @"\vt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(2, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(1, " continuation", string.Empty);
			Assert.AreEqual(17, m_importer.ParaBldrLength);

			// ******** Now send a paragraph marker to force finalizing the previous
			// ********  paragraph and make sure everything's kosher.
			m_importer.ProcessSegment("An intro paragraph", @"\ip");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "An intro paragraph", string.Empty);
			Assert.AreEqual(18, m_importer.ParaBldrLength);

			// verify the first paragraph, from the db
			IStText text = m_importer.SectionContent;
			Assert.AreEqual(1, text.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)text.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Paragraph"), para.StyleRules);
			Assert.AreEqual(2, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "text", "Quoted Text", DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents, 1, " continuation", null, DefaultVernWs);
		}
		#endregion

		#region ProcessSegment07 Text before a main title
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the case where we have text before we have a main title (which should start
		/// a new section).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment07_TextBeforeMainTitle()
		{
			// initialize - process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("Philemon", @"\h_");
			m_importer.ProcessSegment("Paul's Letter to", @"\mt2_");
			IScrBook book = m_importer.ScrBook;
			VerifyNewSectionExists(book, 0);
			m_importer.ProcessSegment("PHILEMON", @"\mt1");
			m_importer.ProcessSegment("Introduction", @"\is_");
			m_importer.ProcessSegment("Philemon was a prominent", @"\ip_");

			// We expect that we have two sections now: one for \h, \mt2, the other for \mt1,
			// \is and \ip
			m_importer.FinalizeImport();
			VerifyNewSectionExists(book, 1);
		}
		#endregion

		#region ProcessSegment08 Book whose chapters are split across files (TE-515)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate a book whose chapters are split across files. (Jira task number is TE-515.)
		/// We will process this marker sequence:
		///    id mt
		///    s p v1 v2
		///    id
		///    c2 s p v1 v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessSegment08_MultipleFilesPerBook()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.ProcessSegment("Exodus", @"\mt");
			m_importer.ProcessSegment("The Israelites Opressed", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 2);
			m_importer.ProcessSegment("", @"\v");
			IScrBook book = m_importer.ScrBook;
			VerifyNewSectionExists(book, 0);

			// Simulate a second file
			m_importer.ProcessSegment("", @"\id");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("The Birth of Moses", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 2);
			m_importer.ProcessSegment("", @"\v");
			VerifyNewSectionExists(book, 1);
		}
		#endregion

		#region Process Stanza Break
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a sequence of Poetry text segments through ProcessSegment, including correctly
		/// used Stanza Break (\b) fields.
		/// We will process this marker sequence:
		///    id c1 q1 v1 q2 q1 v2 q2 b q1 v3 q2 q1 v4 q2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessStanzaBreak()
		{
			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(19, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(19, 0, 0);
			m_importer.ProcessSegment("PSA", @"\id");
			Assert.AreEqual(19, m_importer.BookNumber);
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("PSA", book.BookId);

			// ************** process Chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(19, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(19, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process first stanza *********************
			m_importer.ProcessSegment("", @"\q1");
			m_importer.TextSegment.FirstReference = new BCVRef(19, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(19, 1, 1);
			m_importer.ProcessSegment("Blessed is the man who walketh not in the counsel of the ungodly,", @"\v");
			m_importer.ProcessSegment("nor sitteth in the seat of scoffers.", @"\q2");
			m_importer.ProcessSegment("", @"\q1");
			m_importer.TextSegment.FirstReference = new BCVRef(19, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(19, 1, 2);
			m_importer.ProcessSegment("He doesn't drink smoke or chew,", @"\v");
			m_importer.ProcessSegment("nor go with girls who do.", @"\q2");

			// ************** process stanza break *********************
			m_importer.ProcessSegment(String.Empty, @"\b");

			// ************** process second stanza *********************
			m_importer.ProcessSegment("", @"\q1");
			m_importer.TextSegment.FirstReference = new BCVRef(19, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(19, 1, 3);
			m_importer.ProcessSegment("His delight is in the law of the LORD,", @"\v");
			m_importer.ProcessSegment("and in His law he doth meditate day and night.", @"\q2");
			m_importer.ProcessSegment("", @"\q1");
			m_importer.TextSegment.FirstReference = new BCVRef(19, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(19, 1, 4);
			m_importer.ProcessSegment("He shall be like a pecan tree in a swamp,", @"\v");
			m_importer.ProcessSegment("deep in the heart of Dixie.", @"\q2");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(9, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara stanzaBreakPara = (IStTxtPara)section.ContentOA.ParagraphsOS[4];
			Assert.IsNull(stanzaBreakPara.Contents.Text);
			Assert.AreEqual("Stanza Break",
				stanzaBreakPara.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region FinalizePrevSection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FinalizePrevSection method when the introduction heading and content is
		/// empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FinalizePrevSection_HeadingAndContentEmpty()
		{
			// Set up some test data.
			IScrBook gen = AddBookToMockedScripture(1, "Genesis");
			IScrSection section = AddSectionToMockedBook(gen, true);

			m_importer.Settings.ImportBookIntros = true;
			m_importer.CallFinalizePrevSection(section, ImportDomain.Main, false);

			Assert.AreEqual(1, gen.SectionsOS.Count);
			Assert.IsTrue(section.IsIntro);
			Assert.AreEqual(new ScrReference(1, 1, 0, m_scr.Versification),
				ReflectionHelper.GetField(m_importer, "m_firstImportedRef") as ScrReference);

			ITsStrFactory fact = TsStrFactoryClass.Create();
			ITsString tssExpected = fact.MakeString(string.Empty, m_wsVern);

			// Verify the section head
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssExpected, para.Contents);

			// Verify the first paragraph in the section
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssExpected, para.Contents);
		}
		#endregion

		#region Skipping intro material when option set
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test import skipping all intro material when the option is not set to
		/// include intro material
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SkipIntroMaterial()
		{
			m_importer.Settings.ImportBookIntros = false;
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// TE-6716 - Tests correct import of Title Secondary when skipping introduction
			m_importer.ProcessSegment("The exciting Exodus exit", @"\mt2");

			m_importer.ProcessSegment("Exodus", @"\mt");
			m_importer.ProcessSegment("An Introduction to Exodus", @"\is");
			m_importer.ProcessSegment("", @"\ip");
			m_importer.ProcessSegment("Here is some intro text", @"\vt");
			// TE-7716 - Added character style run to intro and scripture. Skipped intro was causing
			// a paragraph style to be created for the ending marker which then caused scripture text
			// following end marker to be skipped.
			m_importer.ProcessSegment("Begin character run", @"\em");
			m_importer.ProcessSegment("after character run", @"\em*");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("My First Section", @"\s");
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Some verse one text. ", @"\vt");
			m_importer.ProcessSegment("Emphasis", @"\em");
			m_importer.ProcessSegment(" more text", @"\em*");
			m_importer.FinalizeImport();
			IScrBook book = m_importer.ScrBook;

			// Look to see how many sections were created in the book
			Assert.AreEqual(1, book.SectionsOS.Count);

			// Verify the title secondary (TE-6716) and book title
			IStTxtPara para = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
			Assert.AreEqual("The exciting Exodus exit\u2028Exodus",
				para.Contents.Text);

			// Verify the section head
			IScrSection section = book.SectionsOS[0];
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("My First Section", para.Contents.Text);

			// Look at the text of the first paragraph in the section
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Some verse one text. Emphasis more text", para.Contents.Text);
		}
		#endregion

		#region Footnote Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that more than 26 footnotes in a book causes it to wrap back around to "a" for
		/// the marker and that it restarts at "a" for each new book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteMarkerWrapping()
		{
			// make sure we are restarting the sequence
			m_scr.RestartFootnoteSequence = true;
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.ProcessSegment("Intro Para with footnotes", @"\ip");

			for (char ch = 'a'; ch <= 'z'; ch++)
				m_importer.ProcessSegment("footnote text " + ch, @"\f");

			m_importer.ProcessSegment("footnote text a again :-)", @"\f");
			m_importer.ProcessSegment("Another Para", @"\ip");
			VerifySimpleFootnote(0, "footnote text a");
			VerifySimpleFootnote(25, "footnote text z");
			VerifySimpleFootnote(26, "footnote text a again :-)");

			// Next book
			m_importer.TextSegment.FirstReference = new BCVRef(3, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(3, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("footnote text a for book 3", @"\f");
			m_importer.ProcessSegment("Another Para", @"\ip");
			// verify footnotes, from the db
			VerifySimpleFootnote(0, "footnote text a for book 3");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that footnotes having user-customized properties are imported properly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteMarkerCustomProperties()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.ProcessSegment("Section One ", @"\s");
			m_importer.ProcessSegment(" ", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("This is text. ", @"\vt");
			m_importer.ProcessSegment("footnote text ", @"\f");
			m_importer.ProcessSegment(" Another sentence. ", @"\vt");
			m_importer.ProcessSegment("second footnote ", @"\x");
			m_importer.ProcessSegment(" Third sentence ", @"\x*");
			m_importer.ProcessSegment("", @"\p");
			// verify footnotes, from the db
			VerifySimpleFootnote(0, "footnote text", "a");
			VerifySimpleFootnote(1, "second footnote", 1, null,
				"Note Cross-Reference Paragraph", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that footnotes get the correct spacing on either side of the marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteMarkerCorrectSpacing()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("before ", @"\vt");
			m_importer.ProcessSegment("footnote text ", @"\f");
			m_importer.ProcessSegment("after ", @"\vt");

			// verify the run of text with the footnote marker in it
			string expected = "11before" + StringUtils.kChObject + " after ";
			Assert.AreEqual(expected, m_importer.NormalParaStrBldr.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that footnotes get the correct character styles including the use of the
		/// optional \ft* marker for default character style runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteWithCharacterStyles()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("before ", @"\vt");
			m_importer.ProcessSegment("footnote text ", @"\f");
			m_importer.ProcessSegment("emphasis", @"\em");
			m_importer.ProcessSegment(" regular ", @"\ft");
			m_importer.ProcessSegment("trailing text", @"\ft*");
			m_importer.ProcessSegment("after ", @"\vt");

			// verify the run of text with the footnote marker in it
			string expected = "11before" + StringUtils.kChObject + " after ";
			Assert.AreEqual(expected, m_importer.NormalParaStrBldr.Text);
			VerifySimpleFootnote(0, "footnote text emphasis regular trailing text",
			1, "a", "Note General Paragraph", 3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send some segments to simulate a data stream that ends with a footnote, whose last
		/// segment is a run of text with a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EOFAtFootnoteCharStyle()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote with an embedded keyword
			m_importer.ProcessSegment("poetry text", @"\q");
			m_importer.ProcessSegment("footnote text", @"\f");
			m_importer.ProcessSegment("keyword in footnote", @"\kw");
			m_importer.FinalizeImport();
			// verify footnotes, from the db
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("poetry text" + StringUtils.kChObject.ToString(),
				para.Contents.Text);
			ITsString tss = VerifyComplexFootnote(0, "footnote text", 2);
			Assert.AreEqual("keyword in footnote", tss.get_RunText(1));
			Assert.AreEqual("Key Word",
				tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		/// \p \f |fm +|fm*|fr 1.2: |fr*footynote\vt more
		/// Jira number is TE-2433
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandlePseudoUSFMStyleFootnotes_ExplicitFootnoteEnd()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("too ", @"\vt");
			m_importer.ProcessSegment("", @"\f");
			m_importer.ProcessSegment("+", "|fm ");
			m_importer.ProcessSegment("", "|fm*");
			m_importer.ProcessSegment("1.2: ", "|fr ");
			m_importer.ProcessSegment("footynote", "|fr*");
			m_importer.ProcessSegment("more ", @"\vt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11too" + StringUtils.kChObject.ToString() + " more",
				para.Contents.Text, "TE-2431: Space should follow footnote marker");
			VerifySimpleFootnote(0, "footynote", "a");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works: \f |fm +|fm*|fr 1.2: |fr*footynote
		/// Jira number is TE-2433
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandlePseudoUSFMStyleFootnotes_ImplicitFootnoteEnd()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("too ", @"\vt");
			m_importer.ProcessSegment("", @"\f");
			m_importer.ProcessSegment("+", "|fm ");
			m_importer.ProcessSegment("", "|fm*");
			m_importer.ProcessSegment("1.2: ", "|fr ");
			m_importer.ProcessSegment("footynote", "|fr*");
			m_importer.ProcessSegment("more", @"\vt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11too" + StringUtils.kChObject.ToString() + " more",
				para.Contents.Text, "TE-2431: Space should follow footnote marker");
			VerifySimpleFootnote(0, "footynote", "a");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works: \f + \fr 1.2: \ft footynote \vt
		/// Jira number is TE-2433
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandlePseudoUSFMStyleFootnotes_NonInline_ImplicitFootnoteEnd()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Some ", @"\v");
			m_importer.ProcessSegment("+", @"\f");
			m_importer.ProcessSegment("1.2", @"\fr");
			m_importer.ProcessSegment("footynote", @"\ft");
			m_importer.ProcessSegment("more verse text.", @"\vt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Some" + StringUtils.kChObject.ToString() + " more verse text.",
				para.Contents.Text, "TE-2431: Space should follow footnote marker");
			VerifySimpleFootnote(0, "footynote", "a");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works: \v 1 \f + \ft footynote \vt
		/// Jira number is TE-7706
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandlePseudoUSFMStyleFootnotes_NonInline_ImmediatelyAfterVerseNumber()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("footynote ", @"\ft");
			m_importer.ProcessSegment("Verse text. ", @"\vt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsString tssPara = para.Contents;
			Assert.AreEqual(4, tssPara.RunCount);
			Assert.AreEqual(1, exodus.FootnotesOS.Count);
			AssertEx.RunIsCorrect(tssPara, 0, "1", ScrStyleNames.ChapterNumber, m_wsVern);
			AssertEx.RunIsCorrect(tssPara, 1, "1", ScrStyleNames.VerseNumber, m_wsVern);
			VerifyFootnote(exodus.FootnotesOS[0], para, 2);
			AssertEx.RunIsCorrect(tssPara, 3, " Verse text.", null, m_wsVern);
			VerifySimpleFootnote(0, "footynote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \f + \ft In Greek this is the same as \fq Joshua \vt
		/// Jira number is TE-7709
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandlePseudoUSFMStyleFootnotes_NonInline_FnEndsWithFnCharStyle()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Some ", @"\v");
			m_importer.ProcessSegment("+", @"\f");
			m_importer.ProcessSegment("In Greek this is the same as ", @"\ft");
			m_importer.ProcessSegment("Joshua ", @"\fq");
			m_importer.ProcessSegment("more verse text.", @"\vt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsString tssPara = para.Contents;
			Assert.AreEqual(5, tssPara.RunCount);
			Assert.AreEqual(1, exodus.FootnotesOS.Count);
			AssertEx.RunIsCorrect(tssPara, 0, "1", ScrStyleNames.ChapterNumber, m_wsVern);
			AssertEx.RunIsCorrect(tssPara, 1, "1", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(tssPara, 2, "Some", null, m_wsVern);
			VerifyFootnote(exodus.FootnotesOS[0], para, 6);
			AssertEx.RunIsCorrect(tssPara, 4, " more verse text.", null, m_wsVern);
			ITsString tssFootnote = VerifyComplexFootnote(0, "In Greek this is the same as ", 2);
			AssertEx.RunIsCorrect(tssFootnote, 1, "Joshua", "Quoted Text", m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the psuedo-USFM data as produced by our old-style Toolbox export
		/// works: \f + |ft Lev. 1:2 \vt
		/// </summary>
		/// <remarks>Jira number is TE-4877</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandlePseudoUSFMStyleFootnotes_ToolboxExportFormat()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment(" ", @"\v");
			m_importer.ProcessSegment("too ", @"\vt");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("Lev. 1:2 ", "|ft ");
			m_importer.ProcessSegment("more ", @"\vt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11too" + StringUtils.kChObject.ToString() + " more",
				para.Contents.Text, "TE-4877: Footnote text should not be stuck in Scripture");
			VerifySimpleFootnote(0, "Lev. 1:2", "a");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the psuedo-USFM data as produced by our Toolbox interleaved
		/// export works: \vt \f + |ft \vt \btvt \btf + |ft \btvt
		/// </summary>
		/// <remarks>Jira number is TE-4877</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandlePseudoUSFMStyleFootnotes_ToolboxExportFormatBt()
		{
			m_settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment(" ", @"\v");
			m_importer.ProcessSegment("tambien ", @"\vt");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("Palabras ", "|ft ");
			m_importer.ProcessSegment("mas ", @"\vt");
			m_importer.ProcessSegment("too ", @"\btvt");
			m_importer.ProcessSegment("+ ", @"\btf");
			m_importer.ProcessSegment("Words ", "|ft ");
			m_importer.ProcessSegment("more ", @"\btvt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11tambien" + StringUtils.kChObject.ToString() + " mas",
				para.Contents.Text, "TE-4877: Footnote text should not be stuck in Scripture");
			VerifySimpleFootnote(0, "Palabras", "a");

			// Verify BT text
			ICmTranslation trans = para.GetBT();
			ITsString tssBT = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(5, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "too", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);
			AssertEx.RunIsCorrect(tssBT, 4, " more", null, m_wsAnal);

			// Verify BT of footnote
			IStTxtPara footnotePara = (IStTxtPara)GetFootnote(0).ParagraphsOS[0];
			ICmTranslation footnoteBT = footnotePara.GetBT();
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			AssertEx.RunIsCorrect(tssFootnoteBT, 0, "Words", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a footnote using curly brace notation, such that the end-
		/// marker for the elements within the footnote are the same as the end-marker for the
		/// footnote itself.
		/// Jira # is TE-7683.
		/// We will process this marker sequence:
		///    id c1 p v1 vt |f{|fr{}}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteWithCurlyBraceEndMarkers()
		{
			// Note that the overidden version of InitializeImportSettings in this class sets
			// up the additional mappings we need for this test.
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse number *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");

			// ************** process vernacular text and footnote ***************
			m_importer.ProcessSegment("Sahm verz teckst", @"\vt");
			m_importer.ProcessSegment("+ ", @"|f{");
			m_importer.ProcessSegment("Ezo 2:1 ", @"|fr{");
			m_importer.ProcessSegment("feay fye fow fum", @"}");
			m_importer.ProcessSegment(" Za wresd uv verz vahn ", @"}");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(5, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "Sahm verz teckst", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			AssertEx.RunIsCorrect(tssPara, 4, " Za wresd uv verz vahn", null, DefaultVernWs);

			// verify the footnote, from the db
			VerifySimpleFootnote(0, "feay fye fow fum");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unit test for FindCorrespondingFootnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCorrespondingFootnote()
		{
			IScrFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_importer.CurrParaFootnotes.Add(new FootnoteInfo(footnote, ScrStyleNames.NormalFootnoteParagraph));
			footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_importer.CurrParaFootnotes.Add(new FootnoteInfo(footnote, ScrStyleNames.NormalFootnoteParagraph));
			footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_importer.CurrParaFootnotes.Add(new FootnoteInfo(footnote, "Note Cross-Reference Paragraph"));
			List<FootnoteInfo> footnotes = m_importer.CurrParaFootnotes;
			Assert.AreEqual(((FootnoteInfo)footnotes[0]).footnote,
				m_importer.FindCorrespondingFootnote(3456, ScrStyleNames.NormalFootnoteParagraph));
			Assert.AreEqual(((FootnoteInfo)footnotes[0]).footnote,
				m_importer.FindCorrespondingFootnote(7890, ScrStyleNames.NormalFootnoteParagraph));
			Assert.IsNull(m_importer.FindCorrespondingFootnote(3456, "Note Cross-Reference Paragraph"));
			Assert.AreEqual(((FootnoteInfo)footnotes[1]).footnote,
				m_importer.FindCorrespondingFootnote(3456, ScrStyleNames.NormalFootnoteParagraph));
			Assert.AreEqual(((FootnoteInfo)footnotes[2]).footnote,
				m_importer.FindCorrespondingFootnote(3456, "Note Cross-Reference Paragraph"));
			Assert.IsNull(m_importer.FindCorrespondingFootnote(3456, "Note Cross-Reference Paragraph"));
			Assert.AreEqual(((FootnoteInfo)footnotes[1]).footnote,
				m_importer.FindCorrespondingFootnote(7890, ScrStyleNames.NormalFootnoteParagraph));
		}
		#endregion

		#region Picture Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data doesn't crash when the picture is missing:
		///   \fig Desc|Cat|Size|Loc|Copy|Cap|Ref\fig*
		/// Jira number is TE-2935
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStylePicturesPictureMissing()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a picture
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			string fileName = MiscUtils.IsUnix ? "/the Answer is 42.jpg" : @"Q:\the\Answer\is\42.jpg";
			m_importer.ProcessSegment("User-supplied picture|" +  fileName +
				"|col|EXO 1--1||Caption for junk.jpg|", @"\fig");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

			Assert.AreEqual("1" + StringUtils.kChObject.ToString(),
				para.Contents.Text);
			ITsString tss = para.Contents;
			Assert.AreEqual(2, tss.RunCount);
			string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
			Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
			ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);

			Assert.AreEqual("Caption for junk.jpg", picture.Caption.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(fileName, picture.PictureFileRA.InternalPath);
			Assert.AreEqual(picture.PictureFileRA.InternalPath, picture.PictureFileRA.AbsoluteInternalPath);
			byte odt = Convert.ToByte(sObjData[0]);
			Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \fig Desc|Cat|Size|Loc|Copy|Cap|Ref
		/// Jira number is TE-2361
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStylePictures()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");
				m_importer.ProcessSegment("User-supplied picture|" + filemaker.Filename +
					"|col|EXO 1--1||Caption for junk.jpg|", @"\fig");
				m_importer.FinalizeImport();
				IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
				IScrSection section = exodus.SectionsOS[0];
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

				Assert.AreEqual("1" + StringUtils.kChObject.ToString(),
					para.Contents.Text);
				ITsString tss = para.Contents;
				Assert.AreEqual(2, tss.RunCount);
				string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				try
				{
					Assert.AreEqual("Caption for junk.jpg", picture.Caption.VernacularDefaultWritingSystem.Text);
					Assert.IsTrue(picture.PictureFileRA.InternalPath == picture.PictureFileRA.AbsoluteInternalPath);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.IndexOf("junk") >= 0);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.EndsWith(".jpg"));
					byte odt = Convert.ToByte(sObjData[0]);
					Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
				}
				finally
				{
					if (picture != null)
					{
						FileUtils.Delete(picture.PictureFileRA.AbsoluteInternalPath);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \cap
		///   \cat
		///   \figcopy
		///   \figdesc
		///   \figlaypos
		///   \figrefrng
		///   \figscale
		/// Jira number is TE-5732
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleToolboxStylePictures_AllMarkersPresent()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");
				m_importer.ProcessSegment("Caption for junk.jpg", @"\cap");
				m_importer.ProcessSegment(filemaker.Filename, @"\cat");
				m_importer.ProcessSegment("Copyright 1995, David C. Cook.", @"\figcopy");
				m_importer.ProcessSegment("Picture of baby Moses in a basket", @"\figdesc");
				m_importer.ProcessSegment("span", @"\figlaypos");
				m_importer.ProcessSegment("EXO 1--1", @"\figrefrng");
				m_importer.ProcessSegment("56", @"\figscale");
				m_importer.FinalizeImport();
				IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
				IScrSection section = exodus.SectionsOS[0];
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

				Assert.AreEqual("1" + StringUtils.kChObject.ToString(),
					para.Contents.Text);
				ITsString tss = para.Contents;
				Assert.AreEqual(2, tss.RunCount);
				string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				try
				{
					Assert.AreEqual("Caption for junk.jpg", picture.Caption.VernacularDefaultWritingSystem.Text);
					Assert.IsTrue(picture.PictureFileRA.InternalPath == picture.PictureFileRA.AbsoluteInternalPath);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.IndexOf("junk") >= 0);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.EndsWith(".jpg"));
					byte odt = Convert.ToByte(sObjData[0]);
					Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
					Assert.AreEqual("Picture of baby Moses in a basket", picture.Description.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual(PictureLayoutPosition.CenterOnPage, picture.LayoutPos);
					Assert.AreEqual(56, picture.ScaleFactor);
					Assert.AreEqual(PictureLocationRangeType.ReferenceRange, picture.LocationRangeType);
					Assert.AreEqual(02001001, picture.LocationMin);
					Assert.AreEqual(02001022, picture.LocationMax);
					Assert.AreEqual("Copyright 1995, David C. Cook.", picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
				}
				finally
				{
					if (picture != null)
					{
						FileUtils.Delete(picture.PictureFileRA.AbsoluteInternalPath);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \figdesc
		///   \cat
		///   \figcopy
		///   \figscale
		/// Jira number is TE-5732
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleToolboxStylePictures_SomeMarkersPresent()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");
				m_importer.ProcessSegment("Picture of baby Moses in a basket", @"\figdesc");
				m_importer.ProcessSegment(filemaker.Filename, @"\cat");
				m_importer.ProcessSegment("Copyright 1995, David C. Cook.", @"\figcopy");
				m_importer.ProcessSegment("56", @"\figscale");
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
				m_importer.ProcessSegment("", @"\v");
				m_importer.ProcessSegment("Verse two", @"\vt");
				m_importer.FinalizeImport();
				IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
				IScrSection section = exodus.SectionsOS[0];
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

				Assert.AreEqual("1" + StringUtils.kChObject.ToString() + "2Verse two",
					para.Contents.Text);
				ITsString tss = para.Contents;
				Assert.AreEqual(4, tss.RunCount);
				string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				try
				{
					Assert.IsNull(picture.Caption.VernacularDefaultWritingSystem.Text);
					Assert.IsTrue(picture.PictureFileRA.InternalPath == picture.PictureFileRA.AbsoluteInternalPath);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.IndexOf("junk") >= 0);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.EndsWith(".jpg"));
					byte odt = Convert.ToByte(sObjData[0]);
					Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
					Assert.AreEqual("Picture of baby Moses in a basket", picture.Description.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual(PictureLayoutPosition.CenterInColumn, picture.LayoutPos);
					Assert.AreEqual(56, picture.ScaleFactor);
					Assert.AreEqual(PictureLocationRangeType.AfterAnchor, picture.LocationRangeType);
					Assert.AreEqual("Copyright 1995, David C. Cook.", picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
				}
				finally
				{
					if (picture != null)
					{
						FileUtils.Delete(picture.PictureFileRA.AbsoluteInternalPath);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \cat
		///   \cat
		/// Jira number is TE-5732
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleToolboxStylePictures_CatAfterCat()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");
				m_importer.ProcessSegment(filemaker.Filename, @"\cat");
				string fileName = MiscUtils.IsUnix ? "/MissingPicture.jpg" : @"c:\MissingPicture.jpg";
				m_importer.ProcessSegment(fileName, @"\cat");
				m_importer.FinalizeImport();
				IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
				IScrSection section = exodus.SectionsOS[0];
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

				Assert.AreEqual("1" + StringUtils.kChObject.ToString() + StringUtils.kChObject.ToString(),
					para.Contents.Text);
				ITsString tss = para.Contents;
				Assert.AreEqual(3, tss.RunCount);
				string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				try
				{
					Assert.IsNull(picture.Caption.VernacularDefaultWritingSystem.Text);
					Assert.IsTrue(picture.PictureFileRA.InternalPath == picture.PictureFileRA.AbsoluteInternalPath);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.IndexOf("junk") >= 0);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.EndsWith(".jpg"));
					byte odt = Convert.ToByte(sObjData[0]);
					Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
					Assert.IsNull(picture.Description.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual(PictureLayoutPosition.CenterInColumn, picture.LayoutPos);
					Assert.AreEqual(100, picture.ScaleFactor);
					Assert.AreEqual(PictureLocationRangeType.AfterAnchor, picture.LocationRangeType);
					Assert.IsNull(picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
				}
				finally
				{
					if (picture != null)
					{
						FileUtils.Delete(picture.PictureFileRA.AbsoluteInternalPath);
					}
				}

				// Make sure the second picture (the missing one) is okay
				sObjData = tss.get_Properties(2).GetStrPropValue((int)FwTextPropType.ktptObjData);
				guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				Assert.IsNull(picture.Caption.VernacularDefaultWritingSystem.Text);
				Assert.IsTrue(picture.PictureFileRA.InternalPath == picture.PictureFileRA.AbsoluteInternalPath);
				Assert.AreEqual(fileName, picture.PictureFileRA.InternalPath);
				Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, Convert.ToByte(sObjData[0]));
				Assert.IsNull(picture.Description.AnalysisDefaultWritingSystem.Text);
				Assert.AreEqual(PictureLayoutPosition.CenterInColumn, picture.LayoutPos);
				Assert.AreEqual(100, picture.ScaleFactor);
				Assert.AreEqual(PictureLocationRangeType.AfterAnchor, picture.LocationRangeType);
				Assert.IsNull(picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \cat
		/// Jira number is TE-7928
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
		   ExpectedMessage = "Invalid figure file name property(\\r)?\\n(\\r)?\\n" +
		   "\\\\cat InvalidFile|junk\u0000jpg||(\\r)?\\n" +
		   "Attempting to read EXO  Chapter: 1  Verse: 1",
		   MatchType = MessageMatch.Regex)]
		public void HandleToolboxStylePictures_InvalidFigFilename()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");

				// Linux Invalid filename chars are only null and /,
				m_importer.ProcessSegment(MiscUtils.IsUnix ? "InvalidFile|junk\u0000jpg||" : "InvalidFile|.jpg||",
					@"\cat");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data inserts two pictures with missing files:
		///   \cat
		///   \cap
		///   \cap
		/// Jira number is TE-5732
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleToolboxStylePictures_CapAfterCap()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a picture
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("Caption for missing picture 1", @"\cap");
			m_importer.ProcessSegment("Caption for missing picture 2", @"\cap");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

			Assert.AreEqual("1" + StringUtils.kChObject.ToString() + StringUtils.kChObject.ToString(),
				para.Contents.Text);
			ITsString tss = para.Contents;
			Assert.AreEqual(3, tss.RunCount);
			string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
			Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
			ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);

			Assert.AreEqual("Caption for missing picture 1", picture.Caption.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual("MissingPictureInImport.bmp", picture.PictureFileRA.InternalPath);
			byte odt = Convert.ToByte(sObjData[0]);
			Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
			Assert.IsNull(picture.Description.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(PictureLayoutPosition.CenterInColumn, picture.LayoutPos);
			Assert.AreEqual(100, picture.ScaleFactor);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, picture.LocationRangeType);
			Assert.IsNull(picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);

			// Make sure the second picture (also missing) is okay
			sObjData = tss.get_Properties(2).GetStrPropValue((int)FwTextPropType.ktptObjData);
			guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
			picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
			Assert.AreEqual("Caption for missing picture 2", picture.Caption.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual("MissingPictureInImport.bmp", picture.PictureFileRA.InternalPath);
			Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, Convert.ToByte(sObjData[0]));
			Assert.IsNull(picture.Description.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(PictureLayoutPosition.CenterInColumn, picture.LayoutPos);
			Assert.AreEqual(100, picture.ScaleFactor);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, picture.LocationRangeType);
			Assert.IsNull(picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works for Paratext when the directory is not
		/// specified:
		///   \fig Desc|Cat|Size|Loc|Copy|Cap|Ref
		/// Jira number is TE-5080
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStylePictures_NoFolder()
		{
			string sFilePath = Path.Combine(m_importer.DummySoWrapper.ExternalPictureFolders[0], "junk.jpg");
			using (DummyFileMaker filemaker = new DummyFileMaker(sFilePath, false))
			{
				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");
				m_importer.ProcessSegment("User-supplied picture|" + filemaker.Filename +
					"|col|EXO 1--1||Caption for junk.jpg|", @"\fig");
				m_importer.FinalizeImport();
				IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
				IScrSection section = exodus.SectionsOS[0];
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

				Assert.AreEqual("1" + StringUtils.kChObject.ToString(),
					para.Contents.Text);
				ITsString tss = para.Contents;
				Assert.AreEqual(2, tss.RunCount);
				string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				try
				{
					Assert.AreEqual("Caption for junk.jpg", picture.Caption.VernacularDefaultWritingSystem.Text);
					Assert.IsTrue(picture.PictureFileRA.AbsoluteInternalPath == picture.PictureFileRA.InternalPath);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.IndexOf("junk") >= 0);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.EndsWith(".jpg"));
					Assert.AreEqual(sFilePath, picture.PictureFileRA.InternalPath);
					byte odt = Convert.ToByte(sObjData[0]);
					Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
				}
				finally
				{
					if (picture != null)
					{
						FileUtils.Delete(picture.PictureFileRA.AbsoluteInternalPath);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \fig Desc|Cat|Size|Loc|Copy|Cap|Ref
		///   \btfig CapBT
		/// Jira number is TE-3649
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStylePicturesWithBT()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				m_importer.Settings.ImportBackTranslation = true;

				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");
				m_importer.ProcessSegment("User-supplied picture|" + filemaker.Filename +
					"|col|EXO 1--1||Caption for junk.jpg|", @"\fig");
				m_importer.ProcessSegment("back translation for junk.jpg", @"\btfig");
				m_importer.FinalizeImport();
				IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
				IScrSection section = exodus.SectionsOS[0];
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

				Assert.AreEqual("1" + StringUtils.kChObject.ToString(),
					para.Contents.Text);
				ITsString tss = para.Contents;
				Assert.AreEqual(2, tss.RunCount);
				string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				try
				{
					Assert.AreEqual("Caption for junk.jpg", picture.Caption.VernacularDefaultWritingSystem.Text);
					Assert.IsTrue(picture.PictureFileRA.InternalPath == picture.PictureFileRA.AbsoluteInternalPath);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.IndexOf("junk") >= 0);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.EndsWith(".jpg"));
					byte odt = Convert.ToByte(sObjData[0]);
					Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
					Assert.AreEqual(Path.Combine(Path.GetTempPath(), "back translation for junk.jpg"),
						picture.Caption.get_String(DefaultAnalWs).Text);
				}
				finally
				{
					if (picture != null)
					{
						FileUtils.Delete(Path.Combine(DirectoryFinder.FWDataDirectory,
							picture.PictureFileRA.InternalPath));
					}
				}
			}
		}
		#endregion

		#region Title Short tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira # for this is TE-1664
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TitleShortGetsSetToBookTitle()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("Exodus", @"\h");
			Assert.AreEqual("Exodus", m_importer.ScrBook.Name.VernacularDefaultWritingSystem.Text);

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("This is verse text", @"\v");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(2, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Verse Number");
			VerifyBldrRun(1, "This is verse text", null);
		}
		#endregion

		#region Style Userlevel tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that styles that are used for importing get a userlevel set to a negative
		/// number (i.e. the styles are set to being used)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserLevelGetsSetToUsed()
		{
			// set Doxology to being used (negative userlevel)
			IStStyle style = m_styleSheet.FindStyle("Doxology");
			Assert.IsNotNull(style, "Doxology was not found!");
			style.UserLevel = (style.UserLevel > 0 ? -style.UserLevel : style.UserLevel);

			// make sure styles aren't being used
			style = m_styleSheet.FindStyle("Intro Paragraph");
			Assert.IsNotNull(style, "Intro Paragraph was not found!");
			if (style.UserLevel < 0)
				style.UserLevel = -style.UserLevel;
			style = m_styleSheet.FindStyle("Title Main");
			if (style.UserLevel < 0)
				style.UserLevel = -style.UserLevel;
			style = m_styleSheet.FindStyle("Section Head");
			if (style.UserLevel < 0)
				style.UserLevel = -style.UserLevel;
			style = m_styleSheet.FindStyle("Line3");
			if (style.UserLevel < 0)
				style.UserLevel = -style.UserLevel;
			style = m_styleSheet.FindStyle("List Item3");
			if (style.UserLevel < 0)
				style.UserLevel = -style.UserLevel;

			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Title Main text", @"\mt");
			m_importer.ProcessSegment("Section Head text", @"\s");
			m_importer.ProcessSegment("Line3 text", @"\q3");
			m_importer.ProcessSegment("Doxology text", @"\qc");
			m_importer.ProcessSegment("list item3 text", @"\li3");
			m_importer.FinalizeImport();

			style = m_styleSheet.FindStyle("Title Main");
			Assert.IsNotNull(style, "Title Main was not found!");
			Assert.AreEqual(0, style.UserLevel, "should stay 0");

			style = m_styleSheet.FindStyle("Section Head");
			Assert.IsNotNull(style, "Section Head was not found!");
			Assert.AreEqual(0, style.UserLevel, "should stay 0");

			style = m_styleSheet.FindStyle("Line3");
			Assert.IsNotNull(style, "Line3 was not found!");
			Assert.AreEqual(-2, style.UserLevel, "should be changed to being used");

			style = m_styleSheet.FindStyle("Doxology");
			Assert.IsNotNull(style, "Doxology was not found!");
			Assert.AreEqual(-3, style.UserLevel, "should stay as being used");

			style = m_styleSheet.FindStyle("List Item3");
			Assert.IsNotNull(style, "List Item3 was not found!");
			Assert.AreEqual(-4, style.UserLevel, "should be changed to being used");

			style = m_styleSheet.FindStyle("Intro Paragraph");
			Assert.IsNotNull(style, "Intro Paragraph was not found!");
			Assert.AreEqual(2, style.UserLevel, "should not be changed to being used");
		}
		#endregion

		#region Back-to-back in-line Character Styles
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-621
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackToBackCharStyles()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");

			// ******** test a character style, no end marker, terminated by another char style
			m_importer.ProcessSegment("This ", @"\vt");
			m_importer.ProcessSegment("is", @"\kw");
			m_importer.ProcessSegment(" ", @"\kw*");
			m_importer.ProcessSegment("a", @"\gls");
			m_importer.ProcessSegment(" nice test. ", @"\gls*");

			// verify state of NormalParaStrBldr
			//Assert.AreEqual(7, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "This ", null);
			VerifyBldrRun(3, "is", "Key Word");
			VerifyBldrRun(4, " ", null);
			VerifyBldrRun(5, "a", "Gloss");
			VerifyBldrRun(6, " nice test. ", null);
		}
		#endregion

		#region Importing Annotations tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations
		///    id c1 p v1 vt p v2 vt rem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_Simple()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with character style *********************
			m_importer.ProcessSegment("first verse", @"\vt");
			expectedParaRunCount++; // for the verse text

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with character style *********************
			m_importer.ProcessSegment("second verse", @"\vt");
			expectedParaRunCount++; // for the verse text
			m_importer.ProcessSegment("This is an annotation", @"\rem");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];
			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			int paraHvo = section.ContentOA.ParagraphsOS[1].Hvo;
			VerifySimpleAnnotation(paraHvo, 2001002, "This is an annotation", NoteType.Translator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations
		///    id c1 p v1 vt p v2-3 vt rem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_VerseBridge()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with character style *********************
			m_importer.ProcessSegment("first verse", @"\vt");
			expectedParaRunCount++; // for the verse text

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with character style *********************
			m_importer.ProcessSegment("second verse", @"\vt");
			expectedParaRunCount++; // for the verse text
			m_importer.ProcessSegment("This is an annotation", @"\rem");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];
			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			int paraHvo = section.ContentOA.ParagraphsOS[1].Hvo;
			VerifySimpleAnnotation(paraHvo, 2001002, 2001003, "This is an annotation", NoteType.Translator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations where a marker is explicitly mapped with an annotation
		/// type of Consultant note (though there doesn't appear to be a way to do this in the
		/// UI yet).
		///    id c1 p v1 vt p v2 vt crem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_MarkerMappedToConsultantNote()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");

			// ************** process verse text with character style *********************
			m_importer.ProcessSegment("first verse", @"\vt");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse number *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");

			// ************** process verse text *********************
			m_importer.ProcessSegment("second verse", @"\vt");
			m_importer.ProcessSegment("This is an annotation", @"\crem");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];
			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			int paraHvo = section.ContentOA.ParagraphsOS[1].Hvo;
			VerifySimpleAnnotation(paraHvo, 2001002, "This is an annotation", NoteType.Consultant);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations where we simulate importing a non-interleaved file
		/// containing Consultant notes
		///    Scripture file: id c1 p v1 vt
		///	   Cons. Note file: id c1 v1 rem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_NonInterleaved_ConsultantNoteFile()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;
			m_importer.Settings.AddFile("dummy.sfm", ImportDomain.Annotations,
				"en", DummyTeImporter.s_consultantNoteDefn);
			DummyTeImporter.SetUpMappings(m_importer.Settings);
			m_importer.Initialize();

			// ************** process Scripture file *********************

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse number *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment(" ", @"\v");

			// ************** process verse text *********************
			m_importer.ProcessSegment("first verse", @"\vt");

			// ************** process Annotation file *********************

			m_importer.CurrentImportDomain = ImportDomain.Annotations;
			m_importer.DummySoWrapper.m_CurrentWs = m_wsAnal;
			m_importer.DummySoWrapper.SetCurrentAnnotationType(DummyTeImporter.s_consultantNoteDefn.Hvo);

			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse number *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment(" ", @"\v");

			// ************** process annotation *********************
			m_importer.ProcessSegment("Non-interleaved annotation", @"\rem");

			// ************** finalize **************
			m_importer.FinalizeImport();

			int hvoBook = m_importer.UndoInfo.ImportedVersion.BooksOS[0].Hvo;

			IScrSection section = book.SectionsOS[0];
			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			int paraHvo = section.ContentOA.ParagraphsOS[0].Hvo;
			VerifySimpleAnnotation(paraHvo, 2001001, "Non-interleaved annotation", NoteType.Consultant);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations where we skip importing the Scripture file and only
		/// import the notes
		///    Scripture file: id c1 v1
		///	   Cons. Note file: id c1 v1 rem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_WithoutScripture()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBookIntros = false;
			m_importer.Settings.ImportAnnotations = true;
			m_importer.Settings.AddFile("dummy.sfm", ImportDomain.Annotations,
				"en", DummyTeImporter.s_consultantNoteDefn);
			DummyTeImporter.SetUpMappings(m_importer.Settings);
			m_importer.Initialize();

			// ************** process (skip) Scripture file *********************

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			Assert.IsNull(m_scr.FindBook(2));

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse number *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Ignore this ", @"\v");

			// ************** process Annotation file *********************

			m_importer.CurrentImportDomain = ImportDomain.Annotations;
			m_importer.DummySoWrapper.m_CurrentWs = m_wsAnal;
			m_importer.DummySoWrapper.SetCurrentAnnotationType(DummyTeImporter.s_consultantNoteDefn.Hvo);

			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse number *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment(" ", @"\v");

			// ************** process annotation *********************
			m_importer.ProcessSegment("Non-interleaved annotation", @"\note");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify that the note got created
			VerifySimpleAnnotation(0, 2001001, "Non-interleaved annotation", NoteType.Consultant);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations where they are interleaved with the back translation.
		///    Scripture file: id c1 v1 v2
		///	   BT file:        id c1 v1 rem v2
		/// </summary>
		/// <remarks>TE-7281</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_InterleavedInBT()
		{
			// Add Scripture translation data so that back translation will import okay, and
			// add an annotation.
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section = AddSectionToMockedBook(genesis);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "verse 1");
			IScrScriptureNote note = AddAnnotation(para, 01001001, NoteType.Consultant);
			IStTxtPara discussionPara = (IStTxtPara)note.DiscussionOA.ParagraphsOS[0];
			AddRunToMockedPara(discussionPara, "Annotation for verse 1 ",
				Cache.DefaultAnalWs);

			// We don't want to import the translation, but we do want the annotations and
			// back translations.
			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBookIntros = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportAnnotations = true;
			m_importer.Settings.AddFile("dummy.sfm", ImportDomain.Annotations,
				"en", DummyTeImporter.s_consultantNoteDefn);
			DummyTeImporter.SetUpMappings(m_importer.Settings);
			m_importer.Initialize();

			// ************** process Back Translation file ***************
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.DummySoWrapper.m_CurrentWs = m_wsAnal;
			m_importer.DummySoWrapper.SetCurrentAnnotationType(DummyTeImporter.s_consultantNoteDefn.Hvo);

			// ************** process a \id segment *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(1, m_importer.BookNumber);
			// verify that a new book was added to the DB
			Assert.IsNotNull(m_scr.FindBook(1));

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("1", @"\c");

			// ************** process verse one ***************************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("verse one BT", @"\v");

			// ************** process annotation **************************
			m_importer.ProcessSegment("Annotation for verse 1 ", @"\rem");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// We expect that we have one annotation now.
			// verify that the note got created
			Assert.AreEqual(1, m_scr.BookAnnotationsOS[0].NotesOS.Count, "There should be one annotation.");
			VerifySimpleAnnotation(para.Hvo, 1001001, "Annotation for verse 1 ", NoteType.Consultant);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// verify a single annotation on the given object and for the given reference. Should
		/// consist of a single default run, with para style "Remark".
		/// </summary>
		/// <param name="objhvo">ID of annotated object (usually an IStTxtPara)</param>
		/// <param name="scrRef">The reference as an int</param>
		/// <param name="sText">The text of the annotation</param>
		/// <param name="type">Type of note</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySimpleAnnotation(int objhvo, int scrRef, string sText, NoteType type)
		{
			VerifySimpleAnnotation(objhvo, scrRef, scrRef, sText, type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// verify a single annotation on the given object and for the given reference. Should
		/// consist of a single default run, with para style "Remark".
		/// </summary>
		/// <param name="objhvo">ID of annotated object (usually an IStTxtPara)</param>
		/// <param name="startScrRef">The starting reference as an int</param>
		/// <param name="endScrRef">The ending reference as an int</param>
		/// <param name="sText">The text of the annotation</param>
		/// <param name="type">Type of note</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySimpleAnnotation(int objhvo, int startScrRef,
			int endScrRef, string sText, NoteType type)
		{
			int iBook = BCVRef.GetBookFromBcv(startScrRef) - 1;
			 Assert.AreEqual(1, m_scr.BookAnnotationsOS[iBook].NotesOS.Count);
			IScrScriptureNote annotation = m_scr.BookAnnotationsOS[iBook].NotesOS[0];
			if (objhvo != 0)
			{
				Assert.AreEqual(objhvo, annotation.BeginObjectRA.Hvo);
				Assert.AreEqual(objhvo, annotation.EndObjectRA.Hvo);
			}
			else
			{
				Assert.IsNull(annotation.BeginObjectRA);
				Assert.IsNull(annotation.EndObjectRA);
			}
			Assert.AreEqual(startScrRef, annotation.BeginRef);
			Assert.AreEqual(endScrRef, annotation.EndRef);
			Assert.AreEqual(type, annotation.AnnotationType);
			m_importer.VerifyAnnotationText(annotation.DiscussionOA, "Discussion", sText, m_wsAnal);
			m_importer.VerifyInitializedNoteText(annotation.QuoteOA, "Quote");
			m_importer.VerifyInitializedNoteText(annotation.RecommendationOA, "Recommendation");
			m_importer.VerifyInitializedNoteText(annotation.ResolutionOA, "Resolution");
			Assert.AreEqual(0, annotation.ResponsesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations
		///    id c1 p v1 rem v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_InMiddleOfParagraph()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// ************** process an \id segment*********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment(" ", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("first verse ", @"\v");

			// ************** process the annotation *********************
			m_importer.ProcessSegment("This is an annotation ", @"\rem");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("second verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			int paraHvo = para.Hvo;
			Assert.AreEqual("11first verse 2second verse", para.Contents.Text);
			VerifySimpleAnnotation(paraHvo, 2001001, "This is an annotation", NoteType.Translator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations when a duplicate annotation already exists.  We expect
		/// the original annotation to be preserved.
		///    id rem c1 p v1 rem v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_WithIdenticalAnnotation()
		{
			// Create a book (that will be replaced by the imported one).
			IScrBook exodus = AddBookToMockedScripture(2, "Exodus");
			IScrSection section1 = AddSectionToMockedBook(exodus);
			IStTxtPara para1 = AddParaToMockedSectionContent(section1, ScrStyleNames.Normal);
			AddRunToMockedPara(para1, "And I ran from Egypt",
				Cache.DefaultVernWs);
			// A duplicate copy of this annotation should not be created since it is identical
			// to the one being imported (same reference and text).
			IScrScriptureNote origNote1 = AddAnnotation(para1, new BCVRef(2, 1, 1),
				NoteType.Translator, "This is an annotation.");
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "More of the same",
				Cache.DefaultVernWs);
			// This annotation should survive the import since it is in a different verse from the
			// annotation being imported.
			IScrScriptureNote origNote2 = AddAnnotation(para1, new BCVRef(2, 1, 2),
				NoteType.Translator, "This is an annotation.");

			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// ************** process an \id segment*********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process the annotation *********************
			m_importer.ProcessSegment("This is an annotation.", @"\rem");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment(" ", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("first verse ", @"\v");

			// ************** process the annotation *********************
			m_importer.ProcessSegment("This is an annotation.", @"\rem");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("second verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Verify that the original note on verse 1 still exists and that the duplicate was not added.
			Assert.AreEqual(3, m_scr.BookAnnotationsOS[1].NotesOS.Count);
			IScrScriptureNote verse1Note = null;
			IScrScriptureNote verse2Note = null;
			int numVerse1Notes = 0;
			foreach (IScrScriptureNote annotation in m_scr.BookAnnotationsOS[1].NotesOS)
			{
				switch (annotation.BeginRef)
				{
					case 2001001:
						verse1Note = annotation;
						numVerse1Notes++;
						break;
					case 2001002:
						verse2Note = annotation;
						break;
				}
			}
			Assert.IsNotNull(verse1Note, "Note for verse 1 not found.");
			Assert.AreEqual(1, numVerse1Notes, "There should be exactly one note for verse 1");
			Assert.AreEqual(origNote1.Hvo, verse1Note.Hvo,
				"The original note should still be the only note on verse 1");

			Assert.IsNotNull(verse2Note, "Note for verse 2 not found.");
			Assert.AreEqual(origNote2.Hvo, verse2Note.Hvo,
				"The original note should still be the only note on verse 2");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing annotations before the start of the first paragraph. The annotation
		/// should be associated with the book.
		///    id rem c1 p v1 v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_BeforeStartOfParagraph()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// ************** process an \id segment*********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process the annotation *********************
			m_importer.ProcessSegment("This is an annotation ", @"\rem");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment(" ", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("first verse ", @"\v");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("second verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			int paraHvo = para.Hvo;
			Assert.AreEqual("11first verse 2second verse", para.Contents.Text);
			VerifySimpleAnnotation(book.Hvo, 2001000, "This is an annotation", NoteType.Translator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test of importing annotations containing character runs. Test data sequence:
		///    id c1 p v1  vt em {rem em} v2 vt nft {rem vern rt}
		/// Jira number is TE-2066
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_EmbeddedCharacterRuns()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportAnnotations = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Primer versiculo, ", @"\vt");
			m_importer.ProcessSegment("excelente! ", @"\em");
			m_importer.ProcessSegment("First annotation, ", @"\rem");
			m_importer.ProcessSegment("cool! ", @"\em");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Tercer versiculo ", @"\vt");
			m_importer.ProcessSegment("My footnote hurts ", @"\f");
			m_importer.ProcessSegment("Why did you say ", @"\rem");
			m_importer.ProcessSegment("tercer ", @"\vern");
			m_importer.ProcessSegment("in verse 2? ", @"\rt");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			Assert.AreEqual(1, exodus.SectionsOS.Count);
			IScrSection section = exodus.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents;
			Assert.AreEqual(7, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", ScrStyleNames.ChapterNumber, m_wsVern);
			AssertEx.RunIsCorrect(tss, 1, "1", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(tss, 2, "Primer versiculo, ", null, m_wsVern);
			AssertEx.RunIsCorrect(tss, 3, "excelente! ", "Emphasis", m_wsVern);
			AssertEx.RunIsCorrect(tss, 4, "2", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(tss, 5, "Tercer versiculo", null, m_wsVern);
			VerifyFootnoteMarkerOrcRun(tss, 6);
			VerifySimpleFootnote(0, "My footnote hurts");

			IFdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[1].NotesOS;
			Assert.AreEqual(2, notes.Count);

			// verify the annotations

			foreach (IScrScriptureNote annotation in notes)
			{
				Assert.AreEqual(para.Hvo, annotation.BeginObjectRA.Hvo);
				Assert.AreEqual(para.Hvo, annotation.EndObjectRA.Hvo);
				Assert.AreEqual(annotation.BeginRef, annotation.EndRef);
				Assert.IsNotNull(annotation.DiscussionOA, "Should have an StText");
				Assert.AreEqual(1, annotation.DiscussionOA.ParagraphsOS.Count);
				IStTxtPara annPara = (IStTxtPara)annotation.DiscussionOA.ParagraphsOS[0];
				Assert.IsNotNull(annPara.StyleRules, "should have a paragraph style");
				Assert.AreEqual(ScrStyleNames.Remark,
					annPara.StyleRules.GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
			}
			IScrScriptureNote note = notes[0];
			ITsString tssAnn = ((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents;
			Assert.AreEqual(2, tssAnn.RunCount);
			AssertEx.RunIsCorrect(tssAnn, 0, "First annotation, ", null, m_wsAnal);
			AssertEx.RunIsCorrect(tssAnn, 1, "cool!", "Emphasis", m_wsAnal);
			Assert.AreEqual(2001001, note.BeginRef);

			note = notes[1];
			tssAnn = ((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents;
			Assert.AreEqual(3, tssAnn.RunCount);
			AssertEx.RunIsCorrect(tssAnn, 0, "Why did you say ", null, m_wsAnal);
			AssertEx.RunIsCorrect(tssAnn, 1, "tercer ", null, m_wsVern);
			AssertEx.RunIsCorrect(tssAnn, 2, "in verse 2?", null, m_wsAnal);
			Assert.AreEqual(2001002, note.BeginRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing interleaved annotations while not actually importing the Scripture
		///    id c1 p v1 vt p v2 vt rem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_InterleavedButNotImportingScripture()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportAnnotations = true;

			// ************** process an \id segment *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// *** process a sequence of Scripture markers, including chapter and verse ***
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("first verse", @"\vt");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("second verse", @"\vt");

			// ************** process a \rem segment to create an annotation ****************
			m_importer.ProcessSegment("This is an annotation", @"\rem");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify that a new book was NOT created
			Assert.IsNull(m_importer.ScrBook);
			Assert.IsNull(m_scr.FindBook(2));
			VerifySimpleAnnotation(0, 2001002, "This is an annotation", NoteType.Translator);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests importing interleaved annotations while not actually importing the Back Translation
		///    id c1 p v1 vt p v2 vt rem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportAnnotations_InterleavedButNotImportingBT()
		{
			// make sure that notes will get imported
			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = false;
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.Settings.ImportAnnotations = true;

			// ************** process an \id segment *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// *** process a sequence of Scripture markers, including chapter and verse ***
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("first verse", @"\vt");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("second verse", @"\vt");

			// ************** process a \rem segment to create an annotation ****************
			m_importer.ProcessSegment("This is an annotation", @"\rem");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify that a new book was NOT created
			Assert.IsNull(m_importer.ScrBook);
			Assert.IsNull(m_scr.FindBook(2));
			VerifySimpleAnnotation(0, 2001002, "This is an annotation", NoteType.Translator);
		}
		#endregion

		#region Exception Handling Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to clean up a partial section when an unexpected exception is thrown
		/// during import. Jira # for this is TE-5201.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CleanUpPartialSectionTest()
		{
			// We want it to process and \id marker (MAT) and then a section head, and then
			// throw an exception. This should cause the section content to be empty unless
			// our clean-up code works
			m_importer.m_SegmentMarkers = new List<string>(new string[] { @"\id", @"\s" });

			try
			{
				m_importer.Import();
			}
			catch (SOWNoSegException)
			{
				// Ignore this kind of exception -- it was expected
			}

			Assert.IsNotNull(m_importer.UndoInfo);
			Assert.IsNotNull(m_importer.CurrentSection);
			Assert.IsNotNull(m_importer.CurrentSection.HeadingOA);
			Assert.AreEqual(1, m_importer.CurrentSection.HeadingOA.ParagraphsOS.Count);
			Assert.IsNotNull(m_importer.CurrentSection.ContentOA);
			Assert.AreEqual(1, m_importer.CurrentSection.ContentOA.ParagraphsOS.Count);
		}
		#endregion
	}
	#endregion
}
#if __MonoCS__
#pragma warning restore 419 // ambiguous reference; mono bug #639867
#endif
