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
	/// The variant back-reference rule: which rows act on another entry, which refs link a
	/// variant to the shown entry, and what Delete Reference removes.
	/// </summary>
	[TestFixture]
	public class VariantBackReferencesTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _root;
		private ILexEntry _variant;
		private ILexEntryRef _variantRef;
		private ILexEntryRef _unrelatedRef;

		// A root entry and a variant of it: the variant's ref lists the root as its component
		// and primary lexeme; a second ref of the variant points elsewhere.
		[SetUp]
		public void MakeEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var entries = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var forms = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
				var refs = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
				_root = entries.Create();
				_root.LexemeFormOA = forms.Create();
				_root.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
				_variant = entries.Create();
				_variant.LexemeFormOA = forms.Create();
				_variant.EntryRefsOS.Add(_variantRef = refs.Create());
				_variantRef.RefType = LexEntryRefTags.krtVariant;
				_variantRef.ComponentLexemesRS.Add(_root);
				_variantRef.PrimaryLexemesRS.Add(_root);
				var other = entries.Create();
				_variant.EntryRefsOS.Add(_unrelatedRef = refs.Create());
				_unrelatedRef.RefType = LexEntryRefTags.krtVariant;
				_unrelatedRef.ComponentLexemesRS.Add(other);
			});
		}

		[TearDown]
		public void DeleteEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var entry in Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToList())
					entry.Delete();
			});
		}

		[Test]
		public void BackReferencedEntry_IsTheOwnerOfARowObject_WhenThatIsAnotherEntry()
		{
			Assert.That(VariantBackReferences.BackReferencedEntry(_root, _variant.LexemeFormOA), Is.SameAs(_variant),
				"the variant's form row");
			Assert.That(VariantBackReferences.BackReferencedEntry(_root, _variantRef), Is.SameAs(_variant),
				"the variant's ref rows");
			Assert.That(VariantBackReferences.CommandTarget(_root, _variant.LexemeFormOA), Is.SameAs(_variant));
		}

		[Test]
		public void BackReferencedEntry_IsNull_ForTheRootsOwnRows_AndWithoutARootEntry()
		{
			Assert.That(VariantBackReferences.BackReferencedEntry(_root, _root.LexemeFormOA), Is.Null, "owned by the root");
			Assert.That(VariantBackReferences.BackReferencedEntry(_root, _root), Is.Null, "the root itself");
			Assert.That(VariantBackReferences.BackReferencedEntry(_root, _variant), Is.Null, "an entry has no owner");
			Assert.That(VariantBackReferences.BackReferencedEntry(_root.SensesOS[0], _variant.LexemeFormOA), Is.Null,
				"the root is no entry");
			Assert.That(VariantBackReferences.BackReferencedEntry(_root, null), Is.Null);
			Assert.That(VariantBackReferences.CommandTarget(_root, _root.LexemeFormOA), Is.SameAs(_root.LexemeFormOA));
		}

		[Test]
		public void ReferencesTo_ListsTheVariantsRefsNamingTheRoot()
		{
			Assert.That(VariantBackReferences.ReferencesTo(_root, _variant), Is.EqualTo(new[] { _variantRef }));
			Assert.That(VariantBackReferences.ReferencesTo(_variant, _root), Is.Empty, "the root has no refs");
			Assert.That(VariantBackReferences.ReferencesTo(null, _variant), Is.Empty);
		}

		[Test]
		public void DeleteReference_UnlinksTheRoot_AndRemovesTheRef_InOneUndoableStep()
		{
			VariantBackReferences.DeleteReference(_root, _variantRef);

			Assert.That(_variantRef.IsValidObject, Is.False, "the ref is gone");
			Assert.That(_variant.EntryRefsOS.ToList(), Is.EqualTo(new[] { _unrelatedRef }), "the other ref stays");
			Assert.That(_variant.IsValidObject, Is.True, "the variant entry stays");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Delete Reference"));

			Cache.ActionHandlerAccessor.Undo();

			Assert.That(_variant.EntryRefsOS, Has.Count.EqualTo(2), "one Undo restores the ref");
			Assert.That(_variant.EntryRefsOS[0].ComponentLexemesRS.ToList(), Is.EqualTo(new[] { _root }));
			Assert.That(_variant.EntryRefsOS[0].PrimaryLexemesRS.ToList(), Is.EqualTo(new[] { _root }));
		}
	}
}
