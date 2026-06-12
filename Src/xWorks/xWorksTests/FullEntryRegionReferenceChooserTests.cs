// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// 6.3 / B7 / B8 (xml-retirement-blockers) — reference chooser write-back: possibility-list
	/// reference fields compose as EDITABLE rows instead of read-only joined text. Atomic refs
	/// (possAtomicReference, e.g. Status) become Chooser rows whose options come from the field's
	/// possibility list (legacy <c>obj.ReferenceTargetOwner(flid)</c>, the same lane
	/// PossibilityAtomicReferenceSlice uses); vector refs (possVectorReference /
	/// SemDomVectorReference, e.g. Semantic Domains, Usages, Anthropology Categories) become
	/// ReferenceVector rows carrying the current items plus the list's options, edited through
	/// Add/Remove on the fenced session (sda Replace on the vector flid, the legacy
	/// VectorReferenceView update). Deep lists (semantic domains) carry hierarchy on the options
	/// (B8: RegionChoiceOption.Depth) so the chooser can render the legacy indented tree.
	/// </summary>
	[TestFixture]
	public class FullEntryRegionReferenceChooserTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private ILexSense m_sense;
		private ICmSemanticDomain m_domainUniverse;
		private ICmSemanticDomain m_domainSky;
		private ICmSemanticDomain m_domainWeather;
		private ICmPossibility m_statusConfirmed;
		private ICmPossibility m_statusPending;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				EnsureLists();
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(m_sense);
				m_sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("house", Cache.DefaultAnalWs));
				m_sense.SemanticDomainsRC.Add(m_domainUniverse);
			});
		}

		// The memory-only fixture ships no list content; build the minimal real lists the senses
		// reference — a HIERARCHICAL semantic domain list (B8) and a flat status list.
		private void EnsureLists()
		{
			var listFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			if (Cache.LangProject.SemanticDomainListOA == null)
				Cache.LangProject.SemanticDomainListOA = listFactory.Create();
			var semDomList = Cache.LangProject.SemanticDomainListOA;
			if (semDomList.PossibilitiesOS.Count == 0)
			{
				var semDomFactory = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
				m_domainUniverse = semDomFactory.Create();
				semDomList.PossibilitiesOS.Add(m_domainUniverse);
				m_domainUniverse.Name.SetAnalysisDefaultWritingSystem("Universe");
				m_domainSky = semDomFactory.Create();
				m_domainUniverse.SubPossibilitiesOS.Add(m_domainSky);
				m_domainSky.Name.SetAnalysisDefaultWritingSystem("Sky");
				m_domainWeather = semDomFactory.Create();
				m_domainUniverse.SubPossibilitiesOS.Add(m_domainWeather);
				m_domainWeather.Name.SetAnalysisDefaultWritingSystem("Weather");
			}
			else
			{
				m_domainUniverse = (ICmSemanticDomain)semDomList.PossibilitiesOS[0];
				m_domainSky = (ICmSemanticDomain)m_domainUniverse.SubPossibilitiesOS[0];
				m_domainWeather = (ICmSemanticDomain)m_domainUniverse.SubPossibilitiesOS[1];
			}

			if (Cache.LangProject.StatusOA == null)
				Cache.LangProject.StatusOA = listFactory.Create();
			var statusList = Cache.LangProject.StatusOA;
			if (statusList.PossibilitiesOS.Count == 0)
			{
				var possibilityFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
				m_statusConfirmed = possibilityFactory.Create();
				statusList.PossibilitiesOS.Add(m_statusConfirmed);
				m_statusConfirmed.Name.SetAnalysisDefaultWritingSystem("Confirmed");
				m_statusPending = possibilityFactory.Create();
				statusList.PossibilitiesOS.Add(m_statusPending);
				m_statusPending.Name.SetAnalysisDefaultWritingSystem("Pending");
			}
			else
			{
				m_statusConfirmed = statusList.PossibilitiesOS[0];
				m_statusPending = statusList.PossibilitiesOS[1];
			}

			if (Cache.LangProject.LexDbOA.UsageTypesOA == null)
				Cache.LangProject.LexDbOA.UsageTypesOA = listFactory.Create();
			if (Cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Count == 0)
			{
				var usage = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				Cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Add(usage);
				usage.Name.SetAnalysisDefaultWritingSystem("archaic");
			}

			if (Cache.LangProject.AnthroListOA == null)
				Cache.LangProject.AnthroListOA = listFactory.Create();
			if (Cache.LangProject.AnthroListOA.PossibilitiesOS.Count == 0)
			{
				var anthro = Cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>().Create();
				Cache.LangProject.AnthroListOA.PossibilitiesOS.Add(anthro);
				anthro.Name.SetAnalysisDefaultWritingSystem("Kinship");
			}

			// B7: the Publications list behind Publish In / Show As Headword In — the field whose
			// legacy chooser carries the "Edit the Publications list" jump link.
			if (Cache.LangProject.LexDbOA.PublicationTypesOA == null)
				Cache.LangProject.LexDbOA.PublicationTypesOA = listFactory.Create();
			if (Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Count == 0)
			{
				var mainDictionary = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(mainDictionary);
				mainDictionary.Name.SetAnalysisDefaultWritingSystem("Main Dictionary");
			}

			// Gear = configure: the morph-type chooser's list-editor jump is DERIVED from the
			// list (its chooserInfo carries only a title), so the list itself must exist.
			if (Cache.LangProject.LexDbOA.MorphTypesOA == null)
				Cache.LangProject.LexDbOA.MorphTypesOA = listFactory.Create();
		}

		private ComposedEntryRegion Compose(bool showHidden = false)
			=> FullEntryRegionComposer.Compose(m_entry, Cache, showHidden);

		private static LexicalEditRegionField VectorField(ComposedEntryRegion composed, string field)
			=> composed.Model.Fields.Single(f => f.Field == field && f.Kind == RegionFieldKind.ReferenceVector);

		[Test]
		public void Compose_SemanticDomains_IsEditableReferenceVector_WithHierarchicalOptions()
		{
			var composed = Compose();
			var field = VectorField(composed, "SemanticDomains");

			Assert.That(field.IsEditable, Is.True, "6.3: the vector reference is no longer read-only text");
			Assert.That(field.Items.Select(i => i.Key), Is.EqualTo(new[] { m_domainUniverse.Guid.ToString() }),
				"the current items ride the row in vector order");
			Assert.That(field.Items.Single().Name, Does.Contain("Universe"));

			// B8: the option list is the WHOLE possibility tree, hierarchy carried as Depth, in the
			// list's own (tree) order — exactly what the legacy chooser tree shows.
			Assert.That(field.Options.Select(o => o.Key), Is.EqualTo(new[]
				{
					m_domainUniverse.Guid.ToString(), m_domainSky.Guid.ToString(), m_domainWeather.Guid.ToString()
				}),
				"options walk the list tree in document order (parent before children)");
			Assert.That(field.Options.Select(o => o.Depth), Is.EqualTo(new[] { 0, 1, 1 }),
				"sub-domains carry their hierarchy level for the indented chooser");
		}

		[Test]
		public void Edit_SemanticDomains_AddItem_CommitsAsOneUndoStep()
		{
			var composed = Compose();
			var field = VectorField(composed, "SemanticDomains");

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_domainSky.Guid.ToString()), Is.True,
				"adding stages through the fenced session (sda Replace on the vector flid)");
			composed.EditContext.Commit();

			Assert.That(m_sense.SemanticDomainsRC.Select(d => d.Guid),
				Is.EquivalentTo(new[] { m_domainUniverse.Guid, m_domainSky.Guid }));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.SemanticDomainsRC.Select(d => d.Guid),
				Is.EquivalentTo(new[] { m_domainUniverse.Guid }),
				"the add is one step on the global undo stack");
		}

		[Test]
		public void Edit_SemanticDomains_RemoveItem_Commits()
		{
			var composed = Compose();
			var field = VectorField(composed, "SemanticDomains");

			Assert.That(composed.EditContext.TryRemoveReferenceItem(field, m_domainUniverse.Guid.ToString()),
				Is.True);
			composed.EditContext.Commit();

			Assert.That(m_sense.SemanticDomainsRC, Is.Empty);

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.SemanticDomainsRC.Select(d => d.Guid),
				Is.EquivalentTo(new[] { m_domainUniverse.Guid }));
		}

		[Test]
		public void Edit_ReferenceVector_RejectsGarbageUnknownAndDuplicates_WithoutOpeningASession()
		{
			var composed = Compose();
			var field = VectorField(composed, "SemanticDomains");

			Assert.That(composed.EditContext.TryAddReferenceItem(field, "not-a-guid"), Is.False);
			Assert.That(composed.EditContext.TryAddReferenceItem(field, System.Guid.NewGuid().ToString()),
				Is.False, "a guid outside the field's possibility list must not stage");
			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_domainUniverse.Guid.ToString()),
				Is.False, "duplicates are rejected, like the legacy chooser");
			Assert.That(composed.EditContext.TryRemoveReferenceItem(field, m_domainSky.Guid.ToString()),
				Is.False, "removing an item that is not in the vector must not stage");
			Assert.That(composed.EditContext.IsOpen, Is.False, "rejected edits must not open the fence");
			Assert.That(m_sense.SemanticDomainsRC.Count, Is.EqualTo(1), "nothing was written");
		}

		[Test]
		public void Compose_EmptyAlwaysVisibleVector_StillOffersTheAddSlotOptions()
		{
			// SemanticDomains is visibility="always" in LexSense/Normal: with no items the row still
			// composes, editable, with the full option list — the legacy empty slice with the
			// type-ahead add slot.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_sense.SemanticDomainsRC.Clear());

			var composed = Compose();
			var field = VectorField(composed, "SemanticDomains");

			Assert.That(field.Items, Is.Empty);
			Assert.That(field.IsEditable, Is.True);
			Assert.That(field.Options.Count, Is.EqualTo(3), "the add slot offers the whole list");

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_domainWeather.Guid.ToString()), Is.True);
			composed.EditContext.Commit();
			Assert.That(m_sense.SemanticDomainsRC.Single().Guid, Is.EqualTo(m_domainWeather.Guid));
		}

		[Test]
		public void Compose_UsagesAndAnthroCategories_AreEditableReferenceVectors()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_sense.UsageTypesRC.Add(Cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS[0]);
				m_sense.AnthroCodesRC.Add((ICmAnthroItem)Cache.LangProject.AnthroListOA.PossibilitiesOS[0]);
			});

			var composed = Compose();
			var usages = VectorField(composed, "UsageTypes");
			Assert.That(usages.IsEditable, Is.True);
			Assert.That(usages.Options.Select(o => o.Key),
				Does.Contain(Cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS[0].Guid.ToString()),
				"Usages options come from the usage-types possibility list");

			var anthro = VectorField(composed, "AnthroCodes");
			Assert.That(anthro.IsEditable, Is.True);
			Assert.That(anthro.Options.Select(o => o.Key),
				Does.Contain(Cache.LangProject.AnthroListOA.PossibilitiesOS[0].Guid.ToString()),
				"Anthropology Categories options come from the anthro list");
		}

		[Test]
		public void Compose_SenseStatus_AtomicPossibilityReference_IsChooser_AndCommits()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_sense.StatusRA = m_statusConfirmed);

			var composed = Compose();
			var status = composed.Model.Fields.Single(f => f.Field == "Status" && f.ObjectHvo == m_sense.Hvo);

			Assert.That(status.Kind, Is.EqualTo(RegionFieldKind.Chooser),
				"possAtomicReference takes the chooser lane, like the morph type");
			Assert.That(status.IsEditable, Is.True);
			Assert.That(status.SelectedOptionKey, Is.EqualTo(m_statusConfirmed.Guid.ToString()));
			// Review task 6: the empty choice leads (the legacy launcher lets the user clear the
			// reference), then the field's possibility list in list order (ReferenceTargetOwner).
			Assert.That(status.Options.Select(o => o.Key), Is.EqualTo(new[]
				{
					string.Empty, m_statusConfirmed.Guid.ToString(), m_statusPending.Guid.ToString()
				}),
				"empty option first, then the list's options in list order");
			Assert.That(status.Options[0].Name, Is.Not.Null.And.Not.Empty,
				"the empty choice carries the launchers' localized label (ksNullLabel), never blank");

			Assert.That(composed.EditContext.TrySetOption(status, m_statusPending.Guid.ToString()), Is.True);
			composed.EditContext.Commit();
			Assert.That(m_sense.StatusRA, Is.EqualTo(m_statusPending));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.StatusRA, Is.EqualTo(m_statusConfirmed));
		}

		// Review task 6: legacy PossibilityAtomicReferenceLauncher.OnLeave commits an emptied box
		// as AddItem(null) — i.e. the reference CLEARS. The composed chooser's empty option does
		// the same through the fenced session (SetObjProp(hvo, flid, 0)).
		[Test]
		public void Edit_AtomicChooser_EmptyOption_ClearsTheReference()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_sense.StatusRA = m_statusConfirmed);

			var composed = Compose();
			var status = composed.Model.Fields.Single(f => f.Field == "Status" && f.ObjectHvo == m_sense.Hvo);

			Assert.That(composed.EditContext.TrySetOption(status, string.Empty), Is.True,
				"the empty option stages a clear");
			composed.EditContext.Commit();
			Assert.That(m_sense.StatusRA, Is.Null, "the reference cleared, like legacy AddItem(null)");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.StatusRA, Is.EqualTo(m_statusConfirmed),
				"the clear is one step on the global undo stack");
		}

		// The morph-type chooser must NOT gain the empty choice: legacy
		// MorphTypeAtomicLauncher.AllowEmptyItem is false (a form always has a morph type).
		[Test]
		public void Compose_MorphTypeChooser_OffersNoEmptyOption()
		{
			var composed = Compose();
			var morphType = composed.Model.Fields
				.Single(f => f.Field == "MorphType" && f.Kind == RegionFieldKind.Chooser);

			Assert.That(morphType.Options.Select(o => o.Key), Has.None.EqualTo(string.Empty),
				"MorphTypeAtomicLauncher.AllowEmptyItem == false — no empty choice here");
			Assert.That(composed.EditContext.TrySetOption(morphType, string.Empty), Is.False,
				"an empty key must not clear the morph type");
		}

		[Test]
		public void Edit_AtomicChooser_RejectsKeysOutsideTheList_WithoutOpeningASession()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_sense.StatusRA = m_statusConfirmed);

			var composed = Compose();
			var status = composed.Model.Fields.Single(f => f.Field == "Status" && f.ObjectHvo == m_sense.Hvo);

			Assert.That(composed.EditContext.TrySetOption(status, "garbage"), Is.False);
			Assert.That(composed.EditContext.TrySetOption(status, m_domainSky.Guid.ToString()), Is.False,
				"a possibility from ANOTHER list must not be assignable");
			Assert.That(composed.EditContext.IsOpen, Is.False);
			Assert.That(m_sense.StatusRA, Is.EqualTo(m_statusConfirmed));
		}

		// GAP 1 / B7: the layout's chooserLink metadata composes onto the row — LexEntryParts.xml:48-53
		// gives Publish In <chooserLink type="goto" label="Edit the Publications list"
		// tool="publicationsEdit"/>, the legacy chooser dialog's jump LinkLabel
		// (ReallySimpleListChooser.cs:887-900: AddLink(label, kGotoLink, new FwLinkArgs(tool,
		// m_guidLink)) with m_guidLink = Guid.Empty — no flidTextParam on this part).
		[Test]
		public void Compose_PublishIn_CarriesThePublicationsJumpLink_WithEmptyTarget()
		{
			var composed = Compose();
			var publishIn = composed.Model.Fields.Single(f => f.Field == "PublishIn"
				&& f.Kind == RegionFieldKind.ReferenceVector && f.ObjectHvo == m_entry.Hvo);

			Assert.That(publishIn.ChooserLinks, Has.Count.EqualTo(1),
				"the Publish In row carries the layout's jump link");
			var link = publishIn.ChooserLinks[0];
			Assert.That(link.Label, Is.EqualTo("Edit the Publications list"));
			Assert.That(link.Tool, Is.EqualTo("publicationsEdit"));
			Assert.That(link.TargetGuid, Is.Null,
				"legacy m_guidLink stays Guid.Empty for this link — a plain tool jump");
		}

		// Gear = configure: a chooser whose layout authored NO chooserLink (MorphologyParts.xml's
		// MorphTypeBasic chooserInfo is title-only) still resolves its list-editor jump by
		// DERIVATION from the row's possibility list — LexDb.MorphTypes maps to the lists-area
		// morphTypeEdit tool, exactly the legacy AreaListener.GetToolForList clerk-table walk
		// (AreaListener.cs:388-418 over Lists/areaConfiguration.xml's MorphTypeList clerk +
		// Lists/Edit/toolConfiguration.xml's morphTypeEdit tool).
		[Test]
		public void Compose_MorphTypeChooser_DerivesTheMorphTypeEditJump_FromItsList()
		{
			var composed = Compose();
			var morphType = composed.Model.Fields
				.Single(f => f.Field == "MorphType" && f.Kind == RegionFieldKind.Chooser);

			Assert.That(morphType.ChooserLinks, Has.Count.EqualTo(1),
				"no authored link, but the morph-type list resolves a lists-area editor");
			Assert.That(morphType.ChooserLinks[0].Tool, Is.EqualTo("morphTypeEdit"));
			Assert.That(morphType.ChooserLinks[0].TargetGuid, Is.Null, "a plain tool jump");
		}

		// The derived lane never overrides an authored link, and the derivation itself mirrors
		// the legacy clerk table: shipped lists by (owner class, owning field); ownerless custom
		// lists by the dynamically generated Name-without-spaces + "Edit" tool
		// (AreaListener.GetCustomListToolName); anything unmapped resolves to NO tool → no gear.
		[Test]
		public void ResolveListEditorTool_MirrorsTheLegacyListsAreaMapping()
		{
			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(
				Cache.LangProject.SemanticDomainListOA), Is.EqualTo("semanticDomainEdit"));
			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(
				Cache.LangProject.StatusOA), Is.EqualTo("statusEdit"));
			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(
				Cache.LangProject.LexDbOA.UsageTypesOA), Is.EqualTo("usageTypeEdit"));
			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(
				Cache.LangProject.AnthroListOA), Is.EqualTo("anthroEdit"));
			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(
				Cache.LangProject.LexDbOA.PublicationTypesOA), Is.EqualTo("publicationsEdit"));
			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(null), Is.Null);
		}

		[Test]
		public void ResolveListEditorTool_CustomOwnerlessList_DerivesTheGeneratedToolName()
		{
			ICmPossibilityList custom = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				custom = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>()
					.CreateUnowned("Bird Species", Cache.DefaultAnalWs);
			});

			// Legacy AreaListener.GetCustomListToolName: Name without whitespace + "Edit" — the
			// tool name the lists area generates for the custom list's dynamic tool node.
			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(custom),
				Is.EqualTo("BirdSpeciesEdit"));
		}

		[Test]
		public void ResolveListEditorTool_OwnedListOutsideTheListsArea_ResolvesNoTool()
		{
			// LangProject.CheckLists is a possibility-list home with NO lists-area tool (it is
			// excluded even from translated-list export): no tool → the composer adds no link →
			// the row draws no gear.
			ICmPossibilityList checkList = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				checkList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				Cache.LangProject.CheckListsOC.Add(checkList);
			});

			Assert.That(FullEntryRegionComposer.ResolveListEditorTool(checkList), Is.Null,
				"a list with no resolvable lists-area editor yields no jump (and no gear)");
		}

		// The authored-link-wins half: Publish In carries the layout's own chooserLink, so the
		// derived lane must not add a second link (first goto wins at the gear).
		[Test]
		public void Compose_PublishIn_AuthoredLinkWins_NoDerivedDuplicate()
		{
			var composed = Compose();
			var publishIn = composed.Model.Fields.Single(f => f.Field == "PublishIn"
				&& f.Kind == RegionFieldKind.ReferenceVector && f.ObjectHvo == m_entry.Hvo);

			Assert.That(publishIn.ChooserLinks, Has.Count.EqualTo(1),
				"the authored goto link rides alone — derivation only fills gaps");
			Assert.That(publishIn.ChooserLinks[0].Label, Is.EqualTo("Edit the Publications list"),
				"the authored (localizable) label is kept, not the derived format");
		}

		// B7: a chooserInfo guicontrol "...FlatList" spec means the legacy chooser presents the list
		// FLAT (e.g. PeopleFlatList, EnvironmentFlatList); the option builder honors it by emitting
		// depth-0 options while keeping document order.
		[Test]
		public void BuildPossibilityOptions_FlatSpec_FlattensTheHierarchy()
		{
			var hierarchical = FullEntryRegionComposer.BuildPossibilityOptions(
				Cache.LangProject.SemanticDomainListOA, flat: false);
			Assert.That(hierarchical.Select(o => o.Depth), Is.EqualTo(new[] { 0, 1, 1 }));

			var flat = FullEntryRegionComposer.BuildPossibilityOptions(
				Cache.LangProject.SemanticDomainListOA, flat: true);
			Assert.That(flat.Select(o => o.Key), Is.EqualTo(hierarchical.Select(o => o.Key)),
				"flattening keeps the document order");
			Assert.That(flat.Select(o => o.Depth), Is.All.EqualTo(0),
				"a FlatList guicontrol spec suppresses the hierarchy");
		}
	}
}
