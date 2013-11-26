// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2008' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoInterfaceAdditions.cs
// Responsibility: FW Team
//
// <remarks>
// Additions to FDO model interfaces go here.
// One needs to be careful adding new stuff, just because it can be done.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Specialized;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

// Add additional methods/properties to domain object in this file.
// Add new interfaces to the FdoInterfaceDeclarations.cs file.
namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Non-model interface additions for ICmObject.
	/// </summary>
	public partial interface ICmObject : ICmObjectOrId
	{
		///<summary>
		/// Name of the class.
		///</summary>
		string ClassName
		{
			get;
		}

		/// <summary>
		/// Delete the recipient object.
		/// </summary>
		void Delete();

		/// <summary>
		/// Retrieves a service locator for various services related to the data store this object belongs to.
		/// Review team (JohnT): would it be clearer to call this ServiceLocator? Is the extra clarity worth the extra length?
		/// </summary>
		IFdoServiceLocator Services { get; }

		/// <summary>
		/// Retrieve the closest owner, if any, of the specified class; if there is none answer null.
		/// </summary>
		ICmObject OwnerOfClass(int clsid);

		/// <summary>
		/// Retrieve the closest owner, if any, of the specified class; if there is none answer null.
		/// </summary>
		T OwnerOfClass<T>() where T : ICmObject;

		/// <summary>
		/// This is useful for the purpose of creating a 'virtual' attribute which displays the same object as if it
		/// were one of its own properties.
		/// </summary>
		ICmObject Self
		{
			get;
		}

		/// <summary>
		/// Determine if the object satisfies constraints imposed by the class
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="createAnnotation">if set to <c>true</c>, an annotation will be created.</param>
		/// <param name="failure">an explanation of what constraint failed, if any.
		/// Will be null if the method returns true.</param>
		/// <returns>true if the object is all right</returns>
		bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gives an object an opportunity to do any class-specific side-effect work when it has
		/// been cloned with DomainServices.CopyObject. CopyObject will call this method on each
		/// source object it copies after the copy is complete. The copyMap contains the source
		/// object Hvo as the Key and the copied object as the Value.
		/// </summary>
		/// <param name="copyMap"></param>
		/// ------------------------------------------------------------------------------------
		void PostClone(Dictionary<int, ICmObject> copyMap);

		/// <summary>
		/// Add to the collector all the objects to which you have references.
		/// </summary>
		void AllReferencedObjects(List<ICmObject> collector);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="propsToMonitor">hvo, flid pairs which should be monitored, since if they change, the outcome may change</param>
		/// <remarks>e.g. "color" would not be relevant on a part of speech, ever.
		/// e.g.  MoAffixForm.inflection classes are only relevant if the MSAs of the
		/// entry include an inflectional affix MSA.
		/// </remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor);

		/// <summary>
		/// Return true if possibleOwner is one of the owners of 'this'.
		/// (Returns false if possibleOwner is null.)
		/// </summary>
		bool IsOwnedBy(ICmObject possibleOwner);
		/// ------------------------------------------------------------------------------------
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
		/// Get the index of this object in the owner's collection
		/// </summary>
		/// <returns>Index in owner's collection, or -1 if not in collection.</returns>
		int IndexInOwner
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		IEnumerable<ICmObject> ReferenceTargetCandidates(int flid);

		/// <summary>
		/// Determine whether the object is valid
		/// (i.e., has its cache (not disposed) and an hvo greater than zero).
		/// </summary>
		bool IsValidObject { get; }

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
		/// <param name="objSrc"></param>
		void MergeObject(ICmObject objSrc);

		/// <summary>
		///
		/// </summary>
		/// <param name="objSrc"></param>
		/// <param name="fLoseNoStringData"></param>
		void MergeObject(ICmObject objSrc, bool fLoseNoStringData);

		/// <summary>
		/// Gets delete status for the object.
		/// True means it can be deleted, otherwise false.
		/// </summary>
		bool CanDelete
		{
			get;
		}

		/// <summary>
		/// Gets the shortest, non-abbreviated label for the content of this object.
		/// This is the name that you would want to show up in a chooser list.
		/// </summary>
		string ShortName
		{
			get;
		}

		/// <summary>
		/// This name is used in the FdoBrowser. It needs to identify objects unambiguously.
		/// </summary>
		ITsString ObjectIdName { get; }

		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property,
		/// if they want to show something other than
		/// the regular ShortName string.
		/// </remarks>
		ITsString ShortNameTSS
		{
			get;
		}

		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmaion dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		ITsString DeletionTextTSS
		{
			get;
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than
		/// the regular ShortNameTSS string.
		/// </remarks>
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
		/// Gets the writing system for sorting a list of ShortNames.
		/// </summary>
		string SortKeyWs
		{
			get;
		}

		/// <summary>
		/// Gets the sort key for sorting a list of ShortNames.
		/// </summary>
		int SortKey2
		{
			get;
		}

		/// <summary>
		/// Get an alphabetic version of SortKey2. This should always be used when appending
		/// to another string sort key, so as to get the right order for values greater than 9.
		/// </summary>
		string SortKey2Alpha
		{
			get;
		}

		/// <summary>
		/// Get a set of all the objects which refer to this one (in reference properties, not owning ones).
		/// </summary>
		HashSet<ICmObject> ReferringObjects
		{
			get;
		}

		/// <summary>
		/// The objects directly owned by this one.
		/// </summary>
		IEnumerable<ICmObject> OwnedObjects { get; }

		/// <summary>
		/// The objects owned directly or indirectly by this one.
		/// </summary>
		IEnumerable<ICmObject> AllOwnedObjects { get; }
	}

	/// <summary>
	/// Interface that allows several classes to be combined into one signature,
	/// so the signature need not be ICmObject.
	/// </summary>
	public interface IAnalysis : ICmObject
	{
		/// <summary>
		/// Get the WfiWordform.
		/// </summary>
		IWfiWordform Wordform
		{ get; }

		/// <summary>
		/// Returns true if the analysis is or is owned by a wordform. (Not a punctuation form.)
		/// </summary>
		bool HasWordform { get; }

		/// <summary>
		/// The associated WfiAnalysis, if any; returns null for WfiWordform, this for WfiAnalysis, owner for WfiGloss.
		/// </summary>
		IWfiAnalysis Analysis { get; }

		/// <summary>
		/// The form of the analysis, in the specified writing system if that is relevant.
		/// This is the form of the wordform, for everything except punctuationForm.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		ITsString GetForm(int ws);
	}

	/// <summary>
	/// Interface that multiple diverse classes with similar functions implement to group their
	/// shared functions. For now: TextTag and ConstChartWordGroup
	/// </summary>
	public interface IAnalysisReference : ICmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an AnalysisOccurrence equivalent to the end point of this reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		AnalysisOccurrence EndRef();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an AnalysisOccurrence equivalent to the begin point of this reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		AnalysisOccurrence BegRef();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if reference targets valid Segments and Analysis indices.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsValidRef { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change reference to a different Segment object. Used by AnalysisAdjuster and
		/// ReferenceAdjusterService.
		/// </summary>
		/// <param name="newSeg"></param>
		/// <param name="fBegin">True if BeginSegment is affected.</param>
		/// <param name="fEnd">True if EndSegment is affected.</param>
		/// ------------------------------------------------------------------------------------
		void ChangeToDifferentSegment(ISegment newSeg, bool fBegin, bool fEnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change reference to a different AnalysisIndex. Used by AnalysisAdjuster and
		/// ReferenceAdjusterService.
		/// </summary>
		/// <param name="newIndex">change index to this</param>
		/// <param name="fBegin">True if BeginAnalysisIndex is affected.</param>
		/// <param name="fEnd">True if EndAnalysisIndex is affected.</param>
		/// ------------------------------------------------------------------------------------
		void ChangeToDifferentIndex(int newIndex, bool fBegin, bool fEnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the words (as AnalysisOccurrence objects) referenced by the current object.
		/// Returns an empty list if it can't find any words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<AnalysisOccurrence> GetOccurrences();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if this reference occurs after the parameter's reference in the text.
		/// </summary>
		/// <param name="otherReference"></param>
		/// ------------------------------------------------------------------------------------
		bool IsAfter(IAnalysisReference otherReference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the end to the next Analysis, if possible.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		bool GrowFromEnd(bool fignorePunct);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the beginning to the previous Analysis, if
		/// not already at the beginning of the text.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		bool GrowFromBeginning(bool fignorePunct);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the end to the previous Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its endpoint
		/// reaches an Analysis that has a wordform (Wfic).</param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		bool ShrinkFromEnd(bool fignorePunct);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the beginning to the next Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		bool ShrinkFromBeginning(bool fignorePunct);
	}

	///<summary>
	/// Interface of common implementation methods for different kinds of IAnalysisReference
	/// objects. Implemented by ReferenceAdjusterService in DomainServices.
	///</summary>
	internal interface IReferenceAdjuster
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the end to the next Analysis, if possible.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its endpoint
		/// reaches an Analysis that has a wordform (Wfic).</param>
		/// <param name="reference"></param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		bool GrowFromEnd(bool fignorePunct, IAnalysisReference reference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the beginning to the previous Analysis, if
		/// not already at the beginning of the text.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <param name="reference"></param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		bool GrowFromBeginning(bool fignorePunct, IAnalysisReference reference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the end to the previous Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <param name="reference"></param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		bool ShrinkFromEnd(bool fignorePunct, IAnalysisReference reference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the beginning to the next Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <param name="reference"></param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		bool ShrinkFromBeginning(bool fignorePunct, IAnalysisReference reference);
	}

	public partial interface IConstChartClauseMarker
	{
		///<summary>
		/// Returns true if WordGroup property contains a valid reference
		/// (i.e. is not null)
		///</summary>
		bool HasValidRefs { get; }
	}

	public partial interface IConstChartMovedTextMarker
	{
		///<summary>
		/// Returns true if WordGroup property contains a valid reference
		/// (i.e. is not null)
		///</summary>
		bool HasValidRef { get; }
	}

	public partial interface IConstChartWordGroup : IAnalysisReference
	{
		/// <summary>
		/// Get all of the Analyses associated with a single ConstChartWordGroup.
		/// Returns null if there is a problem finding them. Includes PunctuationForms.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IAnalysis> GetAllAnalyses();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares WordGroups in two segments to see if they are similar enough to be considered
		/// the same. Mostly designed for testing. Tests wordforms tagged for same baseline text
		/// and checks to see that they both reference the same CmPossibility column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsAnalogousTo(IConstChartWordGroup otherTag);
	}

	public partial interface ISegment
	{
		/// <summary>
		/// Answer the end of the segment. This is either the end of the paragraph or the start of the next segment.
		/// </summary>
		int EndOffset { get; }

		/// <summary>
		/// The text of the segment, that is, the part of the owning StTxtPara's contents that this segment covers.
		/// </summary>
		ITsString BaselineText { get; }

		/// <summary>
		/// Shortcut, saves Casts and may help if change the model again.
		/// </summary>
		IStTxtPara Paragraph { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the length of the segment's baseline text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int Length { get; }

		/// <summary>
		/// Reports true when there is a translation or non-null note.
		/// </summary>
		bool HasAnnotation { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a label segment (i.e. is defined
		/// as having label text)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsLabel { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance contains only a hard line break
		/// character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsHardLineBreak { get; }

		/// <summary>
		/// Collect set of unique wordforms in the segment
		/// </summary>
		void CollectUniqueWordforms(HashSet<IWfiWordform> wordforms);

		/// <summary>
		/// The section of the segment's text represented by the indicated analysis within the Analyses of the segment.
		/// </summary>
		ITsString GetBaselineText(int ianalysis);

		/// <summary>
		/// Insert into the dictionary the offset for each wordform in Analyses, keyed by the analysis.
		/// Offsets are relative to the paragraph, not the segment.
		/// </summary>
		void GetWordformsAndOffsets(Dictionary<IWfiWordform, int> collector);

		/// <summary>
		/// Returns the BeginOffset (relative to the StTxtPara) of the IAnalysis referenced by the given index.
		/// </summary>
		/// <param name="iAnalysis">Index into AnalysesRS</param>
		/// <returns></returns>
		int GetAnalysisBeginOffset(int iAnalysis);

		/// <summary>
		/// Return a list of all the analyses in the segment, with their begin and end offsets (relative to the paragraph).
		/// </summary>
		/// <returns></returns>
		IList<Tuple<IAnalysis, int, int>> GetAnalysesAndOffsets();

		/// <summary>
		/// Find the analysis closest to the specified range of characters (relative to the segment), prefering
		/// the following word if ambiguous.
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="fExactMatch"></param>
		/// <returns></returns>
		AnalysisOccurrence FindWagform(int ichMin, int ichLim, out bool fExactMatch);

		/// <summary>
		/// Answser a list of count occurrences of the specified analysis (and, optionally, its children).
		/// Any more than count are ignored.
		/// </summary>
		List<LocatedAnalysisOccurrence> GetOccurrencesOfAnalysis(IAnalysis analysis, int count, bool includeChildren);
	}

	public partial interface ILexEntryRef
	{
		/// <summary>
		/// This is a virtual property.  It returns the list of all the
		/// MoMorphoSyntaxAnalysis objects used by top-level senses owned by the owner of this
		/// LexEntryRef.
		/// </summary>
		IEnumerable<IMoMorphSynAnalysis> MorphoSyntaxAnalyses { get; }

		/// <summary>
		/// If this entryref is a complex one, the entries (or senses) under which its owner's full entry is actually published.
		/// Typically these are its PrimaryLexemes. However, if any of those are themselves complex forms,
		/// show their PrimaryEntryRoots, so that we end up with the top-level form that indicates the
		/// actual place to look in the dictionary.
		/// </summary>
		IEnumerable<ILexEntry> PrimaryEntryRoots { get; }

		/// <summary>
		/// This is the same as PrimaryEntryRoots, except that if the only Component is (or is a sense of) the only PrimaryEntryRoot,
		/// it produces an empty list.
		/// </summary>
		IEnumerable<ILexEntry> NonTrivialEntryRoots { get; }
	}

	public partial interface ILexReference
	{
		/// <summary>
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		string LiftResidueContent
		{ get; }

		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		string LiftDateCreated
		{ get; }

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		string LiftDateModified
		{ get; }

		/// <summary>
		/// Return the desired abbreviation for the owning type.
		/// </summary>
		/// <param name="ws">writing system id</param>
		/// <param name="member">The reference member which needs the abbreviation</param>
		string TypeAbbreviation(int ws, ICmObject member);

		/// <summary>
		/// Return the 1-based index of the member in the relation if relevant, otherwise 0.
		/// </summary>
		/// <param name="hvoMember"></param>
		/// <returns></returns>
		int SequenceIndex(int hvoMember);
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
	public partial interface ILangProject : IWritingSystemContainer
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
		IFdoOwningSequence<ICmPossibility> ScriptureAnnotationDfns
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a plausible guess for interpreting the "magic" ws as a real ws.  An invalid
		/// "magic" ws returns itself.
		/// </summary>
		/// <param name="wsMagic">The ws magic.</param>
		/// ------------------------------------------------------------------------------------
		int DefaultWsForMagicWs(int wsMagic);

		/// <summary>
		/// Get the default Constituent Chart template (creating it and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		ICmPossibility GetDefaultChartTemplate();

		/// <summary>
		/// Creates a Constituent Chart template (and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		ICmPossibility CreateChartTemplate(XmlNode spec);

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
		/// Get the default Text Tagging tags list (creating it and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		ICmPossibilityList GetDefaultTextTagList();

		/// <summary>
		/// Creates a list of Text Tagging tags (and any superstructure to hold it as needed).
		/// </summary>
		/// <returns></returns>
		ICmPossibilityList MakeTextTagsList(string xml);

		/// <summary>
		/// Virtual list of texts. Replaces TextsOC now that Text objects are unowned.
		/// </summary>
		IList<IText> Texts { get; }

		/// <summary>
		/// Virtual list of texts that can be interlinearized. Combines Texts and Scripture
		/// </summary>
		IList<IStText> InterlinearTexts { get; }

		/// <summary>
		/// Get all the parts of speech as a flat list.
		/// </summary>
		List<IPartOfSpeech> AllPartsOfSpeech
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the analyzing agent representing the current parser
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="KeyNotFoundException"/>
		/// ------------------------------------------------------------------------------------
		ICmAgent DefaultParserAgent
		{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the analyzing agent representing the current user
		/// </summary>
		/// <returns>a CmAgent object</returns>
		/// <exception cref="KeyNotFoundException"/>
		/// ------------------------------------------------------------------------------------
		ICmAgent DefaultUserAgent
		{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the analyzing agent representing the computer.
		/// Do not use this for the parser;
		/// there is a dedicated agent for that purpose.
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="KeyNotFoundException">
		/// There was no default computer agent.
		/// This should be part of the NewLangProj.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		ICmAgent DefaultComputerAgent
		{ get; }

		/// <summary>
		///
		/// </summary>
		ICmAgent ConstraintCheckerAgent
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
	}

	/// <summary>
	/// Non-model interface additions for ILexDb.
	/// </summary>
	public partial interface ILexDb
	{
		/// <summary>
		/// The main list of lexical entries. Equivalent to ILexEntryRepository.AllInstances().
		/// </summary>
		IEnumerable<ILexEntry> Entries { get; }

		/// <summary>
		/// used when dumping the lexical database for the automated Parser
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		IEnumerable<IMoForm> AllAllomorphs
		{
			get;
		}

		/// <summary>
		/// Resets the homograph numbers for all entries.
		/// </summary>
		void ResetHomographNumbers(IProgress progressBar);

		/// <summary>
		/// Allows user to convert LexEntryType to LexEntryInflType.
		/// </summary>
		void ConvertLexEntryInflTypes(IProgress progressBar, IEnumerable<ILexEntryType> list);

		/// <summary>
		/// Allows user to convert LexEntryInflType to LexEntryType.
		/// </summary>
		void ConvertLexEntryTypes(IProgress progressBar, IEnumerable<ILexEntryType> list);
		/// <summary>
		/// used when dumping the lexical database for the automated Parser
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		IEnumerable<IMoMorphSynAnalysis> AllMSAs
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		List<IReversalIndex> CurrentReversalIndices
		{
			get;
		}
	}

	/// <summary>
	/// Non-model interface additions for ILexExampleSentence.
	/// </summary>
	public partial interface ILexExampleSentence
	{
		/// <summary>
		/// The publications from which this is not excluded, that is, the ones in which it
		/// SHOULD be published.
		/// </summary>
		IFdoSet<ICmPossibility> PublishIn { get; }
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
		ITsString FullReferenceName
		{
			get;
		}

		/// <summary>
		/// The publications from which this is not excluded, that is, the ones in which it
		/// SHOULD be published.
		/// </summary>
		IFdoSet<ICmPossibility> PublishIn { get; }

		/// <summary>
		///
		/// </summary>
		List<ILexSense> AllSenses
		{
			get;
		}

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// PrimaryLexemes field and that is a complex entry type.
		/// </summary>
		IEnumerable<ILexEntry> ComplexFormEntries { get; }

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
		/// The outline number used to label senses.
		/// </summary>
		ITsString LexSenseOutline { get; }

		/// <summary>
		/// Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses.
		/// </summary>
		/// <param name="wsVern"></param>
		ITsString OwnerOutlineNameForWs(int wsVern);

		/// <summary>
		/// A version of OwnerOutlineName that is configurable with a modified homograph number
		/// and to indicate whether it is for a dictionary or reversal view.
		/// </summary>
		ITsString OwnerOutlineNameForWs(int wsVern, int hn, HomographConfiguration.HeadwordVariant hv);

		/// <summary>
		/// Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses and if configured to show sense number in reversals.
		/// </summary>
		ITsString ReversalNameForWs(int wsVern);

		/// <summary>
		/// Generate an id string like "colorful_7ee714ef-2744-4fc2-b407-aab54e66a76f".
		/// If there's a LIFTid element in ImportResidue, use that instead.
		/// </summary>
		string LIFTid
		{ get; }

		/// <summary>
		/// Resets the MSA to an equivalent MSA, whether it finds it, or has to create a new one.
		/// </summary>
		SandboxGenericMSA SandboxMSA
		{ set; }

		/// <summary>
		/// Returns the TsString that represents the LongName of this object.
		/// </summary>
		ITsString LongNameTSS { get; }

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
		/// <param name="newHvos">Set of new senses (including hvoSense).</param>
		bool RDEMergeSense(int hvoDomain, Set<int> newHvos);

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of object ids for
		/// all the LexReferences that contain this LexSense/LexEntry.
		/// Note this is called on SFM export by mdf.xml so needs to be a property.
		/// </summary>
		IEnumerable<ILexReference> LexSenseReferences { get; }
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
		IEnumerable<ILexEntryRef> VariantFormEntryBackRefs { get; }

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

		/// <summary>
		/// Finds the matching variant entry backref.
		/// </summary>
		/// <param name="variantEntryType">Type of the variant entry.</param>
		/// <param name="targetVariantLexemeForm">The target variant lexeme form.</param>
		/// <returns></returns>
		ILexEntryRef FindMatchingVariantEntryBackRef(ILexEntryType variantEntryType, ITsString targetVariantLexemeForm);
	}

	/// <summary>
	/// Non-model interface additions for ILexEntry.
	/// </summary>
	public partial interface ILexEntry : IVariantComponentLexeme
	{
		/// <summary>
		/// Make the other lexentry or sense a component of this. This becomes a complex form if it is not already.
		/// If it already has components other is added and primary lexemes is not affected.
		/// If it has no components other is also put in primary lexemes.
		/// </summary>
		void AddComponent(ICmObject other);

		/// <summary>
		/// Determines if the entry is a circumfix
		/// </summary>
		/// <returns></returns>
		bool IsCircumfix();

		/// <summary>
		/// Answer true if the entry is, directly or indirectly, a component of this entry.
		/// The intent is to use this in reporting that it would be incorrect to make this
		/// a component of the specified entry.
		/// Accordingly, as a special case, it answers true if the entry is the recipient,
		/// even if it has no components.
		/// </summary>
		bool IsComponent(ILexEntry entry);

		/// <summary>
		/// Gets the complex form entries, that is, the entries which should be shown
		/// in the complex forms list for this entry in data entry view.
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// ComponentLexemes field and that is a complex entry type.
		/// </summary>
		IEnumerable<ILexEntry> ComplexFormEntries { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the complex form entries, that is, the entries which should be shown
		/// in the complex forms list for this entry in stem-based view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<ILexEntry> VisibleComplexFormEntries { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the subentries of this entry, that is, the entries which should be shown
		/// as subentries (paragraphs usually indented) under this entry in root-based view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<ILexEntry> Subentries { get; }

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		IMoMorphSynAnalysis FindOrCreateDefaultMsa();

		/// <summary>
		/// Conceptually, this answers AllSenses.Count > 1.
		/// However, it is vastly more efficient, especially when doing a lot of them
		/// and everything is preloaded or the cache is in kalpLoadForAllOfObjectClass mode.
		/// </summary>
		bool HasMoreThanOneSense { get; }

		/// <summary>
		/// Get the number of senses for the LexEntry. Includes subsenses.
		/// </summary>
		int NumberOfSensesForEntry
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ls"></param>
		void MoveSenseToCopy(ILexSense ls);

		/// <summary>
		/// Check whether this entry should be published as a minor entry.
		/// </summary>
		/// <returns></returns>
		bool PublishAsMinorEntry { get; }

		/// <summary>
		/// The publications from which this is not excluded, that is, the ones in which it
		/// SHOULD be published.
		/// </summary>
		IFdoSet<ICmPossibility> PublishIn { get; }

		/// <summary>
		/// The publications from which this is not excluded as a headword, that is, the ones
		/// in which it SHOULD be published as a headword.
		/// </summary>
		IFdoSet<ICmPossibility> ShowMainEntryIn { get; }

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
		string HomographFormKey
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
		IMoMorphType PrimaryMorphType
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		bool IsMorphTypesMixed
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
		/// Return the sense with the specified MSA
		/// </summary>
		ILexSense SenseWithMsa(IMoMorphSynAnalysis msa);

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the LexEntryRef
		/// objects owned by this LexEntry that define this entry as a variant (that is, RefType is krtVariant).
		/// (I think it has PropChanged support...variants list in one entry updates if a variant is made in another)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<ILexEntryRef> VariantEntryRefs { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the LexEntryRef
		/// objects owned by this LexEntry that define this entry as a complex form (that is, RefType is krtComplexForm).
		/// (no PropChanged support yet).
		/// Currently there will be at most one such entry ref; it is used mainly to decide
		/// whether to display the component forms ghost field. (This means it is called by
		/// reflection from a request in XML; please do not remove it just because there are
		/// no direct C# callers.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<ILexEntryRef> ComplexFormEntryRefs { get; }

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own LexEntryRef objects that refer to this LexSense as a
		/// variant (component).
		/// </summary>
		IEnumerable<ILexEntry> VariantFormEntries { get; }

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

		/// <summary>
		/// The canonical unique name of a lexical entry.  This includes
		/// CitationFormWithAffixType (in this implementation) with the homograph number
		/// (if non-zero) appended as a subscript.
		/// </summary>
		ITsString HeadWordForWs(int wsVern);

		/// <summary>
		/// The name of a lexical entry as used in cross-refs in the dictionary view.
		/// </summary>
		ITsString HeadWordRefForWs(int wsVern);

		/// <summary>
		/// The name of a lexical entry as used in cross-refs in the reversls view.
		/// </summary>
		ITsString HeadWordReversalForWs(int wsVern);

		/// <summary>
		/// Generate an id string like "colorful_7ee714ef-2744-4fc2-b407-aab54e66a76f".
		/// If there's a LIFTid element in LiftResidue (or ImportResidue), use that instead.
		/// </summary>
		string LIFTid
		{ get; }

		/// <summary>
		/// Determines whether the entry is in a variant relationship with the given sense (or its entry).
		/// </summary>
		/// <param name="senseTargetComponent">the sense of which we are possibly a variant. If we aren't a variant of the sense,
		/// we will try to see if we are a variant of its owner entry</param>
		/// <param name="matchinEntryRef">if we found a match, the first (and only) ComponentLexeme will have matching sense or its owner entry.</param>
		/// <returns></returns>
		bool IsVariantOfSenseOrOwnerEntry(ILexSense senseTargetComponent, out ILexEntryRef matchinEntryRef);

		/// <summary>
		/// Create stem MSAs to replace affix MSAs, and/or create affix MSAs to replace stem
		/// MSAs. This is harder than it looks, since references to MSAs can occur in several
		/// places, and all of them need to be updated.
		/// </summary>
		/// <param name="rgmsaOld">list of bad MSAs which need to be replaced</param>
		void ReplaceObsoleteMsas(List<IMoMorphSynAnalysis> rgmsaOld);

		/// <summary>
		/// Find a LexEntryRef matching the given targetComponent (exlusively), and variantEntryType.
		/// If we can't match on variantEntryType, we'll just return the reference with the matching component.
		/// </summary>
		/// <param name="targetComponent">match on the LexEntryRef that contains this, and only this component.</param>
		/// <param name="variantEntryType"></param>
		/// <returns></returns>
		ILexEntryRef FindMatchingVariantEntryRef(IVariantComponentLexeme targetComponent, ILexEntryType variantEntryType);

		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of object ids for
		/// all the LexReferences that contain this LexSense/LexEntry.
		/// Note this is called on SFM export by mdf.xml so needs to be a property.
		/// </summary>
		IEnumerable<ILexReference> LexEntryReferences { get; }
	}

	/// <summary>
	/// Non-model interface additions for ILexEntryType.
	/// </summary>
	public partial interface ILexEntryType
	{
		/// <summary>
		/// Convert one LexEntryType to another LexEntryType
		/// </summary>
		/// <param name="lexEntryType"> </param>
		void ConvertLexEntryType(ILexEntryType lexEntryType);
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
		/// Switches select inbound references from the sourceMsa to 'this'.
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
		ITsString FeaturesTSS { get; }

		/// <summary>
		///
		/// </summary>
		ITsString ExceptionFeaturesTSS { get; }

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
		ITsString LongNameAdHocTs
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		string LongNameAdHoc
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="msa"></param>
		/// <returns></returns>
		bool EqualsMsa(IMoMorphSynAnalysis msa);

		/// <summary>
		///
		/// </summary>
		/// <param name="msa"></param>
		/// <returns></returns>
		bool EqualsMsa(SandboxGenericMSA msa);

		/// <summary>
		///
		/// </summary>
		/// <param name="sandboxMsa"></param>
		/// <returns></returns>
		IMoMorphSynAnalysis UpdateOrReplace(SandboxGenericMSA sandboxMsa);
	}

	/// <summary>
	/// Non-model interface additions for ICmPossibilityList.
	/// </summary>
	public partial interface ICmPossibilityList
	{
		/// <summary>
		/// Get all possibilities, recursively, that are ultimately owned by the list.
		/// </summary>
		Set<ICmPossibility> ReallyReallyAllPossibilities
		{
			get;
		}

		/// <summary>
		/// Get a string showing the type of writing system to use.
		/// N.B. For use with lists of Custom items.
		/// </summary>
		/// <returns></returns>
		string GetWsString();

		/// <summary>
		///
		/// </summary>
		/// <param name="stringTbl"></param>
		/// <returns></returns>
		string ItemsTypeName(StringTable stringTbl);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Using the possibility name and writing system, find a possibility, or create a new
		/// one if it doesn't exist.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		ICmPossibility FindOrCreatePossibility(string possibilityPath, int ws);

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
		ICmPossibility FindOrCreatePossibility(string possibilityPath, int ws, bool fFullPath);
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

		/// <summary>
		/// If the recipient is a column in a chart that shouldn't be moved or deleted, report
		/// accordingly and return true. Return false if OK to delete or move.
		/// </summary>
		/// <returns></returns>
		bool CheckAndReportProtectedChartColumn();

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
		/// Gets a value indicating whether this instance is a stem type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a stem type; otherwise, <c>false</c>.
		/// </value>
		bool IsStemType
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a bound type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a bound type; otherwise, <c>false</c>.
		/// </value>
		bool IsBoundType
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is an affix type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is an affix type; otherwise, <c>false</c>.
		/// </value>
		bool IsAffixType
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a prefix type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a prefix type; otherwise, <c>false</c>.
		/// </value>
		bool IsPrefixishType
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a suffix type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a suffix type; otherwise, <c>false</c>.
		/// </value>
		bool IsSuffixishType
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool IsAmbiguousWith(IMoMorphType other);

		/// <summary>
		///
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		string FormWithMarkers(string form);

		/// <summary>
		/// Return the number of unique LexEntries that reference this MoMorphType via MoForm.
		/// </summary>
		int NumberOfLexEntries
		{
			get;
		}
	}

	/// <summary>
	/// Non-model interface additions for IMoStemMsa.
	/// </summary>
	public partial interface IMoStemMsa
	{
		/// <summary>
		/// Set your MsFeatures to a copy of the source object.
		/// </summary>
		void CopyMsFeatures(IFsFeatStruc source);
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
		/// <param name="entries">An array which must contain IReversalIndexEntry objects</param>
		/// <returns>A List of IReversalIndexEntry instances that match any of the entries in the input array.</returns>
		List<IReversalIndexEntry> EntriesForSense(List<IReversalIndexEntry> entries);

		/// <summary>
		///
		/// </summary>
		List<IReversalIndexEntry> AllEntries
		{
			get;
		}

		/// <summary>
		/// Given a string in the form produced by the ReversalIndexEntry.LongName function (names of parents
		/// separated by commas), find or create the child specified.
		/// </summary>
		IReversalIndexEntry FindOrCreateReversalEntry(string longName);
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
		/// Get the set of senses that refer to this reversal entry.
		/// </summary>
		IEnumerable<ILexSense> ReferringSenses { get; }

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

		/// <summary>
		///
		/// </summary>
		List<IReversalIndexEntry> AllEntries
		{
			get;
		}

		/// <summary>
		/// If this is top-level entry, the same as the ShortName.  If it's a subentry,
		/// then a colon-separated list of names from the root entry to this one.
		/// </summary>
		string LongName { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IMoForm
	{
		/// <summary>
		/// An MoForm is considered complete if it has a complete set of forms in all current vernacular writing systems.
		/// </summary>
		bool IsComplete { get; }

		/// <summary>
		/// Swap all references to this MoForm to use the new one
		/// </summary>
		/// <param name="newForm">the new MoForm</param>
		void SwapReferences(IMoForm newForm);

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

		/// <summary>
		/// Gets all morph type reference target candidates.
		/// </summary>
		/// <returns></returns>
		IEnumerable<ICmObject> GetAllMorphTypeReferenceTargetCandidates();

		/// <summary>
		/// Return a marked form in the desired writing system.
		/// </summary>
		string GetFormWithMarkers(int ws);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IMoInflAffixTemplate : ICloneableCmObject
	{
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IMoInflAffixSlot
	{
		/// <summary>
		///
		/// </summary>
		IEnumerable<IMoInflAffMsa> Affixes { get; }
		/// <summary>
		/// Get a list of inflectional affix LexEntries which do not already refer to this slot
		/// </summary>
		IEnumerable<ILexEntry> OtherInflectionalAffixLexEntries { get; }
	}

	public partial interface IFsFeatureSystem
	{
		/// <summary>
		/// Gets the symbolic value with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The symbolic value, or <c>null</c> if not found.</returns>
		IFsSymFeatVal GetSymbolicValue(string id);

		/// <summary>
		/// Gets the feature with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The feature, or <c>null</c> if not found.</returns>
		IFsFeatDefn GetFeature(string id);

		/// <summary>
		/// Gets the type with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The type, or <c>null</c> if not found.</returns>
		IFsFeatStrucType GetFeatureType(string id);

		/// <summary>
		/// Add a feature to the feature system (unless it's already there)
		/// </summary>
		/// <param name="item">the node containing a description of the feature</param>
		/// <returns></returns>
		IFsFeatDefn AddFeatureFromXml(XmlNode item);

		/// <summary>
		/// Gets feature based on XML or creates it if not found
		/// </summary>
		/// <param name="item">Xml description of the item.</param>
		/// <param name="fst">the type</param>
		/// <returns></returns>
		IFsFeatDefn GetOrCreateFeatureFromXml(XmlNode item, IFsFeatStrucType fst);

		/// <summary>
		/// Gets a feature structure type or creates it if not already there.
		/// </summary>
		/// <param name="item">Xml description of the item.</param>
		/// <returns>
		/// FsFeatStrucType corresponding to the type
		/// </returns>
		IFsFeatStrucType GetOrCreateFeatureTypeFromXml(XmlNode item);

		/// <summary>
		/// Gets a complex feature or creates it if not already there; use XML item to do it
		/// </summary>
		/// <param name="item">XML item</param>
		/// <param name="fst">feature structure type which refers to this complex feature</param>
		/// <param name="complexFst">feature structure type of the complex feature</param>
		/// <returns>
		/// IFsComplexFeature corresponding to the name
		/// </returns>
		IFsComplexFeature GetOrCreateComplexFeatureFromXml(XmlNode item, IFsFeatStrucType fst,
			IFsFeatStrucType complexFst);

		/// <summary>
		/// Gets a close feature or creates it if not already there; use XML item to do it
		/// </summary>
		/// <param name="item">XML item</param>
		/// <returns>
		/// FsClosedFeature corresponding to the name
		/// </returns>
		IFsClosedFeature GetOrCreateClosedFeatureFromXml(XmlNode item);
	}

	public partial interface IFsFeatStrucType
	{
		/// <summary>
		/// Gets the feature with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The feature, or <c>null</c> if not found.</returns>
		IFsFeatDefn GetFeature(string id);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsFeatureSpecification : ICloneableCmObject
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
	public partial interface IFsAbstractStructure : ICloneableCmObject
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
	public partial interface IFsFeatStruc : ICloneableCmObject
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="item"></param>
		/// <param name="featsys"></param>
		void AddFeatureFromXml(XmlNode item, IFsFeatureSystem featsys);


		/// <summary>
		///
		/// </summary>
		/// <param name="closedFeat"></param>
		/// <returns></returns>
		IFsClosedValue GetValue(IFsClosedFeature closedFeat);

		/// <summary>
		///
		/// </summary>
		/// <param name="closedFeat"></param>
		/// <returns></returns>
		IFsClosedValue GetOrCreateValue(IFsClosedFeature closedFeat);

		/// <summary>
		///
		/// </summary>
		/// <param name="complexFeat"></param>
		/// <returns></returns>
		IFsComplexValue GetValue(IFsComplexFeature complexFeat);

		/// <summary>
		///
		/// </summary>
		/// <param name="complexFeat"></param>
		/// <returns></returns>
		IFsComplexValue GetOrCreateValue(IFsComplexFeature complexFeat);

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
		string LongNameSorted
		{
			get;
		}

		/// <summary>
		/// Adds additional information to LongName for LIFT export purposes.
		/// </summary>
		string LiftName
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
		ITsString LongNameSortedTSS
		{
			get;
		}

		/// <summary>
		/// Merge one feature structure into another;
		/// when the new has the same feature as the old, use the new
		/// </summary>
		/// <param name="fsNew">the new feature structure</param>
		void PriorityUnion(IFsFeatStruc fsNew);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IFsFeatStrucDisj : ICloneableCmObject
	{
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
	public partial interface ICmBaseAnnotation
	{
		/// <summary>
		/// Answer the string that is actually annotated: the range from BeginOffset to EndOffset in
		/// property Flid (and possibly alternative WsSelector) of BeginObject.
		/// If Flid has not been set and BeginObject is an StTxtPara assumes we want the Contents.
		/// Does not attempt to handle a case where EndObject is different.
		/// </summary>
		/// <returns></returns>
		ITsString TextAnnotated
		{ get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ICmAgent
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an evaluation for the given object.
		/// Note that for no opinion (2), the CmAgentEvaluation is removed from
		/// the evaluations of the analysis. Otherwise any existing evaluations by this agent
		/// are removed from the WfiAnalysis.Evaluations, and if necessary this.Approves
		/// or this.Disapproves is created, and the appropriate one added. Noopinion is
		/// expressed by having no evaluation by this agent in the evaluations collection.
		/// </summary>
		/// <param name="target">
		/// The object we are evaluating.
		/// Currently it must be a WfiAnalysis.
		/// </param>
		/// <param name="opinion">disapproves (0), approves (1), noopinion (2)</param>
		/// <remarks>
		/// Note: This method is the C# port of the "SetAgentEval" Stored Procedure.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		void SetEvaluation(ICmObject target, Opinions opinion);
	}
	public partial interface ICmAgentEvaluation : ICmObject
	{
		/// <summary>
		/// Return true if this evaluation represents approval for its owning agent.
		/// </summary>
		bool Approves { get; }

		/// <summary>
		/// The owning agent is human.
		/// </summary>
		bool Human { get; }
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a match criteria that will match on an empty object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetEmptyObjectMatchCriteria();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save this set of match criteria.
		/// </summary>
		/// <param name="matchVal">Match value</param>
		/// <param name="fIncludeSubitems">Indicates whether to include the subitems of the
		/// given <paramref name="matchVal"/> when looking for a match</param>
		/// <param name="fMatchEmpty">Indicates whether an empty collection should be counted
		/// as a match</param>
		/// ------------------------------------------------------------------------------------
		void SetObjectMatchCriteria(ICmPossibility matchVal, bool fIncludeSubitems, bool fMatchEmpty);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save this set of match criteria.
		/// </summary>
		/// <param name="comparisonType">Type of camparison</param>
		/// <param name="matchVal">Match value</param>
		/// ------------------------------------------------------------------------------------
		void SetIntegerMatchCriteria(ComparisonTypes comparisonType, int matchVal);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the comparison.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ComparisonTypes ComparisonType { get; }
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
	public partial interface ICmFilter : IFilter // Needed for the IFilter
	{
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPartOfSpeech
	{
		/// <summary>
		/// Gets the highest part of speech.
		/// </summary>
		/// <value>The highest part of speech.</value>
		IPartOfSpeech HighestPartOfSpeech
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		bool RequiresInflection
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="item"></param>
		void AddInflectableFeatsFromXml(XmlNode item);

		/// <summary>
		///
		/// </summary>
		IEnumerable<IMoStemName> AllStemNames
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		IEnumerable<IMoInflClass> AllInflectionClasses
		{
			get;
		}

		/// <summary>
		///
		/// </summary>
		IEnumerable<IMoInflAffixSlot> AllAffixSlots
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

	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiAnalysis : IAnalysis
	{
		 /// <summary>
		/// Agents differ in their opinion of the analysis.
		/// </summary>
		bool HasConflictingEvaluations
		{
			get;
		}

		/// <summary>
		/// Return true if the analysis is considered "Complete", not requiring more attention.
		/// An analysis is complete if it occurs and all its properties are set and complete.
		/// </summary>
		bool IsComplete
		{
			get;
		}
		/// <summary>
		/// Move the text annotations that refence this object or any WfiGlosses it owns up to the owning WfiWordform.
		/// </summary>
		/// <remarks>
		/// Client is responsible for Undo/Redo wrapping.
		/// </remarks>
		void MoveConcAnnotationsToWordform();

		/// <summary>
		/// Collect all the MSAs referenced by the given WfiAnalysis.
		/// </summary>
		/// <param name="msaSet">MSAs found by this call are added to msaSet.</param>
		void CollectReferencedMsas(HashSet<IMoMorphSynAnalysis> msaSet);

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
		List<ISegment> ConcordanceIds
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
		/// Get the number of the analysis within the collection of the owning wordform's human approved analyses.
		/// </summary>
		string HumanApprovedNumber { get; }

		/// <summary>
		/// Get the number of the analysis within the collection of the owning wordform's human no-opinion analyses.
		/// (These will be parser approved, but not human approved.)
		/// </summary>
		string HumanNoOpinionNumber { get; }

		/// <summary>
		/// Get the number of the analysis within the collection of the owning wordform's human disapproved analyses.
		/// </summary>
		string HumanDisapprovedNumber { get; }

		/// <summary>
		/// The segments that reference an occurrence of this analysis in a text.
		/// </summary>
		IEnumerable<ISegment> OccurrencesInTexts
		{
			get;
		}

	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPunctuationForm : IAnalysis
	{

	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiGloss : IAnalysis
	{
		/// <summary>
		///
		/// </summary>
		List<ISegment> ConcordanceIds
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
		/// Return true if the gloss is considered "Complete", not requiring more attention.
		/// A gloss is complete if it has a non-empty form in every current analysis writing system.
		/// </summary>
		bool IsComplete { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiMorphBundle
	{
		/// <summary>
		/// If we have a sense return it; if not, and if we have an MSA, return the first sense (with the right MSA if possible)
		/// from the indicated entry.
		/// </summary>
		ILexSense DefaultSense { get; }
		/// <summary>
		/// Return true if the bundle is considered "Complete", not requiring more attention.
		/// A bundle is complete if it has a sense with glosses for all current analysis WSs, a complete morph, and a complete msa.
		/// </summary>
		bool IsComplete { get;}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IWfiWordform : IAnalysis
	{
		/// <summary>
		/// Return all the parts of speech that occur in analyses of this wordform that are used in texts.
		/// </summary>
		IEnumerable<IPartOfSpeech> AttestedPos { get; }

		/// <summary>
		/// If we can efficiently detect that this wordform is probably a spurious one
		/// created this session, delete it.
		/// </summary>
		void DeleteIfSpurious();

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
		List<ISegment> ConcordanceIds
		{
			get;
		}

		/// <summary>
		/// A full concordance of all occurrences of this wordform.
		/// (This is not used in the real concordance views, since they require filtering by text.)
		/// </summary>
		IEnumerable<ISegment> FullConcordanceIds
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
		/// Return true if the wordform is considered "Complete", not requiring more attention.
		/// A wordform is complete if it has at least one analysis and all analyses are complete.
		/// </summary>
		bool IsComplete { get; }

		/// <summary>
		/// The segments that reference an occurrence of this word in a text.
		/// </summary>
		IEnumerable<ISegment> OccurrencesInTexts
		{
			get;
		}

		/// <summary>
		/// A bag containing the segments where this wordform occurs, once for each time it occurs
		/// in the segment.
		/// </summary>
		IBag<ISegment> OccurrencesBag { get; }

		/// <summary>
		/// True, if user and parser are in agreement on status of all analyses.
		/// </summary>
		bool HumanAndParserAgree
		{
			get;
		}

		/// <summary>
		/// Get a List of Hvos that the human has approved of.
		/// </summary>
		IEnumerable<IWfiAnalysis> HumanApprovedAnalyses
		{
			get;
		}

		/// <summary>
		/// Get a List of Hvos that the human has no opinion on.
		/// </summary>
		IEnumerable<IWfiAnalysis> HumanNoOpinionParses
		{
			get;
		}

		/// <summary>
		/// Get a List of Hvos that the human has DISapproved of.
		/// </summary>
		IEnumerable<IWfiAnalysis> HumanDisapprovedParses
		{
			get;
		}

		/// <summary>
		/// Return all the text genres in which this wordform occurs.
		/// </summary>
		IEnumerable<ICmPossibility> TextGenres { get; }

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
		bool IsFootnoteStyle { get; }

		/// <summary>
		///
		/// </summary>
		bool InUse { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IStText
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified paragraph in the text. If the paragraph doesn't exist
		/// (i.e. index is out of range), then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IStTxtPara this[int i]
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use to test if StText is empty
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsEmpty
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for first footnote reference in the StText.
		/// </summary>
		/// <param name="iPara">0-based index of paragraph where footnote was found</param>
		/// <param name="ich">0-based character offset where footnote ORC was found</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindFirstFootnote(out int iPara, out int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for last footnote reference in the StText.
		/// </summary>
		/// <param name="iPara">0-based index of paragraph where footnote was found</param>
		/// <param name="ich">0-based character offset where footnote ORC was found</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindLastFootnote(out int iPara, out int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the next footnote starting from the given paragraph and position.
		/// </summary>
		/// <param name="iPara">Index of paragraph to start search.</param>
		/// <param name="ich">Character index to start search.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search forwards starting with the
		/// run after ich, otherwise we start with the current run.</param>
		/// <returns>Next footnote in string, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindNextFootnote(ref int iPara, ref int ich, bool fSkipCurrentPosition);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the previous footnote starting from the given paragraph and character position.
		/// </summary>
		/// <param name="iPara">Index of paragraph to start search, or -1 to start search in
		/// last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search backwards starting with the
		/// run before ich, otherwise we start with the run ich is in.</param>
		/// <returns>Last footnote in string, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindPreviousFootnote(ref int iPara, ref int ich, bool fSkipCurrentPosition);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new paragraph to this StText. The created paragraph will be of the correct
		/// type for this StText (i.e. a ScrTxtPara or a StTxtPara)
		/// </summary>
		/// <param name="paraStyleName">The name of the paragraph style to use for the new
		/// paragraph</param>
		/// <returns>The created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		IStTxtPara AddNewTextPara(string paraStyleName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a new paragraph into this StText at the specified position. The created
		/// paragraph will be of the correct type for this StText (i.e. a ScrTxtPara or a
		/// StTxtPara)
		/// </summary>
		/// <param name="iPos">The index to insert the paragraph.</param>
		/// <param name="paraStyleName">The name of the paragraph style to use for the new
		/// paragraph</param>
		/// <returns>The created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		IStTxtPara InsertNewTextPara(int iPos, string paraStyleName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new paragraph at the end of an StText.
		/// </summary>
		/// <param name="paragraphIndex"></param>
		/// <param name="paraStyleName"></param>
		/// <param name="tss"></param>
		/// <returns>The created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		IStTxtPara InsertNewPara(int paragraphIndex, string paraStyleName, ITsString tss);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete a paragraph from this StText deleting any owned objects (from ORCs).
		/// </summary>
		/// <param name="para">paragraph to delete</param>
		/// ------------------------------------------------------------------------------------
		void DeleteParagraph(IStTxtPara para);

		/// <summary>
		/// Virtual Property: The Title for the StStext is usually the Title of its owning Text,
		/// but Scripture has a different strategy.
		/// </summary>
		IMultiAccessorBase Title { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to get Text.Source, if one exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IMultiAccessorBase Source { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the comment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IMultiAccessorBase Comment{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to get Text.Genres
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<ICmPossibility> GenreCategories { get; }

		/// <summary>
		/// The primary writing system of the text, which we use to analyze it.
		/// Text in other writing systems is treated as punctuation.
		/// </summary>
		int MainWritingSystem { get; }

		/// <summary>
		/// Virtual property that flags whether or not this text is a translation.
		/// </summary>
		bool IsTranslation { get; set; }

		/// <summary>
		/// Get set of unique wordforms in this text
		/// </summary>
		/// <returns>A HashSet that contains zero, or more, unique wordforms occurring in this text.</returns>
		HashSet<IWfiWordform> UniqueWordforms();
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Interface used to group the StTxtPara and (non-existent) StTable classes
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public partial interface IStPara
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer true if this is the last paragraph in the text (used for displaying
		/// special marks in some views).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsFinalParaInText { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the paragraph StyleName
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string StyleName { get; set; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ICmTranslation : ICloneableCmObject
	{
	}

	public partial interface IText
	{
		/// <summary>
		/// Associate the text with a (newly created) notebook record. Does nothing if it already is.
		/// </summary>
		void AssociateWithNotebook(bool makeYourOwnUow);

		/// <summary>
		/// Reports the Notebook record associated with this text, or null if there isn't one.
		/// </summary>
		IRnGenericRec AssociatedNotebookRecord { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface ITextTag : IAnalysisReference
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares TextTags in two segments to see if they are similar enough to be considered
		/// the same. Mostly designed for testing. Tests wordforms tagged for same baseline text
		/// and checks to see that they both reference the same CmPossibility tag.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsAnalogousTo(ITextTag otherTag);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IStTxtPara : ICloneableCmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the previous paragraph within the StText, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IStTxtPara PreviousParagraph
		{
			get;
		}

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all pictures "owned" by this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<ICmPicture> GetPictures();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the ORC of the specified picture and deletes it from the paragraph and any
		/// back translations and deletes the object itself.
		/// </summary>
		/// <param name="hvoPic">The HVO of the picture to delete</param>
		/// <returns>The character offset of the location where the ORC was found in this
		/// paragraph for the gievn picture. If not found, returns -1.</returns>
		/// ------------------------------------------------------------------------------------
		int DeletePicture(int hvoPic);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all TextTags that only reference Segments in this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<ITextTag> GetTags();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all ConstChartWordGroups that only reference Segments in this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<IConstChartWordGroup> GetChartCellRefs();

		/// ------------------------------------------------------------------------------------
	   /// <summary>
	   /// Return a Reference (e.g., Scripture reference, or text abbreviation/para #/sentence#) for the specified character
	   /// position (in the whole paragraph), which is assumed to belong to the specified segment.
	   /// (For now, ich is not actually used, but it may become important if we decide not to split segements for
	   /// verse numbers.)
	   /// </summary>
		/// ------------------------------------------------------------------------------------
	   ITsString Reference(ISegment seg, int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Splits the paragraph at the specified character index.
		/// </summary>
		/// <param name="ich">The character index where a split will be inserted.</param>
		/// <returns>the new paragraph inserted at the character index</returns>
		/// ------------------------------------------------------------------------------------
		IStTxtPara SplitParaAt(int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the para contents, BT, etc. from the following paragraph to this paragraph.
		/// Also removes the following paragraph afterwords.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void MergeParaWithNext();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces the specified range of text with the specified range in the fromPara to
		/// this paragraph.
		/// </summary>
		/// <param name="ichMinDest">The starting character index where the text will be replaced.</param>
		/// <param name="ichLimDest">The ending character index where the text will be replaced.</param>
		/// <param name="fromPara">The source para for the text.</param>
		/// <param name="ichMinSrc">The starting character index to copy from.</param>
		/// <param name="ichLimSrc">The ending character index to copy from.</param>
		/// ------------------------------------------------------------------------------------
		void ReplaceTextRange(int ichMinDest, int ichLimDest, IStTxtPara fromPara,
			int ichMinSrc, int ichLimSrc);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IFdoOwningSequence<IStFootnote> FootnoteSequence { get; }

		/// <summary>
		/// Analyses of all the segments.
		/// </summary>
		IEnumerable<IAnalysis> Analyses { get;}

		/// <summary>
		/// Collects the HashSet of the unique wordforms in the paragraph.
		/// </summary>
		void CollectUniqueWordforms(HashSet<IWfiWordform> wordforms);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character offset in the free translation (CmTranslation) corresponding to
		/// the start of the given segment.
		/// </summary>
		/// <param name="segment">The character offset in the free translation.</param>
		/// <param name="ws">The writing system HVO.</param>
		/// ------------------------------------------------------------------------------------
		int GetOffsetInFreeTranslationForStartOfSegment(ISegment segment, int ws);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the segment corresponding to the given character offset in the specified free
		/// translation.
		/// </summary>
		/// <param name="ich">The charcater offset.</param>
		/// <param name="ws">The writing system HVO.</param>
		/// ------------------------------------------------------------------------------------
		ISegment GetSegmentForOffsetInFreeTranslation(int ich, int ws);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a character position in the contents of this paragraph, return a character
		/// offset to the start of the free translation for the corresponding segment.
		/// </summary>
		/// <param name="ich">The ich main position.</param>
		/// <param name="btWs">The back translation writing system HVO</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		int GetBtPosition(int ich, int btWs);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IScrTxtPara : IEnumerable
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context of the current paragraph.
		/// </summary>
		/// <exception cref="InvalidOperationException">Unable to get context for paragraph.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		ContextValues Context { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default style (based on context/structure) for the current
		/// paragraph.
		/// </summary>
		/// <exception cref="InvalidOperationException">Unable to get style name for paragraph.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		string DefaultStyleName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets section that contains this paragraph.
		/// </summary>
		/// <returns>the section that owns the paragraph or null if the paragraph is in a
		/// title</returns>
		/// ------------------------------------------------------------------------------------
		IScrSection OwningSection { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if paragraph has chapter or verse numbers in it.
		/// </summary>
		/// <returns><code>true</code> if either a chapter number or verse number is found
		/// in the paragraph</returns>
		/// /// ------------------------------------------------------------------------------------
		bool HasChapterOrVerseNumbers();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mark all of the back translations for this paragraph as unfinished.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void MarkBackTranslationsAsUnfinished();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a structured string in a paragraph or back translation, replaces the given
		/// range with a given string and given run props. Updates the cache.
		/// </summary>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt,
		/// otherwise 0 for the vernacular</param>
		/// <param name="str">the string to be inserted; if null, we remove the range</param>
		/// <param name="ttp">The text props for the string being inserted</param>
		/// <param name="ichMin">character offset Min at which we will replace in the tss
		///  </param>
		/// <param name="ichLim">end of the range at which we will replace in the tss </param>
		/// <param name="ichLimNew">gets set to the end of what we inserted</param>
		/// ------------------------------------------------------------------------------------
		void ReplaceInParaOrBt(int wsAlt, string str, ITsTextProps ttp, int ichMin,
			int ichLim, out int ichLimNew);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a back translation string, try to locate the corresponding
		/// chapter in the vernacular and insert it in the BT
		/// (or update the existing reference in the BT).
		/// </summary>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ichMin">The ich min.</param>
		/// <param name="ichLim">The ich lim.</param>
		/// <param name="ichLimIns">output: set to the end of the new BT chapter number run</param>
		/// <returns>
		/// 	<c>true</c> if a chapter number was inserted; <c>false</c> otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool InsertNextChapterNumberInBt(int wsAlt, int ichMin, int ichLim, out int ichLimIns);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an IP selection in a vernacular paragraph, and a following verse number run,
		/// determine if a verse number is missing and if so insert it at the IP.
		/// </summary>
		/// <param name="ichIp">The character position in the paragraph</param>
		/// <param name="ichMinNextVerse">The character offset at the beginning of the following
		/// verse number run</param>
		/// <param name="sVerseNumIns">output: String representation of the new verse number
		/// inserted, or null if none inserted</param>
		/// <param name="ichLimInserted">output: set to the end of the inserted verse number
		/// run, or -1 if none inserted</param>
		/// ------------------------------------------------------------------------------------
		void InsertMissingVerseNumberInVern(int ichIp, int ichMinNextVerse,
			out string sVerseNumIns, out int ichLimInserted);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a vernacular paragraph string, insert the next verse number.
		/// </summary>
		/// <param name="ich">The character position at which to insert verse number</param>
		/// <param name="sVerseNumIns">The s verse num ins.</param>
		/// <param name="ichLim">Gets set to the end of the new verse number run</param>
		/// <returns>
		/// 	<c>true</c> if we inserted a verse number; <c>false</c> if not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool InsertNextVerseNumberInVern(int ich, out string sVerseNumIns, out int ichLim);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a back translation string, try to locate the corresponding
		/// verse in the vernacular, and insert the verse number in the BT.
		/// </summary>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ich">The character position at which to insert verse number</param>
		/// <param name="sVerseNumIns">output: String containing the inserted verse number,
		/// or null if no verse number inserted</param>
		/// <param name="sChapterNumIns">output: String containing the inserted chapter number,
		/// or null if no chapter number inserted</param>
		/// <param name="ichLimIns">output: Gets set to the end of the new BT chapter/verse numbers
		/// inserted</param>
		/// <returns>
		/// 	<c>true</c> if we inserted a verse number/bridge; <c>false</c> if not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool InsertNextVerseNumberInBt(int wsAlt, int ich, out string sVerseNumIns,
			out string sChapterNumIns, out int ichLimIns);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a verse number run in a vernacular paragraph string, create or extend a verse
		/// bridge.
		/// </summary>
		/// <param name="ichMin">The character offset at the beginning of the verse number
		/// run</param>
		/// <param name="ichLim">the end of the verse number run</param>
		/// <param name="sVerseNumIns">output: String representation of the new end number appended
		/// to verse bridge</param>
		/// <param name="ichLimIns">output: the end offset of the updated verse number run</param>
		/// <returns>
		/// 	<c>true</c> if we updated a verse number/bridge; false if not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool UpdateExistingVerseNumberInVern(int ichMin, int ichLim, out string sVerseNumIns,
			out int ichLimIns);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a verse number run in the back translation string, try to locate the
		/// corresponding verse in the vernacular, and update the verse number in the BT.
		/// </summary>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ichMin">The character offset at the beginning of the BT verse number
		/// run</param>
		/// <param name="ichLim">the end of the verse number run</param>
		/// <param name="sVerseNumIns">output: String representation of the new end number appended
		/// to verse bridge</param>
		/// <param name="sChapterNumIns">output: String containing the inserted chapter number,
		/// or null if no chapter number inserted</param>
		/// <param name="ichLimIns">output: the end offset of the updated chapter/verse numbers</param>
		/// ------------------------------------------------------------------------------------
		void UpdateExistingVerseNumberInBt(int wsAlt, int ichMin, int ichLim,
			out string sVerseNumIns, out string sChapterNumIns, out int ichLimIns);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove any duplicate verse numbers following the new verse number in the following
		/// text in the current as well as the following section, if any.
		/// </summary>
		/// <param name="wsAlt">The writing system, if a back trans multiString alt,
		/// otherwise 0 for the vernacular</param>
		/// <param name="chapterToRemove">A string representation of the duplicate chapter number
		/// to remove.</param>
		/// <param name="verseRangeToRemove">A string representation of the duplicate verse number to
		/// remove. This may also be a verse bridge, in which case we will remove all verse
		/// numbers up to the end value of the bridge</param>
		/// <param name="ich">The character offset after which we start looking for dups</param>
		/// ------------------------------------------------------------------------------------
		void RemoveDuplicateVerseNumbers(int wsAlt, string chapterToRemove,
			string verseRangeToRemove, int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the start and end reference of the specified position <paramref name="ivPos"/>
		/// in the paragraph. Section reference could be used, if available, to fill in missing
		/// information, but (at least for now) we will not search back into previous sections.
		/// </summary>
		/// <param name="ivPos">Character offset in the paragraph.</param>
		/// <param name="refStart">[out] Start reference</param>
		/// <param name="refEnd">[out] End reference</param>
		/// <remarks><p><paramref name="refStart"/> and <paramref name="refEnd"/> are only
		/// different if we have bridged verse numbers.</p>
		/// <p>May return incomplete or invalid reference if, for example, the section
		/// object does not have a valid start reference.</p>
		/// <p>If ivPos LT zero, we will not search this para, but look only in previous
		/// paragraphs</p></remarks>
		/// ------------------------------------------------------------------------------------
		void GetRefsAtPosition(int ivPos, out BCVRef refStart, out BCVRef refEnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the start and end reference of the specified position <paramref name="ivPos"/>
		/// in the paragraph. Section reference could be used, if available, to fill in missing
		/// information, but (at least for now) we will not search back into previous sections.
		/// </summary>
		/// <param name="wsBT">HVO of the writing system of the BT to search, or -1 to search
		/// the vernacular.</param>
		/// <param name="ivPos">Character offset in the paragraph.</param>
		/// <param name="fAssocPrev">Consider this position to be associated with any preceding
		/// text in the paragraph (in the case where ichPos is at a chapter boundary).</param>
		/// <param name="refStart">[out] Start reference</param>
		/// <param name="refEnd">[out] End reference</param>
		/// <remarks><p><paramref name="refStart"/> and <paramref name="refEnd"/> are only
		/// different if we have bridged verse numbers.</p>
		/// <p>May return incomplete or invalid reference if, for example, the section
		/// object does not have a valid start reference.</p>
		/// <p>If ivPos LT zero, we will not search this para, but look only in previous
		/// paragraphs</p></remarks>
		/// ------------------------------------------------------------------------------------
		void GetRefsAtPosition(int wsBT, int ivPos, bool fAssocPrev,
			out BCVRef refStart, out BCVRef refEnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all footnotes "owned" by this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<FootnoteInfo> GetFootnotes();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches the contents of this paragraph forwards for next footnote ORC.
		/// </summary>
		/// <param name="ich">Character index to start search.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search forward starting with the
		/// run after ich, otherwise we start with the current run.</param>
		/// <returns>Next footnote in string after ich, or <c>null</c> if footnote can't be
		/// found.</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindNextFootnoteInContents(ref int ich, bool fSkipCurrentPosition);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches the contents of this paragraph backwards for the previous footnote ORC.
		/// </summary>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the string.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search backwards starting
		/// with the run before ich, otherwise we start with the run ich is in.</param>
		/// <returns>Previous footnote in string, or <c>null</c> if footnote can't be found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindPrevFootnoteInContents(ref int ich, bool fSkipCurrentPosition);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it.
		/// </summary>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindPreviousFootnote(int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all ORCs that reference the specified object in all writing systems for the
		/// back translation of the given (vernacular) paragraph.
		/// </summary>
		/// <param name="guid">guid for the specified object</param>
		/// ------------------------------------------------------------------------------------
		void DeleteAnyBtMarkersForObject(Guid guid);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the text from the specified range in the fromPara to this paragraph. Use this
		/// method only when the paragraphs are in the same book.
		/// </summary>
		/// <param name="ichDest">The character index where the text will be placed.</param>
		/// <param name="sourcePara">The source para for the text.</param>
		/// <param name="ichMinSrc">The starting character index of the text to be moved.</param>
		/// <param name="ichLimSrc">The lim character index of the text to be moved.</param>
		/// <exception cref="ArgumentException">occurs when the source and destination
		/// paragraphs are not owned by the same book</exception>
		/// ------------------------------------------------------------------------------------
		void MoveText(int ichDest, IScrTxtPara sourcePara, int ichMinSrc, int ichLimSrc);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the text from the specified range in the fromPara to this paragraph. Use this
		/// method only when the paragraphs are in the same book.
		/// </summary>
		/// <param name="ichMinDest">The starting character index where the text will be placed.</param>
		/// <param name="ichLimDest">The ending character index where the text will be placed.</param>
		/// <param name="sourcePara">The source para for the text.</param>
		/// <param name="ichMinSrc">The starting character index of the text to be moved.</param>
		/// <param name="ichLimSrc">The lim character index of the text to be moved.</param>
		/// <exception cref="ArgumentException">occurs when the source and destination
		/// paragraphs are not owned by the same book</exception>
		/// ------------------------------------------------------------------------------------
		void MoveText(int ichMinDest, int ichLimDest, IScrTxtPara sourcePara,
			int ichMinSrc, int ichLimSrc);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces this paragraph with the sourcePara. Creates  copies (in this paragraph) of
		/// all objects refered to by ORCs in the sourcePara.
		/// </summary>
		/// <param name="sourcePara">The source para for the text.</param>
		/// ------------------------------------------------------------------------------------
		void ReplacePara(IScrTxtPara sourcePara);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the contents of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Clear();
	}

	public partial interface IScrBookAnnotations
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new check result annotation at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="checkId">The check id.</param>
		/// <param name="bldrQuote">Para builder to use for the cited text paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		IScrScriptureNote InsertErrorAnnotation(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid checkId,
			StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="insertIndex">index where note was inserted into annotation list</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType, out int insertIndex);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <returns>note inserted into annotation list</returns>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// ------------------------------------------------------------------------------------
		IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		///  paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		///  Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		///  paragraph</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType, StTxtParaBldr bldrQuote,
			StTxtParaBldr bldrDiscussion, StTxtParaBldr bldrRecommendation,
			StTxtParaBldr bldrResolution);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object to which annotation refers</param>
		/// <param name="endObject">id of ending object to which annotation refers</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="startOffset">The starting character offset.</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		/// Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		/// paragraph</param>
		/// <param name="insertIndex">out: index where annotation was inserted into
		/// annotation list</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType,
			int startOffset, int endOffset, StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion,
			StTxtParaBldr bldrRecommendation, StTxtParaBldr bldrResolution, out int insertIndex);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object to which annotation refers</param>
		/// <param name="endObject">id of ending object to which annotation refers</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="startOffset">The starting character offset.</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		/// Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		/// paragraph</param>
		/// <param name="insertIndex">index where annotation is to be inserted into
		/// annotation list</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType,
			int startOffset, int endOffset, StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion,
			StTxtParaBldr bldrRecommendation, StTxtParaBldr bldrResolution, int insertIndex);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		///  paragraph</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		IScrScriptureNote InsertImportedNote(BCVRef startRef, BCVRef endRef, ICmObject beginObject,
			ICmObject endObject, Guid guidNoteType, StTxtParaBldr bldrDiscussion);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Canonical Book number (1-66) corresponding to this collection of annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int CanonicalNum { get; }
	}

	public partial interface IScrBookRef
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the standard abbreviation of the book in the UI writing system or an
		/// appropriate fallback.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string UIBookAbbrev { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the standard name of the book in the UI writing system or an appropriate
		/// fallback.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string UIBookName { get; }
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the SIL book ID 3-letter code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string BookId { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the best name for showing in the user interface
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string BestUIName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the best available abbrev for this ScrBook to use in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string BestUIAbbrev { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IScrSection FirstSection { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first Scripture (non-intro) section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IScrSection FirstScriptureSection { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IScrSection LastSection { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets hvos of the writing systems for the back translations used in this book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		HashSet<int> BackTransWs { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare to return tokens for Scripture checking.
		/// </summary>
		/// <param name="chapterNum">0=read whole book, else specified chapter number</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		bool GetTextToCheck(int chapterNum);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next exact match starting from the given section starting at the given
		/// paragraph and character offsets. If not found looking forward from that point, the
		/// search wraps around from the beginning of the book.
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="fFindSectionStart">if set to <c>true</c> this method will return
		/// values corresponding to the beginning of the section head if the target reference is
		/// the first one in a section.</param>
		/// <param name="startingSection">The 0-based index of the first section to look in
		/// </param>
		/// <param name="startingParaIndex">The 0-based index of the first paragraph to look in
		/// </param>
		/// <param name="startingCharIndex">The 0-based index of the first character to look at
		/// </param>
		/// <param name="section">The 0-based index of the section where the reference starts.
		/// </param>
		/// <param name="paraIndex">The 0-based index of the paragraph where the reference starts.
		/// </param>
		/// <param name="ichVerseStart">The 0-based index of the character verse start.</param>
		/// <returns>
		/// 	<c>true</c> if found; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool GetRefStartFromSection(ScrReference targetRef, bool fFindSectionStart,
			IScrSection startingSection, int startingParaIndex, int startingCharIndex,
			out IScrSection section, out int paraIndex, out int ichVerseStart);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section where the given chapter begins.
		/// </summary>
		/// <param name="chapterNum">The chapter number (assumed to be in same versification as
		/// that of the Scripture object).</param>
		/// <returns>the index of the section or -1 if not found</returns>
		/// ------------------------------------------------------------------------------------
		int FindSectionForChapter(int chapterNum);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enumerate all the ITextToken's from the most recent GetText call.
		/// </summary>
		/// <returns>An IEnumerable implementation that allows the caller to retrieve each
		/// text token in sequence.</returns>
		/// ------------------------------------------------------------------------------------
		IEnumerable<ITextToken> TextTokens();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all paragraphs in this book in their natural order (i.e., title
		/// paragraphs first, then intro pragraphs, then Scripture paragraphs). This does not
		/// include footnote paragraphs or picture captions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<IScrTxtPara> Paragraphs { get;}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see whether/how this book can be overwritten with the given saved version.
		/// Possibilities are:
		/// * Full overwrite is possible without losing Scripture data
		/// * Full overwrite would lose Scripture data
		/// * Partial overwrite is possible
		/// </summary>
		/// <param name="savedVersion">The saved version to be checked.</param>
		/// <param name="sDetails">A string with a formatted list of reference ranges included
		/// in current but missing in the saved version (separated by newlines)</param>
		/// <param name="sectionsToRemove">sections that will need to be removed from this book
		/// before a partial overwrite can be executed (will be null unless return type is
		/// Partial).</param>
		/// <param name="missingBtWs">list of back translation writing systems that are used in
		/// this book but not in the savedVersion</param>
		/// ------------------------------------------------------------------------------------
		OverwriteType DetermineOverwritability(IScrBook savedVersion, out string sDetails,
			out List<IScrSection> sectionsToRemove, out HashSet<int> missingBtWs);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote by inserting the footnote marker into the given string builder
		/// and creating a new <see cref="IStFootnote"/> with the footnote marker set to the
		/// same marker. This is the real workhorse, used mainly for internal implementation,
		/// but it's public so import can use it to create footnotes more efficiently.
		/// </summary>
		/// <param name="iInsertAt">Zero-based index of the position in the sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="tsStrBldr">String builder for the paragraph being built</param>
		/// <param name="ich">Character index in paragraph where footnote is to be inserted</param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote InsertFootnoteAt(int iInsertAt, ITsStrBldr tsStrBldr, int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ScrFootnote owned by this ScrBook.
		/// The caller must take care of inserting the proper ORC and the footnote's paragraph
		/// and issusing a propchanged for the new footnote inserted into the book's collection.
		/// </summary>
		/// <param name="iFootnotePos">Zero-based index of the position in the book's sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="ws">The writing system for the footnote marker.</param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote CreateFootnote(int iFootnotePos, int ws);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section forwards for first footnote reference in a section range.
		/// </summary>
		/// <param name="ihvoSectionStart">The index of the starting section in the section hvo array</param>
		/// <param name="ihvoSectionEnd">The index of the ending section in the section hvo array</param>
		/// <returns>
		/// first footnote in a range of sections or null if not found
		/// </returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindFirstFootnoteInSectionRange(int ihvoSectionStart, int ihvoSectionEnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section backwards for last footnote reference in a section range.
		/// </summary>
		/// <param name="ihvoSectionStart">The index of the starting section in the section hvo array</param>
		/// <param name="ihvoSectionEnd">The index of the ending section in the section hvo array</param>
		/// <returns>
		/// last footnote in a range of sections or null if not found
		/// </returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindLastFootnoteInSectionRange(int ihvoSectionStart, int ihvoSectionEnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph with the specified style id, containing the specified verse number,
		/// if specified, and having the correct sequence number. Assumes the reference is
		/// within this book.
		/// </summary>
		/// <param name="targetStyle">style of paragraph to find</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="iPara">0-based index of paragraph</param>
		/// <param name="iVernSection">0-based index of the section the corresponding
		/// vernacular paragraph is in. This will be 0 if no corresponding paragraph can be
		/// found.</param>
		/// <returns>The corresponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		IScrTxtPara FindPara(IStStyle targetStyle, BCVRef targetRef, int iPara, ref int iVernSection);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the content of the given section into the previous section content.
		/// Then deletes the given section.
		/// <param name="iSection">index of the section to be moved</param>
		/// <param name="newStyle">The new style for the moved heading paragraphs.</param>
		/// <exception cref="ArgumentOutOfRangeException">Occurs when section index is invalid.
		/// The index must be greater than 0.</exception>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int MergeSectionIntoPreviousSectionContent(int iSection, IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the content of the given section into the previous section content.
		/// Then deletes the given section.
		/// <param name="iSection">index of the section to be moved</param>
		/// <exception cref="ArgumentOutOfRangeException">Occurs when section index is invalid.
		/// The index must be greater than 0.</exception>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void MergeSectionContentIntoPreviousSectionContent(int iSection);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the requested number of paragraphs from the heading of one section to the end
		/// of the content of the previous section.
		/// </summary>
		/// <param name="iSection">index of the section we move paragraphs from</param>
		/// <param name="iLastPara">index of the last heading paragraph to be moved</param>
		/// <returns> index of first moved paragraph</returns>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iSection is 0,
		/// or out of range. Also when iLastPara is out of range.</exception>
		/// <exception cref="InvalidOperationException">Occurs when the iLastPara is the final
		/// paragraph in the heading. Cannot move all paras in the heading with this method.</exception>
		/// ------------------------------------------------------------------------------------
		int MoveHeadingParasToPreviousSectionContent(int iSection, int iLastPara,
			IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the heading and content of the given section into the next section heading.
		/// Then deletes the given section.
		/// </summary>
		/// <param name="iSection">index of the section to be moved</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <returns>the index of first paragraph originally selected, as viewed by the user</returns>
		/// <exception cref="ArgumentOutOfRangeException">Occurs when section index is invalid.
		/// The index must not be for the last section.</exception>
		/// ------------------------------------------------------------------------------------
		int MergeSectionIntoNextSectionHeading(int iSection, IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merge one or more paragraphs from the content of one section to the beginning of
		/// the heading of the following section. The paragraphs from the given index to the
		/// last content paragraph are moved.
		/// </summary>
		/// <param name="iSection">index of the section we move paragraphs from</param>
		/// <param name="iFirstPara">index of the first content paragraph to be moved</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iSection is the last section,
		/// or out of range. Also when iFirstPara is out of range.</exception>
		/// <exception cref="InvalidOperationException">Occurs when the iFirstPara is the first
		/// paragraph in the contents. Cannot move all paras in the contents with this method.</exception>
		/// ------------------------------------------------------------------------------------
		void MoveContentParasToNextSectionHeading(int iSection, int iFirstPara, IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the specified range from the middle of one section contents to the middle
		/// of another section contents.
		/// All ORC deletions are handled properly.
		/// </summary>
		/// <param name="iSectionStart">The index of the first section</param>
		/// <param name="iSectionEnd">The index of the last section</param>
		/// <param name="iParaStart">The index of the paragraph in the first section</param>
		/// <param name="iParaEnd">The index of the paragraph in the last section</param>
		/// <param name="ichStart">The character position in the paragraph in the first section</param>
		/// <param name="ichEnd">The character position in the paragraph in the last section</param>
		/// ------------------------------------------------------------------------------------
		void DeleteMultiSectionContentRange(int iSectionStart, int iSectionEnd,
			int iParaStart, int iParaEnd, int ichStart, int ichEnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified section in the book. If the section doesn't exist (i.e. index
		/// is out of range), then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IScrSection this[int i]	{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes specified sections in a book.
		/// </summary>
		/// <param name="sectionsToRemove">The sections to remove.</param>
		/// ------------------------------------------------------------------------------------
		void RemoveSections(List<IScrSection> sectionsToRemove);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes specified sections in a book.
		/// </summary>
		/// <param name="sectionsToRemove">The sections to remove.</param>
		/// <param name="progressDlg">The progress dialog box (can be null). Position will be
		/// incremented once per section to be removed (caller is responsible for setting the
		/// range and message appropriately.
		/// </param>
		/// ------------------------------------------------------------------------------------
		void RemoveSections(List<IScrSection> sectionsToRemove, IProgress progressDlg);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next footnote and returns an object that references the footnote and holds
		/// all the necessary info (indices and tags) to locate the footnote marker in the
		/// vernacular text.
		/// </summary>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search</param>
		/// <param name="ich">Character index to start search</param>
		/// <param name="tag">Flid to start search</param>
		/// <returns>Information about the next footnote, or <c>null</c> if there isn't another
		/// footnote in the current book.</returns>
		/// ------------------------------------------------------------------------------------
		FootnoteLocationInfo FindNextFootnote(int iSection, int iParagraph, int ich, int tag);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next footnote and returns an object that references the footnote and holds
		/// all the necessary info (indices and tags) to locate the footnote marker in the
		/// vernacular text.
		/// </summary>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search</param>
		/// <param name="ich">Character index to start search</param>
		/// <param name="tag">Flid to start search</param>
		/// <param name="fSkipCurrentPos"><c>true</c> to start search with run after ich,
		/// <c>false</c> to start with current run.</param>
		/// <returns>Information about the next footnote, or <c>null</c> if there isn't another
		/// footnote in the current book.</returns>
		/// ------------------------------------------------------------------------------------
		FootnoteLocationInfo FindNextFootnote(int iSection, int iParagraph, int ich, int tag,
			bool fSkipCurrentPos);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it. If a footnote is found,
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> will be set to indicate the position before the previous
		/// footnote marker. If no footnote can be found <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// won't change.
		/// </summary>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search, or -1 to start search
		/// in last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="tag">Flid to start search</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindPrevFootnote(ref int iSection, ref int iParagraph, ref int ich, ref int tag);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it. If a footnote is found,
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> will be set to indicate the position before the previous
		/// footnote marker. If no footnote can be found <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// won't change.
		/// </summary>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search, or -1 to start search
		/// in last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="tag">Flid to start search</param>
		/// <param name="fSkipFirstRun"><c>true</c> if the current run where ich is in should
		/// be skipped, otherwise <c>false</c>.</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindPrevFootnote(ref int iSection, ref int iParagraph, ref int ich,
			ref int tag, bool fSkipFirstRun);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote at the specified position in this book.
		/// </summary>
		/// <param name="iSection">Index of section.</param>
		/// <param name="iParagraph">Index of paragraph.</param>
		/// <param name="ich">Character position.</param>
		/// <param name="tag">Tag</param>
		/// <returns>Footnote at specified position, or <c>null</c> if specified position is
		/// not in front of a footnote marker run.</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindCurrentFootnote(int iSection, int iParagraph, int ich, int tag);
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
		NoteType AnnotationType	{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the (first paragraph) of the "quote", or cited text of the
		/// annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string CitedText { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the (first paragraph) of the "quote", or cited text of the
		/// annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsString CitedTextTss { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new note.
		/// </summary>
		/// <param name="guidAnnotationType">GUID representing the type of annotation.</param>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObj">beginning object note refers to</param>
		/// <param name="endObj">ending object note refers to</param>
		/// <param name="startOffset">The starting character offset.</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		/// Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		/// paragraph</param>
		/// ------------------------------------------------------------------------------------
		void InitializeNote(Guid guidAnnotationType, BCVRef startRef, BCVRef endRef,
			ICmObject beginObj, ICmObject endObj, int startOffset, int endOffset,
			StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion, StTxtParaBldr bldrRecommendation,
			StTxtParaBldr bldrResolution);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a response to an annotation
		/// </summary>
		/// <returns>The new IStJournalText that will contain the response</returns>
		/// ------------------------------------------------------------------------------------
		IStJournalText CreateResponse();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets A key that can be used for comparison to determine whether two notes "match"
		/// (i.e., are probably just different versions of each other).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ScrNoteKey Key { get; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
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
		bool BasicSettingsExist { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets wheter project includes separate source(s) with a back translation. If project
		/// has no BT or the BT is interleaved, this returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		bool HasNonInterleavedBT { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether project includes separate source(s) with annotations, or notes. If
		/// project has no annotations or they are interleaved, this returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		bool HasNonInterleavedNotes { get; }

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
		/// Gets/sets the Paratext Back Translation project name
		/// </summary>
		/// <remarks>Setter has side-effect of loading the mappings</remarks>
		/// ------------------------------------------------------------------------------------
		string ParatextBTProj
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Paratext Notes project name
		/// </summary>
		/// <remarks>Setter has side-effect of loading the mappings</remarks>
		/// ------------------------------------------------------------------------------------
		string ParatextNotesProj
		{
			get;
			set;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the in-memory import projects/files are currently accessible from
		/// this machine.
		/// </summary>
		/// <param name="thingsNotFound">A list of Paratext project IDs or file paths that
		/// could not be found.</param>
		/// <remarks>
		/// For Paratext projects, this will only return true if all projects are accessible.
		/// For Standard Format, this will return true if any of the files are accessible.
		/// We think this might make sense, but we aren't sure why.
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		bool ImportProjectIsAccessible(out StringCollection thingsNotFound);

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a MappingSet that is appropriate for the ImportDomain
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		MappingSet GetMappingSetForDomain(ImportDomain domain);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a mapping list based on the import domain
		/// </summary>
		/// <param name="domain">The import domain</param>
		/// <returns>The mapping list</returns>
		/// ------------------------------------------------------------------------------------
		ScrMappingList GetMappingListForDomain(ImportDomain domain);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an import file source that will provide all of the files for an import
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ImportFileSource GetImportFiles(ImportDomain domain);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a file from the file list
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="domain">The domain to remove the file from</param>
		/// <param name="wsId">The writing system identifier for the source (ignored for
		/// scripture domain)</param>
		/// <param name="noteType">The CmAnnotationDefn for the note type (ignored for back
		/// trans and scripture domains)</param>
		/// ------------------------------------------------------------------------------------
		void RemoveFile(string fileName, ImportDomain domain, string wsId, ICmAnnotationDefn noteType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a file to the project, and determine the file encoding and mappings.
		/// </summary>
		/// <param name="fileName">file name to add</param>
		/// <param name="domain">The domain to add the file to</param>
		/// <param name="wsId">The writing system identifier for the source (ignored for
		/// scripture domain)</param>
		/// <param name="noteType">The CmAnnotationDefn for the note type (ignored for back
		/// trans and scripture domains)</param>
		/// <returns>The IScrImportFileInfo representing the added file</returns>
		/// ------------------------------------------------------------------------------------
		IScrImportFileInfo AddFile(string fileName, ImportDomain domain, string wsId,
			ICmAnnotationDefn noteType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a file to the project, and determine the file encoding and mappings.
		/// </summary>
		/// <param name="fileName">file name to add</param>
		/// <param name="domain">The domain to add the file to</param>
		/// <param name="wsId">The writing system identifier for the source (ignored for
		/// scripture domain)</param>
		/// <param name="noteType">The CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// <param name="fileRemovedHandler">Handler for FileRemoved event (can be null if
		/// caller doesn't need to know if a file is removed as a result of a overlapping
		/// conflict</param>
		/// <returns>The IScrImportFileInfo representing the added file</returns>
		/// ------------------------------------------------------------------------------------
		IScrImportFileInfo AddFile(string fileName, ImportDomain domain, string wsId,
			ICmAnnotationDefn noteType, ScrImportFileEventHandler fileRemovedHandler);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check all files that are about to be imported in the given reference range to see
		/// if there are any reference overlaps. If so, resolve the conflict.
		/// </summary>
		/// <param name="start">Start reference</param>
		/// <param name="end">End Reference</param>
		/// ------------------------------------------------------------------------------------
		void CheckForOverlappingFilesInRange(ScrReference start, ScrReference end);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accesses the in-memory copy of the requested mapping list
		/// </summary>
		/// <param name="mappingSet">Indicates the desired mapping list.</param>
		/// <returns>An enumerator for accessing all the ImportMappingInfo objects for the
		/// given domain</returns>
		/// ------------------------------------------------------------------------------------
		IEnumerable Mappings(MappingSet mappingSet);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the mapping info for a given begin marker and set
		/// </summary>
		/// <param name="marker">The begin marker</param>
		/// <param name="mappingSet">Indicates the desired mapping list.</param>
		/// <returns>An ImportMappingInfo representing an import mapping for the begin
		/// marker</returns>
		/// ------------------------------------------------------------------------------------
		ImportMappingInfo MappingForMarker(string marker, MappingSet mappingSet);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set (add or modify) a mapping in the designated mapping list
		/// </summary>
		/// <param name="mappingSet">Indicates the desired mapping list.</param>
		/// <param name="mapping">The mapping info</param>
		/// ------------------------------------------------------------------------------------
		void SetMapping(MappingSet mappingSet, ImportMappingInfo mapping);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete a mapping from the designated mapping list
		/// </summary>
		/// <param name="mappingSet">Indicates the mapping list to delete from.</param>
		/// <param name="mapping">The mapping info</param>
		/// ------------------------------------------------------------------------------------
		void DeleteMapping(MappingSet mappingSet, ImportMappingInfo mapping);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Commit the "temporary" in-memory settings to the permanent properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SaveSettings();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Abandon the "temporary" in-memory settings and re-load from the permanent properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RevertToSaved();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of books that exist for all of the files in this project.
		/// </summary>
		/// <returns>A List of integers representing 1-based canonical book numbers that exist
		/// in any source represented by these import settings</returns>
		/// <exception cref="NotSupportedException">If project is not a supported type</exception>
		/// ------------------------------------------------------------------------------------
		List<int> BooksForProject
		{
			get;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starting reference for the import; for now, we ignore the
		/// chapter and verse since import will always start at the beginning of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		BCVRef StartRef
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ending reference for the import; for now, we ignore the
		/// chapter and verse since import will always end at the end of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		BCVRef EndRef
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets stylesheet for settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IVwStylesheet StyleSheet
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the help file used in a message box if an error occurs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string HelpFile
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the Overlapping File Resolver
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IOverlappingFileResolver OverlappingFileResolver
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this <see cref="IScrImportSet"/> is valid.
		/// </summary>
		/// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
		/// <exception cref="InvalidOperationException">thrown if basic import settings do not
		/// exist</exception>
		/// <exception cref="ScriptureUtilsException">If this is a non-P6 import project and
		/// the strict file scan finds a data error.</exception>
		/// ------------------------------------------------------------------------------------
		bool Valid
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the StartRef and EndRef based on the requested canonical book numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IncludeBooks(int startBook, int endBook, Paratext.ScrVers versification);
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
	public partial interface ICmPicture : IEmbeddedObject
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
		/// Inserts an ORC pointing to this picture at the specified location.
		/// </summary>
		/// <param name="tss">String into which ORC is to be inserted</param>
		/// <param name="ich">character offset where insertion is to occur</param>
		/// <returns>a new TsString with the ORC</returns>
		/// ------------------------------------------------------------------------------------
		ITsString InsertORCAt(ITsString tss, int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an ORC pointing to this picture at the specified location.
		/// </summary>
		/// <param name="tsStrBldr">String into which ORC is to be inserted</param>
		/// <param name="ich">character offset where insertion is to occur</param>
		/// ------------------------------------------------------------------------------------
		void InsertORCAt(ITsStrBldr tsStrBldr, int ich);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the properties of a CmPicture with the given file, caption, and folder.
		/// </summary>
		/// <param name="srcFilename">The full path to the original filename (an internal copy
		/// will be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="ws">The WS for the location in the caption MultiUnicode to put the
		/// caption</param>
		/// ------------------------------------------------------------------------------------
		void UpdatePicture(string srcFilename, ITsString captionTss, string sFolder, int ws);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial interface IScrSection : ICloneableCmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the section contains a given chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ContainsChapter(int chapter);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if this section is an introduction section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsIntro { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of content paragraphs in the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int ContentParagraphCount { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of heading paragraphs in the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int HeadingParagraphCount { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all paragraphs in this section in their natural order (i.e., heading
		/// paragraphs first, then content pragraphs). This does not include footnote paragraphs
		/// or picture captions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<IScrTxtPara> Paragraphs { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is first scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsFirstScriptureSection { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the previous section, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IScrSection PreviousSection { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next section, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IScrSection NextSection { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section content paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IStTxtPara FirstContentParagraph { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section content paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IStTxtPara LastContentParagraph { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section heading paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IStTxtPara FirstHeadingParagraph { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section heading paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IStTxtPara LastHeadingParagraph { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context of the current section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ContextValues Context { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the first paragraph starts with a verse number or a chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool StartsWithVerseOrChapterNumber { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the first paragraph starts with a chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool StartsWithChapterNumber { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the section contains a given reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ContainsReference(ScrReference reference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves paragraphs from the heading of a section to the beginning of its content.
		/// All paragraphs from the given index to the end of the heading are moved.
		/// </summary>
		/// <param name="indexFirstPara">index of the first heading paragraph to be moved</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// ------------------------------------------------------------------------------------
		void MoveHeadingParasToContent(int indexFirstPara, IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves paragraphs from the content of a section to the end of it's heading.
		/// All paragraphs from the beginning of the content to the given index are moved.
		/// </summary>
		/// <param name="indexLastPara">index of the last content paragraph to be moved</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// ------------------------------------------------------------------------------------
		void MoveContentParasToHeading(int indexLastPara, IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the heading of this section.
		/// This method creates a new following section, copies the given heading if any
		/// (e.g. from the revision book) as the new heading, and moves the paragraphs after the
		/// split position in this section heading to the new section heading.
		/// </summary>
		///
		/// <param name="iParaSplit">Index of heading paragraph containing the split position</param>
		/// <param name="ichSplit">Character index within the paragraph at which to split</param>
		/// <returns>the new following section</returns>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iParaSplit or ichSplit
		/// is out of range.</exception>
		/// <exception cref="InvalidStructureException">Occurs if the style of the paragraphs
		/// is not the correct structure.</exception>
		/// ------------------------------------------------------------------------------------
		IScrSection SplitSectionHeading_atIP(int iParaSplit, int ichSplit);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Divides the current section into two sections with section index iSection and
		/// iSection + 1. Moves the selected paragraphs from the heading of
		/// the current section into the content of the current section.
		/// </summary>
		/// <param name="iParaStart">index of the first heading paragraph to be moved into
		/// content</param>
		/// <param name="iParaEnd">index of the last heading paragraph to be moved into
		/// content</param>
		/// <param name="newStyle">The new style for the heading paragraphs that will become
		/// content</param>
		/// ------------------------------------------------------------------------------------
		void SplitSectionHeading_ExistingParaBecomesContent(int iParaStart, int iParaEnd,
			IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the content of this section.
		/// This method creates a new following section, copies the given heading text
		/// as the new heading with new properties applied, and moves the paragraphs after the
		/// split position in this section content to the new section content.
		/// </summary>
		/// <param name="iParaSplit">Index of heading paragraph containing the split position</param>
		/// <param name="headingText">The ITsString that will become the heading.</param>
		/// <param name="headingStyleName">The style name to apply to the heading paragraph.</param>
		/// <returns>the new following section</returns>
		/// ------------------------------------------------------------------------------------
		IScrSection SplitSectionContent_atIP(int iParaSplit, ITsString headingText, string headingStyleName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the content of this section.
		/// This method creates a new following section and moves the paragraphs after the
		/// split position in this section content to the new section content.
		/// </summary>
		///
		/// <param name="iParaSplit">Index of content paragraph containing the split position</param>
		/// <param name="ichSplit">Character index within the paragraph at which to split</param>
		/// <returns>the new following section</returns>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iParaSplit or ichSplit
		/// is out of range.</exception>
		/// <exception cref="InvalidStructureException">Occurs if the style of the paragraphs
		/// is not the correct structure.</exception>
		/// ------------------------------------------------------------------------------------
		IScrSection SplitSectionContent_atIP(int iParaSplit, int ichSplit);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the content of this section.
		/// This method creates a new following section, copies the given heading if any
		/// (e.g. from the revision book) as the new heading, and moves the paragraphs after the
		/// split position in this section content to the new section content.
		/// </summary>
		///
		/// <param name="iParaSplit">Index of content paragraph containing the split position</param>
		/// <param name="ichSplit">Character index within the paragraph at which to split</param>
		/// <param name="newHeading">the StText containing the heading to copy, or null if new
		/// heading is to be empty</param>
		/// <returns>the new following section</returns>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iParaSplit or ichSplit
		/// is out of range.</exception>
		/// <exception cref="InvalidStructureException">Occurs if the style of the paragraphs
		/// is not the correct structure.</exception>
		/// ------------------------------------------------------------------------------------
		IScrSection SplitSectionContent_atIP(int iParaSplit, int ichSplit, IStText newHeading);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when an inner paragraph in the content of this section
		/// has its style changed to a heading.
		/// This method creates a new section using the specified content paragraphs as the new heading
		/// and the remaining paragraphs of this section as its content.
		/// The specified paragraphs must not include the first or last paragraphs. Also, the
		/// caller must make the necessary style changes before calling this method, i.e. the
		/// specified paragraphs must have a section heading style applied.</summary>
		///
		/// <param name="iPara">Index of first content paragraph to be changed to section head.
		/// It must not be the first paragraph.</param>
		/// <param name="cParagraphs">Number of paragraphs to be changed to section head.
		/// The last paragraph in the section content must NOT be included.</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <returns>the new section</returns>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iPara is the first paragraph,
		/// or out of range. Also when the count includes the last paragraph.</exception>
		/// <exception cref="InvalidStructureException">Occurs when the style of the specified paragraphs
		/// is not already section heading structure.</exception>
		/// ------------------------------------------------------------------------------------
		IScrSection SplitSectionContent_ExistingParaBecomesHeading(int iPara, int cParagraphs,
			IStStyle newStyle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the starting and ending display references
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void GetDisplayRefs(out BCVRef startRef, out BCVRef endRef);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnotes in the section heading and contents.
		/// </summary>
		/// <returns>FootnoteInfo list for each footnote in the section</returns>
		/// ------------------------------------------------------------------------------------
		List<IScrFootnote> GetFootnotes();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section forwards for first footnote reference.
		/// </summary>
		/// <param name="iPara">out: The index of the para containing the footnote.</param>
		/// <param name="ich">out: The character index where the footnote was found.</param>
		/// <param name="tag">out: whether the footnote was found in heading or contents</param>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindFirstFootnote(out int iPara, out int ich, out int tag);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section backwards for last footnote reference.
		/// </summary>
		/// <param name="iPara">out: The index of the para containing the footnote.</param>
		/// <param name="ich">out: The character index where the footnote was found.</param>
		/// <param name="tag">out: whether the footnote was found in heading or contents</param>
		/// ------------------------------------------------------------------------------------
		IScrFootnote FindLastFootnote(out int iPara, out int ich, out int tag);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial interface IStFootnote : IEmbeddedObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not to display the target reference for this footnote when it is
		/// displayed at the bottom of the page or in a separate pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool DisplayFootnoteReference { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not to display the marker (caller) for this footnote when it is
		/// displayed at the bottom of the page or in a separate pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool DisplayFootnoteMarker { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FootnoteMarkerTypes MarkerType { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert footnote marker (i.e. Owning ORC run with the footnote GUID in the properties)
		/// into the given string builder for the paragraph.
		/// </summary>
		/// <param name="tsStrBldr">A string builder for the paragraph that is to contain the
		/// footnote owning ORC</param>
		/// <param name="ich">The 0-based character offset into the paragraph at which we will
		/// insert the ORC</param>
		/// <param name="ws">The writing system id for the new ORC run</param>
		/// ------------------------------------------------------------------------------------
		void InsertOwningORCIntoPara(ITsStrBldr tsStrBldr, int ich, int ws);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert footnote marker (i.e. Reference ORC run with the footnote GUID in the properties)
		/// into the given string builder for the translation.
		/// </summary>
		/// <param name="tsStrBldr">A builder for the translation string that is to
		/// contain the footnote reference ORC</param>
		/// <param name="ich">The 0-based character offset into the translation string
		/// at which we will insert the ORC</param>
		/// <param name="ws">The writing system id for the new ORC run</param>
		/// ------------------------------------------------------------------------------------
		void InsertRefORCIntoTrans(ITsStrBldr tsStrBldr, int ich, int ws);
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public partial interface IScrFootnote
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is used to get reference information irrespective of display settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IgnoreDisplaySettings { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the reference of the footnote (chapter:verse)
		/// </summary>
		/// <param name="hvoWs">HVO of the writing system for which we are formatting this
		/// reference.</param>
		/// ------------------------------------------------------------------------------------
		string GetReference(int hvoWs);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the reference of the footnote (chapter:verse)
		/// </summary>
		/// <returns>The reference of this footnote, or an empty string if no paragraph was found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		string RefAsString { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ScrReference StartRef { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ScrReference EndRef { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the paragraph style name of the first paragraph in the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ParaStyleId { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the section that this footnote belongs to (if any).
		/// Returns null if it no longer is found in a paragraph text property (LTB-408),
		/// or if it doesn't belong to a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool TryGetContainingSection(out IScrSection section);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the title (StText) that this footnote belongs to (if any).
		/// Returns 0 if it no longer is found in a paragraph text property (LTB-408),
		/// or if it doesn't belong to a title.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		bool TryGetContainingTitle(out IStText title);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the footnote marker.
		/// </summary>
		/// <param name="markerWS">The WS of the marker</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ITsStrBldr MakeFootnoteMarker(int markerWS);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the marker text (a plain string).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string MarkerAsString { get; }
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IScrMarkerMapping
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the style name from the attached style if one exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string StyleName { get; }
	}

	/// <summary>
	/// Delegate for events that notify of changes to a property on a CmObject
	/// </summary>
	/// <param name="sender">The CM object whose property has changed</param>
	public delegate void PropertyChangedHandler(ICmObject sender);

	/// <summary>
	///
	/// </summary>
	public partial interface IScripture
	{
		/// <summary>
		/// Fired when the ScriptureBooksOS property changes (insertions, deletions)
		/// </summary>
		event PropertyChangedHandler BooksChanged;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance of Scripture has had the
		/// BT CmTranslation fix applied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool FixedParasWithoutBt { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance of Scripture has had the segments fix
		/// applied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool FixedParasWithoutSegments { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the old key terms list with its leftover key term
		/// hierarchical structures has already been removed from a FW language project (not
		/// really on Scripture at all, but this is here since the key terms stuff is really
		/// only used in TE).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool RemovedOldKeyTermsList { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance of Scripture has had the
		/// orphaned footnote fix applied. The setter is really only intended to be used
		/// internally and in the TeScrInitializer. Don't ever set it to false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool FixedOrphanedFootnotes { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether styles referenced in ScrParas have had the fix to
		/// mark these styles as InUse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool FixedStylesInUse { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Getsa value indicating whether this instance of Scripture has had any
		/// paragraphs with ORCS resegmented, following the change to no longer have ORCS cause
		/// segment breaks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ResegmentedParasWithOrcs { get; }

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

		/// <summary>
		/// All the StTexts owned by this Scripture.
		/// </summary>
		IEnumerable<IStText> StTexts { get; }

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
		/// Finds the import settings of the specified type. If the requested type is not found
		/// but import settings of unknown type are found, the unknown settings will be returned.
		/// </summary>
		/// <param name="importType">Type of the import.</param>
		/// <returns>
		/// The import set of the specified type, or an unknown type as a fallback;
		/// otherwise <c>null</c> will be returned.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		IScrImportSet FindImportSettings(TypeOfImport importType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the import settings named Default, if any, or the first available
		/// one (which is probably the only one), or creates new settings if none exist.
		/// </summary>
		/// <param name="importType">type of import type to find.</param>
		/// ------------------------------------------------------------------------------------
		IScrImportSet FindOrCreateDefaultImportSettings(TypeOfImport importType);

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
		/// Copy a book (typically from an archive) to the current version. Caller should
		/// first ensure there is no book with that ID.
		/// </summary>
		/// <param name="book">The book to copy.</param>
		/// <exception cref="InvalidOperationException">Attempt to copy book to current version
		/// when that book already exists in the current version</exception>
		/// <returns>The copied bok</returns>
		/// ------------------------------------------------------------------------------------
		IScrBook CopyBookToCurrent(IScrBook book);

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
		/// Find the specified book
		/// </summary>
		/// <param name="sSilAbbrev">The 3-letter SIL abbreviation (all-caps) for the book
		/// </param>
		/// <returns>The specified book if it exists; otherwise, null</returns>
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a Scripture reference to a string (like this: 16:3).  It will also
		/// handle script digits if necessary.
		/// </summary>
		/// <param name="reference">A ScrReference that holds the chapter and verse to format</param>
		/// <returns>A formatted chapter/verse reference</returns>
		/// <remarks>See TeHeaderFooterVc for an example of how to format the full reference,
		/// including book name. The method BookName in that class could be moved somewhere
		/// more appropriate if it's ever needed.</remarks>
		/// ------------------------------------------------------------------------------------
		string ChapterVerseRefAsString(BCVRef reference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a Scripture reference to a string (perhaps like this: 16:3, depending on the
		/// chapterVerseSeparator).  It will also handle script digits if necessary.
		/// </summary>
		/// <param name="reference">A ScrReference that holds the chapter and verse to format</param>
		/// <param name="hvoWs">The HVO of the writing system in which we are displaying this
		/// reference (used to determine whether to use script digits and whether to include
		/// right-to-left marks)</param>
		/// <returns>A formatted chapter/verse reference</returns>
		/// <remarks>See TeHeaderFooterVc for an example of how to format the full reference,
		/// including book name. The method BookName in that class could be moved somewhere
		/// more appropriate if it's ever needed.</remarks>
		/// ------------------------------------------------------------------------------------
		string ChapterVerseRefAsString(BCVRef reference, int hvoWs);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the chapter verse bridge for a given section.
		/// </summary>
		/// <param name="section"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string ChapterVerseBridgeAsString(IScrSection section);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert two Scripture references to a string (perhaps like this: 16:3-6), depending
		/// on the chapter/verse separator. It will also handle script digits if necessary.
		/// </summary>
		/// <param name="start">A BCVRef that holds the chapter and beginning verse to format</param>
		/// <param name="end">A BCVRef that holds the ending reference (if book and chapter
		/// don't match book and chapter of start ref, an argument exception is hurled</param>
		/// <param name="hvoWs">HVO of the writing system for which we are formatting this
		/// reference.</param>
		/// <returns>A formatted chapter/verse reference</returns>
		/// ------------------------------------------------------------------------------------
		string ChapterVerseBridgeAsString(BCVRef start, BCVRef end, int hvoWs);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Chapter Verse Reference representation for a footnote (ie. sectionCV-CV Footnote(footnoteCV))
		/// Or Title representation (ie. Title Footnote(OwnOrd))
		/// </summary>
		/// <param name="footnote"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string ContainingRefAsString(IScrFootnote footnote);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get Scripture Project Metadata Provide - provides versification. Separate provider
		/// is needed for providing versification to SharedScrUtils.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		IScrProjMetaDataProvider ScrProjMetaDataProvider
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the referenced paragraph for annotations with introduction verse references
		/// so that the annotations point to paragraphs in current books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void AdjustAnnotationReferences();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph with the specified style id, containing the specified verse number,
		/// if specified, and having the correct sequence number.
		/// </summary>
		/// <param name="targetStyle">style of paragraph to find</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="iPara">0-based index of paragraph</param>
		/// <param name="iVernSection">0-based index of the section the corresponding
		/// vernacular paragraph is in. This will be 0 if no corresponding paragraph can be
		/// found.</param>
		/// <returns>The corresponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		IScrTxtPara FindPara(IStStyle targetStyle, BCVRef targetRef, int iPara, ref int iVernSection);
	}

	public partial interface IScrDraft
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="bookOrd"></param>
		/// <returns></returns>
		IScrBook FindBook(int bookOrd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a copy of the specified book to this version.
		/// </summary>
		/// <param name="book">book to copy</param>
		/// <exception cref="InvalidOperationException">Saved version already contains a copy of
		/// the specified book</exception>
		/// <returns>The saved version of the book (copy of original)</returns>
		/// ------------------------------------------------------------------------------------
		IScrBook AddBookCopy(IScrBook book);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the specified book to this version
		/// </summary>
		/// <param name="book">book to move</param>
		/// <exception cref="InvalidOperationException">Saved version already contains a copy of
		/// the specified book</exception>
		/// ------------------------------------------------------------------------------------
		void AddBookMove(IScrBook book);
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

	public partial interface IFsClosedFeature
	{
		/// <summary>
		/// Gets the symbolic value with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The symbolic value, or <c>null</c> if not found.</returns>
		IFsSymFeatVal GetSymbolicValue(string id);

		/// <summary>
		/// Gets a symbolic feature value or creates it if not already there; use XML item to do it
		/// </summary>
		/// <param name="feature">Xml description of the fs</param>
		/// <param name="item">Xml item</param>
		/// <returns>
		/// FsSymFeatVal corresponding to the feature
		/// </returns>
		IFsSymFeatVal GetOrCreateSymbolicValueFromXml(XmlNode feature, XmlNode item);

		/// <summary>
		/// This is a virtual property.  It returns the sorted list of FsSymFeatVal objects
		/// belonging to this FsClosedFeature.  They are sorted by Name.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IFsSymFeatVal> ValuesSorted { get; }
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
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPhEnvironment
	{

		/// <summary>
		/// Determine if the object satisfies constraints imposed by the class
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="createAnnotation">if set to <c>true</c>, an annotation will be created.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <param name="fAdjustSquiggly">if set to <c>true</c>, the squiggly will be adjusted.</param>
		/// <returns>true if the object is all right</returns>
		bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure, bool fAdjustSquiggly);

	}

	/// <summary>
	/// Additional methods for phonological data.
	/// </summary>
	public partial interface IPhPhonData
	{
		/// <summary>
		/// Return the list of all phonemes, each in the default vernacular writing system.
		/// </summary>
		List<string> AllPhonemes();
		/// <summary>
		/// Return the list of abbreviations for all the natural classes defined, each in the
		/// default analysis writing system.
		/// </summary>
		List<string> AllNaturalClassAbbrs();

		/// <summary>
		/// Rebuild the list of PhonRuleFeats
		/// </summary>
		/// <param name="members">list of items to become PhPhonRuleFeats</param>
		void RebuildPhonRuleFeats(IEnumerable<ICmObject> members);

		/// <summary>
		/// Remove any matching items from the PhonRuleFeats list
		/// </summary>
		/// <param name="obj">Object being removed</param>
		void RemovePhonRuleFeat(ICmObject obj);

	}

	public partial interface IPhPhoneme
	{
		/// <summary>
		/// Occurs when the basic IPA symbol property has changed.
		/// </summary>
		event EventHandler BasicIPASymbolChanged;
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

	/// <summary>
	///
	/// </summary>
	public partial interface IPhSegmentRule
	{
		/// <summary>
		/// Gets the order number.
		/// </summary>
		/// <value>The order number.</value>
		int OrderNumber
		{
			get;
			set;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPhRegularRule
	{
		/// <summary>
		/// Gets all of the feature constraints in this rule.
		/// </summary>
		/// <value>The feature constraints.</value>
		IEnumerable<IPhFeatureConstraint> FeatureConstraints
		{
			get;
		}

		/// <summary>
		/// Gets all of the feature constraints in this rule except those
		/// contained within the specified natural class context.
		/// </summary>
		/// <param name="excludeCtxt">The natural class context.</param>
		/// <returns>The feature constraints.</returns>
		IEnumerable<IPhFeatureConstraint> GetFeatureConstraintsExcept(IPhSimpleContextNC excludeCtxt);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPhSegRuleRHS
	{
		/// <summary>
		/// Retrieves the rule that owns this subrule.
		/// </summary>
		IPhRegularRule OwningRule
		{
			get;
		}
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
		/// <returns>The additional context to remove</returns>
		IPhSimpleContext UpdateStrucChange(int strucChangeIndex, int ctxtIndex, bool insert);

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
		/// <param name="ctxt">The context.</param>
		/// <returns>The structural change index.</returns>
		int GetStrucChangeIndex(IPhSimpleContext ctxt);
	}

	/// <summary>
	///
	/// </summary>
	public partial interface IPhContextOrVar
	{
		/// <summary>
		/// Gets the rule that contains this context.
		/// </summary>
		/// <value>The rule.</value>
		ICmObject Rule
		{
			get;
		}

		/// <summary>
		/// Handles any side-effects of removing a context that must be executed before the context is
		/// actually removed. It must be called manually before the context is removed.
		/// </summary>
		void PreRemovalSideEffects();
	}

	/// <summary>
	///
	/// </summary>
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

	public partial interface IRnResearchNbk
	{
		/// <summary>
		/// Gets all records and sub-records.
		/// </summary>
		/// <value>All records.</value>
		IEnumerable<IRnGenericRec> AllRecords
		{
			get;
		}
	}

	public partial interface IRnGenericRec
	{
		/// <summary>
		/// Gets the current roles of participants in this record.
		/// </summary>
		/// <value>The roles.</value>
		IEnumerable<ICmPossibility> Roles
		{
			get;
		}

		/// <summary>
		/// Gets the default roled participants.
		/// </summary>
		/// <value>The default roled participants.</value>
		IRnRoledPartic DefaultRoledParticipants
		{
			get;
		}

		/// <summary>
		/// Make a default RoledParticipant. This is where we put participants unless the user
		/// chooses to divide them into roles. Many users just list participants and are not aware of the intervening
		/// RoledParticipant object. Caller is responsible to make UOW.
		/// </summary>
		IRnRoledPartic MakeDefaultRoledParticipant();
	}
}
