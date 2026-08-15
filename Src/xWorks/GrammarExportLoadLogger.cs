// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using SIL.LCModel;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Collects HCLoader's per-item load warnings into a plain message list instead of
	/// surfacing them modally mid-export. HCLoader already skips the offending item and
	/// keeps going for every one of these, so none of them abort the grammar export.
	/// </summary>
	public class GrammarExportLoadLogger : IHCLoadErrorLogger
	{
		private readonly List<string> m_messages;

		public GrammarExportLoadLogger(List<string> messages)
		{
			m_messages = messages;
		}

		public void InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa)
		{
			m_messages.Add($"Invalid shape '{str}' at position {errorPos}.");
		}

		public void InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa)
		{
			m_messages.Add(isInvalidLhs
				? "Invalid affix process: left-hand side is invalid."
				: "Invalid affix process: right-hand side is invalid.");
		}

		public void InvalidPhoneme(IPhPhoneme phoneme)
		{
			m_messages.Add("Invalid phoneme definition.");
		}

		public void DuplicateGrapheme(IPhPhoneme phoneme)
		{
			m_messages.Add("Duplicate grapheme in a phoneme definition.");
		}

		public void InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa)
		{
			m_messages.Add($"Invalid environment: {reason}");
		}

		public void InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa)
		{
			m_messages.Add($"Invalid reduplication form: {reason}");
		}

		public void InvalidRewriteRule(IPhRegularRule prule, string reason)
		{
			m_messages.Add($"Invalid rewrite rule: {reason}");
		}

		public void InvalidStrata(string strata, string reason)
		{
			m_messages.Add($"Invalid strata '{strata}': {reason}");
		}

		public void OutOfScopeSlot(IMoInflAffixSlot slot, IMoInflAffixTemplate template, string reason)
		{
			m_messages.Add($"Out-of-scope affix slot: {reason}");
		}

		public void UnmatchedReduplicationIndexedClass(IMoForm form, string reason, string environment)
		{
			m_messages.Add($"Unmatched reduplication indexed class: {reason}");
		}
	}
}
