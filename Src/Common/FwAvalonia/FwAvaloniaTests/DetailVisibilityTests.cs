// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Pure model-level visibility computation (the virtualization-safe replacement for
	/// DataTree's
	/// captured-control collapse/expand wiring). No Avalonia types involved, so these are plain
	/// NUnit tests rather than [AvaloniaTest].
	/// </summary>
	[TestFixture]
	public class DetailVisibilityTests
	{
		private static DetailField Header(string id, string label, int indent,
			bool initiallyExpanded = true) => new DetailField(
			id, label, null, null, DetailFieldKind.Header, EditorClassification.GroupingNone,
			null, null, HostRouting.Inherit, null, null, null,
			isEditable: false, indent: indent, isCollapsible: true, isInitiallyExpanded: initiallyExpanded);

		private static DetailField NonCollapsibleHeader(string id, string label, int indent) =>
			new DetailField(id, label, null, null, DetailFieldKind.Header, EditorClassification.GroupingNone,
				null, null, HostRouting.Inherit, null, null, null,
				isEditable: false, indent: indent, isCollapsible: false);

		private static DetailField Text(string id, string label, int indent) =>
			new DetailField(id, label, label, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("en", "value") }, null, null,
				isEditable: true, indent: indent);

		[Test]
		public void NoCollapsibleHeaders_EverythingVisible()
		{
			var fields = new[] { Text("f1", "Field 1", 0), Text("f2", "Field 2", 0) };

			var visible = DetailVisibility.ComputeVisibility(fields, null);

			Assert.That(visible, Is.EqualTo(new[] { true, true }));
		}

		[Test]
		public void CollapsedHeader_HidesOwnedRange_ButNotItself()
		{
			var fields = new[] { Header("h1", "Sense 1", 0), Text("g1", "Gloss", 1), Text("d1", "Definition", 1) };

			var visible = DetailVisibility.ComputeVisibility(fields, id => id == "h1" ? (bool?)false : null);

			Assert.That(visible, Is.EqualTo(new[] { true, false, false }), "header stays visible, its rows hide");
		}

		[Test]
		public void ExpandedHeader_HidesNothing()
		{
			var fields = new[] { Header("h1", "Sense 1", 0), Text("g1", "Gloss", 1) };

			var visible = DetailVisibility.ComputeVisibility(fields, id => id == "h1" ? (bool?)true : null);

			Assert.That(visible, Is.EqualTo(new[] { true, true }));
		}

		[Test]
		public void Nesting_OuterCollapsedInnerExpanded_InnerRowsStayHidden()
		{
			// parent(0) collapsed, child(1) expanded, grandchild(2) owned by both -> hidden
			// because
			// the collapsed ancestor's range still owns it, regardless of the nearer header's
			// state.
			var fields = new[]
			{
				Header("parent", "Sense 1", 0, initiallyExpanded: false),
				Header("child", "Examples", 1, initiallyExpanded: true),
				Text("grand", "Example sentence", 2),
				Text("sibling", "Gloss", 1)
			};

			var visible = DetailVisibility.ComputeVisibility(fields, id => null); // fall back to initial state

			Assert.That(visible, Is.EqualTo(new[] { true, false, false, false }),
				"child header, grandchild row, and the parent's other child row are all hidden by the collapsed parent");
		}

		[Test]
		public void HeaderOwningEmptyRange_IsNotTreatedAsCollapsible()
		{
			// h1 owns the indented child; h2 is followed only by a field at its own indent, so it
			// owns nothing.
			var fields = new[]
			{
				Header("h1", "Sense 1", 0), Text("c1", "Gloss", 1),
				Header("h2", "Sense 2", 0), Text("g1", "Gloss", 0)
			};

			var ranges = DetailVisibility.GetCollapsibleRanges(fields);

			Assert.That(ranges, Has.Count.EqualTo(1), "only h1 owns a non-empty range");
			Assert.That(ranges[0].HeaderIndex, Is.EqualTo(0));

			// Even collapsing h2 (via expansion state) must have no visibility effect since it
			// owns nothing.
			var visible = DetailVisibility.ComputeVisibility(fields, id => id == "h2" ? (bool?)false : (bool?)true);
			Assert.That(visible, Is.EqualTo(new[] { true, true, true, true }));
		}

		[Test]
		public void UnrecordedExpansionState_FallsBackToIsInitiallyExpanded()
		{
			var fields = new[] { Header("h1", "Sense 1", 0, initiallyExpanded: false), Text("g1", "Gloss", 1) };

			var visible = DetailVisibility.ComputeVisibility(fields, id => null);

			Assert.That(visible, Is.EqualTo(new[] { true, false }), "no recorded state, so IsInitiallyExpanded (false) applies");
		}

		[Test]
		public void NullDelegate_IsTolerated()
		{
			var fields = new[] { Header("h1", "Sense 1", 0, initiallyExpanded: false), Text("g1", "Gloss", 1) };

			Assert.DoesNotThrow(() => DetailVisibility.ComputeVisibility(fields, null));
			var visible = DetailVisibility.ComputeVisibility(fields, null);

			Assert.That(visible, Is.EqualTo(new[] { true, false }), "null delegate behaves like an always-null lookup");
		}

		[Test]
		public void NonCollapsibleHeader_IsIgnoredEvenWithFollowingIndentedRows()
		{
			var fields = new[] { NonCollapsibleHeader("h1", "Sense 1", 0), Text("g1", "Gloss", 1) };

			var ranges = DetailVisibility.GetCollapsibleRanges(fields);

			Assert.That(ranges, Is.Empty);
		}

		[Test]
		public void GetVisibleFields_ReturnsOnlyVisibleFieldsInOrder()
		{
			var h1 = Header("h1", "Sense 1", 0, initiallyExpanded: false);
			var g1 = Text("g1", "Gloss", 1);
			var h2 = Header("h2", "Sense 2", 0, initiallyExpanded: true);
			var g2 = Text("g2", "Gloss2", 1);
			var fields = new[] { h1, g1, h2, g2 };

			var result = DetailVisibility.GetVisibleFields(fields, null);

			Assert.That(result, Is.EqualTo(new[] { h1, h2, g2 }));
		}
	}
}
