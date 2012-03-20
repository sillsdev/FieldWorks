using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Xml;
using System.Linq;

using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

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
		protected new MorphTypeChooser GetChooser(IEnumerable<ObjectLabel> labels)
		{
			string sShowAllTypes = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeShowAllTypes", m_ksPath);
			MorphTypeChooser x = new MorphTypeChooser(m_persistProvider, labels, m_fieldName, m_obj, m_displayNameProperty,
				m_flid, sShowAllTypes, m_mediator.HelpTopicProvider);
			x.Cache = m_cache;
			x.NullLabel.DisplayName  = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "nullLabel", "<EMPTY>");
			return x;
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		protected override void HandleChooser()
		{
			string displayWs = "analysis vernacular";
#pragma warning disable 219
			string postDialogMessageTrigger = null;
#pragma warning restore 219

			if (m_configurationNode != null)
			{
				XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				if (node != null)
				{
					displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
					postDialogMessageTrigger = XmlUtils.GetAttributeValue(node, "postChangeMessageTrigger", null);
				}
			}

			var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid),
				m_displayNameProperty, displayWs);

			using (MorphTypeChooser chooser = GetChooser(labels))
			{
				bool fMadeMorphTypeChange = false;
				ILexEntry entry = m_obj.Owner as ILexEntry;
				chooser.InitializeExtras(m_configurationNode, Mediator);
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);
				chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());

				var hvoType = m_cache.DomainDataByFlid.get_ObjectProp(m_obj.Hvo, m_flid);
				var morphTypeRep = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				var type = hvoType != 0 ? morphTypeRep.GetObject(hvoType) : null;
				chooser.MakeSelection(type);
				// LT-4433 changed the Alternate Forms to choose between Stem and Affix automatically
				// when inserting.  Thus, we need the check box in that environment as well.
				//if (m_obj.OwningFlid != (int)LexEntry.LexEntryTags.kflidLexemeForm)
				//    chooser.ShowAllTypesCheckBoxVisible = false;
				if (chooser.ShowDialog() == DialogResult.OK)
				{
					var selected = chooser.ChosenOne.Object as IMoMorphType;
					var original = Target as IMoMorphType;
					string sUndo = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeUndo", m_ksPath);
					string sRedo = m_mediator.StringTbl.GetStringWithXPath("ChangeLexemeMorphTypeRedo", m_ksPath);

					bool fRemoveComponents = false;
					if (selected.Guid == MoMorphTypeTags.kguidMorphRoot
						|| selected.Guid == MoMorphTypeTags.kguidMorphBoundRoot)
					{
						// changing to root...not allowed to have complex forms.
						foreach (ILexEntryRef ler in entry.EntryRefsOS)
						{
							if (ler.RefType == LexEntryRefTags.krtComplexForm)
							{
								fRemoveComponents = true;
								// If there are no components we will delete without asking...but must then check for more
								// complex forms that DO have components.
								if (ler.ComponentLexemesRS.Count > 0)
								{
									// TODO-Linux: Help is not implemented in Mono
									if (MessageBox.Show(FindForm(), DetailControlsStrings.ksRootNoComponentsMessage,
										DetailControlsStrings.ksRootNoComponentsCaption, MessageBoxButtons.YesNo,
										MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, 0, m_mediator.HelpTopicProvider.HelpFile,
										HelpNavigator.Topic, "khtRootCannotHaveComponents") != DialogResult.Yes)
									{
										return;
									}
									break;
								}
							}
						}
					}

					UndoableUnitOfWorkHelper.Do(sUndo, sRedo, entry, () =>
					{
						if (fRemoveComponents)
						{
							foreach (var ler in entry.EntryRefsOS.Where(entryRef => entryRef.RefType == LexEntryRefTags.krtComplexForm))
								entry.EntryRefsOS.Remove(ler);
						}

						if (IsStemType(original) || m_obj is IMoStemAllomorph)
						{
							if (IsStemType(selected))
							{
								Target = selected;
							}
							else
							{
								//have to switch from stem to affix
								fMadeMorphTypeChange = ChangeStemToAffix(entry, selected, sUndo, sRedo);
							}
						}
						else
						{
							// original is affix variety
							if (IsStemType(selected))
							{
								//have to switch from affix to stem
								fMadeMorphTypeChange = ChangeAffixToStem(entry, selected, sUndo, sRedo);
							}
							else
							{
								Target = selected;
							}
						}
						if (selected.Guid == MoMorphTypeTags.kguidMorphPhrase)
						{
							ILexEntryRef ler = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
							entry.EntryRefsOS.Add(ler);
							ler.RefType = LexEntryRefTags.krtComplexForm;
							ler.HideMinorEntry = 1;
						}
					});
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
		private bool ChangeAffixToStem(ILexEntry entry, IMoMorphType type, string sUndo, string sRedo)
		{
			var affix = m_obj as IMoAffixForm;
			if (affix == null)
				throw new ApplicationException("Affix form is not defined");
			var rgmsaOld = new List<IMoMorphSynAnalysis>();
			if (m_obj.OwningFlid == LexEntryTags.kflidLexemeForm)
			{
				foreach (var msa in entry.MorphoSyntaxAnalysesOC)
				{
					if (!(msa is IMoStemMsa))
						rgmsaOld.Add(msa);
				}
			}
			if (CheckForAffixDataLoss(affix, rgmsaOld))
				return false;
			FdoCache cache = m_cache;
			var stem = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			SwapValues(entry, affix, stem, type, rgmsaOld);	// may cause slice/button to be disposed...
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
				case MoAffixProcessTags.kClassId:
					fLoseRule = true;
					break;

				case MoAffixAllomorphTags.kClassId:
					var allo = affix as IMoAffixAllomorph;
					fLoseInfixLoc = allo.PositionRS.Count > 0;
					fLoseGramInfo = allo.MsEnvPartOfSpeechRA != null || allo.MsEnvFeaturesOA != null;
					break;
			}

			for (int i = 0; !fLoseGramInfo && i < rgmsaAffix.Count; ++i)
			{
				var msaInfl = rgmsaAffix[i] as IMoInflAffMsa;
				if (msaInfl != null)
				{
					if (msaInfl.AffixCategoryRA != null ||
						msaInfl.FromProdRestrictRC.Count > 0 ||
						msaInfl.SlotsRC.Count > 0 ||
						msaInfl.InflFeatsOA != null)
					{
						fLoseGramInfo = true;
					}
					continue;
				}
				var msaDeriv = rgmsaAffix[i] as IMoDerivAffMsa;
				if (msaDeriv != null)
				{
					if (msaDeriv.AffixCategoryRA != null ||
						msaDeriv.FromInflectionClassRA != null ||
						msaDeriv.FromPartOfSpeechRA != null ||
						msaDeriv.FromProdRestrictRC.Count > 0 ||
						msaDeriv.FromStemNameRA != null ||
						msaDeriv.StratumRA != null ||
						msaDeriv.ToInflectionClassRA != null ||
						msaDeriv.ToProdRestrictRC.Count > 0 ||
						msaDeriv.FromMsFeaturesOA != null ||
						msaDeriv.ToMsFeaturesOA != null)
					{
						fLoseGramInfo = true;
					}
					continue;
				}
				var msaStep = rgmsaAffix[i] as IMoDerivStepMsa;
				if (msaStep != null)
				{
					if (msaStep.InflectionClassRA != null ||
						msaStep.ProdRestrictRC.Count > 0 ||
						msaStep.InflFeatsOA != null ||
						msaStep.MsFeaturesOA != null)
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

		private bool ChangeStemToAffix(ILexEntry entry, IMoMorphType type, string sUndo, string sRedo)
		{
			var stem = m_obj as IMoStemAllomorph;
			if (stem == null)
				throw new ApplicationException("Stem allomorph is not defined");
			var rgmsaOld = new List<IMoMorphSynAnalysis>();
			if (m_obj.OwningFlid == LexEntryTags.kflidLexemeForm)
			{
				foreach (var msa in entry.MorphoSyntaxAnalysesOC)
				{
					if (msa is IMoStemMsa)
						rgmsaOld.Add(msa);
				}
			}
			if (CheckForStemDataLoss(stem, rgmsaOld))
				return false;
			FdoCache cache = m_cache;
			var affix = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			SwapValues(entry, stem, affix, type, rgmsaOld);
			return true;
		}

		private bool CheckForStemDataLoss(IMoStemAllomorph stem, List<IMoMorphSynAnalysis> rgmsaStem)
		{
			bool fLoseStemName = stem.StemNameRA != null;
			bool fLoseGramInfo = false;
			for (int i = 0; i < rgmsaStem.Count; ++i)
			{
				var msa = rgmsaStem[i] as IMoStemMsa;
				if (msa != null &&
					msa.FromPartsOfSpeechRC.Count > 0 ||
					msa.InflectionClassRA != null ||
					msa.ProdRestrictRC.Count > 0 ||
					msa.StratumRA != null ||
					msa.MsFeaturesOA != null)
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

		private void SwapValues(ILexEntry entry, IMoForm origForm, IMoForm newForm, IMoMorphType type,
			List<IMoMorphSynAnalysis> rgmsaOld)
		{
			DataTree dtree = this.Slice.ContainingDataTree;
			int idx = this.Slice.IndexInContainer;
			dtree.DoNotRefresh = true;	// don't let the datatree repeatedly redraw itself...
			entry.ReplaceMoForm(origForm, newForm);
			newForm.MorphTypeRA = type;
			entry.ReplaceObsoleteMsas(rgmsaOld);
			// Dispose of any obsolete slices: new ones will replace them automatically in a moment
			// when the datatree is redrawn.
			foreach (Slice slice in Slice.ContainingDataTree.Slices.ToArray())
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
			Slice sliceT = dtree.Slices[idx] as Slice;
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

		private bool IsStemType(IMoMorphType type)
		{
			if (type == null)
				return false;

			if ((type.Guid == MoMorphTypeTags.kguidMorphBoundRoot) ||
				(type.Guid == MoMorphTypeTags.kguidMorphBoundStem) ||
				(type.Guid == MoMorphTypeTags.kguidMorphEnclitic) ||
				(type.Guid == MoMorphTypeTags.kguidMorphParticle) ||
				(type.Guid == MoMorphTypeTags.kguidMorphProclitic) ||
				(type.Guid == MoMorphTypeTags.kguidMorphRoot) ||
				(type.Guid == MoMorphTypeTags.kguidMorphStem) ||
				(type.Guid == MoMorphTypeTags.kguidMorphClitic) ||
				(type.Guid == MoMorphTypeTags.kguidMorphPhrase) ||
				(type.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase))
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
