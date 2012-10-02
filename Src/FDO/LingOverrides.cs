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
// File: LingOverrides.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// This file holds the overrides of the generated classes for the Ling module.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices; // needed for Marshal
using System.Windows.Forms;
using System.Xml;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Validation;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls; // for ProgressState
using SIL.Utils;

namespace  SIL.FieldWorks.FDO.Ling
{
	#region Lexicon

	/// <summary>
	/// Completely auto-generated.
	/// </summary>
	public partial class LexDb
	{
		/// <summary>overridden to optimize the loading of this large Vector.</summary>
		public FdoOwningCollection<ILexEntry> EntriesOC
		{
			get
			{	// enhance: now, we drum up a new one each time
				FdoOwningCollection<ILexEntry> c = EntriesOC_Generated;

				//here is the optimization, because there is really only one lexicon and one set of entries
				//and it is enormously faster to just load all of them rather selecting each one of the thousands.
				c.ShouldLoadAllOfType = true;
				return c;
			}
		}

		/// <summary>
		/// used when dumping the lexical database for the automated Parser
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		public FdoObjectSet<IMoForm> AllAllomorphs
		{
			get
			{
				string sQry = string.Format("select obj, txt from MoForm_Form WHERE ws={0}",
					m_cache.LangProject.DefaultVernacularWritingSystem);
				IDbColSpec dcs = DbColSpecClass.Create();
				dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
				dcs.Push((int)DbColType.koctMltAlt, 1, (int)MoForm.MoFormTags.kflidForm , m_cache.LangProject.DefaultVernacularWritingSystem);

				m_cache.LoadData(sQry, dcs, this.Hvo);
				System.Runtime.InteropServices.Marshal.ReleaseComObject(dcs);

				string query = "select id, class$ from MoForm_ order by class$";
				return new FdoObjectSet<IMoForm>(m_cache, query, false, true);
			}
		}

		/// <summary>
		/// A list of all allomorph ids.
		/// </summary>
		public List<int> AllAllomorphsList
		{
			get
			{
				// Have to include the two flids, since MoForms are also owned by other objects.
				string sQry = string.Format("select mff.obj, mff.txt " +
					"FROM MoForm_Form mff " +
					"JOIN CmObject obj ON mff.Obj = obj.Id " +
					"WHERE mff.ws={0} AND (obj.OwnFlid$ = {1} OR obj.OwnFlid$ = {2})",
					m_cache.LangProject.DefaultVernacularWritingSystem,
					(int)LexEntry.LexEntryTags.kflidAlternateForms,
					(int)LexEntry.LexEntryTags.kflidLexemeForm);
				IDbColSpec dcs = DbColSpecClass.Create();
				dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
				dcs.Push((int)DbColType.koctMltAlt, 1, (int)MoForm.MoFormTags.kflidForm, m_cache.LangProject.DefaultVernacularWritingSystem);

				m_cache.LoadData(sQry, dcs, this.Hvo);
				System.Runtime.InteropServices.Marshal.ReleaseComObject(dcs);

				// Have to include the two flids, since MoForms are also owned by other objects.
				string query = string.Format("SELECT id, class$ " +
					"FROM MoForm_ " +
					"WHERE OwnFlid$ = {0} OR OwnFlid$ = {1} " +
					"ORDER BY class$",
					(int)LexEntry.LexEntryTags.kflidAlternateForms,
					(int)LexEntry.LexEntryTags.kflidLexemeForm);
				return DbOps.ReadIntsFromCommand(m_cache, query, null);
			}
		}

		/// <summary>
		/// Get a filtered list of reversal index IDs that correspond to the current writing systems being used.
		/// </summary>
		public List<int> CurrentReversalIndices
		{
			get
			{
				string sql = "SELECT ri.Id"
					+ " FROM LangProject_CurAnalysisWss j_lp_cwa"
					+ " JOIN ReversalIndex ri"
					+ " 	ON ri.WritingSystem = j_lp_cwa.Dst"
					+ " ORDER BY j_lp_cwa.Ord";
				return DbOps.ReadIntsFromCommand(m_cache, sql, null);
			}
		}

		/// <summary>
		/// used when dumping the lexical database for the automated Parser
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		public FdoObjectSet<ILexSense> AllSenses
		{
			get
			{
				string query = "select id, class$ from LexSense_ order by class$";
				return new FdoObjectSet<ILexSense>(m_cache, query, false, true);
			}
		}
		/// <summary>
		/// used when dumping the lexical database for the automated Parser
		/// </summary>
		/// <remarks> Note that you may not find this method in source code,
		/// since it will be used from XML template and accessed dynamically.</remarks>
		public FdoObjectSet<IMoMorphSynAnalysis> AllMSAs
		{
			get
			{
				string query = "select id, class$ from MoMorphSynAnalysis_ order by class$";
				return new FdoObjectSet<IMoMorphSynAnalysis>(m_cache, query, false, true);
			}
		}

		//		/// <summary>
		//		/// get all of the problem annotations pointing to something in the lexical database (eventually)
		//		/// </summary>
		//		/// <remarks> Note that you may not find this method in source code,
		//		/// since it will be used from XML template and accessed dynamically.</remarks>
		//		static public FdoObjectSet GetProblemAnnotations(FdoCache cache)
		//		{
		//			get
		//			{		//todo: once we have a notation definitions, we can filter by those matching the problem definition
		//					//todo: how to select all of the Annotations poignant to buy anything that we own, recursively?
		//				string query = "select id, class$ from CmBaseAnnotation_ order by class$";
		//				return new FdoObjectSet(cache,query,
		//					false,	//these are not ordered
		//					false); //we don't want all annotations
		//			}
		//		}

		/// <summary></summary>
		public void PreloadForGrammarSketch()
		{
			//TODO do something more specific
			PreloadEntriesAndSenses();
		}

		/// <summary></summary>
		public void PreloadForParser()
		{
			//TODO do something more specific
			PreloadEntriesAndSenses();
		}

		/// <summary></summary>
		public void PreloadForLexiconExport()
		{
			//TODO do something more specific
			PreloadEntriesAndSenses();
			LexEntry.PreLoadShortName(m_cache);//test
			LexSense.PreloadShortName(m_cache);//test
		}

		/// <summary>Preloads the cache with most common properties of all lexical entries
		/// and senses.</summary>
		public void PreloadEntriesAndSenses()
		{
			PreloadEntries();
			PreloadSenses();
		}

		/// <summary>Preloads the cache with most common properties of all lexical entries.
		/// </summary>
		public void PreloadEntries()
		{
			string squery =
				" declare @fIsNocountOn int" +
				" set @fIsNocountOn = @@options & 512" +
				" if @fIsNocountOn = 0 set nocount on" +
				" select cf.obj, obj.UpdStmp, cf.txt from LexEntry_CitationForm cf" +
				" join CmObject obj on obj.id = cf.obj" +
				" where cf.ws = " + m_cache.DefaultVernWs +
				" if @fIsNocountOn = 0 set nocount off";
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctMltAlt, 1,
				(int)LexEntry.LexEntryTags.kflidCitationForm, m_cache.DefaultVernWs);
			m_cache.LoadData(squery, dcs, 0);
		}

		/// <summary>Preloads the cache with most common properties of all lexical senses.
		/// </summary>
		public void PreloadSenses()
		{
			string squery =
				" declare @fIsNocountOn int" +
				" set @fIsNocountOn = @@options & 512" +
				" if @fIsNocountOn = 0 set nocount on" +
				" select obj.id, obj.UpdStmp, sg.txt, sd.txt, " + (int)LexSense.LexSenseTags.kflidDefinition +
				", " + m_cache.DefaultAnalWs + ", sd.fmt from CmObject obj" +
				" left outer join LexSense_Gloss sg on sg.obj = obj.id and sg.ws = " + m_cache.DefaultAnalWs +
				" left outer join LexSense_Definition sd on sd.obj = obj.id and sd.ws = " + m_cache.DefaultAnalWs +
				" where obj.class$ = " + LexSense.kClassId +
				" if @fIsNocountOn = 0 set nocount off";
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctMltAlt, 1,
				(int)LexSense.LexSenseTags.kflidGloss, m_cache.DefaultAnalWs);
			dcs.Push((int)DbColType.koctMlaAlt, 1, 0, 0); // Next contains the text of an ML prop of object in column 1.
			dcs.Push((int)DbColType.koctFlid, 1, 0, 0); // Next contains the flid of that ML prop (constant at 34002).
			dcs.Push((int)DbColType.koctEnc, 1, 0, 0); // Next contains the ws of that ML prop.
			dcs.Push((int)DbColType.koctFmt, 1, 0, 0); // Next contains the fmt info for the ML prop.
			m_cache.LoadData(squery, dcs, 0);
		}

		/// <summary>
		/// Resets the homograph numbers for all entries.
		/// </summary>
		public void ResetHomographNumbers(System.Windows.Forms.ProgressBar progressBar)
		{
			List<int> processedEntryIds = new List<int>();
			List<ILexEntry> ie = new List<ILexEntry>(EntriesOC.ToArray());
			progressBar.Minimum = 0;
			progressBar.Maximum = EntriesOC.Count;
			progressBar.Step = 1;
			foreach (ILexEntry le in EntriesOC)
			{
				if (processedEntryIds.Contains(le.Hvo))
				{
					progressBar.PerformStep();
					continue;
				}

				List<ILexEntry> homographs = LexEntry.CollectHomographs(
					le.HomographForm,
					0, // Gathers them all up, including le.
					ie,
					le.MorphType);
				LexEntry.ValidateExistingHomographs(homographs);
				foreach (ILexEntry homograph in homographs)
				{
					processedEntryIds.Add(homograph.Hvo);
					progressBar.PerformStep();
				}
			}
		}

	}

	/// <summary>
	///
	/// </summary>
	public partial class LexExampleSentence
	{

		/// <summary>
		/// This is the string
		/// which is displayed in the Delete Pronunciation dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				//LexExampleSentence LexES =
				//    (LexExampleSentence)CmObject.CreateFromDBObject(m_cache, m_hvo);
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
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
	///
	/// </summary>
	public partial class LexPronunciation
	{


		/// <summary>
		/// This is the string
		/// which is displayed in the Delete Pronunciation dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				//LexPronunciation lexPron =
				//    (LexPronunciation)CmObject.CreateFromDBObject(m_cache, m_hvo);
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteLexPronunciation));
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
				case (int)LexPronunciation.LexPronunciationTags.kflidLocation:
					return m_cache.LangProject.LocationsOA;
				default:
					return base.ReferenceTargetOwner(flid);
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
	}

	/// <summary></summary>
	public partial class LexEntry
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new entry.
		/// The caller is expected to call PropChanged on the cache to notify the record
		/// list of the new record.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="morphType">Type of the morph.</param>
		/// <param name="tssLexemeForm">The TSS lexeme form.</param>
		/// <param name="gloss">The gloss.</param>
		/// <param name="dummyMSA">The dummy MSA.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILexEntry CreateEntry(FdoCache cache, IMoMorphType morphType,
			ITsString tssLexemeForm, string gloss, DummyGenericMSA dummyMSA)
		{
			using (new UndoRedoTaskHelper(cache, Strings.ksUndoCreateEntry, Strings.ksRedoCreateEntry))
			{
				ILexEntry entry = null;
				// NO: Since this fires a PropChanged, and we do it ourselves later on.
				// entry = (LexEntry)entries.Add(new LexEntry());

				// CreateObject creates the entry without a PropChanged.
				int entryHvo = cache.CreateObject(LexEntry.kClassId,
					cache.LangProject.LexDbOAHvo,
					(int)LexDb.LexDbTags.kflidEntries,
					0); // 0 is fine, since the entries prop is not a sequence.
				entry = LexEntry.CreateFromDBObject(cache, entryHvo);

				ILexSense sense = LexSense.CreateSense(entry, dummyMSA, gloss);

				if (morphType.Guid.ToString() == MoMorphType.kguidMorphCircumfix)
				{
					// Set Lexeme form to lexeme form and circumfix
					SetCircumfixLexemeForm(cache, entry, tssLexemeForm, morphType);
					// Create two allomorphs, one for the left member and one for the right member.
					SplitCircumfixIntoLeftAndRightAllomorphs(cache, entry, tssLexemeForm, sense);
				}
				else
				{
					IMoForm allomorph = MoForm.CreateAllomorph(entry, sense.MorphoSyntaxAnalysisRA, tssLexemeForm, morphType, true);
				}
				(entry as LexEntry).UpdateHomographNumbersAccountingForNewEntry();
				return entry;
			}
		}

		private void UpdateHomographNumbersAccountingForNewEntry()
		{
			// (We don't want a citation form by default.  See LT-7220.)
			// Handle homograph number.
			this.HomographNumber = 0;
			// Not all "CollectHomographs" methods use the calling entry.
			// Make sure we use one that does in this case.
			List<ILexEntry> homographs = CollectHomographs(
				this.HomographForm, // This should not have the markers.
				0, // Ensures we get all of them, including this new one.
				GetHomographList(Cache, this.HomographForm),
				this.MorphType);
			LexEntry.ValidateExistingHomographs(homographs);
		}

		/// <summary>
		/// Set DateCreated and DateModified to 'Now', not to the beginning of time.
		/// </summary>
		public override void InitNewInternal()
		{
			DateCreated = DateTime.Now;
			DateModified = DateCreated;
			base.InitNewInternal();
		}

		/// <summary>
		/// Get the minimal set of LexReferences for this entry.
		/// </summary>
		public List<int> MinimalLexReferences
		{
			get { return LexReference.ExtractMinimalLexReferences(m_cache, Hvo); }
		}

		/// <summary>
		/// This method retrieves the list of LexReference objects that contain the LexEntry
		/// or LexSense given by hvo. The list is pruned to remove any LexReference that
		/// targets only hvo unless parent LexRefType is a sequence/scale.  This pruning
		/// is needed to obtain proper display of the Dictionary (publication) view.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static void LoadAllMinimalLexReferences(FdoCache cache, Dictionary<int, List<int> > values)
		{
			LexReference.LoadAllMinimalLexReferences(cache, values);
		}

		/// <summary>
		/// Return true if the entry is considered bound (a bound root or stem).
		/// </summary>
		private bool IsBound
		{
			get
			{
				int mt = MorphType;
				return mt == MoMorphType.kmtBoundRoot || mt == MoMorphType.kmtBoundStem;
			}
		}

		/// <summary>
		/// Confirm that we can break the specified string into the two parts needed for a circumfix.
		/// Return true (and the two parts, not stripped of affix markers) if successful.
		/// </summary>
		static public bool GetCircumfixLeftAndRightParts(FdoCache cache, ITsString tssLexemeForm,
			out string sLeftMember, out string sRightMember)
		{
			// split citation form into left and right parts
			sLeftMember = null;
			sRightMember = null;
			char[] aSpacePeriod = new char[2] { ' ', '.' };
			string lexemeForm = tssLexemeForm.Text;
			int wsVern = StringUtils.GetWsAtOffset(tssLexemeForm, 0);
			int iLeftEnd = lexemeForm.IndexOfAny(aSpacePeriod);
			if (iLeftEnd < 0)
				return false;
			else
				sLeftMember = lexemeForm.Substring(0, iLeftEnd);
			int iRightBegin = lexemeForm.LastIndexOfAny(aSpacePeriod);
			if (iRightBegin < 0)
				return false;
			else
				sRightMember = lexemeForm.Substring(iRightBegin + 1);
			MoMorphTypeCollection mmtCol = new MoMorphTypeCollection(cache);
			int clsidForm;
			try
			{
				string temp = sLeftMember;
				MoMorphType.FindMorphType(cache, mmtCol, ref temp, out clsidForm);
				temp = sRightMember;
				MoMorphType.FindMorphType(cache, mmtCol, ref temp, out clsidForm);
			}
			catch(Exception)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Set the specified WS of the form of your LexemeForm, making sure not to include any
		/// morpheme break characters. As a special case, if your LexemeForm is a circumfix,
		/// do not strip morpheme break characters, and also try to set the form of prefix and suffix.
		/// </summary>
		public void SetLexemeFormAlt(int ws, ITsString tssLexemeFormIn)
		{
			ITsString tssLexemeForm = tssLexemeFormIn;
			MoForm mf = LexemeFormOA as MoForm;
			if (IsCircumfix())
			{
				MoForm mfPrefix = null;
				MoForm mfSuffix = null;
				foreach (MoForm mfT in AllAllomorphs)
				{
					if (mfPrefix == null && mfT.MorphTypeRA.Guid.ToString() == MoMorphType.kguidMorphPrefix)
						mfPrefix = mfT;
					if (mfSuffix == null && mfT.MorphTypeRA.Guid.ToString() == MoMorphType.kguidMorphSuffix)
						mfSuffix = mfT;
				}
				string sLeftMember;
				string sRightMember;
				if (!GetCircumfixLeftAndRightParts(Cache, tssLexemeForm, out sLeftMember, out sRightMember))
					return;
				if (mfPrefix != null)
					mfPrefix.Form.SetAlternative(MoForm.EnsureNoMarkers(sLeftMember.Trim(), Cache), ws);
				if (mfSuffix != null)
					mfSuffix.Form.SetAlternative(MoForm.EnsureNoMarkers(sRightMember.Trim(), Cache), ws);
			}
			else
			{
				// Normal non-circumfix case, set the appropriate alternative on the Lexeme form itself
				// (making sure to include no invalid characters).
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tssLexemeForm = tsf.MakeString(MoForm.EnsureNoMarkers(tssLexemeForm.Text, m_cache), ws);
			}
			if (mf != null)
				mf.Form.SetAlternative(tssLexemeForm, ws);
		}

		/// <summary>
		/// If any allomorphs have a root type (root or bound root), change them to the corresponding stem type.
		/// </summary>
		public void ChangeRootToStem()
		{
			foreach (IMoForm mf in AllAllomorphs)
				mf.ChangeRootToStem();
		}


		static private void SplitCircumfixIntoLeftAndRightAllomorphs(FdoCache cache,
			ILexEntry entry, ITsString tssLexemeForm, ILexSense sense)
		{
			string sLeftMember;
			string sRightMember;
			if (!GetCircumfixLeftAndRightParts(cache, tssLexemeForm, out sLeftMember, out sRightMember))
				return;
			// Create left and right allomorphs
			int wsVern = StringUtils.GetWsAtOffset(tssLexemeForm, 0);
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			MoMorphType.GetMajorAffixMorphTypes(cache, out mmtPrefix, out mmtSuffix, out mmtInfix);
			int clsidForm;
			MoMorphTypeCollection mmtCol = new MoMorphTypeCollection(cache);
			IMoMorphType mmt = MoMorphType.FindMorphType(cache, mmtCol, ref sLeftMember, out clsidForm);
			if ((mmt.Hvo != mmtPrefix.Hvo) &&
				(mmt.Hvo != mmtInfix.Hvo))
				mmt = mmtPrefix; // force a prefix if it's neither a prefix nor an infix
			IMoForm allomorph = MoForm.CreateAllomorph(entry, sense.MorphoSyntaxAnalysisRA,
				StringUtils.MakeTss(sLeftMember, wsVern), mmt, false);
			mmt = MoMorphType.FindMorphType(cache, mmtCol, ref sRightMember, out clsidForm);
			if ((mmt.Hvo != mmtInfix.Hvo) &&
				(mmt.Hvo != mmtSuffix.Hvo))
				mmt = mmtSuffix; // force a suffix if it's neither a suffix nor an infix
			allomorph = MoForm.CreateAllomorph(entry, sense.MorphoSyntaxAnalysisRA,
				StringUtils.MakeTss(sRightMember, wsVern), mmt, false);
		}

		static private void SetCircumfixLexemeForm(FdoCache cache, ILexEntry entry, ITsString tssLexemeForm, IMoMorphType morphType)
		{
			int iHvo = cache.CreateObject(MoAffixAllomorph.kClassId, entry.Hvo, (int)LexEntry.LexEntryTags.kflidLexemeForm, 0);
			MoAffixAllomorph lexemeAllo = new MoAffixAllomorph(cache, iHvo);
			lexemeAllo.Form.SetAlternativeTss(tssLexemeForm);
			lexemeAllo.MorphTypeRA = morphType;
			lexemeAllo.IsAbstract = true;
		}

		/// <summary>
		/// Gets all senses owned by this entry, and all senses they own.
		/// </summary>
		public List<ILexSense> AllSenses
		{
			get
			{
				List<ILexSense> senses = new List<ILexSense>();
				foreach (ILexSense ls in SensesOS)
					senses.AddRange(ls.AllSenses);
				return senses;
			}
		}

		/// <summary>
		/// hvos for senses and their subsenses.
		/// </summary>
		public List<int> AllSenseHvos
		{
			get
			{
				return FdoVectorUtils.ConvertCmObjectsToHvoList<ILexSense>(AllSenses);
			}
		}

		/// <summary>
		/// Get the Number or Senses of this LexEntry.
		/// </summary>
		public int NumberOfSenses
		{
			get
			{
				// Performance enhancement should be looked at here:
				// If you look at the definition of AllSenses it is not very efficient calculating Count.
				return AllSenses.Count;
			}
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
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				int csense = sda.get_VecSize(Hvo, (int)LexEntryTags.kflidSenses);
				if (csense > 1)
					return true;
				if (csense == 0)
					return false;
				// otherwise, we have to consider the possibility that the sense has a subsense
				int hvoSense = sda.get_VecItem(Hvo, (int)LexEntryTags.kflidSenses, 0);
				return (sda.get_VecSize(hvoSense, (int)LexSense.LexSenseTags.kflidSenses) > 0);
			}
		}

		/// <summary>
		/// Get all the morph types for the allomorphs of an entry,
		/// including the lexeme form.
		/// </summary>
		public List<IMoMorphType> MorphTypes
		{
			get
			{
				List<IMoMorphType> types = new List<IMoMorphType>();
				IMoForm lfForm = LexemeFormOA;
				if (lfForm != null && lfForm.MorphTypeRAHvo > 0)
					types.Add(lfForm.MorphTypeRA);
				foreach (IMoForm form in AlternateFormsOS)
				{
					IMoMorphType mmt = form.MorphTypeRA;
					if (mmt != null && !types.Contains(mmt))
						types.Add(mmt);
				}
				return types;
			}
		}

		/// <summary>
		/// Get the morph type (index) for the entry.
		/// </summary>
		public int MorphType
		{
			get
			{
				List<IMoMorphType> types = MorphTypes;
				int type;
				switch (types.Count)
				{
					case 0:
						type = MoMorphType.kmtUnknown;
						break;
					case 1:
						type = MoMorphType.FindMorphTypeIndex(m_cache, types[0]);
						break;
					case 2:
						// probably a circumfix with two infixes
						// fall through to the 3 case
					case 3:
						// probably a circumfix
						type = MoMorphType.FindMorphTypeIndex(m_cache, types[0]);
						if (MoMorphType.kmtCircumfix != type)
							type = MoMorphType.kmtMixed;
						break;
					default:
						type = MoMorphType.kmtMixed;
						break;
				}
				return type;
			}
		}

		/// <summary> a gloss to use when displaying the entry</summary>
		/// <remarks>A lexical entry does not actually have a gloss,
		/// it has senses which each have a gloss. here, we get the gloss of the
		/// first sense to use as the gloss for the entry.</remarks>
		public string PrimaryGloss
		{
			get
			{
				if(this.SensesOS.Count == 0)
					return "";
				LexSense sense =(LexSense)this.SensesOS[0];
				MultiUnicodeAccessor gloss = sense.Gloss;
				if(gloss !=null)
					return gloss.AnalysisDefaultWritingSystem;
				return "";
			}
		}

		/// <summary>
		/// Overrides in order to reset the homograph numbering if needed, and to prevent duplicate MSAs.
		/// </summary>
		/// <param name="objSrc"></param>
		/// <param name="fLoseNoStringData"></param>
		public override void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			if (!(objSrc is ILexEntry))
				return;
			string sFormSrc = ShortName1Static(objSrc.Cache, objSrc.Hvo);
			base.MergeObject(objSrc, fLoseNoStringData);
			// adjust the homograph numbers
			ValidateExistingHomographs(CollectHomographs(sFormSrc, 0));
			// Merge duplicate MSAs.
			List<IMoMorphSynAnalysis> msas = new List<IMoMorphSynAnalysis>(MorphoSyntaxAnalysesOC);
			for (int i = 0; i < msas.Count; i++)
			{
				IMoMorphSynAnalysis first = msas[i];
				if (first.Hvo == (int) SpecialHVOValues.kHvoUnderlyingObjectDeleted)
					continue;
				for (int j = i + 1; j < msas.Count; j++)
				{
					IMoMorphSynAnalysis second = msas[j];
					if (second.Hvo == (int)SpecialHVOValues.kHvoUnderlyingObjectDeleted)
						continue;
					if (first.EqualsMsa(second))
					{
						// Merge the two.
						CmObject.ReplaceReferences(m_cache, second, first);
						second.DeleteUnderlyingObject();
					}
				}
			}
		}

		/// <summary>
		/// Overrides themethod, so we can also merge similar MSAs and allomorphs, after the main merge..
		/// </summary>
		/// <param name="objSrc">Object whose properties will be merged into this object's properties</param>
		public override void MergeObject(ICmObject objSrc)
		{
			if (!(objSrc is ILexEntry))
				return;

			ILexEntry le = objSrc as ILexEntry;
			//  merge the LexemeForm objects first.  This is important, because otherwise the
			// LexemeForm objects would not get merged, and that is needed for proper handling
			// of references and back references.
			if (this.LexemeFormOA != null && le.LexemeFormOA != null)
			{
				this.LexemeFormOA.MergeObject(le.LexemeFormOA);
				le.LexemeFormOA = null;
			}

			// base.MergeObject will call DeleteUnderlyingObject on objSrc,
			// which, in turn, will reset homographs for any similar entries for objSrc.
			base.MergeObject(objSrc);
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
						&& formToProcess.Form.VernacularDefaultWritingSystem == formToConsider.Form.VernacularDefaultWritingSystem
						&& formToProcess.MorphTypeRAHvo == formToConsider.MorphTypeRAHvo)
					{
						formToProcess.MergeObject(formToConsider);
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
						msaToProcess.MergeObject(msaToConsider);
						msaList.Remove(msaToConsider);
					}
				}
			}

			// Reset the homograph numbers, since the demise of objSrc may make the current numbers invalid.
			List<ILexEntry> homographs = CollectHomographs(HomographForm,
				0); // Ensure this entry is included in the reset.
			ValidateExistingHomographs(homographs);
		}

		/// <summary>
		/// Loads the cache with all of the strings which will be needed by the ShortName
		/// property. For performance only.
		/// </summary>
		public static void PreLoadShortName(FdoCache cache)
		{
			// JohnT: there's seldom much point in doing this more than once. (That's bad enough!).
			// The main exception is Refresh, when we throw everything away and start over.
			// There might be other exceptions, such as after a major import.
			if (PreloadPerformedVirtualHandler.PreloadPerformed(cache))
				return;
#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = System.Environment.TickCount;
#endif
			cache.LoadAllOfOneWsOfAMultiUnicode((int)LexEntry.LexEntryTags.kflidCitationForm,
				"LexEntry", cache.DefaultVernWs);
			// This seems to be needed because many lex entries (at least in some databases,
			// such as the Ron Moe one) don't have citation forms, and hence we go to some
			// related MoForm to get the short name for the lex entry.
			cache.LoadAllOfOneWsOfAMultiUnicode((int)
				MoForm.MoFormTags.kflidForm,
				"MoForm", cache.DefaultVernWs);
			// It seems to me (JohnT) that it should also be necessary to preload the
			// UnderlyingForm and Allomorphs properties, but testing with a large dictionary and
			// SQL profiler seems to confirm that it is not. Apparently something I don't know
			// about is preloading this information...perhaps part of pre-loading the sequence
			// of LexEntry objects?
			// Notice we're not preloading the analysis writing system, on the assumption that
			// that is rare. (JT: but, it isn't rare to CHECK and SEE whether there is an
			// analysis WS of the citation form, if we have that case at all and there is no
			// vernacular CF.  For now, I removed looking for the analysis CF from the ShortName
			// method, so it is OK not to preload it.

			// Preload the homograph numbers which will also be needed for the HeadWord, which
			// is used instead of ShortName when uniqueness is desired. Also include class info
			// to avoid later cache misses when loading the objects.
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_Class, 0);
			dcs.Push((int)DbColType.koctObj, 1,
				(int)CmObjectFields.kflidCmObject_Owner, 0);
			dcs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_OwnFlid, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctInt, 1,
				(int)LexEntry.LexEntryTags.kflidHomographNumber, 0);
			cache.LoadData("select Id, Class$, Owner$, OwnFlid$, UpdStmp, HomographNumber from LexEntry_", dcs, 0);

			// Preload the LexemeForm for each LexEntry.
			dcs.Clear();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctObjOwn, 1,
				(int)LexEntry.LexEntryTags.kflidLexemeForm, 0);
			cache.LoadData("select Src, Dst from LexEntry_LexemeForm", dcs, 0);

			// Preload the allomorphs for each LexEntry.  This is needed to obtain the morph
			// types, which are properties of the allomorphs.
			dcs.Clear();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctObjVecOwn, 1,
				(int)LexEntry.LexEntryTags.kflidAlternateForms, 0);
			cache.LoadData("select Src, Dst from LexEntry_AlternateForms order by Src,Ord", dcs, 0);

			// Preload the morphtype for each allomorph (MoForm).
			dcs.Clear();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctObj, 1, (int)MoForm.MoFormTags.kflidMorphType,
				0);
			cache.LoadData("select Id,MorphType from MoForm", dcs, 0);

			// Preload the information for MoMorphTypes.
			dcs.Clear();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctUnicode, 1,
				(int)MoMorphType.MoMorphTypeTags.kflidPostfix, 0);
			dcs.Push((int)DbColType.koctUnicode, 1,
				(int)MoMorphType.MoMorphTypeTags.kflidPrefix, 0);
			dcs.Push((int)DbColType.koctInt, 1,
				(int)MoMorphType.MoMorphTypeTags.kflidSecondaryOrder, 0);
			cache.LoadData("select Id,Postfix,Prefix,SecondaryOrder from MoMorphType", dcs, 0);

			PreloadPerformedVirtualHandler.SetPreloadPerformed(cache);
#if DEBUG
			int tc2 = System.Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "Preloading for LexEntry ShortNames took " + (tc2 - tc1) + " ticks," +
				" or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
			Debug.WriteLine(s);
#endif

			Marshal.ReleaseComObject(dcs); //jdh added dec 1, 2004
		}

		/// <summary>
		/// Get the homograph form from the citation form,
		/// or the lexeme form (no citation form), in that order.
		/// </summary>
		public string HomographForm
		{
			get
			{
				return ShortName1Static(m_cache, m_hvo);
			}
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
				return ShortName1Static(Cache, m_hvo);
			}
		}

		/// <summary>
		/// Static version for avoiding creating actual object.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static string ShortName1Static(FdoCache cache, int hvo)
		{
			return ShortName1StaticForWs(cache, hvo, cache.DefaultVernWs);
		}

		/// <summary>
		/// Static version for avoiding creating actual object.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="wsVern"></param>
		/// <returns></returns>
		public static string ShortName1StaticForWs(FdoCache cache, int hvo, int wsVern)
		{
			if (wsVern <= 0)
				wsVern = cache.DefaultVernWs;
			ISilDataAccess sda = cache.MainCacheAccessor;

			// try vernacular citation
			string label = sda.get_MultiStringAlt(hvo,
				(int)LexEntry.LexEntryTags.kflidCitationForm, wsVern).Text;
			if (label != null && label.Length != 0)
				return label;

			return LexemeFormStaticForWs(cache, hvo, wsVern);
		}

		/// <summary>
		/// Static version for avoiding creating actual object.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="tsb"></param>
		/// <returns></returns>
		public static void ShortName1Static(FdoCache cache, int hvo, ITsIncStrBldr tsb)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			int wsVern = cache.DefaultVernWs;

			// try vernacular citation
			ITsString label = sda.get_MultiStringAlt(hvo,
				(int)LexEntry.LexEntryTags.kflidCitationForm, wsVern);
			if (label.Length != 0)
			{
				tsb.AppendTsString(label);
				return;
			}

			// try lexeme form
			int hvoLf = sda.get_ObjectProp(hvo, (int)LexEntry.LexEntryTags.kflidLexemeForm);
			if (hvoLf != 0)
			{
				label = sda.get_MultiStringAlt(hvoLf, (int)MoForm.MoFormTags.kflidForm,
					wsVern);
				if (label.Length != 0)
				{
					tsb.AppendTsString(label);
					return;
				}
			}

			// Try the first alternate form with the wsVern WS.
			for (int i = 0; i < sda.get_VecSize(hvo, (int)LexEntry.LexEntryTags.kflidAlternateForms); i++)
			{
				int hvoAm = sda.get_VecItem(hvo, (int)LexEntry.LexEntryTags.kflidAlternateForms, i);
				label = sda.get_MultiStringAlt(hvoAm, (int)MoForm.MoFormTags.kflidForm,
					wsVern);
				if (label.Length != 0)
				{
					tsb.AppendTsString(label);
					return;
				}
			}

			// give up
			tsb.AppendTsString(cache.MakeUserTss(Strings.ksQuestions));		// was "??", not "???"
		}

		/// <summary>
		/// Same as ShortName, but ignores Citation form.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		private static string LexemeFormStatic(FdoCache cache, int hvo)
		{
			return LexemeFormStaticForWs(cache, hvo, cache.DefaultVernWs);
		}

		private static string LexemeFormStaticForWs(FdoCache cache, int hvo, int wsVern)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			string label = null;
			// try lexeme form
			int hvoLf = sda.get_ObjectProp(hvo, (int)LexEntry.LexEntryTags.kflidLexemeForm);
			if (hvoLf != 0)
			{
				label = sda.get_MultiStringAlt(hvoLf, (int)MoForm.MoFormTags.kflidForm,
					wsVern).Text;
				if (label != null && label.Length != 0)
					return label;
			}
			// Try the first alternate form with the wsVern WS.
			int cForms = sda.get_VecSize(hvo, (int)LexEntry.LexEntryTags.kflidAlternateForms);
			for (int i = 0; i < cForms; i++)
			{
				int hvoAm = sda.get_VecItem(hvo, (int)LexEntry.LexEntryTags.kflidAlternateForms,
					i);
				label = sda.get_MultiStringAlt(hvoAm, (int)MoForm.MoFormTags.kflidForm,
					wsVern).Text;
				if (label != null && label.Length != 0)
					return label;
			}
			if (label == null || label.Length == 0)
				label = Strings.ksQuestions;					// give up (was "??", not "???")
			return label;
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
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		/// <remarks> precede by PreLoadShortName() when calling this a lot, for example when
		/// sorting  an entire dictionary by this property.</remarks>
		public  ITsString ShortNameTS
		{
			get { return ShortNameTSS; }//temporary
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get { return HeadWordStatic(m_cache, m_hvo); }
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
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteLexEntry, " "));
				tisb.AppendTsString(ShortNameTSS);

				List<LinkedObjectInfo> linkedObjects = LinkedObjects;
				List<int> countedObjectIDs = new List<int>();
				int analCount = 0;
				int alloAHPCount = 0;
				int morphemeAHPCount = 0;
				foreach (LinkedObjectInfo loi in linkedObjects)
				{
					switch (loi.RelObjClass)
					{
						default:
							break;
						case WfiAnalysis.kclsidWfiAnalysis:
						{
							if (loi.RelObjField == (int)WfiAnalysis.WfiAnalysisTags.kflidStems)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++analCount;
								}
							}
							break;
						}
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
						case MoMorphAdhocProhib.kclsidMoMorphAdhocProhib:
						{
							if (loi.RelObjField == (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidFirstMorpheme)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++morphemeAHPCount;
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
				string warningMsg = String.Format("\x2028\x2028{0}\x2028{1}",
					Strings.ksEntryUsedHere, Strings.ksDelEntryDelThese);
				bool wantMainWarningLine = true;
				// Create a string with its own run of properties, so we don't carry on tisb's properties.
				// Otherwise, we might append the string as a superscript, running with the homograph properties (cf. LT-3177).
				ITsIncStrBldr tisb2 = TsIncStrBldrClass.Create();
				if (analCount > 0)
				{
					tisb2.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb2.Append(warningMsg);
					tisb2.Append("\x2028");
					if (analCount > 1)
						tisb2.Append(String.Format(Strings.ksIsUsedXTimesByAnalyses, cnt++, analCount));
					else
						tisb2.Append(String.Format(Strings.ksIsUsedOnceByAnalyses, cnt++));
					wantMainWarningLine = false;
				}
				if (morphemeAHPCount > 0)
				{
					tisb2.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb2.Append(warningMsg);
					tisb2.Append("\x2028");
					if (morphemeAHPCount > 1)
						tisb2.Append(String.Format(Strings.ksIsUsedXTimesByMorphAdhoc,
							cnt++, morphemeAHPCount, "\x2028"));
					else
						tisb2.Append(String.Format(Strings.ksIsUsedOnceByMorphAdhoc,
							cnt++, "\x2028"));
					wantMainWarningLine = false;
				}
				if (alloAHPCount > 0)
				{
					tisb2.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb2.Append(warningMsg);
					tisb2.Append("\x2028");
					if (alloAHPCount > 1)
						tisb2.Append(String.Format(Strings.ksIsUsedXTimesByAlloAdhoc,
							cnt++, alloAHPCount, "\x2028"));
					else
						tisb2.Append(String.Format(Strings.ksIsUsedOnceByAlloAdhoc,
							cnt++, "\x2028"));
				}
				tisb.AppendTsString(tisb2.GetString());

				return tisb.GetString();
			}
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
				int nSortKey2 = 0;
				// This is optimized for speed, in case you were wondering.  PreLoadShortName()
				// loads all of the data this references directly.
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				int hvoLexForm = sda.get_ObjectProp(m_hvo, (int)LexEntry.LexEntryTags.kflidLexemeForm);
				if (hvoLexForm > 0)
				{
					int hvoType = sda.get_ObjectProp(hvoLexForm, (int)MoForm.MoFormTags.kflidMorphType);
					if (hvoType > 0)
					{
						// Leave room for 1023 homographs.
						nSortKey2 = sda.get_IntProp(hvoType,
							(int)MoMorphType.MoMorphTypeTags.kflidSecondaryOrder) * 1024;
					}
				}
				nSortKey2 += m_cache.MainCacheAccessor.get_IntProp(m_hvo,
					(int)LexEntry.LexEntryTags.kflidHomographNumber);
				return nSortKey2;
			}
		}

		/// <summary>
		/// A sort key which combines both SortKey and SortKey2 in a string.
		/// Note: called by reflection as a sortmethod for browse columns.
		/// </summary>
		public string FullSortKey(bool sortedFromEnd, int ws)
		{
			string sKey = ShortName1StaticForWs(Cache, m_hvo, ws);

			if (sortedFromEnd)
				sKey = StringUtils.ReverseString(sKey);

			int nKey2 = this.SortKey2;
			if (nKey2 != 0)
				sKey = sKey + " " + SortKey2Alpha;

			return sKey;
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
		/// A sort key method for sorting on the CitationForm field.
		/// </summary>
		public string CitationFormSortKey(bool sortedFromEnd, int ws)
		{
			string sKey = null;
			if (this.CitationForm != null)
				sKey = this.CitationForm.GetAlternative(ws);
			if (sKey == null)
				sKey = "";

			if (sortedFromEnd)
				sKey = StringUtils.ReverseString(sKey);

			return (this.LexemeFormOA as MoForm).SortKeyMorphType(sKey);
		}

		/// <summary>
		/// The writing system for sorting a list of ShortNames.
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
		/// Side effects of deleting the underlying object.
		/// This is complicated by the existence of homograph numbers.
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Reset homograph numbers of any similar entries, if any.
			// This version of 'CollectHomographs' leaves out this object,
			// which is a good thing.
			ValidateExistingHomographs(CollectHomographs());

			// Call delete side effects for our owned properties.
			foreach (ILexSense sense in SensesOS)
				sense.DeleteObjectSideEffects(objectsToDeleteAlso, state);

			foreach (IMoMorphSynAnalysis msa in MorphoSyntaxAnalysesOC)
				msa.DeleteObjectSideEffects(objectsToDeleteAlso, state);

			foreach (IMoForm mf in AlternateFormsOS)
				mf.DeleteObjectSideEffects(objectsToDeleteAlso, state);

			foreach (ILexPronunciation lp in PronunciationsOS)
				lp.DeleteObjectSideEffects(objectsToDeleteAlso, state);

			if (EtymologyOAHvo > 0)
				EtymologyOA.DeleteObjectSideEffects(objectsToDeleteAlso, state);
			if (LexemeFormOAHvo > 0)
				LexemeFormOA.DeleteObjectSideEffects(objectsToDeleteAlso, state);

			// Deal with critical inbound references on entry and objects it owns.
			// The idea here is to delete any objects that refer to the entry,
			// but ONLY if those objects would then be invalid.
			// We call DeleteUnderlyingObject() directly on any high risk objects,
			// such as MSAs,MoForms, and senses, since the regular deletion SP would not delete
			// any other invalid objects.
			List<LinkedObjectInfo> linkedObjs = LinkedObjects;
			List<int> deletedObjectIDs = new List<int>();
			foreach (LinkedObjectInfo loi in linkedObjs)
			{
				switch (loi.RelObjClass)
				{
					default:
						break;
					case WfiAnalysis.kclsidWfiAnalysis:
					{
						IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_cache, loi.RelObjId);
						if (loi.RelObjField == (int)WfiAnalysis.WfiAnalysisTags.kflidStems)
						{
							if (!deletedObjectIDs.Contains(anal.Hvo))
							{
								deletedObjectIDs.Add(anal.Hvo);
								foreach (IWfiMorphBundle mb in anal.MorphBundlesOS)
									deletedObjectIDs.Add(mb.Hvo);
								anal.DeleteObjectSideEffects(objectsToDeleteAlso, state);
							}
						}
						break;
					}
					case MoAlloAdhocProhib.kclsidMoAlloAdhocProhib:
					{
						if (loi.RelObjField == (int)MoAlloAdhocProhib.MoAlloAdhocProhibTags.kflidFirstAllomorph)
						{
							if (!deletedObjectIDs.Contains(loi.RelObjId))
							{
								deletedObjectIDs.Add(loi.RelObjId);
								IMoAlloAdhocProhib obj = MoAlloAdhocProhib.CreateFromDBObject(m_cache, loi.RelObjId);
								obj.DeleteObjectSideEffects(objectsToDeleteAlso, state);
							}
						}
						break;
					}
					case MoMorphAdhocProhib.kclsidMoMorphAdhocProhib:
					{
						if (loi.RelObjField == (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidFirstMorpheme)
						{
							if (!deletedObjectIDs.Contains(loi.RelObjId))
							{
								deletedObjectIDs.Add(loi.RelObjId);
								IMoMorphAdhocProhib obj = MoMorphAdhocProhib.CreateFromDBObject(m_cache, loi.RelObjId);
								obj.DeleteObjectSideEffects(objectsToDeleteAlso, state);
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
						}
						break;
					}
					case LexReference.kclsidLexReference:
					{
						if (objectsToDeleteAlso.Contains(loi.RelObjId))
							break;
						LexReference lr = (LexReference)LexReference.CreateFromDBObject(m_cache, loi.RelObjId);
						// Delete the Lexical relationship if it will be broken after removing this target.
						if (lr.IncompleteWithoutTarget(this.Hvo))
							lr.DeleteObjectSideEffects(objectsToDeleteAlso, state);
						else
							lr.TargetsRS.Remove(this.Hvo);
						break;
					}
				}
			}

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Collect all the homographs of this LexEntry.  This LexEntry will not be included in
		/// the returned list.
		/// </summary>
		public List<ILexEntry> CollectHomographs()
		{
			return CollectHomographs(ShortName1Static(m_cache, m_hvo), m_hvo);
		}

		/// <summary>
		/// Collect all the homographs of the given form.  This LexEntry may or may not be
		/// included in the returned list.
		/// It will be excluded if the hvo parameter is its own Id.
		/// Otherwise, it will be included.
		/// </summary>
		public List<ILexEntry> CollectHomographs(string sForm, int hvo)
		{
			return CollectHomographs(sForm, hvo, GetHomographList(m_cache, sForm), MorphType);
		}

		/// <summary>
		/// This overload is useful to get a list of homographs for a given string, not starting
		/// with any particlar entry.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sForm"></param>
		/// <param name="morphType"></param>
		/// <returns></returns>
		public static List<ILexEntry> CollectHomographs(FdoCache cache, string sForm, int morphType)
		{
			return CollectHomographs(sForm, 0, GetHomographList(cache, sForm), morphType);
		}

		// This finds all the lex entries that match in either citation form or lexeme form.
		// That's too many, but not by much, compared to a previous version that loaded the
		// whole collection!
		internal static List<ILexEntry> GetHomographList(FdoCache cache, string sForm)
		{
			List<ILexEntry> entries = new List<ILexEntry>();
			if (cache.DatabaseName == null || cache.DatabaseName == string.Empty)
			{
				// running tests...don't have a real database connection, so can't do SQL optimization.
				// Note that the main tests involving homographs use a real connection, but there
				// is at least one test involving them (RDEMerge) where that is not the focus of the test.
				// Just use all the entries in this case.
				entries.AddRange(cache.LangProject.LexDbOA.EntriesOC.ToArray());
				return entries;
			}

			string sql = "declare @sForm NCHAR(4000)"
				+ " set @sForm = ?"
				+ " select co.id, co.class$ from CmObject co "
				+ " join (select obj from LexEntry_CitationForm where Txt= @sForm"
				+ " union select lelf.Src as obj from LexEntry_LexemeForm lelf "
				+ " join MoForm_Form mff on lelf.Dst = mff.Obj and mff.Txt=@sForm) as objects on co.id = objects.obj";

			FdoObjectSet<ILexEntry> homographs = new FdoObjectSet<ILexEntry>(cache, sql, sForm);
			foreach (ILexEntry entry in homographs)
				entries.Add(entry);
			return entries;
		}

		/// <summary>
		/// Main method to collect all the homographs of the given form from the given list of entries.
		/// Set hvo to 0 to collect absolutely every matching homograph.
		/// </summary>
		/// <param name="sForm"></param>
		/// <param name="hvo"></param>
		/// <param name="entries"></param>
		/// <param name="nMorphType"></param>
		public static List<ILexEntry> CollectHomographs(string sForm, int hvo, List<ILexEntry> entries,
			int nMorphType)
		{
			return CollectHomographs(sForm, hvo, entries, nMorphType, false);
		}

		/// <summary>
		/// Collect all the homographs of the given form from the given list of entries.  If fMatchLexForms
		/// is true, then match against lexeme forms even if citation forms exist.  (This behavior is needed
		/// to fix LT-6024 for categorized entry.)
		/// </summary>
		/// <param name="sForm"></param>
		/// <param name="hvo"></param>
		/// <param name="entries"></param>
		/// <param name="nMorphType"></param>
		/// <param name="fMatchLexForms"></param>
		/// <returns></returns>
		internal static List<ILexEntry> CollectHomographs(string sForm, int hvo, List<ILexEntry> entries,
			int nMorphType, bool fMatchLexForms)
		{
			if (sForm == null || sForm == String.Empty || sForm == Strings.ksQuestions)		// was "??", not "???"
				return new List<ILexEntry>(0);
			if (entries.Count == 0)
				return new List<ILexEntry>(0);

			MoMorphTypeCollection typesCol = null;
			List<ILexEntry> rgHomographs = new List<ILexEntry>();
			// Treat stems and roots as equivalent, bound or unbound, as well as entries with no
			// idea what they are.
			if (nMorphType == MoMorphType.kmtBoundRoot ||
				nMorphType == MoMorphType.kmtBoundStem ||
				nMorphType == MoMorphType.kmtUnknown ||
				nMorphType == MoMorphType.kmtRoot ||
				nMorphType == MoMorphType.kmtParticle ||
				nMorphType == MoMorphType.kmtPhrase ||
				nMorphType == MoMorphType.kmtDiscontiguousPhrase)
			{
				nMorphType = MoMorphType.kmtStem;
			}

			FdoCache cache = entries[0].Cache;
			Debug.Assert(cache != null);
			try
			{
				cache.EnableBulkLoadingIfPossible(true);
				foreach (ILexEntry le in entries)
				{
					string homographForm = le.HomographForm;
					string lexemeHomograph = homographForm;
					if (fMatchLexForms)
						lexemeHomograph = LexemeFormStatic(le.Cache, le.Hvo);
					Debug.Assert(le != null);
					if (typesCol == null)
						typesCol = new MoMorphTypeCollection(cache);
					if (le.Hvo != hvo && (homographForm == sForm || lexemeHomograph == sForm))
					{
						List<IMoMorphType> types = le.MorphTypes;
						foreach (IMoMorphType mmt in types)
						{
							int nType = MoMorphType.FindMorphTypeIndex(cache, mmt);
							if (nType == MoMorphType.kmtBoundRoot ||
								nType == MoMorphType.kmtBoundStem ||
								nType == MoMorphType.kmtUnknown ||
								nType == MoMorphType.kmtRoot ||
								nType == MoMorphType.kmtParticle ||
								nType == MoMorphType.kmtPhrase ||
								nType == MoMorphType.kmtDiscontiguousPhrase)
							{
								nType = MoMorphType.kmtStem;
							}
							if (nType == nMorphType)
							{
								rgHomographs.Add(le);
								// Only add it once, even if it has multiple morph type matches.
								break;
							}
						}
						// Go ahead and use it, since it has no types at all, as may be the case
						// for entries created by the Rapid Data Entry tool.
						if (types.Count == 0)
							rgHomographs.Add(le);
					}
				}
			}
			finally
			{
				cache.EnableBulkLoadingIfPossible(false);
			}
			return rgHomographs;
		}

		/// <summary>
		/// Ensure that homograph numbers from 1 to N are set for these N homographs.
		/// This is called on both Insert Entry and Delete Entry.
		/// </summary>
		/// <returns>true if homographs were already valid, false if they had to be renumbered.
		/// </returns>
		public static bool ValidateExistingHomographs(List<ILexEntry> rgHomographs)
		{
			bool fOk = true;

			if (rgHomographs.Count == 0)
				return fOk; // Nothing to renumber.

			if (rgHomographs.Count == 1)
			{
				// Handle case where it is being set to 0.
				ILexEntry lexE = rgHomographs[0];
				if (lexE.HomographNumber != 0)
					lexE.HomographNumber = 0;
				return fOk;
			}

			//IEnumerator ie = rgHomographs.GetEnumerator();
			for (int n = 1; n <= rgHomographs.Count; ++n)
			{
				fOk = false;
				foreach (ILexEntry le in rgHomographs)
				{
					if (le.HomographNumber == n)
					{
						fOk = true;
						break;
					}
				}
				if (!fOk)
				{
					// See if one has a missing number. If so, fill it in with the
					// next needed number.
					foreach (ILexEntry le in rgHomographs)
					{
						if (le.HomographNumber == 0 && n < 256)
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
				int n = 1;
				foreach (ILexEntry le in rgHomographs)
				{
					le.HomographNumber = n;
					// Assigning any numbers > 255 results in a crash.  See LT-6382 and LT-9310.
					if (n < 255)
						++n;
				}
				if (rgHomographs.Count > 255)
				{
					string sForm = rgHomographs[0].CitationForm.VernacularDefaultWritingSystem;
					if (String.IsNullOrEmpty(sForm) && rgHomographs[0].LexemeFormOA != null)
						sForm = rgHomographs[0].LexemeFormOA.Form.VernacularDefaultWritingSystem;
					if (String.IsNullOrEmpty(sForm))
						sForm = rgHomographs[0].ShortName;
					string sMsg = String.Format(Strings.ksHomographLimits, rgHomographs.Count, sForm);
					System.Windows.Forms.MessageBox.Show(sMsg, Strings.ksWarning,
						System.Windows.Forms.MessageBoxButtons.OK,
						System.Windows.Forms.MessageBoxIcon.Warning);
				}
			}
			return fOk;
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
		/// The canonical unique name of a lexical entry.  This includes
		/// CitationFormWithAffixType (in this implementation) with the homograph number
		/// (if non-zero) appended as a subscript.
		/// </summary>
		public ITsString HeadWord
		{
			get
			{
				return HeadWordStaticForWs(m_cache, m_hvo, m_cache.DefaultVernWs);
			}
		}

		/// <summary>
		/// This allows us to get the headword without actually creating an instance...
		/// which can be slow.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		private static ITsString HeadWordStatic(FdoCache cache, int hvo)
		{
			return HeadWordStaticForWs(cache, hvo, cache.DefaultVernWs);
		}

		/// <summary>
		/// The canonical unique name of a lexical entry.  This includes
		/// CitationFormWithAffixType (in this implementation) with the homograph number
		/// (if non-zero) appended as a subscript.
		/// </summary>
		/// <param name="wsVern"></param>
		public ITsString HeadWordForWs(int wsVern)
		{
			return HeadWordStaticForWs(m_cache, m_hvo, wsVern);
		}

		/// <summary>
		/// This allows us to get the headword without actually creating an instance...
		/// which can be slow.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="wsVern"></param>
		/// <returns></returns>
		private static ITsString HeadWordStaticForWs(FdoCache cache, int hvo, int wsVern)
		{
			if (wsVern <= 0)
				wsVern = cache.DefaultVernWs;
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsVern);
			tisb.Append(CitationFormWithAffixTypeStaticForWs(cache, hvo, wsVern));
			// (EricP) Tried to automatically update the homograph number, but doing that here will
			// steal away manual changes to the HomographNumber column. Also suppressing PropChanged
			// is necessary when HomographNumber column is enabled, otherwise changing the entry index can hang.
			//using (new IgnorePropChanged(cache, PropChangedHandling.SuppressView))
			//{
			//	  ValidateExistingHomographs(CollectHomographs(cache, ShortName1StaticForWs(cache, hvo, wsVern), 0, morphType));
			//}
			int nHomograph = cache.MainCacheAccessor.get_IntProp(hvo,
				(int)LexEntry.LexEntryTags.kflidHomographNumber);
			if (nHomograph > 0)
			{
				tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwSuperscriptVal.kssvSub);
				// Bold subscript is easier to read (cf. LT-3177).
				tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				tisb.Append(nHomograph.ToString());
			}
			return tisb.GetString();
		}

		/// <summary>
		/// The Lexeme form with an indication of the affix type.
		/// </summary>
		public string LexemeFormWithAffixType
		{
			get
			{
				return LexemeFormWithAffixTypeHomographStatic(m_cache, m_hvo, false);
			}
		}

		/// <summary>
		/// The Lexeme form with an indication of the affix type and homograph.
		/// </summary>
		public string LexemeFormWithAffixTypeHomograph
		{
			get
			{
				return LexemeFormWithAffixTypeHomographStatic(m_cache, m_hvo, true);
			}
		}

		/// <summary>
		/// Static version for cases where we can't afford to create the object.
		/// Shows lexeme form, morph type, and homograph number.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="fHomograph"></param>
		/// <returns></returns>
		public static string LexemeFormWithAffixTypeHomographStatic(FdoCache cache, int hvo,
			bool fHomograph)
		{
			// This is optimized for speed, in case you were wondering.  PreLoadShortName()
			// loads all of the data this references directly.
			ISilDataAccess sda = cache.MainCacheAccessor;
			int hvoType = 0;
			int hvoLexForm = sda.get_ObjectProp(hvo, (int)LexEntry.LexEntryTags.kflidLexemeForm);
			string form = string.Empty;
			if (hvoLexForm != 0)
			{
				hvoType = sda.get_ObjectProp(hvoLexForm, (int)MoForm.MoFormTags.kflidMorphType);
				form = sda.get_MultiStringAlt(hvoLexForm, (int)MoForm.MoFormTags.kflidForm, cache.DefaultVernWs).Text;
			}
			string prefix = string.Empty;
			string postfix = string.Empty;
			if (hvoType > 0) // It may be null.
			{
				prefix = sda.get_UnicodeProp(hvoType, (int)MoMorphType.MoMorphTypeTags.kflidPrefix);
				postfix = sda.get_UnicodeProp(hvoType, (int)MoMorphType.MoMorphTypeTags.kflidPostfix);
			}
			form = prefix + form + postfix;
			if (fHomograph)
			{
				int nHomograph = sda.get_IntProp(hvo, (int)LexEntry.LexEntryTags.kflidHomographNumber);
				if (nHomograph > 0)
				{
					form += nHomograph;
				}
			}
			return form;
		}

		/// <summary>
		/// Tells whether this LexEntry contains an inflectional affix MSA
		/// </summary>
		/// <returns></returns>
		public bool SupportsInflectionClasses()
		{
			//enhance: this will be rather slow... could have a specialized methods on
			//the collection to check for classes of a given type.
			foreach (IMoMorphSynAnalysis item in MorphoSyntaxAnalysesOC)
			{
				if ((item is IMoInflAffMsa && (item as IMoInflAffMsa).PartOfSpeechRAHvo != 0)
					|| (item is IMoDerivAffMsa && (item as IMoDerivAffMsa).FromInflectionClassRAHvo != 0))
				{
						return true;
				}
			}
			return false;
		}
		/// <summary>
		/// The Citation form with an indication of the affix type.
		/// </summary>
		public string CitationFormWithAffixType
		{
			get
			{
				return CitationFormWithAffixTypeStaticForWs(m_cache, m_hvo,
					m_cache.DefaultVernWs);
			}
		}

		/// <summary>
		/// Static version for cases where we can't afford to create the object.
		/// Note that the name is now somewhat dubious...Form of LexemeForm is shown by
		/// preference.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static string CitationFormWithAffixTypeStatic(FdoCache cache, int hvo)
		{
			// This is optimized for speed, in case you were wondering.  PreLoadShortName()
			// loads all of the data this references directly.
			return CitationFormWithAffixTypeStaticForWs(cache, hvo, cache.DefaultVernWs);
		}

		/// <summary>
		/// Append to the string builder text equivalent to CitationFormWithAffixTypeStatic, but
		/// with the correct writing systems.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="tsb"></param>
		public static void CitationFormWithAffixTypeTss(FdoCache cache, int hvo, ITsIncStrBldr tsb)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			int hvoType = 0;
			int hvoLexForm = sda.get_ObjectProp(hvo, (int)LexEntry.LexEntryTags.kflidLexemeForm);
			if (hvoLexForm != 0)
			{
				hvoType = sda.get_ObjectProp(hvoLexForm, (int)MoForm.MoFormTags.kflidMorphType);
			}
			else
			{
				// No type info...return simpler version of name.
				ShortName1Static(cache, hvo, tsb);
				return;
			}
			string prefix = string.Empty;
			string postfix = string.Empty;
			if (hvoType > 0) // It may be null.
			{
				prefix = sda.get_UnicodeProp(hvoType, (int)MoMorphType.MoMorphTypeTags.kflidPrefix);
				postfix = sda.get_UnicodeProp(hvoType, (int)MoMorphType.MoMorphTypeTags.kflidPostfix);
			}
			// The following code for setting Ws and FontFamily are to fix LT-6238.
			if (!String.IsNullOrEmpty(prefix))
			{
				tsb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, cache.DefaultVernWs);
				tsb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Doulos SIL");
				tsb.Append(prefix);
			}
			ShortName1Static(cache, hvo, tsb);
			if (!String.IsNullOrEmpty(postfix))
			{
				tsb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, cache.DefaultVernWs);
				tsb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Doulos SIL");
				tsb.Append(postfix);
			}
		}

		/// <summary>
		/// The Citation form with an indication of the affix type.  This returns null if there
		/// is not a citation form in the given writing system.
		/// </summary>
		/// <param name="wsVern"></param>
		public string CitationFormWithAffixTypeForWs(int wsVern)
		{
			string form = this.CitationForm.GetAlternative(wsVern);
			if (String.IsNullOrEmpty(form))
				return null;
			else
				return DecorateFormWithAffixMarkers(m_cache, this.Hvo, form);
		}

		/// <summary>
		/// Static version for cases where we can't afford to create the object.
		/// Note that the name is now somewhat dubious...This shows citation form if present
		/// otherwise lexeme form, otherwise question marks. Affix markers are added.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="wsVern"></param>
		/// <returns></returns>
		public static string CitationFormWithAffixTypeStaticForWs(FdoCache cache, int hvo,
			int wsVern)
		{
			// This is optimized for speed, in case you were wondering.  PreLoadShortName()
			// loads all of the data this references directly.
			if (wsVern <= 0)
				wsVern = cache.DefaultVernWs;
			string form = ShortName1StaticForWs(cache, hvo, wsVern);
			return DecorateFormWithAffixMarkers(cache, hvo, form);
		}

		private static string DecorateFormWithAffixMarkers(FdoCache cache, int hvo, string form)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			int hvoType = 0;
			int hvoLexForm = sda.get_ObjectProp(hvo,
				(int)LexEntry.LexEntryTags.kflidLexemeForm);
			if (hvoLexForm != 0)
			{
				hvoType = sda.get_ObjectProp(hvoLexForm, (int)MoForm.MoFormTags.kflidMorphType);
			}
			else
			{
				// No type info...return simpler version of name.
				return form;
			}
			string prefix = string.Empty;
			string postfix = string.Empty;
			if (hvoType > 0) // It may be null.
			{
				prefix = sda.get_UnicodeProp(hvoType,
					(int)MoMorphType.MoMorphTypeTags.kflidPrefix);
				postfix = sda.get_UnicodeProp(hvoType,
					(int)MoMorphType.MoMorphTypeTags.kflidPostfix);
			}
			return prefix + form + postfix;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find an allomorph with the specified form, if any. Searches both LexemeForm and
		/// AlternateForms properties.
		/// </summary>
		/// <param name="tssform">The tssform.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IMoForm FindMatchingAllomorph(ITsString tssform)
		{
			IMoForm lf = LexemeFormOA;
			int wsVern = StringUtils.GetWsAtOffset(tssform, 0);
			string form = tssform.Text;
			if (lf != null && lf.Form.GetAlternative(wsVern) == form)
				return lf;
			foreach (IMoForm mf in AlternateFormsOS)
			{
				if (mf.Form.GetAlternative(wsVern) == form)
				{
					return mf;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the LexEntryRef
		/// objects owned by this LexEntry that define this entry as a complex form.
		/// Note: this must stay in sync with LoadAllComplexFormEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> ComplexFormEntryRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT er.Dst " +
					"FROM LexEntry_EntryRefs er " +
					"JOIN LexEntryRef ler ON ler.Id=er.Dst AND ler.RefType=1 " +
					"WHERE er.Src={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that have define their owners as
		/// complex forms.  The keys in the dictionary are all the LexEntry objects that own
		/// these LexEntryRef objects, and the values are the lists of the owned LexEntryRef
		/// objects that define their owners as complex forms.
		/// Note: this must stay in sync with ComplexFormEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllComplexFormEntryRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT er.Src, er.Dst " +
				"FROM LexEntry_EntryRefs er " +
				"JOIN LexEntryRef ler ON ler.Id=er.Dst AND ler.RefType=1 " +
				"ORDER BY er.Src";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the LexEntryRef
		/// objects owned by this LexEntry that define this entry as a variant.
		/// Note: this must stay in sync with LoadAllVariantEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> VariantEntryRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT er.Dst " +
					"FROM LexEntry_EntryRefs er " +
					"JOIN LexEntryRef ler ON ler.Id=er.Dst AND ler.RefType=0 " +
					"WHERE er.Src={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that define their owners as
		/// variants.  The keys in the dictionary are all the LexEntry objects that own these
		/// LexEntryRef objects, and the values are the lists of the owned LexEntryRef objects
		/// that define their owners as variants.
		/// Note: this must stay in sync with VariantEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllVariantEntryRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT er.Src, er.Dst " +
				"FROM LexEntry_EntryRefs er " +
				"JOIN LexEntryRef ler ON ler.Id=er.Dst AND ler.RefType=0 " +
				"ORDER BY er.Src";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry as a primary component of a complex
		/// form.
		/// Note: this must stay in sync with LoadAllComplexFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> ComplexFormEntryBackRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT pl.Src " +
					"FROM LexEntryRef_PrimaryLexemes pl " +
					"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
					// If the form is a variant, don't treat it as a complex form even if it is.  See LT-9566.
					"JOIN LexEntry_EntryRefs er ON er.Dst=ler.Id " +
					"LEFT OUTER JOIN LexEntry_EntryRefs er2 ON er2.Src=er.Src AND er2.Dst<>er.Dst " +
					"LEFT OUTER JOIN LexEntryRef ler2 ON ler2.Id=er2.Dst AND ler2.RefType=0 " +
					"WHERE pl.Dst={0} AND ler2.Id IS NULL", this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that refer to a LexEntry as a
		/// primary component of a complex form.  The keys in the dictionary are all the
		/// LexEntry objects that are thus referenced, and the values are the lists of
		/// LexEntryRef objects that refer to the keys.
		/// Note: this must stay in sync with ComplexFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllComplexFormEntryBackRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT pl.Dst, pl.Src " +
				"FROM LexEntryRef_PrimaryLexemes pl " +
				"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
				"JOIN LexEntry le ON le.Id=pl.Dst " +
				// If the form is a variant, don't treat it as a complex form even if it is.  See LT-9566.
				"JOIN LexEntry_EntryRefs er ON er.Dst=ler.Id " +
				"LEFT OUTER JOIN LexEntry_EntryRefs er2 ON er2.Src=er.Src AND er2.Dst<>er.Dst " +
				"LEFT OUTER JOIN LexEntryRef ler2 ON ler2.Id=er2.Dst AND ler2.RefType=0 " +
				"WHERE ler2.Id IS NULL " +
				"ORDER BY pl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry, or to a LexSense owned by this
		/// LexEntry (possibly indirectly), as a primary component of a complex form.
		/// Note: this must stay in sync with LoadAllAllComplexFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> AllComplexFormEntryBackRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT pl.Src " +
					"FROM LexEntryRef_PrimaryLexemes pl " +
					"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
					// If the form is a variant, don't treat it as a complex form even if it is.  See LT-9566.
					"JOIN LexEntry_EntryRefs er ON er.Dst=ler.Id " +
					"LEFT OUTER JOIN LexEntry_EntryRefs er2 ON er2.Src=er.Src AND er2.Dst<>er.Dst " +
					"LEFT OUTER JOIN LexEntryRef ler2 ON ler2.Id=er2.Dst AND ler2.RefType=0 " +
					"WHERE ler2.Id IS NULL AND (pl.Dst={0} OR dbo.fnGetEntryForSense(pl.Dst)={0})",
					m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that refer to a LexEntry as a
		/// primary component of a complex form, or to a sense owned by the LexEntry.  The keys
		/// in the dictionary are all the LexEntry objects that are thus referenced, and the
		/// values are the lists of LexEntryRef objects that refer to the keys.
		/// Note: this must stay in sync with AllComplexFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllAllComplexFormEntryBackRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry =
				"SELECT DISTINCT Entry, EntryRef FROM dbo.fnGetAllComplexFormEntryBackRefs() ORDER BY Entry";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexEntry as a variant (component).
		/// Note: this must stay in sync with LoadAllVariantFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> VariantFormEntryBackRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT cl.Src " +
					"FROM LexEntryRef_ComponentLexemes cl " +
					"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
					"WHERE cl.Dst={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that refer to a LexEntry as a
		/// variant (component).  The keys in the dictionary are all the LexEntry objects that
		/// are thus referenced, and the values are the lists of LexEntryRef objects that refer
		/// to the keys.
		/// Note: this must stay in sync with VariantFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllVariantFormEntryBackRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT cl.Dst, cl.Src " +
				"FROM LexEntryRef_ComponentLexemes cl " +
				"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
				"JOIN LexEntry le ON le.Id=cl.Dst " +
				"ORDER BY cl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// <summary>
		/// This method implements a virtual property and so may be called by reflection.
		/// An entry gets displayed as a subentry (at least in a root-based dictionary)
		/// if it has at least one LexEntryRef that has a non-empty PrimaryLexemes list.
		/// </summary>
		public bool IsASubentry
		{
			get
			{
				foreach (LexEntryRef ler in EntryRefsOS)
					if (ler.PrimaryLexemesRS.Count > 0)
						return true;
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry in its
		/// PrimaryLexemes field and that has a nonempty ComplexEntryTypes field.
		/// Note: this must stay in sync with LoadAllComplexFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> ComplexFormEntries
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT er.Src " +
					"FROM LexEntryRef_PrimaryLexemes pl " +
					"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
					"JOIN LexEntry_EntryRefs er ON er.Dst=pl.Src " +
					// If the form is a variant, don't treat it as a complex form even if it is.  See LT-9566.
					"LEFT OUTER JOIN LexEntry_EntryRefs er2 ON er2.Src=er.Src AND er2.Dst<>er.Dst " +
					"LEFT OUTER JOIN LexEntryRef ler2 ON ler2.Id=er2.Dst AND ler2.RefType=0 " +
					"WHERE pl.Dst={0} AND ler2.Id IS NULL",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntry objects that own a LexEntryRef object that
		/// refers to a LexEntry as a component of a complex form.  The keys in the dictionary
		/// are all the LexEntry objects that are thus referenced, and the values are the lists
		/// of the LexEntry objects that own the LexEntryRef objects that refer to the keys.
		/// Note: this must stay in sync with ComplexFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllComplexFormEntries(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT pl.Dst, er.Src " +
				"FROM LexEntryRef_PrimaryLexemes pl " +
				"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
				"JOIN LexEntry_EntryRefs er ON er.Dst=pl.Src " +
				"JOIN LexEntry le ON le.Id=pl.Dst " +
				// If the form is a variant, don't treat it as a complex form even if it is.  See LT-9566.
				"LEFT OUTER JOIN LexEntry_EntryRefs er2 ON er2.Src=er.Src AND er2.Dst<>er.Dst " +
				"LEFT OUTER JOIN LexEntryRef ler2 ON ler2.Id=er2.Dst AND ler2.RefType=0 " +
				"WHERE ler2.Id IS NULL " +
				"ORDER BY pl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own a LexEntryRef that refers to this LexEntry as a variant
		/// (component).
		/// Note: this must stay in sync with LoadAllVariantFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> VariantFormEntries
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT er.Src " +
					"FROM LexEntryRef_ComponentLexemes cl " +
					"JOIN LexEntry_EntryRefs er ON er.Dst=cl.Src " +
					"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
					"WHERE cl.Dst={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that refer to a LexEntry as a
		/// variant (component).  The keys in the dictionary are all the LexEntry objects that
		/// are thus referenced, and the values are the lists of the LexEntry objects that own
		/// the LexEntryRef objects that refer to the keys.
		/// Note: this must stay in sync with VariantFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllVariantFormEntries(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT cl.Dst, er.Src " +
				"FROM LexEntryRef_ComponentLexemes cl " +
				"JOIN LexEntry_EntryRefs er ON er.Dst=cl.Src " +
				"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
				"JOIN LexEntry le ON le.Id=cl.Dst " +
				"ORDER BY cl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the LexEntryRef
		/// objects owned by this LexEntry that have HideMinorEntry set to zero.
		/// Note: this must stay in sync with LoadAllVisibleEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> VisibleEntryRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT ler.Id " +
					"FROM LexEntryRef ler " +
					"JOIN LexEntry_EntryRefs er ON er.Dst=ler.Id AND er.Src={0} " +
					"WHERE ler.HideMinorEntry=0", this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that have HideMinorEntry set to
		/// zero.  The keys in the dictionary are all the LexEntry objects that own these
		/// LexEntryRef objects, and the values are the lists of the owned LexEntryRef objects
		/// that have HideMinorEntry set to zero.
		/// Note: this must stay in sync with VisibleEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllVisibleEntryRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT er.Src, ler.Id " +
				"FROM LexEntryRef ler " +
				"JOIN LexEntry_EntryRefs er ON er.Dst=ler.Id " +
				"WHERE ler.HideMinorEntry=0 " +
				"ORDER BY er.Src";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the LexEntryRef
		/// objects owned by this LexEntry that have HideMinorEntry set to zero and that define
		/// this LexEntry as a variant.
		/// Note: this must stay in sync with LoadAllVisibleVariantEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> VisibleVariantEntryRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT er.Dst " +
					"FROM LexEntry_EntryRefs er " +
					"JOIN LexEntryRef ler ON ler.Id=er.Dst AND ler.HideMinorEntry=0 AND ler.RefType=0 " +
					"WHERE er.Src={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that have HideMinorEntry set to
		/// zero and that define their owner as a variant.  The keys in the dictionary are all
		/// the LexEntry objects that own these LexEntryRef objects, and the values are the
		/// lists of the owned LexEntryRef objects that have HideMinorEntry set to zero and that
		/// define their owners as variants.
		/// Note: this must stay in sync with VisibleVariantEntryRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllVisibleVariantEntryRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT er.Src, er.Dst " +
				"FROM LexEntry_EntryRefs er " +
				"JOIN LexEntryRef ler ON ler.Id=er.Dst AND ler.HideMinorEntry=0 AND ler.RefType=0 " +
				"ORDER BY er.Src";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// <summary>
		/// Check whether this entry should be published as a minor entry.  Although it's essentially
		/// a boolean function, it returns an integer because it is referenced as a virtual property.
		/// </summary>
		/// <returns></returns>
		public int PublishAsMinorEntry
		{
			get
			{
				foreach (LexEntryRef ler in EntryRefsOS)
				{
					if (ler.HideMinorEntry == 0)
						return 1;
				}
				return EntryRefsOS.Count == 0 ? 1 : 0;
			}
		}

		/// <summary>
		/// This is a backreference (virtual) method.  It returns the list of object ids for
		/// all the LexRefTypes that contain a LexReference that includes this LexEntry.
		/// </summary>
		public List<int> LexRefTypes()
		{
			LexEntryReferencesVirtualHandler vh = new LexEntryReferencesVirtualHandler(m_cache);
			if (vh != null)
			{
				return vh.LexRefTypes(this.Hvo);
			}
			return null;
		}

		/// <summary>
		/// This is a backreference (virtual) method.  It returns the list of object ids for
		/// all the LexReferences that contain this LexSense/LexEntry.
		/// Note this is called on SFM export by mdf.xml so needs to be a property.
		/// </summary>
		public List<int> LexReferences
		{
			get
			{
				LexEntryReferencesVirtualHandler vh = new LexEntryReferencesVirtualHandler(m_cache);
				if (vh != null)
				{
					return vh.LexReferences(this.Hvo);
				}
				return null;
			}
		}

		/// <summary>
		/// Determines if the entry is a circumfix
		/// </summary>
		/// <returns></returns>
		public bool IsCircumfix()
		{
			IMoForm form = LexemeFormOA;
			if (form != null)
			{
				IMoMorphType type = form.MorphTypeRA;
				if (type != null)
				{
					if (type.Guid.ToString() == MoMorphType.kguidMorphCircumfix)
						return true;
				}
			}
			return false;
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
					IMoForm[] result = AlternateFormsOS.ToArray();
					return result;
				}
				else
				{
					List<IMoForm> results = new List<IMoForm>();
					results.Add(LexemeFormOA);
					results.AddRange(AlternateFormsOS.ToArray());
					return results.ToArray();
				}
			}
		}

		/// <summary>
		/// If entry has a LexemeForm, that type is primary and should be used for new ones (LT-4872).
		/// </summary>
		/// <returns></returns>
		public int GetDefaultClassForNewAllomorph()
		{
			int newObjectClassId = 0;
			IMoForm mainForm = this.LexemeFormOA;
			int morphType = this.MorphType;
			if (mainForm != null && mainForm.MorphTypeRAHvo != 0)
				morphType = MoMorphType.FindMorphTypeIndex(m_cache, mainForm.MorphTypeRA);
			if (MoMorphType.IsAffixType(morphType))
				newObjectClassId = MoAffixAllomorph.kclsidMoAffixAllomorph;
			else
				newObjectClassId = MoStemAllomorph.kclsidMoStemAllomorph;
			return newObjectClassId;
		}

		/// <summary>
		/// creates a variant entry from this (main) entry,
		/// and links the variant to this (main) entry via
		/// EntryRefs.ComponentLexemes
		///
		/// NOTE: The caller will need to supply the lexemeForm subsequently.
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType)
		{
			return CreateVariantEntryAndBackRef(this, variantType, null);
		}

		/// <summary>
		/// creates a variant entry from this (main) entry,
		/// and links the variant to this (main) entry via
		/// EntryRefs.ComponentLexemes
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <param name="tssVariantLexemeForm">the lexeme form of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType, ITsString tssVariantLexemeForm)
		{
			return CreateVariantEntryAndBackRef(this, variantType, tssVariantLexemeForm);
		}

		/// <summary>
		/// creates a variant entry from this (main) entry using its lexemeForm information,
		/// and links the variant to the given componentLexeme entry via
		/// EntryRefs.ComponentLexemes
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <param name="componentLexeme">the entry or sense of which the new variant entry is a variant</param>
		/// <param name="tssVariantLexemeForm">the lexeme form of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		internal ILexEntryRef CreateVariantEntryAndBackRef(IVariantComponentLexeme componentLexeme, ILexEntryType variantType, ITsString tssVariantLexemeForm)
		{
			using (new UndoRedoTaskHelper(Cache, Strings.ksUndoCreateVariantEntry, Strings.ksRedoCreateVariantEntry))
			{
				ILexEntry variantEntry = new LexEntry();
				Cache.LangProject.LexDbOA.EntriesOC.Add(variantEntry);
				if (this.LexemeFormOA is IMoAffixAllomorph)
					variantEntry.LexemeFormOA = new MoAffixAllomorph();
				else
					variantEntry.LexemeFormOA = new MoStemAllomorph();
				if (this.LexemeFormOA != null)
					variantEntry.LexemeFormOA.MorphTypeRAHvo = this.LexemeFormOA.MorphTypeRAHvo;
				if (tssVariantLexemeForm != null)
					variantEntry.LexemeFormOA.FormMinusReservedMarkers = tssVariantLexemeForm;
				(variantEntry as LexEntry).UpdateHomographNumbersAccountingForNewEntry();
				return (variantEntry as LexEntry).MakeVariantOf(componentLexeme, variantType);
			}
		}

		/// <summary>
		/// Make this entry a variant of the given componentLexeme (primary entry) with
		/// the given variantType
		/// </summary>
		/// <param name="componentLexeme"></param>
		/// <param name="variantType"></param>
		public ILexEntryRef MakeVariantOf(IVariantComponentLexeme componentLexeme, ILexEntryType variantType)
		{
			ILexEntryRef ler = this.EntryRefsOS.Append(new LexEntryRef());
			ler.RefType = LexEntryRef.krtVariant; // variant by default, but good to be explicit here.
			ler.HideMinorEntry = 0;
			ler.ComponentLexemesRS.Append(componentLexeme);
			if (variantType != null)
				ler.VariantEntryTypesRS.Append(variantType);
			return ler;
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
				if (ler.RefType == LexEntryRef.krtVariant &&
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
				matchinEntryRef = this.FindMatchingVariantEntryRef(senseTargetComponent, null);
				if (matchinEntryRef == null)
				{
					// must be in relationship with the sense's entry, rather than the sense.
					matchinEntryRef = this.FindMatchingVariantEntryRef(senseTargetComponent.Entry, null);
				}
			}
			return matchinEntryRef != null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mainEntryOrSense"></param>
		/// <param name="variantEntryType"></param>
		/// <param name="targetVariantLexemeForm"></param>
		/// <returns></returns>
		public static ILexEntryRef FindMatchingVariantEntryBackRef(IVariantComponentLexeme mainEntryOrSense,
			ILexEntryType variantEntryType, ITsString targetVariantLexemeForm)
		{
			ILexEntryRef matchingEntryRef = null;
			foreach (int hvoLexEntryRef in mainEntryOrSense.VariantFormEntryBackRefs)
			{
				ILexEntryRef ler = LexEntryRef.CreateFromDBObject((mainEntryOrSense as ICmObject).Cache, hvoLexEntryRef);
				// this only handles matching single component lexemes,
				// so we only try to match those.
				if (ler.ComponentLexemesRS.Count == 1)
				{
					// next see if we can match on the same variant lexeme form
					ILexEntry variantEntry = (ler as CmObject).Owner as ILexEntry;
					if (variantEntry.LexemeFormOA == null || variantEntry.LexemeFormOA.Form == null)
						continue;
					int wsTargetVariant = StringUtils.GetWsAtOffset(targetVariantLexemeForm, 0);
					if (targetVariantLexemeForm.Equals(variantEntry.LexemeFormOA.Form.GetAlternativeTss(wsTargetVariant)))
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
		///
		/// </summary>
		/// <param name="ls"></param>
		public void MoveSenseToCopy(ILexSense ls)
		{
			m_cache.BeginUndoTask(Strings.ksUndoCreateEntry, Strings.ksRedoCreateEntry);
			// CreateObject creates the entry without a PropChanged.
			int entryHvo = m_cache.CreateObject(LexEntry.kClassId,
				m_cache.LangProject.LexDbOAHvo,
				(int)LexDb.LexDbTags.kflidEntries,
				0); // 0 is fine, since the entries prop is not a sequence.
			// Copy all the basic properties.
			ILexEntry leNew = LexEntry.CreateFromDBObject(m_cache, entryHvo);
			leNew.CitationForm.MergeAlternatives(this.CitationForm);
			leNew.Bibliography.MergeAlternatives(this.Bibliography);
			leNew.Comment.MergeAlternatives(this.Comment);
			leNew.DoNotUseForParsing = this.DoNotUseForParsing;
			leNew.ExcludeAsHeadword = this.ExcludeAsHeadword;
			leNew.LiteralMeaning.MergeAlternatives(this.LiteralMeaning);
			leNew.Restrictions.MergeAlternatives(this.Restrictions);
			leNew.SummaryDefinition.MergeAlternatives(this.SummaryDefinition);
			// Copy the reference attributes.

			// Copy the owned attributes carefully.
			if (this.LexemeFormOAHvo != 0)
				m_cache.CopyObject(this.LexemeFormOAHvo, leNew.Hvo, (int)LexEntryTags.kflidLexemeForm);

			m_cache.CopyOwningSequence(AlternateFormsOS, leNew.Hvo);
			m_cache.CopyOwningSequence(PronunciationsOS, leNew.Hvo);
			m_cache.CopyOwningSequence(EntryRefsOS, leNew.Hvo);

			if (this.EtymologyOAHvo != 0)
				m_cache.CopyObject(this.EtymologyOAHvo, leNew.Hvo, (int)LexEntryTags.kflidEtymology);

			Dictionary<int, int> msaHvos = new Dictionary<int, int>(4);
			(leNew as LexEntry).ReplaceMsasForSense(ls, msaHvos);
			leNew.SensesOS.Append(ls);
			// Delete any MSAs that were used only by the moved sense.  See LT-9952.
			DeleteUnusedMSAs();

			// Handle homograph number.
			leNew.HomographNumber = 0;
			// Not all "CollectHomographs" methods use the calling entry.
			// Make sure we use one that does in this case.
			List<ILexEntry> homographs = CollectHomographs(
				leNew.HomographForm, // This should not have the markers.
				0, // Ensures we get all of them, including this new one.
				GetHomographList(m_cache, leNew.HomographForm),
				leNew.MorphType);
			LexEntry.ValidateExistingHomographs(homographs);

			// CopyObject begins a transaction if one is not already open.
			// But (JohnT, see LT-5293), there @#$%^ well should be one open,
			// because we're in a BeginUndoTask/EndUndoTask block, and if we close it here,
			// EndUndoTask tries to close it again and crashes.
			//if (m_cache.DatabaseAccessor.IsTransactionOpen())
			//    m_cache.DatabaseAccessor.CommitTrans();

			m_cache.EndUndoTask();

			m_cache.MainCacheAccessor.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				this.OwnerHVO, this.OwningFlid, this.OwnOrd, 1, 0);
		}

		/// <summary>
		/// Delete any MSAs that are no longer referenced by a sense (or subsense).
		/// </summary>
		private void DeleteUnusedMSAs()
		{
			Set<int> msasUsed = new Set<int>();
			foreach (int hvo in this.AllSenseHvos)
			{
				int hvoMsa = m_cache.GetObjProperty(hvo, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis);
				if (hvoMsa != 0)
					msasUsed.Add(hvoMsa);
			}
			Set<int> hvosObsolete = new Set<int>();
			foreach (int hvo in this.MorphoSyntaxAnalysesOC.HvoArray)
			{
				if (!msasUsed.Contains(hvo))
					hvosObsolete.Add(hvo);
			}
			if (hvosObsolete.Count > 0)
				CmObject.DeleteObjects(hvosObsolete, m_cache);
		}

		/// <summary>
		/// This goes through the LexSense and all its subsenses to create the needed MSAs on
		/// the current LexEntry, replacing all those found in the LexSense.
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="msaHvos"></param>
		private void ReplaceMsasForSense(ILexSense ls, Dictionary<int, int> msaHvos)
		{
			int hvoMsaOld = ls.MorphoSyntaxAnalysisRAHvo;
			if (hvoMsaOld != 0)
			{
				int hvoMsaNew;
				if (!msaHvos.TryGetValue(hvoMsaOld, out hvoMsaNew))
				{
					hvoMsaNew = m_cache.CopyObject(hvoMsaOld, this.Hvo,
						(int)LexEntryTags.kflidMorphoSyntaxAnalyses);
					msaHvos[hvoMsaOld] = hvoMsaNew;
				}
				ls.MorphoSyntaxAnalysisRAHvo = hvoMsaNew;
			}
			for (int i = 0; i < ls.SensesOS.Count; ++i)
			{
				ReplaceMsasForSense(ls.SensesOS[i], msaHvos);
			}
		}

		/// <summary>
		/// Get the HVO for an appropriate default MoMorphSynAnalysis belonging to this
		/// entry, creating it if necessary.
		/// </summary>
		/// <returns></returns>
		public int FindOrCreateDefaultMsa()
		{
			// Search for an appropriate MSA already existing for the LexEntry.
			foreach (IMoMorphSynAnalysis msa in this.MorphoSyntaxAnalysesOC)
			{
				if (MoMorphType.IsAffixType(this.MorphType))
				{
					if (msa is MoUnclassifiedAffixMsa)
						return msa.Hvo;
				}
				else
				{
					if (msa is MoStemMsa)
						return msa.Hvo;
				}
			}
			// Nothing exists, create the needed MSA.
			MoMorphSynAnalysis msaNew;
			if (MoMorphType.IsAffixType(MorphType))
				msaNew = new MoUnclassifiedAffixMsa();
			else
				msaNew = new MoStemMsa();
			msaNew.InitNew(this.Cache, this.Hvo, this.MorphoSyntaxAnalysesOC.Flid, 0);
			MorphoSyntaxAnalysesOC.Add(msaNew);
			return msaNew.Hvo;
		}

		/// <summary>
		/// Provide the headword plus an abbreviated indication of the entry type.
		/// </summary>
		public ITsString HeadWordAndType
		{
			get
			{
				ITsIncStrBldr tisb = this.HeadWord.GetIncBldr();
				if (this.EntryRefsOS.Count > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
					tisb.Append(" (");
					tisb.AppendTsString(this.ComputeEntryTypeTss());
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
					tisb.Append(")");
				}
				return tisb.GetString();
			}
		}

		private ITsString ComputeEntryTypeTss()
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			foreach (LexEntryRef ler in this.EntryRefsOS)
			{
				if (tisb.Text != null && tisb.Text.Length > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
					tisb.Append("; ");
				}
				bool fFirst = true;
				foreach (LexEntryType type in ler.ComplexEntryTypesRS)
				{
					if (!fFirst)
					{
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
						tisb.Append(", ");
					}
					tisb.AppendTsString(type.ReverseAbbr.BestAnalysisAlternative);
					fFirst = false;
				}
				foreach (LexEntryType type in ler.VariantEntryTypesRS)
				{
					if (!fFirst)
					{
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
						tisb.Append(", ");
					}
					tisb.AppendTsString(type.ReverseAbbr.BestAnalysisAlternative);
					fFirst = false;
				}
			}
			return tisb.GetString();
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
		/// This replaces a MoForm belonging to this LexEntry with another one, presumably
		/// changing from a stem to an affix or vice versa.  (A version of this code originally
		/// appeared in MorphTypeAtomicLauncher.cs, but is also needed for LIFT import.)
		/// </summary>
		/// <param name="mfOld"></param>
		/// <param name="mfNew"></param>
		public void ReplaceMoForm(IMoForm mfOld, IMoForm mfNew)
		{
			// save the environment references, if any.
			int[] envs = null;
			if (mfOld is IMoStemAllomorph)
				envs = (mfOld as IMoStemAllomorph).PhoneEnvRC.HvoArray;
			else if (mfOld is IMoAffixAllomorph)
				envs = (mfOld as IMoAffixAllomorph).PhoneEnvRC.HvoArray;
			else
				envs = new int[0];

			int[] inflClasses = null;
			if (mfOld is IMoAffixForm)
				inflClasses = (mfOld as IMoAffixForm).InflectionClassesRC.HvoArray;
			else
				inflClasses = new int[0];

			// if we are converting from one affix form to another, we should save the morph type
			int oldAffMorphType = 0;
			if (mfOld is IMoAffixForm)
				oldAffMorphType = mfOld.MorphTypeRAHvo;

			if (mfOld.OwningFlid == (int)LexEntry.LexEntryTags.kflidLexemeForm)
			{
				this.AlternateFormsOS.Append(mfNew); // trick to get it to be in DB so SwapReferences works
			}
			else
			{
				// insert the new form in the right location in the sequence.
				Debug.Assert(mfOld.OwningFlid == (int)LexEntry.LexEntryTags.kflidAlternateForms);
				bool fInserted = false;
				for (int i = 0; i < this.AlternateFormsOS.Count; ++i)
				{
					if (this.AlternateFormsOS.HvoArray[i] == mfOld.Hvo)
					{
						this.AlternateFormsOS.InsertAt(mfNew, i);
						fInserted = true;
						break;
					}
				}
				if (!fInserted)
					this.AlternateFormsOS.Append(mfNew);		// This should NEVER happen, but...
			}
			mfOld.SwapReferences(mfNew.Hvo);
			MultiUnicodeAccessor muaOrigForm = mfOld.Form;
			MultiUnicodeAccessor muaNewForm = mfNew.Form;
			muaNewForm.MergeAlternatives(muaOrigForm);
			if (mfOld.OwningFlid == (int)LexEntry.LexEntryTags.kflidLexemeForm)
				this.LexemeFormOA = mfNew;		// do we need to remove it from AlternateFormsOS??
			else
				this.AlternateFormsOS.Remove(mfOld);
			// restore the environment references, if any.
			foreach (int hvo in envs)
			{
				if (mfNew is IMoStemAllomorph)
					(mfNew as IMoStemAllomorph).PhoneEnvRC.Add(hvo);
				else if (mfNew is IMoAffixAllomorph)
					(mfNew as IMoAffixAllomorph).PhoneEnvRC.Add(hvo);
			}

			foreach (int hvo in inflClasses)
			{
				if (mfNew is IMoAffixForm)
					(mfNew as IMoAffixForm).InflectionClassesRC.Add(hvo);
			}

			if (oldAffMorphType != 0 && mfNew is IMoAffixForm)
				mfNew.MorphTypeRAHvo = oldAffMorphType;
		}

		/// <summary>
		/// Generate an id string like "colorful_7ee714ef-2744-4fc2-b407-aab54e66a76f".
		/// If there's a LIFTid element in LiftResidue (or ImportResidue), use that instead.
		/// </summary>
		public string LIFTid
		{
			get
			{
				string sLiftId = null;
				string sResidue = this.LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					sResidue = ExtractLIFTResidue(m_cache, m_hvo,
						(int)LexEntryTags.kflidImportResidue, (int)LexEntryTags.kflidLiftResidue);
				if (!String.IsNullOrEmpty(sResidue))
					sLiftId = ExtractAttributeFromLiftResidue(sResidue, "id");
				if (String.IsNullOrEmpty(sLiftId))
					return this.HeadWord.Text + "_" + this.Guid.ToString();
				else
					return sLiftId;
			}
		}

		/// <summary>
		/// Go through the MSAs belonging to this entry, removing those which are redundant,
		/// and reassigning the sense references as needed.  This doesn't necessarily update
		/// all the places that the MSAs may have been used, but should be safe for operations
		/// like import where they wouldn't be used anywhere but in the entry.
		/// </summary>
		public void MergeRedundantMSAs()
		{
			if (this.MorphoSyntaxAnalysesOC.Count < 2)
				return;
			Dictionary<int, int> dictOldNewMsa = new Dictionary<int, int>();
			IMoMorphSynAnalysis[] rgMsa = this.MorphoSyntaxAnalysesOC.ToArray();
			for (int i = 0; i < rgMsa.Length; ++i)
			{
				if (dictOldNewMsa.ContainsKey(rgMsa[i].Hvo))
					continue;	// we're replacing this MSA, so it can't replace anything.
				for (int j = i + 1; j < rgMsa.Length; ++j)
				{
					if (rgMsa[i].EqualsMsa(rgMsa[j]))
					{
						dictOldNewMsa.Add(rgMsa[j].Hvo, rgMsa[i].Hvo);
					}
				}
			}
			if (dictOldNewMsa.Count == 0)
				return;
			UpdateMsaReferences(dictOldNewMsa);
			foreach (int hvoOld in dictOldNewMsa.Keys)
				this.MorphoSyntaxAnalysesOC.Remove(hvoOld);
		}

		/// <summary>
		/// Go through the senses of this entry, ensuring that each one has an MSA assigned to it.
		/// MergeRedundantMSAs() should be called after this method.
		/// </summary>
		public void EnsureValidMSAsForSenses()
		{
			bool fIsAffix = MoMorphType.IsAffixType(this.MorphType);
			foreach (ILexSense ls in this.AllSenses)
			{
				if (ls.MorphoSyntaxAnalysisRAHvo != 0)
					continue;
				IMoMorphSynAnalysis msa;
				if (fIsAffix)
				{
					msa = FindEmptyAffixMsa();
					if (msa == null)
					{
						msa = new MoUnclassifiedAffixMsa();
						this.MorphoSyntaxAnalysesOC.Add(msa);
					}
				}
				else
				{
					msa = FindEmptyStemMsa();
					if (msa == null)
					{
						msa = new MoStemMsa();
						this.MorphoSyntaxAnalysesOC.Add(msa);
					}
				}
				ls.MorphoSyntaxAnalysisRAHvo = msa.Hvo;
			}
		}

		/// <summary>
		/// Look for an empty existing MoUnclassifiedAffixMsa before creating one.
		/// Deleting them in MergeRedundantMSAs is very expensive!  (See LT-9006)
		/// </summary>
		/// <returns></returns>
		private IMoMorphSynAnalysis FindEmptyAffixMsa()
		{
			foreach (IMoMorphSynAnalysis msa in this.MorphoSyntaxAnalysesOC)
			{
				IMoUnclassifiedAffixMsa msaAffix = msa as IMoUnclassifiedAffixMsa;
				if (msaAffix != null && msaAffix.PartOfSpeechRAHvo == 0)
					return msa;
			}
			return null;
		}

		/// <summary>
		/// Look for an empty existing MoStemMsa before creating one.
		/// Deleting them in MergeRedundantMSAs is very expensive!  (See LT-9006)
		/// </summary>
		/// <returns></returns>
		private IMoMorphSynAnalysis FindEmptyStemMsa()
		{
			foreach (IMoMorphSynAnalysis msa in this.MorphoSyntaxAnalysesOC)
			{
				IMoStemMsa msaStem = msa as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRAHvo == 0 &&
					msaStem.FromPartsOfSpeechRC.Count == 0 &&
					msaStem.InflectionClassRAHvo == 0 &&
					msaStem.ProdRestrictRC.Count == 0 &&
					msaStem.StratumRAHvo == 0 &&
					msaStem.MsFeaturesOAHvo == 0)
				{
					return msaStem;
				}
			}
			return null;
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
				{
					sResidue = ExtractLIFTResidue(m_cache, m_hvo, (int)LexEntry.LexEntryTags.kflidImportResidue,
						(int)LexEntry.LexEntryTags.kflidLiftResidue);
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
				LiftResidue = sResidue;
			}
			return sResidue;
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
					string sStartTag = sResidue.Substring(0, idxEnd);
					int idx = sStartTag.IndexOf(sAttrName + "=");
					if (idx > 0 && Char.IsWhiteSpace(sStartTag[idx - 1]))
					{
						idx += sAttrName.Length + 1;
						char cQuote = sStartTag[idx];
						++idx;
						idxEnd = sStartTag.IndexOf(cQuote, idx);
						if (idxEnd > 0)
						{
							string sXml = sStartTag.Substring(idx, idxEnd - idx);
							return XmlUtils.DecodeXmlAttribute(sXml);
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Return anything from the ImportResidue which occurs prior to whatever LIFT may have
		/// added to it.  (LIFT import no longer adds to ImportResidue, but it did in the past.)
		/// </summary>
		public ITsString NonLIFTImportResidue
		{
			get
			{
				TsStringAccessor tsa = new TsStringAccessor(m_cache, m_hvo, (int)LexEntryTags.kflidImportResidue);
				return ExtractNonLIFTResidue(tsa);
			}
		}

		internal static ITsString ExtractNonLIFTResidue(TsStringAccessor tsa)
		{
			if (tsa.Length < 29)
				return tsa.UnderlyingTsString;
			ITsStrBldr tsb = tsa.UnderlyingTsString.GetBldr();
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
		/// Scan ImportResidue for XML looking string inserted by LIFT import.  If any is found,
		/// move it from ImportResidue to LiftResidue.
		/// </summary>
		/// <returns>string containing any LIFT import residue found in ImportResidue</returns>
		public static string ExtractLIFTResidue(FdoCache cache, int hvo, int flidImportResidue,
			int flidLiftResidue)
		{
			TsStringAccessor tsa = new TsStringAccessor(cache, hvo, flidImportResidue);
			if (tsa.UnderlyingTsString == null || tsa.Length < 13)
				return null;
			int idx = tsa.Text.IndexOf("<lift-residue");
			if (idx >= 0)
			{
				string sLiftResidue = tsa.Text.Substring(idx);
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
					ITsStrBldr tsb = tsa.UnderlyingTsString.GetBldr();
					tsb.Replace(idx, idx + cch, null, null);
					tsa.UnderlyingTsString = tsb.GetString();	// remove from ImportResidue
					cache.SetUnicodeProperty(hvo, flidLiftResidue, sLiftResidue);
				}
				return sLiftResidue;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Create stem MSAs to replace affix MSAs, and/or create affix MSAs to replace stem
		/// MSAs. This is harder than it looks, since references to MSAs can occur in several
		/// places, and all of them need to be updated.
		/// </summary>
		/// <param name="rgmsaOld">list of bad MSAs which need to be replaced</param>
		public void ReplaceObsoleteMsas(List<IMoMorphSynAnalysis> rgmsaOld)
		{
			Dictionary<int, int> mapOldToNewMsa = new Dictionary<int, int>(rgmsaOld.Count);
			// Replace all the affix type MSAs with corresponding stem MSAs, and all stem MSAs
			// with corresponding unclassified affix MSAs.  Only the PartOfSpeech is preserved
			// in this transformation.
			foreach (IMoMorphSynAnalysis msa in rgmsaOld)
			{
				int hvoOld = msa.Hvo;
				int hvoNew;
				if (msa is IMoStemMsa)
					hvoNew = FindOrCreateMatchingAffixMsa(msa as IMoStemMsa);
				else
					hvoNew = FindOrCreateMatchingStemMsa(msa);
				mapOldToNewMsa.Add(hvoOld, hvoNew);
			}
			UpdateMsaReferences(mapOldToNewMsa);
			// Remove the old, obsolete MSAs.
			foreach (IMoMorphSynAnalysis msa in rgmsaOld)
				this.MorphoSyntaxAnalysesOC.Remove(msa);
		}

		private void UpdateMsaReferences(Dictionary<int, int> mapOldToNewMsa)
		{
			if (mapOldToNewMsa.Keys.Count == 0)
				return;
			/*
			 * A PURE FDO APPROACH SEEMS EXCESSIVELY CUMBERSOME, AND LOADS WAY TOO MUCH DATA,
			 * SO WE'LL CHEAT AND USE DIRECT SQL TO OBTAIN OBJECT IDS.  :-(
			 */
			StringBuilder sbKeys = new StringBuilder();
			foreach (int hvo in mapOldToNewMsa.Keys)
			{
				if (sbKeys.Length > 0)
					sbKeys.Append(",");
				sbKeys.Append(hvo);
			}
			string sKeys = sbKeys.ToString();
			Dictionary<int, List<int>> dictFixes = new Dictionary<int,List<int>>();
			string sQry = String.Format(
				"SELECT ls.MorphoSyntaxAnalysis, ls.Id FROM LexSense ls WHERE ls.MorphoSyntaxAnalysis IN ({0}) ORDER BY ls.MorphoSyntaxAnalysis, ls.Id;",
				sKeys);
			DbOps.LoadDictionaryFromCommand(m_cache, sQry, null, dictFixes);
			int flid = (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis;
			foreach (int hvoMsa in dictFixes.Keys)
			{
				int hvoNew = mapOldToNewMsa[hvoMsa];
				foreach (int hvoObj in dictFixes[hvoMsa])
					m_cache.SetObjProperty(hvoObj, flid, hvoNew);
			}
			dictFixes.Clear();
			sQry = String.Format(
				"SELECT wmb.Msa, wmb.Id FROM WfiMorphBundle wmb WHERE wmb.Msa IN ({0}) ORDER BY wmb.Msa, wmb.Id;",
				sKeys);
			DbOps.LoadDictionaryFromCommand(m_cache, sQry, null, dictFixes);
			flid = (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa;
			foreach (int hvoMsa in dictFixes.Keys)
			{
				int hvoNew = mapOldToNewMsa[hvoMsa];
				foreach (int hvoObj in dictFixes[hvoMsa])
					m_cache.SetObjProperty(hvoObj, flid, hvoNew);
			}
			dictFixes.Clear();
			sQry = String.Format(
				"SELECT ah.FirstMorpheme, ah.Id FROM MoMorphAdhocProhib ah WHERE ah.FirstMorpheme IN ({0}) ORDER BY ah.FirstMorpheme, ah.Id;",
				sKeys);
			DbOps.LoadDictionaryFromCommand(m_cache, sQry, null, dictFixes);
			flid = (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidFirstMorpheme;
			foreach (int hvoMsa in dictFixes.Keys)
			{
				int hvoNew = mapOldToNewMsa[hvoMsa];
				foreach (int hvoObj in dictFixes[hvoMsa])
					m_cache.SetObjProperty(hvoObj, flid, hvoNew);
			}
			dictFixes.Clear();
			sQry = String.Format(
				"SELECT am.Dst, am.Src FROM MoMorphAdhocProhib_Morphemes am WHERE am.Dst IN ({0}) ORDER BY am.Dst, am.Src;",
				sKeys);
			DbOps.LoadDictionaryFromCommand(m_cache, sQry, null, dictFixes);
			flid = (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidMorphemes;
			foreach (int hvoMsa in dictFixes.Keys)
			{
				int[] rghvoNew = new int[] { mapOldToNewMsa[hvoMsa] };
				foreach (int hvoObj in dictFixes[hvoMsa])
				{
					int idx = m_cache.GetObjIndex(hvoObj, flid, hvoMsa);
					if (idx >= 0)
						m_cache.ReplaceReferenceProperty(hvoObj, flid, idx, idx + 1, ref rghvoNew);
				}
			}
			dictFixes.Clear();
			sQry = String.Format(
				"SELECT am.Dst, am.Src FROM MoMorphAdhocProhib_RestOfMorphs am WHERE am.Dst IN ({0}) ORDER BY am.Dst, am.Src;",
				sKeys);
			DbOps.LoadDictionaryFromCommand(m_cache, sQry, null, dictFixes);
			flid = (int)MoMorphAdhocProhib.MoMorphAdhocProhibTags.kflidRestOfMorphs;
			foreach (int hvoMsa in dictFixes.Keys)
			{
				int[] rghvoNew = new int[] { mapOldToNewMsa[hvoMsa] };
				foreach (int hvoObj in dictFixes[hvoMsa])
				{
					int idx = m_cache.GetObjIndex(hvoObj, flid, hvoMsa);
					if (idx >= 0)
						m_cache.ReplaceReferenceProperty(hvoObj, flid, idx, idx + 1, ref rghvoNew);
				}
			}
			dictFixes.Clear();
			sQry = String.Format(
				"SELECT mc.Dst, mc.Src FROM MoMorphSynAnalysis_Components mc WHERE mc.Dst IN ({0}) ORDER BY mc.Dst, mc.Src;",
				sKeys);
			DbOps.LoadDictionaryFromCommand(m_cache, sQry, null, dictFixes);
			flid = (int)MoMorphSynAnalysis.MoMorphSynAnalysisTags.kflidComponents;
			foreach (int hvoMsa in dictFixes.Keys)
			{
				int[] rghvoNew = new int[] { mapOldToNewMsa[hvoMsa] };
				foreach (int hvoObj in dictFixes[hvoMsa])
				{
					int idx = m_cache.GetObjIndex(hvoObj, flid, hvoMsa);
					if (idx >= 0)
						m_cache.ReplaceReferenceProperty(hvoObj, flid, idx, idx + 1, ref rghvoNew);
				}
			}
			dictFixes.Clear();
		}

		private int FindOrCreateMatchingAffixMsa(IMoStemMsa msa)
		{
			int hvoPOS = msa.PartOfSpeechRAHvo;
			foreach (IMoMorphSynAnalysis msaT in this.MorphoSyntaxAnalysesOC)
			{
				IMoUnclassifiedAffixMsa msaAffix = msaT as IMoUnclassifiedAffixMsa;
				if (msaAffix != null && msaAffix.PartOfSpeechRAHvo == hvoPOS)
					return msaAffix.Hvo;
			}
			IMoUnclassifiedAffixMsa msaNew = new MoUnclassifiedAffixMsa();
			this.MorphoSyntaxAnalysesOC.Add(msaNew);
			msaNew.PartOfSpeechRAHvo = hvoPOS;
			return msaNew.Hvo;
		}

		private int FindOrCreateMatchingStemMsa(IMoMorphSynAnalysis msa)
		{
			int hvoPOS = 0;
			if (msa is IMoInflAffMsa)
				hvoPOS = (msa as IMoInflAffMsa).PartOfSpeechRAHvo;
			else if (msa is IMoDerivAffMsa)
				hvoPOS = (msa as IMoDerivAffMsa).ToPartOfSpeechRAHvo;
			else if (msa is IMoDerivStepMsa)
				hvoPOS = (msa as IMoDerivStepMsa).PartOfSpeechRAHvo;
			else if (msa is IMoUnclassifiedAffixMsa)
				hvoPOS = (msa as IMoUnclassifiedAffixMsa).PartOfSpeechRAHvo;
			foreach (IMoMorphSynAnalysis msaT in this.MorphoSyntaxAnalysesOC)
			{
				IMoStemMsa msaStem = msaT as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRAHvo == hvoPOS &&
					msaStem.FromPartsOfSpeechRC.Count == 0 &&
					msaStem.InflectionClassRAHvo == 0 &&
					msaStem.ProdRestrictRC.Count == 0 &&
					msaStem.StratumRAHvo == 0 &&
					msaStem.MsFeaturesOAHvo == 0)
				{
					return msaStem.Hvo;
				}
			}
			IMoStemMsa msaNew = new MoStemMsa();
			this.MorphoSyntaxAnalysesOC.Add(msaNew);
			msaNew.PartOfSpeechRAHvo = hvoPOS;
			return msaNew.Hvo;
		}

		/// <summary>
		/// Return DateCreated in Universal (Utc/GMT) time.
		/// </summary>
		public DateTime UtcDateCreated
		{
			get { return DateCreated.ToUniversalTime(); }
		}

		/// <summary>
		/// Return DateModified in Universal (Utc/GMT) time.
		/// </summary>
		public DateTime UtcDateModified
		{
			get { return DateModified.ToUniversalTime(); }
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class LexEtymology
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
		/// Provide something for the LIFT type attribute.
		/// </summary>
		public string LiftType
		{
			get
			{
				int ws = LiftFormWritingSystem;
				if (m_cache.LangProject.VernWssRC.Contains(ws))
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
					int ws = LiftFormWritingSystem;
					if (ws != 0)
					{
						ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
						sSource = lgws.Name.UserDefaultWritingSystem;
						if (String.IsNullOrEmpty(sSource) || sSource == lgws.Name.NotFoundTss.Text)
						{
							sSource = lgws.Name.BestAnalysisVernacularAlternative.Text;
							if (sSource == lgws.Name.NotFoundTss.Text)
								return "UNKNOWN";
						}
					}
				}
				return sSource;
			}
		}

		private int LiftFormWritingSystem
		{
			get
			{
				int wsActual;
				ITsString tssVern = this.Form.GetAlternativeOrBestTss(m_cache.DefaultVernWs, out wsActual);
				if (tssVern == this.Form.NotFoundTss)
				{
					ITsString tssAnal = this.Form.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
					if (tssAnal == this.Form.NotFoundTss)
					{
						// This shouldn't happen, but...
						if (!DbOps.ReadOneIntFromCommand(m_cache,
							String.Format("SELECT Ws FROM LexEtymology_Form WHERE Obj={0}", m_hvo),
							null, out wsActual))
						{
							return 0;
						}
					}
				}
				return wsActual;
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class LexSense
	{
		/// <summary>
		/// Create a new sense and add it to the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="dummyMSA"></param>
		/// <param name="gloss"></param>
		/// <returns></returns>
		public static ILexSense CreateSense(ILexEntry entry, DummyGenericMSA dummyMSA, string gloss)
		{
			ILexSense sense = (ILexSense)entry.SensesOS.Append(new LexSense());
			(sense as LexSense).DummyMSA = dummyMSA;

			// Handle gloss.
			if (gloss != null && gloss.Length > 0)
			{
				if (gloss.Length > 256)
				{
					System.Windows.Forms.MessageBox.Show(Strings.ksTruncatingGloss, Strings.ksWarning,
						System.Windows.Forms.MessageBoxButtons.OK,
						System.Windows.Forms.MessageBoxIcon.Warning);
					gloss = gloss.Substring(0, 256);
				}
				sense.Gloss.AnalysisDefaultWritingSystem = gloss;
			}

			return sense;
		}

		/// <summary>
		/// Called by reflection, this method searches for a WfiAnalysis probably produced as a default
		/// from this sense, and corrects its WfiGloss to match a changed gloss of the sense.
		/// </summary>
		public void AdjustDerivedAnalysis()
		{
			// find a wfigloss
			// that is the only gloss of a wfianalysis
			// that has just one WfiMorphBundle whose sense is the one of interest
			// that has no occurrences
			// and no positive human analysis
			// (Nb:  the left outer joins are all looking for things, such as an additional WfiGloss,
			// that the where clause checks we do NOT find.)
			string sql =
				@"select wg.id from WfiMorphBundle_ wmb
				join WfiAnalysis wa on wa.id = wmb.Owner$
				join WfiGloss_ wg on wg.owner$ = wa.id
				left outer join WfiMorphBundle_ wmbOther on wmbOther.Owner$ = wa.id and wmbOther.id != wmb.id
				left outer join CmBaseAnnotation_ cba on cba.InstanceOf = wg.id
				left outer join WfiGloss_ wgOther on wgOther.owner$ = wa.id and wgOther.id != wg.id
				left outer join CmAgentEvaluation_ cae on cae.target = wa.id and cae.Accepted = 1
				left outer join CmAgent ca on ca.id = cae.owner$ and ca.human = 1
				where wmb.Sense = " +
				this.Hvo
				+
				@"and wmbOther.id is null
					and cba.id is null
					and wgOther.id is null
					and ca.id is null";
			int hvoWg;
			if (!DbOps.ReadOneIntFromCommand(m_cache, sql, null, out hvoWg))
				return;
			WfiGloss wg = CmObject.CreateFromDBObject(m_cache, hvoWg) as WfiGloss;
			wg.Form.UserDefaultWritingSystem = this.Gloss.UserDefaultWritingSystem;
		}

		/// <summary>
		/// Get a list of annotation IDs that that indirectly reference this sense through the WFI.
		/// </summary>
		public List<int> AllSentenceClientIDs
		{
			get
			{
				List<int> annotationIds = new List<int>();

				// Find all sentences that use this sense.
				// Really, it is the annotation ids.
				List<int> analIds = DbOps.ReadIntsFromCommand(m_cache, "SELECT Id FROM WfiMorphBundle WHERE Sense=" + Hvo.ToString(), null);
				foreach (int analId in analIds)
				{
					IWfiMorphBundle mb = WfiMorphBundle.CreateFromDBObject(m_cache, analId);
					IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_cache, mb.OwnerHVO);
					annotationIds.AddRange(CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, anal.Hvo));
					foreach (WfiGloss gloss in anal.MeaningsOC)
						annotationIds.AddRange(CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, gloss.Hvo));
				}

				return annotationIds;
			}
		}

		/// <summary>
		/// Returns twfic annotations that have WfiMorphBundle reference(s) to this sense.
		/// Note: It's possible (though not probable) that we can return more than one instance
		/// to the same annotation, since a twfic can have multiple morphs pointing to the same sense.
		/// For now, we'll just let the client create a set of unique ids, if that's what they want.
		/// Otherwise, for duplicate ids, the client can have the option of looking into WfiAnalysis.MorphBundles
		/// for the relevant sense.
		/// </summary>
		/// <returns></returns>
		public List<int> InstancesInTwfics
		{
			get
			{
				return WfiMorphBundle.OccurrencesInTwfics(this);
			}
		}

		/// <summary>
		/// Get the minimal set of LexReferences for this entry.
		/// </summary>
		public List<int> MinimalLexReferences
		{
			get { return LexReference.ExtractMinimalLexReferences(m_cache, Hvo); }
		}

		/// <summary>
		/// This method retrieves the list of LexReference objects that contain the LexEntry
		/// or LexSense given by hvo. The list is pruned to remove any LexReference that
		/// targets only hvo unless parent LexRefType is a sequence/scale.  This pruning
		/// is needed to obtain proper display of the Dictionary (publication) view.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static void LoadAllMinimalLexReferences(FdoCache cache, Dictionary<int, List<int>> values)
		{
			LexReference.LoadAllMinimalLexReferences(cache, values);
		}

		/// <summary>
		/// Override the base property to deal with deleting MSAs.
		/// </summary>
		public int MorphoSyntaxAnalysisRAHvo
		{
			get
			{
				return MorphoSyntaxAnalysisRAHvo_Generated;
			}
			set
			{
				if (MorphoSyntaxAnalysisRAHvo != value)
				{
					// Only mess with it, if it is a different value
					// since HandleOldMSA() may delete it, which will cause a crash
					// when it tries to get set.
					HandleOldMSA(m_cache, m_hvo, value, false);
					MorphoSyntaxAnalysisRAHvo_Generated = value;
				}
			}
		}

		/// <summary>
		/// Override the base property to deal with deleting MSAs.
		/// </summary>
		public IMoMorphSynAnalysis MorphoSyntaxAnalysisRA
		{
			get
			{
				return MorphoSyntaxAnalysisRA_Generated;
			}
			set
			{
				int newHvo = (value == null) ? 0 : value.Hvo;
				if (MorphoSyntaxAnalysisRAHvo != newHvo)
				{
					// Only mess with it, if it is a different value
					// since HandleOldMSA() may delete it, which will cause a crash
					// when it tries to get set.
					HandleOldMSA(m_cache, m_hvo, newHvo, false);
					MorphoSyntaxAnalysisRA_Generated = value;
				}
			}
		}

		/// <summary>
		/// Handle side effects of settin MSA to new value:
		/// - Any WfiMorphBundle linked to this sense must be changed to point at the new MSA.
		/// - Delete original MSA, if nothing uses it. (If assumeSurvives is true, caller already
		/// knows that something still uses it.)
		/// </summary>
		public static void HandleOldMSA(FdoCache cache, int hvoThis, int hvoNewMsa, bool assumeSurvives)
		{
			///////////////////////////////////////////////////////////////////////////////////
			// Update any WfiMorphBundle which has the old MSA value for this LexSense.
			// (See LT-3804.  This also fixes LT-3937, at least to some degree.)
			// Using direct SQL here saves a 3-level nested foreach loop, which could pull all
			// sorts of stuff into memory with myriad SQL queries.
			if (cache.DatabaseAccessor != null)	// can't run sql in unit tests.
			{
				string sql = string.Format("SELECT Id FROM WfiMorphBundle WHERE Sense={0}", hvoThis);
				int[] rghvoWmb = DbOps.ReadIntArrayFromCommand(cache, sql, null);
				for (int i = 0; i < rghvoWmb.Length; ++i)
				{
					IWfiMorphBundle wmb = WfiMorphBundle.CreateFromDBObject(cache, rghvoWmb[i]);
					wmb.MsaRAHvo = hvoNewMsa;
				}
			}
			if (assumeSurvives)
				return;
			///////////////////////////////////////////////////////////////////////////////////
			ISilDataAccess sda = cache.MainCacheAccessor;
			int oldMsa = cache.MainCacheAccessor.get_ObjectProp(hvoThis, (int)LexSenseTags.kflidMorphoSyntaxAnalysis);
			if (oldMsa != 0)
			{
				sda.SetObjProp(hvoThis, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis, 0);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					hvoThis, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis,
					0, 0, 1);
				MoMorphSynAnalysis msa = (MoMorphSynAnalysis)CmObject.CreateFromDBObject(cache, oldMsa, false);
				if (msa.CanDelete)
					msa.DeleteUnderlyingObject();
			}
		}

		/// <summary>
		/// Resets the MSA to an equivalent MSA, whether it finds it, or has to create a new one.
		/// </summary>
		public DummyGenericMSA DummyMSA
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

				// Start a possibly nested undo task.
				m_cache.BeginUndoTask(Strings.ksUndoSettingFunc, Strings.ksRedoSettingFunc);
				try
				{
					ILexEntry entry = Entry;
					IMoMorphSynAnalysis msaOld = MorphoSyntaxAnalysisRA;
					foreach (MoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
					{
						if (msa.EqualsMsa(value))
						{
							MorphoSyntaxAnalysisRA = msa;
							if (msaOld != null && entry is LexEntry && !(entry as LexEntry).UsesMsa(msaOld))
							{
								CmObject.ReplaceReferences(m_cache, msaOld, msa);
								msaOld.DeleteUnderlyingObject();
							}
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
							MoStemMsa stemMsa = new MoStemMsa();
							stemMsa.InitNew(entry.MorphoSyntaxAnalysesOC.Cache, entry.Hvo, entry.MorphoSyntaxAnalysesOC.Flid, 0);
							if (value.MainPOS > 0)
								stemMsa.PartOfSpeechRAHvo = value.MainPOS;
							stemMsa.FromPartsOfSpeechRC.RemoveAll();
							if (value.FromPartsOfSpeech != null)
								stemMsa.FromPartsOfSpeechRC.Add(value.FromPartsOfSpeech);

							// copy over attributes, such as inflection classes and features, that are still valid for the
							// new category
							MoStemMsa oldStemMsa = msaOld as MoStemMsa;
							if (oldStemMsa != null)
								stemMsa.CopyAttributesIfValid(oldStemMsa);

							entry.MorphoSyntaxAnalysesOC.Add(stemMsa);	// added after setting POS so slice will show POS
							msaMatch = stemMsa;
							break;
						}
						case MsaType.kInfl:
						{
							MoInflAffMsa inflMsa = new MoInflAffMsa();
							inflMsa.InitNew(entry.MorphoSyntaxAnalysesOC.Cache, entry.Hvo, entry.MorphoSyntaxAnalysesOC.Flid, 0);
							if (value.MainPOS > 0)
								inflMsa.PartOfSpeechRAHvo = value.MainPOS;
							if (value.Slot > 0)
								inflMsa.SlotsRC.Add(value.Slot);

							// copy over attributes, such as inflection classes and features, that are still valid for the
							// new category
							MoInflAffMsa oldInflMsa = msaOld as MoInflAffMsa;
							if (oldInflMsa != null)
								inflMsa.CopyAttributesIfValid(oldInflMsa);

							entry.MorphoSyntaxAnalysesOC.Add(inflMsa);	// added after setting POS so slice will show POS
							msaMatch = inflMsa;
							break;
						}
						case MsaType.kDeriv:
						{
							MoDerivAffMsa derivMsa = new MoDerivAffMsa();
							derivMsa.InitNew(entry.MorphoSyntaxAnalysesOC.Cache, entry.Hvo, entry.MorphoSyntaxAnalysesOC.Flid, 0);
							if (value.MainPOS > 0)
								derivMsa.FromPartOfSpeechRAHvo = value.MainPOS;
							if (value.SecondaryPOS > 0)
								derivMsa.ToPartOfSpeechRAHvo = value.SecondaryPOS;

							// copy over attributes, such as inflection classes and features, that are still valid for the
							// new category
							MoDerivAffMsa oldDerivMsa = msaOld as MoDerivAffMsa;
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
							MoUnclassifiedAffixMsa uncMsa = new MoUnclassifiedAffixMsa();
							uncMsa.InitNew(entry.MorphoSyntaxAnalysesOC.Cache, entry.Hvo, entry.MorphoSyntaxAnalysesOC.Flid, 0);
							// No, we want to set the 0 value, too, so the sync$ table shows it;
							// otherwise a running parser will not "see" this new entry's msa info
							//if (value.MainPOS > 0)
								uncMsa.PartOfSpeechRAHvo = value.MainPOS;
							entry.MorphoSyntaxAnalysesOC.Add(uncMsa);	// added after setting POS so slice will show POS
							msaMatch = uncMsa;
							break;
						}
					}
					MorphoSyntaxAnalysisRAHvo = msaMatch.Hvo;
					if (msaOld != null && entry is LexEntry && !(entry as LexEntry).UsesMsa(msaOld))
					{
						CmObject.ReplaceReferences(m_cache, msaOld, msaMatch);
						msaOld.DeleteUnderlyingObject();
					}
				}
				finally
				{
					// Complete the (possibly nested) undo task.
					m_cache.EndUndoTask();
				}
			}
		}

		/// <summary>
		/// Gets this sense and all senses it owns.
		/// </summary>
		public List<ILexSense> AllSenses
		{
			get
			{
				List<ILexSense> senses = new List<ILexSense>();
				senses.Add(this);
				foreach (ILexSense ls in SensesOS)
					senses.AddRange(ls.AllSenses);
				return senses;
			}
		}

		/// <summary>
		/// Get the entry that owns the sense.
		/// </summary>
		public ILexEntry Entry
		{
			get
			{
				int hvoEntry = EntryID;
				if (hvoEntry == 0)
					return null;
				return LexEntry.CreateFromDBObject(m_cache, hvoEntry);
			}
		}

		/// <summary>
		/// Get the ID for the entry that owns this sense.
		/// This SHOULD always be non-zero, but we've encountered at least one pathological database
		/// with senses NOT owned by entries, so try to program defensively against that possibility.
		/// </summary>
		public int EntryID
		{
			get
			{
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				int hvo = Hvo;
				int clid = 0;
				do
				{
					hvo = sda.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
					if (hvo == 0)
						break;
					clid = sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
				}
				while (clid != LexEntry.kClassId);
				return hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexSense as a primary complex form component.
		/// Note: this must stay in sync with LoadAllComplexFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> ComplexFormEntryBackRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT pl.Src " +
					"FROM LexEntryRef_PrimaryLexemes pl " +
					"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
					"WHERE pl.Dst={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that refer to a LexSense as a
		/// primary complex form component.  The keys in the dictionary are all the LexSense
		/// objects that are thus referenced, and the values are the lists of LexEntryRef
		/// objects that refer to the keys.
		/// Note: this must stay in sync with ComplexFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllComplexFormEntryBackRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT pl.Dst, pl.Src " +
				"FROM LexEntryRef_PrimaryLexemes pl " +
				"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
				"JOIN LexSense ls ON ls.Id=pl.Dst " +
				"ORDER BY pl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntryRef objects that refer to this LexSense as a variant component.
		/// Note: this must stay in sync with LoadAllVariantFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> VariantFormEntryBackRefs
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT cl.Src " +
					"FROM LexEntryRef_ComponentLexemes cl " +
					"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
					"WHERE cl.Dst={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects that refer to a LexSense as a
		/// variant component.  The keys in the dictionary are all the LexSense objects that are
		/// thus referenced, and the values are the lists of LexEntryRef objects that refer to
		/// the keys.
		/// Note: this must stay in sync with VariantFormEntryBackRefs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllVariantFormEntryBackRefs(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT cl.Dst, cl.Src " +
				"FROM LexEntryRef_ComponentLexemes cl " +
				"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
				"JOIN LexSense ls ON ls.Id=cl.Dst " +
				"ORDER BY cl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own LexEntryRef objects that refer to this LexSense as a
		/// primary complex form component.
		/// Note: this must stay in sync with LoadAllComplexFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> ComplexFormEntries
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT er.Src " +
					"FROM LexEntryRef_PrimaryLexemes pl " +
					"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
					"JOIN LexEntry_EntryRefs er ON er.Dst=pl.Src " +
					"WHERE pl.Dst={0}", this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntry objects that own LexEntryRef objects that
		/// refer to a LexSense as a primary complex form component.  The keys in the dictionary
		/// are all the LexSense objects that are thus referenced, and the values are the lists
		/// of the LexEntry objects that own the LexEntryRef objects that refer to the keys.
		/// Note: this must stay in sync with ComplexFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllComplexFormEntries(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT pl.Dst, er.Src " +
				"FROM LexEntryRef_PrimaryLexemes pl " +
				"JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1 " +
				"JOIN LexEntry_EntryRefs er ON er.Dst=pl.Src " +
				"JOIN LexSense ls ON ls.Id=pl.Dst " +
				"ORDER BY pl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// <summary>
		/// creates a variant entry from this (main) sense,
		/// and links the variant to this (main) sense via
		/// EntryRefs.ComponentLexemes
		///
		/// NOTE: The caller will need to supply the lexemeForm subsequently.
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType)
		{
			return this.CreateVariantEntryAndBackRef(variantType, null);
		}

		/// <summary>
		/// creates a variant entry from this (main) sense,
		/// and links the variant to this (main) sense via
		/// EntryRefs.ComponentLexemes
		/// </summary>
		/// <param name="variantType">the type of the new variant</param>
		/// <param name="tssVariantLexemeForm">the lexeme form of the new variant</param>
		/// <returns>the new variant entry reference</returns>
		public ILexEntryRef CreateVariantEntryAndBackRef(ILexEntryType variantType, ITsString tssVariantLexemeForm)
		{
			int hvoOwnerEntry = Cache.GetOwnerOfObjectOfClass(this.Hvo, LexEntry.kclsidLexEntry);
			LexEntry entry = LexEntry.CreateFromDBObject(Cache, hvoOwnerEntry) as LexEntry;
			return entry.CreateVariantEntryAndBackRef(this, variantType, tssVariantLexemeForm);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a backreference (virtual) property.  It returns the list of ids for all the
		/// LexEntry objects that own LexEntryRef objects that refer to this LexSense as a
		/// variant (component).
		/// Note: this must stay in sync with LoadAllVariantFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> VariantFormEntries
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT er.Src " +
					"FROM LexEntryRef_ComponentLexemes cl " +
					"JOIN LexEntry_EntryRefs er ON er.Dst=cl.Src " +
					"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
					"WHERE cl.Dst={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntry objects that own LexEntryRef objects that
		/// refer to a LexSense as a variant (component).  The keys in the dictionary are all
		/// the LexSense objects that are thus referenced, and the values are the lists of the
		/// LexEntry objects that own the LexEntryRef objects that refer to the keys.
		/// Note: this must stay in sync with VariantFormEntries!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllVariantFormEntries(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT cl.Dst, er.Src " +
				"FROM LexEntryRef_ComponentLexemes cl " +
				"JOIN LexEntry_EntryRefs er ON er.Dst=cl.Src " +
				"JOIN LexEntryRef ler ON ler.Id=cl.Src AND ler.RefType=0 " +
				"JOIN LexSense ls ON ls.Id=cl.Dst " +
				"ORDER BY cl.Dst";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// <summary>
		/// This is a backreference (virtual) method.  It returns the list of object ids for
		/// all the LexRefTypes that contain a LexReference that includes this LexSense.
		/// </summary>
		public List<int> LexRefTypes()
		{
			LexSenseReferencesVirtualHandler vh = new LexSenseReferencesVirtualHandler(m_cache);
			if (vh != null)
			{
				return vh.LexRefTypes(this.Hvo);
			}
			return null;
		}

		/// <summary>
		/// This is a backreference (virtual) method.  It returns the list of object ids for
		/// all the LexReferences that contain this LexSense/LexEntry.
		/// Note this is called on SFM export by mdf.xml so needs to be a property.
		/// </summary>
		public List<int> LexReferences
		{
			get
			{
				LexSenseReferencesVirtualHandler vh = new LexSenseReferencesVirtualHandler(m_cache);
				if (vh != null)
				{
					return vh.LexReferences(this.Hvo);
				}
				return null;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string with the entry headword and a sense number if there is more than
		/// one sense.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString FullReferenceName
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.AppendTsString(OwnerOutlineName);
				tisb.Append(" ");
				// Add Sense POS and gloss info, as per LT-3811.
				if (MorphoSyntaxAnalysisRAHvo > 0)
				{
					// TODO: Figure out why italic doesn't work here.
					tisb.SetIntPropValues((int)FwTextPropType.ktptItalic,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					tisb.AppendTsString(MorphoSyntaxAnalysisRA.ChooserNameTS);
					tisb.Append(" ");
					tisb.SetIntPropValues((int)FwTextPropType.ktptItalic,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvOff);
				}
				tisb.AppendTsString(Gloss.BestAnalysisAlternative);

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
				return OwnerOutlineNameForWs(m_cache.DefaultVernWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses.
		/// </summary>
		/// <param name="wsVern"></param>
		/// ------------------------------------------------------------------------------------
		public ITsString OwnerOutlineNameForWs(int wsVern)
		{
			if (wsVern <= 0)
				wsVern = m_cache.DefaultVernWs;
			int hvoEntry = EntryID;
			if (hvoEntry == 0)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("<missing entry!!>", wsVern);
			}
			LexEntry le = new LexEntry(m_cache, EntryID, false, false);
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.AppendTsString(le.HeadWordForWs(wsVern));
			if (le.HasMoreThanOneSense)
			{
				// These int props may not be needed, but they're safe.
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
					m_cache.DefaultAnalWs);
				tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwSuperscriptVal.kssvOff);
				tisb.Append(" ");
				tisb.Append(this.SenseNumber);
			}
			return tisb.GetString();
		}


		/// <summary>
		/// Returns the sense number as a string.
		/// one sense.
		/// </summary>
		public string SenseNumber
		{
			get
			{
				return m_cache.GetOutlineNumber(this.Hvo,
					m_cache.GetOwningFlidOfObject(this.Hvo), false, true);
			}
		}

		/// <summary>
		/// Returns the one-based index of this sense in its owner's property, or 0 if it's
		/// the only one.
		/// </summary>
		public int IndexNumber
		{
			get
			{
				int cSenses = m_cache.GetVectorSize(this.OwnerHVO, this.OwningFlid);
				if (cSenses == 1)
					return 0;
				int idx = m_cache.GetObjIndex(this.OwnerHVO, this.OwningFlid, this.Hvo);
				return idx + 1;
			}
		}

		/// <summary>
		/// Alias OwnerOutlineName to allow using a common method for both Senses and Entries.
		/// This is useful especially in Xml configuration parts which can access either type.
		/// </summary>
		public ITsString HeadWord
		{
			get { return OwnerOutlineName; }
		}

		/// <summary>
		/// Alias OwnerOutlineNameForWs to allow using a common method for both Senses and
		/// Entries.  This is useful especially in Xml configuration parts which can access
		/// either type.
		/// </summary>
		/// <param name="wsVern"></param>
		public ITsString HeadWordForWs(int wsVern)
		{
			return OwnerOutlineNameForWs(wsVern);
		}

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				//tisb.AppendTsString(HeadWord);
				tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvOff);
				int wsAnal = m_cache.LangProject.DefaultAnalysisWritingSystem;
				IMoMorphSynAnalysis msa = MorphoSyntaxAnalysisRA;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				if (msa != null)
				{
					//tisb.AppendTsString(tsf.MakeString(" ", wsAnal));
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
						tisb.AppendTsString(tsf.MakeString(" " + Gloss.AnalysisDefaultWritingSystem,
							wsAnal));
					}
				}
				else if (Definition.AnalysisDefaultWritingSystem != null
					&& Definition.AnalysisDefaultWritingSystem.Length > 0)
				{
					tisb.AppendTsString(tsf.MakeString(" ", wsAnal));
					tisb.AppendTsString(Definition.AnalysisDefaultWritingSystem.UnderlyingTsString);
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
				case (int)LexSense.LexSenseTags.kflidSenseType:
					return m_cache.LangProject.LexDbOA.SenseTypesOA;
				case (int)LexSense.LexSenseTags.kflidUsageTypes:
					return m_cache.LangProject.LexDbOA.UsageTypesOA;
				case (int)LexSense.LexSenseTags.kflidDomainTypes:
					return m_cache.LangProject.LexDbOA.DomainTypesOA;
				case (int)LexSense.LexSenseTags.kflidStatus:
					return m_cache.LangProject.LexDbOA.StatusOA;
				case (int)LexSense.LexSenseTags.kflidSemanticDomains:
					return m_cache.LangProject.SemanticDomainListOA;
				case (int)LexSense.LexSenseTags.kflidAnthroCodes:
					return m_cache.LangProject.AnthroListOA;
				case (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis:
					return CmObject.CreateFromDBObject(m_cache, EntryID);
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
				case (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis:
					ILexEntry le = LexEntry.CreateFromDBObject(m_cache, EntryID);
					set = new Set<int>(le.MorphoSyntaxAnalysesOC.HvoArray);
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
			return (flid == (int)LexSense.LexSenseTags.kflidGloss);
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
				int ws = m_cache.DefaultAnalWs;
				ITsString meaning = Gloss.BestAnalysisAlternative;
				if (meaning == null || meaning.Length == 0 || meaning.Text == Strings.ksStars)
				{
					meaning = Definition.BestAnalysisAlternative;
				}
				return meaning;
			}
		}

		/// <summary>
		/// Returns the TsString that represents the LongName of this object.
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				ITsIncStrBldr tisb = HeadWord.GetIncBldr();
				tisb.Append(" (");
				tisb.AppendTsString(ShortNameTSS);
				tisb.Append(")");
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Override default implementation to make a more suitable TS string for a wordform.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				int userWs = m_cache.DefaultUserWs;
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteLexSense, " "));
				tisb.AppendTsString(ShortNameTSS);

				int mbCount = 0;
				int lmeCount = 0;
				int lseCount = 0;
				foreach (LinkedObjectInfo loi in LinkedObjects)
				{
					switch (loi.RelObjClass)
					{
						case WfiMorphBundle.kclsidWfiMorphBundle:
						{
							++mbCount;
							break;
						}
						case LexEntry.kclsidLexEntry:
						{
							++lmeCount;
							break;
						}
							//						case LexEntry.kclsidLexEntry:
							//						{
							//							++lseCount;
							//							break;
							//						}
					}
				}

				int cnt = 1;
				string warningMsg = String.Format("\x2028\x2028{0}", Strings.ksSenseUsedHere);
				bool wantMainWarningLine = true;
				if (mbCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (mbCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesInAnalyses, cnt++, mbCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceInAnalyses, cnt++));
					wantMainWarningLine = false;
				}
				if (lmeCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (lmeCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByEntries, cnt++, lmeCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByEntries, cnt++));
					wantMainWarningLine = false;
				}
				if (lseCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (lseCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesBySubentries, cnt++, lseCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceBySubentries, cnt++));
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Side effects of deleting the underlying object.  This is complicated by the existence of homograph
		/// numbers.
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Recursively call DeleteUnderlyingObject() on subsenses.
			List<LexSense> senses = new List<LexSense>();
			foreach (LexSense sense in SensesOS)
			{
				// Put them in a temporary holder,
				// since looping over the main prop isn't good,
				// when they are being deleted.
				senses.Add(sense);
			}
			foreach (LexSense sense in senses)
				sense.DeleteObjectSideEffects(objectsToDeleteAlso, state);

			if (MorphoSyntaxAnalysisRAHvo > 0)
				MorphoSyntaxAnalysisRAHvo = 0; // This will allow the MSA to be deleted, if appropriate.

			// Deal with critical inbound references on entry and objects it owns.
			// The idea here is to delete any objects that refer to the entry,
			// but ONLY if those objects would then be invalid.
			// We call DeleteUnderlyingObject() directly on any high risk objects,
			// such as MSAs,MoForms, and senses, since the regular deletion SP would not delete
			// any other invalid objects.
			List<LinkedObjectInfo> linkedObjs = LinkedObjects;
			List<int> deletedObjectIDs = new List<int>();
			foreach (LinkedObjectInfo loi in linkedObjs)
			{
				if (loi.RelObjClass == WfiMorphBundle.kclsidWfiMorphBundle)
				{
					if (loi.RelObjField == (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense)
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
					}
				}
				else if (loi.RelObjClass == (int)LexReference.kclsidLexReference && !objectsToDeleteAlso.Contains(loi.RelObjId))
				{
					LexReference lr = (LexReference)LexReference.CreateFromDBObject(m_cache, loi.RelObjId);
					// Delete the Lexical relationship if it will be broken after removing this target.
					if (lr.IncompleteWithoutTarget(this.Hvo))
						lr.DeleteObjectSideEffects(objectsToDeleteAlso, state);
					else
						lr.TargetsRS.Remove(this.Hvo);
				}
			}

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		private void RDEAddDomain(int hvoDomain, int tagList, FdoCache cache)
		{
			this.SemanticDomainsRC.Add(hvoDomain);
		}

		/// <summary>
		/// This is invoked (using reflection) by an XmlRDEBrowseView when the user presses
		/// "Enter" in an RDE view that is displaying lexeme form and definition.
		/// (Maybe also on loss of focus, switch domain, etc?)
		/// It creates a new entry, lexeme form, and sense that are linked to the specified domain.
		/// Typically, later, a call to RDEMergeSense will be made to see whether this
		/// new entry should be merged into some existing sense.
		/// Note that this method is NOT responsible to insert the new sense into
		/// the fake property tagList of hvoDomain. (The caller will do that.)
		/// </summary>
		/// <param name="hvoDomain">database id of the semantic domain</param>
		/// <param name="tagList">id of the inverse relation for the senses that belong to the
		/// domain</param>
		/// <param name="columns"></param>
		/// <param name="rgtss"></param>
		/// <param name="cache"></param>
		/// <param name="stringTbl"></param>
		public static int RDENewSense(int hvoDomain, int tagList,
			List<XmlNode> columns, ITsString[] rgtss, FdoCache cache, StringTable stringTbl)
		{
			Debug.Assert(hvoDomain != 0);
			Debug.Assert(rgtss.Length == columns.Count);

			// Make a new sense in a new entry.
			ILexEntry le = cache.LangProject.LexDbOA.EntriesOC.Add(
				new LexEntry());
			IMoForm morph = null;

			// create a LexSense that has the given definition and semantic domain
			// Needs to be LexSense, since later calls use non-interface methods.
			LexSense ls = (LexSense)le.SensesOS.Append(new LexSense());

			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			// go through each column and store the appropriate information.
			for (int i = 0; i < columns.Count; ++i)
			{
				// Review: Currently we key off the column labels to determine which columns
				// correspond to CitationForm and which correspond to Definition.
				// Ideally we'd like to get at the flids used to build the column display strings.
				// Instead of passing in only ITsStrings, we could pass in a structure containing
				// an index of strings with any corresponding flids.  Here we'd expect strings
				// based upon either LexemeForm.Form or LexSense.Definition. We could probably
				// do this as part of the solution to handling duplicate columns in LT-3763.
				XmlNode column = columns[i] as XmlNode;
				string columnLabel = XmlUtils.GetManditoryAttributeValue(column, "label");
				string[] columnLabelComponents = columnLabel.Split(new char[] {' ', ':'});
				// get column label without writing system or extraneous information.
				string columnBasicLabel = columnLabelComponents[0];
				if (!String.IsNullOrEmpty(columnBasicLabel) && stringTbl != null)
					columnBasicLabel = stringTbl.LocalizeAttributeValue(columnBasicLabel);
				ITsTextProps ttp = rgtss[i].get_PropertiesAt(0);
				int var;
				int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				Debug.Assert(ws != 0);

				ITsString tssStr = rgtss[i];
				string sStr = tssStr.Text;
				if (sStr == null)
					sStr = ""; // otherwise Trim below blows up.
				sStr = sStr.Trim();

				if (columnBasicLabel == Strings.ksWord)
				{
					// This is a lexeme form.

					if (morph == null)
						morph = MoForm.MakeMorph(cache, le, tssStr);
					Debug.Assert(le.LexemeFormOAHvo != 0);
					if (morph is IMoStemAllomorph)
					{
						// Make sure we have a proper allomorph and MSA for this new entry and sense.
						// (See LT-1318 for details and justification.)
						MoMorphTypeCollection typesCol = new MoMorphTypeCollection(cache);
						if (sStr.IndexOf(' ') > 0)
							morph.MorphTypeRA = typesCol.Item(MoMorphType.kmtPhrase);
						else
							morph.MorphTypeRA = typesCol.Item(MoMorphType.kmtStem);
						morph.Form.SetAlternative(sStr, ws);
					}
				}
				else if (columnBasicLabel == Strings.ksDefinition)
				{
					// This is a Definition.
					if (sStr != "")
						ls.Definition.SetAlternative(sStr, ws);
				}
				else
				{
					Debug.Fail("column (" + columnLabel + ") not supported.");
				}
			}
			if (morph == null)
				morph = le.LexemeFormOA = new MoStemAllomorph();

			ls.RDEAddDomain(hvoDomain, tagList, cache);

			if (le.MorphoSyntaxAnalysesOC.Count == 0)
			{
				// Commonly, it's a new entry with no MSAs; make sure it has at least one.
				// This way of doing it allows a good bit of code to be shared with the normal
				// creation path, as if the user made a stem but didn't fill in any grammatical
				// information.
				DummyGenericMSA dummyMsa = new DummyGenericMSA();
				if (morph != null && morph is IMoAffixForm)
					dummyMsa.MsaType = MsaType.kUnclassified;
				else
					dummyMsa.MsaType = MsaType.kStem;
				ls.DummyMSA = dummyMsa;
			}

			// We don't want a partial MSA created, so don't bother doing anything
			// about setting ls.MorphoSyntaxAnalysisRA

			// LT-1731: adding to make sure new entries are added to the lexicon
			//	record list (full edit,...)
			cache.PropChanged(null, PropChangeType.kpctNotifyAll,
				cache.LangProject.LexDbOA.Hvo,
				(int)LexDb.LexDbTags.kflidEntries, 0, 1, 0);

			return ls.Hvo;
		}

		// If hvoSense occurs in property tagList of hvoDomain, remove it and update display.
		// Assumes the sense occurs at most once.
		static void DeleteSense(FdoCache cache, int hvoDomain, int tagList, int hvoSense)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			int citem = sda.get_VecSize(hvoDomain, tagList);
			for (int i = 0; i < citem; i++)
			{
				if (sda.get_VecItem(hvoDomain, tagList, i) == hvoSense)
				{
					cda.CacheReplace(hvoDomain, tagList, i, i + 1, new int[0], 0);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoDomain, tagList, i, 0, 1);
					// Note: if we change this to look for multiple occurrences,
					// remember to fix citems and i here. We've modified the list we're searching.
					break;
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
		/// <param name="tagList"></param>
		/// <param name="columns">List of XmlNode objects</param>
		/// <param name="cache"></param>
		/// <param name="hvoSense"></param>
		/// <param name="newHvos">Set of new senses (including hvoSense).</param>
		public static void RDEMergeSense(int hvoDomain, int tagList,
			List<XmlNode> columns, FdoCache cache, int hvoSense, Set<int> newHvos)
		{
			// The goal is to find a lex entry with the same lexeme form.form as hvoSense's LexEntry.
			ILexSense ls = LexSense.CreateFromDBObject(cache, hvoSense);
			ILexEntry leTarget = LexEntry.CreateFromDBObject(cache, ls.EntryID);
			string homographForm = leTarget.HomographForm;
			string sDefnTarget = ls.Definition.AnalysisDefaultWritingSystem.Text;

			// Check for pre-existing LexEntry which has the same homograph form
			bool fGotExactMatch;
			ILexEntry leSaved = FindBestLexEntryAmongstHomographs(cache, homographForm, sDefnTarget, leTarget, newHvos, hvoDomain, out fGotExactMatch);
			if (fGotExactMatch)
			{
				// delete the entry AND sense
				leTarget.DeleteUnderlyingObject();
				DeleteSense(cache, hvoDomain, tagList, hvoSense);
			}
			else if (leSaved != null)
			{
				// move the one and only sense of leTarget to leSaved...provided it has a compatible MSA
				// of the expected type.
				ILexSense sense = leTarget.SensesOS.FirstItem as ILexSense;
				if (sense.MorphoSyntaxAnalysisRA is MoStemMsa)
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
						sense.MorphoSyntaxAnalysisRA = newMsa;
						// Move it to the new owner.
						cache.MoveOwningSequence(leTarget.Hvo, (int)LexEntry.LexEntryTags.kflidSenses, 0, 0,
							leSaved.Hvo, (int)LexEntry.LexEntryTags.kflidSenses, leSaved.SensesOS.Count);
						// delete the entry.
						leTarget.DeleteUnderlyingObject();
						// But NOT the sense from the domain...it just moved to another entry.
						//DeleteSense(cache, hvoDomain, tagList, hvoSense);
					}
				}
			}
			// else do nothing (no useful match, let the LE survive)
		}

		/// <summary>
		/// this will simply find a lexEntry for adding a new sense to (assuming
		/// we already know that sense hasn't already been created).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="homographForm"></param>
		/// <returns></returns>
		public static ILexEntry FindBestLexEntryAmongstHomographs(FdoCache cache,
			string homographForm)
		{
			bool fGotExactMatch;
			return FindBestLexEntryAmongstHomographs(cache, homographForm, null, null, null, 0, out fGotExactMatch);
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
			List<ILexEntry> rgEntries = LexEntry.CollectHomographs(homographForm, 0,
				LexEntry.GetHomographList(cache, homographForm),
				MoMorphType.kmtStem, true);
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
					fCurrentIsNew = leCurrent.SensesOS.Count == 1 && newHvos.Contains(leCurrent.SensesOS.HvoArray[0]);
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
						string sDefnCurrent = lexS.Definition.AnalysisDefaultWritingSystem.UnderlyingTsString.Text;
						if ((sDefnCurrent == null && sDefnTarget == null) ||
							(sDefnCurrent != null && sDefnTarget != null && sDefnCurrent.Trim() == sDefnTarget.Trim()))
						{
							// We found a sense that has the same citation form and definition as the one
							// we're trying to merge.
							// Add the new domain to that sense (if not already present), delete the temporary one,
							// and return. (We're not displaying this sense, so don't bother trying to update the display)
							if (hvoDomain > 0 && !lexS.SemanticDomainsRC.Contains(hvoDomain))
								lexS.SemanticDomainsRC.Add(hvoDomain);
							fGotExactMatch = true;
						}
					}
				}
			} // loop over matching entries

			return leSaved;
		}

		/// <summary>
		/// Preload short name
		/// </summary>
		/// <param name="cache"></param>
		public static void PreloadShortName(FdoCache cache)
		{
			cache.LoadAllOfOneWsOfAMultiUnicode((int)
				LexSense.LexSenseTags.kflidGloss,
				"LexSense", cache.DefaultAnalWs);
		}

		/// <summary>
		/// The primary sort key for sorting a list of ShortNames.
		/// </summary>
		public override string SortKey
		{
			get { return ShortName; }
		}

		/// <summary>
		/// This method is called by the ReversalEntriesText virtual handler when text may have changed in the
		/// property, in order to update the actual list of reversal entries appropriately.
		/// </summary>
		/// <param name="tssVal">The new string.</param>
		/// <param name="ws">The ws.</param>
		public void CommitReversalEntriesText(ITsString tssVal, int ws)
		{
			LexSenseReversalEntriesTextHandler vh = BaseVirtualHandler.GetInstalledHandler(m_cache,
				"LexSense", LexSenseReversalEntriesTextHandler.StandardFieldName) as LexSenseReversalEntriesTextHandler;
			Debug.Assert(vh != null, "The 'LexSenseReversalEntriesTextHandler' virtual handler has to be created at application startup now.");

			ITsString tssOld = vh.GetValue(m_hvo, ws);
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
			int[] senseEntries = ReversalEntriesRC.HvoArray;
			int originalSenseEntriesCount = senseEntries.Length;
			int indexId;
			DbOps.ReadOneIntFromCommand(m_cache, "SELECT id FROM ReversalIndex WHERE WritingSystem=?", ws, out indexId);
			ReversalIndex revIndex;
			if (indexId == 0)
			{
				// Create the missing reversal index instead of crashing.  See LT-10186.
				ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
				IReversalIndex newIdx = m_cache.LangProject.LexDbOA.ReversalIndexesOC.Add(new ReversalIndex());
				newIdx.WritingSystemRA = lgws;
				// Copy any and all alternatives from lgws.Name to newIdx.Name
				foreach (ILgWritingSystem lgwsLoop in m_cache.LanguageEncodings)
				{
					string lgsNameAlt = lgws.Name.GetAlternative(lgwsLoop.Hvo);
					if (lgsNameAlt != null && lgsNameAlt.Length > 0)
						newIdx.Name.SetAlternative(lgsNameAlt, lgws.Hvo);
				}
				revIndex = (ReversalIndex)newIdx;
			}
			else
			{
				revIndex = (ReversalIndex)CmObject.CreateFromDBObject(m_cache, indexId, false);
			}

			// We need the list of ReversalIndexEntries that this sense references, but which belong
			// to another reversal index. Those hvos, plus any entry hvos from the given 'ws' that are reused,
			// get put into 'survivingEntries'.
			Set<int> survivingEntries = new Set<int>(originalSenseEntriesCount + formsColl.Count);
			// 'entriesNeedingPropChangeBackRef' will hold the hvos of all ReversalIndexEntry objects that need to have
			// their 'ReferringSenses' virtual property (re)computed.
			// Any reversal index entry that gains or loses a reference will need this (re)computing.
			List<int> entriesNeedingPropChangeBackRef = new List<int>(originalSenseEntriesCount + formsColl.Count);
			foreach (int entryHvo in senseEntries)
			{
				// Use 'cheapo' FDO object maker, since it is supposed to all be in the cache already.
				ReversalIndexEntry rie = (ReversalIndexEntry)CmObject.CreateFromDBObject(m_cache, entryHvo, false);
				int wsIndex = 0;
				int hvoIndex = m_cache.GetOwnerOfObjectOfClass(rie.Hvo, ReversalIndex.kclsidReversalIndex);
				if (hvoIndex != 0)
					wsIndex = m_cache.GetIntProperty(hvoIndex, (int)ReversalIndex.ReversalIndexTags.kflidWritingSystem);
				if (wsIndex == ws)
				{
					string form = rie.LongName;
					if (formsColl.Contains(form))
					{
						// Recycling an entry.
						survivingEntries.Add(rie.Hvo);
						formsColl.Remove(form); // Don't need to mess with it later on.
					}
					else
					{
						// It is being removed from the extant reference property,
						// so needs to recompute its back ref virtual handler.
						entriesNeedingPropChangeBackRef.Add(rie.Hvo);
					}
				}
				else
				{
					// These are all in some other ws, so they certainly must survive (cf. LT-3391).
					// Any entries that are reused will get added to this array later on.
					survivingEntries.Add(rie.Hvo);
				}
			}

			// Start Undoable section of code.
			m_cache.BeginUndoTask(Strings.ksUndoMakeRevEntries, Strings.ksRedoMakeRevEntries);
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			IActionHandler acth = sda.GetActionHandler();
			try
			{
				// add undo actions to reload the virtual handler and send prop changes to update displays
				if (acth != null)
				{
					List<PropChangedInfo> pciList = new List<PropChangedInfo>();
					pciList.Add(new PropChangedInfo(m_hvo, vh.Tag, ws, 0, 0));
					acth.AddAction(new PropChangedUndoAction(m_cache, true, PropChangeType.kpctNotifyAll, pciList));
					acth.AddAction(new ReloadVirtualHandlerUndoAction(m_cache, true, vh, m_hvo, vh.Tag, ws));
				}

				int cOldEntries = revIndex.EntriesOC.Count;
				foreach (string currentForm in formsColl)
				{
					int idRevEntry = revIndex.FindOrCreateReversalEntry(currentForm);
					entriesNeedingPropChangeBackRef.Add(idRevEntry);
					survivingEntries.Add(idRevEntry);
				}

				// Notify everyone, and his brother, about the changes done here.
				// PropChanged (1 of 3) Main: Replace main sense property with current set of entries.
				sda.Replace(m_hvo, (int)LexSense.LexSenseTags.kflidReversalEntries, 0, originalSenseEntriesCount,
					survivingEntries.ToArray(), survivingEntries.Count);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvo,
					(int)LexSense.LexSenseTags.kflidReversalEntries, 0, survivingEntries.Count, originalSenseEntriesCount);

				// remove entries from the index that are no longer valid
				foreach (int rieHvo in senseEntries)
				{
					if (!survivingEntries.Contains(rieHvo))
					{
						// the entry is no longer a reversal entry for this sense
						ReversalIndexEntry rie = new ReversalIndexEntry(m_cache, rieHvo);
						if (rie.SenseIds.Count == 0)
							// the entry is longer a reversal entry for any sense
							revIndex.EntriesOC.Remove(rie);
					}
				}

				// PropChanged (2 of 3) Affected Entries: (Re)compute
				// on the virtual property of select reversal index entries.
				ReversalIndexEntry.ResetReferringSenses(m_cache, entriesNeedingPropChangeBackRef);

				// PropChanged (3 of 3) Index Entries: Simulate a complete replacement of the entries collection,
				// BUT only if new entries were added in this method.
				int cNewEntries = revIndex.EntriesOC.Count;
				if (cNewEntries > cOldEntries)
				{
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, indexId,
						(int)ReversalIndex.ReversalIndexTags.kflidEntries,
						0, cNewEntries, cOldEntries);
				}

				// add redo actions to reload the virtual handler and send prop changes to update displays
				if (acth != null)
				{
					acth.AddAction(new ReloadVirtualHandlerUndoAction(m_cache, false, vh, m_hvo, vh.Tag, ws));
					List<PropChangedInfo> pciList = new List<PropChangedInfo>();
					pciList.Add(new PropChangedInfo(m_hvo, vh.Tag, ws, 0, 0));
					acth.AddAction(new PropChangedUndoAction(m_cache, false, PropChangeType.kpctNotifyAll, pciList));
				}
			}
			finally
			{
				if (acth != null && Marshal.IsComObject(acth))
					Marshal.ReleaseComObject(acth);
			}
			// End undoable section of code.
			m_cache.EndUndoTask();
		}

		/// <summary>
		/// Override this to provide special handling on a per-property basis for merging
		/// strings. For example, certain Sense properties insert semi-colon.
		/// If it answers false, the default merge is performed; if it answers true,
		/// an appropriate merge is presumed to have already been done.
		/// Note that on multistring properties it is called only once;
		/// override is responsible to merge all writing systems.
		/// </summary>
		/// <param name="flid">Field to merge.</param>
		/// <param name="cpt">Field type (cpt enumeration)</param>
		/// <param name="other"></param>
		/// <param name="fLoseNoStringData">Currently always true, supposed to indicate that the merge should not lose anything.</param>
		/// <param name="myCurrentValue"></param>
		/// <param name="srcCurrentValue"></param>
		/// <returns></returns>
		protected override bool MergeStringProp(int flid, int cpt, ICmObject other, bool fLoseNoStringData,
			object myCurrentValue, object srcCurrentValue)
		{
			if (flid == (int)LexSense.LexSenseTags.kflidGloss)
			{
				MultiUnicodeAccessor myMua = myCurrentValue as MultiUnicodeAccessor;
				myMua.MergeAlternatives(srcCurrentValue as MultiUnicodeAccessor, fLoseNoStringData, "; ");
				return true;
			}
			else if (flid == (int)LexSense.LexSenseTags.kflidDefinition)
			{
				MultiStringAccessor myMsa = myCurrentValue as MultiStringAccessor;
				myMsa.MergeAlternatives(srcCurrentValue as MultiStringAccessor, fLoseNoStringData, "; ");
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Get the desired type of an MSA to create for this sense.
		/// </summary>
		/// <returns></returns>
		public MsaType GetDesiredMsaType()
		{
			ILexEntry entry = Entry;
			int morphType = entry.MorphType;
			MsaType msaType = MsaType.kNotSet;
			bool fEntryIsAffixType = MoMorphType.IsAffixType(morphType);
			// Treat the type currently specified for the whole entry as having been seen.
			// This helps prevent showing the wrong dialog if the user changes the entry morph type.
			bool fAffixTypeSeen = fEntryIsAffixType;
			bool fStemTypeSeen = !fAffixTypeSeen;
			// Get current MSAs, and check which kind they are.
			// We are interested in knowing if they are the same kind or a mixed bag.
			foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
			{
				string msaTypeName = msa.GetType().Name;
				switch (msaTypeName)
				{
					case "MoStemMsa":
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
								morphType = MoMorphType.kmtMixed;
							}
							break;
						}
					case "MoUnclassifiedAffixMsa":
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
									morphType = MoMorphType.kmtMixed;
							}
							break;
						}
					case "MoInflAffMsa":
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
									morphType = MoMorphType.kmtMixed;
							}
							break;
						}
					case "MoDerivAffMsa":
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
									morphType = MoMorphType.kmtMixed;
							}
							break;
						}
				}
			}
			if (msaType == MsaType.kNotSet || msaType == MsaType.kMixed)
			{
				switch (morphType)
				{
					default:
						// assume unclassified affix.
						msaType = MsaType.kUnclassified;
						break;
					case MoMorphType.kmtMixed:
						// Make it the most general type appropriate for the type of the entry.
						if (fEntryIsAffixType)
							msaType = MsaType.kUnclassified;
						else
							msaType = MsaType.kStem;
						break;
					case MoMorphType.kmtRoot: // Fall through.
					case MoMorphType.kmtStem: // Fall through.
					case MoMorphType.kmtBoundRoot: // Fall through.
					case MoMorphType.kmtBoundStem: // Fall through.
					case MoMorphType.kmtParticle: // Fall through.
					case MoMorphType.kmtClitic: // Fall through.
					case MoMorphType.kmtEnclitic: // Fall through.
					case MoMorphType.kmtProclitic: //All of these get a Stem MSA.
						msaType = MsaType.kStem;
						break;
				}
			}
			return msaType;
		}

		/// <summary>
		/// Produce the list of reversal entries for the given writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString ReversalEntriesText(int ws)
		{
			ITsStrBldr tsb = TsStrBldrClass.Create();
			ITsTextProps ttpWs;
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			ttpWs = propsBldr.GetTextProps();
			tsb.Replace(0, 0, "", ttpWs); // In case it ends up empty, make sure it's empty in the right Ws.
			foreach (ReversalIndexEntry revEntry in this.ReversalEntriesRC)
			{
				if (revEntry.WritingSystem == ws)
				{
					if (tsb.Length > 0)
						tsb.Replace(tsb.Length, tsb.Length, "; ", ttpWs);
					tsb.Replace(tsb.Length, tsb.Length, revEntry.ReversalForm.GetAlternative(ws), ttpWs);
				}
			}
			ITsString tss = tsb.GetString();
			return tss;

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
		/// The LiftResidue field stores XML with an outer element &lt;lift-residue&gt; enclosing
		/// the actual residue.  This returns the actual residue, minus the outer element.
		/// </summary>
		public string LiftResidueContent
		{
			get
			{
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
				{
					sResidue = LexEntry.ExtractLIFTResidue(m_cache, m_hvo,
						(int)LexSense.LexSenseTags.kflidImportResidue,
						(int)LexSense.LexSenseTags.kflidLiftResidue);
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
				LiftResidue = sResidue;
			}
			return sResidue;
		}

		/// <summary>
		/// Generate an id string like "colorful_7ee714ef-2744-4fc2-b407-aab54e66a76f".
		/// If there's a LIFTid element in ImportResidue, use that instead.
		/// </summary>
		public string LIFTid
		{
			get
			{
				string sLiftId = null;
				string sResidue = this.LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
					sResidue = LexEntry.ExtractLIFTResidue(m_cache, m_hvo,
						(int)LexSenseTags.kflidImportResidue, (int)LexSenseTags.kflidLiftResidue);
				if (!String.IsNullOrEmpty(sResidue))
					sLiftId = LexEntry.ExtractAttributeFromLiftResidue(sResidue, "id");
				if (String.IsNullOrEmpty(sLiftId))
					return this.ShortName + "_" + this.Guid.ToString();
				else
					return sLiftId;
			}
		}

		/// <summary>
		/// Get the dateCreated value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateCreated
		{
			get
			{
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
				{
					sResidue = LexEntry.ExtractLIFTResidue(m_cache, m_hvo,
						(int)LexSense.LexSenseTags.kflidImportResidue,
						(int)LexSense.LexSenseTags.kflidLiftResidue);
					if (String.IsNullOrEmpty(sResidue))
						return null;
				}
				return LexEntry.ExtractAttributeFromLiftResidue(sResidue, "dateCreated");
			}
		}

		/// <summary>
		/// Get the dateModified value stored in LiftResidue (if it exists).
		/// </summary>
		public string LiftDateModified
		{
			get
			{
				string sResidue = LiftResidue;
				if (String.IsNullOrEmpty(sResidue))
				{
					sResidue = LexEntry.ExtractLIFTResidue(m_cache, m_hvo,
						(int)LexSense.LexSenseTags.kflidImportResidue,
						(int)LexSense.LexSenseTags.kflidLiftResidue);
					if (String.IsNullOrEmpty(sResidue))
						return null;
				}
				return LexEntry.ExtractAttributeFromLiftResidue(sResidue, "dateModified");
			}
		}

		/// <summary>
		/// Return anything from the ImportResidue which occurs prior to whatever LIFT may have
		/// added to it.  (LIFT import no longer adds to ImportResidue, but it did in the past.)
		/// </summary>
		public ITsString NonLIFTImportResidue
		{
			get
			{
				TsStringAccessor tsa = new TsStringAccessor(m_cache, m_hvo, (int)LexSenseTags.kflidImportResidue);
				return LexEntry.ExtractNonLIFTResidue(tsa);
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class LexReference
	{

		/// <summary>
		/// This is the string (the kind of lexical relation e.g. Antonym Relation)
		/// which is displayed in the Delete Lexical Relation dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				LexRefType lrtOwner =
					(LexRefType)CmObject.CreateFromDBObject(m_cache, OwnerHVO);
				int analWs = m_cache.DefaultAnalWs;
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();

				//If this is a whole/parts kind of relation then show this to the user
				//because ShortName is always Parts and otherwise we would have to figure out if we
				//are deleting this slice from the Lexical entry with the Whole slice or the Parts slice.
				switch ((LexRefType.MappingTypes)lrtOwner.MappingType)
				{
					case LexRefType.MappingTypes.kmtSenseTree:
					case LexRefType.MappingTypes.kmtEntryTree:
					case LexRefType.MappingTypes.kmtEntryOrSenseTree:
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
						tisb.Append(lrtOwner.ShortName);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
						tisb.Append(" / ");
						//Really it would be good to have lrtOwner.ReverseNameTSS which works
						//like lrtOwner.ShortNameTSS.  That way the correct style will show up
						//for the particular ReverseName like it does for ShortNameTSS
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
						tisb.Append(lrtOwner.ReverseName.BestAnalysisAlternative.Text);
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
		///
		/// </summary>
		/// <param name="hvoOld"></param>
		/// <param name="hvoNew"></param>
		public void ReplaceTarget(int hvoOld, int hvoNew)
		{
			// Update the timestamps of the affected objects (LT-5523).
			UpdateTargetTimestamps();
			int[] hvoTargets = this.TargetsRS.HvoArray;
			for (int i = 0; i < hvoTargets.Length; i++)
			{
				if (hvoTargets[i] == hvoOld)
				{
					TargetsRS.RemoveAt(i);
					if (hvoNew != 0)
					{
						// Update the timestamps of the affected objects (LT-5523).
						ICmObject co = CmObject.CreateFromDBObject(m_cache, hvoNew);
						(co as CmObject).UpdateTimestampForVirtualChange();
						TargetsRS.InsertAt(co, i);
					}
					return;
				}
			}
			if (hvoNew != 0)
			{
				// Update the timestamps of the affected objects (LT-5523).
				ICmObject co = CmObject.CreateFromDBObject(m_cache, hvoNew);
				(co as CmObject).UpdateTimestampForVirtualChange();
				TargetsRS.Append(co);
			}
		}

		/// <summary>
		/// Return the desired name for the owning type.
		/// </summary>
		/// <param name="ws">writing system id</param>
		/// <param name="hvoMember">database id of the reference member which needs the
		/// abbreviation</param>
		public string TypeName(int ws, int hvoMember)
		{
			LexRefType lrtOwner;
			SpecialWritingSystemCodes wsCode;
			GetOwnerAndWsCode(ws, out lrtOwner, out wsCode);
			/*
				For all but 2, 6, and 8 the field label for all items would be Name.
				For 2, 6, and 8, the label for the first item would be Name,
				while the label for the other items would be ReverseName.
			 */
			string x = null;
			switch ((LexRefType.MappingTypes)lrtOwner.MappingType)
			{
				case LexRefType.MappingTypes.kmtSenseAsymmetricPair:
				case LexRefType.MappingTypes.kmtSenseTree:
				case LexRefType.MappingTypes.kmtEntryAsymmetricPair:
				case LexRefType.MappingTypes.kmtEntryTree:
				case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					if (ws > 0)
					{
						if (TargetsRS.HvoArray[0] == hvoMember)
							x = lrtOwner.Name.GetAlternative(ws);
						else
							x = lrtOwner.ReverseName.GetAlternative(ws);
					}
					else
					{
						if (TargetsRS.HvoArray[0] == hvoMember)
							x = lrtOwner.Name.GetAlternative(wsCode);
						else
							x = lrtOwner.ReverseName.GetAlternative(wsCode);
					}
					break;
				default:
					if (ws > 0)
						x = lrtOwner.Name.GetAlternative(ws);
					else
						x = lrtOwner.Name.GetAlternative(wsCode);
					break;
			}
			return x;
		}

		/// <summary>
		/// Return the desired abbreviation for the owning type.
		/// </summary>
		/// <param name="ws">writing system id</param>
		/// <param name="hvoMember">database id of the reference member which needs the
		/// abbreviation</param>
		public string TypeAbbreviation(int ws, int hvoMember)
		{
			LexRefType lrtOwner;
			SpecialWritingSystemCodes wsCode;
			GetOwnerAndWsCode(ws, out lrtOwner, out wsCode);
			/*
				For all but 2, 6, and 8 the field label for all items would be Abbreviation.
				For 2, 6, and 8, the label for the first item would be Abbreviation,
				while the label for the other items would be ReverseAbbreviation.
			 */
			string x = null;
			switch ((LexRefType.MappingTypes)lrtOwner.MappingType)
			{
				case LexRefType.MappingTypes.kmtSenseAsymmetricPair:
				case LexRefType.MappingTypes.kmtSenseTree:
				case LexRefType.MappingTypes.kmtEntryAsymmetricPair:
				case LexRefType.MappingTypes.kmtEntryTree:
				case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					if (ws > 0)
					{
						if (TargetsRS.HvoArray[0] == hvoMember)
							x = lrtOwner.Abbreviation.GetAlternative(ws);
						else
							x = lrtOwner.ReverseAbbreviation.GetAlternative(ws);
					}
					else
					{
						if (TargetsRS.HvoArray[0] == hvoMember)
							x = lrtOwner.Abbreviation.GetAlternative(wsCode);
						else
							x = lrtOwner.ReverseAbbreviation.GetAlternative(wsCode);
					}
					break;
				default:
					if (ws > 0)
						x = lrtOwner.Abbreviation.GetAlternative(ws);
					else
						x = lrtOwner.Abbreviation.GetAlternative(wsCode);
					break;
			}
			return x;
		}

		private void GetOwnerAndWsCode(int ws, out LexRefType lrtOwner, out SpecialWritingSystemCodes wsCode)
		{
			lrtOwner =
				(LexRefType)CmObject.CreateFromDBObject(m_cache, OwnerHVO);
			wsCode = SpecialWritingSystemCodes.DefaultAnalysis;
			if (ws < 0)
			{
				switch (ws)
				{

					case (int)CellarModuleDefns.kwsAnal:
						wsCode = SpecialWritingSystemCodes.DefaultAnalysis;
						break;
					case (int)CellarModuleDefns.kwsVern:
						wsCode = SpecialWritingSystemCodes.DefaultVernacular;
						break;
					default:
						wsCode = (SpecialWritingSystemCodes)ws;
						break;
				}
			}

		}

		/// <summary>
		/// Return the 1-based index of the member in the relation if relevant, otherwise 0.
		/// </summary>
		/// <param name="hvoMember"></param>
		/// <returns></returns>
		public int SequenceIndex(int hvoMember)
		{
			LexRefType lrtOwner =
				(LexRefType)CmObject.CreateFromDBObject(m_cache, OwnerHVO);
			switch ((LexRefType.MappingTypes)lrtOwner.MappingType)
			{
				case LexRefType.MappingTypes.kmtEntryOrSenseSequence:
				case LexRefType.MappingTypes.kmtEntrySequence:
				case LexRefType.MappingTypes.kmtSenseSequence:
					for (int i = 0; i < TargetsRS.HvoArray.Length; ++i)
					{
						if (TargetsRS.HvoArray[i] == hvoMember)
							return i + 1;
					}
					return 0;
				default:
					return 0;
			}
		}

		/// <summary>
		/// Test to see if removing the target from the relationship will render the relationship incomplete.
		/// This can be used to decide whether to delete the relationship after deleting one of its targets.
		/// </summary>
		/// <param name="targetHvo"></param>
		/// <returns>true if deleting target will render the relationship inept.</returns>
		public bool IncompleteWithoutTarget(int targetHvo)
		{
			// make sure we contain the targetHvo
			if (!this.TargetsRS.Contains(targetHvo))
				return false;

			if (this.TargetsRS.Count <= 2)
			{
				// if a relationship will have less than 2 items remaining
				// then it won't be a relationship any more.
				return true;
			}
			else if (this.TargetsRS[0].Hvo == targetHvo)
			{
				// if target is the root of a tree, then removing it will break the relationship
				ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, this.OwnerHVO);
				if (lrt.MappingType == (int)LexRefType.MappingTypes.kmtEntryTree ||
					lrt.MappingType == (int)LexRefType.MappingTypes.kmtSenseTree ||
					lrt.MappingType == (int)LexRefType.MappingTypes.kmtEntryOrSenseTree)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This method retrieves the list of LexReference objects that contain the LexEntry
		/// or LexSense given by hvo. The list is pruned to remove any LexReference that
		/// targets only hvo unless parent LexRefType is a sequence/scale.  This pruning
		/// is needed to obtain proper display of the Dictionary (publication) view.
		/// Note: this must stay in sync with LoadAllMinimalLexReferences!
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static List<int> ExtractMinimalLexReferences(FdoCache cache, int hvo)
		{
			string sql = string.Format("SELECT t.Src, ty.MappingType, COUNT(t2.Dst) " +
				"FROM LexReference_Targets t " +
				"JOIN CmObject co on co.Id=t.Src " +
				"JOIN LexRefType ty on ty.Id=co.Owner$ " +
				"JOIN LexReference_Targets t2 on t2.Src=t.Src " +
				"WHERE t.Dst={0} " +
				"GROUP BY t.Src, ty.MappingType", hvo);

			List<int> list = new List<int>();
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				bool fIsNull;
				uint cbSpaceTaken;
				uint uintSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
				while (fMoreRows)
				{
					using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
					{
						odc.GetColValue(1, rgHvo, uintSize, out cbSpaceTaken, out fIsNull, 0);
						if (!fIsNull)
						{
							uint[] uIds = (uint[])MarshalEx.NativeToArray(rgHvo, 1, typeof(uint));
							using (ArrayPtr rgType = MarshalEx.ArrayToNative(1, typeof(uint)))
							{
								odc.GetColValue(2, rgType, 1, out cbSpaceTaken, out fIsNull, 0);
								if (!fIsNull)
								{
									byte[] uTypes = (byte[])MarshalEx.NativeToArray(rgType, 1, typeof(byte));
									if (uTypes[0] == (byte)LexRefType.MappingTypes.kmtSenseSequence ||
										uTypes[0] == (byte)LexRefType.MappingTypes.kmtEntrySequence ||
										uTypes[0] == (byte)LexRefType.MappingTypes.kmtEntryOrSenseSequence)
									{
										list.Add((int)uIds[0]);
									}
									else
									{
										using (ArrayPtr rgCount = MarshalEx.ArrayToNative(1, typeof(uint)))
										{
											odc.GetColValue(3, rgCount, uintSize, out cbSpaceTaken, out fIsNull, 0);
											if (!fIsNull)
											{
												uint[] uCount = (uint[])MarshalEx.NativeToArray(rgCount, 1, typeof(uint));
												if (uCount[0] > 1)
													list.Add((int)uIds[0]);
											}
										}
									}
								}
							}
						}
					}
					odc.NextRow(out fMoreRows);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}

			return list;
		}

		/// <summary>
		/// This method fills the Dictionary with all the lists of LexReference objects. Each
		/// list is pruned to remove any LexReference that targets only the key unless the
		/// parent LexRefType is a sequence/scale.  This pruning is needed to obtain
		/// proper printing of the Dictionary (publication) view.
		/// Note: this must stay in sync with ExtractMinimalLexReferences!
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="values"></param>
		public static void LoadAllMinimalLexReferences(FdoCache cache, Dictionary<int, List<int> > values)
		{
			string sql = "SELECT t.Dst, t.Src, ty.MappingType, COUNT(t2.Dst) " +
				"FROM LexReference_Targets t " +
				"JOIN CmObject co on co.Id=t.Src " +
				"JOIN LexRefType ty on ty.Id=co.Owner$ " +
				"JOIN LexReference_Targets t2 on t2.Src=t.Src " +
				"GROUP BY t.Dst, t.Src, ty.MappingType ORDER BY t.Dst";

			List<int> list = null;
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				bool fIsNull;
				uint cbSpaceTaken;
				uint uintSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
				int currentKey = 0;
				using (ArrayPtr rgXX = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					for (; fMoreRows; odc.NextRow(out fMoreRows))
					{
						odc.GetColValue(1, rgXX, uintSize, out cbSpaceTaken, out fIsNull, 0);
						if (fIsNull)
							continue;
						int key = DbOps.IntFromStartOfUintArrayPtr(rgXX);
						odc.GetColValue(2, rgXX, uintSize, out cbSpaceTaken, out fIsNull, 0);
						if (fIsNull)
							continue;
						int val = DbOps.IntFromStartOfUintArrayPtr(rgXX);
						odc.GetColValue(3, rgXX, 1, out cbSpaceTaken, out fIsNull, 0);
						if (fIsNull)
							continue;
						if (key != currentKey)
						{
							list = new List<int>();
							currentKey = key;
							values[currentKey] = list;
						}
						byte[] uTypes = (byte[])MarshalEx.NativeToArray(rgXX, 1, typeof(byte));
						if (uTypes[0] == (byte)LexRefType.MappingTypes.kmtSenseSequence ||
							uTypes[0] == (byte)LexRefType.MappingTypes.kmtEntrySequence ||
							uTypes[0] == (byte)LexRefType.MappingTypes.kmtEntryOrSenseSequence)
						{
							list.Add(val);
						}
						else
						{
							odc.GetColValue(4, rgXX, uintSize, out cbSpaceTaken, out fIsNull, 0);
							if (fIsNull)
								continue;
							int count = DbOps.IntFromStartOfUintArrayPtr(rgXX);
							if (count > 1)
								list.Add(val);
						}
					}
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
		}

		/// <summary>
		/// Update the timestamps for all of the target objects.
		/// </summary>
		public void UpdateTargetTimestamps()
		{
			for (int i = 0; i < TargetsRS.Count; ++i)
				(TargetsRS[i] as CmObject).UpdateTimestampForVirtualChange();
		}

		/// <summary>
		/// This supports a virtual property for displaying lexical references in a browse column.
		/// See LT-4859 for justification.
		/// </summary>
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
				LexRefType lrtOwner =
					(LexRefType)CmObject.CreateFromDBObject(m_cache, OwnerHVO);
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ITsStrBldr tsb = TsStrBldrClass.Create();
				tsb.ReplaceTsString(0, tsb.Length,
					lrtOwner.Abbreviation.BestAnalysisAlternative);
				switch (lrtOwner.MappingType)
				{
					case (int)LexRefType.MappingTypes.kmtSenseAsymmetricPair:
					case (int)LexRefType.MappingTypes.kmtSenseTree:
					case (int)LexRefType.MappingTypes.kmtEntryAsymmetricPair:
					case (int)LexRefType.MappingTypes.kmtEntryTree:
					case (int)LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair:
					case (int)LexRefType.MappingTypes.kmtEntryOrSenseTree:
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
	///
	/// </summary>
	public partial class LexRefType
	{
		/// <summary>
		///
		/// </summary>
		public enum MappingTypes
		{
			/// <summary></summary>
			kmtSenseCollection = 0,
			/// <summary></summary>
			kmtSensePair = 1,
			/// <summary>Sense Pair with different Forward/Reverse names</summary>
			kmtSenseAsymmetricPair = 2,
			/// <summary></summary>
			kmtSenseTree = 3,
			/// <summary></summary>
			kmtSenseSequence = 4,
			/// <summary></summary>
			kmtEntryCollection = 5,
			/// <summary></summary>
			kmtEntryPair = 6,
			/// <summary>Entry Pair with different Forward/Reverse names</summary>
			kmtEntryAsymmetricPair = 7,
			/// <summary></summary>
			kmtEntryTree = 8,
			/// <summary></summary>
			kmtEntrySequence = 9,
			/// <summary></summary>
			kmtEntryOrSenseCollection = 10,
			/// <summary></summary>
			kmtEntryOrSensePair = 11,
			/// <summary></summary>
			kmtEntryOrSenseAsymmetricPair = 12,
			/// <summary></summary>
			kmtEntryOrSenseTree = 13,
			/// <summary></summary>
			kmtEntryOrSenseSequence = 14
		};

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
				int analWs = m_cache.DefaultAnalWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
				tisb.AppendTsString(ShortNameTSS);

				int cnt = MembersOC.Count;
				if (cnt > 0)
				{
					string warningMsg = String.Format("\x2028\x2028{0}\x2028", Strings.ksLexRefUsedHere);
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append(warningMsg);
					if (cnt > 1)
						tisb.Append(String.Format(Strings.ksContainsXLexRefs, cnt));
					else
						tisb.Append(String.Format(Strings.ksContainsOneLexRef));
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or hvo is 0). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static ITsString BestAnalysisOrVernReverseName(FdoCache cache, int hvo)
		{
			ITsString tss = null;
			if (hvo != 0)
			{
				tss = cache.LangProject.GetMagicStringAlt(
					SIL.FieldWorks.FDO.LangProj.LangProject.kwsFirstAnalOrVern,
					hvo, (int)LexRefType.LexRefTypeTags.kflidReverseName);
			}
			if (tss == null)
			{
				tss = cache.MakeUserTss(Strings.ksQuestions);
				// JohnT: how about this?
				//return cache.MakeUserTss("a " + this.GetType().Name + " with no name");
			}
			return tss;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class MoStemName
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteStemName));
				return tisb.GetString();
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class MoInflAffixSlot
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// This will help when there is no string in the Name field.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteAffixSlot, " "));
				tisb.AppendTsString(ShortNameTSS);
				return tisb.GetString();
			}
		}
	}

	/// <summary></summary>
	public partial class MoInflAffixTemplate
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteAffixTemplate));
				return tisb.GetString();
			}
		}

	}

	/// <summary>
	///
	/// </summary>
	public partial class MoInflClass
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// This will help when there is no string in the Name field.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteInflectionClass, " "));
				tisb.AppendTsString(ShortNameTSS);
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Gets the MoInflClass that owns this MoInflClass,
		/// or null, if it is owned by the list.
		/// </summary>
		public IMoInflClass OwningInflectionClass
		{
			get
			{
				int ownerHvo = OwnerHVO;
				if (m_cache.IsSameOrSubclassOf(m_cache.GetClassOfObject(ownerHvo), MoInflClass.kClassId))
					return new MoInflClass(m_cache, ownerHvo) as IMoInflClass;
				else
					return null;
			}
		}

	}

	/// <summary></summary>
	public partial class PartOfSpeech
	{
		/// <summary>
		/// Get the POS that corresponds to the given source id from the master POS set,
		/// or null if not found.
		/// </summary>
		/// <param name="posSet"></param>
		/// <param name="sourceId"></param>
		/// <returns></returns>
		public static IPartOfSpeech GoldPOS(Set<ICmPossibility> posSet, string sourceId)
		{
			foreach (IPartOfSpeech pos in posSet)
			{
				if (pos.CatalogSourceId == sourceId)
					return pos;
			}

			return null;
		}
		/// <summary>
		/// Get the POS that corresponds to the given source id from the master POS list,
		/// or null if not found.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sourceId"></param>
		/// <returns></returns>
		public static IPartOfSpeech GoldPOS(FdoCache cache, string sourceId)
		{
			if (sourceId == null || sourceId.Length == 0)
				return null;
			string sQry = string.Format("SELECT * from PartOfSpeech " +
				"WHERE CatalogSourceId='{0}'", sourceId);
			int hvo;
			DbOps.ReadOneIntFromCommand(cache, sQry, null, out hvo);
			if (hvo > 0)
				return PartOfSpeech.CreateFromDBObject(cache, hvo);
			return null;
		}

		/// <summary>
		/// Override to exclude CatalogSourceId from being touched in a merge.
		/// </summary>
		/// <param name="flid">Field to merge.</param>
		/// <param name="cpt">Field type (cpt enumeration)</param>
		/// <param name="objSrc">Object being merged into this.</param>
		/// <param name="fLoseNoStringData">Currently always true, supposed to indicate that the merge should not lose anything.</param>
		/// <param name="myCurrentValue">Value for this object obtained from the relevant method for the property.
		/// Depending on the type, may be string, TsStringAccessor, MultiStringAccessor, or MultiUnicodeAccessor.</param>
		/// <param name="srcCurrentValue">Same thing for other.</param>
		/// <returns></returns>
		protected override bool MergeStringProp(int flid, int cpt, ICmObject objSrc, bool fLoseNoStringData,
			object myCurrentValue, object srcCurrentValue)
		{
			if (flid == (int)PartOfSpeech.PartOfSpeechTags.kflidCatalogSourceId)
				return true;
			return false;
		}

		/// <summary>
		/// Attempt to add feature to category as an inflectable feature
		/// </summary>
		/// <param name="cache">FDO cache</param>
		/// <param name="node">MGA node</param>
		/// <param name="feat">feature to add</param>
		public static void TryToAddInflectableFeature(FdoCache cache, XmlNode node, IFsFeatDefn feat)
		{
			if (node == null || feat == null)
				return;
			string sPosId = XmlUtils.GetOptionalAttributeValue(node, "posid");
			while (node.ParentNode != null && sPosId == null)
			{
				node = node.ParentNode;
				sPosId = XmlUtils.GetOptionalAttributeValue(node, "posid");
			}
			IPartOfSpeech pos = PartOfSpeech.GoldPOS(cache, sPosId);
			if (pos != null)
				pos.InflectableFeatsRC.Add(feat);
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
				int analWs = m_cache.DefaultAnalWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
				tisb.AppendTsString(ShortNameTSS);

				List<LinkedObjectInfo> linkedObjects = LinkedObjects;
				List<int> countedObjectIDs = new List<int>();
				int msaCount = 0;
				int analCount = 0;
				int alloCount = 0;
				int revCount = 0;
				foreach (LinkedObjectInfo loi in linkedObjects)
				{
					switch (loi.RelObjClass)
					{
						default:
							break;
						case MoStemMsa.kclsidMoStemMsa:
						{
							if (loi.RelObjField == (int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++msaCount;
								}
							}
							break;
						}
						case MoDerivAffMsa.kclsidMoDerivAffMsa:
						{
							if (loi.RelObjField == (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromPartOfSpeech
								|| loi.RelObjField == (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToPartOfSpeech)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++msaCount;
								}
							}
							break;
						}
						case MoDerivStepMsa.kclsidMoDerivStepMsa:
						{
							if (loi.RelObjField == (int)MoDerivStepMsa.MoDerivStepMsaTags.kflidPartOfSpeech)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++msaCount;
								}
							}
							break;
						}
						case MoInflAffMsa.kclsidMoInflAffMsa:
						{
							if (loi.RelObjField == (int)MoInflAffMsa.MoInflAffMsaTags.kflidPartOfSpeech)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++msaCount;
								}
							}
							break;
						}
						case MoUnclassifiedAffixMsa.kclsidMoUnclassifiedAffixMsa:
						{
							if (loi.RelObjField == (int)MoUnclassifiedAffixMsa.MoUnclassifiedAffixMsaTags.kflidPartOfSpeech)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++msaCount;
								}
							}
							break;
						}
						case MoAffixAllomorph.kclsidMoAffixAllomorph:
						{
							if (loi.RelObjField == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidMsEnvPartOfSpeech)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++alloCount;
								}
							}
							break;
						}
						case ReversalIndexEntry.kclsidReversalIndexEntry:
						{
							if (loi.RelObjField == (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidPartOfSpeech)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++revCount;
								}
							}
							break;
						}
						case WfiAnalysis.kclsidWfiAnalysis:
						{
							if (loi.RelObjField == (int)WfiAnalysis.WfiAnalysisTags.kflidCategory)
							{
								if (!countedObjectIDs.Contains(loi.RelObjId))
								{
									countedObjectIDs.Add(loi.RelObjId);
									++analCount;
								}
							}
							break;
						}
					}
				}

				int cnt = 1;
				string warningMsg = String.Format("\x2028\x2028{0}", Strings.ksCategUsedHere);
				bool wantMainWarningLine = true;
				if (analCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (analCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByWFAnals, cnt++, analCount));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByWFAnals, cnt++));
					wantMainWarningLine = false;
				}
				if (msaCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (msaCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByFuncs, cnt++, msaCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByFuncs, cnt++, "\x2028"));
					wantMainWarningLine = false;
				}
				if (alloCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (alloCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByAllos, cnt++, alloCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByAllos, cnt++, "\x2028"));
				}
				if (revCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (revCount > 1)
						tisb.Append(String.Format(Strings.ksIsUsedXTimesByRevEntries, cnt++, revCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksIsUsedOnceByRevEntries, cnt++, "\x2028"));
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Add any new inflectable features from an Xml description
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="item"></param>
		public void AddInflectableFeatsFromXml(FdoCache cache, XmlNode item)
		{
			IFsFeatureSystem featsys = cache.LangProject.MsFeatureSystemOA;
			XmlNodeList features = item.SelectNodes("fs/f");
			foreach (XmlNode feature in features)
			{
				XmlNode type = feature.SelectSingleNode("fs/@type");
				IFsFeatStrucType fst = null;
				if (type != null)
					fst = FsFeatureSystem.FindOrCreateFeatureTypeBasedOnXmlNode(featsys, type.InnerText, item);
				IFsFeatDefn defn = FsFeatureSystem.FindOrCreateFeatureDefnBasedOnXmlNode(item, featsys, fst);
				if (defn != null)
					InflectableFeatsRC.Add(defn);
			}
		}
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)PartOfSpeech.PartOfSpeechTags.kflidDefaultInflectionClass:
					return this;
				case (int)PartOfSpeech.PartOfSpeechTags.kflidBearableFeatures:
					return m_cache.LangProject.ExceptionFeatureType;
				case (int)PartOfSpeech.PartOfSpeechTags.kflidInflectableFeats:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)PartOfSpeech.PartOfSpeechTags.kflidDefaultInflectionClass:
					set = new Set<int>();
					foreach (IMoInflClass ic in AllInflectionClasses)
						set.Add(ic.Hvo);
					break;
				case (int)PartOfSpeech.PartOfSpeechTags.kflidBearableFeatures:
					set = new Set<int>();
					ILangProject lp = m_cache.LangProject;
					if (lp != null)
					{
						IFsFeatStrucType exceps = lp.ExceptionFeatureType;
						set.AddRange(exceps.FeaturesRS.HvoArray);
					}
					break;
				case (int)PartOfSpeech.PartOfSpeechTags.kflidInflectableFeats:
					set = new Set<int>();
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.HvoArray);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// Get the hvo of the highest PartOfSpeech in the hierarchy
		/// </summary>
		/// <param name="ps">Beginning PartOfSpeech</param>
		/// <returns>Hvo of the highest PartOfSpeech in the hierarchy</returns>
		public int GetHvoOfHighestPartOfSpeech(IPartOfSpeech ps)
		{
			int iHvo = ps.Hvo;
			while (ps.ClassID == PartOfSpeech.kClassId)
			{
				ps = CmObject.CreateFromDBObject(m_cache, ps.OwnerHVO) as IPartOfSpeech;
				if (ps != null)
					iHvo = ps.Hvo;
				else
					break;
			}
			return iHvo;
		}

		/// <summary>
		/// Get all inflection classes owned by this part of speech,
		/// and by any part of speech that owns this one,
		/// up to the owning list.
		/// </summary>
		public MoInflClassCollection AllInflectionClasses
		{
			get
			{
				MoInflClassCollection col = new MoInflClassCollection(m_cache);
				foreach (IMoInflClass ic in InflectionClassesOC)
					col.Add(ic);
				int ownerID = OwnerHVO;
				int classOwner = m_cache.GetClassOfObject(ownerID);
				if (classOwner == ClassID)
				{
					IPartOfSpeech owner = PartOfSpeech.CreateFromDBObject(m_cache, ownerID);
					foreach (IMoInflClass ic in owner.AllInflectionClasses)
						col.Add(ic);
				}
				return col;
			}
		}

		/// <summary>
		/// Get all stem names owned by this part of speech,
		/// and by any part of speech that owns this one,
		/// up to the owning list.
		/// </summary>
		public MoStemNameCollection AllStemNames
		{
			get
			{
				MoStemNameCollection col = new MoStemNameCollection(m_cache);
				foreach (IMoStemName sn in StemNamesOC)
					col.Add(sn);
				int ownerID = OwnerHVO;
				int classOwner = m_cache.GetClassOfObject(ownerID);
				if (classOwner == ClassID)
				{
					IPartOfSpeech owner = PartOfSpeech.CreateFromDBObject(m_cache, ownerID);
					foreach (IMoStemName sn in owner.AllStemNames)
						col.Add(sn);
				}
				return col;
			}
		}
		/// <summary>
		/// Get all affix slots IDs owned by this part of speech,
		/// and by any part of speech that owns this one,
		/// up to the owning list.
		/// </summary>
		public List<int> AllAffixSlotIDs
		{
			get
			{
				List<int> list = new List<int>();
				foreach (IMoInflAffixSlot slot in AffixSlotsOC)
					list.Add(slot.Hvo);
				int ownerID = OwnerHVO;
				int classOwner = m_cache.GetClassOfObject(ownerID);
				if (classOwner == ClassID)
				{
					IPartOfSpeech owner = PartOfSpeech.CreateFromDBObject(m_cache, ownerID);
					foreach (int id in owner.AllAffixSlotIDs)
						list.Add(id);
				}
				return list;
			}
		}

		/// <summary>
		/// Get a list of MSA IDs that reference this POS.
		/// </summary>
		public List<int> AllMSAClientIDs
		{
			get
			{
				// Find all MSAs that reference this POS.
				string sql = String.Format("SELECT Id " +
					"FROM MoStemMsa " +
					"WHERE PartOfSpeech={0} " +
					"UNION " +
					"SELECT Id " +
					"FROM MoUnclassifiedAffixMsa " +
					"WHERE PartOfSpeech={1} " +
					"UNION " +
					"SELECT Id " +
					"FROM MoInflAffMsa " +
					"WHERE PartOfSpeech={2} " +
					"UNION " +
					"SELECT Id " +
					"FROM MoDerivStepMsa " +
					"WHERE PartOfSpeech={3} " +
					"UNION " +
					"SELECT Id " +
					"FROM MoDerivAffMsa " +
					"WHERE FromPartOfSpeech={4} OR ToPartOfSpeech={5}",
					Hvo, Hvo, Hvo, Hvo, Hvo, Hvo);

				return DbOps.ReadIntsFromCommand(m_cache, sql, null);
			}
		}

		/// <summary>
		/// Get a list of sense IDs that reference MSAs that reference this POS.
		/// </summary>
		public List<int> AllSenseClientIDs
		{
			get
			{
				List<int> senseList = new List<int>();

				// Find all MSAs that reference this POS.
				foreach (int msaHvo in AllMSAClientIDs)
				{
					MoMorphSynAnalysis msa = (MoMorphSynAnalysis)CmObject.CreateFromDBObject(m_cache, msaHvo);
					senseList.AddRange(msa.AllSenseClientIDs);
				}

				return senseList;
			}
		}

		/// <summary>
		/// Get a list of entry IDs that own MSAs that reference this POS.
		/// </summary>
		public List<int> AllEntryClientIDs
		{
			get
			{
				List<int> entryList = new List<int>();

				// Find all MSAs that reference this POS.
				foreach (int msaHvo in AllMSAClientIDs)
				{
					MoMorphSynAnalysis msa = (MoMorphSynAnalysis)CmObject.CreateFromDBObject(m_cache, msaHvo);
					ICmObject owner = CmObject.CreateFromDBObject(m_cache, msa.OwnerHVO);
					if (owner is ILexEntry && !entryList.Contains(owner.Hvo))
						entryList.Add(owner.Hvo);
				}

				return entryList;
			}
		}

		/// <summary>
		/// Get a list of analyses IDs that reference MSAs that reference this POS.
		/// </summary>
		public List<int> AllMLAnalysesClientIDs
		{
			get
			{
				List<int> analList = new List<int>();

				// Find all MSAs that reference this POS.
				foreach (int msaHvo in AllMSAClientIDs)
				{
					string sql = String.Format("SELECT Owner$" +
						" FROM WfiMorphBundle_" +
						" WHERE Msa={0}", msaHvo);
					foreach (int analHvo in DbOps.ReadIntsFromCommand(m_cache, sql, null))
					{
						if (!analList.Contains(analHvo))
							analList.Add(analHvo);
					}
				}

				return analList;
			}
		}

		/// <summary>
		/// Get a list of analyses IDs that reference this POS.
		/// </summary>
		public List<int> AllAnalysesClientIDs
		{
			get
			{
				List<int> analList = new List<int>();

				// Find all analyses that reference this POS.
				string sql = String.Format("SELECT Id" +
					" FROM WfiAnalysis" +
					" WHERE Category={0}", Hvo);
				foreach (int analHvo in DbOps.ReadIntsFromCommand(m_cache, sql, null))
				{
					if (!analList.Contains(analHvo))
						analList.Add(analHvo);
				}

				return analList;
			}
		}

		/// <summary>
		/// Get a list of wordform IDs that own analyses that reference this POS.
		/// </summary>
		public List<int> AllWordformClientIDs
		{
			get
			{
				List<int> wordformList = new List<int>();

				// Find all analyses that use this POS (merpheme level).
				// This may over produce, as compounds may 'hit',
				// but where the final word-level category is not a match.
				foreach (int analHvo in AllMLAnalysesClientIDs)
				{
					WfiAnalysis anal = (WfiAnalysis)CmObject.CreateFromDBObject(m_cache, analHvo);
					if (!wordformList.Contains(anal.OwnerHVO))
						wordformList.Add(anal.OwnerHVO);
				}
				// Find all analyses that use this POS (analysis level).
				foreach (int analHvo in AllAnalysesClientIDs)
				{
					IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_cache, analHvo);
					if (!wordformList.Contains(anal.OwnerHVO))
						wordformList.Add(anal.OwnerHVO);
				}

				return wordformList;
			}
		}

		/// <summary>
		/// Get a list of annotation IDs that that indirectly reference this POS through the WFI.
		/// </summary>
		public List<int> AllSentenceClientIDs
		{
			get
			{
				List<int> annotationIds = new List<int>();

				// Find all sentences that use this POS.
				// Really, it is the annotation ids.
				foreach (int wfHvo in AllWordformClientIDs)
				{
					IWfiWordform wf = WfiWordform.CreateFromDBObject(m_cache, wfHvo);
					annotationIds.AddRange(CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, wf.Hvo));
					foreach (IWfiAnalysis anal in wf.AnalysesOC)
					{
						annotationIds.AddRange(CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, anal.Hvo));
						foreach (WfiGloss gloss in anal.MeaningsOC)
							annotationIds.AddRange(CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, gloss.Hvo));
					}
				}

				return annotationIds;
			}
		}

		/// <summary>
		/// Get a list of Compound Rule IDs that reference this POS.
		/// </summary>
		public List<int> AllCompoundRuleClientIDs
		{
			get
			{
				List<int> crList = new List<int>();

				// Find all MSAs that reference this POS.
				foreach (int msaHvo in AllMSAClientIDs)
				{
					MoMorphSynAnalysis msa = (MoMorphSynAnalysis)CmObject.CreateFromDBObject(m_cache, msaHvo);
					ICmObject owner = CmObject.CreateFromDBObject(m_cache, msa.OwnerHVO);
					if (owner is IMoCompoundRule && !crList.Contains(owner.Hvo))
						crList.Add(owner.Hvo);
				}

				return crList;
			}
		}

		/// <summary>
		/// Return the number of unique LexEntries that reference this POS via MoStemMsas.
		/// </summary>
		public int NumberOfLexEntries
		{
			get
			{
				int count = 0;
				// The SQL command must NOT modify the database contents!
				string sSql = String.Format("select count(distinct o.owner$) from MoStemMsa msa" +
					" join CmObject o on o.id = msa.id" +
					" where msa.PartOfSpeech = {0}" +
					" group by msa.PartOfSpeech", Hvo);
				DbOps.ReadOneIntFromCommand(m_cache, sSql, null, out count);
				return count;
			}
		}
		/// <summary>
		/// Determine if the POS or any of its super POSes require inflection (i.e. have an inflectional template)
		/// </summary>
		/// <returns>true if so; false otherwise</returns>
		public bool RequiresInflection()
		{
			bool fResult = false;  // be pessimistic
			PartOfSpeech pos = this;
			while (pos != null)
			{
				if (pos.AffixTemplatesOS.Count > 0)
				{
					fResult = true;
					break;
				}
				ICmObject obj = CmObject.CreateFromDBObject(m_cache, pos.OwnerHVO);
				pos = obj as PartOfSpeech;
			}
			return fResult;
		}
		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>
		/// Default Inflection Class is only relevant when there are inflection classes.
		/// </remarks>
		/// <returns></returns>
		public override bool IsFieldRelevant(int flid)
		{
			if (flid == (int)PartOfSpeech.PartOfSpeechTags.kflidDefaultInflectionClass)
			{
				if (InflectionClassesOC.Count <= 0)
					return false;
			}
			return base.IsFieldRelevant(flid);
		}

		/// <summary>
		/// When a part of speech is moved, if it has templates that refer to slots of its old
		/// owner, they should be copied.
		/// </summary>
		/// <param name="hvoOldOwner"></param>
		public override void MoveSideEffects(int hvoOldOwner)
		{
			base.MoveSideEffects(hvoOldOwner);
			Dictionary<int, int> slots = new Dictionary<int, int>(); // key is old slot, value is new one; may be identical.
			int oldCount = AffixSlotsOC.Count;
			foreach (IMoInflAffixTemplate template in AffixTemplatesOS)
			{
				FixSlots(slots, template.PrefixSlotsRS);
				FixSlots(slots, template.SlotsRS);
				FixSlots(slots, template.SuffixSlotsRS);
			}
			int newCount = AffixSlotsOC.Count;
			if (newCount != oldCount)
			{
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, this.Hvo,
					(int)PartOfSpeech.PartOfSpeechTags.kflidAffixSlots, 0, newCount, oldCount);
				// Undo requires a refresh to get rid of the new slots from the data entry view.
				IActionHandler acth = m_cache.ActionHandlerAccessor;
				if (acth != null)
					acth.AddAction(new UndoRefreshAction());

			}
		}

		private void FixSlots(Dictionary<int, int> slots, FdoReferenceSequence<IMoInflAffixSlot> slotList)
		{
			for (int i = 0; i < slotList.Count; i++)
			{
				IMoInflAffixSlot slot = slotList[i];
				int slotHvo = slot.Hvo;
				int hvoRep = 0;
				if (slots.ContainsKey(slotHvo))
				{
					hvoRep = slots[slotHvo];
				}
				else
				{
					// The slot is fine if it is owned by this or one of the owners of this.
					int hvoOwner = m_cache.GetOwnerOfObject(slotHvo);
					int hvoAllowedOwner = this.Hvo;
					while (hvoAllowedOwner != 0 && hvoOwner != hvoAllowedOwner)
						hvoAllowedOwner = m_cache.GetOwnerOfObject(hvoAllowedOwner);
					if (hvoAllowedOwner == 0)
					{
						// Need to copy slot!
						// This works the first time, but is not redoable.
						//hvoRep = m_cache.CopyObject(slot.Hvo, this.Hvo, (int)PartOfSpeech.PartOfSpeechTags.kflidAffixSlots);
						IMoInflAffixSlot newSlot = AffixSlotsOC.Add(new MoInflAffixSlot());
						hvoRep = newSlot.Hvo;
						newSlot.Description.CopyAlternatives(slot.Description);
						newSlot.Name.CopyAlternatives(slot.Name);
						newSlot.Optional = slot.Optional;
						// And any existing MSAs in the slot should be copied too...these are
						// incoming references, so CopyObject doesn't deal with them.
						string sql = string.Format("select src from MoInflAffMsa_Slots where dst={0}",
							slotHvo);
						foreach (int hvoMsa in DbOps.ReadIntArrayFromCommand(m_cache, sql, null))
						{
							IMoInflAffMsa msa = MoInflAffMsa.CreateFromDBObject(m_cache, hvoMsa);
							msa.SlotsRC.Add(hvoRep);
						}
					}
					else
					{
						hvoRep = slotHvo;
					}
					slots[slotHvo] = hvoRep;
				}
				if (hvoRep != slotHvo)
				{
					slotList.RemoveAt(i);
					slotList.InsertAt(hvoRep, i);
				}
			}
		}
	}

	/// <summary>
	/// Additional methods needed to support the LexEntryRef class.
	/// </summary>
	public partial class LexEntryRef
	{
		/// <summary>
		/// This value is used in LexEntryRef.RefType to indicate a variant.
		/// </summary>
		public const int krtVariant = 0;
		/// <summary>
		/// This value is used in LexEntryRef.RefType to indicate a complex form.
		/// </summary>
		public const int krtComplexForm = 1;

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
				case (int)LexEntryRefTags.kflidComplexEntryTypes:
					return m_cache.LangProject.LexDbOA.ComplexEntryTypesOA;
				case (int)LexEntryRefTags.kflidVariantEntryTypes:
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
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				// MinorEntries, Subentries, Variants
				case (int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes:
				case (int)LexEntryRef.LexEntryRefTags.kflidPrimaryLexemes:
					// TODO: This needs fixing to include senses, but we probably don't want to just have
					// a flat list. We probably want a special chooser that allows selecting the entry,
					// then one of the senses from the entry.
					set = new Set<int>(m_cache.LangProject.LexDbOA.EntriesOC.HvoArray);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// Get a TsString that represents this LexEntryRef as it could be used in a deletion
		/// confirmaion dialogue.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				if (ComplexEntryTypesRS.Count > 0)
				{
					for (int i = 0; i < ComplexEntryTypesRS.Count; ++i)
					{
						if (i > 0)
							tisb.AppendTsString(m_cache.MakeAnalysisTss(", "));
						tisb.AppendTsString(ComplexEntryTypesRS[i].ShortNameTSS);
					}
				}
				else
				{
					for (int i = 0; i < VariantEntryTypesRS.Count; ++i)
					{
						if (i > 0)
							tisb.AppendTsString(m_cache.MakeAnalysisTss(", "));
						tisb.AppendTsString(VariantEntryTypesRS[i].ShortNameTSS);
					}
				}
				tisb.AppendTsString(m_cache.MakeAnalysisTss(": "));
				for (int i = 0; i < ComponentLexemesRS.Count; ++i)
				{
					if (i > 0)
						tisb.AppendTsString(m_cache.MakeAnalysisTss(", "));
					tisb.AppendTsString(ComponentLexemesRS[i].ShortNameTSS);
				}
				return tisb.GetString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the example
		/// sentences owned by top-level senses owned by the owner of this LexEntryRef.
		/// Note: this must stay in sync with LoadAllExampleSentences!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> ExampleSentences
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT e.Dst " +
					"FROM LexEntryRef_ ler " +
					"JOIN LexEntry_Senses s ON s.Src=ler.Owner$ " +
					"JOIN LexSense_Examples e ON e.Src=s.Dst " +
					"WHERE ler.Id={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects whose owning LexEntry objects own
		/// (top-level) LexSense objects that own LexExampleSentence objects.  The keys in the
		/// dictionary are all the LexEntryRef objects that are found with this relationship,
		/// and the values are the lists of the LexExampleSentence objects owned by the LexSense
		/// objects that are owned by the LexEntry objects that own the keys.
		/// Note: this must stay in sync with ExampleSentences!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllExampleSentences(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT ler.Id, e.Dst " +
				"FROM LexEntryRef_ ler " +
				"JOIN LexEntry_Senses s ON s.Src=ler.Owner$ " +
				"JOIN LexSense_Examples e ON e.Src=s.Dst " +
				"ORDER BY ler.Id";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a virtual property.  It returns the list of ids for all the
		/// MoMorphoSyntaxAnalysis objects used by top-level senses owned by the owner of this
		/// LexEntryRef.  Note: this must stay in sync with LoadAllMorphoSyntaxAnalyses!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> MorphoSyntaxAnalyses
		{
			get
			{
				string qry = String.Format("SELECT DISTINCT ls.MorphoSyntaxAnalysis " +
					"FROM LexEntryRef_ ler " +
					"JOIN LexEntry_Senses s ON s.Src=ler.Owner$ " +
					"JOIN LexSense ls ON ls.Id=s.Dst " +
					"WHERE ler.Id={0}",
					this.m_hvo);
				return DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the dictionary for all LexEntryRef objects whose owning LexEntry objects own
		/// (top-level) LexSense objects that refer to MoMorphoSyntaxAnalysis objects.  The keys
		/// in the dictionary are all the LexEntryRef objects that are found with this
		/// relationship, and the values are the lists of the MoMorphoSyntaxAnalysis objects
		/// used by the LexSense objects that are owned by the LexEntry objects that own the
		/// keys.  Note: this must stay in sync with MorphoSyntaxAnalyses!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadAllMorphoSyntaxAnalyses(FdoCache cache,
			Dictionary<int, List<int>> values)
		{
			string qry = "SELECT DISTINCT ler.Id, ls.MorphoSyntaxAnalysis " +
				"FROM LexEntryRef_ ler " +
				"JOIN LexEntry_Senses s ON s.Src=ler.Owner$ " +
				"JOIN LexSense ls ON ls.Id=s.Dst " +
				"ORDER BY ler.Id";
			DbOps.LoadDictionaryFromCommand(cache, qry, null, values);
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
	}

	/// <summary>
	/// Additional methods needed to support the LexEntryRef class.
	/// </summary>
	public partial class LexEntryType : IComparable
	{
		#region IComparable Members

		/// <summary>
		/// Allow LexEntryType objects to be compared/sorted.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			LexEntryType that = obj as LexEntryType;
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

	public partial class MoAffixProcess
	{
		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			IPhVariable var = new PhVariable();
			InputOS.Append(var);

			IMoCopyFromInput copy = new MoCopyFromInput();
			OutputOS.Append(copy);
			copy.ContentRA = var;

			IsAbstract = true;
		}
	}

	public partial class MoMorphData
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
				catch (Exception)
				{
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
				catch (Exception)
				{
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


	#endregion // Lexicon

	#region Reversal

	/// <summary>
	/// Summary description for ReversalIndex.
	/// </summary>
	public partial class ReversalIndex
	{
		/// <summary>
		/// Create other required elements.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			PartsOfSpeechOA = new CmPossibilityList();
			PartsOfSpeechOA.ItemClsid = PartOfSpeech.kClassId;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This allows each class to verify that it is OK to delete the object.
		/// If it is not Ok to delete the object, a message should be given explaining
		/// why the object can't be deleted.
		/// </summary>
		/// <returns>True if Ok to delete.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool ValidateOkToDelete()
		{
			return false;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				string sn = Name.GetAlternative(WritingSystemRAHvo);
				if (sn ==  null || sn == Strings.ksStars)
					sn = Name.AnalysisDefaultWritingSystem;
				if (sn == null || sn == Strings.ksStars)
					sn = Name.BestAnalysisAlternative.Text;
				if (sn == null || sn == Strings.ksStars)
				{
					sn = WritingSystemRA.Name.AnalysisDefaultWritingSystem;
					Name.AnalysisDefaultWritingSystem = sn;
				}
				if (sn == null || sn == Strings.ksStars)
					sn = WritingSystemRA.Name.BestAnalysisAlternative.Text;
				return sn == null || sn == String.Empty ? Strings.ksQuestions : sn;
			}
		}

		/// <summary>
		/// Get a list of all entries and subentries.
		/// </summary>
		public List<int> AllEntries
		{
			get
			{
				string sql = string.Format("SELECT x.Id FROM dbo.fnGetOwnedIds({0},{1},{2}) x" +
					" LEFT OUTER JOIN ReversalIndexEntry_ReversalForm rf ON" +
					" rf.Obj=x.Id AND rf.Ws={3} ORDER BY rf.Txt",
					Hvo,
					(int)ReversalIndex.ReversalIndexTags.kflidEntries,
					(int)ReversalIndexEntry.ReversalIndexEntryTags.kflidSubentries,
					WritingSystemRAHvo);
				return DbOps.ReadIntsFromCommand(m_cache, sql, WritingSystemRAHvo);
			}
		}

		/// <summary>
		/// Try to find an entry in this reversal index with the matching form.
		/// </summary>
		/// <param name="form"></param>
		/// <returns>The HVO of the matching entry, or 0, if not found.</returns>
		public int FindEntryWithForm(string form)
		{
			int idRevEntry = 0;
			IOleDbCommand odc = null;
			try
			{
				//FIX ME!!!
				string sql = "SELECT Obj FROM ReversalIndexEntry_ReversalForm" +
					" WHERE Txt=? AND Ws=?";
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				odc.SetStringParameter(1, // 1-based parameter index
					(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, //flags
					form,
					(uint)form.Length); // despite doc, impl makes clear this is char count
				uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
				odc.SetParameter(2, // 1-based parameter index
					(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, //flags
					(ushort)DBTYPEENUM.DBTYPE_I4,
					new uint[] { (uint)WritingSystemRAHvo },
					uintSize);
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				bool fIsNull;
				uint cbSpaceTaken;

				if (fMoreRows)
				{
					using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
					{
							odc.GetColValue(1, rgHvo, uintSize, out cbSpaceTaken, out fIsNull, 0);
							if (!fIsNull)
								idRevEntry = (int)(uint)Marshal.PtrToStructure((IntPtr)rgHvo, typeof(uint));
					}
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			return idRevEntry;
		}

		/// <summary>
		/// Gets the set of entries owned by this reversal index from the set of entries in the input.
		/// The input will come from some source such as the referenced index entries of a sense.
		/// </summary>
		/// <param name="entries">An array which must contain ReversalIndexEntry only objects</param>
		/// <returns>A List of ReversalIndexEntry IDs that match any of the entries in the input array.</returns>
		public List<int> EntriesForSense(List<IReversalIndexEntry> entries)
		{
			List<int> matchingEntryIDs = new List<int>();
			foreach (IReversalIndexEntry rie in entries)
			{
				int hvoIndex = m_cache.GetOwnerOfObjectOfClass(rie.Hvo, kclsidReversalIndex);
				if (hvoIndex == this.Hvo)
					matchingEntryIDs.Add(rie.Hvo);
			}
			return matchingEntryIDs;
		}

		/// <summary>
		/// Return a deletion description string that might scare off anyone who doesn't
		/// know what he's doing.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int cEntries = this.EntriesOC.Count;
				int cSenses = 0;
				if (cEntries > 0)
				{
					string sSql = string.Format("SELECT COUNT(DISTINCT lsre.Src)" +
						" FROM LexSense_ReversalEntries lsre" +
						" JOIN ReversalIndexEntry rie on rie.Id=lsre.Dst" +
						" JOIN ReversalIndex_Entries e on e.Dst=rie.Id" +
						" JOIN ReversalIndex ri on ri.id=e.Src" +
						" WHERE ri.WritingSystem={0}", this.WritingSystemRAHvo);
					DbOps.ReadOneIntFromCommand(m_cache, sSql, null, out cSenses);
				}
				// "{0} has {1} entries referenced by {2} senses.";
				string sFmt = FdoResources.ResourceString("kstidReversalIndexDeletionText");
				string sDeletionText = string.Format(sFmt, ShortName, cEntries, cSenses);
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(sDeletionText, m_cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// Find the reversal index entry given by the specified long name, or if it doesn't exist, create
		/// it.  In either case, return its hvo.
		/// </summary>
		public int FindOrCreateReversalEntry(string longName)
		{
			List<string> forms = new List<string>();
			ReversalIndexEntry.GetFormList(longName, forms);
			return FindOrCreateReversalEntry(forms);
		}
		/// <summary>
		/// Find the reversal index entry given by rgsForms, or if it doesn't exist, create
		/// it.  In either case, return its hvo.
		/// </summary>
		public int FindOrCreateReversalEntry(List<string> rgsForms)
		{
			List<List<int>> rghvosMatching = new List<List<int>>(rgsForms.Count);
			string sSql = String.Format(
				"SELECT Obj FROM ReversalIndexEntry_ReversalForm WHERE Ws={0} AND Txt=?",
				WritingSystemRAHvo);
			for (int i = 0; i < rgsForms.Count; ++i)
				rghvosMatching.Add(DbOps.ReadIntsFromCommand(Cache, sSql, rgsForms[i]));
			List<int> rghvoOwners = new List<int>(rgsForms.Count);
			rghvoOwners.Add(Hvo);
			// The next two variables record the best partial match, if any.
			int maxLevel = 0;
			int maxOwner = Hvo;
			int hvo = FindMatchingReversalEntry(rgsForms, rghvoOwners, rghvosMatching,
				0, ref maxLevel, ref maxOwner);
			if (hvo == 0)
			{
				// Create whatever we need to since we didn't find a full match.
				ICmObject owner = CmObject.CreateFromDBObject(Cache, maxOwner);
				Debug.Assert(maxLevel < rgsForms.Count);
				for (int i = maxLevel; i < rgsForms.Count; ++i)
				{
					IReversalIndexEntry rie = new ReversalIndexEntry();
					if (owner is IReversalIndex)
					{
						(owner as IReversalIndex).EntriesOC.Add(rie);
					}
					else
					{
						Debug.Assert(owner is IReversalIndexEntry);
						(owner as IReversalIndexEntry).SubentriesOC.Add(rie);
					}
					rie.ReversalForm.SetAlternative(rgsForms[i], WritingSystemRAHvo);
					owner = rie;
					hvo = rie.Hvo;
				}
				Debug.Assert(hvo != 0);
			}
			return hvo;
		}

		private int FindMatchingReversalEntry(List<string> rgsForms, List<int> rghvoOwners,
			List<List<int>> rghvosMatching, int idxForms, ref int maxLevel,
			ref int maxOwner)
		{
			foreach (int entryId in rghvosMatching[idxForms])
			{
				IReversalIndexEntry rie = ReversalIndexEntry.CreateFromDBObject(Cache, entryId);
				Debug.Assert(rie.ReversalIndex.Hvo == rghvoOwners[0]);
				if (rie.ReversalIndex.Hvo != rghvoOwners[0])
					continue;
				if (rie.OwnerHVO != rghvoOwners[idxForms])
					continue;
				int level = idxForms + 1;
				if (level < rgsForms.Count)
				{
					if (level > maxLevel)
					{
						maxLevel = level;
						maxOwner = rie.Hvo;
					}
					// we have a match at this level: recursively check the next level.
					rghvoOwners.Add(rie.Hvo);
					int hvo = FindMatchingReversalEntry(rgsForms, rghvoOwners, rghvosMatching,
						level, ref maxLevel, ref maxOwner);
					if (hvo != 0)
						return hvo;
					rghvoOwners.RemoveAt(level);
				}
				else
				{
					// We have a match all the way down: return the hvo.
					return rie.Hvo;
				}
			}
			return 0;
		}
	}


	/// <summary>
	/// Summary description for ReversalIndexEntry.
	/// </summary>
	public partial class ReversalIndexEntry
	{
		private int m_ws = 0;		// Writing system of the owning ReversalIndex.  Set when needed.

		#region Construction & initialization

		/// <summary>
		/// Create other required elements.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
		}

		#endregion Construction & initialization

		#region Properties

		/// <summary>
		/// The shortest, non-abbreviated label for the content of this object.
		/// this is the name that you would want to show up in a chooser list.
		/// </summary>
		public override string ShortName
		{
			get
			{
				int ws = WritingSystem;
				string sForm = ReversalForm.GetAlternative(ws);
				if (sForm == null || sForm.Length == 0)
					return null;
				else
					return sForm;
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
				int flid = this.OwningFlid;
				int hvoOwner = this.OwnerHVO;
				while (flid == (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidSubentries)
				{
					IReversalIndexEntry rie = ReversalIndexEntry.CreateFromDBObject(m_cache, hvoOwner);
					bldr.Insert(0, ": ");
					bldr.Insert(0, rie.ShortName);
					flid = rie.OwningFlid;
					hvoOwner = rie.OwnerHVO;
				}
				return bldr.ToString();
			}
		}

		/// <summary>
		/// Given a string purporting to be the LongName of a reversal index entry,
		/// split it into the sequence of individial RIE forms that it represents
		/// (from the top of the hierarchy down).
		/// </summary>
		public static void GetFormList(string longNameIn, List<string> forms)
		{
			string longName = longNameIn.Trim();
			forms.Clear();
			// allow the user to indicate subentries by separating words by ':'.
			// See LT-4665.
			string[] rgsDummy = longName.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < rgsDummy.Length; ++i)
			{
				rgsDummy[i] = rgsDummy[i].Trim();
				if (!String.IsNullOrEmpty(rgsDummy[i]))
					forms.Add(rgsDummy[i]);
			}
		}

		/// <summary>
		/// Return the writing system id of the ReversalIndex which owns this ReversalIndexEntry.
		/// </summary>
		public int WritingSystem
		{
			get
			{
				if (m_ws == 0)
				{
					int hvoRevIdx = m_cache.GetOwnerOfObjectOfClass(m_hvo, Ling.ReversalIndex.kclsidReversalIndex);
					Debug.Assert(hvoRevIdx > 0);
					m_ws = m_cache.GetIntProperty(hvoRevIdx, (int)Ling.ReversalIndex.ReversalIndexTags.kflidWritingSystem);
					Debug.Assert(m_ws > 0);
				}
				return m_ws;
			}
		}

		/// <summary>
		/// Return the writing system (ICULocale) used to sort ReversalIndexEntry objects.
		/// </summary>
		public override string SortKeyWs
		{
			get
			{
				string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(
					WritingSystem);

				if (sWs == null || sWs == string.Empty)
					sWs = m_cache.FallbackUserLocale;

				if (sWs == null || sWs == "")
					sWs = "en";
				return sWs;
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
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(ShortName, WritingSystem);
			}
		}

		/// <summary>
		/// Get a list of all owning entries up to the index.
		/// </summary>
		public List<IReversalIndexEntry> AllOwningEntries
		{
			get
			{
				List<IReversalIndexEntry> results = new List<IReversalIndexEntry>();
				IReversalIndexEntry owningEntry = OwningEntry;
				if (owningEntry != null)
				{
					results = owningEntry.AllOwningEntries;
					results.Add(owningEntry);
				}
				return results;
			}
		}

		/// <summary>
		/// Get a list of DB Ids of the senses that refer to the index entry.
		/// </summary>
		public List<int> SenseIds
		{
			get
			{
				string sql = "SELECT Src"
					+ " FROM LexSense_ReversalEntries"
					+ " WHERE Dst=?";
				return DbOps.ReadIntsFromCommand(m_cache, sql, Hvo);
			}
		}

		/// <summary>
		/// Gets the reversal index that ultimately owns this entry.
		/// </summary>
		public IReversalIndex ReversalIndex
		{
			get
			{
				int ownerHvo = OwnerHVO;
				if (m_cache.GetClassOfObject(ownerHvo) == Ling.ReversalIndex.kClassId)
					return new ReversalIndex(m_cache, ownerHvo) as IReversalIndex;
				else
					return OwningEntry.ReversalIndex;
			}
		}

		/// <summary>
		/// Gets the reversal index entry that owns this entry,
		/// or null, if it is owned by the index.
		/// </summary>
		public IReversalIndexEntry OwningEntry
		{
			get
			{
				int ownerHvo = OwnerHVO;
				if (m_cache.GetClassOfObject(ownerHvo) == ReversalIndexEntry.kClassId)
					return new ReversalIndexEntry(m_cache, ownerHvo) as IReversalIndexEntry;
				else
					return null;
			}
		}

		/// <summary>
		/// Gets the entry that is owned by the index.
		/// </summary>
		/// <remarks>
		/// It may return itself, if it is owned by the index,
		/// otherwise, it will move up the ownership chain to find the one
		/// that is owned by the index.
		/// </remarks>
		public IReversalIndexEntry MainEntry
		{
			get
			{
				IReversalIndexEntry owningEntry = OwningEntry;
				if (owningEntry == null)
					return this;
				else
					return owningEntry.MainEntry;
			}
		}

		/// <summary>
		/// Return a string containing all the forms in alternate (related) writing systems.
		/// </summary>
		public ITsString OtherWsFormsTSS
		{
			get
			{
				int wsIndex = this.WritingSystem;
				int[] rgWs = m_cache.LangProject.GetReversalIndexWritingSystems(Hvo, false);
				int wsAnal = m_cache.DefaultAnalWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, wsAnal);
				tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				tisb.Append(" [");
				int cstr = 0;
				for (int i = 0; i < rgWs.Length; ++i)
				{
					int ws = rgWs[i];
					if (ws == wsIndex)
						continue;
					string sForm = ReversalForm.GetAlternative(ws);
					if (sForm != null && sForm.Length != 0)
					{
						if (cstr > 0)
							tisb.Append(",  ");
						++cstr;
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
							(int)FwTextPropVar.ktpvDefault, ws);
						tisb.Append(sForm);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
							(int)FwTextPropVar.ktpvDefault, wsAnal);
					}
				}
				if (cstr > 0)
				{
					tisb.Append("]");
					ITsString tss = tisb.GetString();
					return tss;
				}
				// Nothing there, return an empty string in the analysis writing system.
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", m_cache.DefaultAnalWs);
			}
		}
		#endregion Properties

		/// <summary>
		/// Compute (or recompute) the sense the reference the one reversal entry id.
		/// </summary>
		/// <param name="cache">Regular FDO cache</param>
		/// <param name="entryId">One reversal entry ID to work with</param>
		public static void ResetReferringSenses(FdoCache cache, int entryId)
		{
			List<int> entriesNeedingPropChangeBackRef = new List<int>(1);
			entriesNeedingPropChangeBackRef.Add(entryId);
			ResetReferringSenses(cache, entriesNeedingPropChangeBackRef);
		}

		/// <summary>
		/// Compute (or recompute) the sense the reference each reversal entry id in entryIds.
		/// </summary>
		/// <param name="cache">Regular FDO cache</param>
		/// <param name="entryIds">List of reversal entry IDs to work with</param>
		public static void ResetReferringSenses(FdoCache cache, List<int> entryIds)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			IVwVirtualHandler vhReferringSenses = BaseVirtualHandler.GetInstalledHandler(cache,
				"ReversalIndexEntry", "ReferringSenses");
			Debug.Assert(vhReferringSenses != null,
				"The virtual handler for 'ReversalIndexEntry-ReferringSenses' has to be created at application startup now.");
			int flidReferringSenses = vhReferringSenses.Tag;
			foreach (int hvoRe in entryIds)
			{
				int chvoRefSenseOld = 0;
				if (sda.get_IsPropInCache(hvoRe, flidReferringSenses, (int) CellarModuleDefns.kcptReferenceCollection, 0))
				{
					chvoRefSenseOld = sda.get_VecSize(hvoRe, flidReferringSenses);
				}
				vhReferringSenses.Load(hvoRe, flidReferringSenses, 0, cache.VwCacheDaAccessor);
				int chvoRefSenseNew = sda.get_VecSize(hvoRe, flidReferringSenses);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoRe, flidReferringSenses,
					0, chvoRefSenseNew, chvoRefSenseOld);
			}
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
			MoveIfNeeded(objSrc as IReversalIndexEntry);

			base.MergeObject(objSrc, fLoseNoStringData);
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
	}


	#endregion Reversal

	#region Phonology

	/// <summary>
	///
	/// </summary>
	public partial class PhTerminalUnit
	{
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)PhTerminalUnit.PhTerminalUnitTags.kflidCodes);
		}

		/// <summary>
		/// Initialize a new instance of PhPhoneme with a PhCode.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			PhCode pc = new PhCode();
			CodesOS.Append(pc);
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
				if(Name != null)
				{
					return Name.BestVernacularAlternative;
				}
				else if (CodesOS.Count > 0)
				{
					return CodesOS[0].ShortNameTSS;
				}
				else
				{
					int ws = m_cache.DefaultUserWs;
					string name = Strings.ksQuestions;		// was "??", not "???"
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					return tsf.MakeString(name, ws);
				}
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class PhPhoneme
	{
		/// <summary>
		/// file name for the BasicIPASymbol mapper file
		/// </summary>
		public const string ksBasicIPAInfoFile = "BasicIPAInfo.xml";
		/// <summary>
		/// Get the representations of all of the PhPhonemes in the database.
		/// </summary>
		/// <param name="cache">The connection to a database.</param>
		/// <returns>A string array of all the representations of phonemes.</returns>
		public static string[] PhonemeRepresentations(FdoCache cache)
		{
			// The SQL command must NOT modify the database contents!
			string qry = String.Format("Select rep.Txt "
				+ "from PhPhoneme pn "
				+ "join PhTerminalUnit_Codes tuc "
				+  "on tuc.Src=pn.Id "
				+ "join PhCode_Representation rep "
				+ "on rep.Obj=tuc.Dst and rep.Ws={0}",
				cache.LangProject.DefaultVernacularWritingSystem);
			string[] strings = DbOps.ReadMultiUnicodeTxtStrings(cache, qry);
			return strings;
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
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeletePhoneme, " "));
				tisb.AppendTsString(ShortNameTSS);

				List<LinkedObjectInfo> linkedObjects = LinkedObjects;
				int naturalClassCount = 0;
				Set<int> rules = new Set<int>();
				foreach (LinkedObjectInfo loi in linkedObjects)
				{
					switch (loi.RelObjClass)
					{
						case PhNCSegments.kclsidPhNCSegments:
						{
							++naturalClassCount;
							break;
						}
						case PhSimpleContextSeg.kclsidPhSimpleContextSeg:
						{
							IPhSimpleContextSeg ctxt = new PhSimpleContextSeg(m_cache, loi.RelObjId);
							rules.Add(ctxt.Rule);
							break;
						}
					}
				}
				int cnt = 1;
				string warningMsg = String.Format("\x2028\x2028{0}", Strings.ksPhonemeUsedHere);
				bool wantMainWarningLine = true;
				if (naturalClassCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (naturalClassCount > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesInNatClasses, cnt++, naturalClassCount));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceInNatClasses, cnt++));
					wantMainWarningLine = false;
				}

				if (rules.Count > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (rules.Count > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesInRules, cnt, rules.Count));
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
				case (int)PhPhoneme.PhPhonemeTags.kflidFeatures:
					return m_cache.LangProject.PhFeatureSystemOA;
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
				case (int)PhPhoneme.PhPhonemeTags.kflidFeatures:
					set = new Set<int>();
					set.AddRange(m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.HvoArray);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}
		/// <summary>
		/// Set description based on the content of the BasicIPASymbol field and the BasicIPAInfo document
		/// </summary>
		/// <param name="doc">XmlDocument containing the BasicIPAInfo</param>
		/// <param name="fJustChangedDescription"></param>
		/// <returns><c>true</c> if the description was changed; <c>false</c> otherwise</returns>
		public bool SetDescriptionBasedOnIPA(XmlDocument doc, bool fJustChangedDescription)
		{
			if (!CanAddDescription(fJustChangedDescription))
				return false;
			bool fADescriptionChanged = false;
			foreach (ILgWritingSystem writingSystem in m_cache.LangProject.AnalysisWssRC)
			{
				int ws = writingSystem.Hvo;
				TsStringAccessor accessor = Description.GetAlternative(ws);
				if (accessor == null)
					continue;
				string sDesc = accessor.Text;
				if (string.IsNullOrEmpty(sDesc) || fJustChangedDescription)
				{
					string sLocale = writingSystem.ICULocale;
					string sXPath = "//SegmentDefinition[Representations/Representation[.='" +
									BasicIPASymbol.Text +
									"']]/Descriptions/Description[@lang='" + sLocale + "']";
					XmlNode description = doc.SelectSingleNode(sXPath);
					if (description != null)
					{
						Description.GetAlternative(ws).Text = description.InnerText;
						fADescriptionChanged = true;
					}
					if (string.IsNullOrEmpty(BasicIPASymbol.Text))
					{
						Description.GetAlternative(ws).Text = "";
						fADescriptionChanged = true;
					}
				}
			}
			return fADescriptionChanged;
		}

		private bool CanAddDescription(bool fJustChangedDescription)
		{
			if (!fJustChangedDescription && BasicIPASymbol.Length == 0)
				return false;
			foreach (ILgWritingSystem writingSystem in m_cache.LangProject.AnalysisWssRC)
			{
				int ws = writingSystem.Hvo;
				TsStringAccessor accessor = Description.GetAlternative(ws);
				if (accessor == null)
					continue;
				string sDesc = accessor.Text;
				if (!string.IsNullOrEmpty(sDesc) && !fJustChangedDescription)
					return false;  // some description has content and we did not just put it there
			}
			return true;
		}

		/// <summary>
		/// Set description based on the content of the BasicIPASymbol field and the BasicIPAInfo document
		/// </summary>
		/// <param name="doc">XmlDocument containing the BasicIPAInfo</param>
		/// <param name="fJustChangedFeatures"></param>
		/// <returns><c>true</c> if the description was changed; <c>false</c> otherwise</returns>
		public bool SetFeaturesBasedOnIPA(XmlDocument doc, bool fJustChangedFeatures)
		{
			int tagLongName = Cache.VwCacheDaAccessor.GetVirtualHandlerName("FsFeatStruc", "LongNameTSS").Tag;
			int longNameOldLen = 0;
			if (FeaturesOA != null && FeaturesOA.LongName != null)
				longNameOldLen = FeaturesOA.LongName.Length;
			if (CanAddFeatures(fJustChangedFeatures))
			{
				string sXPath = "//SegmentDefinition[Representations/Representation[.='" + BasicIPASymbol.Text +
								"']]/Features";
				XmlNode features = doc.SelectSingleNode(sXPath);
				if (features != null)
				{
					bool fCreatedNewFS = false;
					foreach (XmlNode feature in features)
					{
						if (feature.Name != "FeatureValuePair")
							continue;
						string sFeature = XmlUtils.GetAttributeValue(feature, "feature");
						string sValue = XmlUtils.GetAttributeValue(feature, "value");
						int hvoFeature = FsFeatureSystem.GetClosedFeature(m_cache, sFeature);
						if (hvoFeature == 0)
							continue;
						int hvoValue = FsFeatureSystem.GetSymbolicValue(m_cache, sValue);
						if (hvoValue == 0)
							continue;
						if (FeaturesOA == null)
						{
							FeaturesOA = new FsFeatStruc();
							fCreatedNewFS = true;
						}
						FsClosedValue value = new FsClosedValue();
						FeaturesOA.FeatureSpecsOC.Add(value);
						value.FeatureRAHvo = hvoFeature;
						value.ValueRAHvo = hvoValue;
					}
					bool madeFeatures = false;
					if (FeaturesOA != null && FeaturesOA.FeatureSpecsOC.Count > 0)
					{
						// notify interested parties of the change
						if (fCreatedNewFS)
						{
							Cache.PropChanged(null, PropChangeType.kpctNotifyAll, Hvo,
											  (int) PhPhonemeTags.kflidFeatures,
											  0, 1, 0);
						}
						Cache.PropChanged(null, PropChangeType.kpctNotifyAll, FeaturesOA.Hvo,
										  (int)FsFeatStruc.FsFeatStrucTags.kflidFeatureSpecs,
										  0, FeaturesOA.FeatureSpecsOC.Count, 0);
						FeaturesOA.UpdateFeatureLongName(tagLongName, longNameOldLen);
						madeFeatures = true;
					}
					return madeFeatures;
				}
			}
			else if (BasicIPASymbol.Length == 0 && fJustChangedFeatures)
			{
				if (FeaturesOA != null)
				{
					// user has cleared the basic IPA symbol; clear the features
					int iCount = FeaturesOA.FeatureSpecsOC.Count;
					FeaturesOA.FeatureSpecsOC.RemoveAll();
					// notify interested parties of the change
					Cache.PropChanged(null, PropChangeType.kpctNotifyAll, FeaturesOA.Hvo,
									  (int) FsFeatStruc.FsFeatStrucTags.kflidFeatureSpecs,
									  0, 0, iCount);
					FeaturesOA.UpdateFeatureLongName(tagLongName, longNameOldLen);
				}
				return true;
			}
			return false;
		}

		private bool CanAddFeatures(bool fJustChangedFeatures)
		{
			if (BasicIPASymbol.Length == 0)
				return false;
			if (FeaturesOAHvo == 0 || FeaturesOA.FeatureSpecsOC.Count == 0 || fJustChangedFeatures)
				return true;
			return false;
		}

		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			List<LinkedObjectInfo> linkedObjects = LinkedObjects;
			foreach (LinkedObjectInfo loi in linkedObjects)
			{
				if (loi.RelObjClass == PhSimpleContextSeg.kclsidPhSimpleContextSeg)
				{
					IPhSimpleContextSeg ctxt = new PhSimpleContextSeg(m_cache, loi.RelObjId);
					ctxt.DeleteUnderlyingObject();
				}
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class PhCode
	{

		/// <summary>
		///
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeletePhRepresentation, " "));
				tisb.AppendTsString(ShortNameTSS);

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
		/// Gets a TsString that represents the shortname of a Text.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				int ws = m_cache.DefaultUserWs;
				string rep = Strings.ksQuestions;		// was "??", not "???"

				if(Representation != null
					&& Representation.VernacularDefaultWritingSystem != null
					&& Representation.VernacularDefaultWritingSystem != String.Empty)
				{
					ws = m_cache.DefaultAnalWs;
					rep = Representation.BestVernacularAlternative.Text;
				}
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(rep, ws);
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
	}

	/// <summary>
	/// Add special behavior.
	/// </summary>
	public partial class PhNaturalClass
	{
		/// <summary>
		/// Delete the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// remove all associated contexts
			List<LinkedObjectInfo> linkedObjects = LinkedObjects;
			foreach (LinkedObjectInfo loi in linkedObjects)
			{
				if (loi.RelObjClass == PhSimpleContextNC.kclsidPhSimpleContextNC)
				{
					IPhSimpleContextNC ctxt = new PhSimpleContextNC(m_cache, loi.RelObjId);
					ctxt.DeleteUnderlyingObject();
				}
			}

			string qry = "select ID from PhEnvironment where StringRepresentation LIKE ?";
			string param = string.Format("%[[]{0}]%", Abbreviation.AnalysisDefaultWritingSystem);
			List<int> phoneEnvIDs = DbOps.ReadIntsFromCommand(m_cache, qry, param);
			param = string.Format("%[[]{0}^%", Abbreviation.AnalysisDefaultWritingSystem);
			phoneEnvIDs.AddRange(DbOps.ReadIntsFromCommand(m_cache, qry, param));

			FdoCache cache = m_cache;
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);

			foreach (int id in phoneEnvIDs)
			{
				ConstraintFailure failure;
				PhEnvironment env = (PhEnvironment)PhEnvironment.CreateFromDBObject(cache, id);
				env.CheckConstraints((int)PhEnvironment.PhEnvironmentTags.kflidStringRepresentation, out failure, /* adjust the squiggly line */ true);
			}
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of a natural class.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				int ws = m_cache.DefaultUserWs;
				string shortName = Strings.ksQuestions;		// was "??", not "???"

				if(Name != null)
				{
					if (Name.AnalysisDefaultWritingSystem != null)
					{
						ws = m_cache.DefaultAnalWs;
						shortName = Name.AnalysisDefaultWritingSystem;
					}
					else if (Name.VernacularDefaultWritingSystem != null)
					{
						ws = m_cache.DefaultVernWs;
						shortName = Name.VernacularDefaultWritingSystem;
					}
				}
				if (shortName == Strings.ksQuestions && Abbreviation != null)	// was "??", not "???"
				{
					if (Abbreviation.AnalysisDefaultWritingSystem != null)
					{
						ws = m_cache.DefaultAnalWs;
						shortName = Abbreviation.AnalysisDefaultWritingSystem;
					}
					else if (Abbreviation.VernacularDefaultWritingSystem != null)
					{
						ws = m_cache.DefaultVernWs;
						shortName = Abbreviation.VernacularDefaultWritingSystem;
					}
				}
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(shortName, ws);
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
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteNaturalClass, " "));
				tisb.AppendTsString(ShortNameTSS);

				List<LinkedObjectInfo> linkedObjs = LinkedObjects;

				Set<int> rules = new Set<int>();
				foreach (LinkedObjectInfo loi in linkedObjs)
				{
					if (loi.RelObjClass == PhSimpleContextNC.kclsidPhSimpleContextNC)
					{
						IPhSimpleContextNC ctxt = new PhSimpleContextNC(m_cache, loi.RelObjId);
						// if there are two natural classes with the same abbreviation, things can get in a state where
						// there is no rule here.
						int iRule = ctxt.Rule;
						if (iRule != 0)
							rules.Add(iRule);
					}
				}

				int cnt = 1;
				string warningMsg = String.Format("\x2028\x2028{0}", Strings.ksNatClassUsedHere);
				if (rules.Count > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (rules.Count > 1)
						tisb.Append(String.Format(Strings.ksUsedXTimesInRules, cnt, rules.Count));
					else
						tisb.Append(String.Format(Strings.ksUsedOnceInRules, cnt));
				}

				string qry = "select count(*) from PhEnvironment where StringRepresentation LIKE ?";
				string param = "%[[]" + Abbreviation.AnalysisDefaultWritingSystem + "]%";
				int cntStandard;
				DbOps.ReadOneIntFromCommand(m_cache, qry, param, out cntStandard);
				int cntIndexed;
				param = "%[[]" + Abbreviation.AnalysisDefaultWritingSystem + "^%";
				DbOps.ReadOneIntFromCommand(m_cache, qry, param, out cntIndexed);
				int totalCount = cntStandard + cntIndexed;
				if ((totalCount) > 0)
				{
					tisb.Append("\x2028\x2028");
					string sMsg;
					if (totalCount > 1)
						sMsg = String.Format(Strings.ksInvalidateXEnvsIfDelNatClass, totalCount, "\x2028");
					else
						sMsg = String.Format(Strings.ksInvalidateOneEnvIfDelNatClass, "\x2028");
					tisb.Append(sMsg);
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Get the abbreviations of all of the PhNaturalClasses in the database.
		/// </summary>
		/// <param name="cache">The connection to a database.</param>
		/// <returns>A string array of the class names.</returns>
		public static string[] ClassAbbreviations(FdoCache cache)
		{
			// The SQL command must NOT modify the database contents!
			string qry = String.Format("Select ncn.Txt "
				+ "from PhNaturalClass nc "
				+ "join PhNaturalClass_Abbreviation ncn "
				+	"on ncn.Obj=nc.Id and ncn.Ws={0}", cache.LangProject.DefaultAnalysisWritingSystem);
			string[] strings = GetStringsFromQuery(cache, qry);
			return strings;
		}

		static private string[] GetStringsFromQuery(FdoCache cache, string qry)
		{
			StringCollection col = new StringCollection();
			IOleDbCommand odc = null;
			cache.DatabaseAccessor.CreateCommand(out odc);
			try
			{
				uint cbSpaceTaken;
				bool fMoreRows;
				bool fIsNull;
				odc.ExecCommand(qry, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				odc.NextRow(out fMoreRows);
				while (fMoreRows)
				{
					using (ArrayPtr prgchName = MarshalEx.ArrayToNative(4000, typeof(uint)))
					{
						odc.GetColValue(1, prgchName, prgchName.Size, out cbSpaceTaken, out fIsNull, 0);
						byte[] rgbTemp = (byte[])MarshalEx.NativeToArray(prgchName, (int)cbSpaceTaken, typeof(byte));
						col.Add(Encoding.Unicode.GetString(rgbTemp));
					}
					odc.NextRow(out fMoreRows);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			string[] strings = new string[col.Count];
			for(int i = 0; i < col.Count; ++i)
				strings[i] = col[i];
			return strings;
		}
		/// <summary>
		/// Get the names of all of the PhNaturalClasses in the database.
		/// </summary>
		/// <param name="cache">The connection to a database.</param>
		/// <returns>A string array of the class names.</returns>
		public static string[] ClassNames(FdoCache cache)
		{
			string qry = String.Format("Select ncn.Txt "
				+ "from PhNaturalClass nc "
				+ "join PhNaturalClass_Name ncn "
				+	"on ncn.Obj=nc.Id and ncn.Ws={0}", cache.LangProject.DefaultAnalysisWritingSystem);
			string[] strings = GetStringsFromQuery(cache, qry);
			return strings;
		}
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)PhNaturalClass.PhNaturalClassTags.kflidName);
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class PhNCSegments
	{
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)PhNCSegments.PhNCSegmentsTags.kflidSegments:
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
				case (int)PhNCSegments.PhNCSegmentsTags.kflidSegments:
					set = new Set<int>();
					foreach(IPhPhonemeSet ps in m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS)
						set.AddRange(ps.PhonemesOC.HvoArray);
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
			return (flid == (int)PhNCSegments.PhNCSegmentsTags.kflidSegments)
				|| base.IsFieldRequired(flid);
		}
	}

	/// <summary>
	/// Add special behavior.
	/// </summary>
	public partial class PhEnvironment
	{
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
				if (StringRepresentation.Text == null)
				{
					ITsString tss;
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					tss = tsf.MakeString("/_", m_cache.DefaultVernWs);
					StringRepresentation.UnderlyingTsString = tss;
				}
				return StringRepresentation.UnderlyingTsString;
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
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeletePhEnvironment, " "));
				tisb.AppendTsString(ShortNameTSS);

				List<LinkedObjectInfo> linkedObjects = LinkedObjects;
				int userCount = 0;
				foreach (LinkedObjectInfo loi in linkedObjects)
				{
					if (loi.RelObjClass == MoAffixAllomorph.kclsidMoAffixAllomorph
						|| loi.RelObjClass == MoStemAllomorph.kclsidMoStemAllomorph)
						++userCount;
				}
				if (userCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptUnderline,
						(int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntNone);
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append("\x2028\x2028");
					if (userCount > 1)
						tisb.Append(String.Format(Strings.ksEnvUsedXTimesByAllos, userCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksEnvUsedOnceByAllos, "\x2028"));
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// Side effects of deleting the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Delete the annotations that refer to objects in this text.
			List<LinkedObjectInfo> linkedObjs = LinkedObjects;
			foreach (LinkedObjectInfo loi in linkedObjs)
			{
				// Delete problem annotations.
				if (loi.RelObjClass == CmBaseAnnotation.kClassId)
					m_cache.DeleteObject(loi.RelObjId);
				else
				{
					if (loi.RelObjClass == MoStemAllomorph.kclsidMoStemAllomorph)
					{
						IMoStemAllomorph stemAllo = MoStemAllomorph.CreateFromDBObject(m_cache, loi.RelObjId);
						stemAllo.PhoneEnvRC.Remove(this);
					}
					else if (loi.RelObjClass == MoAffixAllomorph.kclsidMoAffixAllomorph)
					{
						IMoAffixAllomorph afxAllo = MoAffixAllomorph.CreateFromDBObject(m_cache, loi.RelObjId);
						if (loi.RelObjField == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPhoneEnv)
							afxAllo.PhoneEnvRC.Remove(this);
						if (loi.RelObjField == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition)
							afxAllo.PositionRS.Remove(this);
					}
				}
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Check the validity of the environemtn string, create a problem report, and
		/// if asked, adjust the string itself to show the validity
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <param name="fAdjustSquiggly">whether or not to adjust the string squiggly line</param>
		/// <returns>true, if StringRepresentation is valid, otherwise false.</returns>
		public bool CheckConstraints(int flidToCheck, out ConstraintFailure failure, bool fAdjustSquiggly)
		{
			failure = null;
			bool isValid = true;
			if (flidToCheck == 0
				|| flidToCheck == (int)PhEnvironment.PhEnvironmentTags.kflidStringRepresentation)
			{
				CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_cache, Hvo);
				PhonEnvRecognizer rec = new PhonEnvRecognizer(PhPhoneme.PhonemeRepresentations(m_cache), PhNaturalClass.ClassAbbreviations(m_cache));
				TsStringAccessor strAcc = StringRepresentation;
				ITsString tss = strAcc.UnderlyingTsString;
				ITsStrBldr bldr = tss.GetBldr();
				string strRep = tss.Text;
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
					CreateErrorMessageFromXml(strRep, rec.ErrorMessage, out pos, out sMessage);

					failure = new ConstraintFailure(this, (int)PhEnvironment.PhEnvironmentTags.kflidStringRepresentation,
						sMessage);
					failure.XmlDescription = rec.ErrorMessage;
					if (fAdjustSquiggly)
					{
						// MakeSquigglyLine

						Color col = Color.Red;
						int len = tss.Length;
						bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderline,
							(int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntSquiggle);
						bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderColor,
							(int)FwTextPropVar.ktpvDefault, col.R + (col.B * 256 + col.G) * 256);
					}
					isValid = false;
				}
				if (fAdjustSquiggly)
					strAcc.UnderlyingTsString = bldr.GetString();
			}
			return isValid;
		}


		/// <summary>
		/// Override the inherited method to check the StringRepresentation property.
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>true, if StringRepresentation is valid, otherwise false.</returns>
		public override bool CheckConstraints(int flidToCheck, out ConstraintFailure failure)
		{
			return CheckConstraints(flidToCheck, out failure, /* do not adjust squiggly line */ false);
		}

		/// <summary>
		/// Convert XML message returned from environ validator to English
		/// </summary>
		/// <param name="strRep1">The environment string itself</param>
		/// <param name="sXmlMessage">XML returned from validator</param>
		/// <param name="pos">position value</param>
		/// <param name="sMessage">The created message</param>
		public static void CreateErrorMessageFromXml(string strRep1, string sXmlMessage, out int pos, out string sMessage)
		{
			string strRep = strRep1;
			if (strRep1 == null)
				strRep = "";
			XmlDocument xdoc = new XmlDocument();
			string sStatus = "";
			pos = 0;
			try
			{
				// The validator message, unfortunately, may be invalid XML if
				// there were XML reserved characters in the environment.
				// until we get that fixed, at least don't crash, just draw squiggly under the entire word
				xdoc.LoadXml(sXmlMessage);
				XmlAttribute posAttr = xdoc.DocumentElement.Attributes["pos"];
				pos = (posAttr != null) ? Convert.ToInt32(posAttr.Value) : 0;
				XmlAttribute statusAttr = xdoc.DocumentElement.Attributes["status"];
				sStatus = statusAttr.InnerText;
			}
			catch
			{
				// Eat the exception.
			}
			int len = strRep.Length;
			if (pos >= len)
				pos = Math.Max(0, len - 1); // make sure something will show
			//todo: if the string itself will be part of this message, this needs
			// to put the right places in the right writing systems. note that
			//there is a different constructor we can use which takes a sttext.
			StringBuilder bldrMsg = new StringBuilder();
			bldrMsg.AppendFormat(Strings.ksBadEnv, strRep);
			if (sStatus == "class")
			{
				int iRightBracket = strRep.Substring(pos).IndexOf(']');
				string sClass = strRep.Substring(pos, iRightBracket);
				bldrMsg.AppendFormat(Strings.ksBadClassInEnv, sClass);
			}
			if (sStatus == "segment")
			{
				string sPhoneme = strRep.Substring(pos);
				bldrMsg.AppendFormat(Strings.ksBadPhonemeInEnv, sPhoneme);
			}
			if (sStatus == "missingClosingParen")
			{
				bldrMsg.AppendFormat(Strings.ksMissingCloseParenInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingOpeningParen")
			{
				bldrMsg.AppendFormat(Strings.ksMissingOpenParenInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingClosingSquareBracket")
			{
				bldrMsg.AppendFormat(Strings.ksMissingCloseBracketInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingOpeningSquareBracket")
			{
				bldrMsg.AppendFormat(Strings.ksMissingOpenBracketInEnv, strRep.Substring(pos));
			}
			if (sStatus == "syntax")
			{
				bldrMsg.AppendFormat(Strings.ksBadEnvSyntax, strRep.Substring(pos));
			}
			sMessage = bldrMsg.ToString();
		}

		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>true, if the field is required.</returns>
		public override bool IsFieldRequired(int flid)
		{
			return (flid == (int)PhEnvironment.PhEnvironmentTags.kflidStringRepresentation); // N.B. is for Stage 1 only
		}

		/// <summary>
		/// Gets a set of ids of environments that are valid.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns>Set of ids of environments that are valid.</returns>
		public static Set<int> ValidEnvironments(FdoCache cache)
		{
			Set<int> set = new Set<int>(cache.LangProject.PhonologicalDataOA.EnvironmentsOS.HvoArray);
			// Remove any that have problem annotations.
			string sql = "SELECT env.Id "
				+ "FROM CmBaseAnnotation_ ann "
				+ "JOIN PhEnvironment env ON ann.BeginObject = env.Id";
			foreach (int id in DbOps.ReadIntsFromCommand(cache, sql, null))
				set.Remove(id);
			return set;
		}

		/// <summary>
		/// Get the default infix environment (/#[C]_) from list of environments.
		/// </summary>
		/// <param name="cache">the cache to use</param>
		/// <param name="sDefaultEnv">string representation of the default environment</param>
		/// <returns>hvo of default environment.</returns>
		public static int DefaultInfixEnvironment(FdoCache cache, string sDefaultEnv)
		{
			foreach (IPhEnvironment env in cache.LangProject.PhonologicalDataOA.EnvironmentsOS)
			{
				string sEnv = env.StringRepresentation.Text.Trim();
				string sEnvNoWhitespace = StringUtils.StripWhitespace(sEnv);
				if (sEnvNoWhitespace == sDefaultEnv)
					return env.Hvo;
			}
			return 0;
		}
		/// <summary>
		/// Insert "()" into the rootbox at the current selection, then back up the selection
		/// to be between the parentheses.
		/// </summary>
		/// <param name="rootb"></param>
		public static void InsertOptionalItem(IVwRootBox rootb)
		{
			rootb.OnChar((int)'(');
			rootb.OnChar((int)')');
			// Adjust the selection to be between the parentheses.
			IVwSelection vwsel = rootb.Selection;
			int cvsli = vwsel.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);
			Debug.Assert(ichAnchor == ichEnd);
			Debug.Assert(ichAnchor > 0);
			--ichEnd;
			--ichAnchor;
			rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp, cpropPrevious,
				ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, ttp, true);
		}

		/// <summary>
		/// Determine whether an optional item or a natural class can be inserted at the
		/// location given by ichEnd and ichAnchor.
		/// </summary>
		/// <param name="sEnv"></param>
		/// <param name="ichEnd"></param>
		/// <param name="ichAnchor"></param>
		/// <returns></returns>
		public static bool CanInsertItem(string sEnv, int ichEnd, int ichAnchor)
		{
			Debug.Assert(sEnv != null);
			Debug.Assert(ichEnd <= sEnv.Length && ichAnchor <= sEnv.Length);
			if (ichEnd < 0 || ichAnchor < 0)
				return false;
			int ichSlash = sEnv.IndexOf('/');
			if ((ichSlash < 0) || (ichEnd <= ichSlash) || (ichAnchor <= ichSlash))
				return false;
			// ensure that ichAnchor <= ichEnd.
			int ichT = ichAnchor;
			ichAnchor = Math.Min(ichAnchor, ichEnd);
			ichEnd = Math.Max(ichT, ichEnd);
			int ichHash = sEnv.IndexOf('#');
			if (ichHash < 0)
				return true;
			int ichHash2 = sEnv.IndexOf('#', ichHash + 1);
			if (ichHash2 >= 0)
			{
				// With 2 #, must be between them, or straddling at least one of them.
				if (ichAnchor <= ichHash)
					return (ichEnd > ichHash);
				else if (ichEnd > ichHash2)
					return (ichAnchor <= ichHash2);
				else
					return true;
			}
			else
			{
				// With 1 #, must be on same side as the _, or straddling the #.
				int ichBar = sEnv.IndexOf('_');
				if (ichBar < 0)
					return true;
				if (ichBar < ichHash)
					return (ichAnchor <= ichHash);
				else
					return (ichEnd > ichHash);
			}
		}

		/// <summary>
		/// Determine whether a hash mark (#) can be inserted at the location given by ichEnd
		/// and ichAnchor.
		/// </summary>
		/// <param name="sEnv"></param>
		/// <param name="ichEnd"></param>
		/// <param name="ichAnchor"></param>
		/// <returns></returns>
		public static bool CanInsertHashMark(string sEnv, int ichEnd, int ichAnchor)
		{
			Debug.Assert(sEnv != null);
			Debug.Assert(ichEnd <= sEnv.Length && ichAnchor <= sEnv.Length);
			if (ichEnd < 0 || ichAnchor < 0)
				return false;
			int ichSlash = sEnv.IndexOf('/');
			if ((ichSlash < 0) || (ichEnd <= ichSlash) || (ichAnchor <= ichSlash))
				return false;
			// ensure that ichAnchor <= ichEnd.
			int ichT = ichAnchor;
			ichAnchor = Math.Min(ichAnchor, ichEnd);
			ichEnd = Math.Max(ichT, ichEnd);
			// Check whether ichAnchor is at the beginning of the environment (after the /).
			bool fBegin = CheckForOnlyWhiteSpace(sEnv, ichSlash + 1, ichAnchor);
			// Check whether ichEnd is at the end of the environment.
			bool fEnd = CheckForOnlyWhiteSpace(sEnv, ichEnd, sEnv.Length);
			if (!fBegin && !fEnd)
				return false;	// we must be at the beginning or end!
			int ichHash = sEnv.IndexOf('#');
			if (ichHash >= 0)
			{
				// At least one # exists, look for another.
				int ichHash2 = sEnv.IndexOf('#', ichHash + 1);
				if (ichHash2 < 0)
				{
					// Only 1 # exists, check we're on the opposite side of the _.
					int ichBar = sEnv.IndexOf('_');
					if (ichBar < 0)
					{
						// No _, so we have to analyze the position of the existing #.
						// If we have an illegal #, don't allow the new # unless the old one is
						// being replaced.
						bool fBeginningHash = CheckForOnlyWhiteSpace(sEnv, ichSlash + 1, ichHash);
						bool fEndingHash = CheckForOnlyWhiteSpace(sEnv, ichHash + 1, sEnv.Length);
						if (fBeginningHash)
							return fEnd;
						else if (fEndingHash)
							return fBegin;
						else
							return (ichAnchor <= ichHash) && (ichEnd > ichHash);
					}
					else if (ichBar < ichHash)
					{
						return fBegin;
					}
					else
					{
						return fEnd;
					}
				}
				else
				{
					// Only 2 # may ever exist!
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		private static bool CheckForOnlyWhiteSpace(string sEnv, int ichFirst, int ichLast)
		{
			int cch = ichLast - ichFirst;
			if (cch > 0)
			{
				char[] rgch = sEnv.ToCharArray(ichFirst, cch);
				for (int ich = 0; ich < rgch.Length; ++ich)
				{
					if (!System.Char.IsWhiteSpace(rgch[ich]))
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	public partial class PhSegmentRule
	{
		/// <summary>
		/// Gets or sets the order number.
		/// </summary>
		/// <value>The order number.</value>
		public int OrderNumber
		{
			get
			{
				return IndexInOwner + 1;
			}

			set
			{
				int index = value - 1;
				if (index < 0 || index >= m_cache.GetVectorSize(OwnerHVO, OwningFlid))
					throw new ArgumentOutOfRangeException();

				if (IndexInOwner < index)
					index++;

				m_cache.MoveOwningSequence(OwnerHVO, OwningFlid, IndexInOwner, IndexInOwner, OwnerHVO, OwningFlid,
					index);
			}
		}
	}

	public partial class PhRegularRule
	{
		/// <summary>
		/// Gets all of the feature constraints in this rule.
		/// </summary>
		/// <value>The feature constraints.</value>
		public List<int> FeatureConstraints
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
		public List<int> GetFeatureConstraintsExcept(IPhSimpleContextNC excludeCtxt)
		{
			List<int> featureConstrs = new List<int>();
			CollectVars(StrucDescOS, featureConstrs, excludeCtxt);
			foreach (IPhSegRuleRHS rhs in RightHandSidesOS)
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
		void CollectVars(FdoSequence<IPhSimpleContext> seq, List<int> featureConstrs, IPhSimpleContextNC excludeCtxt)
		{
			foreach (IPhSimpleContext ctxt in seq)
			{
				if ((excludeCtxt == null || ctxt.Hvo != excludeCtxt.Hvo)
					&& ctxt.ClassID == PhSimpleContextNC.kclsidPhSimpleContextNC)
				{
					IPhSimpleContextNC ncCtxt = ctxt as IPhSimpleContextNC;
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
		void CollectVars(IPhPhonContext ctxt, List<int> featureConstrs, IPhSimpleContextNC excludeCtxt)
		{
			if (ctxt == null || (excludeCtxt != null && ctxt.Hvo == excludeCtxt.Hvo))
				return;

			switch (ctxt.ClassID)
			{
				case PhSequenceContext.kclsidPhSequenceContext:
					IPhSequenceContext seqCtxt = ctxt as IPhSequenceContext;
					foreach (IPhPhonContext cur in seqCtxt.MembersRS)
						CollectVars(cur as IPhSimpleContextNC, featureConstrs, excludeCtxt);
					break;

				case PhIterationContext.kclsidPhIterationContext:
					IPhIterationContext iterCtxt = ctxt as IPhIterationContext;
					CollectVars(iterCtxt.MemberRA, featureConstrs, excludeCtxt);
					break;

				case PhSimpleContextNC.kclsidPhSimpleContextNC:
					IPhSimpleContextNC ncCtxt = ctxt as IPhSimpleContextNC;
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
		void CollectVars(FdoSequence<IPhFeatureConstraint> vars, List<int> featureConstrs)
		{
			foreach (IPhFeatureConstraint var in vars)
			{
				bool found = false;
				foreach (int hvo in featureConstrs)
				{
					if (var.Hvo == hvo)
					{
						found = true;
						break;
					}
				}

				if (!found)
					featureConstrs.Add(var.Hvo);
			}
		}

		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			PhSegRuleRHS rhs = new PhSegRuleRHS();
			RightHandSidesOS.Append(rhs);
		}

		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			objectsToDeleteAlso.AddRange(FeatureConstraints);

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	public partial class PhMetathesisRule
	{
		/// <summary>
		/// Left environment
		/// </summary>
		public const int kidxLeftEnv = 0;
		/// <summary>
		/// Left switch
		/// </summary>
		public const int kidxLeftSwitch = 3;
		/// <summary>
		/// Middle
		/// </summary>
		public const int kidxMiddle = 2;
		/// <summary>
		/// Right switch
		/// </summary>
		public const int kidxRightSwitch = 1;
		/// <summary>
		/// Right environment
		/// </summary>
		public const int kidxRightEnv = 4;

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
			int index = indices[kidxMiddle].IndexOf(':');
			if (index != -1)
			{
				isMiddleWithLeftSwitch = indices[kidxMiddle].Substring(index + 1) == "L";
				indices[kidxMiddle] = indices[kidxMiddle].Substring(0, index);
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
			if (indices[kidxMiddle] != -1)
				middleAssocStr = isMiddleWithLeftSwitch ? ":L" : ":R";

			StrucChange.Text = string.Format("{0} {1} {2}{3} {4} {5}", indices[0], indices[1], indices[2], middleAssocStr,
				indices[3], indices[4]);
		}

		/// <summary>
		/// Updates the <c>StrucChange</c> indices for removal and insertion. Should be called after insertion
		/// to StrucDesc and before removal from StrucDesc.
		/// </summary>
		/// <param name="strucChangeIndex">Index in structural change.</param>
		/// <param name="ctxtIndex">Index of the context.</param>
		/// <param name="insert">indicates whether the context will be inserted or removed.</param>
		/// <returns>HVO of additional context to remove</returns>
		public int UpdateStrucChange(int strucChangeIndex, int ctxtIndex, bool insert)
		{
			int delta = insert ? 1 : -1;

			int removeCtxt = 0;

			bool isMiddleWithLeftSwitch;
			int[] indices = GetStrucChangeIndices(out isMiddleWithLeftSwitch);
			switch (strucChangeIndex)
			{
				case kidxLeftEnv:
					indices[kidxLeftEnv] += delta;
					if (indices[kidxLeftSwitch] != -1)
						indices[kidxLeftSwitch] += delta;
					if (indices[kidxMiddle] != -1)
						indices[kidxMiddle] += delta;
					if (indices[kidxRightSwitch] != -1)
						indices[kidxRightSwitch] += delta;
					if (indices[kidxRightEnv] != -1)
						indices[kidxRightEnv] += delta;
					break;

				case kidxLeftSwitch:
					if (insert)
					{
						if (indices[kidxLeftSwitch] == -1)
						{
							// adding new item to empty left switch cell
							indices[kidxLeftSwitch] = ctxtIndex;
							if (indices[kidxMiddle] != -1)
								indices[kidxMiddle] += delta;
						}
						else
						{
							// already something in the cell, so must be adding a middle context
							indices[kidxMiddle] = ctxtIndex;
							isMiddleWithLeftSwitch = true;
						}
					}
					else
					{
						// removing an item
						if (ctxtIndex == indices[kidxLeftSwitch])
						{
							// removing the left switch context
							indices[kidxLeftSwitch] = -1;
							if (indices[kidxMiddle] != -1)
							{
								if (isMiddleWithLeftSwitch)
								{
									// remove the middle context if it is associated with this cell
									removeCtxt = StrucDescOS[indices[kidxMiddle]].Hvo;
									indices[kidxMiddle] = -1;
									delta -= 1;
								}
								else
								{
									indices[kidxMiddle] += delta;
								}
							}
						}
						else
						{
							// removing the middle context
							indices[kidxMiddle] = -1;
						}
					}

					if (indices[kidxRightSwitch] != -1)
						indices[kidxRightSwitch] += delta;
					if (indices[kidxRightEnv] != -1)
						indices[kidxRightEnv] += delta;
					break;

				case kidxRightSwitch:
					if (insert)
					{
						if (indices[kidxRightSwitch] == -1)
						{
							// adding new item to empty right switch cell
							indices[kidxRightSwitch] = ctxtIndex;
						}
						else
						{
							// already something in the cell, so must be adding a middle context
							indices[kidxMiddle] = ctxtIndex;
							indices[kidxRightSwitch] += delta;
							isMiddleWithLeftSwitch = false;
						}
					}
					else
					{
					   // removing an item
						if (ctxtIndex == indices[kidxRightSwitch])
						{
							// removing the right switch context
							indices[kidxRightSwitch] = -1;
							if (indices[kidxMiddle] != -1 && !isMiddleWithLeftSwitch)
							{
								// remove the middle context if it is associated with this cell
								removeCtxt = StrucDescOS[indices[kidxMiddle]].Hvo;
								indices[kidxMiddle] = -1;
								delta -= 1;
							}
						}
						else
						{
							// removing the middle context
							indices[kidxMiddle] = -1;
							indices[kidxRightSwitch] += delta;
						}
					}

					if (indices[kidxRightEnv] != -1)
						indices[kidxRightEnv] += delta;
					break;

				case kidxRightEnv:
					if (insert && indices[kidxRightEnv] == -1)
						indices[kidxRightEnv] = ctxtIndex;
					else if (!insert && (StrucDescOS.Count - indices[kidxRightEnv]) == 1)
						indices[kidxRightEnv] = -1;
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
				return GetIndex(kidxLeftEnv);
			}

			set
			{
				SetIndex(kidxLeftEnv, value);
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
				return GetIndex(kidxRightEnv);
			}

			set
			{
				SetIndex(kidxRightEnv, value);
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
				return GetIndex(kidxLeftSwitch);
			}

			set
			{
				SetIndex(kidxLeftSwitch, value);
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
				return GetIndex(kidxRightSwitch);
			}

			set
			{
				SetIndex(kidxRightSwitch, value);
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
				return GetIndex(kidxMiddle);
			}

			set
			{
				SetIndex(kidxMiddle, value);
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
					return StrucDescOS.Count;
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
					return StrucDescOS.Count;
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
					return StrucDescOS.Count;
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
		/// <param name="ctxtHvo">The context HVO.</param>
		/// <returns>The structural change index.</returns>
		public int GetStrucChangeIndex(int ctxtHvo)
		{
			int index = m_cache.GetObjIndex(Hvo, (int)PhSegmentRuleTags.kflidStrucDesc, ctxtHvo);

			if (index < LeftEnvLimit)
				return kidxLeftEnv;
			else if (index >= LeftSwitchIndex && index < LeftSwitchLimit)
				return kidxLeftSwitch;
			else if (index >= MiddleIndex && index < MiddleLimit)
				return IsMiddleWithLeftSwitch ? kidxLeftSwitch : kidxRightSwitch;
			else if (index >= RightSwitchIndex && index < RightSwitchLimit)
				return kidxRightSwitch;
			else if (index >= RightEnvIndex && index < RightEnvLimit)
				return kidxRightEnv;
			else
				return -1;
		}

		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			StrucChange.Text = "-1 -1 -1 -1 -1";
		}
	}

	public partial class PhIterationContext
	{
		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			Maximum = -1;
			Minimum = 0;
		}

		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			if (MemberRAHvo != 0)
				objectsToDeleteAlso.Add(MemberRAHvo);

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	public partial class PhSequenceContext
	{
		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			objectsToDeleteAlso.AddRange(MembersRS.HvoArray);

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	public partial class PhContextOrVar
	{
		/// <summary>
		/// Gets the HVO of rule that contains this context.
		/// </summary>
		/// <value>The rule HVO.</value>
		public int Rule
		{
			get
			{
				IPhContextOrVar cur = this;
				while (cur != null)
				{
					switch (m_cache.GetClassOfObject(cur.OwnerHVO))
					{
						case PhPhonData.kclsidPhPhonData:
							List<LinkedObjectInfo> linkedObjs = cur.LinkedObjects;
							cur = null;
							foreach (LinkedObjectInfo loi in linkedObjs)
							{
								if ((loi.RelObjClass == PhSequenceContext.kclsidPhSequenceContext
									&& loi.RelObjField == (int)PhSequenceContext.PhSequenceContextTags.kflidMembers)
									|| (loi.RelObjClass == PhIterationContext.kclsidPhIterationContext
									&& loi.RelObjField == (int)PhIterationContext.PhIterationContextTags.kflidMember))
								{
									cur = PhContextOrVar.CreateFromDBObject(m_cache, loi.RelObjId);
									break;
								}
							}
							break;

						case PhSegRuleRHS.kclsidPhSegRuleRHS:
							return m_cache.GetOwnerOfObject(cur.OwnerHVO);

						default:
							return cur.OwnerHVO;
					}
				}

				return 0;
			}
		}

		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			switch (Owner.ClassID)
			{
				case MoAffixProcess.kclsidMoAffixProcess:
					// if this is owned by a MoAffixProcess we must remove all of the associated output mappings
					IMoAffixProcess rule = Owner as IMoAffixProcess;
					foreach (int mappingHvo in rule.OutputOS.HvoArray)
					{
						switch (m_cache.GetClassOfObject(mappingHvo))
						{
							case MoCopyFromInput.kclsidMoCopyFromInput:
								IMoCopyFromInput copy = new MoCopyFromInput(m_cache, mappingHvo);
								if (copy.ContentRAHvo == Hvo)
									objectsToDeleteAlso.Add(mappingHvo);
								break;

							case MoModifyFromInput.kclsidMoModifyFromInput:
								IMoModifyFromInput modify = new MoModifyFromInput(m_cache, mappingHvo);
								if (modify.ContentRAHvo == Hvo)
									objectsToDeleteAlso.Add(mappingHvo);
								break;
						}
					}
					break;

				case PhPhonData.kclsidPhPhonData:
					List<LinkedObjectInfo> linkedObjs = LinkedObjects;
					foreach (LinkedObjectInfo loi in linkedObjs)
					{
						if (loi.RelObjClass == PhIterationContext.kclsidPhIterationContext
							&& loi.RelObjField == (int)PhIterationContext.PhIterationContextTags.kflidMember)
						{
							IPhIterationContext ctxt = new PhIterationContext(m_cache, loi.RelObjId);
							ctxt.DeleteObjectSideEffects(objectsToDeleteAlso, state);
							objectsToDeleteAlso.Add(loi.RelObjId);
						}
						else if (loi.RelObjClass == PhSequenceContext.kclsidPhSequenceContext
							&& loi.RelObjField == (int)PhSequenceContext.PhSequenceContextTags.kflidMembers
							&& m_cache.GetVectorSize(loi.RelObjId, (int)PhSequenceContext.PhSequenceContextTags.kflidMembers) == 1)
						{
							IPhSequenceContext ctxt = new PhSequenceContext(m_cache, loi.RelObjId);
							ctxt.DeleteObjectSideEffects(objectsToDeleteAlso, state);
							objectsToDeleteAlso.Add(loi.RelObjId);
						}
					}
					break;
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	public partial class PhSimpleContext
	{
		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			if (Owner.ClassID == PhMetathesisRule.kclsidPhMetathesisRule)
			{
				// update the StrucChange field to reflect the removed simple context
				IPhMetathesisRule rule = Owner as IPhMetathesisRule;
				int removeCtxt = rule.UpdateStrucChange(rule.GetStrucChangeIndex(Hvo), IndexInOwner, false);
				if (removeCtxt != 0)
					objectsToDeleteAlso.Add(removeCtxt);
			}

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	public partial class PhSimpleContextNC
	{
		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			int ruleHvo = Rule;
			// if there are two natural classes with the same abbreviation, things can get in a state where
			// there is no rule here.
			if (ruleHvo != 0 && m_cache.GetClassOfObject(ruleHvo) == PhRegularRule.kclsidPhRegularRule)
			{
				IPhRegularRule rule = new PhRegularRule(m_cache, ruleHvo);
				List<int> featConstrs = rule.GetFeatureConstraintsExcept(this);
				foreach (IPhFeatureConstraint constr in PlusConstrRS)
				{
					if (!featConstrs.Contains(constr.Hvo))
						objectsToDeleteAlso.Add(constr.Hvo);
				}
				foreach (IPhFeatureConstraint constr in MinusConstrRS)
				{
					if (!featConstrs.Contains(constr.Hvo))
						objectsToDeleteAlso.Add(constr.Hvo);
				}
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	#endregion // Phonology

	#region WFI

	/// <summary>
	///
	/// </summary>
	public partial class WordformInventory : IDummy
	{
		/// <summary>
		/// Returns an Array of StTexts to use in Concordance.
		/// </summary>
		public List<int> ConcordanceTexts
		{
			get
			{
				return new List<int>(); // load manually
			}
			set
			{
				Cache.VwCacheDaAccessor.CacheVecProp(this.Hvo, ConcordanceTextsFlid(Cache), value.ToArray(), value.Count);
			}
		}

		/// <summary>
		/// Concordance Words virtual property, all the wordforms in ConcordanceTexts.
		/// </summary>
		public List<int> ConcordanceWords
		{
			get
			{
				return new List<int>(Cache.GetVectorProperty(this.Hvo, ConcordanceWordformsFlid(Cache), true));
			}
			set
			{
				Cache.VwCacheDaAccessor.CacheVecProp(this.Hvo, ConcordanceWordformsFlid(Cache), value.ToArray(), value.Count);
			}

		}

		/// <summary>
		/// Flid for ConcordanceWordforms virtual property
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int ConcordanceWordformsFlid(FdoCache cache)
		{
			IVwVirtualHandler vh =  cache.VwCacheDaAccessor.GetVirtualHandlerName("WordformInventory", "ConcordanceWords");
			if (vh == null)
				return 0;
			return vh.Tag;
		}

		/// <summary>
		/// Flid for ConcordanceTexts virtual property
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int ConcordanceTextsFlid(FdoCache cache)
		{
			return BaseVirtualHandler.GetInstalledHandlerTag(cache, "WordformInventory", "ConcordanceTexts");
		}

		/// <summary>
		/// Flid for the MatchingConcordanceItems virtual property.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int MatchingConcordanceItemsFlid(FdoCache cache)
		{
			IVwVirtualHandler vh = cache.VwCacheDaAccessor.GetVirtualHandlerName("WordformInventory",
				"MatchingConcordanceItems");
			if (vh == null)
				return 0;
			return vh.Tag;
		}

		/// <summary>
		/// Implement the MatchingConcordanceItems virtual property.
		/// </summary>
		public List<int> MatchingConcordanceItems
		{
			get
			{
				return new List<int>(Cache.GetVectorProperty(this.Hvo, MatchingConcordanceItemsFlid(Cache), true));
			}
		}

		// These two variables are used to retain as long as is safe the dictioanry that is used
		// to parse texts by quickly finding wordforms. This table is for a particular FdoCache;
		// which one is recorded in the cache variable. If we need this table for another cache,
		// we discard the old one and get a new one.
		// Note: it is tempting to make this table a non-static member variable of the FDO object WordformInventory.
		// However, we want only one instance of this large table per cache! And there can easily
		// be multiple instances of the FDO object for the one database object.
		// Also, to avoid tying up too much memory in these tables, we currently maintain only one,
		// rather than one per database.
		// Enhance: it may be worth making a weak reference here so that the dictionary can be garbage
		// collected if it is not actively being used.
		static Dictionary<string, int> s_formToWfId;
		static FdoCache s_cacheThatFormTableIsBasedOn;
		static bool s_fUpdatingWordformsOC = false;
		bool m_fSuspendUpdatingConcordanceWordforms = false;

		/// <summary>
		/// Implement the UnusedWordforms virtual property.
		/// This property will return the ids of all wordforms that have no text support (no twfics).
		/// </summary>
		public List<int> UnusedWordforms
		{
			get
			{
				List<int> unusedWordforms = new List<int>();

				int cnt = 0;
				foreach (WfiWordform wf in WordformsOC)
				{
					if (cnt == 10)
						break;
					if (wf.CanDelete)
					{
						cnt++;
						unusedWordforms.Add(wf.Hvo);
					}
				}

				return unusedWordforms;
			}
		}

		/// <summary>
		/// Notify WordformInventory that its WordformsOC has been updated, so that it can stay in sync with
		/// its internal caches.
		/// Enhance: we could override WordformsOC so that any Add() or Remove() would do this for us.
		/// </summary>
		public static void OnChangedWordformsOC()
		{
			// if the change didn't come from inside WordformInventory method, invalidate our dependent caches.
			if (!UpdatingWordformsOC)
				InvalidateFormIdTable();
		}

		// Enhance JohnT: try to deal with all uses of this. Just discarding it allows all current dummy
		// wordforms and their dummy Wfics to be memory-leaked. One option is to change from static to
		// a member variable of the cache. This wastes a little memory if we're not looking at concordances
		// in both, but prevents the memory leak AND would allow us to work with concordances in two databases
		// at once if we wish.
		private static void InvalidateFormIdTable()
		{
			s_formToWfId = null;
		}

		/// <summary>
		/// WordformInventory virtual properities that base lists upon GetWordformId() and ConvertDummyToReal
		/// should use this cookie to verify whether their ids (esp. dummy ones) are in a valid state.
		/// Use IsExpiredWordformInventoryCookie() to determine validity.
		/// </summary>
		public object WordformInventoryCookie
		{
			get { return s_formToWfId; }
		}

		/// <summary>
		/// Use with WordformInventoryCookie to make sure the virtual properties populated by FormToIdTable
		/// are still valid.
		/// </summary>
		/// <param name="cookie"></param>
		/// <returns></returns>
		public bool IsExpiredWordformInventoryCookie(object cookie)
		{
			return cookie == null || !cookie.Equals(s_formToWfId);
		}

		/// <summary>
		/// Set to true when adding new wordforms to WordformInventory.WordformsOC to prevent invalidating the FormIdTable.
		/// </summary>
		private static bool UpdatingWordformsOC
		{
			get { return s_fUpdatingWordformsOC; }
			set { s_fUpdatingWordformsOC = value; }
		}

		/// <summary>
		/// Caches information that WordformInventory uses for performance optimization.
		/// </summary>
		public void PreLoadFormIdTable()
		{
			// this should load the table if it's not already loaded.
			Dictionary<string, int> table;
			table = FormToWfIdTable;
		}

		/// <summary>
		/// return the (dummy or database) id for the given wordform (and its ws).
		/// </summary>
		/// <param name="tssForm"></param>
		/// <returns></returns>
		public int GetWordformId(ITsString tssForm)
		{
			return GetWordformId(tssForm, false);
		}

		/// <summary>
		/// return the (dummy or database) id for the given wordform (case sensitive) in the given ws.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public int GetWordformId(string form, int ws)
		{
			if (ws <= 0)
			{
				string msg = String.Format("Expected ws({0}) for form '{1}' to be a real writing system", ws, form);
				Debug.Fail(msg);
				throw new ArgumentException(msg);
			}
			form = CheckFormLength(form, false);
			int hvoWordform = 0;
			if (FormToWfIdTable.TryGetValue(form + ws.ToString(), out hvoWordform))
			{
				if (m_cache.IsValidObject(hvoWordform))
					return hvoWordform;
				else
				{
					// Dictionary contains a value for this word form, but the hvo is out of date.
					// Remove this value from the dictionary. The calling method will need to add
					// a new entry to the form to Wfi Id dictionary.
					FormToWfIdTable.Remove(form + ws.ToString());
				}
			}
			return 0;
		}

		/// <summary>
		/// Lookup the form, and try to match (on its writing system).
		/// </summary>
		/// <param name="tssForm"></param>
		/// <param name="fIncludeLowerCaseForm">if true, match on lower case form, if other case was not found.</param>
		/// <returns></returns>
		public int GetWordformId(ITsString tssForm, bool fIncludeLowerCaseForm)
		{
			return GetWordformId(tssForm.Text, GetFirstWsFromTss(tssForm), fIncludeLowerCaseForm);
		}

		internal static int GetFirstWsFromTss(ITsString tssForm)
		{
			return StringUtils.GetWsAtOffset(tssForm, 0);
		}

		/// <summary>
		/// Lookup the form, and try to match on the specified writing system.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <param name="fIncludeLowerCaseForm">match on lower case form, if other case was not found.</param>
		/// <returns></returns>
		public int GetWordformId(string form, int ws, bool fIncludeLowerCaseForm)
		{
			int wfHvo = GetWordformId(form, ws);
			if (fIncludeLowerCaseForm && wfHvo == 0)
			{
				// try finding a lowercase version.
				IWritingSystem wsEngine = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws);
				if (wsEngine == null)
					return 0;
				string locale = wsEngine.IcuLocale;
				CaseFunctions cf = new CaseFunctions(locale);
				string lcForm = cf.ToLower(form);
				// we only want to look up the lower case form, if the given form was not already lowercased.
				if (lcForm != form)
					wfHvo = GetWordformId(lcForm, ws);
			}
			return wfHvo;
		}

		/// <summary>
		/// Get the table that maps wordform strings to WfiWordform IDs.
		/// Recreate it if we don't have one saved for the right cache.
		/// Note: if we go multithreaded, be careful here!
		/// </summary>
		Dictionary<string, int> FormToWfIdTable
		{
			get
			{
				if (m_cache == s_cacheThatFormTableIsBasedOn && s_formToWfId != null)
					return s_formToWfId;

				s_cacheThatFormTableIsBasedOn = m_cache;
				s_formToWfId = new Dictionary<string, int>();
				if (m_cache.DatabaseAccessor == null)
				{
					// currently test-only strategy, eventually may become primary.
					// optimize: may be able to determine that some WSS in CurVernWssRS are not used for Wordform Forms.
					foreach (ILgWritingSystem lws in m_cache.LangProject.CurVernWssRS)
					{
						int ws = lws.Hvo;
						foreach (WfiWordform wff in WordformsOC)
						{
							ITsString tssForm = wff.Form.GetAlternativeTss(ws);
							NoteWordformForFormAndWs(tssForm, ws, wff.Hvo);
						}
					}
				}
				else
				{
					// Optimally, we would do this all with one sql query, but we don't expect
					// to reload this list very often.
					string sqlDistinctWritingSystems = "select distinct(wff.Ws) from WfiWordform_Form wff";
					List<int> usedWritingSystems = new List<int>();
					foreach (int ws in DbOps.ReadIntsFromCommand(m_cache, sqlDistinctWritingSystems, null))
					{
						WfiWordform.PreLoadShortName(m_cache, ws);
						string sqlWordforms = "select wff.Obj from WfiWordform_Form wff where wff.Ws=?";
						foreach (int hvoWordform in DbOps.ReadIntsFromCommand(m_cache, sqlWordforms, ws))
						{
							ITsString tssForm = Cache.GetMultiStringAlt(hvoWordform, (int) WfiWordform.WfiWordformTags.kflidForm, ws);
							NoteWordformForFormAndWs(tssForm, ws, hvoWordform);
						}
						usedWritingSystems.Add(ws);
					}
				}

				// Then search WordformInventory.ConcordanceWordforms for non-real wordforms, and add them.
				LangProject lp = Cache.LangProject as LangProject;
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				IVwVirtualHandler vh;
				if (Cache.TryGetVirtualHandler(WordformInventory.ConcordanceWordformsFlid(m_cache), out vh) &&
					(vh as BaseVirtualHandler).IsPropInCache(sda, this.Hvo, 0))
				{
					int wsActual = 0;
					ITsString tssform = null;
					foreach (int hvoWordform in Cache.GetVectorProperty(this.Hvo, vh.Tag, true))
					{
						if (m_cache.IsDummyObject(hvoWordform))
						{
							// We won't have found it in the database, but we want to add it to our table.
							if (lp.TryWs(LangProject.kwsFirstVernOrNamed, hvoWordform, (int)WfiWordform.WfiWordformTags.kflidForm,
								out wsActual, out tssform))
							{
								s_formToWfId[tssform.Text + wsActual.ToString()] = hvoWordform;
							}
						}
					}
				}

				return s_formToWfId;
			}
		}

		private int NoteWordformForFormAndWs(ITsString tssForm, int ws, int hvoWordform)
		{
			return s_formToWfId[tssForm.Text + ws.ToString()] = hvoWordform;
		}

		/// <summary>
		/// Creates a dummy wordform and updates the appropriate tables.
		/// </summary>
		/// <param name="tssWord"></param>
		/// <returns></returns>
		public int AddDummyWordform(ITsString tssWord)
		{
			int kflidConcordanceWordforms = WordformInventory.ConcordanceWordformsFlid(m_cache);
			int hvoWordform;
			string form = CheckFormLength(tssWord.Text, false);
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			m_cache.CreateDummyID(out hvoWordform);
			cda.CacheIntProp(hvoWordform, (int)CmObjectFields.kflidCmObject_Class, WfiWordform.kclsidWfiWordform);
			int ws = GetFirstWsFromTss(tssWord);
			cda.CacheStringAlt(hvoWordform, (int)WfiWordform.WfiWordformTags.kflidForm, ws, tssWord);
			// say this owned in ConcordanceWordforms, even if we haven't actually added to that list yet.
			cda.CacheIntProp(hvoWordform, (int)CmObjectFields.kflidCmObject_OwnFlid, kflidConcordanceWordforms);
			cda.CacheObjProp(hvoWordform, (int)CmObjectFields.kflidCmObject_Owner, m_cache.LangProject.WordformInventoryOAHvo);
			this.FormToWfIdTable[form + ws.ToString()] = hvoWordform;
			// if kflidConcordanceWordforms is loaded, then add the new hvoWordform.
			if (!SuspendUpdatingConcordanceWordforms)
				TryUpdatingConcordanceWordforms(Cache, 0, hvoWordform);
			//			cda.CacheVecProp(hvoWordform, kflidOccurrences, new int[0], 0);
			//Debug.WriteLine("CreateDummyWordform: new obj id ("+ hvoWordform +") classId ("+ m_cache.GetClassOfObject(hvoWordform) +")");
			// Review JohnT: what do we have to do to be sure the concordance is resorted and redisplayed??
			return hvoWordform;
		}

		/// <summary>
		/// Guard against ridiculously long words so that we don't crash in the database.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="fComplain"></param>
		/// <returns></returns>
		private string CheckFormLength(string form, bool fComplain)
		{
			int cchMax = m_cache.MaxFieldLength((int) WfiWordform.WfiWordformTags.kflidForm);
			if (form.Length > cchMax)
			{
				if (fComplain)
				{
					System.Windows.Forms.MessageBox.Show(Strings.ksWordOrPhraseIsTooLong,
						Strings.ksWarning,
						System.Windows.Forms.MessageBoxButtons.OK,
						System.Windows.Forms.MessageBoxIcon.Warning);
				}
				form = form.Substring(0, cchMax);
			}
			return form;
		}

		/// <summary>
		/// Set this when SuspendUpdatingConcordanceWordforms to prevent trying to
		/// updating ConcordanceWordforms property during conversion / adding wordforms.
		/// </summary>
		public bool SuspendUpdatingConcordanceWordforms
		{
			get { return m_fSuspendUpdatingConcordanceWordforms; }
			set
			{
				bool prevValue = m_fSuspendUpdatingConcordanceWordforms;
				m_fSuspendUpdatingConcordanceWordforms = value;
			}
		}

		/// <summary>
		/// Resets the ConcordanceWordforms property and resets any existing wordform occurrences.
		/// </summary>
		public void ResetConcordanceWordformsAndOccurrences()
		{
			IVwVirtualHandler vh;
			if (Cache.TryGetVirtualHandler(ConcordanceWordformsFlid(Cache), out vh))
			{
				(vh as BaseFDOPropertyVirtualHandler).Clear(this.Hvo, 0);
			}
			this.ResetAllWordformOccurrences();
		}

		/// <summary>
		/// Updates ConcordanceWordform if it's in the cache.
		/// Enhance: We could refactor this to call a more generic FdoCache.TryUpdatingVector().
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <param name="hvoOldWordform">if 0, we'll insert hvoNewWordform at the end of the list.</param>
		/// <param name="hvoNewWordform">if 0, we'll delete hvoOldWordform from the list.</param>
		/// <returns>true, if it updated the vector, false if it didn't.</returns>
		internal static bool TryUpdatingConcordanceWordforms(FdoCache fdoCache, int hvoOldWordform, int hvoNewWordform)
		{
			ISilDataAccess sda = fdoCache.MainCacheAccessor;
			IVwCacheDa cda = fdoCache.VwCacheDaAccessor;
			// If WFI has ConcordanceWordforms property, replace hvoOldWordform with hvoNewWordform in it.
			int kflidConcordanceWordforms = WordformInventory.ConcordanceWordformsFlid(fdoCache);
			int hvoWfi = fdoCache.LangProject.WordformInventoryOAHvo;
			if (sda.get_IsPropInCache(hvoWfi, kflidConcordanceWordforms, (int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				List<int> wordforms = new List<int>(fdoCache.GetVectorProperty(hvoWfi, kflidConcordanceWordforms, true));
				// if wordforms.Count == 0, we must be in a context that has affected the wordforms,
				// but doesn't want to take direct responsibility for maintaining this list.
				if (wordforms.Count == 0)
				{
					return false;
				}
				int ihvoWfNew = wordforms.IndexOf(hvoNewWordform);
				Debug.Assert(ihvoWfNew == -1, "We are trying to insert a new wordform, but it's already in the concordance.");
				int[] newItems = ihvoWfNew == -1 && hvoNewWordform != 0 ? new int[] { hvoNewWordform } : new int[0];
				CacheReplaceOneUndoAction cacheReplaceOccurrenceAction;
				if (hvoOldWordform != 0)
				{
					// we are trying replacing an existing wordform in the ConcordanceWordforms.
					int ihvoWf = wordforms.IndexOf(hvoOldWordform);
					Debug.Assert(ihvoWf != -1, "We are trying to remove an old wordform, but it's not in the concordance.");
					if (ihvoWf >= 0)
					{
						cacheReplaceOccurrenceAction = new CacheReplaceOneUndoAction(fdoCache, hvoWfi,
							kflidConcordanceWordforms, ihvoWf, ihvoWf + 1, newItems);
						cacheReplaceOccurrenceAction.DoIt();
						return true;
					}
					return false;
				}
				else if (ihvoWfNew == -1 && hvoNewWordform != 0)
				{
					// we are inserting a new wordform in ConcordanceWordforms. add it to the end of the list.
					cacheReplaceOccurrenceAction = new CacheReplaceOneUndoAction(fdoCache, hvoWfi,
						kflidConcordanceWordforms, wordforms.Count, wordforms.Count, newItems);
					cacheReplaceOccurrenceAction.DoIt();
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Adds a wordform to our WordformsOC and updates the appropriate tables.
		/// </summary>
		/// <param name="tssWord"></param>
		/// <returns></returns>
		public IWfiWordform AddRealWordform(ITsString tssWord)
		{
			return AddRealWordform(tssWord, !SuspendUpdatingConcordanceWordforms);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tssWord"></param>
		/// <param name="fAddToConcordanceWordforms">if true, try to add the word to ConcordanceWordforms</param>
		/// <returns></returns>
		private IWfiWordform AddRealWordform(ITsString tssWord, bool fAddToConcordanceWordforms)
		{
			return AddRealWordform(tssWord.Text, GetFirstWsFromTss(tssWord), fAddToConcordanceWordforms);
		}

		/// <summary>
		/// Update the FormToWfIdTable to store the specified value for the specified key.
		/// This is useful, for example, when Redo re-creates a real Wordform, and in intervening
		/// request to find it may have created a dummy.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <param name="hvoWf"></param>
		public void UpdateConcWordform(string form, int ws, int hvoWf)
		{
			string key = FormToWfIdKey(form, ws);
			FormToWfIdTable[key] = hvoWf;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <param name="fAddToConcordanceWordforms">if true, try to add the word to ConcordanceWordforms</param>
		/// <returns></returns>
		private IWfiWordform AddRealWordform(string form, int ws, bool fAddToConcordanceWordforms)
		{
			form = CheckFormLength(form, true);
			bool previousState = UpdatingWordformsOC;
			UpdatingWordformsOC = true;
			IWfiWordform wf = null;
			try
			{
				// Verify that we haven't already added this form + ws.
				string key = FormToWfIdKey(form, ws);
				int oldValue = 0;
				if (FormToWfIdTable.TryGetValue(key, out oldValue) && !Cache.IsDummyObject(oldValue))
				{
					string msg = String.Format("We don't expect to have already real form({0}) ws({1}), since it already exists in hvo {2}",
						form, ws, oldValue);
					Debug.Fail(msg);
					throw new ArgumentException(msg);
				}

				// suppress the first prop changed, since we need to set the form before
				// listeners have enough information to do anything else.
				using (new IgnorePropChanged(Cache, PropChangedHandling.SuppressAll))
				{
					wf = m_cache.LangProject.WordformInventoryOA.WordformsOC.Add(new WfiWordform());
					// We currently only add wordforms in default vernacular (cf. LT-5379)
					//wf.Form.SetAlternative(form, ws);
					FormToWfIdTable[key] = wf.Hvo;
					if (fAddToConcordanceWordforms)
					{
						WordformInventory.TryUpdatingConcordanceWordforms(Cache, 0, wf.Hvo);
					}
				}
				wf.Form.SetAlternative(form, ws);
			}
			finally
			{
				UpdatingWordformsOC = previousState;
			}
			return wf;
		}

		/// <summary>
		/// obtain the key used in the FormToWfIdTable.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		private static string FormToWfIdKey(string form, int ws)
		{
			return form + ws.ToString();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public IWfiWordform AddRealWordform(string form, int ws)
		{
			return AddRealWordform(form, ws, !SuspendUpdatingConcordanceWordforms);
		}

		/// <summary>
		/// Clear out wordform Occurrences property from existing wordforms.
		/// This ensures that we won't create duplicates as we parse through a text.
		/// As a result, after we parse, the only occurrences that will be cached
		/// are for whatever we parsed.
		/// The advantage is that we don't have to clean up ones that aren't reused,
		/// or worry about whether we've reused them correctly.
		/// The performance hit may not be noticeable during parse.
		/// However, it's definitely a problem if we parse repeatedly and just let the obsolete
		/// dummy Wfics clutter up all the hashtables in the cache. Currently we at least reuse
		/// them when we reparse the paragraph.
		/// Enhance JohnT: Conceivably we could pass in a set of paragraphs belonging to texts
		/// we want to include but don't want to reparse, and not remove their Wfics.
		/// </summary>
		public void ResetAllWordformOccurrences()
		{
			// first invalidate any existing table, since we want to make sure we
			// are resetting the values of the most up to date list of wordforms (which may
			// have changed due to an Undo).
			Dictionary<string, int> oldFormToWfid = s_formToWfId;
			if (m_cache != s_cacheThatFormTableIsBasedOn)
			{
				// Enhance JohnT: this allows all dummy wordforms to become memory-leaks!
				// But we dare not clear them out, there MIGHT be a window still displaying them (I think?).
				// Most promising option: make s_formToWfId a member variable of the cache, so each can have one.
				WordformInventory.InvalidateFormIdTable();
				oldFormToWfid = null;
			}
			else
			{
				s_formToWfId = null; // force PreLoad to make a new one.
			}
			this.PreLoadFormIdTable();


			if (oldFormToWfid != null)
			{
				// Salvage any dummy wordforms from the old table, to avoid re-creating them
				// (Unless they've become real, in which case, we clear them out.)
				foreach (KeyValuePair<string, int> pair in oldFormToWfid)
				{
					if (m_cache.IsDummyObject(pair.Value))
					{
						if (s_formToWfId.ContainsKey(pair.Key))
						{
							// Junk dummy wordform, probably became real. Recover memory used for it as far as may be.
							m_cache.VwCacheDaAccessor.ClearInfoAbout(pair.Value, VwClearInfoAction.kciaRemoveObjectInfoOnly);
						}
						else
						{
							s_formToWfId[pair.Key] = pair.Value;
						}
					}
				}
			}

			IVwCacheDa cda = s_cacheThatFormTableIsBasedOn.VwCacheDaAccessor;
			int kflidOccurrences = WfiWordform.OccurrencesFlid(s_cacheThatFormTableIsBasedOn);
			IVwVirtualHandler vh;
			if (Cache.TryGetVirtualHandler(kflidOccurrences, out vh))
			{
				//ISilDataAccess sda = s_cacheThatFormTableIsBasedOn.MainCacheAccessor;
				//int tagOccurrences = vh.Tag;
				foreach (int hvo in s_formToWfId.Values)
				{
					// It isn't safe to reuse all of the Wfics, some may belong to texts that are not being
					// cleared and reparsed yet. It IS safe to just clear the occurrences, because the
					// dummy Wfics are conceptually 'owned' more by the relevant text paragraphs.
					//int cOccurrences = sda.get_VecSize(hvo, tagOccurrences);
					//if (cOccurrences > 0)
					//{
					//    int[] hvos;
					//    using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(cOccurrences, typeof(int)))
					//    {
					//        sda.VecProp(hvo, tagOccurrences, cOccurrences, out cOccurrences, arrayPtr);
					//        hvos = (int[])MarshalEx.NativeToArray(arrayPtr, cOccurrences, typeof(int));
					//    }
					//    foreach (int hvoOcc in hvos)
					//    {
					//        if (Cache.IsDummyObject(hvoOcc))
					//        {
					//            if (discardedDummyAnnotationsCollector == null)
					//                cda.ClearInfoAbout(hvoOcc, VwClearInfoAction.kciaRemoveObjectInfoOnly);
					//            else
					//                discardedDummyAnnotationsCollector.Add(hvoOcc);
					//        }
					//    }
					//}
					(vh as BaseFDOPropertyVirtualHandler).Clear(hvo, 0);
				}
			}
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
		///
		/// We also force there to be an external spelling dictionary for the default vernacular WS;
		/// others are updated only if they already exist.
		/// </summary>
		public void ConformSpellingDictToWfi()
		{
			// Force a dictionary to exist for the default vernacular writing system.
			LgWritingSystem lws = (LgWritingSystem) LgWritingSystem.CreateFromDBObject(m_cache, m_cache.DefaultVernWs);
			Enchant.Dictionary dict = lws.EnsureDictionary();
			// Make all existing spelling dictionaries give as nearly as possible the right answers.
			foreach (LgWritingSystem wsObj in m_cache.LangProject.CurVernWssRS)
			{
				int ws = wsObj.Hvo;
				dict = EnchantHelper.GetDictionary(ws, m_cache.LanguageWritingSystemFactoryAccessor);
				if (dict != null)
				{
					foreach (WfiWordform wf in WordformsOC)
					{
						string wordform = wf.Form.GetAlternative(ws);
						if (!string.IsNullOrEmpty(wordform))
							EnchantHelper.SetSpellingStatus(wordform,
															wf.SpellingStatus == (int) SpellingStatusStates.correct, dict);
					}
				}
			}
		}

		/// <summary>
		/// Disable the vernacular spelling dictionary for all vernacular WSs.
		/// </summary>
		public void DisableVernacularSpellingDictionary()
		{
			foreach(LgWritingSystem lws in m_cache.LangProject.CurVernWssRS)
				lws.DisableDictionary();
		}

		#region IDummy Implementation

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="hvoDummy"></param>
		/// <returns></returns>
		public ICmObject ConvertDummyToReal(int flid, int hvoDummy)
		{
			using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(Cache, true))
			{
				ICmObject realObj = null;
				if (flid == ConcordanceWordformsFlid(Cache) &&
					Cache.GetClassOfObject(hvoDummy) == (int)WfiWordform.kclsidWfiWordform)
				{
					// Do the conversion.
					ISilDataAccess sda = Cache.MainCacheAccessor;
					Debug.Assert(Cache.IsDummyObject(hvoDummy), "this should be a dummy wordform.");
					// a dummy wordform should only have one ws associated with it.
					// try to find it, so we can make a new one out of its form.
					MultiAccessor ma = MultiAccessor.CreateMultiAccessor(Cache,
						hvoDummy, (int)WfiWordform.WfiWordformTags.kflidForm, "");
					IWfiWordform wf = null;
					ITsString tssForm;
					int wsActual;
					if (ma.TryWs(LangProject.kwsFirstVernOrNamed, out wsActual, out tssForm))
					{
						int hvoWf = 0;
						if (FormToWfIdTable.TryGetValue(tssForm.Text + wsActual.ToString(), out hvoWf))
						{
							if (Cache.IsDummyObject(hvoWf))
								wf = this.AddRealWordform(tssForm, false);	// do the conversion.
							else
								wf = WfiWordform.CreateFromDBObject(Cache, hvoWf);	// return the conversion
						}
						else
						{
							string msg = String.Format("Can't convert the dummy({0}) form({1}) ws({2}). Wasn't found in our FormToIdTable.",
								hvoDummy, tssForm.Text, wsActual);
							Debug.Fail(msg);
							throw new ArgumentException(msg);
						}
						WfiWordform.CleanupDummyWordformReferences(Cache, hvoDummy, wf.Hvo);
						if (!SuspendUpdatingConcordanceWordforms)
						{
							// Replace the dummy with the real wordform in our ConcordanceWordforms.
							TryUpdatingConcordanceWordforms(Cache, hvoDummy, wf.Hvo);
						}
					}
					realObj = wf;
				}
				else
				{
					throw new Exception("The method or operation is not implemented.");
				}
				return realObj;
			}
		}

		/// <summary>
		/// Notify WordformInventory that a refresh is about to occur, so that it can manage its Dummy list.
		/// </summary>
		public bool OnPrepareToRefresh(object args)
		{
			// the dummy ids in our map will no longer be valid.
			InvalidateFormIdTable();
			return false;
		}

		#endregion IDummy Implementation
	}

	/// <summary>
	/// Values for spelling status of WfiWordform.
	/// </summary>
	public enum SpellingStatusStates
	{
		/// <summary>
		/// dunno
		/// </summary>
		undecided,
		/// <summary>
		/// well-spelled
		/// </summary>
		correct,
		/// <summary>
		/// no good
		/// </summary>
		incorrect
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wordform Inventory Wordform class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class WfiWordform : IDummy
	{
		/// <summary>
		/// Maximum length for the text used in a WfiWordform.
		/// </summary>
		public const int kMaxWordformLength = 300;

		#region Construction and creation

		/// <summary>
		/// Find or create a WfiWordform object and return its (real) HVO.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tss"> the form.</param>
		/// <returns>the hvo of a real wordform.</returns>
		static public int FindOrCreateWordform(FdoCache cache, ITsString tss)
		{
			return FindOrCreateWordform(cache, tss, true);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tss"></param>
		/// <param name="fMakeReal"></param>
		/// <returns></returns>
		static public int FindOrCreateWordform(FdoCache cache, ITsString tss, bool fMakeReal)
		{
			string form = tss.Text;
			int ws = WordformInventory.GetFirstWsFromTss(tss);
			int wfHvo = FindOrCreateWordform(cache, form, ws, fMakeReal);
			return wfHvo;
		}

		/// <summary>
		/// Find a wordform with the given form and writing system,
		/// creating a real one, if it is not found.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="form">The form to look for.</param>
		/// <param name="ws">The writing system to use.</param>
		/// <returns>The wordform, or null if an exception was thrown by the database accessors.</returns>
		public static IWfiWordform FindOrCreateWordform(FdoCache cache, string form, int ws)
		{
			Debug.Assert(!string.IsNullOrEmpty(form));
			int wfHvo = FindOrCreateWordform(cache, form, ws, true);
			return CreateFromDBObject(cache, wfHvo);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <param name="fMakeReal">if true, returns a real wordform.
		/// if false, it creates/returns a dummy if it can't find a real one.</param>
		/// <returns></returns>
		public static int FindOrCreateWordform(FdoCache cache, string form, int ws, bool fMakeReal)
		{
			int wfHvo = FindExistingWordform(form, ws, cache);
			IWfiWordform wf = null;
			if (fMakeReal && cache.IsDummyObject(wfHvo))
			{
				// convert this to a real object.
				wf = CmObject.ConvertDummyToReal(cache, wfHvo) as IWfiWordform;
				wfHvo = wf.Hvo;
			}
			if (wfHvo == 0)
			{
				// Give up looking for one, and just make a new one.
				if (fMakeReal)
				{
					wf = cache.LangProject.WordformInventoryOA.AddRealWordform(form, ws);
					wfHvo = wf.Hvo;
					Debug.Assert(wfHvo > 0);
				}
				else
				{
					wfHvo = cache.LangProject.WordformInventoryOA.AddDummyWordform(CreateWordformTss(form, ws));
				}
			}
			return wfHvo;
		}

		static private int FindExistingWordform(string form, int ws, FdoCache cache)
		{
			return cache.LangProject.WordformInventoryOA.GetWordformId(form, ws, true);
		}

		/// <summary>
		/// Returns an ITsString wordform with the given form and ws.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		private static ITsString CreateWordformTss(string form, int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(form, ws);
		}
		#endregion Construction and creation

		#region miscellaneous
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoCba"></param>
		/// <returns></returns>
		static public int GetWfiWordformFromInstanceOf(FdoCache cache, int hvoCba)
		{
			int hvoWordform = 0;
			if (!TryGetWfiWordformFromInstanceOf(cache, hvoCba, out hvoWordform))
			{
				throw new ArgumentException(String.Format("Could not get the WfiWordform associated with annotation {0} InstanceOf.", hvoCba));
			}
			return hvoWordform;
		}

		/// <summary>
		/// Try to get a WfiWordform determined by the InstanceOf of a CmBaseAnnotation.
		/// Returns true if successful. Punctuation, for example, may answer false.
		/// </summary>
		/// <returns>true if hvoWordform is nonzero</returns>
		public static bool TryGetWfiWordformFromInstanceOf(FdoCache cache, int hvoCba, out int hvoWordform)
		{
			int cbaInstanceOf = cache.GetObjProperty(hvoCba, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
			hvoWordform = GetWordformFromWag(cache, cbaInstanceOf);
			return hvoWordform != 0;
		}

		/// <summary>
		/// Get a wordform from an HVO that may be a WfiWordform, WfiAnalysis, or WfiGloss (or 0).
		/// Answer 0 if arguent is zero, fail if it is some other class.
		/// </summary>
		public static int GetWordformFromWag(FdoCache cache, int cbaInstanceOf)
		{
			int hvoWordform = 0;
			if (cbaInstanceOf != 0)
			{
				int classid = cache.GetClassOfObject(cbaInstanceOf);
				switch (classid)
				{
					case WfiWordform.kclsidWfiWordform:
						hvoWordform = cbaInstanceOf;
						break;
					case WfiAnalysis.kclsidWfiAnalysis:
						hvoWordform = cache.GetOwnerOfObject(cbaInstanceOf);
						break;
					case WfiGloss.kclsidWfiGloss:
						int hvoAnalysis = cache.GetOwnerOfObject(cbaInstanceOf);
						hvoWordform = cache.GetOwnerOfObject(hvoAnalysis);
						break;
					default:
						Debug.Fail("Actual cba (" + cbaInstanceOf + "): Class of InstanceOf (" +
								   classid + ") is not WfiWordform, WfiAnalysis, or WfiGloss.");
						break;
				}
			}
			return hvoWordform;
		}

		/// <summary>
		/// If hvoWordform is the hvo of a capitalized wordform which has no useful information,
		/// delete it. It is considered useless if
		/// - it has no occurrences
		/// - it has no anlyses
		/// - it doesn't have known incorrect spelling status.
		/// Note that the argument may be some other kind of object (typically a WfiAnalysis or WfiGloss).
		/// If so do nothing.
		/// </summary>
		public static void DeleteRedundantCapitalizedWordform(FdoCache cache, int hvoWordform)
		{
			if (cache.GetClassOfObject(hvoWordform) != WfiWordform.kclsidWfiWordform)
				return;
			if (cache.GetVectorProperty(hvoWordform, OccurrencesFlid(cache), true).Length != 0)
				return;
			if (cache.IsValidObject(hvoWordform))
			{
				// If it's real it might have analyses etc.
				WfiWordform wf = (WfiWordform) CmObject.CreateFromDBObject(cache, hvoWordform);
				if (wf.AnalysesOC.Count > 0)
					return;
				// Arguably we should keep it for known correct spelling status. However, if it's ever been
				// confirmed as an analysis, even temporarily, it will have that.
				if (wf.SpellingStatus == (int)SpellingStatusStates.incorrect)
					return;
			}
			foreach (int ws in cache.LangProject.CurVernWssRS.HvoArray)
			{
				CaseFunctions cf = new CaseFunctions(cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws).IcuLocale);
				string text = cache.GetMultiStringAlt(hvoWordform, (int) WfiWordformTags.kflidForm, ws).Text;
				if (!String.IsNullOrEmpty(text) && cf.StringCase(text) == StringCaseStatus.allLower)
					return;
			}
		   cache.DeleteObject(hvoWordform);
		}

#endregion miscellaneous

		#region Misc Properties

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
				if (m_cache.IsDummyObject(this.Hvo))
					return false;		// assume dummy wordform still needs work. (See LT-7816)
				// Susanna doesn't want to check the wordform's properties,
				// other than the analyses.
				if (AnalysesOC.Count == 0)
					return false;
				foreach (IWfiAnalysis anal in AnalysesOC)
				{
					if (!anal.IsComplete)
						return false;
				}

				return base.IsComplete;
			}
		}

		/// <summary>
		/// Clean up anything we can find that uses the old dummy wordform.
		/// For each occurrence of the dummy wordform, update the Twfic's InstanceOf.
		/// Iterate over the Occurrences property to find them.
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <param name="hvoDummyWordform"></param>
		/// <param name="hvoRealWordform"></param>
		internal static void CleanupDummyWordformReferences(FdoCache fdoCache, int hvoDummyWordform, int hvoRealWordform)
		{
			ISilDataAccess sda = fdoCache.MainCacheAccessor;
			int kflidOccurrences = WfiWordform.OccurrencesFlid(fdoCache);
			IVwCacheDa cda = fdoCache.VwCacheDaAccessor;
			int[] occurrences = fdoCache.GetVectorProperty(hvoDummyWordform, kflidOccurrences, true);
			for (int i = 0; i < occurrences.Length; i++)
			{
				int hvoDummyOccurrence = occurrences[i];
				cda.CacheObjProp(hvoDummyOccurrence, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, hvoRealWordform);
				int ownerPrev = fdoCache.GetObjProperty(hvoDummyOccurrence, (int)CmObjectFields.kflidCmObject_Owner);
				if (ownerPrev == hvoDummyWordform)
					cda.CacheObjProp(hvoDummyOccurrence, (int)CmObjectFields.kflidCmObject_Owner, hvoRealWordform);
			}
			cda.CacheVecProp(hvoRealWordform, kflidOccurrences, occurrences, occurrences.Length);

		}

		/// <summary>
		/// Get the annotations that make for a concordance.
		/// </summary>
		public List<int> ConcordanceIds
		{
			get
			{
				List<int> cbaList = OccurrencesInTexts;
				List<int> concordanceIds = new List<int>(cbaList.Count);
				if (cbaList.Count > 0)
				{
					int[] cbas = cbaList.ToArray();
					FdoObjectSet<ICmBaseAnnotation> cbaObjs = new FdoObjectSet<ICmBaseAnnotation>(Cache, cbas, false);
					foreach (ICmBaseAnnotation cba in cbaObjs)
					{
						if (cba.InstanceOfRAHvo == this.Hvo)
						{
							concordanceIds.Add(cba.Hvo);
						}
					}
				}
				return concordanceIds;
			}
		}

		/// <summary>
		/// Get the annotations that make for a full concordance at all three levels.
		/// </summary>
		public List<int> FullConcordanceIds
		{
			get
			{
				return OccurrencesInTexts;
			}
		}

		/// <summary>
		/// Get a count of the annotations that make for a full concordance at all three levels.
		/// </summary>
		public int FullConcordanceCount
		{
			get { return FullConcordanceIds.Count; }
		}

		/// <summary>
		/// The CmBaseAnnotations that reference an occurrence of this word in a text.
		/// Note (JohnT): This and related properties are only valid when texts have been parsed.
		/// The typical path where this happens is ParagraphParser.ConcordTexts(), which is called from
		/// ConcordanceWordsVirtualHandler.Load and parsed a list of texts which is all the interlinear
		/// texts plus typically selected Scripture passages. If this has not been called, or something
		/// equivalent done, OccurrencesInTexts will produce empty lists.
		/// </summary>
		public List<int> OccurrencesInTexts
		{
			get { return new List<int>(this.Cache.GetVectorProperty(this.Hvo, OccurrencesFlid(Cache), true)); }
		}

		/// <summary>
		///
		/// </summary>
		public List<int> TextGenres
		{
			get
			{
				Set<int> genres = new Set<int>();
				List<int> cbaGenres = new List<int>();
				foreach (int hvoAnn in OccurrencesInTexts)
				{
					CmBaseAnnotation cba = new CmBaseAnnotation(Cache, hvoAnn, false, false);
					genres.AddRange(cba.TextGenres);
				}
				// Sort the genres (?)
				return new List<int>(genres);
			}
		}


		/// <summary>
		/// Check whether the current WfiWordform's form is found in a text with the given
		/// writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public bool FormIsUsedWithWs(int ws)
		{
			foreach (int hvoAnno in OccurrencesInTexts)
			{
				ICmBaseAnnotation cba = CmBaseAnnotation.CreateFromDBObject(m_cache, hvoAnno);
				if (cba.WritingSystemRAHvo == 0)
					cba.WritingSystemRAHvo = AssignMissingAnnotationWritingSystem(cba);
				if (cba.WritingSystemRAHvo == ws)
					return true;
			}
			return false;
		}

		private int AssignMissingAnnotationWritingSystem(ICmBaseAnnotation cba)
		{
			return CmBaseAnnotation.GetAnnotationWritingSystem(m_cache, cba.BeginObjectRAHvo,
				cba.BeginOffset, cba.Flid);
		}

		/// <summary>
		/// Flid for virtual property Occurrences
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		static public int OccurrencesFlid(FdoCache cache)
		{
			return DummyVirtualHandler.GetInstalledHandlerTag(cache, "WfiWordform", "OccurrencesInTexts");
		}

		/// <summary>
		/// Adds a new wordform annotation (occurrence) to this wordform.
		/// </summary>
		/// <param name="hvoNewCba"></param>
		public bool TryAddOccurrence(int hvoNewCba)
		{
			int[] occurrences = m_cache.GetVectorProperty(this.Hvo, OccurrencesFlid(Cache), true);
			List<int> occurrencesList = new List<int>(occurrences);
			// lookup the occurrence to see if we already have it. (we don't want to add it twice).
			if (occurrencesList.IndexOf(hvoNewCba) != -1)
				return false;
			WfiWordform.AddDummyAnnotation(Cache, this.Hvo, hvoNewCba);
			// insert at the end.
			CacheReplaceOneUndoAction cacheReplaceOccurrenceAction = new CacheReplaceOneUndoAction(Cache,
				this.Hvo, OccurrencesFlid(Cache), occurrences.Length, occurrences.Length, new int[] { hvoNewCba });
			cacheReplaceOccurrenceAction.DoIt();
			return true;
		}

		/// <summary>
		/// Creates a dummy annotation and gives it a virtual owner.
		/// Enhance: we should make this a nonstatic virtual property of LangProject, since it currently owns the real Annotations.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoWordform"></param>
		/// <param name="hvoAnn"></param>
		/// <returns>hvo of the dummy annotation.</returns>
		static public int AddDummyAnnotation(FdoCache cache, int hvoWordform, int hvoAnn)
		{
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			// say this owned in WfiWordform.OccurrencesInTexts, even if we haven't actually added to that list yet.
			cda.CacheIntProp(hvoAnn, (int)CmObjectFields.kflidCmObject_OwnFlid, OccurrencesFlid(cache));
			// if the owner is a dummy, remember to change it when it becomes real.
			cda.CacheObjProp(hvoAnn, (int)CmObjectFields.kflidCmObject_Owner, hvoWordform);
			return hvoAnn;
		}

		#endregion Misc Properties

		#region parsing related things

		/// <summary>
		/// Get the date that they given agent last evaluated this Wordform.
		/// </summary>
		/// <param name="hvoAgent"></param>
		/// <returns></returns>
		public string GetLastEvaluationDate(int hvoAgent)
		{
			//Enhance (JohnH): we don't really need to load the whole CmEvaluation,
			//	if we just used a SQL query we could just grab the date we need.

			ICmAgentEvaluation evaluation = GetEvaluationFromAgent(hvoAgent);
			if (evaluation == null)
			{
				return Strings.ksNotEvalYet;
			}
			else
			{
				DateTime dt = evaluation.DateCreated;
				if(dt.Date == DateTime.Now.Date)
					return String.Format(Strings.ksTodayAtX, dt.ToShortTimeString());
				else
					return dt.ToString("g");	// Short Date followed by Short Time
			}
		}

		/// <summary>
		/// True, if usear and parser are in agreement on status of all analyses.
		/// </summary>
		public bool HumanAndParserAgree
		{
			get
			{
				// 1. Check on human has no opinion yet.
				// If there are any the parser has found, but the user has not weighed in on,
				// they disagree.
				if (HumanNoOpinionParses.Count > 0)
					return false;

				// 2. Check on ones the user has disapproved of.
				// If the parser can find at least them, there is disagreement.
				foreach (int disapprovedAnalysisHvo in HumanDisapprovedParses)
				{
					IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_cache, disapprovedAnalysisHvo);
					if (anal.ParserStatusIcon == (int)Opinions.approves)
						return false;
				}

				// 3. Check to make sure all human approved analyses are also
				// produced by the parser.
				foreach (int approvedAnalysisHvo in HumanApprovedAnalyses)
				{
					IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_cache, approvedAnalysisHvo);
					if (anal.ParserStatusIcon != (int)Opinions.approves)
						return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Get a List of Hvos that the human has approved of.
		/// </summary>
		public List<int> HumanApprovedAnalyses
		{
			get { return AnalysesWithHumanEvaluation(Opinions.approves); }
		}

		/// <summary>
		/// Get a List of Hvos that the human has no opinion on.
		/// </summary>
		public List<int> HumanNoOpinionParses
		{
			get { return AnalysesWithHumanEvaluation(Opinions.noopinion); }
		}

		/// <summary>
		/// Get a List of Hvos that the human has DISapproved of.
		/// </summary>
		public List<int> HumanDisapprovedParses
		{
			get { return AnalysesWithHumanEvaluation(Opinions.disapproves); }
		}

		private List<int> AnalysesWithHumanEvaluation(Opinions opinion)
		{
			List<int> matchingHvos = new List<int>();
			ICmAgent humanAgent = m_cache.LangProject.DefaultUserAgent;
			// Dummy wordforms shouldn't have any analyses.
			if (IsDummyObject)
				return matchingHvos;
			foreach(IWfiAnalysis wa in AnalysesOC)
			{
				if (wa.GetAgentOpinion(humanAgent) == opinion)
					matchingHvos.Add(wa.Hvo);
			}

			return matchingHvos;
		}

		/// <summary>
		/// Get the date that the default Parser last evaluated this Wordform.
		/// </summary>
		public string LastParseDate
		{
			get
			{
				return  GetLastEvaluationDate(m_cache.LangProject.DefaultParserAgent.Hvo);//enhance?
			}
		}

		/// <summary>
		/// Get the number of valid parses by the default user
		/// </summary>
		public int UserCount
		{
			get
			{
				int hvoAgent = m_cache.LangProject.DefaultUserAgent.Hvo;
				return AgentCount(hvoAgent);
			}
		}

		/// <summary>
		/// Make an (hvo, count) pair in the dictionary for every object of the class,
		/// or at least, every one that has a non-empty list.
		/// Note: this must stay in sync with UserCount!
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="values"></param>
		public static void LoadAllUserCounts(FdoCache cache, Dictionary<int, int> values)
		{
			int hvoAgent = cache.LangProject.DefaultUserAgent.Hvo;
			LoadAllAgentCounts(cache, values, hvoAgent);
		}

		/// <summary>
		/// Get the number of valid parses by the default parser
		/// </summary>
		public int ParserCount
		{
			get
			{
				if (this.IsDummyObject)
				{
					// Let OnRequestConversionToReal() queue up this wordform to become real.
					return 0;
				}
				int hvoAgent = m_cache.LangProject.DefaultParserAgent.Hvo;
				return AgentCount(hvoAgent);
			}
		}

		/// <summary>
		/// Make an (hvo, count) pair in the dictionary for every object of the class,
		/// or at least, every one that has a non-empty list.
		/// Note: this must stay in sync with ParserCount!
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="values"></param>
		public static void LoadAllParserCounts(FdoCache cache, Dictionary<int, int> values)
		{
			int hvoAgent = cache.LangProject.DefaultParserAgent.Hvo;
			LoadAllAgentCounts(cache, values, hvoAgent);
		}

		/// <summary>
		/// Get the count of conflicts between the parser and the human.
		/// </summary>
		public int ConflictCount
		{
			get
			{
				int conflictCount = 0;

				if (IsDummyObject || AnalysesOC.Count == 0)
				{
					conflictCount = -1;
				}
				else
				{
					ICmAgent humanAgent = m_cache.LangProject.DefaultUserAgent;
					ICmAgent parserAgent = m_cache.LangProject.DefaultParserAgent;
					foreach (IWfiAnalysis wa in AnalysesOC)
					{
						Opinions humanOpinion = wa.GetAgentOpinion(humanAgent);
						Opinions parserOpinion = wa.GetAgentOpinion(parserAgent);
						if (humanOpinion != parserOpinion)
						{	// don't count the case where the human has disapproved and the parser does
							// not succeed (i.e. has no opinion).  See LT-7265.
							if (!((humanOpinion == Opinions.disapproves) &&
								  (parserOpinion == Opinions.noopinion)))
								conflictCount++;
						}
					}
				}

				return conflictCount;
			}
		}

		/// <summary>
		/// Get the number of valid parses for the agent specified in hvoAgent
		/// <param name="hvoAgent">The ID of the agent that we are interested in.</param>
		/// </summary>
		public int AgentCount(int hvoAgent)
		{
			int count = 0;
			IOleDbCommand odc = null;
			try
			{
				uint intSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
				Debug.Assert(m_cache != null); // The cache must be set.
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
				odc.SetParameter(1, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, (ushort)DBTYPEENUM.DBTYPE_I4, new uint[] { (uint)Hvo }, intSize);
				// The SQL command must NOT modify the database contents!
				string sSql = string.Format(
					"select count(id) from CmAgentEvaluation_ cae" +
					" join WfiWordform_Analyses wwa on wwa.dst = cae.target" +
					" where cae.accepted = 1 and cae.owner$ = {0} and wwa.src = {1}",
					hvoAgent, Hvo);
				odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				bool fIsNull;
				bool fMoreRows;
				uint cbSpaceTaken;
				odc.GetRowset(0);
				odc.NextRow(out fMoreRows);
				if (fMoreRows)
				{
					using (ArrayPtr rgCount = MarshalEx.ArrayToNative(1, typeof(uint)))
					{
						odc.GetColValue(1, rgCount, rgCount.Size,
							out cbSpaceTaken, out fIsNull, 0);
						uint[] uCnt = (uint[])MarshalEx.NativeToArray(rgCount, 1, typeof(uint));
						count = (int)uCnt[0];
					}
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			return count;
		}

		private static void LoadAllAgentCounts(FdoCache cache, Dictionary<int, int> values, int hvoAgent)
		{
			Dictionary<int, List<int>> dictRaw = new Dictionary<int, List<int>>();
			string qry = String.Format("SELECT wwa.src, cae.id FROM CmAgentEvaluation_ cae" +
				" JOIN WfiWordform_Analyses wwa ON wwa.dst = cae.target" +
				" WHERE cae.accepted = 1 AND cae.owner$ = {0} ORDER BY wwa.src", hvoAgent);
			DbOps.LoadDictionaryFromCommand(cache, qry, null, dictRaw);
			values.Clear();
			foreach (KeyValuePair<int, List<int>> kvp in dictRaw)
				values.Add(kvp.Key, kvp.Value.Count);
		}

		/// <summary>
		/// Get an agent's evaluation of this Wordform.
		/// </summary>
		/// <param name="hvoAgent"></param>
		/// <returns></returns>
		public ICmAgentEvaluation GetEvaluationFromAgent (int hvoAgent)
		{
			string query = string.Format("select id, class$ from CmAgentEvaluation_ " +
				"where target={0} and owner$={1}", m_hvo, hvoAgent);

			FdoObjectSet<ICmAgentEvaluation> evaluations = new FdoObjectSet<ICmAgentEvaluation>(m_cache, query, false);
			Debug.Assert( evaluations.Count < 2,  "program error: there should not be more than one evaluation on this Wordform from this agent.");
			return evaluations.FirstItem;


		}
		#endregion parsing related things

		#region loading things
		/// <summary>
		/// Loads the cache with all of the strings which will be needed by the ShortName
		/// property. For performance only.
		/// </summary>
		public static void PreLoadShortName(FdoCache cache, int ws)
		{
#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = System.Environment.TickCount;
#endif
			cache.LoadAllOfOneWsOfAMultiUnicode((int)WfiWordform.WfiWordformTags.kflidForm,
				"WfiWordform", ws);

			// Include other basic parts of WfiWordform
			// to avoid later cache misses when loading the objects.
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_Class, 0);
			dcs.Push((int)DbColType.koctObj, 1,
				(int)CmObjectFields.kflidCmObject_Owner, 0);
			dcs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_OwnFlid, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			cache.LoadData("select Id, Class$, Owner$, OwnFlid$, UpdStmp from CmObject where class$ = "
				+ WfiWordform.kClassId, dcs, 0);

#if DEBUG
			int tc2 = System.Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "Preloading for WfiWordform ShortNames took " + (tc2 - tc1) + " ticks," +
				" or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
			Debug.WriteLine(s);
#endif

			Marshal.ReleaseComObject(dcs);
		}
		#endregion loading things

		#region editing related things

		/// <summary>
		/// Side effects of deleting the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			WordformInventory.OnChangedWordformsOC();	// we deleted a wordform indirectly, so invalidate its cache.
			// Delete the annotations and evaluations that refer to objects in this wordform.
			List<LinkedObjectInfo> linkedObjs = LinkedObjects;
			foreach (LinkedObjectInfo loi in linkedObjs)
			{
				if (loi.RelObjClass == CmBaseAnnotation.kClassId || loi.RelObjClass == CmAnnotation.kClassId
					|| loi.RelObjClass == CmAgentEvaluation.kClassId)
					m_cache.DeleteObject(loi.RelObjId);
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Called by reflection when EnumComboSlice changes the spelling status.
		/// Eventually we might want to get it called for all ways of changing spell status.
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		public void SpellingStatusChanged(object oldValue, object newValue)
		{
			string text = Form.VernacularDefaultWritingSystem;
			if (string.IsNullOrEmpty(text))
				return; // no value in relevant WS to do anything about.
			EnchantHelper.SetSpellingStatus(text, m_cache.DefaultVernWs,
				m_cache.LanguageWritingSystemFactoryAccessor, (int)newValue == (int)SpellingStatusStates.correct);
		}

		/// <summary>
		/// Override default implementation to make a more suitable TS string for a wordform.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int annotationCount = this.OccurrencesInTexts.Count;
				//foreach (LinkedObjectInfo loi in LinkedObjects)
				//{
				//    if (loi.RelObjClass == CmAnnotation.kClassId)
				//        ++annotationCount;
				//}
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				tisb.AppendTsString(ShortNameTSS);

				if (annotationCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append("\x2028\x2028");
					if (annotationCount > 1)
						tisb.Append(String.Format(Strings.ksWordformUsedXTimes, annotationCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksWordformUsedOnce, "\x2028"));
				}
				return tisb.GetString();
			}
		}

		/// <summary>
		/// WfiWordforms are also used by TE ChkRefs, and should not be deleted if in use by one.
		/// </summary>
		/// <returns></returns>
		public override bool ValidateOkToDelete()
		{
			string sql = "select top 1 id from ChkRef where Rendering = " + Hvo;
			int hvoChkRef;
			if (DbOps.ReadOneIntFromCommand(m_cache, sql, null, out hvoChkRef))
			{
				ChkRef cr = CmObject.CreateFromDBObject(m_cache, hvoChkRef) as ChkRef;
				ChkTerm ct = cr.Owner as ChkTerm;
				string term = "";
				if (ct != null)
					term = ct.Name.BestAnalysisVernacularAlternative.Text;
				string msg = string.Format(Strings.ksWordformUsedByChkRef, term);
				MessageBox.Show(msg, Strings.ksErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Gets delete status for the object.
		/// True means it can be deleted, otherwise false.
		/// </summary>
		public override bool CanDelete
		{
			get
			{
				if (this.OccurrencesInTexts.Count == 0)
				{
					// This is overkill, but in some contexts (e.g. Texts/Edit/Interlinearize),
					// we may not have already haven't loaded the occurrences in text.
					// So, check the real occurrences, before we try to delete it.
					foreach (LinkedObjectInfo loi in LinkedObjects)
					{
						if (loi.RelObjClass == CmAnnotation.kClassId)
						{
							ICmAnnotation ca = CmAnnotation.CreateFromDBObject(m_cache, loi.RelObjId);
							if (ca is ICmBaseAnnotation && (ca as ICmBaseAnnotation).BeginObjectRAHvo != 0)
								return false;
						}
					}
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				int ws = m_cache.DefaultVernWs;
				ITsString tss = m_cache.MainCacheAccessor.get_MultiStringAlt(this.Hvo,
					(int)WfiWordform.WfiWordformTags.kflidForm, ws);
				if (tss.Length != 0)
					return tss;
				return m_cache.MakeUserTss(Strings.ksQuestions);	// was "??", not "???"
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

		#endregion editing related things

		#region IDummy Members

		/// <summary>
		///
		/// </summary>
		/// <param name="owningFlid"></param>
		/// <param name="hvoDummy"></param>
		/// <returns></returns>
		public ICmObject ConvertDummyToReal(int owningFlid, int hvoDummy)
		{
			using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(Cache, true))
			{
				ICmObject realObj = null;
				if (owningFlid == OccurrencesFlid(Cache))
				{
					realObj = CmBaseAnnotation.ConvertBaseAnnotationToReal(Cache, hvoDummy);
				}
				else
				{
					throw new Exception("The method or operation is not implemented.");
				}
				return realObj;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool OnPrepareToRefresh(object args)
		{
			return false;
		}

		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public partial class WfiGloss
	{
		#region Misc Properties

		/// <summary>
		/// Get the annotations that make for a concordance.
		/// </summary>
		public List<int> ConcordanceIds
		{
			get { return CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, m_hvo); }
		}

		/// <summary>
		/// Get the annotations that make for a full concordance at all three levels.
		/// </summary>
		public List<int> FullConcordanceIds
		{
			get { return ConcordanceIds; }
		}

		/// <summary>
		/// Override default implementation to make a more suitable TS string for a wordform.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int annotationCount = 0;
				foreach (LinkedObjectInfo loi in LinkedObjects)
				{
					if (loi.RelObjClass == CmAnnotation.kClassId)
						++annotationCount;
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
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get { return Form.BestAnalysisAlternative; }
		}

		/// <summary>
		/// The shortest, non abbreviated, label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

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
				// Check current WSes in the form.
				// If any are null or empty strings then return false.
				foreach (int ws in m_cache.LangProject.CurAnalysisWssRS.HvoArray)
				{
					string form = Form.GetAlternative(ws);
					if (form == null || form == String.Empty)
						return false;
				}

				return base.IsComplete;
			}
		}

		#endregion Misc Properties

		/// <summary>
		/// Move the text annotations that refence this object or any WfiGlosses it owns up to the owning WfiWordform.
		/// </summary>
		/// <remarks>
		/// Client is responsible for Undo/Redo wrapping.
		/// </remarks>
		public void MoveConcAnnotationsToWordform()
		{
			IWfiAnalysis owner = WfiAnalysis.CreateFromDBObject(m_cache, OwnerHVO);
			int wordformHvo = owner.OwnerHVO;
			foreach (int annHvo in FullConcordanceIds)
			{
				ICmAnnotation ann = CmAnnotation.CreateFromDBObject(m_cache, annHvo);
				ann.InstanceOfRAHvo = wordformHvo;
			}
		}

		/// <summary>
		/// Side effects of deleting the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			MoveConcAnnotationsToWordform();

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class WfiMorphBundle
	{
		/// <summary>
		/// Returns twfic annotations that have WfiMorphBundle reference(s) to this sense.
		/// Note: It's possible (though not probable) that we can return more than one instance
		/// to the same annotation, since a twfic can have multiple morphs pointing to the same sense.
		/// For now, we'll just let the client create a set of unique ids, if that's what they want.
		/// Otherwise, for duplicate ids, the client can have the option of looking into WfiAnalysis.MorphBundles
		/// for the relevant sense.
		/// </summary>
		/// <returns></returns>
		static public List<int> OccurrencesInTwfics(ILexSense ls)
		{
			string whereStmt = String.Format("where wmb.Sense = {0}", ls.Hvo);
			return OccurrencesInTwfics(ls.Cache, whereStmt);
		}

		static private List<int> OccurrencesInTwfics(FdoCache cache, string whereStatement)
		{
			string qry = String.Format("select cba.id from CmBaseAnnotation_ cba" +
				" left outer join WfiGloss_ wg on wg.id = cba.InstanceOf" +
				" join WfiAnalysis wa on wa.id=wg.Owner$ or wa.id = cba.InstanceOf" +
				" join WfiMorphBundle_ wmb on wmb.Owner$=wa.id" +
				" join StText_Paragraphs stp on stp.Dst=cba.BeginObject" +
				" join StTxtPara para on para.Id = stp.Dst" +
				" {0}" +
				" order by stp.Src, stp.Ord, cba.BeginObject, cba.BeginOffset, wmb.OwnOrd$", whereStatement);
			return DbOps.ReadIntsFromCommand(cache, qry, null);
		}

		/// <summary>
		/// Delete the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Trigger cleaning up any unused Msas after this object is deleted.
			this.HandleOldMSA();
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Get the HVO of the default sense, or zero, if none.
		/// </summary>
		public int DefaultSense
		{
			get
			{
				int virtFlid = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiMorphBundle", "DefaultSense");
				return m_cache.MainCacheAccessor.get_ObjectProp(Hvo, virtFlid);
			}
		}

		/// <summary>
		/// Override the base property to deal with deleting MSAs.
		/// </summary>
		public int MsaRAHvo
		{
			get
			{
				return MsaRAHvo_Generated;
			}
			set
			{
				if (MsaRAHvo != value)
				{
					// Only mess with it, if it is a different value
					// since HandleOldMSA() may delete it, which will cause a crash
					// when it tries to get set.
					HandleOldMSA();
					MsaRAHvo_Generated = value;
				}
			}
		}

		/// <summary>
		/// Override the base property to deal with deleting MSAs.
		/// </summary>
		public IMoMorphSynAnalysis MsaRA
		{
			get
			{
				return MsaRA_Generated;
			}
			set
			{
				int newHvo = (value == null) ? 0 : value.Hvo;
				if (MsaRAHvo != newHvo)
				{
					// Only mess with it, if it is a different value
					// since HandleOldMSA() may delete it, which will cause a crash
					// when it tries to get set.
					HandleOldMSA();
					MsaRA_Generated = value;
				}
			}
		}

		/// <summary>
		/// Delete original MSA, if permitted.
		/// </summary>
		private void HandleOldMSA()
		{
			IMoMorphSynAnalysis oldMsa = MsaRA_Generated;
			if (oldMsa != null)
			{
				MsaRA_Generated = null;
				if (oldMsa.CanDelete)
					oldMsa.DeleteUnderlyingObject();
			}
		}

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
				// Check the MSA for existance and completeness.
				if (MsaRA == null || !MsaRA.IsComplete)
					return false;

				// Check the form for existance and completeness.
				if (MorphRA == null || !MorphRA.IsComplete)
					return false;

				// Check the sense for existance and completeness.
				// Dont call the IsComplete property on the sense,
				// because it may be way pickier about what it means to be complete
				// than is required in this context.
				if (SenseRA == null)
					return false;

				// Check the analysis Wse for the sense gloss.
				foreach (int ws in m_cache.LangProject.CurAnalysisWssRS.HvoArray)
				{
					string form = SenseRA.Gloss.GetAlternative(ws);
					if (form == null || form == String.Empty)
						return false;
				}

				return base.IsComplete;
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class WfiWordSet
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wordSetName"></param>
		/// <param name="wordSetDescription"></param>
		/// <returns></returns>
		public static IWfiWordSet Create(FdoCache cache, string wordSetName, string wordSetDescription)
		{
			IWfiWordSet wordSet = cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(new WfiWordSet());
			wordSet.Name.AnalysisDefaultWritingSystem = wordSetName;
			//TODO: deal with duplicate wordSet names
			wordSet.Description.AnalysisDefaultWritingSystem.Text = wordSetDescription;
			//PopulateWordSet(paths, wordSet);
			return wordSet;
		}

		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)WfiWordSet.WfiWordSetTags.kflidCases:
					return m_cache.LangProject.WordformInventoryOA; // review JohnT: can we link to this?
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
				case (int)WfiWordSet.WfiWordSetTags.kflidCases:
					set = new Set<int>(m_cache.LangProject.WordformInventoryOA.WordformsOC.HvoArray);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of a Text.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				int ws = m_cache.DefaultUserWs;
				string name = Strings.ksUnnamed;

				if(Name != null)
				{
					if (Name.AnalysisDefaultWritingSystem != null
						&& Name.AnalysisDefaultWritingSystem != String.Empty)
					{
						ws = m_cache.DefaultAnalWs;
						name = Name.AnalysisDefaultWritingSystem;
					}
					else if (Name.VernacularDefaultWritingSystem != null
						&& Name.VernacularDefaultWritingSystem != String.Empty)
					{
						ws = m_cache.DefaultVernWs;
						name = Name.VernacularDefaultWritingSystem;
					}
				}
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(name, ws);
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class WfiAnalysis
	{
		#region Properties

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
				// If any text annotations reference this analysis, it is not complete.
				if (CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, m_hvo).Count > 0)
					return false;

				// Check approval status.
				// If the human has no opinion, then it is incomplete.
				int humanApprovalStatus = ApprovalStatusIcon;
				if (humanApprovalStatus == (int)Opinions.noopinion)
					return false;

				// Human approved but parser does not.
				int parserApprovalStatus = ParserStatusIcon;
				// NOTE: This assumes the user is using the parser,
				// which may be right. So, it is probably best to not enable it.
				//if ((humanApprovalStatus == (int)Opinions.approves) && (parserApprovalStatus != (int)Opinions.approves))
				//	return false;

				// Human dis-approved but parser approves.
				if ((humanApprovalStatus == (int)Opinions.disapproves) && (parserApprovalStatus == (int)Opinions.approves))
					return false;

				// Check word level category.
				if (CategoryRA == null)
					return false;

				// Check word level glosses.
				if (MeaningsOC.Count == 0)
					return false;
				foreach (IWfiGloss gloss in MeaningsOC)
				{
					if (!gloss.IsComplete)
						return false;
				}

				if (MorphBundlesOS.Count == 0)
					return false;
				foreach (IWfiMorphBundle mb in MorphBundlesOS)
				{
					if (!mb.IsComplete)
						return false;
				}

				return base.IsComplete;
			}
		}

		/// <summary>
		/// Get the annotations that make for a concordance.
		/// </summary>
		public List<int> ConcordanceIds
		{
			get { return CmBaseAnnotation.AnnotationsForInstanceOf(m_cache, m_hvo); }
		}

		/// <summary>
		/// Get the annotations that make for a full concordance at all three levels.
		/// </summary>
		public List<int> FullConcordanceIds
		{
			get
			{
				List<int> fullConc = ConcordanceIds;
				foreach (IWfiGloss gloss in MeaningsOC)
					fullConc.AddRange(gloss.FullConcordanceIds);
				return fullConc;
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
				int cnt = 0;
				int vernWs = m_cache.DefaultVernWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, vernWs);
				ITsString formSN;
				foreach (IWfiMorphBundle mb in MorphBundlesOS)
				{
					if (cnt++ > 0)
						tisb.Append(" ");
					IMoForm form = mb.MorphRA;
					if (form == null) // Some defective morph bundles don't have this property set.
					{
						formSN = mb.Form.VernacularDefaultWritingSystem.UnderlyingTsString;
						if (formSN != null)
							tisb.AppendTsString(formSN);
					}
					else
					{
						IMoMorphType type = form.MorphTypeRA;
						formSN = form.ShortNameTSS;
						if (formSN != null)
						{
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
				int annotationCount = 0;
				bool isParserApproved = false;
				foreach (LinkedObjectInfo loi in linkedObjects)
				{
					switch (loi.RelObjClass)
					{
						case CmAnnotation.kclsidCmAnnotation:
						{
							++annotationCount;
							break;
						}
						case CmAgentEvaluation.kclsidCmAgentEvaluation:
						{
							// See if the evaluation is from the parser,
							// since deleting it will only result in its return,
							// during the next run of the parser.
							ICmAgentEvaluation eval = CmAgentEvaluation.CreateFromDBObject(m_cache, loi.RelObjId);
							ICmAgent agent = CmAgent.CreateFromDBObject(m_cache, eval.OwnerHVO);
							if (!agent.Human && eval.Accepted && !isParserApproved)
								isParserApproved = true;
							break;
						}
					}
				}
				int cnt = 1;
				string warningMsg = String.Format(Strings.ksWarningDelAnalysis, "\x2028");
				bool wantMainWarningLine = true;
				if (annotationCount > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					tisb.Append(warningMsg);
					tisb.Append("\x2028");
					if (annotationCount > 1)
						tisb.Append(String.Format(Strings.ksDelAnalysisUsedXTimes, cnt++, annotationCount, "\x2028"));
					else
						tisb.Append(String.Format(Strings.ksDelAnalysisUsedOnce, cnt++, "\x2028"));
					wantMainWarningLine = false;
				}
				if (isParserApproved)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultUserWs);
					if (wantMainWarningLine)
						tisb.Append(warningMsg);
					tisb.Append("\x2028");
					tisb.Append(String.Format(Strings.ksDelParserAnalysis, cnt));
				}

				return tisb.GetString();
			}
		}

		/// <summary>
		/// relates the opinion of the current user agent to a 0,1, or 2 for use in drawing an icon.
		/// </summary>
		public int ApprovalStatusIcon
		{
			get
			{
				Opinions o = GetAgentOpinion(m_cache.LangProject.DefaultUserAgent);
				switch(o)
				{
					default:
						Debug.Fail("This code does not understand that opinion value.");
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
				Opinions[] values = {Opinions.noopinion, Opinions.approves, Opinions.disapproves};
				SetAgentOpinion(m_cache.LangProject.DefaultUserAgent, values[value]);

				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					OwnerHVO,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "UserCount"),
					0, 0, 0);
				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					OwnerHVO,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "ConflictCount"),
					0, 0, 0);
			}

		}

		/// <summary>
		/// relates the opinion of the current Parser agent to a 0,1, or 2 for use in drawing an icon.
		/// </summary>
		public int ParserStatusIcon
		{
			get
			{
				Opinions o = GetAgentOpinion(m_cache.LangProject.DefaultParserAgent);
				switch(o)
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

		#endregion Properties

		#region Other methods

		/// <summary>
		/// Move the text annotations that refence this object or any WfiGlosses it owns up to the owning WfiWordform.
		/// </summary>
		/// <remarks>
		/// Client is responsible for Undo/Redo wrapping.
		/// </remarks>
		public void MoveConcAnnotationsToWordform()
		{
			int ownerHvo = OwnerHVO;
			foreach (int annHvo in FullConcordanceIds)
			{
				ICmAnnotation ann = CmAnnotation.CreateFromDBObject(m_cache, annHvo);
				ann.InstanceOfRAHvo = ownerHvo;
			}
		}

		/// <summary>
		/// Delete the underlying object
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Shift annotation pointers up to wordform and delete evaluations that refer to objects in this analysis.
			MoveConcAnnotationsToWordform();

			int wordformId = OwnerHVO;
			List<LinkedObjectInfo> linkedObjs = LinkedObjects;
			Debug.WriteLine("Deleting WfiAnalysis: " + Hvo.ToString());
			foreach (LinkedObjectInfo loi in linkedObjs)
			{
				if (loi.RelObjClass == CmAnnotation.kClassId)
				{
					Debug.Assert(false, "Should now be taken care of by the 'MoveConcAnnotationsToWordform' method.");
				}
				else if (loi.RelObjClass == CmAgentEvaluation.kClassId)
				{
					// REVIEW (EberhardB):
					using (new IgnorePropChanged(m_cache, PropChangedHandling.SuppressView))
					{
						Debug.WriteLine("Delete eval using DeleteObject");
						objectsToDeleteAlso.Add(loi.RelObjId);
					}
				}
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Collect all the MSAs referenced under the given WfiAnalysis.
		/// </summary>
		/// <param name="msaHvoList">MSAs found by this call are appended to msaHvoList.</param>
		public void CollectReferencedMsaHvos(List<int> msaHvoList)
		{
			foreach (IWfiMorphBundle mb in MorphBundlesOS)
			{
				if (mb.MsaRAHvo != 0 && !msaHvoList.Contains(mb.MsaRAHvo))
					msaHvoList.Add(mb.MsaRAHvo);
				if (mb.SenseRA != null &&
					mb.SenseRA.MorphoSyntaxAnalysisRAHvo != 0 &&
					!msaHvoList.Contains(mb.SenseRA.MorphoSyntaxAnalysisRAHvo))
				{
					msaHvoList.Add(mb.SenseRA.MorphoSyntaxAnalysisRAHvo);
				}
			}
			foreach (ILexEntry le in StemsRC)
			{
				foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
				{
					if (msa.Hvo != 0 && !msaHvoList.Contains(msa.Hvo))
						msaHvoList.Add(msa.Hvo);
				}
			}
		}

		/// <summary>
		/// tells whether the given agent has approved or disapproved of this analysis, or has not given an opinion.
		/// </summary>
		/// <param name="agent"></param>
		/// <returns>one of the enumerated values in WfiAnalysis.Opinions.</returns>
		public Opinions GetAgentOpinion(ICmAgent agent)
		{
			int hvoEvaluation;
			bool wasAccepted;
			FindEvaluation(agent, out hvoEvaluation, out wasAccepted);

			if (0 ==hvoEvaluation)
				return Opinions.noopinion;
			else
				return wasAccepted ? Opinions.approves : Opinions.disapproves;
		}

		/// <summary>
		/// </summary>
		/// <param name="agent"></param>
		/// <param name="hvoEvaluation">will be zero if no evaluation was found</param>
		/// <param name="wasAccepted">false if no evaluation was found</param>
		protected void FindEvaluation(ICmAgent agent, out int hvoEvaluation, out bool wasAccepted)
		{
			IOleDbCommand odc = null;
			try
			{
				uint intSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
				Debug.Assert(m_cache != null); // The cache must be set.
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
				odc.SetParameter(1, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, (ushort)DBTYPEENUM.DBTYPE_I4, new uint[] { (uint)agent.Hvo }, intSize);
				odc.SetParameter(2, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, (ushort)DBTYPEENUM.DBTYPE_I4, new uint[] { (uint)Hvo }, intSize);
				// The SQL command must NOT modify the database contents!
				string sSql =
					"SELECT cae.id, cae.Accepted " +
					"FROM CmAgentEvaluation cae " +
					"JOIN CmObject co ON co.[Id] = cae.[Id] " +
					"AND co.[Owner$] = ? " +
					"WHERE cae.[Target] = ?";
				odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				bool fIsNull;
				bool fMoreRows;
				uint cbSpaceTaken;
				odc.GetRowset(0);
				odc.NextRow(out fMoreRows);
				if (fMoreRows)
				{
					using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
					{
						odc.GetColValue(1, rgHvo, rgHvo.Size,
							out cbSpaceTaken, out fIsNull, 0);
						uint[] uHvo = (uint[])MarshalEx.NativeToArray(rgHvo, 1, typeof(uint));
						hvoEvaluation = (int)uHvo[0];
						// Note, using bool or ushort instead of uint will randomly fail. Accepted is a bit in
						// SQL Server and comes across as 2 bytes with the top bytes having random data.
						using (ArrayPtr rgAccepted = MarshalEx.ArrayToNative(1, typeof(uint)))
						{
							odc.GetColValue(2, rgAccepted, rgAccepted.Size,
								out cbSpaceTaken, out fIsNull, 0);
							uint[] uAccepted = (uint[])MarshalEx.NativeToArray(rgAccepted, 1, typeof(uint));
							int nAccepted = (int)uAccepted[0];
							wasAccepted = (nAccepted & 0xFFFF) != 0;
						}
					}
				}
				else
				{
					hvoEvaluation = 0;
					wasAccepted = false;
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
		}

		/// <summary>
		/// Tells whether the giving agent has approved or disapproved of this analysis, or has not given an opinion.
		/// </summary>
		/// <param name="agent"></param>
		/// <param name="opinion"></param>
		/// <returns>one of the enumerated values in Opinions.</returns>
		public void SetAgentOpinion(ICmAgent agent, Opinions opinion)
		{
			int wasAccepted = 0;
			//now set the opinion to what it should be
			switch(opinion)
			{
				case Opinions.approves:
					wasAccepted = 1;
					break;
				case Opinions.disapproves:
					wasAccepted = 0;
					break;
				case Opinions.noopinion:
					wasAccepted = 2;
					break;
			}

			agent.SetEvaluation(Hvo, wasAccepted, "");
		}

		/// <summary>
		/// Finds the wfiAnalysis the given wfic cba.InstanceOf. If it's an InstanceOf a WfiWordform,
		/// it'll return the wfiAnalysis of the guess, if found, otherwise it'll return the wfiWordform's first analysis.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoCba"></param>
		/// <returns></returns>
		public static int GetWfiAnalysisFromInstanceOf(FdoCache cache, int hvoCba)
		{
			int cbaInstanceOf = cache.GetObjProperty(hvoCba, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
			Debug.Assert(cbaInstanceOf != 0);
			int hvoWordform;
			int hvoWfiAnalysis = GetWfiAnalysisFromWficInstanceOf(cache, cbaInstanceOf, out hvoWordform);
			if (hvoWfiAnalysis == 0 && hvoWordform != 0)
			{
				// we are probably an instance of a wordform. Try to use a guess or else use first analysis.
				int[] analyses = cache.GetVectorProperty(hvoWordform, (int)WfiWordform.WfiWordformTags.kflidAnalyses, false);
				if (analyses.Length > 0)
				{
					// first see if we have a guess we can use.
					int ktagTwficDefault = StTxtPara.TwficDefaultFlid(cache);
					int hvoTwficAnalysisGuess = cache.GetObjProperty(hvoCba, ktagTwficDefault);
					if (hvoTwficAnalysisGuess != 0)
						hvoWfiAnalysis = GetWfiAnalysisFromWficInstanceOf(cache, hvoTwficAnalysisGuess, out hvoWordform);
					if (hvoWfiAnalysis == 0)
					{
						if (analyses.Length > 1)
						{
							throw new ArgumentException(
								"Couldn't find a guess for a twfic with multiple analyses. Not sure which analysis to return.");
						}
						// since we have more than one Analyses, just return the first one.
						hvoWfiAnalysis = (int) analyses[0];
					}
				}
			}
			return hvoWfiAnalysis;
		}

		private static int GetWfiAnalysisFromWficInstanceOf(FdoCache cache, int hvoInstanceOf, out int hvoWordform)
		{
			hvoWordform = 0;
			int hvoWfiAnalysis = 0;
			int classid = cache.GetClassOfObject(hvoInstanceOf);
			switch (classid)
			{
				case WfiWordform.kclsidWfiWordform:
					hvoWordform = hvoInstanceOf;
					return 0;  // need use or make a guess for analysis
				case WfiAnalysis.kclsidWfiAnalysis:
					hvoWfiAnalysis = hvoInstanceOf;
					break;
				case WfiGloss.kclsidWfiGloss:
					hvoWfiAnalysis = cache.GetOwnerOfObject(hvoInstanceOf);
					break;
				default:
					throw new ArgumentException("cba.InstanceOf(" + hvoInstanceOf + ") class(" +
							   classid + ") is not WfiWordform, WfiAnalysis, or WfiGloss.");
			}
			return hvoWfiAnalysis;
		}

		#endregion Other methods
	}


	#endregion // WFI

	#region Text
	/// <summary>
	/// </summary>
	public partial class Text
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
				int ws = m_cache.DefaultUserWs;
				string name = Strings.ksUntitled;

				if(Name != null)
				{
					if (Name.VernacularDefaultWritingSystem != null
						&& Name.VernacularDefaultWritingSystem != String.Empty)
					{
						ws = m_cache.DefaultVernWs;
						name = Name.VernacularDefaultWritingSystem;
					}
					else if (Name.AnalysisDefaultWritingSystem != null
						&& Name.AnalysisDefaultWritingSystem != String.Empty)
					{
						ws = m_cache.DefaultAnalWs;
						name = Name.AnalysisDefaultWritingSystem;
					}
				}
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(name, ws);
			}
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)Text.TextTags.kflidGenres:
					return Cache.LangProject.GenreListOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Delete the underlying Text object.
		/// </summary>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Delete the annotations that refer to objects in this text.
			List<LinkedObjectInfo> linkedObjs = LinkedObjects;
			foreach (LinkedObjectInfo loi in linkedObjs)
			{
				if (loi.RelObjClass == CmBaseAnnotation.kClassId)
					objectsToDeleteAlso.Add(loi.RelObjId);
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}
	#endregion // Texts
}
