// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using XAmpleManagedWrapper;

namespace XAmpleManagedWrapperTests
{
	[TestFixture]
	public class TestXAmpleDLLWrapper
	{
		protected XAmpleDLLWrapper CreateXAmpleDllWrapper()
		{
			var xAmple = new XAmpleDLLWrapper();
			xAmple.Init();
			return xAmple;
		}

		protected void LoadFilesHelper(XAmpleDLLWrapper wrapper)
		{
			string tempPath = "../../Src/LexText/ParserCore/ParserCoreTests/M3ToXAmpleTransformerTestsDataFiles";
			// TODO: use DirectoryFinder.FWCodeDirectory
			string xPath = "../../DistFiles/" + "/Language Explorer/Configuration/Grammar";
			wrapper.LoadFiles(xPath, tempPath, "StemName3");
		}

		[Test]
		public void TestInit()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
				Assert.That(wrapper, Is.Not.Null);
		}

		[Test]
		public void TestLoadFiles()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
				LoadFilesHelper(wrapper);
		}

		[Test]
		public void TestSetParameter()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
				wrapper.SetParameter("MaxAnalysesToReturn", "3");
		}

		[Test]
		public void TestGetSetup()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
				Assert.That(wrapper.GetSetup(), Is.Not.EqualTo(IntPtr.Zero));
		}

		[Test]
		public void TestSetLogFile()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
				Assert.That(() => wrapper.SetLogFile(Path.GetTempFileName()), Throws.TypeOf<NotImplementedException>());
		}

		[Test]
		[Platform(Include = "Win")]
		public void GetAmpleThreadId_Windows()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				int threadId = wrapper.GetAmpleThreadId();
				Assert.That(threadId, Is.Not.EqualTo(0));
			}
		}

		[Test]
		[Platform(Exclude = "Win")]
		public void GetAmpleThreadId_Linux()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				int threadId = wrapper.GetAmpleThreadId();
				Assert.That(threadId, Is.EqualTo(0));
			}
		}

		[Test]
		public void TestParseString()
		{

			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				LoadFilesHelper(wrapper);
				string parsedString = wrapper.ParseString("Hello");
				Assert.That(parsedString, Is.Not.Empty);
				Assert.That(parsedString, Is.Not.Null);
			}
		}

		[Test]
		public void TestTraceString()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				LoadFilesHelper(wrapper);
				string tracedString = wrapper.TraceString("Hello", "Hello");
				Assert.That(tracedString, Is.Not.Empty);
				Assert.That(tracedString, Is.Not.Null);
			}
		}

		[Test]
		public void TestDisposeBeforeInit()
		{
			Assert.That(() =>
			{
				using (var xAmpleDllWrapper = new XAmpleDLLWrapper())
				{
					// prove that disposing the uninitialized wrapper does not throw
				}
			}, Throws.Nothing);
		}
	}
}
