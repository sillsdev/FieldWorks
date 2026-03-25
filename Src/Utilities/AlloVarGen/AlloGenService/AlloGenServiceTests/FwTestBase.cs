// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.AlloGenModel;
using SIL.AlloGenService;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenServiceTests
{
	public class FwTestBase : MemoryOnlyBackendProviderTestBase
	{
		protected string TestDataDir { get; set; }
		protected string FieldWorksTestFile { get; set; }
		protected LcmCache myCache { get; set; }

		// Following three needed to get cache
		protected ILcmUI m_ui;
		protected string m_projectsDirectory;
		protected ILcmDirectories m_lcmDirectories;

		protected XmlBackEndProvider provider = new XmlBackEndProvider();
		protected string AlloGenExpected { get; set; }
		protected AllomorphGenerators allomorphGenerators;
		protected Operation operation;
		protected Pattern pattern { get; set; }
		protected PatternMatcher patternMatcher { get; set; }
		protected List<WritingSystem> writingSystems = new List<WritingSystem>();

		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
				Sldr.Initialize();
			base.FixtureSetup();
			m_projectsDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(m_projectsDirectory);
			m_ui = new DummyLcmUI();
			m_lcmDirectories = new TestLcmDirectories(m_projectsDirectory);
			CreateWritingSystemList();
		}

		protected void CreateWritingSystemList()
		{
			var styles = Cache.LangProject.StylesOC.ToDictionary(style => style.Name);
			IStStyle normal = Cache.LangProject.StylesOC.FirstOrDefault(
				style => style.Name == "Normal"
			);
			if (normal != null)
			{
				SIL.FieldWorks.FwCoreDlgControls.StyleInfo styleInfo =
					new SIL.FieldWorks.FwCoreDlgControls.StyleInfo(normal);
				IList<CoreWritingSystemDefinition> vernWses = Cache
					.LangProject
					.CurrentVernacularWritingSystems;
				foreach (CoreWritingSystemDefinition def in vernWses)
				{
					float fontSize = Math.Max(def.DefaultFontSize, 10);
					WritingSystem ws = new WritingSystem();
					ws.Handle = def.Handle;
					ws.Font = new Font(def.DefaultFontName, fontSize);
					ws.FontInfo = styleInfo.FontInfoForWs(def.Handle);
					ws.Color = ws.FontInfo.FontColor.Value;
					writingSystems.Add(ws);
				}
			}
		}

		[SetUp]
		virtual public void Setup()
		{
			TestDataDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "AlloVarGen", "AlloGenService", "AlloGenServiceTests", "TestData");
			FieldWorksTestFile = Path.Combine(TestDataDir, "Quechua MYL CausDeriv.fwdata");
			AlloGenExpected = Path.Combine(TestDataDir, "AlloGenExpected.xml");

			var projectId = new TestProjectId(BackendProviderType.kXML, FieldWorksTestFile);
			// following came from LcmCacheTests.cs
			myCache = LcmCache.CreateCacheFromExistingData(
				projectId,
				"en",
				m_ui,
				m_lcmDirectories,
				new LcmSettings(),
				new DummyProgressDlg()
			);

			provider.LoadDataFromFile(AlloGenExpected);
			allomorphGenerators = provider.AlloGens;
			operation = allomorphGenerators.Operations[0];
			pattern = operation.Pattern;
			patternMatcher = new PatternMatcher(myCache, allomorphGenerators);
		}
	}
}
