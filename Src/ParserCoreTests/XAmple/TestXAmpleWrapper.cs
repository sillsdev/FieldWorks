// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.WordWorks.Parser.XAmple
{
	[TestFixture]
	public class TestXAmpleWrapper
	{
		private static XAmpleWrapper InitHelper()
		{
			var xAmple = new XAmpleWrapper();
			xAmple.Init();
			return xAmple;
		}

		private static void LoadFilesHelper(IXAmpleWrapper wrapper)
		{
			var tempPath = Path.Combine(FwDirectoryFinder.SourceDirectory, "ParserCoreTests", "XAmple", "M3ToXAmpleTransformerTestsDataFiles");
			var xPath = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Configuration", "Grammar");
			wrapper.LoadFiles(xPath, tempPath, "StemName3");
		}

		[Test]
		public void TestInit()
		{
			using (var wrapper = InitHelper())
<<<<<<< HEAD:Src/ParserCoreTests/XAmple/TestXAmpleWrapper.cs
			{
				Assert.IsNotNull(wrapper);
			}
||||||| f013144d5:Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/TestXAmpleWrapper.cs
				Assert.IsNotNull(wrapper);
=======
				Assert.That(wrapper, Is.Not.Null);
>>>>>>> develop:Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/TestXAmpleWrapper.cs
		}

		[Test]
		public void TestParseWord()
		{
			using (var xAmple = InitHelper())
			{
				LoadFilesHelper(xAmple);
<<<<<<< HEAD:Src/ParserCoreTests/XAmple/TestXAmpleWrapper.cs
				var parsedWord = xAmple.ParseWord("Hello");
				Assert.IsNotNull(parsedWord);
||||||| f013144d5:Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/TestXAmpleWrapper.cs
				string parsedWord = xAmple.ParseWord("Hello");
				Assert.IsNotNull(parsedWord);
=======
				string parsedWord = xAmple.ParseWord("Hello");
				Assert.That(parsedWord, Is.Not.Null);
>>>>>>> develop:Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/TestXAmpleWrapper.cs
				Assert.IsNotEmpty(parsedWord);
			}
		}

		[Test]
		public void TestTraceWord()
		{
			using (var xAmple = InitHelper())
			{
				LoadFilesHelper(xAmple);
<<<<<<< HEAD:Src/ParserCoreTests/XAmple/TestXAmpleWrapper.cs
				var tracedWord = xAmple.TraceWord("Hello", "Hello");
				Assert.IsNotNull(tracedWord);
||||||| f013144d5:Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/TestXAmpleWrapper.cs
				string tracedWord = xAmple.TraceWord("Hello", "Hello");
				Assert.IsNotNull(tracedWord);
=======
				string tracedWord = xAmple.TraceWord("Hello", "Hello");
				Assert.That(tracedWord, Is.Not.Null);
>>>>>>> develop:Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/TestXAmpleWrapper.cs
				Assert.IsNotEmpty(tracedWord);
			}
		}

		[Test]
		public void TestLoadFiles()
		{
			using (var xAmple = InitHelper())
				LoadFilesHelper(xAmple);
		}

		[Test]
		public void TestSetParameter()
		{
			using (var xAmple = InitHelper())
			{
				xAmple.SetParameter("MaxAnalysesToReturn", "3");
			}
		}

		[Test]
		[Platform(Include = "Win")]
		public void TestAmpleThreadId_Windows()
		{
			using (var xAmple = InitHelper())
			{
				Assert.That(xAmple.AmpleThreadId, Is.Not.EqualTo(0));
			}
		}

		[Test]
		[Platform(Exclude = "Win")]
		public void TestAmpleThreadId_Linux()
		{
			using (var xAmple = InitHelper())
			{
				Assert.That(xAmple.AmpleThreadId, Is.EqualTo(0));
			}
		}
	}
}
