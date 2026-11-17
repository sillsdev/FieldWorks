// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// Which writing systems a sense's Reversal Entries field offers and which of them it
	/// shows. It offers every analysis writing system of the project and, until the user
	/// chooses, shows them all, so an entry in one the project has not checked is still seen.
	/// </summary>
	public static class ReversalEntryWritingSystems
	{
		/// <summary>Every writing system the field can be told to show.</summary>
		public static IReadOnlyList<CoreWritingSystemDefinition> Options(LcmCache cache)
		{
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));
			return cache.LanguageProject.AnalysisWritingSystems.ToList();
		}

		/// <summary>
		/// The writing systems the field shows: the stored selection, restricted to the ids
		/// that are still options and in the options' order; every option when none of them is.
		/// </summary>
		/// <param name="storedSelection">The field's persisted selection, or null for
		/// none.</param>
		public static IReadOnlyList<CoreWritingSystemDefinition> Shown(LcmCache cache,
			IReadOnlyList<string> storedSelection)
			=> Choices(cache).Shown(storedSelection);

		/// <summary>
		/// The field's Writing Systems menu: every option in order, each marked with whether
		/// the field shows it and whether it may be switched off.
		/// </summary>
		/// <param name="storedSelection">The field's persisted selection, or null for
		/// none.</param>
		public static IReadOnlyList<WritingSystemMenuOption> Menu(LcmCache cache,
			IReadOnlyList<string> storedSelection)
			=> Choices(cache).Menu(storedSelection);

		/// <summary>The field's choices: every option, all of them shown by default.</summary>
		public static WritingSystemChoices Choices(LcmCache cache)
		{
			var options = Options(cache);
			return new WritingSystemChoices(options, () => options);
		}
	}
}
