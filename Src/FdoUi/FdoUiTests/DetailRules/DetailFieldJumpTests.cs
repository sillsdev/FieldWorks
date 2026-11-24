// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The row a Jump to field request lands on: the field's own row, the rows that stand in
	/// for the preview's fields no row is named after, the closest row when neither fits, and
	/// which data counts as a row showing the clicked text.
	/// </summary>
	[TestFixture]
	public class DetailFieldJumpTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private IMoStemAllomorph _lexemeForm;
		private ILexSense _sense;
		private IMoStemMsa _msa;
		private ILexEntry _complexForm;
		private ILexEntryRef _complexFormRef;
		private ILexEntryType _complexFormType;
		private ILexEntry _affix;
		private IMoAffixAllomorph _affixForm;

		// An entry "kata" (citation form "katana") with one sense glossed "cut" and defined
		// "to cut", a complex form of it with one type, and a suffix entry.
		[SetUp]
		public void MakeEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var services = Cache.ServiceLocator;
				var entries = services.GetInstance<ILexEntryFactory>();
				_entry = entries.Create();
				_lexemeForm = services.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.LexemeFormOA = _lexemeForm;
				_lexemeForm.Form.set_String(Cache.DefaultVernWs, Vernacular("kata"));
				_entry.CitationForm.set_String(Cache.DefaultVernWs, Vernacular("katana"));
				_sense = services.GetInstance<ILexSenseFactory>().Create();
				_entry.SensesOS.Add(_sense);
				_sense.Gloss.set_String(Cache.DefaultAnalWs, Analysis("cut"));
				_sense.Definition.set_String(Cache.DefaultAnalWs, Analysis("to cut"));
				_msa = services.GetInstance<IMoStemMsaFactory>().Create();
				_entry.MorphoSyntaxAnalysesOC.Add(_msa);
				_sense.MorphoSyntaxAnalysisRA = _msa;

				_complexForm = entries.Create();
				_complexFormRef = services.GetInstance<ILexEntryRefFactory>().Create();
				_complexForm.EntryRefsOS.Add(_complexFormRef);
				_complexFormRef.RefType = LexEntryRefTags.krtComplexForm;
				_complexFormRef.ComponentLexemesRS.Add(_entry);
				var lexDb = Cache.LangProject.LexDbOA;
				if (lexDb.ComplexEntryTypesOA == null)
					lexDb.ComplexEntryTypesOA = services.GetInstance<ICmPossibilityListFactory>().Create();
				_complexFormType = services.GetInstance<ILexEntryTypeFactory>().Create();
				lexDb.ComplexEntryTypesOA.PossibilitiesOS.Add(_complexFormType);
				_complexFormType.Name.set_String(Cache.DefaultAnalWs, Analysis("Compound"));
				_complexFormType.Abbreviation.set_String(Cache.DefaultAnalWs, Analysis("comp."));
				_complexFormRef.ComplexEntryTypesRS.Add(_complexFormType);

				_affix = entries.Create();
				_affixForm = services.GetInstance<IMoAffixAllomorphFactory>().Create();
				_affix.LexemeFormOA = _affixForm;
				_affixForm.MorphTypeRA = services.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphSuffix);
				_affixForm.MorphTypeRA.Name.set_String(Cache.DefaultAnalWs, Analysis("suffix"));
			});
		}

		[TearDown]
		public void DeleteEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in new ICmObject[] { _complexForm, _affix, _entry, _complexFormType })
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
			});
		}

		private ITsString Vernacular(string text) => TsStringUtils.MakeString(text, Cache.DefaultVernWs);

		private ITsString Analysis(string text) => TsStringUtils.MakeString(text, Cache.DefaultAnalWs);

		// A row showing a field of an object, judged to show a text by the field's data.
		private static DetailFieldJumpRow Row(ICmObject obj, string fieldName)
			=> new DetailFieldJumpRow(obj, () => fieldName, text => DetailFieldJump.ShowsText(obj, fieldName, text));

		// The entry's rows as a detail view lists them, a header first.
		private List<DetailFieldJumpRow> EntryRows()
			=> new List<DetailFieldJumpRow>
			{
				null,
				Row(_lexemeForm, "Form"),
				Row(_entry, "CitationForm"),
				Row(_sense, "Gloss"),
				Row(_sense, "Definition"),
				Row(_sense, "MorphoSyntaxAnalysis")
			};

		private int Find(List<DetailFieldJumpRow> rows, ICmObject root, ICmObject fieldObject, string sourceField,
			string text = "nothing shows this")
			=> DetailFieldJump.FindRow(rows, root, fieldObject, sourceField, text);

		[Test]
		public void TheFieldsOwnRow_IsFoundByObjectAndName_WhateverItsText()
		{
			Assert.That(Find(EntryRows(), _entry, _sense, "Definition"), Is.EqualTo(4));
			Assert.That(Find(EntryRows(), _entry, _entry, "CitationForm"), Is.EqualTo(2));
		}

		[TestCase("SensesOS", "Senses")]
		[TestCase("LexemeFormOA", "LexemeForm")]
		[TestCase("MorphoSyntaxAnalysisRA", "MorphoSyntaxAnalysis")]
		[TestCase("ComplexEntryTypesRS", "ComplexEntryTypes")]
		[TestCase("Gloss", "Gloss")]
		[TestCase(null, "")]
		public void PlainFieldName_DropsTheFieldKind(string sourceField, string expected)
		{
			Assert.That(DetailFieldJump.PlainFieldName(sourceField), Is.EqualTo(expected));
		}

		[Test]
		public void AFieldNamedWithItsKind_FindsTheRowOfThePlainName()
		{
			Assert.That(Find(EntryRows(), _entry, _sense, "MorphoSyntaxAnalysisRA"), Is.EqualTo(5));
		}

		[Test]
		public void DefinitionOrGloss_LandsOnWhicheverShowsTheText()
		{
			Assert.That(Find(EntryRows(), _entry, _sense, "DefinitionOrGloss", "to cut"), Is.EqualTo(4));
			Assert.That(Find(EntryRows(), _entry, _sense, "DefinitionOrGloss", "cut"), Is.EqualTo(3));
		}

		[Test]
		public void AHeadword_LandsOnTheFormShowingIt_ElseOnTheLexemeForm()
		{
			Assert.That(Find(EntryRows(), _entry, _entry, "MLHeadWord", "katana"), Is.EqualTo(2), "the citation form");
			Assert.That(Find(EntryRows(), _entry, _entry, "HeadWordRef", "kata"), Is.EqualTo(1), "the lexeme form");
			Assert.That(Find(EntryRows(), _entry, _entry, "MLHeadWord", "kata1"), Is.EqualTo(1),
				"a headword with a homograph number matches neither form's text");
		}

		[Test]
		public void TheGrammaticalInfo_LandsOnTheSenseThatRefersToIt()
		{
			Assert.That(Find(EntryRows(), _entry, _msa, "MLPartOfSpeech"), Is.EqualTo(5));
		}

		[Test]
		public void AComplexFormsPreviewFields_LandOnItsEntryRefsRows()
		{
			var rows = new List<DetailFieldJumpRow>
			{
				Row(_complexForm, "CitationForm"),
				Row(_complexFormRef, "ComponentLexemes"),
				Row(_complexFormRef, "ComplexEntryTypes"),
				Row(_complexFormRef, "PrimaryLexemes")
			};

			Assert.That(Find(rows, _complexForm, _complexFormRef, "ConfigReferencedEntries"), Is.EqualTo(1));
			Assert.That(Find(rows, _complexForm, _complexForm, "LookupComplexEntryType"), Is.EqualTo(2));
			Assert.That(Find(rows, _complexForm, _complexForm, "ComplexFormEntryRefs"), Is.EqualTo(3));
			Assert.That(Find(rows, _complexForm, _complexFormType, "ReverseAbbr"), Is.EqualTo(2),
				"the type's abbreviation lands on the ref that has the type");
		}

		[Test]
		public void AMorphType_LandsOnTheAffixFormThatHasIt()
		{
			var rows = new List<DetailFieldJumpRow> { Row(_affix, "CitationForm"), Row(_affixForm, "MorphTypes") };

			Assert.That(Find(rows, _affix, _affixForm.MorphTypeRA, "MorphTypes"), Is.EqualTo(1));
		}

		[Test]
		public void WithNoRowForTheField_TheClosestRowIsTaken()
		{
			var rows = EntryRows();

			Assert.That(Find(rows, _entry, _sense, "ExampleSentences", "to cut"), Is.EqualTo(4),
				"the object's row showing the text");
			Assert.That(Find(rows, _entry, _complexForm, "Bibliography", "katana"), Is.EqualTo(2),
				"else the first row showing the text");
			Assert.That(Find(rows, _entry, _sense, "ExampleSentences"), Is.EqualTo(3), "else the object's first row");
			Assert.That(Find(rows, _entry, _entry, "Bibliography"), Is.EqualTo(-1),
				"the root's first row is no better than no row");
			Assert.That(Find(rows, _entry, _complexForm, "Bibliography"), Is.EqualTo(-1));
		}

		[Test]
		public void ARowShowingTheText_BeatsAnotherRowOfTheObject()
		{
			var rows = new List<DetailFieldJumpRow> { Row(_sense, "Gloss"), Row(_entry, "CitationForm") };

			Assert.That(Find(rows, _complexForm, _sense, "ExampleSentences", "katana"), Is.EqualTo(1));
		}

		[Test]
		public void HeadersAndMissingInput_FindNothing()
		{
			Assert.That(Find(new List<DetailFieldJumpRow> { null, null }, _entry, _sense, "Gloss"), Is.EqualTo(-1));
			Assert.That(DetailFieldJump.FindRow(null, _entry, _sense, "Gloss", "cut"), Is.EqualTo(-1));
			Assert.That(DetailFieldJump.FindRow(EntryRows(), _entry, null, "Gloss", "cut"), Is.EqualTo(-1));
		}

		[Test]
		public void ARowsFieldName_IsAskedOnlyWhenTheRowIsConsidered()
		{
			var asked = 0;
			var rows = new List<DetailFieldJumpRow>
			{
				Row(_sense, "Gloss"),
				new DetailFieldJumpRow(_entry, () => { asked++; return "CitationForm"; }, null)
			};

			Assert.That(Find(rows, _entry, _sense, "Gloss"), Is.EqualTo(0));
			Assert.That(asked, Is.EqualTo(0), "the search stopped at the first row");
		}

		[Test]
		public void ShowsText_ReadsAMultiWritingSystemField_WhateverTheTextsComposition()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => _sense.Gloss.set_String(Cache.DefaultAnalWs, Analysis("café")));

			Assert.That(DetailFieldJump.ShowsText(_sense, "Gloss", "café"), Is.True);
			Assert.That(DetailFieldJump.ShowsText(_sense, "Gloss", "café"), Is.True, "decomposed");
			Assert.That(DetailFieldJump.ShowsText(_sense, "Gloss", "cafe"), Is.False);
			Assert.That(DetailFieldJump.ShowsText(_sense, "Definition", "café"), Is.False, "another field");
		}

		[Test]
		public void ShowsText_ReadsThePossibilitiesOfAReferenceVector_ByNameOrAbbreviation()
		{
			Assert.That(DetailFieldJump.ShowsText(_complexFormRef, "ComplexEntryTypes", "Compound"), Is.True);
			Assert.That(DetailFieldJump.ShowsText(_complexFormRef, "ComplexEntryTypes", "comp."), Is.True);
			Assert.That(DetailFieldJump.ShowsText(_complexFormRef, "ComplexEntryTypes", "Idiom"), Is.False);
			Assert.That(DetailFieldJump.ShowsText(_complexFormRef, "ComponentLexemes", "kata"), Is.False,
				"an entry is not a possibility");
		}

		[Test]
		public void ShowsText_ReadsAnAffixFormsMorphType()
		{
			Assert.That(DetailFieldJump.ShowsText(_affixForm, "MorphType", "suffix"), Is.True);
			Assert.That(DetailFieldJump.ShowsText(_lexemeForm, "MorphType", "suffix"), Is.False, "a stem's is not read");
		}

		[Test]
		public void ShowsText_IsFalse_ForNoText_AnUnknownField_OrNoObject()
		{
			Assert.That(DetailFieldJump.ShowsText(_sense, "Gloss", ""), Is.False);
			Assert.That(DetailFieldJump.ShowsText(_sense, "Gloss", null), Is.False);
			Assert.That(DetailFieldJump.ShowsText(_sense, "NoSuchField", "cut"), Is.False);
			Assert.That(DetailFieldJump.ShowsText(_sense, null, "cut"), Is.False);
			Assert.That(DetailFieldJump.ShowsText(null, "Gloss", "cut"), Is.False);
			Assert.That(EntryRows().Count(row => row != null && row.ShowsText("cut")), Is.EqualTo(1));
		}
	}
}
