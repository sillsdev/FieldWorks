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
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml; // XMLWriter
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary></summary>
	internal partial class MoMorphSynAnalysis
	{
		private int m_MLPartOfSpeechFlid;
		private int m_MLInflectionClassFlid;

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();

			m_MLPartOfSpeechFlid = Cache.MetaDataCache.GetFieldId("MoMorphSynAnalysis", "MLPartOfSpeech", false);
			m_MLInflectionClassFlid = Cache.MetaDataCache.GetFieldId("MoMorphSynAnalysis", "MLInflectionClass", false);
		}

		/// <summary>
		/// Make the OwningEntry property available for views.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "ILexEntry")]
		public ILexEntry OwningEntry
		{
			get
			{
				return (ILexEntry)Owner;
			}
		}

		/// <summary>
		///
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public virtual ITsString FeaturesTSS
		{
			get
			{ return m_cache.TsStrFactory.MakeString("", Cache.DefaultAnalWs); }
		}

		/// <summary>
		///
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public virtual ITsString ExceptionFeaturesTSS
		{
			get { return m_cache.TsStrFactory.MakeString("", Cache.DefaultAnalWs); }
		}

		/// <summary>
		/// Check whether the specified reference attribute HVO is valid for the
		/// specified field.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="flid">the field ID</param>
		/// <returns>true if the RA is valid, otherwise false</returns>
		protected bool IsReferenceAttributeValid(ICmObject obj, int flid)
		{
			if (obj == null)
				return true;

			return ReferenceTargetCandidates(flid).Contains(obj);
		}

		/// <summary>
		/// Need this version, because the ICmObjectInternal.RemoveObjectSideEffects version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			if (e.Flid != (int) FDO.MoMorphSynAnalysisTags.kflidComponents) return;

			var gonerMsa = (IMoMorphSynAnalysis)e.ObjectRemoved;
			if (gonerMsa.CanDelete)
				((ILexEntry) Owner).MorphoSyntaxAnalysesOC.Remove(gonerMsa);
		}

		/// <summary>
		/// Removes all invalid feature specifications from the given feature
		/// structure.
		/// </summary>
		/// <param name="pos">the category to check for validity</param>
		/// <param name="fs">the field structure</param>
		protected void RemoveInvalidFeatureSpecs(IPartOfSpeech pos, IFsFeatStruc fs)
		{
			if (fs == null || pos == null)
				return;

			foreach (var spec in fs.FeatureSpecsOC)
			{
				if (!IsFeatureValid(pos, spec.FeatureRA))
				{
					fs.FeatureSpecsOC.Remove(spec);
				}
			}
		}

		/// <summary>
		/// Checks if the specified feature definition is valid for the
		/// specified category.
		/// </summary>
		/// <param name="pos">the category to check for validity</param>
		/// <param name="fDefn">the feature definition</param>
		/// <returns>true if the feature is valid, otherwise false</returns>
		protected bool IsFeatureValid(IPartOfSpeech pos, IFsFeatDefn fDefn)
		{
			if (fDefn == null)
				return false;
			while (pos != null)
			{
				if (pos.InflectableFeatsRC.Contains(fDefn))
					return true;
				pos = pos.Owner as IPartOfSpeech; // May not cast to POS if it is the owning list.
			}

			return false;
		}
		/// <summary>
		/// Get gloss of first sense that uses this msa
		/// </summary>
		/// <returns>the gloss as a string</returns>
		public string GetGlossOfFirstSense()
		{
			return
			(from sense in ((ILexEntry)Owner).SensesOS
			 where sense.MorphoSyntaxAnalysisRA == this
			 select sense.Gloss.AnalysisDefaultWritingSystem.Text)
						.DefaultIfEmpty(Strings.ksQuestions).FirstOrDefault();
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
				var servLoc = m_cache.ServiceLocator;
				// If any surviving senses (which must belong to the same entry) refer to it, we can't delete it.
				if (((ILexEntry)Owner).AllSenses.Where(
					sense => sense.MorphoSyntaxAnalysisRA == this).FirstOrDefault() != null)
				{
					return false;
				}
				var morphBundleCount = (servLoc.GetInstance<IWfiMorphBundleRepository>().AllInstances().Where(mb => mb.MsaRA == this)).Count();
				if (morphBundleCount > 0) return false;
				var allMoMorphAdhocProhib = servLoc.GetInstance<IMoMorphAdhocProhibRepository>().AllInstances();
				var mapCount = (allMoMorphAdhocProhib.Where(map => map.FirstMorphemeRA == this
																   || map.MorphemesRS.Contains(this)
																   || map.RestOfMorphsRS.Contains(this))).Count();
				if (mapCount > 0) return false;

				var msaCount = (servLoc.GetInstance<IMoMorphSynAnalysisRepository>().AllInstances().Where(
					msa => msa.ComponentsRS.Contains(this))).Count();
				return msaCount < 1;
			}
		}

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public virtual string InterlinearName
		{
			get { return Strings.ksProgError; }
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public virtual string InterlinearAbbr
		{
			get { return InterlinearAbbrTSS.Text; }
		}

		/// <summary>
		/// Check whether this MoMorphSynAnalysis object is empty of any content.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public virtual bool IsEmpty
		{
			get
			{
				return String.IsNullOrEmpty(GlossString) &&
					   this.GlossBundleRS.Count == 0 &&
					   this.ComponentsRS.Count == 0;
			}
		}

		/// <summary>
		/// Determines whether the specified Object is equal to the current MoStemMsa.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public abstract bool EqualsMsa(IMoMorphSynAnalysis msa);

		/// <summary>
		/// Determines whether the specified sandbox MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public abstract bool EqualsMsa(SandboxGenericMSA msa);

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target to an environment with
		/// no context that would otherwise tell you such things as what LexEntry this belongs to.
		/// </summary>
		public string LongName
		{
			get
			{
				return LongNameTs.Text;
			}
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in the MSADlgLauncherSlice slice.
		/// </summary>
		/// <remarks>
		/// Subclasses must override this to get anything useful.
		/// </remarks>
		[VirtualProperty(CellarPropertyType.String)]
		public abstract ITsString LongNameTs
		{
			get;
		}

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return this.GlossString;
			}
		}

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public abstract ITsString InterlinearNameTSS
		{
			get;
		}

		/// <summary>
		/// Return the Grammatical Info. (POS) TsString for the given writing system.
		/// </summary>
		/// <param name="wsAnal">If this is magic WS then return BestAnalorVern. Otherwise return the
		/// string associated with the specific writing system.</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public abstract ITsString InterlinAbbrTSS(int wsAnal);

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public abstract ITsString InterlinearAbbrTSS
		{
			get;
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in an ad hoc co-prohibition.
		/// TODO (DamienD): register prop change when dependencies change
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public virtual ITsString LongNameAdHocTs
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				tisb.AppendTsString(OwnerOfClass<ILexEntry>().HeadWord);
				tisb.Append(" ");
				tisb.AppendTsString(InterlinearNameTSS);
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in an ad hoc co-prohibition.
		/// </summary>
		public string LongNameAdHoc
		{
			get
			{
				return LongNameAdHocTs.Text;
			}
		}

		/// <summary>
		/// Makes the target method accessible to XmlViews clients.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiString)]
		public VirtualStringAccessor MLPartOfSpeech
		{
			get { return new VirtualStringAccessor(this, m_MLPartOfSpeechFlid, PartOfSpeechForWsTSS); }
		}

		/// <summary>
		/// Makes the target method accessible to XmlViews clients.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiString)]
		public VirtualStringAccessor MLInflectionClass
		{
			get { return new VirtualStringAccessor(this, m_MLInflectionClassFlid, InflectionClassForWsTSS); }
		}

		/// <summary>
		/// Virtual method to give part of speech depending on type of MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public virtual ITsString PartOfSpeechForWsTSS(int ws)
		{
			return Cache.TsStrFactory.EmptyString(ws);
		}

		/// <summary>
		/// Virtual method to give Inflection Class on type of MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public virtual ITsString InflectionClassForWsTSS(int ws)
		{
			return Cache.TsStrFactory.EmptyString(ws);
		}

		/// <summary>
		/// Return the slot objects for the MSA.  Always empty if not inflectional.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "MoInflAffixSlot")]
		public virtual IEnumerable<IMoInflAffixSlot> Slots
		{
			get
			{
				return new IMoInflAffixSlot[0];
			}
		}

		/// <summary>
		/// Update an extant MSA to the new values in the sandbox MSA,
		/// or make a new MSA with the values in the sandbox msa.
		/// </summary>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		/// <remarks>
		/// Subclasses should override this method to do same-class updates,
		/// but then call this method to handle class changing activities.
		/// </remarks>
		public virtual IMoMorphSynAnalysis UpdateOrReplace(SandboxGenericMSA sandboxMsa)
		{
			ILexEntry le = Owner as ILexEntry;
			foreach (MoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				// Check other extant MSAs to see if they match the updated one.
				if (msa != this && msa.EqualsMsa(sandboxMsa))
				{
					msa.MergeObject(this);
					return msa;
				}
			}

			// Make a new MSA.
			IMoMorphSynAnalysis newMsa = null;
			switch (sandboxMsa.MsaType)
			{
				default:
					throw new ApplicationException("Cannot create any other kind of MSA here.");
				case MsaType.kRoot: // Fall through.
				case MsaType.kStem:
					newMsa = Services.GetInstance<IMoStemMsaFactory>().Create(le, sandboxMsa);
					break;
				case MsaType.kDeriv:
					newMsa = Services.GetInstance<IMoDerivAffMsaFactory>().Create(le, sandboxMsa);
					break;
				case MsaType.kInfl:
					newMsa = Services.GetInstance<IMoInflAffMsaFactory>().Create(le, sandboxMsa);
					break;
				case MsaType.kUnclassified:
					newMsa = Services.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create(le, sandboxMsa);
					break;
			}

			newMsa.SwitchReferences(this);
			return newMsa;
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		public virtual string PosFieldName
		{
			get { return this.InterlinearName; }
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
				LiftResidue = sResidue;
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
		/// Switches select inbound references from the sourceMsa to 'this'.
		/// </summary>
		public void SwitchReferences(IMoMorphSynAnalysis sourceMsa)
		{
			foreach (var obj in sourceMsa.ReferringObjects)
			{
				if (obj is IWfiMorphBundle)
				{
					IWfiMorphBundle wmb = obj as IWfiMorphBundle;
					Debug.Assert(wmb.MsaRA == sourceMsa);
					wmb.MsaRA = this;
				}
				else if (obj is ILexSense)
				{
					ILexSense sense = obj as ILexSense;
					Debug.Assert(sense.MorphoSyntaxAnalysisRA == sourceMsa);
					sense.MorphoSyntaxAnalysisRA = this;
				}
				else
				{
					// Alert programmers to need for more code.
					Debug.Assert(obj is IWfiMorphBundle || obj is ILexSense);
				}
			}
		}
	}

	/// <summary></summary>
	internal partial class MoDerivAffMsa
	{
		/// <summary>
		///
		/// </summary>
		public override ITsString FeaturesTSS
		{
			get
			{
				IFsFeatStruc featFrom = FromMsFeaturesOA;
				IFsFeatStruc featTo = ToMsFeaturesOA;
				if (featFrom != null || featTo != null)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					if (featFrom != null)
						tisb.AppendTsString(featFrom.ShortNameTSS);
					else
						tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
					tisb.AppendTsString(m_cache.MakeUserTss(" > "));
					if (featTo != null)
						tisb.AppendTsString(featTo.ShortNameTSS);
					else
						tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
					return tisb.GetString();
				}
				else
				{
					return base.FeaturesTSS;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString ExceptionFeaturesTSS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				if (FromProdRestrictRC.Count > 0)
				{
					bool fFirst = true;
					foreach (CmPossibility pss in FromProdRestrictRC)
					{
						if (!fFirst)
							tisb.AppendTsString(m_cache.MakeUserTss(","));
						tisb.AppendTsString(pss.Abbreviation.BestAnalysisVernacularAlternative);
						fFirst = false;
					}
				}
				if (ToProdRestrictRC.Count > 0)
				{
					if (tisb.Text == null || tisb.Text.Length == 0)
						tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
					tisb.AppendTsString(m_cache.MakeUserTss(" > "));
					bool fFirst = true;
					foreach (CmPossibility pss in ToProdRestrictRC)
					{
						if (!fFirst)
							tisb.AppendTsString(m_cache.MakeUserTss(","));
						tisb.AppendTsString(pss.Abbreviation.BestAnalysisVernacularAlternative);
						fFirst = false;
					}
				}
				else if (tisb.Text != null && tisb.Text.Length > 0)
				{
					tisb.AppendTsString(m_cache.MakeUserTss(String.Format(" > {0}", Strings.ksQuestions)));
				}
				else
				{
					tisb.AppendTsString(m_cache.MakeUserTss(""));
				}
				return tisb.GetString();
			}
		}

		///<summary>
		/// Copies attributes associated with the current "From" POS, such as inflection features and classes, that
		/// are valid from the specified MSA to this MSA. If the attribute is not valid, it is removed from
		/// this MSA.
		///</summary>
		///<param name="srcMsa">the source MSA</param>
		public void CopyToAttributesIfValid(IMoDerivAffMsa srcMsa)
		{
			// inflection classes
			if (IsReferenceAttributeValid(srcMsa.ToInflectionClassRA, MoDerivAffMsaTags.kflidToInflectionClass))
			{
				if (srcMsa.ToInflectionClassRA != ToInflectionClassRA)
					ToInflectionClassRA = srcMsa.ToInflectionClassRA;
			}
			else if (ToInflectionClassRA != null)
			{
				ToInflectionClassRA = null;
			}

			// inflection features
			if (srcMsa.ToMsFeaturesOA == null)
			{
				ToMsFeaturesOA = null;
			}
			else
			{
				if (ToMsFeaturesOA != srcMsa.ToMsFeaturesOA)
					CopyObject<IFsFeatStruc>.CloneFdoObject(srcMsa.ToMsFeaturesOA, newMsa => ToMsFeaturesOA = newMsa);
				RemoveInvalidFeatureSpecs(ToPartOfSpeechRA, ToMsFeaturesOA);
				if (ToMsFeaturesOA.IsEmpty)
					ToMsFeaturesOA = null;
			}
		}

		///<summary>
		/// Copies attributes associated with the current "To" POS, such as inflection features and classes, that
		/// are valid from the specified MSA to this MSA. If the attribute is not valid, it is removed from
		/// this MSA.
		///</summary>
		///<param name="srcMsa">the source MSA</param>
		public void CopyFromAttributesIfValid(IMoDerivAffMsa srcMsa)
		{
			// inflection classes
			if (IsReferenceAttributeValid(srcMsa.FromInflectionClassRA, (int)MoDerivAffMsaTags.kflidFromInflectionClass))
			{
				if (srcMsa.FromInflectionClassRA != FromInflectionClassRA)
					FromInflectionClassRA = srcMsa.FromInflectionClassRA;
			}
			else if (FromInflectionClassRA != null)
			{
				FromInflectionClassRA = null;
			}

			// inflection features
			if (srcMsa.FromMsFeaturesOA == null)
			{
				FromMsFeaturesOA = null;
			}
			else
			{
				if (FromMsFeaturesOA != srcMsa.FromMsFeaturesOA)
					CopyObject<IFsFeatStruc>.CloneFdoObject(srcMsa.FromMsFeaturesOA, newMsa => FromMsFeaturesOA = newMsa);
				RemoveInvalidFeatureSpecs(FromPartOfSpeechRA, FromMsFeaturesOA);
				if (FromMsFeaturesOA.IsEmpty)
					FromMsFeaturesOA = null;
			}

			// stem names
			if (IsReferenceAttributeValid(srcMsa.FromStemNameRA, (int)MoDerivAffMsaTags.kflidFromStemName))
			{
				if (srcMsa.FromStemNameRA != FromStemNameRA)
					FromStemNameRA = srcMsa.FromStemNameRA;
			}
			else if (FromStemNameRA != null)
			{
				FromStemNameRA = null;
			}
		}

		/// <summary>
		/// the way we want to show this in an interlinear view
		/// </summary>
		public override string InterlinearName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// Check whether this MoDerivAffMsa is empty of content.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public override bool IsEmpty
		{
			get
			{
				return ToPartOfSpeechRA == null &&
					   FromPartOfSpeechRA == null &&
					   ToInflectionClassRA == null &&
					   FromInflectionClassRA == null &&
					   ToProdRestrictRC.Count == 0 &&
					   FromProdRestrictRC.Count == 0 &&
					   ToMsFeaturesOA == null &&
					   FromMsFeaturesOA == null &&
					   FromStemNameRA == null &&
					   AffixCategoryRA == null &&
					   StratumRA == null &&
					   base.IsEmpty;
			}
		}

		/// <summary>
		/// Determines whether the specified MoDerivAffMsa is equal to the current MoDerivAffMsa.
		/// </summary>
		/// <param name="msa">The MoDerivAffMsa to compare with the current MoDerivAffMsa.</param>
		/// <returns>true if the specified MoDerivAffMsa is equal to the current MoDerivAffMsa; otherwise, false.</returns>
		public override bool EqualsMsa(IMoMorphSynAnalysis msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoDerivAffMsa.
			if (!(msa is IMoDerivAffMsa))
				return false;

			var derivMsa = (IMoDerivAffMsa)msa;

			return (DomainObjectServices.AreEquivalent(FromMsFeaturesOA, derivMsa.FromMsFeaturesOA)
					&& DomainObjectServices.AreEquivalent(ToMsFeaturesOA, derivMsa.ToMsFeaturesOA)
					&& FromPartOfSpeechRA == derivMsa.FromPartOfSpeechRA
					&& ToPartOfSpeechRA == derivMsa.ToPartOfSpeechRA
					&& FromInflectionClassRA == derivMsa.FromInflectionClassRA
					&& FromStemNameRA == derivMsa.FromStemNameRA
					&& ToInflectionClassRA == derivMsa.ToInflectionClassRA
					&& FromProdRestrictRC.IsEquivalent(derivMsa.FromProdRestrictRC)
					&& ToProdRestrictRC.IsEquivalent(derivMsa.ToProdRestrictRC));
		}

		/// <summary>
		/// Determines whether the specified Sandbox MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoDerivAffMsa to compare with the current MoDerivAffMsa.</param>
		/// <returns>true if the specified MoDerivAffMsa is equal to the current MoDerivAffMsa; otherwise, false.</returns>
		public override bool EqualsMsa(SandboxGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			if (msa.MsaType != MsaType.kDeriv)
				return false;

			// Check the two properties we get from the DLG match, and the others we care about are missing.
			return FromPartOfSpeechRA == msa.MainPOS
				   && ToPartOfSpeechRA == msa.SecondaryPOS
				   && FromInflectionClassRA == null
				   && FromStemNameRA == null
				   && ToInflectionClassRA == null
				   && FromProdRestrictRC.Count == 0
				   && ToProdRestrictRC.Count == 0
				   && (ToMsFeaturesOA == null)
				   && (FromMsFeaturesOA == null);
		}

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in the MSADlgLauncherSlice slice.
		/// </summary>
		public override ITsString LongNameTs
		{
			get
			{
				string sMsaName = "";
				if (FromPartOfSpeechRA != null && ToPartOfSpeechRA != null)
				{
					// Both are non-null.
					if (FromPartOfSpeechRA == ToPartOfSpeechRA)
					{
						// Both are the same POS.
						sMsaName = String.Format(Strings.ksAffixChangesX,
												 CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRA).Text);
					}
					else
					{
						// Different POSes.
						sMsaName = String.Format(Strings.ksAffixConvertsXtoY,
												 CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRA).Text,
												 CmPossibility.BestAnalysisOrVernName(m_cache, ToPartOfSpeechRA).Text);
					}
				}
				else
				{
					// One must be null. Both may be null.
					if (FromPartOfSpeechRA == null && ToPartOfSpeechRA == null)
					{
						// Both are null.
						sMsaName = Strings.ksAffixChangesAny;
					}
					else
					{
						if (FromPartOfSpeechRA == null)
						{
							// From POS is null.
							sMsaName = String.Format(Strings.ksAffixConvertsAnyToX,
													 CmPossibility.BestAnalysisOrVernName(m_cache, ToPartOfSpeechRA).Text);
						}
						else
						{
							// To POS is null.
							sMsaName = String.Format(Strings.ksAffixConvertsXtoAny,
													 CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRA).Text);
						}
					}
				}
				return Cache.TsStrFactory.MakeString(
					sMsaName,
					m_cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				return InterlinearAffix(CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRA, Strings.ksAny),
										CmPossibility.BestAnalysisOrVernName(m_cache, ToPartOfSpeechRA, Strings.ksAny));
			}
		}

		private ITsString InterlinearAffix(ITsString tssFromPartOfSpeech, ITsString tssToPartOfSpeech)
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.AppendTsString(tssFromPartOfSpeech);
			tisb.AppendTsString(TsStringUtils.MakeTss(">", m_cache.WritingSystemFactory.UserWs));
			tisb.AppendTsString(tssToPartOfSpeech);
			return tisb.GetString();
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get
			{
				return InterlinAbbrTSS(WritingSystemServices.kwsFirstAnalOrVern);
			}
		}

		/// <summary>
		/// Return the Grammatical Info. (POS) TsString for the given writing system.
		/// </summary>
		/// <param name="wsAnal">If this is magic WS then return BestAnalorVern. Otherwise return the
		/// string associated with the specific writing system.</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{
			return InterlinearAffix(CmPossibility.TSSAbbrforWS(m_cache, FromPartOfSpeechRA, wsAnal),
									CmPossibility.TSSAbbrforWS(m_cache, ToPartOfSpeechRA, wsAnal));
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get { return InterlinearNameTSS; }
		}

		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or null if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			var posFrom = FromPartOfSpeechRA;
			var posTo = ToPartOfSpeechRA;
			if (posFrom != null || posTo != null)
			{
				var tisb = TsIncStrBldrClass.Create();
				if (posFrom != null)
				{
					var tssPOS = posFrom.Abbreviation.get_String(ws);
					if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
						tssPOS = posFrom.Abbreviation.BestAnalysisVernacularAlternative;
					tisb.AppendTsString(tssPOS);
				}
				else
				{
					tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
				}
				tisb.AppendTsString(m_cache.MakeUserTss(" > "));
				if (posTo != null)
				{
					var tssPOS = posTo.Abbreviation.get_String(ws);
					if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
						tssPOS = posTo.Abbreviation.BestAnalysisVernacularAlternative;
					tisb.AppendTsString(tssPOS);
				}
				else
				{
					tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
				}
				return tisb.GetString();
			}
			return base.PartOfSpeechForWsTSS(ws);
		}

		/// <summary>
		/// Get the inflection class ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The inflection class abbreviation as a TsString for the given writing system,
		/// or null if the inflection class is undefined.</returns>
		/// Note. This is not really the right answer for MoDerivAffMsa. This, along with the POS produces
		/// something like V>N(I>III), while what would really be desired is V(I)>N(III). There are actually
		/// 5 From attributes that need to be nested together with desired order and decorations before the
		/// wedge, and then followed by 4 To attributes again nested together with desired order and decorations.
		/// This kind of change is going to require more massive changes to support.
		public override ITsString InflectionClassForWsTSS(int ws)
		{
			var icFrom = FromInflectionClassRA;
			var icTo = ToInflectionClassRA;
			if (icFrom != null || icTo != null)
			{
				var tisb = TsIncStrBldrClass.Create();
				if (icFrom != null)
				{
					var tssIC = icFrom.Abbreviation.get_String(ws);
					if (tssIC == null || String.IsNullOrEmpty(tssIC.Text))
						tssIC = icFrom.Abbreviation.BestAnalysisVernacularAlternative;
					tisb.AppendTsString(tssIC);
				}
				else
				{
					tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
				}
				tisb.AppendTsString(m_cache.MakeUserTss(" > "));
				if (icTo != null)
				{
					var tssIC = icTo.Abbreviation.get_String(ws);
					if (tssIC == null || String.IsNullOrEmpty(tssIC.Text))
						tssIC = icTo.Abbreviation.BestAnalysisVernacularAlternative;
					tisb.AppendTsString(tssIC);
				}
				else
				{
					tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
				}
				return tisb.GetString();
			}
			return base.InflectionClassForWsTSS(ws);
		}

		partial void FromPartOfSpeechRASideEffects(IPartOfSpeech oldObjValue, IPartOfSpeech newObjValue)
		{
			if (oldObjValue != null && oldObjValue != newObjValue)
				CopyFromAttributesIfValid(this);
		}

		partial void ToPartOfSpeechRASideEffects(IPartOfSpeech oldObjValue, IPartOfSpeech newObjValue)
		{
			if (oldObjValue != null && oldObjValue != newObjValue)
				CopyToAttributesIfValid(this);
		}

		/// <summary>
		/// Update an extant MSA to the new values in the sandbox MSA,
		/// or make a new MSA with the values in the sandbox msa.
		/// </summary>
		/// <param name="sandboxMsa"></param>
		/// <returns></returns>
		public override IMoMorphSynAnalysis UpdateOrReplace(SandboxGenericMSA sandboxMsa)
		{
			if (sandboxMsa.MsaType == MsaType.kDeriv)
			{
				if (FromPartOfSpeechRA != sandboxMsa.MainPOS)
					FromPartOfSpeechRA = sandboxMsa.MainPOS;
				if (ToPartOfSpeechRA != sandboxMsa.SecondaryPOS)
					ToPartOfSpeechRA = sandboxMsa.SecondaryPOS;
				return this;
			}

			return base.UpdateOrReplace(sandboxMsa);
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case MoDerivAffMsaTags.kflidFromInflectionClass:
					return FromPartOfSpeechRA;
				case MoDerivAffMsaTags.kflidToInflectionClass:
					return ToPartOfSpeechRA;
				case MoDerivAffMsaTags.kflidFromPartOfSpeech: // fall through
				case MoDerivAffMsaTags.kflidToPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				case MoDerivAffMsaTags.kflidFromProdRestrict: // fall through
				case MoDerivAffMsaTags.kflidToProdRestrict:
					return m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA;
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
				case MoDerivAffMsaTags.kflidFromInflectionClass:
					if (FromPartOfSpeechRA != null)
						return FromPartOfSpeechRA.AllInflectionClasses.Cast<ICmObject>();
					break;
				case MoDerivAffMsaTags.kflidToInflectionClass:
					if (ToPartOfSpeechRA != null)
						return ToPartOfSpeechRA.AllInflectionClasses.Cast<ICmObject>();
					break;
				case MoDerivAffMsaTags.kflidFromStemName:
					if (FromPartOfSpeechRA != null)
						return FromPartOfSpeechRA.AllStemNames.Cast<ICmObject>();
					break;
				default:
					return base.ReferenceTargetCandidates(flid);
			}
			return new Set<ICmObject>(0);
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want all the stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (ToPartOfSpeechRA == null)
					return String.Empty;
				else
					return this.ToPartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}
	}

	internal partial class MoStemMsa
	{
		/// <summary>
		///
		/// </summary>
		public override ITsString FeaturesTSS
		{
			get
			{
				IFsFeatStruc feat = MsFeaturesOA;
				if (feat != null)
					return feat.ShortNameTSS;
				else
					return base.FeaturesTSS;
			}
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString ExceptionFeaturesTSS
		{
			get
			{
				if (ProdRestrictRC.Count > 0)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					bool fFirst = true;
					foreach (CmPossibility pss in ProdRestrictRC)
					{
						if (!fFirst)
							tisb.AppendTsString(m_cache.MakeUserTss(","));
						tisb.AppendTsString(pss.Abbreviation.BestAnalysisVernacularAlternative);
						fFirst = false;
					}
					return tisb.GetString();
				}
				else
				{
					return base.ExceptionFeaturesTSS;
				}
			}
		}
		///<summary>
		/// Copies attributes associated with the current POS, such as inflection features and classes, that
		/// are valid from the specified MSA to this MSA. If the attribute is not valid, it is removed from
		/// this MSA.
		///</summary>
		///<param name="srcMsa">the source MSA</param>
		public void CopyAttributesIfValid(IMoStemMsa srcMsa)
		{
			// inflection classes
			if (IsReferenceAttributeValid(srcMsa.InflectionClassRA, MoStemMsaTags.kflidInflectionClass))
			{
				if (srcMsa.InflectionClassRA != InflectionClassRA)
					InflectionClassRA = srcMsa.InflectionClassRA;
			}
			else if (InflectionClassRA != null)
			{
				InflectionClassRA = null;
			}

			// inflection features
			if (srcMsa.MsFeaturesOA == null)
			{
				MsFeaturesOA = null;
			}
			else
			{
				if (MsFeaturesOA != srcMsa.MsFeaturesOA)
					CopyObject<IFsFeatStruc>.CloneFdoObject(srcMsa.MsFeaturesOA, newMsa => MsFeaturesOA = newMsa);
				RemoveInvalidFeatureSpecs(PartOfSpeechRA, MsFeaturesOA);
				if (MsFeaturesOA != null)
				{
					if (MsFeaturesOA.IsEmpty)
						MsFeaturesOA = null;
				}
			}
		}

		/// <summary>
		/// Set your MsFeatures to a copy of the source object.
		/// </summary>
		public void CopyMsFeatures(IFsFeatStruc source)
		{
			CopyObject<IFsFeatStruc>.CloneFdoObject(source, newFeat => MsFeaturesOA = newFeat);
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override string InterlinearName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == FDO.MoStemMsaTags.kflidPartOfSpeech);
		}

		/// <summary>
		/// Check whether this MoStemMsa object is empty of any content.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public override bool IsEmpty
		{
			get
			{
				return PartOfSpeechRA == null &&
					   InflectionClassRA == null &&
					   FromPartsOfSpeechRC.Count == 0 &&
					   ProdRestrictRC.Count == 0 &&
					   StratumRA == null &&
					   MsFeaturesOA == null &&
					   base.IsEmpty;
			}
		}

		/// <summary>
		/// Determines whether the specified Object is equal to the current MoStemMsa.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public override bool EqualsMsa(IMoMorphSynAnalysis msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoStemMsa.
			if (!(msa is IMoStemMsa))
				return false;

			var stemMsa = (IMoStemMsa)msa;
			// TODO: Add checks for other properties, when we support using them.
			if (stemMsa.PartOfSpeechRA != PartOfSpeechRA)
				return false;
			if (stemMsa.InflectionClassRA != InflectionClassRA)
				return false;
			if (!DomainObjectServices.AreEquivalent(MsFeaturesOA, stemMsa.MsFeaturesOA))
				return false;
			if (!FromPartsOfSpeechRC.IsEquivalent(stemMsa.FromPartsOfSpeechRC))
				return false;
			return ProdRestrictRC.IsEquivalent(stemMsa.ProdRestrictRC);
		}

		/// <summary>
		/// Determines whether the specified Sandbox MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public override bool EqualsMsa(SandboxGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoStemMsa.
			if (msa.MsaType != MsaType.kStem)
				return false;

			// The dummy generic currently can't have any of these, so if this does, they don't match.
			if (InflectionClassRA != null
				|| (MsFeaturesOA != null && !MsFeaturesOA.IsEmpty)
				|| (ProdRestrictRC.Count != 0)
				|| (FromPartsOfSpeechRC.Count != 0))
				return false;

			// TODO: Add checks for other properties, when we support using them.
			return PartOfSpeechRA == msa.MainPOS;
		}

		/// <summary>
		/// Get a string used to to represent an MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				var tsf = Cache.TsStrFactory;
				var analWs = Cache.DefaultAnalWs;
				var pos = PartOfSpeechRA;
				if (pos != null)
					tisb.AppendTsString(pos.Abbreviation.AnalysisDefaultWritingSystem);
				else
					tisb.AppendTsString(tsf.MakeString(Strings.ksQuestions, analWs));

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in the MSADlgLauncherSlice slice.
		/// </summary>
		public override ITsString LongNameTs
		{
			get
			{
				if (PartOfSpeechRA != null)
					return CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRA);

				var userWs = m_cache.DefaultUserWs;
				var entry = Owner as ILexEntry;
				foreach (var form in entry.AllAllomorphs)
				{
					// LT-7075 was crashing when it was null,
					// trying to get the Guid.
					var guid = System.Guid.Empty;
					if (form.MorphTypeRA != null)
						guid = form.MorphTypeRA.Guid;
					if ((guid != MoMorphTypeTags.kguidMorphClitic) &&
						(guid != MoMorphTypeTags.kguidMorphEnclitic) &&
						(guid != MoMorphTypeTags.kguidMorphProclitic))
						return Cache.TsStrFactory.MakeString(Strings.ksStemNoCatInfo, userWs);
				}
				return Cache.TsStrFactory.MakeString(Strings.ksCliticNoCatInfo, userWs);
			}
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				return InterlinearStem(CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRA));
			}
		}

		private ITsString InterlinearStem(ITsString tssPartOfSpeech)
		{
			var userWs = m_cache.WritingSystemFactory.UserWs;
			if (PartOfSpeechRA == null)
				return TsStringUtils.MakeTss(Strings.ksNotSure, userWs);

			var tssName = tssPartOfSpeech;
			var bldr = tssName.GetBldr();
			int cch = bldr.Length;
			if (InflectionClassRA != null)
			{
				bldr.ReplaceTsString(cch, cch, TsStringUtils.MakeTss("  (", userWs));
				cch = bldr.Length;
				bldr.ReplaceTsString(cch, cch, InflectionClassRA.Abbreviation.BestAnalysisVernacularAlternative);
				cch = bldr.Length;
				bldr.ReplaceTsString(cch, cch, TsStringUtils.MakeTss(") ", userWs));
			}
			else
			{
				bldr.ReplaceTsString(cch, cch, TsStringUtils.MakeTss(" ", userWs));
			}
			cch = bldr.Length;
			var features = MsFeaturesOA;
			if (features != null)
				bldr.ReplaceTsString(cch, cch, TsStringUtils.MakeTss(features.ShortName, userWs));

			return bldr.GetString();
		}

		/// <summary>
		/// Return the Grammatical Info. (POS) TsString for the given writing system.
		/// </summary>
		/// <param name="wsAnal">If this is magic WS then return BestAnalorVern. Otherwise return the
		/// string associated with the specific writing system.</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{
			return InterlinearStem(CmPossibility.TSSAbbrforWS(m_cache, PartOfSpeechRA, wsAnal));
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get
			{
				return InterlinAbbrTSS(WritingSystemServices.kwsFirstAnalOrVern);
			}
		}

		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or null if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			var pos = PartOfSpeechRA;
			if (pos != null)
			{
				var tssPOS = pos.Abbreviation.get_String(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				return base.PartOfSpeechForWsTSS(ws);
			}
		}

		/// <summary>
		/// Get the inflection class ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The inflection class abbreviation as a TsString for the given writing system,
		/// or null if the inflection class is undefined.</returns>
		public override ITsString InflectionClassForWsTSS(int ws)
		{
			var ic = InflectionClassRA;
			if (ic != null)
			{
				var tssIC = ic.Abbreviation.get_String(ws);
				if (tssIC == null || String.IsNullOrEmpty(tssIC.Text))
					tssIC = ic.Abbreviation.BestAnalysisVernacularAlternative;
				return tssIC;
			}
			else
			{
				return base.InflectionClassForWsTSS(ws);
			}
		}

		partial void PartOfSpeechRASideEffects(IPartOfSpeech oldObjValue, IPartOfSpeech newObjValue)
		{
			if (oldObjValue != null && oldObjValue != newObjValue)
				CopyAttributesIfValid(this);
		}

		/// <summary>
		/// Update an extant MSA to the new values in the sandbox MSA,
		/// or make a new MSA with the values in the sandbox msa.
		/// </summary>
		/// <param name="sandboxMsa"></param>
		/// <returns></returns>
		public override IMoMorphSynAnalysis UpdateOrReplace(SandboxGenericMSA sandboxMsa)
		{
			if (sandboxMsa.MsaType == MsaType.kStem || sandboxMsa.MsaType == MsaType.kRoot)
			{
				if (PartOfSpeechRA != sandboxMsa.MainPOS)
					PartOfSpeechRA = sandboxMsa.MainPOS;
				return this;
			}

			return base.UpdateOrReplace(sandboxMsa);
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case MoStemMsaTags.kflidInflectionClass:
					return PartOfSpeechRA;
				case MoStemMsaTags.kflidPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				case MoStemMsaTags.kflidProdRestrict:
					return m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA;
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
				case MoStemMsaTags.kflidInflectionClass:
					if (PartOfSpeechRA != null)
						return PartOfSpeechRA.AllInflectionClasses.Cast<ICmObject>();
					break;
				case MoStemMsaTags.kflidFromPartsOfSpeech:
					return Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
			return new Set<ICmObject>(0);
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want the inflection class numbers or feature stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (PartOfSpeechRA == null)
					return String.Empty;
				else
					return this.PartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>
		/// Inflection class is irrelevant for LeftMsa and RightMsa in binary compounds and also if no category.
		/// FromPartsOfSpeech is irrelevant
		/// </remarks>
		/// <returns></returns>
		public override bool IsFieldRelevant(int flid)
		{
			if (flid == MoStemMsaTags.kflidInflectionClass)
			{
				// Inflection class is irrelevant for left/right members of a compound
				if ((OwningFlid == MoBinaryCompoundRuleTags.kflidLeftMsa) ||
					(OwningFlid == MoBinaryCompoundRuleTags.kflidRightMsa))
					return false;
				if (PartOfSpeechRA == null)
					return false; // if no POS has been specified, then there's no need to show infl class
			}
			else if (flid == MoStemMsaTags.kflidFromPartsOfSpeech)
			{
				if (!OwningLexEntryHasProCliticOrEnclitic())
					return false;
			}
			return base.IsFieldRelevant(flid);
		}

		private bool OwningLexEntryHasProCliticOrEnclitic()
		{
			if (OwningFlid != LexEntryTags.kflidMorphoSyntaxAnalyses)
			{ // FromPartsOfSpeech only relevant for a proclitic or enclitic
				return false;
			}
			try
			{
				ILexEntry entry = Owner as ILexEntry;
				foreach (IMoMorphType mt in entry.MorphTypes)
				{
					if ((mt.Guid == MoMorphTypeTags.kguidMorphProclitic) ||
						(mt.Guid == MoMorphTypeTags.kguidMorphEnclitic))
						return true;
				}
			}
			catch
			{
				return false;
			}
			return false;
		}
	}

	/// <summary></summary>
	internal partial class MoInflAffMsa
	{

		public override ITsString FeaturesTSS
		{
			get
			{
				IFsFeatStruc feat = InflFeatsOA;
				if (feat != null)
					return feat.ShortNameTSS;
				else
					return base.FeaturesTSS;
			}
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString ExceptionFeaturesTSS
		{
			get
			{
				if (FromProdRestrictRC.Count > 0)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					bool fFirst = true;
					foreach (CmPossibility pss in FromProdRestrictRC)
					{
						if (!fFirst)
							tisb.AppendTsString(m_cache.MakeUserTss(","));
						tisb.AppendTsString(pss.Abbreviation.BestAnalysisVernacularAlternative);
						fFirst = false;
					}
					return tisb.GetString();
				}
				else
				{
					return base.ExceptionFeaturesTSS;
				}
			}
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case MoInflAffMsaTags.kflidSlots:
					var target = ((IMoInflAffixSlot) e.ObjectAdded);
					var flid = m_cache.MetaDataCache.GetFieldId2(MoInflAffixSlotTags.kClassId, "Affixes", false);

					var newGuids = (from msa in target.Affixes select msa.Guid).ToArray();

					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(target, flid,
						new Guid[0], newGuids);
					break;
				default:
					base.AddObjectSideEffectsInternal(e);
					break;
			}
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				case MoInflAffMsaTags.kflidSlots:
					var target = ((IMoInflAffixSlot)e.ObjectRemoved);
					var flid = m_cache.MetaDataCache.GetFieldId2(MoInflAffixSlotTags.kClassId, "Affixes", false);

					var newGuids = (from msa in target.Affixes select msa.Guid).ToArray();

					m_cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(target, flid,
						new Guid[0], newGuids);
					break;
				default:
					base.RemoveObjectSideEffectsInternal(e);
					break;
			}
		}

		///<summary>
		/// Copies attributes associated with the current POS, such as inflection features and classes, that
		/// are valid from the specified MSA to this MSA. If the attribute is not valid, it is removed from
		/// this MSA.
		///</summary>
		///<param name="srcMsa">the source MSA</param>
		public void CopyAttributesIfValid(IMoInflAffMsa srcMsa)
		{
			// inflection features
			if (srcMsa.InflFeatsOA == null)
			{
				InflFeatsOA = null;
			}
			else
			{
				if (InflFeatsOA != srcMsa.InflFeatsOA)
					CopyObject<IFsFeatStruc>.CloneFdoObject(srcMsa.InflFeatsOA, newMsa => InflFeatsOA = newMsa);
				RemoveInvalidFeatureSpecs(PartOfSpeechRA, InflFeatsOA);
				if (InflFeatsOA.IsEmpty)
					InflFeatsOA = null;
			}
		}

		/// <summary>
		/// The best/first gloss for this MSA
		/// </summary>
		/// <remarks>Used in the chooser.</remarks>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString Gloss
		{
			get
			{
				ITsString tssGloss = GetFirstGlossOfMSAThatMatchesTss(OwningEntry.SensesOS);
				if (tssGloss == null && OwningEntry.SensesOS.Count > 0)
				{ // if we can't find an MSA that matches, just use first gloss
					return OwningEntry.SensesOS[0].Gloss.BestAnalysisAlternative;
				}
				return tssGloss;
			}
		}

		/// <summary>
		/// the way we want to show this in an interlinear view
		/// </summary>
		public override string InterlinearName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// Check whether this MoInflAffMsa object is empty of content.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public override bool IsEmpty
		{
			get
			{
				return PartOfSpeechRA == null &&
					   SlotsRC.Count == 0 &&
					   AffixCategoryRA == null &&
					   FromProdRestrictRC.Count == 0 &&
					   InflFeatsOA == null &&
					   base.IsEmpty;
			}
		}

		/// <summary>
		/// Determines whether the specified MoInflAffMsa is equal to the current MoInflAffMsa.
		/// </summary>
		/// <param name="msa">The MoInflAffMsa to compare with the current MoInflAffMsa.</param>
		/// <returns>true if the specified MoInflAffMsa is equal to the current MoInflAffMsa; otherwise, false.</returns>
		public override bool EqualsMsa(IMoMorphSynAnalysis msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoInflAffMsa.
			if (!(msa is IMoInflAffMsa))
				return false;

			var inflMsa = (IMoInflAffMsa)msa;
			return DomainObjectServices.AreEquivalent(InflFeatsOA, inflMsa.InflFeatsOA)
				   && FromProdRestrictRC.IsEquivalent(inflMsa.FromProdRestrictRC)
				   && PartOfSpeechRA == inflMsa.PartOfSpeechRA
				   && HasSameSlots(inflMsa);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="inflMsa"></param>
		/// <returns></returns>
		protected bool HasSameSlots(IMoInflAffMsa inflMsa)
		{
			if (SlotsRC.Count != inflMsa.SlotsRC.Count)
				return false;

			var otherSlots = new List<ICmObject>(inflMsa.SlotsRC.Objects);
			foreach (var mySlot in SlotsRC.Objects)
			{
				if (!otherSlots.Contains(mySlot))
					return false;
				otherSlots.Remove(mySlot); // ensure unique matches.
			}
			return true;
		}

		/// <summary>
		/// Determines whether the specified Sandbox MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public override bool EqualsMsa(SandboxGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoStemMsa.
			if (msa.MsaType != MsaType.kInfl)
				return false;

			if (PartOfSpeechRA != msa.MainPOS)
				return false;

			// Can't set these two from the dialog, if non-null, we are not equal.
			if (FromProdRestrictRC.Count != 0)
				return false;
			if (InflFeatsOA != null && !InflFeatsOA.IsEmpty)
				return false;

			// TODO: Add checks for other properties, when we support using them.
			if (msa.Slot == null)
				return (SlotsRC.Count == 0);
			else
				return SlotsRC.Count == 1 && SlotsRC.Contains(msa.Slot);
		}

		/// <summary>
		/// Get the preferred writing system identifier for the class.
		/// </summary>
		protected override string PreferredWsId
		{
			get { return Services.WritingSystems.DefaultVernacularWritingSystem.Id; }
		}

		/// <summary>
		/// the name which assumes the maximum context
		/// </summary>
		public override string ShortName
		{
			get
			{
				var entry = Owner as ILexEntry;
				var sb = new StringBuilder();
				sb.Append(entry.CitationFormWithAffixType);
				sb.Append(" ");
				var sGloss = GetFirstGlossOfMSAThatMatches(entry.SensesOS);
				if (sGloss == null)
				{ // if we can't find an MSA that matches, just use first gloss
					var sense = entry.SensesOS[0];
					sb.Append(sense.Gloss.AnalysisDefaultWritingSystem);
				}
				else
				{
					sb.Append(sGloss);
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in the MSADlgLauncherSlice slice.
		/// </summary>
		public override ITsString LongNameTs
		{
			get
			{
				var sMsaName = "";
				if (PartOfSpeechRA == null)
				{
					sMsaName = Strings.ksAffixInflectsAny;
				}
				else
				{
					if (SlotsRC.Count == 0)
					{
						// Only have the POS.
						Debug.Assert(PartOfSpeechRA != null);
						sMsaName = String.Format(Strings.ksAffixInflectsX,
												 CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRA).Text);
						// don't want this per Andy's comment in LT-4903 because it isn't accurate.
						//tisb.AppendTsString(tsf.MakeString(" in any slot", userWs));
					}
					else
					{
						var bldrSlots = new StringBuilder();
						var cnt = 0;
						foreach (var slot in SlotsRC)
						{
							if (cnt++ > 0)
								bldrSlots.Append("/");
							bldrSlots.Append(slot.ShortName);
						}
						if (cnt > 1)
						{
							sMsaName = String.Format(Strings.ksAffixInXInflectsYPlural,
													 bldrSlots.ToString(),
													 CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRA).Text);
						}
						else
						{
							sMsaName = String.Format(Strings.ksAffixInXInflectsY,
													 bldrSlots.ToString(),
													 CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRA).Text);
						}
					}
				}
				return Cache.TsStrFactory.MakeString(
					sMsaName,
					m_cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// Mimics ShortName, but with TsStrings so we get proper writing systems.
		/// TODO (DamienD): register prop change when dependencies change
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public override ITsString ShortNameTSS
		{
			get
			{
				var entry = Owner as ILexEntry;
				var tisb = TsIncStrBldrClass.Create();
				(entry as LexEntry).CitationFormWithAffixTypeTss(tisb);
				tisb.Append(" ");
				var tssGloss = GetFirstGlossOfMSAThatMatchesTss(entry.SensesOS);
				if (tssGloss == null || tssGloss.Length == 0)
				{
					// if we can't find an MSA that matches, just use first gloss
					var sense = entry.SensesOS[0];
					if (sense != null)
						tssGloss = sense.Gloss.AnalysisDefaultWritingSystem;
				}
				if (tssGloss != null && tssGloss.Length > 0)
					tisb.AppendTsString(tssGloss);
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get the default analysis gloss of the first sense of the specified sense container
		/// (typically our owning entry, but we may recurse through its nested senses)
		/// that uses this MSA.
		/// </summary>
		/// <param name="os"></param>
		/// <returns></returns>
		private ITsString GetFirstGlossOfMSAThatMatchesTss(IFdoOwningSequence<ILexSense> os)
		{
			foreach (var sense in os)
			{
				// Get the gloss of the first sense that refers to this MSA.
				if (sense.MorphoSyntaxAnalysisRA == this)
				{
					return sense.Gloss.AnalysisDefaultWritingSystem;
				}
				else
				{
					var sGloss = GetFirstGlossOfMSAThatMatchesTss(sense.SensesOS);
					if (sGloss != null)
						return sGloss; // first gloss was in a subsense; quit
				}
			}
			return null;
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				return InterlinAbbrTSS(WritingSystemServices.kwsFirstAnalOrVern);
			}
		}

		private ITsString InterlinearInflectionalAffix(ITsString tssPartOfSpeech, int ws)
		{
			if (PartOfSpeechRA == null)
				return TsStringUtils.MakeTss(Strings.ksInflectsAnyCat, m_cache.WritingSystemFactory.UserWs);

			var tsf = Cache.TsStrFactory;
			var tisb = TsIncStrBldrClass.Create();
			var userWs = m_cache.WritingSystemFactory.UserWs;

			tisb.AppendTsString(tssPartOfSpeech);
			tisb.AppendTsString(tsf.MakeString(":", userWs));

			var cnt = 0;
			foreach (MoInflAffixSlot slot in SlotsRC)
			{
				if (cnt++ > 0)
					tisb.AppendTsString(tsf.MakeString("/", userWs));
				tisb.AppendTsString(slot.ShortNameTSSforWS(ws));
			}
			if (cnt == 0) // No slots.
				tisb.AppendTsString(tsf.MakeString(Strings.ksAny, userWs));
			return tisb.GetString();
		}

		/// <summary>
		/// override to return our slots.
		/// Enhance JohnT: generate propchanged when real Slots property changes.
		/// </summary>
		public override IEnumerable<IMoInflAffixSlot> Slots
		{
			get
			{
				return SlotsRC;
			}
		}

		/// <summary>
		/// Return the Grammatical Info. (POS) TsString for the given writing system.
		/// </summary>
		/// <param name="wsAnal">If this is magic WS then return BestAnalorVern. Otherwise return the
		/// string associated with the specific writing system.</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{

			return InterlinearInflectionalAffix(CmPossibility.TSSAbbrforWS(m_cache, PartOfSpeechRA, wsAnal), wsAnal);
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get
			{
				return InterlinAbbrTSS(WritingSystemServices.kwsFirstAnalOrVern);
			}
		}

		private string GetFirstGlossOfMSAThatMatches(IFdoOwningSequence<ILexSense> os)
		{
			var tss = GetFirstGlossOfMSAThatMatchesTss(os);
			if (tss != null)
				return tss.Text;
			return null;
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get { return InterlinearNameTSS; }
		}
		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or empty if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			var pos = PartOfSpeechRA;
			if (pos != null)
			{
				var tssPOS = pos.Abbreviation.get_String(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				return base.PartOfSpeechForWsTSS(ws);
			}
		}

		partial void PartOfSpeechRASideEffects(IPartOfSpeech oldObjValue, IPartOfSpeech newObjValue)
		{
			if (oldObjValue != null && oldObjValue != newObjValue)
				CopyAttributesIfValid(this);
		}

		/// <summary>
		/// Update an extant MSA to the new values in the sandbox MSA,
		/// or make a new MSA with the values in the sandbox msa.
		/// </summary>
		/// <param name="sandboxMsa"></param>
		/// <returns></returns>
		public override IMoMorphSynAnalysis UpdateOrReplace(SandboxGenericMSA sandboxMsa)
		{
			if (sandboxMsa.MsaType == MsaType.kInfl)
			{
				if (PartOfSpeechRA != sandboxMsa.MainPOS)
					PartOfSpeechRA = sandboxMsa.MainPOS;

				if (sandboxMsa.Slot != null && !SlotsRC.Contains(sandboxMsa.Slot))
					SlotsRC.Add(sandboxMsa.Slot);
				// Remove any slots that are not legal for the current POS.
				IEnumerable<IMoInflAffixSlot> allSlots;
				if (PartOfSpeechRA == null)
					allSlots = Enumerable.Empty<IMoInflAffixSlot>();
				else
					allSlots = PartOfSpeechRA.AllAffixSlots;
				foreach (IMoInflAffixSlot slot in SlotsRC.ToArray())
				{
					if (!allSlots.Contains(slot))
						SlotsRC.Remove(slot);
				}
				return this;
			}

			return base.UpdateOrReplace(sandboxMsa);
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case MoInflAffMsaTags.kflidPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				case MoInflAffMsaTags.kflidSlots:
					return PartOfSpeechRA;
				case MoInflAffMsaTags.kflidFromProdRestrict:
					return m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA;
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
				case MoInflAffMsaTags.kflidSlots:
					if (PartOfSpeechRA != null)
						return DomainObjectServices.GetSlots(Cache, Owner as ILexEntry, PartOfSpeechRA).Cast<ICmObject>();
					break;
				default:
					return base.ReferenceTargetCandidates(flid);
			}
			return new Set<ICmObject>(0);
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want all the stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (PartOfSpeechRA == null)
					return String.Empty;
				else
					return this.PartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}
	}

	/// <summary>
	/// TODO: Currently never used. But, when we start using this class, we should revise the Msa tests in LingTests.cs
	/// (e.g. EqualMsaTests).
	/// </summary>
	internal partial class MoDerivStepMsa
	{
		public override ITsString FeaturesTSS
		{
			get
			{
				IFsFeatStruc featMS = MsFeaturesOA;
				IFsFeatStruc featInfl = InflFeatsOA;
				if (featMS != null && featInfl != null)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					tisb.AppendTsString(featMS.ShortNameTSS);
					tisb.AppendTsString(m_cache.MakeUserTss(" / "));
					tisb.AppendTsString(featInfl.ShortNameTSS);
					return tisb.GetString();
				}
				else if (featMS != null)
				{
					return featMS.ShortNameTSS;
				}
				else if (featInfl != null)
				{
					return featInfl.ShortNameTSS;
				}
				else
				{
					return base.FeaturesTSS;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString ExceptionFeaturesTSS
		{
			get
			{
				if (ProdRestrictRC.Count > 0)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					bool fFirst = true;
					foreach (CmPossibility pss in ProdRestrictRC)
					{
						if (!fFirst)
							tisb.AppendTsString(m_cache.MakeUserTss(","));
						tisb.AppendTsString(pss.Abbreviation.BestAnalysisVernacularAlternative);
						fFirst = false;
					}
					return tisb.GetString();
				}
				else
				{
					return base.ExceptionFeaturesTSS;
				}
			}
		}

		/// <summary>
		/// Check whether this MoDerivStepMsa object is empty of content.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public override bool IsEmpty
		{
			get
			{
				return PartOfSpeechRA == null &&
					   InflectionClassRA == null &&
					   ProdRestrictRC.Count == 0 &&
					   InflFeatsOA == null &&
					   MsFeaturesOA == null &&
					   base.IsEmpty;
			}
		}

		/// <summary>
		/// Determines whether the specified MoDerivAffMsa is equal to the current MoDerivAffMsa.
		/// </summary>
		/// <param name="msa">The MoDerivAffMsa to compare with the current MoDerivAffMsa.</param>
		/// <returns>true if the specified MoDerivAffMsa is equal to the current MoDerivAffMsa; otherwise, false.</returns>
		public override bool EqualsMsa(IMoMorphSynAnalysis msa)
		{
			throw new NotImplementedException("'EqualsMsa' not implemneted on class MoDerivStepMsa.");
		}

		/// <summary>
		/// Determines whether the specified Sandbox MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoUnclassifiedAffixMsa to compare with the current MoUnclassifiedAffixMsa.</param>
		/// <returns>true if the specified MoUnclassifiedAffixMsa is equal to the current MoUnclassifiedAffixMsa; otherwise, false.</returns>
		public override bool EqualsMsa(SandboxGenericMSA msa)
		{
			throw new NotImplementedException("'EqualsMsa' not implemneted on class MoDerivStepMsa.");
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				throw new NotImplementedException("'InterlinearNameTSS' not implemented on class MoDerivStepMsa.");
			}
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in the MSADlgLauncherSlice slice.
		/// </summary>
		public override ITsString LongNameTs
		{
			get
			{
				throw new NotImplementedException("'LongNameTs' not implemneted on class MoDerivStepMsa.");
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="wsAnal"></param>
		/// <returns></returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or null if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			var pos = PartOfSpeechRA;
			if (pos != null)
			{
				var tssPOS = pos.Abbreviation.get_String(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				return base.PartOfSpeechForWsTSS(ws);
			}
		}

		/// <summary>
		/// Get the inflection class ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The inflection class abbreviation as a TsString for the given writing system,
		/// or null if the inflection class is undefined.</returns>
		public override ITsString InflectionClassForWsTSS(int ws)
		{
			var ic = InflectionClassRA;
			if (ic != null)
			{
				var tssIC = ic.Abbreviation.get_String(ws);
				if (tssIC == null || String.IsNullOrEmpty(tssIC.Text))
					tssIC = ic.Abbreviation.BestAnalysisVernacularAlternative;
				return tssIC;
			}
			else
			{
				return base.InflectionClassForWsTSS(ws);
			}
		}

		partial void PartOfSpeechRASideEffects(IPartOfSpeech oldObjValue, IPartOfSpeech newObjValue)
		{
			// When we change the part of speech to something different, we can't keep
			// the old InflectionClass since it is part of the original part of speech.
			// We try to allow any code (maybe copy operation) that wants to set the
			// inflection class before the part of speech.
			if (oldObjValue != null && oldObjValue != newObjValue)
				InflectionClassRA = null;
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want all the stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (PartOfSpeechRA == null)
					return String.Empty;
				else
					return this.PartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}
	}

	/// <summary>
	/// TODO: Currently never used. But, when we start using this class, we should revise the Msa tests in LingTests.cs
	/// (e.g. EqualMsaTests).
	/// </summary>
	internal partial class MoUnclassifiedAffixMsa
	{
		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override string InterlinearName
		{
			get
			{
				return InterlinearAbbr;
			}
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override string InterlinearAbbr
		{
			get
			{
				var retval = ChooserNameTS.Text;
				if (retval == null || retval == String.Empty)
					retval = Strings.ksQuestions;
				return retval;
			}
		}

		/// <summary>
		/// Check whether this MoUnclassifiedAffixMsa object is empty of content.
		/// </summary>
		[VirtualProperty(CellarPropertyType.Boolean)]
		public override bool IsEmpty
		{
			get
			{
				return PartOfSpeechRA == null && base.IsEmpty;
			}
		}

		/// <summary>
		/// Determines whether the specified MoDerivAffMsa is equal to the current MoDerivAffMsa.
		/// </summary>
		/// <param name="msa">The MoDerivAffMsa to compare with the current MoDerivAffMsa.</param>
		/// <returns>true if the specified MoDerivAffMsa is equal to the current MoDerivAffMsa; otherwise, false.</returns>
		public override bool EqualsMsa(IMoMorphSynAnalysis msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoUnclassifiedAffixMsa.
			if (!(msa is IMoUnclassifiedAffixMsa))
				return false;

			var uncMsa = (IMoUnclassifiedAffixMsa)msa;
			return PartOfSpeechRA == uncMsa.PartOfSpeechRA;
		}

		/// <summary>
		/// Determines whether the specified Sandbox MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoUnclassifiedAffixMsa to compare with the current MoUnclassifiedAffixMsa.</param>
		/// <returns>true if the specified MoUnclassifiedAffixMsa is equal to the current MoUnclassifiedAffixMsa; otherwise, false.</returns>
		public override bool EqualsMsa(SandboxGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoStemMsa.
			if (msa.MsaType != MsaType.kUnclassified)
				return false;

			// TODO: Add checks for other properties, when we support using them.
			return PartOfSpeechRA == msa.MainPOS;
		}

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in the MSADlgLauncherSlice slice.
		/// </summary>
		public override ITsString LongNameTs
		{
			get
			{
				var sMsaName = "";
				sMsaName = PartOfSpeechRA == null
							? Strings.ksAffixAttachesToAny
							: String.Format(Strings.ksAffixFoundOnX,
											CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRA).Text);
				return Cache.TsStrFactory.MakeString(
					sMsaName,
					m_cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				if (PartOfSpeechRA == null)
					return m_cache.MakeUserTss(Strings.ksAttachesToAnyCat);
				return CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRA);
			}
		}

		/// <summary>
		/// Get the BestAnalorVern PartofSpeech for a Lexmeme.
		/// </summary>
		/// <param name="wsAnal">on this class we are not making use of this parameter</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{
			//throw new Exception("The method or operation is not implemented.");
			return InterlinearNameTSS;
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get { return InterlinAbbrTSS(Cache.DefaultAnalWs); }
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				var tsf = Cache.TsStrFactory;
				var pos = PartOfSpeechRA;
				return pos != null
						? CmPossibility.BestAnalysisOrVernAbbr(Cache, PartOfSpeechRA.Hvo)
						: tsf.MakeString(
							Strings.ksAttachesToAnyCat,
							Cache.DefaultUserWs);
			}
		}
		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or null if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			var pos = PartOfSpeechRA;
			if (pos != null)
			{
				var tssPOS = pos.Abbreviation.get_String(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				return base.PartOfSpeechForWsTSS(ws);
			}
		}

		/// <summary>
		/// Update an extant MSA to the new values in the dummy MSA,
		/// or make a new MSA with the values in the dummy msa.
		/// </summary>
		/// <param name="sandboxMsa"></param>
		/// <returns></returns>
		public override IMoMorphSynAnalysis UpdateOrReplace(SandboxGenericMSA sandboxMsa)
		{
			if (sandboxMsa.MsaType == MsaType.kUnclassified)
			{
				if (PartOfSpeechRA != sandboxMsa.MainPOS)
					PartOfSpeechRA = sandboxMsa.MainPOS;
				return this;
			}

			return base.UpdateOrReplace(sandboxMsa);
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case MoUnclassifiedAffixMsaTags.kflidPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
	}

	/// <summary></summary>
	internal partial class MoInflAffixTemplate
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteAffixTemplate));
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
				case MoInflAffixTemplateTags.kflidSlots:
					// Enhance JohnT: if we really need an implementation for this class and flid, see
					// the corresponding case in ReferenceTargetCandidates. I _think_ it should return
					// the most remote owner that is a PartOfSpeech.
					return null;
				case MoInflAffixTemplateTags.kflidPrefixSlots:
				case MoInflAffixTemplateTags.kflidSuffixSlots:
					return this;
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
				case MoInflAffixTemplateTags.kflidSlots:
					return GetAllSlots();
				case MoInflAffixTemplateTags.kflidPrefixSlots:
					return GetPrefixSlots();
				case MoInflAffixTemplateTags.kflidSuffixSlots:
					// TODO RandyR: Needs to be smarter about using only prefixes/suffixes.
					// The problem is how to tell which they are.
					return GetSuffixSlots();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			Final = true; // Doc says this should be a default (shows in UI as Requires more derivation unchecked)
		}

		/// <summary>
		/// The possible slots for a template are those owned by all owning parts of speech.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<ICmObject> GetAllSlots()
		{
			var pos = ((IPartOfSpeech) OwnerOfClass(PartOfSpeechTags.kClassId));
			while (pos != null)
			{
				foreach (var slot in pos.AffixSlotsOC)
					yield return slot;
				pos = ((IPartOfSpeech) pos.OwnerOfClass(PartOfSpeechTags.kClassId));
			}
		}
		private IEnumerable<ICmObject> GetPrefixSlots()
		{
			return GetSomeSlots(Cache, GetAllSlots(), true);
		}
		private IEnumerable<ICmObject> GetSuffixSlots()
		{
			return GetSomeSlots(Cache, GetAllSlots(), false);
		}
		/// <summary>
		/// Get a set of inflectional affix slots which can be prefixal or suffixal
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="allSlots">Original set of all slots</param>
		/// <param name="fLookForPrefixes">whether to look for prefixal slots</param>
		/// <returns>subset of slots that are either prefixal or suffixal</returns>
		public static IEnumerable<ICmObject> GetSomeSlots(FdoCache cache, IEnumerable<ICmObject> allSlots, bool fLookForPrefixes)
		{
			foreach (MoInflAffixSlot slot in allSlots)
			{
				if (slot == null)
					continue;
				if (slot.Affixes.Count() == 0)
				{ // no affixes in this slot, so include it whether looking for prefix or suffix slots.
					yield return slot;
					continue;
				}
				bool fStopLooking = false;
				foreach (var affix in slot.Affixes)
				{
					LexEntry lex = affix.Owner as LexEntry;
					foreach (var morphType in lex.MorphTypes)
					{
						bool fIsCorrectType;
						if (fLookForPrefixes)
							fIsCorrectType = morphType.IsPrefixishType;
						else
							fIsCorrectType = morphType.IsSuffixishType;
						if (fIsCorrectType)
						{
							yield return slot;
							fStopLooking = true;
							break;
						}
					}
					if (fStopLooking)
						break;
				}
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
			IMoInflAffixTemplate template = clone as IMoInflAffixTemplate;
			if (template == null)
				throw new ApplicationException("Failed to copy inflectional affix template:  the target is not an inflectional affix template.");

			template.Name.CopyAlternatives(Name);
			template.Description.CopyAlternatives(Description);
			template.Final = Final;
			foreach (var obj in PrefixSlotsRS)
				template.PrefixSlotsRS.Add(obj);
			foreach (var obj in SuffixSlotsRS)
				template.SuffixSlotsRS.Add(obj);
			try
			{
				foreach (var obj in SlotsRS)
					template.SlotsRS.Add(obj);
			}
			catch (Exception e)
			{
				Trace.WriteLine("Copying MoInflAffixTemplate.SlotsRS failed; probably ancient, bad data (TestLangProj has this): " + e.Message);
			}
			if (RegionOA != null)
			{
				template.RegionOA = Services.GetInstance<IFsFeatStrucFactory>().Create();
				RegionOA.SetCloneProperties(template.RegionOA);
			}
			if (StratumRA != null)
			{
				template.StratumRA = StratumRA;
			}
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	internal partial class MoInflAffixSlot
	{
		/// <summary>
		/// TODO (DamienD): register prop change when dependencies change
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "MoInflAffMsa")]
		public IEnumerable<IMoInflAffMsa> Affixes
		{
			get
			{
				((ICmObjectRepositoryInternal)Services.ObjectRepository).EnsureCompleteIncomingRefsFrom(MoInflAffMsaTags.kflidSlots);
				return (from msa in m_incomingRefs
						where msa.Source is IMoInflAffMsa && ((IMoInflAffMsa) msa.Source).SlotsRC.Contains(this)
						select (IMoInflAffMsa) msa.Source);
			}
		}

		/// <summary>
		/// Get a list of inflectional affix LexEntries which do not already refer to this slot
		/// </summary>
		public IEnumerable<ILexEntry> OtherInflectionalAffixLexEntries
		{
			get
			{
				return (from le in Services.GetInstance<ILexEntryRepository>().AllInstances()
						where le.MorphoSyntaxAnalysesOC.Any(msa => msa is IMoInflAffMsa)
							&& !le.MorphoSyntaxAnalysesOC.Any(
							msa => msa is IMoInflAffMsa && ((IMoInflAffMsa) msa).SlotsRC.Contains(this))
						select le);
			}
		}

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
				tisb.Append(String.Format(Strings.ksDeleteAffixSlot, " "));
				tisb.AppendTsString(ShortNameTSS);
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
			return flid == FDO.MoInflAffixSlotTags.kflidName;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return ShortNameTSSforWS(WritingSystemServices.kwsFirstAnalOrVern).Text;
			}
		}

		/// <summary>
		/// Get the Inflectional Affix Slot name for a given writing system
		/// </summary>
		/// <param name="wsAnal"></param>
		/// <returns></returns>
		public ITsString ShortNameTSSforWS(int wsAnal)
		{
			var tsf = Cache.TsStrFactory;
			var tisb = TsIncStrBldrClass.Create();

			if (Optional)
				tisb.AppendTsString(tsf.MakeString("(", m_cache.WritingSystemFactory.UserWs));

			ITsString tss = null;
			tss = WritingSystemServices.GetMagicStringAlt(Cache, wsAnal, Hvo, (int)MoInflAffixSlotTags.kflidName);
			tisb.AppendTsString(tss);

			if (Optional)
				tisb.AppendTsString(tsf.MakeString(")", m_cache.WritingSystemFactory.UserWs));

			return tisb.GetString();
		}
	}

	/// <summary></summary>
	internal partial class MoInflClass
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
				tisb.Append(String.Format(Strings.ksDeleteInflectionClass, " "));
				tisb.AppendTsString(ShortNameTSS);
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
			return (flid == FDO.MoInflClassTags.kflidName)
				   || (flid == FDO.MoInflClassTags.kflidAbbreviation);
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				var result = ShortNameTSS.Text;
				return String.IsNullOrEmpty(result) ? Strings.ksQuestions : result;
			}
		}

		/// <summary>
		/// Shortest reasonable name for the object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get { return Name.BestAnalysisVernacularAlternative; }
		}

		/// <summary>
		/// Gets the MoInflClass that owns this MoInflClass,
		/// or null, if it is owned by the list.
		/// </summary>
		public IMoInflClass OwningInflectionClass
		{
			get { return Owner as IMoInflClass; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class MoForm
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
		/// If the morph has a root type (root or bound root), change it to the corresponding stem type.
		/// </summary>
		public void ChangeRootToStem()
		{
			if (MorphTypeRA == Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot))
				MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			else if (MorphTypeRA == Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphBoundRoot))
				MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphBoundStem);

		}

		partial void MorphTypeRASideEffects(IMoMorphType oldObjValue, IMoMorphType newObjValue)
		{
			ClearMonomorphemicMorphData();
			if (Owner is LexEntry && Owner.IsValidObject)
			{
				var entry = ((LexEntry) Owner);
				// Now we have to figure out the old PrimaryMorphType of the entry.
				// It is determined by the first item in this list which HAD a morph type (if any)
				var morphs = entry.AlternateFormsOS.Reverse().ToList();
				if (entry.LexemeFormOA != null)
					morphs.Insert(0, entry.LexemeFormOA);
				IMoMorphType oldPrimaryType = null;
				foreach (var morph in morphs)
				{
					// If the morpheme is this, the one that is changing, then we consider
					// the old value that this is changing from, in determining the old PMT.
					var mt = (morph == this ? oldObjValue : morph.MorphTypeRA);
					if (mt != null)
					{
						oldPrimaryType = mt;
						break;
					}
				}
				if (oldPrimaryType != entry.PrimaryMorphType)
					entry.UpdateHomographs(oldPrimaryType);
			}
		}

		/// <summary>
		/// This method is overridden in MoStemAllomorph to clear a cache when certain things change that we detect
		/// in this class but are only significant for the subclass.
		/// </summary>
		internal virtual void ClearMonomorphemicMorphData()
		{
		}

		/// <summary>
		/// If this is not the LexemeForm, give it the same MorphType as the LF.
		/// </summary>
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			if (((ILexEntry)Owner).LexemeFormOA == null || ((ILexEntry)Owner).LexemeFormOA == this)
				return; // this IS the lexeme form, we can't usefully copy anything from it.

			//When adding an allomorph we want the morphtype to match that of the LexemeForm for the LexEntry
			m_MorphTypeRA = (Owner as LexEntry).LexemeFormOA.MorphTypeRA;
		}

		/// <summary>
		/// An MoForm is considered complete if it has a complete set of forms in all current vernacular writing systems.
		/// </summary>
		public bool IsComplete
		{
			get { return Cache.LangProject.CurrentVernacularWritingSystems.All(ws => Form.get_String(ws.Handle).Length > 0); }
		}

		/// <summary>
		/// A sort key method for sorting on the Lexeme field.
		/// Note: an earlier version included the homograph number, but this is confusing
		/// when it cannot be seen and prevents sorting by a second column from working
		/// as expected.
		/// </summary>
		public string MorphSortKey(bool sortedFromEnd, int ws)
		{
			string sKey = null;
			if (ws == 0)
				ws = m_cache.DefaultVernWs;  // for obsolete string finders.
			if (Form != null)
			{
				var tss = Form.get_String(ws);
				if (tss != null && tss.Length != 0)
					sKey = tss.Text;
			}
			if (sKey == null)
				sKey = "";

			if (sortedFromEnd)
				sKey = TsStringUtils.ReverseString(sKey);

			return SortKeyMorphType(sKey);
		}

		/// <summary>
		/// This adjusts the input key by adding a space and an 11 number to cause things
		/// otherwise equal to group by the morph type of the lexeme form.
		/// </summary>
		/// <param name="sKey"></param>
		/// <returns></returns>
		internal string SortKeyMorphType(string sKey)
		{
			var mmt = MorphTypeRA;
			int nKey2 = 0;
			if (mmt != null)
			{
				nKey2 = SortKey2;
			}

			if (nKey2 != 0)
				sKey = sKey + " " + SortKey2Alpha;
			return sKey;
		}

		/// <summary>
		/// Override to report changing Forms to the owning lex entry (if any).
		/// </summary>
		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
			if (multiAltFlid == MoFormTags.kflidForm && alternativeWs.Handle == Cache.DefaultVernWs && Owner is LexEntry)
			{
				((LexEntry)Owner).MoFormFormChanged(this, originalValue == null ? "" : originalValue.Text);
			}
		}

		/// <summary>
		/// Set the form property, but remove any reserved markers first.
		/// </summary>
		public ITsString FormMinusReservedMarkers
		{
			//review (JH): I find this setter confusing.  THe prop name suggests that the form is already
			//stripped, but this goes and does the stripping.  "FormWithMarkers" would be more clear,
			//I think.
			set
			{
				var wsVern = TsStringUtils.GetWsAtOffset(value, 0);
				Form.set_String(wsVern, MorphServices.EnsureNoMarkers(value.Text, m_cache));
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an allomorph.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="msa">The msa.</param>
		/// <param name="tssform">The tssform.</param>
		/// <param name="morphType">Type of the morph.</param>
		/// <param name="fLexemeForm">set to <c>true</c> to create a lexeme form.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static IMoForm CreateAllomorph(ILexEntry entry, IMoMorphSynAnalysis msa,
											  ITsString tssform, IMoMorphType morphType, bool fLexemeForm)
		{
			MoForm allomorph = null;
			switch (morphType.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphProclitic: // Fall through.
				case MoMorphTypeTags.kMorphClitic: // Fall through.
				case MoMorphTypeTags.kMorphEnclitic:
					Debug.Assert(msa is MoStemMsa, "Wrong MSA for a clitic.");
#pragma warning disable 168
					var stemMsa = (IMoStemMsa)msa;
#pragma warning restore 168
					goto case MoMorphTypeTags.kMorphBoundStem;
				case MoMorphTypeTags.kMorphRoot: // Fall through.
				case MoMorphTypeTags.kMorphBoundRoot: // Fall through.
				case MoMorphTypeTags.kMorphStem: // Fall through.
				case MoMorphTypeTags.kMorphParticle: // Fall through.
				case MoMorphTypeTags.kMorphPhrase: // Fall through.
				case MoMorphTypeTags.kMorphDiscontiguousPhrase: // Fall through.
					// AndyB_Yahoo: On particles, (and LT-485), these are always to be
					// roots that never take any affixes
					// AndyB_Yahoo: Therefore, they need to have StemMsas and Stem
					// allomorphs
				case MoMorphTypeTags.kMorphBoundStem:
					allomorph = new MoStemAllomorph();
					break;
				default:
					// All others, which should get an non-stem MSA and an affix allo.
					Debug.Assert(!(msa is IMoStemMsa), "Wrong MSA for a affix.");
					allomorph = new MoAffixAllomorph();
					break;
			}
			if (fLexemeForm)
				entry.LexemeFormOA = allomorph;
			else
				entry.AlternateFormsOS.Add(allomorph);
			allomorph.MorphTypeRA = morphType; // Has to be done before the next call.
			ITsString tssAllomorphForm;
			var maxLength = entry.Cache.MaxFieldLength(FDO.MoFormTags.kflidForm);
			if (tssform.Length > maxLength)
			{
				var sMessage = String.Format(Strings.ksTruncatedXXXToYYYChars,
											 fLexemeForm ? Strings.ksLexemeForm : Strings.ksAllomorph, maxLength);
				MessageBoxUtils.Show(sMessage, Strings.ksWarning,
													 System.Windows.Forms.MessageBoxButtons.OK,
													 System.Windows.Forms.MessageBoxIcon.Warning);
				tssAllomorphForm = tssform.GetSubstring(0, maxLength);
			}
			else
			{
				tssAllomorphForm = tssform;
			}

			allomorph.FormMinusReservedMarkers = tssAllomorphForm;
			if ((morphType.Guid == MoMorphTypeTags.kguidMorphInfix) ||
				(morphType.Guid == MoMorphTypeTags.kguidMorphInfixingInterfix))
			{
				HandleInfix(entry, allomorph);
			}
			return allomorph;
		}

		private static void HandleInfix(ILexEntry entry, IMoForm allomorph)
		{
			const string sDefaultPostionEnvironment = "/#[C]_";
			var infix = allomorph as IMoAffixAllomorph;
			if (infix == null)
				return; // something's wrong...
			var cache = entry.Cache;
			var env = PhEnvironment.DefaultInfixEnvironment(cache, sDefaultPostionEnvironment);
			if (env != null)
			{
				infix.PositionRS.Add(env);
			}
			else
			{
				// create default infix position environment
				var strFact = cache.TsStrFactory;
				var defAnalWs = entry.Services.WritingSystems.DefaultAnalysisWritingSystem;
				var ppd = cache.LangProject.PhonologicalDataOA;
				env = new PhEnvironment();
				ppd.EnvironmentsOS.Add(env);
				env.StringRepresentation = strFact.MakeString(sDefaultPostionEnvironment, cache.DefaultVernWs);
				env.Description.set_String(defAnalWs.Handle, strFact.MakeString("Default infix position environment", defAnalWs.Handle));
				env.Name.set_String(defAnalWs.Handle, strFact.MakeString("After stem-initial consonant", defAnalWs.Handle));
				infix.PositionRS.Add(env);
			}
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == FDO.MoFormTags.kflidForm)
				   || (flid == FDO.MoFormTags.kflidMorphType);
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
				var vernWs = m_cache.DefaultVernWs;
				var userWs = m_cache.DefaultUserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, vernWs);
				tisb.AppendTsString(ShortNameTSS);

				var countedObjectIDs = new List<int>();
				var alloAHPCount = 0;
				var analCount = 0;
				var servLoc = Cache.ServiceLocator;
				var maps = servLoc.GetInstance<IMoAlloAdhocProhibRepository>().AllInstances();
				foreach (var map in maps.Where(m => m.FirstAllomorphRA == this))
				{
					if (countedObjectIDs.Contains(map.Hvo)) continue;

					countedObjectIDs.Add(map.Hvo);
					++alloAHPCount;
				}
				foreach (var map in maps.Where(m => m.RestOfAllosRS.Contains(this)))
				{
					if (countedObjectIDs.Contains(map.Hvo)) continue;

					countedObjectIDs.Add(map.Hvo);
					++alloAHPCount;
				}
				foreach (var map in maps.Where(m => m.AllomorphsRS.Contains(this)))
				{
					if (countedObjectIDs.Contains(map.Hvo)) continue;

					countedObjectIDs.Add(map.Hvo);
					++alloAHPCount;
				}
				var bundles = servLoc.GetInstance<IWfiMorphBundleRepository>().AllInstances();
				foreach (var mb in bundles.Where(b => b.MorphRA == this))
				{
					if (countedObjectIDs.Contains(mb.Owner.Hvo)) continue;

					countedObjectIDs.Add(mb.Owner.Hvo);
					++analCount;
				}

				var cnt = 1;
				var warningMsg = String.Format("{0}{0}{1}{0}{2}", StringUtils.kChHardLB,
					Strings.ksAlloUsedHere, Strings.ksDelAlloDelThese);
				var wantMainWarningLine = true;
				if (analCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (analCount > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesByAnalyses, cnt++, analCount));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceByAnalyses, cnt++));
					wantMainWarningLine = false;
				}
				if (alloAHPCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append(StringUtils.kChHardLB.ToString());
					if (alloAHPCount > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesByAlloAdhoc, cnt++, alloAHPCount, StringUtils.kChHardLB));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceByAlloAdhoc, cnt++, StringUtils.kChHardLB));
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Set the form property, but remove any reserved markers first.
		/// </summary>
		public ITsString FormWithMarkers
		{
			set
			{
				var wsVern = TsStringUtils.GetWsAtOffset(value, 0);
				Form.set_String(wsVern,
								m_cache.TsStrFactory.MakeString(MorphServices.EnsureNoMarkers(value.Text, m_cache), wsVern));
			}
		}

		/// <summary>
		/// Swap all references to this MoForm to use the new one
		/// </summary>
		/// <param name="newForm">the hvo of the new MoForm</param>
		public void SwapReferences(IMoForm newForm)
		{
			foreach (var adhocProhib in Services.GetInstance<IMoAlloAdhocProhibRepository>().AllInstances())
			{
				if (adhocProhib.FirstAllomorphRA == this)
					adhocProhib.FirstAllomorphRA = newForm;

				for (int i = 0; i < adhocProhib.RestOfAllosRS.Count; i++)
				{
					if (adhocProhib.RestOfAllosRS[i] == this)
						adhocProhib.RestOfAllosRS.Replace(i, 1, new IMoForm[] { newForm });
				}
			}

			foreach (var mb in Services.GetInstance<IWfiMorphBundleRepository>().AllInstances())
			{
				if (mb.MorphRA == this)
					mb.MorphRA = newForm;
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
			get
			{
				var form = Form.VernacularDefaultWritingSystem;
				if (form == null || string.IsNullOrEmpty(form.Text))
					return Strings.ksQuestions;		// was "??", not "???"

				return form.Text;
			}
		}

		/// <summary>
		/// Get a display name suitable for use in displaying ad hoc rules.
		/// </summary>
		/// <remarks>
		/// The returned string will be in this form:
		/// (prfefixMarker) + form + (suffixMarker) + " (glossInfo): " + citationForm
		/// TODO (DamienD): register prop change when dependencies change
		/// </remarks>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString LongNameTSS
		{
			get
			{
				var tisb = TsIncStrBldrClass.Create();
				string pre = null;
				string post = null;
				if (MorphTypeRA != null)
				{
					pre = MorphTypeRA.Prefix;
					post = MorphTypeRA.Postfix;
				}
				// Add prefix marker, if any.
				if (!string.IsNullOrEmpty(pre))
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
						m_cache.DefaultAnalWs);
					tisb.Append(pre);
				}
				// Add Best Vern of the form.
				tisb.AppendTsString(Form.BestVernacularAlternative);

				// Add suffix marker, if any.
				if (!string.IsNullOrEmpty(post))
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
						m_cache.DefaultAnalWs);
					tisb.Append(post);
				}
				if (Owner is ILexEntry)
				{
					var le = Owner as ILexEntry;

					if (le.SensesOS.Count > 0)
					{
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
							m_cache.DefaultAnalWs);
						tisb.Append(" (");
						// Add gloss info.
						// This is the best Anal WS from the gloss of the first sense.
						var ls = le.SensesOS[0];
						tisb.AppendTsString(ls.Gloss.BestAnalysisAlternative);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
							m_cache.DefaultAnalWs);
						tisb.Append("):");
					}

					tisb.AppendTsString(le.HeadWord);
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get a display name suitable for use in displaying ad hoc rules.
		/// </summary>
		/// <remarks>
		/// The returned string will be in this form:
		/// (prfefixMarker) + form + (suffixMarker) + " (glossInfo): " + citationForm
		/// </remarks>
		public string LongName
		{
			get
			{
				return LongNameTSS.Text;
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
				var tss = Form.VernacularDefaultWritingSystem;
				if (tss != null || tss.Length > 0)
					return tss;

				return Cache.TsStrFactory.MakeString(
					Strings.ksQuestions,
					Cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// Gets a set of all hvos of all valid morph type references for this MoForm
		/// </summary>
		/// <returns>A set of hvos.</returns>
		public IEnumerable<ICmObject> GetAllMorphTypeReferenceTargetCandidates()
		{
			var set = new HashSet<ICmObject>(m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.Cast<ICmObject>());
			if (OwningFlid == LexEntryTags.kflidAlternateForms)
				set.Remove(Services.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphCircumfix)); // only for lexemeform
			return set;
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
				LiftResidue = sResidue;
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
		/// Get the ref attribute value stored in LiftResidue (if it exists)
		/// </summary>
		public string LiftRefAttribute
		{
			get { return LexEntry.ExtractAttributeFromLiftResidue(LiftResidue, "ref"); }
		}

		/// <summary>
		/// Return a marked form in the desired writing system.
		/// </summary>
		public string GetFormWithMarkers(int ws)
		{
			string form = Form.get_String(ws).Text;
			if (String.IsNullOrEmpty(form))
				return null;
			else
				return PrecedingSymbol + form + FollowingSymbol;
		}

		/// <summary>
		///
		/// </summary>
		protected string PrecedingSymbol
		{
			get
			{
				if (this.MorphTypeRA != null && this.MorphTypeRA.Prefix != null)
					return this.MorphTypeRA.Prefix;
				else
					return String.Empty;
			}
		}

		/// <summary>
		/// The Symbol
		/// </summary>
		protected string FollowingSymbol
		{
			get
			{
				if (this.MorphTypeRA != null && this.MorphTypeRA.Postfix != null)
					return this.MorphTypeRA.Postfix;
				else
					return String.Empty;
			}
		}

		/// <summary>
		/// A secondary sort key for sorting a list of ShortNames.
		/// Defaults to zero, but gives SecondaryOrder shifted left 10
		/// </summary>
		public override int SortKey2
		{
			get
			{
				int nkey = 0;
				var mmt = MorphTypeRA;
				if (mmt != null)
					nkey = mmt.SecondaryOrder * 1024;
				return nkey;
			}
		}
	}

	internal partial class MoStemAllomorph
	{
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
				case MoFormTags.kflidMorphType:
					return from morphType in m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.Cast<IMoMorphType>()
						   where morphType.IsStemType
						   select morphType as ICmObject;

				case MoStemAllomorphTags.kflidPhoneEnv:
					return Services.GetInstance<IPhEnvironmentRepository>().AllValidInstances().Cast<ICmObject>();

				case MoStemAllomorphTags.kflidStemName:
					var stemNames = new HashSet<ICmObject>();
					if (Owner.ClassID == LexEntryTags.kClassId)
					{
						ILexEntry entry = Owner as ILexEntry;
						foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
						{
							if (msa.ClassID == MoStemMsaTags.kClassId)
							{
								IMoStemMsa infstemmsa = msa as IMoStemMsa;
								IPartOfSpeech pos = infstemmsa.PartOfSpeechRA;
								if (pos != null)
									stemNames.UnionWith(pos.AllStemNames.Cast<ICmObject>());
							}
						}
					}
					return stemNames;

				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		/// <summary>
		/// Set the type of the allomorph to root (a reasonable default).
		/// This method is invoked when creating a real lexeme form from a ghost.
		/// It is called by reflection. Caller is responsible to call within a UOW.
		/// </summary>
		public void SetMorphTypeToRoot()
		{
			MorphTypeRA = Services.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot);
		}
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case MoFormTags.kflidMorphType:
					return m_cache.LangProject.LexDbOA.MorphTypesOA;
				case MoStemAllomorphTags.kflidPhoneEnv:
					return m_cache.LangProject.PhonologicalDataOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		public override bool IsFieldRelevant(int flid)
		{
			if (flid == MoStemAllomorphTags.kflidStemName)
			{
				if (MorphTypeRA == null)
					return false;
				Guid guid = MorphTypeRA.Guid;
				if ((guid == MoMorphTypeTags.kguidMorphBoundRoot)
					|| (guid == MoMorphTypeTags.kguidMorphBoundStem)
					|| (guid == MoMorphTypeTags.kguidMorphPhrase) // LT-7334
					|| (guid == MoMorphTypeTags.kguidMorphRoot)
					|| (guid == MoMorphTypeTags.kguidMorphStem))
					return true;
				else
					return false;
			}
			return base.IsFieldRelevant(flid);
		}

		protected override void OnBeforeObjectDeleted()
		{
			ClearMonomorphemicMorphData();
			base.OnBeforeObjectDeleted();
		}

		/// <summary>
		/// Override to clear the morpheme data cache if the form changes.
		/// This is also adequate to make sure we get added on creation, since there is nothing to add until this property is set.
		/// </summary>
		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
			if (multiAltFlid == MoFormTags.kflidForm)
				ClearMonomorphemicMorphData();
		}

		internal override void ClearMonomorphemicMorphData()
		{
			((Infrastructure.Impl.MoStemAllomorphRepository) Services.GetInstance<IMoStemAllomorphRepository>()).ClearMonomorphemicMorphData();
		}
	}

	internal partial class MoAffixForm
	{
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case MoFormTags.kflidMorphType:
					return m_cache.LangProject.LexDbOA.MorphTypesOA;
				case MoAffixFormTags.kflidInflectionClasses:
					return m_cache.LangProject.PartsOfSpeechOA;
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
				case MoFormTags.kflidMorphType:
					return from morphType in m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.Cast<IMoMorphType>()
						   where morphType.IsAffixType
						   select morphType as ICmObject;

				case MoAffixFormTags.kflidInflectionClasses:
					var classes = new HashSet<ICmObject>();
					var entry = Owner as ILexEntry;
					if (entry != null)
					{
						foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
						{
							if (msa is IMoInflAffMsa)
							{
								var infafxmsa = msa as IMoInflAffMsa;
								var pos = infafxmsa.PartOfSpeechRA;
								if (pos != null)
									classes.UnionWith(pos.AllInflectionClasses.Cast<ICmObject>());
							}
							// Review: is this correct?  I think the TO POS is the relevant
							// one for derivational affixes, but maybe nothing is.
							// HAB says: From is the correct one to use for the allomorphs.  The From indicates
							// the category to which the affix attaches.  This category is the one that
							// may have the inflection classes, one or more of which this allomorph may go with.
							else if (msa is IMoDerivAffMsa)
							{
								var drvafxmsa = msa as IMoDerivAffMsa;
								var pos = drvafxmsa.FromPartOfSpeechRA;
								if (pos != null)
									classes.UnionWith(pos.AllInflectionClasses.Cast<ICmObject>());
							}
						}
					}
					return classes;

				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public override bool IsFieldRelevant(int flid)
		{
			if (flid != MoAffixFormTags.kflidInflectionClasses)
				return true;

			// MoAffixForm.inflection classes are only relevant if the MSAs of the
			// entry include an inflectional affix MSA.

			ILexEntry entry = Owner as ILexEntry;
			return entry.SupportsInflectionClasses() && base.IsFieldRelevant(flid);
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class MoAffixAllomorph
	{
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			if (flid == (int)MoAffixAllomorphTags.kflidPosition)
			{
				if (MorphTypeIsInfix())
					return true;
			}
			return base.IsFieldRequired(flid);
		}

		private bool MorphTypeIsInfix()
		{
			if ((MorphTypeRA != null) &&
				((MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphInfix) ||
				 (MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphInfixingInterfix)))
				return true;

			return false;
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case MoAffixAllomorphTags.kflidPosition:
				case MoAffixAllomorphTags.kflidPhoneEnv:
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
				case MoAffixAllomorphTags.kflidPosition:
				case MoAffixAllomorphTags.kflidPhoneEnv:
					return Services.GetInstance<IPhEnvironmentRepository>().AllValidInstances().Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}

		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>
		/// Position is only relevant when the MorphType is infix.
		/// </remarks>
		/// <returns></returns>
		public override bool IsFieldRelevant(int flid)
		{
			if (flid == MoAffixAllomorphTags.kflidPosition)
			{
				if (!MorphTypeIsInfix())
					return false;
			}
			return base.IsFieldRelevant(flid);
		}
	}

	internal partial class MoAffixProcess
	{
		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);
			if (e.Flid == MoAffixProcessTags.kflidInput && e.ForDeletion)
			{
				var ctxtOrVar = e.ObjectRemoved as IPhContextOrVar;
				foreach (var mapping in OutputOS.ToArray())
				{
					switch (mapping.ClassID)
					{
						case MoCopyFromInputTags.kClassId:
							var copy = mapping as IMoCopyFromInput;
							if (copy.ContentRA == ctxtOrVar)
								OutputOS.Remove(copy);
							break;

						case MoModifyFromInputTags.kClassId:
							var modify = mapping as IMoModifyFromInput;
							if (modify.ContentRA == ctxtOrVar)
								OutputOS.Remove(modify);
							break;
					}
				}
			}
		}
		/// <summary>
		/// This is needed at least for the Affix Process slice to work properly. See FWR-1619.
		/// </summary>
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			IPhVariable var = new PhVariable();
			InputOS.Add(var);

			IMoCopyFromInput copy = new MoCopyFromInput();
			OutputOS.Add(copy);
			copy.ContentRA = var;

			IsAbstract = true;
		}
	}

	internal partial class MoMorphData
	{
		/// <summary>
		/// Gets or sets the active parser.
		/// </summary>
		/// <value>The active parser.</value>
		public string ActiveParser
		{
			get
			{
				try
				{
					XmlDocument doc = new XmlDocument();
					doc.LoadXml(ParserParameters);
					XmlNode parserNode = doc.SelectSingleNode("/ParserParameters/ActiveParser");
					if (parserNode != null)
						return parserNode.InnerText;
				}
				catch
				{
					// Eat exception.
				}
				return "XAmple";
			}

			set
			{
				XmlDocument doc = new XmlDocument();
				XmlNode paramsNode = null;
				try
				{
					doc.LoadXml(ParserParameters);
					paramsNode = doc.SelectSingleNode("/ParserParameters");
				}
				catch
				{
					// Eat exception.
				}

				if (paramsNode == null)
				{
					paramsNode = doc.CreateElement("ParserParameters");
					doc.DocumentElement.AppendChild(paramsNode);
				}

				XmlNode parserNode = paramsNode.SelectSingleNode("ActiveParser");
				if (parserNode == null)
				{
					parserNode = doc.CreateElement("ActiveParser");
					paramsNode.AppendChild(parserNode);
				}
				parserNode.InnerText = value;
				ParserParameters = doc.OuterXml;
			}
		}
	}

	///<summary>
	///
	///</summary>
	internal partial class MoMorphAdhocProhib
	{
		/// <summary>
		/// Override the inherited method to check that there are at least two allomorphs referred to
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="createAnnotation">if set to <c>true</c>, an annotation will be created.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>
		/// true, if there are at least two morphemes, otherwise false.
		/// </returns>
		public override bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			// Use backrefs for this.
			//CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_cache, Hvo);
			if (createAnnotation)
			{
				var agt = Cache.LanguageProject.ConstraintCheckerAgent;
				var anns = Cache.LanguageProject.AnnotationsOC;
				var errReports =
					Cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>().AllInstances().Where(
						error => error.BeginObjectRA == this);
				foreach (var errReport in errReports)
				{
					if (errReport.SourceRA == agt)
						anns.Remove(errReport);
				}
			}

			failure = null;
			var isValid = (FirstMorphemeRA != null) && (RestOfMorphsRS.Count >= 1);
			if (!isValid)
			{
				failure = new ConstraintFailure(this,
												MoMorphAdhocProhibTags.kflidMorphemes,
												Strings.ksMorphConstraintFailure, createAnnotation);
				return false;

				//				CmBaseAnnotation ann = (CmBaseAnnotation)m_cache.LangProject.AnnotationsOC.Add(new CmBaseAnnotation());
				//				ann.CompDetails = "Need to have at least two morphemes chosen";
				//				ann.InstanceOfRAHvo = Hvo;
				//				ann.BeginObjectRA = this;
				//				ann.Flid = (int)BaseMoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidMorphemes;
				// REVIEW JohnH(AndyB): Does this need an agent or type?
				// Answer by RandyR: The removed errors (above) expect to find
				// the Cache.LanguageProject.ConstraintCheckerAgent CmAgent in the anns.
				// ann.SourceRAHvo = m_agentId;
			}
			return true;
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return ((flid == (int)FDO.MoMorphAdhocProhibTags.kflidFirstMorpheme)
					|| (flid == (int)FDO.MoMorphAdhocProhibTags.kflidRestOfMorphs)
				   );
		}

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get
			{
				var sb = new StringBuilder();
				foreach (var msa in RestOfMorphsRS)
				{
					//sb.Append(msa.ShortName);  short name is too short for some msas (just a category, e.g.)
					sb.Append(msa.LongName);
					sb.Append(" ");
				}
				sb.Append("/ ");
				if (FirstMorphemeRA != null)
					sb.Append(FirstMorphemeRA.LongName);
				return sb.ToString();
			}
		}

		partial void FirstMorphemeRASideEffects(IMoMorphSynAnalysis oldObjValue, IMoMorphSynAnalysis newObjValue)
		{
			if (oldObjValue == newObjValue || (oldObjValue == null || !oldObjValue.CanDelete))
				return; // Nothing to do.

			// Wipe out the old MSA.
			((ILexEntry)oldObjValue.Owner).MorphoSyntaxAnalysesOC.Remove(oldObjValue);
		}

		/// <summary>
		/// Need this version, because the ICmObjectInternal.RemoveObjectSideEffects version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			switch (e.Flid)
			{
				default:
					return;
				case FDO.MoMorphAdhocProhibTags.kflidRestOfMorphs: // Fall through.
				case FDO.MoMorphAdhocProhibTags.kflidMorphemes:
					break;
			}

			var gonerMsa = (IMoMorphSynAnalysis)e.ObjectRemoved;
			if (gonerMsa.CanDelete)
				((ILexEntry)gonerMsa.Owner).MorphoSyntaxAnalysesOC.Remove(gonerMsa);
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
				case MoMorphAdhocProhibTags.kflidRestOfMorphs:
				case MoMorphAdhocProhibTags.kflidFirstMorpheme:
					// An earlier version tried to restrict to ones owned by LexEntries, but that is the only possible owner.
					return Services.GetInstance<IMoMorphSynAnalysisRepository>().AllInstances().Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
			}
		}
	}

	/// <summary>
	/// </summary>
	internal partial class MoEndoCompound
	{
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			IMoStemMsaFactory factMsa = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			this.OverridingMsaOA = factMsa.Create();
		}
	}

	/// <summary>
	/// </summary>
	internal partial class MoExoCompound
	{
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoExoCompoundTags.kflidToMsa)
				   || base.IsFieldRequired(flid);
		}

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			IMoStemMsaFactory factMsa = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			this.ToMsaOA = factMsa.Create();
		}
	}

	///<summary>
	///
	///</summary>
	internal partial class MoAlloAdhocProhib
	{
		/// <summary>
		/// Override the inherited method to check that there are at least two allomorphs referred to
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="createAnnotation">if set to <c>true</c>, an annotation will be created.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>
		/// true, if there are at least two allomorphs, otherwise false.
		/// </returns>
		public override bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			// Use back refs to find them.
			//CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_cache, Hvo);
			if (createAnnotation)
			{
				var agt = Cache.LanguageProject.ConstraintCheckerAgent;
				var anns = Cache.LanguageProject.AnnotationsOC;
				var errReports =
					Cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>().AllInstances().Where(
						error => error.BeginObjectRA == this);
				foreach (var errReport in errReports)
				{
					if (errReport.SourceRA == agt)
						anns.Remove(errReport);
				}
			}

			var isValid = (FirstAllomorphRA != null) && (RestOfAllosRS.Count >= 1);
			if (!isValid)
			{
				failure = new ConstraintFailure(this, MoAlloAdhocProhibTags.kflidAllomorphs,
												Strings.ksAlloConstraintFailure, createAnnotation);
				//
				//				CmBaseAnnotation ann = (CmBaseAnnotation)m_cache.LangProject.AnnotationsOC.Add(new CmBaseAnnotation());
				//				ann.CompDetails = "Need to have at least two allomorphs selected";
				//				ann.InstanceOfRAHvo = Hvo;
				//				ann.BeginObjectRA = this;
				//				ann.Flid = (int)BaseMoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidAllomorphs;
				//				obj = ann;
				// REVIEW JohnH(AndyB): Does this need an agent or type?
				// Answer by RandyR: The removed errors (above) expect to find
				// the Cache.LanguageProject.ConstraintCheckerAgent CmAgent in the anns.
				// ann.SourceRAHvo = m_agentId;

				return false;
			}

			failure = null;
			return true;
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return ((flid == (int)MoAlloAdhocProhibTags.kflidFirstAllomorph)
					|| (flid == (int)MoAlloAdhocProhibTags.kflidRestOfAllos)
				   );
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
				case MoAlloAdhocProhibTags.kflidRestOfAllos:
				case MoAlloAdhocProhibTags.kflidFirstAllomorph:
					// An earlier version checked for forms owned by lex entries in LexemeForm or Allomorphs, but those
					// are the only two possible owning properties for MoForm.
					return (from form in Services.GetInstance<IMoFormRepository>().AllInstances()
							where !form.IsAbstract
							select form).Cast<ICmObject>();
				default:
					return base.ReferenceTargetCandidates(flid);
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
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get
			{
				var sb = new StringBuilder();
				foreach (MoForm mf in RestOfAllosRS)
				{
					sb.Append(mf.ShortName);
					sb.Append(" ");
				}
				sb.Append("/ ");
				if (FirstAllomorphRA != null)
					sb.Append(FirstAllomorphRA.ShortName);
				return sb.ToString();
			}
		}
	}

	/// <summary>
	/// </summary>
	internal partial class MoBinaryCompoundRule
	{
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoBinaryCompoundRuleTags.kflidLeftMsa)
				   || (flid == (int)MoBinaryCompoundRuleTags.kflidRightMsa)
				   || base.IsFieldRequired(flid);
		}

		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			IMoStemMsaFactory factMsa = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			this.LeftMsaOA = factMsa.Create();
			this.RightMsaOA = factMsa.Create();
		}
	}

	/// <summary>
	///
	/// </summary>
	internal partial class MoAdhocProhibGr
	{
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoAdhocProhibGrTags.kflidName)
				   || (flid == (int)MoAdhocProhibGrTags.kflidMembers);
		}
	}

	/// <summary>
	/// Add to auto-generated class.
	/// </summary>
	internal partial class MoMorphType : IComparable
	{
		/// <summary>
		/// Gets a value indicating whether this instance is a stem type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a stem type; otherwise, <c>false</c>.
		/// </value>
		public bool IsStemType
		{
			get
			{
				return !IsAffixType;
			}
		}

		/// <summary>
		/// Indicates if this instance is a bound type
		/// <value>
		/// true if this instance is a bound stem or root; otherwise false
		/// </value>
		/// </summary>
		public bool IsBoundType
		{
			get
			{
				switch (Guid.ToString())
				{
					case MoMorphTypeTags.kMorphBoundRoot:
					case MoMorphTypeTags.kMorphBoundStem:
						return true;
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is an affix type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is an affix type; otherwise, <c>false</c>.
		/// </value>
		public bool IsAffixType
		{
			get
			{
				switch (Guid.ToString())
				{
					case MoMorphTypeTags.kMorphCircumfix:
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphPrefix:
					case MoMorphTypeTags.kMorphSimulfix:
					case MoMorphTypeTags.kMorphSuffix:
					case MoMorphTypeTags.kMorphSuprafix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
					case MoMorphTypeTags.kMorphPrefixingInterfix:
					case MoMorphTypeTags.kMorphSuffixingInterfix:
						return true;
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a prefix type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a prefix type; otherwise, <c>false</c>.
		/// </value>
		public bool IsPrefixishType
		{
			get
			{
				switch (Guid.ToString())
				{
					case MoMorphTypeTags.kMorphCircumfix:
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphPrefix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
					case MoMorphTypeTags.kMorphPrefixingInterfix:
						return true;
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a suffix type.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a suffix type; otherwise, <c>false</c>.
		/// </value>
		public bool IsSuffixishType
		{
			get
			{
				switch (Guid.ToString())
				{
					case MoMorphTypeTags.kMorphCircumfix:
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphSuffix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
					case MoMorphTypeTags.kMorphSuffixingInterfix:
						return true;
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Checks two morph types objects to see if they are ambiguous,
		/// regarding the markers used to type them.
		/// </summary>
		/// <param name="other">morph type to compare with</param>
		/// <returns>True, if the two morph types are ambiguous, otherwise false.</returns>
		public bool IsAmbiguousWith(IMoMorphType other)
		{
			Debug.Assert(other != null);
			var areAmbiguous = false;

			switch (Guid.ToString())
			{
				case MoMorphTypeTags.kMorphCircumfix:
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphClitic:
				case MoMorphTypeTags.kMorphParticle:
				case MoMorphTypeTags.kMorphStem:
					if (Guid != other.Guid)
					{
						areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphCircumfix)
									   || (other.Guid == MoMorphTypeTags.kguidMorphRoot)
									   || (other.Guid == MoMorphTypeTags.kguidMorphClitic)
									   || (other.Guid == MoMorphTypeTags.kguidMorphParticle)
									   || (other.Guid == MoMorphTypeTags.kguidMorphStem);
					}
					break;
				case MoMorphTypeTags.kMorphPhrase:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase);
					break;
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphPhrase);
					break;
				case MoMorphTypeTags.kMorphBoundStem:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphBoundRoot);
					break;
				case MoMorphTypeTags.kMorphBoundRoot:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphBoundStem);
					break;
				case MoMorphTypeTags.kMorphInfix:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphInfixingInterfix);
					break;
				case MoMorphTypeTags.kMorphInfixingInterfix:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphInfix);
					break;
				case MoMorphTypeTags.kMorphPrefix:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphPrefixingInterfix);
					break;
				case MoMorphTypeTags.kMorphPrefixingInterfix:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphPrefix);
					break;
				case MoMorphTypeTags.kMorphSuffix:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphSuffixingInterfix);
					break;
				case MoMorphTypeTags.kMorphSuffixingInterfix:
					areAmbiguous = (other.Guid == MoMorphTypeTags.kguidMorphSuffix);
					break;
				default:
					break;
			}

			return areAmbiguous;
		}

		/// <summary>
		/// The sort key for sorting a list of ShortNames.
		/// </summary>
		public override string SortKey
		{
			get
			{
				return Name.BestAnalysisAlternative.Text;
			}
		}

		/// <summary>
		/// The 'name' property with reserved markers, if any.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return FormWithMarkers(Name.BestAnalysisAlternative.Text);
			}
		}

		/// <summary>
		/// Get a form with prefix and suffix markers, if any, for the given form.
		/// </summary>
		/// <param name="form">A string with prefix and suffix markers, if any.</param>
		/// <returns></returns>
		public string FormWithMarkers(string form)
		{
			var pfx = string.IsNullOrEmpty(Prefix) ? "" : Prefix;
			var pstfx = string.IsNullOrEmpty(Postfix) ? "" : Postfix;
			return pfx + MorphServices.StripAffixMarkers(Cache, form) + pstfx;
		}

		/// <summary>
		/// Return the number of unique LexEntries that reference this MoMorphType via MoForm.
		/// </summary>
		/// <remarks>This is rather slow to be a property!</remarks>
		public int NumberOfLexEntries
		{
			get
			{
				//old system used this logic:
				//// Gather up complete set of ids.
				//// Remove any entry ids that are circumfixes.
				//string query = String.Format("SELECT DISTINCT Owner$" +
				//    " FROM MoForm_" +
				//    " WHERE MorphType = {0}", Hvo.ToString());
				//List<int> entries = DbOps.ReadIntsFromCommand(m_cache, query, null);
				//if (Guid.ToString().ToLower() == kguidMorphPrefix.ToLower()
				//    || Guid.ToString().ToLower() == kguidMorphInfix.ToLower()
				//    || Guid.ToString().ToLower() == kguidMorphSuffix.ToLower())
				//{
				//    query = String.Format("SELECT DISTINCT mf.Owner$" +
				//        " FROM MoForm_ mf" +
				//        " JOIN MoMorphType_ mmt ON mf.MorphType = mmt.Id" +
				//        " WHERE mf.OwnFlid$ = {0} AND mmt.Guid$ = '{1}'", (int)LexEntry.LexEntryTags.kflidLexemeForm, kguidMorphCircumfix);
				//    foreach (int id in DbOps.ReadIntsFromCommand(m_cache, query, null))
				//        entries.Remove(id);
				//}
				//return entries.Count;
				int cLex = 0;
				bool fCheckCircumfix = this.Guid == MoMorphTypeTags.kguidMorphPrefix ||
					this.Guid == MoMorphTypeTags.kguidMorphInfix ||
					this.Guid == MoMorphTypeTags.kguidMorphSuffix;
				ILexEntryRepository repoLex = Services.GetInstance<ILexEntryRepository>();
				foreach (ILexEntry le in repoLex.AllInstances())
				{
					bool fAdd = false;
					bool fOk = true;
					foreach (IMoForm mf in le.AllAllomorphs)
					{
						if (mf.MorphTypeRA == this)
						{
							fAdd = true;
							continue;
						}
						if (fCheckCircumfix &&
							mf.MorphTypeRA != null &&
							mf.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphCircumfix)
						{
							fOk = false;
							break;
						}
					}
					if (fAdd && fOk)
						++cLex;
				}
				return cLex;
			}
		}

		#region IComparable Members
		/// <summary>
		/// Allow MoMorphType objects to be compared/sorted.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		int IComparable.CompareTo(object obj)
		{
			MoMorphType that = obj as MoMorphType;
			if (that == null)
				return 1;
			var s1 = SortKey;
			var s2 = that.SortKey;
			if (s1 == null)
				return (s2 == null) ? 0 : 1;
			if (s2 == null)
				return -1;
			var x = s1.CompareTo(s2);
			return x == 0 ? SortKey2 - that.SortKey2 : x;
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	internal partial class MoStemName
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var userWs = m_cache.WritingSystemFactory.UserWs;
				var tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteStemName));
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Create other required elements (one feature structure in regions).
		/// </summary>
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			RegionsOC.Add(new FsFeatStruc());
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				var result = ShortNameTSS.Text;
				return String.IsNullOrEmpty(result) ? Strings.ksQuestions : result;
			}
		}

		/// <summary>
		/// Shortest reasonable name for the object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get { return Name.BestAnalysisVernacularAlternative; }
		}

	}
}
