using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class MorphTypeAtomicLauncher : AtomicReferenceLauncher
	{
		private System.ComponentModel.IContainer components = null;
		const string m_ksPath = "/group[@id='DialogStrings']/";

		public MorphTypeAtomicLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		/// <summary>
		/// Get the SimpleListChooser/
		/// </summary>
		/// <param name="labels">List of objects to show in the chooser.</param>
		/// <returns>The SimpleListChooser.</returns>
		protected new MorphTypeChooser GetChooser(ObjectLabelCollection labels)
		{
			string sShowAllTypes = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeShowAllTypes", m_ksPath);
			MorphTypeChooser x = new MorphTypeChooser(m_persistProvider, labels, m_fieldName, m_obj, m_displayNameProperty,
				m_flid, sShowAllTypes);
			x.Cache = m_cache;
			x.NullLabel.DisplayName  = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "nullLabel", "<EMPTY>");
			return x;
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		protected override void HandleChooser()
		{
			string displayWs = "analysis vernacular";
			string postDialogMessageTrigger = null;

			if (m_configurationNode != null)
			{
				XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				if (node != null)
				{
					displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
					postDialogMessageTrigger = XmlUtils.GetAttributeValue(node, "postChangeMessageTrigger", null);
				}
			}
			Set<int> candidates = m_obj.ReferenceTargetCandidates(m_flid);
			ObjectLabelCollection labels = new ObjectLabelCollection(m_cache, candidates,
				m_displayNameProperty, displayWs);

			using (MorphTypeChooser chooser = GetChooser(labels))
			{
				bool fMadeMorphTypeChange = false;
				ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, m_obj.OwnerHVO);
				chooser.InitializeExtras(m_configurationNode, Mediator);
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);
				int hvoType = m_cache.GetObjProperty(m_obj.Hvo, m_flid);
				chooser.MakeSelection(hvoType);
				// LT-4433 changed the Alternate Forms to choose between Stem and Affix automatically
				// when inserting.  Thus, we need the check box in that environment as well.
				//if (m_obj.OwningFlid != (int)LexEntry.LexEntryTags.kflidLexemeForm)
				//    chooser.ShowAllTypesCheckBoxVisible = false;
				if (chooser.ShowDialog() == DialogResult.OK)
				{
					ObjectLabel selected = chooser.ChosenOne;
					int hvoOriginal = TargetHvo;
					string sUndo = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeUndo", m_ksPath);
					string sRedo = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeRedo", m_ksPath);

					bool fRemoveComponents = false;
					if (selected.Hvo == entry.Cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphRoot))
						|| selected.Hvo == entry.Cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphBoundRoot)))
					{
						// changing to root...not allowed to have complex forms.
						foreach (LexEntryRef ler in entry.EntryRefsOS)
						{
							if (ler.RefType == LexEntryRef.krtComplexForm)
							{
								fRemoveComponents = true;
								// If there are no components we will delete without asking...but must then check for more
								// complex forms that DO have components.
								if (ler.ComponentLexemesRS.Count > 0)
								{
									if (MessageBox.Show(FindForm(), DetailControlsStrings.ksRootNoComponentsMessage,
										DetailControlsStrings.ksRootNoComponentsCaption, MessageBoxButtons.YesNo,
										MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, 0, FwApp.App.HelpFile,
										HelpNavigator.Topic, "khtRootCannotHaveComponents") != DialogResult.Yes)
									{
										return;
									}
									break;
								}
							}
						}
					}

					using (new UndoRedoTaskHelper(entry.Cache, sUndo, sRedo))
					{
						if (fRemoveComponents)
						{
							Set<int> delObjs = new Set<int>();
							foreach (LexEntryRef ler in entry.EntryRefsOS)
							{
								if (ler.RefType == LexEntryRef.krtComplexForm)
									delObjs.Add(ler.Hvo);
							}
							CmObject.DeleteObjects(delObjs, m_cache);
						}

						if (IsStemType(hvoOriginal) || m_obj is MoStemAllomorph)
						{
							if (IsStemType(selected.Hvo))
							{
								TargetHvo = selected.Hvo;
							}
							else
							{
								//have to switch from stem to affix
								fMadeMorphTypeChange = ChangeStemToAffix(entry, selected.Hvo, sUndo, sRedo);
							}
						}
						else
						{
							// original is affix variety
							if (IsStemType(selected.Hvo))
							{
								//have to switch from affix to stem
								fMadeMorphTypeChange = ChangeAffixToStem(entry, selected.Hvo, sUndo, sRedo);
							}
							else
							{
								TargetHvo = selected.Hvo;
							}
						}
						if (selected.Hvo == entry.Cache.GetIdFromGuid(new Guid(MoMorphType.kguidMorphPhrase)))
						{
							ILexEntryRef ler = new LexEntryRef();
							entry.EntryRefsOS.Append(ler);
							ler.RefType = LexEntryRef.krtComplexForm;
							ler.HideMinorEntry = 1;
							// No automatic propchanged for new objects, need to let the view see it.
							// At that point our slice will be disposed, so don't do anything after this.
							entry.Cache.PropChanged(entry.Hvo, (int) LexEntry.LexEntryTags.kflidEntryRefs, 0, 1, 0);
						}
					}
				}
			}
		}

		/// <summary>
		/// Change the affix to a stem (possibly)
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="selectedHvo"></param>
		/// <param name="sUndo"></param>
		/// <param name="sRedo"></param>
		/// <returns>true if change made; false otherwise</returns>
		private bool ChangeAffixToStem(ILexEntry entry, int selectedHvo, string sUndo, string sRedo)
		{
			IMoAffixForm affix = m_obj as IMoAffixForm;
			if (affix == null)
				throw new ApplicationException("Affix form is not defined");
			List<IMoMorphSynAnalysis> rgmsaOld = new List<IMoMorphSynAnalysis>();
			if (m_obj.OwningFlid == (int)LexEntry.LexEntryTags.kflidLexemeForm)
			{
				foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
				{
					if (!(msa is IMoStemMsa))
						rgmsaOld.Add(msa);
				}
			}
			if (CheckForAffixDataLoss(affix, rgmsaOld))
				return false;
			FdoCache cache = m_cache;
			cache.BeginUndoTask(sUndo, sRedo);
			IMoStemAllomorph stem = new MoStemAllomorph();
			SwapValues(entry, affix, stem, selectedHvo, rgmsaOld);	// may cause slice/button to be disposed...
			cache.EndUndoTask();
			return true;
		}

		private bool CheckForAffixDataLoss(IMoAffixForm affix, List<IMoMorphSynAnalysis> rgmsaAffix)
		{
			bool fLoseInflCls = affix.InflectionClassesRC.Count > 0;
			bool fLoseInfixLoc = false;
			bool fLoseGramInfo = false;
			bool fLoseRule = false;
			switch (affix.ClassID)
			{
				case MoAffixProcess.kclsidMoAffixProcess:
					fLoseRule = true;
					break;

				case MoAffixAllomorph.kclsidMoAffixAllomorph:
					IMoAffixAllomorph allo = affix as IMoAffixAllomorph;
					fLoseInfixLoc = allo.PositionRS.Count > 0;
					fLoseGramInfo = allo.MsEnvPartOfSpeechRAHvo != 0 || allo.MsEnvFeaturesOAHvo != 0;
					break;
			}

			for (int i = 0; !fLoseGramInfo && i < rgmsaAffix.Count; ++i)
			{
				IMoInflAffMsa msaInfl = rgmsaAffix[i] as IMoInflAffMsa;
				if (msaInfl != null)
				{
					if (msaInfl.AffixCategoryRAHvo != 0 ||
						msaInfl.FromProdRestrictRC.Count > 0 ||
						msaInfl.SlotsRC.Count > 0 ||
						msaInfl.InflFeatsOAHvo != 0)
					{
						fLoseGramInfo = true;
					}
					continue;
				}
				IMoDerivAffMsa msaDeriv = rgmsaAffix[i] as IMoDerivAffMsa;
				if (msaDeriv != null)
				{
					if (msaDeriv.AffixCategoryRAHvo != 0 ||
						msaDeriv.FromInflectionClassRAHvo != 0 ||
						msaDeriv.FromPartOfSpeechRAHvo != 0 ||
						msaDeriv.FromProdRestrictRC.Count > 0 ||
						msaDeriv.FromStemNameRAHvo != 0 ||
						msaDeriv.StratumRAHvo != 0 ||
						msaDeriv.ToInflectionClassRAHvo != 0 ||
						msaDeriv.ToProdRestrictRC.Count > 0 ||
						msaDeriv.FromMsFeaturesOAHvo != 0 ||
						msaDeriv.ToMsFeaturesOAHvo != 0)
					{
						fLoseGramInfo = true;
					}
					continue;
				}
				IMoDerivStepMsa msaStep = rgmsaAffix[i] as IMoDerivStepMsa;
				if (msaStep != null)
				{
					if (msaStep.InflectionClassRAHvo != 0 ||
						msaStep.ProdRestrictRC.Count > 0 ||
						msaStep.InflFeatsOAHvo != 0 ||
						msaStep.MsFeaturesOAHvo != 0)
					{
						fLoseGramInfo = true;
					}
					continue;
				}
			}
			if (fLoseInflCls || fLoseInfixLoc || fLoseGramInfo || fLoseRule)
			{
				string sMsg;
				if (fLoseInflCls && fLoseInfixLoc && fLoseGramInfo)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseInflClsInfixLocGramInfo", m_ksPath);
				else if (fLoseRule && fLoseInflCls && fLoseGramInfo)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseRuleInflClsGramInfo", m_ksPath);
				else if (fLoseInflCls && fLoseInfixLoc)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseInflClsInfixLoc", m_ksPath);
				else if (fLoseInflCls && fLoseGramInfo)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseInflClsGramInfo", m_ksPath);
				else if (fLoseInfixLoc && fLoseGramInfo)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseInfixLocGramInfo", m_ksPath);
				else if (fLoseRule && fLoseInflCls)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseRuleInflCls", m_ksPath);
				else if (fLoseRule)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseRule", m_ksPath);
				else if (fLoseInflCls)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseInflCls", m_ksPath);
				else if (fLoseInfixLoc)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseInfixLoc", m_ksPath);
				else
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseGramInfo", m_ksPath);
				string sCaption = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeCaption", m_ksPath);
				DialogResult result = MessageBox.Show(sMsg, sCaption,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				if (result == DialogResult.No)
				{
					return true;
				}
			}
			return false;
		}

		private bool ChangeStemToAffix(ILexEntry entry, int selectedHvo, string sUndo, string sRedo)
		{
			IMoStemAllomorph stem = m_obj as IMoStemAllomorph;
			if (stem == null)
				throw new ApplicationException("Stem allomorph is not defined");
			List<IMoMorphSynAnalysis> rgmsaOld = new List<IMoMorphSynAnalysis>();
			if (m_obj.OwningFlid == (int)LexEntry.LexEntryTags.kflidLexemeForm)
			{
				foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
				{
					if (msa is IMoStemMsa)
						rgmsaOld.Add(msa);
				}
			}
			if (CheckForStemDataLoss(stem, rgmsaOld))
				return false;
			FdoCache cache = m_cache;
			cache.BeginUndoTask(sUndo, sRedo);
			IMoAffixAllomorph affix = new MoAffixAllomorph();
			SwapValues(entry, stem, affix, selectedHvo, rgmsaOld);
			cache.EndUndoTask();
			return true;
		}

		private bool CheckForStemDataLoss(IMoStemAllomorph stem, List<IMoMorphSynAnalysis> rgmsaStem)
		{
			bool fLoseStemName = stem.StemNameRAHvo != 0;
			bool fLoseGramInfo = false;
			for (int i = 0; i < rgmsaStem.Count; ++i)
			{
				IMoStemMsa msa = rgmsaStem[i] as IMoStemMsa;
				if (msa != null &&
					msa.FromPartsOfSpeechRC.Count > 0 ||
					msa.InflectionClassRAHvo != 0 ||
					msa.ProdRestrictRC.Count > 0 ||
					msa.StratumRAHvo != 0 ||
					msa.MsFeaturesOAHvo != 0)
				{
					fLoseGramInfo = true;
					break;
				}
			}
			if (fLoseStemName || fLoseGramInfo)
			{
				string sMsg;
				if (fLoseStemName && fLoseGramInfo)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseStemNameGramInfo", m_ksPath);
				else if (fLoseStemName)
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseStemName", m_ksPath);
				else
					sMsg = m_mediator.StringTbl.GetStringWithXPath("ChangeMorphTypeLoseGramInfo", m_ksPath);
				string sCaption = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeCaption", m_ksPath);
				DialogResult result = MessageBox.Show(sMsg, sCaption,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				if (result == DialogResult.No)
				{
					return true;
				}
			}
			return false;
		}

		private void SwapValues(ILexEntry entry, IMoForm origForm, IMoForm newForm, int typeHvo,
			List<IMoMorphSynAnalysis> rgmsaOld)
		{
			DataTree dtree = this.Slice.ContainingDataTree;
			int idx = this.Slice.IndexInContainer;
			dtree.DoNotRefresh = true;	// don't let the datatree repeatedly redraw itself...
			Debug.Assert(entry is LexEntry);
			(entry as LexEntry).ReplaceMoForm(origForm, newForm);
			newForm.MorphTypeRAHvo = typeHvo;
			(entry as LexEntry).ReplaceObsoleteMsas(rgmsaOld);
			// Dispose of any obsolete slices: new ones will replace them automatically in a moment
			// when the datatree is redrawn.
			foreach (Slice slice in this.Slice.ContainingDataTree.Controls)
			{
				if (slice.IsDisposed)
					continue;
				if (slice.Object is IMoMorphSynAnalysis && rgmsaOld.Contains(slice.Object as IMoMorphSynAnalysis))
					slice.Dispose();
				else if (slice is MSAReferenceComboBoxSlice)
					slice.Dispose();
			}
			// now fix the record list, since it may be showing MoForm dependent columns (e.g. MorphType, Homograph, etc...)
			dtree.FixRecordList();
			dtree.DoNotRefresh = false;
			Slice sliceT = dtree.Controls[idx] as Slice;
			if (sliceT != null && sliceT is MorphTypeAtomicReferenceSlice)
			{
				// When the new slice is created, the launch button is placed in the middle of
				// the slice rather than at the end.  This fiddling with the slice width seems
				// to fix that.  Then setting the index restores focus to the new slice.
				sliceT.Width += 1;
				sliceT.Width -= 1;
				dtree.GotoNextSliceAfterIndex(idx - 1);
			}
		}

		private bool IsStemType(int hvo)
		{
			MoMorphTypeCollection types = new MoMorphTypeCollection(m_cache);
			if ((hvo == types.Item(MoMorphType.kmtBoundRoot).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtBoundStem).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtEnclitic).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtParticle).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtProclitic).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtRoot).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtStem).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtClitic).Hvo) ||
				// Andy: no! circumfixes are affixes, not stems: (hvo == types.Item(MoMorphType.kmtCircumfix).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtPhrase).Hvo) ||
				(hvo == types.Item(MoMorphType.kmtDiscontiguousPhrase).Hvo) )
				return true;
			return false;
		}
		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
