// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002-2008, SIL International. All Rights Reserved.
// <copyright from='2002' to='2008' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: OverridesLing.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// This file holds the overrides of the generated classes for the Ling module.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml; // XMLWriter
using System.Drawing;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Validation;
using SIL.CoreImpl; // XmlUtils

namespace SIL.FieldWorks.FDO.DomainImpl
{
	internal partial class LexDb
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Entries
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntry")]
		public IEnumerable<ILexEntry> Entries
		{
			get { return Services.GetInstance<ILexEntryRepository>().AllInstances(); }
		}

		/// <summary>
		/// Gets all the bulk-editable things that might be used as the destination of a bulk edit to
		/// example sentence properties. This includes the senses that do not have examples.
		/// Note that this implementation is only possible because there are no other possible owners for LexExampleSentence.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmObject")]
		public IEnumerable<ICmObject> AllExampleSentenceTargets
		{
			get
			{
				// Optimize JohnT: are we likely to modify any of the iterators while iterating? If not
				// we may not need the ToList().
				return Cache.ServiceLocator.GetInstance<ILexExampleSentenceRepository>().AllInstances().Cast<ICmObject>()
					.Concat((from sense in Cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances()
							 where sense.ExamplesOS.Count == 0
							 select sense).Cast<ICmObject>())
					.ToList();
			}
		}

		/// <summary>
		/// Gets all the bulk-editable things that might be used as the destination of a bulk edit to
		/// example translation properties. This includes the examples that do not have translations.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmObject")]
		public IEnumerable<ICmObject> AllExampleTranslationTargets
		{
			get
			{
				var examples = Cache.ServiceLocator.GetInstance<ILexExampleSentenceRepository>().AllInstances().ToList();
				// Optimize JohnT: are we likely to modify any of the iterators while iterating? If not
				// we may not need the ToList().
				return (from ex in examples from trans in ex.TranslationsOC select trans).Cast<ICmObject>()
					.Concat((from ex in examples
							 where ex.TranslationsOC.Count == 0
							 select ex).Cast<ICmObject>())
					.ToList();
			}
		}

		/// <summary>
		/// Gets all the bulk-editable things that might be used as the destination of a bulk edit to
		/// LexEntryRef properties for complex forms. This includes the entries that do not have any
		/// EntryRefs with the right type.
		/// Note that this implementation only works because LexEntryRef has no other possible owners.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmObject")]
		public IEnumerable<ICmObject> AllComplexEntryRefPropertyTargets
		{
			get
			{
				// Optimize JohnT: are we likely to modify any of the iterators while iterating? If not
				// we may not need the ToList().
				return Cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances()
						.Where(ler => ler.RefType == LexEntryRefTags.krtComplexForm).Cast<ICmObject>()
					.Concat((from entry in Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances()
							 where entry.EntryRefsOS.Where(ler => ler.RefType == LexEntryRefTags.krtComplexForm).Count() == 0
							 select entry).Cast<ICmObject>())
					.ToList();
			}
		}

		/// <summary>
		/// Gets all the bulk-editable things that might be used as the destination of a bulk edit to
		/// LexEntryRef properties for variants. This includes the entries that do not have any
		/// EntryRefs with the right type.
		/// Note that this implementation only works because LexEntryRef has no other possible owners.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmObject")]
		public IEnumerable<ICmObject> AllVariantEntryRefPropertyTargets
		{
			get
			{
				// Optimize JohnT: are we likely to modify any of the iterators while iterating? If not
				// we may not need the ToList().
				return Cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances()
						.Where(ler => ler.RefType == LexEntryRefTags.krtVariant).Cast<ICmObject>()
					.Concat((from entry in Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances()
							 where entry.EntryRefsOS.Where(ler => ler.RefType == LexEntryRefTags.krtVariant).Count() == 0
							 select entry).Cast<ICmObject>())
					.ToList();
			}
		}

		/// <summary>
		/// Gets all the bulk-editable things that might be used as the destination of a bulk edit to
		/// pronunciation. This includes the entries that do not have pronunciations.
		/// Note that this implementation is only possible because there are no other possible owners for LexPronunciation.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmObject")]
		public IEnumerable<ICmObject> AllPossiblePronunciations
		{
			get
			{
				// Optimize JohnT: are we likely to modify any of the iterators while iterating? If not
				// we may not need the ToList().
				return Cache.ServiceLocator.GetInstance<ILexPronunciationRepository>().AllInstances().Cast<ICmObject>()
					.Concat((from entry in Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances()
							 where entry.PronunciationsOS.Count == 0
							 select entry).Cast<ICmObject>())
					.ToList();
			}
		}

		/// <summary>
		/// Gets all the bulk-editable things that might be used as the destination of a bulk edit to
		/// Allomorphs. This includes the entries that do not have allomorphs. It does NOT include
		/// MoForms that are the LexemeForm of some entry. (Possibly the name should indicate this better somehow?)
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmObject")]
		public IEnumerable<ICmObject> AllPossibleAllomorphs
		{
			get
			{
				var entries = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToList();
				// Optimize JohnT: are we likely to modify any of the iterators while iterating? If not
				// we may not need the ToList().
				return (from entry in entries from morph in entry.AlternateFormsOS select morph).Cast<ICmObject>()
					.Concat((from entry in entries
							 where entry.AlternateFormsOS.Count == 0
							 select entry).Cast<ICmObject>())
					.ToList();
			}
		}

		/// <summary>
		/// All the senses in existence. Sometimes convenient to have as a virtual property.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexSense")]
		public IEnumerable<ILexSense> AllSenses
		{
			get
			{
				return m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances();
			}
		}

		/// <summary>
		/// All the Complex and Variant entries
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> AllEntryRefs
		{
			get
			{
				return m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances();
			}
		}

		/// <summary>
		/// Get (create if necessary, in a non-undoable UOW if necessary) PublicationTypesOA in the default state.
		/// </summary>
		[ModelProperty(CellarPropertyType.OwningAtomic, 5005024, "CmPossibilityList")]
		public ICmPossibilityList PublicationTypesOA
		{
			get
			{
				if (PublicationTypesOA_Generated != null)
					return PublicationTypesOA_Generated;
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
					() =>
						{
							PublicationTypesOA_Generated = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
							var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
							// Note: we don't need to localize here because we are deliberately creating a minimal English
							// version of the list. The user can fill in other writing systems if desired.
							PublicationTypesOA_Generated.Name.set_String(wsEn, "Publications");
							var mainDict = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
							PublicationTypesOA_Generated.PossibilitiesOS.Add(mainDict);
							mainDict.Name.set_String(wsEn, "Main Dictionary");

							// The following are not explicitly tested.
							PublicationTypesOA_Generated.ItemClsid = CmPossibilityTags.kClassId;
							PublicationTypesOA_Generated.Depth = 1;
							PublicationTypesOA_Generated.IsSorted = true;
							PublicationTypesOA_Generated.PreventChoiceAboveLevel = 0;

							mainDict.Abbreviation.set_String(wsEn, "Main");
							mainDict.IsProtected = true;
						}
					);
				return PublicationTypesOA_Generated;
			}
			set { PublicationTypesOA_Generated = value; }
		}


		/// <summary>
		/// Resets the homograph numbers for all entries.
		/// </summary>
		public void ResetHomographNumbers(ProgressBar progressBar)
		{
			var processedEntryIds = new List<int>();
			progressBar.Minimum = 0;
			progressBar.Maximum = Entries.Count();
			progressBar.Step = 1;
			UndoableUnitOfWorkHelper.Do(Strings.ksUndoResetHomographs, Strings.ksRedoResetHomographs, Cache.ActionHandlerAccessor,
				() =>
				{
					foreach (var le in Entries)
					{
						if (processedEntryIds.Contains(le.Hvo))
						{
							progressBar.PerformStep();
							continue;
						}

						var homographs = Services.GetInstance<ILexEntryRepository>().CollectHomographs(
							le.HomographForm,
							le.PrimaryMorphType);
						CorrectHomographNumbers(homographs);
						foreach (var homograph in homographs)
						{
							processedEntryIds.Add(homograph.Hvo);
							progressBar.PerformStep();
						}
					}
				});
		}
		///// <summary>
		///// Answer true if the input set of homographs is ordered correctly.
		///// </summary>
		//public static bool ValidateExistingHomographs(List<ILexEntry> rgHomographs)
		//{

		//    if (rgHomographs.Count == 0)
		//        return true; // Nothing to check.

		//    if (rgHomographs.Count == 1)
		//    {
		//        return (rgHomographs[0].HomographNumber == 0); // exactly one in set, should be numbered zero.
		//    }

		//    // More than one, should be numbered 1..n.
		//    var sortedHomographs = rgHomographs.OrderBy(h => h.HomographNumber).ToList();
		//    for (int n = 0; n < sortedHomographs.Count; ++n)
		//    {
		//        if (sortedHomographs[n].HomographNumber != n + 1)
		//            return false;
		//    }
		//    return true;
		//}

		/// <summary>
		/// Ensure that homograph numbers from 1 to N are set for these N homographs.
		/// This is called on both Insert Entry and Delete Entry.
		/// Caller should create unit of work.
		/// </summary>
		/// <returns>true if homographs were already valid, false if they had to be renumbered. (That is, we couldn't fix by just filing in.)
		/// </returns>
		public static bool CorrectHomographNumbers(List<ILexEntry> rgHomographs)
		{
			bool fOk = true;

			if (rgHomographs.Count == 0)
				return fOk; // Nothing to renumber.

			if (rgHomographs.Count == 1)
			{
				// Handle case where it is being set to 0.
				ILexEntry lexE = rgHomographs[0];
				if (lexE.HomographNumber != 0)
				{
					lexE.HomographNumber = 0;
					return true; // renumbered
				}
				return false; // no change
			}

			// For each possible homograph number, we first try to find an existing one that has that number.
			// If so, we leave it alone.
			// If not, and there's one without a number that can be given that number, we do so, filling in the gap
			// without renumbering others.
			// If we can't fill in all the gaps we renumber them all, and return false.
			// (Note that if two have the same number, we will certainly not be able to find one for every number,
			// so we will end up doing a total renumbering.)
			for (int n = 1; n <= rgHomographs.Count; ++n)
			{
				fOk = false;
				foreach (ILexEntry le in rgHomographs)
				{
					if (le.HomographNumber == n)
					{
						fOk = true;
						break; // from inner loop, we found one numbered n
					}
				}
				if (!fOk)
				{
					// See if one has a missing number. If so, fill it in with the
					// next needed number.
					foreach (ILexEntry le in rgHomographs)
					{
						if (le.HomographNumber == 0)
						{
							le.HomographNumber = n;
							fOk = true;
						}
					}
				}
				if (!fOk)
					break;
			}
			if (!fOk)
			{
				// Should we notify the user that we're doing this helpful renumbering for him?
				// We do our best to keep them in the same order.
				int n = 1;
				foreach (ILexEntry le in rgHomographs.OrderBy(h => h.HomographNumber).ToList())
				{
					if (le.HomographNumber != n)
						le.HomographNumber = n;
					n++;
				}
			}
			return fOk;
		}

		/// <summary>
		/// Get a filtered list of reversal indices that correspond to the current writing systems being used.
		/// </summary>
		public List<IReversalIndex> CurrentReversalIndices
		{
			get
			{
				var indices = new List<IReversalIndex>();
				foreach (var ri in ReversalIndexesOC)
				{
					if (!Cache.LanguageProject.CurrentAnalysisWritingSystems.Contains(Services.WritingSystemManager.Get(ri.WritingSystem))) continue;

					indices.Add(ri);
				}
				return indices;
			}
		}
		/// <summary>
		/// used when dumping the lexical database for the automated Parser
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "MoForm")]
		public IEnumerable<IMoForm> AllAllomorphs
		{
			get
			{
				//old system: "select id, class$ from MoForm_ order by class$";
				IMoFormRepository repo = Services.GetInstance<IMoFormRepository>();
				return
					from IMoForm mf in repo.AllInstances()
					orderby mf.ClassID
					select mf;
			}
		}

		/// <summary>
		/// used when dumping the lexical database for the automated Parser
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "MoMorphSynAnalysis")]
		public IEnumerable<IMoMorphSynAnalysis> AllMSAs
		{
			get
			{
				//old system: "select id, class$ from MoMorphSynAnalysis_ order by class$";
				IMoMorphSynAnalysisRepository repo = Services.GetInstance<IMoMorphSynAnalysisRepository>();
				return
					from IMoMorphSynAnalysis msa in repo.AllInstances()
					orderby msa.ClassID
					select msa;
			}
		}
		partial void ValidateIntroductionOA(ref IStText newObjValue)
		{
			if (newObjValue == null)
				throw new InvalidOperationException("New value must not be null.");
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class LexExampleSentence
	{
		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			if (e.Flid == LexExampleSentenceTags.kflidDoNotPublishIn)
			{
				var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
				uowService.RegisterVirtualAsModified(this, "PublishIn", PublishIn.Cast<ICmObject>());
			}
			base.AddObjectSideEffectsInternal(e);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			if (e.Flid == LexExampleSentenceTags.kflidDoNotPublishIn)
			{
				var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
				uowService.RegisterVirtualAsModified(this, "PublishIn", PublishIn.Cast<ICmObject>());
			}
			base.RemoveObjectSideEffectsInternal(e);
		}

		public override ICmObject ReferenceTargetOwner(int flid)
		{
			if (flid == Cache.MetaDataCacheAccessor.GetFieldId2(LexExampleSentenceTags.kClassId, "PublishIn", false))
				return Cache.LangProject.LexDbOA.PublicationTypesOA;
			return base.ReferenceTargetOwner(flid);
		}

		/// <summary>
		/// Object owner. This virtual may seem redundant with CmObject.Owner, but it is important,
		/// because we can correctly indicate the destination class. This is used (at least) in
		/// PartGenerator.GeneratePartsFromLayouts to determine that it needs to generate parts for LexSense.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "LexSense")]
		public ILexSense OwningSense
		{
			get { return (ILexSense)Owner; }
		}

		/// <summary>
		/// The publications from which this is not excluded, that is, the ones in which it
		/// SHOULD be published.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "CmPossibility")]
		public IFdoSet<ICmPossibility> PublishIn
		{
			get
			{
				return new FdoInvertSet<ICmPossibility>(DoNotPublishInRC, Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS);
			}
		}

		/// <summary>
		/// This is the string
		/// which is displayed in the Delete Pronunciation dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteLexExampleSentence));

				return tisb.GetString();
			}
		}

		/// <summary>
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		public string LiftResidueContent
		{
			get
			{
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					return null;
				if (sResidue.IndexOf("<lift-residue") != sResidue.LastIndexOf("<lift-residue"))
					sResidue = RepairLiftResidue(sResidue);
				return LexEntry.ExtractLiftResidueContent(sResidue);
			}
		}

		private string RepairLiftResidue(string sResidue)
		{
			int idx = sResidue.IndexOf("</lift-residue>");
			if (idx > 0)
			{
				// Remove the repeated occurrences of <lift-residue>...</lift-residue>.
				// See LT-10302.
				sResidue = sResidue.Substring(0, idx + 15);
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() => { LiftResidue = sResidue; });
			}
			return sResidue;
		}

		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateCreated
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateCreated"); }
		}

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateModified
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateModified"); }
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class LexPronunciation
	{
		/// <summary>
		/// Object owner. This virtual may seem redundant with CmObject.Owner, but it is important,
		/// because we can correctly indicate the destination class. This is used (at least) in
		/// PartGenerator.GeneratePartsFromLayouts to determine that it needs to generate parts for LexEntry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "LexEntry")]
		public ILexEntry OwningEntry
		{
			get { return (ILexEntry)Owner; }
		}

		/// <summary>
		/// This is the string
		/// which is displayed in the Delete Pronunciation dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteLexPronunciation));
				return tisb.GetString();
			}
		}

		/// <summary>
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		public string LiftResidueContent
		{
			get
			{
				var sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					return null;
				if (sResidue.IndexOf("<lift-residue") != sResidue.LastIndexOf("<lift-residue"))
					sResidue = RepairLiftResidue(sResidue);
				return LexEntry.ExtractLiftResidueContent(sResidue);
			}
		}

		private string RepairLiftResidue(string sResidue)
		{
			int idx = sResidue.IndexOf("</lift-residue>");
			if (idx > 0)
			{
				// Remove the repeated occurrences of <lift-residue>...</lift-residue>.
				// See LT-10302.
				sResidue = sResidue.Substring(0, idx + 15);
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() => { LiftResidue = sResidue; });
			}
			return sResidue;
		}

		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateCreated
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateCreated"); }
		}

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateModified
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateModified"); }
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case LexPronunciationTags.kflidLocation:
					return m_cache.LangProject.LocationsOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
	}

	/// <summary></summary>
	internal partial class LexEntry
	{
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();

			RegisterVirtualsModifiedForObjectCreation(((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService);
		}

		public override ICmObject ReferenceTargetOwner(int flid)
		{
			if (flid == Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PublishIn", false) ||
				flid == Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "ShowMainEntryIn", false))
				return Cache.LangProject.LexDbOA.PublicationTypesOA;
			return base.ReferenceTargetOwner(flid);
		}

		/// <summary>
		/// Answer true if the entry is, directly or indirectly, a component of this entry.
		/// The intent is to use this in reporting that it would be incorrect to make this
		/// a component of the specified entry.
		/// Accordingly, as a special case, it answers true if the entry is the recipient,
		/// even if it has no components.
		/// </summary>
		public bool IsComponent(ILexEntry entry)
		{
			return AllComponents.Contains(entry);
		}

		/// <summary>
		/// Return all the entries which this may not be a component of since they
		/// are or are related to components of this.
		/// </summary>
		IEnumerable<ILexEntry> AllComponents
		{
			get
			{
				yield return this;
				foreach (var ler in EntryRefsOS)
				{
					if (ler.RefType != LexEntryRefTags.krtComplexForm)
						continue; // Enhance JohnT: should we report circular variants also??
					foreach (var obj in ler.ComponentLexemesRS)
					{
						if (obj is ILexEntry)
						{
							var entry = (LexEntry) obj;
							yield return entry;
							foreach (var le in entry.AllComponents)
								yield return le;
						}
						else
							yield return ((ILexSense) obj).Entry;
					}
				}
			}
		}

		internal override void RegisterVirtualsModifiedForObjectCreation(IUnitOfWorkService uow)
		{
			base.RegisterVirtualsModifiedForObjectCreation(uow);
			var cache = Cache; // need to make the Func use this local variable, because on Undo, Cache may return null.
			uow.RegisterVirtualCollectionAsModified(Cache.LangProject.LexDbOA,
				Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries,
				() => cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances(),
				new[] { this }, new ILexEntry[0]);
		}

		/// <summary>
		/// Make the other lexentry a component of this. This becomes a complex form if it is not already.
		/// If it already has components other is added and primary lexemes is not affected.
		/// If it has no components other is also put in primary lexemes.
		/// If it the complex form is not (already known to be) a derivative, also add to ShowComplexFormIn.
		/// If we need to make a new LexEntryRef it should by default not show as a minor entry.
		/// </summary>
		public void AddComponent(ICmObject other)
		{
			if (!(other is ILexEntry) && !(other is ILexSense))
				throw new ArgumentException("components of a lex entry must be entries or senses", "other");
			ILexEntryRef ler = (from item in EntryRefsOS where item.RefType == LexEntryRefTags.krtComplexForm select item).FirstOrDefault();
			if (ler == null)
			{
				ler = Services.GetInstance<ILexEntryRefFactory>().Create();
				EntryRefsOS.Add(ler);

				ler.RefType = LexEntryRefTags.krtComplexForm;
				ler.HideMinorEntry = 0; // LT-10928
				ChangeRootToStem();
			}
			if (!ler.ComponentLexemesRS.Contains(other))
				ler.ComponentLexemesRS.Add(other);
			if (ler.PrimaryLexemesRS.Count == 0)
				ler.PrimaryLexemesRS.Add(other);
			if (!ler.ComplexEntryTypesRS.Contains(Services.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypDerivation)) &&
				!ler.ShowComplexFormsInRS.Contains(other))	// Don't add it twice!  See LT-11562.
			{
				ler.ShowComplexFormsInRS.Add(other);
			}
		}

		internal IEnumerable<ILexEntryRef> EntryRefsWithThisMainEntry
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexEntryRefTags.kflidComponentLexemes);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexEntryRefTags.kflidComponentLexemes)
						yield return sequence.MainObject as ILexEntryRef;
				}
			}
		}

		internal IEnumerable<ILexEntryRef> ComplexFormRefsWithThisPrimaryEntry
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexEntryRefTags.kflidPrimaryLexemes);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexEntryRefTags.kflidPrimaryLexemes &&
						(sequence.MainObject as ILexEntryRef).RefType == LexEntryRefTags.krtComplexForm)
					{
						yield return sequence.MainObject as ILexEntryRef;
					}
				}
			}
		}

		internal IEnumerable<ILexEntryRef> ComplexFormRefsVisibleInThisEntry
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexEntryRefTags.kflidShowComplexFormsIn);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexEntryRefTags.kflidShowComplexFormsIn &&
						(sequence.MainObject as ILexEntryRef).RefType == LexEntryRefTags.krtComplexForm)
					{
						yield return sequence.MainObject as ILexEntryRef;
					}
				}
			}
		}

		internal IEnumerable<ILexReference> ReferringLexReferences
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexReferenceTags.kflidTargets);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexReferenceTags.kflidTargets)
						yield return sequence.MainObject as ILexReference;
				}
			}
		}

		/// <summary>
		/// Return the sense with the specified MSA
		/// </summary>
		public ILexSense SenseWithMsa(IMoMorphSynAnalysis msa)
		{
			return (from sense in AllSenses where sense.MorphoSyntaxAnalysisRA == msa select sense).FirstOrDefault();
		}

		/// <summary>
		/// Initialize the DateCreated and DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateCreated = DateTime.Now;
			m_DateModified = DateTime.Now;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry in ShowComplexFormIn  and are complex
		/// forms.
		/// Enhance JohnT: Generate PropChanged on this for changes to any of
		///     LexEntry.EntryRefs, LexEntryRef.RefType, LexEntryRef.PrimaryEntryOrSense,
		///     or anything that affects GetVariantEntryRefsWithMainEntryOrSense.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> VisibleComplexFormBackRefs
		{
			get
			{
				return VirtualOrderingServices.GetOrderedValue(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexEntryVisibleComplexFormBackRefs,
					((LexEntryRefRepository)Services.GetInstance<ILexEntryRefRepository>()).SortEntryRefs(ComplexFormRefsVisibleInThisEntry));
			}
		}

		/// <summary>
		/// This returns a subset of VisibleComplexFormBackRefs, specifically those that are NOT also subentries.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> ComplexFormsNotSubentries
		{
			get { return VisibleComplexFormBackRefs.Where(ler => !ler.PrimaryLexemesRS.Contains(this)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of all the LexEntryRef
		/// objects owned by this LexEntry that have HideMinorEntry set to zero.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> VisibleEntryRefs
		{
			get
			{
				return from ler in EntryRefsOS where ler.HideMinorEntry == 0 select ler;
			}
		}

		/// <summary>
		/// Return the pictures of all your senses (including subsenses).
		/// Enhance JohnT: Generate PropChanged on changes to:
		///     LexEntry.Senses, LexSense.Senses, LexSense.Pictures.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmPicture")]
		public IEnumerable<ICmPicture> PicturesOfSenses
		{
			get { return from LexSense sense in SensesOS from picture in sense.Pictures select picture; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry, or to a LexSense owned by this
		/// LexEntry (possibly indirectly), as a primary component of a complex form.
		/// These are the objects which are displayed as subentries in a root-based dictionary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> AllSubentries
		{
			get { return Services.GetInstance<ILexEntryRefRepository>().GetSubentriesOfEntryOrSense(ThisAndAllSenses); }
			}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case LexEntryTags.kflidSenses:
			{
				// The virtual property LexSenseOutline may be changed for the inserted sense and all its following senses
				// and their subsenses.
				SensesChangedPosition(e.Index);
				NumberOfSensesChanged(true);
				UpdateMorphoSyntaxAnalysesOfLexEntryRefs();
			}
					break;
				case LexEntryTags.kflidAlternateForms:
					if (e.Index == 0)
			{
				string oldVal = AlternateFormsOS.Count > 1 ? AlternateFormsOS[1].Form.VernacularDefaultWritingSystem.Text : "";
				FirstAlternateFormChanged(oldVal, ((IMoForm)e.ObjectAdded).Form.VernacularDefaultWritingSystem.Text);
			}
					break;
				case LexEntryTags.kflidDoNotPublishIn:
					{
						var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
						uowService.RegisterVirtualAsModified(this, "PublishIn", PublishIn.Cast<ICmObject>());
					}
					break;
				case LexEntryTags.kflidDoNotShowMainEntryIn:
					{
						var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
						uowService.RegisterVirtualAsModified(this, "ShowMainEntryIn", ShowMainEntryIn.Cast<ICmObject>());
					}
					break;
				case LexEntryTags.kflidEntryRefs:
					RegisterPublishAsMinorEntryVirtualChanged();
					break;
			}
			base.AddObjectSideEffectsInternal(e);
		}

		private void RegisterPublishAsMinorEntryVirtualChanged()
		{
			var uowService = ((IServiceLocatorInternal) Services).UnitOfWorkService;
			var publishAsMinorEntry = PublishAsMinorEntry;
			// We don't really know the old value, but pass the opposite so it looks changed.
			// PropChanged doesn't convey old and new values for booleans so it doesn't really matter.
			uowService.RegisterVirtualAsModified(this,
				Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PublishAsMinorEntry", false),
				!publishAsMinorEntry, publishAsMinorEntry);
		}

		/// <summary>
		/// Do side effects resulting from changes to the position of senses from startIndex onwards.
		/// </summary>
		private void SensesChangedPosition(int startIndex)
		{
			int startAt = startIndex;
			// If the second one is modified, the first may be affected too (e.g. we were not displaying a number at all
			// and now show it as sense 1).
			if (startIndex == 1)
				startAt--;
			for (int i = startAt; i < SensesOS.Count; i++)
				((LexSense) SensesOS[i]).LexSenseOutlineChanged();
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case LexEntryTags.kflidSenses:
			{
				// The virtual property LexSenseOutline may be changed for the senses after the deleted one
				// and their subsenses.
				if (!Cache.ObjectsBeingDeleted.Contains(this))
				{
					SensesChangedPosition(e.Index);
					NumberOfSensesChanged(false);
					UpdateMorphoSyntaxAnalysesOfLexEntryRefs();
				}
			}
					break;
				case LexEntryTags.kflidAlternateForms:
					if (e.Index == 0)
			{
				if (!Cache.ObjectsBeingDeleted.Contains(this))
				{
					string newVal = AlternateFormsOS.Count > 0
										? AlternateFormsOS[0].Form.VernacularDefaultWritingSystem.Text
										: "";
					FirstAlternateFormChanged(((IMoForm) e.ObjectRemoved).Form.VernacularDefaultWritingSystem.Text,
						newVal);
				}
			}
					break;
				case LexEntryTags.kflidDoNotPublishIn:
					{
						var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
						uowService.RegisterVirtualAsModified(this, "PublishIn", PublishIn.Cast<ICmObject>());
					}
					break;
				case LexEntryTags.kflidDoNotShowMainEntryIn:
					{
						var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
						uowService.RegisterVirtualAsModified(this, "ShowMainEntryIn", ShowMainEntryIn.Cast<ICmObject>());
					}
					break;
				case LexEntryTags.kflidEntryRefs:
					RegisterPublishAsMinorEntryVirtualChanged();
					break;
			}
			base.RemoveObjectSideEffectsInternal(e);
		}
		/// <summary>
		/// If any allomorphs have a root type (root or bound root), change them to the corresponding stem type.
		/// </summary>
		public void ChangeRootToStem()
		{
			foreach (var mf in AllAllomorphs)
				mf.ChangeRootToStem();
		}

		/// <summary>
		/// Determines if the entry is a circumfix
		/// </summary>
		/// <returns></returns>
		public bool IsCircumfix()
		{
			var form = LexemeFormOA;
			if (form != null)
			{
				var type = form.MorphTypeRA;
				if (type != null)
				{
					if (type.Guid == MoMorphTypeTags.kguidMorphCircumfix)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Set the specified WS of the form of your LexemeForm, making sure not to include any
		/// morpheme break characters. As a special case, if your LexemeForm is a circumfix,
		/// do not strip morpheme break characters, and also try to set the form of prefix and suffix.
		/// </summary>
		public void SetLexemeFormAlt(int ws, ITsString tssLexemeFormIn)
		{
			var tssLexemeForm = tssLexemeFormIn;
			var mf = LexemeFormOA;
			if (IsCircumfix())
			{
				IMoForm mfPrefix = null;
				IMoForm mfSuffix = null;
				foreach (var mfT in AllAllomorphs)
				{
					if (mfPrefix == null && mfT.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphPrefix)
						mfPrefix = mfT;
					if (mfSuffix == null && mfT.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphSuffix)
						mfSuffix = mfT;
				}
				string sLeftMember;
				string sRightMember;
				if (!StringServices.GetCircumfixLeftAndRightParts(Cache, tssLexemeForm, out sLeftMember, out sRightMember))
					return;
				if (mfPrefix != null)
					mfPrefix.Form.set_String(ws, MorphServices.EnsureNoMarkers(sLeftMember.Trim(), Cache));
				if (mfSuffix != null)
					mfSuffix.Form.set_String(ws, MorphServices.EnsureNoMarkers(sRightMember.Trim(), Cache));
			}
			else
			{
				// Normal non-circumfix case, set the appropriate alternative on the Lexeme form itself
				// (making sure to include no invalid characters).
				tssLexemeForm = Cache.TsStrFactory.MakeString(MorphServices.EnsureNoMarkers(tssLexemeForm.Text, m_cache), ws);
			}
			if (mf != null)
				mf.Form.set_String(ws, tssLexemeForm);
		}

		/// <summary>
		/// Tells whether this LexEntry contains an inflectional affix MSA
		/// </summary>
		public bool SupportsInflectionClasses()
		{
			foreach (IMoMorphSynAnalysis item in MorphoSyntaxAnalysesOC)
			{
				if ((item is IMoInflAffMsa && (item as IMoInflAffMsa).PartOfSpeechRA != null)
					|| (item is IMoDerivAffMsa && (item as IMoDerivAffMsa).FromInflectionClassRA != null))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Make this entry a variant of the given componentLexeme (primary entry or sense) with
		/// the given variantType
		/// </summary>
		/// <param name="componentLexeme"></param>
		/// <param name="variantType"></param>
		public ILexEntryRef MakeVariantOf(IVariantComponentLexeme componentLexeme, ILexEntryType variantType)
		{
			var ler = Services.GetInstance<ILexEntryRefFactory>().Create();
			EntryRefsOS.Add(ler);
			ler.RefType = LexEntryRefTags.krtVariant; // variant by default, but good to be explicit here.
			ler.HideMinorEntry = 0;
			ler.ComponentLexemesRS.Add(componentLexeme);
			if (variantType != null)
				ler.VariantEntryTypesRS.Add(variantType);
			return ler;
		}

		/// <summary>
		/// If entry has a LexemeForm, that type is primary and should be used for new ones (LT-4872).
		/// </summary>
		/// <returns></returns>
		public int GetDefaultClassForNewAllomorph()
		{
			// TODO: what about entries with mixed morph types?
			var morphType = PrimaryMorphType;
			return (morphType == null || morphType.IsStemType) ? MoStemAllomorphTags.kClassId : MoAffixAllomorphTags.kClassId;
		}

		/// <summary>
		/// Determines whether the entry is in a variant relationship with the given sense (or its entry).
		/// </summary>
		/// <param name="senseTargetComponent">the sense of which we are possibly a variant. If we aren't a variant of the sense,
		/// we will try to see if we are a variant of its owner entry</param>
		/// <param name="matchinEntryRef">if we found a match, the first (and only) ComponentLexeme will have matching sense or its owner entry.</param>
		/// <returns></returns>
		public bool IsVariantOfSenseOrOwnerEntry(ILexSense senseTargetComponent, out ILexEntryRef matchinEntryRef)
		{
			matchinEntryRef = null;
			if (senseTargetComponent != null && senseTargetComponent.Hvo != 0 && senseTargetComponent.EntryID != this.Hvo)
			{
				// expect hvoLexEntry to be a variant of the sense or the sense's entry.
				matchinEntryRef = FindMatchingVariantEntryRef(senseTargetComponent, null);
				if (matchinEntryRef == null)
				{
					// must be in relationship with the sense's entry, rather than the sense.
					matchinEntryRef = FindMatchingVariantEntryRef(senseTargetComponent.Entry, null);
				}
			}
			return matchinEntryRef != null;
		}

		/// <summary>
		/// This replaces a MoForm belonging to this LexEntry with another one, presumably
		/// changing from a stem to an affix or vice versa.  (A version of this code originally
		/// appeared in MorphTypeAtomicLauncher.cs, but is also needed for LIFT import.)
		/// </summary>
		/// <param name="mfOld"></param>
		/// <param name="mfNew"></param>
		public void ReplaceMoForm(IMoForm mfOld, IMoForm mfNew)
		{
			// save the environment references, if any.
			IEnumerable<IPhEnvironment> envs = null;
			if (mfOld is IMoStemAllomorph)
				envs = (mfOld as IMoStemAllomorph).PhoneEnvRC.ToArray();
			else if (mfOld is IMoAffixAllomorph)
				envs = (mfOld as IMoAffixAllomorph).PhoneEnvRC.ToArray();

			IEnumerable<IMoInflClass> inflClasses = null;
			if (mfOld is IMoAffixForm)
				inflClasses = (mfOld as IMoAffixForm).InflectionClassesRC.ToArray();

			// if we are converting from one affix form to another, we should save the morph type
			IMoMorphType oldAffMorphType = null;
			if (mfOld is IMoAffixForm)
				oldAffMorphType = mfOld.MorphTypeRA;


			if (mfOld.OwningFlid == LexEntryTags.kflidLexemeForm)
			{
				AlternateFormsOS.Add(mfNew); // trick to get it to be in DB so SwapReferences works
			}
			else
			{
				// insert the new form in the right location in the sequence.
				Debug.Assert(mfOld.OwningFlid == LexEntryTags.kflidAlternateForms);
				int index = AlternateFormsOS.IndexOf(mfOld);
				if (index != -1)
					AlternateFormsOS.Insert(index, mfNew);
				else
					AlternateFormsOS.Add(mfNew);	// This should NEVER happen, but...
			}
			mfOld.SwapReferences(mfNew);
			var muaOrigForm = mfOld.Form;
			var muaNewForm = mfNew.Form;
			muaNewForm.MergeAlternatives(muaOrigForm);
			if (mfOld.OwningFlid == LexEntryTags.kflidLexemeForm)
				LexemeFormOA = mfNew;
			else
				AlternateFormsOS.Remove(mfOld);

			// restore the environment references, if any.
			if (envs != null)
			{
				foreach (var env in envs)
				{
					if (mfNew is IMoStemAllomorph)
						(mfNew as IMoStemAllomorph).PhoneEnvRC.Add(env);
					else if (mfNew is IMoAffixAllomorph)
						(mfNew as IMoAffixAllomorph).PhoneEnvRC.Add(env);
				}
			}

			if (inflClasses != null)
			{
				foreach (var inflClass in inflClasses)
				{
					if (mfNew is IMoAffixForm)
						(mfNew as IMoAffixForm).InflectionClassesRC.Add(inflClass);
				}
			}

			if (oldAffMorphType != null && mfNew is IMoAffixForm)
				mfNew.MorphTypeRA = oldAffMorphType;
		}

		/// <summary>
		/// Create stem MSAs to replace affix MSAs, and/or create affix MSAs to replace stem
		/// MSAs. This is harder than it looks, since references to MSAs can occur in several
		/// places, and all of them need to be updated.
		/// </summary>
		/// <param name="rgmsaOld">list of bad MSAs which need to be replaced</param>
		public void ReplaceObsoleteMsas(List<IMoMorphSynAnalysis> rgmsaOld)
		{
			var mapOldToNewMsa = new Dictionary<IMoMorphSynAnalysis, IMoMorphSynAnalysis>(rgmsaOld.Count);
			// Replace all the affix type MSAs with corresponding stem MSAs, and all stem MSAs
			// with corresponding unclassified affix MSAs.  Only the PartOfSpeech is preserved
			// in this transformation.
			foreach (var msa in rgmsaOld)
			{
				IMoMorphSynAnalysis newMsa;
				if (msa is IMoStemMsa)
					newMsa = FindOrCreateMatchingAffixMsa(msa as IMoStemMsa);
				else
					newMsa = FindOrCreateMatchingStemMsa(msa);
				mapOldToNewMsa[msa] = newMsa;
			}
			UpdateMsaReferences(mapOldToNewMsa);
			// Remove the old, obsolete MSAs.
			foreach (var msa in rgmsaOld)
			{
				if (msa.IsValidObject)
					MorphoSyntaxAnalysesOC.Remove(msa);
			}
		}

		private void UpdateMsaReferences(Dictionary<IMoMorphSynAnalysis, IMoMorphSynAnalysis> mapOldToNewMsa)
		{
			if (mapOldToNewMsa.Keys.Count == 0)
				return;

			foreach (var sense in AllSenses)
			{
				IMoMorphSynAnalysis newMsa;
				if (mapOldToNewMsa.TryGetValue(sense.MorphoSyntaxAnalysisRA, out newMsa))
					sense.MorphoSyntaxAnalysisRA = newMsa;
			}

			foreach (var mb in Services.GetInstance<IWfiMorphBundleRepository>().AllInstances())
			{
				IMoMorphSynAnalysis newMsa;
				if (mb.MsaRA != null &&  mapOldToNewMsa.TryGetValue(mb.MsaRA, out newMsa))
					mb.MsaRA = newMsa;
			}

			foreach (var adhocProhib in Services.GetInstance<IMoMorphAdhocProhibRepository>().AllInstances())
			{
				IMoMorphSynAnalysis newMsa;
				if (mapOldToNewMsa.TryGetValue(adhocProhib.FirstMorphemeRA, out newMsa))
					adhocProhib.FirstMorphemeRA = newMsa;
				for (int i = 0; i < adhocProhib.MorphemesRS.Count; i++)
				{
					if (mapOldToNewMsa.TryGetValue(adhocProhib.MorphemesRS[i], out newMsa))
						adhocProhib.MorphemesRS.Replace(i, 1, new IMoMorphSynAnalysis[] { newMsa });
				}
				for (int i = 0; i < adhocProhib.RestOfMorphsRS.Count; i++)
				{
					if (mapOldToNewMsa.TryGetValue(adhocProhib.RestOfMorphsRS[i], out newMsa))
						adhocProhib.RestOfMorphsRS.Replace(i, 1, new IMoMorphSynAnalysis[] { newMsa });
				}
			}

			foreach (var msa in MorphoSyntaxAnalysesOC)
			{
				for (int i = 0; i < msa.ComponentsRS.Count; i++)
				{
					IMoMorphSynAnalysis newMsa;
					if (mapOldToNewMsa.TryGetValue(msa.ComponentsRS[i], out newMsa))
						msa.ComponentsRS.Replace(i, 1, new IMoMorphSynAnalysis[] { newMsa });
				}
			}
		}

		private IMoMorphSynAnalysis FindOrCreateMatchingAffixMsa(IMoStemMsa msa)
		{
			var POS = msa.PartOfSpeechRA;
			foreach (var msaT in MorphoSyntaxAnalysesOC)
			{
				var msaAffix = msaT as IMoUnclassifiedAffixMsa;
				if (msaAffix != null && msaAffix.PartOfSpeechRA == POS)
					return msaAffix;
			}
			var msaNew = new MoUnclassifiedAffixMsa();
			MorphoSyntaxAnalysesOC.Add(msaNew);
			msaNew.PartOfSpeechRA = POS;
			return msaNew;
		}

		private IMoMorphSynAnalysis FindOrCreateMatchingStemMsa(IMoMorphSynAnalysis msa)
		{
			IPartOfSpeech POS = null;
			if (msa is IMoInflAffMsa)
				POS = (msa as IMoInflAffMsa).PartOfSpeechRA;
			else if (msa is IMoDerivAffMsa)
				POS = (msa as IMoDerivAffMsa).ToPartOfSpeechRA;
			else if (msa is IMoDerivStepMsa)
				POS = (msa as IMoDerivStepMsa).PartOfSpeechRA;
			else if (msa is IMoUnclassifiedAffixMsa)
				POS = (msa as IMoUnclassifiedAffixMsa).PartOfSpeechRA;
			foreach (var msaT in MorphoSyntaxAnalysesOC)
			{
				var msaStem = msaT as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRA == POS &&
					msaStem.FromPartsOfSpeechRC.Count == 0 &&
					msaStem.InflectionClassRA == null &&
					msaStem.ProdRestrictRC.Count == 0 &&
					msaStem.StratumRA == null &&
					msaStem.MsFeaturesOA == null)
				{
					return msaStem;
				}
			}
			var msaNew = new MoStemMsa();
			MorphoSyntaxAnalysesOC.Add(msaNew);
			msaNew.PartOfSpeechRA = POS;
			return msaNew;
		}

		/// <summary>
		/// Get the appropriate default MoMorphSynAnalysis belonging to this
		/// entry, creating it if necessary.
		/// </summary>
		/// <returns></returns>
		public IMoMorphSynAnalysis FindOrCreateDefaultMsa()
		{
			// Search for an appropriate MSA already existing for the LexEntry.
			// TODO: what about entries with mixed morph types?
			var type = PrimaryMorphType;
			foreach (var msa in MorphoSyntaxAnalysesOC)
			{
				if (type != null && type.IsAffixType)
				{
					if (msa is IMoUnclassifiedAffixMsa)
						return msa;
				}
				else
				{
					if (msa is IMoStemMsa)
						return msa;
				}
			}
			// Nothing exists, create the needed MSA.
			MoMorphSynAnalysis msaNew;
			if (type != null && type.IsAffixType)
				msaNew = new MoUnclassifiedAffixMsa();
			else
				msaNew = new MoStemMsa();
			MorphoSyntaxAnalysesOC.Add(msaNew);

			return msaNew;
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			if (EntryRefsOS.Count == 0)
					return (flid == LexEntryTags.kflidSenses); // Main entry must have senses.
			else
					return (flid == LexEntryTags.kflidMainEntriesOrSenses); // Minor or subentry must be related to a main one.
		}

		/// <summary>
		/// Creates a new lex entry which copies most of the fields of this one, except Senses and MorphoSyntaxAnalyses.
		/// The specified sense, which must be a sense of this, along with its children, is moved to become the sole
		/// top-level sense of the new entry. Whatever MSAs are needed by the moved sense(s) are copied to the new
		/// entry also.
		/// Enhance JohnT: Create a test for this (it was fairly mechanically ported from 6.0, where there was also no test).
		/// </summary>
		public void MoveSenseToCopy(ILexSense ls)
		{
			UndoableUnitOfWorkHelper.Do(Strings.ksUndoCreateEntry, Strings.ksRedoCreateEntry, ls.Cache.ActionHandlerAccessor,
				() =>
					{
						// Copy all the basic properties.
						ILexEntry leNew = ls.Services.GetInstance<ILexEntryFactory>().Create();
						leNew.CitationForm.MergeAlternatives(this.CitationForm);
						leNew.Bibliography.MergeAlternatives(this.Bibliography);
						leNew.Comment.MergeAlternatives(this.Comment);
						leNew.DoNotUseForParsing = this.DoNotUseForParsing;
						leNew.LiteralMeaning.MergeAlternatives(this.LiteralMeaning);
						leNew.Restrictions.MergeAlternatives(this.Restrictions);
						leNew.SummaryDefinition.MergeAlternatives(this.SummaryDefinition);
						// Copy the reference attributes.

						// Copy the owned attributes carefully.
						if (LexemeFormOA != null)
							CopyObject<IMoForm>.CloneFdoObject(LexemeFormOA, newForm => leNew.LexemeFormOA = newForm);

						CopyObject<IMoForm>.CloneFdoObjects(AlternateFormsOS, newForm => leNew.AlternateFormsOS.Add(newForm));
						CopyObject<ILexPronunciation>.CloneFdoObjects(PronunciationsOS, newPron => leNew.PronunciationsOS.Add(newPron));
						CopyObject<ILexEntryRef>.CloneFdoObjects(EntryRefsOS, newEr => leNew.EntryRefsOS.Add(newEr));

						if (EtymologyOA != null)
							CopyObject<ILexEtymology>.CloneFdoObject(EtymologyOA, newEtymology => leNew.EtymologyOA = newEtymology);

						var msaCorrespondence = new Dictionary<IMoMorphSynAnalysis, IMoMorphSynAnalysis>(4);
						(leNew as LexEntry).ReplaceMsasForSense(ls, msaCorrespondence);
						foreach (var sense in ls.AllSenses)
						{
							foreach (var source in sense.ReferringObjects)
							{
								var mb = source as WfiMorphBundle;
								if (mb == null || mb.MorphRA == null)
									continue;
								if (mb.MorphRA == LexemeFormOA)
									mb.MorphRA = leNew.LexemeFormOA;
								else // point to the corresponding allomorph
									mb.MorphRA = leNew.AlternateFormsOS[AlternateFormsOS.IndexOf(mb.MorphRA)];
							}
						}
						leNew.SensesOS.Add(ls); // moves it
					});
		}
		/// <summary>
		/// This goes through the LexSense and all its subsenses to create the needed MSAs on
		/// the current LexEntry, replacing all those found in the LexSense.
		/// The top-level call should pass a new, empty dictionary, which is passed on to child senses
		/// with oldMsa->newMsa correspondences added.
		/// </summary>
		private void ReplaceMsasForSense(ILexSense ls, Dictionary<IMoMorphSynAnalysis, IMoMorphSynAnalysis> msaCorrespondence)
		{
			var msaOld = ls.MorphoSyntaxAnalysisRA;
			if (msaOld != null)
			{
				IMoMorphSynAnalysis msaNew;
				if (!msaCorrespondence.TryGetValue(msaOld, out msaNew))
				{
					msaNew = CopyObject<IMoMorphSynAnalysis>.CloneFdoObject(msaOld,
						newMsa => MorphoSyntaxAnalysesOC.Add(newMsa));
					msaCorrespondence[msaOld] = msaNew;
				}
				ls.MorphoSyntaxAnalysisRA = msaNew;
			}
			foreach (var sense in ls.SensesOS)
			{
				ReplaceMsasForSense(sense, msaCorrespondence);
			}
		}
		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmaion dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteLexEntry, " "));
				tisb.AppendTsString(ShortNameTSS);

				var countedObjectIDs = new List<int>();
				var analCount = 0;
				var alloAHPCount = 0;
				var morphemeAHPCount = 0;

				// Spin through lots of back references.
				var servLoc = m_cache.ServiceLocator;
				var analyses =
					servLoc.GetInstance<IWfiAnalysisRepository>().AllInstances().Where(
						anal => anal.StemsRC.Contains(this));
				foreach (var anal in analyses)
				{
					countedObjectIDs.Add(anal.Hvo);
					++analCount;
				}

				var aaps = servLoc.GetInstance<IMoAlloAdhocProhibRepository>().AllInstances();
				foreach (var map in aaps.Where(m => m.FirstAllomorphRA == LexemeFormOA))
				{
					if (countedObjectIDs.Contains(map.Hvo)) continue;

					countedObjectIDs.Add(map.Hvo);
					++alloAHPCount;
				}
				var bundles = servLoc.GetInstance<IWfiMorphBundleRepository>().AllInstances();
				foreach (var mb in bundles.Where(m => m.MorphRA == LexemeFormOA))
				{
					if (countedObjectIDs.Contains(mb.Owner.Hvo)) continue;

					countedObjectIDs.Add(mb.Owner.Hvo);
					++analCount;
				}
				foreach (var fm in AlternateFormsOS)
				{
					foreach (var map in aaps.Where(x => x.FirstAllomorphRA == fm))
					{
						if (countedObjectIDs.Contains(map.Hvo)) continue;

						countedObjectIDs.Add(map.Hvo);
						++alloAHPCount;
					}
					foreach (var mb in bundles.Where(m => m.MorphRA == fm))
					{
						if (!countedObjectIDs.Contains(mb.Owner.Hvo))
						{
							countedObjectIDs.Add(mb.Owner.Hvo);
							++analCount;
						}
					}
				}
				var maps = servLoc.GetInstance<IMoMorphAdhocProhibRepository>().AllInstances();
				foreach (var msa in MorphoSyntaxAnalysesOC)
				{
					foreach (var map in maps.Where(m => m.FirstMorphemeRA == msa))
					{
						if (countedObjectIDs.Contains(map.Hvo)) continue;

						countedObjectIDs.Add(map.Hvo);
						++morphemeAHPCount;
					}
					foreach (var map in maps.Where(m => m.RestOfMorphsRS.Contains(msa)))
					{
						if (countedObjectIDs.Contains(map.Hvo)) continue;

						countedObjectIDs.Add(map.Hvo);
						++morphemeAHPCount;
					}
					foreach (var mb in bundles.Where(m => m.MsaRA == msa))
					{
						if (countedObjectIDs.Contains(mb.Owner.Hvo)) continue;

						countedObjectIDs.Add(mb.Owner.Hvo);
						++analCount;
					}
				}
				foreach (var ls in AllSenses)
				{
					foreach (var mb in bundles.Where(m => m.SenseRA == ls))
					{
						if (countedObjectIDs.Contains(mb.Owner.Hvo)) continue;

						countedObjectIDs.Add(mb.Owner.Hvo);
						++analCount;
					}
				}

				var cnt = 1;
				var warningMsg = String.Format("{0}{0}{1}{0}{2}", StringUtils.kChHardLB,
											   Strings.ksEntryUsedHere, Strings.ksDelEntryDelThese);
				var wantMainWarningLine = true;
				// Create a string with its own run of properties, so we don't carry on tisb's properties.
				// Otherwise, we might append the string as a superscript, running with the homograph properties (cf. LT-3177).
				var tisb2 = TsIncStrBldrClass.Create();
				if (analCount > 0)
				{
					tisb2.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					tisb2.Append(warningMsg);
					tisb2.Append(StringUtils.kChHardLB.ToString());
					if (analCount > 1)
						tisb2.Append(String.Format(Strings.ksIsUsedXTimesByAnalyses, cnt++, analCount));
					else
						tisb2.Append(String.Format(Strings.ksIsUsedOnceByAnalyses, cnt++));
					wantMainWarningLine = false;
				}
				if (morphemeAHPCount > 0)
				{
					tisb2.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb2.Append(warningMsg);
					tisb2.Append(StringUtils.kChHardLB.ToString());
					if (morphemeAHPCount > 1)
						tisb2.Append(String.Format(Strings.ksIsUsedXTimesByMorphAdhoc,
												   cnt++, morphemeAHPCount, StringUtils.kChHardLB));
					else
						tisb2.Append(String.Format(Strings.ksIsUsedOnceByMorphAdhoc,
												   cnt++, StringUtils.kChHardLB));
					wantMainWarningLine = false;
				}
				if (alloAHPCount > 0)
				{
					tisb2.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb2.Append(warningMsg);
					tisb2.Append(StringUtils.kChHardLB.ToString());
					if (alloAHPCount > 1)
						tisb2.Append(String.Format(Strings.ksIsUsedXTimesByAlloAdhoc,
												   cnt++, alloAHPCount, StringUtils.kChHardLB));
					else
						tisb2.Append(String.Format(Strings.ksIsUsedOnceByAlloAdhoc,
												   cnt++, StringUtils.kChHardLB));
				}
				if (tisb2.Text != null) // otherwise GetString may fail, due to no writing system
					tisb.AppendTsString(tisb2.GetString());

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Answer all the homographs of this, not including itself.
		/// </summary>
		/// <returns></returns>
		public List<ILexEntry> CollectHomographs()
		{
			return CollectHomographs(HomographForm, Hvo);
		}

		/// <summary>
		/// Collect all the homographs of the given form, except the one whose HVO is hvoExclude (often this).
		/// </summary>
		public List<ILexEntry> CollectHomographs(string sForm, int hvoExclude)
		{
			var result = Services.GetInstance<ILexEntryRepository>().GetHomographs(sForm);
			int index = result.FindIndex(0, result.Count, entry => entry.Hvo == hvoExclude);
			if (index != -1)
				result.RemoveAt(index);
			return result;
		}

		partial void LexemeFormOASideEffects(IMoForm oldObjValue, IMoForm newObjValue)
		{
			string oldVal = oldObjValue == null ? "" : oldObjValue.Form.VernacularDefaultWritingSystem.Text;
			string newVal = newObjValue == null ? "" : newObjValue.Form.VernacularDefaultWritingSystem.Text;
			LexemeFormChanged(oldVal, newVal);
		}

		/// <summary>
		/// Something happened which may cause a change to the MLHeadword Virtual property for the specified writing system.
		/// </summary>
		/// <param name="ws"></param>
		internal void MLHeadwordChanged(int ws)
		{
			// Enhance JohnT: is there some way to pass a valid old value? Does it matter?
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(this,
				Cache.ServiceLocator.GetInstance<Virtuals>().LexEntryMLHeadWord, ws, null, MLHeadWord.get_String(ws));
			((LexEntryRepository)Cache.ServiceLocator.GetInstance<ILexEntryRepository>()).SomeHeadWordChanged();
		}

		/// <summary>
		/// Handle a (possible) change to the lexeme form's default vern WS. Arguments are
		/// the old and new default vernacular lexeme form.
		/// </summary>
		internal void LexemeFormChanged(string oldForm, string newForm)
		{
			if (oldForm == newForm)
				return; // some calls may not result from a change to that alternative.
			if (CitationForm.VernacularDefaultWritingSystem.Length > 0)
				return; // HomographForm is determined by CF and so has not changed.
			string oldVal = oldForm;
			if (string.IsNullOrEmpty(oldVal))
			{
				// The old value is determined in the way HomographForm does if there is no CF or LF.
				oldVal = StringServices.FirstAlternateForm(this, Cache.DefaultVernWs);
			}
			UpdateHomographs(oldVal);
			// Another consequence is that the MLOwnerOutlineName of senses will change.
			// Enhance JohnT: conceivably the change does not affect this WS, or does affect some other.
			NotifySensesOfHeadwordChange();
		}

		private void NotifySensesOfHeadwordChange()
		{
			foreach (LexSense sense in AllSenses)
			{
				sense.EntryHeadwordChanged(Cache.DefaultVernWs);
			}
		}

		/// <summary>
		/// Handle a (possible) change to the first alternate form's default vern WS. Arguments are
		/// the old and new default vernacular lexeme form.
		/// </summary>
		internal void FirstAlternateFormChanged(string oldForm, string newForm)
		{
			if (oldForm == newForm)
				return; // some calls may not result from a change to that alternative.
			if (CitationForm.VernacularDefaultWritingSystem.Length > 0)
				return; // HomographForm is determined by CF and so has not changed.
			if (LexemeFormOA != null && LexemeFormOA.Form.VernacularDefaultWritingSystem.Length > 0)
				return; // HF is determined by LF and so has not changed.
			string oldVal = oldForm;
			if (string.IsNullOrEmpty(oldVal))
			{
				// The old value is determined in the way HomographForm does if there is no CF or LF.
				oldVal = StringServices.DefaultHomographString();
			}
			UpdateHomographs(oldVal);
		}

		/// <summary>
		/// Handle a change to the Form of an MoForm. Caller should ensure that form is owned by this entry,
		/// an that its default vernacualar WS has changed. oldValue is the previous value of Form.VernacularDefaultWritingSystem.Text.
		/// </summary>
		internal void MoFormFormChanged(IMoForm form, string oldValue)
		{
			var newValue = form.Form.VernacularDefaultWritingSystem.Text;
			if (form == LexemeFormOA)
				LexemeFormChanged(oldValue, newValue);
			else if (AlternateFormsOS[0] == form)
				FirstAlternateFormChanged(oldValue, newValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the citation form changes in the default vernacular WS, it affects our HomographForm,
		/// and hence possibly various homograph numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid,
			IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
			if (multiAltFlid == LexEntryTags.kflidCitationForm && alternativeWs.Handle == Cache.DefaultVernWs)
			{
				// WILL affect HomographForm.
				string oldHf = originalValue == null ? "" : originalValue.Text;
				// If the old CF was empty, the old Hf is whatever we would normally compute as the HF from the LF etc.
				if (string.IsNullOrEmpty(oldHf))
					oldHf = StringServices.LexemeFormStaticForWs(this, alternativeWs.Handle);
				UpdateHomographs(oldHf);
				// Another consequence is that the MLOwnerOutlineName of senses will change.
				// Enhance JohnT: conceivably the change does not affect this WS, or does affect some other.
				NotifySensesOfHeadwordChange();
				// And almost certainly the HeadWord will too.
				MLHeadwordChanged(alternativeWs.Handle);
			}
		}


		/// <summary>
		/// Something changed that may cause the homograph form to differ from oldHf, which it used to be.
		/// If it has really changed, update everything that needs to change.
		/// </summary>
		/// <param name="oldHf"></param>
		internal void UpdateHomographs(string oldHf)
		{
			if (Cache.ObjectsBeingDeleted.Contains(this))
				return; // if something is being changed as part of the process of deleting it, ignore; we already removed it.
			UpdateHomographs(oldHf, HomographForm, PrimaryMorphType, PrimaryMorphType);
		}

		internal void UpdateHomographs (IMoMorphType oldType)
		{
			var form = HomographForm;
			UpdateHomographs(form, form, oldType, PrimaryMorphType);
		}

		/// <summary>
		/// Update this entry and its old and new homographs to be correctly numbered.
		/// oldHf should be the correct HomographForm that this entry used to have
		/// oldType1 should be what the PrimaryMorphType used to be.
		/// Unless the object is being deleted (newHf and newType1 both null), only one should be changing
		/// </summary>
		private void UpdateHomographs(string oldHf, string newHf, IMoMorphType oldType1, IMoMorphType newType1)
		{
			var repo = Services.GetInstance<ILexEntryRepository>();
			var newType = repo.HomographMorphType(Cache, newType1);
			// This is deliberately obtained before updating, so it usually does NOT include this.
			// However it might in one case: where the homograph cache had not previously been built.
			var newHomographs = repo.GetHomographs(newHf)
				.Where(le => repo.HomographMorphType(Cache, le.PrimaryMorphType) == newType).ToList();
			newHomographs.Sort((first, second) => first.HomographNumber.CompareTo(second.HomographNumber));

			// This test used to be at the top of this method, but LT-13152 showed
			// that we still need to do some processing if our condition is true
			// when we merge one homograph into another of the same form:
			if (oldHf == newHf && oldType1 == newType1)
			{
				// Just make sure the new homograph numbers are correct:
				for (int i = 0; i < newHomographs.Count; i++)
					newHomographs[i].HomographNumber = i + 1;
				return;
			}

			// At some point AFTER we get the two lists we must fix the cache in the repository.
			// However, if we just now built the cache, the entry is already in the right case.
			if (!newHomographs.Remove(this))
				((ILexEntryRepositoryInternal)repo).UpdateHomographCache(this, oldHf);
			// OldHomographs should not include this, since something changed.
			var oldType = repo.HomographMorphType(Cache, oldType1);
			var oldHomographs = repo.GetHomographs(oldHf)
				.Where(le => repo.HomographMorphType(Cache, le.PrimaryMorphType) == oldType).ToList();
			oldHomographs.Sort((first, second) => first.HomographNumber.CompareTo(second.HomographNumber));

			// Fix old homographs, if any (may not have old ones if had no previous CF or LF).
			if (oldHomographs.Count == 1)
			{
				// down to one item, the survivor will not be a homograph.
				oldHomographs[0].HomographNumber = 0;
			}
			else
			{
				// Adjust the homograph numbers.
				for (int i = 0; i < oldHomographs.Count; i++)
					oldHomographs[i].HomographNumber = i + 1;
			}
			if (newHomographs.Count == 0)
				HomographNumber = 0;
			else
			{
				HomographNumber = newHomographs.Count + 1; // give the new one the next available number.
				if (newHomographs.Count == 1)
					newHomographs[0].HomographNumber = 1; // was zero, now it has a homograph.
			}
		}

		/// <summary>
		/// Clean up its homographs as if it was ceasing to have any HF.
		/// </summary>
		protected override void OnBeforeObjectDeleted()
		{
			UpdateHomographs(HomographForm, "", PrimaryMorphType, null);
			RegisterVirtualsModifiedForObjectDeletion(((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService);
			base.OnBeforeObjectDeleted();
		}

		internal override void RegisterVirtualsModifiedForObjectDeletion(IUnitOfWorkService uow)
		{
			var cache = Cache; // need to make the Func use this local variable, because on Redo, Cache may return null.
			uow.RegisterVirtualCollectionAsModified(Cache.LangProject.LexDbOA,
				Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries,
				() => cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances(),
				new ILexEntry[0], new[] { this });
			base.RegisterVirtualsModifiedForObjectDeletion(uow);
		}

		/// <summary>
		/// Get the homograph form from the citation form,
		/// or the lexeme form (no citation form), in that order. In the unlikely event that the lexeme form has
		/// no value in the specified writing system, tries the alternative forms.
		/// Note that various other routines know this logic, since they need to figure the old HF after something
		/// that might affect it has changed.
		/// </summary>
		public string HomographForm
		{
			get
			{
				return StringServices.ShortName1Static(this);
			}
		}

		/// <summary>
		/// Get all the morph types for the allomorphs of an entry,
		/// including the lexeme form. The morph types are ordered
		/// from general to specific.
		/// </summary>
		public List<IMoMorphType> MorphTypes
		{
			get
			{
				var types = new List<IMoMorphType>();
				var lfForm = LexemeFormOA;
				if (lfForm != null && lfForm.MorphTypeRA != null)
					types.Add(lfForm.MorphTypeRA);
				// reverse the order of the forms since they are ordered
				// from specific to general
				foreach (var form in AlternateFormsOS.Reverse())
				{
					var mmt = form.MorphTypeRA;
					if (mmt != null && !types.Contains(mmt))
						types.Add(mmt);
				}
				return types;
			}
		}

		/// <summary>
		/// Get the primary (most general) morph type for the entry.
		/// </summary>
		public IMoMorphType PrimaryMorphType
		{
			get
			{
				var types = MorphTypes;
				if (types.Count == 0)
					return null;
				return types[0];
			}
		}

		/// <summary>
		/// Gets a value indicating whether the forms in this entry have mixed morph types.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the forms in this entry have mixed morph types; otherwise, <c>false</c>.
		/// </value>
		public bool IsMorphTypesMixed
		{
			get
			{
				var types = MorphTypes;
				switch (types.Count)
				{
					case 0:
					case 1:
						return false;
					case 2:
					// probably a circumfix with two infixes
					// fall through to the 3 case
					case 3:
						// probably a circumfix
						return types[0].Guid != MoMorphTypeTags.kguidMorphCircumfix;
					default:
						return true;
				}
			}
		}

		/// <summary>
		/// This and all the senses it owns (possibly recursively, that is, including subsenses).
		/// </summary>
		internal List<ICmObject> ThisAndAllSenses
		{
			get {
				var senses = AllSenses;
				var result = new List<ICmObject>(senses.Count + 1);
				result.Add(this);
				foreach (ICmObject sense in senses)
					result.Add(sense);
				return result;
			}
		}

		/// <summary>
		/// Gets all senses owned by this entry, and all senses they own.
		/// Enhance JohnT: generate PropChanged for changes to LexEntry.Senses, LexSense.Senses.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexSense")]
		public List<ILexSense> AllSenses
		{
			get
			{
				var senses = new List<ILexSense>();
				foreach (var ls in SensesOS)
					senses.AddRange(ls.AllSenses);

				return senses;
			}
		}

		/// <summary>
		/// Get the number of senses in this LexEntry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int NumberOfSensesForEntry
		{
			get
			{
				return AllSenses.Count;
			}
		}

		/// <summary>
		/// Something changed which may cause our NumberOfSensesForEntry to be invalid. Ensure a PropChanged will update views.
		/// </summary>
		/// <param name="fAdd">true if adding a sense, false if deleting.</param>
		internal void NumberOfSensesChanged(bool fAdd)
		{
			int nosFlid = m_cache.MetaDataCache.GetFieldId2(LexEntryTags.kClassId, "NumberOfSensesForEntry", false);
			int newNumberOfSenses = NumberOfSensesForEntry;
			int oldNumberOfSenses = fAdd ? newNumberOfSenses - 1 : newNumberOfSenses + 1;
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(
					this, nosFlid, oldNumberOfSenses, newNumberOfSenses);
		}

		/// <summary>
		/// Check whether this LexEntry has any senses or subsenses that use the given MSA.
		/// </summary>
		/// <param name="msaOld"></param>
		/// <returns></returns>
		internal bool UsesMsa(IMoMorphSynAnalysis msaOld)
		{
			if (msaOld == null)
				return false;

			foreach (LexSense ls in this.SensesOS)
			{
				if (ls.UsesMsa(msaOld))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Generate an id string like "colorful_7ee714ef-2744-4fc2-b407-aab54e66a76f".
		/// If there's a LIFTid element in LiftResidue (or ImportResidue), use that instead,
		/// but use the real guid instead of the stored one.  See FWR-2621.
		/// </summary>
		public string LIFTid
		{
			get
			{
				string sLiftId = null;
				var sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					sResidue = ExtractLIFTResidue(m_cache, m_hvo,
												  (int)LexEntryTags.kflidImportResidue, (int)LexEntryTags.kflidLiftResidue);
				if (!String.IsNullOrEmpty(sResidue))
					sLiftId = ExtractAttributeFromLiftResidue(sResidue, "id");
				if (String.IsNullOrEmpty(sLiftId))
					return HeadWord.Text + "_" + Guid;
				int idx = sLiftId.IndexOf('_');
				if (idx >= 0)
					return sLiftId.Substring(0, idx) + "_" + Guid;
				else
					return sLiftId + "_" + Guid;
			}
		}

		/// <summary>
		/// Scan ImportResidue for XML looking string inserted by LIFT import.  If any is found,
		/// move it from ImportResidue to LiftResidue.
		/// </summary>
		/// <returns>string containing any LIFT import residue found in ImportResidue</returns>
		public static string ExtractLIFTResidue(FdoCache cache, int hvo, int flidImportResidue,
			int flidLiftResidue)
		{
			ITsString tss = cache.MainCacheAccessor.get_StringProp(hvo, flidImportResidue);
			if (tss == null || tss.Length < 13)
				return null;
			int idx = tss.Text.IndexOf("<lift-residue");
			if (idx >= 0)
			{
				string sLiftResidue = tss.Text.Substring(idx);
				int idx2 = sLiftResidue.IndexOf("</lift-residue>");
				if (idx2 >= 0)
				{
					idx2 += 15;
					if (sLiftResidue.Length > idx2)
						sLiftResidue = sLiftResidue.Substring(0, idx2);
				}
				if (flidLiftResidue != 0)
				{
					int cch = sLiftResidue.Length;
					ITsStrBldr tsb = tss.GetBldr();
					tsb.Replace(idx, idx + cch, null, null);
					tss = tsb.GetString();	// remove from ImportResidue
					cache.MainCacheAccessor.SetString(hvo, flidImportResidue, tss);
					cache.MainCacheAccessor.set_UnicodeProp(hvo, flidLiftResidue, sLiftResidue);
				}
				return sLiftResidue;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Get the preferred writing system identifier for the class.
		/// </summary>
		protected override string PreferredWsId
		{
			get { return Services.WritingSystems.DefaultVernacularWritingSystem.Id; }
		}

		/// <summary>
		/// The primary sort key for sorting a list of ShortNames.
		/// </summary>
		public override string SortKey
		{
			get { return ShortName1; }
		}

		/// <summary>
		/// A secondary sort key for sorting a list of ShortNames.  Defaults to zero.
		/// </summary>
		public override int SortKey2
		{
			get
			{
				var nSortKey2 = 0;
				var lf = LexemeFormOA;
				if (lf != null)
				{
					var mmt = lf.MorphTypeRA;
					if (mmt != null)
					{
						nSortKey2 = mmt.SecondaryOrder * 1024;
					}
				}
				nSortKey2 += HomographNumber;
				return nSortKey2;
			}
		}

		/// <summary>
		/// A sort key which combines both SortKey and SortKey2 in a string array.
		/// Note: called by reflection as a sortmethod for browse columns.
		/// </summary>
		public string[] FullSortKey(bool sortedFromEnd, int ws)
		{
			// This returns headword = citation form if it exists, otherwise lexeme form.
			string sKey = StringServices.ShortName1StaticForWs(this, ws);

			if (sortedFromEnd)
				sKey = TsStringUtils.ReverseString(sKey);

			// Append 11 digit sortkey after space. sortkey = morphtype number << 10 bits | homograph number
			int nKey2 = this.SortKey2;
			if (nKey2 != 0)
				return new [] {sKey, SortKey2Alpha};

			return new [] {sKey};
		}

		/// <summary>
		/// Sorting on an allomorphs column on an entry without allomorphs will
		/// result in trying to sort on the (ghost) owner entry. In that case,
		/// we want to return an empty string, indicating that there was no
		/// allomorph form to create a key for.
		/// </summary>
		/// <param name="sortedFromEnd"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public string MorphSortKey(bool sortedFromEnd, int ws)
		{
			return "";
		}

		/// <summary>
		/// A sort key method for sorting on the CitationForm field and morph type.
		/// Result is a string, space, and
		/// </summary>
		public string CitationFormSortKey(bool sortedFromEnd, int ws)
		{
			string sKey = null;
			if (CitationForm != null)
			{
				var tss = CitationForm.get_String(ws);
				if (tss != null && tss.Length != 0)
					sKey = tss.Text;
			}
			if (sKey == null)
				sKey = "";

			if (sortedFromEnd)
				sKey = TsStringUtils.ReverseString(sKey);

			var mmt = LexemeFormOA;
			if (mmt != null)
			{
				// Append 11 digit sortkey after space. sortkey = morphtype number << 10 bits
				sKey = (mmt as MoForm).SortKeyMorphType(sKey);
			}

			return sKey;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		/// <remarks> precede by PreLoadShortName() when calling this a lot, for example when
		/// sorting  an entire dictionary by this property.</remarks>
		public string ShortName1
		{
			get
			{
				return StringServices.ShortName1Static(this);
			}
		}

		/// <summary>
		/// The canonical unique name of a lexical entry.  This includes
		/// CitationFormWithAffixType (in this implementation) with the homograph number
		/// (if non-zero) appended as a subscript.
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString HeadWord
		{
			get
			{
				return StringServices.HeadWordForWsAndHn(this, Cache.DefaultVernWs, ((ILexEntry) this).HomographNumber);
			}
		}

		/// <summary>
		/// Virtual property allows Headword to be read through cache.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public VirtualStringAccessor MLHeadWord
		{
			get
			{
				return new VirtualStringAccessor(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexEntryMLHeadWord, HeadWordForWs);
			}
		}

		/// <summary>
		/// Virtual property allows HeadWordRef to be read through cache.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public VirtualStringAccessor HeadWordRef
		{
			get
			{
				return new VirtualStringAccessor(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexEntryHeadWordRef, HeadWordRefForWs);
			}
		}

		/// <summary>
		/// Virtual property allows HeadWordReversal to be read through cache.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public VirtualStringAccessor HeadWordReversal
		{
			get
			{
				return new VirtualStringAccessor(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexEntryHeadWordReversal, HeadWordReversalForWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the LexEntryRef
		/// objects owned by this LexEntry that define this entry as a variant (that is, RefType is krtVariant).
		/// (I think it has PropChanged support...variants list in one entry updates if a variant is made in another)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> VariantEntryRefs
		{
			get
			{
				return from entryRef in EntryRefsOS
					   where (entryRef.RefType == LexEntryRefTags.krtVariant)
						select entryRef;
			}
		}

		/// <summary>
		/// Get the minimal set of LexReferences for this entry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "MinimalLexReferences")]
		public List<ILexReference> MinimalLexReferences
		{
			get
			{
				var targets =
					Cache.ServiceLocator.GetInstance<ILexReferenceRepository>().AllInstances().Where(
						lexRef => lexRef.TargetsRS.Contains(this));
				return LexReference.ExtractMinimalLexReferences(targets);
			}
		}

		/// <summary>
		/// Gets the complex form entries, that is, the entries which should be shown
		/// in the complex forms list for this entry in data entry view.
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// ComponentLexemes field and that is a complex entry type.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> ComplexFormEntries
		{
			get
			{
				return VirtualOrderingServices.GetOrderedValue(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexEntryComplexFormEntries,
					Services.GetInstance<ILexEntryRepository>().GetComplexFormEntries(this));
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the subentries of this entry, that is, the entries which should be shown
		/// as subentries (paragraphs usually indented) under this entry in root-based view.
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// PrimaryLexemes field and that is a complex entry type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> Subentries
		{
			get
			{
				return VirtualOrderingServices.GetOrderedValue(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexEntrySubentries,
					Services.GetInstance<ILexEntryRepository>().GetSubentries(this));
			}
		}

		/// <summary>
		/// Gets the complex form entries, that is, the entries which should be shown
		/// in the visible complex forms list for this entry in stem-based view, and in data entry view.
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// ShowComplexFormsIn field and that is a complex entry type.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> VisibleComplexFormEntries
		{
			get
			{
				return VirtualOrderingServices.GetOrderedValue(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexEntryVisibleComplexFormEntries,
					Services.GetInstance<ILexEntryRepository>().GetVisibleComplexFormEntries(this));
			}
		}

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own LexEntryRef objects that refer to this LexSense as a
		/// variant (component).
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> VariantFormEntries
		{
			get
			{
				return Services.GetInstance<ILexEntryRepository>().GetVariantFormEntries(this);
			}
		}

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of object ids for
		/// all the LexReferences that contain this LexSense/LexEntry.
		/// Note this is called on SFM export by mdf.xml so needs to be a property.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexReference")]
		public IEnumerable<ILexReference> LexEntryReferences
		{
			get
			{
				return Services.GetInstance<ILexReferenceRepository>().GetReferencesWithTarget(this);
			}
		}

		/// <summary>
		/// The canonical unique name of a lexical entry.  This includes
		/// CitationFormWithAffixType (in this implementation) with the homograph number
		/// (if non-zero) appended as a subscript (or superscript, or prepended, or not at all...see HomographConfiguration).
		/// </summary>
		/// <param name="wsVern"></param>
		public ITsString HeadWordForWs(int wsVern)
		{
			// If it has absolutely no citation for or lexeme form, we want to return an empty string
			// rather than provide just affix markers and/or homograph number. This is so it can 'disappear'
			// as not having a value when multiple alternatives are being shown. See LT-11170, later comments.
			// So, pass empty string for final argument.
			return StringServices.HeadWordForWsAndHn(this, wsVern, ((ILexEntry) this).HomographNumber, "");
		}
		/// <summary>
		/// The name of a lexical entry as used in cross-refs in the dictionary view.  This includes
		/// CitationFormWithAffixType (in this implementation) with the homograph number
		/// (if non-zero)appended as a subscript (or superscript, or prepended, or not at all...see HomographConfiguration)
		/// </summary>
		public ITsString HeadWordRefForWs(int wsVern)
		{
			return StringServices.HeadWordForWsAndHn(this, wsVern, ((ILexEntry)this).HomographNumber, "",
				HomographConfiguration.HeadwordVariant.DictionaryCrossRef);
		}

		/// <summary>
		/// The name of a lexical entry as used in cross-refs in the reversls view.  This includes
		/// CitationFormWithAffixType (in this implementation) with the homograph number
		/// (if non-zero)appended as a subscript (or superscript, or prepended, or not at all...see HomographConfiguration)
		/// </summary>
		public ITsString HeadWordReversalForWs(int wsVern)
		{
			return StringServices.HeadWordForWsAndHn(this, wsVern, ((ILexEntry)this).HomographNumber, "",
				HomographConfiguration.HeadwordVariant.ReversalCrossRef);
		}

		/// <summary>
		/// This allows us to get the headword without actually creating an instance...
		/// which can be slow.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		private static ITsString HeadWordStatic(ILexEntry entry)
		{
			return StringServices.HeadWordForWsAndHn(entry, entry.Cache.DefaultVernWs, entry.HomographNumber);
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		/// <remarks> precede by PreLoadShortName() when calling this a lot, for example when
		/// sorting  an entire dictionary by this property.</remarks>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get { return HeadWordStatic(this); }
		}

		/// <summary>
		/// Conceptually, this answers AllSenses.Count > 1.
		/// However, it is vastly more efficient, especially when doing a lot of them
		/// and everything is preloaded or the cache is in kalpLoadForAllOfObjectClass mode.
		/// </summary>
		public bool HasMoreThanOneSense
		{
			get
			{
				return SensesOS.Count > 1
					   || (SensesOS.Count == 1 && SensesOS[0].SensesOS.Count > 0);
			}
		}

		/// <summary>
		/// Return all allomorphs of the entry: first the lexeme form, then the alternate forms.
		/// </summary>
		public IMoForm[] AllAllomorphs
		{
			get
			{
				if (LexemeFormOA == null)
				{
					return AlternateFormsOS.ToArray();
				}
				else
				{
					var results = new List<IMoForm>();
					results.Add(LexemeFormOA);
					results.AddRange(AlternateFormsOS.ToArray());
					return results.ToArray();
				}
			}
		}

		/// <summary>
		/// The Citation form with an indication of the affix type.
		/// </summary>
		public string CitationFormWithAffixType
		{
			get
			{
				return CitationFormWithAffixTypeForWs(Cache.DefaultVernWs);
			}
		}

		/// <summary>
		/// The Citation form with an indication of the affix type.  This returns null if there
		/// is not a citation form in the given writing system.
		/// </summary>
		/// <remarks>This is used by reflection for LIFT export.</remarks>
		public string CitationFormWithAffixTypeForWs(int wsVern)
		{
			var tss = CitationForm.get_String(wsVern);
			return tss == null || tss.Length == 0
					? null
					: StringServices.DecorateFormWithAffixMarkers(this, tss.Text);
		}

		/// <summary>
		/// Append to the string builder text equivalent to CitationFormWithAffixTypeStatic, but
		/// with the correct writing systems.
		/// </summary>
		/// <param name="tsb"></param>
		public void CitationFormWithAffixTypeTss(ITsIncStrBldr tsb)
		{
			var form = LexemeFormOA;
			if (form == null)
			{
				// No type info...return simpler version of name.
				StringServices.ShortName1Static(this, tsb);
				return;
			}

			var prefix = string.Empty;
			var postfix = string.Empty;
			var mType = form.MorphTypeRA;
			if (mType != null) // It may be null.
			{
				prefix = mType.Prefix;
				postfix = mType.Postfix;
			}
			// The following code for setting Ws and FontFamily are to fix LT-6238.
			IWritingSystem defVernWs = Services.WritingSystems.DefaultVernacularWritingSystem;
			if (!String.IsNullOrEmpty(prefix))
			{
				tsb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, defVernWs.Handle);
				tsb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Doulos SIL");
				tsb.Append(prefix);
			}
			StringServices.ShortName1Static(this, tsb);
			if (!String.IsNullOrEmpty(postfix))
			{
				tsb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, defVernWs.Handle);
				tsb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Doulos SIL");
				tsb.Append(postfix);
			}
		}

		internal static string ExtractLiftResidueContent(string sResidue)
		{
			if (sResidue.StartsWith("<lift-residue"))
			{
				int idx = sResidue.IndexOf('>');
				sResidue = sResidue.Substring(idx + 1);
			}
			if (sResidue.EndsWith("</lift-residue>"))
			{
				sResidue = sResidue.Substring(0, sResidue.Length - 15);
			}
			return sResidue;
		}

		internal static string ExtractAttributeFromLiftResidue(string sResidue, string sAttrName)
		{
			if (!String.IsNullOrEmpty(sResidue) && sResidue.StartsWith("<lift-residue"))
			{
				int idxEnd = sResidue.IndexOf('>');
				if (idxEnd > 0)
				{
					var sStartTag = sResidue.Substring(0, idxEnd);
					var idx = sStartTag.IndexOf(sAttrName + "=");
					if (idx > 0 && Char.IsWhiteSpace(sStartTag[idx - 1]))
					{
						idx += sAttrName.Length + 1;
						var cQuote = sStartTag[idx];
						++idx;
						idxEnd = sStartTag.IndexOf(cQuote, idx);
						if (idxEnd > 0)
						{
							return XmlUtils.DecodeXmlAttribute(sStartTag.Substring(idx, idxEnd - idx));
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Find a LexEntryRef matching the given targetComponent (exlusively), and variantEntryType.
		/// If we can't match on variantEntryType, we'll just return the reference with the matching component.
		/// </summary>
		/// <param name="targetComponent">match on the LexEntryRef that contains this, and only this component.</param>
		/// <param name="variantEntryType"></param>
		/// <returns></returns>
		public ILexEntryRef FindMatchingVariantEntryRef(IVariantComponentLexeme targetComponent, ILexEntryType variantEntryType)
		{
			ILexEntryRef matchingEntryRef = null;
			foreach (ILexEntryRef ler in EntryRefsOS)
			{
				if (ler.RefType == LexEntryRefTags.krtVariant &&
					ler.ComponentLexemesRS.Count == 1 &&
					ler.ComponentLexemesRS.Contains(targetComponent))
				{
					matchingEntryRef = ler;
					// next see if we can also match against the type, we'll just 'use' that one.
					// otherwise keep going just in case we can find one that does match.
					if (variantEntryType != null && ler.VariantEntryTypesRS.Contains(variantEntryType))
						break;
				}
			}
			return matchingEntryRef;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mainEntryOrSense"></param>
		/// <param name="variantEntryType"></param>
		/// <param name="targetVariantLexemeForm"></param>
		/// <returns></returns>
		internal static ILexEntryRef FindMatchingVariantEntryBackRef(IVariantComponentLexeme mainEntryOrSense,
			ILexEntryType variantEntryType, ITsString targetVariantLexemeForm)
		{
			ILexEntryRef matchingEntryRef = null;
			foreach (ILexEntryRef ler in mainEntryOrSense.VariantFormEntryBackRefs)
			{
				// this only handles matching single component lexemes,
				// so we only try to match those.
				if (ler.ComponentLexemesRS.Count == 1)
				{
					// next see if we can match on the same variant lexeme form
					ILexEntry variantEntry = (ler as CmObject).Owner as ILexEntry;
					if (variantEntry.LexemeFormOA == null || variantEntry.LexemeFormOA.Form == null)
						continue;
					int wsTargetVariant = TsStringUtils.GetWsAtOffset(targetVariantLexemeForm, 0);
					if (targetVariantLexemeForm.Equals(variantEntry.LexemeFormOA.Form.get_String(wsTargetVariant)))
					{
						// consider this a possible match. we'll use the last such possibility
						// if we can't find a matching variantEntryType (below.)
						matchingEntryRef = ler;
						// next see if we can also match against the type, we'll just 'use' that one.
						// otherwise keep going just in case we can find one that does match.
						if (variantEntryType != null && ler.VariantEntryTypesRS.Contains(variantEntryType))
							break;
					}
					// continue...
				}
			}
			return matchingEntryRef;
		}

		/// <summary>
		/// Check whether this entry should be published as a minor entry.
		/// </summary>
		/// <returns></returns>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool PublishAsMinorEntry
		{
			get
			{
				return EntryRefsOS.Cast<LexEntryRef>().Any(ler => ler.HideMinorEntry == 0);
			}
			set
			{
				// Make all of them consistent. If there are none this field can't be set, but should not be
				// shown, because it is meaningless.
				foreach (var ler in EntryRefsOS)
					ler.HideMinorEntry = value ? 0 : 1;
			}
		}

		/// <summary>
		/// The publications from which this is not excluded, that is, the ones in which it
		/// SHOULD be published.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "CmPossibility")]
		public IFdoSet<ICmPossibility> PublishIn
		{
			get
			{
				return new FdoInvertSet<ICmPossibility>(DoNotPublishInRC, Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS);
			}
		}

		/// <summary>
		/// The publications from which this headword is not excluded, that is, the ones in which it
		/// SHOULD be published.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "CmPossibility")]
		public IFdoSet<ICmPossibility> ShowMainEntryIn
		{
			get
			{
				return new FdoInvertSet<ICmPossibility>(DoNotShowMainEntryInRC, Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS);
			}
		}

		/// <summary>
		/// This is a virtual property.  It returns the list of all the LexEntryRef
		/// objects owned by this LexEntry that have HideMinorEntry set to zero and that define
		/// this LexEntry as a variant.
		/// </summary>
		/// <value>The visible variant entry refs.</value>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> VisibleVariantEntryRefs
		{
			get
			{
				return from lexEntryRef in EntryRefsOS
					   where lexEntryRef.HideMinorEntry == 0 && lexEntryRef.RefType == LexEntryRefTags.krtVariant
					   select lexEntryRef;
			}
		}

		#region Implementation of IVariantComponentLexeme

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry as a variant (component).
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> VariantFormEntryBackRefs
		{
			get
			{
				return Services.GetInstance<ILexEntryRefRepository>().GetVariantEntryRefsWithMainEntryOrSense(this);
			}
		}

		/// <summary>
		/// creates a variant entry from this (main entry or sense) component,
		/// and links the variant to this (main entry or sense) component via
		/// EntryRefs.ComponentLexemes
		///
		/// NOTE: The caller will need to supply the lexemeForm subsequently.
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType)
		{
			return CreateVariantEntryAndBackRef(variantType, null);
		}

		/// <summary>
		/// creates a variant entry from this (main entry or sense) component,
		/// and links the variant to this (main entry or sense) component via
		/// EntryRefs.ComponentLexemes
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <param name="tssVariantLexemeForm">the lexeme form of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType, ITsString tssVariantLexemeForm)
		{
			return CreateVariantEntryAndBackRef(this, variantType, tssVariantLexemeForm);
		}

		internal ILexEntryRef CreateVariantEntryAndBackRef(IVariantComponentLexeme componentLexeme, ILexEntryType variantType,
			ITsString tssVariantLexemeForm)
		{
			var variantEntry = Services.GetInstance<ILexEntryFactory>().Create();
			if (this.LexemeFormOA is IMoAffixAllomorph)
				variantEntry.LexemeFormOA = Services.GetInstance<IMoAffixAllomorphFactory>().Create();
			else
				variantEntry.LexemeFormOA = Services.GetInstance<IMoStemAllomorphFactory>().Create();
			if (this.LexemeFormOA != null)
				variantEntry.LexemeFormOA.MorphTypeRA = this.LexemeFormOA.MorphTypeRA;
			if (tssVariantLexemeForm != null)
				variantEntry.LexemeFormOA.FormMinusReservedMarkers = tssVariantLexemeForm;
			return variantEntry.MakeVariantOf(componentLexeme, variantType);
		}

		public ILexEntryRef FindMatchingVariantEntryBackRef(ILexEntryType variantEntryType, ITsString targetVariantLexemeForm)
		{
			return FindMatchingVariantEntryBackRef(this, variantEntryType, targetVariantLexemeForm);
		}

		#endregion

		/// <summary>
		/// Return DateCreated in Universal (Utc/GMT) time.
		/// </summary>
		//[VirtualProperty(CellarPropertyType.Time)]
		public DateTime UtcDateCreated
		{
			get { return DateCreated.ToUniversalTime(); }
		}

		/// <summary>
		/// Return DateModified in Universal (Utc/GMT) time.
		/// </summary>
		//[VirtualProperty(CellarPropertyType.Time)]
		public DateTime UtcDateModified
		{
			get { return DateModified.ToUniversalTime(); }
		}

		/// <summary>
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		//[VirtualProperty(CellarPropertyType.String)]
		public string LiftResidueContent
		{
			get
			{
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
				{
					sResidue = ExtractLIFTResidue(m_cache, m_hvo, LexEntryTags.kflidImportResidue,
						LexEntryTags.kflidLiftResidue);
					if (String.IsNullOrEmpty(sResidue))
						return null;
				}
				if (sResidue.IndexOf("<lift-residue") != sResidue.LastIndexOf("<lift-residue"))
					sResidue = RepairLiftResidue(sResidue);
				return ExtractLiftResidueContent(sResidue);
			}
		}

		private string RepairLiftResidue(string sResidue)
		{
			int idx = sResidue.IndexOf("</lift-residue>");
			if (idx > 0)
			{
				// Remove the repeated occurrences of <lift-residue>...</lift-residue>.
				// See LT-10302.
				sResidue = sResidue.Substring(0, idx + 15);
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() => { LiftResidue = sResidue; });
			}
			return sResidue;
		}

		/// <summary>
		/// Return anything from the ImportResidue which occurs prior to whatever LIFT may have
		/// added to it.  (LIFT import no longer adds to ImportResidue, but it did in the past.)
		/// </summary>
		//[VirtualProperty(CellarPropertyType.String)]
		public ITsString NonLIFTImportResidue
		{
			get
			{
				ITsString tss = m_cache.MainCacheAccessor.get_StringProp(m_hvo, LexEntryTags.kflidImportResidue);
				return ExtractNonLIFTResidue(tss);
			}
		}

		internal static ITsString ExtractNonLIFTResidue(ITsString tss)
		{
			if (tss == null || tss.Length < 29)
				return tss;
			ITsStrBldr tsb = tss.GetBldr();
			int idx = tsb.Text.IndexOf("<lift-residue");
			if (idx >= 0)
			{
				int idxEnd = tsb.Text.IndexOf("</lift-residue>", idx + 14);
				if (idxEnd >= 0)
					tsb.Replace(idx, idxEnd + 15, null, null);
			}
			return tsb.GetString();
		}

		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateCreated
		{
			get { return ExtractAttributeFromLiftResidue(LiftResidue, "dateCreated"); }
		}

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateModified
		{
			get { return ExtractAttributeFromLiftResidue(LiftResidue, "dateModified"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of all the LexEntryRef
		/// objects owned by this LexEntry that define this entry as a complex form.
		/// Currently there will be at most one such entry ref. (This property is called by
		/// reflection from a request in XML; please do not remove it just because there are
		/// no direct C# callers.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> ComplexFormEntryRefs
		{
			get
			{
				return
					from ILexEntryRef lexRef
						in this.EntryRefsOS
					where lexRef.RefType == LexEntryRefTags.krtComplexForm
					select lexRef;
			}
		}

		/// <summary>
		/// If this entry is a complex one, the primary lexemes (under which it is shown as a subentry).
		/// Otherwise empty.
		/// No PropChanged support as yet.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmObject")]
		public IEnumerable<ICmObject> PrimaryComponentLexemes
		{
			get { var ler = ComplexFormEntryRefs.FirstOrDefault();
			if (ler == null)
				return new ICmObject[0];
				return ler.PrimaryLexemesRS;
			}
		}

		/// <summary>
		/// The Lexeme form with an indication of the affix type.
		/// </summary>
		public string LexemeFormWithAffixType
		{
			get
			{
				if (this.LexemeFormOA != null)
				{
					string form = this.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text;
					string prefix = String.Empty;
					string postfix = String.Empty;
					if (this.LexemeFormOA.MorphTypeRA != null)
					{
						prefix = this.LexemeFormOA.MorphTypeRA.Prefix;
						postfix = this.LexemeFormOA.MorphTypeRA.Postfix;
					}
					return prefix + form + postfix;
				}
				else
				{
					return String.Empty;
				}
			}
		}

		/// <summary>
		/// The canonical unique name of a lexical entry as a string.
		/// </summary>
		public string ReferenceName
		{
			get
			{
				return HeadWord.Text;
			}
		}

		/// <summary>
		/// Overrides the method, so we can also merge similar MSAs and allomorphs, after the main merge.
		/// </summary>
		public override void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			if (!(objSrc is ILexEntry))
				return;

			var homoForm = HomographForm;
			ILexEntry le = objSrc as ILexEntry;
			// If the lexeme forms don't match, make the other LF an allomorph.
			if (LexemeFormOA != null && le.LexemeFormOA != null &&
				LexemeFormOA.Form.VernacularDefaultWritingSystem != null && le.LexemeFormOA.Form.VernacularDefaultWritingSystem != null
				&& LexemeFormOA.Form.VernacularDefaultWritingSystem.Text != le.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text)
			{
				// Order here is important. We must update any homographs of the entry that is going away.
				// We must do that AFTER we remove its lexeme form so that it is no longer a homograph.
				// We must record what form we need to adjust the homographs of BEFORE we change it.
				var otherHomoForm = le.HomographForm;
				AlternateFormsOS.Add(le.LexemeFormOA);
				if (le.HomographNumber != 0)
					((LexEntry)le).UpdateHomographs(otherHomoForm);
			}
			//  merge the LexemeForm objects first, if this is possible.  This is important, because otherwise the
			// LexemeForm objects would not get merged, and that is needed for proper handling
			// of references and back references.
			if (this.LexemeFormOA != null && le.LexemeFormOA != null && LexemeFormOA.ClassID == le.LexemeFormOA.ClassID)
			{
				this.LexemeFormOA.MergeObject(le.LexemeFormOA, fLoseNoStringData);
				le.LexemeFormOA = null;
			}

			// If this has entry refs to the merged entry or any of its senses, clear them out, since an object can't
			// have itself or its own senses as components. If that results in an empty entryRef, delete it.
			// Likewise references in the opposite direction must go.
			RemoveCrossRefsBetween(this, le);
			RemoveCrossRefsBetween(le, this);

			// If there are LexEntryRefs that reference the entry being merged, we need to fix them explicitly.
			// Base.MergeObject would make an attempt, but it can't handle the interaction between
			// ComponentLexemes and PrimaryLexemes. (Removing the old object from component lexemes
			// removes it from PrimaryLexemes, and then it isn't there as expected to replace, which crashes.
			// See FWR-3535.)
			// Also, it may be that the object we are merging with references this (or one of its senses) as a component.
			// That would become circular so we have to remove it.
			foreach (LexEntryRef leref in (from item in objSrc.ReferringObjects where item is LexEntryRef select item).ToArray())
			{
				leref.ReplaceComponent(le, this);
			}

			// base.MergeObject will call DeleteUnderlyingObject on objSrc,
			// which, in turn, will reset homographs for any similar entries for objSrc.


			Debug.Assert(m_cache != null);
			// We don't allow merging items of different classes.
			Debug.Assert(ClassID == objSrc.ClassID);
			if (ClassID != objSrc.ClassID)
				return;

			IFwMetaDataCacheManaged mdc = (IFwMetaDataCacheManaged)m_cache.MetaDataCache;
			var flidList = from flid in mdc.GetFields(ClassID, true, (int)CellarPropertyTypeFilter.All)
						   where !m_cache.MetaDataCache.get_IsVirtual(flid)
						   select flid;
			List<int> flidListWithPubs = new List<int>(flidList);
			flidListWithPubs.Remove(FDO.LexEntryTags.kflidDoNotShowMainEntryIn);
			flidListWithPubs.Remove(FDO.LexEntryTags.kflidDoNotPublishIn);
			flidListWithPubs.Add(mdc.GetFieldId("LexEntry", "ShowMainEntryIn", false));
			flidListWithPubs.Add(mdc.GetFieldId("LexEntry", "PublishIn", false));
			// Process all the fields in the source.
			MergeSelectedPropertiesOfObject(objSrc, fLoseNoStringData, flidListWithPubs.ToArray());
			// NB: objSrc is now invalid, so don't try to use it.

			List<IMoForm> formList = new List<IMoForm>();
			int i;
			// Merge any equivalent alternate forms.
			foreach (IMoForm form in AlternateFormsOS)
				formList.Add(form);
			while (formList.Count > 0)
			{
				IMoForm formToProcess = formList[0];
				formList.RemoveAt(0);
				for (i = formList.Count - 1; i >= 0; --i)
				{
					IMoForm formToConsider = formList[i];
					if (formToProcess.GetType() == formToConsider.GetType()
						&& formToProcess.Form.VernacularDefaultWritingSystem.Text == formToConsider.Form.VernacularDefaultWritingSystem.Text
						&& formToProcess.MorphTypeRA == formToConsider.MorphTypeRA)
					{
						formToProcess.MergeObject(formToConsider, fLoseNoStringData);
						formList.Remove(formToConsider);
					}
				}
			}

			// Merge equivalent MSAs.
			List<IMoMorphSynAnalysis> msaList = new List<IMoMorphSynAnalysis>();
			foreach (IMoMorphSynAnalysis msa in MorphoSyntaxAnalysesOC)
				msaList.Add(msa);
			while (msaList.Count > 0)
			{
				IMoMorphSynAnalysis msaToProcess = msaList[0];
				msaList.RemoveAt(0);
				for (i = msaList.Count - 1; i >= 0; --i)
				{
					IMoMorphSynAnalysis msaToConsider = msaList[i];
					if (msaToProcess.EqualsMsa(msaToConsider))
					{
						// LT-13007 if true is passed in here, merging two entries with identical MSAs
						// gives duplicated grammatical information.
						msaToProcess.MergeObject(msaToConsider, false);
						msaList.Remove(msaToConsider);
					}
				}
			}

			// If the user merged one homograph into another of the same form, we
			// need to recalculate the homograph numbers (LT-13152):
			UpdateHomographs(homoForm);
		}

		/// <summary>
		/// Remove any LexEntryRef connections from source to dest (or its senses).
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dest"></param>
		private void RemoveCrossRefsBetween(ILexEntry source, ILexEntry dest)
		{
			var badTargets = new HashSet<ICmObject>(dest.AllSenses.Cast<ICmObject>());
			badTargets.Add(dest);
			for (int iEntry = source.EntryRefsOS.Count - 1; iEntry >= 0; iEntry--)
			{
				var ler = source.EntryRefsOS[iEntry];
				for (int j = ler.ComponentLexemesRS.Count - 1; j >= 0; j--)
				{
					var target = ler.ComponentLexemesRS[j];
					if (badTargets.Contains(target))
					{
						ler.ComponentLexemesRS.RemoveAt(j);
						ler.PrimaryLexemesRS.Remove(target);
					}
				}
				if (ler.ComponentLexemesRS.Count == 0)
					source.EntryRefsOS.RemoveAt(iEntry);
			}
		}

		internal void UpdateMorphoSyntaxAnalysesOfLexEntryRefs()
		{
			var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
			foreach (var entryRef in EntryRefsOS)
				uowService.RegisterVirtualAsModified(entryRef, "MorphoSyntaxAnalyses", entryRef.MorphoSyntaxAnalyses.Cast<ICmObject>());
		}

		/// <summary>
		/// Return the number of analyses in interlinear text for this entry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int EntryAnalysesCount
		{
			get
			{
				int count = 0;
				List<IMoForm> forms = new List<IMoForm>();
				if (LexemeFormOA != null)
					forms.Add(LexemeFormOA);
				foreach (IMoForm mfo in AlternateFormsOS)
					forms.Add(mfo);
				foreach (IMoForm mfo in forms)
				{
					foreach (ICmObject cmo in mfo.ReferringObjects)
						if (cmo is IWfiMorphBundle)
							count += (cmo.Owner as WfiAnalysis).OccurrencesInTexts.Count<ISegment>();
				}
				return count;
			}
		}
	}

	internal partial class PartOfSpeech
	{
		public IEnumerable<IMoStemName> AllStemNames
		{
			get
			{
				var stemNames = new HashSet<IMoStemName>(StemNamesOC);
				if (Owner.ClassID == PartOfSpeechTags.kClassId)
				{
					var owner = Owner as IPartOfSpeech;
					stemNames.UnionWith(owner.AllStemNames);
				}
				return stemNames;
			}
		}

		/// <summary>
		/// Get all inflection classes owned by this part of speech,
		/// and by any part of speech that owns this one,
		/// up to the owning list.
		/// </summary>
		public IEnumerable<IMoInflClass> AllInflectionClasses
		{
			get
			{
				var classes = new HashSet<IMoInflClass>(InflectionClassesOC);
				if (Owner.ClassID == PartOfSpeechTags.kClassId)
				{
					var owner = Owner as IPartOfSpeech;
					classes.UnionWith(owner.AllInflectionClasses);
				}
				return classes;
			}
		}

		/// <summary>
		/// Get all affix slots owned by this part of speech,
		/// and by any part of speech that owns this one,
		/// up to the owning list.
		/// </summary>
		public IEnumerable<IMoInflAffixSlot> AllAffixSlots
		{
			get
			{
				var slots = new HashSet<IMoInflAffixSlot>(AffixSlotsOC);
				if (Owner.ClassID == PartOfSpeechTags.kClassId)
				{
					var owner = Owner as IPartOfSpeech;
					slots.UnionWith(owner.AllAffixSlots);
				}
				return slots;
			}
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case PartOfSpeechTags.kflidDefaultInflectionClass:
					return this;
				case PartOfSpeechTags.kflidBearableFeatures:
					return m_cache.LangProject.ExceptionFeatureType;
				case PartOfSpeechTags.kflidInflectableFeats:
					return m_cache.LangProject.MsFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Get a set of objects that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case PartOfSpeechTags.kflidDefaultInflectionClass:
					return AllInflectionClasses.Cast<ICmObject>();
				case PartOfSpeechTags.kflidBearableFeatures:
					return m_cache.LangProject.ExceptionFeatureType.FeaturesRS.Cast<ICmObject>();
				case PartOfSpeechTags.kflidInflectableFeats:
					return m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}
		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmation dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.DefaultUserWs;
				var analWs = m_cache.DefaultAnalWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
				tisb.AppendTsString(ShortNameTSS);

				var countedObjectIDs = new List<int>();
				var msaCount = 0;
				var analCount = 0;
				var alloCount = 0;
				var revCount = 0;
				var servLoc = Cache.ServiceLocator;
				var stemMsas =
					servLoc.GetInstance<IMoStemMsaRepository>().AllInstances().Where(stemMsa => stemMsa.PartOfSpeechRA == this);
				foreach (var msa in stemMsas)
				{
					if (countedObjectIDs.Contains(msa.Hvo)) continue;

					countedObjectIDs.Add(msa.Hvo);
					++msaCount;
				}
				var derivAfxMsas = servLoc.GetInstance<IMoDerivAffMsaRepository>().AllInstances();
				foreach (var msa in derivAfxMsas.Where(derivAfxMsa => derivAfxMsa.FromPartOfSpeechRA == this))
				{
					if (countedObjectIDs.Contains(msa.Hvo)) continue;

					countedObjectIDs.Add(msa.Hvo);
					++msaCount;
				}
				foreach (var msa in derivAfxMsas.Where(derivAfxMsa => derivAfxMsa.ToPartOfSpeechRA == this))
				{
					if (countedObjectIDs.Contains(msa.Hvo)) continue;

					countedObjectIDs.Add(msa.Hvo);
					++msaCount;
				}
				var derivStepMsas = servLoc.GetInstance<IMoDerivStepMsaRepository>().AllInstances();
				foreach (var msa in derivStepMsas.Where(m => m.PartOfSpeechRA == this))
				{
					if (countedObjectIDs.Contains(msa.Hvo)) continue;

					countedObjectIDs.Add(msa.Hvo);
					++msaCount;
				}
				var inflAfxMsas = servLoc.GetInstance<IMoInflAffMsaRepository>().AllInstances();
				foreach (var msa in inflAfxMsas.Where(m => m.PartOfSpeechRA == this))
				{
					if (countedObjectIDs.Contains(msa.Hvo)) continue;

					countedObjectIDs.Add(msa.Hvo);
					++msaCount;
				}
				var uncAfxMsas = servLoc.GetInstance<IMoUnclassifiedAffixMsaRepository>().AllInstances();
				foreach (var msa in uncAfxMsas.Where(m => m.PartOfSpeechRA == this))
				{
					if (countedObjectIDs.Contains(msa.Hvo)) continue;

					countedObjectIDs.Add(msa.Hvo);
					++msaCount;
				}
				var afxAllos = servLoc.GetInstance<IMoAffixAllomorphRepository>().AllInstances();
				foreach (var allo in afxAllos.Where(a => a.MsEnvPartOfSpeechRA == this))
				{
					if (countedObjectIDs.Contains(allo.Hvo)) continue;

					countedObjectIDs.Add(allo.Hvo);
					++alloCount;
				}
				var entries = servLoc.GetInstance<IReversalIndexEntryRepository>().AllInstances();
				foreach (var rei in entries.Where(e => e.PartOfSpeechRA == this))
				{
					if (countedObjectIDs.Contains(rei.Hvo)) continue;

					countedObjectIDs.Add(rei.Hvo);
					++revCount;
				}
				var anals = servLoc.GetInstance<IWfiAnalysisRepository>().AllInstances();
				foreach (var anal in anals.Where(a => a.CategoryRA == this))
				{
					if (countedObjectIDs.Contains(anal.Hvo)) continue;

					countedObjectIDs.Add(anal.Hvo);
					++analCount;
				}

				var cnt = 1;
				var warningMsg = String.Format("\x2028\x2028{0}", Strings.ksCategUsedHere);
				var wantMainWarningLine = true;
				if (analCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (analCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByWFAnals, cnt++, analCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByWFAnals, cnt++));
					wantMainWarningLine = false;
				}
				if (msaCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (msaCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByFuncs, cnt++, msaCount, StringUtils.kChHardLB));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByFuncs, cnt++, StringUtils.kChHardLB));
					wantMainWarningLine = false;
				}
				if (alloCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (alloCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByAllos, cnt++, alloCount, StringUtils.kChHardLB));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByAllos, cnt++, StringUtils.kChHardLB));
				}
				if (revCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (revCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByRevEntries, cnt++, revCount, StringUtils.kChHardLB));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByRevEntries, cnt++, StringUtils.kChHardLB));
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Determine if the POS or any of its super POSes require inflection (i.e. have an inflectional template)
		/// </summary>
		/// <returns>true if so; false otherwise</returns>
		public bool RequiresInflection
		{
			get
			{
				for (PartOfSpeech pos = this; pos != null; pos = pos.Owner as PartOfSpeech)
				{
					if (pos.AffixTemplatesOS.Count > 0)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Add any new inflectable features from an Xml description
		/// </summary>
		/// <param name="item">The item.</param>
		public void AddInflectableFeatsFromXml(XmlNode item)
		{
			var featsys = m_cache.LanguageProject.MsFeatureSystemOA;
			var fst = featsys.GetOrCreateFeatureTypeFromXml(item);
			if (fst != null)
			{
				var defn = featsys.GetOrCreateFeatureFromXml(item, fst);
				if (defn != null)
					InflectableFeatsRC.Add(defn);
			}
		}

		/// <summary>
		/// Gets the highest PartOfSpeech in the hierarchy
		/// </summary>
		/// <value>The highest part of speech.</value>
		public IPartOfSpeech HighestPartOfSpeech
		{
			get
			{
				IPartOfSpeech pos = this;
				while (pos.ClassID == PartOfSpeechTags.kClassId)
				{
					var owner = pos.OwnerOfClass<IPartOfSpeech>();
					if (owner != null)
						pos = owner;
					else
						break;
				}
				return pos;
			}
		}

		/// <summary>
		/// Return the number of unique LexEntries that reference this POS via MoStemMsas.
		/// </summary>
		public int NumberOfLexEntries
		{
			get
			{
				//old system used this logic:
				//int count = 0;
				//// The SQL command must NOT modify the database contents!
				//string sSql = String.Format("select count(distinct o.owner$) from MoStemMsa msa" +
				//    " join CmObject o on o.id = msa.id" +
				//    " where msa.PartOfSpeech = {0}" +
				//    " group by msa.PartOfSpeech", Hvo);
				//DbOps.ReadOneIntFromCommand(m_cache, sSql, null, out count);
				//return count;
				int cLex = 0;
				ILexEntryRepository repoLex = Services.GetInstance<ILexEntryRepository>();
				foreach (ILexEntry le in repoLex.AllInstances())
				{
					foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
					{
						if (msa is IMoStemMsa && (msa as IMoStemMsa).PartOfSpeechRA == this)
						{
							++cLex;
							break;
						}
					}
				}
				return cLex;
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class LexSense
	{
		private int m_MLOwnerOutlineNameFlid;

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();

			m_MLOwnerOutlineNameFlid = Cache.MetaDataCache.GetFieldId("LexSense", "MLOwnerOutlineName", false);
		}

		internal IEnumerable<ILexEntryRef> EntryRefsWithThisMainSense
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexEntryRefTags.kflidComponentLexemes);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexEntryRefTags.kflidComponentLexemes)
						yield return sequence.MainObject as ILexEntryRef;
				}
			}
		}

		/// <summary>
		/// The publications from which this is not excluded, that is, the ones in which it
		/// SHOULD be published.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "CmPossibility")]
		public IFdoSet<ICmPossibility> PublishIn
		{
			get
			{
				return new FdoInvertSet<ICmPossibility>(DoNotPublishInRC, Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS);
			}
		}

		internal IEnumerable<ILexEntryRef> ComplexFormRefsWithThisPrimarySense
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexEntryRefTags.kflidPrimaryLexemes);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexEntryRefTags.kflidPrimaryLexemes &&
						(sequence.MainObject as ILexEntryRef).RefType == LexEntryRefTags.krtComplexForm)
					{
						yield return sequence.MainObject as ILexEntryRef;
					}
				}
			}
		}

		internal IEnumerable<ILexEntryRef> ComplexFormRefsVisibleInThisSense
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexEntryRefTags.kflidShowComplexFormsIn);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexEntryRefTags.kflidShowComplexFormsIn &&
						(sequence.MainObject as ILexEntryRef).RefType == LexEntryRefTags.krtComplexForm)
					{
						yield return sequence.MainObject as ILexEntryRef;
					}
				}
			}
		}

		internal IEnumerable<ILexReference> ReferringLexReferences
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexReferenceTags.kflidTargets);
				foreach (var item in m_incomingRefs)
				{
					var sequence = item as FdoReferenceSequence<ICmObject>;
					if (sequence == null)
						continue;
					if (sequence.Flid == LexReferenceTags.kflidTargets)
						yield return sequence.MainObject as ILexReference;
				}
			}
		}

		/// <summary>
		/// This is called (by reflection) in an RDE view (DoMerges() method of XmlBrowseRDEView)
		/// that is creating LexSenses (and entries) by having
		/// the user enter a lexeme form and definition
		/// for a collection of words in a given semantic domain.
		/// On loss of focus, switch domain, etc., this method is called for each
		/// newly created sense, to determine whether it can usefully be merged into some
		/// pre-existing lex entry.
		///
		/// The idea is to do one of the following, in order of preference:
		/// (a) If there are other LexEntries which have the same LF and a sense with the
		/// same definition, add hvoDomain to the domains of those senses, and delete hvoSense.
		/// (b) If there is a pre-existing LexEntry (not the owner of one of newHvos)
		/// that has the same lexeme form, move hvoSense to that LexEntry.
		/// (c) If there is another new LexEntry (the owner of one of newHvos other than hvoSense)
		/// that has the same LF, we want to merge the two. In this case we expect to be called
		/// in turn for all of these senses, so to simplify, the one with the smallest HVO
		/// is kept and the others merged.
		/// </summary>
		/// <param name="hvoDomain"></param>
		/// <param name="columns">List of XmlNode objects</param>
		/// <param name="cache"></param>
		/// <param name="newHvos">Set of new senses (including hvoSense).</param>
		/// <returns>true if the sense has been deleted</returns>
		public bool RDEMergeSense(int hvoDomain,
			List<XmlNode> columns, FdoCache cache, Set<int> newHvos)
		{
			bool result = false;
			// The goal is to find a lex entry with the same lexeme form.form as our LexEntry.
			ILexSense ls = cache.ServiceLocator.GetObject(Hvo) as ILexSense;
			ILexEntry leTarget = ls.OwnerOfClass<ILexEntry>();
			string homographForm = leTarget.HomographForm;
			string sDefnTarget = ls.Definition.AnalysisDefaultWritingSystem.Text;

			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
				"Undo adding sense or entry in Categorized Edit",
				"Redo adding sense or entry in Categorized Edit",
				m_cache.ActionHandlerAccessor, () =>
				{

					// Check for pre-existing LexEntry which has the same homograph form
					bool fGotExactMatch;
					ILexEntry leSaved = FindBestLexEntryAmongstHomographs(cache, homographForm, sDefnTarget, leTarget, newHvos, hvoDomain, out fGotExactMatch);
					if (fGotExactMatch)
					{
						// delete the entry AND sense
						leTarget.Delete(); // careful! This just got deleted.
						result = true; // deleted sense altogether
					}
					else if (leSaved != null)
					{
						// move the one and only sense of leTarget to leSaved...provided it has a compatible MSA
						// of the expected type.
						if (MorphoSyntaxAnalysisRA is MoStemMsa)
						{
							IMoMorphSynAnalysis newMsa = null;
							foreach (IMoMorphSynAnalysis msa in leSaved.MorphoSyntaxAnalysesOC)
							{
								if (msa is IMoStemMsa)
									newMsa = msa;
							}
							if (newMsa != null)
							{
								// Fix the MSA of the sense to point at one of the MSAs of the new owner.
								MorphoSyntaxAnalysisRA = newMsa;
								// Move it to the new owner.
								leSaved.SensesOS.Add(this);
								// delete the entry.
								leTarget.Delete();
								// But NOT the sense from the domain...it just moved to another entry.
								//DeleteSense(cache, hvoDomain, tagList, hvoSense);
							}
						}
					}
				});
			// else do nothing (no useful match, let the LE survive)
			return result;
		}

		/// <summary>
		/// find the best existing LexEntry option matching 'homographform' (and possibly 'sDefnTarget')
		/// in order to determine if we should merge leTarget into that entry.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="homographForm"></param>
		/// <param name="sDefnTarget"></param>
		/// <param name="leTarget">a LexEntry that you want to consider merging into a more appropriate LexEntry,
		/// if null, we ignore 'newHvos' and 'hvoDomain'</param>
		/// <param name="newHvos"></param>
		/// <param name="hvoDomain"></param>
		/// <param name="fGotExactMatch"></param>
		/// <returns></returns>
		private static ILexEntry FindBestLexEntryAmongstHomographs(FdoCache cache,
			string homographForm, string sDefnTarget, ILexEntry leTarget, Set<int> newHvos,
			int hvoDomain, out bool fGotExactMatch)
		{
			ILexEntry leSaved = null;
			var entryRepo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			List<ILexEntry> rgEntries = ((ILexEntryRepositoryInternal)entryRepo).CollectHomographs(homographForm, 0,
				entryRepo.GetHomographs(homographForm),
				cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem), true);
			leSaved = null; // saved entry to merge into (from previous iteration)
			bool fSavedIsOld = false; // true if leSaved is old (and non-null).
			fGotExactMatch = false; // true if we find a match for cf AND defn.
			bool fCurrentIsNew = false;
			foreach (ILexEntry leCurrent in rgEntries)
			{
				if (leTarget != null)
				{
					if (leCurrent.Hvo == leTarget.Hvo)
						continue; // not interested in merging with ourself.
					// See if this is one of the newly added entries. If it is, it has exactly one sense,
					// and that sense is in our list.
					fCurrentIsNew = leCurrent.SensesOS.Count == 1 && newHvos.Contains(leCurrent.SensesOS.ToHvoArray()[0]);
					if (fCurrentIsNew && leCurrent.Hvo > leTarget.Hvo)
						continue;  // won't consider ANY kind of merge with a new object of greater HVO.
				}
				// Decide whether lexE should be noted as the entry that we will merge with if
				// we don't find an exact match.
				if (!fGotExactMatch) // leMerge is irrelevant if we already got an exact match.
				{
					if (leSaved == null)
					{
						leSaved = leCurrent;
						fSavedIsOld = !fCurrentIsNew;
					}
					else // we have already found a candidate
					{
						if (fSavedIsOld)
						{
							// We will only consider the new one if it is also old, and
							// (rather arbitrarily) if it has a smaller HVO
							if ((!fCurrentIsNew) && leCurrent.Hvo < leSaved.Hvo)
							{
								leSaved = leCurrent; // fSavedIsOld stays true.
							}
						}
						else // we already have a candidate, but it is another of the new entries
						{
							// if current is old, we'll use it for sure
							if (!fCurrentIsNew)
							{
								leSaved = leCurrent;
								fSavedIsOld = false; // since fCurrentIsNew is false.
							}
							else
							{
								// we already have a new candidate (which must have a smaller hvo than target)
								// and now we have another new entry which matches!
								// We'll prefer it only if its hvo is smaller still.
								if (leCurrent.Hvo < leSaved.Hvo)
								{
									leSaved = leCurrent; // fSavedIsOld stays false.
								}
							}
						}
					}
				}

				// see if we want to try to find a matching existing sense.
				if (sDefnTarget == null)
					continue;
				// This deals with all senses in the entry,
				// whether owned directly by the entry or by its senses
				// at whatever level.
				// If the new definition matches an existing defintion (or if both
				// are missing) add the current domain to the existing sense.
				// Note: if more than one sense has the same definition (maybe missing) we should
				// add the domain to all senses--not just the first one encountered.
				foreach (ILexSense lexS in leCurrent.AllSenses)
				{
					if (lexS.Definition != null
						&& lexS.Definition.AnalysisDefaultWritingSystem != null)
					{
						string sDefnCurrent = lexS.Definition.AnalysisDefaultWritingSystem.Text;
						if ((sDefnCurrent == null && sDefnTarget == null) ||
							(sDefnCurrent != null && sDefnTarget != null && sDefnCurrent.Trim() == sDefnTarget.Trim()))
						{
							// We found a sense that has the same citation form and definition as the one
							// we're trying to merge.
							// Add the new domain to that sense (if not already present), delete the temporary one,
							// and return. (We're not displaying this sense, so don't bother trying to update the display)
							if (hvoDomain > 0)
							{
								var domain = cache.ServiceLocator.GetObject(hvoDomain) as ICmSemanticDomain;
								if (!lexS.SemanticDomainsRC.Contains(domain))
									lexS.SemanticDomainsRC.Add(domain);
								fGotExactMatch = true;
							}
						}
					}
				}
			} // loop over matching entries

			return leSaved;
		}
		/// <summary>
		/// Return your own pictures followed by those of any subsenses.
		/// </summary>
		public IEnumerable<ICmPicture> Pictures
		{
			get
			{
				return
					PicturesOS.Concat(from LexSense sense in SensesOS
									  from picture in sense.Pictures
									  select picture);
			}
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case LexSenseTags.kflidSenses:
					// The virtual properties LexSenseOutline and LexEntry.NumberOfSensesForEntry may be changed for the inserted sense
					// and all its following senses and their subsenses.
					SensesChangedPosition(e.Index);
					((LexEntry) Entry).NumberOfSensesChanged(true);
					break;
				case LexSenseTags.kflidReversalEntries:
					int ws = ((ReversalIndexEntry) e.ObjectAdded).WritingSystem;
					if (ws != 0) // defensive, mainly for tests
						ReversalEntriesBulkTextChanged(ws);
					ReversalEntryReferringSensesChanged(e.ObjectAdded as ReversalIndexEntry, true);
					break;
				case LexSenseTags.kflidSemanticDomains:
					List<ICmObject> backrefs = ((CmSemanticDomain)e.ObjectAdded).ReferringSenses.Cast<ICmObject>().ToList();
					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(e.ObjectAdded,
						"ReferringSenses", backrefs);
					break;
				case LexSenseTags.kflidPictures:
					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(OwningEntry, "PicturesOfSenses",
						((LexEntry)OwningEntry).PicturesOfSenses.Cast<ICmObject>());
					break;
				case LexSenseTags.kflidDoNotPublishIn:
					var uowService = ((IServiceLocatorInternal) Services).UnitOfWorkService;
					uowService.RegisterVirtualAsModified(this, "PublishIn", PublishIn.Cast<ICmObject>());
					break;
			}
			base.AddObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// Do side effects resulting from changes to the position of senses from startIndex onwards.
		/// </summary>
		private void SensesChangedPosition(int startIndex)
		{
			for (int i = startIndex; i < SensesOS.Count; i++)
				(SensesOS[i] as LexSense).LexSenseOutlineChanged();
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case LexSenseTags.kflidSenses:
					// The virtual property LexSenseOutline may be changed for the senses after the deleted one
					// and their subsenses.
					SensesChangedPosition(e.Index);
					((LexEntry) Entry).NumberOfSensesChanged(false);
					break;
				case LexSenseTags.kflidReversalEntries:
					int ws = ((ReversalIndexEntry)e.ObjectRemoved).WritingSystem;
					if (ws != 0) // defensive, mainly for tests
						ReversalEntriesBulkTextChanged(ws);
					ReversalEntryReferringSensesChanged(e.ObjectRemoved as ReversalIndexEntry, false);
					break;
				case LexSenseTags.kflidSemanticDomains:
					List<ICmObject> backrefs = ((CmSemanticDomain)e.ObjectRemoved).ReferringSenses.Cast<ICmObject>().ToList();
					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(e.ObjectRemoved,
						"ReferringSenses", backrefs);
					break;
				case LexSenseTags.kflidPictures:
					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(OwningEntry, "PicturesOfSenses",
						((LexEntry)OwningEntry).PicturesOfSenses.Cast<ICmObject>());
					break;
				case LexSenseTags.kflidDoNotPublishIn:
					var uowService = ((IServiceLocatorInternal)Services).UnitOfWorkService;
					uowService.RegisterVirtualAsModified(this, "PublishIn", PublishIn.Cast<ICmObject>());
					break;
			}
			base.RemoveObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// Something changed which may cause our LexSenseOutline to be invalid. Ensure a PropChanged will update views.
		/// </summary>
		internal void LexSenseOutlineChanged()
		{
			int flid = m_cache.MetaDataCache.GetFieldId2(LexSenseTags.kClassId, "LexSenseOutline", false);
			ITsString tssOutline = LexSenseOutline;
			// We can't get a true old value, but a string the same length with different characters should cause the appropriate display
			// updating. Pathologically, the old value might differ in length; if that causes a problem at some point, we'll have to
			// deal with it.
			ITsStrBldr bldr = tssOutline.GetBldr();
			StringBuilder sb = new StringBuilder(bldr.Length);
			sb.Append(' ', bldr.Length);
			bldr.Replace(0, bldr.Length, sb.ToString(), null);
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(this, flid, bldr.GetString(), tssOutline);
			foreach (LexSense sense in SensesOS)
				sense.LexSenseOutlineChanged();
			// If our sense number changed, our MLOwnerOutlineName changes too.
			// Enhance JohnT: conceivably other Wss of it are in use and change also.
			MLOwnerOutlineNameChanged(Cache.DefaultVernWs);
		}

		/// <summary>
		/// Something changed which may cause our ReversalIndexBulkText to be invalid for the specified writing system.
		/// Ensure a PropChanged will update views.
		/// </summary>
		internal void ReversalEntriesBulkTextChanged(int ws)
		{
			// Enhance JohnT: is there some way to pass a valid old value? Does it matter?
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(this,
				ReversalEntriesBulkTextFlid, ws, null, ReversalEntriesBulkTextForWs(ws));
		}

		private void ReversalEntryReferringSensesChanged(ReversalIndexEntry rie, bool fAdded)
		{
			var unitOfWorkService = ((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService;
			// We don't need to record virtual property changes for newly created objects. Nothing can be displaying the old value.
			if (unitOfWorkService.IsNew(rie))
				return;
			int flid = m_cache.MetaDataCache.GetFieldId2(ReversalIndexEntryTags.kClassId, "ReferringSenses", false);
			List<Guid> guids = new List<Guid>();
			var referringSenses = rie.ReferringSenses;
			foreach (ILexSense ls in referringSenses)
			{
				if (ls == this && !fAdded)
					continue;
				guids.Add(ls.Guid);
			}
			if (fAdded && !referringSenses.Contains(this))
				guids.Add(this.Guid);
			unitOfWorkService.RegisterVirtualAsModified(rie,
				flid, new Guid[0], guids.ToArray());
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == LexSenseTags.kflidGloss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexSense in ShowComplexFormIn  and are complex
		/// forms.
		/// Enhance JohnT: Generate PropChanged on this for changes to any of
		///     LexEntry.EntryRefs, LexEntryRef.RefType, LexEntryRef.PrimaryEntryOrSense,
		///     or anything that affects GetVariantEntryRefsWithMainEntryOrSense.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> VisibleComplexFormBackRefs
		{
			get
			{
				return VirtualOrderingServices.GetOrderedValue(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseVisibleComplexFormBackRefs,
					((LexEntryRefRepository)Services.GetInstance<ILexEntryRefRepository>()).SortEntryRefs(ComplexFormRefsVisibleInThisSense));
			}
		}

		/// <summary>
		/// This returns a subset of VisibleComplexFormBackRefs, specifically those that are NOT also subentries.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> ComplexFormsNotSubentries
		{
			get { return VisibleComplexFormBackRefs.Where(ler => !ler.PrimaryLexemesRS.Contains(this)); }
		}

		/// <summary>
		/// Virtual property allows Headword to be read through cache.
		/// Enhance JohnT: propChange for changes to sense organization, headword
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiString)]
		public VirtualStringAccessor MLOwnerOutlineName
		{
			get
			{
				return new VirtualStringAccessor(this, m_MLOwnerOutlineNameFlid, OwnerOutlineNameForWs);
			}
		}
		/// <summary>
		/// Gets the lexical entry that owns this entry. This is redundant; sometime we should refactor it out.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "LexEntry")]
		public ILexEntry OwningEntry
		{
			get { return Entry; }
		}

		/// <summary>
		/// Override default implementation to make a more suitable TS string for a wordform.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
									  (int)FwTextPropVar.ktpvEnum,
									  (int)TptEditable.ktptNotEditable);
				var userWs = m_cache.WritingSystemFactory.UserWs;
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteLexSense, " "));
				tisb.AppendTsString(ShortNameTSS);

				var lmeCount = 0;
				var lseCount = 0;
				var servLoc = Cache.ServiceLocator;

				var bundles = servLoc.GetInstance<IWfiMorphBundleRepository>().AllInstances();
				var mbCount = (bundles.Where(bundle => bundle.SenseRA == this)).Count();

				var entries = servLoc.GetInstance<ILexEntryRepository>().AllInstances();
				lmeCount = (entries.Where(e => e.MainEntriesOrSensesRS.Contains(this))).Count();
				//foreach (var obj in entries.Where(e => e.MainEntriesOrSensesRS.Contains(this)))
				//{
				//    if (obj is ILexEntry)
				//        ++lmeCount;
				//    else
				//        ++lseCount;
				//}

				var cnt = 1;
				var warningMsg = String.Format("\x2028\x2028{0}", Strings.ksSenseUsedHere);
				var wantMainWarningLine = true;
				if (mbCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (mbCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesInAnalyses, cnt++, mbCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceInAnalyses, cnt++));
					wantMainWarningLine = false;
				}
				if (lmeCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (lmeCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByEntries, cnt++, lmeCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByEntries, cnt++));
					wantMainWarningLine = false;
				}
				if (lseCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (lseCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesBySubentries, cnt++, lseCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceBySubentries, cnt++));
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get the desired type of an MSA to create for this sense.
		/// </summary>
		/// <returns></returns>
		public MsaType GetDesiredMsaType()
		{
			var entry = Entry;
			var morphType = entry.PrimaryMorphType;
			var morphTypesMixed = entry.IsMorphTypesMixed;
			MsaType msaType = MsaType.kNotSet;
			var fEntryIsAffixType = morphType != null ? morphType.IsAffixType : false;
			// Treat the type currently specified for the whole entry as having been seen.
			// This helps prevent showing the wrong dialog if the user changes the entry morph type.
			var fAffixTypeSeen = fEntryIsAffixType;
			var fStemTypeSeen = !fAffixTypeSeen;
			// Get current MSAs, and check which kind they are.
			// We are interested in knowing if they are the same kind or a mixed bag.
			foreach (var msa in entry.MorphoSyntaxAnalysesOC)
			{
				switch (msa.ClassID)
				{
					case MoStemMsaTags.kClassId:
						{
							fStemTypeSeen = true;
							if (msaType == MsaType.kNotSet && !fEntryIsAffixType)
							{
								msaType = MsaType.kStem;
							}
							else if (msaType != MsaType.kStem)
							{
								msaType = MsaType.kMixed;
								Debug.Assert(fAffixTypeSeen);
								morphTypesMixed = true;
							}
							break;
						}
					case MoUnclassifiedAffixMsaTags.kClassId:
						{
							fAffixTypeSeen = true;
							if (msaType == MsaType.kNotSet && fEntryIsAffixType)
							{
								msaType = MsaType.kUnclassified;
							}
							else if (msaType != MsaType.kUnclassified)
							{
								msaType = MsaType.kMixed;
								if (fStemTypeSeen)
									morphTypesMixed = true;
							}
							break;
						}
					case MoInflAffMsaTags.kClassId:
						{
							fAffixTypeSeen = true;
							if (msaType == MsaType.kNotSet && fEntryIsAffixType)
							{
								msaType = MsaType.kInfl;
							}
							else if (msaType != MsaType.kInfl)
							{
								msaType = MsaType.kMixed;
								if (fStemTypeSeen)
									morphTypesMixed = true;
							}
							break;
						}
					case MoDerivAffMsaTags.kClassId:
						{
							fAffixTypeSeen = true;
							if (msaType == MsaType.kNotSet && fEntryIsAffixType)
							{
								msaType = MsaType.kDeriv;
							}
							else if (msaType != MsaType.kDeriv)
							{
								msaType = MsaType.kMixed;
								if (fStemTypeSeen)
									morphTypesMixed = true;
							}
							break;
						}
				}
			}
			if (msaType == MsaType.kNotSet || msaType == MsaType.kMixed)
			{
				if (morphTypesMixed)
				{
					// TODO: what about entries with mixed morph types?
					// Make it the most general type appropriate for the type of the entry.
					if (fEntryIsAffixType)
						msaType = MsaType.kUnclassified;
					else
						msaType = MsaType.kStem;
				}
				else if (morphType == null || morphType.IsAffixType)
				{
					msaType = MsaType.kUnclassified;
				}
				else
				{
					msaType = MsaType.kStem;
				}
			}
			return msaType;
		}

		/// <summary>
		/// Gets this sense and all senses it owns.
		/// </summary>
		public List<ILexSense> AllSenses
		{
			get
			{
				var senses = new List<ILexSense>();
				senses.Add(this);
				foreach (var ls in SensesOS)
					senses.AddRange(ls.AllSenses);

				return senses;
			}
		}

		/// <summary>
		/// Get the outline number used to display senses.
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString LexSenseOutline
		{
			get
			{
				string outline = m_cache.GetOutlineNumber(this, LexSenseTags.kflidSenses, false, true);
				return m_cache.MakeUserTss(outline);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string with the entry headword and a sense number if there is more than
		/// one sense.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString FullReferenceName
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.AppendTsString(OwnerOutlineName);
				tisb.Append(" ");
				// Add Sense POS and gloss info, as per LT-3811.
				if (MorphoSyntaxAnalysisRA != null)
				{
					ITsStrBldr tsb = MorphoSyntaxAnalysisRA.ChooserNameTS.GetBldr();
					tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptItalic,
						(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
					tisb.AppendTsString(tsb.GetString());
					tisb.Append(" ");
				}
				tisb.AppendTsString(Gloss.BestAnalysisAlternative);
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Check whether this sense or any of its subsenses uses the given MSA.
		/// </summary>
		/// <param name="msaOld"></param>
		/// <returns></returns>
		internal bool UsesMsa(IMoMorphSynAnalysis msaOld)
		{
			if (msaOld.Equals(MorphoSyntaxAnalysisRA))	// == doesn't work!  See LT-7088.
				return true;
			foreach (LexSense ls in SensesOS)
			{
				if (ls.UsesMsa(msaOld))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Get the entry that owns the sense.
		/// </summary>
		public ILexEntry Entry
		{
			get { return OwnerOfClass<ILexEntry>(); }
		}

		/// <summary>
		/// Resets the MSA to an equivalent MSA, whether it finds it, or has to create a new one.
		/// </summary>
		public SandboxGenericMSA SandboxMSA
		{
			set
			{
				if (value == null)
					return;

				// JohnT: per LT-4900, we changed out minds again, and want an MSA made even if it has no information.
				// This is currently necessary for proper operation of the parser: only entries with MSAs are considered
				// as possible analysis components, and when the parser is filing results, it creates analyses which point
				// to them.
				//if (value.MainPOS == 0 && value.SecondaryPOS == 0 && value.Slot == 0 && value.MsaType == MsaType.kUnclassified)
				//    return;		// no real information available -- don't bother (LT-4433) (But see LT-4870 for inclusion of type--JohnT)

				var entry = Entry;
				var msaOld = MorphoSyntaxAnalysisRA;
				foreach (var msa in entry.MorphoSyntaxAnalysesOC)
				{
					if ((msa as MoMorphSynAnalysis).EqualsMsa(value))
					{
						MorphoSyntaxAnalysisRA = msa; // setter handles deleting msaOld if it is no longer used.
						return;
					}
				}

				// Need to create a new one.
				IMoMorphSynAnalysis msaMatch = null;
				switch (value.MsaType)
				{
					case MsaType.kRoot: // Fall through
					case MsaType.kStem:
						{
							var stemMsa = new MoStemMsa();
							entry.MorphoSyntaxAnalysesOC.Add(stemMsa);
							if (value.MainPOS != null)
								stemMsa.PartOfSpeechRA = value.MainPOS;
							stemMsa.FromPartsOfSpeechRC.Clear();
							if (value.FromPartsOfSpeech != null)
							{
								foreach (var pos in value.FromPartsOfSpeech)
									stemMsa.FromPartsOfSpeechRC.Add(pos);
							}

							// copy over attributes, such as inflection classes and features, that are still valid for the
							// new category
							var oldStemMsa = msaOld as MoStemMsa;
							if (oldStemMsa != null)
								stemMsa.CopyAttributesIfValid(oldStemMsa);

							entry.MorphoSyntaxAnalysesOC.Add(stemMsa);	// added after setting POS so slice will show POS
							msaMatch = stemMsa;
							break;
						}
					case MsaType.kInfl:
						{
							var inflMsa = new MoInflAffMsa();
							entry.MorphoSyntaxAnalysesOC.Add(inflMsa);
							if (value.MainPOS != null)
								inflMsa.PartOfSpeechRA = value.MainPOS;
							if (value.Slot != null)
								inflMsa.SlotsRC.Add(value.Slot);

							// copy over attributes, such as inflection classes and features, that are still valid for the
							// new category
							var oldInflMsa = msaOld as MoInflAffMsa;
							if (oldInflMsa != null)
								inflMsa.CopyAttributesIfValid(oldInflMsa);

							entry.MorphoSyntaxAnalysesOC.Add(inflMsa);	// added after setting POS so slice will show POS
							msaMatch = inflMsa;
							break;
						}
					case MsaType.kDeriv:
						{
							var derivMsa = new MoDerivAffMsa();
							entry.MorphoSyntaxAnalysesOC.Add(derivMsa);
							if (value.MainPOS != null)
								derivMsa.FromPartOfSpeechRA = value.MainPOS;
							if (value.SecondaryPOS != null)
								derivMsa.ToPartOfSpeechRA = value.SecondaryPOS;

							// copy over attributes, such as inflection classes and features, that are still valid for the
							// new category
							var oldDerivMsa = msaOld as MoDerivAffMsa;
							if (oldDerivMsa != null)
							{
								derivMsa.CopyToAttributesIfValid(oldDerivMsa);
								derivMsa.CopyFromAttributesIfValid(oldDerivMsa);
							}

							entry.MorphoSyntaxAnalysesOC.Add(derivMsa);	// added after setting POS so slice will show POS
							msaMatch = derivMsa;
							break;
						}
					case MsaType.kUnclassified:
						{
							var uncMsa = new MoUnclassifiedAffixMsa();
							entry.MorphoSyntaxAnalysesOC.Add(uncMsa);
							if (value.MainPOS != null)
								uncMsa.PartOfSpeechRA = value.MainPOS;
							else
								uncMsa.PartOfSpeechRA = null;
							entry.MorphoSyntaxAnalysesOC.Add(uncMsa);	// added after setting POS so slice will show POS
							msaMatch = uncMsa;
							break;
						}
				}
				MorphoSyntaxAnalysisRA = Cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(msaMatch.Hvo);
				if (msaOld != null && msaOld.IsValidObject && entry is LexEntry && !(entry as LexEntry).UsesMsa(msaOld))
				{
					ReplaceReferences(m_cache, msaOld, msaMatch);
					// ReplaceReferences may well delete this object for us.  See FWR-2855.
					if (msaOld.IsValidObject)
						Cache.DomainDataByFlid.DeleteObj(msaOld.Hvo);
				}
			}
		}

		/// <summary>
		///If there's no LiftResidue, return the sense's guid as a string.
		/// If there's a LIFTid element in ImportResidue in the form of 'gloss_&lt;guid&gt;, use
		/// that instead, but use the real guid instead of the stored one.  (See FWR-2621.)  If
		/// the stored id does not have an underscore, just return the sense's guid as a string.
		/// </summary>
		public string LIFTid
		{
			get
			{
				string sLiftId = null;
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					sResidue = LexEntry.ExtractLIFTResidue(m_cache, m_hvo,
						LexSenseTags.kflidImportResidue, LexSenseTags.kflidLiftResidue);
				if (!String.IsNullOrEmpty(sResidue))
					sLiftId = LexEntry.ExtractAttributeFromLiftResidue(sResidue, "id");
				if (String.IsNullOrEmpty(sLiftId))
					return Guid.ToString();
				int idx = sLiftId.IndexOf('_');
				if (idx >= 0)
					return sLiftId.Substring(0, idx) + "_" + Guid;
				return Guid.ToString();
			}
		}

		/// <summary>
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		//[VirtualProperty(CellarPropertyType.String)]
		public string LiftResidueContent
		{
			get
			{
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
				{
					sResidue = LexEntry.ExtractLIFTResidue(m_cache, m_hvo, LexSenseTags.kflidImportResidue,
						LexSenseTags.kflidLiftResidue);
					if (String.IsNullOrEmpty(sResidue))
						return null;
				}
				if (sResidue.IndexOf("<lift-residue") != sResidue.LastIndexOf("<lift-residue"))
					sResidue = RepairLiftResidue(sResidue);
				return LexEntry.ExtractLiftResidueContent(sResidue);
			}
		}

		private string RepairLiftResidue(string sResidue)
		{
			int idx = sResidue.IndexOf("</lift-residue>");
			if (idx > 0)
			{
				// Remove the repeated occurrences of <lift-residue>...</lift-residue>.
				// See LT-10302.
				sResidue = sResidue.Substring(0, idx + 15);
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() => { LiftResidue = sResidue; });
			}
			return sResidue;
		}

		/// <summary>
		/// Get the flid we use for this virtual property. Probably not used enough to cache.
		/// </summary>
		public int ReversalEntriesBulkTextFlid
		{
			get { return Cache.MetaDataCache.GetFieldId("LexSense", "ReversalEntriesBulkText", false); }
		}

			/// <summary>
		/// Virtual property allows ReversalEntries to be read through cache as a delimited string.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public VirtualStringAccessor ReversalEntriesBulkText
		{
			get
			{
				return new VirtualStringAccessor(this, ReversalEntriesBulkTextFlid, ReversalEntriesBulkTextForWs,
					SetReversalEntriesBulkTextForWs);
			}
		}

		private ITsString ReversalEntriesBulkTextForWs(int ws)
		{
			ITsStrBldr tsb = TsStrBldrClass.Create();
			ITsTextProps ttpWs;
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			ttpWs = propsBldr.GetTextProps();
			tsb.Replace(0, 0, "", ttpWs); // In case it ends up empty, make sure it's empty in the right Ws.
			foreach (ReversalIndexEntry revEntry in ReversalEntriesRC)
			{
				if (revEntry.WritingSystem == ws)
				{
					if (tsb.Length > 0)
						tsb.Replace(tsb.Length, tsb.Length, "; ", ttpWs);
					tsb.Replace(tsb.Length, tsb.Length, revEntry.LongName, ttpWs);
				}
			}
			return tsb.GetString();
		}

		private void SetReversalEntriesBulkTextForWs(int ws, ITsString tssVal)
		{
			ITsString tssOld = ReversalEntriesBulkTextForWs(ws);
			// The old and new values could be in another order, and this test won't catch that case.
			// That condition won't be fatal, however, so don't fret about it.
			if (tssOld.Equals(tssVal))
				return; // no change has occurred

			string val = tssVal.Text;
			if (val == null)
				val = ""; // This will effectively cause any extant entries for the given 'ws' to be removed in the end.

			StringCollection formsColl = new StringCollection();
			foreach (string form in val.Split(';'))
			{
				// These strings will be null, if there are two semi-colons together.
				// Or, it may be just whitespace, if it is '; ;'.
				if (form == null || form.Trim().Length == 0)
					continue;
				formsColl.Add(form.Trim());
			}
			var senseEntries = ReversalEntriesRC.ToArray();
			int originalSenseEntriesCount = senseEntries.Length;

			// We need the list of ReversalIndexEntries that this sense references, but which belong
			// to another reversal index. Those hvos, plus any entry hvos from the given 'ws' that are reused,
			// get put into 'survivingEntries'.
			var survivingEntries = new List<IReversalIndexEntry>(originalSenseEntriesCount + formsColl.Count);
			foreach (ReversalIndexEntry rie in senseEntries)
			{
				var owningIndex = rie.OwnerOfClass<ReversalIndex>();
				int wsIndex = 0;
				if (owningIndex != null)
					wsIndex = Services.WritingSystemManager.GetWsFromStr(owningIndex.WritingSystem);
				if (wsIndex == ws)
				{
					string form = rie.LongName;
					if (formsColl.Contains(form))
					{
						// Recycling an entry.
						survivingEntries.Add(rie);
						formsColl.Remove(form); // Don't need to mess with it later on.
					}
				}
				else
				{
					// These are all in some other ws, so they certainly must survive (cf. LT-3391).
					// Any entries that are reused will get added to this array later on.
					survivingEntries.Add(rie);
				}
			}

			// Start Undoable section of code.
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Strings.ksUndoMakeRevEntries, Strings.ksRedoMakeRevEntries,
				Cache.ActionHandlerAccessor,
				() =>
					{
						IReversalIndex revIndex = Services.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(ws);
						ISilDataAccess sda = m_cache.MainCacheAccessor;
						IActionHandler acth = sda.GetActionHandler();
						foreach (string currentForm in formsColl)
						{
							var idRevEntry = revIndex.FindOrCreateReversalEntry(currentForm);
							survivingEntries.Add(idRevEntry);
						}
						var goners = new HashSet<ICmObject>(ReversalEntriesRC.Cast<ICmObject>());
						var newbies = new HashSet<ICmObject>(survivingEntries.Cast<ICmObject>());
						goners.ExceptWith(newbies); // orginals, except the survivors
						newbies.ExceptWith(ReversalEntriesRC.Cast<ICmObject>());
							// survivors, except the ones already present
						ReversalEntriesRC.Replace(goners, newbies);
						// Delete any leaf RIEs that have no referring senses and no children; probably spurious ones made
						// by typing in the Reversals field.
						foreach (IReversalIndexEntry rie in goners)
						{
							if (rie.SubentriesOC.Count == 0 &&
								Services.GetInstance<ILexSenseRepository>().InstancesWithReversalEntry(rie).FirstOrDefault() == null)
							{
								if (rie.Owner is ReversalIndex)
									(rie.Owner as ReversalIndex).EntriesOC.Remove(rie);
								else
									(rie.Owner as ReversalIndexEntry).SubentriesOC.Remove(rie);
							}

						}
						// End undoable section of code.
					});
		}


		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateCreated
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateCreated"); }
		}

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateModified
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateModified"); }
		}

		/// <summary>
		/// Returns the TsString that represents the LongName of this object.
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				var tisb = HeadWord.GetIncBldr();
				tisb.Append(" (");
				tisb.AppendTsString(ShortNameTSS);
				tisb.Append(")");
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
									  (int)FwTextPropVar.ktpvEnum,
									  (int)FwTextToggleVal.kttvForceOn);
				tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
									  (int)FwTextPropVar.ktpvEnum,
									  (int)FwTextToggleVal.kttvOff);
				var wsAnal = Cache.DefaultAnalWs;
				var msa = MorphoSyntaxAnalysisRA;
				var tsf = Cache.TsStrFactory;
				if (msa != null)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptItalic,
										  (int)FwTextPropVar.ktpvEnum,
										  (int)FwTextToggleVal.kttvForceOn);
					tisb.AppendTsString(msa.ChooserNameTS);
					tisb.SetIntPropValues((int)FwTextPropType.ktptItalic,
										  (int)FwTextPropVar.ktpvEnum,
										  (int)FwTextToggleVal.kttvOff);
				}

				if (Gloss.AnalysisDefaultWritingSystem != null)
				{
					if (Gloss.AnalysisDefaultWritingSystem.Length > 0)
					{
						tisb.AppendTsString(tsf.MakeString((string.IsNullOrEmpty(tisb.Text) ? "" : " ") + Gloss.AnalysisDefaultWritingSystem.Text,
														   wsAnal));
					}
				}
				else if (Definition.AnalysisDefaultWritingSystem != null
						 && Definition.AnalysisDefaultWritingSystem.Length > 0)
				{
					if (!string.IsNullOrEmpty(tisb.Text))
						tisb.AppendTsString(tsf.MakeString(" ", wsAnal));
					tisb.AppendTsString(Definition.AnalysisDefaultWritingSystem);
				}
				if (string.IsNullOrEmpty(tisb.Text))
				{
					// This is not just to prevent a blank item in a combo, but an actual crash (FWR-3224):
					// If nothing has been put in the builder it currently has no WS, and that is not allowed.
					return tsf.MakeString(Strings.ksBlankSense, Cache.DefaultUserWs);
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				var meaning = Gloss.BestAnalysisAlternative;
				if (meaning == null || meaning.Length == 0 || meaning.Text == Strings.ksStars)
				{
					meaning = Definition.BestAnalysisAlternative;
				}
				return meaning;
			}
		}

		/// <summary>
		/// Alias OwnerOutlineName, adding a bit more information to make it obvious that we're
		/// referring to a sense even when there's only one sense, to allow using a common method
		/// for both Senses and Entries.
		/// </summary>
		public ITsString HeadWord
		{
			get
			{
				var tisb = OwnerOutlineName.GetIncBldr();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
				tisb.Append(" (");
				tisb.AppendTsString(ShortNameTSS);
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
				tisb.Append(")");
				return tisb.GetString();

			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString OwnerOutlineName
		{
			get
			{
				return OwnerOutlineNameForWs(Cache.DefaultVernWs);
			}
		}

		/// <summary>
		/// Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses.
		/// </summary>
		public ITsString OwnerOutlineNameForWs(int wsVern)
		{
			return OwnerOutlineNameForWs(wsVern, HomographConfiguration.HeadwordVariant.DictionaryCrossRef);
		}
		/// <summary>
		/// Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses.
		/// </summary>
		public ITsString OwnerOutlineNameForWs(int wsVern, HomographConfiguration.HeadwordVariant hv)
		{
			return OwnerOutlineNameForWs(wsVern, Entry.HomographNumber, hv);
		}
		/// <summary>
		/// Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses.
		/// Note: changes here probably require changes also in DictionaryPublicationDecorator.OwnerOutlineNameForWs
		/// </summary>
		public ITsString OwnerOutlineNameForWs(int wsVern, int hn,  HomographConfiguration.HeadwordVariant hv)
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			var lexEntry = Entry;
			tisb.AppendTsString(StringServices.HeadWordForWsAndHn(lexEntry, wsVern, hn, "", hv));
			var hc = Services.GetInstance<HomographConfiguration>();
			if (hc.ShowSenseNumber(hv) && lexEntry.HasMoreThanOneSense)
			{
				// These int props may not be needed, but they're safe.
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
									  Cache.DefaultAnalWs);
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
									  HomographConfiguration.ksSenseReferenceNumberStyle);
				tisb.Append(" ");
				tisb.Append(SenseNumber);
			}
			return tisb.GetString();
		}

		/// <summary>
		/// Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses and if configured to show sense number in reversals.
		/// </summary>
		public ITsString ReversalNameForWs(int wsVern)
		{
			return OwnerOutlineNameForWs(wsVern, HomographConfiguration.HeadwordVariant.ReversalCrossRef);
		}

		/// <summary>
		/// Virtual property allows HeadwordReversal to be read through cache.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public VirtualStringAccessor ReversalName
		{
			get
			{
				return new VirtualStringAccessor(this, Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseReversalName, ReversalNameForWs);
			}
		}

		/// <summary>
		/// Get the ID for the entry that owns this sense.
		/// </summary>
		public int EntryID
		{
			get { return Entry.Hvo; }
		}

		/// <summary>
		/// Returns the sense number as a string.
		/// one sense.
		/// </summary>
		public string SenseNumber
		{
			get
			{
				string number = "";

				if (Owner is ILexEntry)
				{
					var le = Owner as ILexEntry;
					int idx = le.SensesOS.IndexOf(this) + 1;
					number = idx.ToString();
				}
				else
				{
					var ls = Owner as LexSense;
					int idx = ls.SensesOS.IndexOf(this) + 1;
					number = ls.SenseNumber + "." + idx.ToString();
				}

				return number;
			}
		}

		/// <summary>
		/// Returns the one-based index of this sense in its owner's property, or 0 if it's
		/// the only one.  This is used in LIFT export.
		/// </summary>
		public int IndexNumber
		{
			get
			{
				int cSenses = m_cache.MainCacheAccessor.get_VecSize(this.Owner.Hvo, this.OwningFlid);
				if (cSenses == 1)
					return 0;
				int idx = m_cache.MainCacheAccessor.GetObjIndex(this.Owner.Hvo, this.OwningFlid, this.Hvo);
				return idx + 1;
			}
		}

		/// <summary>
		/// Get the minimal set of LexReferences for this entry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "MinimalLexReferences")]
		public List<ILexReference> MinimalLexReferences
		{
			get
			{
				var targets =
					Cache.ServiceLocator.GetInstance<ILexReferenceRepository>().AllInstances().Where(
						target => target.TargetsRS.Contains(this));
				return LexReference.ExtractMinimalLexReferences(targets);
			}
		}

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// PrimaryLexemes field and that is a complex entry type.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> ComplexFormEntries
		{
			get
			{
				return Services.GetInstance<ILexEntryRepository>().GetComplexFormEntries(this);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the subentries of this entry, that is, the entries which should be shown
		/// as subentries (paragraphs usually indented) under this sense's entry in root-based view.
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexSense in its
		/// PrimaryLexemes field and that is a complex entry type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> Subentries
		{
			get
			{
				return Services.GetInstance<ILexEntryRepository>().GetSubentries(this);
			}
		}		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// ShowComplexFormIn field and that is a complex entry type.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> VisibleComplexFormEntries
		{
			get
			{
				return Services.GetInstance<ILexEntryRepository>().GetVisibleComplexFormEntries(this);
			}
		}

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own LexEntryRef objects that refer to this LexSense as a
		/// variant (component).
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntry")]
		public IEnumerable<ILexEntry> VariantFormEntries
		{
			get
			{
				return Services.GetInstance<ILexEntryRepository>().GetVariantFormEntries(this);
			}
		}

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of object ids for
		/// all the LexReferences that contain this LexSense/LexEntry.
		/// Note this is called on SFM export by mdf.xml so needs to be a property.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexReference")]
		public IEnumerable<ILexReference> LexSenseReferences
		{
			get
			{
				return Services.GetInstance<ILexReferenceRepository>().GetReferencesWithTarget(this);
			}
		}

		partial void MorphoSyntaxAnalysisRASideEffects(IMoMorphSynAnalysis oldObjValue, IMoMorphSynAnalysis newObjValue)
		{
			// Do nothing, if they are the same.
			if (oldObjValue == newObjValue) return;

			HandleOldMSA(Cache, this, oldObjValue, newObjValue, false);
			// May affect the MorphoSyntaxAnalyses of any LexEntryRefs on the owning lex entry
			UpdateMorphoSyntaxAnalysesOfLexEntryRefs();
		}

		/// <summary>
		/// Handle side effects of setting MSA to new value:
		/// - Any WfiMorphBundle linked to this sense must be changed to point at the new MSA.
		/// - Delete original MSA, if nothing uses it. (If assumeSurvives is true, caller already
		/// knows that something still uses it.)
		/// </summary>
		public void HandleOldMSA(FdoCache cache, ILexSense sense, IMoMorphSynAnalysis oldMsa, IMoMorphSynAnalysis newMsa, bool assumeSurvives)
		{
			if (oldMsa == null || !oldMsa.IsValidObject)
				return; // May have been deleted already, e.g., when deleting the whole entry.
			// Update any WfiMorphBundle which has the old MSA value for this LexSense.
			// (See LT-3804. This also fixes LT-3937, at least to some degree.)
			var morphBundles = from mb in cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances()
							   where mb.SenseRA == sense
							   select mb;
			foreach (var mb in morphBundles)
				mb.MsaRA = newMsa;

			if (assumeSurvives || (!oldMsa.IsValidObject || !oldMsa.CanDelete))
				return;

			// Wipe out the old MSA.
			sense.Entry.MorphoSyntaxAnalysesOC.Remove(oldMsa);
		}

		private void UpdateMorphoSyntaxAnalysesOfLexEntryRefs()
		{
			var owningEntry = Owner as LexEntry;
			if (owningEntry == null)
				return; // MorphoSyntaxAnalyses is only computed from top-level senses.
			owningEntry.UpdateMorphoSyntaxAnalysesOfLexEntryRefs();
		}

		public void AdjustDerivedAnalysis()
		{
			// This can sometimes be called during the process of deleting an entry (with its
			// senses).  See FWR-3327.
			if (!this.IsValidObject)
				return;
			// find a wfigloss
			// that is the only gloss of a wfianalysis
			// that has just one WfiMorphBundle whose sense is the one of interest
			// that has no occurrences
			// and no positive human analysis

			var allGlosses = Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().AllInstances();
			var allSegs = Cache.ServiceLocator.GetInstance<ISegmentRepository>().AllInstances();

			var wg = (from gloss in allGlosses
					  let bundles = (gloss.Owner as IWfiAnalysis).MorphBundlesOS.Where(bundle => bundle.SenseRA != null)
					  where bundles.Count() == 1 && bundles.First() == this
						  && !allSegs.Any(seg => seg.AnalysesRS.Contains(gloss))
						  && !(gloss.Owner as IWfiAnalysis).EvaluationsRC.Any(eval=>eval.Approves && (eval.Owner as ICmAgent).Human)
					  select gloss).FirstOrDefault();

			if (wg != null)
				wg.Form.UserDefaultWritingSystem = this.Gloss.UserDefaultWritingSystem;
		}

		#region Implementation of IVariantComponentLexeme

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry as a variant (component).
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexEntryRef")]
		public IEnumerable<ILexEntryRef> VariantFormEntryBackRefs
		{
			get
			{
				return Services.GetInstance<ILexEntryRefRepository>().GetVariantEntryRefsWithMainEntryOrSense(this);
			}
		}

		/// <summary>
		/// creates a variant entry from this (main entry or sense) component,
		/// and links the variant to this (main entry or sense) component via
		/// EntryRefs.ComponentLexemes
		///
		/// NOTE: The caller will need to supply the lexemeForm subsequently.
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType)
		{
			return CreateVariantEntryAndBackRef(variantType, null);
		}

		/// <summary>
		/// creates a variant entry from this (main entry or sense) component,
		/// and links the variant to this (main entry or sense) component via
		/// EntryRefs.ComponentLexemes
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <param name="tssVariantLexemeForm">the lexeme form of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType, ITsString tssVariantLexemeForm)
		{
			var entry = Owner as LexEntry;
			return entry.CreateVariantEntryAndBackRef(this, variantType, tssVariantLexemeForm);
		}

		public ILexEntryRef FindMatchingVariantEntryBackRef(ILexEntryType variantEntryType, ITsString targetVariantLexemeForm)
		{
			return LexEntry.FindMatchingVariantEntryBackRef(this, variantEntryType, targetVariantLexemeForm);
		}

		#endregion

		/// <summary>
		/// Return anything from the ImportResidue which occurs prior to whatever LIFT may have
		/// added to it.  (LIFT import no longer adds to ImportResidue, but it did in the past.)
		/// </summary>
		public ITsString NonLIFTImportResidue
		{
			get
			{
				ITsString tss = m_cache.MainCacheAccessor.get_StringProp(m_hvo, LexSenseTags.kflidImportResidue);
				return LexEntry.ExtractNonLIFTResidue(tss);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string with the entry headword and a sense number if there is more than
		/// one sense.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ReferenceName
		{
			get
			{
				return OwnerOutlineName.Text;
			}
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case LexSenseTags.kflidSenseType:
					return m_cache.LangProject.LexDbOA.SenseTypesOA;
				case LexSenseTags.kflidUsageTypes:
					return m_cache.LangProject.LexDbOA.UsageTypesOA;
				case LexSenseTags.kflidDomainTypes:
					return m_cache.LangProject.LexDbOA.DomainTypesOA;
				case LexSenseTags.kflidStatus:
					return m_cache.LangProject.StatusOA;
				case LexSenseTags.kflidSemanticDomains:
					return m_cache.LangProject.SemanticDomainListOA;
				case LexSenseTags.kflidAnthroCodes:
					return m_cache.LangProject.AnthroListOA;
				case LexSenseTags.kflidMorphoSyntaxAnalysis:
					return OwningEntry;
				default:
					if (flid == Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "PublishIn", false))
						return Cache.LangProject.LexDbOA.PublicationTypesOA;

					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Get a set of CmObjects that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of objects.
		/// Alternatively, or as well, they should override ReferenceTargetOwner (the latter
		/// alone may be overridden if the candidates are the items in a possibility list,
		/// independent of the recipient object).
		/// </summary>
		/// <param name="flid">The reference property that can store the objects.</param>
		/// <returns>A set of objects</returns>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case LexSenseTags.kflidMorphoSyntaxAnalysis:
					return OwningEntry.MorphoSyntaxAnalysesOC.Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		internal void EntryHeadwordChanged(int ws)
		{
			MLOwnerOutlineNameChanged(ws);
		}

		private void MLOwnerOutlineNameChanged(int ws)
		{
			InternalServices.UnitOfWorkService.RegisterVirtualAsModified(this, "MLOwnerOutlineName", ws);
		}

		/// <summary>
		/// Ensure that merging senses inserts semicolons between the merged gloss and definition strings.
		/// </summary>
		/// <returns>true if merge handled, otherwise false</returns>
		protected override bool MergeStringProp(int flid, int cpt, ICmObject objSrc, bool fLoseNoStringData,
			object myCurrentValue, object srcCurrentValue)
		{
			if (flid == LexSenseTags.kflidGloss)
			{
				MultiUnicodeAccessor myMua = myCurrentValue as MultiUnicodeAccessor;
				if (myMua != null)
				{
					myMua.MergeAlternatives(srcCurrentValue as MultiUnicodeAccessor, fLoseNoStringData, "; ");
					return true;
				}
			}
			else if (flid == LexSenseTags.kflidDefinition)
			{
				MultiStringAccessor myMsa = myCurrentValue as MultiStringAccessor;
				if (myMsa != null)
				{
					myMsa.MergeAlternatives(srcCurrentValue as MultiStringAccessor, fLoseNoStringData, "; ");
					return true;
				}
			}
			return false;
			//return base.MergeStringProp(flid, cpt, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue);
		}

		/// <summary>
		/// Return the number of analyses in interlinear text for this sense.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int SenseAnalysesCount
		{
			get
			{
				int count = 0;
				var anals = new List<IWfiAnalysis>();
				foreach (ICmObject cmo in ReferringObjects)
					if (cmo is IWfiMorphBundle)
						count += (cmo.Owner as WfiAnalysis).OccurrencesInTexts.Count<ISegment>();
				return count;
			}
		}
	}

	/// <summary>
	/// Summary description for ReversalIndex.
	/// </summary>
	internal partial class ReversalIndex
	{
		/// <summary>
		/// Gets the set of entries owned by this reversal index from the set of entries in the input.
		/// The input will come from some source such as the referenced index entries of a sense.
		/// </summary>
		/// <param name="entries">An array which must contain IReversalIndexEntry objects</param>
		/// <returns>A List of IReversalIndexEntry instances that match any of the entries in the input array.</returns>
		public List<IReversalIndexEntry> EntriesForSense(List<IReversalIndexEntry> entries)
		{
			List<IReversalIndexEntry> matchingEntries = new List<IReversalIndexEntry>();
			foreach (IReversalIndexEntry rie in entries)
			{
				if (rie.ReversalIndex == this)
					matchingEntries.Add(rie);
			}
			return matchingEntries;
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case ReversalIndexTags.kflidEntries:
					List<ICmObject> currentVal = AllEntries.Cast<ICmObject>().ToList();
					Services.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(this, "AllEntries", currentVal);
						break;
			}
			base.AddObjectSideEffectsInternal(e);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case ReversalIndexTags.kflidEntries:
					List<ICmObject> currentVal = AllEntries.Cast<ICmObject>().ToList();
					Services.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(this, "AllEntries", currentVal);
					break;
			}
			base.RemoveObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// Get a list of all entries and subentries.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "AllEntries")]
		public List<IReversalIndexEntry> AllEntries
		{
			get
			{
				var allEntries = new List<IReversalIndexEntry>();

				foreach (var rei in EntriesOC)
				{
					allEntries.AddRange(rei.AllEntries);
				}

				return allEntries;
			}
		}

		/// <summary>
		/// Maps ReversalForm onto entry for the directly-owned RIEs.
		/// </summary>
		private Dictionary<string, IReversalIndexEntry> m_topEntries;

		/// <summary>
		/// Given a string in the form produced by the ReversalIndexEntry.LongName function (names of parents
		/// separated by commas), find or create the child specified. Caller should start UOW.
		/// </summary>
		public IReversalIndexEntry FindOrCreateReversalEntry(string longName)
		{
			var nameParts = longName.Trim().Split(':');
			var topName = nameParts[0];
			int ws = Services.WritingSystemManager.GetWsFromStr(WritingSystem);

			if (m_topEntries == null)
			{
				m_topEntries = new Dictionary<string, IReversalIndexEntry>(EntriesOC.Count);
				foreach (var entry in EntriesOC)
					m_topEntries[entry.ReversalForm.get_String(ws).Text ?? ""] = entry;
			}
			IReversalIndexEntry rie;
			// We test IsValidObject rather than trying to set up UndoActions to remove deleted RIEs from the dictionary.
			if (!m_topEntries.TryGetValue(topName, out rie) || !rie.IsValidObject)
			{
				rie = Services.GetInstance<IReversalIndexEntryFactory>().Create();
				EntriesOC.Add(rie);
				rie.ReversalForm.set_String(ws, Cache.TsStrFactory.MakeString(topName, ws));
			}
			for (int i = 1; i < nameParts.Length; i++)
			{
				var nextName = nameParts[i].Trim();
				if (nextName.Length == 0)
					continue;
				var nextRie =
					(from item in rie.SubentriesOC where item.ShortName == nextName select item).FirstOrDefault();
				if (nextRie == null)
				{
					nextRie = Services.GetInstance<IReversalIndexEntryFactory>().Create();
					rie.SubentriesOC.Add(nextRie);
					nextRie.ReversalForm.set_String(ws, Cache.TsStrFactory.MakeString(nextName, ws));
				}
				rie = nextRie;
			}
			return rie;
		}


		/// <summary>
		/// Return a deletion description string that might scare off anyone who doesn't
		/// know what he's doing.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var cEntries = 0;
				var senseIds = new List<int>();

				var senses = Cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances();
				foreach (var rei in AllEntries)
				{
					++cEntries;
					foreach (var ls in senses.Where(s => s.ReversalEntriesRC.Contains(rei)))
					{
						if (!senseIds.Contains(ls.Hvo))
							senseIds.Add(ls.Hvo);
					}
				}

				// "{0} has {1} entries referenced by {2} senses.";
				var sDeletionText = string.Format(Properties.Resources.kstidReversalIndexDeletionText,
					ShortName, cEntries, senseIds.Count);
				return m_cache.TsStrFactory.MakeString(sDeletionText, m_cache.WritingSystemFactory.UserWs);
			}
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				IWritingSystem ws = Services.WritingSystemManager.Get(WritingSystem);
				ITsString tss = Name.get_String(ws.Handle);
				if (tss == null || tss.Length == 0 || tss.Text == Strings.ksStars)
					tss = Name.AnalysisDefaultWritingSystem;
				if (tss == null || tss.Length == 0 || tss.Text == Strings.ksStars)
					tss = Name.BestAnalysisAlternative;
				// This solution must be at least accompanied by comments that let the
				// programmer know that 'getting' ShortName needs to be inside of a UOW!
				// But that seems extreme!
				//if (tss == null || tss.Length == 0 || tss.Text == Strings.ksStars)
				//{
				//    string text = ws.DisplayLabel;
				//    if (!string.IsNullOrEmpty(text))
				//    {
				//        Name.SetAnalysisDefaultWritingSystem(text);
				//        tss = Name.AnalysisDefaultWritingSystem;
				//    }
				//}
				if (tss.Text == Strings.ksStars)
					tss = null;

				return tss == null || tss.Length == 0 ? Strings.ksQuestions : tss.Text;
			}
		}

		internal void ChildReversalFormChanged(ReversalIndexEntry rie, ITsString originalValue)
		{
			if (m_topEntries == null)
				return;
			string oldKey = null;
			if (originalValue != null && !String.IsNullOrEmpty(originalValue.Text))
			{
				oldKey = originalValue.Text;
				m_topEntries.Remove(oldKey);
			}
			string newKey = rie.ShortName;
			m_topEntries[rie.ShortName ?? ""] = rie;
			var action = new UndoUpdateDictionaryAction<IReversalIndexEntry>(oldKey, newKey, rie, m_topEntries);
			Cache.ActionHandlerAccessor.AddAction(action);
		}
	}

	/// <summary>
	/// Summary description for ReversalIndexEntry.
	/// </summary>
	internal partial class ReversalIndexEntry
	{
		/// <summary>
		///
		/// </summary>
		public List<IReversalIndexEntry> AllOwningEntries
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Get a list of all entries and subentries.
		/// </summary>
		public List<IReversalIndexEntry> AllEntries
		{
			get
			{
				var allEntries = new List<IReversalIndexEntry>();
				allEntries.Add(this);

				foreach (var rei in SubentriesOC)
				{
					allEntries.AddRange(rei.AllEntries);
				}

				return allEntries;
			}
		}

		/// <summary>
		/// Answer true for root entries (not subentries).
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsRoot
		{
			get { return Owner is IReversalIndex; }
		}

		/// <summary>
		/// Get the preferred ICULocale string for the class.
		/// </summary>
		protected override string PreferredWsId
		{
			get { return Cache.WritingSystemFactory.GetStrFromWs(WritingSystem); }
		}

		/// <summary>
		/// The shortest, non-abbreviated label for the content of this object.
		/// this is the name that you would want to show up in a chooser list.
		/// </summary>
		public override string ShortName
		{
			get
			{
				var ws = WritingSystem;
				var sForm = ReversalForm.get_String(ws).Text;
				return string.IsNullOrEmpty(sForm) ? null : sForm;
			}
		}

		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
			switch (multiAltFlid)
			{
				case ReversalIndexEntryTags.kflidReversalForm:
					if (Owner is ReversalIndex)
						((ReversalIndex) Owner).ChildReversalFormChanged(this, originalValue);
					break;
			}
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				return Cache.TsStrFactory.MakeString(ShortName, WritingSystem);
			}
		}

		/// <summary>
		/// If this is top-level entry, the same as the ShortName.  If it's a subentry,
		/// then a colon-separated list of names from the root entry to this one.
		/// </summary>
		public string LongName
		{
			get
			{
				StringBuilder bldr = new StringBuilder(ShortName);
				for (IReversalIndexEntry rie = this.Owner as IReversalIndexEntry;
					 rie != null;
					 rie = rie.Owner as IReversalIndexEntry)
				{
					bldr.Insert(0, ": ");
					bldr.Insert(0, rie.ShortName);
				}
				return bldr.ToString();
			}
		}

		/// <summary>
		/// Return the writing system id of the ReversalIndex which owns this ReversalIndexEntry.
		/// </summary>
		public int WritingSystem
		{
			get
			{
				var ri = OwnerOfClass<IReversalIndex>();
				if (!string.IsNullOrEmpty(ri.WritingSystem))
					return Services.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
				return 0;
			}
		}

		/// <summary>
		/// Gets the entry that is owned by the index.
		/// </summary>
		/// <remarks>
		/// It may return itself, if it is owned by the index.  Otherwise, it will move up the
		/// ownership chain to find the one that is owned by the index.
		/// </remarks>
		public IReversalIndexEntry MainEntry
		{
			get
			{
				if (Owner is IReversalIndex)
					return this;
				else
					return OwningEntry.MainEntry;
			}
		}

		/// <summary>
		/// Gets the reversal index that ultimately owns this entry.
		/// </summary>
		public IReversalIndex ReversalIndex
		{
			get
			{
				if (Owner is IReversalIndex)
					return Owner as IReversalIndex;
				else
					return OwningEntry.ReversalIndex;
			}
		}

		/// <summary>
		/// Move 'this' to a safe place, if needed.
		/// </summary>
		/// <param name="rieSrc"></param>
		/// <remarks>
		/// When merging or moving a reversal entry, the new home ('this') may actually be owned by
		/// the other entry, in which case 'this' needs to be relocated, before the merge/move.
		/// </remarks>
		/// <returns>
		/// 1. The new owner (ReversalIndex or ReversalIndexEntry), or
		/// 2. null, if no move was needed.
		/// </returns>
		public ICmObject MoveIfNeeded(IReversalIndexEntry rieSrc)
		{
			Debug.Assert(rieSrc != null);
			ICmObject newOwner = null;
			IReversalIndexEntry rieOwner = this;
			while (true)
			{
				rieOwner = rieOwner.OwningEntry;
				if (rieOwner == null || rieOwner.Equals(rieSrc))
					break;
			}
			if (rieOwner != null && rieOwner.Equals(rieSrc))
			{
				// Have to move 'this' to a safe location.
				rieOwner = rieSrc.OwningEntry;
				if (rieOwner != null)
				{
					rieOwner.SubentriesOC.Add(this);
					newOwner = rieOwner;
				}
				else
				{
					// Move it clear up to the index.
					IReversalIndex ri = rieSrc.ReversalIndex;
					ri.EntriesOC.Add(this);
					newOwner = ri;
				}
			}
			// 'else' means there is no ownership issues to using normal merging/moving.

			return newOwner;
		}

		/// <summary>
		/// Gets the reversal index entry that owns this entry,
		/// or null, if it is owned by the index.
		/// </summary>
		public IReversalIndexEntry OwningEntry
		{
			get
			{
				if (Owner is IReversalIndexEntry)
					return Owner as IReversalIndexEntry;
				return null;
			}
		}

		/// <summary>
		/// Get the set of senses that refer to this reversal entry.
		/// This is a virtual, backreference property.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexSense")]
		public IEnumerable<ILexSense> ReferringSenses
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexSenseTags.kflidReversalEntries);
				// This set needs to be returned as a sorted list.  See FWR-3173.
				List<ILexSense> senses = new List<ILexSense>();
				foreach (var item in m_incomingRefs)
				{
					var collection = item as FdoReferenceCollection<IReversalIndexEntry>;
					if (collection == null)
						continue;
					if (collection.Flid == LexSenseTags.kflidReversalEntries)
						senses.Add(collection.MainObject as ILexSense);
				}
				senses.Sort(new CompareSensesForReversal(m_cache.LangProject.DefaultVernacularWritingSystem));
				return senses;
			}
		}

		/// <summary>
		/// Comparer class for sorting senses when displayed as ReferringSenses of a
		/// ReversalIndexEntry object.
		/// </summary>
		class CompareSensesForReversal : Comparer<ILexSense>
		{
			IWritingSystem m_wsVern;
			internal CompareSensesForReversal(IWritingSystem ws)
			{
				m_wsVern = ws;
			}

			public override int Compare(ILexSense x, ILexSense y)
			{
				ITsString tss1 = x.FullReferenceName;
				ITsString tss2 = y.FullReferenceName;
				return m_wsVern.Collator.Compare(tss1.Text, tss2.Text);
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class PhTerminalUnit
	{
		/// <summary>
		/// Get the preferred writing system identifier for the class.
		/// </summary>
		protected override string PreferredWsId
		{
			get { return Services.WritingSystems.DefaultVernacularWritingSystem.Id; }
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == FDO.PhTerminalUnitTags.kflidCodes);
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of a Text.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				if (Name != null)
				{
					return Name.BestVernacularAlternative;
				}
				else if (CodesOS.Count > 0)
				{
					return CodesOS[0].ShortNameTSS;
				}
				else
				{
					var ws = m_cache.DefaultUserWs;
					var name = Strings.ksQuestions;		// was "??", not "???"
					return m_cache.TsStrFactory.MakeString(name, ws);
				}
			}
		}

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			CodesOS.Add(new PhCode());
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class PhPhonData
	{
		/// <summary>
		/// Return the list of all phonemes, each in the default vernacular writing system.
		/// </summary>
		public List<string> AllPhonemes()
		{
			List<string> phReps = new List<string>();
			foreach (var pSet in this.PhonemeSetsOS)
			{
				foreach (var ph in pSet.PhonemesOC)
				{
					foreach (var code in ph.CodesOS)
					{
						var repStr = code.Representation.VernacularDefaultWritingSystem.Text;
						if (!string.IsNullOrEmpty(repStr))
							phReps.Add(code.Representation.VernacularDefaultWritingSystem.Text);
					}
				}
			}
			return phReps;
		}

		/// <summary>
		/// Return the list of abbreviations for all the natural classes defined, each in the
		/// default analysis writing system.
		/// </summary>
		public List<string> AllNaturalClassAbbrs()
		{
			List<string> clAbbrs = new List<string>();
			foreach (var nc in this.NaturalClassesOS)
			{
				var abbrStr = nc.Abbreviation.AnalysisDefaultWritingSystem.Text;
				if (!string.IsNullOrEmpty(abbrStr))
					clAbbrs.Add(nc.Abbreviation.AnalysisDefaultWritingSystem.Text);
			}
			return clAbbrs;
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class PhPhoneme
	{
		public event EventHandler BasicIPASymbolChanged;

		partial void BasicIPASymbolSideEffects(ITsString originalValue, ITsString newValue)
		{
			if (BasicIPASymbolChanged != null)
				BasicIPASymbolChanged(this, new EventArgs());
		}

		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmaion dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeletePhoneme, " "));
				tisb.AppendTsString(ShortNameTSS);

				var servLoc = Cache.ServiceLocator;
				var naturalClassCount = (servLoc.GetInstance<IPhNCSegmentsRepository>().AllInstances().Where(
					segment => segment.SegmentsRC.Contains(this))).Count();
				var rulesCount = (servLoc.GetInstance<IMoPhonolRuleAppRepository>().AllInstances().Where(
					segment => segment.RuleRA == this)).Count();

				var cnt = 1;
				var warningMsg = String.Format("{0}{0}{1}", StringUtils.kChHardLB, Strings.ksPhonemeUsedHere);
				var wantMainWarningLine = true;
				if (naturalClassCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (naturalClassCount > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesInNatClasses, cnt++, naturalClassCount));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceInNatClasses, cnt++));
					wantMainWarningLine = false;
				}

				if (rulesCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.WritingSystemFactory.UserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (rulesCount > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesInRules, cnt, rulesCount));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceInRules, cnt));
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case PhPhonemeTags.kflidFeatures:
					return m_cache.LangProject.PhFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Get a set of objects that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case PhPhonemeTags.kflidFeatures:
					return m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		protected override void OnBeforeObjectDeleted()
		{
			base.OnBeforeObjectDeleted();
			foreach (var segCtxt in Services.GetInstance<IPhSimpleContextSegRepository>().InstancesWithPhoneme(this))
			{
				segCtxt.PreRemovalSideEffects();
				m_cache.DomainDataByFlid.DeleteObj(segCtxt.Hvo);
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class PhCode
	{
		/// <summary>
		///
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.WritingSystemFactory.UserWs);
				tisb.Append(String.Format(Strings.ksDeletePhRepresentation, " "));
				tisb.AppendTsString(ShortNameTSS);

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get the preferred writing system identifier for the class.
		/// </summary>
		protected override string PreferredWsId
		{
			get { return Services.WritingSystems.DefaultVernacularWritingSystem.Id; }
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of a Text.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get { return Representation.BestVernacularAlternative; }
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class PhNCSegments
	{
		/// <summary>
		/// Tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == PhNCSegmentsTags.kflidSegments)
				   || base.IsFieldRequired(flid);
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case PhNCSegmentsTags.kflidSegments:
					return m_cache.LangProject.PhonologicalDataOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case PhNCSegmentsTags.kflidSegments:
					return (from ps in m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS
						   from ph in ps.PhonemesOC
						   select ph).Cast<ICmObject>();
			}
			return base.ReferenceTargetCandidates(flid);
		}
	}

	/// <summary>
	/// Add special behavior.
	/// </summary>
	internal partial class PhNaturalClass
	{
		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion
		/// confirmation dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteNaturalClass, " "));
				tisb.AppendTsString(ShortNameTSS);

				Set<int> rules = new Set<int>();
				foreach (var cmo in ReferringObjects)
				{
					IPhSimpleContextNC ctxt = cmo as IPhSimpleContextNC;
					if (ctxt != null)
					{
						// if there are two natural classes with the same abbreviation, things
						// can get in a state where there is no rule here.
						if (ctxt.Rule != null)
							rules.Add(ctxt.Rule.Hvo);
					}
				}
				int cnt = 1;
				if (rules.Count > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append("\x2028\x2028");
					tisb.Append(Strings.ksNatClassUsedHere);
					tisb.Append("\x2028");
					if (rules.Count > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesInRules, cnt, rules.Count));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceInRules, cnt));
				}
				string std = String.Format("[{0}]", Abbreviation.AnalysisDefaultWritingSystem.Text);
				string indexed = String.Format("[{0}^", Abbreviation.AnalysisDefaultWritingSystem.Text);
				int cUsed = 0;
				var repoEnv = m_cache.ServiceLocator.GetInstance<IPhEnvironmentRepository>();
				foreach (var env in repoEnv.AllInstances())
				{
					string text = env.StringRepresentation.Text ?? "";
					if (text.Contains(std) || text.Contains(indexed))
						++cUsed;
				}
				if (cUsed > 0)
				{
					tisb.Append("\x2028\x2028");
					string sMsg;
					if (cUsed > 1)
						sMsg = String.Format(Strings.ksInvalidateXEnvsIfDelNatClass, cUsed);
					else
						sMsg = String.Format(Strings.ksInvalidateOneEnvIfDelNatClass);
					tisb.Append(sMsg);
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of a natural class.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				if (Name != null)
				{
					ITsString name = Name.AnalysisDefaultWritingSystem;
					if (name != null && name.Length > 0)
						return name;
					name = Name.VernacularDefaultWritingSystem;
					if (name != null && name.Length > 0)
						return name;
					name = Name.BestAnalysisVernacularAlternative;
					if (name != null && name.Length > 0 && name.Text != Name.NotFoundTss.Text)
						return name;
				}
				if (Abbreviation != null)
				{
					ITsString abbrev = Abbreviation.AnalysisDefaultWritingSystem;
					if (abbrev != null && abbrev.Length > 0)
						return abbrev;
					abbrev = Abbreviation.VernacularDefaultWritingSystem;
					if (abbrev != null && abbrev.Length > 0)
						return abbrev;
					abbrev = Abbreviation.BestAnalysisVernacularAlternative;
					if (abbrev != null && abbrev.Length > 0 && abbrev.Text != Abbreviation.NotFoundTss.Text)
						return abbrev;
				}
				return Cache.TsStrFactory.MakeString(
					Strings.ksQuestions,
					m_cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == FDO.PhNaturalClassTags.kflidName);
		}

		protected override void OnBeforeObjectDeleted()
		{
			base.OnBeforeObjectDeleted();
			foreach (var ncCtxt in Services.GetInstance<IPhSimpleContextNCRepository>().InstancesWithNC(this))
			{
				ncCtxt.PreRemovalSideEffects();
				m_cache.DomainDataByFlid.DeleteObj(ncCtxt.Hvo);
			}
		}
	}

	internal partial class PhSegmentRule
	{
		/// <summary>
		/// Gets or sets the order number.
		/// </summary>
		/// <value>The order number.</value>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int OrderNumber
		{
			get
			{
				return IndexInOwner + 1;
			}

			set
			{
				int index = value - 1;
				if (index < 0 || index >= m_cache.DomainDataByFlid.get_VecSize(Owner.Hvo, OwningFlid))
					throw new ArgumentOutOfRangeException();

				if (IndexInOwner < index)
					index++;

				m_cache.DomainDataByFlid.MoveOwnSeq(Owner.Hvo, OwningFlid, IndexInOwner, IndexInOwner, Owner.Hvo,
					OwningFlid, index);
			}
		}
	}

	internal partial class PhRegularRule
	{
		/// <summary>
		/// Gets all of the feature constraints in this rule.
		/// </summary>
		/// <value>The feature constraints.</value>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "PhFeatureConstraint")]
		public IEnumerable<IPhFeatureConstraint> FeatureConstraints
		{
			get
			{
				return GetFeatureConstraintsExcept(null);
			}
		}

		/// <summary>
		/// Gets all of the feature constraints in this rule except those
		/// contained within the specified natural class context.
		/// </summary>
		/// <param name="excludeCtxt">The natural class context.</param>
		/// <returns>The feature constraints.</returns>
		public IEnumerable<IPhFeatureConstraint> GetFeatureConstraintsExcept(IPhSimpleContextNC excludeCtxt)
		{
			var featureConstrs = new List<IPhFeatureConstraint>();
			CollectVars(StrucDescOS, featureConstrs, excludeCtxt);
			foreach (var rhs in RightHandSidesOS)
			{
				CollectVars(rhs.StrucChangeOS, featureConstrs, excludeCtxt);
				CollectVars(rhs.LeftContextOA, featureConstrs, excludeCtxt);
				CollectVars(rhs.RightContextOA, featureConstrs, excludeCtxt);
			}
			return featureConstrs;
		}

		/// <summary>
		/// Collects all of the alpha variables in the specified sequence of simple contexts.
		/// </summary>
		/// <param name="seq">The sequence.</param>
		/// <param name="featureConstrs">The feature constraints.</param>
		/// <param name="excludeCtxt">The natural class context to exclude.</param>
		void CollectVars(IEnumerable<IPhSimpleContext> seq, List<IPhFeatureConstraint> featureConstrs, IPhSimpleContextNC excludeCtxt)
		{
			foreach (var ctxt in seq)
			{
				if ((excludeCtxt == null || ctxt != excludeCtxt)
					&& ctxt.ClassID == PhSimpleContextNCTags.kClassId)
				{
					var ncCtxt = ctxt as IPhSimpleContextNC;
					CollectVars(ncCtxt, featureConstrs, excludeCtxt);
				}
			}
		}

		/// <summary>
		/// Collects all of the alpha variables in the specified sequence of simple contexts.
		/// </summary>
		/// <param name="ctxt">The context.</param>
		/// <param name="featureConstrs">The feature indices.</param>
		/// <param name="excludeCtxt">The natural class context to exclude.</param>
		void CollectVars(IPhPhonContext ctxt, List<IPhFeatureConstraint> featureConstrs, IPhSimpleContextNC excludeCtxt)
		{
			if (ctxt == null || (excludeCtxt != null && ctxt == excludeCtxt))
				return;

			switch (ctxt.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					var seqCtxt = ctxt as IPhSequenceContext;
					foreach (var cur in seqCtxt.MembersRS)
						CollectVars(cur as IPhSimpleContextNC, featureConstrs, excludeCtxt);
					break;

				case PhIterationContextTags.kClassId:
					var iterCtxt = ctxt as IPhIterationContext;
					CollectVars(iterCtxt.MemberRA, featureConstrs, excludeCtxt);
					break;

				case PhSimpleContextNCTags.kClassId:
					var ncCtxt = ctxt as IPhSimpleContextNC;
					CollectVars(ncCtxt.PlusConstrRS, featureConstrs);
					CollectVars(ncCtxt.MinusConstrRS, featureConstrs);
					break;
			}
		}

		/// <summary>
		/// Collects all of the alpha variables in the specified sequence.
		/// </summary>
		/// <param name="vars">The sequence of variables.</param>
		/// <param name="featureConstrs">The feature constraints.</param>
		void CollectVars(IEnumerable<IPhFeatureConstraint> vars, List<IPhFeatureConstraint> featureConstrs)
		{
			foreach (var var in vars)
			{
				if (!featureConstrs.Contains(var))
					featureConstrs.Add(var);
			}
		}

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			RightHandSidesOS.Add(new PhSegRuleRHS());
		}

		protected override void OnBeforeObjectDeleted()
		{
			base.OnBeforeObjectDeleted();
			foreach (var constr in FeatureConstraints)
				m_cache.LanguageProject.PhonologicalDataOA.FeatConstraintsOS.Remove(constr);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);
			if (e.Flid == PhRegularRuleTags.kflidStrucDesc)
			{
				var removedCtxt = e.ObjectRemoved as IPhSimpleContextNC;
				if (removedCtxt != null)
				{
					var featConstrs = GetFeatureConstraintsExcept(removedCtxt);
					foreach (var constr in removedCtxt.PlusConstrRS)
					{
						if (!featConstrs.Contains(constr))
							m_cache.LanguageProject.PhonologicalDataOA.FeatConstraintsOS.Remove(constr);
					}
					foreach (var constr in removedCtxt.MinusConstrRS)
					{
						if (!featConstrs.Contains(constr))
							m_cache.LanguageProject.PhonologicalDataOA.FeatConstraintsOS.Remove(constr);
					}
				}
			}
		}
	}

	internal partial class PhSegRuleRHS
	{
		/// <summary>
		/// Retrieves the rule that owns this subrule.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "PhRegularRule")]
		public IPhRegularRule OwningRule
		{
			get { return OwnerOfClass<IPhRegularRule>(); }
		}
	}

	internal partial class PhMetathesisRule
	{
		/// <summary>
		/// Gets the structural change indices.
		/// </summary>
		/// <param name="isMiddleWithLeftSwitch">if set to <c>true</c> the context is associated with the left switch context,
		/// otherwise it is associated with the right context.</param>
		/// <returns>The structural change indices.</returns>
		public int[] GetStrucChangeIndices(out bool isMiddleWithLeftSwitch)
		{
			isMiddleWithLeftSwitch = false;
			string[] indices = StrucChange.Text.Split(' ');
			int index = indices[PhMetathesisRuleTags.kidxMiddle].IndexOf(':');
			if (index != -1)
			{
				isMiddleWithLeftSwitch = indices[PhMetathesisRuleTags.kidxMiddle].Substring(index + 1) == "L";
				indices[PhMetathesisRuleTags.kidxMiddle] = indices[PhMetathesisRuleTags.kidxMiddle].Substring(0, index);
			}
			return Array.ConvertAll<string, int>(indices, Int32.Parse);
		}

		/// <summary>
		/// Sets the structural change indices.
		/// </summary>
		/// <param name="indices">The structural change indices.</param>
		/// <param name="isMiddleWithLeftSwitch">if set to <c>true</c> the context is associated with the left switch context,
		/// otherwise it is associated with the right context.</param>
		public void SetStrucChangeIndices(int[] indices, bool isMiddleWithLeftSwitch)
		{
			string middleAssocStr = "";
			if (indices[PhMetathesisRuleTags.kidxMiddle] != -1)
				middleAssocStr = isMiddleWithLeftSwitch ? ":L" : ":R";

			var tisb = m_cache.TsStrFactory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
			tisb.Append(string.Format("{0} {1} {2}{3} {4} {5}", indices[0], indices[1], indices[2], middleAssocStr,
				indices[3], indices[4]));
			StrucChange = tisb.GetString();
		}

		/// <summary>
		/// Updates the <c>StrucChange</c> indices for removal and insertion. Should be called after insertion
		/// to StrucDesc and before removal from StrucDesc.
		/// </summary>
		/// <param name="strucChangeIndex">Index in structural change.</param>
		/// <param name="ctxtIndex">Index of the context.</param>
		/// <param name="insert">indicates whether the context will be inserted or removed.</param>
		/// <returns>The additional context to remove</returns>
		public IPhSimpleContext UpdateStrucChange(int strucChangeIndex, int ctxtIndex, bool insert)
		{
			int delta = insert ? 1 : -1;

			IPhSimpleContext removeCtxt = null;

			bool isMiddleWithLeftSwitch;
			int[] indices = GetStrucChangeIndices(out isMiddleWithLeftSwitch);
			switch (strucChangeIndex)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					indices[PhMetathesisRuleTags.kidxLeftEnv] += delta;
					if (indices[PhMetathesisRuleTags.kidxLeftSwitch] != -1)
						indices[PhMetathesisRuleTags.kidxLeftSwitch] += delta;
					if (indices[PhMetathesisRuleTags.kidxMiddle] != -1)
						indices[PhMetathesisRuleTags.kidxMiddle] += delta;
					if (indices[PhMetathesisRuleTags.kidxRightSwitch] != -1)
						indices[PhMetathesisRuleTags.kidxRightSwitch] += delta;
					if (indices[PhMetathesisRuleTags.kidxRightEnv] != -1)
						indices[PhMetathesisRuleTags.kidxRightEnv] += delta;
					break;

				case PhMetathesisRuleTags.kidxLeftSwitch:
					if (insert)
					{
						if (indices[PhMetathesisRuleTags.kidxLeftSwitch] == -1)
						{
							// adding new item to empty left switch cell
							indices[PhMetathesisRuleTags.kidxLeftSwitch] = ctxtIndex;
							if (indices[PhMetathesisRuleTags.kidxMiddle] != -1)
								indices[PhMetathesisRuleTags.kidxMiddle] += delta;
						}
						else
						{
							// already something in the cell, so must be adding a middle context
							indices[PhMetathesisRuleTags.kidxMiddle] = ctxtIndex;
							isMiddleWithLeftSwitch = true;
						}
					}
					else
					{
						// removing an item
						if (ctxtIndex == indices[PhMetathesisRuleTags.kidxLeftSwitch])
						{
							// removing the left switch context
							indices[PhMetathesisRuleTags.kidxLeftSwitch] = -1;
							if (indices[PhMetathesisRuleTags.kidxMiddle] != -1)
							{
								if (isMiddleWithLeftSwitch)
								{
									// remove the middle context if it is associated with this cell
									removeCtxt = StrucDescOS[indices[PhMetathesisRuleTags.kidxMiddle]];
									indices[PhMetathesisRuleTags.kidxMiddle] = -1;
									delta -= 1;
								}
								else
								{
									indices[PhMetathesisRuleTags.kidxMiddle] += delta;
								}
							}
						}
						else
						{
							// removing the middle context
							indices[PhMetathesisRuleTags.kidxMiddle] = -1;
						}
					}

					if (indices[PhMetathesisRuleTags.kidxRightSwitch] != -1)
						indices[PhMetathesisRuleTags.kidxRightSwitch] += delta;
					if (indices[PhMetathesisRuleTags.kidxRightEnv] != -1)
						indices[PhMetathesisRuleTags.kidxRightEnv] += delta;
					break;

				case PhMetathesisRuleTags.kidxRightSwitch:
					if (insert)
					{
						if (indices[PhMetathesisRuleTags.kidxRightSwitch] == -1)
						{
							// adding new item to empty right switch cell
							indices[PhMetathesisRuleTags.kidxRightSwitch] = ctxtIndex;
						}
						else
						{
							// already something in the cell, so must be adding a middle context
							indices[PhMetathesisRuleTags.kidxMiddle] = ctxtIndex;
							indices[PhMetathesisRuleTags.kidxRightSwitch] += delta;
							isMiddleWithLeftSwitch = false;
						}
					}
					else
					{
						// removing an item
						if (ctxtIndex == indices[PhMetathesisRuleTags.kidxRightSwitch])
						{
							// removing the right switch context
							indices[PhMetathesisRuleTags.kidxRightSwitch] = -1;
							if (indices[PhMetathesisRuleTags.kidxMiddle] != -1 && !isMiddleWithLeftSwitch)
							{
								// remove the middle context if it is associated with this cell
								removeCtxt = StrucDescOS[indices[PhMetathesisRuleTags.kidxMiddle]];
								indices[PhMetathesisRuleTags.kidxMiddle] = -1;
								delta -= 1;
							}
						}
						else
						{
							// removing the middle context
							indices[PhMetathesisRuleTags.kidxMiddle] = -1;
							indices[PhMetathesisRuleTags.kidxRightSwitch] += delta;
						}
					}

					if (indices[PhMetathesisRuleTags.kidxRightEnv] != -1)
						indices[PhMetathesisRuleTags.kidxRightEnv] += delta;
					break;

				case PhMetathesisRuleTags.kidxRightEnv:
					if (insert && indices[PhMetathesisRuleTags.kidxRightEnv] == -1)
						indices[PhMetathesisRuleTags.kidxRightEnv] = ctxtIndex;
					else if (!insert && (StrucDescOS.Count - indices[PhMetathesisRuleTags.kidxRightEnv]) == 1)
						indices[PhMetathesisRuleTags.kidxRightEnv] = -1;
					break;
			}
			SetStrucChangeIndices(indices, isMiddleWithLeftSwitch);
			return removeCtxt;
		}

		/// <summary>
		/// Gets or sets the index of the last context in the left environment.
		/// </summary>
		/// <value>The index of the left environment.</value>
		public int LeftEnvIndex
		{
			get
			{
				return GetIndex(PhMetathesisRuleTags.kidxLeftEnv);
			}

			set
			{
				SetIndex(PhMetathesisRuleTags.kidxLeftEnv, value);
			}
		}

		/// <summary>
		/// Gets or sets the index of the first context in the right environment.
		/// </summary>
		/// <value>The index of the right environment.</value>
		public int RightEnvIndex
		{
			get
			{
				return GetIndex(PhMetathesisRuleTags.kidxRightEnv);
			}

			set
			{
				SetIndex(PhMetathesisRuleTags.kidxRightEnv, value);
			}
		}

		/// <summary>
		/// Gets or sets the index of the left switch context.
		/// </summary>
		/// <value>The index of the left switch context.</value>
		public int LeftSwitchIndex
		{
			get
			{
				return GetIndex(PhMetathesisRuleTags.kidxLeftSwitch);
			}

			set
			{
				SetIndex(PhMetathesisRuleTags.kidxLeftSwitch, value);
			}
		}

		/// <summary>
		/// Gets or sets the index of the right switch context.
		/// </summary>
		/// <value>The index of the right switch context.</value>
		public int RightSwitchIndex
		{
			get
			{
				return GetIndex(PhMetathesisRuleTags.kidxRightSwitch);
			}

			set
			{
				SetIndex(PhMetathesisRuleTags.kidxRightSwitch, value);
			}
		}

		/// <summary>
		/// Gets or sets the index of the middle context.
		/// </summary>
		/// <value>The index of the middle context.</value>
		public int MiddleIndex
		{
			get
			{
				return GetIndex(PhMetathesisRuleTags.kidxMiddle);
			}

			set
			{
				SetIndex(PhMetathesisRuleTags.kidxMiddle, value);
			}

		}

		private int GetIndex(int index)
		{
			bool isMiddleWithLeftSwitch;
			int[] indices = GetStrucChangeIndices(out isMiddleWithLeftSwitch);
			return indices[index];
		}

		private void SetIndex(int index, int value)
		{
			bool isMiddleWithLeftSwitch;
			int[] indices = GetStrucChangeIndices(out isMiddleWithLeftSwitch);
			indices[index] = value;
			SetStrucChangeIndices(indices, isMiddleWithLeftSwitch);
		}

		/// <summary>
		/// Gets the limit of the middle context.
		/// </summary>
		/// <value>The limit of the middle context.</value>
		public int MiddleLimit
		{
			get
			{
				if (RightSwitchIndex != -1)
					return RightSwitchIndex;
				else if (RightEnvIndex != -1)
					return RightEnvIndex;
				else
					return RightEnvLimit;
			}
		}

		/// <summary>
		/// Gets the limit of the left environment.
		/// </summary>
		/// <value>The limit of the left environment.</value>
		public int LeftEnvLimit
		{
			get
			{
				return LeftEnvIndex + 1;
			}
		}

		/// <summary>
		/// Gets the limit of the right environment.
		/// </summary>
		/// <value>The limit of the right environment.</value>
		public int RightEnvLimit
		{
			get
			{
				return StrucDescOS.Count;
			}
		}

		/// <summary>
		/// Gets the limit of the left switch context.
		/// </summary>
		/// <value>The limit of the left switch context.</value>
		public int LeftSwitchLimit
		{
			get
			{
				if (MiddleIndex != -1)
					return MiddleIndex;
				else if (RightSwitchIndex != -1)
					return RightSwitchIndex;
				else if (RightEnvIndex != -1)
					return RightEnvIndex;
				else
					return RightEnvLimit;
			}
		}

		/// <summary>
		/// Gets the limit of the right switch context.
		/// </summary>
		/// <value>The limit of the right switch context.</value>
		public int RightSwitchLimit
		{
			get
			{
				if (RightEnvIndex != -1)
					return RightEnvIndex;
				else
					return RightEnvLimit;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the middle context is associated
		/// with the left switch context or right switch context.
		/// </summary>
		/// <value><c>true</c> if the context is associated with the left switch context,
		/// otherwise <c>false</c>.</value>
		public bool IsMiddleWithLeftSwitch
		{
			get
			{
				bool isMiddleWithLeftSwitch;
				GetStrucChangeIndices(out isMiddleWithLeftSwitch);
				return isMiddleWithLeftSwitch;
			}

			set
			{
				bool isMiddleWithLeftSwitch;
				int[] indices = GetStrucChangeIndices(out isMiddleWithLeftSwitch);
				SetStrucChangeIndices(indices, value);
			}
		}

		/// <summary>
		/// Gets the structural change index that the specified context is part of.
		/// </summary>
		/// <param name="ctxt">The context.</param>
		/// <returns>The structural change index.</returns>
		public int GetStrucChangeIndex(IPhSimpleContext ctxt)
		{
			var index = ctxt.IndexInOwner;

			if (index < LeftEnvLimit)
				return PhMetathesisRuleTags.kidxLeftEnv;
			else if (index >= LeftSwitchIndex && index < LeftSwitchLimit)
				return PhMetathesisRuleTags.kidxLeftSwitch;
			else if (index >= MiddleIndex && index < MiddleLimit)
				return IsMiddleWithLeftSwitch ? PhMetathesisRuleTags.kidxLeftSwitch : PhMetathesisRuleTags.kidxRightSwitch;
			else if (index >= RightSwitchIndex && index < RightSwitchLimit)
				return PhMetathesisRuleTags.kidxRightSwitch;
			else if (index >= RightEnvIndex && index < RightEnvLimit)
				return PhMetathesisRuleTags.kidxRightEnv;
			else
				return -1;
		}

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			SetStrucChangeIndices(new int[] { -1, -1, -1, -1, -1 }, true);
		}
	}

	internal partial class PhIterationContext
	{
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			Maximum = -1;
			Minimum = 0;
		}

		protected override void OnBeforeObjectDeleted()
		{
			base.OnBeforeObjectDeleted();
			if (MemberRA != null)
				m_cache.LanguageProject.PhonologicalDataOA.ContextsOS.Remove(MemberRA);
		}
	}

	internal partial class PhSequenceContext
	{
		protected override void OnBeforeObjectDeleted()
		{
			base.OnBeforeObjectDeleted();
			foreach (var ctxt in MembersRS.ToArray())
				m_cache.LanguageProject.PhonologicalDataOA.ContextsOS.Remove(ctxt);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);
			if (!Cache.ObjectsBeingDeleted.Contains(this) &&
				e.Flid == PhSequenceContextTags.kflidMembers &&
				MembersRS.Count == 0)
			{
				// Delete self because we just deleted our only reason for existence!
				if (Owner.ClassID == PhSegRuleRHSTags.kClassId)
				{
					var rhs = (IPhSegRuleRHS) Owner;
					if (rhs.LeftContextOA != null && rhs.LeftContextOA.Hvo == Hvo)
						rhs.LeftContextOA = null;
					else
						rhs.RightContextOA = null;
				}
			}
		}
	}

	internal partial class PhContextOrVar
	{
		/// <summary>
		/// Gets the rule that contains this context.
		/// </summary>
		/// <value>The rule.</value>
		public ICmObject Rule
		{
			get
			{
				var seqCtxtRep = Services.GetInstance<IPhSequenceContextRepository>();
				var iterCtxtRep = Services.GetInstance<IPhIterationContextRepository>();
				IPhContextOrVar cur = this;
				while (cur != null)
				{
					switch (cur.Owner.ClassID)
					{
						case PhPhonDataTags.kClassId:
							var curCtxt = cur as IPhPhonContext;
							cur = seqCtxtRep.InstancesWithMember(curCtxt).FirstOrDefault();
							if (cur == null)
								cur = iterCtxtRep.InstancesWithMember(curCtxt).FirstOrDefault();
							break;

						case PhSegRuleRHSTags.kClassId:
							return cur.Owner.Owner;

						default:
							return cur.Owner;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Handles any side-effects of removing a context that must be executed before the context is
		/// actually removed. It must be called manually before the context is removed.
		/// N.B. Care must be taken when overriding this method that containers of contexts aren't
		/// removed before the processing of the context itself has finished, since deleting the container
		/// will delete its contents automatically!
		/// </summary>
		public virtual void PreRemovalSideEffects()
		{
			// do nothing
		}
	}

	internal partial class PhPhonContext
	{
		public override void PreRemovalSideEffects()
		{
			if (Owner.ClassID == PhPhonDataTags.kClassId)
			{
				// FWR-2416 Don't delete an owning sequence here!
				// Instead tell the sequence to delete itself on removing its last member.
				// The below is possibly a bad idea for the same reason.
				foreach (var iterCtxt in Services.GetInstance<IPhIterationContextRepository>().InstancesWithMember(this))
					m_cache.DomainDataByFlid.DeleteObj(iterCtxt.Hvo);
			}
		}
	}

	internal partial class PhSimpleContext
	{
		public override void PreRemovalSideEffects()
		{
			base.PreRemovalSideEffects();
			if (Owner.ClassID == PhMetathesisRuleTags.kClassId)
			{
				var rule = Owner as IPhMetathesisRule;
				var ctxtToRemove = rule.UpdateStrucChange(rule.GetStrucChangeIndex(this), IndexInOwner, false);
				if (ctxtToRemove != null)
					rule.StrucDescOS.Remove(ctxtToRemove);
			}
		}
	}

	internal partial class LexReference
	{
		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			if (e.Flid == LexReferenceTags.kflidTargets)
			{
				// register the virtual prop LexEntryReference back ref as modified for the added
				// lex entry
				ILexEntry entry = e.ObjectAdded as ILexEntry;
				if (entry != null)
					UpdateLexEntryReferences(entry, true);
				UpdateMinimalLexReferences(null);
				if (e.ObjectAdded is LexSense)
				{
					List<ICmObject> backrefs = ((LexSense)e.ObjectAdded).LexSenseReferences.Cast<ICmObject>().ToList();
					Services.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(e.ObjectAdded, "LexSenseReferences",
						backrefs);
					entry = (e.ObjectAdded as LexSense).Entry;
				}
				if (entry != null)
					entry.DateModified = DateTime.Now;
			}

			base.AddObjectSideEffectsInternal(e);
		}

		private void UpdateMinimalLexReferences(ICmObject extraTarget)
		{
			var uowService = Services.GetInstance<IUnitOfWorkService>();
			var targets = TargetsRS.ToList();
			if (extraTarget != null)
				targets.Add(extraTarget);
			foreach (var target in targets)
			{
				if (target is LexEntry)
					uowService.RegisterVirtualAsModified(target, "MinimalLexReferences", ((LexEntry)target).MinimalLexReferences.Cast<ICmObject>());
				else if (target is LexSense)
					uowService.RegisterVirtualAsModified(target, "MinimalLexReferences", ((LexSense)target).MinimalLexReferences.Cast<ICmObject>());
			}
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			if (e.Flid == LexReferenceTags.kflidTargets)
			{
				// register the virtual prop LexEntryReference back ref as modified for the removed
				// lex entry
				ILexEntry entry = e.ObjectRemoved as ILexEntry;
				if (entry != null)
					UpdateLexEntryReferences(entry, false);
				UpdateMinimalLexReferences(e.ObjectRemoved);
				if (e.ObjectRemoved is LexSense)
				{
					List<ICmObject> backrefs = ((LexSense)e.ObjectRemoved).LexSenseReferences.Cast<ICmObject>().ToList();
					Services.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(e.ObjectRemoved, "LexSenseReferences",
						backrefs);
					entry = (e.ObjectRemoved as LexSense).Entry;
				}
				if (entry != null)
					entry.DateModified = DateTime.Now;
			}

			base.RemoveObjectSideEffectsInternal(e);
		}

		void UpdateLexEntryReferences(ILexEntry entry, bool fAdded)
		{
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(entry, "LexEntryReferences",
				Services.GetInstance<ILexReferenceRepository>().GetReferencesWithTarget(entry).Cast<ICmObject>());
		}

		/// <summary>
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		public string LiftResidueContent
		{
			get
			{
				var sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					return null;
				if (sResidue.IndexOf("<lift-residue") != sResidue.LastIndexOf("<lift-residue"))
					sResidue = RepairLiftResidue(sResidue);
				return LexEntry.ExtractLiftResidueContent(sResidue);
			}
		}

		private string RepairLiftResidue(string sResidue)
		{
			int idx = sResidue.IndexOf("</lift-residue>");
			if (idx > 0)
			{
				// Remove the repeated occurrences of <lift-residue>...</lift-residue>.
				// See LT-10302.
				sResidue = sResidue.Substring(0, idx + 15);
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() => { LiftResidue = sResidue; });
			}
			return sResidue;
		}

		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateCreated
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateCreated"); }
		}

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateModified
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateModified"); }
		}

		/// <summary>
		/// This is the string (the kind of lexical relation e.g. Antonym Relation)
		/// which is displayed in the Delete Lexical Relation dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var lrtOwner = Owner as ILexRefType;
				var analWs = m_cache.DefaultAnalWs;
				var userWs = m_cache.DefaultUserWs;
				var tisb = TsIncStrBldrClass.Create();

				//If this is a whole/parts kind of relation then show this to the user
				//because ShortName is always Parts and otherwise we would have to figure out if we
				//are deleting this slice from the Lexical entry with the Whole slice or the Parts slice.
				switch ((MappingTypes)lrtOwner.MappingType)
				{
					case MappingTypes.kmtSenseTree:
					case MappingTypes.kmtEntryTree:
					case MappingTypes.kmtEntryOrSenseTree:
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
						tisb.Append(lrtOwner.ShortName);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
						tisb.Append(" / ");
						//Really it would be good to have lrtOwner.ReverseNameTSS which works
						//like lrtOwner.ShortNameTSS.  That way the correct style will show up
						//for the particular ReverseName like it does for ShortNameTSS
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
						tisb.AppendTsString(lrtOwner.ReverseName.BestAnalysisAlternative);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
						tisb.Append(Strings.ksLexRelation);
						break;
					default:
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
						tisb.AppendTsString(lrtOwner.ShortNameTSS);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
						tisb.Append(Strings.ksLexRelation);
						break;
				}
				return tisb.GetString();
			}
		}
		/// <summary>
		/// This method is typically called with a list of LexSenseReferences from a LexEntry or LexSense.
		/// From the LexReferences that have that entry or sense as target, we want to prune those that target only
		/// the one object, unless the parent LexRefType is a sequence/scale.  This pruning
		/// is needed to obtain proper display of the Dictionary (publication) view.
		/// </summary>
		public static List<ILexReference> ExtractMinimalLexReferences(IEnumerable<ILexReference> sources)
		{
			var result = new List<ILexReference>();
			foreach (var source in sources)
			{
				var owningType = (ILexRefType) source.Owner;
				int mapType = owningType.MappingType;
				if (mapType == (int)MappingTypes.kmtSenseSequence ||
					mapType == (int)MappingTypes.kmtEntrySequence ||
					mapType == (int)MappingTypes.kmtEntryOrSenseSequence ||
					source.TargetsRS.Count > 1)
				{
					result.Add(source);
				}
			}
			return result;
		}

		/// <summary>
		/// Return the 1-based index of the member in the relation if relevant, otherwise 0.
		/// </summary>
		/// <param name="hvoMember"></param>
		/// <returns></returns>
		public int SequenceIndex(int hvoMember)
		{
			var lrtOwner = Owner as ILexRefType;
			switch ((MappingTypes)lrtOwner.MappingType)
			{
				case MappingTypes.kmtEntryOrSenseSequence:
				case MappingTypes.kmtEntrySequence:
				case MappingTypes.kmtSenseSequence:
					var i = 0;
					foreach (var target in TargetsRS)
					{
						if (target.Hvo == hvoMember)
							return i + 1;
						i++;
					}
					return 0;
				default:
					return 0;
			}
		}

		/// <summary>
		/// Return the desired abbreviation for the owning type.
		/// </summary>
		/// <param name="ws">writing system id</param>
		/// <param name="member">The reference member which needs the abbreviation</param>
		public string TypeAbbreviation(int ws, ICmObject member)
		{
			var lrtOwner = Owner as ILexRefType;
			var wsCode = SpecialWritingSystemCodes.DefaultAnalysis;
			if (ws < 0)
			{
				switch (ws)
				{

					case WritingSystemServices.kwsAnal:
						wsCode = SpecialWritingSystemCodes.DefaultAnalysis;
						break;
					case WritingSystemServices.kwsVern:
						wsCode = SpecialWritingSystemCodes.DefaultVernacular;
						break;
					default:
						wsCode = (SpecialWritingSystemCodes)ws;
						break;
				}
			}

			/*
				For all but 2, 6, and 8 the field label for all items would be Abbreviation.
				For 2, 6, and 8, the label for the first item would be Abbreviation,
				while the label for the other items would be ReverseAbbreviation.
			 */
			string x = null;
			switch ((MappingTypes)lrtOwner.MappingType)
			{
				case MappingTypes.kmtSenseAsymmetricPair:
				case MappingTypes.kmtSenseTree:
				case MappingTypes.kmtEntryAsymmetricPair:
				case MappingTypes.kmtEntryTree:
				case MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case MappingTypes.kmtEntryOrSenseTree:
					x = ws > 0
							? (TargetsRS[0] == member
								? lrtOwner.Abbreviation.get_String(ws).Text
								: lrtOwner.ReverseAbbreviation.get_String(ws).Text)
							: (TargetsRS[0] == member
								? lrtOwner.Abbreviation.get_String((int) wsCode).Text
								: lrtOwner.ReverseAbbreviation.get_String((int) wsCode).Text);
					break;
				default:
					x = ws > 0 ? lrtOwner.Abbreviation.get_String(ws).Text : lrtOwner.Abbreviation.get_String((int) wsCode).Text;
					break;
			}
			return x;
		}

		/// <summary>
		/// Replace one occurrences of the old object with the new one. If not found, append the new one.
		/// (Unless new is null...then just delete the old if present.)
		/// </summary>
		public void ReplaceTarget(ICmObject oldObj, ICmObject newObj)
		{
#if WANTPORT //(FLEx 2800, items in lexical relations should have modify times change when added or removed)
	// Update the timestamps of the affected objects (LT-5523).
			UpdateTargetTimestamps();
#endif
			for (var i = 0; i < TargetsRS.Count; i++)
			{
				if (TargetsRS[i] == oldObj)
				{
					TargetsRS.RemoveAt(i);
					if (newObj != null)
					{
#if WANTPORT //(FLEx 2800, items in lexical relations should have modify times change when added or removed)
	// Update the timestamps of the affected objects (LT-5523).
						ICmObject co = CmObject.CreateFromDBObject(m_cache, hvoNew);
						(co as CmObject).UpdateTimestampForVirtualChange();
#endif
						TargetsRS.Insert(i, newObj);
					}
					return;
				}
			}
			if (newObj != null)
			{
#if WANTPORT // (FLEx 2800, items in lexical relations should have modify times change when added or removed)
	// Update the timestamps of the affected objects (LT-5523).
				ICmObject co = CmObject.CreateFromDBObject(m_cache, hvoNew);
				(co as CmObject).UpdateTimestampForVirtualChange();
#endif
				TargetsRS.Add(newObj);
			}
		}

		/// <summary>
		/// This supports a virtual property for displaying lexical references in a browse column.
		/// See LT-4859 for justification.
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString FullDisplayText
		{
			get
			{
				/* This XML fragment was the prototype for this property:
					<obj field="OwnerHVO" layout="empty">
						<span>
							<properties>
								<bold value="on"/>
							</properties>
							<string field="Abbreviation" ws="best analysis"/>
							<if field="MappingType" intmemberof="2,3,7,8,12,13">
								<lit>-</lit>
								<string field="ReverseAbbreviation" ws="best analysis"/>
							</if>
							<lit>:  </lit>
						</span>
					</obj>
					<seq field="Targets" layout="empty" sep=", ">
						<if is="LexEntry">
							<string field="HeadWord"/>
						</if>
						<if is="LexSense">
							<string field="FullReferenceName"/>
						</if>
					</seq>
				 */
				LexRefType lrtOwner = this.Owner as LexRefType;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ITsStrBldr tsb = TsStrBldrClass.Create();
				tsb.ReplaceTsString(0, tsb.Length,
					lrtOwner.Abbreviation.BestAnalysisAlternative);
				switch (lrtOwner.MappingType)
				{
					case (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case (int)LexRefTypeTags.MappingTypes.kmtSenseTree:
					case (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case (int)LexRefTypeTags.MappingTypes.kmtEntryTree:
					case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
					case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
						tsb.ReplaceTsString(tsb.Length, tsb.Length,
							tsf.MakeString("-", Cache.DefaultAnalWs));
						tsb.ReplaceTsString(tsb.Length, tsb.Length,
							lrtOwner.ReverseAbbreviation.BestAnalysisAlternative);
						break;
				}
				tsb.ReplaceTsString(tsb.Length, tsb.Length,
					tsf.MakeString(":  ", Cache.DefaultAnalWs));
				tsb.SetIntPropValues(0, tsb.Length,
					(int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				ITsString tsSep = tsf.MakeString(", ", Cache.DefaultAnalWs);
				for (int i = 0; i < TargetsRS.Count; ++i)
				{
					if (i > 0)
						tsb.ReplaceTsString(tsb.Length, tsb.Length, tsSep);
					LexEntry le = TargetsRS[i] as LexEntry;
					if (le != null)
					{
						tsb.ReplaceTsString(tsb.Length, tsb.Length, le.HeadWord);
					}
					else
					{
						LexSense ls = TargetsRS[i] as LexSense;
						if (ls != null)
							tsb.ReplaceTsString(tsb.Length, tsb.Length, ls.FullReferenceName);
					}
				}
				return tsb.GetString();
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class LexRefType
	{
		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmation dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var analWs = m_cache.DefaultAnalWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
				tisb.AppendTsString(ShortNameTSS);

				int cnt = MembersOC.Count;
				if (cnt > 0)
				{
					var warningMsg = String.Format("\x2028\x2028{0}\x2028", Strings.ksLexRefUsedHere);
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.WritingSystemFactory.UserWs);
					tisb.Append(warningMsg);
					if (cnt > 1)
						tisb.Append(String.Format(Strings.ksContainsXLexRefs, cnt));
					else
						tisb.Append(String.Format(Strings.ksContainsOneLexRef));
				}

				return tisb.GetString();
			}
		}
	}

	/// <summary>
	/// Add special behavior.
	/// </summary>
	internal partial class PhEnvironment
	{
		/// <summary>
		/// Get the default infix environment (/#[C]_) from list of environments.
		/// </summary>
		/// <param name="cache">the cache to use</param>
		/// <param name="sDefaultEnv">string representation of the default environment</param>
		/// <returns>default environment, or null if none.</returns>
		internal static IPhEnvironment DefaultInfixEnvironment(FdoCache cache, string sDefaultEnv)
		{
			foreach (var env in cache.LangProject.PhonologicalDataOA.EnvironmentsOS)
			{
				var sEnv = env.StringRepresentation.Text.Trim();
				var sEnvNoWhitespace = StringUtils.StripWhitespace(sEnv);
				if (sEnvNoWhitespace == sDefaultEnv)
					return env;
			}
			return null;
		}

		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmaion dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeletePhEnvironment, " "));
				tisb.AppendTsString(ShortNameTSS);

				var servLoc = Cache.ServiceLocator;
				var userCount = (from allo in servLoc.GetInstance<IMoAffixAllomorphRepository>().AllInstances()
								 where allo.PhoneEnvRC.Contains(this)
								 select allo).Count();
				userCount += (from allo in servLoc.GetInstance<IMoStemAllomorphRepository>().AllInstances()
							  where allo.PhoneEnvRC.Contains(this)
							  select allo).Count();

				if (userCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptUnderline,
										  (int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntNone);
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					tisb.Append("\x2028\x2028");
					if (userCount > 1)
						tisb.Append(String.Format(Strings.ksEnvUsedXTimesByAllos, userCount, StringUtils.kChHardLB));
					else
						tisb.Append(String.Format(Strings.ksEnvUsedOnceByAllos, StringUtils.kChHardLB));
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == PhEnvironmentTags.kflidStringRepresentation); // N.B. is for Stage 1 only
		}

		/// <summary>
		/// The shortest, non-abbreviated label for the content of this object.
		/// This is the name that you would want to show up in a chooser list.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of a Text.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				if (StringRepresentation == null || StringRepresentation.Length == 0)
				{
					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, () =>
						{
							StringRepresentation = Cache.TsStrFactory.MakeString("/_", m_cache.DefaultUserWs);
						});
				}

				return StringRepresentation;
			}
		}

		/// <summary>
		/// Override the inherited method to check the StringRepresentation property.
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="createAnnotation">if set to <c>true</c>, an annotation will be created.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>
		/// true, if StringRepresentation is valid, otherwise false.
		/// </returns>
		public override bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			return CheckConstraints(flidToCheck, createAnnotation, out failure, /* do not adjust squiggly line */ false);
		}

		/// <summary>
		/// Check the validity of the environemtn string, create a problem report, and
		/// if asked, adjust the string itself to show the validity
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="createAnnotation">if set to <c>true</c>, an annotation will be created.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <param name="fAdjustSquiggly">whether or not to adjust the string squiggly line</param>
		/// <returns>
		/// true, if StringRepresentation is valid, otherwise false.
		/// </returns>
		public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure,
			bool fAdjustSquiggly)
		{
			ConstraintFailure failureT = null;
			var isValid = true;
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, () =>
				{
					if (flidToCheck == 0 || flidToCheck == PhEnvironmentTags.kflidStringRepresentation)
					{
						//CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_cache, Hvo);
						if (createAnnotation)
						{
							var agt = Cache.LanguageProject.ConstraintCheckerAgent;
							var anns = Cache.LanguageProject.AnnotationsOC;
							var errors = from error in Cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>().AllInstances()
										 where error.BeginObjectRA == this
										 select error;
							foreach (var errReport in errors)
							{
								if (errReport.SourceRA == agt)
									anns.Remove(errReport);
							}
						}
						PhonEnvRecognizer rec = CreatePhonEnvRecognizer(m_cache);
						var tss = StringRepresentation;
						var bldr = tss.GetBldr();
						var strRep = tss.Text;
						if (rec.Recognize(strRep))
						{
							if (fAdjustSquiggly)
							{
								// ClearSquigglyLine
								bldr.SetIntPropValues(0, tss.Length, (int)FwTextPropType.ktptUnderline,
													  (int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntNone);
							}
						}
						else
						{
							int pos;
							string sMessage;
							StringServices.CreateErrorMessageFromXml(strRep, rec.ErrorMessage, out pos, out sMessage);

							failureT = new ConstraintFailure(this, PhEnvironmentTags.kflidStringRepresentation,
															sMessage, createAnnotation);
							failureT.XmlDescription = rec.ErrorMessage;
							if (fAdjustSquiggly)
							{
								// MakeSquigglyLine

								var col = Color.Red;
								var len = tss.Length;
								bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderline,
													  (int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntSquiggle);
								bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderColor,
													  (int)FwTextPropVar.ktpvDefault, col.R + (col.B * 256 + col.G) * 256);
							}
							isValid = false;
						}
						if (fAdjustSquiggly)
							StringRepresentation = bldr.GetString();
					}
				});
			failure = failureT;
			return isValid;
		}

		internal static PhonEnvRecognizer CreatePhonEnvRecognizer(FdoCache cache)
		{
			var phReps = new List<string>();
			foreach (var pSet in cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS)
			{
				foreach (var ph in pSet.PhonemesOC)
				{
					foreach (var code in ph.CodesOS)
					{
						phReps.Add(code.Representation.VernacularDefaultWritingSystem.Text);
					}
				}
			}
			var clAbbrs = new List<string>();
			foreach (var nc in cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS)
			{
				var abbr = nc.Abbreviation.AnalysisDefaultWritingSystem.Text;
				if (abbr == null)
					abbr = nc.Abbreviation.BestAnalysisVernacularAlternative.Text;
				if (abbr == null)
					abbr = nc.Name.BestAnalysisVernacularAlternative.Text;
				clAbbrs.Add(abbr);
			}
			return new PhonEnvRecognizer(phReps.ToArray(), clAbbrs.ToArray());
		}
	}

	internal partial class LexEntryRef
	{
		protected override void ValidateAddObjectInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case LexEntryRefTags.kflidComponentLexemes:
					var entry = e.ObjectAdded as ILexEntry;
					if (entry == null)
						entry = ((ILexSense)e.ObjectAdded).Entry;
					if (entry.IsComponent((ILexEntry)Owner))
						throw new ArgumentException("components can't be circular");
					break;
			}
			base.ValidateAddObjectInternal(e);
		}

		/// <summary>
		/// For correct handling of some virtual properties, we must not allow the type of a LER
		/// to be changed once it has components.
		/// (If we need to change this, note that various virtual properties, such as VariantFormEntries,
		/// will need to update automatically when type changes and components is non-empty. See LT-12671.
		/// </summary>
		partial void ValidateRefType(ref int newValue)
		{
			if (newValue == RefType)
				return; // no change, no problem
			if (ComponentLexemesRS.Count != 0)
				throw new InvalidOperationException("Must not change EntryType after setting component lexemes");
		}

		/// <summary>
		/// The headword of the owning entry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString HeadWord
		{
			get { return ((ILexEntry) Owner).HeadWord; }
		}

		/// <summary>
		/// This is the same as PrimaryEntryRoots, except that if the only Component is (or is a sense of) the only PrimaryEntryRoot,
		/// it produces an empty list.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntry")]
		public IEnumerable<ILexEntry> NonTrivialEntryRoots
		{
			get
			{
				var result = PrimaryEntryRoots.ToList();
				if (result.Count != ComponentLexemesRS.Count)
					return result;
				for (int i = 0; i < result.Count; i++)
				{
					var item = result[i];
					var component = ComponentLexemesRS[i];
					if (!MatchingRoots(item, component))
						return result; // some difference, we don't want to suppress anything.
				}
				return new ILexEntry[0]; // all match, suppress them.
			}
		}

		bool MatchingRoots(ILexEntry item, ICmObject component)
		{
			if (item == component)
				return true;
			if (component is ILexSense && ((ILexSense)component).Entry == item)
				return true;
			return false;
		}

		/// <summary>
		/// If this entryref is a complex one, the entries under which its owner's full entry is actually published.
		/// Typically these are its PrimaryLexemes or their owing entries if they are senses.
		/// However, if any of those are themselves complex forms,
		/// show their PrimaryEntryRoots, so that we end up with the top-level form that indicates the
		/// actual place to look in the dictionary.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexEntry")]
		public IEnumerable<ILexEntry> PrimaryEntryRoots
		{
			get
			{
				if (RefType != LexEntryRefTags.krtComplexForm)
					yield break;
				foreach (var item in PrimaryLexemesRS)
				{
					var entry = item as ILexEntry;
					if (entry == null)
						entry = ((ILexSense) item).Entry;
					if (entry.ComplexFormEntryRefs.Count() == 0)
						yield return entry;
					else
					{
						foreach (var component in entry.ComplexFormEntryRefs.First().PrimaryEntryRoots)
							yield return component;
					}
				}
			}
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case LexEntryRefTags.kflidShowComplexFormsIn:
					// register the virtual prop ComplexFormEntries back ref as modified for the added
					// lex entry
					if (RefType == LexEntryRefTags.krtComplexForm)
					{
						UpdateVisibleComplexFormEntryBackRefs(e.ObjectAdded);
						UpdateVisibleComplexFormEntries(e.ObjectAdded);
					}
					break;
				case LexEntryRefTags.kflidPrimaryLexemes:
					// register the virtual prop ComplexFormEntries back ref as modified for the added
					// lex entry
					if (RefType == LexEntryRefTags.krtComplexForm)
					{
						UpdateSubentries(e.ObjectAdded);
						UpdateComplexFormEntryBackRefs(e.ObjectAdded, true);
						UpdateComplexFormsNotSubentries(e.ObjectAdded);
						if (!ShowComplexFormsInRS.Contains(e.ObjectAdded))
							ShowComplexFormsInRS.Add(e.ObjectAdded);
					}
					break;

				case LexEntryRefTags.kflidComponentLexemes:
					// register the virtual prop VariantFormEntryBackRefs back ref as modified for the added
					// lex entry or sense
					if (RefType == LexEntryRefTags.krtVariant)
					{
						UpdateVariantFormEntryBackRefs(e.ObjectAdded as IVariantComponentLexeme, true);
						UpdateVariantFormEntries(e.ObjectAdded);
					}
					// register the virtual prop ComplexFormEntries back ref as modified for the added
					// lex entry
					else if (RefType == LexEntryRefTags.krtComplexForm)
					{
						UpdateComplexFormEntries(e.ObjectAdded);
						UpdateComplexFormEntryBackRefs(e.ObjectAdded, true);
					}
					break;
			}

			base.AddObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// Replace original with replacement in your component and primary lexemes.
		/// </summary>
		/// <remarks>This is tricky because we want to preserve the position(s),
		/// but removing something from components also removes it from primary,
		/// and something can't be put in prinary that isn't in components.</remarks>
		internal void ReplaceComponent(ILexEntry original, ILexEntry replacement)
		{
			if (original == replacement)
				return; // paranoia, but the algorithm will actually not work right if they are the same.
			var componentIndexes = new List<int>();
			var primaryIndexes = new List<int>();
			for (int i = 0; i < ComponentLexemesRS.Count; i++)
			{
				if (ComponentLexemesRS[i] == original)
					componentIndexes.Add(i);
			}
			for (int i = 0; i < PrimaryLexemesRS.Count; i++)
			{
				if (PrimaryLexemesRS[i] == original)
					primaryIndexes.Add(i);
			}
			foreach (int index in componentIndexes)
				ComponentLexemesRS[index] = replacement;
			// The above will have removed them all from PrimaryLexemes, also.
			// Put the replacements back. The order is important here, for example,
			// in the unlikley case that original was at positions 0 and 2, until we insert
			// a replacement at 0, 2 will not be the right place to insert the other.
			foreach (int index in primaryIndexes)
				PrimaryLexemesRS.Insert(index, replacement);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of all the
		/// MoMorphoSyntaxAnalysis objects used by top-level senses owned by the owner of this
		/// LexEntryRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "MoMorphSynAnalysis")]
		public IEnumerable<IMoMorphSynAnalysis> MorphoSyntaxAnalyses
		{
			get
			{
				return (from sense in ((ILexEntry)Owner).SensesOS
						where sense.MorphoSyntaxAnalysisRA != null
						select sense.MorphoSyntaxAnalysisRA)
						.Distinct();
			}
		}

		private void UpdateComplexFormEntryBackRefs(ICmObject thingAddedOrRemoved, bool fAdded)
		{
			var entry = thingAddedOrRemoved as LexEntry;
			if (entry != null)
			{
				List<ICmObject> backrefs = entry.VisibleComplexFormBackRefs.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(entry,
					"VisibleComplexFormBackRefs", backrefs);
				return;
			}
			var sense = thingAddedOrRemoved as LexSense;
			if (sense != null)
			{
				List<ICmObject> backrefs = sense.VisibleComplexFormBackRefs.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(sense,
					"VisibleComplexFormBackRefs", backrefs);
			}
		}

		private void UpdateVisibleComplexFormEntryBackRefs(ICmObject thingAddedOrRemoved)
		{
			UpdateComplexFormsNotSubentries(thingAddedOrRemoved);
			var entry = thingAddedOrRemoved as LexEntry;
			if (entry != null)
			{
				List<ICmObject> backrefs = entry.VisibleComplexFormBackRefs.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(entry,
					"VisibleComplexFormBackRefs", backrefs);
				return;
			}
			var sense = thingAddedOrRemoved as LexSense;
			if (sense != null)
			{
				List<ICmObject> backrefs = sense.VisibleComplexFormBackRefs.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(sense,
					"VisibleComplexFormBackRefs", backrefs);
			}
		}

		private void UpdateComplexFormsNotSubentries(ICmObject thingAddedOrRemoved)
		{
			var entry = thingAddedOrRemoved as LexEntry;
			if (entry != null)
			{
				List<ICmObject> backrefs = entry.ComplexFormsNotSubentries.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(entry,
					"ComplexFormsNotSubentries", backrefs);
				return;
			}
			var sense = thingAddedOrRemoved as LexSense;
			if (sense != null)
			{
				List<ICmObject> backrefs = sense.ComplexFormsNotSubentries.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(sense,
					"ComplexFormsNotSubentries", backrefs);
			}

		}

		private void UpdateVisibleComplexFormEntries(ICmObject thingAddedOrRemoved)
		{
			var entry = thingAddedOrRemoved as LexEntry;
			if (entry != null)
				{
				List<ICmObject> backrefs = entry.VisibleComplexFormEntries.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(entry,
					"VisibleComplexFormEntries", backrefs);
				return;
				}
			var sense = thingAddedOrRemoved as LexSense;
			if (sense != null)
				{
				List<ICmObject> backrefs = sense.VisibleComplexFormEntries.Cast<ICmObject>().ToList();
				Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(sense,
					"VisibleComplexFormEntries", backrefs);
			}
		}

		/// <summary>
		/// Object owner. This virtual may seem redundant with CmObject.Owner, but it is important,
		/// because we can correctly indicate the destination class. This is used (at least) in
		/// PartGenerator.GeneratePartsFromLayouts to determine that it needs to generate parts for LexEntry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "LexEntry")]
		public ILexEntry OwningEntry
		{
			get { return OwnerOfClass<ILexEntry>(); }
		}

		/// <summary>
		/// Gets the object which, for the indicated property of the recipient, the user is
		/// most likely to want to edit if the ReferenceTargetCandidates do not include the
		/// target he wants.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case LexEntryRefTags.kflidComplexEntryTypes:
					return m_cache.LangProject.LexDbOA.ComplexEntryTypesOA;
				case LexEntryRefTags.kflidVariantEntryTypes:
					return m_cache.LangProject.LexDbOA.VariantEntryTypesOA;
			}
			return null;
		}

		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				// MinorEntries, Subentries, Variants
				case LexEntryRefTags.kflidComponentLexemes:
				case LexEntryRefTags.kflidPrimaryLexemes:
					// TODO: This needs fixing to include senses, but we probably don't want to just have
					// a flat list. We probably want a special chooser that allows selecting the entry,
					// then one of the senses from the entry.
					return m_cache.LangProject.LexDbOA.Entries.Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case LexEntryRefTags.kflidPrimaryLexemes:
					// register the virtual prop ComplexFormEntries back ref as modified for the removed
					// lex entry or sense
					if (RefType == LexEntryRefTags.krtComplexForm)
					{
						UpdateSubentries(e.ObjectRemoved);
						UpdateComplexFormEntryBackRefs(e.ObjectRemoved, true);
						UpdateComplexFormsNotSubentries(e.ObjectRemoved);
					}
					break;
				case LexEntryRefTags.kflidShowComplexFormsIn:
					UpdateVisibleComplexFormEntryBackRefs(e.ObjectRemoved);
					UpdateVisibleComplexFormEntries(e.ObjectRemoved);
					break;

				case LexEntryRefTags.kflidComplexEntryTypes:
					if (ComplexEntryTypesRS.Count == 0) // no longer a complex entry? But we use RefType for that...
					{
						foreach (ICmObject obj in PrimaryLexemesRS)
							UpdateComplexFormEntries(obj);
					}
					break;

				case LexEntryRefTags.kflidComponentLexemes:
					// register the virtual prop VariantFormEntryBackRefs back ref as modified for the removed
					// lex entry or sense
					if (RefType == LexEntryRefTags.krtVariant)
					{
						UpdateVariantFormEntryBackRefs(e.ObjectRemoved as IVariantComponentLexeme, false);
						UpdateVariantFormEntries(e.ObjectRemoved);
					}
					else
					{
						UpdateComplexFormEntries(e.ObjectRemoved);
					}
					// Update PrimaryLexemes to remove the ComponentLexeme from "Show Subentry under" too
					RemoveComponentFromVisibilityLists(e.ObjectRemoved);
					break;
			}

			base.RemoveObjectSideEffectsInternal(e);
		}

		private void RemoveComponentFromVisibilityLists(ICmObject objRemoved)
		{
			// The same object being removed from ComponentLexemes needs to also be removed
			// from these lists, which are supposed to be subsets.
			// Not, however, if it is still in ComponentLexemes, as may for example happen when they are being re-ordered.
			if (ComponentLexemesRS.Contains(objRemoved))
				return;
				PrimaryLexemesRS.Remove(objRemoved);
			ShowComplexFormsInRS.Remove(objRemoved);
		}

		void UpdateComplexFormEntries(ICmObject mainEntryOrSense)
		{
			var flid = m_cache.MetaDataCache.GetFieldId2(mainEntryOrSense.ClassID, "ComplexFormEntries", false);

			var guids = (from entry in Services.GetInstance<ILexEntryRepository>().GetComplexFormEntries(mainEntryOrSense)
						 select entry.Guid).ToArray();

			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(mainEntryOrSense, flid,
				new Guid[0], guids);
		}

		/// <summary>
		/// When PrimaryLexemes changes, update Subentries (and AllSubentries) appropriately.
		/// </summary>
		/// <param name="mainEntryOrSense"></param>
		void UpdateSubentries(ICmObject mainEntryOrSense)
		{
			var flid = m_cache.MetaDataCache.GetFieldId2(mainEntryOrSense.ClassID, "Subentries", false);

			var guids = (from entry in Services.GetInstance<ILexEntryRepository>().GetSubentries(mainEntryOrSense)
						 select entry.Guid).ToArray();

			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(mainEntryOrSense, flid,
				new Guid[0], guids);

			var le = mainEntryOrSense as LexEntry;
			if (le == null)
				le = (LexEntry)((ILexSense) mainEntryOrSense).Entry;

			flid = m_cache.MetaDataCache.GetFieldId2(LexEntryTags.kClassId, "AllSubentries", false);

			guids = (from entry in le.AllSubentries select entry.Guid).ToArray();

			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(le, flid,
				new Guid[0], guids);
		}

		void UpdateVariantFormEntries(ICmObject mainEntryOrSense)
		{
			var flid = m_cache.MetaDataCache.GetFieldId2(mainEntryOrSense.ClassID, "VariantFormEntries", false);

			var guids = (from entry in Services.GetInstance<ILexEntryRepository>().GetVariantFormEntries(mainEntryOrSense)
						 select entry.Guid).ToArray();

			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(mainEntryOrSense, flid,
				new Guid[0], guids);
		}

		void UpdateVariantFormEntryBackRefs(IVariantComponentLexeme componentLexeme, bool fAdded)
		{
			var flid = m_cache.MetaDataCache.GetFieldId2(componentLexeme.ClassID, "VariantFormEntryBackRefs", false);

			List<ILexEntryRef> backrefs = componentLexeme.VariantFormEntryBackRefs.ToList();
			if (backrefs.Contains(this))
			{
				if (!fAdded)
					backrefs.Remove(this);
			}
			else if (fAdded)
			{
				backrefs.Add(this);
			}
			var guids = backrefs.Select(entryRef => entryRef.Guid).ToArray();

			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(componentLexeme, flid,
				new Guid[0], guids);
		}

		partial void HideMinorEntrySideEffects(int originalValue, int newValue)
		{
			var entry = Owner as LexEntry;
			if (entry != null)
			{
				var flid = m_cache.MetaDataCache.GetFieldId2(LexEntryTags.kClassId, "PublishAsMinorEntry", false);

				var origVal = false;
				foreach (var ler in entry.EntryRefsOS)
				{
					if (ler == this)
					{
						if (originalValue == 0)
						{
							origVal = true;
							break;
						}
					}
					else if (ler.HideMinorEntry == 0)
					{
						origVal = true;
						break;
					}
				}

				var newVal = entry.PublishAsMinorEntry;
				if (origVal != newVal)
				{
					((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(entry,
						flid, origVal, newVal);
				}

				flid = m_cache.MetaDataCache.GetFieldId2(LexEntryTags.kClassId, "VisibleVariantEntryRefs", false);
				var guids = entry.VisibleVariantEntryRefs.Select(entryRef => entryRef.Guid).ToArray();
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(entry, flid,
					new Guid[0], guids);
			}
		}

		/// <summary>
		/// Gets the sort key for sorting a list of ShortNames.
		/// </summary>
		/// <value></value>
		public override string SortKey
		{
			get
			{
				return Owner.SortKey;
			}
		}

		/// <summary>
		/// Gets a secondary sort key for sorting a list of ShortNames.  Defaults to zero.
		/// </summary>
		/// <value></value>
		public override int SortKey2
		{
			get
			{
				return Owner.SortKey2;
			}
		}

		/// <summary>
		/// Gets the writing system for sorting a list of ShortNames.
		/// </summary>
		/// <value></value>
		public override string SortKeyWs
		{
			get
			{
				return Owner.SortKeyWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the example
		/// sentences owned by top-level senses owned by the owner of this LexEntryRef.
		/// Enhance JohnT: implement automatic update when senses or examples change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "LexExampleSentence")]
		public IEnumerable<ILexExampleSentence> ExampleSentences
		{
			get
			{
				return from sense in ((ILexEntry) Owner).SensesOS from example in sense.ExamplesOS select example;
			}
		}
	}

	/// <summary>
	/// Additional methods needed to support the LexEntryRef class.
	/// </summary>
	internal partial class LexEntryType : IComparable
	{
		#region IComparable Members

		/// <summary>
		/// Allow LexEntryType objects to be compared/sorted.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			ILexEntryType that = obj as ILexEntryType;
			if (that == null)
				return 1;
			string s1 = this.SortKey;
			string s2 = that.SortKey;
			if (s1 == null)
				return (s2 == null) ? 0 : 1;
			else if (s2 == null)
				return -1;
			int x = s1.CompareTo(s2);
			if (x == 0)
				return this.SortKey2 - that.SortKey2;
			else
				return x;
		}

		#endregion
	}

	internal partial class LexEtymology
	{
		/// <summary>
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		public string LiftResidueContent
		{
			get
			{
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					return null;
				if (sResidue.IndexOf("<lift-residue") != sResidue.LastIndexOf("<lift-residue"))
					sResidue = RepairLiftResidue(sResidue);
				return LexEntry.ExtractLiftResidueContent(sResidue);
			}
		}

		private string RepairLiftResidue(string sResidue)
		{
			int idx = sResidue.IndexOf("</lift-residue>");
			if (idx > 0)
			{
				// Remove the repeated occurrences of <lift-residue>...</lift-residue>.
				// See LT-10302.
				sResidue = sResidue.Substring(0, idx + 15);
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() => { LiftResidue = sResidue; });
			}
			return sResidue;
		}

		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateCreated
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateCreated"); }
		}

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateModified
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "dateModified"); }
		}

		/// <summary>
		/// Provide something for the LIFT type attribute.
		/// </summary>
		public string LiftType
		{
			get
			{
				IWritingSystem ws = LiftFormWritingSystem;
				if (ws != null && Services.WritingSystems.VernacularWritingSystems.Contains(ws))
					return "proto";
				else
					return "borrowed";
			}
		}

		/// <summary>
		/// Provide something for the LIFT source attribute.
		/// </summary>
		public string LiftSource
		{
			get
			{
				string sSource = this.Source;
				if (String.IsNullOrEmpty(sSource))
				{
					IWritingSystem ws = LiftFormWritingSystem;
					if (ws != null)
					{
						sSource = ws.DisplayLabel;
					}
				}
				if (string.IsNullOrEmpty(sSource))
					return "UNKNOWN";
				return sSource;
			}
		}

		private IWritingSystem LiftFormWritingSystem
		{
			get
			{
				int wsActual = 0;
				ITsString tss = this.Form.GetAlternativeOrBestTss(m_cache.DefaultVernWs, out wsActual);
				if (tss == this.Form.NotFoundTss || wsActual == 0)
				{
					tss = this.Form.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
					if (tss == this.Form.NotFoundTss || wsActual == 0)
					{
						if (this.Form.StringCount > 0)
							tss = this.Form.GetStringFromIndex(0, out wsActual);
						else
							wsActual = 0;
						if (wsActual == 0)
						{
							return null;
						}
					}
				}
				return Services.WritingSystemManager.Get(wsActual);
			}
		}

		/// <summary>
		/// Override of ShortName
		/// </summary>
		public override string ShortName
		{
			get
			{
				if (!string.IsNullOrEmpty(Source))
				{
					return String.Format("{0} ({1})",
						this.Form.BestVernacularAnalysisAlternative.Text, this.Source);
				}
				else
				{
					return this.Form.BestVernacularAnalysisAlternative.Text;
				}
			}
		}

		/// <summary>
		/// Override of ShortNameTSS
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				if (!string.IsNullOrEmpty(Source))
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					tisb.AppendTsString(this.Form.BestVernacularAnalysisAlternative);
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
					tisb.Append(" (");
					tisb.Append(this.Source);
					tisb.Append(")");
					return tisb.GetString();
				}
				else
				{
					return this.Form.BestVernacularAnalysisAlternative;
				}
			}
		}
	}
}
