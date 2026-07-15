// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// advanced-entry-view (Configure-Columns P1): the LCModel-free owned column model exposes the legacy
	/// show/hide/reorder mutations with the legacy guards — an exact-duplicate add is refused, the last shown
	/// column cannot be removed, and the shown order is preserved through MoveUp/MoveDown. Stale persisted
	/// columns the catalog no longer offers are dropped on construction.
	/// </summary>
	[TestFixture]
	public class BrowseColumnModelTests
	{
		private static IReadOnlyList<BrowseColumnChoice> Catalog() => new[]
		{
			new BrowseColumnChoice("form|vernacular|Lexeme Form", "Lexeme Form"),
			new BrowseColumnChoice("gloss|analysis|Gloss", "Gloss", hasWritingSystemOption: true),
			new BrowseColumnChoice("cf|vernacular|Citation Form", "Citation Form")
		};

		private static BrowseColumnModel Model(params string[] shownKeys)
			=> new BrowseColumnModel(Catalog(), shownKeys.Select(k => new BrowseColumnEntry(k)).ToList());

		[Test]
		public void Construct_DropsStalePersistedColumnsNotInCatalog()
		{
			var model = new BrowseColumnModel(Catalog(), new[]
			{
				new BrowseColumnEntry("form|vernacular|Lexeme Form"),
				new BrowseColumnEntry("ghost|x|Gone") // not in the catalog anymore
			});
			Assert.That(model.ShownKeys, Is.EqualTo(new[] { "form|vernacular|Lexeme Form" }));
		}

		[Test]
		public void Add_AppendsCatalogColumn_AndRefusesDuplicate()
		{
			var model = Model("form|vernacular|Lexeme Form");
			Assert.That(model.Add("gloss|analysis|Gloss"), Is.True);
			Assert.That(model.ShownKeys, Is.EqualTo(new[] { "form|vernacular|Lexeme Form", "gloss|analysis|Gloss" }));

			Assert.That(model.Add("gloss|analysis|Gloss"), Is.False, "an already-shown column is not added twice");
			Assert.That(model.Add("not-in-catalog"), Is.False, "a non-catalog key is rejected");
		}

		[Test]
		public void Remove_DropsColumn_ButRefusesTheLast()
		{
			var model = Model("form|vernacular|Lexeme Form", "gloss|analysis|Gloss");
			Assert.That(model.Remove("form|vernacular|Lexeme Form"), Is.True);
			Assert.That(model.ShownKeys, Is.EqualTo(new[] { "gloss|analysis|Gloss" }));

			Assert.That(model.Remove("gloss|analysis|Gloss"), Is.False, "the last shown column cannot be removed");
			Assert.That(model.ShownKeys.Count, Is.EqualTo(1));
		}

		[Test]
		public void MoveUp_MoveDown_ReorderShownPreservingTheRest()
		{
			var model = Model("form|vernacular|Lexeme Form", "gloss|analysis|Gloss", "cf|vernacular|Citation Form");

			Assert.That(model.MoveDown("form|vernacular|Lexeme Form"), Is.True);
			Assert.That(model.ShownKeys, Is.EqualTo(new[]
			{
				"gloss|analysis|Gloss", "form|vernacular|Lexeme Form", "cf|vernacular|Citation Form"
			}));

			Assert.That(model.MoveUp("cf|vernacular|Citation Form"), Is.True);
			Assert.That(model.ShownKeys, Is.EqualTo(new[]
			{
				"gloss|analysis|Gloss", "cf|vernacular|Citation Form", "form|vernacular|Lexeme Form"
			}));

			Assert.That(model.MoveUp("gloss|analysis|Gloss"), Is.False, "the first column cannot move up");
			Assert.That(model.MoveDown("form|vernacular|Lexeme Form"), Is.False, "the last column cannot move down");
		}

		[Test]
		public void SetWidth_RecordsPerColumnWidth()
		{
			var model = Model("form|vernacular|Lexeme Form", "gloss|analysis|Gloss");
			model.SetWidth("gloss|analysis|Gloss", 220);
			Assert.That(model.WidthOf("gloss|analysis|Gloss"), Is.EqualTo(220));
			Assert.That(model.WidthOf("form|vernacular|Lexeme Form"), Is.Null);
		}
	}
}
