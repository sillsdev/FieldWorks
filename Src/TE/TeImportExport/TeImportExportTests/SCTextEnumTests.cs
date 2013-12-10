// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SCTextEnumTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using ECInterfaces;
using SilEncConverters40;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.TE
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
	public class SCTextEnumTests : ScrInMemoryFdoTestBase
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
			foreach (IWritingSystem ws in Cache.ServiceLocator.WritingSystemManager.LocalWritingSystems)
				ws.LegacyMapping = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_settings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create(ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(m_settings);
			m_settings.ImportTypeEnum = TypeOfImport.Other;
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
		private IWritingSystem VernacularWs
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
			Assert.IsNotNull(textEnum, "No TextEnum object was returned");

			ISCTextSegment textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read GEN 2:1.");
			Assert.AreEqual(expectedRef, textSeg.FirstReference, "Incorrect reference returned");
			Assert.AreEqual(" Le ciel, la terre et tous leurs lments furent achevs. ",
				textSeg.Text, "Incorrect data found at GEN 2:1");
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
			Assert.IsNotNull(textSeg, "Unable to read first segment");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read second segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"\it fun \it* Mi\\abi \taki ", textSeg.Text);
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
			Assert.IsNotNull(textSeg, @"Unable to read \id segment");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \mt segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \it segment");
			Assert.AreEqual(@"\it", textSeg.Marker);
			Assert.AreEqual(@"fun", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \it* segment");
			Assert.AreEqual(@"\it*", textSeg.Marker);
			Assert.AreEqual(@" Mi", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \\abi segment");
			Assert.AreEqual(@"\\abi", textSeg.Marker);
			Assert.AreEqual(@"", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \taki segment");
			Assert.AreEqual(@"\taki", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
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
			Assert.IsNotNull(textSeg, @"Unable to read \id segment");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \mt segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"fun ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \f segment");
			Assert.AreEqual(@"\f", textSeg.Marker);
			Assert.AreEqual(@"footnote ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \f* segment");
			Assert.AreEqual(@"\f*", textSeg.Marker);
			Assert.AreEqual(@", isn't it? ", textSeg.Text);
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
			Assert.IsNotNull(textSeg, @"Unable to read \id segment");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \mt segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"fun ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \f segment");
			Assert.AreEqual(@"\f", textSeg.Marker);
			Assert.AreEqual(@"+ ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \ft segment");
			Assert.AreEqual(@"\ft", textSeg.Marker);
			Assert.AreEqual(@"footnote ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read \f* segment");
			Assert.AreEqual(@"\f*", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
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
			Assert.IsNotNull(textSeg, "Unable to read first segment");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("PHP ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg);
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(50001001, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg);
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(50001001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(" verse 1 of phillipians ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg);
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(50001002, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(" here is verse 2 ", textSeg.Text);

			Assert.IsNull(textEnum.Next());
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
				Assert.IsNotNull(textSeg, "Unable to read first segment");
				Assert.AreEqual(@"\id", textSeg.Marker);
				Assert.AreEqual("EPH ", textSeg.Text);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg);
				Assert.AreEqual(@"\mt", textSeg.Marker);
				Assert.AreEqual("Ephesians ", textSeg.Text);
				Assert.AreEqual(49001000, textSeg.FirstReference.BBCCCVVV);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg);
				Assert.AreEqual(@"\c", textSeg.Marker);
				Assert.AreEqual(" ", textSeg.Text);
				Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg);
				Assert.AreEqual(@"\p", textSeg.Marker);
				Assert.AreEqual(string.Empty, textSeg.Text);
				Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg);
				Assert.AreEqual(@"\v", textSeg.Marker);
				Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);
				Assert.AreEqual(" hello there ", textSeg.Text);

				Assert.IsNull(textEnum.Next());
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
			Assert.IsNotNull(textSeg, "Unable to read first segment");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg);
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(49001000, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg);
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg);
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(" hello there ", textSeg.Text);

			Assert.IsNull(textEnum.Next());
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
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("MAT ", textSeg.Text);
			Assert.AreEqual(40, textSeg.FirstReference.Book);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(0, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(" First Chapter ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" Verse text ", textSeg.Text);
			Assert.AreEqual(@"1.", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@"aForgot space! ", textSeg.Text);
			Assert.AreEqual(@"2-3", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(2, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(3, textSeg.LastReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@"ab.Missing Space ", textSeg.Text);
			Assert.AreEqual(@"2-3", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(2, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(3, textSeg.LastReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 6");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@"abMissing Space ", textSeg.Text);
			Assert.AreEqual(@"2-3.", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(2, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(3, textSeg.LastReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 7");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@"bMissing Space ", textSeg.Text);
			Assert.AreEqual(@"2-3a.", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(2, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(3, textSeg.LastReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 8");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@"a.b.Missing Space ", textSeg.Text);
			Assert.AreEqual(@"2-3.", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(2, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(3, textSeg.LastReference.Verse);
			// lower 3 bits represent the versification
			Assert.AreEqual(0, textSeg.LastReference.Segment);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 9");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@"-blah ", textSeg.Text);
			Assert.AreEqual(@"5", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(5, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 10");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" blah ", textSeg.Text);
			Assert.AreEqual(@"6-", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(6, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 11");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@"a,8.a,9a. testing ", textSeg.Text);
			Assert.AreEqual(@"7.", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(7, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(7, textSeg.LastReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 12");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" Text with RTL ", textSeg.Text);
			Assert.AreEqual("8\u200f-\u200f9", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(8, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(9, textSeg.LastReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 13");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" Text with unicode hyphen ", textSeg.Text);
			Assert.AreEqual("10\u201011", textSeg.LiteralVerseNum);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(10, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Chapter);
			Assert.AreEqual(11, textSeg.LastReference.Verse);
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
			Assert.IsNotNull(textSeg, "Unable to read segment 1");

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(string.Empty, textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\it ", textSeg.Marker);
			Assert.AreEqual("Matthew", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\it*", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
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
			Assert.IsNotNull(textSeg, "Unable to read id segment ");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("MAT ", textSeg.Text);
			Assert.AreEqual(40, textSeg.FirstReference.Book);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read mt segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(0, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read c segment");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment v 1a");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" Verse part a ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.FirstReference.Segment);
			Assert.AreEqual(1, textSeg.LastReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Segment);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment v 1c");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" Verse part b ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);
			Assert.AreEqual(2, textSeg.FirstReference.Segment);
			Assert.AreEqual(1, textSeg.LastReference.Verse);
			Assert.AreEqual(2, textSeg.LastReference.Segment);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment v 1e");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" Verse part c ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);
			Assert.AreEqual(3, textSeg.FirstReference.Segment);
			Assert.AreEqual(1, textSeg.LastReference.Verse);
			Assert.AreEqual(3, textSeg.LastReference.Segment);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment v 2a-2b");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(2, textSeg.FirstReference.Verse);
			Assert.AreEqual(1, textSeg.FirstReference.Segment);
			Assert.AreEqual(2, textSeg.LastReference.Verse);
			Assert.AreEqual(2, textSeg.LastReference.Segment);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment v 3-4a");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(3, textSeg.FirstReference.Verse);
			Assert.AreEqual(0, textSeg.FirstReference.Segment);
			Assert.AreEqual(4, textSeg.LastReference.Verse);
			Assert.AreEqual(1, textSeg.LastReference.Segment);
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
			Assert.IsNotNull(textSeg, "Unable to read id segment ");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("MAT ", textSeg.Text);
			Assert.AreEqual(40, textSeg.FirstReference.Book);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read c segment");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment v 1-3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);
			Assert.AreEqual(0, textSeg.FirstReference.Segment);
			Assert.AreEqual(3, textSeg.LastReference.Verse);
			Assert.AreEqual(0, textSeg.LastReference.Segment);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment vt");
			Assert.AreEqual(@"\vt", textSeg.Marker);
			Assert.AreEqual(@"El era la inceput cu Dumenzeu ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);
			Assert.AreEqual(0, textSeg.FirstReference.Segment);
			Assert.AreEqual(3, textSeg.LastReference.Verse);
			Assert.AreEqual(0, textSeg.LastReference.Segment);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment btvt");
			Assert.AreEqual(@"\btvt", textSeg.Marker);
			Assert.AreEqual(@"He was with God ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);
			Assert.AreEqual(0, textSeg.FirstReference.Segment);
			Assert.AreEqual(3, textSeg.LastReference.Verse);
			Assert.AreEqual(0, textSeg.LastReference.Segment);

			Assert.IsNull(textEnum.Next(), "Read too many segments");
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
			Assert.IsNotNull(textSeg, "Unable to read id segment ");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("MAT ", textSeg.Text);
			Assert.AreEqual(40, textSeg.FirstReference.Book);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read mt segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"MATTHEW ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read c segment");
			Assert.AreEqual(@"\c", textSeg.Marker);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read v 1");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual("1", textSeg.LiteralVerseNum);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read first vt segment");
			Assert.AreEqual(@"\vt", textSeg.Marker);
			Assert.AreEqual(@"THIS IS ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read emphasis segment");
			Assert.AreEqual(@"\em ", textSeg.Marker);
			Assert.AreEqual(@"MY", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read emphasis segment");
			Assert.AreEqual(@"\em*", textSeg.Marker);
			Assert.AreEqual(@" VERSE TEXT WITH A ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read Spanish segment");
			Assert.AreEqual(@"\sp", textSeg.Marker);
			Assert.AreEqual(@"espanol ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read keyword segment");
			Assert.AreEqual(@"\k", textSeg.Marker);
			Assert.AreEqual(@"KEYWORD ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read footnote text segment");
			Assert.AreEqual(@"\f", textSeg.Marker);
			Assert.AreEqual(@"FOOTNOTE TEXT ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read Spanish keyword in footnote segment");
			Assert.AreEqual(@"\spkwf", textSeg.Marker);
			Assert.AreEqual(@"raro ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read end of footnote segment");
			Assert.AreEqual(@"\ft", textSeg.Marker);
			Assert.AreEqual(@"END OF FOOTNOTE ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read btvt segment");
			Assert.AreEqual(@"\btvt", textSeg.Marker);
			Assert.AreEqual(@"my ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read BT \em segment");
			Assert.AreEqual(@"\em ", textSeg.Marker);
			Assert.AreEqual(@"Back", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, @"Unable to read BT \em segment");
			Assert.AreEqual(@"\em*", textSeg.Marker);
			Assert.AreEqual(@" translation ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read BT keyword segment");
			Assert.AreEqual(@"\k", textSeg.Marker);
			Assert.AreEqual(@"keywordbt ", textSeg.Text);

			Assert.IsNull(textEnum.Next());
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
			Assert.IsNotNull(textSeg, "Unable to read id segment ");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("MAT ", textSeg.Text);
			Assert.AreEqual(40, textSeg.FirstReference.Book);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read mt segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"MATTHEW ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read c segment");
			Assert.AreEqual(@"\c", textSeg.Marker);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read v 1");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual("1", textSeg.LiteralVerseNum);
			Assert.AreEqual(@" THIS IS MY VERSE TEXT ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read btvt segment");
			Assert.AreEqual(@"\rt", textSeg.Marker);
			Assert.AreEqual(@"my Back translation ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read v 2");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual("2", textSeg.LiteralVerseNum);
			Assert.AreEqual(@" SECOND VERSE ", textSeg.Text);

			Assert.IsNull(textEnum.Next());
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
			Assert.IsNotNull(textSeg, "Unable to read id segment ");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("MAT ", textSeg.Text);
			Assert.AreEqual(40, textSeg.FirstReference.Book);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read mt segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"MATTHEW ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read c segment");
			Assert.AreEqual(@"\c", textSeg.Marker);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read v 1");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual("1", textSeg.LiteralVerseNum);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read first vt segment");
			Assert.AreEqual(@"\vt", textSeg.Marker);
			Assert.AreEqual(@"MY ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read untranslated word segment (Spanish)");
			Assert.AreEqual(@"\uw ", textSeg.Marker);
			Assert.AreEqual(@"retronica", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to segment following untranslated word");
			Assert.AreEqual(@"\uw*", textSeg.Marker);
			Assert.AreEqual(@" TRANSLATION ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read keyword segment");
			Assert.AreEqual(@"\k", textSeg.Marker);
			Assert.AreEqual(@"KEYWORDBT ", textSeg.Text);

			Assert.IsNull(textEnum.Next());
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
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\s", textSeg.Marker);
			Assert.AreEqual(@"My Section ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"Some verse text ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"More text ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 6");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(2, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 7");
			Assert.AreEqual(@"\s", textSeg.Marker);
			Assert.AreEqual(@"Dude ", textSeg.Text);
			Assert.AreEqual(2, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 8");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"Beginning of chapter two ", textSeg.Text);
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
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\s", textSeg.Marker);
			Assert.AreEqual(@"My Section ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" verse one text ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"Some text ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 6");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" verse two text ", textSeg.Text);
			Assert.AreEqual(1, textSeg.FirstReference.Chapter);
			Assert.AreEqual(2, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 7");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"More text ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 8");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(2, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 9");
			Assert.AreEqual(@"\s", textSeg.Marker);
			Assert.AreEqual(@"Dude ", textSeg.Text);
			Assert.AreEqual(2, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 10");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"Beginning of chapter two ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 11");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(3, textSeg.FirstReference.Chapter);
			Assert.AreEqual(1, textSeg.FirstReference.Verse);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 10");
			Assert.AreEqual(@"\s", textSeg.Marker);
			Assert.AreEqual(@"Last Chapter ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNull(textSeg, "Shouldn't be any more data");
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
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("ROM ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@"\mt", textSeg.Marker);
			Assert.AreEqual(@"Rom\\ans ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(@" This is a ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@"picture", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@"c:\scr\files\pic1.jpg", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@" of ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@"Rome", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment");
			Assert.AreEqual(@". ", textSeg.Text);
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
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
					"SIL.FieldWorks.TE.EncTest.map"))
				{
					Assert.IsNotNull(stream);

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
				Assert.NotNull(m_converters["MyConverter"], "MyConverter didn't get added");

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
				Assert.IsNotNull(textSeg, "Unable to read segment");
				Assert.AreEqual(@"\id", textSeg.Marker);
				Assert.AreEqual("ROM ", textSeg.Text);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg, "Unable to read segment");
				Assert.AreEqual(@"\mt", textSeg.Marker);
				Assert.AreEqual("\u0966\u0967\u0968\u0969\u096a\u096b\u096c\u096d\u096e\u096f ", textSeg.Text);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg, "Unable to read segment");
				Assert.AreEqual(@"\s", textSeg.Marker);
				Assert.AreEqual("\u0492\u043a\u2013\u04e9 ", textSeg.Text);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg, "Unable to read segment");
				Assert.AreEqual(@"\c", textSeg.Marker);
				Assert.AreEqual(@" ", textSeg.Text);

				textSeg = textEnum.Next();
				Assert.IsNotNull(textSeg, "Unable to read segment");
				Assert.AreEqual(@"\v", textSeg.Marker);
				Assert.AreEqual(@" ", textSeg.Text);
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
		[ExpectedException(typeof(EncodingConverterException), ExpectedMessage="Encoding converter not found.",
			MatchType=MessageMatch.StartsWith)]
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
			Assert.IsNotNull(textSeg, "Unable to read segment");

				// read the \mt segment which should cause an exception
				textSeg = textEnum.Next();
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
			Assert.IsNotNull(textSeg, "Unable to read segment 1 from file 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);
			Assert.AreEqual(49001000, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2 from file 1");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3 from file 1");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
			Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(49001001, textSeg.LastReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4 from file 1");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" Text ", textSeg.Text);
			Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(49001004, textSeg.LastReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1 from file 2");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);
			Assert.AreEqual(49001000, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2 from file 2");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3 from file 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
			Assert.AreEqual(49002001, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4 from file 2");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" More Text ", textSeg.Text);
			Assert.AreEqual(49002001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(49002001, textSeg.LastReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1 from file 3");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);
			Assert.AreEqual(49001000, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2 from file 3");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3 from file 3");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
			Assert.AreEqual(49003001, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4 from file 3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" Last Text continued verse text ", textSeg.Text);
			Assert.AreEqual(49003001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(49003002, textSeg.LastReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1 from file 4");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("COL ", textSeg.Text);
			Assert.AreEqual(51001000, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2 from file 4");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3 from file 4");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);
			Assert.AreEqual(51001001, textSeg.FirstReference.BBCCCVVV);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4 from file 4");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" Colossians Text ", textSeg.Text);
			Assert.AreEqual(51001001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(51001001, textSeg.LastReference.BBCCCVVV);
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
			Assert.AreEqual(@"\id", segment.Marker);

			segment = textEnum.Next();
			Assert.AreEqual(@"\c", segment.Marker);

			segment = textEnum.Next();
			Assert.AreEqual(@"\v", segment.Marker);

			segment = textEnum.Next();
			Assert.AreEqual(@"\v", segment.Marker);

			segment = textEnum.Next();
			Assert.AreEqual(@"\id", segment.Marker);

			segment = textEnum.Next();
			Assert.AreEqual(@"\c", segment.Marker);

			segment = textEnum.Next();
			Assert.AreEqual(@"\v", segment.Marker);

			Assert.IsNull(textEnum.Next());
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
			Assert.IsNotNull(textSeg, "Unable to read segment 1 from file 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2 from file 1");
			Assert.AreEqual(@"\p", textSeg.Marker);
			Assert.AreEqual(@"", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3 from file 1");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4 from file 1");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" \u1234 ", textSeg.Text);
			Assert.AreEqual(49001001, textSeg.FirstReference.BBCCCVVV);
			Assert.AreEqual(49001001, textSeg.LastReference.BBCCCVVV);
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
			Assert.AreEqual(3, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\i", @"\i*", "Emphasis"));
			Assert.AreEqual(4, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\i", MappingSet.Main);
			Assert.AreEqual(@"\i", mapping.BeginMarker);
			Assert.AreEqual(@"\i*", mapping.EndMarker);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" This ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual(@"\i", textSeg.Marker);
			Assert.AreEqual("is", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual(@"\i*", textSeg.Marker);
			Assert.AreEqual(" nice. ", textSeg.Text);
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
			Assert.AreEqual(1, bookList1.Count);
			Assert.AreEqual(40, bookList1[0]);

			// check file with three books
			sourceEnum.MoveNext();
			info = (ScrImportFileInfo)sourceEnum.Current;
			List<int> bookList2 = info.BooksInFile;
			Assert.AreEqual(3, bookList2.Count);
			Assert.AreEqual(48, bookList2[0]);
			Assert.AreEqual(49, bookList2[1]);
			Assert.AreEqual(50, bookList2[2]);
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
			Assert.AreEqual(3, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\f ", @"\fe", false,
				MappingTargetType.TEStyle, MarkerDomain.Footnote, "Note General Paragraph", null));
			Assert.AreEqual(4, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\f ", MappingSet.Main);
			Assert.AreEqual(@"\f ", mapping.BeginMarker);
			Assert.AreEqual(@"\fe", mapping.EndMarker);
			Assert.AreEqual(true, mapping.IsInline);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" This don't work", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual(@"\f ", textSeg.Marker);
			Assert.AreEqual("Footnote.", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual(@"\fe", textSeg.Marker);
			Assert.AreEqual(". ", textSeg.Text);
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
			Assert.AreEqual(3, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\f ", @"\fe", false,
				MappingTargetType.TEStyle, MarkerDomain.Footnote, "Note General Paragraph", null));
			Assert.AreEqual(4, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			ImportMappingInfo mapping = m_settings.MappingForMarker(@"\f ", MappingSet.Main);
			Assert.AreEqual(@"\f ", mapping.BeginMarker);
			Assert.AreEqual(@"\fe", mapping.EndMarker);
			Assert.AreEqual(true, mapping.IsInline);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" This don't ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual(@"\f ", textSeg.Marker);
			Assert.AreEqual("Footnote. ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual(@"\fe", textSeg.Marker);
			Assert.AreEqual(" work. ", textSeg.Text);
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
			Assert.AreEqual(3, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("%f", "%f*", false,
				MappingTargetType.TEStyle, MarkerDomain.Footnote, "Note General Paragraph", null));
			m_settings.SetMapping(MappingSet.Main,
				new ImportMappingInfo("%i", "%i*", "Emphasis"));

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" This ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual("%f", textSeg.Marker);
			Assert.AreEqual("is a ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual("%i", textSeg.Marker);
			Assert.AreEqual("emphasized", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 6");
			Assert.AreEqual("%i*", textSeg.Marker);
			Assert.AreEqual(" footnote", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 7");
			Assert.AreEqual("%f*", textSeg.Marker);
			Assert.AreEqual(" test. ", textSeg.Text);
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
			Assert.AreEqual(3, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|i", "|r", "Emphasis"));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|u", "|r", "Key Word"));
			Assert.AreEqual(5, m_settings.GetMappingListForDomain(ImportDomain.Main).Count);

			ImportMappingInfo mapping = m_settings.MappingForMarker(@"|i", MappingSet.Main);
			Assert.AreEqual(@"|i", mapping.BeginMarker);
			Assert.AreEqual(@"|r", mapping.EndMarker);
			Assert.AreEqual(true, mapping.IsInline);

			mapping = m_settings.MappingForMarker(@"|u", MappingSet.Main);
			Assert.AreEqual(@"|u", mapping.BeginMarker);
			Assert.AreEqual(@"|r", mapping.EndMarker);
			Assert.AreEqual(true, mapping.IsInline);

			ISCTextEnum textEnum = GetTextEnum(ImportDomain.Main,
				new ScrReference(49, 0, 0, ScrVers.English), new ScrReference(49, 1, 1, ScrVers.English));

			ISCTextSegment textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 1");
			Assert.AreEqual(@"\id", textSeg.Marker);
			Assert.AreEqual("EPH ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 2");
			Assert.AreEqual(@"\c", textSeg.Marker);
			Assert.AreEqual(@" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 3");
			Assert.AreEqual(@"\v", textSeg.Marker);
			Assert.AreEqual(" This ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 4");
			Assert.AreEqual("|i", textSeg.Marker);
			Assert.AreEqual("is", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 5");
			Assert.AreEqual("|r", textSeg.Marker);
			Assert.AreEqual(" ", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 6");
			Assert.AreEqual("|u", textSeg.Marker);
			Assert.AreEqual("a", textSeg.Text);

			textSeg = textEnum.Next();
			Assert.IsNotNull(textSeg, "Unable to read segment 7");
			Assert.AreEqual("|r", textSeg.Marker);
			Assert.AreEqual(" nice test. ", textSeg.Text);
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
				Assert.AreEqual(e.ErrorCode, SUE_ErrorCode.FileError);
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
			Assert.AreEqual(2, m_settings.GetImportFiles(ImportDomain.Main).Count);
			FileInfo exodusFile = new FileInfo(sFilename);

			// now delete exodus and read the segments
			exodusFile.Delete();
			Assert.AreEqual(2, m_settings.GetImportFiles(ImportDomain.Main).Count);
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
			Assert.IsNull(textEnum.Next(), "Should be no segments to read");
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

			Assert.IsNull(textEnum.Next(), "Should be no segments to read");
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

			Assert.AreEqual(7, text.CurrentLineNumber);
		}
		#endregion
	}
	#endregion
}
