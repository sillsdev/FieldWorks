// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Lexeme Form conversions: each adds an entry ref of the unspecified type in one
	/// undoable step, and an entry becomes a complex form only once.
	/// </summary>
	[TestFixture]
	public class EntryConversionTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;

		// A root entry with one variant ref.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var services = Cache.ServiceLocator;
				_entry = services.GetInstance<ILexEntryFactory>().Create();
				var form = services.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.LexemeFormOA = form;
				form.MorphTypeRA = services.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot);
				var variantRef = services.GetInstance<ILexEntryRefFactory>().Create();
				_entry.EntryRefsOS.Add(variantRef);
				variantRef.RefType = LexEntryRefTags.krtVariant;
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
		public void ConvertIntoComplexForm_AddsTheFirstRef_AndMakesARootAStem_InOneUndoableStep()
		{
			Assert.That(EntryConversion.CanConvertIntoComplexForm(_entry), Is.True);

			var entryRef = EntryConversion.ConvertIntoComplexForm(_entry, "Undo convert", "Redo convert");

			Assert.That(_entry.EntryRefsOS.First(), Is.SameAs(entryRef));
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(entryRef.ComplexEntryTypesRS.Single().Guid, Is.EqualTo(LexEntryTypeTags.kguidLexTypeUnspecifiedComplexForm));
			Assert.That(entryRef.HideMinorEntry, Is.EqualTo(0));
			Assert.That(((IMoStemAllomorph)_entry.LexemeFormOA).MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphStem));
			Assert.That(EntryConversion.CanConvertIntoComplexForm(_entry), Is.False, "an entry becomes a complex form once");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo convert"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_entry.EntryRefsOS, Has.Count.EqualTo(1));
		}

		[Test]
		public void ConvertIntoVariant_AddsTheLastRef_AsOftenAsAsked()
		{
			var first = EntryConversion.ConvertIntoVariant(_entry, "Undo", "Redo");
			var second = EntryConversion.ConvertIntoVariant(_entry, "Undo", "Redo");

			Assert.That(_entry.EntryRefsOS.Skip(1), Is.EqualTo(new[] { first, second }));
			Assert.That(second.RefType, Is.EqualTo(LexEntryRefTags.krtVariant));
			Assert.That(second.VariantEntryTypesRS.Single().Guid, Is.EqualTo(LexEntryTypeTags.kguidLexTypeUnspecifiedVar));
			Assert.That(EntryConversion.CanConvertIntoComplexForm(_entry), Is.True, "variant refs do not count");
		}

		[Test]
		public void CanConvertIntoComplexForm_IsFalse_ForNoEntry()
		{
			Assert.That(EntryConversion.CanConvertIntoComplexForm(null), Is.False);
		}
	}
}
