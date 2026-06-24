// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using FwAvaloniaTests.VisualChecks;
using FwAvaloniaDialogsTests;

namespace FwAvaloniaTests
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 3.2) — the environment editor control: commit a valid string,
	/// reject an invalid one (restore the last committed value + show the error). PNG + AssertNoCrowding.
	/// </summary>
	[TestFixture]
	public class PhEnvironmentEditorTests
	{
		private sealed class FakeSink : IPhEnvironmentCommandSink
		{
			public string LastCommitted;
			// "valid" = anything that is non-empty and balanced-ish; the real recognizer is tested in xWorks.
			public bool Validate(string representation) => !(representation ?? "").Contains("X");
			public bool Commit(string representation) { LastCommitted = representation; return true; }
		}

		private static (PhEnvironmentEditor Editor, Window Window, FakeSink Sink) Show(string initial)
		{
			var sink = new FakeSink();
			var editor = new PhEnvironmentEditor(initial, sink);
			var window = new Window { Content = editor, Width = 360, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (editor, window, sink);
		}

		private static TextBox Box(Control root) =>
			root.GetVisualDescendants().OfType<TextBox>().First();

		[AvaloniaTest]
		public void ValidEdit_Commits()
		{
			var (editor, window, sink) = Show("/ _ #");
			DialogSnapshot.Capture(window, "PhEnvironmentEditor-01-initial");
			DialogLayoutAssert.AssertNoCrowding(editor);

			Box(editor).Text = "/ a _ #";
			Assert.That(editor.TryCommit(), Is.True);
			Assert.That(sink.LastCommitted, Is.EqualTo("/ a _ #"));
			Assert.That(editor.CommittedText, Is.EqualTo("/ a _ #"));
		}

		[AvaloniaTest]
		public void InvalidEdit_IsRejected_AndRestoresLastCommitted()
		{
			var (editor, window, sink) = Show("/ _ #");

			Box(editor).Text = "/ X _ #";   // the fake validator rejects anything containing X
			Assert.That(editor.TryCommit(), Is.False);
			Assert.That(sink.LastCommitted, Is.Null, "an invalid string is never committed");
			Assert.That(Box(editor).Text, Is.EqualTo("/ _ #"), "the box restores the last committed value");

			DialogSnapshot.Capture(window, "PhEnvironmentEditor-02-invalid");
			DialogLayoutAssert.AssertNoCrowding(editor);
		}

		[AvaloniaTest]
		public void InsertToolbar_OffersTheLiteralInserts()
		{
			var (editor, _, _) = Show(string.Empty);
			var ids = editor.GetVisualDescendants().OfType<Button>()
				.Select(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b))
				.Where(id => id != null && id.StartsWith("PhEnvInsert-"))
				.ToList();
			Assert.That(ids, Is.EquivalentTo(new[] { "PhEnvInsert-#", "PhEnvInsert-[", "PhEnvInsert-]", "PhEnvInsert-/", "PhEnvInsert-_" }),
				"the insert toolbar offers the boundary/optional/slash/underscore literals");
		}
	}
}
