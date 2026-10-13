// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>Checks that trace callbacks preserve supplied failure facts.</summary>
	[TestFixture]
	public class FwXmlTraceManagerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FwXmlTraceManager m_manager;
		private Stratum m_stratum;
		private Word m_word;
		private XElement m_trace;

		/// <summary>Creates a synthetic word and an empty trace for each test.</summary>
		[SetUp]
		public void SetUpTrace()
		{
			var table = new CharacterDefinitionTable();
			table.AddSegment("a");
			m_stratum = new Stratum(table) { Name = "synthetic" };
			m_trace = new XElement("WordSynthesisTrace");
			m_word = new Word(m_stratum, table.Segment("a")) { CurrentTrace = m_trace };
			m_manager = new FwXmlTraceManager(Cache);
		}

		/// <summary>Preserves each distinct affix environment, including circumfix
		/// halves.</summary>
		[TestCase(null, "/ p _", null)]
		[TestCase(null, null, "/ _ s")]
		[TestCase("/ e _", "/ p _", "/ _ s")]
		public void PatternFailure_PreservesEachEnvironment(string env, string prefix, string suffix)
		{
			var rule = new AffixProcessRule { Name = "synthetic affix" };
			var allomorph = new AffixProcessAllomorph();
			rule.Allomorphs.Add(allomorph);
			allomorph.Properties[HCParser.Env] = env;
			allomorph.Properties[HCParser.PrefixEnv] = prefix;
			allomorph.Properties[HCParser.SuffixEnv] = suffix;

			m_manager.MorphologicalRuleNotApplied(rule, 0, m_word, FailureReason.Pattern, null);

			Assert.That(m_trace.Descendants("Environment").Select(e => e.Value),
				Is.EqualTo(new[] { env, prefix, suffix }.Where(e => e != null)));
		}

		/// <summary>Records the rejected allomorph with each supplied failed
		/// alternative.</summary>
		[Test]
		public void EnvironmentFailure_PreservesAllomorphAndAlternatives()
		{
			var root = CreateRoot("root");
			var environments = new[]
			{
				new AllomorphEnvironment(ConstraintType.Require, null, null) { Name = "/ p _" },
				new AllomorphEnvironment(ConstraintType.Require, null, null) { Name = "/ _ s" }
			};

			m_manager.Failed(null, m_word, FailureReason.Environments, root, environments);

			var reason = m_trace.Element("ParseCompleteTrace").Element("FailureReason");
			Assert.That(reason.Elements("Environment").Select(e => e.Value),
				Is.EqualTo(new[] { "/ p _", "/ _ s" }));
			Assert.That((int)reason.Element("Allomorph").Attribute("id"),
				Is.EqualTo((int)root.Properties[HCParser.FormID]));
		}

		/// <summary>Preserves both productivity restriction lists for the recorded
		/// reason.</summary>
		[Test]
		public void CompoundingFailure_PreservesProductivityRestrictions()
		{
			var rule = new CompoundingRule { Name = "synthetic compound" };
			rule.NonHeadProdRestrictionsMprFeatures.Add(new MprFeature { Name = "required" });
			var actual = new MprFeatureSet { new MprFeature { Name = "actual" } };

			m_manager.CompoundingRuleNotUnapplied(rule, -1, m_word,
				FailureReason.NonHeadProdRestrictMprFeatures, actual);

			var reason = m_trace.Element("CompoundingRuleAnalysisTrace").Element("FailureReason");
			Assert.That((string)reason.Attribute("type"), Is.EqualTo("missingProdRestrict"));
			Assert.That(reason.Element("StemProdRestricts").Element("MprFeature").Value,
				Is.EqualTo("actual"));
			Assert.That(reason.Element("RuleProdRestricts").Element("MprFeature").Value,
				Is.EqualTo("required"));
		}

		/// <summary>Does not infer productivity failure from an unrecognized reason's
		/// operand.</summary>
		[Test]
		public void CompoundingFailure_UnknownReasonIsNotGuessedFromOperand()
		{
			m_manager.CompoundingRuleNotUnapplied(new CompoundingRule(), -1, m_word,
				(FailureReason)int.MaxValue, new MprFeatureSet());

			var reason = m_trace.Element("CompoundingRuleAnalysisTrace").Element("FailureReason");
			Assert.That((string)reason.Attribute("type"), Is.EqualTo("unknown"));
		}

		private RootAllomorph CreateRoot(string headword)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphStem),
				TsStringUtils.MakeString(headword, Cache.DefaultVernWs), "synthetic gloss",
				new SandboxGenericMSA { MsaType = MsaType.kStem });
			var hcEntry = new LexEntry();
			hcEntry.Properties[HCParser.MsaID] = entry.MorphoSyntaxAnalysesOC.Single().Hvo;
			var root = new RootAllomorph(new Segments(m_stratum.CharacterDefinitionTable, "a"));
			root.Properties[HCParser.FormID] = entry.LexemeFormOA.Hvo;
			hcEntry.Allomorphs.Add(root);
			m_stratum.Entries.Add(hcEntry);
			return root;
		}
	}
}
