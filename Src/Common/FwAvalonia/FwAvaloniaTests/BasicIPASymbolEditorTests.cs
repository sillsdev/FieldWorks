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
	/// avalonia-rule-formula-editor (task 3.1) — the Basic IPA Symbol editor control: typing a symbol commits
	/// through the sink; with no sink it is read-only. PNG + AssertNoCrowding.
	/// </summary>
	[TestFixture]
	public class BasicIPASymbolEditorTests
	{
		private sealed class FakeSink : IBasicIpaSymbolCommandSink
		{
			public string LastCommitted;
			public bool Commit(string symbol) { LastCommitted = symbol; return true; }
		}

		private static TextBox Box(Control root) => root.GetVisualDescendants().OfType<TextBox>().First();

		[AvaloniaTest]
		public void Editable_CommitsTypedSymbol()
		{
			var sink = new FakeSink();
			var editor = new BasicIPASymbolEditor("p", sink);
			var window = new Window { Content = editor, Width = 240, Height = 70 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			DialogSnapshot.Capture(window, "BasicIPASymbolEditor-01-editable");
			DialogLayoutAssert.AssertNoCrowding(editor);

			Box(editor).Text = "pʰ";
			Assert.That(editor.TryCommit(), Is.True);
			Assert.That(sink.LastCommitted, Is.EqualTo("pʰ"));
			Assert.That(editor.CommittedText, Is.EqualTo("pʰ"));
		}

		[AvaloniaTest]
		public void NoSink_IsReadOnly()
		{
			var editor = new BasicIPASymbolEditor("p", null);
			var window = new Window { Content = editor, Width = 240, Height = 70 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(editor.IsEditable, Is.False);
			Assert.That(Box(editor).IsReadOnly, Is.True);
		}
	}
}
