using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Windows.Forms;

using XCore;
using SIL.Utils;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// LexEntryMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// this class would normally be constructed by the factory method on DTMenuHandler,
	/// when the XML configuration of the RecordEditView specifies this class.
	///
	/// This is an IxCoreColleague, so it gets a chance to modify
	/// the display characteristics of the menu just before the menu is displayed.
	/// </summary>
	public class LexEntryMenuHandler : SIL.FieldWorks.XWorks.DTMenuHandler
	{
		//need a default constructor for dynamic loading
		public LexEntryMenuHandler()
		{
		}

		/// <summary>
		/// decide whether to display this tree insert Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayDataTreeInsert(object commandObject, ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null && m_dataEntryForm.Slices.Count > 0)
				slice = m_dataEntryForm.FieldAt(0);
			if (slice == null || slice.IsDisposed
				|| (m_dataEntryForm.Mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk).ListSize == 0)
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
			FDO.ILexEntry entry = slice.Object as ILexEntry;
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
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null && m_dataEntryForm.Slices.Count > 0)
				slice = m_dataEntryForm.FieldAt(0);
			if (slice == null
				|| (m_dataEntryForm.Mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk).ListSize == 0)
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
			FDO.ILexEntry entry = slice.Object as ILexEntry;
			if (entry == null)
				entry = slice.ContainingDataTree.Root as ILexEntry;
			display.Visible = entry != null;
			display.Enabled = entry != null;
			return true;
		}

		/// <summary>
		/// determine if this is the correct place [it's the only one that handles the message, and
		/// it defaults to false, so it should be]
		/// </summary>
		protected  bool InFriendlyAreaShow_DictionaryPubPreview
		{
			get
			{
				string desiredArea = "lexicon";

				// see if it's the right area
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
				if (areaChoice != null && areaChoice == desiredArea)
				{
					// now see if it's one of the right tools
					string[] allowedTools = {"simpleLexiconEdit", "lexiconTestEdit",
							"lexiconEdit", "lexiconEdit", "lexiconFullEdit"};
					string toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null);
					foreach (string tool in allowedTools)
					{
						if (toolChoice != null && toolChoice == tool)
							return true;
					}
				}
				return false; //we are not in a valid area
			}
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayShow_DictionaryPubPreview(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = InFriendlyAreaShow_DictionaryPubPreview;
			return true; //we've handled this
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
			Slice slice = m_dataEntryForm.CurrentSlice;
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
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			if (slice != null)
			{
				FdoCache cache = m_dataEntryForm.Cache;
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


		/// <summary>
		/// Extract information from the slice which will be needed to call NotifyVirtualChanged
		/// after the slice has been disposed.
		/// </summary>
		/// <param name="slice"></param>
		/// <param name="propName"></param>
		/// <param name="clid"></param>
		/// <param name="parent"></param>
		private static void GetNotifyVirtChgInfo(Slice slice, out string propName, out int clid,
			out Control parent)
		{
			XmlNode xa = null;
			XmlNode caller = slice.CallerNode;
			if (caller != null)
				xa = caller.Attributes["notifyVirtual"];
			if (xa == null)
				xa = slice.ConfigurationNode.Attributes["notifyVirtual"];
			propName = (xa != null) ? xa.Value : "";
			clid = (int)slice.Object.ClassID;
			parent = slice.Parent;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDemoteSense(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null || slice.Object == null ||
				(slice.Object.OwningFlid != LexSenseTags.kflidSenses) &&
				(slice.Object.OwningFlid != LexEntryTags.kflidSenses))
			{
				display.Enabled = false;
			}
			else
			{
				int chvo = m_dataEntryForm.Cache.DomainDataByFlid.get_VecSize(slice.Object.Owner.Hvo,
					(int)slice.Object.OwningFlid);
				display.Enabled = chvo > 1;
			}
			return true; //we've handled this
		}

		public bool OnPromoteSense(object cmd)
		{
			Command command = (Command) cmd;
			Slice slice = m_dataEntryForm.CurrentSlice;
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
			Slice slice = m_dataEntryForm.CurrentSlice;
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

		public virtual bool OnPictureProperties(object cmd)
		{
			// Command command = (Command)cmd; // CS0219
			Slice slice = m_dataEntryForm.CurrentSlice;
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
			Slice slice = m_dataEntryForm.CurrentSlice;
			ILexEntry entry = m_dataEntryForm.Root as ILexEntry;
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

		public virtual bool OnSwapLexemeWithAllomorph(object cmd)
		{
			ILexEntry entry = m_dataEntryForm.Root as ILexEntry;
			FdoCache cache = m_dataEntryForm.Cache;
			if (entry != null)
			{
				Form mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
				mainWindow.Cursor = Cursors.WaitCursor;
				using (SwapLexemeWithAllomorphDlg dlg = new SwapLexemeWithAllomorphDlg())
				{
					dlg.SetDlgInfo(cache, m_mediator, entry);
					if (DialogResult.OK == dlg.ShowDialog(mainWindow))
					{
						SwapAllomorphWithLexeme(entry, dlg.SelectedAllomorph, cmd as Command);
					}
				}
				mainWindow.Cursor = Cursors.Default;
			}
			return true;
		}

		public virtual bool OnDisplaySwapLexemeWithAllomorph(object commandObject, ref UIItemDisplayProperties display)
		{
			ILexEntry entry = m_dataEntryForm.Root as ILexEntry;
			bool enable = entry != null && entry.AlternateFormsOS.Count > 0;
			display.Visible = enable;
			display.Enabled = enable;
			return true;
		}

		public virtual bool OnDisplayConvertLexemeForm(object commandObject, ref UIItemDisplayProperties display)
		{
			Command cmd = commandObject as Command;
			int fromClsid = m_dataEntryForm.Cache.MetaDataCacheAccessor.GetClassId(cmd.GetParameter("fromClassName"));
			ILexEntry entry = m_dataEntryForm.Root as ILexEntry;
			bool enable = entry != null && fromClsid != 0 && entry.LexemeFormOA.ClassID == fromClsid;
			display.Visible = enable;
			display.Enabled = enable;
			return true;
		}

		public bool OnConvertLexemeForm(object cmd)
		{
			var command = cmd as Command;
			var toClsid = m_dataEntryForm.Cache.MetaDataCacheAccessor.GetClassId(command.GetParameter("toClassName"));
			var entry = m_dataEntryForm.Root as ILexEntry;
			if (entry != null)
			{
				if (CheckForFormDataLoss(entry.LexemeFormOA, toClsid))
				{
					var mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
					IMoForm newForm = null;
					using (new WaitCursor(mainWindow))
					{
						UndoableUnitOfWorkHelper.Do(command.UndoText, command.RedoText, entry, () =>
						{
							newForm = CreateNewForm(entry, toClsid);
							entry.ReplaceMoForm(entry.LexemeFormOA, newForm);
						});
						m_dataEntryForm.RefreshList(false);
					}
					SelectNewFormSlice(newForm);
				}
			}
			return true;
		}

		public virtual bool OnDisplayConvertAllomorph(object commandObject, ref UIItemDisplayProperties display)
		{
			Command cmd = commandObject as Command;
			int fromClsid = m_dataEntryForm.Cache.MetaDataCacheAccessor.GetClassId(cmd.GetParameter("fromClassName"));
			Slice slice = m_dataEntryForm.CurrentSlice;
			IMoForm allomorph = slice.Object as IMoForm;
			bool enable = allomorph != null && fromClsid != 0 && allomorph.ClassID == fromClsid;
			display.Visible = enable;
			display.Enabled = enable;
			return true;
		}

		public bool OnConvertAllomorph(object cmd)
		{
			var command = cmd as Command;
			int toClsid = (int)m_dataEntryForm.Cache.MetaDataCacheAccessor.GetClassId(command.GetParameter("toClassName"));
			var entry = m_dataEntryForm.Root as ILexEntry;
			var slice = m_dataEntryForm.CurrentSlice;
			var allomorph = slice.Object as IMoForm;
			if (entry != null && allomorph != null && toClsid != 0)
			{
				if (CheckForFormDataLoss(allomorph, toClsid))
				{
					var mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
					IMoForm newForm = null;
					using (new WaitCursor(mainWindow))
					{
						UndoableUnitOfWorkHelper.Do(command.UndoText, command.RedoText, entry, () =>
						{
							newForm = CreateNewForm(entry, toClsid);
							entry.ReplaceMoForm(allomorph, newForm);
						});
						m_dataEntryForm.RefreshList(false);
					}
					SelectNewFormSlice(newForm);
				}
			}
			return true;
		}

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
			foreach (Slice slice in m_dataEntryForm.Slices)
			{
				if (slice.Object.Hvo == newForm.Hvo)
				{
					m_dataEntryForm.ActiveControl = slice;
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
						msg = LexEdStrings.ksConvertFormLoseEnvInfixLocGramInfo;
					else if (loseEnv && losePos)
						msg = LexEdStrings.ksConvertFormLoseEnvInfixLoc;
					else if (loseEnv && loseGram)
						msg = LexEdStrings.ksConvertFormLoseEnvGramInfo;
					else if (losePos && loseGram)
						msg = LexEdStrings.ksConvertFormLoseInfixLocGramInfo;
					else if (loseEnv)
						msg = LexEdStrings.ksConvertFormLoseEnv;
					else if (losePos)
						msg = LexEdStrings.ksConvertFormLoseInfixLoc;
					else if (loseGram)
						msg = LexEdStrings.ksConvertFormLoseGramInfo;
					break;

				case MoAffixProcessTags.kClassId:
					msg = LexEdStrings.ksConvertFormLoseRule;
					break;

				case MoStemAllomorphTags.kClassId:
					// not implemented
					break;
			}

			if (msg != null)
			{
				DialogResult result = MessageBox.Show(msg, LexEdStrings.ksConvertFormLoseCaption,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				return result == DialogResult.Yes;
			}

			return true;
		}
	}
}
