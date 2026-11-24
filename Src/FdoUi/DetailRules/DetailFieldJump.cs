// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// A detail row as Jump to field sees it: the object and field it shows, and whether it
	/// shows a given text.
	/// </summary>
	public sealed class DetailFieldJumpRow
	{
		private readonly Func<string> m_fieldName;
		private readonly Func<string, bool> m_showsText;

		/// <param name="obj">The object the row shows.</param>
		/// <param name="fieldName">Gives the name of the row's field, empty for none; asked
		/// only when the row is considered.</param>
		/// <param name="showsText">Whether the row shows a text; null for a row that never
		/// does.</param>
		public DetailFieldJumpRow(ICmObject obj, Func<string> fieldName, Func<string, bool> showsText)
		{
			Object = obj;
			m_fieldName = fieldName;
			m_showsText = showsText;
		}

		public ICmObject Object { get; }

		public string FieldName => m_fieldName?.Invoke() ?? string.Empty;

		public bool ShowsText(string text) => m_showsText != null && m_showsText(text);
	}

	/// <summary>
	/// The row a Jump to field request lands on. The request names the object and field a
	/// piece of the dictionary preview was generated from, and the text that piece shows.
	/// </summary>
	public static class DetailFieldJump
	{
		/// <summary>
		/// Finds the row for the request: the first row showing the field of the object, by
		/// name or by the rule for a preview field no row is named after. Failing that, the
		/// closest row: the object's row showing the text, else the first row showing the
		/// text, else the object's first row unless the object is the root.
		/// </summary>
		/// <param name="rows">The rows in view order; a null entry is a row never landed
		/// on, such as a header.</param>
		/// <param name="root">The object the detail view shows.</param>
		/// <param name="sourceField">The preview's name for the field, which may end in the
		/// field's kind (OA, OS, OC, RA, RS or RC).</param>
		/// <returns>The row's index, or -1 when no row fits.</returns>
		public static int FindRow(IReadOnlyList<DetailFieldJumpRow> rows, ICmObject root,
			ICmObject fieldObject, string sourceField, string text)
		{
			if (rows == null || fieldObject == null)
				return -1;
			var fieldName = PlainFieldName(sourceField);
			if (fieldName.Length > 0)
			{
				for (var i = 0; i < rows.Count; i++)
				{
					if (rows[i] != null && ShowsField(rows[i], fieldObject, fieldName, text))
						return i;
				}
			}

			var objectRow = -1;
			var textRow = -1;
			for (var i = 0; i < rows.Count; i++)
			{
				var row = rows[i];
				if (row == null)
					continue;
				// A headword that matches neither form's text lands on the lexeme form.
				if (IsHeadword(fieldName) && row.FieldName == "Form" && ShowsFormOf(row, fieldObject))
					return i;
				if (row.Object == fieldObject && row.ShowsText(text))
					return i;
				if (objectRow < 0 && fieldObject != root && row.Object == fieldObject)
					objectRow = i;
				if (textRow < 0 && row.ShowsText(text))
					textRow = i;
			}
			return textRow >= 0 ? textRow : objectRow;
		}

		/// <summary>
		/// The field name without the two letters that give an owning or reference field's
		/// kind; empty for null.
		/// </summary>
		public static string PlainFieldName(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
				return string.Empty;
			foreach (var kind in new[] { "OA", "OS", "OC", "RA", "RS", "RC" })
			{
				if (fieldName.EndsWith(kind, StringComparison.Ordinal))
					return fieldName.Substring(0, fieldName.Length - 2);
			}
			return fieldName;
		}

		/// <summary>
		/// Whether the object's field shows the text: as an alternative of a
		/// multi-writing-system field, as the name or abbreviation of an affix's morph type,
		/// or as that of a possibility a reference vector holds.
		/// </summary>
		public static bool ShowsText(ICmObject obj, string fieldName, string text)
		{
			if (obj == null || !obj.IsValidObject || string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(text))
				return false;
			var mdc = (IFwMetaDataCacheManaged)obj.Cache.MetaDataCacheAccessor;
			if (!mdc.FieldExists(obj.ClassID, fieldName, true))
				return false;
			var flid = mdc.GetFieldId2(obj.ClassID, fieldName, true);
			var sda = obj.Cache.DomainDataByFlid;
			switch ((CellarPropertyType)mdc.GetFieldType(flid))
			{
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					return AnyAlternativeIs(sda.get_MultiStringProp(obj.Hvo, flid), text);
				case CellarPropertyType.ReferenceAtomic:
					return fieldName == "MorphType" && obj is IMoAffixForm affix
						&& IsNameOrAbbreviation(affix.MorphTypeRA, text);
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					var repository = obj.Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
					for (var i = 0; i < sda.get_VecSize(obj.Hvo, flid); i++)
					{
						if (repository.TryGetObject(sda.get_VecItem(obj.Hvo, flid, i), out var item)
							&& IsNameOrAbbreviation(item as ICmPossibility, text))
						{
							return true;
						}
					}
					return false;
			}
			return false;
		}

		private static bool IsHeadword(string fieldName)
			=> fieldName == "MLHeadWord" || fieldName == "HeadWordRef";

		// Whether the row shows the object itself or, for an entry, its lexeme form.
		private static bool ShowsFormOf(DetailFieldJumpRow row, ICmObject fieldObject)
			=> row.Object == fieldObject
				|| fieldObject is ILexEntry entry && entry.LexemeFormOA == row.Object;

		// Whether the row shows the field of the object: under the field's own name, or by
		// the rule for a preview field that no row is named after.
		private static bool ShowsField(DetailFieldJumpRow row, ICmObject fieldObject, string fieldName,
			string text)
		{
			var rowField = row.FieldName;
			var rowObject = row.Object;
			if (rowField == fieldName && rowObject == fieldObject)
				return true;
			switch (fieldName)
			{
				case "ComplexFormEntryRefs":
					return rowField == "PrimaryLexemes"
						&& rowObject is ILexEntryRef subentryRef && subentryRef.OwningEntry == fieldObject;
				case "ConfigReferencedEntries":
					return rowField == "ComponentLexemes" && rowObject == fieldObject;
				case "DefinitionOrGloss":
					return (rowField == "Definition" || rowField == "Gloss")
						&& rowObject == fieldObject && row.ShowsText(text);
				case "LookupComplexEntryType":
					return rowField == "ComplexEntryTypes"
						&& rowObject is ILexEntryRef typedRef && typedRef.OwningEntry == fieldObject;
				case "MLHeadWord":
				case "HeadWordRef":
					return (rowField == "Form" || rowField == "CitationForm")
						&& ShowsFormOf(row, fieldObject) && row.ShowsText(text);
				case "MLPartOfSpeech":
					return rowField == "MorphoSyntaxAnalysis"
						&& rowObject is ILexSense sense && sense.MorphoSyntaxAnalysisRA == fieldObject;
				case "MorphTypes":
					if (!(rowObject is IMoAffixForm affix))
						return false;
					return rowField == "MorphType" && fieldObject is IMoMorphSynAnalysis msa
							&& msa.MorphTypes.Contains(affix.MorphTypeRA)
						|| rowField == "MorphTypes" && affix.MorphTypeRA == fieldObject;
				case "ReverseAbbr":
					if (!(rowObject is ILexEntryRef entryRef))
						return false;
					return rowField == "ComplexEntryTypes" && entryRef.ComplexEntryTypesRS.Contains(fieldObject)
						|| rowField == "VariantEntryTypes" && entryRef.EntryTypes.Contains(fieldObject);
			}
			return false;
		}

		private static bool IsNameOrAbbreviation(ICmPossibility possibility, string text)
			=> possibility != null
				&& (AnyAlternativeIs(possibility.Name, text) || AnyAlternativeIs(possibility.Abbreviation, text));

		// Compared decomposed, so composed and decomposed spellings of the text are the same.
		private static bool AnyAlternativeIs(ITsMultiString alternatives, string text)
		{
			if (alternatives == null)
				return false;
			var wanted = TsStringUtils.NormalizeNfd(text);
			for (var i = 0; i < alternatives.StringCount; i++)
			{
				var alternative = alternatives.GetStringFromIndex(i, out _).Text;
				if (!string.IsNullOrEmpty(alternative) && TsStringUtils.NormalizeNfd(alternative) == wanted)
					return true;
			}
			return false;
		}
	}
}
