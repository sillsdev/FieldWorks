// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The section-header construction and child-indent rule are shared by both detail
	/// projectors (thin mapper + full composer) via <see cref="DetailStructureRules"/>. These lock
	/// the shared rules directly; the mapper/composer snapshot tests prove neither path's output changed.
	/// </summary>
	[TestFixture]
	public class DetailStructureRulesTests
	{
		[TestCase(null, 2, 2)]    // unlabeled passthrough group keeps depth
		[TestCase("", 2, 2)]
		[TestCase("Header", 2, 3)] // labeled group indents one level
		[TestCase("Header", 0, 1)]
		public void ChildIndent_LabeledGroupIndentsOneLevel(string label, int depth, int expected)
		{
			Assert.That(DetailStructureRules.ChildIndent(label, depth), Is.EqualTo(expected));
		}

		[Test]
		public void BuildHeaderField_Defaults_MatchTheThinMapperHeader()
		{
			var header = DetailStructureRules.CreateHeaderField(
				"g", "Group", "Field", "vern", EditorClassification.GroupingNone,
				"autoId", "loc.key", HostRouting.Inherit, depth: 2);

			Assert.That(header.Kind, Is.EqualTo(DetailFieldKind.Header));
			Assert.That(header.StableId, Is.EqualTo("g"));
			Assert.That(header.Label, Is.EqualTo("Group"));
			Assert.That(header.Indent, Is.EqualTo(2));
			Assert.That(header.IsEditable, Is.False);
			Assert.That(header.AutomationId, Is.EqualTo("autoId"));
			// Thin-mapper defaults: no collapse affordance, no menu/HVO.
			Assert.That(header.IsCollapsible, Is.False);
			Assert.That(header.IsInitiallyExpanded, Is.True);
			Assert.That(header.MenuId, Is.Null);
			Assert.That(header.HotlinksId, Is.Null);
			Assert.That(header.ObjectHvo, Is.EqualTo(0));
		}

		[Test]
		public void BuildHeaderField_RichArgs_MatchTheComposerHeader()
		{
			var header = DetailStructureRules.CreateHeaderField(
				"g", "Group", "Field", "vern", EditorClassification.GroupingNone,
				"autoId", "loc.key", HostRouting.Inherit, depth: 1,
				isCollapsible: true, isInitiallyExpanded: false,
				menuId: "mnuSec", hotlinksId: "hot", objectHvo: 42);

			Assert.That(header.Kind, Is.EqualTo(DetailFieldKind.Header));
			Assert.That(header.IsCollapsible, Is.True);
			Assert.That(header.IsInitiallyExpanded, Is.False);
			Assert.That(header.MenuId, Is.EqualTo("mnuSec"));
			Assert.That(header.HotlinksId, Is.EqualTo("hot"));
			Assert.That(header.ObjectHvo, Is.EqualTo(42));
		}
	}
}
