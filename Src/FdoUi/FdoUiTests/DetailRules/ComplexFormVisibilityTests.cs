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
	/// The complex-form visibility marks: which components may carry one, and that toggling
	/// keeps a mark in component order as one undoable task.
	/// </summary>
	[TestFixture]
	public class ComplexFormVisibilityTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _first;
		private ILexSense _secondSense;
		private ILexEntry _third;
		private ILexEntry _complexForm;
		private ILexEntryRef _complexFormRef;

		// A complex form of three components in order: an entry, a sense of another entry, an
		// entry. No component is marked.
		[SetUp]
		public void MakeComplexForm()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var entries = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				_first = entries.Create();
				var second = entries.Create();
				_secondSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				second.SensesOS.Add(_secondSense);
				_third = entries.Create();
				_complexForm = entries.Create();
				_complexFormRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				_complexForm.EntryRefsOS.Add(_complexFormRef);
				_complexFormRef.RefType = LexEntryRefTags.krtComplexForm;
				_complexFormRef.ComponentLexemesRS.Add(_first);
				_complexFormRef.ComponentLexemesRS.Add(_secondSense);
				_complexFormRef.ComponentLexemesRS.Add(_third);
			});
		}

		[TearDown]
		public void DeleteComplexForm()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var entry in new[] { _complexForm, _first, _secondSense.Entry, _third })
				{
					if (entry.IsValidObject)
						entry.Delete();
				}
			});
		}

		[Test]
		public void CanShowSubentryUnderComponent_NeedsAComplexFormRef_AndAnEntryOrSense()
		{
			Assert.That(ComplexFormVisibility.CanShowSubentryUnderComponent(_complexFormRef, _first), Is.True);
			Assert.That(ComplexFormVisibility.CanShowSubentryUnderComponent(_complexFormRef, _secondSense), Is.True);
			Assert.That(ComplexFormVisibility.CanShowSubentryUnderComponent(_complexFormRef, _complexFormRef), Is.False,
				"only an entry or a sense can be a primary lexeme");
			Assert.That(ComplexFormVisibility.CanShowSubentryUnderComponent(null, _first), Is.False);
			Assert.That(ComplexFormVisibility.CanShowSubentryUnderComponent(MakeVariantRef(), _first), Is.False,
				"a variant has no subentry to show");
		}

		// A variant of the first entry, owned by a new entry; the ref type is fixed before the
		// component is added, as the model requires.
		private ILexEntryRef MakeVariantRef()
		{
			ILexEntryRef variantRef = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var variant = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				variantRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				variant.EntryRefsOS.Add(variantRef);
				variantRef.RefType = LexEntryRefTags.krtVariant;
				variantRef.ComponentLexemesRS.Add(_first);
			});
			return variantRef;
		}

		[Test]
		public void ToggleSubentryUnderComponent_KeepsThePrimaryLexemesInComponentOrder()
		{
			Toggle(_secondSense);
			Assert.That(_complexFormRef.PrimaryLexemesRS, Is.EqualTo(new ICmObject[] { _secondSense }));
			Assert.That(ComplexFormVisibility.ShowsSubentryUnderComponent(_complexFormRef, _secondSense), Is.True);
			Assert.That(ComplexFormVisibility.ShowsSubentryUnderComponent(_complexFormRef, _first), Is.False);

			Toggle(_third);
			Toggle(_first);
			Assert.That(_complexFormRef.PrimaryLexemesRS, Is.EqualTo(new ICmObject[] { _first, _secondSense, _third }),
				"a later mark is inserted where the component order puts it");

			Toggle(_secondSense);
			Assert.That(_complexFormRef.PrimaryLexemesRS, Is.EqualTo(new ICmObject[] { _first, _third }),
				"toggling a marked component unmarks it");
		}

		[Test]
		public void ToggleSubentryUnderComponent_LeavesAnUnlistedComponentUnmarked()
		{
			Toggle(_complexForm);
			Assert.That(_complexFormRef.PrimaryLexemesRS, Is.Empty);
		}

		[Test]
		public void ToggleSubentryUnderComponent_IsItsOwnUndoableTask()
		{
			ComplexFormVisibility.ToggleSubentryUnderComponent(_complexFormRef, _first, "Undo mark", "Redo mark");

			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo mark"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_complexFormRef.PrimaryLexemesRS, Is.Empty);
		}

		[Test]
		public void ComplexFormRefOf_IsTheComplexFormRef_OrNull()
		{
			Assert.That(ComplexFormVisibility.ComplexFormRefOf(_complexForm), Is.SameAs(_complexFormRef));
			Assert.That(ComplexFormVisibility.ComplexFormRefOf(_first), Is.Null, "a plain entry is no complex form");
			Assert.That(ComplexFormVisibility.ComplexFormRefOf(null), Is.Null);
			Assert.That(ComplexFormVisibility.ComplexFormRefOf(MakeVariantRef().OwningEntry), Is.Null,
				"a variant ref does not count");
		}

		[Test]
		public void ToggleShowComplexFormIn_KeepsTheShowInListInComponentOrder()
		{
			ComplexFormVisibility.ToggleShowComplexFormIn(_complexFormRef, _third, "Undo", "Redo");
			ComplexFormVisibility.ToggleShowComplexFormIn(_complexFormRef, _first, "Undo", "Redo");
			Assert.That(_complexFormRef.ShowComplexFormsInRS, Is.EqualTo(new ICmObject[] { _first, _third }));
			Assert.That(ComplexFormVisibility.ShowsComplexFormIn(_complexFormRef, _third), Is.True);
			Assert.That(ComplexFormVisibility.ShowsComplexFormIn(_complexFormRef, _secondSense), Is.False);

			ComplexFormVisibility.ToggleShowComplexFormIn(_complexFormRef, _first, "Undo", "Redo");
			Assert.That(_complexFormRef.ShowComplexFormsInRS.Single(), Is.SameAs(_third));
		}

		private void Toggle(ICmObject component)
			=> ComplexFormVisibility.ToggleSubentryUnderComponent(_complexFormRef, component, "Undo", "Redo");
	}
}
