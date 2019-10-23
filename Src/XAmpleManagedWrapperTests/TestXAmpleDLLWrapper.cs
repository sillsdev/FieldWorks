// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
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
			string tempPath = Path.Combine(FwDirectoryFinder.SourceDirectory, "ParserCoreTests", "M3ToXAmpleTransformerTestsDataFiles");
			string xPath = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Configuration", "Grammar");
			wrapper.LoadFiles(xPath, tempPath, "StemName3");
		}

		[Test]
		public void TestInit()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
				Assert.IsNotNull(wrapper);
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
				Assert.AreNotEqual(IntPtr.Zero, wrapper.GetSetup());
		}

		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void TestSetLogFile()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
				wrapper.SetLogFile(Path.GetTempFileName());
		}

		[Test]
		[Platform(Include = "Win")]
		public void GetAmpleThreadId_Windows()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				int threadId = wrapper.GetAmpleThreadId();
				Assert.AreNotEqual(0, threadId);
			}
		}

		[Test]
		[Platform(Exclude = "Win")]
		public void GetAmpleThreadId_Linux()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				int threadId = wrapper.GetAmpleThreadId();
				Assert.AreEqual(0, threadId);
			}
		}

		[Test]
		public void TestParseString()
		{

			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				LoadFilesHelper(wrapper);
				string parsedString = wrapper.ParseString("Hello");
				Assert.IsNotEmpty(parsedString);
				Assert.IsNotNull(parsedString);
			}
		}

		[Test]
		public void TestTraceString()
		{
			using (XAmpleDLLWrapper wrapper = CreateXAmpleDllWrapper())
			{
				LoadFilesHelper(wrapper);
				string tracedString = wrapper.TraceString("Hello", "Hello");
				Assert.IsNotEmpty(tracedString);
				Assert.IsNotNull(tracedString);
			}
		}

		[Test]
		public void TestDisposeBeforeInit()
		{
			Assert.DoesNotThrow(() =>
			{
				using (var xAmpleDllWrapper = new XAmpleDLLWrapper())
				{
					// prove that disposing the uninitialized wrapper does not throw
				}
			});
		}
	}
}
