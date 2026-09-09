// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
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
	/// shares with every other composed row.
	///
	/// Legacy's row is BOTH: PhoneEnvReferenceLauncher opens a SimpleListChooser over the
	/// project's existing environments, and its inline PhoneEnvReferenceView lets the user type a
	/// new environment string that ConnectToRealCache reconciles into the project
	/// (find-or-create, matching with spaces stripped). So the Avalonia row composes as an
	/// ordinary reference vector -- the chooser half -- whose edit context also offers
	/// IReferenceItemCreation.
	///
	/// Both data states are covered: the empty project and the populated one compose through
	/// different branches, so a fixture without environments can pass for the wrong reason.
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

		public override void TestTearDown()
		{
			// Clear the WHOLE inventory: create-on-type means the code under test mints
			// environments too, and NonUndoableUnitOfWorkHelper bypasses the base UndoAll.
			var inventory = Cache.LanguageProject.PhonologicalDataOA?.EnvironmentsOS;
			if (inventory != null && inventory.Count > 0)
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					foreach (var env in inventory.ToList())
						env.Delete();
				});
			}
			base.TestTearDown();
		}

		private int EnvironmentCount
			=> Cache.LanguageProject.PhonologicalDataOA?.EnvironmentsOS.Count ?? 0;

		private void GiveProjectAnEnvironment(string representation)
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var phonData = Cache.LanguageProject.PhonologicalDataOA;
				var env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				phonData.EnvironmentsOS.Add(env);
				// Vernacular, as legacy types them -- seeding analysis would make the wrong
				// writing system look normal here.
				env.StringRepresentation = TsStringUtils.MakeString(
					representation, Cache.DefaultVernWs);
			});
		}

		private DetailField ComposeEnvironmentsRow(bool showHiddenFields = true)
		{
			var fields = DetailComposer.Compose(m_entry, Cache, showHiddenFields).Model.Fields;
			return fields.FirstOrDefault(
				f => f.Field == "PhoneEnv" && f.ObjectHvo == m_allomorph.Hvo);
		}

		/// <summary>
		/// The row is a chooser AND create-capable, in BOTH data states.
		///
		/// Legacy's PhoneEnvReferenceLauncher opens a SimpleListChooser over the existing
		/// environments, so the chooser IS parity rather than a stand-in for it. What a chooser
		/// alone cannot give is typing a NEW environment string, which ConnectToRealCache
		/// reconciles into the project. The row has to be both: asserting it is not a chooser
		/// would drive an implementation that drops half of what legacy does.
		/// </summary>
		[Test]
		public void Compose_Environments_IsChooserRowSupportingCreate()
		{
			var empty = ComposeEnvironments();
			Assert.That(empty.Row, Is.Not.Null, "the allomorph composes an Environments row");
			Assert.That(empty.Row.Kind, Is.EqualTo(DetailFieldKind.ReferenceVector),
				"with NO environments in the project the row must still be the chooser row: the "
				+ "read-only fall-through would leave the user nothing to do in the very state "
				+ "where they need to type the project's first environment");
			Assert.That(empty.Row.IsEditable, Is.True);
			Assert.That(empty.Creation, Is.Not.Null,
				"the composed context offers the create sub-capability");
			Assert.That(empty.Creation.CanCreateReferenceItem(empty.Row), Is.True,
				"which is what makes the picker show its create row");

			GiveProjectAnEnvironment("/ # _");

			var populated = ComposeEnvironments();
			Assert.That(populated.Row.Kind, Is.EqualTo(DetailFieldKind.ReferenceVector),
				"and once the project owns environments it is the same chooser row");
			Assert.That(populated.Creation.CanCreateReferenceItem(populated.Row), Is.True,
				"still create-capable -- picking from the list never replaces typing a new one");
			Assert.That(populated.Row.Options, Is.Not.Empty,
				"the existing environment is offered as a candidate to pick");
		}

		/// <summary>
		/// A typed string find-or-creates: matching an existing environment attaches THAT one
		/// rather than minting a duplicate, and the match strips spaces exactly as legacy's
		/// ConnectToRealCache does, so "/ # _" and "/#_" are one environment.
		/// </summary>
		[Test]
		public void Environments_CreateFromTypedText_FindsExisting_MatchingWithSpacesStripped()
		{
			GiveProjectAnEnvironment("/ # _");
			var composed = ComposeEnvironments();
			var before = EnvironmentCount;

			Assert.That(composed.Creation.TryCreateAndAddReferenceItem(composed.Row, "/#_"), Is.True,
				"the spacing differs but the environment does not");
			composed.Context.Commit();

			Assert.That(EnvironmentCount, Is.EqualTo(before),
				"matched the existing environment with spaces stripped; no duplicate was created");
			Assert.That(m_allomorph.PhoneEnvRC.Count, Is.EqualTo(1),
				"and it was attached to the allomorph");
		}

		/// <summary>A string the project does not have yet is created and attached.</summary>
		[Test]
		public void Environments_CreateFromTypedText_CreatesWhenTheProjectHasNoMatch()
		{
			var composed = ComposeEnvironments();

			Assert.That(composed.Creation.TryCreateAndAddReferenceItem(composed.Row, "/ # _"), Is.True,
				"an empty project must still accept the first environment the user types");
			composed.Context.Commit();

			Assert.That(EnvironmentCount, Is.EqualTo(1), "the environment was minted");
			Assert.That(m_allomorph.PhoneEnvRC.Count, Is.EqualTo(1));
			Assert.That(m_allomorph.PhoneEnvRC.First().StringRepresentation.Text,
				Is.EqualTo("/ # _"), "stored as typed, spaces and all");
		}

		/// <summary>
		/// A created environment carries the VERNACULAR writing system, as legacy's
		/// PhoneEnvReferenceView does (m_wsVern). An environment string is phonology in the
		/// vernacular script.
		///
		/// The wrong writing system is invisible in a view that renders every reference-vector
		/// item in one font. It shows up in a view that honours each string's own writing system,
		/// where two identical-looking environments draw with different fonts.
		/// </summary>
		[Test]
		public void Environments_CreateFromTypedText_TagsTheStringVernacular()
		{
			var composed = ComposeEnvironments();

			Assert.That(composed.Creation.TryCreateAndAddReferenceItem(composed.Row, "/ # _"), Is.True);
			composed.Context.Commit();

			var created = m_allomorph.PhoneEnvRC.First().StringRepresentation;
			Assert.That(created.get_WritingSystem(0), Is.EqualTo(Cache.DefaultVernWs),
				"the analysis writing system gives the string the wrong font wherever a view "
				+ "renders its own writing system, which is how this was spotted");
			Assert.That(created.get_WritingSystem(0), Is.Not.EqualTo(Cache.DefaultAnalWs));
		}

		/// <summary>
		/// A malformed string is still created and attached. Legacy's ConnectToRealCache applies
		/// no validity filter when minting -- CheckConstraints only drives the squiggly line --
		/// so rejecting it here would silently discard what the user typed.
		/// </summary>
		[Test]
		public void Environments_CreateFromMalformedText_StillCreates_LikeLegacy()
		{
			var composed = ComposeEnvironments();

			Assert.That(composed.Creation.TryCreateAndAddReferenceItem(composed.Row, "/ _ ["), Is.True,
				"legacy stores a malformed environment and annotates it; it does not refuse it");
			composed.Context.Commit();

			Assert.That(m_allomorph.PhoneEnvRC.Count, Is.EqualTo(1),
				"the user's text survived rather than being dropped on the floor");
		}

		/// <summary>
		/// The composed item carries the DOMAIN's verdict, from the same CheckConstraints legacy
		/// runs for its squiggly -- so the two views cannot disagree about what is invalid.
		/// Composing must not write: CheckConstraints is called with createAnnotation false.
		/// </summary>
		[Test]
		public void Compose_Environments_AnnotatesTheItem_WhenTheDomainReportsItInvalid()
		{
			var composed = ComposeEnvironments();
			Assert.That(composed.Creation.TryCreateAndAddReferenceItem(composed.Row, "/ _ ["), Is.True);
			Assert.That(composed.Creation.TryCreateAndAddReferenceItem(composed.Row, "/ # _"), Is.True);
			composed.Context.Commit();

			var row = ComposeEnvironmentsRow();
			Assert.That(row.Items, Has.Count.EqualTo(2), "both are attached; neither is filtered");

			var bad = row.Items.Single(i => i.Name.Contains("["));
			var good = row.Items.Single(i => !i.Name.Contains("["));
			Assert.That(bad.HasValidationMessage, Is.True,
				"the malformed environment is annotated rather than rejected");
			Assert.That(bad.ValidationMessage, Is.Not.Empty,
				"and carries the domain's explanation for the tooltip");
			Assert.That(good.HasValidationMessage, Is.False,
				"the well-formed sibling is clean -- the check discriminates");
		}

		/// <summary>
		/// Composing is a READ. CheckConstraints can mint a ConstraintFailure annotation and
		/// adjust the stored string, so calling it the wrong way would have compose writing to
		/// the model.
		/// </summary>
		[Test]
		public void Compose_Environments_DoesNotWrite_WhenAnnotatingAnInvalidItem()
		{
			var composed = ComposeEnvironments();
			composed.Creation.TryCreateAndAddReferenceItem(composed.Row, "/ _ [");
			composed.Context.Commit();

			// NOT the undo count: CheckConstraints writes NON-undoably, so that probe would pass
			// even if composing wrote. These are what it actually touches.
			var annotationsBefore = Cache.LanguageProject.AnnotationsOC.Count;
			var stringBefore = m_allomorph.PhoneEnvRC.First().StringRepresentation.Text;

			var row = ComposeEnvironmentsRow();
			Assert.That(row.Items.Any(i => i.HasValidationMessage), Is.True,
				"the verdict was computed");
			Assert.That(Cache.LanguageProject.AnnotationsOC.Count, Is.EqualTo(annotationsBefore),
				"composing minted no ConstraintFailure annotation");
			Assert.That(m_allomorph.PhoneEnvRC.First().StringRepresentation.Text,
				Is.EqualTo(stringBefore),
				"and did not rewrite the stored string to carry a squiggly");
		}

		private struct ComposedEnvironments
		{
			public DetailField Row;
			public IDetailEditContext Context;
			public IReferenceItemCreation Creation;
		}

		private ComposedEnvironments ComposeEnvironments()
		{
			var composed = DetailComposer.Compose(m_entry, Cache, true);
			return new ComposedEnvironments
			{
				Row = composed.Model.Fields.FirstOrDefault(
					f => f.Field == "PhoneEnv" && f.ObjectHvo == m_allomorph.Hvo),
				Context = composed.EditContext,
				Creation = composed.EditContext as IReferenceItemCreation
			};
		}

		/// <summary>
		/// The Environments row keeps the jump its layout authors -- "Edit the Environments",
		/// targeting the Grammar area's EnvironmentEdit tool. With a valid environment already in
		/// the project, PhoneEnv yields a reference-target candidate and the row composes through
		/// AddGenericReferenceVector, which carries authored chooser links.
		/// </summary>
		[Test]
		public void Compose_Environments_KeepsTheAuthoredEditEnvironmentsLink()
		{
			GiveProjectAnEnvironment("/ # _");

			var row = ComposeEnvironmentsRow();

			Assert.That(row, Is.Not.Null, "the allomorph composes an Environments row");
			Assert.That(row.ChooserLinks, Is.Not.Empty,
				"the layout authors a goto chooserLink for this field; composing must carry it. "
				+ $"Composed kind: {row.Kind}, editable: {row.IsEditable}");
			Assert.That(row.ChooserLinks[0].Tool, Is.EqualTo("EnvironmentEdit"),
				"the jump targets the Grammar area's Environments tool");
		}

		/// <summary>
		/// A project with NO environments keeps the jump too. That is the state where the
		/// Environments tool is most needed and least reachable: a row with no chooser links
		/// leaves no way to get there.
		/// </summary>
		[Test]
		public void Compose_Environments_KeepsTheJump_WhenTheProjectHasNoEnvironments()
		{
			var row = ComposeEnvironmentsRow();

			Assert.That(row, Is.Not.Null);
			Assert.That(row.ChooserLinks, Is.Not.Empty,
				"an empty project is exactly when the user most needs the Environments tool");
			Assert.That(row.ChooserLinks[0].Tool, Is.EqualTo("EnvironmentEdit"));
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
		/// the re-show does.
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
