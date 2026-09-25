// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The help-topic rule against a provider that knows a chosen set of topics, so each
	/// fallback level is reached deliberately.
	/// </summary>
	[TestFixture]
	public class FieldHelpTopicsTests
	{
		private static Func<string, bool> Knows(params string[] topics)
		{
			var known = new HashSet<string>(topics, StringComparer.Ordinal);
			return known.Contains;
		}

		private static HelpTopicSubject Gloss()
			=> new HelpTopicSubject
			{
				FieldName = "Gloss", Label = "Gloss", ClassName = "LexSense",
				OwnerClassName = "LexEntry", AreaName = "lexicon", ToolName = "lexiconEdit"
			};

		[Test]
		public void ExplicitTopic_Wins_EvenWhenUnknown()
		{
			Assert.That(FieldHelpTopics.Resolve("khtpField-Authored", FieldHelpTopics.FieldPrefix, Gloss(), Knows()),
				Is.EqualTo("khtpField-Authored"));
		}

		[Test]
		public void TheRuleFormulaTopic_RedirectsToTheEnvironmentChooser()
		{
			Assert.That(FieldHelpTopics.Resolve("khtpField-PhRegularRule-RuleFormula", FieldHelpTopics.FieldPrefix,
				Gloss(), Knows()), Is.EqualTo("khtpChoose-Environment"));
		}

		[Test]
		public void Generated_PrefersToolClassField()
		{
			var known = Knows("khtpField-lexiconEdit-LexSense-Gloss", "khtpField-lexiconEdit-Gloss", "khtpField-Gloss");
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, Gloss(), known),
				Is.EqualTo("khtpField-lexiconEdit-LexSense-Gloss"));
		}

		[Test]
		public void Generated_FallsThroughToolField_ClassField_ThenField()
		{
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, Gloss(), Knows("khtpField-lexiconEdit-Gloss")),
				Is.EqualTo("khtpField-lexiconEdit-Gloss"));
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, Gloss(), Knows("khtpField-LexSense-Gloss")),
				Is.EqualTo("khtpField-LexSense-Gloss"));
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, Gloss(), Knows("khtpField-Gloss")),
				Is.EqualTo("khtpField-Gloss"));
		}

		[Test]
		public void Generated_TriesTheLabel_WhenTheFieldNameYieldsNothing()
		{
			var subject = Gloss();
			subject.Label = "Complex Forms";
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, subject,
				Knows("khtpField-lexiconEdit-LexSense-ComplexForms")),
				Is.EqualTo("khtpField-lexiconEdit-LexSense-ComplexForms"));
		}

		[Test]
		public void Generated_UsesTheLabel_WhenThereIsNoFieldName()
		{
			var subject = Gloss();
			subject.FieldName = null;
			subject.Label = "Semantic Domains";
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, subject, Knows("khtpField-SemanticDomains")),
				Is.EqualTo("khtpField-SemanticDomains"));
		}

		[Test]
		public void Generated_NothingKnown_GivesTheNoTopicDefault_OrTheListDefault()
		{
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, Gloss(), Knows()),
				Is.EqualTo(FieldHelpTopics.NoHelpTopic));
			var list = Gloss();
			list.AreaName = "lists";
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, list, Knows()),
				Is.EqualTo("khtp-CustomListField"));
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.ChooserPrefix, Gloss(), Knows()),
				Is.EqualTo("khtpChoose-CmPossibility"));
		}

		[Test]
		public void Generated_Possibility_TriesItsSortKey()
		{
			var subject = new HelpTopicSubject
			{
				FieldName = "Name", ClassName = "CmPossibility", SortKey = "Genres",
				AreaName = "lists", ToolName = "genresEdit"
			};
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, subject, Knows("khtpField-genresEdit-Genres-Name")),
				Is.EqualTo("khtpField-genresEdit-Genres-Name"));
		}

		[Test]
		public void Generated_ExtendedNoteExampleAndTranslation_UseTheNoteClass()
		{
			var example = new HelpTopicSubject
			{
				FieldName = "Example", ClassName = "LexExampleSentence", OwnerClassName = "LexExtendedNote",
				ToolName = "lexiconEdit"
			};
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, example,
				Knows("khtpField-lexiconEdit-LexExtendedNote-Example")),
				Is.EqualTo("khtpField-lexiconEdit-LexExtendedNote-Example"));
			var translation = new HelpTopicSubject
			{
				FieldName = "Translations", ClassName = "LexExampleSentence", OwnerClassName = "LexExtendedNote",
				ToolName = "lexiconEdit"
			};
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, translation,
				Knows("khtpField-lexiconEdit-LexExtendedNote-Translations")),
				Is.EqualTo("khtpField-lexiconEdit-LexExtendedNote-Translations"));
		}

		[Test]
		public void Targets_UnderAnEntry_IsACrossReference_UnderASense_ALexicalRelation()
		{
			var entry = new HelpTopicSubject { FieldName = "Targets", ToolName = "lexiconEdit", TargetsParentIsEntry = true };
			var sense = new HelpTopicSubject { FieldName = "Targets", ToolName = "lexiconEdit", TargetsParentIsEntry = false };
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, entry, Knows()),
				Is.EqualTo("khtpField-lexiconEdit-CrossReferenceSubitem"));
			Assert.That(FieldHelpTopics.Resolve(null, FieldHelpTopics.FieldPrefix, sense, Knows()),
				Is.EqualTo("khtpField-lexiconEdit-LexicalRelationSubitem"));
		}

		[Test]
		public void ToAlphanumericId_CapitalizesEachRun_AndDropsTheRest()
		{
			Assert.That(FieldHelpTopics.ToAlphanumericId("Complex Forms"), Is.EqualTo("ComplexForms"));
			Assert.That(FieldHelpTopics.ToAlphanumericId("semantic-domain 2"), Is.EqualTo("SemanticDomain2"));
			Assert.That(FieldHelpTopics.ToAlphanumericId(null), Is.EqualTo(string.Empty));
		}
	}
}
