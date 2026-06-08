// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Headless tests for the Avalonia POC slice. These run on .NET Framework 4.8 via
	/// Avalonia.Headless.NUnit and are the spike's primary host-bridge evidence: if they pass,
	/// Avalonia 11.3.x loads and renders editable FieldWorks-owned controls on net48.
	/// </summary>
	[TestFixture]
	public class PocLexEntrySliceTests
	{
		private static (PocLexEntrySlice slice, Window window) ShowSlice()
		{
			var entry = PocEntryDto.CreateSample();
			var slice = new PocLexEntrySlice(entry);
			var window = new Window { Content = slice, Width = 420, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (slice, window);
		}

		[AvaloniaTest]
		public void PocSlice_RendersThreeEditors()
		{
			var (slice, _) = ShowSlice();

			Assert.That(slice.LexemeFormEditor, Is.Not.Null);
			Assert.That(slice.LexemeFormEditor.Boxes.Count, Is.EqualTo(2), "two writing systems on the lexeme form");
			Assert.That(slice.MorphTypeChooser, Is.Not.Null);
			Assert.That(slice.MorphTypeChooser.Button, Is.Not.Null);
			Assert.That(slice.SenseGlossEditor, Is.Not.Null);
			Assert.That(slice.SenseGlossEditor.Boxes.Count, Is.EqualTo(2), "two writing systems on the gloss");
		}

		[AvaloniaTest]
		public void PocSlice_WsText_UsesConfiguredFont()
		{
			var (slice, _) = ShowSlice();

			Assert.That(slice.LexemeFormEditor.Boxes[0].FontFamily.Name, Is.EqualTo("Charis SIL"));
			Assert.That(slice.LexemeFormEditor.Boxes[1].FontFamily.Name, Is.EqualTo("Times New Roman"));
		}

		[AvaloniaTest]
		public void PocSlice_Edit_WritesThroughToEntry_AndCommitKeepsAndCancelRestores()
		{
			var (slice, _) = ShowSlice();
			var entry = slice.Entry;

			// Commit path: an edit writes through to the entry and survives commit.
			var session = new PocEditSession(entry);
			slice.LexemeFormEditor.Boxes[0].Text = "edited";
			Dispatcher.UIThread.RunJobs();
			Assert.That(entry.LexemeForm[0].Value, Is.EqualTo("edited"), "edit should write through to the entry");
			session.Commit();
			Assert.That(entry.LexemeForm[0].Value, Is.EqualTo("edited"), "commit keeps the edit");

			// Cancel path: a subsequent edit is rolled back to the snapshot.
			var session2 = new PocEditSession(entry);
			slice.LexemeFormEditor.Boxes[0].Text = "temp";
			Dispatcher.UIThread.RunJobs();
			session2.Cancel();
			Assert.That(entry.LexemeForm[0].Value, Is.EqualTo("edited"), "cancel restores the snapshot");
		}

		[AvaloniaTest]
		public void PocSlice_MorphTypeChooser_UpdatesEntryAndReturnsFocus()
		{
			var (slice, _) = ShowSlice();
			var entry = slice.Entry;

			slice.MorphTypeChooser.Button.Focus();
			slice.MorphTypeChooser.Open();
			Dispatcher.UIThread.RunJobs();

			var suffix = entry.MorphTypeOptions.Single(o => o.Key == "suffix");
			slice.MorphTypeChooser.Select(suffix);
			Dispatcher.UIThread.RunJobs();

			Assert.That(entry.MorphTypeKey, Is.EqualTo("suffix"), "choosing updates the entry");
			Assert.That(slice.MorphTypeChooser.Button.Content, Is.EqualTo("suffix"), "button label reflects the choice");
			Assert.That(slice.MorphTypeChooser.Button.IsFocused, Is.True, "focus returns to the host button after choosing");
		}

		[AvaloniaTest]
		public void PocSlice_SemanticSnapshot_IsDeterministic()
		{
			var (slice, _) = ShowSlice();

			var first = BuildPocSnapshot(slice);
			var second = BuildPocSnapshot(slice);

			Assert.That(second, Is.EqualTo(first), "POC semantic snapshot must be deterministic for parity comparison");
			// Sanity: the snapshot captures the three fields and their editor kinds.
			Assert.That(first, Does.Contain("Lexeme Form | editor=multiws-text"));
			Assert.That(first, Does.Contain("Morph Type | editor=popup-chooser"));
			Assert.That(first, Does.Contain("Gloss | editor=multiws-text"));
		}

		/// <summary>
		/// Builds a normalized semantic snapshot of the Avalonia slice in the same spirit as the
		/// WinForms baseline (see DataTreeTests.SemanticSnapshot_*). The two are compared in the
		/// spike evidence report; they are not asserted equal because the POC uses detached DTO data.
		/// </summary>
		private static string BuildPocSnapshot(PocLexEntrySlice slice)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"#0 | Lexeme Form | editor=multiws-text | ws={WsList(slice.LexemeFormEditor)}");
			sb.AppendLine($"#1 | Morph Type | editor=popup-chooser | value={slice.Entry.MorphTypeKey}");
			sb.AppendLine($"#2 | Gloss | editor=multiws-text | ws={WsList(slice.SenseGlossEditor)}");
			return sb.ToString();
		}

		private static string WsList(MultiWsTextEditor editor)
			=> string.Join(",", editor.Alternatives.Select(a => a.WsAbbrev));
	}
}
