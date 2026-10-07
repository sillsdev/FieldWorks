// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.Phonology;
using SIL.LCModel.DomainServices;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// Why an environment string is not well formed, as Describe Error in Environment reports
	/// it. The text is whatever the editor holds, saved or not.
	/// </summary>
	public static class EnvironmentErrors
	{
		/// <summary>
		/// The recognizer environments are checked with, over the project's phonemes and natural
		/// class abbreviations.
		/// </summary>
		public static PhonEnvRecognizer CreateRecognizer(LcmCache cache)
		{
			var phonology = cache.LangProject.PhonologicalDataOA;
			return new PhonEnvRecognizer(phonology.AllPhonemes().ToArray(),
				phonology.AllNaturalClassAbbrs().ToArray());
		}

		/// <summary>
		/// The explanation of what is wrong with <paramref name="text"/>; null when it is a
		/// well-formed environment, and for empty text, which has nothing to describe.
		/// </summary>
		/// <param name="recognizer">The recognizer to check with; see
		/// <see cref="CreateRecognizer"/>.</param>
		/// <param name="text">The environment string.</param>
		public static string Describe(PhonEnvRecognizer recognizer, string text)
		{
			if (string.IsNullOrEmpty(text) || recognizer.Recognize(text))
				return null;
			StringServices.CreateErrorMessageFromXml(text, recognizer.ErrorMessage, out _, out var message);
			return message;
		}
	}
}
