// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Utils;
using XCore;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for InflAffixTemplateControl.
	/// </summary>
	public class InflAffixTemplateControl : XmlView
	{
		ICmObject m_obj;		// item clicked
		IMoInflAffixSlot m_slot;		// slot to which chosen MSA belongs
		IMoInflAffixTemplate m_template;
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

		public InflAffixTemplateControl(LcmCache cache, int hvoRoot, XmlNode xnSpec)
			: base(hvoRoot, XmlUtils.GetAttributeValue(xnSpec, "layout"), true)
		{
			m_xnSpec = xnSpec["deParams"];
			Cache = cache;
			m_template = Cache.ServiceLocator.GetInstance<IMoInflAffixTemplateRepository>().GetObject(m_hvoRoot);
		}

		protected override void Dispose(bool disposing)
		{
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

		protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
		{
			base.OnValidating(e);
			OnLostFocus(new EventArgs());
		}

		public void SetStringTableValues()
		{
			CheckDisposed();

			m_sStem = StringTable.Table.GetString("Stem", "Linguistics/Morphology/TemplateTable");

			m_sSlotChooserTitle = StringTable.Table.GetString("SlotChooserTitle", "Linguistics/Morphology/TemplateTable");
			m_sSlotChooserInstructionalText = StringTable.Table.GetString("SlotChooserInstructionalText", "Linguistics/Morphology/TemplateTable");
			m_sObligatorySlot = StringTable.Table.GetString("ObligatorySlot", "Linguistics/Morphology/TemplateTable");
			m_sOptionalSlot = StringTable.Table.GetString("OptionalSlot", "Linguistics/Morphology/TemplateTable");

			m_sNewSlotName = StringTable.Table.GetString("NewSlotName", "Linguistics/Morphology/TemplateTable");
			m_sUnnamedSlotName = StringTable.Table.GetString("UnnamedSlotName", "Linguistics/Morphology/TemplateTable");

			m_sInflAffixChooserTitle = StringTable.Table.GetString("InflAffixChooserTitle", "Linguistics/Morphology/TemplateTable");
			m_sInflAffixChooserInstructionalTextReq = StringTable.Table.GetString("InflAffixChooserInstructionalTextReq", "Linguistics/Morphology/TemplateTable");
			m_sInflAffixChooserInstructionalTextOpt = StringTable.Table.GetString("InflAffixChooserInstructionalTextOpt", "Linguistics/Morphology/TemplateTable");
			m_sInflAffix = StringTable.Table.GetString("InflAffix", "Linguistics/Morphology/TemplateTable");

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
					new Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom),
					new Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom),
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
				m_obj = Cache.ServiceLocator.GetObject(hvo);
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

			HandleMove((Command)cmd, true);
			return true;	//we handled this.
		}

		private void HandleMove(Command cmd, bool bLeft)
		{
			ILcmReferenceSequence<IMoInflAffixSlot> seq;
			int index;
			var slot = m_obj as IMoInflAffixSlot;
			GetAffixSequenceContainingSlot(slot, out seq, out index);
			UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, m_template,
				() =>
					{
						seq.RemoveAt(index);
						int iOffset = (bLeft) ? -1 : 1;
						seq.Insert(index + iOffset, slot);
					});
		}

		public bool OnInflTemplateMoveSlotRight(object cmd)
		{
			CheckDisposed();

			HandleMove((Command)cmd, false);
			return true;	//we handled this.
		}
		public bool OnInflTemplateToggleSlotOptionality(object cmd)
		{
			CheckDisposed();

			var slot = m_obj as IMoInflAffixSlot;
			if (slot != null)
			{
				string sName = slot.Name.BestAnalysisVernacularAlternative.Text;
				string sUndo = String.Format(MEStrings.ksUndoChangeOptionalityOfSlot, sName);
				string sRedo = String.Format(MEStrings.ksRedoChangeOptionalityOfSlot, sName);
				using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(
					Cache.ActionHandlerAccessor, sUndo, sRedo))
				{
					slot.Optional = !slot.Optional;
					helper.RollBack = false;
				}
				m_rootb.Reconstruct();
			}
			return true;	//we handled this.
		}
		public bool OnInflTemplateRemoveSlot(object cmd)
		{
			CheckDisposed();

			ILcmReferenceSequence<IMoInflAffixSlot> seq;
			int index;
			GetAffixSequenceContainingSlot(m_obj as IMoInflAffixSlot, out seq, out index);
			using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor,
				String.Format(MEStrings.ksUndoRemovingSlot, seq[index].Name.BestAnalysisVernacularAlternative.Text),
				String.Format(MEStrings.ksRedoRemovingSlot, seq[index].Name.BestAnalysisVernacularAlternative.Text)))
			{
				seq.RemoveAt(index);
				helper.RollBack = false;
			}
			return true;	//we handled this.
		}

		public bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			Command command = (XCore.Command)commandObject;
			string tool = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "tool");
			var inflMsa = m_obj as IMoInflAffMsa;
			m_mediator.PostMessage("FollowLink", new FwLinkArgs(tool, inflMsa.Owner.Guid));
			return true; // handled this
		}

		public bool OnInflTemplateRemoveInflAffixMsa(object cmd)
		{
			CheckDisposed();

			// the user says to remove this affix (msa) from the slot;
			// if there are other infl affix msas in the entry, we delete the MoInflAffMsa completely;
			// otherwise, we remove the slot info.
			var inflMsa = m_obj as IMoInflAffMsa;
			if (inflMsa == null)
				return true; // play it safe
			var lex = inflMsa.OwnerOfClass<ILexEntry>();
			if (lex == null)
				return true; // play it safe
			UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoRemovingAffix, MEStrings.ksRedoRemovingAffix,
				Cache.ActionHandlerAccessor,
				() =>
					{
						if (OtherInflAffixMsasExist(lex, inflMsa))
						{
							// remove this msa because there are others
							lex.MorphoSyntaxAnalysesOC.Remove(inflMsa);
						}
						else
						{
							// this is the only one; remove it
							inflMsa.SlotsRC.Clear();
						}
					});
			m_rootb.Reconstruct();  // work around because <choice> is not smart enough to remember its dependencies
			return true;	//we handled this.
		}

		public bool OnInflTemplateMoveUpInflAffixMsa(object cmd)
		{
			return MoveInflAffixMsa(true);
		}
		public bool OnInflTemplateMoveDownInflAffixMsa(object cmd)
		{
			return MoveInflAffixMsa(false);
		}

		private bool MoveInflAffixMsa(bool up)
		{
			CheckDisposed();

			var inflMsa = m_obj as IMoInflAffMsa;
			if (inflMsa == null)
				return true; // play it safe
			if (!MoveableMSA(inflMsa, up))
				return true;
			UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoRemovingAffix, MEStrings.ksRedoRemovingAffix,
				Cache.ActionHandlerAccessor,
				() =>
				{
					List<ICmObject> vals = m_slot.Affixes.ToList<ICmObject>();
					int pos = vals.IndexOf(inflMsa);
					vals.RemoveAt(pos);
					if (up)
						vals.Insert(pos - 1, inflMsa);
					else
						vals.Insert(pos + 1, inflMsa);
					int flid = Cache.DomainDataByFlid.MetaDataCache.GetFieldId2(m_slot.ClassID, "Affixes", true);
					VirtualOrderingServices.SetVO(m_slot, flid, vals);
				});
			m_rootb.Reconstruct();  // work around because <choice> is not smart enough to remember its dependencies
			return true;    //we handled this.
		}

		private bool OtherInflAffixMsasExist(ILexEntry lex, IMoInflAffMsa inflMsa)
		{
			bool fOtherInflAffixMsasExist = false;  // assume we won't find an existing infl affix msa
			foreach (var msa in lex.MorphoSyntaxAnalysesOC)
			{
				if (msa.ClassID == MoInflAffMsaTags.kClassId)
				{ // is an inflectional affix msa
					if (msa != inflMsa)
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

			using (var chooser = MakeChooserWithExtantMsas(m_slot, cmd as XCore.Command))
			{
				chooser.ShowDialog();
				if (chooser.DialogResult == DialogResult.OK)
				{
					if (chooser.ChosenObjects != null && chooser.ChosenObjects.Count() > 0)
					{
						UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoAddAffixes, MEStrings.ksRedoAddAffixes,
							Cache.ActionHandlerAccessor,
							() =>
								{
									foreach (var obj in chooser.ChosenObjects)
									{
										AddInflAffixMsaToSlot(obj, m_slot);
									}
								});
					}
				}
			}
			return true;	//we handled this.
		}

		private void AddInflAffixMsaToSlot(ICmObject obj, IMoInflAffixSlot slot)
		{
			var inflMsa = obj as IMoInflAffMsa;
			if (inflMsa == null)
				return;
			var lex = inflMsa.OwnerOfClass<ILexEntry>();
			if (lex == null)
				return; // play it safe
			bool fMiamSet = false;  // assume we won't find an existing infl affix msa
			foreach (var msa in lex.MorphoSyntaxAnalysesOC)
			{
				if (msa.ClassID == MoInflAffMsaTags.kClassId)
				{ // is an inflectional affix msa
					var miam = (IMoInflAffMsa)msa;
					var pos = miam.PartOfSpeechRA;
					if (pos == null)
					{ // use the first unspecified one
						miam.PartOfSpeechRA = slot.OwnerOfClass<IPartOfSpeech>();
						miam.SlotsRC.Clear();  // just in case...
						miam.SlotsRC.Add(slot);
						fMiamSet = true;
						break;
					}
					else if (pos.AllAffixSlots.Contains(slot))
					{ // if the slot is in this POS
						if (miam.SlotsRC.Count == 0)
						{ // use the first available
							miam.SlotsRC.Add(slot);
							fMiamSet = true;
							break;
						}
						else if (miam.SlotsRC.Contains(slot))
						{ // it is already set (probably done by the CreateEntry dialog process)
							fMiamSet = true;
							break;
						}
						else if (lex.IsCircumfix())
						{ // only circumfixes can more than one slot
							miam.SlotsRC.Add(slot);
							fMiamSet = true;
							break;
						}
					}
				}
			}
			if (!fMiamSet)
			{  // need to create a new infl affix msa
				var newMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				lex.MorphoSyntaxAnalysesOC.Add(newMsa);
				EnsureNewMsaHasSense(lex, newMsa);
				newMsa.SlotsRC.Add(slot);
				newMsa.PartOfSpeechRA = slot.OwnerOfClass<IPartOfSpeech>();
			}
		}

		private void EnsureNewMsaHasSense(ILexEntry lex, IMoInflAffMsa newMsa)
		{
			// if no lexsense has this msa, copy first sense and have it refer to this msa
			bool fASenseHasMsa = false;
			foreach (var sense in lex.AllSenses)
			{
				if (sense.MorphoSyntaxAnalysisRA == newMsa)
				{
					fASenseHasMsa = true;
					break;
				}
			}
			if (!fASenseHasMsa)
			{
				var newSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				lex.SensesOS.Add(newSense);
				var firstSense = lex.SensesOS[0];
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
			var candidates = new HashSet<ICmObject>();
			bool fIsPrefixSlot = m_template.PrefixSlotsRS.Contains(slot);
			foreach (var lex in slot.OtherInflectionalAffixLexEntries)
			{
				bool fInclude = EntryHasAffixThatMightBeInSlot(lex, fIsPrefixSlot);
				if (fInclude)
				{
					foreach (var msa in lex.MorphoSyntaxAnalysesOC)
					{
						if (msa is IMoInflAffMsa)
						{
							candidates.Add(msa);
							break;
						}
					}
				}
			}
			var labels = ObjectLabel.CreateObjectLabels(Cache, candidates.OrderBy(iafmsa => iafmsa.Owner.ShortName), null);
			XCore.PersistenceProvider persistProvider = new PersistenceProvider(m_mediator, m_propertyTable);
			var aiForceMultipleChoices = new ICmObject[0];
			var chooser = new SimpleListChooser(persistProvider, labels,
				m_ChooseInflectionalAffixHelpTopic, Cache, aiForceMultipleChoices,
				m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
			chooser.SetHelpTopic("khtpChoose-Grammar-InflAffixTemplateControl");
			chooser.SetFontForDialog(new int[] { Cache.DefaultVernWs, Cache.DefaultAnalWs }, StyleSheet, WritingSystemFactory);
			chooser.Cache = Cache;
			// We don't want the ()'s indicating optionality since the text spells it out.
			chooser.TextParam = slot.Name.AnalysisDefaultWritingSystem.Text;
			chooser.Title = m_sInflAffixChooserTitle;
			if (slot.Optional)
				chooser.InstructionalText = m_sInflAffixChooserInstructionalTextOpt;
			else
				chooser.InstructionalText = m_sInflAffixChooserInstructionalTextReq;
			chooser.AddLink(m_sInflAffix, SimpleListChooser.LinkType.kDialogLink,
				new MakeInflAffixEntryChooserCommand(Cache, true, m_sInflAffix, fIsPrefixSlot, slot, m_mediator, m_propertyTable));
			chooser.SetObjectAndFlid(slot.Hvo, slot.OwningFlid);
			string sGuiControl = XmlUtils.GetOptionalAttributeValue(cmd.ConfigurationNode, "guicontrol");
			if (!String.IsNullOrEmpty(sGuiControl))
			{
				chooser.ReplaceTreeView(m_mediator, m_propertyTable, sGuiControl);
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
			var morphTypes = lex.MorphTypes;
			foreach (var morphType in morphTypes)
			{
				if (fIsPrefixSlot)
				{
					if (morphType.IsPrefixishType)
					{
						fInclude = true;
						break;
					}
				}
				else
				{
					// is a suffix slot
					if (morphType.IsSuffixishType)
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
				chooser.ShowDialog(this);
				if (chooser.ChosenOne != null)
				{
					var chosenSlot = chooser.ChosenOne.Object as IMoInflAffixSlot;
					int flid = 0;
					int ihvo = -1;
					if (m_obj.ClassID == MoInflAffixSlotTags.kClassId)
					{
						HandleInsertAroundSlot(fBefore, chosenSlot, out flid, out ihvo);
					}
					else if (m_obj.ClassID == MoInflAffixTemplateTags.kClassId)
					{
						HandleInsertAroundStem(fBefore, chosenSlot, out flid, out ihvo);
					}
					m_rootb.Reconstruct(); // Ensure that the table gets redrawn
					if (chooser.LinkExecuted)
					{
						// Select the header of the newly added slot in case the user wants to edit it.
						// See LT-8209.
						SelLevInfo[] rgvsli = new SelLevInfo[1];
						rgvsli[0].hvo = chosenSlot.Hvo;
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
			if (m_obj.ClassID == MoInflAffixTemplateTags.kClassId)
			{
				if (fBefore)
					fIsPrefixSlot = true;
				else
					fIsPrefixSlot = false;
			}
			else if (m_obj.ClassID == MoInflAffixSlotTags.kClassId)
			{
				if (m_template.PrefixSlotsRS.Contains(m_obj as IMoInflAffixSlot))
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
				slotFlid = MoInflAffixTemplateTags.kflidPrefixSlots;
			else
				slotFlid = MoInflAffixTemplateTags.kflidSuffixSlots;
			var labels = ObjectLabel.CreateObjectLabels(Cache, m_template.ReferenceTargetCandidates(slotFlid), null);
			PersistenceProvider persistProvider = new PersistenceProvider(m_mediator, m_propertyTable);
			SimpleListChooser chooser = new SimpleListChooser(persistProvider, labels,
				m_ChooseSlotHelpTopic, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
			chooser.SetHelpTopic("khtpChoose-Grammar-InflAffixTemplateControl");
			chooser.Cache = Cache;
			chooser.TextParamHvo = m_template.Owner.Hvo;
			chooser.Title = m_sSlotChooserTitle;
			chooser.InstructionalText = m_sSlotChooserInstructionalText;
			IPartOfSpeech pos = m_template.OwnerOfClass<IPartOfSpeech>();
			string sTopPOS = pos.ShortName;
			string sLabel = String.Format(m_sObligatorySlot, sTopPOS);
			chooser.AddLink(sLabel, SimpleListChooser.LinkType.kSimpleLink,
				new MakeInflAffixSlotChooserCommand(Cache, true, sLabel, pos.Hvo,
				false, m_mediator, m_propertyTable));
			sLabel = String.Format(m_sOptionalSlot, sTopPOS);
			chooser.AddLink(sLabel, SimpleListChooser.LinkType.kSimpleLink,
				new MakeInflAffixSlotChooserCommand(Cache, true, sLabel, pos.Hvo, true,
				m_mediator, m_propertyTable));
			chooser.SetObjectAndFlid(pos.Hvo, MoInflAffixTemplateTags.kflidSlots);
			return chooser;
		}

		private bool IsRTL()
		{
			return Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.RightToLeftScript;
		}

		private void HandleInsertAroundSlot(bool fBefore, IMoInflAffixSlot chosenSlot, out int flid, out int ihvo)
		{
			ILcmReferenceSequence<IMoInflAffixSlot> seq;
			int index;
			flid = GetAffixSequenceContainingSlot(m_obj as IMoInflAffixSlot, out seq, out index);
			int iOffset = (fBefore) ? 0 : 1;
			UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoAddSlot, MEStrings.ksRedoAddSlot, Cache.ActionHandlerAccessor,
				() => seq.Insert(index + iOffset, chosenSlot));
			// The views system numbers visually, so adjust index for RTL vernacular writing system.
			ihvo = index + iOffset;
			if (IsRTL())
				ihvo = (seq.Count - 1) - ihvo;
		}

		private void HandleInsertAroundStem(bool fBefore, IMoInflAffixSlot chosenSlot, out int flid, out int ihvo)
		{
			if (fBefore)
			{
				flid = MoInflAffixTemplateTags.kflidPrefixSlots;
				// The views system numbers visually, so adjust index for RTL vernacular writing system.
				if (IsRTL())
					ihvo = 0;
				else
					ihvo = m_template.PrefixSlotsRS.Count;
				UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoAddSlot, MEStrings.ksRedoAddSlot, Cache.ActionHandlerAccessor,
					() => m_template.PrefixSlotsRS.Add(chosenSlot));
			}
			else
			{
				flid = MoInflAffixTemplateTags.kflidSuffixSlots;
				// The views system numbers visually, so adjust index for RTL vernacular writing system.
				if (IsRTL())
					ihvo = m_template.SuffixSlotsRS.Count;
				else
					ihvo = 0;
				UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoAddSlot, MEStrings.ksRedoAddSlot, Cache.ActionHandlerAccessor,
					() => m_template.SuffixSlotsRS.Insert(0, chosenSlot));
			}
		}

		private int GetAffixSequenceContainingSlot(IMoInflAffixSlot slot, out ILcmReferenceSequence<IMoInflAffixSlot> seq, out int index)
		{
			index = m_template.PrefixSlotsRS.IndexOf(slot);
			if (index >= 0)
			{
				seq = m_template.PrefixSlotsRS;
				return MoInflAffixTemplateTags.kflidPrefixSlots;
			}
			else
			{
				index = m_template.SuffixSlotsRS.IndexOf(slot);
				if (index >= 0)
				{
					seq = m_template.SuffixSlotsRS;
					return MoInflAffixTemplateTags.kflidSuffixSlots;
				}
			}
			seq = null;
			return 0;
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
		//        display.Visible = display.Enabled = (helpTopicProvider.GetHelpString(m_ChooseInflectionalAffixHelpTopic) == null ? false : true);
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

			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), m_ChooseInflectionalAffixHelpTopic);
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

		/// <summary>
		/// Fix the name of any slot that is still "Type slot name here".
		/// </summary>
		/// <param name="slot"></param>
		private void FixSlotName(IMoInflAffixSlot slot)
		{
			if (slot.Name.AnalysisDefaultWritingSystem.Text == m_sNewSlotName)
				slot.Name.SetAnalysisDefaultWritingSystem(GetNextUnnamedSlotName());
		}

		/// <summary>
		/// Return true if no slot name needs to be adjusted by FixSlotName.
		/// </summary>
		private bool AllSlotNamesOk
		{
			get
			{
				foreach (var slot in m_template.PrefixSlotsRS)
				{
					if (slot.Name.AnalysisDefaultWritingSystem.Text == m_sNewSlotName)
						return false;
				}
				foreach (var slot in m_template.SuffixSlotsRS)
				{
					if (slot.Name.AnalysisDefaultWritingSystem.Text == m_sNewSlotName)
						return false;
				}
				return true;
			}
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
			var pos = m_template.OwnerOfClass<IPartOfSpeech>();
			while (pos != null)
			{
				foreach (IMoInflAffixSlot slot in pos.AffixSlotsOC)
				{
					if (slot.Name.AnalysisDefaultWritingSystem == null ||
						slot.Name.BestAnalysisAlternative.Text == null ||
						slot.Name.BestAnalysisAlternative.Text.StartsWith(m_sUnnamedSlotName))
					{
						int len = m_sUnnamedSlotName.Length;
						string sValue = slot.Name.BestAnalysisAlternative.Text.Substring(len);
						int i;
						try
						{
							i = Convert.ToInt32(sValue);
						}
						catch (Exception)
						{ // default to 9999 if what's after is not a number
							i = 9999; // use something very unlikely to happen normally
						}
						aiUnnamedSlotValues.Add(i);
					}
				}
				pos = pos.OwnerOfClass<IPartOfSpeech>();
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
			// During deletion of a Grammar Category, Windows/.Net can pass through here after
			// the template has been deleted, resulting a crash trying to verify the slot names
			// of the template.  See LT-13932.
			if (m_template.IsValidObject && !AllSlotNamesOk)
			{
				UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoChangeSlotName, MEStrings.ksRedoChangeSlotName,
					Cache.ActionHandlerAccessor,
					() =>
					{
						foreach (var slot in m_template.PrefixSlotsRS)
							FixSlotName(slot);
						foreach (var slot in m_template.SuffixSlotsRS)
							FixSlotName(slot);
					});
			}
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
			if (m_obj.ClassID == MoInflAffMsaTags.kClassId)
			{
				return DetermineMsaContextMenuItemLabel(sLabel);
			}
			else if (m_obj.ClassID == MoInflAffixSlotTags.kClassId)
			{
				m_slot = m_obj as IMoInflAffixSlot;
				return DoXXXReplace(sLabel, TsSlotName(m_slot));
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
			if (m_obj.ClassID == MoInflAffixSlotTags.kClassId)
			{
				return DoXXXReplace(sLabel, TsSlotName(m_obj as IMoInflAffixSlot));
			}
			else if (m_obj.ClassID == MoInflAffixTemplateTags.kClassId)
			{
				var tssStem = TsStringUtils.MakeString(m_sStem, Cache.DefaultUserWs);
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
			if (m_obj.ClassID != MoInflAffixSlotTags.kClassId)
			{
				fEnabled = false;
			}
			else
			{
				if (!SetEnabledIfFindSlotInSequence(m_template.PrefixSlotsRS, out fEnabled, fMoveLeft))
				{
					SetEnabledIfFindSlotInSequence(m_template.SuffixSlotsRS, out fEnabled, fMoveLeft);
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
			if (m_obj.ClassID == MoInflAffixSlotTags.kClassId)
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
			if (m_obj.ClassID == MoInflAffMsaTags.kClassId)
				return DetermineMsaContextMenuItemLabel(sLabel);
			else
				return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForInflTemplateMoveInflAffixMsa(string sLabel, bool up, out bool fEnabled)
		{
			CheckDisposed();
			fEnabled = MoveableMSA(m_obj as IMoInflAffMsa, up);
			if (m_obj.ClassID == MoInflAffMsaTags.kClassId)
				return DetermineMsaContextMenuItemLabel(sLabel);
			else
				return null;
		}

		internal bool MoveableMSA(IMoInflAffMsa msa, bool up)
		{
			if (m_slot == null)
				return false;
			List<ICmObject> vals = m_slot.Affixes.ToList<ICmObject>();
			int pos = vals.IndexOf(msa);
			if (up && pos == 0)
				return false; // Cannot move up.
			if (!up && pos == vals.Count - 1)
				return false; // Cannot move down.
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <returns></returns>
		internal ITsString MenuLabelForJumpToTool(string sLabel)
		{
			CheckDisposed();
			if (m_obj.ClassID == MoInflAffMsaTags.kClassId)
			{
				return TsStringUtils.MakeString(sLabel, Cache.DefaultUserWs);
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
			if (m_obj.ClassID == MoInflAffMsaTags.kClassId ||
				m_obj.ClassID == MoInflAffixSlotTags.kClassId ||
				m_obj.ClassID == MoInflAffixTemplateTags.kClassId)
			{
				// Display help only if there's a topic linked to the generated ID in the resource file.
				if (m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").GetHelpString(m_ChooseInflectionalAffixHelpTopic) != null)
					return TsStringUtils.MakeString(sLabel, Cache.DefaultUserWs);
			}
			return null;
		}

		private bool SetEnabledIfFindSlotInSequence(ILcmReferenceSequence<IMoInflAffixSlot> slots, out bool fEnabled, bool bIsLeft)
		{
			var index = slots.IndexOf(m_obj as IMoInflAffixSlot);
			if (index >= 0)
			{	// it was found
				bool bAtEdge;
				if (bIsLeft)
					bAtEdge = (index == 0);
				else
					bAtEdge = (index == slots.Count - 1);
				if (bAtEdge || slots.Count == 1)
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
			var msa = m_obj as IMoInflAffMsa;
			ITsString tss = DoYYYReplace(sLabel, msa.ShortNameTSS);
			ITsString tssSlotName = TsSlotNameOfMsa(msa);
			return DoXXXReplace(tss, tssSlotName);
		}

		private ITsString TsSlotNameOfMsa(IMoInflAffMsa msa)
		{
			ITsString tssResult = null;
			IMoInflAffixSlot slot = null;
#if !MaybeSomeDayToTryAndGetRemoveMsaCorrectForCircumfixes
			if (msa.SlotsRC.Count > 0)
			{
				slot = msa.SlotsRC.First();
			}
			tssResult = TsSlotName(slot);
			m_slot = slot;
#else
			slot = (FDO.Ling.MoInflAffixSlot)CmObject.CreateFromDBObject(this.Cache, m_hvoSlot);
			sResult = TsSlotName(slot);
#endif
			return tssResult;
		}

		private ITsString TsSlotName(IMoInflAffixSlot slot)
		{
			if (slot != null)
			{
				if (slot.Name.AnalysisDefaultWritingSystem.Text == m_sNewSlotName)
				{
					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
						() => slot.Name.SetAnalysisDefaultWritingSystem(GetNextUnnamedSlotName()));
				}
				return slot.Name.AnalysisDefaultWritingSystem;
			}
			else
			{
				return TsStringUtils.MakeString(MEStrings.ksQuestions, Cache.DefaultUserWs);
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
			ITsString tss = TsStringUtils.MakeString(sSource, Cache.DefaultUserWs);
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
