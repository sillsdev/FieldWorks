// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;
using SIL.WritingSystems.Tests;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	public class AdvancedScriptRegionVariantModelTests
	{
		/// <summary>Test that we can set the region to a pre-loaded Private Use region</summary>
		[Test]
		public void SetRegionToQMFromEmptyWorks()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			var regions = model.GetRegions();
			model.Region = regions.First(s => s.Code == "QM");
			Assert.That(model.RegionCode, Is.EqualTo("QM"));
			Assert.That(model.Region.Code, Is.EqualTo("QM"));
			Assert.That(model.Region.IsPrivateUse);
		}

		/// <summary>Test that we can set the region to a not-yet-loaded Private Use region</summary>
		[Test]
		public void SetRegionToQRFromEmptyWorks()
		{
			const string qr = "QR";
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.GetRegions().Any(r => r.Code == qr), Is.False, "Most private use regions should not be preloaded");
			model.Region = new AdvancedScriptRegionVariantModel.RegionListItem(new RegionSubtag(qr, "Private Use"));
			Assert.That(model.RegionCode, Is.EqualTo(qr));
			Assert.That(model.Region.Code, Is.EqualTo(qr));
			Assert.That(model.Region.IsPrivateUse);
		}

		/// <summary>Test that we can set the script to a pre-loaded Private Use script</summary>
		[Test]
		public void SetScriptToQaaaFromEmptyWorks()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			var scripts = model.GetScripts();
			model.Script = scripts.First(s => s.Code == "Qaaa");
			Assert.That(model.ScriptCode, Is.EqualTo("Qaaa"));
			Assert.That(model.Script.Code, Is.EqualTo("Qaaa"));
			Assert.That(model.Script.IsPrivateUse);
		}

		/// <summary>Test that we can set the script to a not-yet-loaded Private Use script</summary>
		[Test]
		public void SetScriptToQaadFromEmptyWorks()
		{
			const string qaad = "Qaad";
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.GetScripts().Any(s => s.Code == qaad), Is.False, "Most private use scripts should not be preloaded");
			model.Script = new AdvancedScriptRegionVariantModel.ScriptListItem(new ScriptSubtag(qaad, "Private Use"));
			Assert.That(model.ScriptCode, Is.EqualTo(qaad));
			Assert.That(model.Script.Code, Is.EqualTo(qaad));
			Assert.That(model.Script.IsPrivateUse);
		}

		/// <summary/>
		[Test]
		public void SetCodeToLangOnlyReturnsEmptyScriptRegionVariant()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa-Qaaa-QM-fonipa-x-Scrp-ST-extra" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				Code = "qaa"
			};
			Assert.That(model.ScriptCode, Is.Null);
			Assert.That(model.ScriptName, Is.Null);
			Assert.That(model.Script.Code, Is.Null);
			Assert.That(model.RegionCode, Is.Null);
			Assert.That(model.RegionName, Is.Null);
			Assert.That(model.Region.Code, Is.Null);
			Assert.That(model.StandardVariant, Is.Null);
			Assert.That(model.OtherVariants, Is.Empty);
		}

		/// <summary/>
		[Test]
		public void CodeSetToFullSILCustomFillsInAllData()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				Code = "qaa-Qaaa-QM-fonipa-x-kal-Kala-KA-extra1-extra2"
			};
			Assert.That(model.Script.Code, Is.EqualTo("Kala"));
			Assert.That(model.Script.IsPrivateUse, Is.True);
			Assert.That(model.ScriptCode, Is.EqualTo("Kala"));
			Assert.That(model.Region.Code, Is.EqualTo("KA"));
			Assert.That(model.Region.IsPrivateUse, Is.True);
			Assert.That(model.RegionCode, Is.EqualTo("KA"));
			Assert.That(model.StandardVariant, Is.EqualTo("fonipa"));
			Assert.That(model.OtherVariants, Is.EqualTo("x-extra1-extra2"));
		}

		/// <summary/>
		[TestCase("qaa-Qaaa-QM-fonipa-x-kal-Kala-KA-extra1-extra2", true)]
		[TestCase("fr-Qaaa", false)]
		[TestCase("qa-Qaa", false)]
		public void ValidateIetfCode_ReturnsExpected(string newCode, bool expectedValue)
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.ValidateIetfCode(newCode), Is.EqualTo(expectedValue));
		}

		/// <summary/>
		[Test]
		public void CodeSetToFullNonSILCustomFillsInAllData()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				Code = "qaa-Qaax-QX-fonipa-x-kal-Kala-KA-extra1-extra2"
			};
			Assert.That(model.ScriptCode, Is.EqualTo("Qaax")); // Not Qaaa so the Kala is just a custom variant
			Assert.That(model.RegionCode, Is.EqualTo("QX")); // Not QM so KA is just a custom variant
			Assert.That(model.StandardVariant, Is.EqualTo("fonipa"));
			Assert.That(model.OtherVariants, Is.EqualTo("x-Kala-KA-extra1-extra2"));
		}

		/// <summary>
		/// Proves that you can remove the custom script and region codes by changing the 'Code' field
		/// </summary>
		[Test]
		public void CodeRemovingPUCodesWorks()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-Qaaa-QM-x-Cust-CT" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				RegionName = "CustomRegion"
			};
			Assert.That(model.RegionCode, Is.EqualTo("CT"));
			Assert.That(model.ScriptCode, Is.EqualTo("Cust"));
			model.Code = "fr-Qaaa-QM";
			Assert.That(model.ScriptCode, Is.EqualTo("Qaaa"), "Private use script code was not removed.");
			Assert.That(model.RegionCode, Is.EqualTo("QM"), "Private use region code was not removed.");
		}

		/// <summary/>
		[Test]
		public void SetScriptUpdatesCodeAndScriptCode()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				Script = new ScriptListItem(StandardSubtags.RegisteredScripts.First(s => s.Code == "Arab"))
			};
			Assert.That(model.ScriptCode, Is.EqualTo("Arab"));
			Assert.That(model.Code, Is.EqualTo("fr-Arab"));
		}

		/// <summary/>
		[Test]
		public void SetRegionUpdatesCodeAndRegionCode()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-x-special" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				Region = new RegionListItem(StandardSubtags.RegisteredRegions.First(s => s.Code == "US"))
			};
			Assert.That(model.RegionCode, Is.EqualTo("US"));
			Assert.That(model.Code, Is.EqualTo("fr-US-x-special"));
		}

		/// <summary/>
		[Test]
		public void SetRegionCodeDoesNotBlankOutRegion()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-Qaaa-QM-x-Cust-CT" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				RegionName = "CustReg"
			};
			model.Region = model.GetRegions().First(r => r.Name == "CustReg");
			Assert.That(model.Region.Label, Is.EqualTo("CustReg (CT)"));
			model.RegionCode = "CM";
			Assert.That(model.Region.Label, Is.EqualTo("CustReg (CM)"));
		}

		/// <summary/>
		[Test]
		public void SetScriptCodeDoesNotBlankOutScript()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-Qaaa-QM-x-Cust-CT" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				ScriptName = "CustScr"
			};
			model.Script = model.GetScripts().First(r => r.Name == "CustScr");
			Assert.That(model.Script.Label, Is.EqualTo("CustScr (Cust)"));
			model.ScriptCode = "Crud";
			Assert.That(model.Script.Label, Is.EqualTo("CustScr (Crud)"));
		}

		/// <summary/>
		[Test]
		public void GetRegionNameDoesNotCrashOnEmptyRegion()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-x-special" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.RegionName, Is.Null);
		}

		/// <summary/>
		[Test]
		public void GetScriptNameDoesNotCrashOnEmptyScript()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa-x-special" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.ScriptName, Is.Null);
		}

		/// <summary/>
		[Test]
		public void EnableRegionCodeDoesNotCrashOnEmptyRegion()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-x-special" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.Region.Code, Is.Null);
			Assert.That(model.EnableRegionCode, Is.False);
		}

		/// <summary/>
		[Test]
		public void EnableScriptCodeDoesNotCrashOnEmptyScript()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa-x-special" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.EnableScriptCode, Is.False);
		}

		/// <summary/>
		[Test]
		public void SetCustomRegionCodeToRealCodeDoesNotLoseCustomStatus()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-QM-x-CT" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				RegionName = "South Texas", RegionCode = "CM"
			};
			Assert.That(model.RegionName, Is.EqualTo("South Texas"));
			Assert.That(model.Code, Is.EqualTo("fr-QM-x-CM"));
		}

		/// <summary/>
		[Test]
		public void SetScriptToQaaaDoesNotLoseCustomCodeOrName()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-Qaaa-x-Cust" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				ScriptName = "SouthernDrawn"
			};
			model.Script = model.GetScripts().First(r => r.Code == "Qaaa");
			Assert.That(model.ScriptCode, Is.EqualTo("Cust"));
			Assert.That(model.ScriptName, Is.EqualTo("SouthernDrawn"));
			Assert.That(model.Code, Is.EqualTo("fr-Qaaa-x-Cust"));
		}

		/// <summary/>
		[Test]
		public void SetCustomScriptCodeDoesNotLoseCustomName()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa-Qaaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.ScriptCode, Is.EqualTo("Qaaa"));
			model.ScriptName = "SuperScript";
			Assert.That(model.ScriptName, Is.EqualTo("SuperScript"));
			model.ScriptCode = "Cust";
			Assert.That(model.ScriptName, Is.EqualTo("SuperScript"));
		}

		/// <summary/>
		[Test]
		public void SetScriptNameUpdatesModel()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa-Qaaa-x-kal-Kala" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				ScriptName = "Kalabanolized"
			};
			Assert.That(model.ScriptCode, Is.EqualTo("Kala"));
			Assert.That(fwWsModel.CurrentWsSetupModel.CurrentIso15924Script.Name, Is.EqualTo("Kalabanolized"));
		}

		/// <summary/>
		[Test]
		public void GetStandardVariants_ListsBasics()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			CollectionAssert.AreEquivalent(new[]
				{
					"None", "ALA-LC Romanization, 1997 edition", "International Phonetic Alphabet",
					"Kirshenbaum Phonetic Alphabet", "North American Phonetic Alphabet",
					"Simplified form", "Uralic Phonetic Alphabet", "X-SAMPA transcription"
				},
				model.GetStandardVariants().Select(v => v.Name));
		}

		/// <summary/>
		[Test]
		public void GetStandardVariants_ListsLanguageSpecific()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			CollectionAssert.Contains(model.GetStandardVariants().Select(v => v.Name), "Early Modern French");
		}

		/// <summary/>
		[Test]
		public void GetStandardVariants_ListsLanguageSpecificWithVariants()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr-x-extra" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			CollectionAssert.Contains(model.GetStandardVariants().Select(v => v.Name), "Early Modern French");
		}

		/// <summary/>
		[Test]
		public void GetScriptList_ContainsQaaa()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.IsTrue(model.GetScripts().Any(s => s.IsPrivateUse && s.Code == "Qaaa"));
		}

		/// <summary/>
		[Test]
		public void GetRegionList_ContainsQM()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			var regions = model.GetRegions();
			Assert.IsTrue(regions.Any(r => r.IsPrivateUse && r.Code == "QM"));
		}

		/// <summary/>
		[TestCase("en-Qaaa", true)]
		[TestCase("en-Qaaa-x-Kala", true)]
		[TestCase("en-Qaax", false)]
		[TestCase("en-Qaax-x-Kala", false)]
		[TestCase("en-Latn", false)]
		public void IsScriptCodeEnabled(string languageCode, bool shouldBeEnabled)
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { languageCode }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.EnableScriptCode, Is.EqualTo(shouldBeEnabled));
		}

		/// <summary/>
		[TestCase("en-QM", true)]
		[TestCase("en-QM-x-KA", true)]
		[TestCase("en-QZ", false)]
		[TestCase("en-QQ-x-Kala", false)]
		[TestCase("en-US", false)]
		public void IsRegionCodeEnabled(string languageCode, bool shouldBeEnabled)
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { languageCode }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.EnableRegionCode, Is.EqualTo(shouldBeEnabled));
		}

		/// <summary/>
		[Test]
		public void RegionListItem_Equals()
		{
			var regionOne = new RegionListItem(new RegionSubtag("Qaaa"));
			var regionTwo = new RegionListItem(new RegionSubtag("Qaaa"));
			Assert.AreEqual(regionOne, regionTwo);
		}

		/// <summary/>
		[Test]
		public void RegionListItem_NotEquals()
		{
			var regionOne = new RegionListItem(new RegionSubtag("Qaaa"));
			var regionTwo = new RegionListItem(new RegionSubtag("Qaaa", "Booga"));
			Assert.AreNotEqual(regionOne, regionTwo);
		}

		/// <summary/>
		[Test]
		public void ScriptItemChange_UpdatesCode()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "qaa-Qaaa-x-Kalaba-Kala-extra", "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				Script = new ScriptListItem(new ScriptSubtag("Mala", "not Kala"))
			};
			Assert.That(model.Code, Is.EqualTo("qaa-Qaaa-x-Kalaba-Mala-extra"));
		}

		/// <summary/>
		[Test]
		public void StandardVariantChange_UpdatesCode()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "de-DE", "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				StandardVariant = "fonipa"
			};
			Assert.That(model.Code, Is.EqualTo("de-DE-fonipa"));
		}

		/// <summary/>
		[Test]
		public void StandardVariantChange_SetToNull_Removes_AndUpdatesCode()
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "de-DE-fonipa", "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				StandardVariant = null
			};
			Assert.That(model.Code, Is.EqualTo("de-DE"));
		}

		/// <summary/>
		[Test]
		public void OtherVariantChange_UpdatesCode_StandardVariantRemains()
		{
			// Including fonipa sets the StandardVariant
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "de-DE-fonipa", "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				OtherVariants = "x-special"
			};
			Assert.That(model.Code, Is.EqualTo("de-DE-fonipa-x-special"));
		}

		/// <summary/>
		[Test]
		public void OtherVariantChange_UpdatesCode()
		{
			// Including fonipa sets the StandardVariant
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "de-DE", "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel)
			{
				OtherVariants = "x-special"
			};
			Assert.That(model.Code, Is.EqualTo("de-DE-x-special"));
		}

		/// <summary/>
		[TestCase("fonipa", true)]
		[TestCase("fonipa-simple", true)]
		[TestCase("x-special", true)]
		[TestCase("totaljunk", false)]
		// [TestCase("x-privateusewaytoolong", false)] // libpalaso fix needs to happen
		public void ValidateOtherVariantsWorks(string code, bool expectedResult)
		{
			var fwWsModel = new FwWritingSystemSetupModel(new TestWSContainer(new[] { "fr" }, new[] { "en" }), FwWritingSystemSetupModel.ListType.Vernacular);
			var model = new AdvancedScriptRegionVariantModel(fwWsModel);
			Assert.That(model.ValidateOtherVariants(code), Is.EqualTo(expectedResult));
		}

		/// <summary>
		/// This test class adds some convenient methods for mocking an IWritingSystemContainer
		/// </summary>
		private sealed class TestWSContainer : IWritingSystemContainer
		{
			private IWritingSystemContainer _writingSystemContainerImplementation;
			private List<CoreWritingSystemDefinition> _vernacular = new List<CoreWritingSystemDefinition>();
			private List<CoreWritingSystemDefinition> _analysis = new List<CoreWritingSystemDefinition>();
			private List<CoreWritingSystemDefinition> _curVern = new List<CoreWritingSystemDefinition>();
			private List<CoreWritingSystemDefinition> _curAnaly = new List<CoreWritingSystemDefinition>();

			internal TestWSContainer(string[] vernacular, string[] analysis = null, string[] curVern = null, string[] curAnaly = null)
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

			public void AddToCurrentAnalysisWritingSystems(CoreWritingSystemDefinition ws)
			{
				throw new System.NotSupportedException();
			}

			public void AddToCurrentVernacularWritingSystems(CoreWritingSystemDefinition ws)
			{
				throw new System.NotSupportedException();
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
			private IWritingSystemRepository Repo { get; }
		}
	}
}
