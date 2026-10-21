// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Insert rule of a detail row: which fields take a new object, which object and
	/// field a row's command resolves to, and where the new object goes; which allomorph
	/// inserts an entry offers, and which entry a variant insert extends.
	/// </summary>
	[TestFixture]
	public class DetailObjectInsertionTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private ILexSense _sense1;
		private ILexSense _sense2;
		private ILexExampleSentence _example;
		private ILexExtendedNote _note1;
		private ILexExtendedNote _note2;
		private ILexEtymology _etymology1;
		private ILexEtymology _etymology2;

		// A stem entry with two senses, two etymologies, and on the first sense one example
		// and two extended notes.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var services = Cache.ServiceLocator;
				_entry = services.GetInstance<ILexEntryFactory>().Create();
				var form = services.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.LexemeFormOA = form;
				form.MorphTypeRA = services.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
				_sense1 = services.GetInstance<ILexSenseFactory>().Create();
				_entry.SensesOS.Add(_sense1);
				_sense2 = services.GetInstance<ILexSenseFactory>().Create();
				_entry.SensesOS.Add(_sense2);
				_example = services.GetInstance<ILexExampleSentenceFactory>().Create();
				_sense1.ExamplesOS.Add(_example);
				_note1 = services.GetInstance<ILexExtendedNoteFactory>().Create();
				_sense1.ExtendedNoteOS.Add(_note1);
				_note2 = services.GetInstance<ILexExtendedNoteFactory>().Create();
				_sense1.ExtendedNoteOS.Add(_note2);
				_etymology1 = services.GetInstance<ILexEtymologyFactory>().Create();
				_entry.EtymologyOS.Add(_etymology1);
				_etymology2 = services.GetInstance<ILexEtymologyFactory>().Create();
				_entry.EtymologyOS.Add(_etymology2);
			});
		}

		[TearDown]
		public void DeleteEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (_entry.IsValidObject)
					_entry.Delete();
			});
		}

		[Test]
		public void CanInsertInto_ASequence_AndReportsItsEnd()
		{
			Assert.That(DetailObjectInsertion.CanInsertInto(_sense1, "ExtendedNote", out var index), Is.True);
			Assert.That(index, Is.EqualTo(2));
			Assert.That(DetailObjectInsertion.CanInsertInto(_sense2, "ExtendedNote", out index), Is.True);
			Assert.That(index, Is.EqualTo(0));
		}

		[Test]
		public void CanInsertInto_AnAtomicField_OnlyWhileItIsEmpty()
		{
			Assert.That(DetailObjectInsertion.CanInsertInto(_entry, "LexemeForm", out var index), Is.False, "filled");
			Assert.That(index, Is.EqualTo(-1));
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _entry.LexemeFormOA = null);
			Assert.That(DetailObjectInsertion.CanInsertInto(_entry, "LexemeForm", out index), Is.True, "empty");
			Assert.That(index, Is.EqualTo(-1), "an atomic field has no index");
		}

		[Test]
		public void CanInsertInto_AVirtualReferenceVector_ButNotAPlainReferenceField()
		{
			Assert.That(DetailObjectInsertion.CanInsertInto(_entry, "VariantFormEntryBackRefs", out var index), Is.True,
				"a virtual back reference takes a new object");
			Assert.That(index, Is.EqualTo(0));
			Assert.That(DetailObjectInsertion.CanInsertInto(_sense1, "SemanticDomains", out _), Is.False,
				"a reference collection chooses, it does not create");
			Assert.That(DetailObjectInsertion.CanInsertInto(_sense1, "MorphoSyntaxAnalysis", out _), Is.False,
				"a reference atomic field likewise");
		}

		[Test]
		public void CanInsertInto_RefusesAnUnknownField_AndAMissingObject()
		{
			Assert.That(DetailObjectInsertion.CanInsertInto(_sense1, "NoSuchField", out _), Is.False);
			Assert.That(DetailObjectInsertion.CanInsertInto(_sense1, "", out _), Is.False);
			Assert.That(DetailObjectInsertion.CanInsertInto(null, "Senses", out _), Is.False);
			var note = _note2;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => note.Delete());
			Assert.That(DetailObjectInsertion.CanInsertInto(note, "Examples", out _), Is.False, "a deleted object");
		}

		[Test]
		public void CanInsert_AsksTheRootFirst_ThenTheRowsObject()
		{
			Assert.That(DetailObjectInsertion.CanInsert(_entry, _etymology1, "Senses", out var index), Is.True,
				"the root has Senses");
			Assert.That(index, Is.EqualTo(2), "the index is the root's");
			Assert.That(DetailObjectInsertion.CanInsert(_entry, _sense1, "ExtendedNote", out index), Is.True,
				"the row's sense has ExtendedNote");
			Assert.That(index, Is.EqualTo(2));
			Assert.That(DetailObjectInsertion.CanInsert(_entry, _etymology1, "ExtendedNote", out index), Is.False,
				"neither the entry nor an etymology has ExtendedNote");
			Assert.That(index, Is.EqualTo(-1));
			Assert.That(DetailObjectInsertion.CanInsert(null, _sense1, "Examples", out _), Is.True, "no root");
		}

		[Test]
		public void IsOwningFieldName_TellsOwningFieldsFromReferenceOnes()
		{
			Assert.That(DetailObjectInsertion.IsOwningFieldName(Cache, "ExtendedNote"), Is.True);
			Assert.That(DetailObjectInsertion.IsOwningFieldName(Cache, "LexemeForm"), Is.True, "an owning atomic field");
			Assert.That(DetailObjectInsertion.IsOwningFieldName(Cache, "VariantFormEntryBackRefs"), Is.False,
				"a virtual reference vector");
			Assert.That(DetailObjectInsertion.IsOwningFieldName(Cache, "SemanticDomains"), Is.False);
			Assert.That(DetailObjectInsertion.IsOwningFieldName(Cache, "NoSuchField"), Is.False);
			Assert.That(DetailObjectInsertion.IsOwningFieldName(Cache, null), Is.False);
		}

		[Test]
		public void ResolveFlid_PrefersTheOwnerClassesField_ElseTheCandidates()
		{
			Assert.That(DetailObjectInsertion.ResolveFlid(_note1, "ExtendedNote", 0), Is.EqualTo(0),
				"a note has no ExtendedNote field");
			Assert.That(DetailObjectInsertion.ResolveFlid(_note1, "ExtendedNote", LexSenseTags.kClassId),
				Is.EqualTo(LexSenseTags.kflidExtendedNote), "the owner class has it");
			Assert.That(DetailObjectInsertion.ResolveFlid(_sense1, "Senses", LexEntryTags.kClassId),
				Is.EqualTo(LexEntryTags.kflidSenses), "the owner class's field wins over the candidate's own");
			Assert.That(DetailObjectInsertion.ResolveFlid(_sense1, "Senses", 0), Is.EqualTo(LexSenseTags.kflidSenses));
			Assert.That(DetailObjectInsertion.ResolveFlid(_sense1, "Senses", LexEtymologyTags.kClassId),
				Is.EqualTo(LexSenseTags.kflidSenses), "an owner class without the field changes nothing");
		}

		[Test]
		public void OwnerOf_IsTheCandidate_OrItsOwnerWhenTheFieldIsTheOwners()
		{
			Assert.That(DetailObjectInsertion.OwnerOf(_sense1, LexSenseTags.kflidSenses), Is.SameAs(_sense1));
			Assert.That(DetailObjectInsertion.OwnerOf(_note1, LexSenseTags.kflidExtendedNote), Is.SameAs(_sense1));
			Assert.That(DetailObjectInsertion.OwnerOf(_sense1, LexEntryTags.kflidSenses), Is.SameAs(_entry));
			Assert.That(DetailObjectInsertion.OwnerOf(_note1, LexEntryTags.kflidEtymology), Is.Null,
				"a field of neither class has no owner here");
			Assert.That(DetailObjectInsertion.OwnerOf(_note1, 0), Is.Null);
		}

		// Insert Subsense from a row deep inside a sense (an example's translation): the
		// nearest enclosing sense takes it, at the end of its subsenses.
		[Test]
		public void ResolveTarget_FromADeepRow_ClimbsToTheNearestObjectOfTheOwnerClass()
		{
			ICmTranslation translation = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>()
					.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				translation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(_example, type);
			});

			var target = DetailObjectInsertion.ResolveTarget(translation, "Senses", "LexSense", "LexSense", false);

			Assert.That(target.Owner, Is.SameAs(_sense1));
			Assert.That(target.Flid, Is.EqualTo(LexSenseTags.kflidSenses));
			Assert.That(target.Position, Is.EqualTo(0));
		}

		[Test]
		public void ConcreteClass_IsTheClassItself_OrTheEntrysDefaultAllomorphForMoForm()
		{
			Assert.That(DetailObjectInsertion.ConcreteClass(_sense1, LexSenseTags.kClassId), Is.EqualTo(LexSenseTags.kClassId));
			Assert.That(DetailObjectInsertion.ConcreteClass(_entry, MoFormTags.kClassId),
				Is.EqualTo(_entry.GetDefaultClassForNewAllomorph()));
			Assert.That(DetailObjectInsertion.ConcreteClass(_entry, MoFormTags.kClassId), Is.EqualTo(MoStemAllomorphTags.kClassId),
				"a stem entry's allomorphs are stem allomorphs");
			Assert.That(DetailObjectInsertion.ConcreteClass(_sense1, MoFormTags.kClassId), Is.EqualTo(0),
				"only an entry knows its allomorph class");
			Assert.That(DetailObjectInsertion.ConcreteClass(_entry, CmObjectTags.kClassId), Is.EqualTo(0),
				"any other abstract class");
			Assert.That(DetailObjectInsertion.ConcreteClass(_entry, 0), Is.EqualTo(0));
		}

		[Test]
		public void IsOfferedOnEntry_AnAffixProcess_OnlyOnAnAffixEntry()
		{
			const string affixProcess = "CmdDataTree-Insert-AffixProcess";
			Assert.That(DetailObjectInsertion.IsOfferedOnEntry(_entry, affixProcess), Is.False, "a stem entry");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _entry.LexemeFormOA.MorphTypeRA =
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphSuffix));

			Assert.That(DetailObjectInsertion.IsOfferedOnEntry(_entry, affixProcess), Is.True, "a suffix entry");
		}

		[Test]
		public void IsOfferedOnEntry_AnyOtherInsert_OnAnyEntry()
		{
			const string allomorph = "CmdDataTree-Insert-AlternateForm";
			Assert.That(DetailObjectInsertion.IsOfferedOnEntry(_entry, allomorph), Is.True, "no allomorph yet");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				_entry.AlternateFormsOS.Add(Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create()));

			Assert.That(DetailObjectInsertion.IsOfferedOnEntry(_entry, allomorph), Is.True, "the first allomorph is an affix");
			Assert.That(DetailObjectInsertion.IsOfferedOnEntry(null, allomorph), Is.False);
		}

		[Test]
		public void IsVariantInsert_IsAnEntryInsertedIntoTheVirtualVariantBackReferences()
		{
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			var backRefs = mdc.GetFieldId2(LexEntryTags.kClassId, DetailObjectInsertion.VariantBackRefField, true);

			Assert.That(DetailObjectInsertion.IsVariantInsert("VariantFormEntryBackRefs", "LexEntry"), Is.True);
			Assert.That(DetailObjectInsertion.IsVariantInsert("VariantFormEntryBackRefs", "LexSense"), Is.False);
			Assert.That(DetailObjectInsertion.IsVariantInsert("Senses", "LexEntry"), Is.False);
			Assert.That(DetailObjectInsertion.IsVariantInsert(_entry, backRefs, LexEntryTags.kClassId), Is.True);
			Assert.That(DetailObjectInsertion.IsVariantInsert(_sense1, backRefs, LexEntryTags.kClassId), Is.False,
				"only an entry has variants");
			Assert.That(DetailObjectInsertion.IsVariantInsert(_entry, backRefs, LexSenseTags.kClassId), Is.False);
			Assert.That(DetailObjectInsertion.IsVariantInsert(_entry, LexEntryTags.kflidSenses, LexEntryTags.kClassId), Is.False);
			Assert.That(DetailObjectInsertion.IsVariantInsert(_entry, 0, LexEntryTags.kClassId), Is.False);
		}

		[Test]
		public void VariantInsertTarget_IsTheRowsEntry_ElseTheShownEntry()
		{
			const string field = DetailObjectInsertion.VariantBackRefField;
			ILexEntry other = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				other = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());
			try
			{
				Assert.That(DetailObjectInsertion.VariantInsertTarget(other, _entry, field, "LexEntry"), Is.SameAs(other),
					"the row's entry wins");
				Assert.That(DetailObjectInsertion.VariantInsertTarget(_sense1, _entry, field, "LexEntry"), Is.SameAs(_entry),
					"a row of another class: the shown entry");
				Assert.That(DetailObjectInsertion.VariantInsertTarget(_sense1, _sense2, field, "LexEntry"), Is.Null);
				Assert.That(DetailObjectInsertion.VariantInsertTarget(_entry, _entry, "Senses", "LexSense"), Is.Null,
					"no variant insert");
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => other.Delete());
			}
		}

		[Test]
		public void InsertionPosition_IsAfterTheNearestAnchor_ElseTheEnd_OrTheFieldKindsMarker()
		{
			Assert.That(DetailObjectInsertion.InsertionPosition(_entry, LexEntryTags.kflidSenses, new[] { _sense1.Hvo }),
				Is.EqualTo(1));
			Assert.That(DetailObjectInsertion.InsertionPosition(_entry, LexEntryTags.kflidSenses, new[] { _example.Hvo, _sense2.Hvo }),
				Is.EqualTo(2), "the first anchor that is an item wins");
			Assert.That(DetailObjectInsertion.InsertionPosition(_entry, LexEntryTags.kflidSenses, new[] { _example.Hvo }),
				Is.EqualTo(2), "no anchor among the items: the end");
			Assert.That(DetailObjectInsertion.InsertionPosition(_entry, LexEntryTags.kflidSenses, null), Is.EqualTo(2));
			Assert.That(DetailObjectInsertion.InsertionPosition(_example, LexExampleSentenceTags.kflidTranslations, null),
				Is.EqualTo(DetailObjectInsertion.CollectionPosition));
			Assert.That(DetailObjectInsertion.InsertionPosition(_entry, LexEntryTags.kflidLexemeForm, null),
				Is.EqualTo(DetailObjectInsertion.AtomicPosition));
		}

		[Test]
		public void ResolveTarget_ASubsense_GoesUnderTheSense_AtTheEnd()
		{
			var target = DetailObjectInsertion.ResolveTarget(_sense1, "Senses", "LexSense", "LexSense", false);

			Assert.That(target.Owner, Is.SameAs(_sense1));
			Assert.That(target.Flid, Is.EqualTo(LexSenseTags.kflidSenses));
			Assert.That(target.ClassId, Is.EqualTo(LexSenseTags.kClassId));
			Assert.That(target.Position, Is.EqualTo(0));
		}

		[Test]
		public void ResolveTarget_ASenseBesideTheRowsSense_GoesRightAfterIt()
		{
			var target = DetailObjectInsertion.ResolveTarget(_sense1, "Senses", "LexSense", null, insertAtOwner: true);

			Assert.That(target.Owner, Is.SameAs(_entry));
			Assert.That(target.Flid, Is.EqualTo(LexEntryTags.kflidSenses));
			Assert.That(target.Position, Is.EqualTo(1), "after the first sense, before the second");
		}

		[Test]
		public void ResolveTarget_FromAnItemWithoutTheField_GoesToItsOwner_AfterTheItem()
		{
			var note = DetailObjectInsertion.ResolveTarget(_note1, "ExtendedNote", "LexExtendedNote", null, false);
			Assert.That(note.Owner, Is.SameAs(_sense1));
			Assert.That(note.Flid, Is.EqualTo(LexSenseTags.kflidExtendedNote));
			Assert.That(note.Position, Is.EqualTo(1));

			var etymology = DetailObjectInsertion.ResolveTarget(_etymology2, "Etymology", "LexEtymology", "LexEntry", false);
			Assert.That(etymology.Owner, Is.SameAs(_entry));
			Assert.That(etymology.Flid, Is.EqualTo(LexEntryTags.kflidEtymology));
			Assert.That(etymology.Position, Is.EqualTo(2));
		}

		[Test]
		public void ResolveTarget_FromTheOwnerItself_GoesAtTheEnd()
		{
			var example = DetailObjectInsertion.ResolveTarget(_sense1, "Examples", "LexExampleSentence", null, false);
			Assert.That(example.Owner, Is.SameAs(_sense1));
			Assert.That(example.Position, Is.EqualTo(1));

			var firstNote = DetailObjectInsertion.ResolveTarget(_sense2, "ExtendedNote", "LexExtendedNote", null, false);
			Assert.That(firstNote.Owner, Is.SameAs(_sense2));
			Assert.That(firstNote.Position, Is.EqualTo(0), "the sense's first note");
		}

		[Test]
		public void ResolveTarget_AnEmptyAtomicField_TakesTheNewObject()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _entry.LexemeFormOA = null);

			var target = DetailObjectInsertion.ResolveTarget(_entry, "LexemeForm", "MoStemAllomorph", null, false);

			Assert.That(target.Owner, Is.SameAs(_entry));
			Assert.That(target.Flid, Is.EqualTo(LexEntryTags.kflidLexemeForm));
			Assert.That(target.Position, Is.EqualTo(DetailObjectInsertion.AtomicPosition));
		}

		[Test]
		public void ResolveTarget_IsNull_ForAReferenceField_AFilledAtomicField_AnUnknownClass_OrNoFieldUpTheChain()
		{
			Assert.That(DetailObjectInsertion.ResolveTarget(_entry, "VariantFormEntryBackRefs", "LexEntry", null, false), Is.Null,
				"a reference field takes no new object");
			Assert.That(_entry.LexemeFormOA, Is.Not.Null, "precondition: the entry has a lexeme form");
			Assert.That(DetailObjectInsertion.ResolveTarget(_entry, "LexemeForm", "MoStemAllomorph", null, false), Is.Null,
				"a filled atomic field: inserting would replace its object");
			Assert.That(DetailObjectInsertion.ResolveTarget(_sense1, "Senses", "NoSuchClass", null, false), Is.Null);
			Assert.That(DetailObjectInsertion.ResolveTarget(_etymology1, "ExtendedNote", "LexExtendedNote", null, false), Is.Null,
				"nothing from the etymology up to the entry has ExtendedNote");
			Assert.That(DetailObjectInsertion.ResolveTarget(null, "Senses", "LexSense", null, false), Is.Null);
		}
	}
}
