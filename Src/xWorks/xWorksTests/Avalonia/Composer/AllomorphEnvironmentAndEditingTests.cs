// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Environments row of an allomorph, plus the editing and refresh contracts the row
	/// shares
	/// with every other composed row.
	///
	/// Legacy edits environments as typed strings validated one row at a time, then reconciles
	/// them
	/// against the project's environment inventory. The Avalonia composer has no editor for that
	/// editor string, so the row currently falls through the reference-vector walker into one of
	/// two wrong shapes depending on whether the project owns any environments at all. Both
	/// shapes
	/// are covered here so a fixture without environments cannot pass for the wrong reason.
	/// </summary>
	[TestFixture]
	public class AllomorphEnvironmentAndEditingTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IMoStemAllomorph m_allomorph;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var lexemeForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = lexemeForm;
				lexemeForm.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("barigi", Cache.DefaultVernWs));

				m_allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(m_allomorph);
				m_allomorph.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("barigi-stem", Cache.DefaultVernWs));
			});
		}

		// The project owns no environments until a test asks for them, which is what separates
		// the
		// row's two wrong shapes.
		private void GiveProjectAnEnvironment(string representation)
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var phonData = Cache.LanguageProject.PhonologicalDataOA;
				var env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				phonData.EnvironmentsOS.Add(env);
				env.StringRepresentation = TsStringUtils.MakeString(
					representation, Cache.DefaultAnalWs);
			});
		}

		private DetailField ComposeEnvironmentsRow(bool showHiddenFields = true)
		{
			var fields = DetailComposer.Compose(m_entry, Cache, showHiddenFields).Model.Fields;
			return fields.FirstOrDefault(
				f => f.Field == "PhoneEnv" && f.ObjectHvo == m_allomorph.Hvo);
		}

		/// <summary>
		/// The row is the environment editor in BOTH data states. With no environments in the
		/// project it must not degrade to a blank read-only text row, and with environments
		/// present
		/// it must not become the generic reference-vector chooser: legacy types environment
		/// strings, it does not pick them from a list.
		/// </summary>
		[Test]
		public void Compose_Environments_UsesEnvironmentEditor_NotGenericReferenceVector()
		{
			var emptyProject = ComposeEnvironmentsRow();
			Assert.That(emptyProject, Is.Not.Null, "the allomorph composes an Environments row");
			Assert.That(emptyProject.Kind, Is.Not.EqualTo(DetailFieldKind.ReferenceVector),
				"environments are typed, not chosen from a possibility list");
			Assert.That(emptyProject.IsEditable, Is.True,
				"with no environments in the project the row still accepts typing; a blank read-only "
				+ "row is the fall-through, not the editor");

			GiveProjectAnEnvironment("/ _ a");

			var populatedProject = ComposeEnvironmentsRow();
			Assert.That(populatedProject, Is.Not.Null);
			Assert.That(populatedProject.Kind, Is.Not.EqualTo(DetailFieldKind.ReferenceVector),
				"once the project owns environments the row must not become the generic chooser: "
				+ "picking an existing environment is not the legacy interaction");
		}

		/// <summary>
		/// Editing an allomorph's form through the composed edit context commits as ONE undoable
		/// step on the global stack legacy views share, so a single Ctrl+Z restores it.
		/// </summary>
		[Test]
		public void Edit_AllomorphForm_CommitsAsOneUndoStep()
		{
			var composed = DetailComposer.Compose(m_entry, Cache);
			var formField = composed.Model.Fields.First(
				f => f.Field == "Form" && f.ObjectHvo == m_allomorph.Hvo);
			var ws = formField.Values.First(v => !string.IsNullOrEmpty(v.WsTag)).WsTag;

			Assert.That(composed.EditContext.TrySetText(formField, ws, "barigi-edited"), Is.True,
				"the allomorph's form row stages text like any other composed text row");
			composed.EditContext.Commit();

			Assert.That(AllomorphFormText, Is.EqualTo("barigi-edited"), "the edit reached the model");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);

			Cache.ActionHandlerAccessor.Undo();

			Assert.That(AllomorphFormText, Is.EqualTo("barigi-stem"),
				"ONE undo restores the allomorph's form; the edit is a single step");
		}

		private string AllomorphFormText
			=> m_allomorph.Form.get_String(Cache.DefaultVernWs).Text;

		/// <summary>
		/// An allomorph added outside the detail view reaches the view through the shared
		/// PropChanged bus, and the re-composed model gains its row -- rows do not live-update,
		/// the
		/// re-show does.
		/// </summary>
		[Test]
		public void Refresh_AllomorphSection_ReflectsExternalPropChanged()
		{
			var rowsBefore = CountAllomorphFormRows();
			var refreshes = 0;

			using (new AvaloniaDetailRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator()))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.AlternateFormsOS.Add(
						Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create()));

				Assert.That(refreshes, Is.GreaterThanOrEqualTo(1),
					"adding an allomorph elsewhere must signal the Avalonia view to re-show");
			}

			Assert.That(CountAllomorphFormRows(), Is.EqualTo(rowsBefore + 1),
				"the re-composed detail carries the externally added allomorph");
		}

		private int CountAllomorphFormRows()
			=> DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Count(f => f.Field == "Form" && f.ObjectHvo != m_entry.LexemeFormOA.Hvo);
	}
}
