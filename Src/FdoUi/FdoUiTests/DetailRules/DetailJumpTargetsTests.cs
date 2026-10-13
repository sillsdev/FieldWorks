// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The jump rule of a detail row: which object each tool's jump shows for the row's
	/// object and field, and the notebook record a text's jump creates.
	/// </summary>
	[TestFixture]
	public class DetailJumpTargetsTests : MemoryOnlyBackendProviderTestBase
	{
		private static readonly Guid Fallback = Guid.NewGuid();

		private ILexEntry _entry;
		private ILexSense _sense;
		private ILexEntry _other;
		private IText _text;

		// Two entries with lexeme forms and a sense each, and a text with no notebook record.
		[SetUp]
		public void MakeObjects()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = MakeEntry();
				_sense = _entry.SensesOS[0];
				_other = MakeEntry();
				_text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			});
		}

		private ILexEntry MakeEntry()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
			return entry;
		}

		private static Guid Concordance(string className, ICmObject root, ICmObject target, int? flid,
			string commandId = "CmdJumpToConcordance")
			=> DetailJumpTargets.Resolve(DetailJumpTargets.ConcordanceTool, className, commandId, Fallback, root, target, flid).Guid;

		[Test]
		public void Concordance_TheRootEntryCommand_TargetsTheRoot_WhateverTheRow()
		{
			Assert.That(Concordance("LexEntry", _entry, _sense, LexSenseTags.kflidGloss,
				DetailJumpTargets.RootEntryConcordanceCommandId), Is.EqualTo(_entry.Guid));
			Assert.That(Concordance("LexEntry", _entry, _entry, null,
				DetailJumpTargets.RootEntryConcordanceCommandId), Is.EqualTo(_entry.Guid), "no current row");
		}

		[Test]
		public void Concordance_AnEntry_IsTheRowsEntry_OrTheOneOwningTheRowsObject()
		{
			Assert.That(Concordance("LexEntry", _entry, _entry, LexEntryTags.kflidCitationForm), Is.EqualTo(_entry.Guid));
			Assert.That(Concordance("LexEntry", _entry, _entry.LexemeFormOA, MoFormTags.kflidForm), Is.EqualTo(_entry.Guid),
				"a form row jumps to the entry owning the form");
			Assert.That(Concordance("LexEntry", _entry, _other.LexemeFormOA, MoFormTags.kflidForm), Is.EqualTo(_other.Guid),
				"another entry's form row jumps to that entry");
			Assert.That(Concordance("LexEntry", _entry, _entry, null), Is.EqualTo(_entry.Guid), "a header row has no field");
		}

		[Test]
		public void Concordance_AnEntry_IsNotOffered_OnAnEntryRefRow_OrWhenTheRootIsNoEntry()
		{
			ILexEntryRef entryRef = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				_entry.EntryRefsOS.Add(entryRef);
			});

			Assert.That(Concordance("LexEntry", _entry, entryRef, LexEntryRefTags.kflidComponentLexemes), Is.EqualTo(Fallback));
			Assert.That(Concordance("LexEntry", _sense, _entry, LexEntryTags.kflidCitationForm), Is.EqualTo(Fallback),
				"the rule needs an entry as root");
		}

		[Test]
		public void Concordance_ASense_OnlyWhenItIsTheRootsOwn()
		{
			Assert.That(Concordance("LexSense", _entry, _sense, LexSenseTags.kflidGloss), Is.EqualTo(_sense.Guid));
			Assert.That(Concordance("LexSense", _entry, _other.SensesOS[0], LexSenseTags.kflidGloss), Is.EqualTo(Fallback));
			Assert.That(Concordance("LexSense", _entry, _entry, LexEntryTags.kflidCitationForm), Is.EqualTo(Fallback));
		}

		[Test]
		public void Concordance_AForm_OnlyFromTheFormFieldItself()
		{
			var form = _entry.LexemeFormOA;
			Assert.That(Concordance("MoForm", _entry, form, MoFormTags.kflidForm), Is.EqualTo(form.Guid));
			Assert.That(Concordance("MoForm", _entry, form, MoFormTags.kflidIsAbstract), Is.EqualTo(Fallback),
				"a sibling row on the same form does not show the form");
			Assert.That(Concordance("MoForm", _entry, form, null), Is.EqualTo(Fallback), "a header row shows no field");
			Assert.That(Concordance("MoForm", _entry, _entry, LexEntryTags.kflidCitationForm), Is.EqualTo(Fallback));
		}

		[Test]
		public void Concordance_DeclinesARow_WhoseFieldIsUnknownOrVirtual()
		{
			var virtualFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "VariantFormEntryBackRefs", false);
			Assert.That(Cache.MetaDataCacheAccessor.get_IsVirtual(virtualFlid), Is.True, "precondition");

			Assert.That(Concordance("LexEntry", _entry, _entry, 0), Is.EqualTo(Fallback));
			Assert.That(Concordance("LexEntry", _entry, _entry, virtualFlid), Is.EqualTo(Fallback));
		}

		[Test]
		public void LexiconEdit_TargetsTheEntryOwningTheRowsObject_UnlessThatIsTheRoot()
		{
			Guid Lexicon(ICmObject target) => DetailJumpTargets.Resolve(DetailJumpTargets.LexiconEditTool, "LexEntry",
				"CmdEntryJumpToDefault", Fallback, _entry, target, MoFormTags.kflidForm).Guid;

			Assert.That(Lexicon(_other.LexemeFormOA), Is.EqualTo(_other.Guid), "a variant's form row jumps to the variant");
			Assert.That(Lexicon(_entry.LexemeFormOA), Is.EqualTo(Fallback), "the root's own form offers no jump");
			Assert.That(Lexicon(_other.SensesOS[0]), Is.EqualTo(_other.Guid));
			Assert.That(Lexicon(_other), Is.EqualTo(Fallback), "an entry row is owned by no entry");
		}

		[Test]
		public void NotebookEdit_TargetsTheRecordOwningTheRowsObject_OrTheTextsRecord()
		{
			DetailJumpTarget Notebook(ICmObject target) => DetailJumpTargets.Resolve(DetailJumpTargets.NotebookEditTool,
				"RnGenericRec", "CmdJumpToNotebook", Fallback, _text, target, null);

			var before = Notebook(_text);
			Assert.That(before.Guid, Is.EqualTo(_text.Guid), "a text without a record is offered on its own guid");
			Assert.That(before.TextWithoutRecord, Is.SameAs(_text), "and the jump must create the record first");

			// The association runs its own unit of work, as it does from a jump.
			var record = DetailJumpTargets.AssociateWithNotebook(_text);
			Assert.That(record, Is.Not.Null.And.SameAs(_text.AssociatedNotebookRecord), "the record is created and associated");
			IStText description = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				description = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				record.DescriptionOA = description;
			});

			var after = Notebook(_text);
			Assert.That(after.Guid, Is.EqualTo(record.Guid));
			Assert.That(after.TextWithoutRecord, Is.Null);
			Assert.That(Notebook(description).Guid, Is.EqualTo(record.Guid), "a row of something the record owns");
			Assert.That(Notebook(_entry).Guid, Is.EqualTo(Fallback));
		}

		[Test]
		public void InterlinearEdit_TargetsATextRow_Only()
		{
			Guid Interlinear(ICmObject target) => DetailJumpTargets.Resolve(DetailJumpTargets.InterlinearEditTool, "Text",
				"CmdJumpToText", Fallback, _text, target, null).Guid;

			Assert.That(Interlinear(_text), Is.EqualTo(_text.Guid));
			Assert.That(Interlinear(_entry), Is.EqualTo(Fallback));
		}

		[Test]
		public void NoRowObject_OrAnUnknownTool_FallsBack()
		{
			Assert.That(DetailJumpTargets.Resolve(DetailJumpTargets.ConcordanceTool, "LexEntry", "CmdEntryJumpToConcordance",
				Fallback, _entry, null, null).Guid, Is.EqualTo(Fallback));
			Assert.That(DetailJumpTargets.Resolve("publicationsEdit", "ICmPossibility", "CmdPublicationsJumpToDefault",
				Guid.Empty, _entry, _entry, null).Guid, Is.EqualTo(Guid.Empty));
			Assert.That(() => DetailJumpTargets.AssociateWithNotebook(null), Throws.ArgumentNullException);
		}
	}
}
