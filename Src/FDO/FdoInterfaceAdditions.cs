// --------------------------------------------------------------------------------------------
// Copyright (C) 2006 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: FdoInterfaceAdditions.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Additions to FDO model interfaces go here.
// One needs to be careful adding new stuff, just because it can be done.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using Paratext;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.FDO
{
	#region Enumerations
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Style context values
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum ContextValues
	{
		/// <summary></summary>
		General,
		/// <summary></summary>
		Annotation,
		/// <summary></summary>
		BackMatter,
		/// <summary></summary>
		BackTranslation,
		/// <summary></summary>
		Book,
		/// <summary>This MUST match the value of knContextInternal in FmtGenDlg.cpp</summary>
		Internal,
		/// <summary>This MUST match the value of knContextInternalMappable in FmtGenDlg.cpp</summary>
		InternalMappable,
		/// <summary></summary>
		Intro,
		/// <summary></summary>
		Note,
		/// <summary></summary>
		Publication,
		/// <summary></summary>
		Text,
		/// <summary></summary>
		Title,
		/// <summary>Not a real Context!</summary>
		EndMarker,
		/// <summary></summary>
		IntroTitle,
		/// <summary>
		/// This one is currently used by Flex, not TE. It is a style which the user may select,
		/// but only for purposes of configuring a view (e.g., Dictionary view), not for general
		/// use within a string. This allows us to define these styles externally and in
		/// parallel with a particular default version of the view, without having to do
		/// data migration.
		/// </summary>
		InternalConfigureView,
		/// <summary>Not a real style at all (used to support export settings)</summary>
		PsuedoStyle,
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum StructureValues
	{
		/// <summary></summary>
		Undefined,
		/// <summary></summary>
		Heading,
		/// <summary></summary>
		Body,
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum FunctionValues
	{
		/// <summary>Used for normal prose styles</summary>
		Prose,
		/// <summary></summary>
		Line,
		/// <summary></summary>
		List,
		/// <summary>Used for table styles</summary>
		Table,
		/// <summary>Used for Chapter Number style</summary>
		Chapter,
		/// <summary>Used for Verse Number style</summary>
		Verse,
		/// <summary>This is needed to qualify internalMappable styles that are only valid for
		/// use in footnotes</summary>
		Footnote,
		/// <summary>Used only for the "Stanza Break" style, which is required to have no contents
		/// and be used only between stanzas of poetry</summary>
		StanzaBreak,
	}
	#endregion

	/// <summary>
	/// Non-model interface additions for ICmObject.
	/// </summary>
	public partial interface ICmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the Object is complete (true)
		/// or if it still needs to have work done on it (false).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsComplete
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object is real.
		/// </summary>
		/// <remarks> some statistics on the cost of this function:
		/// Loading the 412 WfiWordforms in WFI.Wordforms vector one-by-one on a 1.6 GHz Athlon, in debug mode:
		///		4.4 seconds without validation, 4.9 sec. with validation.
		///		loading that vector all at once (using foreach which uses an ObjectSet)
		///		.3 seconds without validation, .42 seconds with validation
		///</remarks>
		/// ------------------------------------------------------------------------------------
		bool IsRealObject
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This allows each class to verify that it is OK to delete the object.
		/// If it is not Ok to delete the object, a message should be given explaining
		/// why the object can't be deleted.
		/// </summary>
		/// <returns>True if Ok to delete.</returns>
		/// ------------------------------------------------------------------------------------
		bool ValidateOkToDelete();

		/// <summary>
		/// Implement any side effects that should be done when an object is moved.
		/// Note: not all subclasses necessarily implement all that should be done;
		/// for example, if this is used to move senses from one entry to another, there
		/// ought to be something to copy the MSA, but that isn't done yet.
		/// </summary>
		/// <param name="hvoOldOwner"></param>
		void MoveSideEffects(int hvoOldOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the object satisfies constraints imposed by the class
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>true if the object is all right</returns>
		/// ------------------------------------------------------------------------------------
		bool CheckConstraints(int flidToCheck, out ConstraintFailure failure);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy contents of this object to another one
		/// </summary>
		/// <param name="objNew">target object</param>
		/// <remarks>override this to copy the content</remarks>
		/// ------------------------------------------------------------------------------------
		void CopyTo(ICmObject objNew);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a list of ClassAndPropInfo objects giving information about the classes that can be
		/// stored in the owning properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<ClassAndPropInfo> PropsAndClassesOwnedBy
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>e.g. "color" would not be relevant on a part of speech, ever.
		/// e.g.  MoAffixForm.inflection classes are only relevant if the MSAs of the
		/// entry include an inflectional affix MSA.
		/// </remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		bool IsFieldRelevant(int flid);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns the object found in the given flid
		/// </summary>
		/// <remarks> assumes that this property has been loaded into the cache,
		/// which was always true at the time of this writing.</remarks>
		/// <param name="flid"></param>
		/// <returns>a CmObject or null</returns>
		/// ------------------------------------------------------------------------------------
		ICmObject GetObjectInAtomicField(int flid);

		/// <summary>
		/// Gets the object which, for the indicated property of the recipient, the user is
		/// most likely to want to edit if the ReferenceTargetCandidates do not include the
		/// target he wants.
		/// The canonical example, supported by the default implementation of
		/// ReferenceTargetCandidates, is a possibility list, where the targets are the items.
		/// Subclasses which have reference properties edited by the simple list chooser
		/// should generally override either this or ReferenceTargetCandidates or both.
		/// The implementations of the two should naturally be consistent.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		ICmObject ReferenceTargetOwner(int flid);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>e.e. MoAdhocProhib makes no sense without "morphemes".
		/// e.g. MoEndoCompound makes no sense without a left and a right compound.
		/// </remarks>
		/// <returns>true, if the field is required.</returns>
		/// ------------------------------------------------------------------------------------
		bool IsFieldRequired(int flid);

		/// <summary>
		/// Owner for this object.
		/// </summary>
		ICmObject Owner{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of this object in the owner's collection
		/// </summary>
		/// <returns>Index in owner's collection, or -1 if not in collection.</returns>
		/// ------------------------------------------------------------------------------------
		int IndexInOwner
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		Set<int> ReferenceTargetCandidates(int flid);

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		bool IsValidObject();

		/// <summary>
		/// Main cache wrapper class.
		/// </summary>
		FdoCache Cache
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<LinkedObjectInfo> LinkedObjects
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		bool IsDummyObject
		{
			get;
		}

		/// <summary>
		/// Get all objects that refer to this object,
		/// but not to objects it owns.
		/// </summary>
		List<LinkedObjectInfo> BackReferences
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="objSrc"></param>
		void MergeObject(ICmObject objSrc);

		/// <summary>
		///
		/// </summary>
		/// <param name="objSrc"></param>
		/// <param name="fLoseNoStringData"></param>
		void MergeObject(ICmObject objSrc, bool fLoseNoStringData);

		/// <summary>
		///
		/// </summary>
		bool CanDelete
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		void DeleteUnderlyingObject();

		/// <summary>
		///
		/// </summary>
		/// <param name="state"></param>
		void DeleteUnderlyingObject(ProgressState state);

		/// <summary>
		///
		/// </summary>
		/// <param name="objectsToDeleteAlso"></param>
		/// <param name="state"></param>
		void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state);

		/// <summary>
		///
		/// </summary>
		string ShortName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString ShortNameTSS
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString DeletionTextTSS
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString ChooserNameTS
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string SortKey
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string SortKeyWs
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int SortKey2
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string SortKey2Alpha
		{
			get;
		}

		/// <summary>
		/// Notifies those interested that this object has been created, initialized, and added to its owner.
		/// </summary>
		void NotifyNew();
	}

	public partial interface ICmMajorObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for a header/footer set with the specified name in the DB
		/// </summary>
		/// <param name="name">The name of the header/footer set</param>
		/// <returns>
		/// The header/footer set with the given name if it was found, null otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		IPubHFSet FindHeaderFooterSetByName(string name);
	}

	/// <summary>
	/// Non-model interface additions for ILangProject.
	/// </summary>
	public partial interface ILangProject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Key Terms list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ICmPossibilityList KeyTermsList
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of CmAnnotationDefns that belong to Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FdoOwningSequence<ICmPossibility> ScriptureAnnotationDfns
		{
			get;
		}

		/// <summary>
		/// Get the default Constituent Chart template (creating it and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		ICmPossibility GetDefaultChartTemplate();

		/// <summary>
		/// Creates a Constituent Chart template (and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		CmPossibility CreateChartTemplate(XmlNode spec);

		/// <summary>
		/// Get the default Constituent Chart markers list (creating it and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		ICmPossibility GetDefaultChartMarkers();

		/// <summary>
		/// Creates a list of Constituent Chart markers (and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		ICmPossibilityList MakeChartMarkers(string xml);

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		Set<NamedWritingSystem> GetPronunciationWritingSystems();

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		Set<NamedWritingSystem> GetDbNamedWritingSystems();

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		Set<NamedWritingSystem> GetActiveNamedWritingSystems();

		/// <summary>
		/// Gets the current analysis and vernacular writing systems.
		/// </summary>
		/// <value>The current analysis and vernacular writing systems.</value>
		Set<int> CurrentAnalysisAndVernWss { get;}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		Set<NamedWritingSystem> GetAllNamedWritingSystems();

		/// <summary>
		///
		/// </summary>
		/// <param name="icuLocale"></param>
		/// <returns></returns>
		string GetWritingSystemName(string icuLocale);

		/// <summary>
		///
		/// </summary>
		void CacheDefaultWritingSystems();

		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		ITsString GetMagicStringAlt(int ws, int hvo, int flid);

		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="fWantString"></param>
		/// <param name="retWs"></param>
		/// <returns></returns>
		ITsString GetMagicStringAlt(int ws, int hvo, int flid, bool fWantString, out int retWs);

		/// <summary>
		///
		/// </summary>
		void InitializePronunciationWritingSystems();

		/// <summary>
		///
		/// </summary>
		/// <param name="sValues"></param>
		void UpdatePronunciationWritingSystems(string sValues);

		/// <summary>
		/// Get the language project's list of pronunciation writing systems into sync with the supplied list.
		/// </summary>
		void UpdatePronunciationWritingSystems(int[] newValues);

		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		int ActualWs(int ws, int hvo, int flid);

		/// <summary>
		///
		/// </summary>
		/// <param name="magicName"></param>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		int ActualWs(string magicName, int hvo, int flid);

		/// <summary>
		///
		/// </summary>
		FdoObjectSet<IPartOfSpeech> AllPartsOfSpeech
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int DefaultUserWritingSystem
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int DefaultPronunciationWritingSystem
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int DefaultAnalysisWritingSystem
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string DefaultAnalysisWritingSystemICULocale
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		MultiUnicodeAccessor DefaultAnalysisWritingSystemName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string DefaultUserWritingSystemICULocale
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		MultiUnicodeAccessor DefaultUserWritingSystemName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string DefaultUserWritingSystemFont
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int DefaultVernacularWritingSystem
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string DefaultVernacularWritingSystemICULocale
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		MultiUnicodeAccessor DefaultVernacularWritingSystemName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string DefaultVernacularWritingSystemFont
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string DefaultAnalysisWritingSystemFont
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a font name for the default font for the given writing system.
		/// </summary>
		/// <remarks> see comments under DefaultVernacularWritingSystemFont</remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string GetDefaultFontForWs(int ws);

		/// <summary>
		///
		/// </summary>
		ICmAgent ConstraintCheckerAgent
		{
			get;
		}

		/// <summary>
		/// Gets the analyzing agent representing the current user
		/// </summary>
		ICmAgent DefaultUserAgent
		{
			get;
		}

		/// <summary>
		/// Gets the analyzing agent representing the current parser
		/// </summary>
		ICmAgent DefaultParserAgent
		{
			get;
		}

		/// <summary>
		/// Gets the analyzing agent representing the computer. Do not use this for the parser;
		/// there is a dedicated agent for that purpose.
		/// </summary>
		ICmAgent DefaultComputerAgent
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		IFsFeatStrucType ExceptionFeatureType
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoReversalIndexEntry"></param>
		/// <param name="forceIncludeEnglish"></param>
		/// <returns></returns>
		int[] GetReversalIndexWritingSystems(int hvoReversalIndexEntry, bool forceIncludeEnglish);

		/// <summary>
		/// Return ExtLinkRootDir if set, or set it to FWDataDirectory and return that value.
		/// </summary>
		string ExternalLinkRootDir
		{
			get;
		}
	}

	/// <summary>
	/// Non-model interface additions for ILexDb.
	/// </summary>
	public partial interface ILexDb
	{
		/// <summary>
		///
		/// </summary>
		FdoObjectSet<IMoForm> AllAllomorphs
		{
			get;
		}
		/// <summary>
		///
		/// </summary>
		FdoObjectSet<IMoMorphSynAnalysis> AllMSAs
		{
			get;
		}
		/// <summary>
		///
		/// </summary>
		List<int> CurrentReversalIndices
		{
			get;
		}
	}

	/// <summary>
	/// Non-model interface additions for ILexSense.
	/// </summary>
	public partial interface ILexSense : IVariantComponentLexeme
	{
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		MsaType GetDesiredMsaType();

		/// <summary>
		///
		/// </summary>
		List<ILexSense> AllSenses
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ILexEntry Entry
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int EntryID
		{
			get;
		}

		/// <summary>
		/// If there is a guessed analysis derived from this sense, adjust its WfiGloss to match ours.
		/// </summary>
		void AdjustDerivedAnalysis();
	}


	/// <summary>
	/// interfaces common to LexEntry and LexSense pertaining to
	/// their use as targets in LexEntryRef.ComponentLexemes.
	/// </summary>
	public interface IVariantComponentLexeme : ICmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry in the ComponentLexemes field and
		/// that have a nonempty VariantEntryTypes field.
		/// Note: this must stay in sync with LoadAllVariantFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<int> VariantFormEntryBackRefs { get; }

		/// <summary>
		/// creates a variant entry from this (main entry or sense) component,
		/// and links the variant to this (main entry or sense) component via
		/// EntryRefs.ComponentLexemes
		///
		/// NOTE: The caller will need to supply the lexemeForm subsequently.
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType);

		/// <summary>
		/// creates a variant entry from this (main entry or sense) component,
		/// and links the variant to this (main entry or sense) component via
		/// EntryRefs.ComponentLexemes
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <param name="tssVariantLexemeForm">the lexeme form of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType, ITsString tssVariantLexemeForm);
	}

	/// <summary>
	/// Non-model interface additions for ILexDb.
	/// </summary>
	public partial interface ILexEntry : IVariantComponentLexeme
	{
		/// <summary>
		/// Determines if the entry is a circumfix
		/// </summary>
		/// <returns></returns>
		bool IsCircumfix();

		/// <summary>
		/// Find an allomorph with the specified form, if any. Searches both LexemeForm and
		/// AlternateForms properties.
		/// </summary>
		/// <param name="tssForm"></param>
		IMoForm FindMatchingAllomorph(ITsString tssForm);

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		int FindOrCreateDefaultMsa();

		/// <summary>
		///
		/// </summary>
		/// <param name="ls"></param>
		void MoveSenseToCopy(ILexSense ls);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Citations the form sort key.
		/// </summary>
		/// <param name="sortedFromEnd">if set to <c>true</c> [sorted from end].</param>
		/// <param name="ws">The ws.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string CitationFormSortKey(bool sortedFromEnd, int ws);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fulls the sort key.
		/// </summary>
		/// <param name="sortedFromEnd">if set to <c>true</c> [sorted from end].</param>
		/// <param name="ws">The ws.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string FullSortKey(bool sortedFromEnd, int ws);

		/// <summary>
		///
		/// </summary>
		string CitationFormWithAffixType
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string HomographForm
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString HeadWord
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int MorphType
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<IMoMorphType> MorphTypes
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		IMoForm[] AllAllomorphs
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<ILexSense> AllSenses
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		bool SupportsInflectionClasses();

		/// <summary>
		/// If entry has a LexemeForm, that type is primary and should be used for new ones (LT-4872).
		/// </summary>
		/// <returns></returns>
		int GetDefaultClassForNewAllomorph();

		/// <summary>
		/// Make this entry a variant of the given componentLexeme (primary entry or sense) with
		/// the given variantType
		/// </summary>
		/// <param name="componentLexeme"></param>
		/// <param name="variantType"></param>
		ILexEntryRef MakeVariantOf(IVariantComponentLexeme componentLexeme, ILexEntryType variantType);

		/// <summary>
		/// This replaces a MoForm belonging to this LexEntry with another one, presumably
		/// changing from a stem to an affix or vice versa.
		/// </summary>
		/// <param name="mfOld"></param>
		/// <param name="mfNew"></param>
		void ReplaceMoForm(IMoForm mfOld, IMoForm mfNew);

		/// <summary>
		/// Set the specified WS of the form of your LexemeForm, making sure not to include any
		/// morpheme break characters. As a special case, if your LexemeForm is a circumfix,
		/// do not strip morpheme break characters, and also try to set the form of prefix and suffix.
		/// </summary>
		void SetLexemeFormAlt(int ws, ITsString tssLexemeFormIn);

		/// <summary>
		/// If any allomorphs have a root type (root or bound root), change them to the corresponding stem type.
		/// </summary>
		void ChangeRootToStem();
	}

	/// <summary>
	/// Non-model interface additions for IMoMorphSynAnalysis.
	/// </summary>
	public partial interface IMoMorphSynAnalysis
	{
		/// <summary>
		/// Get gloss of first sense that uses this msa
		/// </summary>
		/// <returns>the gloss as a string</returns>
		string GetGlossOfFirstSense();

		/// <summary>
		///
		/// </summary>
		/// <param name="sourceMsa"></param>
		void SwitchReferences(IMoMorphSynAnalysis sourceMsa);

		/// <summary>
		///
		/// </summary>
		string InterlinearName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString InterlinearNameTSS
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string InterlinearAbbr
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString InterlinearAbbrTSS
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString InterlinAbbrTSS(int ws);

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		string PosFieldName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString LongNameTs
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string LongName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="msa"></param>
		/// <returns></returns>
		bool EqualsMsa(IMoMorphSynAnalysis msa);
	}

	/// <summary>
	/// Non-model interface additions for ICmPossibilityList.
	/// </summary>
	public partial interface IWordformInventory
	{
		/// <summary>
		/// Clear out wordform Occurrences property from existing wordforms.
		/// This ensures that we won't create duplicates as we parse through a text.
		/// As a result, after we parse, the only occurrences that will be cached
		/// are for whatever we parsed.
		/// The advantage is that we don't have to clean up ones that aren't reused,
		/// or worry about whether we've reused them correctly.
		/// The performance hit may not be noticeable during parse.
		/// </summary>
		void ResetAllWordformOccurrences();

		/// <summary>
		/// Creates a dummy wordform and updates the appropriate tables.
		/// </summary>
		/// <param name="tssWord"></param>
		/// <returns></returns>
		int AddDummyWordform(ITsString tssWord);

		/// <summary>
		/// return the (dummy or database) id for the given wordform (and its ws).
		/// </summary>
		/// <param name="tssForm"></param>
		/// <returns></returns>
		int GetWordformId(ITsString tssForm);

		/// <summary>
		/// Lookup the form, and try to match (on its writing system).
		/// </summary>
		/// <param name="tssForm"></param>
		/// <param name="fIncludeLowerCaseForm">if true, match on lower case form, if other case was not found.</param>
		/// <returns></returns>
		int GetWordformId(ITsString tssForm, bool fIncludeLowerCaseForm);

		/// <summary>
		///
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <param name="fIncludeLowerCaseForm"></param>
		/// <returns></returns>
		int GetWordformId(string form, int ws, bool fIncludeLowerCaseForm);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return the (dummy or database) id for the given wordform (case sensitive) in the given ws.
		/// </summary>
		/// <param name="form">The form.</param>
		/// <param name="ws">The writing system</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		int GetWordformId(string form, int ws);

		/// <summary>
		///
		/// </summary>
		/// <param name="tssWord"></param>
		/// <returns></returns>
		IWfiWordform AddRealWordform(ITsString tssWord);

		/// <summary>
		///
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		IWfiWordform AddRealWordform(string form, int ws);
		/// <summary>
		/// The Array of StTexts used to compute ConcordanceWords.
		/// </summary>
		List<int> ConcordanceTexts
		{
			get;
			set;
		}

		/// <summary>
		/// Concordance Words virtual property, all the wordforms in ConcordanceTexts.
		/// </summary>
		List<int> ConcordanceWords
		{
			get;
			set;
		}
		/// <summary>
		/// Make the external spelling dictionary conform as closely as possible to the spelling
		/// status recorded in the Wfi. We try to keep these in sync, but when we first create
		/// an external spelling dictionary we need to make it match, and even later, on restoring
		/// a backup or when a user on another computer changed the database, we may need to
		/// re-synchronize. The best we can do is to Add all the words we know are correct and
		/// Remove all the others we know about at all; it's possible that a wordform that was
		/// previously correct and is now deleted will be thought correct by the dictionary.
		/// In the case of a major language, of course, it's also possible that words that were never
		/// in our inventory at all will be marked correct. This is the best we know how to do.
		/// </summary>
		void ConformSpellingDictToWfi();

		/// <summary>
		/// Disable the vernacular spelling dictionary.
		/// </summary>
		void DisableVernacularSpellingDictionary();

		/// <summary>
		/// Update the FormToWfIdTable to store the specified value for the specified key.
		/// This is useful, for example, when Redo re-creates a real Wordform, and in intervening
		/// request to find it may have created a dummy.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <param name="hvoWf"></param>
		void UpdateConcWordform(string form, int ws, int hvoWf);
	}

	/// <summary>
	/// Non-model interface additions for ICmPossibilityList.
	/// </summary>
	public partial interface ICmPossibilityList
	{
		/// <summary>
		///
		/// </summary>
		Set<ICmPossibility> ReallyReallyAllPossibilities
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stringTbl"></param>
		/// <returns></returns>
		string ItemsTypeName(StringTable stringTbl);

		/// <summary>
		/// Look up a possibility in a list having a known GUID value
		/// </summary>
		/// <param name="guid">The GUID value</param>
		/// <returns>the possibility</returns>
		ICmPossibility LookupPossibilityByGuid(Guid guid);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Using the possibility name and writing system, find a possibility, or create a new
		/// one if it doesn't exist.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		int FindOrCreatePossibility(string possibilityPath, int ws);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Using the possibility name and writing system, find a possibility, or create a new
		/// one if it doesn't exist.
		/// </summary>
		/// <param name="possibilityPath">name of the possibility path delimited by ORCs (if
		/// the fFullPath is <c>false</c></param>
		/// <param name="ws">writing system</param>
		/// <param name="fFullPath">whether the full path is provided (possibilities that are
		/// not on the top level will provide the full possibilityPath with ORCs between the
		/// possibility names of each level)</param>
		/// -----------------------------------------------------------------------------------
		int FindOrCreatePossibility(string possibilityPath, int ws, bool fFullPath);
	}

	/// <summary>
	/// Non-model interface additions for ICmPossibility.
	/// </summary>
	public partial interface ICmPossibility
	{
		/// <summary>
		/// Move 'this' to a safe place, if needed.
		/// </summary>
		/// <param name="possSrc"></param>
		/// <remarks>
		/// When merging or moving a CmPossibility, the new home ('this') may actually be owned by
		/// the other CmPossibility, in which case 'this' needs to be relocated, before the merge/move.
		/// </remarks>
		/// <returns>
		/// 1. The new owner (CmPossibilityList or CmPossibility), or
		/// 2. null, if no move was needed.
		/// </returns>
		ICmObject MoveIfNeeded(ICmPossibility possSrc);

		/// <summary>
		///
		/// </summary>
		/// <param name="strTable"></param>
		/// <returns></returns>
		string ItemTypeName(StringTable strTable);

		/// <summary>
		/// Abbreviation and Name with hyphen between.
		/// </summary>
		string AbbrAndName
		{
			get;
		}

		/// <summary>
		/// Get Subpossibilities of the CmPossibility.
		/// For Performance (used in conjunction with PreLoadList).
		/// </summary>
		/// <returns>Set of subpossibilities</returns>
		Set<int> SubPossibilities();


		/// <summary>
		///
		/// </summary>
		Set<ICmPossibility> ReallyReallyAllPossibilities
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ICmPossibilityList OwningList
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ICmPossibility OwningPossibility
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ICmPossibility MainPossibility
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility name hierarchy in the default analysis
		/// writing system with the top-level category first and the last item at the end of the
		/// string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string NameHierarchyString
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility abbreviation hierarchy in the default
		/// analysis writing system with the top-level category first and the last item at the
		/// end of the string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string AbbrevHierarchyString
		{
			get;
		}
	}

	/// <summary>
	/// Non-model interface additions for IReversalIndex.
	/// </summary>
	public partial interface IMoMorphType
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="types"></param>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		bool IsAmbiguousWith(FdoCache cache, MoMorphTypeCollection types,
			IMoMorphType first, IMoMorphType second);

		/// <summary>
		///
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		string FormWithMarkers(string form);
	}

	/// <summary>
	/// Non-model interface additions for IReversalIndex.
	/// </summary>
	public partial interface IReversalIndex
	{
		/// <summary>
		/// Gets the set of entries owned by this reversal index from the set of entries in the input.
		/// The input will come from some source such as the referenced index entries of a sense.
		/// </summary>
		/// <param name="entries">An array which must contain ReversalIndexEntry only objects</param>
		/// <returns>A List of ReversalIndexEntry IDs that match any of the entries in the input array.</returns>
		List<int> EntriesForSense(List<IReversalIndexEntry> entries);

		/// <summary>
		///
		/// </summary>
		List<int> AllEntries
		{
			get;
		}
	}

	/// <summary>
	/// Non-model interface additions for IReversalIndexEntry.
	/// </summary>
	public partial interface IReversalIndexEntry
	{
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
		ICmObject MoveIfNeeded(IReversalIndexEntry rieSrc);

		/// <summary>
		///
		/// </summary>
		IReversalIndexEntry OwningEntry
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		IReversalIndexEntry MainEntry
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		IReversalIndex ReversalIndex
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<IReversalIndexEntry> AllOwningEntries
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IMoForm
	{
		/// <summary>
		/// Swap all references to this MoForm to use the new one
		/// </summary>
		/// <param name="newFormHvo">the hvo of the new MoForm</param>
		void SwapReferences(int newFormHvo);

		/// <summary>
		///
		/// </summary>
		ITsString FormMinusReservedMarkers
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the LongName
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LongName
		{
			get;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the LongNameTSS
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsString LongNameTSS
		{
			get;
		}

		/// <summary>
		/// If the morph has a root type (root or bound root), change it to the corresponding stem type.
		/// </summary>
		void ChangeRootToStem();
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IMoGlossItem
	{
		///<summary>
		///Recursively search for an item embedded within a MoGlossItem.  If not found, the result is null.
		///</summary>
		///<param name="sName">Name attribute to look for (in default analysis writing system)</param>
		///<param name="sAbbreviation">Abbreviation attribute to look for (in default analysis writing system)</param>
		///
		IMoGlossItem FindEmbeddedItem(string sName, string sAbbreviation);

		///<summary>
		///Recursively search for an item embedded within a MoGlossItem.  If not found, the result is null.
		///</summary>
		///<param name="sName">Name attribute to look for (in default analysis writing system)</param>
		///<param name="sAbbreviation">Abbreviation attribute to look for (in default analysis writing system)</param>
		///<param name="fRecurse">Recurse through embedded layers</param>
		///
		IMoGlossItem FindEmbeddedItem(string sName, string sAbbreviation, bool fRecurse);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IMoInflAffixSlot
	{
		/// <summary>
		/// Get a list of inflectional affix LexEntries which do not already refer to this slot
		/// </summary>
		List<int> OtherInflectionalAffixLexEntries
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IMoInflAffMsa
	{
		/// <summary>
		/// Remove any slots in SlotsRC
		/// </summary>
		void ClearAllSlots();
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsFeatureSpecification
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool IsEquivalent(IFsFeatureSpecification other);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsAbstractStructure
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool IsEquivalent(IFsAbstractStructure other);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsFeatStruc
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="item"></param>
		void AddFeatureFromXml(FdoCache cache, XmlNode item);

		/// <summary>
		///
		/// </summary>
		/// <param name="closedFeatHvo"></param>
		/// <returns></returns>
		IFsClosedValue FindClosedValue(int closedFeatHvo);
		/// <summary>
		///
		/// </summary>
		/// <param name="closedFeatHvo"></param>
		/// <returns></returns>
		IFsClosedValue FindOrCreateClosedValue(int closedFeatHvo);

		/// <summary>
		///
		/// </summary>
		/// <param name="complexFeatHvo"></param>
		/// <returns></returns>
		IFsComplexValue FindOrCreateComplexValue(int complexFeatHvo);

		/// <summary>
		///
		/// </summary>
		bool IsEmpty
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string LongName
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		ITsString LongNameTSS
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tagLongName"></param>
		/// <param name="longNameOldLen"></param>
		void UpdateFeatureLongName(int tagLongName, int longNameOldLen);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsSymFeatVal
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="sAbbrev"></param>
		/// <param name="sName"></param>
		/// <returns></returns>
		void SimpleInit(string sAbbrev, string sName);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ICmAgent
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoTarget"></param>
		/// <param name="accepted"></param>
		/// <param name="details"></param>
		void SetEvaluation(int hvoTarget, int accepted, string details);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IUserView: IFlidProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the CmPossibilityList that should be used to supply the
		/// possibilities for the given field in this view.
		/// </summary>
		/// <param name="flid">The field ID</param>
		/// <returns>HVO of a CmPossibilityList, or 0 of the given field is not displayed in
		/// this view or if it is not associated with a possibility list</returns>
		/// ------------------------------------------------------------------------------------
		int GetPossibilityListForProperty(int flid);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Name of view within its task (on the sidebar)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ViewNameShort
		{
			get;
			set;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ICmCell
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the match value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int MatchValue
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given value matches this cell's match criteria.
		/// </summary>
		/// <param name="val"></param>
		/// <returns>True if a match, False if not</returns>
		/// ------------------------------------------------------------------------------------
		bool MatchesCriteria(int val);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if anything in the given array matches this cell's match criteria.
		/// </summary>
		/// <param name="val"></param>
		/// <returns>True if a match, False if not</returns>
		/// ------------------------------------------------------------------------------------
		bool MatchesCriteria(int[] val);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the existing Contents to determine the integer match criteria and set member
		/// data needed by MatchesCriteria.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ParseIntegerMatchCriteria();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the existing Contents to determine the object match criteria and set member
		/// data needed by MatchesCriteria.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ParseObjectMatchCriteria();
	}

	/// <summary>
	/// Additional inteface methods for ICmFile
	/// </summary>
	public partial interface ICmFile
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the absolute InternalPath (i.e., the InternalPath combined with the FW Data
		/// Directory)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string AbsoluteInternalPath
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ICmFilter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// UserView being filtered. If this filter filters on a field whose possible values
		/// come from a possibility list, the user view will be used to determine which
		/// possibility list should be used to retrieve the possibility to match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUserView UserView
		{
			get;
			set;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPartOfSpeech
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		int GetHvoOfHighestPartOfSpeech(IPartOfSpeech ps);

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		bool RequiresInflection();

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="item"></param>
		void AddInflectableFeatsFromXml(FdoCache cache, XmlNode item);

		/// <summary>
		///
		/// </summary>
		List<int> AllAffixSlotIDs
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int NumberOfLexEntries
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		MoInflClassCollection AllInflectionClasses
		{
			get;
		}
		/// <summary>
		///
		/// </summary>
		MoStemNameCollection AllStemNames
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiAnalysis
	{
		/// <summary>
		/// Move the text annotations that refence this object or any WfiGlosses it owns up to the owning WfiWordform.
		/// </summary>
		/// <remarks>
		/// Client is responsible for Undo/Redo wrapping.
		/// </remarks>
		void MoveConcAnnotationsToWordform();

		/// <summary>
		/// Collect all the MSAs referenced under the given WfiAnalysis.
		/// </summary>
		/// <param name="msaHvoList">MSAs found by this call are appended to msaHvoList.</param>
		void CollectReferencedMsaHvos(List<int> msaHvoList);

		/// <summary>
		/// tells whether the given agent has approved or disapproved of this analysis, or has not given an opinion.
		/// </summary>
		/// <param name="agent"></param>
		/// <returns>one of the enumerated values in WfiAnalysis.Opinions.</returns>
		Opinions GetAgentOpinion(ICmAgent agent);

		/// <summary>
		/// Tells whether the giving agent has approved or disapproved of this analysis, or has not given an opinion.
		/// </summary>
		/// <param name="agent"></param>
		/// <param name="opinion"></param>
		/// <returns>one of the enumerated values in Opinions.</returns>
		void SetAgentOpinion(ICmAgent agent, Opinions opinion);

		/// <summary>
		///
		/// </summary>
		int ParserStatusIcon
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int ApprovalStatusIcon
		{
			get;
			set;
		}

		/// <summary>
		///
		/// </summary>
		List<int> ConcordanceIds
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<int> FullConcordanceIds
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiGloss
	{
		/// <summary>
		///
		/// </summary>
		List<int> ConcordanceIds
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<int> FullConcordanceIds
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiMorphBundle
	{
		/// <summary>
		/// Get the HVO of the default sense, or zero, if none.
		/// </summary>
		int DefaultSense
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiWordform
	{
		/// <summary>
		///
		/// </summary>
		int ParserCount
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int UserCount
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		int ConflictCount
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<int> ConcordanceIds
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<int> FullConcordanceIds
		{
			get;
		}

		/// <summary>
		/// Get a count of the annotations that make for a full concordance at all three levels.
		/// </summary>
		int FullConcordanceCount
		{
			get;
		}

		/// <summary>
		/// The CmBaseAnnotations that reference an occurrence of this word in a text.
		/// </summary>
		List<int> OccurrencesInTexts
		{
			get;
		}

		/// <summary>
		/// True, if usear and parser are in agreement on status of all analyses.
		/// </summary>
		bool HumanAndParserAgree
		{
			get;
		}

		/// <summary>
		/// Get a List of Hvos that the human has approved of.
		/// </summary>
		List<int> HumanApprovedAnalyses
		{
			get;
		}

		/// <summary>
		/// Get a List of Hvos that the human has no opinion on.
		/// </summary>
		List<int> HumanNoOpinionParses
		{
			get;
		}

		/// <summary>
		/// Get a List of Hvos that the human has DISapproved of.
		/// </summary>
		List<int> HumanDisapprovedParses
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IStStyle
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this style is a footnote style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsFootnoteStyle
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		bool InUse
		{
			get;
			set;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IStText
	{
		/// <summary>
		/// The time stamp for the last time we parsed.
		/// Returns 0 if we haven't parsed yet.
		/// </summary>
		long LastParsedTimestamp
		{
			get;
		}

		/// <summary>
		/// Set after parsing a text with ParagraphParser.
		/// </summary>
		void RecordParseTimestamp();

		/// <summary>
		/// A text "IsUpToDate" when it has been parsed by ParagraphParser after being modified.
		/// </summary>
		/// <returns></returns>
		bool IsUpToDate();
		/// <summary>
		/// Creates Text Objects in each paragraph
		/// </summary>
		void CreateTextObjects();
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IStTxtPara
	{
		/// <summary>
		/// Returns a List of CmBaseAnnotation ids denoting twfic and punctuation forms found in a Segment.
		/// </summary>
		/// <param name="hvoAnnotationSegment"></param>
		/// <returns></returns>
		List<int> SegmentForms(int hvoAnnotationSegment);

		/// <summary>
		/// Virtual Property: a List of CmBaseAnnotation ids denoting segment (e.g. sentences) in the paragraph.
		/// </summary>
		List<int> Segments
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all pictures "owned" by this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<ICmPicture> GetPictures();

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		ICmTranslation GetOrCreateBT();

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		ICmTranslation GetBT();

		/// <summary>
		///
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		bool GetFootnoteOwnerAndFlid(out ICmObject owner, out int flid);
		/// <summary>
		/// Collects the set of hvos denoting the wordforms in the paragraph.
		/// </summary>
		void CollectUniqueWordforms(Set<int> wordforms);
		/// <summary>
		/// Creates Text Objects in this paragraph
		/// </summary>
		void CreateTextObjects();
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IScrBook
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a <see cref = "IScrBook"/>'s Title paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InitTitlePara();

		/// <summary>
		/// Get the SIL book ID 3-letter code.
		/// </summary>
		string BookId { get; }

		/// <summary>
		/// Gets the best name for showing in the user interface
		/// </summary>
		string BestUIName { get; }

		/// <summary>
		/// Gets the versification currently in effect for Scripture.
		/// </summary>
		ScrVers Versification { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IScrScriptureNote
	{

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the annotation type from the Guid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		NoteType AnnotationType
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the (first paragraph) of the "quote", or cited text of the
		/// annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string CitedText
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the (first paragraph) of the "quote", or cited text of the
		/// annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsString CitedTextTss { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IScrImportSet
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets ImportType. When it is changed, we need to delete the old Sources.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		TypeOfImport ImportTypeEnum
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Paratext Scripture project ID.
		/// </summary>
		/// <remarks>Setter has side-effect of loading the mappings</remarks>
		/// ------------------------------------------------------------------------------------
		string ParatextScrProj
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import the vernacular Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ImportTranslation
		{
			get;
			set;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Indicates if basic properties are set to allow import. For example, if this is a
		/// Paratext project import, at least the vernacular project must be specified for this
		/// to return true. If this is an Other project, at least one filename must be
		/// specified for this to return true.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		bool BasicSettingsExist
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import back translations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ImportBackTranslation
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import Annotations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ImportAnnotations
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import introductions to books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ImportBookIntros
		{
			get;
			set;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface to allow application-specific parsing/generation of a string representing a
	/// picture-location.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPictureLocationBridge
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the picture location range string.
		/// </summary>
		/// <param name="s">The string representation of the picture location range.</param>
		/// <param name="anchorLocation">The anchor location.</param>
		/// <param name="locType">The type of the location range. The incoming value tells us
		/// the assumed type for parsing. The out value can be set to a different type if we
		/// discover that the actual value is another type.</param>
		/// <param name="locationMin">The location min.</param>
		/// <param name="locationMax">The location max.</param>
		/// ------------------------------------------------------------------------------------
		void ParsePictureLoc(string s, int anchorLocation, ref PictureLocationRangeType locType,
			out int locationMin, out int locationMax);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the picture location.
		/// </summary>
		/// <param name="picture">The picture.</param>
		/// ------------------------------------------------------------------------------------
		string GetPictureLocAsString(ICmPicture picture);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ICmPicture : IPictureLocationBridge
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the picture suitable for exporting or for building
		/// a clipboard representation of the object.
		/// </summary>
		/// <param name="fFileNameOnly">If set to <c>true</c> the picture filename does not
		/// contain the full path specification. Use <c>false</c> for the full path (e.g., for
		/// use on the clipboard).
		/// </param>
		/// <param name="sReference">A string containing the picture's reference (Can be null
		/// or empty).</param>
		/// <param name="picLocBridge">A picture location bridge object (can be null).</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string GetTextRepOfPicture(bool fFileNameOnly, string sReference,
			IPictureLocationBridge picLocBridge);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the layout position as a string. Rather that just taking the natural string
		/// representation of each of the <see cref="PictureLayoutPosition"/> values, we convert
		/// a couple of them to use USFM-compatible variations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LayoutPosAsString {get;}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description in the English writing system. Returns empty strng if this
		/// is not set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string EnglishDescriptionAsString { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new CmPicture by creating a copy of the file in the given folder and
		/// hooking everything up.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// <param name="ws">The WS for the location in the caption MultiUnicode to put the
		/// caption</param>
		/// ------------------------------------------------------------------------------------
		void InitializeNewPicture(string srcFilename, ITsString captionTss, string sFolder, int ws);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ICmMedia
	{
		/// <summary>
		/// </summary>
		void InitializeNewMedia(string sFile, string sLabel,
			string sCmFolderName, int ws);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IScrSection
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if this section is an introduction section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsIntro
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the start and end section references to reflect the content of the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void AdjustReferences();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the start and end section references to reflect the content of the section.
		/// </summary>
		/// <param name="fIsIntro">if set to <c>true</c> this is an intro section.</param>
		/// ------------------------------------------------------------------------------------
		void AdjustReferences(bool fIsIntro);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context of the current section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ContextValues Context
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IScripture
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets whether the footnote reference should be displayed for footnotes of the
		/// given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <param name="value"><c>true</c> if the reference should be displayed; <c>false</c>
		/// otherwise</param>
		/// ------------------------------------------------------------------------------------
		void SetDisplayFootnoteReference(string styleId, bool value);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the footnote settings are all set to the default values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool HasDefaultFootnoteSettings
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the import settings named Default, if any, or the first available
		/// one (which is probably the only one), or returns 0 if no settings are available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int DefaultImportSettingsHvo
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets default import settings. Default settings can be for Paratext 5, Paratext 6,
		/// and Other USFM. Setting the import settings to a null value, clears the settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IScrImportSet DefaultImportSettings
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a header footer set by name
		/// </summary>
		/// <param name="name">Name of the desired HF set</param>
		/// <returns>The set if found; otherwise <c>null</c></returns>
		/// ------------------------------------------------------------------------------------
		IPubHFSet FindNamedHfSet(string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts an integer into a string of digits using the zero digit defined for
		/// Scripture.
		/// </summary>
		/// <param name="nValue">Value to be converted</param>
		/// <returns>Converted string for value</returns>
		/// ------------------------------------------------------------------------------------
		string ConvertToString(int nValue);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines type of footnote marker to use for footnotes of the given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns>The type of footnote marker to be used for the given style</returns>
		/// ------------------------------------------------------------------------------------
		FootnoteMarkerTypes DetermineFootnoteMarkerType(string styleId);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines footnote marker symbol to use for footnotes of the given style. Does not
		/// try to take into account whether the symbolic footnote type is in use.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns>The footnote marker symbol to be used for the given style</returns>
		/// ------------------------------------------------------------------------------------
		string DetermineFootnoteMarkerSymbol(string styleId);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a saved version without adding books. Use AddBookToSavedVersion to add books.
		/// </summary>
		/// <remarks>Note that saved versions are called ScrDrafts in the database and used to
		/// called "archives" in the UI.</remarks>
		/// <param name="description">Description for the saved version</param>
		/// <returns>The new saved version</returns>
		/// ------------------------------------------------------------------------------------
		IScrDraft CreateSavedVersion(string description);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a saved version,adding the list of books as specified.
		/// </summary>
		/// <remarks>Note that saved versions are called ScrDrafts in the database and used to
		/// called "archives" in the UI.</remarks>
		/// <param name="description">Description for the saved version</param>
		/// <param name="hvoBooks">Books that are copied to the saved version</param>
		/// <returns>The new saved version</returns>
		/// ------------------------------------------------------------------------------------
		IScrDraft CreateSavedVersion(string description, int[] hvoBooks);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add book to the specified saved version.
		/// </summary>
		/// <remarks>Note that saved versions are called ScrDrafts in the database and used to
		/// called "archives" in the UI.</remarks>
		/// <param name="version">The saved version</param>
		/// <param name="book">book to add</param>
		/// <returns>The HVO of the saved version of the book</returns>
		/// ------------------------------------------------------------------------------------
		int AddBookToSavedVersion(IScrDraft version, IScrBook book);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add book to the specified saved version.
		/// </summary>
		/// <remarks>Note that saved versions are called ScrDrafts in the database and used to
		/// called "archives" in the UI.</remarks>
		/// <param name="version">The saved version</param>
		/// <param name="hvoBook">HVO of book to add</param>
		/// <returns>The HVO of the saved version of the book</returns>
		/// ------------------------------------------------------------------------------------
		int AddBookToSavedVersion(IScrDraft version, int hvoBook);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote marker for footnotes of the given style. (Note that for
		/// auto-sequence footnotes, this will just be an "a", not the correct marker from the
		/// sequence.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns>A string with the marker</returns>
		/// ------------------------------------------------------------------------------------
		string GetFootnoteMarker(string styleId);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote marker to use for the specified footnote paragraph style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <param name="footnoteMarker">TS String with the marker for the actual footnote whose
		/// marker is being computed. This will be used if this type of footnote is supposed to
		/// be a sequence.</param>
		/// <returns>A TsString with the marker to display</returns>
		/// ------------------------------------------------------------------------------------
		ITsString GetFootnoteMarker(string styleId, ITsString footnoteMarker);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the footnote marker should be displayed in the footnote pane for
		/// footnotes of the given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns><c>true</c> if the marker should be displayed; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool GetDisplayFootnoteMarker(string styleId);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that can be used to format a verse bridge for the given writing
		/// system, including the right-to-left marks if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string BridgeForWs(int hvoWs);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that can be used to format a chapter-verse separator for the given
		/// writing system, including the right-to-left marks if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ChapterVerseSeparatorForWs(int hvoWs);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that can be used to format a verse separator for the given
		/// writing system, including the right-to-left marks if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string VerseSeparatorForWs(int hvoWs);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the footnote reference should be displayed for footnotes of the
		/// given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns><c>true</c> if the reference should be displayed; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool GetDisplayFootnoteReference(string styleId);

		/// <summary>
		///
		/// </summary>
		string GeneralFootnoteMarker
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		bool DisplayFootnoteMarker
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ttp"></param>
		/// <returns></returns>
		IStStyle FindStyle(ITsTextProps ttp);

		/// <summary>
		///
		/// </summary>
		/// <param name="styleName"></param>
		/// <returns></returns>
		IStStyle FindStyle(string styleName);

		/// <summary>
		///
		/// </summary>
		/// <param name="sSilAbbrev"></param>
		/// <returns></returns>
		IScrBook FindBook(string sSilAbbrev);

		/// <summary>
		///
		/// </summary>
		/// <param name="bookOrd"></param>
		/// <returns></returns>
		IScrBook FindBook(int bookOrd);

		/// <summary>
		/// Convert from a vernacular verse or chapter number to the kind that should be
		/// displayed in back translations, as configured for this Scripture object.
		/// </summary>
		/// <param name="vernVerseChapterText">Verse or chapter number text. This may be
		/// <c>null</c>.</param>
		/// <returns>The converted verse or chapter number, or empty string if
		/// <paramref name="vernVerseChapterText"/> is <c>null</c>.</returns>
		string ConvertVerseChapterNumForBT(string vernVerseChapterText);

		/// <summary>
		/// Return a TsString in which any CV numbers have been replaced by their BT equivalents
		/// (in the specified writing system). Other properties, including style, are copied
		/// from the input numbers.
		/// </summary>
		ITsString ConvertCVNumbersInStringForBT(ITsString input, int wsTrans);

	}
	public partial interface IScrDraft : ICmObject
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="bookOrd"></param>
		/// <returns></returns>
		IScrBook FindBook(int bookOrd);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsComplexValue
	{
		/// <summary>
		///
		/// </summary>
		string LongName
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsClosedValue
	{
		/// <summary>
		///
		/// </summary>
		string LongName
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ILgWritingSystem
	{
		/// <summary>
		///
		/// </summary>
		string Abbreviation
		{
			get;
		}

		/// <summary>
		/// See http://www.rfc-editor.org/rfc/rfc4646.txt
		/// </summary>
		string RFC4646bis { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPublication
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether the publication is left bound.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsLeftBound
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the thickness of the footnote separator line, in millipoints.
		/// TODO: This should be added to the model and be configurable in Page Setup Dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int FootnoteSepThickness
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPhEnvironment
	{

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the object satisfies constraints imposed by the class
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <param name="fAdjustSquiggly">if set to <c>true</c> [f adjust squiggly].</param>
		/// <returns>true if the object is all right</returns>
		/// ------------------------------------------------------------------------------------
		bool CheckConstraints(int flidToCheck, out ConstraintFailure failure, bool fAdjustSquiggly);

	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPhPhoneme
	{
		/// <summary>
		/// Set description based on the content of the BasicIPASymbol field and the BasicIPAInfo document
		/// </summary>
		/// <param name="doc">XmlDocument containing the BasicIPAInfo</param>
		/// <param name="fJustChangedDescription"></param>
		/// <returns><c>true</c> if the description was changed; <c>false</c> otherwise</returns>
		bool SetDescriptionBasedOnIPA(XmlDocument doc, bool fJustChangedDescription);

		/// <summary>
		/// Set description based on the content of the BasicIPASymbol field and the BasicIPAInfo document
		/// </summary>
		/// <param name="doc">XmlDocument containing the BasicIPAInfo</param>
		/// <param name="fJustChangedFeatures"></param>
		/// <returns><c>true</c> if the description was changed; <c>false</c> otherwise</returns>
		bool SetFeaturesBasedOnIPA(XmlDocument doc, bool fJustChangedFeatures);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPubHFSet
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="copyFrom"></param>
		void CloneDetails(IPubHFSet copyFrom);
	}

	public partial interface IPhSegmentRule
	{
		/// <summary>
		/// Gets the order number.
		/// </summary>
		/// <value>The order number.</value>
		int OrderNumber
		{
			get;
		}
	}

	public partial interface IPhRegularRule
	{
		/// <summary>
		/// Gets all of the feature constraints in this rule.
		/// </summary>
		/// <value>The feature constraints.</value>
		List<int> FeatureConstraints
		{
			get;
		}

		/// <summary>
		/// Gets all of the feature constraints in this rule except those
		/// contained within the specified natural class context.
		/// </summary>
		/// <param name="excludeCtxt">The natural class context.</param>
		/// <returns>The feature constraints.</returns>
		List<int> GetFeatureConstraintsExcept(IPhSimpleContextNC excludeCtxt);
	}

	public partial interface IPhMetathesisRule
	{
		/// <summary>
		/// Gets the structural change indices.
		/// </summary>
		/// <param name="isMiddleWithLeftSwitch">if set to <c>true</c> the context is associated with the left switch context,
		/// otherwise it is associated with the right context.</param>
		/// <returns>The structural change indices.</returns>
		int[] GetStrucChangeIndices(out bool isMiddleWithLeftSwitch);

		/// <summary>
		/// Sets the structural change indices.
		/// </summary>
		/// <param name="indices">The structural change indices.</param>
		/// <param name="isMiddleWithLeftSwitch">if set to <c>true</c> the context is associated with the left switch context,
		/// otherwise it is associated with the right context.</param>
		void SetStrucChangeIndices(int[] indices, bool isMiddleWithLeftSwitch);

		/// <summary>
		/// Updates the <c>StrucChange</c> indices for removal and insertion. Should be called before insertion
		/// or removal from StrucDesc.
		/// </summary>
		/// <param name="strucChangeIndex">Index in structural change.</param>
		/// <param name="ctxtIndex">Index of the context.</param>
		/// <param name="insert">indicates whether the context will be inserted or removed.</param>
		/// <returns>HVO of additional context to remove</returns>
		int UpdateStrucChange(int strucChangeIndex, int ctxtIndex, bool insert);

		/// <summary>
		/// Gets or sets the index of the last context in the left environment.
		/// </summary>
		/// <value>The index of the left environment.</value>
		int LeftEnvIndex
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the index of the first context in the right environment.
		/// </summary>
		/// <value>The index of the right environment.</value>
		int RightEnvIndex
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the index of the left switch context.
		/// </summary>
		/// <value>The index of the left switch context.</value>
		int LeftSwitchIndex
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the index of the right switch context.
		/// </summary>
		/// <value>The index of the right switch context.</value>
		int RightSwitchIndex
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the index of the middle context.
		/// </summary>
		/// <value>The index of the middle context.</value>
		int MiddleIndex
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the limit of the middle context.
		/// </summary>
		/// <value>The middle limit.</value>
		int MiddleLimit
		{
			get;
		}

		/// <summary>
		/// Gets the limit of the left environment.
		/// </summary>
		/// <value>The limit of the left environment.</value>
		int LeftEnvLimit
		{
			get;
		}

		/// <summary>
		/// Gets the limit of the right environment.
		/// </summary>
		/// <value>The limit of the right environment.</value>
		int RightEnvLimit
		{
			get;
		}

		/// <summary>
		/// Gets the limit of the left switch context.
		/// </summary>
		/// <value>The limit of the left switch context.</value>
		int LeftSwitchLimit
		{
			get;
		}

		/// <summary>
		/// Gets the limit of the right switch context.
		/// </summary>
		/// <value>The limit of the right switch context.</value>
		int RightSwitchLimit
		{
			get;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the middle context is associated
		/// with the left switch context or right switch context.
		/// </summary>
		/// <value><c>true</c> if the context is associated with the left switch context,
		/// otherwise <c>false</c>.</value>
		bool IsMiddleWithLeftSwitch
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the structural change index that the specified context is part of.
		/// </summary>
		/// <param name="ctxtHvo">The context HVO.</param>
		/// <returns>The structural change index.</returns>
		int GetStrucChangeIndex(int ctxtHvo);
	}

	public partial interface IPhContextOrVar
	{
		/// <summary>
		/// Gets the HVO of rule that contains this context.
		/// </summary>
		/// <value>The rule HVO.</value>
		int Rule
		{
			get;
		}
	}

	public partial interface IMoMorphData
	{
		/// <summary>
		/// Gets or sets the active parser.
		/// </summary>
		/// <value>The active parser.</value>
		string ActiveParser
		{
			get;
			set;
		}
	}
}
