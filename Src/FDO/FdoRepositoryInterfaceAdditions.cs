// --------------------------------------------------------------------------------------------
// Copyright (C) 2009 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: FdoRepositoryInterfaceAdditions.cs
// Responsibility: Randy Regnier
//
// <remarks>
// Add additional methods/properties to Repository interfaces in this file.
// Implementation of the additional interface information should go into the FdoRepositoryAdditions.cs file.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch;

namespace SIL.FieldWorks.FDO
{
	public partial interface ICmObjectRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the class ID of the object with the specified HVO
		/// </summary>
		/// <param name="hvo">The HVO</param>
		/// <returns>The class ID of the object with the specified HVO</returns>
		/// <exception cref="KeyNotFoundException">If no object has the specified HVO</exception>
		/// ------------------------------------------------------------------------------------
		int GetClsid(int hvo);

		/// <summary>
		/// Returns true if this is an ID that can be looked up using GetObject().
		/// </summary>
		bool IsValidObjectId(int hvo);

		/// <summary>
		/// Returns true if this is an ID that can be looked up using GetObject().
		/// </summary>
		bool IsValidObjectId(Guid guid);

		/// <summary>
		/// This method should be used when it is desirable to have HVOs associated with
		/// Guids for objects which may not yet have been fluffed up. The ObjectOrId may be
		/// passed to GetHvoFromObjectOrId to get an HVO; anything that actually uses
		/// the HVO will result in the object being fluffed, but that can be delayed (e.g.,
		/// when persisting a pre-sorted list of guids).
		/// It will return null if the guid does not correspond to a real object (that is,
		/// for success the identity map must contain either a CmObject or a surrogate, not
		/// just a CmObjectId.)
		/// </summary>
		ICmObjectOrId GetObjectOrIdWithHvoFromGuid(Guid guid);

		/// <summary>
		/// Get the HVO associatd with the given ID or object. May actually create the
		/// association, though it is more normal for it to be created in a call to
		/// GetObjectOrIdWithHvoFromGuid, or simply when the CmObject is created.
		/// </summary>
		int GetHvoFromObjectOrId(ICmObjectOrId id);

		/// <summary>
		/// Answer true if the object in question was created this session, that is, since
		/// the program started up (or, more precisely, since this repository was instantiated).
		/// </summary>
		bool WasCreatedThisSession(ICmObject obj);

		/// <summary>
		/// Answer true if instances of the specified class have been created this session, that is, since
		/// the program started up (or, more precisely, since this repository was instantiated).
		/// Note: may answer true, even if all such creations have been Undone.
		/// </summary>
		bool InstancesCreatedThisSession(int classId);

		/// <summary>
		/// Record that the indicated object is in focus. This will postpone automatic deletion of the
		/// object until it is no longer focused. Multiple calls to this must be balanced by multiple
		/// calls to RemoveFocusedObject. Deletion happens only when it is in focus nowhere.
		/// Currently this is used only to postpone deletion of wordforms as a side effect of editing
		/// the baseline.
		/// </summary>
		void AddFocusedObject(ICmObject obj);

		/// <summary>
		/// Record that the indicated object is no longer in focus. This will postpone automatic deletion of the
		/// object until it is no longer focused. Multiple calls to this must be balanced by multiple
		/// calls to RemoveFocusedObject. Deletion happens only when it is in focus nowhere.
		/// Currently this is used only to postpone deletion of wordforms as a side effect of editing
		/// the baseline.
		/// </summary>
		void RemoveFocusedObject(ICmObject obj);
	}

	/// <summary>
	///
	/// </summary>
	public interface IAnalysisRepository : IRepository<IAnalysis>
	{
	}

	/// <summary>
	/// Additions to the Semantic Domain Repository to handle searches
	/// </summary>
	public partial interface ICmSemanticDomainRepository
	{
		/// <summary>
		/// Finds all the semantic domains that contain 'searchString' in their text fields.
		/// Semantic Domains typically have:
		///   Abbreviation (a hierarchical number, e.g. "8.3.3")
		///   Name (e.g. "Light")
		///   Description (e.g. "Use this domain for words related to light.")
		///   OCM codes and Louw and Nida codes
		///   Questions (e.g. "(1) What words refer to light?")
		///   Example Words (e.g. "light, sunshine, gleam (n), glare (n), glow (n), radiance,")
		/// Search strings beginning with numbers will search Abbreviation only and only match at the beginning.
		///   (so searching for "3.3" won't return "8.3.3")
		/// Search strings beginning with alphabetic chars will search Name and Example Words.
		/// For alphabetic searches, hits will be returned in the following order:
		///   1) Name begins with search string
		///   2) Name or Example Words contain words (bounded by whitespace) that match the search string
		///   3) Name or Example Words contain words that begin with the search string
		///
		/// N.B.: This method looks for matches in the BestAnalysisAlternative.
		/// This ought to match what is displayed in the UI, so if the UI doesn't use
		/// BestAnalysisAlternative one of them needs to be changed.
		/// </summary>
		/// <param name="searchString"></param>
		/// <returns></returns>
		IEnumerable<ICmSemanticDomain> FindDomainsThatMatch(string searchString);

		/// <summary>
		/// Takes the gloss, a short definition (if only one or two words), and reversal from a LexSense
		/// and uses those words as search keys to find Semantic Domains that have one of those words in
		/// their Name or ExampleWords fields.
		///
		/// N.B.: This method looks for matches in the BestAnalysisAlternative writing system.
		/// This ought to match what is displayed in the UI, so if the UI doesn't use
		/// BestAnalysisAlternative one of them needs to be changed.
		/// </summary>
		/// <param name="sense"></param>
		/// <returns></returns>
		IEnumerable<ICmSemanticDomain> FindDomainsThatMatchWordsIn(ILexSense sense);

		/// <summary>
		/// Takes the gloss, a short definition (if only one or two words), and reversal from a LexSense
		/// and uses those words as search keys to find Semantic Domains that have one of those words in
		/// their Name or Example Words fields.
		/// In addition, this method returns additional partial matches in the 'out' parameter where one
		/// of the search keys matches the beginning of one of the words in the domain's Name or Example
		/// Words fields.
		///
		/// N.B.: This method looks for matches in the BestAnalysisAlternative writing system.
		/// This ought to match what is displayed in the UI, so if the UI doesn't use
		/// BestAnalysisAlternative one of them needs to be changed.
		/// </summary>
		/// <param name="sense">A LexSense</param>
		/// <param name="partialMatches">extra partial matches</param>
		/// <returns></returns>
		IEnumerable<ICmSemanticDomain> FindDomainsThatMatchWordsIn(ILexSense sense, out IEnumerable<ICmSemanticDomain> partialMatches);

		/// <summary>
		/// This method assumes that a CachingSemDomSearchEngine has cached the Semantic Domains by
		/// search key (a Tuple of word string and writing system integer). It then takes the gloss,
		/// a short definition (if only one or two words), and reversal from a LexSense and uses those
		/// words as search keys to find Semantic Domains that have one of those words in
		/// their Name or Example Words fields.
		/// </summary>
		/// <param name="semDomCache"></param>
		/// <param name="sense"></param>
		/// <returns></returns>
		IEnumerable<ICmSemanticDomain> FindCachedDomainsThatMatchWordsInSense(SemDomSearchCache semDomCache, ILexSense sense);
	}

	public partial interface IConstChartMovedTextMarkerRepository
	{
		/// <summary>
		/// Determines whether a ConstChartWordGroup has a MovedTextMarker
		/// that references it.
		/// </summary>
		/// <param name="ccwg"></param>
		/// <returns></returns>
		bool WordGroupIsMoved(IConstChartWordGroup ccwg);
	}

	public partial interface IConstituentChartCellPartRepository
	{
		/// <summary>
		/// Answer all the constituent chart cells that use this possibility to identify their column.
		/// </summary>
		IEnumerable<IConstituentChartCellPart> InstancesWithChartCellColumn(ICmPossibility target);
	}

	public partial interface IDsChartRepository
	{
		/// <summary>
		/// Answer all the charts that use this possibility as their template.
		/// </summary>
		IEnumerable<IDsChart> InstancesWithTemplate(ICmPossibility target);
	}

	public partial interface IPhEnvironmentRepository
	{
		/// <summary>
		/// Get all IPhEnvironment that have no problem annotations.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IPhEnvironment> AllValidInstances();
	}

	public partial interface IStTextRepository
	{
		/// <summary>
		/// Returns StTexts, possibly spanning multiple owning fields.
		/// </summary>
		/// <param name="hvos"></param>
		/// <returns></returns>
		IList<IStText> GetObjects(IList<int> hvos);
	}

	public partial interface IStFootnoteRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a footnote from the ORC reference in text properties
		/// </summary>
		/// <returns>The footnote referenced in the properties or <c>null</c> if the properties
		/// were not for a footnote ORC</returns>
		/// ------------------------------------------------------------------------------------
		IStFootnote GetFootnoteFromObjData(string objData);
	}

	public partial interface IScrFootnoteRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a footnote from the ORC reference in text properties
		/// </summary>
		/// <param name="ttp">The text properties</param>
		/// <returns>The footnote referenced in the properties or <c>null</c> if the properties
		/// were not for a footnote ORC</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote GetFootnoteFromProps(ITsTextProps ttp);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries to get a footnote from a guid.
		/// </summary>
		/// <param name="footnoteGuid">Guid that identifies a footnote</param>
		/// <param name="footnote">Footnote with footnoteGuid as its id</param>
		/// <returns><c>true</c> if the footnote is found; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		bool TryGetFootnote(Guid footnoteGuid, out IScrFootnote footnote);
	}

	public partial interface IStTxtParaRepository
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="hvos"></param>
		/// <returns></returns>
		IList<IStTxtPara> GetObjects(IList<int> hvos);
	}

	public partial interface IWfiMorphBundleRepository
	{
		/// <summary>
		/// Answer all the bundles that have the specified Sense as their target.
		/// </summary>
		IEnumerable<IWfiMorphBundle> InstancesWithSense(ILexSense target);
	}

	public partial interface IWfiWordformRepository
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="tssForm"></param>
		/// <param name="fIncludeLowerCaseForm"></param>
		/// <param name="wf"></param>
		/// <returns></returns>
		bool TryGetObject(ITsString tssForm, bool fIncludeLowerCaseForm, out IWfiWordform wf);

		/// <summary>
		///
		/// </summary>
		/// <param name="tssForm"></param>
		/// <param name="wf"></param>
		/// <returns></returns>
		bool TryGetObject(ITsString tssForm, out IWfiWordform wf);

		/// <summary>
		/// Get an existing wordform that matches the given <paramref name="form"/>
		/// and <paramref name="ws"/>
		/// </summary>
		/// <param name="ws">Writing system of the form</param>
		/// <param name="form">Form to match, along with the writing system</param>
		/// <returns>The matching wordform, or null if not found.</returns>
		IWfiWordform GetMatchingWordform(int ws, string form);
	}

	/// <summary>
	/// Internal methods of WfiWordformRepository needed outside the Impl subdomain
	/// </summary>
	internal interface IWfiWordformRepositoryInternal
	{
		bool OccurrencesInTextsInitialized { get; }
		/// <summary>
		/// Ensures the OccurrencesInTexts caches have been initialized.
		/// </summary>
		void EnsureOccurrencesInTexts();

		void UpdateForm(ITsString oldForm, IWfiWordform wf, int ws);
		void RemoveForm(ITsString oldForm, int ws);
		void AddToPhraseDictionary(ITsString possiblePhrase);
		void RemovePhraseFromDictionary(ITsString possiblePhrase);
		Dictionary<string, HashSet<ITsString>> FirstWordToPhrases { get;}
	}

	/// <summary>
	/// Internal methods of PunctuationFormRepository needed outside the Impl subdomain
	/// </summary>
	internal interface IPunctuationFormRepositoryInternal
	{
		void UpdateForm(ITsString oldForm, IPunctuationForm pf);
		void RemoveForm(ITsString oldForm);
	}

	public partial interface IPunctuationFormRepository
	{
		/// <summary>
		/// Find the PunctuationForm that has the specified target as its form (WS and other properties are ignored).
		/// </summary>
		bool TryGetObject(ITsString tssTarget, out IPunctuationForm pf);

	}

	public partial interface IReversalIndexRepository
	{
		/// <summary>
		/// Get (or create if needed) the index with the specified writing system.
		/// </summary>
		IReversalIndex FindOrCreateIndexForWs(int ws);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Hand-written extensions for: ICmAnnotationDefnRepository
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial interface ICmAnnotationDefnRepository
	{
		/// <summary>
		/// AnnotationDefn used for Translator Annotations.
		/// </summary>
		ICmAnnotationDefn TranslatorAnnotationDefn { get; }
		/// <summary>
		/// AnnotationDefn used for Consultant Annotations.
		/// </summary>
		ICmAnnotationDefn ConsultantAnnotationDefn { get; }
		/// <summary>
		/// AnnotationDefn used for editorial checking inconsistencies.
		/// </summary>
		ICmAnnotationDefn CheckingError { get; }
	}

	public partial interface IMoMorphTypeRepository
	{
		/// <summary>
		/// Get the MoMorphType objects for the major types.
		/// </summary>
		/// <param name="mmtStem"></param>
		/// <param name="mmtPrefix"></param>
		/// <param name="mmtSuffix"></param>
		/// <param name="mmtInfix"></param>
		/// <param name="mmtBoundStem"></param>
		/// <param name="mmtProclitic"></param>
		/// <param name="mmtEnclitic"></param>
		/// <param name="mmtSimulfix"></param>
		/// <param name="mmtSuprafix"></param>
		void GetMajorMorphTypes(out IMoMorphType mmtStem, out IMoMorphType mmtPrefix,
			out IMoMorphType mmtSuffix, out IMoMorphType mmtInfix,
			out IMoMorphType mmtBoundStem, out IMoMorphType mmtProclitic,
			out IMoMorphType mmtEnclitic, out IMoMorphType mmtSimulfix,
			out IMoMorphType mmtSuprafix);
	}

	public partial interface ILexEntryRepository
	{
		/// Returns the list of all the complex form LexEntry objects that refer to the specified
		/// LexEntry/LexSense in ShowComplexFormsIn.
		IEnumerable<ILexEntry> GetVisibleComplexFormEntries(ICmObject mainEntryOrSense);

		/// Returns the list of all the complex form LexEntry objects that refer to the specified
		/// LexEntry/LexSense in ComponentLexemes.
		IEnumerable<ILexEntry> GetComplexFormEntries(ICmObject mainEntryOrSense);

		/// Returns the list of all the complex form LexEntry objects that refer to the specified
		/// LexEntry/LexSense in PrmimaryLexemes.
		IEnumerable<ILexEntry> GetSubentries(ICmObject mainEntryOrSense);

			/// Returns the list of all the variant form LexEntry objects that refer to the specified
		/// LexEntry/LexSense as the main entry or sense.
		IEnumerable<ILexEntry> GetVariantFormEntries(ICmObject mainEntryOrSense);

		/// <summary>
		/// Clear the list of homograph information
		/// </summary>
		void ResetHomographs(IProgress progressBar);

		/// <summary>
		/// Return a list of all the homographs of the specified form.
		/// </summary>
		List<ILexEntry> GetHomographs(string sForm);

		/// <summary>
		/// This overload is useful to get a list of homographs for a given string, not starting
		/// with any particlar entry, but limited to ones compatible with the specified morph type.
		/// </summary>
		List<ILexEntry> CollectHomographs(string sForm, IMoMorphType morphType);

		/// <summary>
		/// Maps the specified morph type onto a canonical one that should be used in comparing two
		/// entries to see whether they are homographs.
		/// </summary>
		IMoMorphType HomographMorphType(FdoCache cache, IMoMorphType morphType);

		/// <summary>
		/// Find the list of LexEntry objects which conceivably match the given wordform.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssWf"></param>
		/// <param name="wfa"></param>
		/// <param name="duplicates"></param>
		/// <returns></returns>
		List<ILexEntry> FindEntriesForWordform(FdoCache cache, ITsString tssWf, IWfiAnalysis wfa, ref bool duplicates);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find wordform given a cache and the string.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssWf"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ILexEntry FindEntryForWordform(FdoCache cache, ITsString tssWf);
	}

	internal interface ILexEntryRepositoryInternal
	{
		/// <summary>
		/// Update the homograph cache when an entry's HomographForm changes.
		/// </summary>
		void UpdateHomographCache(ILexEntry entry, string oldHf);

		/// <summary>
		/// Collect all the homographs of the given form from the given list of entries.  If fMatchLexForms
		/// is true, then match against lexeme forms even if citation forms exist.  (This behavior is needed
		/// to fix LT-6024 for categorized entry [now called Collect Words].)
		/// </summary>
		List<ILexEntry> CollectHomographs(string sForm, int hvo, List<ILexEntry> entries,
			IMoMorphType morphType, bool fMatchLexForms);
	}

	public partial interface ILexEntryRefRepository
	{
		/// <summary>
		/// Returns the list of all the variant LexEntryRef objects that refer to the specified
		/// LexEntry/LexSense as the main entry or sense.
		/// </summary>
		IEnumerable<ILexEntryRef> GetVariantEntryRefsWithMainEntryOrSense(ICmObject mainEntryOrSense);
		/// <summary>
		/// Returns all the complex forms which have the indicated entry or sense as one of their
		/// PrimaryLexemes.
		/// </summary>
		/// <returns></returns>
		IEnumerable<ILexEntryRef> GetSubentriesOfEntryOrSense(ICmObject mainEntryOrSense);

		/// <summary>
		/// Returns the list of all the complex form LexEntryRef objects that refer to the specified
		/// LexEntry/LexSense in ShowComplexFormIn.
		/// </summary>
		IEnumerable<ILexEntryRef> VisibleEntryRefsOfEntryOrSense(ICmObject mainEntryOrSense);

		/// <summary>
		/// Returns all the complex forms which have any of the indicated entries or senses as one of their
		/// PrimaryLexemes.
		/// </summary>
		IEnumerable<ILexEntryRef> GetSubentriesOfEntryOrSense(IEnumerable<ICmObject> targets);
	}

	public partial interface ILexSenseRepository
	{
		/// <summary>
		/// Backreference property for SemanticDomain property of LexSense.
		/// </summary>
		IEnumerable<ILexSense> InstancesWithSemanticDomain(ICmSemanticDomain domain);
		/// <summary>
		/// Backreference property for ReversalEntries property of LexSense.
		/// </summary>
		IEnumerable<ILexSense> InstancesWithReversalEntry(IReversalIndexEntry entry);
	}

	public partial interface ILexReferenceRepository
	{
		/// <summary>
		/// Returns the list of all the LexReference objects that refer to the specified
		/// LexEntry/LexSense as a target.
		/// </summary>
		IEnumerable<ILexReference> GetReferencesWithTarget(ICmObject target);
	}

	public partial interface IPhSequenceContextRepository
	{
		/// <summary>
		/// Returns all sequence contexts that contain the specified context.
		/// </summary>
		/// <param name="ctxt">The context.</param>
		/// <returns></returns>
		IEnumerable<IPhSequenceContext> InstancesWithMember(IPhPhonContext ctxt);
	}

	public partial interface IPhIterationContextRepository
	{
		/// <summary>
		/// Returns all iteration contexts that iterate over the specified context.
		/// </summary>
		/// <param name="ctxt">The context.</param>
		/// <returns></returns>
		IEnumerable<IPhIterationContext> InstancesWithMember(IPhPhonContext ctxt);
	}

	public partial interface IPhSimpleContextSegRepository
	{
		/// <summary>
		/// Returns all phoneme contexts that reference the specified phoneme.
		/// </summary>
		/// <param name="phoneme">The phoneme.</param>
		/// <returns></returns>
		IEnumerable<IPhSimpleContextSeg> InstancesWithPhoneme(IPhPhoneme phoneme);
	}

	public partial interface IPhSimpleContextNCRepository
	{
		/// <summary>
		/// Returns all natural class contexts that reference the specified natural class.
		/// </summary>
		/// <param name="nc">The natural class.</param>
		/// <returns></returns>
		IEnumerable<IPhSimpleContextNC> InstancesWithNC(IPhNaturalClass nc);
	}

	#region IParagraphCounterRepository
	/// <summary>
	///
	/// </summary>
	public interface IParagraphCounterRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registers the type used to create new para counters for the specified view type id
		/// </summary>
		/// <typeparam name="T">The class of para counter to create</typeparam>
		/// <param name="viewTypeId">The view type id.</param>
		/// ------------------------------------------------------------------------------------
		void RegisterViewTypeId<T>(int viewTypeId) where T : IParagraphCounter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unregisters the view type id.
		/// </summary>
		/// <param name="viewTypeId">The view type id.</param>
		/// ------------------------------------------------------------------------------------
		void UnregisterViewTypeId(int viewTypeId);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph counter for the gieven type of view
		/// </summary>
		/// <param name="viewTypeId">An identifier for a group of views that share the same
		/// height estimator</param>
		/// ------------------------------------------------------------------------------------
		IParagraphCounter GetParaCounter(int viewTypeId);
	}
	#endregion

	#region IFilteredScrBookRepository
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Repository of Scripture book filters
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFilteredScrBookRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all of the book filters from this repository
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Clear();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets virtual property handler corresponding to filter instance.
		/// </summary>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// ------------------------------------------------------------------------------------
		FilteredScrBooks GetFilterInstance(int filterInstance);
	}
	#endregion

	public partial interface IScrCheckRunRepository
	{
		/// <summary>
		/// Gets the instance of the run history for the given check if any.
		/// </summary>
		/// <param name="bookId">The canonical number (1-based) of the desired book</param>
		/// <param name="checkId">A GUID that uniquely identifies the editorial check</param>
		/// <returns>The run history for the requested check or <c>null</c></returns>
		IScrCheckRun InstanceForCheck(int bookId, Guid checkId);
	}

	public partial interface IPublicationRepository
	{
		/// <summary>
		/// Finda a publication with the given name.
		/// </summary>
		/// <param name="name">Name of the desired publication</param>
		/// <returns>The publication or null if no matching publicaiton is found</returns>
		IPublication FindByName(string name);
	}

	public partial interface IScrDraftRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified ScrDraft.
		/// </summary>
		/// <param name="description">The description of the ScrDraft to find.</param>
		/// <param name="draftType">Type of the ScrDraft to find.</param>
		/// ------------------------------------------------------------------------------------
		IScrDraft GetDraft(string description, ScrDraftType draftType);
	}

	public partial interface IScrBookAnnotationsRepository
	{
		/// <summary>
		/// Gets the annotations for the given book.
		/// </summary>
		/// <param name="bookId">The canonical number (1-based) of the desired book</param>
		/// <returns>The annotations for the requested book</returns>
		IScrBookAnnotations InstanceForBook(int bookId);
	}

	public partial interface ITextTagRepository
	{
		/// <summary>
		/// Gets all text tags that reference the specified text markup tag.
		/// </summary>
		IEnumerable<ITextTag> GetByTextMarkupTag(ICmPossibility tag);
	}
}
