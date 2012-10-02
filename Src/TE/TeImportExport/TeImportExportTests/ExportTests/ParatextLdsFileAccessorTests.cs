// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatextLdsFileAccessorTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ParatextLdsFileAccessor class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ParatextLdsFileAccessorTests : ScrInMemoryFdoTestBase
	{
		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the in-memory cache data needed by the tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_scrInMemoryCache.InitializeAnnotationDefs();
			Cache.LangProject.Name.UserDefaultWritingSystem = "Werbl";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the Paratext lds file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteParatextLdsFile()
		{
			if (File.Exists("test.lds"))
				File.Delete("test.lds");

			int wsHvo = Cache.DefaultVernWs;
			LgWritingSystem vernWs = new LgWritingSystem(Cache, wsHvo);
			vernWs.Name.UserDefaultWritingSystem = "French";

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			StyleInfoTable styleTable = new StyleInfoTable("Normal",
				Cache.LanguageWritingSystemFactoryAccessor);
			styleTable.Add("Normal", normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			DummyFileWriter actualLds = new DummyFileWriter();
			ReflectionHelper.CallMethod(ldsAccessor, "WriteParatextLdsFile", "test.lds",
				Cache.DefaultVernWs, normalEntry, actualLds);

			// Verify the .lds file
			string[] expectedLds =
			{
				"[General]",
				"codepage=65001",
				"RTL=F",
				"font=Wingdings",
				"name=French",
				"size=14",
				string.Empty,
				"[Checking]",
				string.Empty,
				"[Characters]",
				string.Empty,
				"[Punctuation]"
			};

			actualLds.VerifyOutput(expectedLds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the Paratext lds file when a back translation is being
		/// exported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteParatextLdsFile_BT()
		{
			if (File.Exists("test.lds"))
				File.Delete("test.lds");

			int wsUrduBT = InMemoryFdoCache.s_wsHvos.Ur;
			IWritingSystem btWs = Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsUrduBT);
			string btWsName = btWs.get_UiName(Cache.DefaultUserWs);

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			StyleInfoTable styleTable = new StyleInfoTable("Normal",
				Cache.LanguageWritingSystemFactoryAccessor);
			styleTable.Add("Normal", normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			DummyFileWriter fileWriterLDS = new DummyFileWriter();
			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			ReflectionHelper.CallMethod(ldsAccessor, "WriteParatextLdsFile", "test.lds",
				wsUrduBT, normalEntry, fileWriterLDS);

			// Verify the .lds file
			string[] expectedLds =
			{
				"[General]",
				"codepage=65001",
				"RTL=T",
				"font=Wingdings",
				"name=" + btWs.get_UiName(Cache.DefaultUserWs),
				"size=14",
				string.Empty,
				"[Checking]",
				string.Empty,
				"[Characters]",
				string.Empty,
				"[Punctuation]"
			};

			fileWriterLDS.VerifyOutput(expectedLds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where nothing changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateParatextLdsFile_NoChange()
		{
			if (File.Exists("test.lds"))
				File.Delete("test.lds");

			int wsHvo = Cache.DefaultVernWs;
			LgWritingSystem vernWs = new LgWritingSystem(Cache, wsHvo);
			vernWs.Name.UserDefaultWritingSystem = "French";

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			StyleInfoTable styleTable = new StyleInfoTable("Normal",
				Cache.LanguageWritingSystemFactoryAccessor);
			styleTable.Add("Normal", normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 10000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Times New Roman";

			string ldsContentsOrig =
				"[General]" + Environment.NewLine +
				"codepage=65001" + Environment.NewLine +
				"DialogFontsize=0" + Environment.NewLine +
				"LowerCaseLetters=" + Environment.NewLine +
				"NoCaseLetters=" + Environment.NewLine +
				"RTL=F" + Environment.NewLine +
				"UpperCaseLetters=" + Environment.NewLine +
				"errors=" + Environment.NewLine +
				"font=Times New Roman" + Environment.NewLine +
				"name=French" + Environment.NewLine +
				"separator=" + Environment.NewLine +
				"size=10" + Environment.NewLine +
				Environment.NewLine +
				"[Checking]" + Environment.NewLine +
				Environment.NewLine +
				"[Characters]" + Environment.NewLine +
				Environment.NewLine +
				"[Punctuation]" + Environment.NewLine +
				"diacritics=" + Environment.NewLine +
				"medial=";

			DummyFileWriter writer = new DummyFileWriter();
			writer.Open("test.lds");
			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			ReflectionHelper.CallMethod(ldsAccessor, "UpdateLdsContents", ldsContentsOrig,
				normalEntry, Cache.DefaultVernWs, (FileWriter)writer);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[General]",
				"codepage=65001",
				"DialogFontsize=0",
				"LowerCaseLetters=",
				"NoCaseLetters=",
				"RTL=F",
				"UpperCaseLetters=",
				"errors=",
				"font=Times New Roman",
				"name=French",
				"separator=",
				"size=10",
				//Environment.NewLine +
				"[Checking]",
				//Environment.NewLine +
				"[Characters]",
				//Environment.NewLine +
				"[Punctuation]",
				"diacritics=",
				"medial="
			};
			writer.VerifyOutput(expectedLdsContents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that UpdateParatextLdsFile returns false if given a filename for a non-existent
		/// file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateParatextLdsFile_FileMissing()
		{
			FileWriter writer = new FileWriter();

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			Assert.IsFalse(ReflectionHelper.GetBoolResult(ldsAccessor, "UpdateLdsFile",
				"c:\\whatever\\wherever\\whenever\\yeah.lds", new DummyUsfmStyEntry(),
				Cache.DefaultVernWs, writer));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where the font and font size are
		/// different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateParatextLdsFile_UpdateFontAndSizeAndAddRTL()
		{
			if (File.Exists("test.lds"))
				File.Delete("test.lds");

			int wsHvo = Cache.DefaultVernWs;
			LgWritingSystem vernWs = new LgWritingSystem(Cache, wsHvo);
			vernWs.Name.UserDefaultWritingSystem = "French";

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			StyleInfoTable styleTable = new StyleInfoTable("Normal",
				Cache.LanguageWritingSystemFactoryAccessor);
			styleTable.Add("Normal", normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			string ldsContentsOrig =
				"[General]" + Environment.NewLine +
				"codepage=65001" + Environment.NewLine +
				"DialogFontsize=0" + Environment.NewLine +
				"LowerCaseLetters=" + Environment.NewLine +
				"NoCaseLetters=" + Environment.NewLine +
				"UpperCaseLetters=" + Environment.NewLine +
				"errors=" + Environment.NewLine +
				"font=Arial" + Environment.NewLine +
				"name=French" + Environment.NewLine +
				"separator=" + Environment.NewLine +
				"size=32" + Environment.NewLine +
				Environment.NewLine +
				"[Checking]" + Environment.NewLine +
				Environment.NewLine +
				"[Characters]" + Environment.NewLine +
				Environment.NewLine +
				"[Punctuation]" + Environment.NewLine +
				"diacritics=" + Environment.NewLine +
				"medial=";

			DummyFileWriter writer = new DummyFileWriter();
			writer.Open("test.lds");
			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			ReflectionHelper.CallMethod(ldsAccessor, "UpdateLdsContents",
				ldsContentsOrig, normalEntry, Cache.DefaultVernWs, writer);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[General]",
				"codepage=65001",
				"DialogFontsize=0",
				"LowerCaseLetters=",
				"NoCaseLetters=",
				"UpperCaseLetters=",
				"errors=",
				"font=Wingdings",
				"name=French",
				"separator=",
				"size=14",
				"RTL=F",
				//Environment.NewLine, string.Empty
				"[Checking]",
				//Environment.NewLine, string.Empty
				"[Characters]",
				//Environment.NewLine, string.Empty
				"[Punctuation]",
				"diacritics=",
				"medial="
			};
		   ((DummyFileWriter)writer).VerifyOutput(expectedLdsContents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where the font and font size are
		/// different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateParatextLdsFile_CriticalStuffMissing()
		{
			if (File.Exists("test.lds"))
				File.Delete("test.lds");

			int wsHvo = Cache.DefaultVernWs;
			LgWritingSystem vernWs = new LgWritingSystem(Cache, wsHvo);
			vernWs.Name.UserDefaultWritingSystem = "French";

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			StyleInfoTable styleTable = new StyleInfoTable("Normal",
				Cache.LanguageWritingSystemFactoryAccessor);
			styleTable.Add("Normal", normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			string ldsContentsOrig =
				"[General]";

			DummyFileWriter writer = new DummyFileWriter();
			writer.Open("test.lds");
			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			ReflectionHelper.CallMethod(ldsAccessor, "UpdateLdsContents",
				ldsContentsOrig, normalEntry, Cache.DefaultVernWs, writer);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[General]",
				"codepage=65001",
				"font=Wingdings",
				"size=14",
				"name=French",
				"RTL=F"
			};

			// TODO: Fix use of writers. FileWriter cannot be cast successfully to DummyFileWriter and
			// DummyFileWriter cannot be passed to UpdateLdsContents.
			writer.VerifyOutput(expectedLdsContents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where the font and font size are
		/// different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateParatextLdsFile_GeneralSectionMissing()
		{
			if (File.Exists("test.lds"))
				File.Delete("test.lds");

			int wsHvo = Cache.DefaultVernWs;
			LgWritingSystem vernWs = new LgWritingSystem(Cache, wsHvo);
			vernWs.Name.UserDefaultWritingSystem = "French";

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			StyleInfoTable styleTable = new StyleInfoTable("Normal",
				Cache.LanguageWritingSystemFactoryAccessor);
			styleTable.Add("Normal", normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			string ldsContentsOrig =
				"[OtherStuff]";

			DummyFileWriter writer = new DummyFileWriter();
			writer.Open("test.lds");
			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			ReflectionHelper.CallMethod(ldsAccessor, "UpdateLdsContents",
				ldsContentsOrig, normalEntry, Cache.DefaultVernWs, writer);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[OtherStuff]",
				//Environment.NewLine +
				"[General]",
				"codepage=65001",
				"font=Wingdings",
				"size=14",
				"name=French",
				"RTL=F"
			};

			writer.VerifyOutput(expectedLdsContents);
		}
		#endregion
	}
}
