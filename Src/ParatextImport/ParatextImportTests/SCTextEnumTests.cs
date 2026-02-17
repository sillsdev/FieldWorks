// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel;
using SIL.LCModel.Utils;
using ECInterfaces;
using SilEncConverters40;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.DomainServices;

namespace ParatextImport
{
	#region DummyEncConverter class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// test version of IEncConverter that uppercases a string.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyEncConverter : IEncConverter
	{
		#region Methods we care about
		/// <summary>
		/// The only method we really care about for testing
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>
		public string Convert(string sInput)
		{
			return sInput.ToUpper();
		}
		#endregion

		#region Members we don't care about
		/// <summary></summary>
		public bool DirectionForward
		{
			get { return false; }
			set { }
		}

		/// <summary></summary>
		public int CodePageInput
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		/// <summary></summary>
		public string[] AttributeKeys
		{
			get { return null; }
		}

		/// <summary></summary>
		public string ImplementType
		{
			get
			{
				return null;
			}
		}

		/// <summary></summary>
		public string ProgramID
		{
			get { return null; }
		}

		/// <summary></summary>
		public string ConverterIdentifier
		{
			get
			{
				return null;
			}
		}

		/// <summary></summary>
		public string ConvertToUnicode(byte[] baInput)
		{
			return null;
		}

		/// <summary></summary>
		public bool Debug
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		/// <summary></summary>
		public ECInterfaces.ConvType ConversionType
		{
			get
			{
				return new ECInterfaces.ConvType();
			}
		}

		/// <summary></summary>
		public byte[] ConvertFromUnicode(string sInput)
		{
			return null;
		}

		/// <summary></summary>
		public string LeftEncodingID
		{
			get
			{
				return null;
			}
		}

		/// <summary></summary>
		public string ConvertEx(string sInput, ECInterfaces.EncodingForm inEnc, int ciInput, ECInterfaces.EncodingForm outEnc, out int ciOutput, ECInterfaces.NormalizeFlags eNormalizeOutput, bool bForward)
		{
			ciOutput = 0;
			return null;
		}

		/// <summary></summary>
		public int ProcessType
		{
			get
			{
				return 0;
			}
		}

		/// <summary></summary>
		public void Initialize(string converterName, string ConverterIdentifier, ref string lhsEncodingID, ref string rhsEncodingID, ref ECInterfaces.ConvType eConversionType, ref int processTypeFlags, int CodePageInput, int CodePageOutput, bool bAdding)
		{
		}

		/// <summary></summary>
		public ECInterfaces.EncodingForm EncodingOut
		{
			get
			{
				return new ECInterfaces.EncodingForm();
			}
			set
			{
			}
		}

		/// <summary></summary>
		public string RightEncodingID
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		/// <summary></summary>
		public string Name
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		/// <summary></summary>
		public ECInterfaces.EncodingForm EncodingIn
		{
			get
			{
				return new ECInterfaces.EncodingForm();
			}
			set
			{
			}
		}

		/// <summary></summary>
		public string AttributeValue(string sKey)
		{
			return null;
		}

		/// <summary></summary>
		public string[] ConverterNameEnum
		{
			get
			{
				return null;
			}
		}

		/// <summary></summary>
		public int CodePageOutput
		{
			get
			{
				return 0;
			}
			set { }
		}

		/// <summary></summary>
		public ECInterfaces.NormalizeFlags NormalizeOutput
		{
			get { return new ECInterfaces.NormalizeFlags(); }
			set { }
		}

		/// <summary></summary>
		public IEncConverterConfig Configurator
		{
			get { return null; }
		}

		private bool m_fIsInRepository = false;
		/// <summary></summary>
		public bool IsInRepository
		{
			get { return m_fIsInRepository; }
			set { m_fIsInRepository = value; }
		}
		#endregion
	}
	#endregion

	#region SCTextEnumTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing tests for SCTextEnum.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SCTextEnumTests : ScrInMemoryLcmTestBase
	{
		#region data members
		private IScrImportSet m_settings;
		private IEncConverters m_converters;
		private MockFileOS m_fileOs;
		#endregion

		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		[TearDown]
		public override void TestTearDown()
		{
			FileUtils.Manager.Reset();
			base.TestTearDown();
			foreach (CoreWritingSystemDefinition ws in Cache.ServiceLocator.WritingSystemManager.WritingSystems)
				ws.LegacyMapping = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_settings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(m_settings);
			m_settings.ImportTypeEnum = TypeOfImport.Other;
			m_settings.Initialize(null, null);
			m_converters = null;
			m_fileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_fileOs);
		}

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to get a TextEnum ready to read
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ISCTextEnum GetTextEnum(ImportDomain domain, BCVRef startRef, BCVRef endRef)
		{
			// Save settings before enumerating, which will get the styles hooked up in the mapping list
			m_settings.SaveSettings();

			SCScriptureText scText = new SCScriptureText(m_settings, domain);
			ReflectionHelper.SetField(scText, "m_encConverters", m_converters);
			return scText.TextEnum(startRef, endRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default vernacular writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private CoreWritingSystemDefinition VernacularWs
		{
			get
			{
				return Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			}
		}
		#endregion

		#region Tests for normal operation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test reads a portion of a book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("We do not support reading parts of books yet")]
		public void Read_GEN_partial()
		{
			string filename = m_fileOs.MakeSfFile("GEN",
				@"\c 1",
				@"\v 1 In the beginning",
				@"\v 2 And the earth was formless and void",
				@"\c 2",
				@"\v 1 Le ciel, la terre et tous leurs lments furent achevs.",
				@"\v 2 This should not be read.");

			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ScrReference expectedRef = new ScrReference(1, 2, 1, ScrVers.English);
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main, expectedRef, expectedRef);
			Assert.That(textEnum, Is.Not.Null, "No TextEnum object was returned");

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read GEN 2:1.");
			Assert.That(textSeg.FirstReference, Is.EqualTo(expectedRef), "Incorrect reference returned");
			Assert.That(textSeg.Text, Is.EqualTo(" Le ciel, la terre et tous leurs lments furent achevs. "), "Incorrect data found at GEN 2:1");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-186
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IgnoreBackslashesInDataForOther()
		{
			string filename = m_fileOs.MakeSfFile("EPH", @"\mt \it fun \it* Mi\\abi \taki", @"\c 1", @"\v 1");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read first segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read second segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"\it fun \it* Mi\\abi \taki "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-918
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TreatBackslashesInDataAsInlineMarkersForP5()
		{
			string filename = m_fileOs.MakeSfFile("EPH", @"\mt \it fun\it* Mi\\abi \taki", @"\c 1", @"\v 1");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			m_settings.ImportTypeEnum = TypeOfImport.Paratext5;

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \id segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \mt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@""));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \it segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\it"));
			Assert.That(textSeg.Text, Is.EqualTo(@"fun"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \it* segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\it*"));
			Assert.That(textSeg.Text, Is.EqualTo(@" Mi"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \\abi segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\\abi"));
			Assert.That(textSeg.Text, Is.EqualTo(@""));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \taki segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\taki"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-918
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleP5EndMarkerFollowedByComma()
		{
			string filename = m_fileOs.MakeSfFile("EPH", @"\mt fun \f footnote \f*, isn't it?", @"\c 1", @"\v 1");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			m_settings.ImportTypeEnum = TypeOfImport.Paratext5;

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \id segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \mt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"fun "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \f segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\f"));
			Assert.That(textSeg.Text, Is.EqualTo(@"footnote "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \f* segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\f*"));
			Assert.That(textSeg.Text, Is.EqualTo(@", isn't it? "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-918
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleP5InlineMarkerWithNoExplicitEndMarker()
		{
			string filename = m_fileOs.MakeSfFile("EPH", @"\mt fun \f + \ft footnote \f*", @"\c 1", @"\v 1");
			m_settings.ImportTypeEnum = TypeOfImport.Paratext5;
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \id segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \mt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"fun "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \f segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\f"));
			Assert.That(textSeg.Text, Is.EqualTo(@"+ "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \ft segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\ft"));
			Assert.That(textSeg.Text, Is.EqualTo(@"footnote "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read \f* segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\f*"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test capability to exclude verses by range
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExcludeByRange()
		{
			string filename = m_fileOs.MakeSfFile("EPH", @"\mt Ephesians",
									 @"\c 1",
									 @"\v 1 hello there",
									 @"\id PHP",
									 @"\c 1",
									 @"\v 1 verse 1 of phillipians",
									 @"\v 2 here is verse 2",
									 @"\id COL",
									 @"\c 1",
									 @"\v 1 colossians start",
									 @"\v 2 more stuff");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(50, 0, 0, ScrVers.English), new ScrReference(50, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read first segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("PHP "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null);
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(50001001));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null);
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(50001001));
			Assert.That(textSeg.Text, Is.EqualTo(" verse 1 of phillipians "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null);
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(50001002));
			Assert.That(textSeg.Text, Is.EqualTo(" here is verse 2 "));

			Assert.That(textEnum.Next(), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test handling an inline verse number that follows a paragraph marker. This should
		/// be legal Paratext 6 data. (TE-8424)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InlineVerseNumberAfterParaMarker()
		{
			TempSFFileMaker fileMaker = new TempSFFileMaker();
			// WANTTESTPORT: (TE) Remove braces once no more integrations needed from FW_6.0 (indentation will force merge conflicts to prevent unintended changes)
			{
				string fileName = fileMaker.CreateFileNoID(
					new string[] {
									 @"\id EPH",
									 @"\mt Ephesians",
									 @"\c 1",
									 @"\p \v 1 hello there"});
				// The import type for Individual Paratext 6 files is Paratext 5.
				m_settings.ImportTypeEnum = TypeOfImport.Paratext5;
				m_settings.AddFile(fileName, ImportDomain.Main, null, null);

				ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
					new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

				ISCTextSegment textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null, "Unable to read first segment");
				Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
				Assert.That(textSeg.Text, Is.EqualTo("EPH "));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null);
				Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
				Assert.That(textSeg.Text, Is.EqualTo("Ephesians "));
				Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001000));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null);
				Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
				Assert.That(textSeg.Text, Is.EqualTo(" "));
				Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null);
				Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
				Assert.That(textSeg.Text, Is.EqualTo(string.Empty));
				Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null);
				Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
				Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));
				Assert.That(textSeg.Text, Is.EqualTo(" hello there "));

				Assert.That(textEnum.Next(), Is.Null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test excluding data before an id line
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExcludeBeforeIDLine()
		{
			string filename = m_fileOs.MakeSfFile(null,
				@"\_sh",
				@"\id EPH",
				@"\mt Ephesians",
				@"\c 1",
				@"\v 1 hello there");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read first segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null);
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001000));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null);
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null);
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));
			Assert.That(textSeg.Text, Is.EqualTo(" hello there "));

			Assert.That(textEnum.Next(), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-147
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SOMustSupportTextAfterVerseAndChapterNum()
		{
			string filename = m_fileOs.MakeSfFile(Encoding.UTF8, false, "MAT",
				@"\mt Matthew",
				@"\c 1 First Chapter",
				@"\v 1. Verse text",
				@"\v 2-3aForgot space!",
				@"\v 2-3ab.Missing Space",
				@"\v 2-3.abMissing Space",
				@"\v 2-3a.bMissing Space",
				@"\v 2-3.a.b.Missing Space",
				@"\v 5-blah",
				@"\v 6- blah",
				@"\v 7.a,8.a,9a. testing",
				"\\v 8\u200f-\u200f9 Text with RTL",
				"\\v 10\u201011 Text with unicode hyphen");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(40, 0, 0, ScrVers.English), new ScrReference(40, 1, 16, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("MAT "));
			Assert.That(textSeg.FirstReference.Book, Is.EqualTo(40));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(0));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(" First Chapter "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" Verse text "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"1."));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@"aForgot space! "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"2-3"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(2));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@"ab.Missing Space "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"2-3"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(2));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 6");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@"abMissing Space "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"2-3."));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(2));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 7");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@"bMissing Space "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"2-3a."));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(2));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 8");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@"a.b.Missing Space "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"2-3."));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(2));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));
			// lower 3 bits represent the versification
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(0));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 9");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@"-blah "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"5"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(5));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 10");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" blah "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"6-"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(6));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 11");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@"a,8.a,9a. testing "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo(@"7."));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(7));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(7));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 12");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" Text with RTL "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo("8\u200f-\u200f9"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(8));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(9));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 13");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" Text with unicode hyphen "));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo("10\u201011"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(10));
			Assert.That(textSeg.LastReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(11));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an inline marker immediately following a line marker (TE-3234)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InlineAfterLineMaker()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\it ", @"\it*", "Emphasis"));

			string filename = m_fileOs.MakeSfFile(Encoding.UTF8, false, "MAT",
				@"\mt \it Matthew\it*",
				@"\c 1",
				@"\v 1");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(40, 0, 0, ScrVers.English), new ScrReference(40, 1, 16, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(string.Empty));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\it "));
			Assert.That(textSeg.Text, Is.EqualTo("Matthew"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\it*"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-147
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumSubsegments()
		{
			string filename = m_fileOs.MakeSfFile("MAT",
				@"\mt Matthew",
				@"\c 1",
				@"\v 1a Verse part a",
				@"\v 1c Verse part b",
				@"\v 1e Verse part c",
				@"\v 2a-2b",
				@"\v 3-4a");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(40, 0, 0, ScrVers.English), new ScrReference(40, 1, 4, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read id segment ");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("MAT "));
			Assert.That(textSeg.FirstReference.Book, Is.EqualTo(40));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read mt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(0));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read c segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment v 1a");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" Verse part a "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment v 1c");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" Verse part b "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(2));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(2));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment v 1e");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" Verse part c "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(3));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(3));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment v 2a-2b");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(2));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(1));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(2));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(2));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment v 3-4a");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(3));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(0));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(4));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to enumerate segments when vernacular text has a verse bridge and there
		/// is an interleaved back translation. Jira number for this is TE-6048
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseBridgesWithInterleavedBt()
		{
			string filename = m_fileOs.MakeSfFile("MAT",
				@"\c 1",
				@"\v 1-3",
				@"\vt El era la inceput cu Dumenzeu",
				@"\btvt He was with God");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\btvt", MappingSet.Main);
			mapping.Domain = MarkerDomain.BackTrans;
			// Save settings before enumerating, which will get the styles hooked up in the mapping list
			m_settings.SaveSettings();

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(40, 0, 0, ScrVers.English), new ScrReference(40, 1, 3, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read id segment ");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("MAT "));
			Assert.That(textSeg.FirstReference.Book, Is.EqualTo(40));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read c segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment v 1-3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(0));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(0));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment vt");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\vt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"El era la inceput cu Dumenzeu "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(0));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(0));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment btvt");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\btvt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"He was with God "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Segment, Is.EqualTo(0));
			Assert.That(textSeg.LastReference.Verse, Is.EqualTo(3));
			Assert.That(textSeg.LastReference.Segment, Is.EqualTo(0));

			Assert.That(textEnum.Next(), Is.Null, "Read too many segments");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the encoding converter is being called on the proper text segments.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertingTextSegments_MainImportDomain()
		{
			string filename = m_fileOs.MakeSfFile(Encoding.GetEncoding(EncodingConstants.kMagicCodePage), false, "MAT",
				@"\mt Matthew",
				@"\c 1",
				@"\v 1",
				@"\vt This is \em my\em* verse text with a",
				@"\sp espanol",
				@"\k keyword",
				@"\f footnote text", // This tests the switch statement in GetEncodingConverterForMarkerDomain
				@"\spkwf raro", // This tests the need for & ~MarkerDomain.Footnote in ConvertSource
				@"\ft end of footnote",
				@"\btvt my \em Back\em* translation",
				@"\k keywordbt");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\em ", @"\em*", "Emphasis"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\k", null, "Key Word"));

			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\sp", MappingSet.Main);
			mapping.StyleName = "Default Paragraph Characters";
			mapping.WsId = "es";
			mapping = m_settings.MappingForMarker(@"\f", MappingSet.Main);
			mapping.StyleName = ScrStyleNames.NormalFootnoteParagraph;

			mapping = m_settings.MappingForMarker(@"\spkwf", MappingSet.Main);
			mapping.StyleName = "Key Word";
			mapping.WsId = "es";
			mapping.Domain = MarkerDomain.Default | MarkerDomain.Footnote;

			mapping = m_settings.MappingForMarker(@"\ft", MappingSet.Main);
			mapping.StyleName = "Default Paragraph Characters";
			mapping.Domain = MarkerDomain.Default | MarkerDomain.Footnote;

			mapping = m_settings.MappingForMarker(@"\btvt", MappingSet.Main);
			mapping.Domain = MarkerDomain.BackTrans;

			// Set the vernacular WS to use the UPPERCASE encoding converter
			VernacularWs.LegacyMapping = "UPPERCASE";

			// Save settings before enumerating, which will get the styles hooked up in the mapping list
			m_settings.SaveSettings();

			m_converters = MockRepository.GenerateStub<IEncConverters>();
			m_converters.Stub(x => x["UPPERCASE"]).Return(new DummyEncConverter());
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(40, 0, 0, ScrVers.English),
				new ScrReference(40, 1, 2, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read id segment ");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("MAT "));
			Assert.That(textSeg.FirstReference.Book, Is.EqualTo(40));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read mt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"MATTHEW "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read c segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read v 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo("1"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read first vt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\vt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"THIS IS "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read emphasis segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\em "));
			Assert.That(textSeg.Text, Is.EqualTo(@"MY"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read emphasis segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\em*"));
			Assert.That(textSeg.Text, Is.EqualTo(@" VERSE TEXT WITH A "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read Spanish segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\sp"));
			Assert.That(textSeg.Text, Is.EqualTo(@"espanol "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read keyword segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\k"));
			Assert.That(textSeg.Text, Is.EqualTo(@"KEYWORD "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read footnote text segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\f"));
			Assert.That(textSeg.Text, Is.EqualTo(@"FOOTNOTE TEXT "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read Spanish keyword in footnote segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\spkwf"));
			Assert.That(textSeg.Text, Is.EqualTo(@"raro "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read end of footnote segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\ft"));
			Assert.That(textSeg.Text, Is.EqualTo(@"END OF FOOTNOTE "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read btvt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\btvt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"my "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read BT \em segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\em "));
			Assert.That(textSeg.Text, Is.EqualTo(@"Back"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, @"Unable to read BT \em segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\em*"));
			Assert.That(textSeg.Text, Is.EqualTo(@" translation "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read BT keyword segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\k"));
			Assert.That(textSeg.Text, Is.EqualTo(@"keywordbt "));

			Assert.That(textEnum.Next(), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the encoding converter is being called on the proper text segments when
		/// BT is mapped to default para chars (TE-5079)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertingTextSegments_InterleavedBt()
		{
			string filename = m_fileOs.MakeSfFile(Encoding.GetEncoding(EncodingConstants.kMagicCodePage), false, "MAT",
				@"\mt Matthew",
				@"\c 1",
				@"\v 1 This is my verse text",
				@"\rt my Back translation",
				@"\v 2 Second verse");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\rt", MappingSet.Main);
			mapping.StyleName = "Default Paragraph Characters";
			mapping.Domain = MarkerDomain.BackTrans;

			// Set the vernacular WS to use the UPPERCASE encoding converter
			VernacularWs.LegacyMapping = "UPPERCASE";

			// Save settings before enumerating, which will get the styles hooked up in the mapping list
			m_settings.SaveSettings();

			m_converters = MockRepository.GenerateStub<IEncConverters>();
			m_converters.Stub(x => x["UPPERCASE"]).Return(new DummyEncConverter());
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(40, 0, 0, ScrVers.English),
				new ScrReference(40, 1, 2, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read id segment ");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("MAT "));
			Assert.That(textSeg.FirstReference.Book, Is.EqualTo(40));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read mt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"MATTHEW "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read c segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read v 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo("1"));
			Assert.That(textSeg.Text, Is.EqualTo(@" THIS IS MY VERSE TEXT "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read btvt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\rt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"my Back translation "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read v 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo("2"));
			Assert.That(textSeg.Text, Is.EqualTo(@" SECOND VERSE "));

			Assert.That(textEnum.Next(), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the encoding converter is being called on the proper text segments when
		/// processing a file in the BT domain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertingTextSegments_BTImportDomain()
		{
			string filename = m_fileOs.MakeSfFile(Encoding.GetEncoding(EncodingConstants.kMagicCodePage), false, "MAT",
				@"\mt Matthew",
				@"\c 1",
				@"\v 1",
				@"\vt my \uw retronica\uw* translation",
				@"\k keywordbt");
			m_settings.AddFile(filename, ImportDomain.BackTrans, "en", null);

			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\uw ", @"\uw*", false,
				MappingTargetType.TEStyle, MarkerDomain.BackTrans, "Untranslated Word", "es"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\k", null, "Key Word"));

			// Set the English WS to use the UPPERCASE encoding converter
			Cache.ServiceLocator.WritingSystemManager.Get("en").LegacyMapping = "UPPERCASE";

			// Save settings before enumerating, which will get the styles hooked up in the mapping list
			m_settings.SaveSettings();

			m_converters = MockRepository.GenerateStub<IEncConverters>();
			m_converters.Stub(x => x["UPPERCASE"]).Return(new DummyEncConverter());
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.BackTrans,
				new ScrReference(40, 0, 0, ScrVers.English),
				new ScrReference(40, 1, 2, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read id segment ");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("MAT "));
			Assert.That(textSeg.FirstReference.Book, Is.EqualTo(40));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read mt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"MATTHEW "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read c segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read v 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.LiteralVerseNum, Is.EqualTo("1"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read first vt segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\vt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"MY "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read untranslated word segment (Spanish)");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\uw "));
			Assert.That(textSeg.Text, Is.EqualTo(@"retronica"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to segment following untranslated word");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\uw*"));
			Assert.That(textSeg.Text, Is.EqualTo(@" TRANSLATION "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read keyword segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\k"));
			Assert.That(textSeg.Text, Is.EqualTo(@"KEYWORDBT "));

			Assert.That(textEnum.Next(), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-503
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TESOMustAllowImportWithoutVerseNumbers()
		{
			string filename = m_fileOs.MakeSfFile("EPH", @"\c 1", @"\s My Section",
				@"\p Some verse text", @"\p More text", @"\c 2", @"\s Dude",
				@"\p Beginning of chapter two");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 2, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\s"));
			Assert.That(textSeg.Text, Is.EqualTo(@"My Section "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Some verse text "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@"More text "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 6");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(2));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 7");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\s"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Dude "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(2));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 8");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Beginning of chapter two "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-503
		/// Added test so that some chapters contain verses and others don't. Could be more
		/// robust by adding multiple files with different patterns of verse number usage:
		/// First Chapter, Last Chapter, inbetween, and different combinations of them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TESOAllowsChaptersWithAndWithoutVerses()
		{
			string filename = m_fileOs.MakeSfFile("EPH",
				@"\c 1",
				@"\s My Section",
				@"\v 1 verse one text",
				@"\p Some text",
				@"\v 2 verse two text",
				@"\p More text",
				@"\c 2",
				@"\s Dude",
				@"\p Beginning of chapter two",
				@"\c 3",
				@"\s Last Chapter");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 2, 131, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\s"));
			Assert.That(textSeg.Text, Is.EqualTo(@"My Section "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" verse one text "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Some text "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 6");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" verse two text "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(1));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(2));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 7");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@"More text "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 8");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(2));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 9");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\s"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Dude "));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(2));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 10");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Beginning of chapter two "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 11");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.FirstReference.Chapter, Is.EqualTo(3));
			Assert.That(textSeg.FirstReference.Verse, Is.EqualTo(1));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 10");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\s"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Last Chapter "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Null, "Shouldn't be any more data");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-248
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DistinguishInlineMarkersFromBackslashesInData()
		{
			string filename = m_fileOs.MakeSfFile(Encoding.UTF8, false, "ROM",
				@"\mt Rom\\ans", // really writes a double backslash
				@"\c 1",
				@"\v 1 This is a %b~picture~ |f{c:\scr\files\pic1.jpg} of %b~Rome~.");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|f{", "}", "Figure"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("%b~", "~", "Key Word"));

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(45, 0, 0, ScrVers.English), new ScrReference(45, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("ROM "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
			Assert.That(textSeg.Text, Is.EqualTo(@"Rom\\ans "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(@" This is a "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Text, Is.EqualTo(@"picture"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Text, Is.EqualTo(@"c:\scr\files\pic1.jpg"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Text, Is.EqualTo(@" of "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Text, Is.EqualTo(@"Rome"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
			Assert.That(textSeg.Text, Is.EqualTo(@". "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test converting ASCII to Unicode during import
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux FWNX-611: fix this unit test on Linux 64-bit.")]
		public void ConvertAsciiToUnicode()
		{
			string encFileName = Path.Combine(Path.GetTempPath(), "test.map");
			try
			{
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ParatextImport.EncTest.map"))
				{
					Assert.That(stream, Is.Not.Null);

					// Define an encoding converter
					using (StreamReader reader = new StreamReader(stream))
					{
						using (StreamWriter writer = new StreamWriter(encFileName))
						{
							writer.Write(reader.ReadToEnd());
						}
					}
				}

				m_converters = new EncConverters();
				m_converters.Add("MyConverter", encFileName, ConvType.Legacy_to_from_Unicode, string.Empty,
					string.Empty, ProcessTypeFlags.UnicodeEncodingConversion);
				Assert.That(m_converters["MyConverter"], Is.Not.Null, "MyConverter didn't get added");

				string filename = m_fileOs.MakeSfFile(Encoding.GetEncoding(EncodingConstants.kMagicCodePage),
					false, "ROM",
					@"\mt 0123456789",
					"\\s \u0081\u009a\u0096\u00b5",
					@"\c 1",
					@"\v 1");
				m_settings.AddFile(filename, ImportDomain.Main, null, null);

				// Set the vernacular WS to use the MyConverter encoder
				VernacularWs.LegacyMapping = "MyConverter";

				ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
					new ScrReference(45, 0, 0, ScrVers.English),
					new ScrReference(45, 1, 1, ScrVers.English));

				ISCTextSegment textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
				Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
				Assert.That(textSeg.Text, Is.EqualTo("ROM "));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
				Assert.That(textSeg.Marker, Is.EqualTo(@"\mt"));
				Assert.That(textSeg.Text, Is.EqualTo("\u0966\u0967\u0968\u0969\u096a\u096b\u096c\u096d\u096e\u096f "));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
				Assert.That(textSeg.Marker, Is.EqualTo(@"\s"));
				Assert.That(textSeg.Text, Is.EqualTo("\u0492\u043a\u2013\u04e9 "));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
				Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
				Assert.That(textSeg.Text, Is.EqualTo(@" "));

				textSeg = textEnum.Next();
				Assert.That(textSeg, Is.Not.Null, "Unable to read segment");
				Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
				Assert.That(textSeg.Text, Is.EqualTo(@" "));
			}
			finally
			{
				m_converters.Remove("MyConverter");
				try
				{
					FileUtils.Delete(encFileName);
				}
				catch { }
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test missing encoding converters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MissingEncodingConverter()
		{
			string encFileName = string.Empty;
			IEncConverters converters = new EncConverters();

			string filename = m_fileOs.MakeSfFile(Encoding.GetEncoding(EncodingConstants.kMagicCodePage), false, "ROM",
				@"\mt 0123456789",
				@"\c 1",
				@"\v 1");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			// Set the vernacular WS to use the MissingEncoder encoder
			VernacularWs.LegacyMapping = "MissingEncoder";

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(45, 0, 0, ScrVers.English), new ScrReference(45, 1, 1, ScrVers.English));

			// read the \id segment
			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment");

			// read the \mt segment which should cause an exception
			Assert.That(() => textSeg = textEnum.Next(),
				Throws.TypeOf<EncodingConverterException>().With.Message.StartsWith("Encoding converter not found."));
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-325: Make sure that the files are closed after we're done with ScriptureObjects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureFilesAreClosed()
		{
			string filename = m_fileOs.MakeSfFile("GEN", @"\c 2", @"\v 1 Cool beans");

			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(1, 2, 1, ScrVers.English), new ScrReference(1, 2, 1, ScrVers.English));
			FileUtils.Delete(filename);

			// do it a second time just to be sure
			filename = m_fileOs.MakeSfFile("EXO", @"\c 1", @"\v 1 Cold legumes");

			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(2, 1, 1, ScrVers.English), new ScrReference(2, 1, 1, ScrVers.English));
			FileUtils.Delete(filename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to seamlessly retrieve segments from an SF source that consists of
		/// multiple files. Jira number for this is TE-515
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultipleFileSFProject()
		{
			string[] files = new string[4];
			files[0] = m_fileOs.MakeSfFile("EPH", new string[] { @"\p", "\\c 1", "\\v 1-4 Text" });
			files[1] = m_fileOs.MakeSfFile("EPH", new string[] { @"\p", "\\c 2", "\\v 1 More Text" });
			files[2] = m_fileOs.MakeSfFile("EPH", new string[] { @"\p", "\\c 3", "\\v 1-2 Last Text", "continued verse text" });
			files[3] = m_fileOs.MakeSfFile("COL", new string[] { @"\p", "\\c 1", "\\v 1 Colossians Text" });

			foreach (string sFile in files)
				m_settings.AddFile(sFile, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(51, 4, 18, ScrVers.English));

			GC.Collect();

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001000));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@""));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));
			Assert.That(textSeg.LastReference.BBCCCVVV, Is.EqualTo(49001001));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" Text "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));
			Assert.That(textSeg.LastReference.BBCCCVVV, Is.EqualTo(49001004));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1 from file 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001000));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2 from file 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@""));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3 from file 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49002001));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4 from file 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" More Text "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49002001));
			Assert.That(textSeg.LastReference.BBCCCVVV, Is.EqualTo(49002001));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1 from file 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001000));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2 from file 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@""));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3 from file 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49003001));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4 from file 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" Last Text continued verse text "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49003001));
			Assert.That(textSeg.LastReference.BBCCCVVV, Is.EqualTo(49003002));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1 from file 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("COL "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(51001000));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2 from file 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@""));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3 from file 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(51001001));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4 from file 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" Colossians Text "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(51001001));
			Assert.That(textSeg.LastReference.BBCCCVVV, Is.EqualTo(51001001));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test breaking a book across files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultipleFileBookImport()
		{
			string[] files = new string[2];
			files[0] = m_fileOs.MakeSfFile("MAT", new string[] { @"\c 1", @"\v 1", @"\v 2" });
			files[1] = m_fileOs.MakeSfFile("MAT", new string[] { @"\c 2", @"\v 1" });

			foreach (string filename in files)
				m_settings.AddFile(filename, ImportDomain.Main, null, null);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new BCVRef(40001001), new BCVRef(40001001));
			ISCTextSegment segment;
			segment = textEnum.Next();
			Assert.That(segment.Marker, Is.EqualTo(@"\id"));

			segment = textEnum.Next();
			Assert.That(segment.Marker, Is.EqualTo(@"\c"));

			segment = textEnum.Next();
			Assert.That(segment.Marker, Is.EqualTo(@"\v"));

			segment = textEnum.Next();
			Assert.That(segment.Marker, Is.EqualTo(@"\v"));

			segment = textEnum.Next();
			Assert.That(segment.Marker, Is.EqualTo(@"\id"));

			segment = textEnum.Next();
			Assert.That(segment.Marker, Is.EqualTo(@"\c"));

			segment = textEnum.Next();
			Assert.That(segment.Marker, Is.EqualTo(@"\v"));

			Assert.That(textEnum.Next(), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-718
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoNotCallConverterForUnicode()
		{
			string filename = m_fileOs.MakeSfFile(Encoding.UTF8, true, "EPH",
				@"\p", @"\c 1", "\\v 1 \u1234");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);

			// Set the vernacular WS to use the Garbagio encoder
			VernacularWs.LegacyMapping = "Garbagio";

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\p"));
			Assert.That(textSeg.Text, Is.EqualTo(@""));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4 from file 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" \u1234 "));
			Assert.That(textSeg.FirstReference.BBCCCVVV, Is.EqualTo(49001001));
			Assert.That(textSeg.LastReference.BBCCCVVV, Is.EqualTo(49001001));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-1841
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SfNonSpaceDelimitedInlineBackslashMarkers()
		{
			string filename = m_fileOs.MakeSfFile("EPH",
				new string[] { @"\c 1", @"\v 1 This \iis\i* nice." });
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(3));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\i", @"\i*", "Emphasis"));
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(4));
			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\i", MappingSet.Main);
			Assert.That(mapping.BeginMarker, Is.EqualTo(@"\i"));
			Assert.That(mapping.EndMarker, Is.EqualTo(@"\i*"));

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" This "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\i"));
			Assert.That(textSeg.Text, Is.EqualTo("is"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\i*"));
			Assert.That(textSeg.Text, Is.EqualTo(" nice. "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetBooksForFile method with a single book per a file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BooksInFile()
		{
			string file1 = m_fileOs.MakeSfFile("MAT",
				new string[] { @"\c 1", @"\v 1" });
			m_settings.AddFile(file1, ImportDomain.Main, null, null);

			// three books in file
			string file2 = m_fileOs.MakeSfFile("GAL",
				new string[] { @"\c 1", @"\v 1", @"\id EPH", @"\c 1", @"\v 1", @"\id PHP", @"\c 1", @"\v 1" });
			m_settings.AddFile(file2, ImportDomain.Main, null, null);

			// check file with one book
			ImportFileSource source = m_settings.GetImportFiles(ImportDomain.Main);
			IEnumerator sourceEnum = source.GetEnumerator();
			sourceEnum.MoveNext();
			ScrImportFileInfo info = (ScrImportFileInfo)sourceEnum.Current;
			List<int> bookList1 = info.BooksInFile;
			Assert.That(bookList1.Count, Is.EqualTo(1));
			Assert.That(bookList1[0], Is.EqualTo(40));

			// check file with three books
			sourceEnum.MoveNext();
			info = (ScrImportFileInfo)sourceEnum.Current;
			List<int> bookList2 = info.BooksInFile;
			Assert.That(bookList2.Count, Is.EqualTo(3));
			Assert.That(bookList2[0], Is.EqualTo(48));
			Assert.That(bookList2[1], Is.EqualTo(49));
			Assert.That(bookList2[2], Is.EqualTo(50));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-1475
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SfSpaceDelimitedInlineBackslashMarkers()
		{
			string filename = m_fileOs.MakeSfFile("EPH",
				new string[] { @"\c 1", @"\v 1 This don't work\f Footnote.\fe." });
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(3));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\f ", @"\fe", false,
				MappingTargetType.TEStyle, MarkerDomain.Footnote, "Note General Paragraph", null));
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(4));
			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\f ", MappingSet.Main);
			Assert.That(mapping.BeginMarker, Is.EqualTo(@"\f "));
			Assert.That(mapping.EndMarker, Is.EqualTo(@"\fe"));
			Assert.That(mapping.IsInline, Is.EqualTo(true));

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" This don't work"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\f "));
			Assert.That(textSeg.Text, Is.EqualTo("Footnote."));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\fe"));
			Assert.That(textSeg.Text, Is.EqualTo(". "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-1350
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SfDroppedSpaceAfterEndingBackslashMarkers()
		{
			// FYI: this data intentionally includes a spurious space following "don't" and
			// another following "Footnote."
			string filename = m_fileOs.MakeSfFile("EPH",
				new string[] { @"\c 1", @"\v 1 This don't \f Footnote. \fe work." });
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(3));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\f ", @"\fe", false,
				MappingTargetType.TEStyle, MarkerDomain.Footnote, "Note General Paragraph", null));
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(4));
			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\f ", MappingSet.Main);
			Assert.That(mapping.BeginMarker, Is.EqualTo(@"\f "));
			Assert.That(mapping.EndMarker, Is.EqualTo(@"\fe"));
			Assert.That(mapping.IsInline, Is.EqualTo(true));

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" This don't "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\f "));
			Assert.That(textSeg.Text, Is.EqualTo("Footnote. "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\fe"));
			Assert.That(textSeg.Text, Is.EqualTo(" work. "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test character styles embedded in footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CharStyleInFootnote()
		{
			string filename = m_fileOs.MakeSfFile("EPH",
				new string[] { @"\c 1", @"\v 1 This %fis a %iemphasized%i* footnote%f* test." });
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(3));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("%f", "%f*", false,
				MappingTargetType.TEStyle, MarkerDomain.Footnote, "Note General Paragraph", null));
			m_settings.SetMapping(MappingSet.Main,
				new ImportMappingInfo("%i", "%i*", "Emphasis"));

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" This "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo("%f"));
			Assert.That(textSeg.Text, Is.EqualTo("is a "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo("%i"));
			Assert.That(textSeg.Text, Is.EqualTo("emphasized"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 6");
			Assert.That(textSeg.Marker, Is.EqualTo("%i*"));
			Assert.That(textSeg.Text, Is.EqualTo(" footnote"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 7");
			Assert.That(textSeg.Marker, Is.EqualTo("%f*"));
			Assert.That(textSeg.Text, Is.EqualTo(" test. "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-621
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SfBackToBackInlineMarkers()
		{
			string filename = m_fileOs.MakeSfFile("EPH",
				new string[] { @"\c 1", @"\v 1 This |iis|r |ua|r nice test." });
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(3));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|i", "|r", "Emphasis"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|u", "|r", "Key Word"));
			Assert.That(m_settings.GetMappingListForDomain(ImportDomain.Main).Count, Is.EqualTo(5));

			ImportMappingInfo mapping = m_settings.MappingForMarker(@"|i", MappingSet.Main);
			Assert.That(mapping.BeginMarker, Is.EqualTo(@"|i"));
			Assert.That(mapping.EndMarker, Is.EqualTo(@"|r"));
			Assert.That(mapping.IsInline, Is.EqualTo(true));

			mapping = m_settings.MappingForMarker(@"|u", MappingSet.Main);
			Assert.That(mapping.BeginMarker, Is.EqualTo(@"|u"));
			Assert.That(mapping.EndMarker, Is.EqualTo(@"|r"));
			Assert.That(mapping.IsInline, Is.EqualTo(true));

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 1");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\id"));
			Assert.That(textSeg.Text, Is.EqualTo("EPH "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 2");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\c"));
			Assert.That(textSeg.Text, Is.EqualTo(@" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 3");
			Assert.That(textSeg.Marker, Is.EqualTo(@"\v"));
			Assert.That(textSeg.Text, Is.EqualTo(" This "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 4");
			Assert.That(textSeg.Marker, Is.EqualTo("|i"));
			Assert.That(textSeg.Text, Is.EqualTo("is"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 5");
			Assert.That(textSeg.Marker, Is.EqualTo("|r"));
			Assert.That(textSeg.Text, Is.EqualTo(" "));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 6");
			Assert.That(textSeg.Marker, Is.EqualTo("|u"));
			Assert.That(textSeg.Text, Is.EqualTo("a"));

			textSeg = textEnum.Next();
			Assert.That(textSeg, Is.Not.Null, "Unable to read segment 7");
			Assert.That(textSeg.Marker, Is.EqualTo("|r"));
			Assert.That(textSeg.Text, Is.EqualTo(" nice test. "));
		}
		#endregion

		#region Error reporting tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that we get the proper error when a data file has been deleted.
		/// Jira number for this is TE-466 (part of TE-76).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeletedDataFile()
		{
			string sFilename = m_fileOs.MakeSfFile("GEN",
					new string[] { @"\mt Genesis", @"\c 1", @"\v 1 My verse" });
				m_settings.AddFile(sFilename, ImportDomain.Main, null, null);
				ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
					new ScrReference(1, 0, 0, ScrVers.English), new ScrReference(1, 1, 1, ScrVers.English));
			FileUtils.Delete(sFilename);

				// calling Next will cause the file to be read
			ScriptureUtilsException e = Assert.Throws<ScriptureUtilsException>(() => textEnum.Next());
				Assert.That(SUE_ErrorCode.FileError, Is.EqualTo(e.ErrorCode));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that SCTextEnum ignores irrelevent data files that have been deleted.
		/// It should only check for the existence of files that are actually in range to be
		/// loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IgnoreDeletedDataFile()
		{
			string sFilename = m_fileOs.MakeSfFile(Encoding.UTF8, true, "GEN",
				@"\mt Genesis", @"\c 1", @"\v 1 My verse");
			m_settings.AddFile(sFilename, ImportDomain.Main, null, null);
			sFilename = m_fileOs.MakeSfFile(Encoding.UTF8, true, "EXO",
				@"\mt Exodus", @"\c 1", @"\v 1 Delete me!");
			m_settings.AddFile(sFilename, ImportDomain.Main, null, null);
			Assert.That(m_settings.GetImportFiles(ImportDomain.Main).Count, Is.EqualTo(2));
			FileInfo exodusFile = new FileInfo(sFilename);

			// now delete exodus and read the segments
			exodusFile.Delete();
			Assert.That(m_settings.GetImportFiles(ImportDomain.Main).Count, Is.EqualTo(2));
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(1, 0, 0, ScrVers.English), new ScrReference(1, 1, 1, ScrVers.English));

			// assume that we will not get an error reading segments.
			while (textEnum.Next() != null)
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that no segments are read and no error occurs when attempting to read segments
		/// using a reference range for which no data files exist. Jira number for this is TE-76.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MissingDataFile()
		{
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(1, 0, 0, ScrVers.English), new ScrReference(1, 1, 1, ScrVers.English));
			Assert.That(textEnum.Next(), Is.Null, "Should be no segments to read");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that no segments are read and no error occurs when attempting to read segments
		/// using a reference range for which data does not exist. Jira number for this is TE-76.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoRelevantData()
		{
			string sFilename = m_fileOs.MakeSfFile("GEN", @"\mt Genesis", @"\c 1", @"\v 1 My verse");
			m_settings.AddFile(sFilename, ImportDomain.Main, null, null);
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(2, 0, 0, ScrVers.English), new ScrReference(2, 1, 1, ScrVers.English));

			Assert.That(textEnum.Next(), Is.Null, "Should be no segments to read");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that line numbers are reported correctly for lines that don't start with a
		/// backslash marker (TE-5538).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LineNumbers()
		{
			string filename = m_fileOs.MakeSfFile("GEN",
				@"\c 1",
				@"\v 1",
				@"\vt First line",
				@"second line",
				@"third line",
				@"\vt next marker");
			m_settings.AddFile(filename, ImportDomain.Main, null, null);
			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(1, 0, 0, ScrVers.English), new ScrReference(1, 1, 1, ScrVers.English));

			ISCTextSegment text = textEnum.Next(); // id line
			text = textEnum.Next();	// Chapter one
			text = textEnum.Next(); // Verse one
			text = textEnum.Next(); // First to third line
			text = textEnum.Next();	// next marker

			Assert.That(text.CurrentLineNumber, Is.EqualTo(7));
		}
		#endregion
	}
	#endregion
}
