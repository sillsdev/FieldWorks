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
using System.Linq;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TextTag class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class TextTag
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares TextTags in two segments to see if they are similar enough to be considered
		/// the same. Mostly designed for testing, but in production code because it has some
		/// application in charting (searches?). Tests wordforms tagged for same baseline text
		/// and checks to see that they both reference the same CmPossibility tag.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsAnalogousTo(ITextTag otherTag)
		{
			if (otherTag == null)
				return false;
			if (this.TagRA != otherTag.TagRA)
				return false;
			var myWordforms = this.GetOccurrences();
			var otherWordforms = otherTag.GetOccurrences();
			if (myWordforms == null || otherWordforms == null)
				throw new ArgumentException("Found an invalid TextTag.");
			if (myWordforms.Count != otherWordforms.Count)
				return false;
			// Below LINQ returns false if it finds any tagged wordforms in the two lists
			// that have different baseline text (at the same index)
			return !myWordforms.Where((t, i) => t.BaselineText.Text != otherWordforms[i].BaselineText.Text).Any();
		}

	#region IAnalysisReference members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an AnalysisOccurrence equivalent to the begin point of this reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public AnalysisOccurrence BegRef()
		{
			return new AnalysisOccurrence(BeginSegmentRA, BeginAnalysisIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an AnalysisOccurrence equivalent to the end point of this reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public AnalysisOccurrence EndRef()
		{
			return new AnalysisOccurrence(EndSegmentRA, EndAnalysisIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change reference to a different Segment object. Used by AnalysisAdjuster.
		/// </summary>
		/// <param name="newSeg"></param>
		/// <param name="fBegin">True if BeginSegment is affected.</param>
		/// <param name="fEnd">True if EndSegment is affected.</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeToDifferentSegment(ISegment newSeg, bool fBegin, bool fEnd)
		{
			if (newSeg == null)
				throw new ArgumentNullException();
			if (fBegin)
				BeginSegmentRA = newSeg;
			if (fEnd)
				EndSegmentRA = newSeg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change reference to a different AnalysisIndex. Used by AnalysisAdjuster.
		/// If AnalysisReference is multi-Segment, this presently only handles
		/// changes to one endpoint.
		/// </summary>
		/// <param name="newIndex">change index to this</param>
		/// <param name="fBegin">True if BeginAnalysisIndex is affected.</param>
		/// <param name="fEnd">True if EndAnalysisIndex is affected.</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeToDifferentIndex(int newIndex, bool fBegin, bool fEnd)
		{
			if (newIndex < 0)
				throw new ArgumentOutOfRangeException("newIndex", "Can't set index to a negative number.");
			if (fEnd && fBegin &&
				BeginSegmentRA != EndSegmentRA)
				throw new NotImplementedException();
			if (fBegin)
			{
				BeginAnalysisIndex = newIndex;
				var max = BeginSegmentRA.AnalysesRS.Count - 1;
				BeginAnalysisIndex = Math.Min(BeginAnalysisIndex, max);
				BeginAnalysisIndex = Math.Max(BeginAnalysisIndex, 0);
			}
			if (fEnd)
			{
				EndAnalysisIndex = newIndex;
				var max = EndSegmentRA.AnalysesRS.Count - 1;
				EndAnalysisIndex = Math.Min(EndAnalysisIndex, max);
				EndAnalysisIndex = Math.Max(EndAnalysisIndex, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the words (as AnalysisOccurrence objects) referenced by the current text tag.
		/// Returns an empty list if it can't find any words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<AnalysisOccurrence> GetOccurrences()
		{
			var result = new List<AnalysisOccurrence>();
			if (!IsValidRef)
				return result;
			var point1 = new AnalysisOccurrence(BeginSegmentRA, BeginAnalysisIndex);
			var point2 = new AnalysisOccurrence(EndSegmentRA, EndAnalysisIndex);
			var curOccurrence = point1;
			while (curOccurrence.IsValid)
			{
				// This is the new "Wfic" test (wordform as opposed to punctuation).
				if (curOccurrence.HasWordform)
					result.Add(curOccurrence);
				if (curOccurrence == point2)
					break; // Reached endpoint.
				curOccurrence = curOccurrence.NextWordform();
				if (curOccurrence == null)
					throw new ArgumentException("TextTag has a bad EndPoint.");
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if this reference occurs after the parameter's reference in the text.
		/// </summary>
		/// <param name="otherReference"></param>
		/// ------------------------------------------------------------------------------------
		public bool IsAfter(IAnalysisReference otherReference)
		{
			var otherBegPoint = otherReference.BegRef();
			var myEndPoint = EndRef();
			// Test to see if we're at least in the same paragraph!
			if (myEndPoint.Segment.Owner.Hvo == otherBegPoint.Segment.Owner.Hvo)
			{
				return myEndPoint.GetMyBeginOffsetInPara() > otherBegPoint.GetMyEndOffsetInPara();
			}
			// Different paragraphs
			return myEndPoint.Segment.Owner.IndexInOwner > otherBegPoint.Segment.Owner.IndexInOwner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the end to the next Analysis, if possible.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GrowFromEnd(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().GrowFromEnd(
				fignorePunct, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the beginning to the previous Analysis, if
		/// not already at the beginning of the text.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GrowFromBeginning(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().GrowFromBeginning(
				fignorePunct, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the end to the previous Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShrinkFromEnd(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().ShrinkFromEnd(
				fignorePunct, this);
		}

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
		public bool ShrinkFromBeginning(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().ShrinkFromBeginning(
				fignorePunct, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if reference targets valid Segments and Analysis indices, the beginning
		/// point of the reference is not after the ending point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsValidRef
		{
			get
			{
				if (BeginSegmentRA == null || EndSegmentRA == null ||
					BeginSegmentRA.AnalysesRS == null || EndSegmentRA.AnalysesRS == null ||
					BeginAnalysisIndex < 0 || EndAnalysisIndex < 0 ||
					BeginAnalysisIndex >= BeginSegmentRA.AnalysesRS.Count ||
					EndAnalysisIndex >= EndSegmentRA.AnalysesRS.Count)
					return false;
				return !BegRef().IsAfter(EndRef());
			}
		}

	#endregion

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Punctuation Form class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class PunctuationForm : IAnalysis
	{
		/// <summary>
		/// Get the wordform
		/// </summary>
		public IWfiWordform Wordform
		{
			get { return null; }
		}

		/// <summary>
		/// Punctuation forms don't have wordforms.
		/// </summary>
		public bool HasWordform {get { return false;}}

		#region IAnalysis Members

		/// <summary>
		/// A PunctuationForm has no associated analysis.
		/// </summary>
		public IWfiAnalysis Analysis
		{
			get { return null; }
		}

		#endregion

		partial void FormSideEffects(ITsString originalValue, ITsString newValue)
		{
			var repo = Services.GetInstance<IPunctuationFormRepository>() as IPunctuationFormRepositoryInternal;
			repo.UpdateForm(originalValue, this);
		}

		protected override void OnBeforeObjectDeleted()
		{
			var repo = Services.GetInstance<IPunctuationFormRepository>() as IPunctuationFormRepositoryInternal;
			repo.RemoveForm(Form);

			base.OnBeforeObjectDeleted();
		}

		/// <summary>
		/// Get the form (what appears in text). Implements interface member for IAnalysis. PunctuationForm ignores
		/// the ws argument.
		/// </summary>
		public ITsString GetForm(int ws)
		{
			return Form;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wordform class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class WfiWordform
	{
		/// <summary>
		/// Get the wordform
		/// </summary>
		public IWfiWordform Wordform
		{
			get { return this; }
		}

		/// <summary>
		/// Wordforms have wordforms!
		/// </summary>
		public bool HasWordform { get { return true; } }

		/// <summary>
		/// Get the number of valid parses by the default parser
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int ParserCount
		{
			get
			{
				return AgentCount(m_cache.LanguageProject.DefaultParserAgent);
			}
		}

		/// <summary>
		/// Get the number of valid parses by the default user
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int UserCount
		{
			get
			{
				return AgentCount(m_cache.LanguageProject.DefaultUserAgent);
			}
		}

		/// <summary>
		/// Return a count of the analyses of the wordform that have conflicting evaluations, that is, EvaluationsOC contains
		/// at least one Approves and at least one Disapproves.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int ConflictCount
		{
			get
			{
				return AnalysesOC.Count(analysis => analysis.HasConflictingEvaluations);
			}
		}

		/// <summary>
		/// Return all the text genres in which this wordform occurs.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "CmPossibility")]
		public IEnumerable<ICmPossibility> TextGenres
		{
			get
			{
				var result = new HashSet<ICmPossibility>();
				foreach (var occ in OccurrencesInTexts)
				{
					var stText = occ.Owner.Owner as IStText;
					if (stText == null)
						continue; // May be possible for occurrences in picture captions, eventually.
					foreach (var genre in stText.GenreCategories)
						result.Add(genre);
				}
				return result;
			}
		}

		/// <summary>
		/// The analyses that occur in texts. It counts if one of their glosses occurs.
		/// </summary>
		IEnumerable<IWfiAnalysis> AttestedAnalyses
		{
			get
			{
				var result = new HashSet<IWfiAnalysis>();
				foreach (var seg in OccurrencesInTexts)
				{
					foreach (var analysis in seg.AnalysesRS)
					{
						if (analysis.Wordform != this)
							continue; // not an occurrence of this!
						if (analysis is IWfiAnalysis)
							result.Add(analysis as IWfiAnalysis);
						else if (analysis is IWfiGloss)
							result.Add(analysis.Owner as IWfiAnalysis);
						if (result.Count == AnalysesOC.Count)
							return result; // If they're all attested we can stop
					}
				}
				return result;
			}
		}

		/// <summary>
		/// If we can efficiently detect that this wordform is probably a spurious one
		/// created this session, delete it.
		/// </summary>
		public void DeleteIfSpurious()
		{
			// Sometimes a wordform may occur repeatedly in a list and has already been
			// deleted on an earlier occurrence.
			if (!IsValidObject)
				return;
			if (SpellingStatus != (int)SpellingStatusStates.undecided)
				return; // we know something about it that we don't want to forget.
			if (AnalysesOC.Count > 0)
				return; // likewise
			if (!Services.ObjectRepository.WasCreatedThisSession(this))
				return; // often very slow to delete objects created earlier.
			if (!CanDelete)
				return;
			// Currently we allow deletion of wordforms which occur in WordSets, but we don't want to
			// delete them automatically.
			if (ReferringObjects.Count > 0)
				return;
			var repositoryInternal = ((ICmObjectRepositoryInternal)Services.ObjectRepository);
			if (repositoryInternal.IsFocused(this))
			{
				repositoryInternal.DeleteFocusedObjectWhenNoLongerFocused(this);
				return;
			}
			((ICmObjectInternal)this).DeleteObject();
		}

		/// <summary>
		/// Return all the parts of speech that occur in analyses of this wordform that are used in texts.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "PartOfSpeech")]
		public IEnumerable<IPartOfSpeech> AttestedPos
		{
			get
			{
				var result = new HashSet<IPartOfSpeech>();
				foreach (var analysis in AttestedAnalyses)
					if (analysis.CategoryRA != null)
						result.Add(analysis.CategoryRA);
				return result;
			}
		}
		/// <summary>
		/// Return true if the wordform is considered "Complete", not requiring more attention.
		/// A wordform is complete if it has at least one analysis and all analyses are complete.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsComplete
		{
			get
			{
				// It is more efficient to use SomeAnalysisOccursDirectly because we determine whether any do so in a single
				// pass through the wordform's OccurrencesInTexts, rather than one per analysis.
				// Logically this is equivalent to AnalysesOC.Count > 0 && AnalysesOC.All(analysis=>IsComplete).
				return AnalysesOC.Count > 0 && AnalysesOC.All(analysis => ((WfiAnalysis)analysis).IsFullyFormed) && !SomeAnalysisOccursDirectly;
			}
		}

		/// <summary>
		/// True if any analysis
		/// </summary>
		bool SomeAnalysisOccursDirectly
		{
			get
			{
				return
					(from seg in OccurrencesInTexts
					 from analysis in seg.AnalysesRS
					 where analysis is IWfiAnalysis && AnalysesOC.Contains((IWfiAnalysis)analysis)
					 select analysis).FirstOrDefault() != null;
			}
		}

		/// <summary>
		/// Returns the segments that reference this wordform, but not anything it owns.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "Segment")]
		public List<ISegment> ConcordanceIds
		{
			get
			{
				return new List<ISegment>(from occ in OccurrencesInTexts
										  where occ.AnalysesRS.Contains(this)
										  select occ);
			}
		}

		/// <summary>
		/// A full concordance of all occurrences of this wordform.
		/// (This is not used in the real concordance views, since they require filtering by text.)
		/// </summary>
		public IEnumerable<ISegment> FullConcordanceIds
		{
			get { return OccurrencesInTexts; }
		}

		/// <summary>
		/// Get the number of concorded words in texts.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int FullConcordanceCount
		{
			get { return OccurrencesInTexts.Count(); }
		}

		// All the segments which have this wordform or one of its glosses or analyses in their analysis list.
		// Note: A segment will occur more than once in the bag if the wordform occurs more than once in the segment.
		// Use this variable with extreme care! SimpleBag is a tricky struct designed to optimize memory usage.
		// Because it is a struct, if you copy it even to a local variable you are working with a copy. Changes
		// should NOT be made to such a copy! Don't make this object accessible outside the WfiWordform class.
		private SimpleBag<ISegment> m_occurrencesInTexts;
		/// <summary>
		/// The Segments that reference an occurrence of this word in a text.
		/// Note: the very first call to this for a given language project can be quite slow.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "Segment")]
		public IEnumerable<ISegment> OccurrencesInTexts
		{
			get
			{
				EnsureOccurrencesInTexts();
				return m_occurrencesInTexts;
			}
		}

		/// <summary>
		/// For performance, we'd like this bag to be accessible, but it's unsafe to let out
		/// a copy of the struct, which nothing else should modify and where a client would
		/// see some future changes but not all. Instead we let out a carefully wrapped read-only version.
		/// </summary>
		public IBag<ISegment> OccurrencesBag
		{
			get
			{
				EnsureOccurrencesInTexts();
				return new BagWrapper<ISegment>( ()=> m_occurrencesInTexts);
			}
		}

		/// <summary>
		/// Initialize the OccurrencesInTexts property for all Wordforms, if this has not already been done.
		/// </summary>
		void EnsureOccurrencesInTexts()
		{
			// Enhance JohnT: we could enhance SimpleBag so that it can tell whether it has been initialized;
			// put another magic value in the instance variable (perhaps an empty array) so we can distinguish
			// Empty/uninitialized from Empty/Initialized. Then we could directly ask the bag whether it
			// needs to be initialized, which might be a little more efficient than looking up the repository.
			// A lesser but easier enhancement would be to skip this step unless the bag is empty (but note
			// that getting its count can be expensive; we'd need a new Empty method on the bag).
			var repository = (IWfiWordformRepositoryInternal) Services.GetInstance<IWfiWordformRepository>();
			repository.EnsureOccurrencesInTexts();
		}

		/// <summary>
		/// Record the specified segment as containing an occurrence of this wordform.
		/// </summary>
		internal void AddOccurenceInText(ISegment seg)
		{
			m_occurrencesInTexts.Add(seg);
		}

		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			if (multiAltFlid == WfiWordformTags.kflidForm)
			{
				var repo = Services.GetInstance<IWfiWordformRepository>() as IWfiWordformRepositoryInternal;
				repo.UpdateForm(originalValue, this, alternativeWs.Handle);
			}
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
		}

		protected override void OnBeforeObjectDeleted()
		{
			var repo = Services.GetInstance<IWfiWordformRepository>() as IWfiWordformRepositoryInternal;
			foreach (int ws in Form.AvailableWritingSystemIds)
				repo.RemoveForm(Form.get_String(ws), ws);
			RegisterVirtualsModifiedForObjectDeletion(((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService);
			base.OnBeforeObjectDeleted();
		}

		internal override void RegisterVirtualsModifiedForObjectDeletion(IUnitOfWorkService uow)
		{
			var repo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var guids = (from wf in repo.AllInstances() select wf.Guid).ToArray();
			var newGuids = (from item in guids where item != this.Guid select item).ToArray();
			uow.RegisterVirtualAsModified(Cache.LangProject,
				Cache.ServiceLocator.GetInstance<Virtuals>().LangProjectAllWordforms, guids, newGuids);
			base.RegisterVirtualsModifiedForObjectDeletion(uow);
		}
		/// <summary>
		/// Record that the specified segment has one fewer occurrences of this.
		/// Does not complain if not found; may not have been initialized.
		/// It is odd if the set exists and we don't find it, but there seems little to be
		/// gained by complaining.
		/// </summary>
		internal void RemoveOccurenceInText(ISegment seg)
		{
			m_occurrencesInTexts.Remove(seg);
		}

		/// <summary>
		/// True, if user and parser are in agreement on status of all analyses.
		/// </summary>
		public bool HumanAndParserAgree
		{
			get { throw new NotImplementedException(); }
		}

		private IEnumerable<IWfiAnalysis> AnalysesWithHumanEvaluation(Opinions opinion)
		{
			ICmAgent humanAgent = m_cache.LangProject.DefaultUserAgent;
			return from wa in AnalysesOC where wa.GetAgentOpinion(humanAgent) == opinion select wa;
		}

		/// <summary>
		/// Get a List of Hvos that the human has approved of.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "WfiAnalysis")]
		public IEnumerable<IWfiAnalysis> HumanApprovedAnalyses
		{
			get { return AnalysesWithHumanEvaluation(Opinions.approves); }
		}

		/// <summary>
		/// Get a List of Hvos that the human has no opinion on.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "WfiAnalysis")]
		public IEnumerable<IWfiAnalysis> HumanNoOpinionParses
		{
			get { return AnalysesWithHumanEvaluation(Opinions.noopinion); }
		}

		/// <summary>
		/// Get a List of Hvos that the human has DISapproved of.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "WfiAnalysis")]
		public IEnumerable<IWfiAnalysis> HumanDisapprovedParses
		{
			get { return AnalysesWithHumanEvaluation(Opinions.disapproves); }
		}

		/// <summary>
		/// Get the number of valid parses for the agent specified in hvoAgent
		/// <param name="agent">The ID of the agent that we are interested in.</param>
		/// </summary>
		public int AgentCount(ICmAgent agent)
		{
			var count = 0;
			foreach (var anal in AnalysesOC)
			{
				foreach (var eval in anal.EvaluationsRC)
				{
					if (eval.Approves && eval.Owner == agent)
						++count;
				}
			}
			return count;
		}

		/// <summary>
		/// Gets delete status for the object.
		/// True means it can be deleted, otherwise false.
		/// </summary>
		public override bool CanDelete
		{
			get
			{
				if (!base.CanDelete)
					return false;
				EnsureCompleteIncomingRefs();
				var badRefs = from item in m_incomingRefs.Items
							  where !(item is FdoReferenceCollection<IWfiWordform>)
									|| ((FdoReferenceCollection<IWfiWordform>) item).Flid != WfiWordSetTags.kflidCases
							  select item;
				// This is actually more efficient than checking the count.
				if (badRefs.FirstOrDefault() != null)
					return false;
				EnsureOccurrencesInTexts();
				return m_occurrencesInTexts.FirstOrDefault() == null;
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
				tisb.AppendTsString(ShortNameTSS);
				var okRefs = from item in m_incomingRefs.Items
							  where (item is FdoReferenceCollection<IWfiWordform>)
									&& ((FdoReferenceCollection<IWfiWordform>)item).Flid == WfiWordSetTags.kflidCases
							  select item;
				if (okRefs.FirstOrDefault() != null)
				{
					var desc = Cache.TsStrFactory.MakeString(": " + Strings.ksMemberOfWordSet + " \"", Cache.DefaultUserWs);
					tisb.AppendTsString(desc);
					bool fFirst = true;
					foreach (var item in okRefs)
					{
						if (!fFirst)
							tisb.Append(", ");
						var wordset = (IWfiWordSet) ((FdoReferenceCollection<IWfiWordform>) item).MainObject;
						tisb.AppendTsString(wordset.Name.BestAnalysisVernacularAlternative);
					}
					tisb.Append("\"");
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// The shortest, non abbreviated, label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				var tss = Form.VernacularDefaultWritingSystem;

				if (tss != null && tss.Length != 0)
					return tss;

				return Cache.TsStrFactory.MakeString(
					Strings.ksQuestions,
					Cache.DefaultUserWs);
			}
		}

		#region Misc Properties

		#endregion Misc Properties

		/// <summary>
		/// A WfiWordform does not uniquely identify an Analysis.
		/// Enhance JohnT: as we see more how this is used, it MIGHT be helpful to answer the only analysis,
		/// if the wordform has just one, or even the first if it has several. But probably that should be
		/// another method if we need it.
		/// </summary>
		public IWfiAnalysis Analysis
		{
			get { return null; }
		}

		/// <summary>
		/// Get the form in the specified WS (implements interface member for IAnalysis)
		/// </summary>
		public ITsString GetForm(int ws)
		{
			return Form.get_String(ws);
		}

	}

	/// <summary></summary>
	internal partial class WfiMorphBundle
	{
		/// <summary>
		/// If we have a sense return it; if not, and if we have an MSA, return the first sense (with the right MSA if possible)
		/// from the indicated entry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "LexSense")]
		public ILexSense DefaultSense
		{
			get
			{
				if (SenseRA != null)
					return SenseRA;
				// Try for a default.
				if (MsaRA != null)
				{
					var entry = (ILexEntry)MsaRA.Owner;
					var sense = entry.SenseWithMsa(MsaRA);
					// no sense has right MSA...go for the first sense of any kind.
					if (sense == null && entry.SensesOS.Count > 0)
						sense = entry.SensesOS[0];
					return sense;
				}
				return null;
			}
		}

		partial void MsaRASideEffects(IMoMorphSynAnalysis oldObjValue, IMoMorphSynAnalysis newObjValue)
		{
			// Do nothing, if they are the same.
			if (oldObjValue == null || !oldObjValue.IsValidObject || oldObjValue == newObjValue) return;

			if (oldObjValue.CanDelete)
				((ILexEntry)oldObjValue.Owner).MorphoSyntaxAnalysesOC.Remove(oldObjValue);
		}

		/// <summary>
		/// Return true if the bundle is considered "Complete", not requiring more attention.
		/// A bundle is complete if it has a sense with glosses for all current analysis WSs, a complete morph, and an msa.
		/// </summary>
		/// <remarks>It would be consistent to require the MSA to be complete, too, but we haven't defined such a notion yet.
		/// We could move the checking of sense gloss to an IsComplete implementation on Sense, but we may one day
		/// want a more elaborate notion of the completeness of a sense, while a full set of glosses is enough for
		/// the completeness of the morph bundle using it.</remarks>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsComplete
		{
			get
			{
				return SenseRA != null && MsaRA != null && MorphRA != null
					&& Services.WritingSystems.CurrentAnalysisWritingSystems.All(ws => SenseRA.Gloss.get_String(ws.Handle).Length > 0)
					&& MorphRA.IsComplete;
			}
		}

		/// <summary>
		/// When we delete an MoForm, and don't replace it with something else, we want to restore the form of the
		/// bundle so that the morpheme does not seem to disappear from the bundle (LT-11591, LT-11281)
		/// </summary>
		partial void MorphRASideEffects(IMoForm oldForm, IMoForm newForm)
		{
			if (newForm == null)
			{
				foreach (var ws in oldForm.Form.AvailableWritingSystemIds)
				{
					Form.set_String(ws, oldForm.Form.get_String(ws));
				}
			}

		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class WfiGloss
	{
		/// <summary>
		/// Get the wordform
		/// </summary>
		public IWfiWordform Wordform
		{
			get { return ((IAnalysis)Owner).Wordform; }
		}

		/// <summary>
		/// WfiGlosses have wordforms!
		/// </summary>
		public bool HasWordform { get { return true; } }

		/// <summary>
		/// Return true if the gloss is considered "Complete", not requiring more attention.
		/// Agloss is complete if it has a non-empty form in every current analysis writing system.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsComplete
		{
			get
			{
				return Services.WritingSystems.CurrentAnalysisWritingSystems.All(ws => Form.get_String(ws.Handle).Length > 0);
			}
		}
		/// <summary>
		/// The shortest, non abbreviated, label for the content of this object.
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
			get { return Form.BestAnalysisAlternative; }
		}

		/// <summary>
		/// Override default implementation to make a more suitable TS string for a WfiGloss.
		/// </summary>
		/// <remarks>I'm not sure this is ever called.</remarks>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int annotationCount = 0;
				foreach (ICmObject cmo in ReferringObjects)
				{
					if (cmo is ISegment)
					{
						foreach (var x in (cmo as ISegment).AnalysesRS)
						{
							if (x == this)
								++annotationCount;
						}
					}
					else
					{
						++annotationCount;
					}
				}
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				tisb.AppendTsString(ShortNameTSS);

				int cnt = 1;
				if (annotationCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append("\x2028\x2028");
					tisb.Append(Strings.ksWarningDelWfGloss);
					tisb.Append("\x2028");
					if (annotationCount > 1)
						tisb.Append(String.Format(Strings.ksDelWfGlossUsedXTimes, cnt++, annotationCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksDelWfGlossUsedOnce, cnt++, "\x2028"));
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Returns the segments that reference this gloss.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "Segment")]
		public List<ISegment> ConcordanceIds
		{
			get
			{
				return new List<ISegment>(from occ in Wordform.OccurrencesInTexts
									 where occ.AnalysesRS.Contains(this)
									 select occ);
			}
		}

		/// <summary>
		///
		/// </summary>
		public List<int> FullConcordanceIds
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// For a WfiGloss the associated analysis is the owner.
		/// </summary>
		public IWfiAnalysis Analysis
		{
			get { return Owner as IWfiAnalysis; }
		}

		/// <summary>
		/// Get the form of the associated wordform.
		/// </summary>
		public ITsString GetForm(int ws)
		{
			return Wordform.GetForm(ws);
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class WfiWordSet
	{
		/// <summary>
		/// Gets a TsString that represents the shortname of a Text.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				if (Name != null)
				{
					if (Name.AnalysisDefaultWritingSystem != null
						&& Name.AnalysisDefaultWritingSystem.Text != String.Empty)
					{
						return Name.AnalysisDefaultWritingSystem;
					}
					else if (Name.VernacularDefaultWritingSystem != null
							 && Name.VernacularDefaultWritingSystem.Text != String.Empty)
					{
						return Name.VernacularDefaultWritingSystem;
					}
				}

				return Cache.TsStrFactory.MakeString(
					Strings.ksUnnamed,
					Cache.DefaultUserWs);
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class WfiAnalysis
	{
		/// <summary>
		/// Get the wordform
		/// </summary>
		public IWfiWordform Wordform
		{
			get { return ((IAnalysis)Owner).Wordform; }
		}

		/// <summary>
		/// Analyses have wordforms.
		/// </summary>
		public bool HasWordform { get { return true; } }

		/// <summary>
		/// Agents differ in their opinion of the analysis.
		/// </summary>
		public bool HasConflictingEvaluations
		{
			get { return (from eval in EvaluationsRC select eval.Approves).Distinct().Count() > 1; }
		}

		/// <summary>
		/// Return true if the analysis is considered "Complete", not requiring more attention.
		/// An analysis is complete if it occurs and satisfies the conditions in IsFullyFormed are satisfied.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsComplete
		{
			get
			{
				return IsFullyFormed && !OccursDirectly;
			}
		}

		/// <summary>
		/// True if some segment directly references this analysis. References to its glosses do NOT count.
		/// The most efficient way I know to tell that it occurs is to check all the segments in which its
		/// wordform occurs. Note
		/// </summary>
		internal bool OccursDirectly
		{
			get
			{
				return
					(from seg in Wordform.OccurrencesInTexts
					 from anal in seg.AnalysesRS
					 where anal == this
					 select anal).FirstOrDefault() != null;
			}
		}

		/// <summary>
		/// This expresses the part of IsComplete that does not depend on having occurrences.
		/// An analysis is fully formed if it
		///     - has a human opinion
		///     - has no conflicting opinions
		///     - has a category
		///     - has at least one gloss and all glosses are complete
		///     - has morph bundles, all complete
		/// </summary>
		internal bool IsFullyFormed
		{
			get
			{
				if (MeaningsOC.Count == 0 || !MeaningsOC.All(gloss => gloss.IsComplete))
					return false;
				if (MorphBundlesOS.Count == 0 || !MorphBundlesOS.All(mb => mb.IsComplete))
					return false;
				if (CategoryRA == null)
					return false;
				if (HasConflictingEvaluations)
					return false;
				if ((from eval in EvaluationsRC where eval.Human select eval).FirstOrDefault() == null)
					return false;
				return true;
			}
		}

		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property,
		/// if they want to show something other than the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				var cnt = 0;
				var vernWs = m_cache.DefaultVernWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, vernWs);
				ITsString formSN;
				foreach (var mb in MorphBundlesOS)
				{
					if (cnt++ > 0)
						tisb.Append(" ");
					var form = mb.MorphRA;
					if (form == null) // Some defective morph bundles don't have this property set.
					{
						formSN = mb.Form.VernacularDefaultWritingSystem;
						if (formSN == null)
						{
							tisb.Append("???");
						}
						else
						{
							if (formSN.Length == 0)
								tisb.Append("???");
							else
								tisb.AppendTsString(formSN);
						}
					}
					else
					{
						formSN = form.ShortNameTSS;
						if (formSN != null)
						{
							var type = form.MorphTypeRA;
							if (type != null && type.Prefix != null && type.Prefix.Length > 0)
								tisb.Append(type.Prefix);
							tisb.AppendTsString(formSN);
							if (type != null && type.Postfix != null && type.Postfix.Length > 0)
								tisb.Append(type.Postfix);
						}
					}
				}

				return tisb.GetString();
			}
		}

	/// <summary>
	/// Gets a TsString that represents this object as it could be used in a deletion
	/// confirmation dialogue.
	/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int vernWs = m_cache.DefaultVernWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, vernWs);
				tisb.AppendTsString(ShortNameTSS);

				bool isParserApproved = false;
				foreach (var eval in m_EvaluationsRC)
				{
					ICmAgent agent = eval.Owner as ICmAgent;
					if (!agent.Human && eval.Approves && !isParserApproved)
						isParserApproved = true;
				}
				int annotationCount = 0;
				// Note that this analysis may occur more than once in a single segment.
				foreach (ICmObject cmo in ReferringObjects)
				{
					if (cmo is ISegment)
					{
						foreach (var x in (cmo as ISegment).AnalysesRS)
						{
							if (x == this)
								++annotationCount;
						}
					}
					else
					{
						++annotationCount;
					}
				}
				foreach (var gloss in MeaningsOC)
				{
					foreach (ICmObject cmo in gloss.ReferringObjects)
					{
						if (cmo is ISegment)
						{
							foreach (var x in (cmo as ISegment).AnalysesRS)
							{
								if (x == gloss)
									++annotationCount;
							}
						}
						else
						{
							++annotationCount;
						}
					}
				}
				int cnt = 1;
				bool wantMainWarningLine = true;
				if (annotationCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append("\x2028\x2028");
					tisb.Append(Strings.ksWarningDelAnalysis);
					tisb.Append("\x2028");
					if (annotationCount > 1)
						tisb.Append(String.Format(Strings.ksDelAnalysisUsedXTimes, cnt++, annotationCount));
					else
						tisb.Append(String.Format(Strings.ksDelAnalysisUsedOnce, cnt++));
					wantMainWarningLine = false;
				}
				if (isParserApproved)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
					{
						tisb.Append("\x2028\x2028");
						tisb.Append(Strings.ksWarningDelAnalysis);
					}
					tisb.Append("\x2028");
					tisb.Append(String.Format(Strings.ksDelParserAnalysis, cnt));
				}
				return tisb.GetString();
			}
		}

		#region Implementation of IWfiAnalysis

		/// <summary>
		/// Move the text annotations that refence this object or any WfiGlosses it owns up to the owning WfiWordform.
		/// </summary>
		/// <remarks>
		/// Client is responsible for Undo/Redo wrapping.
		/// </remarks>
		public void MoveConcAnnotationsToWordform()
		{
			// The ToArray serves to make a copy, since the changes in the loop will modify OccurrencesInTexts.
			foreach (var seg in Wordform.OccurrencesInTexts.ToArray())
			{
				for (int i = 0; i < seg.AnalysesRS.Count; i++)
				{
					var analysis = seg.AnalysesRS[i];
					if (analysis == this || analysis.Owner == this)
						seg.AnalysesRS[i] = this.Wordform;
				}
			}
		}

		/// <summary>
		/// Collect all the MSAs referenced by the given WfiAnalysis.
		/// </summary>
		/// <param name="msaSet">MSAs found by this call are added to msaSet.</param>
		public void CollectReferencedMsas(HashSet<IMoMorphSynAnalysis> msaSet)
		{
			msaSet.UnionWith(from mb in MorphBundlesOS
									select mb.MsaRA);
		}

		/// <summary>
		/// tells whether the given agent has approved or disapproved of this analysis, or has not given an opinion.
		/// </summary>
		public Opinions GetAgentOpinion(ICmAgent agent)
		{
			var agentEvaluation = FindEvaluation(agent);

			if (null == agentEvaluation)
				return Opinions.noopinion;

			return agentEvaluation.Approves ? Opinions.approves : Opinions.disapproves;
		}

		/// <summary>
		/// Find the evaluation of this analysis by the specified agent. Specifically, find a CmAgentEvaluation
		/// whose owner is the agent in our Evaluations. If there is none return null.
		/// </summary>
		/// <param name="agent"></param>
		protected ICmAgentEvaluation FindEvaluation(ICmAgent agent)
		{
			return (from cae in EvaluationsRC where cae.Owner == agent select cae).FirstOrDefault();
		}

		/// <summary>
		/// Tells whether the giving agent has approved or disapproved of this analysis, or has not given an opinion.
		/// </summary>
		/// <param name="agent"></param>
		/// <param name="opinion"></param>
		/// <returns>one of the enumerated values in Opinions.</returns>
		public void SetAgentOpinion(ICmAgent agent, Opinions opinion)
		{
			//int wasAccepted = 0;
			////now set the opinion to what it should be
			//switch (opinion)
			//{
			//    case Opinions.approves:
			//        wasAccepted = 1;
			//        break;
			//    case Opinions.disapproves:
			//        wasAccepted = 0;
			//        break;
			//    case Opinions.noopinion:
			//        wasAccepted = 2;
			//        break;
			//}

			agent.SetEvaluation(this, opinion);
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch(e.Flid)
			{
				case WfiAnalysisTags.kflidEvaluations:
					AdjustApprovalLists((IWfiWordform)Owner);
					return;
			}
			base.AddObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// Enhance JohnT: could possibly avoid changing them all, by careful analysis of how the opinion changed.
		/// </summary>
		private void AdjustApprovalLists(IWfiWordform wordform)
		{
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(wordform,
				m_cache.ServiceLocator.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, "HumanApprovedAnalyses", false),
				new Guid[0],
				(from analysis in wordform.HumanApprovedAnalyses select analysis.Guid).ToArray());
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(wordform,
				m_cache.ServiceLocator.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, "HumanNoOpinionParses", false),
				new Guid[0],
				(from analysis in wordform.HumanNoOpinionParses select analysis.Guid).ToArray());
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(wordform,
				m_cache.ServiceLocator.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, "HumanDisapprovedParses", false),
				new Guid[0],
				(from analysis in wordform.HumanDisapprovedParses select analysis.Guid).ToArray());
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case WfiAnalysisTags.kflidEvaluations:
					AdjustApprovalLists((IWfiWordform)Owner);
					return;
			}
			base.RemoveObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// Get the number of the analysis within the collection of the owning wordform's human approved analyses.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Unicode)]
		public string HumanApprovedNumber
		{
			get
			{
				return GetPosition(((IWfiWordform)Owner).HumanApprovedAnalyses);
			}
		}

		/// <summary>
		/// Get the number of the analysis within the collection of the owning wordform's human no-opinion analyses.
		/// (These will be parser approved, but not human approved.)
		/// </summary>
		[VirtualProperty(CellarPropertyType.Unicode)]
		public string HumanNoOpinionNumber
		{
			get
			{
				return GetPosition(((IWfiWordform)Owner).HumanNoOpinionParses);
			}
		}

		/// <summary>
		/// Get the number of the analysis within the collection of the owning wordform's human disapproved analyses.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Unicode)]
		public string HumanDisapprovedNumber
		{
			get
			{
				return GetPosition(((IWfiWordform)Owner).HumanDisapprovedParses);
			}
		}

		private string GetPosition(IEnumerable<IWfiAnalysis> items)
		{
			return (items.ToList().IndexOf(this) + 1).ToString();
		}

		/// <summary>
		///
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int ParserStatusIcon
		{
			get
			{
				var o = GetAgentOpinion(m_cache.LanguageProject.DefaultParserAgent);
				switch (o)
				{
					default:
						return 0;
					case Opinions.approves:
						return 1;
					case Opinions.noopinion:
						return 2;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		[VirtualProperty(CellarPropertyType.Integer)]
		public int ApprovalStatusIcon
		{
			get
			{
				var o = GetAgentOpinion(m_cache.LanguageProject.DefaultUserAgent);
				switch (o)
				{
					default:
						//Debug.Fail("This code does not understand that opinion value.");
						return 0;
					case Opinions.approves:
						return 1;
					case Opinions.disapproves:
						return 2;
					case Opinions.noopinion:
						return 0;

				}
			}
			set
			{
				//				//setting to no opinion is not implemented yet (well it was but it has a bug,
				//				//see code elsewhere in this file), so we will just skipped them over to approving again.
				//				//This assumes that they have gone from disapprove and we will take them back to approve,
				//				//rather than first letting them go into no opinion.
				//
				//				if(value == 0)
				//					value = 1;
				Opinions[] values = { Opinions.noopinion, Opinions.approves, Opinions.disapproves };
				SetAgentOpinion(m_cache.LanguageProject.DefaultUserAgent, values[value]);
			}
		}

		/// <summary>
		/// Returns the segments that reference this analysis, but not anything it owns.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "Segment")]
		public List<ISegment> ConcordanceIds
		{
			get
			{
				return new List<ISegment>(from occ in Wordform.OccurrencesInTexts
									 where occ.AnalysesRS.Contains(this)
									 select occ);
			}
		}

		/// <summary>
		///
		/// </summary>
		public List<int> FullConcordanceIds
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		public IWfiAnalysis Analysis
		{
			get { return this; }
		}

		/// <summary>
		/// Get the form of the associated wordform.
		/// </summary>
		public ITsString GetForm(int ws)
		{
			return Wordform.GetForm(ws);
		}
	}

	/// <summary>
	/// </summary>
	internal partial class Text
	{
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ShortName;
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
					if (Name.VernacularDefaultWritingSystem != null
						&& !string.IsNullOrEmpty(Name.VernacularDefaultWritingSystem.Text))
					{
						return Name.VernacularDefaultWritingSystem;
					}
					if (Name.BestAnalysisVernacularAlternative != null
							 && !string.IsNullOrEmpty(Name.BestAnalysisVernacularAlternative.Text))
					{
						return Name.BestAnalysisVernacularAlternative;
					}
				}

				return Cache.TsStrFactory.MakeString(
					Strings.ksUntitled,
					Cache.DefaultUserWs);
			}
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case TextTags.kflidGenres:
					if (ContentsOA != null)
						InternalServices.UnitOfWorkService.RegisterVirtualAsModified(ContentsOA, "GenreCategories", GenresRC.Cast<ICmObject>());
					return; // still do the default thing, base class has this property too.
			}
			base.AddObjectSideEffectsInternal(e);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case TextTags.kflidGenres:
					if (ContentsOA != null)
						InternalServices.UnitOfWorkService.RegisterVirtualAsModified(ContentsOA, "GenreCategories", GenresRC.Cast<ICmObject>());
					return; // still do the default thing, base class has this property too.
			} base.RemoveObjectSideEffectsInternal(e);
		}

		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			switch(multiAltFlid)
			{
				case CmMajorObjectTags.kflidName:
					if (ContentsOA != null)
						InternalServices.UnitOfWorkService.RegisterVirtualAsModified(ContentsOA, "Title", alternativeWs.Handle);
					break; // still do the default thing, base class has this property too.
				case TextTags.kflidDescription:
					if (ContentsOA != null)
						InternalServices.UnitOfWorkService.RegisterVirtualAsModified(ContentsOA, "Comment", alternativeWs.Handle);
					return; // still do the default thing, base class has this property too.
				case TextTags.kflidAbbreviation:
					if (ContentsOA != null)
						InternalServices.UnitOfWorkService.RegisterVirtualAsModified(ContentsOA, "TitleAbbreviation", alternativeWs.Handle);
					return; // still do the default thing, base class has this property too.
			}
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
		}

		/// <summary>
		/// Lets choosers know where to find list items.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case TextTags.kflidGenres:
					return Cache.LangProject.GenreListOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		partial void ValidateContentsOA(ref IStText newObjValue)
		{
			if (newObjValue == null)
				throw new InvalidOperationException("New value must not be null.");
		}

		partial void ContentsOASideEffects(IStText oldObjValue, IStText newObjValue)
		{
			var langProj = Cache.ServiceLocator.GetInstance<ILangProjectRepository>().AllInstances().First();
			// since langProj.InterlinearTexts is always recomputed, we always know the updated list,
			// and we can't really know the state of the old guids only infer the old guids.
			// but we can infer it if we have a newObjValue.
			var newGuids = (from item in langProj.InterlinearTexts select item.Guid).ToList();
			var oldGuids = (new Guid[0]).ToList();
			if (oldObjValue == null)
			{
				// remove the newObjValue to figure the old guids.
				oldGuids = new List<Guid>(newGuids);
				oldGuids.Remove(newObjValue.Guid);
			}
			var flid = m_cache.MetaDataCache.GetFieldId2(LangProjectTags.kClassId, "InterlinearTexts", false);
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(langProj, flid,
																												 oldGuids.ToArray(),
																												 newGuids.ToArray());
		}
	}
}
