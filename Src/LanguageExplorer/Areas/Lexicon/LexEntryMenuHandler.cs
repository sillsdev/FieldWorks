// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel;
using LanguageExplorer.Works;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Lexicon
{
#if RANDYTODO
	// TODO: I don't think subclassing DTMenuHandler will be needed/supported in the new world order.
	/// <summary>
	/// LexEntryMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// </summary>
	internal sealed class LexEntryMenuHandler : DTMenuHandler
	{
		/// <summary>
		/// Need a default constructor for dynamic loading
		/// </summary>
		public LexEntryMenuHandler()
		{
		}

#if RANDYTODO
		/// <summary>
		/// decide whether to display this tree insert Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayDataTreeInsert(object commandObject, ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataTree.CurrentSlice;
			if (slice == null && m_dataTree.Slices.Count > 0)
				slice = m_dataTree.FieldAt(0);
			if (slice == null || slice.IsDisposed
				|| (RecordClerk.RecordClerkRepository.ActiveRecordClerk.ListSize == 0)
			{
				// don't display the datatree menu/toolbar items when we don't have a data tree slice.
				// (If the slice is disposed, we're in a weird state, possibly trying to update the toolbar during OnIdle though we haven't
				// in fact finished reconstructing the tree. Leave things disabled, and hope they will get enabled
				// on the next call when things have stabilized.)
				display.Visible = false;
				display.Enabled = false;
				return true;
			}

			base.OnDisplayDataTreeInsert(commandObject, ref display);

			if (!(slice.Object is ILexEntry) && !(slice.ContainingDataTree.Root is ILexEntry))
				return false;
			ILexEntry entry = slice.Object as ILexEntry;
			if (entry == null)
				entry = slice.ContainingDataTree.Root as ILexEntry;
			if (entry == null || !entry.IsValidObject)
			{
				// At one point this could happen during delete object. Not sure it will be possible when I
				// finish debugging that, but the defensive programming doesn't hurt.
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			XCore.Command command = (XCore.Command)commandObject;

			if (command.Id.EndsWith("AffixProcess"))
			{
				var mmt = entry.PrimaryMorphType;
				bool enable = mmt != null && mmt.IsAffixType;
				display.Enabled = enable;
				display.Visible = enable;
				return true;
			}

			//if there aren't any alternate forms, go ahead and let the user choose either kind
			if (entry.AlternateFormsOS.Count==0)
				return true;

			if (command.Id.EndsWith("AffixAllomorph"))
			{
				if (!(entry.AlternateFormsOS[0] is IMoAffixAllomorph))
					display.Visible = false;
				return true;
			}

			if (command.Id.EndsWith("StemAllomorph"))
			{
				if (!(entry.AlternateFormsOS[0] is IMoStemAllomorph))
					display.Visible = false;
				return true;
			}

			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// We want to be able to insert a sound/movie file for a pronunciation, even when the
		/// pronunciation doesn't yet exist.  See LT-6685.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayInsertMediaFile(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataTree.CurrentSlice;
			if (slice == null && m_dataTree.Slices.Count > 0)
				slice = m_dataTree.FieldAt(0);
			if (slice == null
				|| (RecordClerk.RecordClerkRepository.ActiveRecordClerk.ListSize == 0)
			{
				// don't display the datatree menu/toolbar items when we don't have a data tree slice.
				display.Visible = false;
				display.Enabled = false;
				return true;
			}
			display.Enabled = false;
			base.OnDisplayInsertMediaFile(commandObject, ref display);
			if (display.Enabled)
				return true;
			if (!(slice.Object is ILexEntry) && !(slice.ContainingDataTree.Root is ILexEntry))
				return false;
			ILexEntry entry = slice.Object as ILexEntry;
			if (entry == null)
				entry = slice.ContainingDataTree.Root as ILexEntry;
			display.Visible = entry != null;
			display.Enabled = entry != null;
			return true;
		}

		/// <summary>
		/// Handle the message to delete a Sense. The Sense # slice is virtual, therefore
		/// we need to issue a notification of virtualPropertyChange so that the numbering on
		/// other remaining senses is corrected.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns>true to indicate the message was handled</returns>
		public bool OnDataTreeDeleteSense(object cmd)
		{
			Command command = (Command)cmd;
			Slice slice = m_dataTree.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			Debug.Assert(!slice.IsDisposed, "The current slice is already disposed??");
			if (slice != null)
			{
				slice.HandleDeleteCommand(command);
			}
			return true;	//we handled this.
		}

		public bool OnDemoteSense(object cmd)
		{
			Command command = (Command) cmd;
			Slice slice = m_dataTree.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			if (slice != null)
			{
				LcmCache cache = m_dataTree.Cache;
				int hvoOwner = slice.Object.Owner.Hvo;
				int flid = slice.Object.OwningFlid;
				int chvo = cache.DomainDataByFlid.get_VecSize(hvoOwner, flid);
				int ihvo = cache.DomainDataByFlid.GetObjIndex(hvoOwner, flid, slice.Object.Hvo);
				Debug.Assert(ihvo >= 0);
				if (ihvo >= 0)
				{
					int ihvoNewOwner = (ihvo == 0) ? 1 : ihvo - 1;
					int hvoNewOwner = cache.DomainDataByFlid.get_VecItem(hvoOwner, flid, ihvoNewOwner);
					int chvoDst = cache.DomainDataByFlid.get_VecSize(hvoNewOwner,
						LexSenseTags.kflidSenses);
					UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoDemote, LexEdStrings.ksRedoDemote, Cache.ActionHandlerAccessor,
						()=>cache.DomainDataByFlid.MoveOwnSeq(hvoOwner, flid, ihvo, ihvo, hvoNewOwner,
						LexSenseTags.kflidSenses, chvoDst));
				}
			}
			return true;
		}

		/// <summary>
		/// decide whether to enable this tree delete Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeDeleteSense(object commandObject,
			ref UIItemDisplayProperties display)
		{
			return OnDisplayDataTreeDelete(commandObject, ref display); //we handled this, no need to ask anyone else.
		}
#endif

#if RANDYTODO
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDemoteSense(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataTree.CurrentSlice;
			if (slice == null || slice.Object == null ||
				(slice.Object.OwningFlid != LexSenseTags.kflidSenses) &&
				(slice.Object.OwningFlid != LexEntryTags.kflidSenses))
			{
				display.Enabled = false;
			}
			else
			{
				int chvo = m_dataTree.Cache.DomainDataByFlid.get_VecSize(slice.Object.Owner.Hvo,
					(int)slice.Object.OwningFlid);
				display.Enabled = chvo > 1;
			}
			return true; //we've handled this
		}

		public bool OnPromoteSense(object cmd)
		{
			Command command = (Command) cmd;
			Slice slice = m_dataTree.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			if (slice != null)
			{
				var target = slice.Object as ILexSense;
				var oldOwner = slice.Object.Owner as ILexSense;
				if (target == null || oldOwner == null)
					return true; // done, but can't promote top-level sense or something that isn't one.
				var index = oldOwner.IndexInOwner;
				var newOwner = oldOwner.Owner;
				UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoDemote, LexEdStrings.ksRedoDemote, Cache.ActionHandlerAccessor,
					() =>
						{
							if (newOwner is ILexEntry)
							{
								var newOwningEntry = (ILexEntry) newOwner;
								newOwningEntry.SensesOS.Insert(index + 1, target);
							}
							else
							{
								var newOwningSense = (ILexSense) newOwner;
								newOwningSense.SensesOS.Insert(index + 1, target);
							}
						});
			}
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayPromoteSense(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataTree.CurrentSlice;
			if (slice == null || slice.Object == null ||
				slice.Object.OwningFlid != LexSenseTags.kflidSenses)
			{
				display.Enabled = false;
			}
			else
			{
				display.Enabled = true;
			}
			return true; //we've handled this
		}
#endif

		public bool OnPictureProperties(object cmd)
		{
			Slice slice = m_dataTree.CurrentSlice;
			if (slice != null)
			{
				List<PictureSlice> slices = new List<PictureSlice>();

				// Create an array of potential slices to call the showProperties method on.  If we're being called from a PictureSlice,
				// there's no need to go through the whole list, so we can be a little more intelligent
				if (slice is PictureSlice)
				{
					slices.Add(slice as PictureSlice);
				}
				else
				{
					foreach (Slice oslice in slice.ContainingDataTree.Slices)
					{
						if (oslice is PictureSlice)
							slices.Add(oslice as PictureSlice);
					}
				}

				foreach (PictureSlice pslice in slices)
				{
					// Make sure the target slice refers to the same object that we do
					if (pslice.Object == slice.Object)
					{
						pslice.showProperties();
						break;
					}
				}
			}

			return true; // we've handled this
		}

#if RANDYTODO
		public virtual bool OnDisplayPictureProperties(object commandObject, ref UIItemDisplayProperties display)
		{
			// It is always possible to access the properties of a picture if we're on a picture slice, which is
			// the only time this menu item will be displayed
			display.Visible = true;
			display.Enabled = true;
			return true;
		}

		private void SwapAllomorphWithLexeme(ILexEntry entry, IMoForm allomorph, Command cmd)
		{
			UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, entry, () =>
			{
				entry.AlternateFormsOS.Insert(allomorph.IndexInOwner, entry.LexemeFormOA);
				entry.LexemeFormOA = allomorph;
			});
		}

		public virtual bool OnSwapAllomorphWithLexeme(object cmd)
		{
			Slice slice = m_dataTree.CurrentSlice;
			ILexEntry entry = m_dataTree.Root as ILexEntry;
			IMoForm allomorph = slice.Object as IMoForm;
			if (entry != null && allomorph != null)
			{
				SwapAllomorphWithLexeme(entry, allomorph, cmd as Command);
			}
			return true;
		}

		public virtual bool OnDisplaySwapAllomorphWithLexeme(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Visible = true;
			display.Enabled = true;
			return true;
		}
#endif

		/// <summary />
		public bool OnSwapLexemeWithAllomorph(object cmd)
		{
			ILexEntry entry = m_dataTree.Root as ILexEntry;
			LcmCache cache = m_dataTree.Cache;
			if (entry != null)
			{
				Form mainWindow = PropertyTable.GetValue<Form>("window");
				using (new WaitCursor(mainWindow))
				{
					using (SwapLexemeWithAllomorphDlg dlg = new SwapLexemeWithAllomorphDlg())
					{
						dlg.SetDlgInfo(cache, PropertyTable, entry);
						if (DialogResult.OK == dlg.ShowDialog(mainWindow))
						{
#if RANDYTODO
							SwapAllomorphWithLexeme(entry, dlg.SelectedAllomorph, cmd as Command);
#endif
						}
					}
				}
			}
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplaySwapLexemeWithAllomorph(object commandObject, ref UIItemDisplayProperties display)
		{
			ILexEntry entry = m_dataTree.Root as ILexEntry;
			bool enable = entry != null && entry.AlternateFormsOS.Count > 0;
			display.Visible = enable;
			display.Enabled = enable;
			return true;
		}

		public virtual bool OnDisplayConvertLexemeForm(object commandObject, ref UIItemDisplayProperties display)
		{
			Command cmd = commandObject as Command;
			int fromClsid = m_dataTree.Cache.MetaDataCacheAccessor.GetClassId(cmd.GetParameter("fromClassName"));
			ILexEntry entry = m_dataTree.Root as ILexEntry;
			bool enable = entry != null && fromClsid != 0 && entry.LexemeFormOA.ClassID == fromClsid;
			display.Visible = enable;
			display.Enabled = enable;
			return true;
		}

		public bool OnConvertLexemeForm(object cmd)
		{
			var command = cmd as Command;
			var toClsid = m_dataTree.Cache.MetaDataCacheAccessor.GetClassId(command.GetParameter("toClassName"));
			var entry = m_dataTree.Root as ILexEntry;
			if (entry != null)
			{
				if (CheckForFormDataLoss(entry.LexemeFormOA, toClsid))
				{
					var mainWindow = PropertyTable.GetValue<Form>("window");
					IMoForm newForm = null;
					using (new WaitCursor(mainWindow))
					{
						UndoableUnitOfWorkHelper.Do(command.UndoText, command.RedoText, entry, () =>
						{
							newForm = CreateNewForm(entry, toClsid);
							entry.ReplaceMoForm(entry.LexemeFormOA, newForm);
						});
						m_dataTree.RefreshList(false);
					}
					SelectNewFormSlice(newForm);
				}
			}
			return true;
		}

		public virtual bool OnDisplayConvertAllomorph(object commandObject, ref UIItemDisplayProperties display)
		{
			Command cmd = commandObject as Command;
			int fromClsid = m_dataTree.Cache.MetaDataCacheAccessor.GetClassId(cmd.GetParameter("fromClassName"));
			Slice slice = m_dataTree.CurrentSlice;
			IMoForm allomorph = slice.Object as IMoForm;
			bool enable = allomorph != null && fromClsid != 0 && allomorph.ClassID == fromClsid;
			display.Visible = enable;
			display.Enabled = enable;
			return true;
		}

		public bool OnConvertAllomorph(object cmd)
		{
			var command = cmd as Command;
			int toClsid = (int)m_dataTree.Cache.MetaDataCacheAccessor.GetClassId(command.GetParameter("toClassName"));
			var entry = m_dataTree.Root as ILexEntry;
			var slice = m_dataTree.CurrentSlice;
			var allomorph = slice.Object as IMoForm;
			if (entry != null && allomorph != null && toClsid != 0)
			{
				if (CheckForFormDataLoss(allomorph, toClsid))
				{
					var mainWindow = PropertyTable.GetValue<Form>("window");
					IMoForm newForm = null;
					using (new WaitCursor(mainWindow))
					{
						UndoableUnitOfWorkHelper.Do(command.UndoText, command.RedoText, entry, () =>
						{
							newForm = CreateNewForm(entry, toClsid);
							entry.ReplaceMoForm(allomorph, newForm);
						});
						m_dataTree.RefreshList(false);
					}
					SelectNewFormSlice(newForm);
				}
			}
			return true;
		}
#endif

		IMoForm CreateNewForm(ILexEntry parent, int clsid)
		{
			switch (clsid)
			{
				case MoAffixProcessTags.kClassId:
					return parent.Services.GetInstance<IMoAffixProcessFactory>().Create();

				case MoAffixAllomorphTags.kClassId:
					return parent.Services.GetInstance<IMoAffixAllomorphFactory>().Create();

				case MoStemAllomorphTags.kClassId:
					return parent.Services.GetInstance<IMoStemAllomorphFactory>().Create();
			}
			return null;
		}

		void SelectNewFormSlice(IMoForm newForm)
		{
			foreach (Slice slice in m_dataTree.Slices)
			{
				if (slice.Object.Hvo == newForm.Hvo)
				{
					m_dataTree.ActiveControl = slice;
					break;
				}
			}
		}

		bool CheckForFormDataLoss(IMoForm origForm, int toClsid)
		{
			string msg = null;
			switch (origForm.ClassID)
			{
				case MoAffixAllomorphTags.kClassId:
					IMoAffixAllomorph affAllo = origForm as IMoAffixAllomorph;
					bool loseEnv = affAllo.PhoneEnvRC.Count > 0;
					bool losePos = affAllo.PositionRS.Count > 0;
					bool loseGram = affAllo.MsEnvFeaturesOA != null || affAllo.MsEnvPartOfSpeechRA != null;
					if (loseEnv && losePos && loseGram)
						msg = LanguageExplorerResources.ksConvertFormLoseEnvInfixLocGramInfo;
					else if (loseEnv && losePos)
						msg = LanguageExplorerResources.ksConvertFormLoseEnvInfixLoc;
					else if (loseEnv && loseGram)
						msg = LanguageExplorerResources.ksConvertFormLoseEnvGramInfo;
					else if (losePos && loseGram)
						msg = LanguageExplorerResources.ksConvertFormLoseInfixLocGramInfo;
					else if (loseEnv)
						msg = LanguageExplorerResources.ksConvertFormLoseEnv;
					else if (losePos)
						msg = LanguageExplorerResources.ksConvertFormLoseInfixLoc;
					else if (loseGram)
						msg = LanguageExplorerResources.ksConvertFormLoseGramInfo;
					break;

				case MoAffixProcessTags.kClassId:
					msg = LanguageExplorerResources.ksConvertFormLoseRule;
					break;

				case MoStemAllomorphTags.kClassId:
					// not implemented
					break;
			}

			if (msg != null)
			{
				DialogResult result = MessageBox.Show(msg, LanguageExplorerResources.ksConvertFormLoseCaption,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				return result == DialogResult.Yes;
			}

			return true;
		}
	}
#endif
}
