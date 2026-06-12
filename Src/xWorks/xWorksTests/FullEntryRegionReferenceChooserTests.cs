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
			Assert.That(status.Options.Select(o => o.Key), Is.EqualTo(new[]
				{
					m_statusConfirmed.Guid.ToString(), m_statusPending.Guid.ToString()
				}),
				"options come from the field's possibility list (ReferenceTargetOwner), in list order");

			Assert.That(composed.EditContext.TrySetOption(status, m_statusPending.Guid.ToString()), Is.True);
			composed.EditContext.Commit();
			Assert.That(m_sense.StatusRA, Is.EqualTo(m_statusPending));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.StatusRA, Is.EqualTo(m_statusConfirmed));
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

		// GAP 1 control: a chooserInfo without links (MorphologyParts.xml:280-283, title only)
		// composes a chooser row with NO jump links — the link lane is data-driven, never invented.
		[Test]
		public void Compose_MorphTypeChooser_HasNoJumpLinks()
		{
			var composed = Compose();
			var morphType = composed.Model.Fields
				.Single(f => f.Field == "MorphType" && f.Kind == RegionFieldKind.Chooser);

			Assert.That(morphType.ChooserLinks, Is.Empty,
				"MoForm-Detail-MorphTypeBasic's chooserInfo carries only a title, no chooserLink");
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
