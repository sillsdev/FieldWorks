// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Locks the two menu-id compositions, which stay separate. A row's slice menu
	/// (<see cref="RecordEditView.ComposeSliceMenuIds"/>) is the row's own menu plus exactly one
	/// shared trailing group; its in-string menu
	/// (<see cref="RecordEditView.ComposeInStringMenuIds"/>) is the contextMenu binding alone.
	/// The shared group belongs only to the former.
	/// </summary>
	[TestFixture]
	public class DetailContextMenuCompositionTests
	{
		// The Lexeme Form row's bindings (MorphologyParts.xml, MoForm-Detail-AsLexemeForm).
		private const string LexemeFormMenu = "mnuDataTree-LexemeForm";
		private const string LexemeFormContextMenu = "mnuDataTree-LexemeFormContext";

		[Test]
		public void SliceMenu_ForAMultiStringRow_AddsTheMultiStringGroup_NotTheObjectGroup()
		{
			var ids = RecordEditView.ComposeSliceMenuIds(LexemeFormMenu, isMultiStringRow: true);

			// Only mnuDataTree-MultiStringSlice carries the Writing Systems submenu; composing
			// mnuDataTree-Object here would drop Writing Systems off the slice menu.
			Assert.That(ids, Is.EqualTo(new[] { LexemeFormMenu, RecordEditView.MultiStringSliceMenuId }));
			Assert.That(ids, Has.No.Member(RecordEditView.ObjectMenuId),
				"both shared menus define Field Visibility / Move Field / Help, so adding both doubles it");
		}

		[Test]
		public void SliceMenu_ForASingleStringRow_AddsTheObjectGroup()
		{
			var ids = RecordEditView.ComposeSliceMenuIds("mnuDataTree-Help", isMultiStringRow: false);

			Assert.That(ids, Is.EqualTo(new[] { "mnuDataTree-Help", RecordEditView.ObjectMenuId }));
			Assert.That(ids, Has.No.Member(RecordEditView.MultiStringSliceMenuId));
		}

		[Test]
		public void SliceMenu_ForAnUnboundRow_IsTheSharedObjectGroupAlone()
		{
			// A row with no menu= binding still composes mnuDataTree-Object, so Field
			// Visibility / Move Field / Help stay reachable on every row.
			Assert.That(RecordEditView.ComposeSliceMenuIds(null, isMultiStringRow: false),
				Is.EqualTo(new[] { RecordEditView.ObjectMenuId }));
			Assert.That(RecordEditView.ComposeSliceMenuIds(string.Empty, isMultiStringRow: false),
				Is.EqualTo(new[] { RecordEditView.ObjectMenuId }));
		}

		// Every binding a row can carry, including a row bound directly to one of the two shared
		// menus, which already supplies the group its own way.
		[Test]
		public void SliceMenu_NeverCarriesBothSharedGroupSources()
		{
			var bindings = new[]
			{
				LexemeFormMenu, null, string.Empty,
				RecordEditView.ObjectMenuId, RecordEditView.MultiStringSliceMenuId
			};
			foreach (var binding in bindings)
			{
				foreach (var isMultiString in new[] { true, false })
				{
					var ids = RecordEditView.ComposeSliceMenuIds(binding, isMultiString);
					var sharedGroupSources = ids.Count(id =>
						id == RecordEditView.MultiStringSliceMenuId || id == RecordEditView.ObjectMenuId);
					Assert.That(sharedGroupSources, Is.EqualTo(1),
						$"exactly one shared-group source must be present (menu={binding ?? "<null>"}, "
						+ $"isMultiStringRow={isMultiString})");
				}
			}
		}

		[Test]
		public void SliceMenu_WithAnExplicitObjectBinding_DoesNotRepeatIt()
		{
			// Even on a multistring row: the row's own binding already carries the shared group,
			// so the multistring group must not be appended on top of it.
			Assert.That(RecordEditView.ComposeSliceMenuIds(RecordEditView.ObjectMenuId,
					isMultiStringRow: false),
				Is.EqualTo(new[] { RecordEditView.ObjectMenuId }));
			Assert.That(RecordEditView.ComposeSliceMenuIds(RecordEditView.ObjectMenuId,
					isMultiStringRow: true),
				Is.EqualTo(new[] { RecordEditView.ObjectMenuId }));
		}

		[Test]
		public void InStringMenu_IsTheContextBindingAlone_EvenOnAMultiStringRow()
		{
			var ids = RecordEditView.ComposeInStringMenuIds(LexemeFormContextMenu);

			// A single menu id, so the Lexeme Form value area shows exactly its two
			// Concordance commands -- no shared group.
			Assert.That(ids, Is.EqualTo(new[] { LexemeFormContextMenu }));
			Assert.That(ids, Has.No.Member(RecordEditView.MultiStringSliceMenuId));
			Assert.That(ids, Has.No.Member(RecordEditView.ObjectMenuId));
		}

		[Test]
		public void InStringMenu_IsEmpty_WhenTheRowHasNoContextBinding()
		{
			// An unbound value box keeps its own local menu; it must not fall back to the
			// slice menu.
			Assert.That(RecordEditView.ComposeInStringMenuIds(null), Is.Empty);
			Assert.That(RecordEditView.ComposeInStringMenuIds(string.Empty), Is.Empty);
		}
	}
}
