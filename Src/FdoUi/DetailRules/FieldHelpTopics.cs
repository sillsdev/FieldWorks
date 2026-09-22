// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using SIL.FieldWorks.Common.FwUtils;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The field a help topic is resolved for: what the layout and the field's object say
	/// about it, plus the area and tool showing it. Every value is a plain string so either
	/// detail view can supply it.
	/// </summary>
	public sealed class HelpTopicSubject
	{
		/// <summary>The layout's field name (the <c>field</c> attribute); may be null.</summary>
		public string FieldName { get; set; }

		/// <summary>The layout's raw, unlocalized label; may be null.</summary>
		public string Label { get; set; }

		/// <summary>The class name of the field's object.</summary>
		public string ClassName { get; set; }

		/// <summary>The class name of the object's owner; null for an unowned object.</summary>
		public string OwnerClassName { get; set; }

		/// <summary>The object's sort key; consulted only for a CmPossibility.</summary>
		public string SortKey { get; set; }

		/// <summary>
		/// For a lexical-relation Targets field: true when the relation is shown under an
		/// entry, false under a sense. Null for every other field.
		/// </summary>
		public bool? TargetsParentIsEntry { get; set; }

		/// <summary>The current area choice, e.g. "lexicon" or "lists".</summary>
		public string AreaName { get; set; }

		/// <summary>The current tool, e.g. "lexiconEdit".</summary>
		public string ToolName { get; set; }
	}

	/// <summary>
	/// Resolves the help topic id of a detail field: the layout's explicit id when it has one,
	/// otherwise an id generated from the field, its object and the tool, taking the first
	/// candidate the help provider knows.
	/// </summary>
	public static class FieldHelpTopics
	{
		/// <summary>The prefix of a field's own help topic.</summary>
		public const string FieldPrefix = "khtpField";

		/// <summary>The prefix of a field's chooser-dialog help topic.</summary>
		public const string ChooserPrefix = "khtpChoose";

		/// <summary>The topic offered when no generated candidate exists.</summary>
		public const string NoHelpTopic = "khtpNoHelpTopic";

		/// <summary>
		/// The help topic for a field whose subject is already known.
		/// </summary>
		/// <param name="explicitTopicId">The layout's <c>helpTopicID</c>; null or empty to
		/// generate one.</param>
		/// <param name="prefix"><see cref="FieldPrefix"/> or <see cref="ChooserPrefix"/>.</param>
		/// <param name="subject">The field being resolved.</param>
		/// <param name="topicExists">Whether the help provider knows a topic id.</param>
		public static string Resolve(string explicitTopicId, string prefix, HelpTopicSubject subject,
			Func<string, bool> topicExists)
		{
			if (subject == null)
				throw new ArgumentNullException(nameof(subject));
			return Resolve(explicitTopicId, prefix, () => subject, topicExists);
		}

		/// <summary>
		/// The help topic for a field, describing the subject only when one has to be
		/// generated: an explicit id returns before <paramref name="subject"/> is called.
		/// </summary>
		/// <param name="explicitTopicId">The layout's <c>helpTopicID</c>; null or empty to
		/// generate one.</param>
		/// <param name="prefix"><see cref="FieldPrefix"/> or <see cref="ChooserPrefix"/>.</param>
		/// <param name="subject">Describes the field; called at most once.</param>
		/// <param name="topicExists">Whether the help provider knows a topic id.</param>
		public static string Resolve(string explicitTopicId, string prefix, Func<HelpTopicSubject> subject,
			Func<string, bool> topicExists)
		{
			if (subject == null)
				throw new ArgumentNullException(nameof(subject));
			if (topicExists == null)
				throw new ArgumentNullException(nameof(topicExists));

			var authored = NormalizeExplicit(explicitTopicId);
			if (authored != null)
				return authored;
			var described = subject() ?? throw new InvalidOperationException("The help-topic subject is null.");
			return Generate(prefix, described, topicExists);
		}

		/// <summary>
		/// The layout's authored topic as the product uses it, or null when there is none.
		/// The rule-formula field's authored topic points at the environment chooser topic.
		/// </summary>
		public static string NormalizeExplicit(string explicitTopicId)
		{
			if (explicitTopicId == "khtpField-PhRegularRule-RuleFormula")
				return "khtpChoose-Environment";
			return string.IsNullOrEmpty(explicitTopicId) ? null : explicitTopicId;
		}

		/// <summary>
		/// The topic-exists predicate for a help provider: a non-empty id the provider has a
		/// string for. A null provider knows nothing.
		/// </summary>
		public static Func<string, bool> KnownBy(IHelpTopicProvider provider)
			=> id => provider != null && !string.IsNullOrEmpty(id) && provider.GetHelpString(id) != null;

		private static string Generate(string prefix, HelpTopicSubject subject, Func<string, bool> topicExists)
		{
			// Cross-reference (entry level) and lexical-relation (sense level) subitems share one
			// topic per level.
			if (subject.FieldName == "Targets" && subject.TargetsParentIsEntry.HasValue)
			{
				return subject.TargetsParentIsEntry.Value
					? prefix + "-" + subject.ToolName + "-CrossReferenceSubitem"
					: prefix + "-" + subject.ToolName + "-LexicalRelationSubitem";
			}

			var labelId = ToAlphanumericId(subject.Label);
			var fieldName = string.IsNullOrEmpty(subject.FieldName) ? labelId : subject.FieldName;
			var candidate = GenerateForField(prefix, fieldName, subject, topicExists);
			if (topicExists(candidate))
				return candidate;
			// The field attribute gave nothing the provider knows; try the label.
			candidate = GenerateForField(prefix, labelId, subject, topicExists);
			if (topicExists(candidate))
				return candidate;
			if (prefix == ChooserPrefix)
				return "khtpChoose-CmPossibility";
			// An undefined list falls back to the generic list-field topic.
			return subject.AreaName == "lists" ? "khtp-CustomListField" : NoHelpTopic;
		}

		// Candidates from most to least specific: tool+class+field, the possibility's sort key,
		// tool+field, class+field, field. The last is returned unverified.
		private static string GenerateForField(string prefix, string fieldName, HelpTopicSubject subject,
			Func<string, bool> topicExists)
		{
			var className = subject.ClassName;
			// An Example or Translation under an extended note has its own topics.
			if ((fieldName == "Example" || (fieldName ?? string.Empty).StartsWith("Translation", StringComparison.Ordinal))
				&& subject.OwnerClassName == "LexExtendedNote")
			{
				className = "LexExtendedNote";
			}
			var tool = subject.ToolName;

			var candidate = prefix + "-" + tool + "-" + className + "-" + fieldName;
			if (topicExists(candidate))
				return candidate;
			if (className == "CmPossibility")
			{
				candidate = prefix + "-" + tool + "-" + subject.SortKey + "-" + fieldName;
				if (topicExists(candidate))
					return candidate;
			}
			candidate = prefix + "-" + tool + "-" + fieldName;
			if (topicExists(candidate))
				return candidate;
			candidate = prefix + "-" + className + "-" + fieldName;
			if (topicExists(candidate))
				return candidate;
			return prefix + "-" + fieldName;
		}

		/// <summary>
		/// A label reduced to a topic-id fragment: letters and digits only, each run
		/// capitalized ("Complex Forms" becomes "ComplexForms"). Empty for a null label.
		/// </summary>
		public static string ToAlphanumericId(string label)
		{
			var id = new StringBuilder();
			if (string.IsNullOrEmpty(label))
				return string.Empty;
			var nextCapital = true;
			foreach (var ch in label)
			{
				if (char.IsLetterOrDigit(ch))
				{
					id.Append(nextCapital ? char.ToUpper(ch) : ch);
					nextCapital = false;
				}
				else
				{
					nextCapital = true;
				}
			}
			return id.ToString();
		}
	}
}
