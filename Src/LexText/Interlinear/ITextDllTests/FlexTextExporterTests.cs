// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class FlexTextExporterTests : InterlinearTestBase
	{
		[Test]
		public void ExportTexts_OneText_WritesOneFlexTextFile()
		{
			var text = MakeOneParagraphText("Exporter Test Text");
			var tempFolder = MakeTempFolder();
			try
			{
				var messages = new List<string>();

				FlexTextExporter.ExportTexts(Cache, new[] { text }, tempFolder,
					new[] { "HCGrammar" }, messages);

				Assert.That(messages, Is.Empty);
				Assert.That(File.Exists(Path.Combine(tempFolder, "Exporter Test Text.flextext")), Is.True);
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}
		}

		[Test]
		public void ExportTexts_TextNamedLikeTheGrammarFile_DoesNotOverwriteIt()
		{
			var text = MakeOneParagraphText("HCGrammar");
			var tempFolder = MakeTempFolder();
			try
			{
				var messages = new List<string>();

				FlexTextExporter.ExportTexts(Cache, new[] { text }, tempFolder,
					new[] { "HCGrammar" }, messages);

				Assert.That(messages, Is.Empty);
				Assert.That(File.Exists(Path.Combine(tempFolder, "HCGrammar.flextext")), Is.False);
				Assert.That(File.Exists(Path.Combine(tempFolder, "HCGrammar (2).flextext")), Is.True);
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}
		}

		[Test]
		public void ExportTexts_OnAThreadPoolThread_WritesTheFileWithoutThrowing()
		{
			// The real export runs inside a ProgressDialogWithTask BackgroundWorker, i.e. on an
			// MTA thread-pool thread rather than the STA UI thread. InterlinVc is built here, so
			// anything in it that needs an apartment or a UI-thread handle fails there and only
			// there.
			var text = MakeOneParagraphText("Background Thread Text");
			var tempFolder = MakeTempFolder();
			try
			{
				var messages = new List<string>();
				ApartmentState apartment = ApartmentState.Unknown;
				Exception thrown = null;
				var finished = new ManualResetEventSlim();
				ThreadPool.QueueUserWorkItem(_ =>
				{
					try
					{
						apartment = Thread.CurrentThread.GetApartmentState();
						FlexTextExporter.ExportTexts(Cache, new[] { text }, tempFolder,
							new[] { "HCGrammar" }, messages);
					}
					catch (Exception e)
					{
						thrown = e;
					}
					finally
					{
						finished.Set();
					}
				});
				Assert.That(finished.Wait(TimeSpan.FromMinutes(1)), Is.True, "export thread did not finish");

				Assert.That(apartment, Is.EqualTo(ApartmentState.MTA), "expected the thread pool to be MTA");
				Assert.That(thrown, Is.Null, thrown?.ToString());
				Assert.That(messages, Is.Empty, string.Join("; ", messages));
				Assert.That(File.Exists(Path.Combine(tempFolder, "Background Thread Text.flextext")), Is.True);
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}
		}

		/// <remarks>
		/// Already runs inside an ambient undo task here, so this must not open its own --
		/// LCM disallows nested undo tasks.
		/// </remarks>
		private IStText MakeOneParagraphText(string name)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.Texts.Add(text);
			text.ContentsOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.Name.SetAnalysisDefaultWritingSystem(name);
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ContentsOA.ParagraphsOS.Add(para);
			para.Contents = TsStringUtils.MakeString("hello world", Cache.DefaultVernWs);
			return text.ContentsOA;
		}

		private static string MakeTempFolder()
		{
			var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempFolder);
			return tempFolder;
		}
	}
}
