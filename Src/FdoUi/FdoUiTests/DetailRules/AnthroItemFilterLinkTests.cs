// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The anthropology-category filter jump: where it applies and what its link carries.
	/// </summary>
	[TestFixture]
	public class AnthroItemFilterLinkTests
	{
		[Test]
		public void Applies_OnlyToTheAnthropologyCategoriesField()
		{
			Assert.That(AnthroItemFilterLink.Applies("AnthroCodes"), Is.True);
			Assert.That(AnthroItemFilterLink.Applies("anthrocodes"), Is.False, "field names are exact");
			Assert.That(AnthroItemFilterLink.Applies("SemanticDomains"), Is.False);
			Assert.That(AnthroItemFilterLink.Applies(null), Is.False);
		}

		[Test]
		public void Create_NamesTheTool_HoldsItsListLoad_AndCarriesTheCategory()
		{
			var link = AnthroItemFilterLink.Create("my-project", "notebookEdit", 4321);

			Assert.That(link, Is.InstanceOf<FwAppArgs>());
			Assert.That(((FwAppArgs)link).Database, Is.EqualTo("my-project"));
			Assert.That(link.ToolName, Is.EqualTo("notebookEdit"));
			Assert.That(link.TargetGuid, Is.EqualTo(Guid.Empty), "the link filters a list; it targets no record");
			Assert.That(link.PropertyTableEntries.Select(p => p.name + "=" + p.value), Is.EqualTo(new[]
			{
				"SuspendLoadListUntilOnChangeFilter=notebookEdit", "LinkSetupInfo=FilterAnthroItems",
				"HvoOfAnthroItem=4321"
			}));
		}

		[Test]
		public void Create_RequiresATool()
		{
			Assert.That(() => AnthroItemFilterLink.Create("my-project", null, 1), Throws.ArgumentException);
			Assert.That(() => AnthroItemFilterLink.Create("my-project", "", 1), Throws.ArgumentException);
		}
	}
}
