using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;
using SIL.WritingSystems.Tests;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// This test class adds some convenient methods for mocking an IWritingSystemContainer
	/// </summary>
	internal class TestWSContainer : IWritingSystemContainer
	{
		private IWritingSystemContainer _writingSystemContainerImplementation;

		private List<CoreWritingSystemDefinition> _vernacular =
			new List<CoreWritingSystemDefinition>();

		private List<CoreWritingSystemDefinition> _analysis =
			new List<CoreWritingSystemDefinition>();

		private List<CoreWritingSystemDefinition> _curVern =
			new List<CoreWritingSystemDefinition>();

		private List<CoreWritingSystemDefinition> _curAnaly =
			new List<CoreWritingSystemDefinition>();

		public TestWSContainer(string[] vernacular, string[] analysis = null,
			string[] curVern = null, string[] curAnaly = null)
		{
			foreach (var lang in vernacular)
			{
				var ws = new CoreWritingSystemDefinition(lang);
				_vernacular.Add(ws);
				if (curVern == null)
				{
					_curVern.Add(ws);
				}
			}

			if (analysis != null)
			{
				foreach (var lang in analysis)
				{
					var ws = new CoreWritingSystemDefinition(lang);
					_analysis.Add(ws);
					if (curAnaly == null)
					{
						_curAnaly.Add(ws);
					}
				}
			}

			if (curVern != null)
			{
				foreach (var lang in curVern)
				{
					_curVern.Add(new CoreWritingSystemDefinition(lang));
				}
			}

			if (curAnaly != null)
			{
				foreach (var lang in curAnaly)
				{
					_curAnaly.Add(new CoreWritingSystemDefinition(lang));
				}
			}

			Repo = new TestLdmlInXmlWritingSystemRepository();
		}

		public TestWSContainer(CoreWritingSystemDefinition[] vernacular)
		{
			_vernacular.AddRange(vernacular);
		}

		public void AddToCurrentAnalysisWritingSystems(CoreWritingSystemDefinition ws)
		{
			throw new System.NotImplementedException();
		}

		public void AddToCurrentVernacularWritingSystems(CoreWritingSystemDefinition ws)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<CoreWritingSystemDefinition> AllWritingSystems { get; }
		public ICollection<CoreWritingSystemDefinition> AnalysisWritingSystems => _analysis;

		public ICollection<CoreWritingSystemDefinition> VernacularWritingSystems => _vernacular;
		public IList<CoreWritingSystemDefinition> CurrentAnalysisWritingSystems => _curAnaly;
		public IList<CoreWritingSystemDefinition> CurrentVernacularWritingSystems => _curVern;
		public IList<CoreWritingSystemDefinition> CurrentPronunciationWritingSystems { get; }
		public CoreWritingSystemDefinition DefaultAnalysisWritingSystem { get; set; }
		public CoreWritingSystemDefinition DefaultVernacularWritingSystem { get; set; }
		public CoreWritingSystemDefinition DefaultPronunciationWritingSystem { get; }

		/// <summary>
		/// Test repo
		/// </summary>
		public IWritingSystemRepository Repo { get; set; }
	}
}