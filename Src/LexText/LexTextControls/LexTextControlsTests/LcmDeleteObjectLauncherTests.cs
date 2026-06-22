// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the §19g delete-confirmation flow (<see cref="LcmDeleteObjectLauncher"/>):
	/// running the caller-supplied removal in ONE undoable step over a real LcmCache (via InternalsVisibleTo). The
	/// modal loop + CanDelete gate are desktop-only / covered by the headless DeleteConfirmationDialogTests; here
	/// we cover the delete core (RunDelete) — that it performs the removal, leaves it undoable, and is a no-op for
	/// a null action. The base opens an undoable UOW in TestSetup and RunDelete opens its own, so each test ends
	/// the base task first (mirrors LcmCreateFeatureLauncherTests).
	/// </summary>
	[TestFixture]
	public class LcmDeleteObjectLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexReference MakeReference()
		{
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var refTypeFactory = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			var refType = refTypeFactory.Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(refType);
			var lrFactory = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>();
			var lr = lrFactory.Create();
			refType.MembersOC.Add(lr);
			return lr;
		}

		[Test]
		public void RunDelete_RemovesObject_InOneUndoableStep()
		{
			// Build the reference inside the base's open undo task, THEN end it so RunDelete opens its own.
			var lr = MakeReference();
			m_actionHandler.EndUndoTask();
			var hvo = lr.Hvo;
			Assert.That(Cache.ServiceLocator.IsValidObjectId(hvo), Is.True);

			LcmDeleteObjectLauncher.RunDelete(Cache, "undo delete", "redo delete",
				() => Cache.DomainDataByFlid.DeleteObj(hvo));

			Assert.That(Cache.ServiceLocator.IsValidObjectId(hvo), Is.False, "the object is deleted");
			Assert.That(m_actionHandler.CanUndo(), Is.True, "the delete is one undoable step");

			m_actionHandler.Undo();
			Assert.That(Cache.ServiceLocator.IsValidObjectId(hvo), Is.True, "undo restores the object");
		}

		[Test]
		public void RunDelete_NullAction_IsNoOp()
		{
			m_actionHandler.EndUndoTask();
			Assert.DoesNotThrow(() => LcmDeleteObjectLauncher.RunDelete(Cache, "u", "r", null));
		}

		[Test]
		public void RunDelete_RemovesFromCollection()
		{
			// Create the entry + sense inside the base's open undo task; end it so RunDelete opens its own.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			m_actionHandler.EndUndoTask();
			Assert.That(entry.SensesOS.Contains(sense), Is.True);

			// Remove it the way a relation-delete call site would, inside the launcher's UOW.
			LcmDeleteObjectLauncher.RunDelete(Cache, "u", "r", () => entry.SensesOS.Remove(sense));
			Assert.That(entry.SensesOS.Contains(sense), Is.False, "the removal ran inside the undoable step");
		}
	}
}
