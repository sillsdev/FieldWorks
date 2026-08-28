// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Tests that populating a phoneme's phonological features from its Basic IPA Symbol
	/// leaves at most one feature specification per phonological feature (LT-22714).
	/// </summary>
	[TestFixture]
	// Some tests here build a real Slice, which is a UserControl whose rootsite creates
	// apartment-threaded Views COM objects. NUnit 3 defaults to MTA.
	[Apartment(System.Threading.ApartmentState.STA)]
	public class BasicIPASymbolSliceTests : MemoryOnlyBackendProviderTestBase
	{
		private IPhPhoneme m_phoneme;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				var langProject = Cache.LanguageProject;
				if (langProject.PhonologicalDataOA == null)
				{
					langProject.PhonologicalDataOA =
						Cache.ServiceLocator.GetInstance<IPhPhonDataFactory>().Create();
				}
				if (langProject.PhFeatureSystemOA == null)
				{
					langProject.PhFeatureSystemOA =
						Cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>().Create();
				}
				var phonemeSet = Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create();
				langProject.PhonologicalDataOA.PhonemeSetsOS.Add(phonemeSet);
				m_phoneme = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				phonemeSet.PhonemesOC.Add(m_phoneme);
			});
		}

		public override void TestTearDown()
		{
			m_phoneme = null;
			base.TestTearDown();
		}

		/// <summary>
		/// The shipped IPA inventory must not name the same feature twice for one segment,
		/// because each pair becomes a separate feature specification on the phoneme.
		/// </summary>
		[Test]
		public void BasicIPAInfo_NoSegmentNamesTheSameFeatureTwice()
		{
			var offenders = new List<string>();
			foreach (var segment in IpaInfoDocument().XPathSelectElements(
				"/SegmentDefinitions/SegmentDefinition"))
			{
				var featureIds = segment.XPathSelectElements("Features/FeatureValuePair")
					.Select(pair => (string)pair.Attribute("feature"))
					.ToList();
				var duplicated = featureIds.GroupBy(id => id)
					.Where(group => group.Count() > 1)
					.Select(group => group.Key)
					.ToList();
				if (duplicated.Count > 0)
				{
					var representation = segment.XPathSelectElement("Representations/Representation");
					offenders.Add(string.Format("{0}: {1}",
						representation == null ? "?" : representation.Value.Trim(),
						string.Join(", ", duplicated)));
				}
			}
			Assert.That(offenders, Is.Empty,
				"segments naming a feature more than once: " + string.Join("; ", offenders));
		}

		[Test]
		public void SettingSymbol_AddsEachFeatureOnce()
		{
			CreateFeatureSystemFor("j");
			using (CreateSlice())
			{
				SetSymbol("j");

				AssertNoFeatureIsSpecifiedTwice();
			}
		}

		/// <summary>
		/// Reproduces the reported sequence: once a symbol has populated the features, the
		/// slice re-enters its populate branch on every later call.
		/// </summary>
		[Test]
		public void RepopulatingSameSymbol_DoesNotDuplicateFeatures()
		{
			CreateFeatureSystemFor("p");
			using (var slice = CreateSlice())
			{
				SetSymbol("p");
				var countAfterFirstPopulate = m_phoneme.FeaturesOA.FeatureSpecsOC.Count;

				NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
				{
					slice.SetFeaturesBasedOnIPA();
					slice.SetFeaturesBasedOnIPA();
				});

				AssertNoFeatureIsSpecifiedTwice();
				Assert.That(m_phoneme.FeaturesOA.FeatureSpecsOC.Count,
					Is.EqualTo(countAfterFirstPopulate));
			}
		}

		/// <summary>
		/// Correcting a symbol must not leave the phoneme carrying two specifications, with
		/// contradictory values, for the features the two symbols share.
		/// </summary>
		[Test]
		public void ChangingSymbol_DoesNotDuplicateSharedFeatures()
		{
			CreateFeatureSystemFor("p", "t");
			using (CreateSlice())
			{
				SetSymbol("p");
				SetSymbol("t");

				AssertNoFeatureIsSpecifiedTwice();
				foreach (var pair in FeaturePairsFor("t"))
				{
					var spec = m_phoneme.FeaturesOA.FeatureSpecsOC.OfType<IFsClosedValue>()
						.SingleOrDefault(value => value.FeatureRA.CatalogSourceId == pair.Key);
					Assert.That(spec, Is.Not.Null, "no specification for " + pair.Key);
					Assert.That(spec.ValueRA.CatalogSourceId, Is.EqualTo(pair.Value),
						"wrong value for " + pair.Key);
				}
			}
		}

		/// <summary>
		/// Features set through the chooser leave the slice unaware that the feature structure
		/// is already populated, so a later symbol edit must still not duplicate anything.
		/// </summary>
		[Test]
		public void SymbolEditAfterFeaturesAlreadySet_DoesNotDuplicateFeatures()
		{
			CreateFeatureSystemFor("p", "t");
			using (var slice = CreateSlice())
			{
				SetSymbol("p");
				NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
				{
					var voice = Cache.LanguageProject.PhFeatureSystemOA.GetFeature("fPAVoice");
					var closedValue = m_phoneme.FeaturesOA.GetOrCreateValue((IFsClosedFeature)voice);
					closedValue.FeatureRA = voice;
					closedValue.ValueRA = ((IFsClosedFeature)voice).ValuesOC.First();
				});

				SetSymbol("t");

				AssertNoFeatureIsSpecifiedTwice();
			}
		}

		private void AssertNoFeatureIsSpecifiedTwice()
		{
			Assert.That(m_phoneme.FeaturesOA, Is.Not.Null, "no features were populated");
			var duplicated = m_phoneme.FeaturesOA.FeatureSpecsOC
				.GroupBy(spec => spec.FeatureRA)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key.CatalogSourceId)
				.ToList();
			Assert.That(duplicated, Is.Empty,
				"features specified more than once: " + string.Join(", ", duplicated));
		}

		/// <summary>
		/// The populate service must be idempotent on its own terms, whatever the slice's gate
		/// decides. These drive it directly, so they do not construct a UserControl and do not
		/// depend on the latch -- which is what the LT-22716 repair will do too.
		/// </summary>
		[Test]
		public void Populator_AppliedRepeatedly_WritesEachFeatureOnce()
		{
			CreateFeatureSystemFor("p");
			SetSymbol("p");

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(Cache, m_phoneme, IpaInfoDocument());
				PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(Cache, m_phoneme, IpaInfoDocument());
				PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(Cache, m_phoneme, IpaInfoDocument());
			});

			AssertNoFeatureIsSpecifiedTwice();
			Assert.That(PhonemeFeaturePopulator.DuplicatedFeatureIds(m_phoneme), Is.Empty);
		}

		[Test]
		public void Populator_AfterASymbolChange_KeepsOneSpecPerFeatureWithTheNewValue()
		{
			CreateFeatureSystemFor("p", "t");
			SetSymbol("p");
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
				PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(Cache, m_phoneme, IpaInfoDocument()));

			SetSymbol("t");
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
				PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(Cache, m_phoneme, IpaInfoDocument()));

			AssertNoFeatureIsSpecifiedTwice();
			foreach (var pair in FeaturePairsFor("t"))
			{
				var spec = m_phoneme.FeaturesOA.FeatureSpecsOC.OfType<IFsClosedValue>()
					.SingleOrDefault(value => value.FeatureRA.CatalogSourceId == pair.Key);
				Assert.That(spec, Is.Not.Null, "no specification for " + pair.Key);
				Assert.That(spec.ValueRA.CatalogSourceId, Is.EqualTo(pair.Value),
					"wrong value for " + pair.Key);
			}
		}

		/// <summary>An empty symbol writes nothing, so the repair cannot blank a
		/// phoneme.</summary>
		[Test]
		public void Populator_WithNoSymbol_WritesNothing()
		{
			CreateFeatureSystemFor("p");
			SetSymbol(string.Empty);

			int written = 0;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
				written = PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(Cache, m_phoneme,
					IpaInfoDocument()));

			Assert.That(written, Is.Zero);
		}

		/// <summary>
		/// DuplicatedFeatureIds is what a repair would report, so it has to actually see damage
		/// rather than always returning empty.
		/// </summary>
		[Test]
		public void DuplicatedFeatureIds_ReportsAnInjectedDuplicate()
		{
			CreateFeatureSystemFor("p");
			SetSymbol("p");
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(Cache, m_phoneme, IpaInfoDocument());
				// The pre-fix behaviour: a second spec for a feature already specified.
				var existing = m_phoneme.FeaturesOA.FeatureSpecsOC.OfType<IFsClosedValue>().First();
				var extra = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
				m_phoneme.FeaturesOA.FeatureSpecsOC.Add(extra);
				extra.FeatureRA = existing.FeatureRA;
				extra.ValueRA = existing.ValueRA;
			});

			Assert.That(PhonemeFeaturePopulator.DuplicatedFeatureIds(m_phoneme),
				Has.Length.EqualTo(1));
		}

		private BasicIPASymbolSlice CreateSlice()
		{
			var slice = new BasicIPASymbolSlice(Cache, "customWithParams",
				PhPhonemeTags.kflidBasicIPASymbol, null, m_phoneme, null,
				Cache.DefaultPronunciationWs);
			slice.Cache = Cache;
			return slice;
		}

		private void SetSymbol(string ipaSymbol)
		{
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				m_phoneme.BasicIPASymbol =
					TsStringUtils.MakeString(ipaSymbol, Cache.DefaultPronunciationWs);
			});
		}

		private static XDocument IpaInfoDocument()
		{
			return XDocument.Load(Path.Combine(FwDirectoryFinder.TemplateDirectory,
				PhPhonemeTags.ksBasicIPAInfoFile));
		}

		/// <summary>
		/// Gets the feature-to-value catalog ids the shipped inventory lists for a symbol,
		/// keeping the first value where a symbol names the same feature more than once.
		/// </summary>
		private static IDictionary<string, string> FeaturePairsFor(string ipaSymbol)
		{
			var pairs = new Dictionary<string, string>();
			var features = IpaInfoDocument().XPathSelectElement(
				"/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='"
				+ ipaSymbol + "']]/Features");
			Assert.That(features, Is.Not.Null, "no inventory entry for " + ipaSymbol);
			foreach (var pair in features.Elements("FeatureValuePair"))
			{
				var featureId = (string)pair.Attribute("feature");
				if (!pairs.ContainsKey(featureId))
					pairs.Add(featureId, (string)pair.Attribute("value"));
			}
			return pairs;
		}

		/// <summary>
		/// Builds the closed features and symbolic values the given symbols refer to, since a
		/// memory-only project starts with an empty phonological feature system.
		/// </summary>
		private void CreateFeatureSystemFor(params string[] ipaSymbols)
		{
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				var featureSystem = Cache.LanguageProject.PhFeatureSystemOA;
				foreach (var ipaSymbol in ipaSymbols)
				{
					foreach (var pair in FeaturePairsFor(ipaSymbol))
					{
						var closedFeature =
							featureSystem.GetFeature(pair.Key) as IFsClosedFeature;
						if (closedFeature == null)
						{
							closedFeature = Cache.ServiceLocator
								.GetInstance<IFsClosedFeatureFactory>().Create();
							featureSystem.FeaturesOC.Add(closedFeature);
							closedFeature.CatalogSourceId = pair.Key;
							closedFeature.Name.SetAnalysisDefaultWritingSystem(pair.Key);
						}
						if (closedFeature.GetSymbolicValue(pair.Value) == null)
						{
							var symbolicValue = Cache.ServiceLocator
								.GetInstance<IFsSymFeatValFactory>().Create();
							closedFeature.ValuesOC.Add(symbolicValue);
							symbolicValue.CatalogSourceId = pair.Value;
							symbolicValue.Name.SetAnalysisDefaultWritingSystem(pair.Value);
						}
					}
				}
			});
		}
	}
}
