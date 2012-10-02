// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InflAffixTemplateControl.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for InflAffixTemplateControl.
	/// </summary>
	public class InflAffixTemplateControl : XmlView, XCore.IxCoreColleague
	{
		int m_hvo = 0;		// hvo of item clicked
		int m_class = 0;    // class id of item clicked
		int m_hvoSlot = 0;		// hvo of slot to which chosen MSA belongs
		IMoInflAffixTemplate m_template = null;
		string m_sStem;
		string m_sSlotChooserTitle;
		string m_sSlotChooserInstructionalText;
		string m_sObligatorySlot;
		string m_sOptionalSlot;
		string m_sNewSlotName;
		string m_sUnnamedSlotName;
		string m_sInflAffixChooserTitle;
		string m_sInflAffixChooserInstructionalTextReq;
		string m_sInflAffixChooserInstructionalTextOpt;
		string m_sInflAffix;
		string m_ChooseInflectionalAffixHelpTopic = "InflectionalAffixes";
		string m_ChooseSlotHelpTopic = "Slot";

		protected event InflAffixTemplateEventHandler ShowContextMenu;

		public InflAffixTemplateControl(FdoCache cache, int hvoRoot, XmlNode xnSpec, StringTable stringTable)
			: base(hvoRoot, XmlUtils.GetAttributeValue(xnSpec, "layout"), stringTable, true)
		{
			m_xnSpec = xnSpec["deParams"];
			Cache = cache;
			m_template = new MoInflAffixTemplate(cache, m_hvoRoot);
		}

		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
			}

			m_template = null;
			m_sStem = null;
			m_sSlotChooserTitle = null;
			m_sSlotChooserInstructionalText = null;
			m_sObligatorySlot = null;
			m_sOptionalSlot = null;
			m_sNewSlotName = null;
			m_sUnnamedSlotName = null;
			m_sInflAffixChooserTitle = null;
			m_sInflAffixChooserInstructionalTextReq = null;
			m_sInflAffixChooserInstructionalTextOpt = null;
			m_sInflAffix = null;
		}


		public void SetStringTableValues(StringTable stringTable)
		{
			CheckDisposed();

			m_sStem = stringTable.GetString("Stem", "Linguistics/Morphology/TemplateTable");

			m_sSlotChooserTitle = stringTable.GetString("SlotChooserTitle", "Linguistics/Morphology/TemplateTable");
			m_sSlotChooserInstructionalText = stringTable.GetString("SlotChooserInstructionalText", "Linguistics/Morphology/TemplateTable");
			m_sObligatorySlot = stringTable.GetString("ObligatorySlot", "Linguistics/Morphology/TemplateTable");
			m_sOptionalSlot = stringTable.GetString("OptionalSlot", "Linguistics/Morphology/TemplateTable");

			m_sNewSlotName = stringTable.GetString("NewSlotName", "Linguistics/Morphology/TemplateTable");
			m_sUnnamedSlotName = stringTable.GetString("UnnamedSlotName", "Linguistics/Morphology/TemplateTable");

			m_sInflAffixChooserTitle = stringTable.GetString("InflAffixChooserTitle", "Linguistics/Morphology/TemplateTable");
			m_sInflAffixChooserInstructionalTextReq = stringTable.GetString("InflAffixChooserInstructionalTextReq", "Linguistics/Morphology/TemplateTable");
			m_sInflAffixChooserInstructionalTextOpt = stringTable.GetString("InflAffixChooserInstructionalTextOpt", "Linguistics/Morphology/TemplateTable");
			m_sInflAffix = stringTable.GetString("InflAffix", "Linguistics/Morphology/TemplateTable");

		}

		/// <summary>
		/// Intercepts mouse clicks on Command Icons and translates them into right mouse clicks
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			Point pt;
			int tag;
			using (new HoldGraphics(this))
			{
				pt = PixelToView(new Point(e.X, e.Y));
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);

				ITsString tss;
				int ichAnchor;
				bool fAssocPrev;
				int hvoObj;
				int ws;
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
			}

			if (tag == 0) // indicates it is an icon
				OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			else
				base.OnMouseUp(e);
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			Slice slice = FindParentSlice();
			Debug.Assert(slice != null);
			if (slice != null)
			{
				// Make sure we are a current slice so we are a colleague so we can enable menu items.
				if (slice != slice.ContainingDataTree.CurrentSlice)
					slice.ContainingDataTree.CurrentSlice = slice;
			}
			if (ShowContextMenu == null)
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			else
			{
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y,
					new SIL.FieldWorks.Common.Utils.Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom),
					new SIL.FieldWorks.Common.Utils.Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom),
					false);
				if (sel == null)
					return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot); // no object, so quit and let base handle it
				int index;
				int hvo, tag, prev; // dummies.
				IVwPropertyStore vps; // dummy
				// Level 0 would give info about ktagText and the hvo of the dummy line object.
				// Level 1 gives info about which line object it is in the root.
				sel.PropInfo(false, 0, out hvo, out tag, out index, out prev, out vps);  // using level 1 for an msa should return the slot it belongs in
#if MaybeSomeDayToTryAndGetRemoveMsaCorrectForCircumfixes
				int indexSlot;
				int hvoSlot, tagSlot, prevSlot; // dummies.
				IVwPropertyStore vpsSlot; // dummy
				sel.PropInfo(false, 1, out hvoSlot, out tagSlot, out indexSlot, out prevSlot, out vpsSlot);
				int classSlot = Cache.GetClassOfObject(hvoSlot);
				if (classSlot == FDO.Ling.MoInflAffixSlot.kClassId)
					m_hvoSlot = hvoSlot;
#endif
				m_hvo = hvo;
				m_class = Cache.GetClassOfObject(hvo);
				ShowContextMenu(this, new InflAffixTemplateEventArgs(this, this.m_xnSpec, pt, tag));
				return true; // we've handled it
			}
		}

		/// <summary>
		/// The slice is no longer a direct parent, so hunt for it up the Parent chain.
		/// </summary>
		/// <returns></returns>
		private Slice FindParentSlice()
		{
			Control ctl = this.Parent;
			while (ctl != null)
			{
				Slice slice = ctl as Slice;
				if (slice != null)
					return slice;
				ctl = ctl.Parent;
			}
			return null;
		}

		/// <summary>
		/// Set the handler which will be invoked when the user right-clicks on the
		/// Inflectional Affix Template slice, or in some other way invokes the context menu.
		/// </summary>
		/// <param name="handler"></param>
		public void SetContextMenuHandler(InflAffixTemplateEventHandler handler)
		{
			CheckDisposed();

			//note the = instead of += we do not want more than 1 handler trying to open the context menu!
			//you could try changing this if we wanted to have a fall back handler, and if there
			//was some way to get the first handler to be able to say "don't pass on this message"
			//when it handled the menu display itself.
			ShowContextMenu = handler;
		}
		/// <summary>
		/// Invoked by a slice when the user does something to bring up a context menu
		/// </summary>
		public void ShowSliceContextMenu(object sender, InflAffixTemplateEventArgs e)
		{
			CheckDisposed();

			//just pass this onto, for example, the XWorks View that owns us,
			//assuming that it has subscribed to this event on this object.
			//If it has not, then this will still point to the "auto menu handler"
			Debug.Assert(ShowContextMenu != null, "this should always be set to something");
			ShowContextMenu(sender, e);
		}
		public bool OnInflTemplateInsertSlotBefore(object cmd)
		{
			CheckDisposed();

			HandleInsert(true);
			return true;	//we handled this.
		}
		public bool OnInflTemplateInsertSlotAfter(object cmd)
		{
			CheckDisposed();

			HandleInsert(false);
			return true;	//we handled this.
		}
		public bool OnInflTemplateMoveSlotLeft(object cmd)
		{
			CheckDisposed();

			HandleMove(true);
			return true;	//we handled this.
		}

		private void HandleMove(bool bLeft)
		{
			FDO.FdoReferenceSequence<IMoInflAffixSlot> seq = null;
			int index = -1;
			GetAffixSequenceContainingHvo(m_hvo, out seq, out index);
			seq.RemoveAt(index);
			int iOffset = (bLeft) ? -1 : 1;
			seq.InsertAt(m_hvo, index + iOffset);
#if CausesDebugAssertBecauseOnlyWorksOnStTexts
			RefreshDisplay();
#endif
		}
		public bool OnInflTemplateMoveSlotRight(object cmd)
		{
			CheckDisposed();

			HandleMove(false);
			return true;	//we handled this.
		}
		public bool OnInflTemplateToggleSlotOptionality(object cmd)
		{
			CheckDisposed();

			IMoInflAffixSlot slot = MoInflAffixSlot.CreateFromDBObject(Cache, m_hvo);
			if (slot != null)
			{
				slot.Optional = !slot.Optional;
				m_rootb.Reconstruct();
			}
			return true;	//we handled this.
		}
		public bool OnInflTemplateRemoveSlot(object cmd)
		{
			CheckDisposed();

			FdoReferenceSequence<IMoInflAffixSlot> seq;
			int index;
			GetAffixSequenceContainingHvo(m_hvo, out seq, out index);
			seq.RemoveAt(index);
			return true;	//we handled this.
		}

		public bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			Command command = (XCore.Command)commandObject;
			string tool = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "tool");
			IMoInflAffMsa inflMsa = MoInflAffMsa.CreateFromDBObject(Cache, m_hvo);
			m_mediator.PostMessage("FollowLink",
				SIL.FieldWorks.FdoUi.FwLink.Create(tool, Cache.GetGuidFromId(inflMsa.OwnerHVO), Cache.ServerName, Cache.DatabaseName));

			return true; // handled this
		}

		public bool OnInflTemplateRemoveInflAffixMsa(object cmd)
		{
			CheckDisposed();

			// the user says to remove this affix (msa) from the slot;
			// if there are other infl affix msas in the entry, we delete the MoInflAffMsa completely;
			// otherwise, we remove the slot info.
			IMoInflAffMsa inflMsa = MoInflAffMsa.CreateFromDBObject(Cache, m_hvo);
			if (inflMsa == null)
				return true; // play it safe
			ILexEntry lex = LexEntry.CreateFromDBObject(Cache, inflMsa.OwnerHVO);
			if (lex == null)
				return true; // play it safe
			if (OtherInflAffixMsasExist(lex, inflMsa))
			{ // remove this msa because there are others
				lex.MorphoSyntaxAnalysesOC.Remove(m_hvo);
			}
			else
			{ // this is the only one; remove it
				inflMsa.ClearAllSlots();
			}
			m_rootb.Reconstruct();  // work around because <choice> is not smart enough to remember its dependencies
			return true;	//we handled this.
		}

		private bool OtherInflAffixMsasExist(ILexEntry lex, IMoInflAffMsa inflMsa)
		{
			bool fOtherInflAffixMsasExist = false;  // assume we won't find an existing infl affix msa
			foreach (IMoMorphSynAnalysis msa in lex.MorphoSyntaxAnalysesOC)
			{
				if (msa.ClassID == MoInflAffMsa.kclsidMoInflAffMsa)
				{ // is an inflectional affix msa
					if (msa.Hvo != inflMsa.Hvo)
					{ // it's not the one the user requested to remove
						fOtherInflAffixMsasExist = true;
						break;
					}
				}
			}
			return fOtherInflAffixMsasExist;
		}

		public bool OnInflTemplateAddInflAffixMsa(object cmd)
		{
			CheckDisposed();

			IMoInflAffixSlot slot = MoInflAffixSlot.CreateFromDBObject(Cache, m_hvoSlot);
			using (SimpleListChooser chooser = MakeChooserWithExtantMsas(slot, cmd as XCore.Command))
			{
				chooser.ShowDialog();
				if (chooser.DialogResult == DialogResult.OK)
				{
					if (chooser.ChosenHvos != null)
					{
						foreach (int hvo in chooser.ChosenHvos)
						{
							AddInflAffixMsaToSlot(hvo, slot.Hvo);
						}
					}
				}
			}
			m_rootb.Reconstruct();  // work around because <choice> is not smart enough to remember its dependencies
			return true;	//we handled this.
		}

		private void AddInflAffixMsaToSlot(int hvo, int slotHvo)
		{
			ICmObject obj = CmObject.CreateFromDBObject(Cache, hvo);
			IMoInflAffMsa inflMsa = obj as IMoInflAffMsa;
			if (inflMsa == null)
				return;
			ILexEntry lex = LexEntry.CreateFromDBObject(Cache, inflMsa.OwnerHVO);
			if (lex == null)
				return; // play it safe
			bool fMiamSet = false;  // assume we won't find an existing infl affix msa
			foreach (IMoMorphSynAnalysis msa in lex.MorphoSyntaxAnalysesOC)
			{
				if (msa.ClassID == MoInflAffMsa.kclsidMoInflAffMsa)
				{ // is an inflectional affix msa
					IMoInflAffMsa miam = (IMoInflAffMsa)msa;
					IPartOfSpeech pos = miam.PartOfSpeechRA;
					if (pos == null)
					{ // use the first unspecified one
						IMoInflAffixSlot slot = MoInflAffixSlot.CreateFromDBObject(Cache, slotHvo);
						miam.PartOfSpeechRAHvo = slot.OwnerHVO;
						miam.ClearAllSlots();  // just in case...
						miam.SlotsRC.Add(slotHvo);
						fMiamSet = true;
						break;
					}
					else if (pos.AllAffixSlotIDs.Contains(slotHvo))
					{ // if the slot is in this POS
						if (miam.SlotsRC.Count == 0)
						{ // use the first available
							miam.SlotsRC.Add(slotHvo);
							fMiamSet = true;
							break;
						}
						else if (miam.SlotsRC.Contains(slotHvo))
						{ // it is already set (probably done by the CreateEntry dialog process)
							fMiamSet = true;
							break;
						}
						else if (lex.IsCircumfix())
						{ // only circumfixes can more than one slot
							miam.SlotsRC.Add(slotHvo);
							fMiamSet = true;
							break;
						}
					}
				}
			}
			if (!fMiamSet)
			{  // need to create a new infl affix msa
				IMoInflAffMsa newMsa = new MoInflAffMsa();
				lex.MorphoSyntaxAnalysesOC.Add(newMsa);
				EnsureNewMsaHasSense(lex, newMsa);
				newMsa.SlotsRC.Add(slotHvo);
				IMoInflAffixSlot slot = MoInflAffixSlot.CreateFromDBObject(Cache, slotHvo);
				newMsa.PartOfSpeechRAHvo = slot.OwnerHVO;
			}
		}

		private void EnsureNewMsaHasSense(ILexEntry lex, IMoInflAffMsa newMsa)
		{
			// if no lexsense has this msa, copy first sense and have it refer to this msa
			bool fASenseHasMsa = false;
			foreach (ILexSense sense in lex.AllSenses)
			{
				if (sense.MorphoSyntaxAnalysisRAHvo == newMsa.Hvo)
				{
					fASenseHasMsa = true;
					break;
				}
			}
			if (!fASenseHasMsa)
			{
				ILexSense newSense = new FDO.Ling.LexSense();
				lex.SensesOS.Append(newSense);
				ILexSense firstSense = lex.SensesOS.FirstItem;
				// only copying gloss for now and only copying default analysis ws
				//newSense.Definition.AnalysisDefaultWritingSystem.Text = firstSense.Definition.AnalysisDefaultWritingSystem.Text;
				newSense.Gloss.AnalysisDefaultWritingSystem = firstSense.Gloss.AnalysisDefaultWritingSystem;
				//newSense.GrammarNote.AnalysisDefaultWritingSystem.Text = firstSense.GrammarNote.AnalysisDefaultWritingSystem.Text;
				//newSense.SemanticsNote.AnalysisDefaultWritingSystem.Text = firstSense.SemanticsNote.AnalysisDefaultWritingSystem.Text;
				newSense.MorphoSyntaxAnalysisRA = newMsa;
			}
		}

		private SimpleListChooser MakeChooserWithExtantMsas(IMoInflAffixSlot slot, XCore.Command cmd)
		{
			// Want the list of all lex entries which have an infl affix Msa
			// Do not want to list the infl affix Msas that are already assigned to the slot.
			Set<int> candidates = new Set<int>();
			bool fIsPrefixSlot = m_template.PrefixSlotsRS.Contains(slot.Hvo);
			foreach (int hvoLexEntry in slot.OtherInflectionalAffixLexEntries)
			{
				ILexEntry lex = new LexEntry(Cache, hvoLexEntry);
				bool fInclude = EntryHasAffixThatMightBeInSlot(lex, fIsPrefixSlot);
				if (fInclude)
				{
					foreach (IMoMorphSynAnalysis msa in lex.MorphoSyntaxAnalysesOC)
					{
						if (msa.ClassID == MoInflAffMsa.kclsidMoInflAffMsa)
						{
							candidates.Add(msa.Hvo);
							break;
						}
					}
				}
			}
			ObjectLabelCollection labels = new ObjectLabelCollection(Cache, candidates, null);
			XCore.PersistenceProvider persistProvider =
				new PersistenceProvider(m_mediator.PropertyTable);
			int[] aiForceMultipleChoices = new int[1];
			SimpleListChooser chooser =
				new SimpleListChooser(persistProvider, labels, m_ChooseInflectionalAffixHelpTopic, Cache, aiForceMultipleChoices);
			chooser.SetFontForDialog(new int[] { Cache.DefaultVernWs, Cache.DefaultAnalWs }, StyleSheet, WritingSystemFactory);
			chooser.Cache = Cache;
			// We don't want the ()'s indicating optionality since the text spells it out.
			chooser.TextParam = slot.Name.AnalysisDefaultWritingSystem;
			chooser.Title = m_sInflAffixChooserTitle;
			if (slot.Optional)
				chooser.InstructionalText = m_sInflAffixChooserInstructionalTextOpt;
			else
				chooser.InstructionalText = m_sInflAffixChooserInstructionalTextReq;
			chooser.AddLink(m_sInflAffix, SimpleListChooser.LinkType.kDialogLink,
				new MakeInflAffixEntryChooserCommand(Cache, true, m_sInflAffix, fIsPrefixSlot, slot, m_mediator));
			chooser.SetObjectAndFlid(slot.Hvo, slot.OwningFlid);
			string sGuiControl = XmlUtils.GetOptionalAttributeValue(cmd.ConfigurationNode, "guicontrol");
			if (!String.IsNullOrEmpty(sGuiControl))
			{
				chooser.ReplaceTreeView(m_mediator, sGuiControl);
			}
			return chooser;
		}

		/// <summary>
		/// Determine if the lex entry can appear in the prefix/suffix slot
		/// </summary>
		/// <param name="lex"></param>
		/// <param name="fIsPrefixSlot"></param>
		/// <returns>true if the lex entry can appear in the slot</returns>
		private bool EntryHasAffixThatMightBeInSlot(ILexEntry lex, bool fIsPrefixSlot)
		{
			bool fInclude = false; // be pessimistic
			List<IMoMorphType> morphTypes = lex.MorphTypes;
			foreach (IMoMorphType morphType in morphTypes)
			{
				if (fIsPrefixSlot)
				{
					if (MoMorphType.IsPrefixishType(Cache, morphType.Hvo))
					{
						fInclude = true;
						break;
					}
				}
				else
				{
					// is a suffix slot
					if (MoMorphType.IsSuffixishType(Cache, morphType.Hvo))
					{
						fInclude = true;
						break;
					}
				}
			}
			return fInclude;
		}

		private void HandleInsert(bool fBefore)
		{
			bool fIsPrefixSlot = GetIsPrefixSlot(fBefore);
			using (SimpleListChooser chooser = MakeChooserWithExtantSlots(fIsPrefixSlot))
			{
				chooser.ShowDialog();
				if (chooser.ChosenOne != null)
				{
					int hvoChosen = chooser.ChosenOne.Hvo;
					int flid = 0;
					int ihvo = -1;
					if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
					{
						HandleInsertAroundSlot(fBefore, hvoChosen, out flid, out ihvo);
					}
					else if (m_class == MoInflAffixTemplate.kclsidMoInflAffixTemplate)
					{
						HandleInsertAroundStem(fBefore, hvoChosen, out flid, out ihvo);
					}
					m_rootb.Reconstruct(); // Ensure that the table gets redrawn
					if (chooser.LinkExecuted)
					{
						// Select the header of the newly added slot in case the user wants to edit it.
						// See LT-8209.
						SelLevInfo[] rgvsli = new SelLevInfo[1];
						rgvsli[0].hvo = hvoChosen;
						rgvsli[0].ich = -1;
						rgvsli[0].ihvo = ihvo;
						rgvsli[0].tag = flid;
						m_rootb.MakeTextSelInObj(0, 1, rgvsli, 0, null, true, true, true, false, true);
					}
#if CausesDebugAssertBecauseOnlyWorksOnStTexts
					RefreshDisplay();
#endif
				}
			}
		}

		private bool GetIsPrefixSlot(bool fBefore)
		{
			bool fIsPrefixSlot = false;
			if (m_class == MoInflAffixTemplate.kclsidMoInflAffixTemplate)
			{
				if (fBefore)
					fIsPrefixSlot = true;
				else
					fIsPrefixSlot = false;
			}
			else if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
			{
				List<int> listHvos = new List<int>(m_template.PrefixSlotsRS.HvoArray);
				int index = listHvos.IndexOf(m_hvoSlot);
				if (index >= 0)
					fIsPrefixSlot = true;
				else
					fIsPrefixSlot = false;
			}
			return fIsPrefixSlot;
		}

		private SimpleListChooser MakeChooserWithExtantSlots(bool fIsPrefixSlot)
		{
			int slotFlid;
			if (fIsPrefixSlot)
				slotFlid = (int)MoInflAffixTemplate.MoInflAffixTemplateTags.kflidPrefixSlots;
			else
				slotFlid = (int) MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSuffixSlots;
			ObjectLabelCollection labels = new ObjectLabelCollection(
				Cache,
				m_template.ReferenceTargetCandidates(slotFlid),
				null);
			PersistenceProvider persistProvider =
				new PersistenceProvider(m_mediator.PropertyTable);
			SimpleListChooser chooser =
				new SimpleListChooser(persistProvider, labels, m_ChooseSlotHelpTopic);
			chooser.Cache = Cache;
			chooser.TextParamHvo = m_template.OwnerHVO;
			chooser.Title = m_sSlotChooserTitle;
			chooser.InstructionalText = m_sSlotChooserInstructionalText;
			string sTopPOS;
			int posHvo = GetHvoOfHighestPOS(m_template.OwnerHVO, out sTopPOS);
			string sLabel = String.Format(m_sObligatorySlot, sTopPOS);
			chooser.AddLink(sLabel, SimpleListChooser.LinkType.kSimpleLink,
				new MakeInflAffixSlotChooserCommand(Cache, true, sLabel, posHvo,
				false, m_mediator));
			sLabel = String.Format(m_sOptionalSlot, sTopPOS);
			chooser.AddLink(sLabel, SimpleListChooser.LinkType.kSimpleLink,
				new MakeInflAffixSlotChooserCommand(Cache, true, sLabel, posHvo, true,
				m_mediator));
			chooser.SetObjectAndFlid(posHvo, (int)FDO.Ling.MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSlots);
			return chooser;
		}

		private int GetHvoOfHighestPOS(int startHvo, out string sTopPOS)
		{
			int posHvo = 0;
			sTopPOS = MEStrings.ksQuestions;
			ICmObject obj = CmObject.CreateFromDBObject(Cache, startHvo);
			while (obj.ClassID == PartOfSpeech.kclsidPartOfSpeech)
			{
				posHvo = obj.Hvo;
				sTopPOS = obj.ShortName;
				obj = CmObject.CreateFromDBObject(Cache, obj.OwnerHVO);
			}
			return posHvo;
		}

		private bool IsRTL()
		{
			return Cache.GetBoolProperty(Cache.DefaultVernWs,
				(int)SIL.FieldWorks.FDO.Cellar.LgWritingSystem.LgWritingSystemTags.kflidRightToLeft);
		}

		private void HandleInsertAroundSlot(bool fBefore, int hvoChosen, out int flid, out int ihvo)
		{
			FdoReferenceSequence<IMoInflAffixSlot> seq = null;
			int index = -1;
			GetAffixSequenceContainingHvo(m_hvo, out seq, out index);
			int iOffset = (fBefore) ? 0 : 1;
			seq.InsertAt(hvoChosen, index + iOffset);
			flid = seq.Flid;
			// The views system numbers visually, so adjust index for RTL vernacular writing system.
			ihvo = index + iOffset;
			if (IsRTL())
				ihvo = (seq.Count - 1) - ihvo;
		}

		private void HandleInsertAroundStem(bool fBefore, int hvoChosen, out int flid, out int ihvo)
		{
			if (fBefore)
			{
				flid = (int)MoInflAffixTemplate.MoInflAffixTemplateTags.kflidPrefixSlots;
				// The views system numbers visually, so adjust index for RTL vernacular writing system.
				if (IsRTL())
					ihvo = 0;
				else
					ihvo = m_template.PrefixSlotsRS.Count;
				m_template.PrefixSlotsRS.Append(hvoChosen);
			}
			else
			{
				flid = (int)MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSuffixSlots;
				// The views system numbers visually, so adjust index for RTL vernacular writing system.
				if (IsRTL())
					ihvo = m_template.SuffixSlotsRS.Count;
				else
					ihvo = 0;
				m_template.SuffixSlotsRS.InsertAt(hvoChosen, 0);
			}
		}

		private void GetAffixSequenceContainingHvo(int hvo, out FdoReferenceSequence<IMoInflAffixSlot> seq, out int index)
		{
			List<int> listHvos = new List<int>(m_template.PrefixSlotsRS.HvoArray);
			index = listHvos.IndexOf(hvo);
			if (index >= 0)
			{
				seq = m_template.PrefixSlotsRS;
			}
			else
			{
				listHvos = new List<int>(m_template.SuffixSlotsRS.HvoArray);
				index = listHvos.IndexOf(hvo);
				if (index >= 0)
					seq = m_template.SuffixSlotsRS;
				else
					seq = null;
			}
		}
		//public virtual bool OnDisplayInflTemplateInsertSlotBefore(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    DetermineSlotContextMenuItemContent(display);
		//    //			display.Enabled = true;
		//    return true; //we've handled this
		//}
		//public virtual bool OnDisplayInflTemplateInsertSlotAfter(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    DetermineSlotContextMenuItemContent(display);
		//    //			display.Enabled = true;
		//    return true; //we've handled this
		//}
		//public virtual bool OnDisplayInflTemplateMoveSlotLeft(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    DetermineSlotContextMenuItemContent(display);
		//    if (m_class != SIL.FieldWorks.FDO.Ling.MoInflAffixSlot.kclsidMoInflAffixSlot)
		//        display.Enabled = false;
		//    else
		//    {
		//        List<int> listPrefixSlotHvos = new List<int>(m_template.PrefixSlotsRS.HvoArray);
		//        if (!SetDisplayEnabledIfFindSlotInSequence(listPrefixSlotHvos, display, true))
		//        {
		//            List<int> listSuffixSlotHvos = new List<int>(m_template.SuffixSlotsRS.HvoArray);
		//            SetDisplayEnabledIfFindSlotInSequence(listSuffixSlotHvos, display, true);
		//        }
		//    }
		//    return true; //we've handled this
		//}
		//
		//private bool SetDisplayEnabledIfFindSlotInSequence(List<int> listSlotHvos, UIItemDisplayProperties display, bool bIsLeft)
		//{
		//    int index = listSlotHvos.IndexOf(m_hvo);
		//    if (index >= 0)
		//    {	// it was found
		//        bool bAtEdge;
		//        if (bIsLeft)
		//            bAtEdge = (index == 0);
		//        else
		//            bAtEdge = (index == listSlotHvos.Count - 1);
		//        if (bAtEdge || listSlotHvos.Count == 1)
		//            display.Enabled = false;  // Cannot move it left when it's at the left edge or there's only one
		//        else
		//            display.Enabled = true;
		//        return true;
		//    }
		//    else
		//        return false;
		//}
		//public virtual bool OnDisplayInflTemplateMoveSlotRight(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    DetermineSlotContextMenuItemContent(display);
		//    if (m_class != SIL.FieldWorks.FDO.Ling.MoInflAffixSlot.kclsidMoInflAffixSlot)
		//        display.Enabled = false;
		//    else
		//    {
		//        List<int> listPrefixSlotHvos = new List<int>(m_template.PrefixSlotsRS.HvoArray);
		//        if (!SetDisplayEnabledIfFindSlotInSequence(listPrefixSlotHvos, display, false))
		//        {
		//            List<int> listSuffixSlotHvos = new List<int>(m_template.SuffixSlotsRS.HvoArray);
		//            SetDisplayEnabledIfFindSlotInSequence(listSuffixSlotHvos, display, false);
		//        }
		//    }
		//    return true; //we've handled this
		//}
		//public virtual bool OnDisplayInflTemplateRemoveSlot(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    DetermineSlotContextMenuItemContent(display);
		//    if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
		//        display.Enabled = true;
		//    else
		//        display.Enabled = false;
		//    return true; //we've handled this
		//}
		//public virtual bool OnDisplayInflTemplateToggleSlotOptionality(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    DetermineSlotContextMenuItemContent(display);
		//    if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
		//        display.Enabled = true;
		//    else
		//        display.Enabled = false;
		//    return true; //we've handled this
		//}
		//public virtual bool OnDisplayInflTemplateAddInflAffixMsa(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    if (m_class == MoInflAffMsa.kclsidMoInflAffMsa)
		//        DetermineMsaContextMenuItemContent(display);
		//    else if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
		//    {
		//        IMoInflAffixSlot slot = new MoInflAffixSlot(Cache, m_hvo);
		//        display.Text = DoXXXReplace(display.Text, CheckSlotName(slot));
		//        m_hvoSlot = m_hvo;
		//        display.Enabled = true;
		//    }
		//    else
		//    {
		//        display.Visible = false;
		//        display.Enabled = false;
		//    }
		//    return true; //we've handled this
		//}
		//
		//public virtual bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    if (m_class == MoInflAffMsa.kclsidMoInflAffMsa)
		//        display.Visible = display.Enabled = true;
		//    else
		//    {
		//        display.Visible = false;
		//        display.Enabled = false;
		//    }
		//    return true; //we've handled this
		//}
		//
		//public virtual bool OnDisplayInflTemplateRemoveInflAffixMsa(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    if (m_class == MoInflAffMsa.kclsidMoInflAffMsa)
		//        DetermineMsaContextMenuItemContent(display);
		//    else
		//    {
		//        display.Visible = false;
		//        display.Enabled = false;
		//    }
		//    return true; //we've handled this
		//}
		//
		//public virtual bool OnDisplayInflAffixTemplateHelp(object commandObject, ref UIItemDisplayProperties display)
		//{
		//    CheckDisposed();
		//
		//    if (m_class == MoInflAffMsa.kclsidMoInflAffMsa ||
		//        m_class == MoInflAffixSlot.kclsidMoInflAffixSlot ||
		//        m_class == MoInflAffixTemplate.kclsidMoInflAffixTemplate)
		//    {
		//        // Only display help if there's a topic linked to the generated ID in the resource file
		//        display.Visible = display.Enabled = (FwApp.App.GetHelpString(m_ChooseInflectionalAffixHelpTopic, 0) == null ? false : true);
		//    }
		//    else
		//    {
		//        display.Enabled = false;
		//        display.Visible = false;
		//    }
		//    return true; //we've handled this
		//}

		public bool OnInflAffixTemplateHelp(object cmd)
		{
			CheckDisposed();

			ShowHelp.ShowHelpTopic(FwApp.App, m_ChooseInflectionalAffixHelpTopic);
			return true;
		}

		//private void DetermineMsaContextMenuItemContent(UIItemDisplayProperties display)
		//{
		//    IMoInflAffMsa msa = new MoInflAffMsa(Cache, m_hvo);
		//    display.Text = DoYYYReplace(display.Text, msa.ShortName);
		//    string sSlotName = GetSlotNameOfMsa(msa);
		//    if (sSlotName != null)
		//        display.Text = DoXXXReplace(display.Text, sSlotName);
		//    display.Visible = true;
		//    display.Enabled = true;
		//}
		//
//        private string GetSlotNameOfMsa(IMoInflAffMsa msa)
//        {
//            string sResult = null;
//            IMoInflAffixSlot slot = null;
//#if !MaybeSomeDayToTryAndGetRemoveMsaCorrectForCircumfixes
//            int[] hvos = msa.SlotsRC.HvoArray;
//            if (hvos.Length > 0)
//            {
//                int hvo = hvos[0];
//                slot = MoInflAffixSlot.CreateFromDBObject(this.Cache, hvo);
//            }
//            sResult = CheckSlotName(slot);
//            m_hvoSlot = slot.Hvo;
//#else
//            slot = (FDO.Ling.MoInflAffixSlot)CmObject.CreateFromDBObject(this.Cache, m_hvoSlot);
//            sResult = CheckSlotName(slot);
//#endif
//            return sResult;
//        }

#if WhenModelIsChangedSoInflMsaHasThePOSOrSlotRefAttr
		private string FindMsaInSlots(List<int> listSlotHvos)
		{
			string sResult = null;
			foreach (int hvoSlot in listSlotHvos)
			{
				sResult = FindSlotNameOfMsa(hvoSlot);
				if (sResult != null)
				{
					m_hvoSlot = hvoSlot;
					return sResult;
				}
			}
			return null;
		}

		private string FindSlotNameOfMsa(int hvoSlot)
		{
#if WantWWStuff // TODO: AndyB(RandyR): Fix this, if it is still needed.
			IMoInflAffixSlot slot = new SIL.FieldWorks.FDO.Ling.MoInflAffixSlot(Cache, hvoSlot);
			List<int> listMsaHvos = new List<int>(slot.AffixesRS.HvoArray);
			int index = listMsaHvos.IndexOf(m_hvo);
			if (index >= 0)
				return slot.Name.AnalysisDefaultWritingSystem;
#endif
			return null;
		}
#endif
		//private void DetermineSlotContextMenuItemContent(UIItemDisplayProperties display)
		//{
		//    if (m_class == 0)
		//        return; // should not happen
		//    if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
		//    {
		//        IMoInflAffixSlot slot = new MoInflAffixSlot(Cache, m_hvo);
		//        display.Text = DoXXXReplace(display.Text, CheckSlotName(slot));
		//    }
		//    else if (m_class == MoInflAffixTemplate.kclsidMoInflAffixTemplate)
		//    {
		//        display.Text = DoXXXReplace(display.Text, m_sStem);
		//    }
		//    else if (m_class == MoInflAffMsa.kclsidMoInflAffMsa)
		//    {
		//        display.Visible = false;
		//        display.Enabled = false;
		//    }
		//}
		//
		//private string DoXXXReplace(string sSource, string sReplace)
		//{
		//    return sSource.Replace("XXX", sReplace);
		//}
		//private string DoYYYReplace(string sSource, string sReplace)
		//{
		//    return sSource.Replace("YYY", sReplace);
		//}
		private string CheckSlotName(IMoInflAffixSlot slot)
		{
			string sResult;
			if (slot != null)
			{
				if (slot.Name.AnalysisDefaultWritingSystem == m_sNewSlotName)
					slot.Name.AnalysisDefaultWritingSystem = GetNextUnnamedSlotName();
				sResult = slot.Name.AnalysisDefaultWritingSystem; ;
			}
			else
				sResult = MEStrings.ksQuestions;
			return sResult;
		}
		private string GetNextUnnamedSlotName()
		{
			// get count of how many unnamed slots there are in this pos and its parent
			// append that number plus one to the string table name
			List<int> aiUnnamedSlotValues = GetAllUnnamedSlotValues();
			aiUnnamedSlotValues.Sort();
			int iMax = aiUnnamedSlotValues.Count;
			int iValueToUse = iMax + 1;  // default to the next one
			// find any "holes" in the numbered sequence (in case the user has renamed
			//   one or more of them since the last time we did this)
			for (int i = 0; i < iMax; i++)
			{
				int iValue = i + 1;
				if (aiUnnamedSlotValues[i] != iValue)
				{	// use the value in the "hole"
					iValueToUse = iValue;
					break;
				}
			}
			return m_sUnnamedSlotName + iValueToUse.ToString();
		}
		private List<int> GetAllUnnamedSlotValues()
		{
			List<int> aiUnnamedSlotValues = new List<int>();
			ICmObject obj = CmObject.CreateFromDBObject(Cache, m_template.OwnerHVO);
			IPartOfSpeech pos = obj as PartOfSpeech;
			while (pos != null)
			{
				foreach (IMoInflAffixSlot slot in pos.AffixSlotsOC)
				{
					if (slot.Name.AnalysisDefaultWritingSystem == null ||
						slot.Name.AnalysisDefaultWritingSystem.StartsWith(m_sUnnamedSlotName))
					{
						string sValue = m_sUnnamedSlotName;
						int i;
						try
						{
							i = Convert.ToInt32(sValue);
						}
						catch (Exception e)
						{ // default to 9999 if what's after is not a number
							string s = e.GetType().ToString(); // make compiler happy
							i = 9999; // use something very unlikely to happen normally
						}
						aiUnnamedSlotValues.Add(i);
					}
				}
				obj = CmObject.CreateFromDBObject(Cache, pos.OwnerHVO);
				pos = obj as IPartOfSpeech;
			}
			return aiUnnamedSlotValues;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When focus is lost, stop filtering messages to catch characters
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			foreach (IMoInflAffixSlot slot in m_template.PrefixSlotsRS)
				slot.Name.AnalysisDefaultWritingSystem = CheckSlotName(slot);
			foreach (IMoInflAffixSlot slot in m_template.SuffixSlotsRS)
				slot.Name.AnalysisDefaultWritingSystem = CheckSlotName(slot);
			base.OnLostFocus(e);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForInflTemplateAddInflAffixMsa(string sLabel)
		{
			CheckDisposed();
			if (m_class == MoInflAffMsa.kclsidMoInflAffMsa)
			{
				return DetermineMsaContextMenuItemLabel(sLabel);
			}
			else if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
			{
				m_hvoSlot = m_hvo;
				IMoInflAffixSlot slot = new MoInflAffixSlot(Cache, m_hvo);
				return DoXXXReplace(sLabel, TsSlotName(slot));
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <returns></returns>
		internal ITsString DetermineSlotContextMenuItemLabel(string sLabel)
		{
			CheckDisposed();
			if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
			{
				IMoInflAffixSlot slot = new MoInflAffixSlot(Cache, m_hvo);
				return DoXXXReplace(sLabel, TsSlotName(slot));
			}
			else if (m_class == MoInflAffixTemplate.kclsidMoInflAffixTemplate)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ITsString tssStem = tsf.MakeString(m_sStem, Cache.DefaultUserWs);
				return DoXXXReplace(sLabel, tssStem);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <param name="fMoveLeft"></param>
		/// <param name="fEnabled"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForInflTemplateMoveSlot(string sLabel, bool fMoveLeft, out bool fEnabled)
		{
			CheckDisposed();
			ITsString tssLabel = DetermineSlotContextMenuItemLabel(sLabel);
			if (m_class != SIL.FieldWorks.FDO.Ling.MoInflAffixSlot.kclsidMoInflAffixSlot)
			{
				fEnabled = false;
			}
			else
			{
				List<int> listPrefixSlotHvos = new List<int>(m_template.PrefixSlotsRS.HvoArray);
				if (!SetEnabledIfFindSlotInSequence(listPrefixSlotHvos, out fEnabled, fMoveLeft))
				{
					List<int> listSuffixSlotHvos = new List<int>(m_template.SuffixSlotsRS.HvoArray);
					SetEnabledIfFindSlotInSequence(listSuffixSlotHvos, out fEnabled, fMoveLeft);
				}
			}
			return tssLabel;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <param name="fEnabled"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForInflTemplateAffixSlotOperation(string sLabel, out bool fEnabled)
		{
			CheckDisposed();
			ITsString tssLabel = DetermineSlotContextMenuItemLabel(sLabel);
			if (m_class == MoInflAffixSlot.kclsidMoInflAffixSlot)
				fEnabled = true;
			else
				fEnabled = false;
			return tssLabel;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForInflTemplateRemoveInflAffixMsa(string sLabel)
		{
			CheckDisposed();
			if (m_class == MoInflAffMsa.kclsidMoInflAffMsa)
				return DetermineMsaContextMenuItemLabel(sLabel);
			else
				return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForJumpToTool(string sLabel)
		{
			CheckDisposed();
			if (m_class == MoInflAffMsa.kclsidMoInflAffMsa)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(sLabel, Cache.DefaultUserWs);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForInflAffixTemplateHelp(string sLabel)
		{
			CheckDisposed();
			if (m_class == MoInflAffMsa.kclsidMoInflAffMsa ||
				m_class == MoInflAffixSlot.kclsidMoInflAffixSlot ||
				m_class == MoInflAffixTemplate.kclsidMoInflAffixTemplate)
			{
				// Display help only if there's a topic linked to the generated ID in the resource file.
				if (FwApp.App.GetHelpString(m_ChooseInflectionalAffixHelpTopic, 0) != null)
				{
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					return tsf.MakeString(sLabel, Cache.DefaultUserWs);
				}
			}
			return null;
		}

		private bool SetEnabledIfFindSlotInSequence(List<int> listSlotHvos, out bool fEnabled, bool bIsLeft)
		{
			int index = listSlotHvos.IndexOf(m_hvo);
			if (index >= 0)
			{	// it was found
				bool bAtEdge;
				if (bIsLeft)
					bAtEdge = (index == 0);
				else
					bAtEdge = (index == listSlotHvos.Count - 1);
				if (bAtEdge || listSlotHvos.Count == 1)
					fEnabled = false;  // Cannot move it left when it's at the left edge or there's only one
				else
					fEnabled = true;
				return true;
			}
			else
			{
				fEnabled = false;
				return false;
			}
		}

		private ITsString DetermineMsaContextMenuItemLabel(string sLabel)
		{
			IMoInflAffMsa msa = new MoInflAffMsa(Cache, m_hvo);
			ITsString tss = DoYYYReplace(sLabel, msa.ShortNameTSS);
			ITsString tssSlotName = TsSlotNameOfMsa(msa);
			return DoXXXReplace(tss, tssSlotName);
		}

		private ITsString TsSlotNameOfMsa(IMoInflAffMsa msa)
		{
			ITsString tssResult = null;
			IMoInflAffixSlot slot = null;
#if !MaybeSomeDayToTryAndGetRemoveMsaCorrectForCircumfixes
			int[] hvos = msa.SlotsRC.HvoArray;
			if (hvos.Length > 0)
			{
				int hvo = hvos[0];
				slot = MoInflAffixSlot.CreateFromDBObject(this.Cache, hvo);
			}
			tssResult = TsSlotName(slot);
			m_hvoSlot = slot.Hvo;
#else
			slot = (FDO.Ling.MoInflAffixSlot)CmObject.CreateFromDBObject(this.Cache, m_hvoSlot);
			sResult = TsSlotName(slot);
#endif
			return tssResult;
		}

		private ITsString TsSlotName(IMoInflAffixSlot slot)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			if (slot != null)
			{
				if (slot.Name.AnalysisDefaultWritingSystem == m_sNewSlotName)
					slot.Name.AnalysisDefaultWritingSystem = GetNextUnnamedSlotName();
				return tsf.MakeString(slot.Name.AnalysisDefaultWritingSystem,
					Cache.DefaultAnalWs);
			}
			else
			{
				return tsf.MakeString(MEStrings.ksQuestions, Cache.DefaultUserWs);
			}
		}

		private ITsString DoXXXReplace(string sSource, ITsString tssReplace)
		{
			return DoReplaceToken(sSource, tssReplace, "XXX");
		}

		private ITsString DoYYYReplace(string sSource, ITsString tssReplace)
		{
			return DoReplaceToken(sSource, tssReplace, "YYY");
		}

		private ITsString DoReplaceToken(string sSource, ITsString tssReplace, string sToken)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeString(sSource, Cache.DefaultUserWs);
			return DoReplaceToken(tss, tssReplace, sToken);
		}

		private ITsString DoXXXReplace(ITsString tssSource, ITsString tssReplace)
		{
			return DoReplaceToken(tssSource, tssReplace, "XXX");
		}

		private ITsString DoReplaceToken(ITsString tssSource, ITsString tssReplace, string sToken)
		{
			ITsStrBldr tsb = tssSource.GetBldr();
			int ich = tsb.Text.IndexOf(sToken);
			while (ich >= 0)
			{
				if (ich > 0)
					tsb.ReplaceTsString(ich, ich + sToken.Length, tssReplace);
				if (ich + tssReplace.Length >= tsb.Length)
					break;
				ich = tsb.Text.IndexOf(sToken, ich + tssReplace.Length);
			}
			return tsb.GetString();
		}
	}
}
