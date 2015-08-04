// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: OverridesCellar.cs
// Responsibility: FW Team

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Collections.Generic;
using System.Text; // StringBuilder
using System.Xml; // XMLWriter
using System.Diagnostics;
using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using SILUBS.PhraseTranslationHelper;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	#region StPara Class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for StPara.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal partial class StPara
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer true if this is the last paragraph in the text (used for displaying
		/// special marks in some views).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsFinalParaInText
		{
			get
			{
				IFdoOwningSequence<IStPara> paras = ((IStText)Owner).ParagraphsOS;
				return (paras.IndexOf(this) == paras.Count - 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the StyleName
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleName
		{
			get
			{
				return StyleRules != null ? StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) : null;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentException("StyleName cannot be set to null or empty string.");
				StyleRules = StyleUtils.ParaStyleTextProps(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle when the StylesRules change.
		/// </summary>
		/// <param name="originalValue">The original value.</param>
		/// <param name="newValue">The new value.</param>
		/// ------------------------------------------------------------------------------------
		partial void StyleRulesSideEffects(ITsTextProps originalValue, ITsTextProps newValue)
		{
			OnStyleRulesChange(originalValue, newValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// An overrideable method to handle when the StylesRules change.
		/// </summary>
		/// <param name="originalValue">The original value.</param>
		/// <param name="newValue">The new value.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnStyleRulesChange(ITsTextProps originalValue, ITsTextProps newValue)
		{
		}
	}
	#endregion

	#region StJournalText class
	internal partial class StJournalText
	{
		/// <summary>
		/// Initialize the DateCreated and DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateCreated = DateTime.Now;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows this StJournalText to perform any side-effects when the contents of one of its
		/// paragraphs changes.
		/// </summary>
		/// <param name="stTxtPara">The changed paragraph.</param>
		/// <param name="originalValue">The original value.</param>
		/// <param name="newValue">The new value.</param>
		/// ------------------------------------------------------------------------------------
		internal override void OnParagraphContentsChanged(IStTxtPara stTxtPara,
			ITsString originalValue, ITsString newValue)
		{
			IScrScriptureNote note = OwnerOfClass<IScrScriptureNote>();
			if (note != null)
				note.DateModified = DateTime.Now;
		}
	}
	#endregion

	#region ScrScriptureNote class
	internal partial class ScrScriptureNote
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Side effects when the QuotaOA changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		partial void QuoteOASideEffects(IStJournalText oldObjValue, IStJournalText newObjValue)
		{
			SetDate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Side effects when the DiscussionOA changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		partial void DiscussionOASideEffects(IStJournalText oldObjValue, IStJournalText newObjValue)
		{
			SetDate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Side effects when the ResolutionOA changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		partial void ResolutionOASideEffects(IStJournalText oldObjValue, IStJournalText newObjValue)
		{
			SetDate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Side effects when the RecommendationOA changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		partial void RecommendationOASideEffects(IStJournalText oldObjValue, IStJournalText newObjValue)
		{
			SetDate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Side effects when the ResolutionOA changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		partial void ResolutionStatusSideEffects(NoteStatus originalValue, NoteStatus newValue)
		{
			SetDate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the modified date and, if not set, the created date.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetDate()
		{
			if (DateCreated == DateTime.MinValue)
				DateCreated = DateTime.Now;
			DateModified = DateTime.Now;
		}
	}
	#endregion

	#region StStyle Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Formatting style
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class StStyle
	{
		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this style is a footnote style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsFootnoteStyle
		{
			get
			{
				return Context == ContextValues.Note;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether the style is in use. Note: "In use" generally means that the
		/// style is being used to mark up some text somewhere in the project, but it's
		/// possible (probable, even) that this will return <c>true</c> even when a style was
		/// used at some time and is no longer being used.
		/// </summary>
		/// <remarks>Virtual to allow dynamic mocks to override it</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool InUse
		{
			get
			{
				return (UserLevel <= 0);
			}
			internal set
			{
				if ((value && UserLevel > 0) || (!value && UserLevel < 0))
					UserLevel *= -1;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the proposed new value for the next style.
		/// </summary>
		/// <param name="newObjValue">The new next style value.</param>
		/// ------------------------------------------------------------------------------------
		partial void ValidateNextRA(ref IStStyle newObjValue)
		{
			if (Type == StyleType.kstParagraph)
				ValidateNextParaContextAndStructure(Context, Structure, newObjValue);
			else if (newObjValue != null)
				throw new InvalidOperationException("Character styles cannot have a style for the following paragraph.");
		}

		partial void ValidateContext(ref ContextValues newValue)
		{
			if (Type == StyleType.kstParagraph)
				ValidateNextParaContextAndStructure(newValue, Structure, NextRA);
		}

		partial void ValidateStructure(ref StructureValues newValue)
		{
			if (Type == StyleType.kstParagraph)
				ValidateNextParaContextAndStructure(Context, newValue, NextRA);
		}

		partial void ValidateType(ref StyleType newValue)
		{
			if (newValue == StyleType.kstParagraph)
				ValidateNextParaContextAndStructure(Context, Structure, NextRA);
			else if (NextRA != null)
				throw new InvalidOperationException("Character styles cannot have a style for the following paragraph.");
		}

		private void ValidateNextParaContextAndStructure(ContextValues context, StructureValues structure, IStStyle nextStyle)
		{
			if (this != nextStyle && nextStyle != null && !StyleServices.IsContextInternal(context) && (context != nextStyle.Context ||
				structure == StructureValues.Body && nextStyle.Structure != structure))
			{
				throw new InvalidOperationException(string.Format("Style {0} cannot use {1} as the style for the following paragraph because these two styles cannot be applied in the same context.", Name, nextStyle.Name));
			}
		}
	}
	#endregion

	#region ScrDraft class
	internal partial class ScrDraft
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the date when this ScrDraft is created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			base.AddObjectSideEffectsInternal(e);
			DateCreated = DateTime.Now;
		}
	}
	#endregion

	#region ChkTerm
	internal partial class ChkTerm : IKeyTerm
	{
		#region IKeyTerm Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the term in the "source" language (i.e., the source of the UNS questions list,
		/// which is in English).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Term
		{
			get { return Name.get_String(Cache.WritingSystemFactory.GetWsFromStr("en")).Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the renderings for the term in the target language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> Renderings
		{
			get
			{
				return RenderingsOC.Select(r => r.SurfaceFormRA.Wordform.Form.VernacularDefaultWritingSystem.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the references of all occurences of this key term as integers in the form
		/// BBBCCCVVV.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<int> BcvOccurences
		{
			get
			{
				foreach (IChkRef occurence in OccurrencesOS)
					yield return occurence.Ref;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the primary (best) rendering for the term in the target language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BestRendering
		{
			get
			{
				int max = 0;
				string bestTranslation = null;
				Dictionary<string, int> occurrences = new Dictionary<string, int>();
				foreach (IChkRef chkRef in OccurrencesOS)
				{
					IWfiWordform rendering = chkRef.RenderingRA;
					if (rendering == null)
						continue;
					string text = rendering.Form.BestVernacularAlternative.Text;
					int num;
					occurrences.TryGetValue(text, out num);
					occurrences[text] = ++num;
					if (num > max)
					{
						bestTranslation = text;
						max = num;
					}
				}
				return bestTranslation;
			}
		}
		#endregion
	}

	#endregion

	#region CmProject class
	internal partial class CmProject
	{
		/// <summary>
		/// Initialize the DateCreated and DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateCreated = DateTime.Now;
			m_DateModified = DateTime.Now;
		}

	}
	#endregion

	#region CmMajorObject Class
	/// <summary>
	///
	/// </summary>
	internal partial class CmMajorObject
	{
		/// <summary>
		/// Initialize the DateCreated and DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateCreated = DateTime.Now;
			m_DateModified = DateTime.Now;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ShortName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override string ShortName
		{
			get
			{
				if (Name != null)
				{
					if (Name.AnalysisDefaultWritingSystem != null && Name.AnalysisDefaultWritingSystem.Text != null)
						return Name.AnalysisDefaultWritingSystem.Text;

					if (Name.UserDefaultWritingSystem != null && Name.UserDefaultWritingSystem.Text != null)
						return Name.UserDefaultWritingSystem.Text;

					if (Name.NotFoundTss != null && Name.NotFoundTss.Text != null)
						return Name.NotFoundTss.Text;
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property,
		/// if they want to show something other than the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
#pragma warning disable 219
				ITsStrFactory tsf = Cache.TsStrFactory;
				ITsString tss = Name.AnalysisDefaultWritingSystem;
				int ws = Cache.WritingSystemFactory.UserWs;
#pragma warning restore 219
				if (tss == null || tss.Length == 0)
				{
					tss = Name.VernacularDefaultWritingSystem;
					ws = m_cache.DefaultVernWs;
				}
				return tss;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for a header/footer set with the specified name in the DB
		/// </summary>
		/// <param name="name">The name of the header/footer set</param>
		/// <returns>
		/// The header/footer set with the given name if it was found, null otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IPubHFSet FindHeaderFooterSetByName(string name)
		{
			foreach (IPubHFSet hfSet in HeaderFooterSetsOC)
			{
				if (hfSet.Name == name)
					return hfSet;
			}
			return null;
		}
	}
	#endregion

	#region CmAgentEvaluation Class
	internal partial class CmAgentEvaluation
	{
		public bool Approves
		{
			get { return (Owner as CmAgent).ApprovesOA == this; }
		}

		public bool Human
		{
			get { return (Owner as CmAgent).Human; }
		}

	}
	#endregion

	#region CmAgent Class
	internal partial class CmAgent
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
		public void SetEvaluation(ICmObject target, Opinions opinion)
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (target is IWfiAnalysis)
			{
				if (DisapprovesOA == null)
					DisapprovesOA = Services.GetInstance<ICmAgentEvaluationFactory>().Create();
				if (ApprovesOA == null)
					ApprovesOA = Services.GetInstance<ICmAgentEvaluationFactory>().Create();
				var analysis = target as IWfiAnalysis;
				if (opinion != Opinions.approves)
					analysis.EvaluationsRC.Remove(ApprovesOA);
				if (opinion != Opinions.disapproves)
					analysis.EvaluationsRC.Remove(DisapprovesOA);
				if (opinion == Opinions.approves)
					analysis.EvaluationsRC.Add(ApprovesOA);
				if (opinion == Opinions.disapproves)
					analysis.EvaluationsRC.Add(DisapprovesOA);
			}
			else
			{
				throw new ArgumentException("'target' must be a WfiAnalysis.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name.AnalysisDefaultWritingSystem.Text;
		}
		partial void ValidateNotesOA(ref IStText newObjValue)
		{
			if (newObjValue == null)
				throw new InvalidOperationException("New value must not be null.");
		}
	}
	#endregion

	#region CmAnnotation Class
	internal partial class CmAnnotation
	{
		/// <summary>
		/// Initialize the DateCreated and DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateCreated = DateTime.Now;
			m_DateModified = DateTime.Now;
		}

		/// <summary>
		/// Override to handle Source.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case CmAnnotationTags.kflidSource:
					return m_cache.LangProject;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}


		/// ------------------------------------------------------------------------------------
	/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Note that in this case we override this as WELL as ReferenceTargetOwner, in order
		/// to filter the list to human agents.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		/// ------------------------------------------------------------------------------------
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case CmAnnotationTags.kflidSource:
					Set<ICmObject> set = new Set<ICmObject>();
					foreach (ICmAgent agent in m_cache.LangProject.AnalyzingAgentsOC)
					{
						if (agent.Human)
							set.Add(agent);
					}
					return set;
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}
	}
	#endregion

	#region CmBaseAnnotation Class
	/// <summary>
	///
	/// </summary>
	internal partial class CmBaseAnnotation
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The shortest, non abbreviated, label for the content of this object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ShortName
		{
			get { return BeginObjectRA != null ? BeginObjectRA.ShortName : Strings.ksOrphanAnnotation; }
		}

		/// <summary>
		/// Answer the string that is actually annotated: the range from BeginOffset to EndOffset in
		/// property Flid (and possibly alternative WsSelector) of BeginObject.
		/// If Flid has not been set and BeginObject is an StTxtPara assumes we want the Contents.
		/// Does not attempt to handle a case where EndObject is different.
		/// </summary>
		/// <returns></returns>
		public ITsString TextAnnotated
		{
			get
			{
				ITsString tssObj;
				int flid = Flid;
				if (flid == 0 && BeginObjectRA is IStTxtPara)
					flid = StTxtParaTags.kflidContents;
				if (WsSelector == 0)
					tssObj = Cache.DomainDataByFlid.get_StringProp(BeginObjectRA.Hvo, flid);
				else
					tssObj = Cache.DomainDataByFlid.get_MultiStringAlt(BeginObjectRA.Hvo, flid, WsSelector);
				var bldr = tssObj.GetBldr();
				if (EndOffset < bldr.Length)
					bldr.ReplaceTsString(EndOffset, bldr.Length, null);
				if (BeginOffset > 0)
					bldr.ReplaceTsString(0, BeginOffset, null);
				return bldr.GetString();
			}
		}


		/// <summary>
		/// Used to get Text.Genres
		/// </summary>
		public List<ICmPossibility> TextGenres
		{
			get
			{
				IStText text = OwningStText();
				if (text != null)
				{
					return text.GenreCategories;
				}
				else
				{
					return new List<ICmPossibility>();
				}
			}
		}

		private IStText OwningStText()
		{
			if (BeginObjectRA != null && BeginObjectRA is IStTxtPara)
				return BeginObjectRA.Owner as StText;
			return null;
		}

		/// <summary>
		/// Used to get Text.Title
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString TextTitleForWs(int ws)
		{
			IStText text = OwningStText();
			if (text != null)
			{
				return text.Title.get_String(ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Used to get Text.Source
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString TextSourceForWs(int ws)
		{
			IStText text = OwningStText();
			if (text != null)
			{
				return text.Source.get_String(ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Used to get Text.Description
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString TextCommentForWs(int ws)
		{
			IStText text = OwningStText();
			if (text != null)
			{
				return text.Comment.get_String(ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// CmBaseAnnotations need to check BeginObject and other fields depending upon their type.
		/// </summary>
		/// <returns></returns>
		public override bool IsValidObject
		{
			get
			{
				return base.IsValidObject && (BeginObjectRA == null || BeginObjectRA.IsValidObject);
			}
		}
	}
	#endregion

	#region CmAnthroItem Class
	/// <summary>
	///
	/// </summary>
	internal partial class CmAnthroItem
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
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// Subclasses should override this property,
		/// if they want to show something other than
		/// the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				return AbbrAndNameTSS;
			}
		}
	}
	#endregion

	#region CmSemanticDomain Class
	/// <summary>
	///
	/// </summary>
	internal partial class CmSemanticDomain
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
		/// For now this needs to include the abbreviation to sort correctly
		/// in the record list.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return AbbrAndName;
			}
		}

		/// <summary>
		/// Get the set of senses that refer to this semantic domain.
		/// This is a virtual, backreference property.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "LexSense")]
		public IEnumerable<ILexSense> ReferringSenses
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(LexSenseTags.kflidSemanticDomains);
				List<ILexSense> senses = new List<ILexSense>();
				foreach (var item in m_incomingRefs)
				{
					var collection = item as FdoReferenceCollection<ICmSemanticDomain>;
					if (collection == null)
						continue;
					if (collection.Flid == LexSenseTags.kflidSemanticDomains)
						senses.Add(collection.MainObject as ILexSense);
				}
				var collator = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultVernWs).Collator;
				// they need to be sorted, at least for the classified dictionary view.
				senses.Sort((x, y) => collator.Compare(((LexSense)x).OwnerOutlineName.Text ?? "", ((LexSense)y).OwnerOutlineName.Text ?? ""));
				return senses;
			}
		}

		/// <summary>
		/// Overridden to handle Type property.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case CmSemanticDomainTags.kflidOcmRefs:
					return m_cache.LangProject.AnthroListOA;
				case CmSemanticDomainTags.kflidRelatedDomains:
					return m_cache.LangProject.SemanticDomainListOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
	}
	}
	#endregion

	#region CmDomainQ Class
	/// <summary></summary>
	internal partial class CmDomainQ
	{
		/// <summary>
		/// Override for ShortNameTSS.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get { return this.Question.BestAnalysisVernacularAlternative; }
		}

		/// <summary>
		/// Override for ShortName.
		/// </summary>
		public override string ShortName
		{
			get { return this.Question.BestAnalysisVernacularAlternative.Text; }
		}
	}
	#endregion

	#region CmPossibilityList Class
	internal partial class CmPossibilityList
	{
		private Dictionary<int, Dictionary<string, ICmPossibility>> m_possibilityMap;

		/// <summary>
		/// Get all possibilities, recursively, that are ultimately owned by the list.
		/// </summary>
		public Set<ICmPossibility> ReallyReallyAllPossibilities
		{
			get
			{
				Set<ICmPossibility> set = new Set<ICmPossibility>();
				foreach (var pss in PossibilitiesOS)
				{
					set.Add(pss);
					set.AddRange(pss.ReallyReallyAllPossibilities);
				}
				return set;
			}
		}

		/// <summary>
		/// Get a string showing the type of writing system to use.
		/// N.B. For use with lists of Custom items.
		/// </summary>
		/// <returns></returns>
		public string GetWsString()
		{
			string ws;
			switch (WsSelector)
			{
				case WritingSystemServices.kwsAnal: // fall through; shouldn't happen
				case WritingSystemServices.kwsAnals:
					ws = "best analysis";
					break;
				case WritingSystemServices.kwsVern: // fall through; shouldn't happen
				case WritingSystemServices.kwsVerns:
					ws = "best vernacular";
					break;
				case WritingSystemServices.kwsAnalVerns:
					ws = "best analorvern";
					break;
				case WritingSystemServices.kwsVernAnals:
					ws = "best vernoranal";
					break;
				default:
					throw new Exception("Unknown writing system code found.");
			}
			return ws;
		}

		/// <summary>
		/// Name or, if missing, abbreviation is the best name for a possibility list to show in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				var result = Name.BestAnalysisVernacularAlternative;
				if (result.Text == Strings.ksStars)
					result = Abbreviation.BestAnalysisVernacularAlternative;
				if (result.Text == Strings.ksStars)
				{
					// No Name OR Abbreviation! Try the owning property name.
					result = Cache.MakeUserTss(Cache.MetaDataCache.GetFieldName(OwningFlid));
					if (Owner != Cache.LangProject)
					{
						var bldr = result.GetBldr();
						bldr.Replace(0, 0, ".", null);
						bldr.ReplaceTsString(0, 0, Owner.ShortNameTSS);
						result = bldr.GetString();
					}
				}
				return result;
			}
		}

		/// <summary>
		/// The type of items contained in this list.
		/// </summary>
		/// <param name="stringTbl">string table containing mappings for list item names.</param>
		/// <returns></returns>
		public string ItemsTypeName(StringTable stringTbl)
		{
			string listName;
			if (Owner != null)
				listName = Cache.DomainDataByFlid.MetaDataCache.GetFieldName(OwningFlid);
			else
				listName = Name.BestAnalysisVernacularAlternative.Text;
			var itemsTypeName = stringTbl.GetString(listName, "PossibilityListItemTypeNames");
			return itemsTypeName != "*" + listName + "*"
					? itemsTypeName
					: (PossibilitiesOS.Count > 0
						? stringTbl.GetString(PossibilitiesOS[0].GetType().Name, "ClassNames")
						: itemsTypeName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look up a possibility in a list having a known GUID value
		/// </summary>
		/// <param name="guid">The GUID value</param>
		/// <returns>the possibility</returns>
		/// ------------------------------------------------------------------------------------
		public ICmPossibility LookupPossibilityByGuid(Guid guid)
		{
			foreach (ICmPossibility poss in PossibilitiesOS)
			{
				if (poss.Guid == guid)
					return poss;
			}
			throw new ArgumentException("List does not contain the requested CmPossibility.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name.AnalysisDefaultWritingSystem.Text;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Using the possibility name and writing system, find a possibility, or create a new
		/// one if it doesn't exist.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ICmPossibility FindOrCreatePossibility(string possibilityPath, int ws)
		{
			return FindOrCreatePossibility(possibilityPath, ws, true);
		}

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
		public ICmPossibility FindOrCreatePossibility(string possibilityPath, int ws, bool fFullPath)
		{
			if (m_possibilityMap == null)
				m_possibilityMap = new Dictionary<int, Dictionary<string, ICmPossibility>>();

			if (!m_possibilityMap.ContainsKey(ws))
				CacheNoteCategories(PossibilitiesOS, ws);

			ICmPossibility poss;
			if (!fFullPath)
			{
				// The category name is not found in the hash table for the first level and
				// only the category name is provided. So... we will search through subpossibilities
				// for the first name that matches. If we don't find a matching name, we will
				// create a new possibility.
				poss = FindPossibilityByName(PossibilitiesOS, possibilityPath, ws);
				if (poss != null)
					return poss;
			}

			Dictionary<string, ICmPossibility> wsHashTable;
			if (m_possibilityMap.TryGetValue(ws, out wsHashTable))
			{
				if (wsHashTable.TryGetValue(possibilityPath, out poss))
					return poss;

				// Parse the category path and create any missing categories.
				IFdoOwningSequence<ICmPossibility> possibilityList = PossibilitiesOS;
				int level = 1;
				foreach (string strName in possibilityPath.Split(StringUtils.kChObject))
				{
					string possibilityKey = GetPossibilitySubPath(possibilityPath, level);
					ICmPossibility possibility;
					if (!wsHashTable.TryGetValue(possibilityKey, out poss))
					{
						// Category is missing, so create a new one.
						possibility = CreatePossibilityOrAppropriateSubclass();
						possibilityList.Add(possibility);
						possibility.Abbreviation.set_String(ws, strName);
						possibility.Name.set_String(ws, strName);
						m_possibilityMap[ws][possibilityKey] = poss = possibility;
					}
					else
						possibility = poss;

					// Set the current possibility list to the category subpossibility list
					// as we continue our search.
					possibilityList = possibility.SubPossibilitiesOS;
					level++;
				}

				if (poss != null)
					return poss; // only return the possibility if we were able to create a valid one
			}

			throw new InvalidProgramException(
				"Unable to create a dictionary for the writing system in the annotation category hash table");
		}

		private ICmPossibility CreatePossibilityOrAppropriateSubclass()
		{
			var destinationClassId = ItemClsid;
			if (destinationClassId < CmPossibilityTags.kClassId)
				destinationClassId = CmPossibilityTags.kClassId; // Default to CmPossibility
			var factType = GetServicesFromFWClass.GetFactoryTypeFromFWClassID(Cache.MetaDataCache as IFwMetaDataCacheManaged, destinationClassId);
			var factory = Cache.ServiceLocator.GetInstance(factType);
			return ReflectionHelper.GetResult(factory, "Create", null) as ICmPossibility;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the name of the possibility by name only.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ICmPossibility FindPossibilityByName(IFdoOwningSequence<ICmPossibility> possList, string possibilityPath, int ws)
		{

			foreach (CmPossibility poss in possList)
			{
				int dummyWs;
				if (possibilityPath.Equals(poss.Name.GetAlternativeOrBestTss(ws, out dummyWs).Text) ||
					possibilityPath.Equals(poss.Abbreviation.GetAlternativeOrBestTss(ws, out dummyWs).Text))
				{
					return poss;
				}

				// Search any subpossibilities of this possibility.
				ICmPossibility subPoss = FindPossibilityByName(poss.SubPossibilitiesOS, possibilityPath, ws);
				if (subPoss != null)
					return subPoss;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the portion of the possibilityPath as specified by the level.
		/// </summary>
		/// <param name="possibilityPath">The possibility path with levels delimited by ORCs.</param>
		/// <param name="level">The level to which we want the path. For example, if the path
		/// contains three levels and the path is only needed to level two, category level should
		/// be two.</param>
		/// ------------------------------------------------------------------------------------
		private string GetPossibilitySubPath(string possibilityPath, int level)
		{
			StringBuilder strBldr = new StringBuilder();
			int iLevel = 0;
			foreach (string possibility in possibilityPath.Split(StringUtils.kChObject))
			{
				if (iLevel < level)
				{
					if (strBldr.Length > 0)
						strBldr.Append(StringUtils.kChObject); // category previously added to string, so add delimiter

					strBldr.Append(possibility);
				}
				else
					break;
				iLevel++;
			}

			return strBldr.ToString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Fill the possibility map from name to HVO for looking up possibilities.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void CacheNoteCategories(IFdoOwningSequence<ICmPossibility> possibilityList, int ws)
		{
			if (!m_possibilityMap.ContainsKey(ws))
				m_possibilityMap[ws] = new Dictionary<string, ICmPossibility>();

			string s;
			foreach (ICmPossibility poss in possibilityList)
			{
				string sNotFound = poss.Abbreviation.NotFoundTss.Text;

				s = poss.AbbrevHierarchyString;
				if (!string.IsNullOrEmpty(s) && s != sNotFound)
					m_possibilityMap[ws][s] = poss;

				s = poss.NameHierarchyString;
				if (!string.IsNullOrEmpty(s) && s != sNotFound)
					m_possibilityMap[ws][s] = poss;

				CacheNoteCategories(poss.SubPossibilitiesOS, ws);
			}
		}
		/// <summary>
		/// Collect the referring FsFeatureSpecification objects (already done), plus any of
		/// their owners which would then be empty.  Then delete them.
		/// </summary>
		/// <param name="e"></param>
		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);
			if (OwningFlid == MoMorphDataTags.kflidProdRestrict)
			{
				Cache.LangProject.PhonologicalDataOA.RemovePhonRuleFeat(e.ObjectRemoved);
			}
		}

	}
	#endregion

	#region CmCustomItem Class
	/// <summary>
	///
	/// </summary>
	internal partial class CmCustomItem
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name for the type of CmCustomItem.
		/// </summary>
		/// <param name="strTable">string table containing mappings for list item names.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ItemTypeName(StringTable strTable)
		{
			return strTable.GetString(GetType().Name, "ClassNames");
		}

		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// Subclasses DID override this property,
		/// because they wanted to show something other than
		/// the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				var wsString = OwningList.GetWsString();
				ITsString tss = null;
				switch (wsString)
				{
					case "best vernacular":
						tss = BestVernacularName();
						break;
					case "best analorvern":
						tss = BestAnalysisOrVernName(m_cache, this);
						break;
					case "best vernoranal":
						tss = BestVernOrAnalysisName();
						break;
					default:
						tss = BestAnalysisName(m_cache, this);
						break;
				}
				return tss;
			}
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or pss is null). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <returns></returns>
		private ITsString BestVernOrAnalysisName()
		{
			return BestAlternative(m_cache, this, WritingSystemServices.kwsFirstVernOrAnal,
								   CmPossibilityTags.kflidName, Strings.ksQuestions);
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or pss is null). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <returns></returns>
		private ITsString BestVernacularName()
		{
			return BestAlternative(m_cache, this, WritingSystemServices.kwsFirstVern,
								   CmPossibilityTags.kflidName, Strings.ksQuestions);
		}
	}
	#endregion

	#region CmPossibility Class
	/// <summary>
	///
	/// </summary>
	internal partial class CmPossibility
	{
		/// <summary>
		/// Initialize the DateCreated and DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateCreated = DateTime.Now;
			m_DateModified = DateTime.Now;
		}

		/// <summary>
		/// Tracing variable - used to control when and what is output to the debug and trace listeners
		/// </summary>
		public static TraceSwitch CellarTimingSwitch = new TraceSwitch("CellarTiming", "Used for diagnostic timing output", "Off");

		/// <summary>
		/// Call this method before trying to make possSrc be owned by 'this', which is illegal if possSrc now owns this,
		/// because it would make a circularity. To make it possible for 'this' to own possSrc in such a case, we move
		/// 'this' up to be owned by the owner of possSrc.
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
		public ICmObject MoveIfNeeded(ICmPossibility possSrc)
		{
			Debug.Assert(possSrc != null);
			ICmObject newOwner = null;
			ICmPossibility possOwner = this;
			while (true)
			{
				possOwner = possOwner.OwningPossibility;
				if (possOwner == null || possOwner.Equals(possSrc))
					break;
			}
			if (possOwner != null && possOwner.Equals(possSrc))
			{
				// Have to move 'this' to a safe location.
				possOwner = possSrc.OwningPossibility;
				if (possOwner != null)
				{
					possOwner.SubPossibilitiesOS.Add(this);
					newOwner = possOwner;
				}
				else
				{
					// Move it clear up to the list.
					ICmPossibilityList list = possSrc.OwningList;
					list.PossibilitiesOS.Add(this);
					newOwner = list;
				}
			}
			// 'else' means there is no ownership issues to using normal merging/moving.

			return newOwner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name for the type of CmPossibility. Subclasses may override.
		/// </summary>
		/// <param name="strTable">string table containing mappings for list item names.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ItemTypeName(StringTable strTable)
		{
			var owningList = OwningList;
			if (owningList.OwningFlid == 0)
				return strTable.GetString(GetType().Name, "ClassNames");
			var owningFieldName =
				Cache.DomainDataByFlid.MetaDataCache.GetFieldName(owningList.OwningFlid);
			var itemsTypeName = owningList.ItemsTypeName(strTable);
			return itemsTypeName != "*" + owningFieldName + "*"
					? itemsTypeName
					: strTable.GetString(GetType().Name, "ClassNames");
		}

		/// <summary>
		///
		/// </summary>
		public ICmPossibility MainPossibility
		{
			get
			{
				ICmPossibility owningPoss = OwningPossibility;
				if (owningPoss == null)
					return this;
				else
					return owningPoss.MainPossibility;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility name hierarchy in the default analysis
		/// writing system with the top-level category first and the last item at the end of the
		/// string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string NameHierarchyString
		{
			get
			{
				return GetHierarchyString(true, m_cache.DefaultAnalWs);
			}
		}

		/// <summary>
		/// Various fields of CmPossibility should reference various specific lists.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case CmPossibilityTags.kflidRestrictions:
					return m_cache.LangProject.RestrictionsOA;
				case CmPossibilityTags.kflidConfidence:
					return m_cache.LangProject.ConfidenceLevelsOA;
				case CmPossibilityTags.kflidStatus:
					return m_cache.LangProject.StatusOA;
				case CmPossibilityTags.kflidResearchers:
					return m_cache.LangProject.PeopleOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility abbreviation hierarchy in the default
		/// analysis writing system with the top-level category first and the last item at the
		/// end of the string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AbbrevHierarchyString
		{
			get
			{
				return GetHierarchyString(false, m_cache.DefaultAnalWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility hierarchy with the top-level possibility
		/// first and the last item at the end of the string.
		/// </summary>
		/// <param name="fGetName">if <c>true</c> get the name of the possibility; if <c>false</c>
		/// get the abbreviation for the possibility.</param>
		/// <param name="ws">Writing system of possibility name or abbreviation.</param>
		/// ------------------------------------------------------------------------------------
		private string GetHierarchyString(bool fGetName, int ws)
		{
			StringBuilder strBldr = new StringBuilder();
			string strPossibility = null;

			// Go through the hierarchy getting the name of categories until the top-level
			// CmPossibility is found.
			ICmObject categoryObj = this;
			int dummy;
			while (categoryObj is ICmPossibility)
			{
				if (fGetName)
					strPossibility = ((ICmPossibility)categoryObj).Name.GetAlternativeOrBestTss(ws, out dummy).Text;
				else
					strPossibility = ((ICmPossibility)categoryObj).Abbreviation.GetAlternativeOrBestTss(ws, out dummy).Text;
				bool fTextFound = !string.IsNullOrEmpty(strPossibility) && !strPossibility.Equals(Name.NotFoundTss.Text);
				if (fTextFound)
					strBldr.Insert(0, strPossibility);

				categoryObj = categoryObj.Owner;

				if (categoryObj is ICmPossibility && fTextFound)
					strBldr.Insert(0, StringUtils.kChObject); // character delimiter (ORC)
			}

			return strBldr.ToString();
		}

		/// <summary>
		/// Get Subpossibilities of the CmPossibility.
		/// For Performance (used in conjunction with PreLoadList).
		/// </summary>
		/// <returns>Set of subpossibilities</returns>
		public Set<int> SubPossibilities()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the list that ultimately owns this CmPossibility.
		/// </summary>
		public ICmPossibilityList OwningList
		{
			get { return Owner is ICmPossibilityList ? Owner as ICmPossibilityList : OwningPossibility.OwningList; }
		}

		/// <summary>
		///
		/// </summary>
		ICmPossibilityList ICmPossibility.OwningList
		{
			get { return OwningList; }
		}

		/// <summary>
		///
		/// </summary>
		ICmPossibility ICmPossibility.OwningPossibility
		{
			get { return OwningPossibility; }
		}

		/// <summary>
		/// Gets the CmPossibility that owns this CmPossibility,
		/// or null, if it is owned by the list.
		/// </summary>
		public ICmPossibility OwningPossibility
		{
			get { return Owner is ICmPossibility ? Owner as ICmPossibility : null; }
		}

		/// <summary>
		/// Get all possibilities, recursively, that are ultimately owned by the possibility.
		/// </summary>
		public Set<ICmPossibility> ReallyReallyAllPossibilities
		{
			get
			{
				var set = new Set<ICmPossibility>();
				foreach (var pss in SubPossibilitiesOS)
				{
					set.Add(pss);
					set.AddRange(pss.ReallyReallyAllPossibilities);
				}
				return set;
			}
		}

		/// <summary>
		/// Abbreviation and Name with hyphen between.
		/// </summary>
		public string AbbrAndName
		{
			get
			{
				return AbbrAndNameTSS.Text;
			}
		}

		/// <summary>
		/// Abbreviation and Name with hyphen between.
		/// </summary>
		public ITsString AbbrAndNameTSS
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				var tsf = Cache.TsStrFactory;
				tisb.AppendTsString(Abbreviation.BestAnalysisAlternative);
				tisb.AppendTsString(tsf.MakeString(Strings.ksNameAbbrSep, m_cache.DefaultUserWs));
				tisb.AppendTsString(Name.BestAnalysisAlternative);
				return tisb.GetString();
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
				return BestAnalysisName(m_cache, this);
			}
		}

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
			get
			{
				return ShortNameTSS.Text;
			}
		}

		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
			switch (multiAltFlid)
			{
				case CmPossibilityTags.kflidName:
					int flid = m_cache.MetaDataCache.GetFieldId2(CmPossibilityTags.kClassId, "ShortNameTSS", true);
					ITsString newVirtual = ShortNameTSS;
					// We can't get a true old value, but the old value of the name is a good guess.
					((IServiceLocatorInternal) m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(
						this, flid, originalValue, newVirtual);
					break;
			}
		}


		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or pss is null). Return the best available analysis.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="pss"></param>
		/// <returns></returns>
		internal static ITsString BestAnalysisName(FdoCache cache, ICmPossibility pss)
		{
			return BestAlternative(cache, pss,
								   WritingSystemServices.kwsFirstAnal,
								   (int)CmPossibilityTags.kflidName, Strings.ksQuestions);
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or pss is null). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="pss"></param>
		/// <returns></returns>
		internal static ITsString BestAnalysisOrVernName(FdoCache cache, ICmPossibility pss)
		{
			// JohnT: how about this for a default?
			// "a " + this.GetType().Name + " with no name"
			return BestAnalysisOrVernName(cache, pss, Strings.ksQuestions);
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or pss is null). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="pss"></param>
		/// <param name="defValue">string to use (in default user ws) if hvo is zero</param>
		/// <returns></returns>
		internal static ITsString BestAnalysisOrVernName(FdoCache cache, ICmPossibility pss, string defValue)
		{
			return BestAlternative(cache, pss,
								   WritingSystemServices.kwsFirstAnalOrVern,
								   CmPossibilityTags.kflidName, defValue);
		}

		protected static ITsString BestAlternative(FdoCache cache, ICmPossibility pss, int wsMagic, int flid, string defValue)
		{
			ITsString tss = null;
			if (pss != null)
				tss = WritingSystemServices.GetMagicStringAlt(cache, wsMagic, pss.Hvo, flid);
			if (tss == null || tss.Length == 0)
				tss = TsStringUtils.MakeTss(defValue, cache.WritingSystemFactory.UserWs);
			return tss;
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or hvo is 0). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		internal static ITsString BestAnalysisOrVernAbbr(FdoCache cache, int hvo)
		{
			ITsString tss = null;
			if (hvo != 0)
			{
				tss = WritingSystemServices.GetMagicStringAlt(cache, WritingSystemServices.kwsFirstAnalOrVern, hvo,
					CmPossibilityTags.kflidAbbreviation);
			}
			if (tss == null || tss.Length == 0)
			{
				tss = TsStringUtils.MakeTss(Strings.ksQuestions, cache.WritingSystemFactory.UserWs);
				// JohnT: how about this?
				//return TsStringUtils.MakeTss("a " + this.GetType().Name + " with no name", cache.WritingSystemFactory.UserWs);
			}
			return tss;
		}

		/// <summary>
		/// Return the Abbreviation for the specified CmPossibility if one exists for ws (or '???' if it has no name
		/// or hvo is 0).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="pss"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		internal static ITsString TSSAbbrforWS(FdoCache cache, ICmPossibility pss, int ws)
		{
			ITsString tss = null;
			if (pss != null)
			{
				tss = WritingSystemServices.GetMagicStringAlt(cache, ws, pss.Hvo, (int)CmPossibilityTags.kflidAbbreviation);
			}
			if (tss == null || tss.Length == 0)
			{
				tss = TsStringUtils.MakeTss(Strings.ksQuestions, cache.WritingSystemFactory.UserWs);
			}
			return tss;
		}

		public bool IsDefaultDiscourseTemplate
		{
			get
			{
				ICmPossibilityList ccTempl = Cache.LangProject.DiscourseDataOA.ConstChartTemplOA;
				if (OwningList != ccTempl)
					return false;

				IFdoOwningSequence<ICmPossibility> discourseTemplates = ccTempl.PossibilitiesOS;
				return discourseTemplates.Count == 1 && Hvo == discourseTemplates[0].Hvo;
			}
		}

		public bool IsThisOrDescendantInUseAsChartColumn
		{
			get
			{
				if (OwningList != Cache.LangProject.DiscourseDataOA.ConstChartTemplOA)
					return false;

				CmPossibility rootPossibility = this;
				while (rootPossibility.Owner is CmPossibility)
					rootPossibility = (CmPossibility) rootPossibility.Owner;
				IDsChart chart = Services.GetInstance<IDsChartRepository>().InstancesWithTemplate(rootPossibility).FirstOrDefault();
				return chart != null && GetIsThisOrDescendantInUseAsChartColumn();
			}
		}

		private bool GetIsThisOrDescendantInUseAsChartColumn()
		{
			var repo = Services.GetInstance<IConstituentChartCellPartRepository>();
			if (repo.InstancesWithChartCellColumn(this).FirstOrDefault() != null)
				return true;
			return SubPossibilitiesOS.Cast<CmPossibility>().Any(poss => poss.GetIsThisOrDescendantInUseAsChartColumn());
		}

		public bool IsOnlyTextMarkupTag
		{
			get
			{
				ICmPossibilityList tags = Cache.LangProject.TextMarkupTagsOA;
				if (OwningList != tags)
					return false;

				IFdoOwningSequence<ICmPossibility> tagTypes = tags.PossibilitiesOS;
				return tagTypes.Count == 1 && this == tagTypes[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We don't want to delete items that are protected.
		/// </summary>
		/// <returns>True if Ok to delete.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanDelete
		{
			get
			{
				return base.CanDelete
					&& !IsDefaultDiscourseTemplate && !IsThisOrDescendantInUseAsChartColumn
					&& !IsOnlyTextMarkupTag
					&& (OwningList != Cache.LangProject.TextMarkupTagsOA || !Services.GetInstance<ITextTagRepository>().GetByTextMarkupTag(this).Any())
					&& !IsProtected;
			}
		}

		/// <summary>
		/// LIFT (WeSay) doesn't put hyphen between the abbreviation and the name.
		/// </summary>
		public string LiftAbbrAndName
		{
			get
			{
				return String.Format("{0} {1}",
					Abbreviation.BestAnalysisVernacularAlternative.Text,
					Name.BestAnalysisVernacularAlternative.Text);
			}
		}
		partial void ValidateDiscussionOA(ref IStText newObjValue)
		{
			if (newObjValue == null)
				throw new InvalidOperationException("New value must not be null.");
		}

		/// <summary>
		/// Override the method to see if the objSrc owns 'this',
		/// in which case, we will need to move 'this' to a safe spot where it won't be deleted,
		/// when objSrc gets deleted.
		/// </summary>
		/// <param name="objSrc"></param>
		/// <param name="fLoseNoStringData"></param>
		public override void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			MoveIfNeeded(objSrc as ICmPossibility);

			base.MergeObject(objSrc, fLoseNoStringData);
		}
	}
	#endregion

	#region CmTranslation Class
	internal partial class CmTranslation
	{
		/// <summary>
		/// Object owner of class LexExampleSentence. May be null, as the owner can be of another class.
		/// This virtual may seem redundant with CmObject.Owner, but it is important,
		/// because we can correctly indicate the destination class. This is used (at least) in
		/// PartGenerator.GeneratePartsFromLayouts to determine that it needs to generate parts for LexEntry.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "LexExampleSentence")]
		public ILexExampleSentence OwningExample
		{
			get { return (ILexExampleSentence)Owner; }
		}
	}
	#endregion

	#region VirtualOrdering Class
	internal partial class VirtualOrdering
	{
		public override bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
		{
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var flids = mdc.GetFields(ClassID, true, (int)CellarPropertyTypeFilter.All);
			return flids.Contains(flid);
		}
	}
	#endregion

	#region FsClosedFeature Class
	/// <summary>
	/// Summary description for FsClosedFeature
	/// </summary>
	internal partial class FsClosedFeature
	{
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return Name.BestAnalysisAlternative;
			}
		}

		/// <summary>
		/// Gets the symbolic value with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The symbolic value, or <c>null</c> if not found.</returns>
		public IFsSymFeatVal GetSymbolicValue(string id)
		{
			foreach (var symval in ValuesOC)
			{
				if (symval.CatalogSourceId == id)
					return symval;
			}
			return null;
		}

		/// <summary>
		/// Gets a symbolic feature value or creates it if not already there; use XML item to do it
		/// </summary>
		/// <param name="feature">Xml description of the fs</param>
		/// <param name="item">Xml item</param>
		/// <returns>
		/// FsSymFeatVal corresponding to the feature
		/// </returns>
		public IFsSymFeatVal GetOrCreateSymbolicValueFromXml(XmlNode feature, XmlNode item)
		{
			var id = feature.SelectSingleNode("ancestor::item[@id][position()=1]/@id");
			if (id == null)
				return null;

			var symFV = GetSymbolicValue(id.InnerText);
			if (symFV == null)
			{
				// The XML is from a file shipped with FieldWorks. It is quite likely multiple users
				// of a project could independently add the same items, so we create them with fixed guids
				// so merge will recognize them as the same objects.
				var guid = new Guid(feature.SelectSingleNode("ancestor::item[@id][position()=1]/@guid").InnerText);
				symFV = Services.GetInstance<IFsSymFeatValFactory>().Create(guid, this);
				symFV.CatalogSourceId = id.InnerText;
				var abbr = item.SelectSingleNode("abbrev");
				FsFeatureSystem.SetInnerText(m_cache, symFV.Abbreviation, abbr);
				var term = item.SelectSingleNode("term");
				FsFeatureSystem.SetInnerText(m_cache, symFV.Name, term);
				var def = item.SelectSingleNode("def");
				FsFeatureSystem.SetInnerText(m_cache, symFV.Description, def);
				symFV.ShowInGloss = true;
			}
			return symFV;
		}

		/// <summary>
		/// This is a virtual property.  It returns the sorted list of FsSymFeatVal objects
		/// belonging to this FsClosedFeature.  They are sorted by Name.
		/// </summary>
		[VirtualProperty(CellarPropertyType.OwningCollection, "FsSymFeatVal")]
		public IEnumerable<IFsSymFeatVal> ValuesSorted
		{
			get
			{
				var sortedVaues = from v in ValuesOC
								  orderby v.Name.BestAnalysisAlternative.Text
								  select v;
				return sortedVaues;
			}
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch(e.Flid)
			{
				case FsClosedFeatureTags.kflidValues:
					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(this, "ValuesSorted", ValuesSorted);
					break;
			}
			base.AddObjectSideEffectsInternal(e);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch(e.Flid)
			{
				case FsClosedFeatureTags.kflidValues:
					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(this, "ValuesSorted", ValuesSorted);
					break;
			}
			base.RemoveObjectSideEffectsInternal(e);
		}
	}
	#endregion

	#region CmFile class

	/// <summary>
	///
	/// </summary>
	internal partial class CmFile
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the InternalPath (relative to the LinkedFilesRootDir).
		/// Alternatively it may be an absolute path (if it is 'rooted' as understood by
		/// Path.IsPathRooted).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ModelProperty(CellarPropertyType.Unicode, 47004, "string")]
		public string InternalPath
		{
			get
			{
				return InternalPath_Generated;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				string srcFilename = LinkedFilesRelativePathHelper.GetRelativeLinkedFilesPath(value, m_cache.LangProject.LinkedFilesRootDir);

				InternalPath_Generated = srcFilename;
			}
		}

		/// <summary>
		/// Gets the base filename of the InternalPath.
		/// </summary>
		public string InternalBasename
		{
			get
			{
				string path = InternalPath_Generated;
				return Path.GetFileName(path);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the absolute InternalPath (either the InternalPath itself, or if relative,
		/// combined with either the project's LinkedFiles directory or the FW Data Directory)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AbsoluteInternalPath
		{
			get
			{
				string internalPath = InternalPath;
				if (String.IsNullOrEmpty(internalPath))
					internalPath = DomainObjectServices.EmptyFileName;
				return LinkedFilesRelativePathHelper.GetFullPathFromRelativeLFPath(internalPath, m_cache.LangProject.LinkedFilesRootDir);
			}
		}

		partial void InternalPathSideEffects(string originalValue, string newValue)
		{
			var flid = m_cache.MetaDataCache.GetFieldId2(CmPictureTags.kClassId, "PathNameTSS", false);
			foreach (ICmPicture pict in m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().AllInstances())
			{
				if (pict.PictureFileRA == this)
				{
					((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(pict,
						flid, m_cache.MakeUserTss(""), (pict as CmPicture).PathNameTSS);
				}
			}
		}
	}

	#endregion // CmFile

	#region CmPerson class
	/// <summary>
	///
	/// </summary>
	internal partial class CmPerson
	{
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>An array of hvos.</returns>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case CmPersonTags.kflidPositions:
					return m_cache.LangProject.PositionsOA;
				case CmPersonTags.kflidPlacesOfResidence:
					return m_cache.LangProject.LocationsOA;
				case CmPersonTags.kflidEducation:
					return m_cache.LangProject.EducationOA;
				case CmPersonTags.kflidPlaceOfBirth:
					return m_cache.LangProject.LocationsOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// Subclasses should override this property,
		/// if they want to show something other than
		/// the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				return BestAnalysisOrVernName(m_cache, this);
			}
		}
	}
	#endregion

	#region CmLocation class
	/// <summary>
	///
	/// </summary>
	internal partial class CmLocation
	{
		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// Subclasses should override this property,
		/// if they want to show something other than
		/// the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				return BestAnalysisOrVernName(m_cache, this);
			}
		}
	}
	#endregion

	#region CmMedia Class

	/// <summary>
	///
	/// </summary>
	internal partial class CmMedia
	{
		// JohnT: this was once overridden to delete the associated CmFile also, if nothing else uses it.
		// However we can no longer be sure of this without searching all strings in the system to see whether
		// any contain embedded links to the file. For now we are just accepting that some CmFiles will leak.
		//internal override void OnBeforeObjectDeleted()

	}
	#endregion // CmMedia

	#region FsComplexFeature Class
	/// <summary>
	/// Summary description for FsComplexFeature
	/// </summary>
	internal partial class FsComplexFeature
	{
		/// <summary>
		/// Overridden to handle Type.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case FsComplexFeatureTags.kflidType:
					return m_cache.LangProject.MsFeatureSystemOA;
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
				case FsComplexFeatureTags.kflidType:
					Set<ICmObject> set = new Set<ICmObject>();
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.TypesOC);
					return set;
				default:
					return base.ReferenceTargetCandidates(flid);
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
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				tisb.AppendTsString(Name.BestAnalysisAlternative);
				if (TypeRA != null)
				{
					var rs = TypeRA.FeaturesRS;
					if (rs.Count > 0)
					{
						tisb.Append("(");
						var count = 0;
						foreach (var defn in rs)
						{
							// Add sep after the first one.
							if (count++ > 0)
								tisb.Append(Strings.ksListSep);
							tisb.AppendTsString(defn.Name.BestAnalysisAlternative);
						}
						tisb.Append(")");
					}
				}
				return tisb.GetString();
			}
		}
	}
	#endregion

	#region FsClosedValue Class
	/// <summary>
	/// Summary description for FsClosedValue
	/// </summary>
	internal partial class FsClosedValue
	{
		private const string m_ksUnknown = "???";

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				ITsStrFactory tsf = Cache.TsStrFactory;
				int analWs = m_cache.DefaultAnalWs;

				tisb.AppendTsString(tsf.MakeString("[", analWs));

				IFsFeatDefn feature = FeatureRA;
				if (feature != null)
					tisb.AppendTsString(tsf.MakeString(feature.Name.BestAnalysisAlternative.Text, analWs));
				else
					tisb.AppendTsString(tsf.MakeString(Strings.ksQuestions, analWs));

				tisb.AppendTsString(tsf.MakeString(" : ", analWs));

				IFsSymFeatVal value = ValueRA;
				if (value != null)
					tisb.AppendTsString(tsf.MakeString(value.Name.BestAnalysisAlternative.Text, analWs));
				else
					tisb.AppendTsString(tsf.MakeString(Strings.ksQuestions, analWs));

				tisb.AppendTsString(tsf.MakeString("]", analWs));

				return tisb.GetString();
			}
		}

		/// <summary>
		/// True if the objects are considered equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return (other as IFsClosedValue).ValueRA == ValueRA;
		}

		/// <summary>
		/// A bracketed form e.g. [Gen:Masc]
		/// </summary>
		public string LongName
		{
			get { return LongNameTSS.Text; }
		}

		/// <summary>
		/// A bracketed form e.g. [Gen:Masc]
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				return GetFeatureValueString(true);
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
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return GetFeatureValueString(false);
			}
		}

		/// <summary>
		/// Get feature:value string
		/// </summary>
		/// <param name="fLongForm">use long form</param>
		/// <returns></returns>
		public ITsString GetFeatureValueString(bool fLongForm)
		{
			var tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultUserWs);

			var sFeature = GetFeatureString(fLongForm);
			var sValue = GetValueString(fLongForm);
			if ((!fLongForm) &&
				(FeatureRA != null) &&
				(FeatureRA.DisplayToRightOfValues))
			{
				tisb.Append(sValue);
				tisb.Append(sFeature);
			}
			else
			{
				tisb.Append(sFeature);
				tisb.Append(sValue);
			}
			return tisb.GetString();
		}

		private string GetValueString(bool fLongForm)
		{
			var sValue = "";
			if (ValueRA != null)
			{
				if (fLongForm || ValueRA.ShowInGloss)
				{
					sValue = ValueRA.Abbreviation.BestAnalysisAlternative.Text;
					if (sValue == null || sValue.Length == 0)
						sValue = ValueRA.Name.BestAnalysisAlternative.Text;
					if (!fLongForm)
						sValue = sValue + ValueRA.RightGlossSep.AnalysisDefaultWritingSystem.Text;
				}
			}
			else
				sValue = m_ksUnknown;
			return sValue;
		}

		private string GetFeatureString(bool fLongForm)
		{
			var sFeature = "";
			if (FeatureRA != null)
			{
				if (fLongForm || FeatureRA.ShowInGloss)
				{
					sFeature = FeatureRA.Abbreviation.BestAnalysisAlternative.Text;
					if (sFeature == null || sFeature.Length == 0)
						sFeature = FeatureRA.Name.BestAnalysisAlternative.Text;
					if (fLongForm)
						sFeature = sFeature + ":";
					else
						sFeature = sFeature + FeatureRA.RightGlossSep.BestAnalysisAlternative.Text;
				}
			}
			else
				sFeature = m_ksUnknown;
			return sFeature;
		}

		/// <summary>
		/// Overridden to handle reference props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case FsFeatureSpecificationTags.kflidFeature:
					return Owner;
				case FsClosedValueTags.kflidValue:
					return FeatureRA;
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
			Set<ICmObject> set = null;
			switch (flid)
			{
				case FsFeatureSpecificationTags.kflidFeature:
					// Find all exception features for the "owning" PartOfSpeech and all of its owning POSes
					set = GetFeatureList();
					break;
				case FsClosedValueTags.kflidValue:
					set = new Set<ICmObject>();
					if (FeatureRA != null)
					{
						IFsClosedFeature feat = FeatureRA as IFsClosedFeature;
						if (feat != null)
							set.AddRange(feat.ValuesOC);
					}
					break;
				default:
					return base.ReferenceTargetCandidates(flid);
			}
			return set;
		}

		/// <summary>
		/// Copy more properties as needed.
		/// </summary>
		internal override void SetMoreCloneProperties(ICmObject clone)
		{
			IFsClosedValue val = (IFsClosedValue)clone;
			val.ValueRA = ValueRA;
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsFeatureSpecification CreateNewObject()
		{
			return Services.GetInstance<IFsClosedValueFactory>().Create();
		}

		public class FsClosedValueComparer : IEqualityComparer<IFsClosedValue>
		{

			public bool Equals(IFsClosedValue value1, IFsClosedValue value2)
			{
				if (value1.FeatureRA.Hvo == value2.FeatureRA.Hvo &&
					value1.ValueRA.Hvo == value2.ValueRA.Hvo)
				{
					return true;
				}
				return false;
			}


			public int GetHashCode(IFsClosedValue value)
			{
				int hCode = value.FeatureRA.Hvo ^ value.ValueRA.Hvo;
				return hCode.GetHashCode();
			}

		}
	}
	#endregion

	#region FsComplexValue Class
	/// <summary>
	/// Summary description for FsComplexValue
	/// </summary>
	internal partial class FsComplexValue
	{
		private const string m_ksUnknown = "???";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the default values after the initialization of a CmObject. At the point that
		/// this method is called, the object should have an HVO, Guid, and a cache set.
		/// </summary>
		/// <remarks>
		/// [NB: This is *not* for use during object reconstitution.
		/// Use DoAdditionalReconstruction() during object reconstitution.]
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			ValueOA = Services.GetInstance<IFsFeatStrucFactory>().Create();
		}

		/// <summary>
		/// True if the objects are considered equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			var otherValue = (other as IFsComplexValue).ValueOA;
			var thisValue = ValueOA;
			if (otherValue == null && thisValue == null)
				return true;
			return otherValue.IsEquivalent(thisValue);
		}

		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public string LongName
		{
			get { return LongNameTSS.Text; }
		}

		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				return GetFeatureValueString(true);
			}
		}

		/// <summary>
		/// A sorted bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public string LongNameSorted
		{
			get { return LongNameSortedTSS.Text; }
		}

		/// <summary>
		/// A sorted bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public ITsString LongNameSortedTSS
		{
			get
			{
				return GetFeatureValueStringSorted();
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
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return GetFeatureValueString(false);
			}
		}

		private ITsString GetFeatureValueString(bool fLongForm)
		{
			var tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
				0, Cache.DefaultAnalWs);
			var sFeature = GetFeatureString(fLongForm);
			var sValue = GetValueString(fLongForm);
			tisb.Append(sFeature);
			tisb.Append(sValue);
			return tisb.GetString();
		}

		private ITsString GetFeatureValueStringSorted()
		{
			var tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
				0, Cache.DefaultAnalWs);
			var sFeature = GetFeatureString(true);
			tisb.Append(sFeature);
			tisb.AppendTsString((ValueOA as FsFeatStruc).GetFeatureValueStringSorted());
			return tisb.GetString();
		}

		private string GetValueString(bool fLongForm)
		{
			var sValue = "";
			if (ValueOA != null)
			{
				var fs = ValueOA as IFsFeatStruc;
				if (fs != null)
				{
					sValue = fLongForm ? fs.LongName : fs.ShortName;
				}
			}
			else
				sValue = m_ksUnknown;
			return sValue;
		}

		private string GetFeatureString(bool fLongForm)
		{
			var sFeature = "";
			if (FeatureRA != null)
			{
				if (fLongForm || FeatureRA.ShowInGloss)
				{
					sFeature = FeatureRA.Abbreviation.BestAnalysisAlternative.Text;
					if (string.IsNullOrEmpty(sFeature))
						sFeature = FeatureRA.Name.BestAnalysisAlternative.Text;
					sFeature = fLongForm ? sFeature + ":" : sFeature + FeatureRA.RightGlossSep.BestAnalysisAlternative.Text;
				}
			}
			else
				sFeature = m_ksUnknown;
			return sFeature;
		}

		/// <summary>
		/// Copy more properties as needed.
		/// </summary>
		internal override void SetMoreCloneProperties(ICmObject clone)
		{
			IFsComplexValue val = (IFsComplexValue)clone;
			val.ValueOA = (ValueOA as FsAbstractStructure).CreateNewObject();
			ValueOA.SetCloneProperties(val.ValueOA);
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsFeatureSpecification CreateNewObject()
		{
			return Services.GetInstance<IFsComplexValueFactory>().Create();
		}
	}
	#endregion

	#region FsFeatureSystem Class
	/// <summary>
	///
	/// </summary>
	internal partial class FsFeatureSystem
	{
		/// <summary>
		/// Gets the symbolic value with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The symbolic value, or <c>null</c> if not found.</returns>
		public IFsSymFeatVal GetSymbolicValue(string id)
		{
			foreach (var feat in FeaturesOC)
			{
				var closed = feat as IFsClosedFeature;
				if (closed != null)
				{
					var symval = closed.GetSymbolicValue(id);
					if (symval != null)
						return symval;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the feature with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The feature, or <c>null</c> if not found.</returns>
		public IFsFeatDefn GetFeature(string id)
		{
			foreach (var feat in FeaturesOC)
			{
				if (feat.CatalogSourceId == id)
					return feat;
			}
			return null;
		}

		/// <summary>
		/// Gets the type with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The type, or <c>null</c> if not found.</returns>
		public IFsFeatStrucType GetFeatureType(string id)
		{
			foreach (var type in TypesOC)
			{
				if (type.CatalogSourceId == id)
					return type;
			}
			return null;
		}

		/// <summary>
		/// Add a feature to the feature system (unless it's already there)
		/// </summary>
		/// <param name="item">the node containing a description of the feature</param>
		/// <returns></returns>
		public IFsFeatDefn AddFeatureFromXml(XmlNode item)
		{
			if (item == null)
				return null;

			var fst = GetOrCreateFeatureTypeFromXml(item);
			if (fst == null)
				return null;
			return GetOrCreateFeatureFromXml(item, fst);
		}

		/// <summary>
		/// Gets the feature based on XML or creates it if not found
		/// </summary>
		/// <param name="item">Xml description of the item</param>
		/// <param name="fst">the type</param>
		/// <returns></returns>
		public IFsFeatDefn GetOrCreateFeatureFromXml(XmlNode item, IFsFeatStrucType fst)
		{
			IFsClosedFeature closed = null;
			IFsComplexFeature complex = null;
			XmlNode feature = item.SelectSingleNode("fs/f");
			if (feature != null)
			{
				var featName = feature.SelectSingleNode("@name");
				var fs = feature.SelectSingleNode("fs");
				if (fs != null)
				{
					// do complex part
					var complexFst = GetOrCreateFeatureTypeFromXml(feature);
					complex = GetOrCreateComplexFeatureFromXml(item, fst, complexFst);
					// use the type of the complex feature for the closed feature
					fst = complexFst;
				}
				closed = GetOrCreateClosedFeatureFromXml(item);
				SetFeaturesInTypeFromXml(fst, item, closed);
				closed.GetOrCreateSymbolicValueFromXml(feature, item);
			}
			if (complex != null)
				return complex;
			else
				return closed;
		}

		/// <summary>
		/// Gets a feature structure type or creates it if not already there.
		/// </summary>
		/// <param name="item">The XML item.</param>
		/// <returns>
		/// FsFeatStrucType corresponding to the type
		/// </returns>
		public IFsFeatStrucType GetOrCreateFeatureTypeFromXml(XmlNode item)
		{
			// Be careful not to dereference a null.  See FWR-1093.
			if (item == null)
				return null;
			XmlNode xn = item.SelectSingleNode("fs/@type");
			if (xn == null)
				return null;
			var type = xn.InnerText;
			if (String.IsNullOrEmpty(type))
				return null;

			var fst = GetFeatureType(type);
			if (fst == null)
			{
				// The XML is from a file shipped with FieldWorks. It is quite likely multiple users
				// of a project could independently add the same items, so we create them with fixed guids
				// so merge will recognize them as the same objects.
				string guid = XmlUtils.GetAttributeValue(item.SelectSingleNode("fs"), "typeguid");
				fst = Services.GetInstance<IFsFeatStrucTypeFactory>().Create(new Guid(guid), this);
				fst.CatalogSourceId = type;

				var parentFsType = item.SelectSingleNode("ancestor::item[@type='fsType' and not(@status)]");
				if (parentFsType == null)
				{
					// do not have any real values for abbrev, name, or description.  Just use the abbreviation
					foreach (IWritingSystem ws in Services.WritingSystems.AnalysisWritingSystems)
					{
						var tss = m_cache.TsStrFactory.MakeString(type, ws.Handle);
						fst.Abbreviation.set_String(ws.Handle, tss);
						fst.Name.set_String(ws.Handle, tss);
					}
				}
				else
				{
					var abbr = parentFsType.SelectSingleNode("abbrev");
					SetInnerText(m_cache, fst.Abbreviation, abbr);
					var term = parentFsType.SelectSingleNode("term");
					SetInnerText(m_cache, fst.Name, term);
					var def = parentFsType.SelectSingleNode("def");
					SetInnerText(m_cache, fst.Description, def);
				}
			}
			return fst;
		}

		/// <summary>
		/// Gets a complex feature or creates it if not already there; use XML item to do it
		/// </summary>
		/// <param name="item">XML item</param>
		/// <param name="fst">feature structure type which refers to this complex feature</param>
		/// <param name="complexFst">feature structure type of the complex feature</param>
		/// <returns>
		/// IFsComplexFeature corresponding to the name
		/// </returns>
		public IFsComplexFeature GetOrCreateComplexFeatureFromXml(XmlNode item, IFsFeatStrucType fst,
			IFsFeatStrucType complexFst)
		{
			XmlNode id = item.SelectSingleNode("../../../@id");
			if (id == null)
				return null;
			var complex = GetFeature(id.InnerText) as IFsComplexFeature;
			if (complex == null)
			{
				// The XML is from a file shipped with FieldWorks. It is quite likely multiple users
				// of a project could independently add the same items, so we create them with fixed guids
				// so merge will recognize them as the same objects.
				var guid = new Guid(item.SelectSingleNode("../../../@guid").InnerText);
				complex = Services.GetInstance<IFsComplexFeatureFactory>().Create(guid, this);

				complex.CatalogSourceId = id.InnerText;
				var abbr = item.SelectSingleNode("../../../abbrev");
				SetInnerText(m_cache, complex.Abbreviation, abbr);
				var term = item.SelectSingleNode("../../../term");
				SetInnerText(m_cache, complex.Name, term);
				var def = item.SelectSingleNode("../../../def");
				SetInnerText(m_cache, complex.Description, def);
				SetFeaturesInTypeFromXml(fst, item, complex);
				complex.TypeRA = complexFst;
			}
			return complex;
		}

		/// <summary>
		/// Gets a close feature or creates it if not already there; use XML item to do it
		/// </summary>
		/// <param name="item">XML item</param>
		/// <returns>
		/// FsClosedFeature corresponding to the name
		/// </returns>
		public IFsClosedFeature GetOrCreateClosedFeatureFromXml(XmlNode item)
		{
			var id = item.SelectSingleNode("../@id");
			var closed = GetFeature(id.InnerText) as IFsClosedFeature;
			if (closed == null)
			{
				// The XML is from a file shipped with FieldWorks. It is quite likely multiple users
				// of a project could independently add the same items, so we create them with fixed guids
				// so merge will recognize them as the same objects.
				var guid = new Guid(item.ParentNode.Attributes["guid"].Value);
				closed = Services.GetInstance<IFsClosedFeatureFactory>().Create(guid, this);
				FeaturesOC.Add(closed);

				closed.CatalogSourceId = id.InnerText;
				var abbr = item.SelectSingleNode("../abbrev");
				SetInnerText(m_cache, closed.Abbreviation, abbr);
				var term = item.SelectSingleNode("../term");
				SetInnerText(m_cache, closed.Name, term);
				var def = item.SelectSingleNode("../def");
				SetInnerText(m_cache, closed.Description, def);
			}
			return closed;
		}

		private void SetFeaturesInTypeFromXml(IFsFeatStrucType fst, XmlNode item, IFsFeatDefn featDefn)
		{
			if (fst == null)
				return;
			var id = item.SelectSingleNode("../@id");
			if (fst.GetFeature(id.InnerText) == null)
			{
				if (featDefn.ClassID == FsComplexFeatureTags.kClassId)
				{ // for complex features, if they have a higher containing feature structure type, then all we want are the closed features
					if (item.SelectSingleNode("ancestor::item[@type='fsType' and not(@status)]") != null)
						return;
				}
				fst.FeaturesRS.Add(featDefn);
			}
		}

		internal static void SetInnerText(FdoCache cache, ITsMultiString item, XmlNode node)
		{
			if (node != null)
			{
				var ws = cache.WritingSystemFactory.GetWsFromStr(XmlUtils.GetAttributeValue(node, "ws"));
				var newValue = node.InnerText;
				if (ws <= 0)
				{
					ws = cache.DefaultAnalWs;
					newValue = node.InnerXml;
				}
				item.set_String(ws, cache.TsStrFactory.MakeString(newValue, ws));
			}
		}
	}
	#endregion

	#region FsFeatStrucType Class
	internal partial class FsFeatStrucType
	{
		/// <summary>
		/// Gets the feature with the specified catalog ID.
		/// </summary>
		/// <param name="id">The catalog ID.</param>
		/// <returns>The feature, or <c>null</c> if not found.</returns>
		public IFsFeatDefn GetFeature(string id)
		{
			foreach (var feat in FeaturesRS)
			{
				if (feat.CatalogSourceId == id)
					return feat;
			}
			return null;
		}

		/// <summary>
		/// Overridden to handle Features.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case FsFeatStrucTypeTags.kflidFeatures:
					return Owner;
				default:
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
				case FsFeatStrucTypeTags.kflidFeatures:
					var featsys = OwnerOfClass<IFsFeatureSystem>();
					return featsys.FeaturesOC.Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
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
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return Name.BestAnalysisAlternative;
			}
		}
	}
	#endregion

	#region FsAbstractStructure Class
	/// <summary>
	///
	/// </summary>
	internal partial class FsAbstractStructure
	{
		/// <summary>
		/// True if the objects are considered equivalent. This base class has no properties so
		/// the objects are trivially equivalent if of the same class.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public virtual bool IsEquivalent(IFsAbstractStructure other)
		{
			if (other == null)
				return false;

			// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			return other.GetType() == GetType();
		}


		#region ICloneableCmObject Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <param name="clone">Destination object for clone operation</param>
		/// ------------------------------------------------------------------------------------
		public void SetCloneProperties(ICmObject clone)
		{
			CopyPropertiesTo(clone);
		}
		#endregion

		/// <summary>
		/// Copy all the properties from this object to the target (clone).
		/// </summary>
		internal abstract void CopyPropertiesTo(ICmObject clone);
		/// <summary>
		/// Make a new object of this class.  This must be implemented by each subclass.
		/// </summary>
		internal abstract IFsAbstractStructure CreateNewObject();
	}
	#endregion

	#region FsFeatureSpecification Class
	internal partial class FsFeatureSpecification
	{
		/// <summary>
		/// True if the objects are considered equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public virtual bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (other == null)
				return false;

			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (other.GetType() != GetType())
				return false;

			if (other.RefNumber != RefNumber)
				return false;
			if (other.ValueState != ValueState)
				return false;
			if (other.FeatureRA != FeatureRA)
				return false;

			return true;
		}

		/// <summary>
		/// Find all exception features for the "owning" PartOfSpeech and all of its owning POSes
		/// </summary>
		/// <returns>list of these features</returns>
		protected Set<ICmObject> GetFeatureList()
		{
			Set<ICmObject> set = new Set<ICmObject>();
			IFsFeatStruc fs = Owner as IFsFeatStruc;
			if (fs != null)
			{
				ICmObject msaOwner = GetMsaOwnerOfFs(fs);
				IMoMorphSynAnalysis msa = msaOwner as IMoMorphSynAnalysis;
				if (msa == null)
					msa = TryEndocentricCompound(fs);

				if (msa != null)
				{
					IPartOfSpeech pos = null;
					switch (msa.ClassID)
					{
						case MoStemMsaTags.kClassId:
							IMoStemMsa stemMsa = msa as IMoStemMsa;
							pos = stemMsa.PartOfSpeechRA;
							break;
						case MoInflAffMsaTags.kClassId:
							IMoInflAffMsa inflMsa = msa as IMoInflAffMsa;
							pos = inflMsa.PartOfSpeechRA;
							break;
						case MoDerivAffMsaTags.kClassId:
							IMoDerivAffMsa derivMsa = msa as IMoDerivAffMsa;
							if (derivMsa.FromProdRestrictRC.Contains((ICmPossibility)fs))
								pos = derivMsa.FromPartOfSpeechRA;
							else
								pos = derivMsa.ToPartOfSpeechRA;
							break;
						case MoUnclassifiedAffixMsaTags.kClassId:
							IMoUnclassifiedAffixMsa unclassMsa = msa as IMoUnclassifiedAffixMsa;
							pos = unclassMsa.PartOfSpeechRA;
							break;
					}
					while (pos != null)
					{
						switch (GetNonFSOwningFlid(fs))
						{
							case PartOfSpeechTags.kflidDefaultFeatures: // fall through
							case MoDerivAffMsaTags.kflidFromMsFeatures: // fall through
							case MoDerivAffMsaTags.kflidToMsFeatures: // fall through
							case MoInflAffMsaTags.kflidInflFeats: // fall through
							case MoStemMsaTags.kflidMsFeatures:
								set.AddRange(pos.InflectableFeatsRC);
								break;
							case MoCompoundRuleTags.kflidToProdRestrict: // fall through
							case MoDerivAffMsaTags.kflidFromProdRestrict: // fall through
							case MoDerivAffMsaTags.kflidToProdRestrict: // fall through
							case MoInflAffMsaTags.kflidFromProdRestrict: // fall through
							case MoStemMsaTags.kflidProdRestrict:
								set.AddRange(pos.BearableFeaturesRC);
								break;
						}
						pos = Owner as IPartOfSpeech;
					}
				}
			}
			return set;
		}

		private int GetNonFSOwningFlid(IFsFeatStruc fs)
		{
			if (fs == null)
				return 0;
			int flid = fs.OwningFlid;
			ICmObject owner = fs.Owner; // prime the pump
			while (true)
			{ // pump
				IFsComplexValue complex = owner as IFsComplexValue;
				if (complex == null)
					return flid; // it's not nested; return owning flid
				IFsFeatStruc fsOwner = complex.Owner as IFsFeatStruc;
				if (fsOwner == null)
					return 0; // give up; don't know what's going on
				owner = fsOwner.Owner;
				flid = fsOwner.OwningFlid;
			}

		}

		private ICmObject GetMsaOwnerOfFs(IFsFeatStruc fs)
		{
			if (fs == null)
				return null;
			ICmObject owner = fs.Owner; // prime the pump
			while (true)
			{ // pump
				IMoMorphSynAnalysis msa = owner as IMoMorphSynAnalysis;
				if (msa != null)
					return msa; // found msa owner
				IFsComplexValue complex = owner as IFsComplexValue;
				if (complex == null)
					return null; // give up; don't know what's going on
				IFsFeatStruc fsOwner = complex.Owner as IFsFeatStruc;
				if (fsOwner == null)
					return null; // give up; don't know what's going on
				owner = fsOwner.Owner;
			}

		}

		/// <summary>
		/// See if the owner of the feature structure is an endocentric compound.
		/// If so, use the stem msa of the head.
		/// </summary>
		/// <param name="fs">feature structure we're checking</param>
		private IMoMorphSynAnalysis TryEndocentricCompound(IFsFeatStruc fs)
		{
			IMoEndoCompound endo = (IMoEndoCompound)fs.Owner;
			if (endo != null)
				return (endo.HeadLast) ? endo.RightMsaOA : endo.LeftMsaOA;
			return null;
		}

		/// <summary>
		/// Overridden to handle Features.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case FsFeatureSpecificationTags.kflidFeature:
					return m_cache.LangProject.MsFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Override to handle reference props of this class.
		/// </summary>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case FsFeatureSpecificationTags.kflidFeature:
					Set<ICmObject> set = new Set<ICmObject>();
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.FeaturesOC);
					return set;
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		#region ICloneableCmObject Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <param name="clone">Destination object for clone operation</param>
		/// ------------------------------------------------------------------------------------
		public void SetCloneProperties(ICmObject clone)
		{
			IFsFeatureSpecification spec = (IFsFeatureSpecification)clone;
			spec.RefNumber = RefNumber;
			spec.ValueState = ValueState;
			spec.FeatureRA = FeatureRA;		// Is this correct???
			SetMoreCloneProperties(clone);
		}
		#endregion

		/// <summary>
		/// Copy more properties as needed.  This must be implemented by each subclass.
		/// </summary>
		internal abstract void SetMoreCloneProperties(ICmObject clone);
		/// <summary>
		/// Make a new object of this class.  This must be implemented by each subclass.
		/// </summary>
		internal abstract IFsFeatureSpecification CreateNewObject();
	}
	#endregion

	#region FsNegatedValue Class
	/// <summary>
	/// Summary description for FsNegatedValue.
	/// </summary>
	internal partial class FsNegatedValue
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return (other as IFsNegatedValue).ValueRA == ValueRA;
		}

		/// <summary>
		/// Override to handle reference props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case FsFeatureSpecificationTags.kflidFeature:
					return m_cache.LangProject.MsFeatureSystemOA;
				case FsNegatedValueTags.kflidValue:
					return FeatureRA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Get a list of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>An array of hvos.</returns>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case FsFeatureSpecificationTags.kflidFeature:
					// Find all exception features for the "owning" PartOfSpeech and all of its owning POSes
					return GetFeatureList();
				case FsNegatedValueTags.kflidValue:
					Set<ICmObject> set = new Set<ICmObject>();
					if (FeatureRA != null)
					{
						IFsClosedFeature feat = FeatureRA as IFsClosedFeature;
						if (feat != null)
							set.AddRange(feat.ValuesOC);
					}
					return set;
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}


		/// <summary>
		/// Copy more properties as needed.
		/// </summary>
		internal override void SetMoreCloneProperties(ICmObject clone)
		{
			IFsNegatedValue val = (IFsNegatedValue)clone;
			val.ValueRA = ValueRA;
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsFeatureSpecification CreateNewObject()
		{
			return Services.GetInstance<IFsNegatedValueFactory>().Create();
		}
	}
	#endregion

	#region FsDisjunctiveValue Class
	/// <summary>
	///
	/// </summary>
	internal partial class FsDisjunctiveValue
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return (other as IFsDisjunctiveValue).ValueRC.IsEquivalent(ValueRC);
		}

		/// <summary>
		/// Copy more properties as needed.
		/// </summary>
		internal override void SetMoreCloneProperties(ICmObject clone)
		{
			IFsDisjunctiveValue disjValue = (IFsDisjunctiveValue)clone;
			foreach (var val in ValueRC)
				disjValue.ValueRC.Add(val);
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsFeatureSpecification CreateNewObject()
		{
			return Services.GetInstance<IFsDisjunctiveValueFactory>().Create();
		}
	}
	#endregion

	#region FsOpenValue Class
	/// <summary>
	///
	/// </summary>
	internal partial class FsOpenValue
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// Since FdoOpenValue isn't used yet, this is something of a skeleton. We just check
		/// the analysis writing systems.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return Value.AnalysisDefaultWritingSystem.Equals(((IFsOpenValue)other).Value.AnalysisDefaultWritingSystem);
		}

		/// <summary>
		/// Copy more properties as needed.
		/// </summary>
		internal override void SetMoreCloneProperties(ICmObject clone)
		{
			IFsOpenValue val = (IFsOpenValue)clone;
			val.Value.CopyAlternatives(Value);
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsFeatureSpecification CreateNewObject()
		{
			return Services.GetInstance<IFsOpenValueFactory>().Create();
		}
	}
	#endregion

	#region FsFeatStrucDisj Class
	/// <summary>
	///
	/// </summary>
	internal partial class FsFeatStrucDisj
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsAbstractStructure other)
		{
			if (!base.IsEquivalent(other))
				return false;

			var otherContents = (other as IFsFeatStrucDisj).ContentsOC;
			var thisContents = ContentsOC;
			if (otherContents.Count != thisContents.Count)
				return false;
			foreach (var fsOther in otherContents)
			{
				var fMatch = false;
				foreach (var fsThis in thisContents)
				{
					if (fsThis.IsEquivalent(fsOther))
					{
						fMatch = true;
						break;
					}
				}
				if (!fMatch)
					return false;
			}
			return true;
		}

		#region ICloneableCmObject Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <param name="clone">Destination object for clone operation</param>
		/// ------------------------------------------------------------------------------------
		public new void SetCloneProperties(ICmObject clone)
		{
			CopyPropertiesTo(clone);
		}
		#endregion

		/// <summary>
		/// Copy all the properties from this object to the target (clone).
		/// </summary>
		internal override void CopyPropertiesTo(ICmObject clone)
		{
			IFsFeatStrucDisj disj = (IFsFeatStrucDisj)clone;
			foreach (var oldFeat in ContentsOC)
			{
				IFsFeatStruc newFeat = (oldFeat as FsFeatStruc).CreateNewObject() as IFsFeatStruc;
				disj.ContentsOC.Add(newFeat);
				oldFeat.SetCloneProperties(newFeat);
			}
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsAbstractStructure CreateNewObject()
		{
			return Services.GetInstance<IFsFeatStrucDisjFactory>().Create();
		}
	}
	#endregion

	#region FsSharedValue Class
	/// <summary>
	///
	/// </summary>
	internal partial class FsSharedValue
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return ((IFsSharedValue)other).ValueRA == ValueRA;
		}

		/// <summary>
		/// Copy more properties as needed.
		/// </summary>
		internal override void SetMoreCloneProperties(ICmObject clone)
		{
			IFsSharedValue shared = (IFsSharedValue)clone;
			shared.ValueRA = ValueRA;
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsFeatureSpecification CreateNewObject()
		{
			return Services.GetInstance<IFsSharedValueFactory>().Create();
		}
	}
	#endregion

	#region FsSymFeatVal Class
	internal partial class FsSymFeatVal
	{
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return Name.BestAnalysisAlternative;
			}
		}

		/// <summary>
		/// Set abbreviation and name; also set show in gloss to true
		/// </summary>
		/// <param name="sAbbrev"></param>
		/// <param name="sName"></param>
		public void SimpleInit(string sAbbrev, string sName)
		{
			foreach (IWritingSystem ws in Services.WritingSystems.AnalysisWritingSystems)
			{
				Abbreviation.set_String(ws.Handle, Cache.TsStrFactory.MakeString(sAbbrev, ws.Handle));
				if (ws.Id == "en")
					Name.set_String(ws.Handle, Cache.TsStrFactory.MakeString(sName, ws.Handle));
			}
			ShowInGloss = true;
		}

		List<ICmObject> m_backrefsToDelete;

		protected override void OnBeforeObjectDeleted()
		{
			base.OnBeforeObjectDeleted();
			CollectDefunctReferringObjects();
			DeleteDefunctReferringObjects();
		}

		private void CollectDefunctReferringObjects()
		{
			if (m_backrefsToDelete == null)
				m_backrefsToDelete = new List<ICmObject>();
			else
				m_backrefsToDelete.Clear();
			foreach (ICmObject obj in ReferringObjects)
			{
				if (obj is IFsClosedValue)
					m_backrefsToDelete.Add(obj);
			}
		}

		/// <summary>
		/// Finish the process of deleting any referring IFsClosedValue objects, which become
		/// defunct when this object is deleted.
		/// </summary>
		/// <remarks>
		/// See FWR-2799.
		/// The WANTPORT was originally handled by implementing OnBeforeObjectDeleted and
		/// RemoveObjectSideEffectsInternal.  But the latter was never called, because it
		/// relates to owning the object being removed, not to being that object.
		/// </remarks>
		private void DeleteDefunctReferringObjects()
		{
			if (m_backrefsToDelete != null)
			{
				foreach (ICmObject obj in m_backrefsToDelete)
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
				m_backrefsToDelete.Clear();
			}
		}
	}
	#endregion

	#region FsFeatStruc Class
	/// <summary>
	/// Summary description for FsFeatStruc
	/// </summary>
	internal partial class FsFeatStruc
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// This will help when there is no string in the Name field.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteFeatureSet, " "));
				tisb.AppendTsString(LongNameTSS);
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get an extant closed value.
		/// </summary>
		/// <param name="closedFeat">FsClosedFeature to find</param>
		/// <returns>FsClosedValue</returns>
		public IFsClosedValue GetValue(IFsClosedFeature closedFeat)
		{
			foreach (var spec in FeatureSpecsOC)
			{
				if (spec.FeatureRA == closedFeat)
					return spec as IFsClosedValue;
			}
			return null;
		}

		/// <summary>
		/// Get an extant closed value or create a new one if not already there.
		/// </summary>
		/// <param name="closedFeat">FsClosedFeature to find</param>
		/// <returns>FsComplexValue</returns>
		public IFsClosedValue GetOrCreateValue(IFsClosedFeature closedFeat)
		{
			var val = GetValue(closedFeat);
			if (val == null)
			{
				// Not found; create a new one.
				val = new FsClosedValue();
				FeatureSpecsOC.Add(val);
			}
			return val;
		}

		/// <summary>
		/// Get an extant complex value.
		/// </summary>
		/// <param name="complexFeat">FsComplexFeature to find</param>
		/// <returns>FsComplexValue</returns>
		public IFsComplexValue GetValue(IFsComplexFeature complexFeat)
		{
			foreach (var spec in FeatureSpecsOC)
			{
				if (spec.FeatureRA == complexFeat)
					return spec as IFsComplexValue;
			}
			return null;
		}

		/// <summary>
		/// Get an extant complex value or create a new one if not already there.
		/// </summary>
		/// <param name="complexFeat">FsComplexFeature to find</param>
		/// <returns>FsComplexValue</returns>
		public IFsComplexValue GetOrCreateValue(IFsComplexFeature complexFeat)
		{
			var val = GetValue(complexFeat);
			if (val == null)
			{
				// Not found; create a new one.
				val = new FsComplexValue();
				FeatureSpecsOC.Add(val);
			}
			return val;
		}

		/// <summary>
		/// Add features based on an XML description
		/// </summary>
		/// <param name="item">the node containing a description of the feature</param>
		/// <param name="featsys">The feature system.</param>
		public void AddFeatureFromXml(XmlNode item, IFsFeatureSystem featsys)
		{
			if (item == null)
				return;

			var fst = featsys.GetOrCreateFeatureTypeFromXml(item);
			if (fst == null)
				return;

			TypeRA = fst;

			var feature = item.SelectSingleNode("fs/f");
			if (feature != null)
			{
				var featName = feature.SelectSingleNode("@name");
				var fs = feature.SelectSingleNode("fs");
				IFsFeatStruc featStruct = this;
				if (fs != null)
				{
					// do complex part
					var complexFst = featsys.GetOrCreateFeatureTypeFromXml(item);
					var complexFeat = featsys.GetOrCreateComplexFeatureFromXml(item, fst, complexFst);
					var complexValue = GetOrCreateValue(complexFeat);
					if (complexFeat != null)
						complexValue.FeatureRA = complexFeat;
					if (complexValue.ValueOA == null)
						complexValue.ValueOA = new FsFeatStruc();
					featStruct = (IFsFeatStruc)complexValue.ValueOA;
					if (fst != null)
						featStruct.TypeRA = fst;
				}
				var closedFeat = featsys.GetOrCreateClosedFeatureFromXml(item);
				var closedValue = (featStruct as IFsFeatStruc).GetOrCreateValue(closedFeat);
				if (closedFeat != null)
					closedValue.FeatureRA = closedFeat;

				var fsfv = closedFeat.GetOrCreateSymbolicValueFromXml(feature, item);
				if (fsfv != null)
					closedValue.ValueRA = fsfv;
			}
		}

		/// <summary>
		/// Answer true if this feature structure is 'empty', that is, equivalent to not having
		/// one at all.
		/// </summary>
		/// <returns></returns>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsEmpty
		{
			get { return TypeRA == null && FeatureDisjunctionsOC.Count == 0 && FeatureSpecsOC.Count == 0; }

			// TODO: The following definition of IsEmpty is more accurate for LiftExporter,
			// but it is used in so many places that it's not trivial to make sure it doesn't break something else.

			// FeatureDisjunctions not yet used. Export should consider a FeatStruc as empty if either Type
			// or FeatureSpecs is empty. (LT-13596)
			//get { return TypeRA == null || FeatureSpecsOC.Count == 0; }
		}

		/// <summary>
		/// Answer true if the argument feature structure is equivalent to the recipient.
		/// Review JohnT: should we just call this Equals?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsAbstractStructure other)
		{
			if (other == null)
				return this.IsEmpty;
			if (!base.IsEquivalent(other))
				return false;

			var fs = other as IFsFeatStruc;
			if (fs.TypeRA != TypeRA)
				return false;
			var otherFeatures = fs.FeatureSpecsOC;
			var thisFeatures = FeatureSpecsOC;
			if (otherFeatures.Count != thisFeatures.Count)
				return false;
			var otherDisjunctions = fs.FeatureDisjunctionsOC;
			var thisDisjunctions = FeatureDisjunctionsOC;
			if (otherDisjunctions.Count != thisDisjunctions.Count)
				return false;
			foreach (var fsOther in otherFeatures)
			{
				bool fMatch = false;
				foreach (var fsThis in thisFeatures)
				{
					if (fsThis.IsEquivalent(fsOther))
					{
						fMatch = true;
						break;
					}
				}
				if (!fMatch)
					return false;
			}
			foreach (var fsdOther in otherDisjunctions)
			{
				var fMatch = false;
				foreach (var fsdThis in thisDisjunctions)
				{
					if (fsdThis.IsEquivalent(fsdOther))
					{
						fMatch = true;
						break;
					}
				}
				if (!fMatch)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get the value, if any, for the specifed feature name
		/// </summary>
		/// <param name="featureName"></param>
		/// <returns></returns>
		public ITsString GetFeatureValueTSS(string featureName)
		{
			var tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultUserWs);

			var features = from s in FeatureSpecsOC
						  where s.FeatureRA.Abbreviation.BestAnalysisAlternative.Text == featureName
						  select s;
			if (features.Any())
			{
				var feature = features.First();
				if (feature != null)
				{
					var closedValue = feature as FsClosedValue;
					if (closedValue != null)
					{

						tisb.AppendTsString(closedValue.ValueRA.Abbreviation.BestAnalysisAlternative);
					}
				}
			}
			return tisb.GetString();
		}

		/// <summary>
		/// A bracketed form prefixed by the Type (abbreviation) in braces.
		/// </summary>
		public string LiftName
		{
			get
			{
				if (TypeRA == null)
					return LongName;
				else
					return String.Format("{{{0}}}{1}",
						TypeRA.Abbreviation.BestAnalysisVernacularAlternative.Text, LongName);
			}
		}

		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public string LongName
		{
			get { return LongNameTSS.Text; }
		}

		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString LongNameTSS
		{
			get
			{
				return GetFeatureValueString(true);
			}
		}

		/// <summary>
		/// A bracketed form sorted at each level e.g. [Asp:Aor NounAgr:[Gen:Masc Num:Sg Pers:1]]
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString LongNameSortedTSS
		{
			get
			{
				return GetFeatureValueStringSorted();
			}
		}
		/// <summary>
		/// A bracketed form sorted at each level e.g. [Asp:Aor NounAgr:[Gen:Masc Num:Sg Pers:1]]
		/// </summary>
		public string LongNameSorted
		{
			get { return LongNameSortedTSS.Text; }
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case FsFeatStrucTags.kflidFeatureSpecs:
					LongNameTSSChanged();
					break;
			}
			base.AddObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// Something changed which may cause our LongNameTSS to be invalid. Ensure a PropChanged will update views.
		/// </summary>
		internal void LongNameTSSChanged()
		{
			int flid = m_cache.MetaDataCache.GetFieldId2(FsFeatStrucTags.kClassId, "LongNameTSS", false);
			ITsString tssLongNameTSS = LongNameTSS;
			// We can't get a true old value, but a string the same length with different characters should cause the appropriate display
			// updating. Pathologically, the old value might differ in length; if that causes a problem at some point, we'll have to
			// deal with it.
			ITsStrBldr bldr = tssLongNameTSS.GetBldr();
			StringBuilder sb = new StringBuilder(bldr.Length);
			sb.Append(' ', bldr.Length);
			bldr.Replace(0, bldr.Length, sb.ToString(), null);
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(this, flid, bldr.GetString(), tssLongNameTSS);
		}

		/// <summary>
		/// Perform a priority union operation:
		/// Priority union is the same as unification except for handling conflicts and storing results.
		/// In unification, a conflict causes the operation to fail. In priority union, a conflict is resolved
		/// by taking the value in the argument feature structure. In unification, both the current feature
		/// structure and the argument feature structure are replaced by the unified result.
		/// In priority union, only the current feature structure is replaced by the result.
		/// The argument feature structure is not changed.
		/// </summary>
		/// <param name="fsNew">the argument feature structure</param>
		public void PriorityUnion(IFsFeatStruc fsNew)
		{
			var commonFeatures = from newItem in fsNew.FeatureSpecsOC
								 from myitem in FeatureSpecsOC
								 where (newItem is IFsClosedValue && (newItem as IFsClosedValue).FeatureRA.Name == myitem.FeatureRA.Name) ||
								 (newItem is IFsComplexValue && (newItem as IFsComplexValue).FeatureRA.Name == myitem.FeatureRA.Name)
								 select newItem;
			var nonCommonFeatures = from newItem in fsNew.FeatureSpecsOC
									where !commonFeatures.Contains(newItem)
									select newItem;
			foreach (var spec in commonFeatures)
			{
				IFsFeatureSpecification spec1 = spec;
				var myFeatureValues = from v in FeatureSpecsOC
									  where v.FeatureRA.Name == spec1.FeatureRA.Name
									  select v;
				var closed = myFeatureValues.First() as IFsClosedValue;
				if (closed != null)
				{
					var newClosedValue = spec as IFsClosedValue;
					if (newClosedValue != null)
						closed.ValueRA = newClosedValue.ValueRA;
				}
				else
				{
					var complex = myFeatureValues.First() as IFsComplexValue;
					if (complex != null)
					{
						var newComplexValue = spec as IFsComplexValue;
						if (newComplexValue != null)
						{
							var fs = complex.ValueOA as IFsFeatStruc;
							if (fs != null)
								fs.PriorityUnion((spec as IFsComplexValue).ValueOA as IFsFeatStruc);
						}
					}
				}
			}
			foreach (var spec in nonCommonFeatures)
			{
				CopyObject<IFsFeatureSpecification>.CloneFdoObject(spec,
						newSpec => FeatureSpecsOC.Add(newSpec));
			}

		}
		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case FsFeatStrucTags.kflidFeatureSpecs:
					LongNameTSSChanged();
					break;
			}
			base.RemoveObjectSideEffectsInternal(e);
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return GetFeatureValueString(false);
			}
		}

		/// <summary>
		/// overridden because the ShortName can very well be empty.
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public override ITsString ChooserNameTS
		{
			get
			{
				ITsString result = ShortNameTSS;
				if (result != null && result.Length > 0)
					return result;
				result = LongNameTSS;
				if (result != null && result.Length > 0)
					return result;
				return m_cache.MakeUserTss(SIL.FieldWorks.FDO.Strings.ksEmptyFeatureStructure);
			}
		}

		private ITsString GetFeatureValueString(bool fLongForm)
		{
			var tisb = TsIncStrBldrClass.Create();
			var iCount = FeatureSpecsOC.Count;
			if (fLongForm && iCount > 0)
			{
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
					m_cache.DefaultAnalWs);
				tisb.Append("[");
			}
			var count = 0;
			foreach (var spec in FeatureSpecsOC)
			{
				if (count++ > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
						m_cache.DefaultAnalWs);
					tisb.Append(" "); // insert space except for first item
				}
				var cv = spec as IFsClosedValue;
				if (cv != null)
				{
					if (fLongForm)
					{
						tisb.AppendTsString((cv as FsClosedValue).LongNameTSS);
					}
					else
					{
						tisb.AppendTsString(cv.ShortNameTSS);
					}
				}
				else
				{
					var complex = spec as IFsComplexValue;
					if (complex != null)
					{
						if (fLongForm)
							tisb.AppendTsString((complex as FsComplexValue).LongNameTSS);
						else
							tisb.AppendTsString(complex.ShortNameTSS);
					}
				}
			}
			if (fLongForm && iCount > 0)
			{
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
					m_cache.DefaultAnalWs);
				tisb.Append("]");
			}
			if (tisb.Text == null)
			{
				// Ensure that we have a ws for the empty string!  See FWR-1122.
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
					m_cache.DefaultAnalWs);
			}
			return tisb.GetString();
		}

		internal ITsString GetFeatureValueStringSorted()
		{
			var tisb = TsIncStrBldrClass.Create();
			var iCount = FeatureSpecsOC.Count;
			if (iCount > 0)
			{
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
					m_cache.DefaultAnalWs);
				tisb.Append("[");
			}
			var count = 0;
			var sortedSpecs = from s in FeatureSpecsOC
							  orderby s.FeatureRA.Name.BestAnalysisAlternative.Text
							  select s;
			foreach (var spec in sortedSpecs)
			{
				if (count++ > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
						m_cache.DefaultAnalWs);
					tisb.Append(" "); // insert space except for first item
				}
				var cv = spec as IFsClosedValue;
				if (cv != null)
				{
						tisb.AppendTsString((cv as FsClosedValue).LongNameTSS);
				}
				else
				{
					var complex = spec as IFsComplexValue;
					if (complex != null)
					{
							tisb.AppendTsString((complex as FsComplexValue).LongNameSortedTSS);
					}
				}
			}
			if (iCount > 0)
			{
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
					m_cache.DefaultAnalWs);
				tisb.Append("]");
			}
			if (tisb.Text == null)
			{
				// Ensure that we have a ws for the empty string!  See FWR-1122.
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
					m_cache.DefaultAnalWs);
			}
			return tisb.GetString();
		}
		/// <summary>
		/// Provide a "Name" for this (is a long name)
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return LongNameSorted;
		}

		/// <summary>
		/// Override to handle reference props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case FsFeatStrucTags.kflidType:
					return m_cache.LangProject.MsFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>An array of hvos.</returns>
		public override IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			switch (flid)
			{
				case FsFeatStrucTags.kflidType:
					Set<ICmObject> set = new Set<ICmObject>();
#if NotNow
					// for now when only have exception features...
					FsFeatStrucType fsType = m_cache.LangProject.GetExceptionFeatureType();
					set.Add(fsType);
#endif
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.TypesOC);
					return set;
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		#region ICloneableCmObject Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <param name="clone">Destination object for clone operation</param>
		/// ------------------------------------------------------------------------------------
		public new void SetCloneProperties(ICmObject clone)
		{
			CopyPropertiesTo(clone);
		}
		#endregion

		/// <summary>
		/// Copy all the properties from this object to the target (clone).
		/// </summary>
		internal override void CopyPropertiesTo(ICmObject clone)
		{
			IFsFeatStruc feat = (IFsFeatStruc)clone;
			feat.TypeRA = TypeRA;
			foreach (var oldDisj in FeatureDisjunctionsOC)
			{
				var newDisj = Services.GetInstance<IFsFeatStrucDisjFactory>().Create();
				feat.FeatureDisjunctionsOC.Add(newDisj);
				oldDisj.SetCloneProperties(newDisj);
			}
			foreach (IFsFeatureSpecification oldSpec in FeatureSpecsOC)
			{
				var newSpec = (oldSpec as FsFeatureSpecification).CreateNewObject();
				feat.FeatureSpecsOC.Add(newSpec);
				oldSpec.SetCloneProperties(newSpec);
			}
		}

		/// <summary>
		/// Make a new object of this class.
		/// </summary>
		internal override IFsAbstractStructure CreateNewObject()
		{
			return Services.GetInstance<IFsFeatStrucFactory>().Create();
		}
	}
	#endregion

	#region FsFeatDefn Class
	/// <summary>
	/// We need to override the OnBeforeObjectDeleted and RemoveObjectSideEffectsInternal methods
	/// (see LT-4155 and FWR-2793).
	/// </summary>
	internal partial class FsFeatDefn
	{
		private List<ICmObject> m_backrefsToDelete;

		/// <summary>
		/// We need to get rid of referring FsFeatureSpecification objects as well, plus any
		/// of their owners which would then be empty.  This records the relevant referring
		/// FsFeatureSpecification objects before their references get cleared (which occurs
		/// before RemoveObjectSideEffectsInternal get called).
		/// </summary>
		protected override void OnBeforeObjectDeleted()
		{
			base.OnBeforeObjectDeleted();
			if (m_backrefsToDelete == null)
				m_backrefsToDelete = new List<ICmObject>();
			else
				m_backrefsToDelete.Clear();
			foreach (ICmObject obj in ReferringObjects)
			{
				if (obj is IFsFeatureSpecification &&
					(obj as IFsFeatureSpecification).FeatureRA == this)
				{
					m_backrefsToDelete.Add(obj);
				}
				else if (obj is IPhFeatureConstraint &&
					(obj as IPhFeatureConstraint).FeatureRA == this)
				{
					m_backrefsToDelete.Add(obj);
				}
			}
		}

		/// <summary>
		/// Collect the referring FsFeatureSpecification objects (already done), plus any of
		/// their owners which would then be empty.  Then delete them.
		/// </summary>
		/// <param name="e"></param>
		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);
			if (e.ForDeletion && m_backrefsToDelete != null)
			{
				List<ICmObject> objectsToDeleteAlso = new List<ICmObject>();
				foreach (ICmObject obj in m_backrefsToDelete)
				{
					if (obj.IsValidObject)
					{
						objectsToDeleteAlso.Add(obj);
						RemoveUnwantedFeatureStuff(objectsToDeleteAlso, obj.Owner);
					}
				}
				foreach (ICmObject obj in objectsToDeleteAlso)
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
				m_backrefsToDelete.Clear();
			}
		}
		private void RemoveUnwantedFeatureStuff(List<ICmObject> objectsToDeleteAlso, ICmObject obj)
		{
			if (obj != null && obj.IsValidObject)
			{
				bool fDelete = false;
				if (obj is FsFeatStruc)
				{
					int cDisj = (obj as FsFeatStruc).FeatureDisjunctionsOC.Count;
					int cSpec = (obj as FsFeatStruc).FeatureSpecsOC.Count;
					if (cDisj + cSpec == 1)
						fDelete = true;
				}
				else if (obj is FsFeatStrucDisj)
				{
					int cContents = (obj as FsFeatStrucDisj).ContentsOC.Count;
					if (cContents == 1)
						fDelete = true;
				}
				else if (obj is FsComplexValue)
				{
					fDelete = true;
				}
				if (fDelete)
				{
					objectsToDeleteAlso.Add(obj);
					RemoveUnwantedFeatureStuff(objectsToDeleteAlso, obj.Owner);
				}
			}
		}
	}
	#endregion

	#region Publication Class
	/// <summary>
	/// Publication class coded manually to add some static methods.
	/// </summary>
	internal partial class Publication
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether the publication is left bound.
		/// REVIEW: When we start supporting top-binding, this property might not serve our
		/// needs anymore.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLeftBound
		{
			get { return BindingEdge == 0; }
			set { BindingEdge = value ? BindingSide.Left : BindingSide.Right; }
		}
	}
	#endregion

	#region PubHFSet class
	/// <summary>
	/// PubHFSet class coded manually to add some methods.
	/// </summary>
	internal partial class PubHFSet
	{
		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the name of the set.
		/// </summary>
		/// <returns>the name of the set</returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy the details from the given pub H/F set to this one.
		/// </summary>
		/// <param name="copyFrom">The set to clone</param>
		/// ------------------------------------------------------------------------------------
		public void CloneDetails(IPubHFSet copyFrom)
		{
			Description = copyFrom.Description;

			// if the header and the footer were null (if they are missing in the database)
			// then create them
			if (DefaultHeaderOA == null)
				DefaultHeaderOA = new PubHeader();
			if (DefaultFooterOA == null)
				DefaultFooterOA = new PubHeader();
			DefaultHeaderOA.OutsideAlignedText = copyFrom.DefaultHeaderOA.OutsideAlignedText;
			DefaultHeaderOA.CenteredText = copyFrom.DefaultHeaderOA.CenteredText;
			DefaultHeaderOA.InsideAlignedText = copyFrom.DefaultHeaderOA.InsideAlignedText;
			DefaultFooterOA.OutsideAlignedText = copyFrom.DefaultFooterOA.OutsideAlignedText;
			DefaultFooterOA.CenteredText = copyFrom.DefaultFooterOA.CenteredText;
			DefaultFooterOA.InsideAlignedText = copyFrom.DefaultFooterOA.InsideAlignedText;


			var owningDiv = Owner as IPubDivision;
			if (owningDiv.DifferentFirstHF)
			{
				// if the header and the footer were null (if they are the same as the odd page)
				// then create them
				if (FirstHeaderOA == null)
					FirstHeaderOA = new PubHeader();
				if (FirstFooterOA == null)
					FirstFooterOA = new PubHeader();
				FirstHeaderOA.OutsideAlignedText = copyFrom.FirstHeaderOA.OutsideAlignedText;
				FirstHeaderOA.CenteredText = copyFrom.FirstHeaderOA.CenteredText;
				FirstHeaderOA.InsideAlignedText = copyFrom.FirstHeaderOA.InsideAlignedText;
				FirstFooterOA.OutsideAlignedText = copyFrom.FirstFooterOA.OutsideAlignedText;
				FirstFooterOA.CenteredText = copyFrom.FirstFooterOA.CenteredText;
				FirstFooterOA.InsideAlignedText = copyFrom.FirstFooterOA.InsideAlignedText;
			}
			else
			{
				FirstHeaderOA = null;
				FirstFooterOA = null;
			}
			if (owningDiv.DifferentEvenHF)
			{
				// if the header and the footer were null (if they are the same as the odd page)
				// then create them
				if (EvenHeaderOA == null)
					EvenHeaderOA = new PubHeader();
				if (EvenFooterOA == null)
					EvenFooterOA = new PubHeader();
				EvenHeaderOA.OutsideAlignedText = copyFrom.EvenHeaderOA.OutsideAlignedText;
				EvenHeaderOA.CenteredText = copyFrom.EvenHeaderOA.CenteredText;
				EvenHeaderOA.InsideAlignedText = copyFrom.EvenHeaderOA.InsideAlignedText;
				EvenFooterOA.OutsideAlignedText = copyFrom.EvenFooterOA.OutsideAlignedText;
				EvenFooterOA.CenteredText = copyFrom.EvenFooterOA.CenteredText;
				EvenFooterOA.InsideAlignedText = copyFrom.EvenFooterOA.InsideAlignedText;
			}
			else
			{
				EvenHeaderOA = null;
				EvenFooterOA = null;
			}
		}
		#endregion
	}
	#endregion

	#region Segment class
	internal partial class Segment
	{
		#region ISegment Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The actual text of the segment, as it appears in the underlying paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString BaselineText
		{
			get { return ((IStTxtPara)Owner).Contents.GetSubstring(BeginOffset, EndOffset); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override for handling side-effects for editing free translation comments.
		/// </summary>
		/// <param name="multiAltFlid"></param>
		/// <param name="alternativeWs"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		/// ------------------------------------------------------------------------------------
		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
			if (multiAltFlid == SegmentTags.kflidFreeTranslation)
			{
				// Make sure the paragraph belongs to Scripture.
				ScrTxtPara para = Paragraph as ScrTxtPara;
				if (para == null || para.m_paraCloneInProgress)
					return;
				BackTranslationAndFreeTranslationUpdateHelper.Do(para,
					() => ScriptureServices.UpdateMainTransFromSegmented(para, alternativeWs.Handle));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Need this version, because the ICmObjectInternal.AddObjectSideEffects version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			if (e.Flid == SegmentTags.kflidAnalyses)
			{
				if (!((IWfiWordformRepositoryInternal)Services.GetInstance<IWfiWordformRepository>()).OccurrencesInTextsInitialized)
					return; // should not maintain a structure we have not yet built; danger of duplicates.
				var wf = ((IAnalysis)e.ObjectAdded).Wordform as WfiWordform;
				if (wf != null)
				{
					Cache.ActionHandlerAccessor.AddAction(new GenericPropChangeUndoAction(
						() => wf.AddOccurenceInText(this),
						() => wf.RemoveOccurenceInText(this),
						wf,
						Cache.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, "FullConcordanceCount", false)));
				}
				return;
			}
			base.AddObjectSideEffectsInternal(e);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			if (e.Flid == SegmentTags.kflidAnalyses)
			{
				// It's harmless to try to remove it even if we haven't initialized the set yet.
				var wf = ((IAnalysis)e.ObjectRemoved).Wordform as WfiWordform;
				if (wf != null)
				{
					Cache.ActionHandlerAccessor.AddAction(new GenericPropChangeUndoAction(
						() => wf.RemoveOccurenceInText(this),
						() => wf.AddOccurenceInText(this),
						wf,
						Cache.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, "FullConcordanceCount", false)));
				}
				return;
			}
			base.RemoveObjectSideEffectsInternal(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer the end of the segment. This is either the end of the paragraph or the start of the next segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int EndOffset
		{
			get
			{
				IStTxtPara para = (IStTxtPara)Owner;
				int index = para.SegmentsOS.IndexOf(this);
				if (index < para.SegmentsOS.Count - 1)
					return para.SegmentsOS[index + 1].BeginOffset;
				return para.Contents.Length;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the baseline text for the specified analaysis.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString GetBaselineText(int ianalysis)
		{
			int ichLim;
			var method = new ParsedParagraphOffsetsMethod(this);
			int ichMin = method.GetAnalysisOffsets(ianalysis, out ichLim);
			return method.Baseline.Substring(ichMin, ichLim - ichMin);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collect set of unique wordforms in the segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CollectUniqueWordforms(HashSet<IWfiWordform> wordforms)
		{
			foreach (var analysis in AnalysesRS)
			{
				var wf = analysis.Wordform;
				if (wf != null) // can be for punctuation
					wordforms.Add(wf);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert into the dictionary the offset for each wordform in Analyses, keyed by the analysis.
		/// Offsets are relative to the paragraph, not the segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GetWordformsAndOffsets(Dictionary<IWfiWordform, int> collector)
		{
			new ParagraphOffsetsMethod(this).GetWordformsAndOffsets(collector);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the analysis closest to the specified range of characters (relative to the segment), prefering
		/// the following word if ambiguous.
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="fExactMatch"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public AnalysisOccurrence FindWagform(int ichMin, int ichLim, out bool fExactMatch)
		{
			return new ParsedParagraphOffsetsMethod(this).FindAnnotation(ichMin, ichLim, out fExactMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the BeginOffset (relative to the StTxtPara) of the IAnalysis referenced by the given index.
		/// </summary>
		/// <param name="iAnalysis">Index into AnalysesRS</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetAnalysisBeginOffset(int iAnalysis)
		{
			var offsetList = new ParsedParagraphOffsetsMethod(this).GetAnalysesAndOffsets();
			return offsetList[iAnalysis].Item2;
		}

		/// <summary>
		/// Return a list of all the analyses in the segment, with their begin and end offsets (relative to the paragraph).
		/// </summary>
		/// <returns></returns>
		public IList<Tuple<IAnalysis, int, int>> GetAnalysesAndOffsets()
		{
			return new ParsedParagraphOffsetsMethod(this).GetAnalysesAndOffsets();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer an array of count (located) occurrences of the specified IAnalysis (and, optionally, its children).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<LocatedAnalysisOccurrence> GetOccurrencesOfAnalysis(IAnalysis analysis, int count, bool includeChildren)
		{
			return new ParsedParagraphOffsetsMethod(this).GetOccurrencesOfAnalysis(analysis, count, includeChildren);
		}

		/// <summary>
		/// Reports true when there is a translation or non-null note.
		/// </summary>
		public bool HasAnnotation
		{
			get { return LiteralTranslation != null || FreeTranslation != null || NotesOS.Count > 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a label segment (i.e. is defined
		/// as having label text)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLabel
		{
			get { return SegmentBreaker.HasLabelText(Paragraph.Contents, BeginOffset, EndOffset); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the length of the segment's baseline text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Length
		{
			get { return EndOffset - BeginOffset; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance contains only a hard line break
		/// character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsHardLineBreak
		{
			get
			{
				return (EndOffset - BeginOffset == 1 &&
					Paragraph.Contents.Text[BeginOffset] == StringUtils.kChHardLB);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shortcut, saves Casts and may help if change the model again.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara Paragraph
		{
			get { return (IStTxtPara)Owner; }
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the translations, notes, and analyses from the specified source segment to
		/// this segment.
		/// </summary>
		/// <param name="segSrc">The source segment.</param>
		/// <param name="fMerge"><c>true</c>if the translations and analyses from the source
		/// segment are to be merged with this segment's. If set to <c>false</c> the destination
		/// segment translations will be overwritten even if the destination translations
		/// contain existing text.</param>
		/// ------------------------------------------------------------------------------------
		internal void AssimilateSegment(ISegment segSrc, bool fMerge)
		{
			if (segSrc.IsLabel) // nb don't check segDest, its offsets may not be consistent with para contents.
				return;

			bool fNeedToCopyGlosses = false;
			if (fMerge)
			{
				FreeTranslation.AppendAlternatives(segSrc.FreeTranslation);
				LiteralTranslation.AppendAlternatives(segSrc.LiteralTranslation);
				fNeedToCopyGlosses = true;
			}
			else
			{
				FreeTranslation.CopyAlternatives(segSrc.FreeTranslation, true);
				LiteralTranslation.CopyAlternatives(segSrc.LiteralTranslation, true);
				NotesOS.Clear();

				if (!Paragraph.ParseIsCurrent)
				{
					// Since the parse is not current, we don't really care what analyses we
					// have. As a matter-of-fact, the AnalysisAdjuster most likely didn't
					// update the wordforms for the new text. Assume that the analyses from
					// the source segment are good enough to have the ParagraphParser keep
					// the most amount of data when re-parsing.
					// ENHANCE: It might be better to replace the existing analyses with the ones from
					// the source instead of removing them all and then doing a copy
					AnalysesRS.Clear();
					segSrc.AnalysesRS.CopyTo(AnalysesRS, AnalysesRS.Count);
				}
				else
				{
					// It's very likely that the AnalysisAdjuster has already created wordforms for
					// any words replaced by the source segment. In most cases we just want to copy
					// the analyses from the source segment to this segment. However, if the replacing
					// of this segment created punctuation forms that were the result of a merging of
					// punctuation forms from the beginning of one segment and the end of another
					// segment, then the punctuation form in the source segment will be wrong. In
					// this case we need to keep the form that the AnalysisAdjuster calculated for
					// us. (TE-9287)
					fNeedToCopyGlosses = true;
				}
			}

			if (fNeedToCopyGlosses)
			{
				// When merging/appending, it's very likely that the AnalysisAdjuster has already
				// created wordforms for any words moved over from the source paragraph. However,
				// the source paragraph could contain glosses for the words which would be
				// prefereable to the wordforms. In this case we want to replace the created
				// wordforms with their glosses if the gloss is for the same word.
				int iDest = AnalysesRS.Count - 1;
				for (int iSrc = segSrc.AnalysesRS.Count - 1; iSrc >= 0 && iDest >= 0; iSrc--, iDest--)
				{
					IAnalysis srcAnalysis = segSrc.AnalysesRS[iSrc];
					IAnalysis destAnalysis = AnalysesRS[iDest];
					if (srcAnalysis is IWfiGloss && destAnalysis is IWfiWordform &&
						srcAnalysis.Wordform == destAnalysis)
					{
						// Found a gloss in the source that matches the wordform in the destination.
						// Assume it's better to have the gloss from source.
						AnalysesRS[iDest] = srcAnalysis;
					}
				}
			}

			foreach (INote note in segSrc.NotesOS)
			{
				INote newNote = Cache.ServiceLocator.GetInstance<INoteFactory>().Create();
				NotesOS.Add(newNote);
				newNote.Content.CopyAlternatives(note.Content);
			}
		}
	}
	#endregion

	internal partial class Text
	{
		/// <summary>
		/// Associate the text with a (newly created) notebook record. Does nothing if it already is.
		/// </summary>
		public void AssociateWithNotebook(bool makeYourOwnUow)
		{
			if(IsAlreadyAssociatedWithNotebook())
				return;
			if (makeYourOwnUow)
			{
				UndoableUnitOfWorkHelper.Do(Strings.ksUndoCreateNotebookRecord, Strings.ksRedoCreateNotebookRecord,
					Cache.ActionHandlerAccessor, AssociateWithNotebook);
			}
			else
				AssociateWithNotebook();
		}

		private bool IsAlreadyAssociatedWithNotebook()
		{
			EnsureCompleteIncomingRefs();
			return m_incomingRefs.OfType<RnGenericRec>().Any();
		}

		private void AssociateWithNotebook()
		{
			var rec = Cache.ServiceLocator.GetInstance<RnGenericRecFactory>().Create();
			if (Cache.LangProject.ResearchNotebookOA == null)
				Cache.LangProject.ResearchNotebookOA = Cache.ServiceLocator.GetInstance<IRnResearchNbkFactory>().Create();
			Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(rec);
			rec.TextRA = this;
			var cmPossibilityRepository = Services.GetInstance<ICmPossibilityRepository>();
			ICmPossibility eventItem;
			if (cmPossibilityRepository.TryGetObject(RnResearchNbkTags.kguidRecEvent, out eventItem)) // should succeed except in tests
				rec.TypeRA = eventItem;
		}

		/// <summary>
		/// Reports the Notebook record associated with this text, or null if there isn't one.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "Text")]
		public IRnGenericRec AssociatedNotebookRecord
		{
			get
			{
				EnsureCompleteIncomingRefs();
				return ReferringObjects.OfType<RnGenericRec>().Select(
					referringObject => referringObject as IRnGenericRec).FirstOrDefault();
			}
		}

	}

	#region ParagraphAnalysisFinder class
	/// <summary>
	/// Class that knows how to work through the text of a paragraph and determine its analyses.
	/// </summary>
	class ParagraphAnalysisFinder
	{
		/// <summary>
		/// Current position in m_baseline.
		/// </summary>
		protected int m_ich;

		protected int m_length; // of m_baseline
		protected WordMaker m_wordMaker;

		/// <summary>
		/// Index of current analysis.
		/// </summary>
		protected int m_ianalysis;

		public ParagraphAnalysisFinder(ITsString baseline, ILgWritingSystemFactory wsf)
		{
			Baseline = baseline;
			m_length = Baseline.Length;
			m_wordMaker = new WordMaker(Baseline, wsf);
		}

		public ITsString Baseline { get; private set; }
		public int Position { get { return m_ich; } set { m_ich = value;} }

		/// <summary>
		/// Answer whether the character at this position is wordforming.
		/// </summary>
		/// <param name="ich"></param>
		internal bool IsWordforming(int ich)
		{
			return m_wordMaker.IsWordforming(ich);
		}

		private bool IsCurrentCharInWord()
		{
			return (m_wordMaker.IsWordforming(m_ich));
		}

		/// <summary>
		/// Advance m_ich to the first character that is part of the current analysis.
		/// Currently this just means skipping white space; anything else is made into some
		/// kind of analysis, even if just a PunctuationForm.
		/// </summary>
		internal void AdvanceToAnalysis()
		{
			while (m_ich < m_length && m_wordMaker.IsWhite(m_ich))
				m_ich = m_wordMaker.NextChar(m_ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assuming m_ich is at the start of a word, advance it to the first character after
		/// the word. Subclasses which assume current wordforms are valid can do this more
		/// efficiently. "words" consist of all word-forming or all non-word-forming characters
		/// between whitespace characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void AdvancePastWord()
		{
			AdvancePastWord(int.MaxValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assuming m_ich is at the start of a word, advance it to the first character after
		/// the word. Subclasses which assume current wordforms are valid can do this more
		/// efficiently. "words" consist of all word-forming or all non-word-forming characters
		/// between whitespace characters.
		/// </summary>
		/// <param name="ichLimOfWord">The limit of the last character in the baseline to allow
		/// in the word.</param>
		/// ------------------------------------------------------------------------------------
		internal virtual void AdvancePastWord(int ichLimOfWord)
		{
			if (m_ich >= m_length || m_ich == ichLimOfWord)
				return; // can't advance, hope caller eventually realizes it.
			int ichStart = m_ich;
			if (IsCurrentCharInWord())
			{
				// Make a token that is all wordforming characters
				while (m_ich < m_length && m_ich < ichLimOfWord && IsCurrentCharInWord())
					m_ich = m_wordMaker.NextChar(m_ich);
			}
			else
			{
				// Make a token that has no wordforming characters, that is, up to the next
				// whitespace or wordforming character.
				while (m_ich < m_length && m_ich < ichLimOfWord && !m_wordMaker.IsWhite(m_ich) && !IsCurrentCharInWord())
					m_ich = m_wordMaker.NextChar(m_ich);
			}
			Debug.Assert(m_ich > ichStart);
			m_ianalysis++;
		}
	}
	#endregion

	#region ParagraphOffsetMethod
	/// <summary>
	/// Method object used in various methods related to getting actual offsets of analyses in segment.
	/// </summary>
	class ParagraphOffsetsMethod : ParagraphAnalysisFinder
	{
		protected ISegment m_segment;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="seg"></param>
		public ParagraphOffsetsMethod(ISegment seg)
			: base(seg.BaselineText, seg.Services.WritingSystemFactory)
		{
			m_segment = seg;
		}

		/// <summary>
		/// Get the offset where the specified analysis starts.
		/// </summary>
		public int GetAnalysisOffset(int ianalysis)
		{
			AdvanceToAnalysis();
			while (m_ianalysis < ianalysis)
			{
				AdvancePastWord();
				AdvanceToAnalysis();
			}
			return m_ich;
		}

		/// <summary>
		/// Get the start and limit of the indicated analysis.
		/// </summary>
		/// <param name="ianalysis"></param>
		/// <param name="ichLim"></param>
		/// <returns></returns>
		public int GetAnalysisOffsets(int ianalysis, out int ichLim)
		{
			int result = GetAnalysisOffset(ianalysis);
			AdvancePastWord();
			ichLim = m_ich;
			return result;
		}

		/// <summary>
		/// Insert into the dictionary the offset for each wordform in Analyses, keyed by the analysis.
		/// Offsets are relative to the paragraph, not the segment.
		/// </summary>
		public void GetWordformsAndOffsets(Dictionary<IWfiWordform, int> collector)
		{
			AdvanceToAnalysis();
			while (m_ianalysis < m_segment.AnalysesRS.Count)
			{
				IWfiWordform wf = m_segment.AnalysesRS[m_ianalysis] as IWfiWordform;
				if (wf != null)
					collector[wf] = m_ich + m_segment.BeginOffset;
				AdvancePastWord();
				AdvanceToAnalysis();
			}
		}
	}
	#endregion

	#region ParsedParagraphOffsetsMethod
	/// <summary>
	/// Subclass which assumes the current list of analyses is valid.
	/// </summary>
	class ParsedParagraphOffsetsMethod : ParagraphOffsetsMethod
	{
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="seg"></param>
		public ParsedParagraphOffsetsMethod(ISegment seg) : base(seg)
		{}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Advance position to the first character after the current word. Since we assume the
		/// analysis is right, we can jump forward the whole length of the wordform. "words"
		/// consist of all word-forming or all non-word-forming characters between whitespace
		/// characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal override void AdvancePastWord(int ichLimOfWord)
		{
			if (m_ianalysis >= m_segment.AnalysesRS.Count) // Oops!
			{
				//Use DoSomehow to avoid crash if this was triggered during a PropertyChangeEvent
				NonUndoableUnitOfWorkHelper.DoSomehow(m_segment.Cache.ActionHandlerAccessor,
					() => m_segment.Paragraph.ParseIsCurrent = false);
				Debug.Fail("Paragraph is supposedly parsed correctly, but analysis list is inconsistent with content");
				// We'd better make some progress, or this can produce an infinite loop (LT-13633).
				m_ich = Math.Max(m_ich + 1, m_segment.BaselineText.Length - 1); // May fix LT-12657
				return;
			}
			m_ich += m_segment.AnalysesRS[m_ianalysis].GetForm(TsStringUtils.GetWsAtOffset(Baseline, m_ich)).Length;
			if (m_ich > m_length)
			{
				//Use DoSomehow to avoid crash if this was triggered during a PropertyChangeEvent
				NonUndoableUnitOfWorkHelper.DoSomehow(m_segment.Cache.ActionHandlerAccessor,
					() => m_segment.Paragraph.ParseIsCurrent = false); //We don't think the parse is right, flag for reparsing.
				m_ich = m_length; // May prevent crash (see FWR-3221).
				Debug.Fail("Paragraph is supposedly parsed correctly, but analysis list is inconsistent with content");
			}
			m_ianalysis++;
		}

		public AnalysisOccurrence FindAnnotation(int ichMin, int ichLim, out bool fExactMatch)
		{
			fExactMatch = false; // default
			if (m_segment.AnalysesRS.Count == 0)
				return null;
			AdvanceToAnalysis();
			while (m_ich <= ichMin)
			{
				int startWord = m_ich;
				AdvancePastWord();
				if (m_ich >= ichLim)
				{
					// Word starts at or before ichMin and ends at or after ichLim. Return this word.
					fExactMatch = startWord == ichMin && m_ich == ichLim;
					return new AnalysisOccurrence(m_segment, m_ianalysis - 1);
				}
				AdvanceToAnalysis();
				// if we reached the end of the segment, answer the last word (not exact).
				if (m_ich == m_length)
					return new AnalysisOccurrence(m_segment, m_segment.AnalysesRS.Count - 1);
			}
			// At this point m_ich is at the start of a word which begins after ichMin.
			// Since we didn't return after advancing past the previous word, the ich must
			// be in the punctuation between the words. We want the current (following) word.
			// It's not an exact match.
			return new AnalysisOccurrence(m_segment, m_ianalysis);
		}

		/// <summary>
		/// Return a list of all the analyses in the segment, with their begin and end offsets.
		/// </summary>
		public List<Tuple<IAnalysis, int, int>> GetAnalysesAndOffsets()
		{
			AdvanceToAnalysis();
			var result = new List<Tuple<IAnalysis, int, int>>(m_segment.AnalysesRS.Count);
			while (m_ianalysis < m_segment.AnalysesRS.Count)
			{
				int begin = m_ich + m_segment.BeginOffset;
				IAnalysis analysis = m_segment.AnalysesRS[m_ianalysis];
				AdvancePastWord();
				result.Add(new Tuple<IAnalysis, int, int>(analysis, begin, m_ich + m_segment.BeginOffset));
				AdvanceToAnalysis();
			}
			return result;
		}

		internal List<LocatedAnalysisOccurrence> GetOccurrencesOfAnalysis(IAnalysis analysis, int count, bool includeChildren)
		{
			Debug.Assert(count >= 0);
			AdvanceToAnalysis();
			List<LocatedAnalysisOccurrence> result = (count == int.MaxValue)
							? new List<LocatedAnalysisOccurrence>()
							: new List<LocatedAnalysisOccurrence>(count);
			int i = 0;
			while (m_ianalysis < m_segment.AnalysesRS.Count)
			{
				var foundAnalysis = m_segment.AnalysesRS[m_ianalysis];
				if(foundAnalysis == analysis
					|| (includeChildren
						&& (foundAnalysis.Owner == analysis
							|| (foundAnalysis.Owner != null && foundAnalysis.Owner.Owner == analysis))))
				{
					result.Add(new LocatedAnalysisOccurrence(m_segment, m_ianalysis, m_ich + m_segment.BeginOffset));
					i++;
					if (i >= count)
						return result;
				}
				AdvancePastWord();
				AdvanceToAnalysis();
			}
			return result;
		}
	}
	#endregion
}
