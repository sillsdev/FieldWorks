// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The lexical-relation rule: what Delete Relation does per relation type and side, the
	/// note the confirmation adds, which chooser Add / Replace opens, how a choice joins the
	/// relation (a pair's partner, a tree's whole, a member), and the details edit.
	/// </summary>
	[TestFixture]
	public class LexicalRelationEditingTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexRefType _synonyms;
		private ILexRefType _antonym;
		private ILexRefType _partWhole;
		private ILexRefType _seeAlso;
		private ILexEntry _face;
		private ILexEntry _nose;
		private ILexEntry _eye;
		private ILexEntry _head;
		private ILexReference _synonymSet;
		private ILexReference _antonymPair;
		private ILexReference _faceParts;
		private ILexReference _seeAlsoRef;

		// face, nose and eye are synonyms (collection); face and nose antonyms (pair); nose
		// and eye parts of face (tree); face points to eye (unidirectional). All entry types.
		[SetUp]
		public void MakeRelations()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (Cache.LangProject.LexDbOA.ReferencesOA == null)
					Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				_synonyms = MakeType("Synonyms", LexRefTypeTags.MappingTypes.kmtEntryCollection);
				_antonym = MakeType("Antonym", LexRefTypeTags.MappingTypes.kmtEntryPair);
				_partWhole = MakeType("Whole", LexRefTypeTags.MappingTypes.kmtEntryTree);
				_seeAlso = MakeType("See Also", LexRefTypeTags.MappingTypes.kmtEntryUnidirectional);
				_face = MakeEntry("face");
				_nose = MakeEntry("nose");
				_eye = MakeEntry("eye");
				_head = MakeEntry("head");
				_synonymSet = MakeRelation(_synonyms, _face, _nose, _eye);
				_antonymPair = MakeRelation(_antonym, _face, _nose);
				_faceParts = MakeRelation(_partWhole, _face, _nose, _eye);
				_seeAlsoRef = MakeRelation(_seeAlso, _face, _eye);
			});
		}

		[TearDown]
		public void DeleteEverything()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var entry in Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToList())
					entry.Delete();
				foreach (var type in new[] { _synonyms, _antonym, _partWhole, _seeAlso })
				{
					if (type.IsValidObject)
						type.Delete();
				}
			});
		}

		[Test]
		public void DeleteKind_LeavesACollection_DeletesAPairAndAUnidirectional_AndSplitsATreeBySide()
		{
			Assert.That(LexicalRelationEditing.DeleteKind(_synonymSet, _nose), Is.EqualTo(LexicalRelationDeleteKind.RemoveFromRelation));
			Assert.That(LexicalRelationEditing.DeleteKind(_antonymPair, _nose), Is.EqualTo(LexicalRelationDeleteKind.DeleteRelation));
			Assert.That(LexicalRelationEditing.DeleteKind(_seeAlsoRef, _face), Is.EqualTo(LexicalRelationDeleteKind.DeleteRelation));
			Assert.That(LexicalRelationEditing.DeleteKind(_faceParts, _face), Is.EqualTo(LexicalRelationDeleteKind.DeleteRelation),
				"the whole takes the tree with it");
			Assert.That(LexicalRelationEditing.DeleteKind(_faceParts, _nose), Is.EqualTo(LexicalRelationDeleteKind.RemoveFromRelation),
				"a part just leaves");
		}

		[Test]
		public void DeleteNote_NamesTheSurvivingRelation_OrTheTree_OrNothing()
		{
			var collectionNote = LexicalRelationEditing.DeleteNote(_synonymSet, _nose);
			Assert.That(collectionNote?.Text, Does.StartWith("Note: the listed references will still be").And.Contain("Synonyms")
				.And.EndWith(" relation."));
			Assert.That(LexicalRelationEditing.DeleteNote(_faceParts, _face)?.Text,
				Does.StartWith("Note: this will remove this entire").And.EndWith("reference set tree."));
			Assert.That(LexicalRelationEditing.DeleteNote(_seeAlsoRef, _face), Is.Not.Null, "a unidirectional set goes whole");
			Assert.That(LexicalRelationEditing.DeleteNote(_antonymPair, _nose), Is.Null, "a pair has nothing to add");
			Assert.That(LexicalRelationEditing.DeleteNote(_faceParts, _nose), Is.Null, "a part leaving says nothing");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _synonymSet.TargetsRS.Remove(_eye));
			Assert.That(LexicalRelationEditing.DeleteNote(_synonymSet, _nose), Is.Null, "two members: nothing survives to mention");
		}

		[Test]
		public void Delete_RemovesTheObjectFromACollection_AndDeletesAPair_EachInOneUndoableStep()
		{
			LexicalRelationEditing.Delete(_synonymSet, _nose);
			Assert.That(_synonymSet.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _face, _eye }));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Delete Relation"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_synonymSet.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _face, _nose, _eye }));

			LexicalRelationEditing.Delete(_antonymPair, _nose);
			Assert.That(_antonymPair.IsValidObject, Is.False);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_antonym.MembersOC, Has.Count.EqualTo(1), "one Undo brings the pair back");
		}

		[Test]
		public void Chooser_PicksTheDialogByWhatTheTypeRelates_AndAddsOrReplacesBySide()
		{
			var add = LexicalRelationEditing.Chooser(_synonymSet, _nose);
			Assert.That(add.UseLinkDialog, Is.False, "an entry type uses the entry chooser");
			Assert.That(add.IsReplace, Is.False);
			Assert.That(add.Title, Is.EqualTo("Add Reference (Synonyms)"));
			Assert.That(add.ButtonText, Is.EqualTo("&Add"));

			var replacePartner = LexicalRelationEditing.Chooser(_antonymPair, _nose);
			Assert.That(replacePartner.IsReplace, Is.True, "a complete pair replaces its other member");
			Assert.That(replacePartner.Title, Is.EqualTo("Replace Reference"));
			Assert.That(replacePartner.ButtonText, Is.EqualTo("&Replace"));

			Assert.That(LexicalRelationEditing.Chooser(_faceParts, _face).IsReplace, Is.False, "the whole adds parts");
			Assert.That(LexicalRelationEditing.Chooser(_faceParts, _nose).IsReplace, Is.True, "a part replaces its whole");

			ILexRefType senses = null, either = null;
			ILexReference senseSet = null, eitherSet = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				senses = MakeType("Sense synonyms", LexRefTypeTags.MappingTypes.kmtSenseCollection);
				either = MakeType("Related", LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence);
				senseSet = MakeRelation(senses, _face.SensesOS[0], _nose.SensesOS[0]);
				eitherSet = MakeRelation(either, _face, _nose.SensesOS[0]);
			});
			var sensesOnly = LexicalRelationEditing.Chooser(senseSet, _face.SensesOS[0]);
			Assert.That(sensesOnly.UseLinkDialog && sensesOnly.SensesOnly && !sensesOnly.AllowSenses, Is.True);
			Assert.That(sensesOnly.Title, Is.EqualTo("Add Reference (Sense synonyms)"));
			var eitherOne = LexicalRelationEditing.Chooser(eitherSet, _face);
			Assert.That(eitherOne.UseLinkDialog && eitherOne.AllowSenses && !eitherOne.SensesOnly, Is.True);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				senses.Delete();
				either.Delete();
			});
		}

		[Test]
		public void AddOrReplace_AppendsToACollection_UnlessTheChoiceIsAMemberAlready()
		{
			Assert.That(LexicalRelationEditing.AddOrReplace(_synonymSet, _nose, _head), Is.SameAs(_synonymSet));
			Assert.That(_synonymSet.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _face, _nose, _eye, _head }));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Add Reference"));

			Assert.That(LexicalRelationEditing.AddOrReplace(_synonymSet, _nose, _eye), Is.Null, "already a member");
			Assert.That(_synonymSet.TargetsRS, Has.Count.EqualTo(4));
		}

		[Test]
		public void AddOrReplace_ReplacesThePartnerOfAPair_InOneStep()
		{
			Assert.That(LexicalRelationEditing.AddOrReplace(_antonymPair, _nose, _head), Is.SameAs(_antonymPair));
			Assert.That(_antonymPair.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _head, _nose }), "face gave way to head");
			Assert.That(_antonymPair.IsValidObject, Is.True, "never down to one member (LT-13729)");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Replace Reference"));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_antonymPair.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _face, _nose }));
		}

		[Test]
		public void AddOrReplace_MovesAPartToTheRelationOfTheChosenWhole_OrCreatesOne()
		{
			var moved = LexicalRelationEditing.AddOrReplace(_faceParts, _nose, _head);
			Assert.That(moved, Is.Not.SameAs(_faceParts), "no relation had head as its whole, so one was created");
			Assert.That(moved.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _head, _nose }));
			Assert.That(_faceParts.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _face, _eye }), "nose left face's parts");

			Assert.That(LexicalRelationEditing.AddOrReplace(_faceParts, _eye, _head), Is.SameAs(moved),
				"eye joins head's existing relation");
			Assert.That(moved.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _head, _nose, _eye }));
			Assert.That(_faceParts.IsValidObject, Is.False, "face's relation lost its last part");
		}

		[Test]
		public void ReplaceTreeWhole_RetargetsARelationWithOnePart_AndIgnoresTheSameWhole()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _faceParts.TargetsRS.Remove(_eye));

			var result = LexicalRelationEditing.AddOrReplace(_faceParts, _nose, _head);
			Assert.That(result, Is.SameAs(_faceParts), "the only part keeps its relation, retargeted (LT-14926)");
			Assert.That(_faceParts.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _head, _nose }));

			Assert.That(LexicalRelationEditing.AddOrReplace(_faceParts, _nose, _head), Is.SameAs(_faceParts),
				"choosing the current whole changes nothing (LT-10987)");
			Assert.That(_faceParts.TargetsRS.ToList(), Is.EqualTo(new ICmObject[] { _head, _nose }));
		}

		[Test]
		public void SetDetails_SetsNameAndComment_InOneUndoableStep()
		{
			LexicalRelationEditing.SetDetails(_synonymSet, "body parts", "the usual three");

			Assert.That(_synonymSet.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("body parts"));
			Assert.That(_synonymSet.Comment.AnalysisDefaultWritingSystem.Text, Is.EqualTo("the usual three"));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Edit Reference Set Details"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_synonymSet.Name.AnalysisDefaultWritingSystem.Text, Is.Null.Or.Empty);
		}

		private ILexRefType MakeType(string name, LexRefTypeTags.MappingTypes mapping)
		{
			var type = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(type);
			type.MappingType = (int)mapping;
			type.Name.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(name, Cache.DefaultAnalWs));
			return type;
		}

		private ILexEntry MakeEntry(string form)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(form, Cache.DefaultVernWs));
			entry.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
			return entry;
		}

		private ILexReference MakeRelation(ILexRefType type, params ICmObject[] targets)
		{
			var relation = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			type.MembersOC.Add(relation);
			foreach (var target in targets)
				relation.TargetsRS.Add(target);
			return relation;
		}
	}
}
