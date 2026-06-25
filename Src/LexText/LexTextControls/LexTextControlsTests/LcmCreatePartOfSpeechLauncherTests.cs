// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the "Create a new Part of Speech" flow (<see cref="LcmCreatePartOfSpeechLauncher"/>),
	/// MSA-port Stage 4: building the master-category catalog from an eticPOSList document (the same parse the WinForms
	/// <see cref="MasterCategoryListDlg"/> uses), projecting it into hierarchical chooser candidates, and the
	/// chosen-catalog-id → new <c>IPartOfSpeech</c> + <see cref="FwPosNode"/> round-trip — the unit-testable core that
	/// mirrors <see cref="MasterCategoryListDlg"/>'s create-in-project logic (fixed guid + name/abbr + CatalogSourceId)
	/// in one undoable step. The modal loop itself is desktop-only, so it is exercised by the headless dialog tests in
	/// FwAvaloniaDialogsTests; here we cover the create core over a real LcmCache, visible via InternalsVisibleTo.
	/// </summary>
	[TestFixture]
	public class LcmCreatePartOfSpeechLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		// A small two-level catalog in document order: Adposition > {Postposition}, plus a flat Adjective. Guids are
		// fixed (as in GOLDEtic.xml) so MasterCategory.AddToDatabase creates the POS with the catalog guid.
		private const string CatalogXml =
@"<eticPOSList>
   <item type='category' id='Adjective' guid='30d07580-5052-4d91-bc24-469b8b2d7df9'>
      <abbrev ws='en'>adj</abbrev>
      <term ws='en'>Adjective</term>
      <def ws='en'>An adjective modifies a noun.</def>
   </item>
   <item type='category' id='Adposition' guid='ae115ea8-2cd7-4501-8ae7-dc638e4f17c5'>
      <abbrev ws='en'>adp</abbrev>
      <term ws='en'>Adposition</term>
      <def ws='en'>An adposition relates a complement to another unit.</def>
      <item type='category' id='Postposition' guid='18f1b2b8-0ce3-4889-90e9-003fed6a969f'>
         <abbrev ws='en'>post</abbrev>
         <term ws='en'>Postposition</term>
         <def ws='en'>A postposition occurs after its complement.</def>
      </item>
   </item>
</eticPOSList>";

		// The base opens an undoable UOW in TestSetup; LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog calls
		// MasterCategory.AddToDatabase which opens its OWN UndoableUnitOfWorkHelper.Do, so end the base's open task
		// first (a nested task would throw "Nested tasks are not supported"), mirroring MasterCategoryTests.
		private System.Collections.Generic.IReadOnlyList<LcmCreatePartOfSpeechLauncher.CatalogCategory> Catalog()
		{
			var doc = new XmlDocument();
			doc.LoadXml(CatalogXml);
			return LcmCreatePartOfSpeechLauncher.BuildCatalog(Cache, doc.DocumentElement);
		}

		// ----- catalog → candidates: hierarchical, depth-tagged, keyed by catalog id -----

		[Test]
		public void BuildCatalog_ProjectsItemsInDocumentOrderWithDepth()
		{
			var catalog = Catalog();
			Assert.That(catalog.Select(c => c.Id),
				Is.EqualTo(new[] { "Adjective", "Adposition", "Postposition" }),
				"every catalog item is present in document order");
			Assert.That(catalog.Single(c => c.Id == "Adjective").Depth, Is.EqualTo(0));
			Assert.That(catalog.Single(c => c.Id == "Adposition").Depth, Is.EqualTo(0));
			Assert.That(catalog.Single(c => c.Id == "Postposition").Depth, Is.EqualTo(1),
				"a nested catalog item carries its catalog depth (folded into the reused chooser tree)");
		}

		[Test]
		public void BuildCandidates_AreHierarchicalSingleSelectKeyedByCatalogId()
		{
			var candidates = LcmCreatePartOfSpeechLauncher.BuildCandidates(Catalog());
			var input = LcmCreatePartOfSpeechLauncher.BuildInput(Catalog());

			Assert.That(input.Hierarchical, Is.True, "the catalog is presented as the reused hierarchical chooser tree");
			Assert.That(input.SelectionMode, Is.EqualTo(ChooserSelectionMode.Single), "create chooses ONE category");
			Assert.That(input.ForbidEmptySelection, Is.True, "OK is gated until a category is chosen");
			Assert.That(candidates.Any(o => o.Key == "Adposition" && o.Name == "Adposition" && o.Depth == 0), Is.True);
			Assert.That(candidates.Any(o => o.Key == "Postposition" && o.Depth == 1), Is.True);
		}

		// ----- chosen catalog id -> new IPartOfSpeech in PartsOfSpeechOA (right guid/name/abbr) -----

		[Test]
		public void CreatePosFromCatalog_CreatesThePosInPartsOfSpeechOA_WithCatalogGuidNameAndAbbr()
		{
			m_actionHandler.EndUndoTask(); // AddToDatabase opens its own undoable step

			var posList = Cache.LangProject.PartsOfSpeechOA;
			var before = posList.ReallyReallyAllPossibilities.Count;

			var pos = LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, Catalog(), "Adjective");

			Assert.That(pos, Is.Not.Null, "the chosen catalog category is created as a project POS");
			Assert.That(pos.Guid.ToString(), Is.EqualTo("30d07580-5052-4d91-bc24-469b8b2d7df9"),
				"the POS is created with the catalog's fixed guid (merge parity, MasterCategoryListDlg)");
			Assert.That(pos.Name.BestAnalysisAlternative.Text, Is.EqualTo("Adjective"), "the catalog term becomes the name");
			Assert.That(pos.Abbreviation.BestAnalysisAlternative.Text, Is.EqualTo("adj"), "the catalog abbrev is set");
			Assert.That(pos.CatalogSourceId, Is.EqualTo("Adjective"), "the CatalogSourceId is stamped");
			Assert.That(pos.Owner, Is.EqualTo(posList), "a top-level catalog category lands in PartsOfSpeechOA");
			Assert.That(posList.ReallyReallyAllPossibilities.Count, Is.EqualTo(before + 1),
				"exactly one POS is added");
		}

		[Test]
		public void CreatePosFromCatalog_NestedCategory_NestsUnderItsCatalogParentPos()
		{
			m_actionHandler.EndUndoTask();
			var catalog = Catalog();

			// Create the parent (Adposition), then the child (Postposition) which must nest under it.
			var parent = LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, catalog, "Adposition");
			var child = LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, catalog, "Postposition");

			Assert.That(child, Is.Not.Null);
			Assert.That(child.Guid.ToString(), Is.EqualTo("18f1b2b8-0ce3-4889-90e9-003fed6a969f"));
			Assert.That(child.Owner, Is.EqualTo(parent),
				"a nested catalog category is created under its catalog parent's POS (MasterCategoryListDlg parity)");
		}

		// ----- the returned FwPosNode matches the created POS -----

		[Test]
		public void BuildNode_ReturnsAFwPosNodeMatchingTheCreatedPos()
		{
			m_actionHandler.EndUndoTask();
			var pos = LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, Catalog(), "Adjective");

			var node = LcmCreatePartOfSpeechLauncher.BuildNode(pos);

			Assert.That(node, Is.Not.Null);
			Assert.That(node.Id, Is.EqualTo(pos.Guid.ToString()), "the node id is the new POS guid string");
			Assert.That(node.Name, Is.EqualTo("Adjective"), "the node name is the new POS name");
			Assert.That(node.Abbreviation, Is.EqualTo("adj"), "the node carries the new POS abbreviation");
		}

		[Test]
		public void CreatePosFromCatalog_NullOrUnknownChosenId_ReturnsNull()
		{
			m_actionHandler.EndUndoTask();
			Assert.That(LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, Catalog(), null), Is.Null);
			Assert.That(LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, Catalog(), "NotAnId"), Is.Null,
				"an unknown catalog id resolves to no POS (a cancelled/empty pick)");
		}

		[Test]
		public void CreatePosFromCatalog_AlreadyInstalledCategory_ResolvesToTheExistingPos_NoDuplicate()
		{
			m_actionHandler.EndUndoTask();
			var posList = Cache.LangProject.PartsOfSpeechOA;

			var first = LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, Catalog(), "Adjective");
			var count = posList.ReallyReallyAllPossibilities.Count;

			// Choosing the SAME catalog category again (rebuilt catalog now sees it installed) resolves to the
			// existing POS without creating a duplicate — MasterCategory.AddToDatabase no-ops when InDatabase.
			var again = LcmCreatePartOfSpeechLauncher.CreatePosFromCatalog(Cache, Catalog(), "Adjective");
			Assert.That(again, Is.SameAs(first), "an already-installed catalog category resolves to its existing POS");
			Assert.That(posList.ReallyReallyAllPossibilities.Count, Is.EqualTo(count), "no duplicate POS is created");
		}
	}
}
