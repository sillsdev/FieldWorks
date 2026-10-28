// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>What Delete Relation does on a lexical-relation row.</summary>
	public enum LexicalRelationDeleteKind
	{
		/// <summary>The shown object leaves the relation, which goes on among the
		/// others.</summary>
		RemoveFromRelation,
		/// <summary>The whole relation is deleted.</summary>
		DeleteRelation
	}

	/// <summary>
	/// The chooser Add Reference / Replace Reference opens on a lexical-relation row: which
	/// dialog, what it may select, and its title and button.
	/// </summary>
	public sealed class LexicalRelationChooser
	{
		public LexicalRelationChooser(bool useLinkDialog, bool sensesOnly, bool allowSenses, bool isReplace,
			string title, string buttonText)
		{
			UseLinkDialog = useLinkDialog;
			SensesOnly = sensesOnly;
			AllowSenses = allowSenses;
			IsReplace = isReplace;
			Title = title;
			ButtonText = buttonText;
		}

		/// <summary>True for the entry-or-sense link dialog; false for the entry-only
		/// chooser.</summary>
		public bool UseLinkDialog { get; }

		/// <summary>The link dialog offers senses only.</summary>
		public bool SensesOnly { get; }

		/// <summary>The link dialog offers senses as well as entries.</summary>
		public bool AllowSenses { get; }

		/// <summary>The choice replaces the row's reference instead of adding one.</summary>
		public bool IsReplace { get; }

		public string Title { get; }

		public string ButtonText { get; }
	}

	/// <summary>
	/// The commands of a lexical-relation row (Delete Relation, Add Reference, Replace
	/// Reference, Edit Reference Set Details): how each edits the relation from the object
	/// whose detail shows it, and what the dialogs they open need. The relation's mapping
	/// type and which side the shown object is on decide the behaviour.
	/// </summary>
	public static class LexicalRelationEditing
	{
		/// <summary>The help topic of the Add / Replace Reference chooser.</summary>
		public const string ChooserHelpTopic = "khtpChooseLexicalRelationAdd";

		/// <summary>The undo text of adding a reference to a relation.</summary>
		public static string UndoAddReference => FdoUiStrings.ksUndoAddRef;

		/// <summary>The redo text of adding a reference to a relation.</summary>
		public static string RedoAddReference => FdoUiStrings.ksRedoAddRef;

		/// <summary>The undo text of replacing a relation's reference.</summary>
		public static string UndoReplaceReference => FdoUiStrings.ksUndoReplaceRef;

		/// <summary>The redo text of replacing a relation's reference.</summary>
		public static string RedoReplaceReference => FdoUiStrings.ksRedoReplaceRef;

		/// <summary>The chooser's button when it adds a reference.</summary>
		public static string AddButtonText => FdoUiStrings.ks_Add;

		/// <summary>The chooser's button when it replaces a reference.</summary>
		public static string ReplaceButtonText => FdoUiStrings.ks_Replace;

		/// <summary>The chooser's title when it adds a reference to a relation of the named
		/// type.</summary>
		public static string AddReferenceTitle(string typeName)
			=> string.Format(FdoUiStrings.ksIdentifyXEntry, typeName);

		/// <summary>The chooser's title when it adds a reference to a relation of the named
		/// type, worded for what the type relates: senses, entries or either.</summary>
		public static string AddReferenceTitle(LexRefTypeTags.MappingTypes mapping, string typeName)
		{
			switch (mapping)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtSensePair:
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtSenseSequence:
				case LexRefTypeTags.MappingTypes.kmtSenseTree:
				case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					return string.Format(FdoUiStrings.ksIdentifyXSense, typeName);
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
					return string.Format(FdoUiStrings.ksIdentifyXLexEntryOrSense, typeName);
				default:
					return string.Format(FdoUiStrings.ksIdentifyXLexEntry, typeName);
			}
		}

		/// <summary>The relation's mapping type.</summary>
		public static LexRefTypeTags.MappingTypes MappingTypeOf(ILexReference relation)
			=> (LexRefTypeTags.MappingTypes)((ILexRefType)relation.Owner).MappingType;

		/// <summary>Whether the relation is a tree (whole / parts).</summary>
		public static bool IsTree(LexRefTypeTags.MappingTypes mapping)
			=> mapping == LexRefTypeTags.MappingTypes.kmtSenseTree
				|| mapping == LexRefTypeTags.MappingTypes.kmtEntryTree
				|| mapping == LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree;

		/// <summary>Whether the relation is a pair, symmetric or not.</summary>
		public static bool IsPair(LexRefTypeTags.MappingTypes mapping)
		{
			switch (mapping)
			{
				case LexRefTypeTags.MappingTypes.kmtSensePair:
				case LexRefTypeTags.MappingTypes.kmtEntryPair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
					return true;
				default:
					return false;
			}
		}

		/// <summary>Whether <paramref name="current"/> heads the relation: the whole of a
		/// tree, the first of a sequence or pair.</summary>
		public static bool IsFirstTarget(ILexReference relation, ICmObject current)
			=> relation.TargetsRS.Count > 0 && relation.TargetsRS[0] == current;

		/// <summary>
		/// What Delete Relation does for <paramref name="current"/>: it leaves a collection
		/// or sequence, and a tree it is a part of; a pair, a unidirectional relation and a
		/// tree it is the whole of are deleted.
		/// </summary>
		public static LexicalRelationDeleteKind DeleteKind(ILexReference relation, ICmObject current)
		{
			var mapping = MappingTypeOf(relation);
			switch (mapping)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtSenseSequence:
				case LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
					return LexicalRelationDeleteKind.RemoveFromRelation;
				default:
					return IsTree(mapping) && !IsFirstTarget(relation, current)
						? LexicalRelationDeleteKind.RemoveFromRelation
						: LexicalRelationDeleteKind.DeleteRelation;
			}
		}

		/// <summary>
		/// The note the delete confirmation shows under the object: for a collection or
		/// sequence with more than two members, that the others stay related; for a tree
		/// or unidirectional relation being deleted, that the whole set goes. Null when
		/// there is nothing to add.
		/// </summary>
		public static ITsString DeleteNote(ILexReference relation, ICmObject current)
		{
			var type = (ILexRefType)relation.Owner;
			var cache = relation.Cache;
			var userWs = cache.WritingSystemFactory.UserWs;
			var builder = TsStringUtils.MakeIncStrBldr();
			builder.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
			var kind = DeleteKind(relation, current);
			switch (MappingTypeOf(relation))
			{
				case LexRefTypeTags.MappingTypes.kmtSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtSenseSequence:
				case LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
					if (relation.TargetsRS.Count <= 2)
						return null;
					builder.Append(string.Format(FdoUiStrings.ksDeleteSequenceCollectionA, StringUtils.kChHardLB.ToString()));
					builder.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
						cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
					builder.Append(type.ShortName);
					builder.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					builder.Append(FdoUiStrings.ksDeleteSequenceCollectionB);
					return builder.GetString();
				case LexRefTypeTags.MappingTypes.kmtSenseTree:
				case LexRefTypeTags.MappingTypes.kmtEntryTree:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
				case LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
					if (kind != LexicalRelationDeleteKind.DeleteRelation)
						return null;
					builder.Append(string.Format(FdoUiStrings.ksDeleteLexTree, StringUtils.kChHardLB));
					return builder.GetString();
				default:
					return null;
			}
		}

		/// <summary>Runs Delete Relation for <paramref name="current"/> as
		/// <see cref="DeleteKind"/> says, in one undoable step.</summary>
		public static void Delete(ILexReference relation, ICmObject current)
		{
			if (relation == null)
				throw new ArgumentNullException(nameof(relation));
			if (current == null)
				throw new ArgumentNullException(nameof(current));
			var kind = DeleteKind(relation, current);
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoDeleteRelation, FdoUiStrings.ksRedoDeleteRelation, current, () =>
			{
				if (kind == LexicalRelationDeleteKind.RemoveFromRelation)
					relation.TargetsRS.Remove(current);
				else
					relation.Cache.DomainDataByFlid.DeleteObj(relation.Hvo);
			});
		}

		/// <summary>
		/// The chooser Add / Replace Reference opens for <paramref name="current"/>: a pair
		/// replaces the other member, a tree's part replaces its whole, everything else adds.
		/// Sense types offer senses, entry types entries, the rest both.
		/// </summary>
		public static LexicalRelationChooser Chooser(ILexReference relation, ICmObject current)
		{
			var mapping = MappingTypeOf(relation);
			var type = (ILexRefType)relation.Owner;
			var name = type.Name.BestAnalysisAlternative.Text;
			bool useLinkDialog = false, sensesOnly = false, allowSenses = false;
			switch (mapping)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtSensePair:
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtSenseSequence:
				case LexRefTypeTags.MappingTypes.kmtSenseTree:
				case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					useLinkDialog = true;
					sensesOnly = true;
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
					useLinkDialog = true;
					allowSenses = true;
					break;
			}
			var isReplace = IsReplace(relation, current);
			string title;
			switch (mapping)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryCollection:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
					title = AddReferenceTitle(mapping, name);
					break;
				default:
					title = isReplace ? FdoUiStrings.ksReplaceXEntry : AddReferenceTitle(name);
					break;
			}
			return new LexicalRelationChooser(useLinkDialog, sensesOnly, allowSenses, isReplace, title,
				isReplace ? ReplaceButtonText : AddButtonText);
		}

		/// <summary>Whether the chooser replaces: a pair with its other member in place, or
		/// a tree seen from a part.</summary>
		public static bool IsReplace(ILexReference relation, ICmObject current)
		{
			var mapping = MappingTypeOf(relation);
			if (IsPair(mapping))
				return PairPartner(relation, current) != null;
			return IsTree(mapping) && !IsFirstTarget(relation, current);
		}

		/// <summary>The other member of a pair, null when the pair is not yet complete.</summary>
		public static ICmObject PairPartner(ILexReference relation, ICmObject current)
		{
			if (relation.TargetsRS.Count < 2)
				return null;
			var partner = relation.TargetsRS[0];
			return partner == current ? relation.TargetsRS[1] : partner;
		}

		/// <summary>
		/// Makes <paramref name="chosen"/> the reference for <paramref name="current"/>, in
		/// one undoable step: a pair's other member is replaced, a tree's part joins the
		/// relation whose whole is the choice, and any other relation gains the choice as a
		/// member unless it is one already.
		/// </summary>
		/// <returns>The relation <paramref name="current"/> is now in; null when nothing
		/// changed.</returns>
		public static ILexReference AddOrReplace(ILexReference relation, ICmObject current, ICmObject chosen)
		{
			if (relation == null)
				throw new ArgumentNullException(nameof(relation));
			if (current == null)
				throw new ArgumentNullException(nameof(current));
			if (chosen == null)
				throw new ArgumentNullException(nameof(chosen));
			var mapping = MappingTypeOf(relation);
			ILexReference result = relation;
			if (IsPair(mapping))
			{
				var replace = PairPartner(relation, current) != null;
				UndoableUnitOfWorkHelper.Do(replace ? FdoUiStrings.ksUndoReplaceRef : FdoUiStrings.ksUndoAddRef,
					replace ? FdoUiStrings.ksRedoReplaceRef : FdoUiStrings.ksRedoAddRef, relation,
					() => ReplacePairPartner(relation, current, chosen));
			}
			else if (IsTree(mapping) && !IsFirstTarget(relation, current))
			{
				UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoReplaceRef, FdoUiStrings.ksRedoReplaceRef, relation,
					() => result = ReplaceTreeWhole(relation, current, chosen));
			}
			else
			{
				if (relation.TargetsRS.Contains(chosen))
					return null;
				UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoAddRef, FdoUiStrings.ksRedoAddRef, relation,
					() => relation.TargetsRS.Add(chosen));
			}
			return result;
		}

		/// <summary>Replaces the member of the pair that is not <paramref name="current"/>
		/// with <paramref name="chosen"/>, in one step so the pair never drops to one
		/// member and deletes itself (LT-13729).</summary>
		public static void ReplacePairPartner(ILexReference relation, ICmObject current, ICmObject chosen)
		{
			var index = relation.TargetsRS[0] == current ? 1 : 0;
			relation.TargetsRS.Replace(index, index < relation.TargetsRS.Count ? 1 : 0, new[] { chosen });
		}

		/// <summary>
		/// Makes <paramref name="whole"/> the whole that <paramref name="part"/> belongs to.
		/// A relation of the type with that whole takes the part in; else the part's relation
		/// is retargeted when the part is its only one (LT-14926), or the part leaves it for
		/// a new relation. Choosing the part's own whole changes nothing (LT-10987).
		/// </summary>
		/// <returns>The relation the part is now in.</returns>
		public static ILexReference ReplaceTreeWhole(ILexReference relation, ICmObject part, ICmObject whole)
		{
			var type = (ILexRefType)relation.Owner;
			var existing = type.MembersOC.FirstOrDefault(r => r.TargetsRS.Count > 0 && r.TargetsRS[0] == whole);
			if (existing == null)
			{
				if (relation.TargetsRS.Count == 2)
				{
					// Setting the first slot keeps the two members, so the relation survives.
					relation.TargetsRS[0] = whole;
					return relation;
				}
				relation.TargetsRS.Remove(part);
				existing = type.Services.GetInstance<ILexReferenceFactory>().Create();
				type.MembersOC.Add(existing);
				existing.TargetsRS.Add(whole);
			}
			else
			{
				if (existing == relation)
					return relation;
				if (relation.TargetsRS.Count == 2)
					type.MembersOC.Remove(relation);
				else
					relation.TargetsRS.Remove(part);
			}
			existing.TargetsRS.Add(part);
			return existing;
		}

		/// <summary>Sets the relation's name and comment (Edit Reference Set Details) in
		/// one undoable step.</summary>
		public static void SetDetails(ILexReference relation, string name, string comment)
		{
			if (relation == null)
				throw new ArgumentNullException(nameof(relation));
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoEditRefSetDetails, FdoUiStrings.ksRedoEditRefSetDetails, relation, () =>
			{
				relation.Name.SetAnalysisDefaultWritingSystem(name);
				relation.Comment.SetAnalysisDefaultWritingSystem(comment);
			});
		}
	}
}
