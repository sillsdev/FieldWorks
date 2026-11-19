// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The current row, the row of the shown record that last had keyboard focus: the view
	/// reports each row that takes focus, and the row is found again in every rebuilt view.
	/// </summary>
	[TestFixture]
	public class DetailCurrentRowTests
	{
		[AvaloniaTest]
		public void TheView_ReportsTheRowAnEditorOrItsMenuButtonFocuses()
		{
			var view = new DataTree(Model(formHvo: 8), menuRequested: request => { });
			var window = new Window { Content = view, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var reported = new List<string>();
			view.RowFocused += row => reported.Add(row.RowAutomationId);

			Find<TextBox>(view, "LexEntry/identity/#1@3.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			Find<Control>(view, "LexEntry/identity/#0@8.FieldMenu").Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(reported, Is.EqualTo(new[] { "LexEntry/identity/#1@3", "LexEntry/identity/#0@8" }));
		}

		[Test]
		public void TheCurrentRow_IsFoundAgainInARebuiltView_ByItsIdElseItsObject()
		{
			var currentRow = new DetailCurrentRow();
			Assert.That(currentRow.ResolveIn(Model(formHvo: 8)), Is.Null, "no row has had focus");

			currentRow.Remember(Model(formHvo: 8).Fields[1]);
			var rebuilt = Model(formHvo: 8);
			Assert.That(currentRow.ResolveIn(rebuilt), Is.SameAs(rebuilt.Fields[1]), "the same row of the rebuilt view");

			currentRow.Remember(Model(formHvo: 8).Fields[0]);
			var withAnotherRowId = new DetailModel("LexEntry", "identity", new List<DetailField>
			{
				Row("LexEntry/identity/#2@8", "Form", 8)
			}, new List<ViewDiagnostic>());
			Assert.That(currentRow.ResolveIn(withAnotherRowId), Is.SameAs(withAnotherRowId.Fields[0]), "the object's first row");
			Assert.That(currentRow.ResolveIn(Model(formHvo: 9)), Is.Null, "neither the row nor its object is shown");

			currentRow.Forget();
			Assert.That(currentRow.ResolveIn(Model(formHvo: 8)), Is.Null);
		}

		// A Lexeme Form row with a field menu, of the form with the given hvo; then a Gloss
		// row of object 3.
		private static DetailModel Model(int formHvo)
			=> new DetailModel("LexEntry", "identity", new List<DetailField>
			{
				Row("LexEntry/identity/#0@" + formHvo, "Form", formHvo, "mnuDataTree-LexemeForm"),
				Row("LexEntry/identity/#1@3", "Gloss", 3)
			}, new List<ViewDiagnostic>());

		private static DetailField Row(string stableId, string field, int objectHvo, string menuId = null)
			=> new DetailField(stableId, field, field, "vernacular", DetailFieldKind.Text, EditorClassification.Known, null, null,
				HostRouting.Product, new List<DetailWsValue> { new DetailWsValue("vern", "casa") }, null, null, menuId: menuId,
				objectHvo: objectHvo);

		private static T Find<T>(Control root, string automationId) where T : Control
			=> root.GetVisualDescendants().OfType<T>().First(c => AutomationProperties.GetAutomationId(c) == automationId);
	}
}
