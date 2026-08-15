// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class FlexTextAIExportListenerTests : InterlinearTestBase
	{
		[Test]
		public void ExportTextsAsFlexText_OneText_WritesOneFlexTextFileAndMarksHandled()
		{
			// Already runs inside an ambient undo task here, so this must not open its
			// own -- LCM disallows nested undo tasks.
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.Texts.Add(text);
			text.ContentsOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.Name.SetAnalysisDefaultWritingSystem("Listener Test Text");
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ContentsOA.ParagraphsOS.Add(para);
			para.Contents = TsStringUtils.MakeString("hello world", Cache.DefaultVernWs);

			var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempFolder);
			try
			{
				var listener = new FlexTextAIExportListener();
				var request = new ExportTextsAsFlexTextRequest(new[] { text.ContentsOA }, tempFolder);

				listener.ExportTextsAsFlexTextForTests(Cache, request);

				Assert.That(request.Handled, Is.True);
				Assert.That(request.Failures, Is.Empty);
				Assert.That(File.Exists(Path.Combine(tempFolder, "Listener Test Text.flextext")), Is.True);
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}
		}
	}
}
