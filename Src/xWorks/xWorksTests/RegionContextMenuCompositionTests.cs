// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Locks the in-string right-click menu-id composition (<see cref="RecordEditView.ComposeContextMenuIds"/>)
	/// against the legacy <c>DTMenuHandler.MakeSliceContextMenu</c> recipe. Both
	/// mnuDataTree-MultiStringSlice and mnuDataTree-Object independently define the shared
	/// Field Visibility / Move Field / Help group, so composing BOTH source menus makes that group
	/// appear twice on a bridged row. These tests prove the composition adds at most one of the two.
	/// </summary>
	[TestFixture]
	public class RegionContextMenuCompositionTests
	{
		[Test]
		public void MultiStringRow_AddsMultiStringSliceButNotObject()
		{
			var ids = RecordEditView.ComposeContextMenuIds("mnuDataTree-CitationFormContext",
				isMultiStringRow: true);

			// The multistring slice group carries the Writing Systems submenu plus the shared
			// Field Visibility / Move Field / Help leaves, so mnuDataTree-Object must NOT also be added
			// (that would double the shared group).
			Assert.That(ids, Is.EqualTo(new[]
			{
				"mnuDataTree-CitationFormContext",
				RecordEditView.MultiStringSliceMenuId
			}));
			Assert.That(ids, Has.No.Member(RecordEditView.ObjectMenuId),
				"A multistring row must not add mnuDataTree-Object on top of mnuDataTree-MultiStringSlice: "
				+ "both define Field Visibility / Move Field / Help, so the group would appear twice.");
		}

		[Test]
		public void SingleStringRow_AddsObjectButNotMultiStringSlice()
		{
			var ids = RecordEditView.ComposeContextMenuIds("mnuDataTree-CitationFormContext",
				isMultiStringRow: false);

			Assert.That(ids, Is.EqualTo(new[]
			{
				"mnuDataTree-CitationFormContext",
				RecordEditView.ObjectMenuId
			}));
			Assert.That(ids, Has.No.Member(RecordEditView.MultiStringSliceMenuId));
		}

		[Test]
		public void SharedSliceGroupSourceMenus_AreNeverBothPresent()
		{
			// The defect: a bridged multistring row composing BOTH shared source menus. Whatever the
			// row kind, the two menus that carry the shared group must never both appear.
			foreach (var isMultiString in new[] { true, false })
			{
				var ids = RecordEditView.ComposeContextMenuIds("mnuDataTree-CitationFormContext",
					isMultiString);
				var sharedGroupSources = ids.Count(id =>
					id == RecordEditView.MultiStringSliceMenuId || id == RecordEditView.ObjectMenuId);
				Assert.That(sharedGroupSources, Is.EqualTo(1),
					$"Exactly one shared-group source menu must be present (isMultiStringRow={isMultiString}); "
					+ "adding both doubles the Field Visibility / Move Field / Help group.");
			}
		}

		[Test]
		public void EmptyFieldContextMenuId_IsDropped()
		{
			var ids = RecordEditView.ComposeContextMenuIds(null, isMultiStringRow: false);

			Assert.That(ids, Is.EqualTo(new[] { RecordEditView.ObjectMenuId }));
		}
	}
}
