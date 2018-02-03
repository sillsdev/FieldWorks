// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.Collections;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class ComplexConcPatternModel
	{
		private readonly ComplexConcPatternSda m_sda;
		private readonly SpanFactory<ShapeNode> m_spanFactory;
		private Matcher<ComplexConcParagraphData, ShapeNode> m_matcher;
		private readonly LcmCache m_cache;
		private FeatureSystem m_featSys;

		public ComplexConcPatternModel(LcmCache cache)
			: this(cache, new ComplexConcGroupNode())
		{
		}

		public ComplexConcPatternModel(LcmCache cache, ComplexConcPatternNode root)
		{
			m_cache = cache;
			Root = root;
			m_spanFactory = new ShapeSpanFactory();
			m_sda = new ComplexConcPatternSda(cache.GetManagedSilDataAccess(), Root);
		}

		public ISilDataAccessManaged DataAccess => m_sda;

		public ComplexConcPatternNode Root { get; }

		public bool IsPatternEmpty => Root.IsLeaf || (Root.Children.Count == 1 && Root.Children[0] is ComplexConcWordBdryNode);

		public ComplexConcPatternNode GetNode(int hvo)
		{
			return m_sda.Nodes[hvo];
		}

		public void Compile()
		{
			m_featSys = new FeatureSystem
				{
					new SymbolicFeature("type",
						new FeatureSymbol("bdry", "Boundary"),
						new FeatureSymbol("word", "Word"),
						new FeatureSymbol("morph", "Morph"),
						new FeatureSymbol("ttag", "Text Tag")) {Description = "Type"},
					new SymbolicFeature("anchorType",
						new FeatureSymbol("paraBdry", "Paragraph"),
						new FeatureSymbol("segBdry", "Segment"),
						new FeatureSymbol("wordBdry", "Word"))
				};
			foreach (var ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				m_featSys.Add(new StringFeature($"entry-{ws.Handle}") { Description = $"Entry-{ws.Abbreviation}" });
				m_featSys.Add(new StringFeature($"form-{ws.Handle}") { Description = $"Form-{ws.Abbreviation}" });
			}

			foreach (var ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
			{
				m_featSys.Add(new StringFeature($"gloss-{ws.Handle}") { Description = $"Gloss-{ws.Abbreviation}" });
			}

			m_featSys.Add(new SymbolicFeature("cat", m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().AllInstances()
				.Select(pos => new FeatureSymbol(pos.Hvo.ToString(CultureInfo.InvariantCulture), pos.Abbreviation.BestAnalysisAlternative.Text)))
				{
					Description = "Category"
				});

			m_featSys.Add(new SymbolicFeature("tag", m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS
				.SelectMany(poss => poss.SubPossibilitiesOS, (category, tag) => new FeatureSymbol(tag.Hvo.ToString(CultureInfo.InvariantCulture), tag.Abbreviation.BestAnalysisAlternative.Text)))
				{
					Description = "Tag"
				});

			m_featSys.Add(new ComplexFeature("infl") {Description = "Infl", DefaultValue = FeatureStruct.New().Value});
			foreach (var feature in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				var complexFeat = feature as IFsComplexFeature;
				if (complexFeat != null)
				{
					m_featSys.Add(new ComplexFeature(complexFeat.Hvo.ToString(CultureInfo.InvariantCulture)) {Description = complexFeat.Abbreviation.BestAnalysisAlternative.Text, DefaultValue = FeatureStruct.New().Value});
				}
				else
				{
					var closedFeat = (IFsClosedFeature) feature;
					m_featSys.Add(new SymbolicFeature(closedFeat.Hvo.ToString(CultureInfo.InvariantCulture), closedFeat.ValuesOC.Select(sym =>
						new FeatureSymbol(sym.Hvo.ToString(CultureInfo.InvariantCulture), sym.Abbreviation.BestAnalysisAlternative.Text))
						.Concat(new FeatureSymbol(closedFeat.Hvo.ToString(CultureInfo.InvariantCulture) + "_us", "unspecified")))
						{
							Description = closedFeat.Abbreviation.BestAnalysisAlternative.Text,
							DefaultSymbolID = closedFeat.Hvo.ToString(CultureInfo.InvariantCulture) + "_us"
						});
				}
			}

			var pattern = new Pattern<ComplexConcParagraphData, ShapeNode>();
			pattern.Children.Add(Root.GeneratePattern(m_featSys));
			m_matcher = new Matcher<ComplexConcParagraphData, ShapeNode>(m_spanFactory, pattern, new MatcherSettings<ShapeNode> {UseDefaults = true});
		}

		public IEnumerable<IParaFragment> Search(IStText text)
		{
			if (IsPatternEmpty)
			{
				return Enumerable.Empty<IParaFragment>();
			}

			var matches = new List<IParaFragment>();
			foreach (var para in text.ParagraphsOS.OfType<IStTxtPara>())
			{
				IParaFragment lastFragment = null;
				var data = new ComplexConcParagraphData(m_spanFactory, m_featSys, para);
				var match = m_matcher.Match(data);
				while (match.Success)
				{
					if (match.Span.Start == match.Span.End && ((FeatureSymbol) match.Span.Start.Annotation.FeatureStruct.GetValue<SymbolicFeatureValue>("type")).ID == "bdry")
					{
						match = match.NextMatch();
						continue;
					}

					var startNode = match.Span.Start;
					if (((FeatureSymbol) startNode.Annotation.FeatureStruct.GetValue<SymbolicFeatureValue>("type")).ID == "bdry")
					{
						startNode = startNode.Next;
					}

					var startAnn = startNode.Annotation;
					if (((FeatureSymbol) startAnn.FeatureStruct.GetValue<SymbolicFeatureValue>("type")).ID == "morph")
					{
						startAnn = startAnn.Parent;
					}

					var startAnalysis = (Tuple<IAnalysis, int, int>)startAnn.Data;
					var endNode = match.Span.End;
					if (((FeatureSymbol) endNode.Annotation.FeatureStruct.GetValue<SymbolicFeatureValue>("type")).ID == "bdry")
					{
						endNode = endNode.Prev;
					}

					var endAnn = endNode.Annotation;
					if (((FeatureSymbol) endAnn.FeatureStruct.GetValue<SymbolicFeatureValue>("type")).ID == "morph")
					{
						endAnn = endAnn.Parent;
					}

					Debug.Assert(startNode.CompareTo(endNode) <= 0);

					var endAnalysis = (Tuple<IAnalysis, int, int>)endAnn.Data;

					if (lastFragment != null && lastFragment.GetMyBeginOffsetInPara() == startAnalysis.Item2 && lastFragment.GetMyEndOffsetInPara() == endAnalysis.Item3)
					{
						match = GetNextMatch(match);
						continue;
					}

					var seg = para.SegmentsOS.Last(s => s.BeginOffset <= startAnalysis.Item2);
					lastFragment = new ParaFragment(seg, startAnalysis.Item2, endAnalysis.Item3, startAnalysis.Item1);
					matches.Add(lastFragment);

					match = GetNextMatch(match);
				}
			}

			return matches;
		}

		private static Match<ComplexConcParagraphData, ShapeNode> GetNextMatch(Match<ComplexConcParagraphData, ShapeNode> match)
		{
			var nextNode = match.Span.GetEnd(match.Matcher.Direction);
			if (((FeatureSymbol) nextNode.Annotation.FeatureStruct.GetValue<SymbolicFeatureValue>("type")).ID != "bdry")
			{
				nextNode = nextNode.GetNext(match.Matcher.Direction);
			}
			return match.Matcher.Match(match.Input, nextNode);
		}
	}
}