// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Characterization tests that lock down the CURRENT undo/redo transaction behavior for the
	/// editor-replacement candidate fields shown by <see cref="DataTree"/> (task 2.6 of
	/// lexical-edit-avalonia-migration). They assert on the LCModel source of truth and the action
	/// handler's undo/redo state, which is framework-neutral: the Avalonia editors must commit through
	/// the same fenced LCModel transactions (see avalonia-edit-sessions / avalonia-undo-redo) and
	/// therefore reproduce exactly these undo/redo results.
	///
	/// Pattern: the test base opens an ambient undo task during setup; we close it with
	/// <c>m_actionHandler.EndUndoTask()</c>, then make discrete undoable edits via
	/// <see cref="UndoableUnitOfWorkHelper.Do(string,string,SIL.LCModel.Infrastructure.IActionHandler,System.Action)"/>
	/// and exercise <c>Undo()</c>/<c>Redo()</c>.
	/// </summary>
	[TestFixture]
	public class DataTreeUndoRedoCharacterizationTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Inventory m_parts;
		private Inventory m_layouts;
		private ILexEntry m_entry;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private DataTree m_dtree;
		private Form m_parent;

		#region Fixture Setup and Teardown

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_layouts = DataTreeTests.GenerateLayouts();
			m_parts = DataTreeTests.GenerateParts();
		}

		#endregion

		#region Test Setup and Teardown

		public override void TestSetup()
		{
			base.TestSetup();

			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("original-cf", Cache.DefaultVernWs);
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("original-bib");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("original-bib");

			m_dtree = new DataTree();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			// Close the ambient setup undo task so the edits below are discrete, undoable units.
			m_actionHandler.EndUndoTask();
		}

		public override void TestTearDown()
		{
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
				m_parent = null;
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}

			base.TestTearDown();
		}

		#endregion

		private string CitationForm => m_entry.CitationForm.VernacularDefaultWritingSystem.Text;

		private string Bibliography => m_entry.Bibliography.AnalysisDefaultWritingSystem.Text;

		private void EditCitationForm(string value)
		{
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => m_entry.CitationForm.VernacularDefaultWritingSystem =
					TsStringUtils.MakeString(value, Cache.DefaultVernWs));
		}

		/// <summary>
		/// A multistring (CitationForm) edit can be undone back to the original value and redone to the
		/// edited value, and the action handler's CanUndo/CanRedo flags track the cycle.
		/// </summary>
		[Test]
		public void UndoRedo_CitationFormEdit_RevertsAndReplays()
		{
			Assert.That(CitationForm, Is.EqualTo("original-cf"));

			EditCitationForm("edited-cf");
			Assert.That(CitationForm, Is.EqualTo("edited-cf"));
			Assert.That(m_actionHandler.CanUndo(), Is.True);

			m_actionHandler.Undo();
			Assert.That(CitationForm, Is.EqualTo("original-cf"), "Undo should restore the original value.");
			Assert.That(m_actionHandler.CanRedo(), Is.True);

			m_actionHandler.Redo();
			Assert.That(CitationForm, Is.EqualTo("edited-cf"), "Redo should re-apply the edit.");
		}

		/// <summary>A multistring (Bibliography) edit undoes/redoes symmetrically.</summary>
		[Test]
		public void UndoRedo_BibliographyEdit_RevertsAndReplays()
		{
			Assert.That(Bibliography, Is.EqualTo("original-bib"));

			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => m_entry.Bibliography.SetAnalysisDefaultWritingSystem("edited-bib"));
			Assert.That(Bibliography, Is.EqualTo("edited-bib"));

			m_actionHandler.Undo();
			Assert.That(Bibliography, Is.EqualTo("original-bib"));

			m_actionHandler.Redo();
			Assert.That(Bibliography, Is.EqualTo("edited-bib"));
		}

		/// <summary>
		/// Two field edits made inside a single undoable unit collapse into one undo step: a single
		/// Undo reverts both fields together.
		/// </summary>
		[Test]
		public void UndoRedo_TwoFieldsInOneTask_FormSingleUndoStep()
		{
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler, () =>
			{
				m_entry.CitationForm.VernacularDefaultWritingSystem =
					TsStringUtils.MakeString("cf2", Cache.DefaultVernWs);
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("bib2");
			});
			Assert.That(CitationForm, Is.EqualTo("cf2"));
			Assert.That(Bibliography, Is.EqualTo("bib2"));

			// A single undo reverts BOTH fields (one transaction).
			m_actionHandler.Undo();
			Assert.That(CitationForm, Is.EqualTo("original-cf"));
			Assert.That(Bibliography, Is.EqualTo("original-bib"));
		}

		/// <summary>
		/// After an edit is undone, re-showing the object rebuilds the slice tree from the (reverted)
		/// model, so the realized slice reflects the undone value. This characterizes that the visible
		/// slice tracks the LCModel source of truth across undo.
		/// </summary>
		[Test]
		public void Undo_ThenReshow_SliceReflectsRevertedValue()
		{
			EditCitationForm("edited-cf");
			m_actionHandler.Undo();

			// Rebuild the slice tree from the current (reverted) model state.
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			var cfSlice = (Slice)m_dtree.Controls[0];
			Assert.That(cfSlice.Label, Is.EqualTo("CitationForm"));
			Assert.That(CitationForm, Is.EqualTo("original-cf"),
				"After undo, the model (and therefore a freshly built slice) shows the original value.");
		}

		/// <summary>Consecutive distinct edits form distinct undo steps, undone in reverse order.</summary>
		[Test]
		public void UndoRedo_ConsecutiveEdits_AreDistinctSteps()
		{
			EditCitationForm("v1");
			EditCitationForm("v2");
			Assert.That(CitationForm, Is.EqualTo("v2"));

			m_actionHandler.Undo();
			Assert.That(CitationForm, Is.EqualTo("v1"), "First undo reverts the most recent edit.");

			m_actionHandler.Undo();
			Assert.That(CitationForm, Is.EqualTo("original-cf"), "Second undo reverts the earlier edit.");
		}
	}
}
