// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using FwBuildTasks;
using NUnit.Framework;
using SIL.TestUtilities;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class WxsToWxiTests
	{
		private TestBuildEngine _tbi;
		private WxsToWxi _task;

		private const string XmlHeader = @"<?xml version=""1.0"" encoding=""utf-8""?>";

		private const string WxsOpen = XmlHeader + @"<Wix xmlns = 'http://schemas.microsoft.com/wix/2006/wi'><Fragment>";

		private const string WxsClose = @"</Fragment></Wix>";

		private const string WxiOpen = XmlHeader + @"<Include>";

		private const string WxiClose = @"</Include>";

		private const string WxCore = @"
			<DirectoryRef Id='APPFOLDER'>
				<Component Id='cmp6272D87E84ED26C6466BC3114A55BB62' Guid='*'>
					<File Id='filD9B26B175A6E9020FDE0C8C0F3FD7A99' KeyPath='yes' Source='$(var.MASTERBUILDDIR)\Aga.Controls.dll' />
				</Component>
			</DirectoryRef>
			<ComponentGroup Id='HarvestedAppFiles'>
				<ComponentRef Id = 'cmp6272D87E84ED26C6466BC3114A55BB62' />
			</ComponentGroup>";


		[SetUp]
		public void TestSetup()
		{
			_tbi = new TestBuildEngine();
			_task = new WxsToWxi { BuildEngine = _tbi };
		}

		[TearDown]
		public void TestTeardown()
		{
			if (File.Exists(_task.SourceFile))
			{
				File.Delete(_task.SourceFile);
			}

			var destFile = Path.ChangeExtension(_task.SourceFile, "wxi");
			if (File.Exists(destFile))
			{
				// ReSharper disable AssignNullToNotNullAttribute -- Justification: if the path is null, File.Exists should return false.
				File.Delete(destFile);
				// ReSharper restore AssignNullToNotNullAttribute
			}
		}

		[Test]
		public void Works()
		{
			var wxsFile = Path.GetTempFileName();
			File.WriteAllText(wxsFile, WxsOpen + WxCore + WxsClose);
			_task.SourceFile = wxsFile;

			// SUT
			_task.Execute();

			Assert.IsEmpty(_tbi.Errors);
			Assert.IsEmpty(_tbi.Warnings);
			Assert.IsEmpty(_tbi.Messages);
			var wxiFile = Path.ChangeExtension(wxsFile, "wxi");
			AssertThatXmlIn.String(WxiOpen + WxCore + WxiClose).EqualsIgnoreWhitespace(File.ReadAllText(wxiFile));
		}

		[Test]
		public void NoWixElt_LogsError()
		{
			_task.SourceFile = Path.GetTempFileName();
			File.WriteAllText(_task.SourceFile, WxiOpen + WxCore + WxiClose);

			// SUT
			_task.Execute();

			Assert.IsNotEmpty(_tbi.Errors);
			StringAssert.Contains("No <Wix> element", _tbi.Errors[0]);
		}
	}
}
