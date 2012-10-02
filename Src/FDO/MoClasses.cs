// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MoClasses.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// This file holds the overrides of the morphology-related generated classes for the Ling module.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls; // for ProgressState


namespace SIL.FieldWorks.FDO.Ling
{
	/// <summary>
	///
	/// </summary>
	public partial class MoAdhocProhibGr
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoAdhocProhibGr.MoAdhocProhibGrTags.kflidName)
				   || (flid == (int)MoAdhocProhibGr.MoAdhocProhibGrTags.kflidMembers);
		}
	}
}

namespace SIL.FieldWorks.FDO.Ling
{
	/// <summary>
	/// </summary>
	public partial class MoBinaryCompoundRule
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. (DeleteUnderlyingObject() is not meeded.)

		/// <summary>
		/// Create other required elements.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			LeftMsaOA = new MoStemMsa();
			RightMsaOA = new MoStemMsa();
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoBinaryCompoundRule.MoBinaryCompoundRuleTags.kflidLeftMsa)
				   || (flid == (int)MoBinaryCompoundRule.MoBinaryCompoundRuleTags.kflidRightMsa)
				   || base.IsFieldRequired(flid);
		}
	}
}

namespace SIL.FieldWorks.FDO.Ling
{
	/// <summary>
	/// </summary>
	public partial class MoEndoCompound
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. (DeleteUnderlyingObject() is not meeded.)

		/// <summary>
		/// Create other required elements.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			this.OverridingMsaOA = new MoStemMsa();
		}

	}
}

namespace SIL.FieldWorks.FDO.Ling
{
	/// <summary>
	/// </summary>
	public partial class MoExoCompound
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. (DeleteUnderlyingObject() is not meeded.)

		/// <summary>
		/// Create other required elements.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			ToMsaOA = new MoStemMsa();
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoExoCompound.MoExoCompoundTags.kflidToMsa)
				   || base.IsFieldRequired(flid);
		}
	}


	/// <summary>
	/// This class is used to provide 'sandbox' capability during MSA creation.
	/// It can be compared to a real MSA, but does it on the cheap without creating a real one,
	/// which may end be being deleted in the end.
	/// </summary>
	public class DummyGenericMSA
	{
		private MsaType m_type = MsaType.kStem;
		private int m_mainPOS = 0;
		private int m_secondaryPOS = 0;
		private int m_slot = 0;
		private FdoReferenceCollection<IPartOfSpeech> m_fromPOSes;

		/// <summary>
		/// Gets or sets the dummy MSA from parts of speech.
		/// </summary>
		public FdoReferenceCollection<IPartOfSpeech> FromPartsOfSpeech
		{
			get { return m_fromPOSes; }
			set
			{
				m_fromPOSes = value;
			}
		}

		/// <summary>
		/// Gets or sets the dummy MSA type, which corresponds to the obejct class.
		/// </summary>
		public MsaType MsaType
		{
			get { return m_type; }
			set
			{
				if (value != MsaType.kNotSet && value != MsaType.kRoot)
					m_type = value;
			}
		}

		/// <summary>
		/// Gets or sets the main POS value.
		/// </summary>
		public int MainPOS
		{
			get { return m_mainPOS; }
			set { m_mainPOS = value; }
		}

		/// <summary>
		/// Gets or sets the secondary POS value.
		/// </summary>
		public int SecondaryPOS
		{
			get { return m_secondaryPOS; }
			set { m_secondaryPOS = value; }
		}

		/// <summary>
		/// Gets or sets the Slot value.
		/// </summary>
		public int Slot
		{
			get { return m_slot; }
			set { m_slot = value; }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DummyGenericMSA()
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="msa"></param>
		/// <returns></returns>
		public static DummyGenericMSA Create(IMoMorphSynAnalysis msa)
		{
			switch (msa.ClassID)
			{
				case MoStemMsa.kclsidMoStemMsa:
					return new DummyGenericMSA(msa as IMoStemMsa);
				case MoInflAffMsa.kclsidMoInflAffMsa:
					return new DummyGenericMSA(msa as IMoInflAffMsa);
				case MoDerivAffMsa.kclsidMoDerivAffMsa:
					return new DummyGenericMSA(msa as IMoDerivAffMsa);
				case MoUnclassifiedAffixMsa.kclsidMoUnclassifiedAffixMsa:
					return new DummyGenericMSA(msa as IMoUnclassifiedAffixMsa);
				/* Not supported yet, so it throws an exception.
					case MoDerivStepMsa.kclsidMoDerivStepMsa:
						return new DummyGenericMSA(msa as MoDerivStepMsa);
						*/
			}
			return null;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private DummyGenericMSA(IMoDerivAffMsa derivMsa)
		{
			m_type = MsaType.kDeriv;
			m_mainPOS = derivMsa.FromPartOfSpeechRAHvo;
			m_secondaryPOS = derivMsa.ToPartOfSpeechRAHvo;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private DummyGenericMSA(IMoDerivStepMsa stepMsa)
		{
			Debug.Assert(false, "Step MSAs are not supported yet.");
			/*
			m_type;
			m_mainPOS;
			m_secondaryPOS;
			m_slot;
			*/
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private DummyGenericMSA(IMoInflAffMsa inflMsa)
		{
			m_type = MsaType.kInfl;
			m_mainPOS = inflMsa.PartOfSpeechRAHvo;
			//m_slot = inflMsa.SlotsRC;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private DummyGenericMSA(IMoStemMsa stemMsa)
		{
			m_type = MsaType.kStem;
			m_mainPOS = stemMsa.PartOfSpeechRAHvo;
			m_fromPOSes = stemMsa.FromPartsOfSpeechRC;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private DummyGenericMSA(IMoUnclassifiedAffixMsa uncMsa)
		{
			m_type = MsaType.kUnclassified;
			m_mainPOS = uncMsa.PartOfSpeechRAHvo;
		}
	}

	/// <summary></summary>
	public partial class MoMorphSynAnalysis
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS.

		/// <summary>
		/// Determines whether the specified Object is equal to the current MoStemMsa.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public virtual bool EqualsMsa(IMoMorphSynAnalysis msa)
		{
			throw new ApplicationException("Subclasses must override this method.");
		}

		/// <summary>
		/// Determines whether the specified dummy MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public virtual bool EqualsMsa(DummyGenericMSA msa)
		{
			throw new ApplicationException("Subclasses must override this method.");
		}

		/// <summary>
		/// Update an extant MSA to the new values in the dummy MSA,
		/// or make a new MSA with the values in the dummy msa.
		/// </summary>
		/// <param name="dummyMsa"></param>
		/// <remarks>
		/// Subclasses should override this method to do same-class updates,
		/// but then call this method to handle class changing activities.
		/// </remarks>
		public virtual IMoMorphSynAnalysis UpdateOrReplace(DummyGenericMSA dummyMsa)
		{
			ILexEntry le = LexEntry.CreateFromDBObject(m_cache, OwnerHVO);
			foreach (MoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				// Check other extant MSAs to see if they match the updated one.
				if (msa != this && msa.EqualsMsa(dummyMsa))
				{
					msa.MergeObject(this);
					return msa;
				}
			}

			// Make a new MSA.
			IMoMorphSynAnalysis newMsa = null;
			switch (dummyMsa.MsaType)
			{
				default:
					throw new ApplicationException("Cannot create any other kind of MSA here.");
				case MsaType.kRoot: // Fall through.
				case MsaType.kStem:
					newMsa = MoStemMsa.CreateFromDummy(le, dummyMsa);
					break;
				case MsaType.kDeriv:
					newMsa = MoDerivAffMsa.CreateFromDummy(le, dummyMsa);
					break;
				case MsaType.kInfl:
					newMsa = MoInflAffMsa.CreateFromDummy(le, dummyMsa);
					break;
				case MsaType.kUnclassified:
					newMsa = MoUnclassifiedAffixMsa.CreateFromDummy(le, dummyMsa);
					break;
			}

			newMsa.SwitchReferences(this);
			DeleteUnderlyingObject();

			return newMsa;
		}

		/// <summary>
		/// Delete the MSAs in the msaHvoList, if possible.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="msaHvoList"></param>
		public static void DeleteUnusedMsas(FdoCache cache, List<int> msaHvoList)
		{
			List<int> alreadyTried = new List<int>(msaHvoList.Count);
			foreach (int msaHvo in msaHvoList)
			{
				if (!alreadyTried.Contains(msaHvo))
				{
					MoMorphSynAnalysis msa = new MoMorphSynAnalysis(cache, msaHvo);
					if (msa != null && msa.CanDelete)
						msa.DeleteUnderlyingObject();
					alreadyTried.Add(msaHvo);
				}
			}
		}

		/// <summary>
		/// Switches select inbound references from the sourceMsa to 'this'.
		/// </summary>
		/// <param name="sourceMsa"></param>
		public void SwitchReferences(IMoMorphSynAnalysis sourceMsa)
		{
			foreach (LinkedObjectInfo loi in sourceMsa.LinkedObjects)
			{
				switch (loi.RelObjClass)
				{
					case WfiMorphBundle.kclsidWfiMorphBundle:
						{
							IWfiMorphBundle mb = WfiMorphBundle.CreateFromDBObject(m_cache, loi.RelObjId);
							mb.MsaRAHvo = Hvo;
							break;
						}
					case LexSense.kclsidLexSense:
						{
							ILexSense sense = LexSense.CreateFromDBObject(m_cache, loi.RelObjId);
							sense.MorphoSyntaxAnalysisRAHvo = Hvo;
							break;
						}
				}
			}
		}

		/// <summary>
		/// Gets delete status for the object.
		/// True means it can be deleted, otherwise false.
		/// </summary>
		public override bool CanDelete
		{
			get
			{
				return LinkedObjects.Count == 0;
			}
		}

		/// <summary>
		/// Get a list of sense IDs that reference this MSA.
		/// </summary>
		public List<int> AllSenseClientIDs
		{
			get
			{
				string sql = String.Format("SELECT Id FROM LexSense WHERE MorphoSyntaxAnalysis={0}", m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, sql, null);
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
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public virtual ITsString InterlinearNameTSS
		{
			get { throw new NotImplementedException("Subclasses must implement this method."); }
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public virtual string InterlinearAbbr
		{
			get { return InterlinearAbbrTSS.Text; }
		}

		/// <summary>
		/// Return the Grammatical Info. (POS) TsString for the given writing system.
		/// </summary>
		/// <param name="wsAnal">If this is magic WS then return BestAnalorVern. Otherwise return the
		/// string associated with the specific writing system.</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public virtual ITsString InterlinAbbrTSS(int wsAnal)
		{
			throw new NotImplementedException("Subclasses must implement this method.");
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public virtual ITsString InterlinearAbbrTSS
		{
			get { throw new NotImplementedException("Subclasses must implement this method."); }
		}


		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		public virtual string PosFieldName
		{
			get { return this.InterlinearName; }
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
		/// Get a string used to to represent any MSA as a reference target to an environment with
		/// no context that would otherwise tell you such things as what LexEntry this belongs to.
		/// </summary>
		public string LongNameAdHoc
		{
			get
			{
				string pfx = "";
				IOleDbCommand odc = null;
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				try
				{
					// The stored procedure must NOT modify the database contents!
					string sqlQuery = String.Format(
						"exec DisplayName_MSA \'<root><Obj Id=\"{0}\"/></root>\', 1", m_hvo);
					uint cbSpaceTaken;
					bool fMoreRows;
					bool fIsNull;
					using (ArrayPtr rgchUsername = MarshalEx.ArrayToNative(4000, typeof(char)))
					{
						odc.ExecCommand(sqlQuery,
							(int)SqlStmtType.knSqlStmtStoredProcedure);
						odc.GetRowset(0);
						odc.NextRow(out fMoreRows);
						Debug.Assert(fMoreRows,
							"ID doesn't appear to be for a MoMorphSynAnalysis.");
						odc.GetColValue(3, rgchUsername, rgchUsername.Size, out cbSpaceTaken, out fIsNull, 0);
						byte[] rgbTemp = (byte[])MarshalEx.NativeToArray(rgchUsername, (int)cbSpaceTaken, typeof(byte));
						pfx = Encoding.Unicode.GetString(rgbTemp);
					}
				}
				finally
				{
					DbOps.ShutdownODC(ref odc);
				}
				return pfx;
			}
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in an ad hoc co-prohibition.
		/// </summary>
		public ITsString LongNameAdHocTs
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.Append(LongNameAdHoc);
				return tisb.GetString();
			}
		}

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
		public virtual ITsString LongNameTs
		{
			get { throw new NotImplementedException("Subclasses must implement this method."); }
		}

		/// <summary>
		/// Delete the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Trigger cleaning up any unused Msas after this object is deleted.
			this.HandleOldMSAs();
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Delete original MSAs in Components, if permitted.
		/// </summary>
		private void HandleOldMSAs()
		{
			while (this.ComponentsRS.Count > 0)
			{
				IMoMorphSynAnalysis oldMsa = ComponentsRS[0];
				ComponentsRS.RemoveAt(0);
				if (oldMsa.CanDelete)
					oldMsa.DeleteUnderlyingObject();
			}
		}

		/// <summary>
		/// Get gloss of first sense that uses this msa
		/// </summary>
		/// <returns>the gloss as a string</returns>
		public string GetGlossOfFirstSense()
		{
			ILexEntry le = LexEntry.CreateFromDBObject(m_cache, OwnerHVO);
			foreach (ILexSense sense in le.SensesOS)
			{
				if (sense.MorphoSyntaxAnalysisRAHvo == this.Hvo)
				{
					return sense.Gloss.AnalysisDefaultWritingSystem;
				}
			}
			return Strings.ksQuestions;
		}

		/// <summary>
		///
		/// </summary>
		public ITsString PartOfSpeechTSS
		{
			get { return PartOfSpeechForWsTSS(m_cache.LangProject.DefaultAnalysisWritingSystem); }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public virtual ITsString PartOfSpeechForWsTSS(int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString("", ws);
		}

		/// <summary>
		/// Return the slot object ids for the MSA.  Always empty if not inflectional.
		/// </summary>
		public virtual List<int> Slots
		{
			get
			{
				return new List<int>();
			}
		}


		/// <summary>
		///
		/// </summary>
		public ITsString InflectionClassTSS
		{
			get { return InflectionClassForWsTSS(m_cache.LangProject.DefaultAnalysisWritingSystem); }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public virtual ITsString InflectionClassForWsTSS(int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString("", ws);
		}

		/// <summary>
		///
		/// </summary>
		public virtual ITsString FeaturesTSS
		{
			get
			{ return m_cache.MakeAnalysisTss(""); }
		}

		/// <summary>
		///
		/// </summary>
		public virtual ITsString ExceptionFeaturesTSS
		{
			get { return m_cache.MakeAnalysisTss(""); }
		}

		/// <summary>
		/// Check whether this MoMorphSynAnalysis object is empty of any content.
		/// </summary>
		public virtual bool IsEmpty
		{
			get
			{
				return String.IsNullOrEmpty(this.GlossString) &&
					this.GlossBundleRS.Count == 0 &&
					this.ComponentsRS.Count == 0;
			}
		}

		/// <summary>
		/// Check whether the specified reference attribute HVO is valid for the
		/// specified field.
		/// </summary>
		/// <param name="hvo">the RA HVO</param>
		/// <param name="flid">the field ID</param>
		/// <returns>true if the RA is valid, otherwise false</returns>
		protected bool IsReferenceAttributeValid(int hvo, int flid)
		{
			if (hvo == 0)
				return true;

			Set<int> candidates = ReferenceTargetCandidates(flid);
			return candidates.Contains(hvo);
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

			foreach (IFsFeatureSpecification spec in fs.FeatureSpecsOC)
			{
				if (!IsFeatureValid(pos, spec.FeatureRAHvo))
				{
					fs.FeatureSpecsOC.Remove(spec);
				}
			}
		}

		/// <summary>
		/// Checks if the specified feature definition HVO is valid for the
		/// specified category.
		/// </summary>
		/// <param name="pos">the category to check for validity</param>
		/// <param name="hvo">the feature definition HVO</param>
		/// <returns>true if the feature is valid, otherwise false</returns>
		protected bool IsFeatureValid(IPartOfSpeech pos, int hvo)
		{
			while (pos != null)
			{
				if (pos.InflectableFeatsRC.Contains(hvo))
				{
					return true;
				}
				ICmObject cobj = CmObject.CreateFromDBObject(pos.Cache, pos.OwnerHVO);
				pos = cobj as IPartOfSpeech;
			}

			return false;
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
	}

	/// <summary>
	/// Summary description for MoStemName.
	/// </summary>
	public partial class MoStemName
	{
		/// <summary>
		/// Create other required elements (one feature structure in regions).
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			RegionsOC.Add(new FsFeatStruc());
		}
	}

	/// <summary>
	/// Summary description for CmPossibility.
	/// </summary>
	public partial class MoStemMsa
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS.
		// NOTE: Don't override ShortName here, as the superclass override will do it right.

		///<summary>
		/// Copies attributes associated with the current POS, such as inflection features and classes, that
		/// are valid from the specified MSA to this MSA. If the attribute is not valid, it is removed from
		/// this MSA.
		///</summary>
		///<param name="srcMsa">the source MSA</param>
		public void CopyAttributesIfValid(MoStemMsa srcMsa)
		{
			// inflection classes
			if (IsReferenceAttributeValid(srcMsa.InflectionClassRAHvo, (int)MoStemMsaTags.kflidInflectionClass))
			{
				if (srcMsa.InflectionClassRAHvo != InflectionClassRAHvo)
					InflectionClassRAHvo = srcMsa.InflectionClassRAHvo;
			}
			else if (InflectionClassRAHvo != 0)
			{
				InflectionClassRAHvo = 0;
			}

			// inflection features
			if (srcMsa.MsFeaturesOAHvo == 0)
			{
				MsFeaturesOA = null;
			}
			else
			{
				if (MsFeaturesOAHvo != srcMsa.MsFeaturesOAHvo)
					m_cache.CopyObject(srcMsa.MsFeaturesOAHvo, m_hvo, (int)MoStemMsaTags.kflidMsFeatures);
				RemoveInvalidFeatureSpecs(PartOfSpeechRA, MsFeaturesOA);
				if (MsFeaturesOA.IsEmpty)
					MsFeaturesOA = null;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int PartOfSpeechRAHvo
		{
			get
			{
				return PartOfSpeechRAHvo_Generated;
			}
			set
			{
				int oldHvo = PartOfSpeechRAHvo_Generated;
				PartOfSpeechRAHvo_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (oldHvo != 0 && oldHvo != value)
					CopyAttributesIfValid(this);
			}
		}

		/// <summary>
		///
		/// </summary>
		public IPartOfSpeech PartOfSpeechRA
		{
			get
			{
				return PartOfSpeechRA_Generated;
			}
			set
			{
				int oldHvo = PartOfSpeechRAHvo;
				PartOfSpeechRA_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (oldHvo != 0 && oldHvo != value.Hvo)
					CopyAttributesIfValid(this);
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

			IMoStemMsa stemMsa = (IMoStemMsa)msa;
			// TODO: Add checks for other properties, when we support using them.
			if (stemMsa.PartOfSpeechRAHvo != PartOfSpeechRAHvo)
				return false;
			if (stemMsa.InflectionClassRAHvo != InflectionClassRAHvo)
				return false;
			if (!FsFeatStruc.AreEquivalent(MsFeaturesOA, stemMsa.MsFeaturesOA))
				return false;
			if (!FromPartsOfSpeechRC.IsEquivalent(stemMsa.FromPartsOfSpeechRC))
				return false;
			return ProdRestrictRC.IsEquivalent(stemMsa.ProdRestrictRC);
		}

		/// <summary>
		/// Determines whether the specified dummy MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public override bool EqualsMsa(DummyGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoStemMsa.
			if (msa.MsaType != MsaType.kStem)
				return false;

			// The dummy generic currently can't have any of these, so if this does, they don't match.
			if (InflectionClassRAHvo != 0
				|| (MsFeaturesOAHvo != 0 && !MsFeaturesOA.IsEmpty)
				|| (ProdRestrictRC.Count != 0)
				|| (FromPartsOfSpeechRC.Count != 0))
				return false;

			// TODO: Add checks for other properties, when we support using them.
			return PartOfSpeechRAHvo == msa.MainPOS;
		}

		/// <summary>
		/// Update an extant MSA to the new values in the dummy MSA,
		/// or make a new MSA with the values in the dummy msa.
		/// </summary>
		/// <param name="dummyMsa"></param>
		public override IMoMorphSynAnalysis UpdateOrReplace(DummyGenericMSA dummyMsa)
		{
			if (dummyMsa.MsaType == MsaType.kStem || dummyMsa.MsaType == MsaType.kRoot)
			{
				if (PartOfSpeechRAHvo != dummyMsa.MainPOS)
					PartOfSpeechRAHvo = dummyMsa.MainPOS;
				return this;
			}

			return base.UpdateOrReplace(dummyMsa);
		}

		/// <summary>
		/// Create a new MoStemMsa, based on the given dummy MSA.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="dummyMsa"></param>
		/// <returns></returns>
		public static IMoStemMsa CreateFromDummy(ILexEntry entry, DummyGenericMSA dummyMsa)
		{
			Debug.Assert(dummyMsa.MsaType == MsaType.kRoot || dummyMsa.MsaType == MsaType.kStem);

			IMoStemMsa stemMsa = (IMoStemMsa)entry.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
			stemMsa.PartOfSpeechRAHvo = dummyMsa.MainPOS;

			return stemMsa;
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				int analWs = m_cache.LangProject.DefaultAnalysisWritingSystem;
				IPartOfSpeech pos = PartOfSpeechRA;
				if (pos != null)
					tisb.AppendTsString(tsf.MakeString(pos.Abbreviation.AnalysisDefaultWritingSystem, analWs));
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
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				int analWs = m_cache.LangProject.DefaultAnalysisWritingSystem;
				int userWs = m_cache.LangProject.DefaultUserWritingSystem;
				if (PartOfSpeechRAHvo == 0)
				{
					ILexEntry entry = LexEntry.CreateFromDBObject(Cache, OwnerHVO);
					foreach (IMoForm form in entry.AllAllomorphs)
					{
						// LT-7075 was crashing when it was null, trying to get the Guid
						string sType = string.Empty;
						if (form.MorphTypeRA != null)
							sType = form.MorphTypeRA.Guid.ToString();
						if ((sType != MoMorphType.kguidMorphClitic) &&
							(sType != MoMorphType.kguidMorphEnclitic) &&
							(sType != MoMorphType.kguidMorphProclitic))
							return tsf.MakeString(Strings.ksStemNoCatInfo, userWs);
					}
					return tsf.MakeString(Strings.ksCliticNoCatInfo, userWs);
				}
				else
					return CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRAHvo);
			}
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override string InterlinearName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				return InterlinearStem(CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRAHvo));
			}
		}

		private ITsString InterlinearStem(ITsString tssPartOfSpeech)
		{
			if (PartOfSpeechRAHvo == 0)
			{
				return m_cache.MakeUserTss(Strings.ksNotSure);
			}
			ITsString tssName = tssPartOfSpeech;
			ITsStrBldr bldr = tssName.GetBldr();
			int cch = bldr.Length;
			if (InflectionClassRAHvo != 0)
			{
				bldr.ReplaceTsString(cch, cch, m_cache.MakeUserTss("  ("));
				cch = bldr.Length;
				bldr.ReplaceTsString(cch, cch, InflectionClassRA.Abbreviation.BestAnalysisVernacularAlternative);
				cch = bldr.Length;
				bldr.ReplaceTsString(cch, cch, m_cache.MakeUserTss(") "));
			}
			else
			{
				bldr.ReplaceTsString(cch, cch, m_cache.MakeUserTss(" "));
			}
			cch = bldr.Length;
			IFsFeatStruc features = MsFeaturesOA;
			if (features != null)
				bldr.ReplaceTsString(cch, cch, m_cache.MakeUserTss(features.ShortName));
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
				return InterlinearStem(CmPossibility.TSSAbbrforWS(m_cache, PartOfSpeechRAHvo, wsAnal));
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get
			{
				return InterlinAbbrTSS(LangProject.kwsFirstAnalOrVern);
			}
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want the inflection class numbers or feature stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (PartOfSpeechRAHvo == 0)
					return String.Empty;
				else
					return this.PartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		/// <summary>
		/// Get a string used to to represent an MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoStemMsa.MoStemMsaTags.kflidInflectionClass:
					return PartOfSpeechRA;
				case (int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				case (int)MoStemMsa.MoStemMsaTags.kflidProdRestrict:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoStemMsa.MoStemMsaTags.kflidInflectionClass:
					set = new Set<int>();
					if (PartOfSpeechRAHvo > 0)
					{
						foreach (MoInflClass ic in PartOfSpeechRA.AllInflectionClasses)
							set.Add(ic.Hvo);
					}
					break;
				case (int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech:
					set = new Set<int>();
					foreach (IPartOfSpeech pos in Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS)
					{
						set.Add(pos.Hvo);
					}
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
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
			if (flid == (int)MoStemMsa.MoStemMsaTags.kflidInflectionClass)
			{
				// Inflection class is irrelevant for left/right members of a compound
				if ((OwningFlid == (int)MoBinaryCompoundRule.MoBinaryCompoundRuleTags.kflidLeftMsa) ||
					(OwningFlid == (int)MoBinaryCompoundRule.MoBinaryCompoundRuleTags.kflidRightMsa))
					return false;
				if (PartOfSpeechRAHvo == 0)
					return false; // if no POS has been specified, then there's no need to show infl class
			}
			else if (flid == (int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech)
			{
				if (!OwningLexEntryHasProCliticOrEnclitic())
					return false;
			}
			return base.IsFieldRelevant(flid);
		}
		private bool OwningLexEntryHasProCliticOrEnclitic()
		{
			if (OwningFlid != (int)LexEntry.LexEntryTags.kflidMorphoSyntaxAnalyses)
			{ // FromPartsOfSpeech only relevant for a proclitic or enclitic
				return false;
			}
			try
			{
				ILexEntry entry = LexEntry.CreateFromDBObject(Cache, OwnerHVO);
				foreach (IMoMorphType mt in entry.MorphTypes)
				{
					if ((mt.Guid.ToString() == MoMorphType.kguidMorphProclitic) ||
						(mt.Guid.ToString() == MoMorphType.kguidMorphEnclitic))
						return true;
				}
			}
			catch
			{
				return false;
			}

			return false;
		}
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech);
		}

		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or null if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			IPartOfSpeech pos = PartOfSpeechRA;
			if (pos != null)
			{
				ITsString tssPOS = pos.Abbreviation.GetAlternativeTss(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

		/// <summary>
		/// Get the Inflection Class Name for this MSA.
		/// </summary>
		public override ITsString InflectionClassForWsTSS(int ws)
		{
			IMoInflClass incl = InflectionClassRA;
			if (incl != null)
			{
				ITsString tss = incl.Abbreviation.GetAlternativeTss(ws);
				if (tss == null || String.IsNullOrEmpty(tss.Text))
					tss = incl.Abbreviation.BestAnalysisVernacularAlternative;
				return tss;
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

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
					return m_cache.MakeAnalysisTss("");
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
					return m_cache.MakeAnalysisTss("");
				}
			}
		}

		/// <summary>
		/// Check whether this MoStemMsa object is empty of any content.
		/// </summary>
		public override bool IsEmpty
		{
			get
			{
				return this.PartOfSpeechRAHvo == 0 &&
					this.InflectionClassRAHvo == 0 &&
					this.FromPartsOfSpeechRC.Count == 0 &&
					this.ProdRestrictRC.Count == 0 &&
					this.StratumRAHvo == 0 &&
					this.MsFeaturesOAHvo == 0 &&
					base.IsEmpty;
			}
		}
	}

	/// <summary></summary>
	public partial class MoDerivAffMsa
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS.
		// NOTE: Don't override ShortName here, as the superclass override will do it right.

		///<summary>
		/// Copies attributes associated with the current "From" POS, such as inflection features and classes, that
		/// are valid from the specified MSA to this MSA. If the attribute is not valid, it is removed from
		/// this MSA.
		///</summary>
		///<param name="srcMsa">the source MSA</param>
		public void CopyToAttributesIfValid(MoDerivAffMsa srcMsa)
		{
			// inflection classes
			if (IsReferenceAttributeValid(srcMsa.ToInflectionClassRAHvo, (int)MoDerivAffMsaTags.kflidToInflectionClass))
			{
				if (srcMsa.ToInflectionClassRAHvo != ToInflectionClassRAHvo)
					ToInflectionClassRAHvo = srcMsa.ToInflectionClassRAHvo;
			}
			else if (ToInflectionClassRAHvo != 0)
			{
				ToInflectionClassRAHvo = 0;
			}

			// inflection features
			if (srcMsa.ToMsFeaturesOAHvo == 0)
			{
				ToMsFeaturesOA = null;
			}
			else
			{
				if (ToMsFeaturesOAHvo != srcMsa.ToMsFeaturesOAHvo)
					m_cache.CopyObject(srcMsa.ToMsFeaturesOAHvo, m_hvo, (int)MoDerivAffMsaTags.kflidToMsFeatures);
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
		public void CopyFromAttributesIfValid(MoDerivAffMsa srcMsa)
		{
			// inflection classes
			if (IsReferenceAttributeValid(srcMsa.FromInflectionClassRAHvo, (int)MoDerivAffMsaTags.kflidFromInflectionClass))
			{
				if (srcMsa.FromInflectionClassRAHvo != FromInflectionClassRAHvo)
					FromInflectionClassRAHvo = srcMsa.FromInflectionClassRAHvo;
			}
			else if (FromInflectionClassRAHvo != 0)
			{
				FromInflectionClassRAHvo = 0;
			}

			// inflection features
			if (srcMsa.FromMsFeaturesOAHvo == 0)
			{
				FromMsFeaturesOA = null;
			}
			else
			{
				if (FromMsFeaturesOAHvo != srcMsa.FromMsFeaturesOAHvo)
					m_cache.CopyObject(srcMsa.FromMsFeaturesOAHvo, m_hvo, (int)MoDerivAffMsaTags.kflidFromMsFeatures);
				RemoveInvalidFeatureSpecs(FromPartOfSpeechRA, FromMsFeaturesOA);
				if (FromMsFeaturesOA.IsEmpty)
					FromMsFeaturesOA = null;
			}

			// stem names
			if (IsReferenceAttributeValid(srcMsa.FromStemNameRAHvo, (int)MoDerivAffMsaTags.kflidFromStemName))
			{
				if (srcMsa.FromStemNameRAHvo != FromStemNameRAHvo)
					FromStemNameRAHvo = srcMsa.FromStemNameRAHvo;
			}
			else if (FromStemNameRAHvo != 0)
			{
				FromStemNameRAHvo = 0;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int ToPartOfSpeechRAHvo
		{
			get
			{
				return ToPartOfSpeechRAHvo_Generated;
			}
			set
			{
				int originalHvo = ToPartOfSpeechRAHvo;
				ToPartOfSpeechRAHvo_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (originalHvo != 0 && originalHvo != value)
					CopyToAttributesIfValid(this);
			}
		}

		/// <summary>
		///
		/// </summary>
		public IPartOfSpeech ToPartOfSpeechRA
		{
			get
			{
				return ToPartOfSpeechRA_Generated;
			}
			set
			{
				int originalHvo = ToPartOfSpeechRAHvo;
				ToPartOfSpeechRA_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (originalHvo != 0 && originalHvo != value.Hvo)
					CopyToAttributesIfValid(this);
			}
		}

		/// <summary>
		///
		/// </summary>
		public int FromPartOfSpeechRAHvo
		{
			get
			{
				return FromPartOfSpeechRAHvo_Generated;
			}
			set
			{
				int hvo = m_cache.GetObjProperty(m_hvo, (int)MoDerivAffMsaTags.kflidFromPartOfSpeech);
				FromPartOfSpeechRAHvo_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (hvo != 0 && hvo != value)
					CopyFromAttributesIfValid(this);
			}
		}

		/// <summary>
		///
		/// </summary>
		public IPartOfSpeech FromPartOfSpeechRA
		{
			get
			{
				return FromPartOfSpeechRA_Generated;
			}
			set
			{
				int originalHvo = FromPartOfSpeechRAHvo_Generated;
				FromPartOfSpeechRA_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (originalHvo != 0 && originalHvo != value.Hvo)
					CopyFromAttributesIfValid(this);
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

			IMoDerivAffMsa derivMsa = (IMoDerivAffMsa)msa;

			return (FsFeatStruc.AreEquivalent(FromMsFeaturesOA, derivMsa.FromMsFeaturesOA)
				&& FsFeatStruc.AreEquivalent(ToMsFeaturesOA, derivMsa.ToMsFeaturesOA)
				&& FromPartOfSpeechRAHvo == derivMsa.FromPartOfSpeechRAHvo
				&& ToPartOfSpeechRAHvo == derivMsa.ToPartOfSpeechRAHvo
				&& FromInflectionClassRAHvo == derivMsa.FromInflectionClassRAHvo
				&& FromStemNameRAHvo == derivMsa.FromStemNameRAHvo
				&& ToInflectionClassRAHvo == derivMsa.ToInflectionClassRAHvo
				&& FromProdRestrictRC.IsEquivalent(derivMsa.FromProdRestrictRC)
				&& ToProdRestrictRC.IsEquivalent(derivMsa.ToProdRestrictRC));
		}

		/// <summary>
		/// Determines whether the specified dummy MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoDerivAffMsa to compare with the current MoDerivAffMsa.</param>
		/// <returns>true if the specified MoDerivAffMsa is equal to the current MoDerivAffMsa; otherwise, false.</returns>
		public override bool EqualsMsa(DummyGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			if (msa.MsaType != MsaType.kDeriv)
				return false;

			// Check the two properties we get from the DLG match, and the others we care about are missing.
			return FromPartOfSpeechRAHvo == msa.MainPOS
				&& ToPartOfSpeechRAHvo == msa.SecondaryPOS
				&& FromInflectionClassRAHvo == 0
				&& FromStemNameRAHvo == 0
				&& ToInflectionClassRAHvo == 0
				&& FromProdRestrictRC.Count == 0
				&& ToProdRestrictRC.Count == 0
				&& (ToMsFeaturesOAHvo == 0 || ToMsFeaturesOA.IsEmpty)
				&& (FromMsFeaturesOAHvo == 0 || FromMsFeaturesOA.IsEmpty);
		}

		/// <summary>
		/// Update an extant MSA to the new values in the dummy MSA,
		/// or make a new MSA with the values in the dummy msa.
		/// </summary>
		/// <param name="dummyMsa"></param>
		public override IMoMorphSynAnalysis UpdateOrReplace(DummyGenericMSA dummyMsa)
		{
			if (dummyMsa.MsaType == MsaType.kDeriv)
			{
				if (FromPartOfSpeechRAHvo != dummyMsa.MainPOS)
					FromPartOfSpeechRAHvo = dummyMsa.MainPOS;
				if (ToPartOfSpeechRAHvo != dummyMsa.SecondaryPOS)
					ToPartOfSpeechRAHvo = dummyMsa.SecondaryPOS;
				return this;
			}

			return base.UpdateOrReplace(dummyMsa);
		}

		/// <summary>
		/// Create a new MoDerivAffMsa, based on the given dummy MSA.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="dummyMsa"></param>
		/// <returns></returns>
		public static IMoDerivAffMsa CreateFromDummy(ILexEntry entry, DummyGenericMSA dummyMsa)
		{
			Debug.Assert(dummyMsa.MsaType == MsaType.kDeriv);

			IMoDerivAffMsa derivMsa = (IMoDerivAffMsa)entry.MorphoSyntaxAnalysesOC.Add(new MoDerivAffMsa());
			derivMsa.FromPartOfSpeechRAHvo = dummyMsa.MainPOS;
			derivMsa.ToPartOfSpeechRAHvo = dummyMsa.SecondaryPOS;

			return derivMsa;
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromInflectionClass:
					return FromPartOfSpeechRA;
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToInflectionClass:
					return ToPartOfSpeechRA;
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromPartOfSpeech: // fall through
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromProdRestrict: // fall through
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToProdRestrict:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromInflectionClass:
					set = new Set<int>();
					if (FromPartOfSpeechRAHvo > 0)
						foreach (MoInflClass ic in FromPartOfSpeechRA.AllInflectionClasses)
							set.Add(ic.Hvo);
					break;
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToInflectionClass:
					set = new Set<int>();
					if (ToPartOfSpeechRAHvo > 0)
						foreach (MoInflClass ic in ToPartOfSpeechRA.AllInflectionClasses)
							set.Add(ic.Hvo);
					break;
				case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromStemName:
					set = new Set<int>();
					if (FromPartOfSpeechRAHvo > 0)
						foreach (MoStemName sn in FromPartOfSpeechRA.AllStemNames)
							set.Add(sn.Hvo);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// the way we want to show this in an interlinear view
		/// </summary>
		public override string InterlinearName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				return InterlinearAffix(CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRAHvo, Strings.ksAny),
					CmPossibility.BestAnalysisOrVernName(m_cache, ToPartOfSpeechRAHvo, Strings.ksAny));
			}
		}

		private ITsString InterlinearAffix(ITsString tssFromPartOfSpeech, ITsString tssToPartOfSpeech)
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.AppendTsString(tssFromPartOfSpeech);
			tisb.AppendTsString(m_cache.MakeUserTss(">"));
			tisb.AppendTsString(tssToPartOfSpeech);
			return tisb.GetString();
		}

		/// <summary>
		/// Return the Grammatical Info. (POS) TsString for the given writing system.
		/// </summary>
		/// <param name="wsAnal">If this is magic WS then return BestAnalorVern. Otherwise return the
		/// string associated with the specific writing system.</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{
			return InterlinearAffix(CmPossibility.TSSAbbrforWS(m_cache, FromPartOfSpeechRAHvo, wsAnal),
					CmPossibility.TSSAbbrforWS(m_cache, ToPartOfSpeechRAHvo, wsAnal));
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get
			{
				return InterlinAbbrTSS(LangProject.kwsFirstAnalOrVern);
			}
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
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				int analWs = m_cache.LangProject.DefaultAnalysisWritingSystem;
				int userWs = m_cache.LangProject.DefaultUserWritingSystem;
				string sMsaName = "";
				if (FromPartOfSpeechRAHvo > 0 && ToPartOfSpeechRAHvo > 0)
				{
					// Both are non-null.
					if (FromPartOfSpeechRAHvo == ToPartOfSpeechRAHvo)
					{
						// Both are the same POS.
						sMsaName = String.Format(Strings.ksAffixChangesX,
							CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRAHvo).Text);
					}
					else
					{
						// Different POSes.
						sMsaName = String.Format(Strings.ksAffixConvertsXtoY,
							CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRAHvo).Text,
							CmPossibility.BestAnalysisOrVernName(m_cache, ToPartOfSpeechRAHvo).Text);
					}
				}
				else
				{
					// One must be null. Both may be null.
					if (FromPartOfSpeechRAHvo == 0 && ToPartOfSpeechRAHvo == 0)
					{
						// Both are null.
						sMsaName = Strings.ksAffixChangesAny;
					}
					else
					{
						if (FromPartOfSpeechRAHvo == 0)
						{
							// From POS is null.
							sMsaName = String.Format(Strings.ksAffixConvertsAnyToX,
								CmPossibility.BestAnalysisOrVernName(m_cache, ToPartOfSpeechRAHvo).Text);
						}
						else
						{
							// To POS is null.
							sMsaName = String.Format(Strings.ksAffixConvertsXtoAny,
								CmPossibility.BestAnalysisOrVernName(m_cache, FromPartOfSpeechRAHvo).Text);
						}
					}
				}
				return tsf.MakeString(sMsaName, userWs);
			}
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get { return InterlinearNameTSS; }
		}
		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>
		/// Inflection class is irrelevant if no category has been specified
		/// </remarks>
		/// <returns></returns>
		public override bool IsFieldRelevant(int flid)
		{
			if (flid == (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromInflectionClass)
			{
				if (FromPartOfSpeechRAHvo == 0)
					return false; // if no POS has been specified, then there's no need to show infl class
			}
			if (flid == (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToInflectionClass)
			{
				if (ToPartOfSpeechRAHvo == 0)
					return false; // if no POS has been specified, then there's no need to show infl class
			}
			return base.IsFieldRelevant(flid);
		}

		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or null if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			IPartOfSpeech posFrom = FromPartOfSpeechRA;
			IPartOfSpeech posTo = ToPartOfSpeechRA;
			if (posFrom != null || posTo != null)
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				if (posFrom != null)
				{
					ITsString tssPOS = posFrom.Abbreviation.GetAlternativeTss(ws);
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
					ITsString tssPOS = posTo.Abbreviation.GetAlternativeTss(ws);
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
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

		/// <summary>
		/// Get the Inflection Class Name for this MSA.
		/// </summary>
		public override ITsString InflectionClassForWsTSS(int ws)
		{
			IMoInflClass inclFrom = FromInflectionClassRA;
			IMoInflClass inclTo = ToInflectionClassRA;
			if (inclFrom != null || inclTo != null)
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				if (inclFrom != null)
				{
					ITsString tss = inclFrom.Abbreviation.GetAlternativeTss(ws);
					if (tss == null || String.IsNullOrEmpty(tss.Text))
						tss = inclFrom.Abbreviation.BestAnalysisVernacularAlternative;
					tisb.AppendTsString(tss);
				}
				else
				{
					tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
				}
				tisb.AppendTsString(m_cache.MakeUserTss(" > "));
				if (inclTo != null)
				{
					ITsString tss = inclTo.Abbreviation.GetAlternativeTss(ws);
					if (tss == null || String.IsNullOrEmpty(tss.Text))
						tss = inclTo.Abbreviation.BestAnalysisVernacularAlternative;
					tisb.AppendTsString(tss);
				}
				else
				{
					tisb.AppendTsString(m_cache.MakeUserTss(Strings.ksQuestions));
				}
				return tisb.GetString();
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

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
					return m_cache.MakeAnalysisTss("");
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

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want all the stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (ToPartOfSpeechRAHvo == 0)
					return String.Empty;
				else
					return this.ToPartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		/// <summary>
		/// Check whether this MoDerivAffMsa is empty of content.
		/// </summary>
		public override bool IsEmpty
		{
			get
			{
				return this.ToPartOfSpeechRAHvo == 0 &&
					this.FromPartOfSpeechRAHvo == 0 &&
					this.ToInflectionClassRAHvo == 0 &&
					this.FromInflectionClassRAHvo == 0 &&
					this.ToProdRestrictRC.Count == 0 &&
					this.FromProdRestrictRC.Count == 0 &&
					this.ToMsFeaturesOAHvo == 0 &&
					this.FromMsFeaturesOAHvo == 0 &&
					this.FromStemNameRAHvo == 0 &&
					this.AffixCategoryRAHvo == 0 &&
					this.StratumRAHvo == 0 &&
					base.IsEmpty;
			}
		}
	}

	/// <summary></summary>
	public partial class MoUnclassifiedAffixMsa
	{
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoUnclassifiedAffixMsa.MoUnclassifiedAffixMsaTags.kflidPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				default:
					return base.ReferenceTargetOwner(flid);
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

			IMoUnclassifiedAffixMsa uncMsa = (IMoUnclassifiedAffixMsa)msa;
			return PartOfSpeechRAHvo == uncMsa.PartOfSpeechRAHvo;
		}

		/// <summary>
		/// Determines whether the specified dummy MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoUnclassifiedAffixMsa to compare with the current MoUnclassifiedAffixMsa.</param>
		/// <returns>true if the specified MoUnclassifiedAffixMsa is equal to the current MoUnclassifiedAffixMsa; otherwise, false.</returns>
		public override bool EqualsMsa(DummyGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoStemMsa.
			if (msa.MsaType != MsaType.kUnclassified)
				return false;

			// TODO: Add checks for other properties, when we support using them.
			return PartOfSpeechRAHvo == msa.MainPOS;
		}

		/// <summary>
		/// Update an extant MSA to the new values in the dummy MSA,
		/// or make a new MSA with the values in the dummy msa.
		/// </summary>
		/// <param name="dummyMsa"></param>
		public override IMoMorphSynAnalysis UpdateOrReplace(DummyGenericMSA dummyMsa)
		{
			if (dummyMsa.MsaType == MsaType.kUnclassified)
			{
				if (PartOfSpeechRAHvo != dummyMsa.MainPOS)
					PartOfSpeechRAHvo = dummyMsa.MainPOS;
				return this;
			}

			return base.UpdateOrReplace(dummyMsa);
		}

		/// <summary>
		/// Create a new MoUnclassifiedAffixMsa, based on the given dummy MSA.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="dummyMsa"></param>
		/// <returns></returns>
		public static IMoUnclassifiedAffixMsa CreateFromDummy(ILexEntry entry, DummyGenericMSA dummyMsa)
		{
			Debug.Assert(dummyMsa.MsaType == MsaType.kUnclassified);

			IMoUnclassifiedAffixMsa uncMsa = (IMoUnclassifiedAffixMsa)entry.MorphoSyntaxAnalysesOC.Add(new MoUnclassifiedAffixMsa());
			uncMsa.PartOfSpeechRAHvo = dummyMsa.MainPOS;

			return uncMsa;
		}

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
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				if (PartOfSpeechRAHvo == 0)
					return m_cache.MakeUserTss(Strings.ksAttachesToAnyCat);
				return CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRAHvo);
			}
		}

		/// <summary>
		/// Get the BestAnalorVern PartofSpeech for a Lexmeme.
		/// </summary>
		/// <param name="wsAnal">on this class we are not making use of this parameter</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{
			return InterlinearNameTSS;
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override string InterlinearAbbr
		{
			get
			{
				string retval = ChooserNameTS.Text;
				if (retval == null || retval == String.Empty)
					retval = Strings.ksQuestions;
				return retval;
			}
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
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				int analWs = m_cache.LangProject.DefaultAnalysisWritingSystem;
				int userWs = m_cache.LangProject.DefaultUserWritingSystem;
				string sMsaName = "";
				if (PartOfSpeechRAHvo == 0)
					sMsaName = Strings.ksAffixAttachesToAny;
				else
					sMsaName = String.Format(Strings.ksAffixFoundOnX,
						CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRAHvo).Text);
				return tsf.MakeString(sMsaName, userWs);
			}
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				IPartOfSpeech pos = PartOfSpeechRA;
				if (pos != null)
					return CmPossibility.BestAnalysisOrVernAbbr(m_cache, PartOfSpeechRAHvo);
				else
					return tsf.MakeString(Strings.ksAttachesToAnyCat, m_cache.DefaultUserWs);
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
			IPartOfSpeech pos = PartOfSpeechRA;
			if (pos != null)
			{
				ITsString tssPOS = pos.Abbreviation.GetAlternativeTss(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

		/// <summary>
		/// Check whether this MoUnclassifiedAffixMsa object is empty of content.
		/// </summary>
		public override bool IsEmpty
		{
			get
			{
				return this.PartOfSpeechRAHvo == 0 && base.IsEmpty;
			}
		}
	}

	/// <summary></summary>
	public partial class MoInflAffMsa
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS.
		// NOTE: Don't override ShortName here, as the superclass override will do it right.

		///<summary>
		/// Copies attributes associated with the current POS, such as inflection features and classes, that
		/// are valid from the specified MSA to this MSA. If the attribute is not valid, it is removed from
		/// this MSA.
		///</summary>
		///<param name="srcMsa">the source MSA</param>
		public void CopyAttributesIfValid(MoInflAffMsa srcMsa)
		{
			// inflection features
			if (srcMsa.InflFeatsOAHvo == 0)
			{
				InflFeatsOA = null;
			}
			else
			{
				if (InflFeatsOAHvo != srcMsa.InflFeatsOAHvo)
					m_cache.CopyObject(srcMsa.InflFeatsOAHvo, m_hvo, (int)MoInflAffMsaTags.kflidInflFeats);
				RemoveInvalidFeatureSpecs(PartOfSpeechRA, InflFeatsOA);
				if (InflFeatsOA.IsEmpty)
					InflFeatsOA = null;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int PartOfSpeechRAHvo
		{
			get
			{
				return PartOfSpeechRAHvo_Generated;
			}
			set
			{
				int oldHvo = PartOfSpeechRAHvo_Generated;
				PartOfSpeechRAHvo_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (oldHvo != 0 && oldHvo != value)
					CopyAttributesIfValid(this);
			}
		}

		/// <summary>
		///
		/// </summary>
		public IPartOfSpeech PartOfSpeechRA
		{
			get
			{
				return PartOfSpeechRA_Generated;
			}
			set
			{
				int oldHvo = PartOfSpeechRAHvo;
				PartOfSpeechRA_Generated = value;
				// Retain attributes associated with this POS that are still valid.
				if (oldHvo != 0 && oldHvo != value.Hvo)
					CopyAttributesIfValid(this);
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

			MoInflAffMsa inflMsa = (MoInflAffMsa)msa;
			return FsFeatStruc.AreEquivalent(InflFeatsOA, inflMsa.InflFeatsOA)
				&& FromProdRestrictRC.IsEquivalent(inflMsa.FromProdRestrictRC)
				&& PartOfSpeechRAHvo == inflMsa.PartOfSpeechRAHvo
				&& HasSameSlots(inflMsa);
		}

		/// <summary>
		/// Update an extant MSA to the new values in the dummy MSA,
		/// or make a new MSA with the values in the dummy msa.
		/// </summary>
		/// <param name="dummyMsa"></param>
		public override IMoMorphSynAnalysis UpdateOrReplace(DummyGenericMSA dummyMsa)
		{
			if (dummyMsa.MsaType == MsaType.kInfl)
			{
				if (PartOfSpeechRAHvo != dummyMsa.MainPOS)
					PartOfSpeechRAHvo = dummyMsa.MainPOS;
				if (dummyMsa.Slot > 0 && !SlotsRC.Contains(dummyMsa.Slot))
					SlotsRC.Add(dummyMsa.Slot);
				// Remove any slots that are not legal for the current POS.
				List<int> allSlots;
				if (PartOfSpeechRAHvo == 0)
					allSlots = new List<int>(0);
				else
					allSlots = PartOfSpeechRA.AllAffixSlotIDs;
				int[] slots = SlotsRC.HvoArray;
				foreach (int slotHvo in slots)
				{
					if (!allSlots.Contains(slotHvo))
						SlotsRC.Remove(slotHvo);
				}
				return this;
			}

			return base.UpdateOrReplace(dummyMsa);
		}

		/// <summary>
		/// Create a new MoInflAffMsa, based on the given dummy MSA.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="dummyMsa"></param>
		/// <returns></returns>
		public static IMoInflAffMsa CreateFromDummy(ILexEntry entry, DummyGenericMSA dummyMsa)
		{
			Debug.Assert(dummyMsa.MsaType == MsaType.kInfl);

			IMoInflAffMsa inflMsa = (IMoInflAffMsa)entry.MorphoSyntaxAnalysesOC.Add(new MoInflAffMsa());
			inflMsa.PartOfSpeechRAHvo = dummyMsa.MainPOS;
			if (dummyMsa.Slot > 0)
				inflMsa.SlotsRC.Add(dummyMsa.Slot);

			return inflMsa;
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

			List<int> inflMsaSlots = new List<int>(inflMsa.SlotsRC.HvoArray);
			for (int ihvo = 0; ihvo < SlotsRC.Count; ihvo++)
			{
				if (!inflMsaSlots.Contains(SlotsRC.HvoArray[ihvo]))
					return false;
				inflMsaSlots.Remove(SlotsRC.HvoArray[ihvo]); // ensure unique matches.
			}
			return true;
		}
		/// <summary>
		/// Determines whether the specified dummy MSA is equal to the current MSA.
		/// </summary>
		/// <param name="msa">The MoStemMsa to compare with the current MoStemMsa.</param>
		/// <returns>true if the specified MoStemMsa is equal to the current MoStemMsa; otherwise, false.</returns>
		public override bool EqualsMsa(DummyGenericMSA msa)
		{
			// This is the behavior defined by Object.Equals().
			if (msa == null)
				return false;

			// Make sure that we can  cast this object to a MoStemMsa.
			if (msa.MsaType != MsaType.kInfl)
				return false;

			if (PartOfSpeechRAHvo != msa.MainPOS)
				return false;

			// Can't set these two from the dialog, if non-null, we are not equal.
			if (FromProdRestrictRC.Count != 0)
				return false;
			if (InflFeatsOAHvo != 0 && !InflFeatsOA.IsEmpty)
				return false;

			// TODO: Add checks for other properties, when we support using them.
			if (msa.Slot == 0)
				return (SlotsRC.Count == 0);
			else
				return SlotsRC.Count == 1 && SlotsRC.Contains(msa.Slot);
		}

		/// <summary>
		/// the way we want to show this in an interlinear view
		/// </summary>
		public override string InterlinearName
		{
			get { return InterlinearNameTSS.Text; }
		}

		/// <summary>
		/// The way we want to show this in an interlinear view
		/// </summary>
		public override ITsString InterlinearNameTSS
		{
			get
			{
				return InterlinAbbrTSS(LangProject.kwsFirstAnalOrVern);
			}
		}

		private ITsString InterlinearInflectionalAffix(ITsString tssPartOfSpeech, int ws)
		{
			if (PartOfSpeechRAHvo == 0)
			{
				return m_cache.MakeUserTss(Strings.ksInflectsAnyCat);
			}
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			int userWs = m_cache.LangProject.DefaultUserWritingSystem;

			tisb.AppendTsString(tssPartOfSpeech);
			tisb.AppendTsString(tsf.MakeString(":", userWs));

			int cnt = 0;
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
		/// Return the Grammatical Info. (POS) TsString for the given writing system.
		/// </summary>
		/// <param name="wsAnal">If this is magic WS then return BestAnalorVern. Otherwise return the
		/// string associated with the specific writing system.</param>
		/// <returns>Abbreviated PartOfSpeech for Interlinear view.</returns>
		public override ITsString InterlinAbbrTSS(int wsAnal)
		{

				return InterlinearInflectionalAffix(CmPossibility.TSSAbbrforWS(m_cache, PartOfSpeechRAHvo, wsAnal),wsAnal);
		}

		/// <summary>
		/// Abbreviated PartOfSpeech for Interlinear.
		/// </summary>
		public override ITsString InterlinearAbbrTSS
		{
			get
			{
				return InterlinAbbrTSS(LangProject.kwsFirstAnalOrVern);
			}
		}

		/// <summary>
		/// Get a TS string used to to represent any MSA in the MSADlgLauncherSlice slice.
		/// </summary>
		public override ITsString LongNameTs
		{
			get
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				int userWs = m_cache.LangProject.DefaultUserWritingSystem;
				string sMsaName = "";
				if (PartOfSpeechRAHvo == 0)
				{
					sMsaName = Strings.ksAffixInflectsAny;
				}
				else
				{
					if (SlotsRC.Count == 0)
					{
						// Only have the POS.
						Debug.Assert(PartOfSpeechRAHvo > 0);
						sMsaName = String.Format(Strings.ksAffixInflectsX,
							CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRAHvo).Text);
						// don't want this per Andy's comment in LT-4903 because it isn't accurate.
						//tisb.AppendTsString(tsf.MakeString(" in any slot", userWs));
					}
					else
					{
						StringBuilder bldrSlots = new StringBuilder();
						int cnt = 0;
						foreach (MoInflAffixSlot slot in SlotsRC)
						{
							if (cnt++ > 0)
								bldrSlots.Append("/");
							bldrSlots.Append(slot.ShortName);
						}
						if (cnt > 1)
						{
							sMsaName = String.Format(Strings.ksAffixInXInflectsYPlural,
								bldrSlots.ToString(),
								CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRAHvo).Text);
						}
						else
						{
							sMsaName = String.Format(Strings.ksAffixInXInflectsY,
								bldrSlots.ToString(),
								CmPossibility.BestAnalysisOrVernName(m_cache, PartOfSpeechRAHvo).Text);
						}
					}
				}
				return tsf.MakeString(sMsaName, userWs);
			}
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get { return InterlinearNameTSS; }
		}

		/// <summary>
		/// the name which assumes the maximum context
		/// </summary>
		public override string ShortName
		{
			get
			{
				ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, OwnerHVO);
				StringBuilder sb = new StringBuilder();
				sb.Append(entry.CitationFormWithAffixType);
				sb.Append(" ");
				string sGloss = GetFirstGlossOfMSAThatMatches(entry.SensesOS);
				if (sGloss == null)
				{ // if we can't find an MSA that matches, just use first gloss
					LexSense sense = (LexSense)entry.SensesOS.FirstItem;
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
		/// Mimics ShortName, but with TsStrings so we get proper writing systems.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				LexEntry.CitationFormWithAffixTypeTss(m_cache, OwnerHVO, tisb);
				tisb.Append(" ");
				ITsString tssGloss = GetBestGlossOfMSAThatMatchesTss(OwnerHVO);
				if (tssGloss != null)
					tisb.AppendTsString(tssGloss);
				return tisb.GetString();
			}
		}

		private ITsString GetBestGlossOfMSAThatMatchesTss(int hvoEntry)
		{
			ITsString tssGloss = GetFirstGlossOfMSAThatMatchesTss(hvoEntry, (int)LexEntry.LexEntryTags.kflidSenses);
			if (tssGloss == null)
			{ // if we can't find an MSA that matches, just use first gloss
				int[] ahvoSenses = Cache.GetVectorProperty(hvoEntry, (int)LexEntry.LexEntryTags.kflidSenses, false);
				if (ahvoSenses.Length > 0 && ahvoSenses[0] > 0)
					tssGloss = Cache.MainCacheAccessor.get_MultiStringAlt(ahvoSenses[0], (int)LexSense.LexSenseTags.kflidGloss,
																		  m_cache.DefaultAnalWs);
			}
			return tssGloss;
		}

		private string GetFirstGlossOfMSAThatMatches(FdoOwningSequence<ILexSense> os)
		{
			string sGloss = null;
			foreach (ILexSense sense in os)
			{ // get the gloss of the first sense that refers to this MSA
				if (sense.MorphoSyntaxAnalysisRAHvo == Hvo)
				{
					sGloss = sense.Gloss.AnalysisDefaultWritingSystem;
					break;  // found first gloss; quit
				}
				else
				{
					sGloss = GetFirstGlossOfMSAThatMatches(sense.SensesOS);
					if (sGloss != null)
						break; // first gloss was in a subsense; quit
				}
			}
			return sGloss;
		}


		/// <summary>
		/// Get the default analysis gloss of the first sense of the specified sense container
		/// (typically our owning entry, but we may recurse through its nested senses)
		/// that uses this MSA.
		/// </summary>
		/// <param name="hvoSenseContainer"></param>
		/// <param name="flidSenses"></param>
		/// <returns></returns>
		private ITsString GetFirstGlossOfMSAThatMatchesTss(int hvoSenseContainer, int flidSenses)
		{
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			int chvo = sda.get_VecSize(hvoSenseContainer, flidSenses);
			for (int i = 0; i < chvo; i++)
			{ // get the gloss of the first sense that refers to this MSA
				int hvoSense = sda.get_VecItem(hvoSenseContainer, flidSenses, i);
				if (sda.get_ObjectProp(hvoSense, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis) == Hvo)
				{
					return sda.get_MultiStringAlt(hvoSense, (int)LexSense.LexSenseTags.kflidGloss,
						Cache.DefaultAnalWs);
				}
				else
				{
					ITsString sGloss = GetFirstGlossOfMSAThatMatchesTss(hvoSense, (int)LexSense.LexSenseTags.kflidSenses);
					if (sGloss != null)
						return sGloss; // first gloss was in a subsense; quit
				}
			}
			return null;
		}

		/// <summary>
		/// The writing system for sorting a list of ShortNames: vernacular.
		/// </summary>
		public override string SortKeyWs
		{
			get
			{
				string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(
					m_cache.DefaultVernWs);

				if (sWs == null || sWs == string.Empty)
					sWs = m_cache.FallbackUserLocale;

				if (sWs == null || sWs == "")
					sWs = "en";
				return sWs;
			}
		}

		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>
		/// Inflection class is irrelevant for LeftMsa and RightMsa in binary compounds
		/// </remarks>
		/// <returns></returns>
		public override bool IsFieldRelevant(int flid)
		{
			if (flid == (int)MoInflAffMsa.MoInflAffMsaTags.kflidAffixCategory)
			{
				if (this.AffixCategoryRAHvo == 0)  // Only get info on import, so it's not relevant if this is 0
					return false;
			}
			return base.IsFieldRelevant(flid);
		}

		/// <summary>
		/// Remove any slots in SlotsRC
		/// </summary>
		public void ClearAllSlots()
		{
			if (SlotsRC.Count > 0)
			{
				foreach (int hvo in SlotsRC.HvoArray)
					SlotsRC.Remove(hvo);
			}
		}
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoInflAffMsa.MoInflAffMsaTags.kflidPartOfSpeech:
					return m_cache.LangProject.PartsOfSpeechOA;
				case (int)MoInflAffMsa.MoInflAffMsaTags.kflidSlots:
					return PartOfSpeechRA;
				case (int)MoInflAffMsa.MoInflAffMsaTags.kflidFromProdRestrict:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoInflAffMsa.MoInflAffMsaTags.kflidSlots:
					if (PartOfSpeechRAHvo > 0)
					{
						IPartOfSpeech pos = PartOfSpeechRA;
						LexEntry lex = this.Owner as LexEntry;
						set = GetSetOfSlots(Cache, lex, pos);
					}
					else
						set = new Set<int>();
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// Get set of inflectional affix slots appropriate for the entry
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="lex">lex entry</param>
		/// <param name="pos">part of speech</param>
		/// <returns></returns>
		public static Set<int> GetSetOfSlots(FdoCache cache, LexEntry lex, IPartOfSpeech pos)
		{
			Set<int> set;
			bool fIsPrefixal = false;
			bool fIsSuffixal = false;
			List<IMoMorphType> morphTypes = lex.MorphTypes;
			foreach (IMoMorphType morphType in morphTypes)
			{
				if (MoMorphType.IsPrefixishType(cache, morphType.Hvo))
					fIsPrefixal = true;
				if (MoMorphType.IsSuffixishType(cache, morphType.Hvo))
					fIsSuffixal = true;
			}
			if (fIsPrefixal && fIsSuffixal)
				set = new Set<int>(pos.AllAffixSlotIDs);
			else
				set = MoInflAffixTemplate.GetSomeSlots(cache, new Set<int>(pos.AllAffixSlotIDs), fIsPrefixal);
			return set;
		}

		/// <summary>
		/// Get the part of speech ITsString for this MSA.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The part of speech abbreviation as a TsString for the given writing system,
		/// or empty if the part of speech is undefined.</returns>
		public override ITsString PartOfSpeechForWsTSS(int ws)
		{
			IPartOfSpeech pos = PartOfSpeechRA;
			if (pos != null)
			{
				ITsString tssPOS = pos.Abbreviation.GetAlternativeTss(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

		/// <summary>
		/// Return the slots for this inflectional MSA.
		/// </summary>
		public override List<int> Slots
		{
			get
			{
				return new List<int>(SlotsRC.HvoArray);
			}
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString FeaturesTSS
		{
			get
			{
				IFsFeatStruc feat = InflFeatsOA;
				if (feat != null)
					return feat.ShortNameTSS;
				else
					return m_cache.MakeAnalysisTss("");
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
					return m_cache.MakeAnalysisTss("");
				}
			}
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want all the stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (PartOfSpeechRAHvo == 0)
					return String.Empty;
				else
					return this.PartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		/// <summary>
		/// The best/first gloss for this MSA
		/// </summary>
		/// <remarks>Used in the chooser.</remarks>
		public ITsString GlossTSS
		{
			get
			{
				return GetBestGlossOfMSAThatMatchesTss(OwnerHVO);
			}
		}
		/// <summary>
		/// Check whether this MoInflAffMsa object is empty of content.
		/// </summary>
		public override bool IsEmpty
		{
			get
			{
				return this.PartOfSpeechRAHvo == 0 &&
					this.SlotsRC.Count == 0 &&
					this.AffixCategoryRAHvo == 0 &&
					this.FromProdRestrictRC.Count == 0 &&
					this.InflFeatsOAHvo == 0 &&
					base.IsEmpty;
			}
		}
	}


	/// <summary>
	/// TODO: Currently never used. But, when we start using this class, we should revise the Msa tests in LingTests.cs
	/// (e.g. EqualMsaTests).
	/// </summary>
	public partial class MoDerivStepMsa
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS.
		// NOTE: Don't override ShortName here, as the superclass override will do it right.

		/// <summary>
		///
		/// </summary>
		public int PartOfSpeechRAHvo
		{
			get
			{
				return PartOfSpeechRAHvo_Generated;
			}
			set
			{
				int originalHvo = PartOfSpeechRAHvo_Generated;
				PartOfSpeechRAHvo_Generated = value;
				// When we change the part of speech to something different, we can't keep
				// the old InflectionClass since it is part of the original part of speech.
				// We try to allow any code (maybe copy operation) that wants to set the
				// inflection class before the part of speech.
				if (originalHvo != 0 && originalHvo != value)
					m_cache.SetObjProperty(m_hvo, (int)MoDerivStepMsaTags.kflidInflectionClass, 0);
			}
		}

		/// <summary>
		///
		/// </summary>
		public IPartOfSpeech PartOfSpeechRA
		{
			get
			{
				return PartOfSpeechRA_Generated;
			}
			set
			{
				int originalHvo = PartOfSpeechRAHvo_Generated;
				PartOfSpeechRA_Generated = value;
				// When we change the part of speech to something different, we can't keep
				// the old InflectionClass since it is part of the original part of speech.
				// We try to allow any code (maybe copy operation) that wants to set the
				// inflection class before the part of speech.
				if (originalHvo != 0 && originalHvo != value.Hvo)
					m_cache.SetObjProperty(m_hvo, (int)MoDerivStepMsaTags.kflidInflectionClass, 0);
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
			IPartOfSpeech pos = PartOfSpeechRA;
			if (pos != null)
			{
				ITsString tssPOS = pos.Abbreviation.GetAlternativeTss(ws);
				if (tssPOS == null || String.IsNullOrEmpty(tssPOS.Text))
					tssPOS = pos.Abbreviation.BestAnalysisVernacularAlternative;
				return tssPOS;
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

		/// <summary>
		/// Get the Inflection Class Name for this MSA.
		/// </summary>
		public override ITsString InflectionClassForWsTSS(int ws)
		{
			IMoInflClass incl = InflectionClassRA;
			if (incl != null)
			{
				ITsString tss = incl.Abbreviation.GetAlternativeTss(ws);
				if (tss == null || String.IsNullOrEmpty(tss.Text))
					tss = incl.Abbreviation.BestAnalysisVernacularAlternative;
				return tss;
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", ws);
			}
		}

		/// <summary>
		///
		/// </summary>
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
					return m_cache.MakeAnalysisTss("");
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
					return m_cache.MakeAnalysisTss("");
				}
			}
		}

		/// <summary>
		/// A name suitable for export into a pos field, like for use in ToolBox/LexiquePro/WeSay(via LIFT)
		/// </summary>
		/// <remarks>We don't want all the stuff that the interlinear name uses.</remarks>
		public override string PosFieldName
		{
			get
			{
				if (PartOfSpeechRAHvo == 0)
					return String.Empty;
				else
					return this.PartOfSpeechRA.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		/// <summary>
		/// Check whether this MoDerivStepMsa object is empty of content.
		/// </summary>
		public override bool IsEmpty
		{
			get
			{
				return this.PartOfSpeechRAHvo == 0 &&
					this.InflectionClassRAHvo == 0 &&
					this.ProdRestrictRC.Count == 0 &&
					this.InflFeatsOAHvo == 0 &&
					this.MsFeaturesOAHvo == 0 &&
					base.IsEmpty;
			}
		}
	}

	/// <summary></summary>
	public partial class MoInflClass
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS.

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				string result = ShortNameTSS.Text;
				if (String.IsNullOrEmpty(result))
					return Strings.ksQuestions;		// was "??", not "???"
				else return result;

			}
		}

		/// <summary>
		/// Shortest reasonable name for the object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return this.Name.BestAnalysisVernacularAlternative;
			}
		}
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoInflClass.MoInflClassTags.kflidName)
				|| (flid == (int)MoInflClass.MoInflClassTags.kflidAbbreviation);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class MoForm
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the Object is complete (true)
		/// or if it still needs to have work done on it (false).
		/// Subclasses that override this property should call the superclass property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsComplete
		{
			get
			{
				// Check the alternates.
				foreach (int ws in m_cache.LangProject.CurVernWssRS.HvoArray)
				{
					string form = Form.GetAlternative(ws);
					if (form == null || form == String.Empty)
						return false;
				}
				return base.IsComplete;
			}
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
			if (this.Form != null)
				sKey = this.Form.GetAlternative(ws);
			if (sKey == null)
				sKey = "";
			if (sortedFromEnd)
				sKey = StringUtils.ReverseString(sKey);

			return SortKeyMorphType(sKey);
		}

		/// <summary>
		/// This adjusts the input key by adding a space and number to cause things
		/// otherwise equal to group by the morph type of the lexeme form.
		/// </summary>
		/// <param name="sKey"></param>
		/// <returns></returns>
		internal string SortKeyMorphType(string sKey)
		{
			int hvoType = this.MorphTypeRAHvo;
			int nKey2 = 0;
			if (hvoType > 0)
			{
				nKey2 = m_cache.MainCacheAccessor.get_IntProp(hvoType,
					(int)MoMorphType.MoMorphTypeTags.kflidSecondaryOrder);
			}

			if (nKey2 != 0)
				sKey = sKey + " " + nKey2; // These are never > 9 at present.
			return sKey;
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
		public static IMoForm CreateAllomorph(ILexEntry entry, IMoMorphSynAnalysis msa,
			ITsString tssform, IMoMorphType morphType, bool fLexemeForm)
		{
			IMoForm allomorph = null;
			switch (morphType.Guid.ToString())
			{
				case Ling.MoMorphType.kguidMorphProclitic: // Fall through.
				case Ling.MoMorphType.kguidMorphClitic: // Fall through.
				case Ling.MoMorphType.kguidMorphEnclitic:
					Debug.Assert(msa is IMoStemMsa, "Wrong MSA for a clitic.");
					IMoStemMsa stemMsa = (IMoStemMsa) msa;
					goto case Ling.MoMorphType.kguidMorphBoundStem;
				case Ling.MoMorphType.kguidMorphRoot: // Fall through.
				case Ling.MoMorphType.kguidMorphBoundRoot: // Fall through.
				case Ling.MoMorphType.kguidMorphStem: // Fall through.
				case Ling.MoMorphType.kguidMorphParticle: // Fall through.
				case Ling.MoMorphType.kguidMorphPhrase: // Fall through.
				case Ling.MoMorphType.kguidMorphDiscontiguousPhrase: // Fall through.
					// AndyB_Yahoo: On particles, (and LT-485), these are always to be
					// roots that never take any affixes
					// AndyB_Yahoo: Therefore, they need to have StemMsas and Stem
					// allomorphs
				case Ling.MoMorphType.kguidMorphBoundStem:
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
				allomorph = (IMoForm) entry.AlternateFormsOS.Append(allomorph);
			allomorph.MorphTypeRA = morphType; // Has to be done before the next call.
			ITsString tssAllomorphForm = null;
			int maxLength = entry.Cache.MaxFieldLength((int) MoForm.MoFormTags.kflidForm);
			if (tssform.Length > maxLength)
			{
				string sMessage = String.Format(Strings.ksTruncatedXXXToYYYChars,
												fLexemeForm ? Strings.ksLexemeForm : Strings.ksAllomorph, maxLength);
				System.Windows.Forms.MessageBox.Show(sMessage, Strings.ksWarning,
													 System.Windows.Forms.MessageBoxButtons.OK,
													 System.Windows.Forms.MessageBoxIcon.Warning);
				tssAllomorphForm = tssform.GetSubstring(0, maxLength);
			}
			else
			{
				tssAllomorphForm = tssform;
			}
			allomorph.FormMinusReservedMarkers = tssAllomorphForm;
			if ((morphType.Guid.ToString() == Ling.MoMorphType.kguidMorphInfix) ||
				(morphType.Guid.ToString() == Ling.MoMorphType.kguidMorphInfixingInterfix))
			{
				HandleInfix(entry, allomorph);
			}
			return allomorph;
		}

		private static void HandleInfix(ILexEntry entry, IMoForm allomorph)
		{
			const string sDefaultPostionEnvironment = "/#[C]_";
			IMoAffixAllomorph infix = allomorph as IMoAffixAllomorph;
			if (infix == null)
				return; // something's wrong...
			FdoCache cache = entry.Cache;
			int defaultInfixPositionHvo = PhEnvironment.DefaultInfixEnvironment(cache, sDefaultPostionEnvironment);
			if (defaultInfixPositionHvo > 0)
				infix.PositionRS.Append(defaultInfixPositionHvo);
			else
			{
				// create default infix position environment
				IPhPhonData ppd = cache.LangProject.PhonologicalDataOA;
				PhEnvironment env = new PhEnvironment();
				ppd.EnvironmentsOS.Append(env);
				env.StringRepresentation.Text = sDefaultPostionEnvironment;
				env.Description.AnalysisDefaultWritingSystem.Text = "Default infix position environment";
				env.Name.AnalysisDefaultWritingSystem = "After stem-initial consonant";
				cache.PropChanged(null, PropChangeType.kpctNotifyAll, ppd.Hvo, (int)PhPhonData.PhPhonDataTags.kflidEnvironments, ppd.EnvironmentsOS.Count - 1, 1, 0);
				infix.PositionRS.Append(env);
			}
		}


		/// <summary>
		/// Side effects of deleting the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			List<LinkedObjectInfo> linkedObjs = LinkedObjects;
			List<int> deletedObjectIDs = new List<int>();
			foreach (LinkedObjectInfo loi in linkedObjs)
			{
				switch (loi.RelObjClass)
				{
					case MoAlloAdhocProhib.kclsidMoAlloAdhocProhib:
						{
							IMoAlloAdhocProhib aacp = MoAlloAdhocProhib.CreateFromDBObject(m_cache, loi.RelObjId);
							switch (loi.RelObjField)
							{
								case (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidFirstAllomorph:
									{
										aacp.DeleteObjectSideEffects(objectsToDeleteAlso, state);
										break;
									}
								case (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidAllomorphs:
									{
										aacp.AllomorphsRS.Remove(this);
										break;
									}
								case (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidRestOfAllos:
									{
										aacp.RestOfAllosRS.Remove(this);
										break;
									}
							}
							break;
						}
					case WfiMorphBundle.kclsidWfiMorphBundle:
						{
							if (!deletedObjectIDs.Contains(loi.RelObjId))
							{
								IWfiMorphBundle mb = WfiMorphBundle.CreateFromDBObject(m_cache, loi.RelObjId);
								if (!deletedObjectIDs.Contains(mb.OwnerHVO))
								{
									IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_cache, mb.OwnerHVO);
									deletedObjectIDs.Add(anal.Hvo);
									foreach (IWfiMorphBundle mbInner in anal.MorphBundlesOS)
										deletedObjectIDs.Add(mbInner.Hvo);
									anal.DeleteObjectSideEffects(objectsToDeleteAlso, state);
								}
							}
							break;
						}
				}
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		///  Do something
		/// </summary>
		/// <param name="form"></param>
		/// <param name="cache"></param>
		/// <returns>string</returns>
		public static string EnsureNoMarkers(string form, FdoCache cache)
		{
			return MoMorphType.StripAffixMarkers(cache, form);
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
				int wsVern = StringUtils.GetWsAtOffset(value, 0);
				Form.SetAlternative(EnsureNoMarkers(value.Text, m_cache), wsVern);
			}
		}

		/// <summary>
		/// Get the form with its markers.
		/// </summary>
		public string FormWithReservedMarkers
		{
			get { return ShortName; }
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				string form = Form.VernacularDefaultWritingSystem;
				if (form == null || form == String.Empty)
					form = Strings.ksQuestions;		// was "??", not "???"
				return form;
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
				int ws = m_cache.DefaultVernWs;
				string form = Form.VernacularDefaultWritingSystem;
				if (form == null || form == String.Empty)
					ws = m_cache.DefaultUserWs;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(ShortName, ws);
			}
		}

		/// <summary>
		/// Return a marked form in the desired writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public string FormWithMarkers(int ws)
		{
			string form = Form.GetAlternative(ws);
			if (String.IsNullOrEmpty(form))
				return null;
			else
				return PrecedingSymbol + form + FollowingSymbol;
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
				int vernWs = m_cache.DefaultVernWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, vernWs);
				tisb.AppendTsString(ShortNameTSS);

				List<LinkedObjectInfo> linkedObjects = LinkedObjects;
				List<int> countedObjectIDs = new List<int>();
				int alloAHPCount = 0;
				int analCount = 0;
				foreach (LinkedObjectInfo loi in linkedObjects)
				{
					switch (loi.RelObjClass)
					{
						default:
							break;
						case MoAlloAdhocProhib.kclsidMoAlloAdhocProhib:
							{
								if (loi.RelObjField == (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidFirstAllomorph)
								{
									if (!countedObjectIDs.Contains(loi.RelObjId))
									{
										countedObjectIDs.Add(loi.RelObjId);
										++alloAHPCount;
									}
								}
								break;
							}
						case WfiMorphBundle.kclsidWfiMorphBundle:
							{
								if (loi.RelObjField == (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph
									|| loi.RelObjField == (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense
									|| loi.RelObjField == (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa)
								{
									if (!countedObjectIDs.Contains(loi.RelObjId))
									{
										IWfiMorphBundle mb = WfiMorphBundle.CreateFromDBObject(m_cache, loi.RelObjId);
										if (!countedObjectIDs.Contains(mb.OwnerHVO))
										{
											countedObjectIDs.Add(mb.OwnerHVO);
											++analCount;
										}
									}
								}
								break;
							}
					}
				}

				int cnt = 1;
				string warningMsg = String.Format("\x2028\x2028{0}\x2028{1}", Strings.ksAlloUsedHere, Strings.ksDelAlloDelThese);
				bool wantMainWarningLine = true;
				if (analCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (analCount > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesByAnalyses, cnt++, analCount));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceByAnalyses, cnt++));
					wantMainWarningLine = false;
				}
				if (alloAHPCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (alloAHPCount > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesByAlloAdhoc, cnt++, alloAHPCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceByAlloAdhoc, cnt++, "\x2028"));
				}
				return tisb.GetString();
			}
		}
		/// <summary>
		/// Swap all references to this MoForm to use the new one
		/// </summary>
		/// <param name="newFormHvo">the hvo of the new MoForm</param>
		public void SwapReferences(int newFormHvo)
		{
			string cmd = string.Format("UPDATE MoAlloAdhocProhib  " +
				"SET FirstAllomorph={0} WHERE FirstAllomorph={1}",
				newFormHvo, this.m_hvo);
			DbOps.ExecuteStatementNoResults(m_cache, cmd, null);
			cmd = string.Format("UPDATE WfiMorphBundle  " +
				"SET Morph={0} WHERE Morph={1}",
				newFormHvo, this.m_hvo);
			DbOps.ExecuteStatementNoResults(m_cache, cmd, null);
			cmd = string.Format("UPDATE MoAlloAdhocProhib_RestOfAllos  " +
				"SET Dst={0} WHERE Dst={1}",
				newFormHvo, this.m_hvo);
			DbOps.ExecuteStatementNoResults(m_cache, cmd, null);
		}

		/// <summary>
		/// Get a display name suitable for use in displaying ad hoc rules.
		/// </summary>
		public string LongName
		{
			get
			{
				string name = "??? (???):???";
				IOleDbCommand odc = null;
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				try
				{
					// The stored procedure must NOT modify the database contents!
					string sqlQuery = String.Format("exec DisplayName_MoForm {0}", m_hvo);
					uint cbSpaceTaken;
					bool fMoreRows;
					bool fIsNull;
					using (ArrayPtr prgchUsername = MarshalEx.ArrayToNative(4000, typeof(char)))
					{
						odc.ExecCommand(sqlQuery,
							(int)SqlStmtType.knSqlStmtStoredProcedure);
						odc.GetRowset(0);
						odc.NextRow(out fMoreRows);
#if DEBUG
						if (!fMoreRows)
							Debug.WriteLine("No rows.");
						Debug.Assert(fMoreRows, "ID doesn't appear to be for a MoForm.");
#endif
						odc.GetColValue(1, prgchUsername, prgchUsername.Size, out cbSpaceTaken, out fIsNull, 0);
						byte[] rgbTemp = (byte[])MarshalEx.NativeToArray(prgchUsername, (int)cbSpaceTaken, typeof(byte));
						name = Encoding.Unicode.GetString(rgbTemp);
					}
				}
				finally
				{
					DbOps.ShutdownODC(ref odc);
				}
				if (string.IsNullOrEmpty(name))
					name = "??? (???):???";
				return name;
			}
		}

		/// <summary>
		/// Get a display name suitable for use in displaying ad hoc rules as an ITsString.
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				string form = LongName;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ITsString tss = tsf.MakeString(form, m_cache.DefaultVernWs);
				ITsStrBldr tsb = tss.GetBldr();
				tsb.ReplaceTsString(0, tsb.Length, tss);
				// we can only hope that parentheses are available in the analysis language.
				int ichMin = form.IndexOf('(');
				if (ichMin >= 0)
				{
					int ichMax = form.IndexOf(')', ichMin);
					if (ichMax > ichMin)
					{
						tsb.SetIntPropValues(ichMin, ichMax + 1,
							(int)FwTextPropType.ktptWs,
							(int)FwTextPropVar.ktpvDefault,
							m_cache.DefaultAnalWs);
					}
				}
				tss = tsb.GetString();
				return tss;
			}
		}

		/// <summary>
		/// If the morph has a root type (root or bound root), change it to the corresponding stem type.
		/// </summary>
		public void ChangeRootToStem()
		{
			if (MorphTypeRAHvo == Cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphRoot)))
				MorphTypeRAHvo = Cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphStem));
			else if (MorphTypeRAHvo == Cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphBoundRoot)))
				MorphTypeRAHvo = Cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphBoundStem));

		}

		/// <summary>
		/// The writing system for sorting a list of ShortNames: vernacular.
		/// </summary>
		public override string SortKeyWs
		{
			get
			{
				string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(
					m_cache.DefaultVernWs);

				if (sWs == null || sWs == string.Empty)
					sWs = m_cache.FallbackUserLocale;

				if (sWs == null || sWs == String.Empty)
					sWs = "en";
				return sWs;
			}
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
					return "";
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
					return "";
			}
		}
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)MoForm.MoFormTags.kflidForm)
				|| (flid == (int)MoForm.MoFormTags.kflidMorphType);
		}

		/// <summary>
		/// Make a new MoForm (actually the appropriate subclass, as deduced by FindMorphType
		/// from fullForm), add it to the morphemes of the owning lex entry, set its
		/// MoMorphType, also as deduced by FindMorphType from fullForm, and also set the form
		/// itself.
		/// If the entry doesn't already have a lexeme form, put the new morph there.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="owner"></param>
		/// <param name="fullForm">uses default vernacular writing system</param>
		/// <returns></returns>
		public static IMoForm MakeMorph(FdoCache cache, ILexEntry owner, string fullForm)
		{
			return MakeMorph(cache, owner, StringUtils.MakeTss(fullForm, cache.DefaultVernWs));
		}

		/// <summary>
		/// Make a new MoForm (actually the appropriate subclass, as deduced by FindMorphType
		/// from fullForm), add it to the morphemes of the owning lex entry, set its
		/// MoMorphType, also as deduced by FindMorphType from tssfullForm, and also set the form
		/// itself.
		/// If the entry doesn't already have a lexeme form, put the new morph there.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="owner"></param>
		/// <param name="tssfullForm">uses the ws of tssfullForm</param>
		/// <returns></returns>
		public static IMoForm MakeMorph(FdoCache cache, ILexEntry owner, ITsString tssfullForm)
		{
			int clsidForm; // The subclass of MoMorph to create if we need a new object.
			string realForm = tssfullForm.Text; // Gets stripped of morpheme-type characters.
			IMoMorphType mmt = MoMorphType.FindMorphType(cache,
				new MoMorphTypeCollection(cache), ref realForm, out clsidForm);
			IMoForm allomorph = null;
			switch (clsidForm)
			{
				case MoStemAllomorph.kclsidMoStemAllomorph:
					allomorph = new MoStemAllomorph();
					break;
				case MoAffixAllomorph.kclsidMoAffixAllomorph:
					allomorph = new MoAffixAllomorph();
					break;
				default:
					throw new InvalidProgramException(
						"unexpected MoForm subclass returned from FindMorphType");
			}
			if (owner.LexemeFormOAHvo == 0)
				owner.LexemeFormOA = allomorph;
			else
			{
				// An earlier version inserted at the start, to avoid making it the default
				// underlying form, which was the last one. But now we have an explicit
				// lexeme form. So go ahead and put it at the end.
				allomorph = owner.AlternateFormsOS.Append(allomorph);
			}
			ITsString tssRealForm;
			if (tssfullForm.Text != realForm)
			{
				// make a new tsString with the old ws.
				tssRealForm = StringUtils.MakeTss(realForm,
					StringUtils.GetWsAtOffset(tssfullForm, 0));
			}
			else
			{
				tssRealForm = tssfullForm;
			}
			allomorph.MorphTypeRA = mmt; // Has to be done, before the next call.
			allomorph.FormMinusReservedMarkers = tssRealForm;
			return allomorph;
		}
		/// <summary>
		/// Gets a set of all hvos of all valid morph type references for this MoForm
		/// </summary>
		/// <returns>A set of hvos.</returns>
		public Set<int> GetAllMorphTypeReferenceTargetCandidates()
		{
			Set<int> set = new Set<int>(m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.HvoArray);
			MoMorphTypeCollection types = new MoMorphTypeCollection(m_cache);
			if (OwningFlid == (int)LexEntry.LexEntryTags.kflidAlternateForms)
				set.Remove(types.Item(MoMorphType.kmtCircumfix).Hvo); // only for lexemeform
			return set;
		}

		/// <summary>
		/// get all analysis alteratives decorated with morph-type markers, e.g. ("-is, -iz")
		/// </summary>
		public Dictionary<string, string> NamesWithMarkers
		{
			get
			{
				Dictionary<string, string> markers = new Dictionary<string, string>();
				foreach (ILgWritingSystem ws in Cache.LangProject.CurVernWssRS)
				{
					string f = Form.GetAlternative(ws.Hvo);
					if (f != null)
					{
						markers.Add(ws.Abbreviation, string.Format("{0}{1}{2}", PrecedingSymbol, f, FollowingSymbol));
					}
				}
				return markers;
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
	}

	/// <summary>
	///
	/// </summary>
	public partial class MoAffixForm
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoForm.MoFormTags.kflidMorphType:
					return m_cache.LangProject.LexDbOA.MorphTypesOA;
				case (int)MoAffixForm.MoAffixFormTags.kflidInflectionClasses:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoForm.MoFormTags.kflidMorphType:
					set = new Set<int>(m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.HvoArray);
					// Remove root and stem types.
					MoMorphTypeCollection types = new MoMorphTypeCollection(m_cache);
					set.Remove(types.Item(MoMorphType.kmtBoundRoot).Hvo);
					set.Remove(types.Item(MoMorphType.kmtBoundStem).Hvo);
					set.Remove(types.Item(MoMorphType.kmtClitic).Hvo);
					set.Remove(types.Item(MoMorphType.kmtEnclitic).Hvo);
					set.Remove(types.Item(MoMorphType.kmtParticle).Hvo);
					set.Remove(types.Item(MoMorphType.kmtProclitic).Hvo);
					set.Remove(types.Item(MoMorphType.kmtRoot).Hvo);
					set.Remove(types.Item(MoMorphType.kmtStem).Hvo);
					set.Remove(types.Item(MoMorphType.kmtPhrase).Hvo);
					set.Remove(types.Item(MoMorphType.kmtDiscontiguousPhrase).Hvo);
					if (OwningFlid == (int)LexEntry.LexEntryTags.kflidAlternateForms)
						set.Remove(types.Item(MoMorphType.kmtCircumfix).Hvo); // only for lexemeform
					break;
				case (int)MoAffixForm.MoAffixFormTags.kflidInflectionClasses:
					set = new Set<int>();
					//List<CmPossibility> poses = m_cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities;
					int ownerID = OwnerHVO;
					int ownerClass = m_cache.GetIntProperty(ownerID, (int)CmObjectFields.kflidCmObject_Class);
					if (ownerClass == LexEntry.kclsidLexEntry)
					{
						ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, ownerID);
						foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
						{
							if (msa is IMoInflAffMsa)
							{
								IMoInflAffMsa infafxmsa = (IMoInflAffMsa)msa;
								IPartOfSpeech pos = infafxmsa.PartOfSpeechRA;
								if (pos != null)
								{
									foreach (IMoInflClass ic in pos.AllInflectionClasses)
										set.Add(ic.Hvo);
								}
							}
							// Review: is this correct?  I think the TO POS is the relevant
							// one for derivational affixes, but maybe nothing is.
							// HAB says: From is the correct one to use for the allomorphs.  The From indicates
							// the category to which the affix attaches.  This category is the one that
							// may have the inflection classes, one or more of which this allomorph may go with.
							else if (msa is IMoDerivAffMsa)
							{
								IMoDerivAffMsa drvafxmsa = (IMoDerivAffMsa)msa;
								IPartOfSpeech pos = drvafxmsa.FromPartOfSpeechRA;
								if (pos != null)
								{
									foreach (IMoInflClass ic in pos.AllInflectionClasses)
										set.Add(ic.Hvo);
								}
							}
						}
					}
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public override bool IsFieldRelevant(int flid)
		{
			if (flid != (int)MoAffixForm.MoAffixFormTags.kflidInflectionClasses)
				return true;

			// MoAffixForm.inflection classes are only relevant if the MSAs of the
			// entry include an inflectional affix MSA.

			ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, this.OwnerHVO);
			return entry.SupportsInflectionClasses() && base.IsFieldRelevant(flid);
		}
	}

	/// <summary></summary>
	public partial class MoStemAllomorph
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		public override void InitNewInternal()
		{
			if (OwningFlid == (int)LexEntry.LexEntryTags.kflidAlternateForms)
			{
				ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, OwnerHVO);
				IMoForm lexemeForm = entry.LexemeFormOA;
				if (lexemeForm != null && lexemeForm is MoStemAllomorph)
				{
					IMoMorphType mmt = lexemeForm.MorphTypeRA;
					if (mmt != null)
						MorphTypeRA = mmt;
				}
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
			if (flid == (int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName)
			{
				if (MorphTypeRA == null)
					return false;
				string sMorphTypeGuid = MorphTypeRA.Guid.ToString();
				if ((sMorphTypeGuid == MoMorphType.kguidMorphBoundRoot)
					|| (sMorphTypeGuid == MoMorphType.kguidMorphBoundStem)
					|| (sMorphTypeGuid == MoMorphType.kguidMorphPhrase) // LT-7334
					|| (sMorphTypeGuid == MoMorphType.kguidMorphRoot)
					|| (sMorphTypeGuid == MoMorphType.kguidMorphStem))
					return true;
				else
					return false;
			}
			return base.IsFieldRelevant(flid);
		}

		/// <summary>
		/// Set the type of the allomorph to root (a reasonable default).
		/// This method is invoked when creating a real lexeme form from a ghost.
		/// It is called by reflection.
		/// </summary>
		public void SetMorphTypeToRoot()
		{
			this.MorphTypeRAHvo = m_cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphRoot));
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoForm.MoFormTags.kflidMorphType:
					return m_cache.LangProject.LexDbOA.MorphTypesOA;
				case (int)MoStemAllomorph.MoStemAllomorphTags.kflidPhoneEnv:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoForm.MoFormTags.kflidMorphType:
					set = new Set<int>(m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.HvoArray);
					// Remove affix types.
					MoMorphTypeCollection types = new MoMorphTypeCollection(m_cache);
					set.Remove(types.Item(MoMorphType.kmtCircumfix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtInfix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtPrefix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtSimulfix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtSuffix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtSuprafix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtInfixingInterfix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtPrefixingInterfix).Hvo);
					set.Remove(types.Item(MoMorphType.kmtSuffixingInterfix).Hvo);
					break;
				case (int)MoStemAllomorph.MoStemAllomorphTags.kflidPhoneEnv:
					set = PhEnvironment.ValidEnvironments(m_cache);
					break;
				case (int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName:
					set = new Set<int>();
					//List<CmPossibility> poses = m_cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities;
					int ownerID = OwnerHVO;
					int ownerClass = m_cache.GetIntProperty(ownerID, (int)CmObjectFields.kflidCmObject_Class);
					if (ownerClass == LexEntry.kclsidLexEntry)
					{
						ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, ownerID);
						foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
						{
							if (msa is IMoStemMsa)
							{
								IMoStemMsa infstemmsa = (IMoStemMsa)msa;
								IPartOfSpeech pos = infstemmsa.PartOfSpeechRA;
								if (pos != null)
								{
									foreach (IMoStemName sn in pos.AllStemNames)
										set.Add(sn.Hvo);
								}
							}
						}
					}
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class MoAffixAllomorph
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		public override void InitNewInternal()
		{
			if (OwningFlid == (int)LexEntry.LexEntryTags.kflidAlternateForms)
			{
				ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, OwnerHVO);
				IMoForm lexemeForm = entry.LexemeFormOA;
				if (lexemeForm != null && lexemeForm is MoAffixAllomorph)
				{
					IMoMorphType mmt = lexemeForm.MorphTypeRA;
					if (mmt != null)
					{
						string mmtGuid = mmt.Guid.ToString();
						if (mmtGuid == MoMorphType.kguidMorphPrefix
							|| mmtGuid == MoMorphType.kguidMorphPrefixingInterfix
							|| mmtGuid == MoMorphType.kguidMorphInfix
							|| mmtGuid == MoMorphType.kguidMorphInfixingInterfix
							|| mmtGuid == MoMorphType.kguidMorphSuffix
							|| mmtGuid == MoMorphType.kguidMorphSuffixingInterfix)
						{
							MorphTypeRA = mmt;
						}
					}
				}
			}
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition:
				case (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPhoneEnv:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition:
				case (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPhoneEnv:
					set = PhEnvironment.ValidEnvironments(m_cache);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
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
			if (flid == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition)
			{
				if (!MorphTypeIsInfix())
					return false;
			}
			return base.IsFieldRelevant(flid);
		}

		private bool MorphTypeIsInfix()
		{
			if ((MorphTypeRA != null) &&
				((MorphTypeRA.Guid.ToString().ToLower() == MoMorphType.kguidMorphInfix.ToLower()) ||
				 (MorphTypeRA.Guid.ToString().ToLower() == MoMorphType.kguidMorphInfixingInterfix.ToLower())))
				return true;
			else
				return false;
		}
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			if (flid == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition)
			{
				if (MorphTypeIsInfix())
					return true;
			}
			return base.IsFieldRequired(flid);
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class MoInflAffixSlot
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS, and DeleteObjectSideEffects().

		/// <summary>
		/// Get the Inflectional Affix Slot name for a given writing system
		/// </summary>
		/// <param name="wsAnal"></param>
		/// <returns></returns>
		public ITsString ShortNameTSSforWS(int wsAnal)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			if (Optional)
				tisb.AppendTsString(tsf.MakeString("(", m_cache.DefaultUserWs));

			ITsString tss = null;
			tss = m_cache.LangProject.GetMagicStringAlt(wsAnal,
					Hvo, (int)MoInflAffixSlot.MoInflAffixSlotTags.kflidName);
			tisb.AppendTsString(tss);

			if (Optional)
				tisb.AppendTsString(tsf.MakeString(")", m_cache.DefaultUserWs));

			return tisb.GetString();
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return ShortNameTSSforWS(LangProject.kwsFirstAnalOrVern).Text;
			}
		}

		/// <summary>
		/// Return a shortname label marked as if a prefix.
		/// </summary>
		public ITsString PrefixMarkedShortNameTSS
		{
			get
			{
				return GetAffixMarkedNameTss(MoMorphType.kguidMorphPrefix);
			}
		}

		/// <summary>
		/// Return a shortname label marked as if a suffix.
		/// </summary>
		public ITsString SuffixMarkedShortNameTSS
		{
			get
			{
				return GetAffixMarkedNameTss(MoMorphType.kguidMorphSuffix);
			}
		}

		private ITsString GetAffixMarkedNameTss(string sGuidType)
		{
			ITsString tss = m_cache.LangProject.GetMagicStringAlt(LangProject.kwsFirstAnalOrVern,
				Hvo, (int)MoInflAffixSlot.MoInflAffixSlotTags.kflidName);
			bool fVernRTL = m_cache.GetBoolProperty(m_cache.DefaultVernWs,
				(int)LgWritingSystem.LgWritingSystemTags.kflidRightToLeft);
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultVernWs);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, 0x00000000);	// Black
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Doulos SIL");
			ITsTextProps ttp = tpb.GetTextProps();
			ITsStrBldr bldr = tss.GetBldr();
			if (Optional)
			{
				bldr.Replace(0, 0, fVernRTL ? ")" : "(", ttp);
				bldr.Replace(bldr.Length, bldr.Length, fVernRTL ? "(" : ")", ttp);
			}
			IMoMorphType mmtAffix = null;
			foreach (IMoMorphType mmt in m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				if (mmt.Guid.ToString() == sGuidType)
				{
					mmtAffix = mmt;
					break;
				}
			}
			Debug.Assert(mmtAffix != null);
			if (!String.IsNullOrEmpty(mmtAffix.Prefix))
				bldr.Replace(0, 0, mmtAffix.Prefix, ttp);
			if (!String.IsNullOrEmpty(mmtAffix.Postfix))
				bldr.Replace(bldr.Length, bldr.Length, mmtAffix.Postfix, ttp);
			return bldr.GetString();
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return flid == (int)MoInflAffixSlot.MoInflAffixSlotTags.kflidName;
		}
		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public List<int> Affixes
		{
			get
			{
				// JohnT: was as follows, but my version is simpler and more efficient.
				//				string qry = String.Format("select msa.Id "
				//					+ "from MoInflAffMsa msa "
				//					+ "JOIN MoInflAffMsa_Slots slots ON slots.Src = msa.Id "
				//					+ "and slots.Dst = {0}",
				//					Hvo);
				string qry = String.Format("select src from MoInflAffMsa_Slots slots "
					+ "where Dst = {0}", Hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// <summary>
		/// Get a list of inflectional affix LexEntries which do not already refer to this slot
		/// </summary>
		public List<int> OtherInflectionalAffixLexEntries
		{
			get
			{
				string qry = String.Format("SELECT le.Id "
					+ "FROM LexEntry le "
					+ "JOIN MoInflAffMsa_ msa ON le.Id = msa.Owner$ "
					+ "WHERE NOT le.Id in ("
					+ "SELECT leInner.Id "
					+ "FROM LexEntry leInner "
					+ "JOIN MoInflAffMsa_ msa ON leInner.Id = msa.Owner$ "
					+ "JOIN MoInflAffMsa_Slots slots ON slots.Src = msa.Id "
					+ "and slots.Dst = {0}) ", this.Hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}
	}

	/// <summary></summary>
	public partial class MoInflAffixTemplate
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSlots:
					// Enhance JohnT: if we really need an implementation for this class and flid, see
					// the corresponding case in ReferenceTargetCandidates. I _think_ it should return
					// the most remote owner that is a PartOfSpeech.
					return null;
				case (int)MoInflAffixTemplate.MoInflAffixTemplateTags.kflidPrefixSlots:
				case (int)MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSuffixSlots:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int) MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSlots:
					set = GetAllSlots();
					break;
				case (int) MoInflAffixTemplate.MoInflAffixTemplateTags.kflidPrefixSlots:
					set = GetPrefixSlots();
					break;
					// Fall through.
				case (int) MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSuffixSlots:
					// TODO RandyR: Needs to be smarter about using only prefixes/suffixes.
					// The problem is how to tell which they are.
					set = GetSuffixSlots();
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}
		private Set<int> GetPrefixSlots()
		{
			Set<int> set = GetSomeSlots(Cache, GetAllSlots(), true);
			return set;
		}
		private Set<int> GetSuffixSlots()
		{
			Set<int> set = GetSomeSlots(Cache, GetAllSlots(),false);
			return set;
		}
		/// <summary>
		/// Get a set of inflectional affix slots which can be prefixal or suffixal
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="allSlots">Original set of all slots</param>
		/// <param name="fLookForPrefixes">whether to look for prefixal slots</param>
		/// <returns>subset of slots that are either prefixal or suffixal</returns>
		public static Set<int> GetSomeSlots(FdoCache cache, Set<int> allSlots, bool fLookForPrefixes)
		{
			Set<int> set = new Set<int>();
			//Set<int> allSlots = GetAllSlots();
			foreach (int hvoSlot in allSlots)
			{
				MoInflAffixSlot slot = CmObject.CreateFromDBObject(cache, hvoSlot) as MoInflAffixSlot;
				if (slot == null)
					continue;
				bool fStopLooking = false;
				List<int> hvosAffixes = slot.Affixes;
				if (hvosAffixes.Count == 0)
				{ // no affixes in this slot, so include it
					set.Add(hvoSlot);
					continue;
				}
				foreach (int hvoMsa in hvosAffixes)
				{
					int hvoLex = cache.GetOwnerOfObject(hvoMsa);
					LexEntry lex = CmObject.CreateFromDBObject(cache, hvoLex) as LexEntry;
					List<IMoMorphType> morphTypes = lex.MorphTypes;
					foreach (IMoMorphType morphType in morphTypes)
					{
						bool fIsCorrectType;
						if (fLookForPrefixes)
							fIsCorrectType = MoMorphType.IsPrefixishType(cache, morphType.Hvo);
						else
							fIsCorrectType = MoMorphType.IsSuffixishType(cache, morphType.Hvo);
						if (fIsCorrectType)
						{
							set.Add(hvoSlot);
							fStopLooking = true;
							break;
						}
					}
					if (fStopLooking)
						break;
				}
			}
			return set;
		}

		private Set<int> GetAllSlots()
		{
			Set<int> set;
			string qry = String.Format("declare @uid uniqueidentifier; "
									   + "exec GetOwnershipPath$ @uid output, {0}, -1; "
									   + "select ias.Id from MoInflAffixSlot_ ias "
									   + "join ObjInfoTbl$ oit "
									   + "On oit.ObjId=ias.Owner$ "
									   + "where oit.uid=@uid and oit.ObjClass={1}; "
									   + "exec CleanObjInfoTbl$ @uid; ",
									   OwnerHVO, PartOfSpeech.kClassId);
			set = new Set<int>(DbOps.ReadIntsFromCommand(m_cache, qry, null));
			return set;
		}

		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		public override void InitNewInternal()
		{
			Final = true; // Default value is true
		}

		/// <summary>
		/// Copy contents of this object to another one
		/// </summary>
		/// <param name="objNew">target object</param>
		public override void CopyTo(ICmObject objNew)
		{
			base.CopyTo(objNew);
			IMoInflAffixTemplate template = objNew as IMoInflAffixTemplate;
			if (template == null)
			{
				throw new ApplicationException("Failed to copy inflectional affix template:  the target is not an inflectional affix template.");
			}
			// All is valid so now copy the fields
			template.Name.CopyAlternatives(Name);
			template.Description.CopyAlternatives(Description);
			template.Final = Final;
			foreach (int hvo in PrefixSlotsRS.HvoArray)
			{
				template.PrefixSlotsRS.Append(hvo);
			}
			foreach (int hvo in SuffixSlotsRS.HvoArray)
			{
				template.SuffixSlotsRS.Append(hvo);
			}
			try
			{
				foreach (int hvo in SlotsRS.HvoArray)
				{
					template.SlotsRS.Append(Hvo);
				}
			}
			catch(Exception e)
			{
				Trace.WriteLine("Copying MoInflAffixTemplate.SlotsRS failed; probably ancient, bad data (TestLangProj has this): " + e.Message);
			}

			if (RegionOA != null)
			{
				template.RegionOA = new FsFeatStruc();
				RegionOA.CopyTo(template.RegionOA);
			}
			if (StratumRA != null)
			{
				template.StratumRA = new MoStratum();
				StratumRA.CopyTo(template.StratumRA);
			}
		}


	}

	///<summary>
	///
	///</summary>
	public partial class MoMorphAdhocProhib
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				foreach (MoMorphSynAnalysis msa in this.RestOfMorphsRS)
				{
					//sb.Append(msa.ShortName);  short name is too short for some msas (just a category, e.g.)
					sb.Append(msa.LongName);
					sb.Append(" ");
				}
				sb.Append("/ ");
				if (this.FirstMorphemeRA != null)
					sb.Append(this.FirstMorphemeRA.LongName);
				return sb.ToString();
			}
		}
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidRestOfMorphs:
				case (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidFirstMorpheme:
					// JohnT: I can't think of any plausible object to edit.
					return null;
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidRestOfMorphs:
				// fall through
				case (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidFirstMorpheme:
					set = new Set<int>(DbOps.ReadIntsFromCommand(m_cache,
						// Get only MSAs owned by LexEntry objects.
						"SELECT msa.[ID] "
						+ "FROM MoMorphSynAnalysis msa "
						+ "JOIN CmObject objMsa ON objMsa.[Id] = msa.[Id] "
						+ "JOIN LexEntry le ON le.[Id] = objMsa.Owner$",
						null));
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return ((flid == (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidFirstMorpheme)
				|| (flid == (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidRestOfMorphs)
				);
		}
		/// <summary>
		/// Override the inherited method to check that there are at least two allomorphs referred to
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>true, if there are at least two morphemes, otherwise false.</returns>
		public override bool CheckConstraints(int flidToCheck, out ConstraintFailure failure)
		{
			CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_cache, Hvo);
			failure = null;
			bool isValid = (FirstMorphemeRA != null) && (RestOfMorphsRS.Count >= 1);
			if (!isValid)
			{
				failure = new ConstraintFailure(this,
					(int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidMorphemes,
					Strings.ksMorphConstraintFailure);
				return false;

				//				CmBaseAnnotation ann = (CmBaseAnnotation)m_cache.LangProject.AnnotationsOC.Add(new CmBaseAnnotation());
				//				ann.CompDetails = "Need to have at least two morphemes chosen";
				//				ann.InstanceOfRAHvo = Hvo;
				//				ann.BeginObjectRA = this;
				//				ann.Flid = (int)BaseMoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidMorphemes;
				// REVIEW JohnH(AndyB): Does this need an agent or type?
				// ann.SourceRAHvo = m_agentId;
			}
			return isValid;
		}

		/// <summary>
		/// Delete the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Trigger cleaning up any unused Msas after this object is deleted.
			this.HandleOldMSAs(objectsToDeleteAlso);
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Override the base property to deal with deleting MSAs.
		/// </summary>
		public int FirstMorphemeRAHvo
		{
			get
			{
				return FirstMorphemeRAHvo_Generated;
			}
			set
			{
				if (FirstMorphemeRAHvo != value)
				{
					// Only mess with it, if it is a different value
					// since HandleOldFirstMorpheme() may delete it, which will cause a crash
					// when it tries to get set.
					HandleOldFirstMorpheme(null);
					FirstMorphemeRAHvo_Generated = value;
				}
			}
		}

		/// <summary>
		/// Override the base property to deal with deleting MSAs.
		/// </summary>
		public IMoMorphSynAnalysis FirstMorphemeRA
		{
			get
			{
				return FirstMorphemeRA_Generated;
			}
			set
			{
				int newHvo = (value == null) ? 0 : value.Hvo;
				if (FirstMorphemeRAHvo != newHvo)
				{
					// Only mess with it, if it is a different value
					// since HandleOldFirstMorpheme() may delete it, which will cause a crash
					// when it tries to get set.
					HandleOldFirstMorpheme(null);
					FirstMorphemeRA_Generated = value;
				}
			}
		}

		/// <summary>
		/// Delete original MSA, if permitted.
		/// </summary>
		private void HandleOldFirstMorpheme(Set<int> objectsToDeleteAlso)
		{
			IMoMorphSynAnalysis oldMsa = FirstMorphemeRA;
			if (oldMsa != null)
			{
				FirstMorphemeRA_Generated = null;
				if (oldMsa.CanDelete)
				{
					if (objectsToDeleteAlso == null)
						oldMsa.DeleteUnderlyingObject();
					else if (oldMsa.Hvo != 0 && !objectsToDeleteAlso.Contains(oldMsa.Hvo))
						objectsToDeleteAlso.Add(oldMsa.Hvo);
				}
			}
		}

		/// <summary>
		/// Delete original MSAs in FirstMorpheme, Morphemes, and RestOfMorphs, if permitted.
		/// </summary>
		private void HandleOldMSAs(Set<int> objectsToDeleteAlso)
		{
			// FirstMorpheme
			this.HandleOldFirstMorpheme(objectsToDeleteAlso);

			// Morphemes
			while (this.MorphemesRS.Count > 0)
			{
				IMoMorphSynAnalysis oldMsa = MorphemesRS[0];
				MorphemesRS.RemoveAt(0);
				if (oldMsa.CanDelete)
				{
					if (objectsToDeleteAlso == null)
						oldMsa.DeleteUnderlyingObject();
					else if (oldMsa.Hvo != 0 && !objectsToDeleteAlso.Contains(oldMsa.Hvo))
						objectsToDeleteAlso.Add(oldMsa.Hvo);
				}
			}

			// RestOfMorphs
			while (this.RestOfMorphsRS.Count > 0)
			{
				IMoMorphSynAnalysis oldMsa = RestOfMorphsRS[0];
				RestOfMorphsRS.RemoveAt(0);
				if (oldMsa.CanDelete)
				{
					if (objectsToDeleteAlso == null)
						oldMsa.DeleteUnderlyingObject();
					else if (oldMsa.Hvo != 0 && !objectsToDeleteAlso.Contains(oldMsa.Hvo))
						objectsToDeleteAlso.Add(oldMsa.Hvo);
				}
			}
		}
	}

	///<summary>
	///
	///</summary>
	public partial class MoAlloAdhocProhib
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// Get a string used to to represent any MSA as a reference target.
		/// </summary>
		public override string ShortName
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				foreach (IMoForm mf in this.RestOfAllosRS)
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

		/// <summary>
		/// The writing system for sorting a list of ShortNames: vernacular.
		/// </summary>
		public override string SortKeyWs
		{
			get
			{
				string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(
					m_cache.DefaultVernWs);

				if (sWs == null || sWs == string.Empty)
					sWs = m_cache.FallbackUserLocale;

				if (sWs == null || sWs == "")
					sWs = "en";
				return sWs;
			}
		}
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidRestOfAllos:
				case (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidFirstAllomorph:
					// JohnT: I can't think of any plausible object to edit.
					return null;
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidRestOfAllos:
				// fall through
				case (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidFirstAllomorph:
					string qry = String.Format("SELECT ID FROM MoForm_ WHERE (OwnFlid$={0} OR OwnFlid$={1}) AND IsAbstract=0",
						((int)LexEntry.LexEntryTags.kflidAlternateForms).ToString(),
						((int)LexEntry.LexEntryTags.kflidLexemeForm).ToString());
					set = new Set<int>(DbOps.ReadIntsFromCommand(m_cache, qry, null));
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return ((flid == (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidFirstAllomorph)
				|| (flid == (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidRestOfAllos)
				);
		}

		/// <summary>
		/// Override the inherited method to check that there are at least two allomorphs referred to
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>true, if there are at least two allomorphs, otherwise false.</returns>
		public override bool CheckConstraints(int flidToCheck, out ConstraintFailure failure)
		{
			CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_cache, Hvo);

			bool isValid = (FirstAllomorphRA != null) && (RestOfAllosRS.Count >= 1);
			if (!isValid)
			{
				failure = new ConstraintFailure(this, (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidAllomorphs,
					Strings.ksAlloConstraintFailure);
				//
				//				CmBaseAnnotation ann = (CmBaseAnnotation)m_cache.LangProject.AnnotationsOC.Add(new CmBaseAnnotation());
				//				ann.CompDetails = "Need to have at least two allomorphs selected";
				//				ann.InstanceOfRAHvo = Hvo;
				//				ann.BeginObjectRA = this;
				//				ann.Flid = (int)BaseMoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidAllomorphs;
				//				obj = ann;
				// REVIEW JohnH(AndyB): Does this need an agent or type?
				// ann.SourceRAHvo = m_agentId;

				return false;
			}
			else
			{
				failure = null;
				return true;
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class MoGlossItem
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		/// <summary>
		/// Gloss Item Type enumeration: the type of the gloss item.
		/// </summary>
		public enum ItemType
		{
			/// <summary>The item's type is unknown (should not happen).</summary>
			unknown = 0,
			/// <summary>The item corresponds to a feature structure type.</summary>
			fsType,
			/// <summary>The item corresponds to an inflectional feature.</summary>
			feature,
			/// <summary>The item corresponds to an inflectional feature value. </summary>
			inflValue,
			/// <summary>The item corresponds to a complex feature that embeds another feature structure. </summary>
			complex,
			/// <summary>The item is the gloss for a derivational morpheme. </summary>
			deriv,
			/// <summary>The item is in the list only for the purpose of organization and grouping. </summary>
			group,
			/// <summary>The item is a cross-reference to the term that is used in preference to this one.</summary>
			xref,
		}

		///<summary>
		///Recursively search for an item embedded within a MoGlossItem.  If not found, the result is null.
		///</summary>
		///<param name="sName">Name attribute to look for (in default analysis writing system)</param>
		///<param name="sAbbreviation">Abbreviation attribute to look for (in default analysis writing system)</param>
		///
		public IMoGlossItem FindEmbeddedItem(string sName, string sAbbreviation)
		{
			return FindEmbeddedItem(sName, sAbbreviation, true);
		}
		///<summary>
		///Recursively search for an item embedded within a MoGlossItem.  If not found, the result is null.
		///</summary>
		///<param name="sName">Name attribute to look for (in default analysis writing system)</param>
		///<param name="sAbbreviation">Abbreviation attribute to look for (in default analysis writing system)</param>
		///<param name="fRecurse">Recurse through embedded layers</param>
		///
		public IMoGlossItem FindEmbeddedItem(string sName, string sAbbreviation, bool fRecurse)
		{
			IMoGlossItem giFound = null;
			foreach (IMoGlossItem gi in GlossItemsOS)
			{
				if (gi.Name.AnalysisDefaultWritingSystem == sName &&
					gi.Abbreviation.AnalysisDefaultWritingSystem == sAbbreviation)
				{
					giFound = gi;
					break;
				}
				if (fRecurse)
				{
					giFound = gi.FindEmbeddedItem(sName, sAbbreviation);
					if (giFound != null)
						break;
				}
			}
			return giFound;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class MoGlossSystem
	{
		// TODO RandyR: Add ShortNameTSS and DeletionTextTSS. DeleteObjectSideEffects().

		///<summary>
		///Recursively search for an item embedded within a MoGlossSystem.  If not found, the result is null.
		///</summary>
		///<param name="sName">Name attribute to look for (in default analysis writing system)</param>
		///<param name="sAbbreviation">Abbreviation attribute to look for (in default analysis writing system)</param>
		///
		public IMoGlossItem FindEmbeddedItem(string sName, string sAbbreviation)
		{
			return FindEmbeddedItem(sName, sAbbreviation, true);
		}
		///<summary>
		///Recursively search for an item embedded within a MoGlossSystem.  If not found, the result is null.
		///</summary>
		///<param name="sName">Name attribute to look for (in default analysis writing system)</param>
		///<param name="sAbbreviation">Abbreviation attribute to look for (in default analysis writing system)</param>
		///<param name="fRecurse">Recurse through embedded layers</param>
		///
		public IMoGlossItem FindEmbeddedItem(string sName, string sAbbreviation, bool fRecurse)
		{
			IMoGlossItem giFound = null;
			foreach (IMoGlossItem gi in GlossesOC)
			{
				if (gi.Name.AnalysisDefaultWritingSystem == sName &&
					gi.Abbreviation.AnalysisDefaultWritingSystem == sAbbreviation)
				{
					giFound = gi;
					break;
				}
				if (fRecurse)
				{
					giFound = gi.FindEmbeddedItem(sName, sAbbreviation);
					if (giFound != null)
						break;
				}
			}
			return giFound;
		}

	}

	/// <summary>
	/// Override auto-generated class.
	/// </summary>
	public partial class MoMorphType : IComparable
	{
		// These are fixed GUIDS for possibility lists and items that are guaranteed to remain
		// in the database and can thus be used by application code.
		//
		// It might be better to define these as Guids instead of strings, but C# won't let us
		// do that since Guids are objects, which require a constructor and can't be const.
		// We're lucky that strings can be initialized const without an explicit constructor!
		// Also, strings can be used in case statements, but Guids cannot, so it's easier to
		// to convert the guid variable to a string first when a case statement is appropriate.
		// The hex chars A-F are given in lowercase here because C#'s Guid.ToString() method
		// is defined to produce lowercase hex digit chars.
		//
		/// <summary>Major Entry Types Possibility List</summary>
		public const string kguidMorphTypes = "d7f713d8-e8cf-11d3-9764-00c04f186933";
		/// <summary>Bound Root item in Morph Types list</summary>
		public const string kguidMorphBoundRoot = "d7f713e4-e8cf-11d3-9764-00c04f186933";
		/// <summary>Bound Stem item in Morph Types list</summary>
		public const string kguidMorphBoundStem = "d7f713e7-e8cf-11d3-9764-00c04f186933";
		/// <summary>Circumfix item in Morph Types list</summary>
		public const string kguidMorphCircumfix = "d7f713df-e8cf-11d3-9764-00c04f186933";
		/// <summary>Clitic item in Morph Types list</summary>
		public const string kguidMorphClitic = "c2d140e5-7ca9-41f4-a69a-22fc7049dd2c";
		/// <summary>Enclitic item in Morph Types list</summary>
		public const string kguidMorphEnclitic = "d7f713e1-e8cf-11d3-9764-00c04f186933";
		/// <summary>Infix item in Morph Types list</summary>
		public const string kguidMorphInfix = "d7f713da-e8cf-11d3-9764-00c04f186933";
		/// <summary>Particle item in Morph Types list</summary>
		public const string kguidMorphParticle = "56db04bf-3d58-44cc-b292-4c8aa68538f4";
		/// <summary>Prefix item in Morph Types list</summary>
		public const string kguidMorphPrefix = "d7f713db-e8cf-11d3-9764-00c04f186933";
		/// <summary>Proclitic item in Morph Types list</summary>
		public const string kguidMorphProclitic = "d7f713e2-e8cf-11d3-9764-00c04f186933";
		/// <summary>Root item in Morph Types list</summary>
		public const string kguidMorphRoot = "d7f713e5-e8cf-11d3-9764-00c04f186933";
		/// <summary>Simulfix item in Morph Types list</summary>
		public const string kguidMorphSimulfix = "d7f713dc-e8cf-11d3-9764-00c04f186933";
		/// <summary>Stem item in Morph Types list</summary>
		public const string kguidMorphStem = "d7f713e8-e8cf-11d3-9764-00c04f186933";
		/// <summary>Suffix item in Morph Types list</summary>
		public const string kguidMorphSuffix = "d7f713dd-e8cf-11d3-9764-00c04f186933";
		/// <summary>Suprafix item in Morph Types list</summary>
		public const string kguidMorphSuprafix = "d7f713de-e8cf-11d3-9764-00c04f186933";
		/// <summary>Infixing Interfix item in Morph Types list</summary>
		public const string kguidMorphInfixingInterfix = "18d9b1c3-b5b6-4c07-b92c-2fe1d2281bd4";
		/// <summary>Prefixing Interfix item in Morph Types list</summary>
		public const string kguidMorphPrefixingInterfix = "af6537b0-7175-4387-ba6a-36547d37fb13";
		/// <summary>Suffixing Interfix item in Morph Types list</summary>
		public const string kguidMorphSuffixingInterfix = "3433683d-08a9-4bae-ae53-2a7798f64068";
		/// <summary>Phrase item in Morph Types list</summary>
		public const string kguidMorphPhrase = "a23b6faa-1052-4f4d-984b-4b338bdaf95f";
		/// <summary>Discontiguous phrase item in Morph Types list</summary>
		public const string kguidMorphDiscontiguousPhrase = "0cc8c35a-cee9-434d-be58-5d29130fba5b";

		/// <summary>
		/// Index for an unknown type.
		/// </summary>
		public const int kmtUnknown = -2;
		/// <summary>
		/// Index for a mixed type.
		/// </summary>
		public const int kmtMixed = -1;
		/// <summary>
		/// Index for a bound root.
		/// </summary>
		public const int kmtBoundRoot = 0;
		/// <summary>
		/// Index for a bound stem.
		/// </summary>
		public const int kmtBoundStem = 1;
		/// <summary>
		/// Index for a circumfix.
		/// </summary>
		public const int kmtCircumfix = 2;
		/// <summary>
		/// Index for an enclitic.
		/// </summary>
		public const int kmtEnclitic = 3;
		/// <summary>
		/// Index for an infix.
		/// </summary>
		public const int kmtInfix = 4;
		/// <summary>
		/// Index for a particle.
		/// </summary>
		public const int kmtParticle = 5;
		/// <summary>
		/// Index for a prefix.
		/// </summary>
		public const int kmtPrefix = 6;
		/// <summary>
		/// Index for a proclitic.
		/// </summary>
		public const int kmtProclitic = 7;
		/// <summary>
		/// Index for a root.
		/// </summary>
		public const int kmtRoot = 8;
		/// <summary>
		/// Index for a simulfix.
		/// </summary>
		public const int kmtSimulfix = 9;
		/// <summary>
		/// Index for a stem.
		/// </summary>
		public const int kmtStem = 10;
		/// <summary>
		/// Index for a suffix.
		/// </summary>
		public const int kmtSuffix = 11;
		/// <summary>
		/// Index for a suprafix.
		/// </summary>
		public const int kmtSuprafix = 12;
		/// <summary>
		/// Index for an infixing interfix.
		/// </summary>
		public const int kmtInfixingInterfix = 13;
		/// <summary>
		/// Index for an prefixing interfix.
		/// </summary>
		public const int kmtPrefixingInterfix = 14;
		/// <summary>
		/// Index for an suffixing interfix.
		/// </summary>
		public const int kmtSuffixingInterfix = 15;
		/// <summary>
		/// Index for a phrase.
		/// </summary>
		public const int kmtPhrase = 16;
		/// <summary>
		/// Index for a discontiguous phrase.
		/// </summary>
		public const int kmtDiscontiguousPhrase = 17;
		/// <summary>
		/// Index for a clitic.
		/// </summary>
		public const int kmtClitic = 18;

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
		/// Get a form with prefix and suffix markers, if any, for the given form.
		/// </summary>
		/// <param name="form">A string with prefix and suffix markers, if any.</param>
		/// <returns></returns>
		public string FormWithMarkers(string form)
		{
			string pfx = (Prefix == null || Prefix.Length == 0) ? "" : Prefix;
			string pstfx = (Postfix == null || Postfix.Length == 0) ? "" : Postfix;
			return pfx + MoForm.EnsureNoMarkers(form, m_cache) + pstfx;
		}

		/// <summary>
		/// Checks two morph types objects to see if they are ambiguous,
		/// regarding the markers used to type them.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="types">Collection of morph types.</param>
		/// <param name="first">First morph type to compare</param>
		/// <param name="second">Second morph type to compare</param>
		/// <returns>True, if the two morph types are ambiguous, otherwise false.</returns>
		public bool IsAmbiguousWith(FdoCache cache, MoMorphTypeCollection types,
			IMoMorphType first, IMoMorphType second)
		{
			// Debug.Assert(types != null); JohnT removed assert as argument is not used and new caller does not provide
			Debug.Assert(first != null);
			Debug.Assert(second != null);
			int idxFirst = FindMorphTypeIndex(cache, first);
			int idxSecond = FindMorphTypeIndex(cache, second);
			bool areAmbiguous = false;

			switch (idxFirst)
			{
				case kmtCircumfix:
				case kmtRoot:
				case kmtProclitic:
				case kmtClitic:
				case kmtParticle:
				case kmtEnclitic:
				case kmtStem:
				case kmtPhrase:
				case kmtDiscontiguousPhrase:
					if (idxSecond != idxFirst)
						areAmbiguous = (idxSecond == kmtCircumfix)
							|| (idxSecond == kmtRoot)
							|| (idxSecond == kmtClitic)
							|| (idxSecond == kmtProclitic)
							|| (idxSecond == kmtParticle)
							|| (idxSecond == kmtEnclitic)
							|| (idxSecond == kmtStem)
							|| (idxSecond == kmtPhrase)
							|| (idxSecond == kmtDiscontiguousPhrase);
					break;
				case kmtBoundStem:
					areAmbiguous = (idxSecond == kmtBoundRoot);
					break;
				case kmtBoundRoot:
					areAmbiguous = (idxSecond == kmtBoundStem);
					break;
				case kmtInfix:
					areAmbiguous = (idxSecond == kmtInfixingInterfix);
					break;
				case kmtInfixingInterfix:
					areAmbiguous = (idxSecond == kmtInfix);
					break;
				case kmtPrefix:
					areAmbiguous = (idxSecond == kmtPrefixingInterfix);
					break;
				case kmtPrefixingInterfix:
					areAmbiguous = (idxSecond == kmtPrefix);
					break;
				case kmtSuffix:
					areAmbiguous = (idxSecond == kmtSuffixingInterfix);
					break;
				case kmtSuffixingInterfix:
					areAmbiguous = (idxSecond == kmtSuffix);
					break;
				default:
					break;
			}

			return areAmbiguous;
		}

		/// <summary>
		/// Return a list of the Hvos of all the morph types that are ambiguous with the input.
		/// </summary>
		/// <returns></returns>
		public List<int> AmbiguousTypes
		{
			get
			{
				List<int> result = new List<int>();
				result.Add(Hvo);
				foreach (IMoMorphType mmt in Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
				{
					if (mmt.Hvo != Hvo && IsAmbiguousWith(Cache, null, this, mmt))
						result.Add(mmt.Hvo);
				}
				return result;
			}
		}

		/// <summary>
		///  Get the enumeration MorphType for the given MoMorphType object.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mmt"></param>
		/// <returns></returns>
		public static int FindMorphTypeIndex(FdoCache cache, IMoMorphType mmt)
		{
			Debug.Assert(cache != null);
			if (mmt == null)
				return kmtUnknown;
			//Debug.Assert(mmt != null); // JT: can happen from MorphType routine if Morph has no type set.

			int mtIdx;
			switch (mmt.Guid.ToString())
			{
				case MoMorphType.kguidMorphBoundRoot:
					mtIdx = kmtBoundRoot;
					break;
				case MoMorphType.kguidMorphBoundStem:
					mtIdx = kmtBoundStem;
					break;
				case MoMorphType.kguidMorphCircumfix:
					mtIdx = kmtCircumfix;
					break;
				case MoMorphType.kguidMorphClitic:
					mtIdx = kmtClitic;
					break;
				case MoMorphType.kguidMorphEnclitic:
					mtIdx = kmtEnclitic;
					break;
				case MoMorphType.kguidMorphInfix:
					mtIdx = kmtInfix;
					break;
				case MoMorphType.kguidMorphParticle:
					mtIdx = kmtParticle;
					break;
				case MoMorphType.kguidMorphPrefix:
					mtIdx = kmtPrefix;
					break;
				case MoMorphType.kguidMorphProclitic:
					mtIdx = kmtProclitic;
					break;
				case MoMorphType.kguidMorphRoot:
					mtIdx = kmtRoot;
					break;
				case MoMorphType.kguidMorphSimulfix:
					mtIdx = kmtSimulfix;
					break;
				case MoMorphType.kguidMorphStem:
					mtIdx = kmtStem;
					break;
				case MoMorphType.kguidMorphSuffix:
					mtIdx = kmtSuffix;
					break;
				case MoMorphType.kguidMorphSuprafix:
					mtIdx = kmtSuprafix;
					break;
				case MoMorphType.kguidMorphInfixingInterfix:
					mtIdx = kmtInfixingInterfix;
					break;
				case MoMorphType.kguidMorphPrefixingInterfix:
					mtIdx = kmtPrefixingInterfix;
					break;
				case MoMorphType.kguidMorphSuffixingInterfix:
					mtIdx = kmtSuffixingInterfix;
					break;
				case MoMorphType.kguidMorphPhrase:
					mtIdx = kmtPhrase;
					break;
				case MoMorphType.kguidMorphDiscontiguousPhrase:
					mtIdx = kmtDiscontiguousPhrase;
					break;
				default:
					throw new ArgumentException("Unrecognized morph type Guid.", "mmt");
			}
			return mtIdx;
		}

		/// <summary>
		/// Get the MoMorphType objects for the major types.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mmtStem"></param>
		/// <param name="mmtPrefix"></param>
		/// <param name="mmtSuffix"></param>
		/// <param name="mmtInfix"></param>
		/// <param name="mmtBoundStem"></param>
		/// <param name="mmtProclitic"></param>
		/// <param name="mmtEnclitic"></param>
		/// <param name="mmtSimulfix"></param>
		/// <param name="mmtSuprafix"></param>
		public static void GetMajorMorphTypes(FdoCache cache, out IMoMorphType mmtStem, out IMoMorphType mmtPrefix,
			out IMoMorphType mmtSuffix, out IMoMorphType mmtInfix, out IMoMorphType mmtBoundStem, out IMoMorphType mmtProclitic,
			out IMoMorphType mmtEnclitic, out IMoMorphType mmtSimulfix, out IMoMorphType mmtSuprafix)
		{
			mmtStem = null;
			mmtPrefix = null;
			mmtSuffix = null;
			mmtInfix = null;
			mmtBoundStem = null;
			mmtProclitic = null;
			mmtEnclitic = null;
			mmtSimulfix = null;
			mmtSuprafix = null;

			foreach (IMoMorphType mmt in cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				switch (mmt.Guid.ToString())
				{
					case MoMorphType.kguidMorphStem:
						mmtStem = mmt;
						break;
					case MoMorphType.kguidMorphPrefix:
						mmtPrefix = mmt;
						break;
					case MoMorphType.kguidMorphSuffix:
						mmtSuffix = mmt;
						break;
					case MoMorphType.kguidMorphInfix:
						mmtInfix = mmt;
						break;
					case MoMorphType.kguidMorphBoundStem:
						mmtBoundStem = mmt;
						break;
					case MoMorphType.kguidMorphProclitic:
						mmtProclitic = mmt;
						break;
					case MoMorphType.kguidMorphEnclitic:
						mmtEnclitic = mmt;
						break;
					case MoMorphType.kguidMorphSimulfix:
						mmtSimulfix = mmt;
						break;
					case MoMorphType.kguidMorphSuprafix:
						mmtSuprafix = mmt;
						break;
				}
			}
		}
		/// <summary>
		/// Get the MoMorphType objects for the major affix types.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mmtPrefix"></param>
		/// <param name="mmtSuffix"></param>
		/// <param name="mmtInfix"></param>
		public static void GetMajorAffixMorphTypes(FdoCache cache, out IMoMorphType mmtPrefix,
			out IMoMorphType mmtSuffix, out IMoMorphType mmtInfix)
		{
			mmtPrefix = null;
			mmtSuffix = null;
			mmtInfix = null;

			foreach (IMoMorphType mmt in cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				switch (mmt.Guid.ToString())
				{
					case MoMorphType.kguidMorphPrefix:
						mmtPrefix = mmt;
						break;
					case MoMorphType.kguidMorphSuffix:
						mmtSuffix = mmt;
						break;
					case MoMorphType.kguidMorphInfix:
						mmtInfix = mmt;
						break;
				}
			}
		}

		/// <summary>
		/// Determine whether the given object is an affix type morph type.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoMorphType"></param>
		/// <returns></returns>
		public static bool IsAffixType(FdoCache cache, int hvoMorphType)
		{
			Guid guid = cache.GetGuidFromId(hvoMorphType);
			switch (guid.ToString())
			{
				case kguidMorphCircumfix:
				case kguidMorphInfix:
				case kguidMorphPrefix:
				case kguidMorphSimulfix:
				case kguidMorphSuffix:
				case kguidMorphSuprafix:
				case kguidMorphInfixingInterfix:
				case kguidMorphPrefixingInterfix:
				case kguidMorphSuffixingInterfix:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Determine whether the given index value refers to an affix type morph type.
		/// </summary>
		/// <param name="idxMorphType"></param>
		/// <returns></returns>
		public static bool IsAffixType(int idxMorphType)
		{
			switch (idxMorphType)
			{
				case kmtCircumfix:
				case kmtInfix:
				case kmtPrefix:
				case kmtSimulfix:
				case kmtSuffix:
				case kmtSuprafix:
				case kmtInfixingInterfix:
				case kmtPrefixingInterfix:
				case kmtSuffixingInterfix:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Determine whether the given object is a "prefix-ish" morph type.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoMorphType"></param>
		/// <returns></returns>
		public static bool IsPrefixishType(FdoCache cache, int hvoMorphType)
		{
			Guid guid = cache.GetGuidFromId(hvoMorphType);
			switch (guid.ToString())
			{
				case kguidMorphCircumfix:
				case kguidMorphInfix:
				case kguidMorphPrefix:
				case kguidMorphInfixingInterfix:
				case kguidMorphPrefixingInterfix:
					return true;
				default:
					return false;
			}
		}
		/// <summary>
		/// Determine whether the given object is a "suffix-ish" morph type.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoMorphType"></param>
		/// <returns></returns>
		public static bool IsSuffixishType(FdoCache cache, int hvoMorphType)
		{
			Guid guid = cache.GetGuidFromId(hvoMorphType);
			switch (guid.ToString())
			{
				case kguidMorphSuffix:
				case kguidMorphSuffixingInterfix:
					return true;
				default:
					return false;
			}
		}
		/// <summary>
		/// Get the morph type and class ID for the given input string.
		/// </summary>
		/// <param name="cache">The cache to look in.</param>
		/// <param name="types">Collection of all of the MoMorphType objects.</param>
		/// <param name="fullForm">The MoForm form, plus optional key characters before and/or after the form.</param>
		/// <param name="clsidForm">Return the clsid for the form.</param>
		/// <returns>The MoMorphType indicated by the possible markers.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown in the following cases:
		/// 1. The input form is an empty string,
		/// 2. The imput form is improperly marked according to the current settings of the
		///		MoMorphType objects.
		/// </exception>
		public static IMoMorphType FindMorphType(FdoCache cache, MoMorphTypeCollection types,
			ref string fullForm, out int clsidForm)
		{
			Debug.Assert(cache != null);
			Debug.Assert(fullForm != null);

			clsidForm = MoStemAllomorph.kclsidMoStemAllomorph;	// default
			IMoMorphType mt = null;
			fullForm = fullForm.Trim();
			if (fullForm.Length == 0)
				throw new ArgumentException("The form is empty.", "fullForm");

			string sLeading;
			string sTrailing;
			GetAffixMarkers(cache, fullForm, out sLeading, out sTrailing);

			/*
			 Not dealt with.
			 particle	(ambiguous: particle, circumfix, root, stem)
			 circumfix	(ambiguous: particle, circumfix, root, stem)
			 root		(ambiguous: particle, circumfix, root, stem)
			 bound root	(ambiguous: bound root, bound stem)
			 infixing interfix	(ambiguous: infixing interfix, infix)
			 prefixing interfix	(ambiguous: prefixing interfix, prefix)
			 suffixing interfix	(ambiguous: suffixing interfix, suffix)
			 End of not dealt with.

			What we do deal with.
			 prefix-	(ambiguous: prefixing interfix, prefix)
			=simulfix=
			-suffix		(ambiguous: suffixing interfix, suffix)
			-infix-		(ambiguous: infixing interfix, infix)
			~suprafix~
			=enclitic
			 proclitic=
			*bound stem	(ambiguous: bound root, bound stem)
			 stem		(ambiguous: particle, circumfix, root, stem)
			End of what we do deal with.

			For ambiguous cases, pick 'root' and 'bound root', as per LarryH's suggestion on 11/18/2003.
			(Changed: May, 2004). For ambiguous cases, pick 'stem' and 'bound stem',
			as per WordWorks May, 2004 meeting (Andy Black & John Hatton).
			For ambiguous cases, pick 'infix', 'prefix', and 'suffix'.
			 */
			if (sLeading == types.Item(kmtStem).Prefix && sTrailing == types.Item(kmtStem).Postfix)
			{
				mt = types.Item(kmtStem);	// may be ambiguous with particle, root, and circumfix
			}
			else if (sLeading == types.Item(kmtPrefix).Prefix && sTrailing == types.Item(kmtPrefix).Postfix)
			{
				mt = types.Item(kmtPrefix); // may be ambiguous with prefixing interfix
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtInfix).Prefix && sTrailing == types.Item(kmtInfix).Postfix)
			{
				mt = types.Item(kmtInfix);	// may be ambiguous with infixing interfix
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtSuffix).Prefix && sTrailing == types.Item(kmtSuffix).Postfix)
			{
				mt = types.Item(kmtSuffix);	// may be ambiguous with suffixing interfix
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtBoundStem).Prefix && sTrailing == types.Item(kmtBoundStem).Postfix)
			{
				mt = types.Item(kmtBoundStem);	// may be ambiguous with bound root
			}
			else if (sLeading == types.Item(kmtProclitic).Prefix && sTrailing == types.Item(kmtProclitic).Postfix)
			{
				mt = types.Item(kmtProclitic);
			}
			else if (sLeading == types.Item(kmtEnclitic).Prefix && sTrailing == types.Item(kmtEnclitic).Postfix)
			{
				mt = types.Item(kmtEnclitic);
			}
			else if (sLeading == types.Item(kmtSimulfix).Prefix && sTrailing == types.Item(kmtSimulfix).Postfix)
			{
				mt = types.Item(kmtSimulfix);
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtSuprafix).Prefix && sTrailing == types.Item(kmtSuprafix).Postfix)
			{
				mt = types.Item(kmtSuprafix);
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtBoundRoot).Prefix && sTrailing == types.Item(kmtBoundRoot).Postfix)
			{
				mt = types.Item(kmtBoundRoot);
			}
			else if (sLeading == types.Item(kmtRoot).Prefix && sTrailing == types.Item(kmtRoot).Postfix)
			{
				mt = types.Item(kmtRoot);
			}
			else if (sLeading == types.Item(kmtParticle).Prefix && sTrailing == types.Item(kmtParticle).Postfix)
			{
				mt = types.Item(kmtParticle);
			}
			else if (sLeading == types.Item(kmtCircumfix).Prefix && sTrailing == types.Item(kmtCircumfix).Postfix)
			{
				mt = types.Item(kmtCircumfix);
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtPrefixingInterfix).Prefix && sTrailing == types.Item(kmtPrefixingInterfix).Postfix)
			{
				mt = types.Item(kmtPrefixingInterfix);
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtInfixingInterfix).Prefix && sTrailing == types.Item(kmtInfixingInterfix).Postfix)
			{
				mt = types.Item(kmtInfixingInterfix);
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else if (sLeading == types.Item(kmtSuffixingInterfix).Prefix && sTrailing == types.Item(kmtSuffixingInterfix).Postfix)
			{
				mt = types.Item(kmtSuffixingInterfix);
				clsidForm = MoAffixAllomorph.kclsidMoAffixAllomorph;
			}
			else
			{
				if (sLeading == null && sTrailing == null)
					throw new Exception(String.Format(Strings.ksInvalidUnmarkedForm0, fullForm));
				else if (sLeading == null)
					throw new Exception(String.Format(Strings.ksInvalidForm0Trailing1, fullForm, sTrailing));
				else if (sTrailing == null)
					throw new Exception(String.Format(Strings.ksInvalidForm0Leading1, fullForm, sLeading));
				else
					throw new Exception(String.Format(Strings.ksInvalidForm0Leading1Trailing2, fullForm, sLeading, sTrailing));
			}

			if (sLeading != null)
				fullForm = fullForm.Substring(sLeading.Length);
			if (sTrailing != null)
				fullForm = fullForm.Substring(0, fullForm.Length - sTrailing.Length);

			// Handle prhase
			if (mt.Guid.ToString().ToLower() == MoMorphType.kguidMorphStem)
			{
				if (fullForm.IndexOf(" ") != -1)
					mt = types.Item(kmtPhrase);
			}

			// Check to see if it has any of the reserved characters remaining,
			// as we have now stripped them off the ends,
			// and it is illegal to have them internal to the form.
			// (SteveMc) But is it? What about hyphenated words? Or contractions?
			// if ((MiscUtils.IndexOfAnyString(fullForm, prefixMarkers, 0, out iMatchedPrefix) > -1) ||
			// 	(MiscUtils.IndexOfAnyString(fullForm, postfixMarkers, 0, out iMatchedPrefix) > -1))
			// {
			// 	throw new Exception("\"" + fullForm + "\" is not a valid morpheme. It should not have a reserved morpheme marker within the form. Did you forget spaces?");
			// }

			return mt;
		}

		/// <summary>
		/// Return a stripped form of a (full)form which may contain affix markers.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="fullForm">string containing affix markers</param>
		/// <returns>string without the prefix and postfix markers</returns>
		static internal string StripAffixMarkers(FdoCache cache, string fullForm)
		{
			string[] prefixMarkers = MoMorphType.PrefixMarkers(cache);
			string[] postfixMarkers = MoMorphType.PostfixMarkers(cache);

			if (fullForm != null && fullForm.Length > 0)
			{
				string strippedForm = fullForm.Trim();
				int iMatchedPrefix;
				int iMatchedPostfix;
				MoMorphType.IdentifyAffixMarkers(fullForm, prefixMarkers, postfixMarkers, out iMatchedPrefix, out iMatchedPostfix);
				// cut out leading type marker
				if (iMatchedPrefix >= 0)
					strippedForm = strippedForm.Substring(prefixMarkers[iMatchedPrefix].Length);
				// cut out trailing morpheme type marker
				if (iMatchedPostfix >= 0)
				{
					strippedForm = strippedForm.Substring(0,
						strippedForm.Length - postfixMarkers[iMatchedPostfix].Length);
				}
				return strippedForm;
			}
			return "";
		}

		static private void GetAffixMarkers(FdoCache cache, string fullForm, out string prefixMarker, out string postfixMarker)
		{
			string[] prefixMarkers = MoMorphType.PrefixMarkers(cache);
			string[] postfixMarkers = MoMorphType.PostfixMarkers(cache);

			prefixMarker = null;
			postfixMarker = null;
			int iMatchedPrefix;
			int iMatchedPostfix;
			IdentifyAffixMarkers(fullForm, prefixMarkers, postfixMarkers, out iMatchedPrefix, out iMatchedPostfix);
			if (iMatchedPrefix >= 0)
				prefixMarker = prefixMarkers[iMatchedPrefix];
			if (iMatchedPostfix >= 0)
				postfixMarker = postfixMarkers[iMatchedPostfix];
		}
		static private void IdentifyAffixMarkers(string fullForm, string[] prefixMarkers, string[] postfixMarkers,
			out int iMatchedPrefix, out int iMatchedPostfix)
		{
			int ichPrefixMatch = MiscUtils.IndexOfAnyString(fullForm, prefixMarkers, out iMatchedPrefix, StringComparison.Ordinal);
			// prefix must match on first character
			if (ichPrefixMatch != 0)
				iMatchedPrefix = -1;
			int ichPostfixMatch = -1;
			iMatchedPostfix = -1;
			for (int i = 0; i < postfixMarkers.Length; ++i)
			{
				ichPostfixMatch = fullForm.LastIndexOf(postfixMarkers[i]);
				if (ichPostfixMatch > 0 && ichPostfixMatch + postfixMarkers[i].Length == fullForm.Length)
				{
					if (iMatchedPostfix == -1)
						iMatchedPostfix = i;
					else if (postfixMarkers[i].Length > postfixMarkers[iMatchedPostfix].Length)
						iMatchedPostfix = i;
				}
			}
		}

		/// <summary>
		/// Return the list of strings used to mark morphemes at the beginning. These must not occur as part
		/// of the text of a morpheme (except at the boundaries to indicate its type).
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static string[] PrefixMarkers(FdoCache cache)
		{
			StringCollection rgMarkers = new StringCollection();
			foreach (MoMorphType mmt in cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				if (mmt.Prefix != null && mmt.Prefix.Length != 0 && !rgMarkers.Contains(mmt.Prefix))
					rgMarkers.Add(mmt.Prefix);
			}
			string[] rgs = new string[rgMarkers.Count];
			for (int i = 0; i < rgMarkers.Count; ++i)
				rgs[i] = rgMarkers[i];
			return rgs;
		}

		/// <summary>
		/// Return the list of strings used to mark morphemes at the end. These must not occur as part
		/// of the text of a morpheme (except at the boundaries to indicate its type).
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static string[] PostfixMarkers(FdoCache cache)
		{
			StringCollection rgMarkers = new StringCollection();
			foreach (MoMorphType mmt in cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				if (mmt.Postfix != null && mmt.Postfix.Length != 0 && !rgMarkers.Contains(mmt.Postfix))
					rgMarkers.Add(mmt.Postfix);
			}
			string[] rgs = new string[rgMarkers.Count];
			for (int i = 0; i < rgMarkers.Count; ++i)
				rgs[i] = rgMarkers[i];
			return rgs;
		}

		/// <summary>
		/// Return the number of unique LexEntries that reference this MoMorphType via MoForm.
		/// </summary>
		public int NumberOfLexEntries
		{
			get
			{
				// The SQL command must NOT modify the database contents!
				string query = String.Format("SELECT DISTINCT Owner$" +
					" FROM MoForm_" +
					" WHERE MorphType = {0}", Hvo.ToString());
				// Gather up complete set of ids.
				List<int> entries = DbOps.ReadIntsFromCommand(m_cache, query, null);

				// Remove any entry ids that are circumfixes.
				if (Guid.ToString().ToLower() == kguidMorphPrefix.ToLower()
					|| Guid.ToString().ToLower() == kguidMorphInfix.ToLower()
					|| Guid.ToString().ToLower() == kguidMorphSuffix.ToLower())
				{
					query = String.Format("SELECT DISTINCT mf.Owner$" +
						" FROM MoForm_ mf" +
						" JOIN MoMorphType_ mmt" +
						"     ON mf.MorphType = mmt.Id" +
						" WHERE mf.OwnFlid$ = {0} AND mmt.Guid$ = '{1}'", (int)LexEntry.LexEntryTags.kflidLexemeForm, kguidMorphCircumfix);
					foreach (int id in DbOps.ReadIntsFromCommand(m_cache, query, null))
						entries.Remove(id);
				}
				return entries.Count;
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
}
