// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RenderingSelectionRuleTests.cs
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class RenderingSelectionRuleTests
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question does not match.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleDoesNotApply()
		{
			var selectRenderingEndingInAAfterXyz = new RenderingSelectionRule(@"\bxyz {0}", @"a$");
			Assert.IsNull(selectRenderingEndingInAAfterXyz.ChooseRendering(
				"Why even bother with Mark?", new Word[] {"Mark"}, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question does not match because it
		/// doesn't contain the key term in the correct place.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleDoesNotApply_TermNotInQuestion()
		{
			var selectRenderingEndingInAAfterXyz = new RenderingSelectionRule(@"\bwith {0}", @"a$");
			Assert.IsNull(selectRenderingEndingInAAfterXyz.ChooseRendering(
				"Why even bother with a dude named Mark?", new Word[] { "Mark" }, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question matches because the
		/// specified preceding word is present and there is a rendering which matches the
		/// rendering selector expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleApplies_RenderingMatchFound_PrecedingWord()
		{
			var selectRenderingEndingInBAfterWith = new RenderingSelectionRule(@"\bwith {0}", @"b$");
			Assert.AreEqual("Renderingb", selectRenderingEndingInBAfterWith.ChooseRendering(
				"Why even bother with Mark?", new Word[] { "Mark" }, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question matches because the
		/// specified following word is present and there is a rendering which matches the
		/// rendering selector expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleApplies_RenderingMatchFound_FollowingWord()
		{
			var selectRenderingEndingInBAfterWith = new RenderingSelectionRule(@"{0} for\b", @"b$");
			Assert.AreEqual("Renderingb", selectRenderingEndingInBAfterWith.ChooseRendering(
				"Was Mark for or against discipleship?", new Word[] { "Mark" }, new[] { "bob Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question matches because the
		/// specified prefix is present and there is a rendering which matches the rendering
		/// selector expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleApplies_RenderingMatchFound_Prefix()
		{
			var selectRenderingEndingInBAfterWith = new RenderingSelectionRule(@"\bpre\w*{0}", @"^pre");
			Assert.AreEqual("preRendering", selectRenderingEndingInBAfterWith.ChooseRendering(
				"What is a prebaptism ceremony?", new Word[] { "bapt" }, new[] { "unRendering preacher", "preRendering", "antiRendering" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question matches because the
		/// specified suffix is present and there is a rendering which matches the rendering
		/// selector expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleApplies_RenderingMatchFound_Suffix()
		{
			var selectRenderingEndingInBAfterWith = new RenderingSelectionRule(@"{0}\w*ed\b", "o\u0301$");
			Assert.AreEqual("sano\u0301", selectRenderingEndingInBAfterWith.ChooseRendering(
				"Was the woman healed?", new Word[] { "heal" }, new[] { "sanaba", "sano\u0301", "curaba", "curo\u0301" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question matches because the
		/// specified preceding word is present and there is a rendering which matches the
		/// rendering selector expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleApplies_MultiWordTermMatch()
		{
			var selectRenderingEndingInBAfterWith = new RenderingSelectionRule(@"Why {0}", @"b$");
			Assert.AreEqual("Renderingb", selectRenderingEndingInBAfterWith.ChooseRendering(
				"Why even - bother - with Mark?", new Word[] { "even", "bother", "with" }, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the given question matches but there is no
		/// rendering which matches the rendering selector expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_RuleApplies_RenderingMatchNotFound()
		{
			var selectRenderingEndingInCAfterWith = new RenderingSelectionRule(@"\bwith\b {0}", @"c$");
			Assert.IsNull(selectRenderingEndingInCAfterWith.ChooseRendering(
				"Why even bother with Mark?", new Word[] { "Mark" }, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the rule is invalid because the question
		/// matching expression does not have a format placeholder for the key term.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_InvalidQuestionMatchExpr_NoKeyTermPlaceholder()
		{
			var bogusRule = new RenderingSelectionRule(@"\bxyz\b", "a$");
			Assert.IsNull(bogusRule.ChooseRendering(
				"Is this a question about xyz?", new Word[] { "Mark" }, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the rule is invalid because the question
		/// matching expression is not a valid regular expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_InvalidQuestionMatchExpr_BadRegex()
		{
			var bogusRule = new RenderingSelectionRule(@"(about\b {0}", "a$");
			Assert.IsNull(bogusRule.ChooseRendering(
				"Is this a question about Mark?", new Word[] { "Mark" }, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ChooseRendering when the rule is invalid because the rendering
		/// matching expression is not a valid regular expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ChooseRendering_InvalidRenderingMatchExpr_BadRegex()
		{
			var bogusRule = new RenderingSelectionRule(@"about\b {0}", @"a)");
			Assert.IsNull(bogusRule.ChooseRendering(
				"Is this a question about Mark?", new Word[] { "Mark" }, new[] { "Renderinga", "Renderingb" }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the QuestionMatchSuffix property.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetQuestionMatchSuffix()
		{
			var rule = new RenderingSelectionRule();
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchSuffix);
			Assert.IsNull(rule.QuestionMatchingPattern);
			rule.QuestionMatchSuffix = "post";
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Suffix, rule.QuestionMatchCriteriaType);
			Assert.AreEqual("post", rule.QuestionMatchSuffix);
			Assert.AreEqual(@"{0}\w*post\b", rule.QuestionMatchingPattern);
			rule.QuestionMatchSuffix = string.Empty;
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchSuffix);
			Assert.IsNull(rule.QuestionMatchingPattern);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the QuestionMatchPrefix property.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetQuestionMatchPrefix()
		{
			var rule = new RenderingSelectionRule();
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchPrefix);
			Assert.IsNull(rule.QuestionMatchingPattern);
			rule.QuestionMatchPrefix = "pre";
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Prefix, rule.QuestionMatchCriteriaType);
			Assert.AreEqual("pre", rule.QuestionMatchPrefix);
			Assert.AreEqual(@"\bpre\w*{0}", rule.QuestionMatchingPattern);
			rule.QuestionMatchPrefix = string.Empty;
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchPrefix);
			Assert.IsNull(rule.QuestionMatchingPattern);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the QuestionMatchPrecedingWord property.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetQuestionMatchPrecedingWord()
		{
			var rule = new RenderingSelectionRule();
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchPrecedingWord);
			Assert.IsNull(rule.QuestionMatchingPattern);
			rule.QuestionMatchPrecedingWord = "before";
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.PrecedingWord, rule.QuestionMatchCriteriaType);
			Assert.AreEqual("before", rule.QuestionMatchPrecedingWord);
			Assert.AreEqual(@"\bbefore {0}", rule.QuestionMatchingPattern);
			rule.QuestionMatchPrecedingWord = string.Empty;
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchPrecedingWord);
			Assert.IsNull(rule.QuestionMatchingPattern);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the QuestionMatchFollowingWord property.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetQuestionMatchFollowingWord()
		{
			var rule = new RenderingSelectionRule();
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchFollowingWord);
			Assert.IsNull(rule.QuestionMatchingPattern);
			rule.QuestionMatchFollowingWord = "after";
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.FollowingWord, rule.QuestionMatchCriteriaType);
			Assert.AreEqual("after", rule.QuestionMatchFollowingWord);
			Assert.AreEqual(@"{0} after\b", rule.QuestionMatchingPattern);
			rule.QuestionMatchFollowingWord = string.Empty;
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Undefined, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchFollowingWord);
			Assert.IsNull(rule.QuestionMatchingPattern);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the QuestionMatchType property when the regular expression does not match any
		/// of the pre-defined formats.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetQuestionMatchType_Custom()
		{
			var rule = new RenderingSelectionRule(@"\bbefore {0} after\b", "ation$");
			Assert.AreEqual(RenderingSelectionRule.QuestionMatchType.Custom, rule.QuestionMatchCriteriaType);
			Assert.IsNull(rule.QuestionMatchSuffix);
			Assert.IsNull(rule.QuestionMatchPrefix);
			Assert.IsNull(rule.QuestionMatchPrecedingWord);
			Assert.IsNull(rule.QuestionMatchFollowingWord);
			Assert.AreEqual(@"\bbefore {0} after\b", rule.QuestionMatchingPattern);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RenderingMatchSuffix property.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetRenderingMatchSuffix()
		{
			var rule = new RenderingSelectionRule();
			Assert.AreEqual(RenderingSelectionRule.RenderingMatchType.Undefined, rule.RenderingMatchCriteriaType);
			Assert.IsNull(rule.RenderingMatchSuffix);
			Assert.IsNull(rule.RenderingMatchingPattern);
			rule.RenderingMatchSuffix = "post";
			Assert.AreEqual(RenderingSelectionRule.RenderingMatchType.Suffix, rule.RenderingMatchCriteriaType);
			Assert.AreEqual("post", rule.RenderingMatchSuffix);
			Assert.AreEqual("post$", rule.RenderingMatchingPattern);
			rule.RenderingMatchSuffix = string.Empty;
			Assert.AreEqual(RenderingSelectionRule.RenderingMatchType.Undefined, rule.RenderingMatchCriteriaType);
			Assert.IsNull(rule.RenderingMatchSuffix);
			Assert.IsNull(rule.RenderingMatchingPattern);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RenderingMatchPrefix property.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetRenderingMatchPrefix()
		{
			var rule = new RenderingSelectionRule();
			Assert.AreEqual(RenderingSelectionRule.RenderingMatchType.Undefined, rule.RenderingMatchCriteriaType);
			Assert.IsNull(rule.RenderingMatchPrefix);
			Assert.IsNull(rule.RenderingMatchingPattern);
			rule.RenderingMatchPrefix = "pre";
			Assert.AreEqual(RenderingSelectionRule.RenderingMatchType.Prefix, rule.RenderingMatchCriteriaType);
			Assert.AreEqual("pre", rule.RenderingMatchPrefix);
			Assert.AreEqual("^pre", rule.RenderingMatchingPattern);
			rule.RenderingMatchPrefix = string.Empty;
			Assert.AreEqual(RenderingSelectionRule.RenderingMatchType.Undefined, rule.RenderingMatchCriteriaType);
			Assert.IsNull(rule.RenderingMatchPrefix);
			Assert.IsNull(rule.RenderingMatchingPattern);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RenderingMatchType property when the regular expression does not match any
		/// of the pre-defined formats.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetRenderingMatchType_Custom()
		{
			var rule = new RenderingSelectionRule(@"{0} after\b", " ");
			Assert.AreEqual(RenderingSelectionRule.RenderingMatchType.Custom, rule.RenderingMatchCriteriaType);
			Assert.IsNull(rule.RenderingMatchSuffix);
			Assert.IsNull(rule.RenderingMatchPrefix);
			Assert.AreEqual(" ", rule.RenderingMatchingPattern);
		}
	}
}
