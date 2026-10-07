// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The environment leaves that run through the host: the natural-class insert, whose
	/// chooser the host opens, and Describe Error, whose explanation the host shows. The host
	/// is faked here because the real one opens modal dialogs.
	/// </summary>
	[TestFixture]
	public class EnvironmentMenuLeavesTests : MemoryOnlyBackendProviderTestBase
	{
		private sealed class FakeHost : IEnvironmentMenuHost
		{
			public IPhNaturalClass Chosen;
			public int ChooserOpened;
			public readonly List<string> Shown = new List<string>();

			public IPhNaturalClass ChooseNaturalClass()
			{
				ChooserOpened++;
				return Chosen;
			}

			// Text judged malformed, with its explanation; anything else is well formed.
			public readonly Dictionary<string, string> Errors = new Dictionary<string, string>();
			public readonly List<string> Judged = new List<string>();

			public string DescribeEnvironmentError(string text)
			{
				Judged.Add(text);
				return Errors.TryGetValue(text, out var message) ? message : null;
			}

			public void ShowEnvironmentError(string message) => Shown.Add(message);
		}

		private static DetailField Row(string validationMessage) => new DetailField(
			"MoStemAllomorph/x/#0", "Environments", "PhoneEnv", null,
			DetailFieldKind.ReferenceVector, EditorClassification.Known, "PhoneEnv", null,
			HostRouting.Inherit, null, null, null, isEditable: true,
			items: new List<DetailChoiceOption> { new DetailChoiceOption("e1", "/_", 0, validationMessage) });

		// The stub's defaults here are an item editor on "/_" with the caret after the slash.
		private static DetailMenuRequest Request(TextEditorStub editor)
		{
			editor.SelectedItemKey = "e1";
			editor.SelectedItemIndex = 0;
			if (editor.EditorText == null)
			{
				editor.EditorText = "/_";
				editor.Caret = 1;
			}
			return DetailMenuRequest.FromAnchor(null, Row(null), DetailMenuKind.ItemMenu, editor);
		}

		private IPhNaturalClass MakeNaturalClass(string abbreviation)
		{
			IPhNaturalClass naturalClass = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var created = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
				Cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS.Add(created);
				created.Abbreviation.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString(abbreviation, Cache.DefaultAnalWs));
				naturalClass = created;
			});
			return naturalClass;
		}

		[Test]
		public void InsertNaturalClass_TypesTheChosenClassAbbreviation_Bracketed_AtTheSnapshottedCaret()
		{
			var host = new FakeHost { Chosen = MakeNaturalClass("C") };
			var editor = new TextEditorStub { EditorText = "/_", Caret = 1 };

			var leaf = EnvironmentMenuLeaves.BuildInsert(EnvironmentMenuLeaves.InsertNaturalClassMessage,
				"Insert Natural Class", Request(editor), host);
			Assert.That(leaf.IsEnabled, Is.True, "an item goes after the slash");
			leaf.Execute();

			Assert.That(host.ChooserOpened, Is.EqualTo(1));
			Assert.That(editor.Typed, Is.EqualTo(new[] { (1, 1, "[C]", 0) }),
				"the abbreviation, not the name, in brackets, with the caret after it");
		}

		[Test]
		public void InsertNaturalClass_TypesNothing_WhenTheChooserIsCancelled()
		{
			var host = new FakeHost { Chosen = null };
			var editor = new TextEditorStub();

			EnvironmentMenuLeaves.BuildInsert(EnvironmentMenuLeaves.InsertNaturalClassMessage,
				"Insert Natural Class", Request(editor), host).Execute();

			Assert.That(host.ChooserOpened, Is.EqualTo(1));
			Assert.That(editor.Typed, Is.Empty);
		}

		[Test]
		public void InsertOptionalItem_LeavesTheCaretBetweenTheParentheses()
		{
			var editor = new TextEditorStub { EditorText = "/_", Caret = 2 };

			EnvironmentMenuLeaves.BuildInsert(EnvironmentMenuLeaves.InsertOptionalItemMessage,
				"Insert Optional Item", Request(editor), new FakeHost()).Execute();

			Assert.That(editor.Typed, Is.EqualTo(new[] { (2, 2, "()", 1) }));
		}

		[Test]
		public void BuildInsert_RejectsAMessageThatIsNotAnInsert()
		{
			Assert.That(() => EnvironmentMenuLeaves.BuildInsert(EnvironmentMenuLeaves.ShowErrorMessage, "x",
				Request(new TextEditorStub()), new FakeHost()), Throws.InvalidOperationException);
		}

		[Test]
		public void DescribeError_ShowsTheTextsExplanation_AndIsDisabledWithoutOne()
		{
			var host = new FakeHost();
			const string explanation = "There is a problem with this environment string '/#'";
			host.Errors["/#"] = explanation;

			var described = EnvironmentMenuLeaves.BuildDescribeError("Describe Error", "/#", host);
			Assert.That(described.IsEnabled, Is.True);
			described.Execute();
			Assert.That(host.Shown, Is.EqualTo(new[] { explanation }));

			var clean = EnvironmentMenuLeaves.BuildDescribeError("Describe Error", "/#_", host);
			Assert.That(clean.IsEnabled, Is.False);
			Assert.That(clean.Execute, Is.Null);

			host.Judged.Clear();
			Assert.That(EnvironmentMenuLeaves.BuildDescribeError("Describe Error", null, host).IsEnabled, Is.False);
			Assert.That(EnvironmentMenuLeaves.BuildDescribeError("Describe Error", "", host).IsEnabled, Is.False);
			Assert.That(host.Judged, Is.Empty, "no text, nothing to judge");
		}
	}
}
