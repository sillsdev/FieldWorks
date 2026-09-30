// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using XCore;
using FeatVals = System.Collections.Generic.Dictionary<string, string>;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Fixture support for the rule formula view constructors: an in-memory phonological feature
	/// system, builders for the natural classes and rule contexts a formula draws, and a property
	/// table the view constructors can measure fonts against.
	/// </summary>
	public abstract class RuleFormulaVcTestBase : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Mediator m_mediator;

		/// <summary>
		/// The property table the view constructors under test are constructed with. It
		/// carries no stylesheet, so font measurement falls back to writing system defaults.
		/// </summary>
		protected PropertyTable m_propertyTable;

		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
		}

		public override void TestTearDown()
		{
			m_propertyTable?.Dispose();
			m_mediator?.Dispose();
			m_propertyTable = null;
			m_mediator = null;
			base.TestTearDown();
		}

		/// <summary>
		/// Adds the closed phonological features the natural class builders draw values from.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();
			AddClosedFeature("vd", "+", "-");
			AddClosedFeature("cons", "+", "-");
			AddClosedFeature("cont", "+", "-");
			AddClosedFeature("nasal", "+", "-");
		}

		/// <summary>
		/// Adds a closed phonological feature carrying the given symbolic values, naming the
		/// feature and each of its values after its own abbreviation.
		/// </summary>
		protected IFsClosedFeature AddClosedFeature(string abbreviation, params string[] values)
		{
			IFsClosedFeature feature = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			Cache.LanguageProject.PhFeatureSystemOA.FeaturesOC.Add(feature);
			feature.Name.SetAnalysisDefaultWritingSystem(abbreviation);
			feature.Abbreviation.SetAnalysisDefaultWritingSystem(abbreviation);
			foreach (string value in values)
			{
				IFsSymFeatVal symbol = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
				feature.ValuesOC.Add(symbol);
				symbol.Name.SetAnalysisDefaultWritingSystem(value);
				symbol.Abbreviation.SetAnalysisDefaultWritingSystem(value);
			}
			return feature;
		}

		/// <summary>
		/// Adds a feature-based natural class carrying the given feature values. The name and
		/// abbreviation are set in the default analysis writing system, which is where the
		/// natural class editor puts them; either may be null to leave it unset.
		/// </summary>
		protected IPhNCFeatures AddFeatureNaturalClass(string name, string abbreviation, FeatVals featVals)
		{
			IPhNCFeatures natClass = Cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS.Add(natClass);
			if (name != null)
				natClass.Name.SetAnalysisDefaultWritingSystem(name);
			if (abbreviation != null)
				natClass.Abbreviation.SetAnalysisDefaultWritingSystem(abbreviation);
			natClass.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			foreach (KeyValuePair<string, string> featVal in featVals)
				AddClosedValue(natClass.FeaturesOA, featVal.Key, featVal.Value);
			return natClass;
		}

		/// <summary>
		/// Adds a feature-based natural class named the way one built for a rule is named, in the
		/// user writing system and with no abbreviation.
		/// </summary>
		protected IPhNCFeatures AddRuleNamedFeatureNaturalClass(string ruleName, FeatVals featVals)
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, null, featVals);
			natClass.Name.SetUserWritingSystem(string.Format(MEStrings.ksRuleNCFeatsName, ruleName));
			return natClass;
		}

		/// <summary>
		/// Adds a segment-based natural class with the given name and abbreviation.
		/// </summary>
		protected IPhNCSegments AddSegmentNaturalClass(string name, string abbreviation)
		{
			IPhNCSegments natClass = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS.Add(natClass);
			natClass.Name.SetAnalysisDefaultWritingSystem(name);
			natClass.Abbreviation.SetAnalysisDefaultWritingSystem(abbreviation);
			return natClass;
		}

		/// <summary>
		/// Adds a regular phonological rule and returns its right-hand side.
		/// </summary>
		protected IPhSegRuleRHS AddRegularRule(string name)
		{
			IPhRegularRule rule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonRulesOS.Add(rule);
			rule.Name.SetAnalysisDefaultWritingSystem(name);
			return rule.RightHandSidesOS[0];
		}

		/// <summary>
		/// Adds a metathesis rule. Its structural change indices are left unset, so it holds
		/// contexts without assigning any of them to a switch or environment cell.
		/// </summary>
		protected IPhMetathesisRule AddMetathesisRule(string name)
		{
			IPhMetathesisRule rule = Cache.ServiceLocator.GetInstance<IPhMetathesisRuleFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonRulesOS.Add(rule);
			rule.Name.SetAnalysisDefaultWritingSystem(name);
			return rule;
		}

		/// <summary>
		/// Adds a natural class simple context to a metathesis rule's structural description,
		/// assigning the class once the context is owned.
		/// </summary>
		protected IPhSimpleContextNC AddStrucDescNCContext(IPhMetathesisRule rule, IPhNaturalClass natClass)
		{
			IPhSimpleContextNC ctxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			rule.StrucDescOS.Add(ctxt);
			ctxt.FeatureStructureRA = natClass;
			return ctxt;
		}

		/// <summary>
		/// Adds an affix process rule as the lexeme form of a new entry, with an empty input
		/// sequence.
		/// </summary>
		protected IMoAffixProcess AddAffixProcessRule()
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoAffixProcess rule = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
			entry.LexemeFormOA = rule;
			rule.InputOS.Clear();
			return rule;
		}

		/// <summary>
		/// Adds a natural class simple context to an affix process rule's input, assigning the
		/// class once the context is owned.
		/// </summary>
		protected IPhSimpleContextNC AddInputNCContext(IMoAffixProcess rule, IPhNaturalClass natClass)
		{
			IPhSimpleContextNC ctxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			rule.InputOS.Add(ctxt);
			ctxt.FeatureStructureRA = natClass;
			return ctxt;
		}

		/// <summary>
		/// Adds a natural class simple context to a rule's structural description, assigning the
		/// class once the context is owned.
		/// </summary>
		protected IPhSimpleContextNC AddStrucDescNCContext(IPhSegRuleRHS rhs, IPhNaturalClass natClass)
		{
			IPhSimpleContextNC ctxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			rhs.OwningRule.StrucDescOS.Add(ctxt);
			ctxt.FeatureStructureRA = natClass;
			return ctxt;
		}

		/// <summary>
		/// Adds a natural class simple context to a rule's structural change, assigning the class
		/// once the context is owned.
		/// </summary>
		protected IPhSimpleContextNC AddStrucChangeNCContext(IPhSegRuleRHS rhs, IPhNaturalClass natClass)
		{
			IPhSimpleContextNC ctxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			rhs.StrucChangeOS.Add(ctxt);
			ctxt.FeatureStructureRA = natClass;
			return ctxt;
		}

		/// <summary>
		/// Adds a natural class simple context owned by the phonological data, so it can be a
		/// member of a sequence or iteration context.
		/// </summary>
		protected IPhSimpleContextNC AddStandaloneNCContext(IPhNaturalClass natClass)
		{
			IPhSimpleContextNC ctxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.ContextsOS.Add(ctxt);
			ctxt.FeatureStructureRA = natClass;
			return ctxt;
		}

		/// <summary>
		/// Adds a sequence context over the given members, which must already be owned.
		/// </summary>
		protected IPhSequenceContext AddSequenceContext(params IPhPhonContext[] members)
		{
			IPhSequenceContext ctxt = Cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.ContextsOS.Add(ctxt);
			foreach (IPhPhonContext member in members)
				ctxt.MembersRS.Add(member);
			return ctxt;
		}

		/// <summary>
		/// Adds an iteration context over the given member, which may be null.
		/// </summary>
		protected IPhIterationContext AddIterationContext(IPhPhonContext member)
		{
			IPhIterationContext ctxt = Cache.ServiceLocator.GetInstance<IPhIterationContextFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.ContextsOS.Add(ctxt);
			ctxt.MemberRA = member;
			ctxt.Minimum = 1;
			ctxt.Maximum = -1;
			return ctxt;
		}

		private void AddClosedValue(IFsFeatStruc featStruc, string featureAbbr, string valueAbbr)
		{
			IFsClosedFeature feature = (IFsClosedFeature) Cache.LanguageProject.PhFeatureSystemOA
				.FeaturesOC.First(f => f.Abbreviation.AnalysisDefaultWritingSystem.Text == featureAbbr);
			IFsSymFeatVal symbol = feature.ValuesOC
				.First(v => v.Abbreviation.AnalysisDefaultWritingSystem.Text == valueAbbr);
			IFsClosedValue closedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			featStruc.FeatureSpecsOC.Add(closedValue);
			closedValue.FeatureRA = feature;
			closedValue.ValueRA = symbol;
		}
	}
}
