// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The commands that change which object is an entry's lexeme form: swapping it with an
	/// allomorph, and converting a form to another class (messages SwapLexemeWithAllomorph,
	/// SwapAllomorphWithLexeme, ConvertLexemeForm and ConvertAllomorph).
	/// </summary>
	public static class LexemeFormConversion
	{
		/// <summary>Whether the entry has a lexeme form and an allomorph to swap it
		/// with.</summary>
		public static bool CanSwap(ILexEntry entry)
			=> entry != null && entry.LexemeFormOA != null && entry.AlternateFormsOS.Count > 0;

		/// <summary>
		/// Makes <paramref name="allomorph"/> the entry's lexeme form and puts the old lexeme
		/// form in its place among the allomorphs, in one undoable step.
		/// </summary>
		public static void Swap(ILexEntry entry, IMoForm allomorph, string undoText, string redoText)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));
			if (allomorph == null)
				throw new ArgumentNullException(nameof(allomorph));
			UndoableUnitOfWorkHelper.Do(undoText, redoText, entry, () =>
			{
				entry.AlternateFormsOS.Insert(allomorph.IndexInOwner, entry.LexemeFormOA);
				entry.LexemeFormOA = allomorph;
			});
		}

		/// <summary>Whether <paramref name="form"/> is of the class a conversion starts
		/// from.</summary>
		public static bool CanConvert(IMoForm form, int fromClassId)
			=> form != null && fromClassId != 0 && form.ClassID == fromClassId;

		/// <summary>The title of the warning <see cref="DataLossMessage"/> is shown
		/// under.</summary>
		public static string DataLossCaption => FdoUiStrings.ksConvertFormLoseCaption;

		/// <summary>
		/// The warning to confirm before converting <paramref name="form"/>, naming what the
		/// conversion loses: an affix allomorph's environments, infix positions and
		/// grammatical information, or an affix process's rule. Null when nothing is lost.
		/// </summary>
		public static string DataLossMessage(IMoForm form)
		{
			switch (form)
			{
				case IMoAffixAllomorph allomorph:
					var losesEnvironments = allomorph.PhoneEnvRC.Count > 0;
					var losesPositions = allomorph.PositionRS.Count > 0;
					var losesGrammar = allomorph.MsEnvFeaturesOA != null || allomorph.MsEnvPartOfSpeechRA != null;
					if (losesEnvironments && losesPositions && losesGrammar)
						return FdoUiStrings.ksConvertFormLoseEnvInfixLocGramInfo;
					if (losesEnvironments && losesPositions)
						return FdoUiStrings.ksConvertFormLoseEnvInfixLoc;
					if (losesEnvironments && losesGrammar)
						return FdoUiStrings.ksConvertFormLoseEnvGramInfo;
					if (losesPositions && losesGrammar)
						return FdoUiStrings.ksConvertFormLoseInfixLocGramInfo;
					if (losesEnvironments)
						return FdoUiStrings.ksConvertFormLoseEnv;
					if (losesPositions)
						return FdoUiStrings.ksConvertFormLoseInfixLoc;
					if (losesGrammar)
						return FdoUiStrings.ksConvertFormLoseGramInfo;
					return null;
				case IMoAffixProcess _:
					return FdoUiStrings.ksConvertFormLoseRule;
				default:
					return null;
			}
		}

		/// <summary>
		/// Replaces <paramref name="form"/> in <paramref name="entry"/> with a new form of
		/// <paramref name="toClassId"/> carrying what the two classes share, in one undoable
		/// step.
		/// </summary>
		/// <param name="toClassId">MoAffixProcess, MoAffixAllomorph or MoStemAllomorph.</param>
		/// <returns>The new form.</returns>
		public static IMoForm Convert(ILexEntry entry, IMoForm form, int toClassId, string undoText, string redoText)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));
			if (form == null)
				throw new ArgumentNullException(nameof(form));
			IMoForm newForm = null;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, entry, () =>
			{
				newForm = CreateForm(entry, toClassId);
				entry.ReplaceMoForm(form, newForm);
			});
			return newForm;
		}

		private static IMoForm CreateForm(ILexEntry entry, int classId)
		{
			switch (classId)
			{
				case MoAffixProcessTags.kClassId:
					return entry.Services.GetInstance<IMoAffixProcessFactory>().Create();
				case MoAffixAllomorphTags.kClassId:
					return entry.Services.GetInstance<IMoAffixAllomorphFactory>().Create();
				case MoStemAllomorphTags.kClassId:
					return entry.Services.GetInstance<IMoStemAllomorphFactory>().Create();
				default:
					throw new ArgumentOutOfRangeException(nameof(classId), classId, "Not a form class a conversion can produce.");
			}
		}
	}
}
