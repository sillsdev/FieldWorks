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
	/// D3 follow-up — `LexReferenceMultiSlice` should compose as native Avalonia rows rather than
	/// a deferred custom-slice placeholder. These tests pin the relation-type walk semantics the
	/// legacy slice generated dynamically: forward vs reverse labels and which targets show on each
	/// side of the relation.
	/// </summary>
	[TestFixture]
	public class LexReferenceMultiSliceTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry MakeEntry(string form, string gloss = null)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(form, Cache.DefaultVernWs));
			if (gloss != null)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			}
			return entry;
		}

		private ILexRefType MakeRefType(string name, string reverseName, LexRefTypeTags.MappingTypes mappingType)
		{
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

			var type = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(type);
			type.MappingType = (int)mappingType;
			type.Name.set_String(Cache.DefaultAnalWs, name);
			if (!string.IsNullOrEmpty(reverseName))
				type.ReverseName.set_String(Cache.DefaultAnalWs, reverseName);
			return type;
		}

		private ILexReference MakeReference(ILexRefType type, params ICmObject[] targets)
		{
			var reference = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			type.MembersOC.Add(reference);
			foreach (var target in targets)
				reference.TargetsRS.Add(target);
			return reference;
		}

		private static LexicalEditRegionField RelationRow(ComposedEntryRegion composed, int relationHvo)
			=> composed.Model.Fields.Single(f => f.Kind == RegionFieldKind.ReferenceVector && f.ObjectHvo == relationHvo);

		private const string LexReferenceMultiSliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.LexReferenceMultiSlice";

		[Test]
		public void Compose_EntryTreeRelation_UsesForwardAndReverseLabels_WithTheExpectedTargets()
		{
			ILexEntry body = null;
			ILexEntry arm = null;
			ILexEntry leg = null;
			ILexReference relation = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				body = MakeEntry("corps", "body");
				arm = MakeEntry("bras", "arm");
				leg = MakeEntry("jambe", "leg");
				var type = MakeRefType("Part", "Whole", LexRefTypeTags.MappingTypes.kmtEntryTree);
				relation = MakeReference(type, body, arm, leg);
			});

			var forward = FullEntryRegionComposer.Compose(body, Cache);
			var forwardRow = RelationRow(forward, relation.Hvo);
			Assert.Multiple(() =>
			{
				Assert.That(forwardRow.Label, Is.EqualTo("Part"));
				Assert.That(forwardRow.Items.Select(i => i.Name), Is.EqualTo(new[] { arm.HeadWord.Text, leg.HeadWord.Text }));
				Assert.That(forwardRow.Items.Select(i => i.Key), Is.EqualTo(new[] { arm.Guid.ToString(), leg.Guid.ToString() }));
				Assert.That(forwardRow.MenuId, Is.EqualTo("mnuDataTree-DeleteAddLexReference"));
			});

			var reverse = FullEntryRegionComposer.Compose(arm, Cache);
			var reverseRow = RelationRow(reverse, relation.Hvo);
			Assert.Multiple(() =>
			{
				Assert.That(reverseRow.Label, Is.EqualTo("Whole"));
				Assert.That(reverseRow.Items.Select(i => i.Name), Is.EqualTo(new[] { body.HeadWord.Text }));
				Assert.That(reverseRow.MenuId, Is.EqualTo("mnuDataTree-DeleteReplaceLexReference"));
			});

			Assert.That(forward.CustomEditorFields.Select(f => f.ClassName),
				Has.No.Member(LexReferenceMultiSliceClassName),
				"the composed relation rows should absorb the legacy multi-slice rather than leaving a deferred custom-slice placeholder");
		}

		[Test]
		public void Compose_SenseSequenceRelation_RendersANestedRelationRow_WithTheOtherSenseName()
		{
			ILexEntry entry = null;
			ILexSense firstSense = null;
			ILexSense secondSense = null;
			ILexReference relation = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = MakeEntry("homme", "man");
				firstSense = entry.SensesOS[0];
				secondSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(secondSense);
				secondSense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("male", Cache.DefaultAnalWs));
				var type = MakeRefType("Sibling", null, LexRefTypeTags.MappingTypes.kmtSenseSequence);
				relation = MakeReference(type, firstSense, secondSense);
			});

			var composed = FullEntryRegionComposer.Compose(entry, Cache);
			var row = composed.Model.Fields.Single(f => f.Kind == RegionFieldKind.ReferenceVector
				&& f.ObjectHvo == relation.Hvo
				&& f.Items.Any(i => i.Key == secondSense.Guid.ToString()));

			Assert.Multiple(() =>
			{
				Assert.That(row.Field, Is.EqualTo("LexSenseReferences"));
				Assert.That(row.Label, Is.EqualTo("Sibling"));
				Assert.That(row.Items.Select(i => i.Key), Is.EqualTo(new[] { secondSense.Guid.ToString() }));
				Assert.That(row.Items.Single().Name,
					Is.EqualTo(secondSense.OwnerOutlineNameForWs(Cache.DefaultVernWs).Text),
					"sense targets display their owner-outline name, like the existing entry/sense reference lane");
				Assert.That(row.MenuId, Is.EqualTo("mnuDataTree-DeleteAddLexReference"));
			});
		}
	}
}