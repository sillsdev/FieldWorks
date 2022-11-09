// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using FwBuildTasks;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace SIL.FieldWorks.Build.Tasks
{
	[TestFixture]
	public class MakeTests
	{
		private class MakeSubclass : Make
		{
			public string GetCommandLine()
			{
				return GenerateCommandLineCommands();
			}
		}

		private TestBuildEngine _tbi;

		[SetUp]
		public void TestSetup()
		{
			_tbi = new TestBuildEngine();
		}

		[Test]
		[Platform(Exclude = "Linux", Reason = "Windows specific test")]
		public void CommandLine_NoAdditionalMacros_Windows()
		{
			var sut = new MakeSubclass
			{
				BuildEngine = _tbi,
				Makefile = "MyMakefile"
			};

			Assert.That(sut.GetCommandLine(), Is.EqualTo("/nologo BUILD_TYPE=d /f MyMakefile"));
		}

		[Test]
		[Platform(Include = "Linux", Reason = "Linux specific test")]
		public void CommandLine_NoAdditionalMacros_Linux()
		{
			var sut = new MakeSubclass
			{
				BuildEngine = _tbi,
				Makefile = "MyMakefile"
			};

			Assert.That(sut.GetCommandLine(), Is.EqualTo("BUILD_TYPE=d -f MyMakefile all"));
		}

		[Test]
		[Platform(Exclude = "Linux", Reason = "Windows specific test")]
		public void CommandLine_AdditionalMacros_Windows()
		{
			var sut = new MakeSubclass
			{
				BuildEngine = _tbi,
				Makefile = "MyMakefile",
				Macros = "A=B C=D"
			};

			Assert.That(sut.GetCommandLine(), Is.EqualTo("/nologo BUILD_TYPE=d A=B C=D /f MyMakefile"));
		}

		[Test]
		[Platform(Include = "Linux", Reason = "Linux specific test")]
		public void CommandLine_AdditionalMacros_Linux()
		{
			var sut = new MakeSubclass
			{
				BuildEngine = _tbi,
				Makefile = "MyMakefile",
				Macros = "A=B C=D"
			};

			Assert.That(sut.GetCommandLine(), Is.EqualTo("BUILD_TYPE=d A=B C=D -f MyMakefile all"));
		}

	}
}