// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The word around a point in a string: from the point it grows both ways while each
	/// character is word-forming in its writing system or is a number, and stops where the
	/// writing system or the spelling-check setting changes, or what two styles set of them.
	/// The same word the Views text engine selects (VwTextSelection::FindWordBoundaries).
	/// </summary>
	public static class TextWordBoundaries
	{
		/// <summary>Finds the word around <paramref name="ich"/> in <paramref name="tss"/>;
		/// empty when no word touches it.</summary>
		/// <param name="writingSystemFactory">Says which characters each writing system
		/// counts as word-forming.</param>
		/// <param name="styleProperties">The properties a named style gives, null when the
		/// style is unknown; null for no stylesheet, where styles never end a word.</param>
		/// <param name="ichMin">The word's start.</param>
		/// <param name="ichLim">The word's end.</param>
		public static void Find(ITsString tss, int ich, ILgWritingSystemFactory writingSystemFactory,
			Func<string, ITsTextProps> styleProperties, out int ichMin, out int ichLim)
		{
			var length = tss?.Length ?? 0;
			ich = Math.Max(0, Math.Min(ich, length));
			ichMin = ichLim = ich;
			if (length == 0)
				return;
			var text = tss.Text;
			var start = tss.get_PropertiesAt(ich == length ? ich - 1 : ich);
			while (ichLim < length && IsInWord(tss, text, ichLim, start, writingSystemFactory, styleProperties))
				ichLim++;
			while (ichMin > 0 && IsInWord(tss, text, ichMin - 1, start, writingSystemFactory, styleProperties))
				ichMin--;
		}

		private static bool IsInWord(ITsString tss, string text, int ich, ITsTextProps start,
			ILgWritingSystemFactory writingSystemFactory, Func<string, ITsTextProps> styleProperties)
		{
			var props = tss.get_PropertiesAt(ich);
			if (IndicatesWordBreak(props, start, styleProperties))
				return false;
			var ch = text[ich];
			if (Icu.Character.IsNumeric(ch))
				return true;
			var ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out _);
			var engine = writingSystemFactory?.get_EngineOrNull(ws);
			return engine != null && engine.get_IsWordForming(ch);
		}

		// Whether two characters' properties keep them out of one word. Writing systems count
		// only when both sides carry properties; styles count by what they set.
		private static bool IndicatesWordBreak(ITsTextProps first, ITsTextProps second,
			Func<string, ITsTextProps> styleProperties)
		{
			if (ReferenceEquals(first, second))
				return false;
			if (first != null && second != null && PropertyDiffers(first, second, (int)FwTextPropType.ktptWs))
				return true;
			if (PropertyDiffers(first, second, (int)FwTextPropType.ktptSpellCheck))
				return true;
			if (styleProperties == null)
				return false;
			var firstStyle = first?.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			var secondStyle = second?.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			if (string.IsNullOrEmpty(firstStyle) && string.IsNullOrEmpty(secondStyle))
				return false;
			if (string.Equals(firstStyle, secondStyle, StringComparison.Ordinal))
				return false;
			return IndicatesWordBreak(string.IsNullOrEmpty(firstStyle) ? null : styleProperties(firstStyle),
				string.IsNullOrEmpty(secondStyle) ? null : styleProperties(secondStyle), null);
		}

		// An absent property has its default value; a variant counts only when both set it.
		private static bool PropertyDiffers(ITsTextProps first, ITsTextProps second, int property)
		{
			int firstValue = 0, secondValue = 0, firstVariant = -1, secondVariant = -1;
			if (first != null)
			{
				firstValue = first.GetIntPropValues(property, out firstVariant);
				if (firstVariant == -1)
					firstValue = 0;
			}
			if (second != null)
			{
				secondValue = second.GetIntPropValues(property, out secondVariant);
				if (secondVariant == -1)
					secondValue = 0;
			}
			if (firstValue != secondValue)
				return true;
			return firstVariant != -1 && secondVariant != -1 && firstVariant != secondVariant;
		}
	}
}
