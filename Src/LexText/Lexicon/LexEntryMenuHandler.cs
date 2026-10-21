// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

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
	public class LexEntryMenuHandler : DTMenuHandler
	{
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
				|| (m_propertyTable.GetValue<RecordClerk>("ActiveClerk")).ListSize == 0)
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
			var offered = DetailObjectInsertion.IsOfferedOnEntry(entry, command.Id);
			if (command.Id.EndsWith("AffixProcess"))
			{
				display.Enabled = offered;
				display.Visible = offered;
			}
			else if (!offered)
			{
				display.Visible = false;
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
				|| (m_propertyTable.GetValue<RecordClerk>("ActiveClerk")).ListSize == 0)
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
		/// determine if this is the correct place [it's the only one that handles the message, and
		/// it defaults to false, so it should be]
		/// </summary>
		protected  bool InFriendlyAreaShow_DictionaryPubPreview
		{
			get
			{
				string desiredArea = "lexicon";

				// see if it's the right area
				string areaChoice = m_propertyTable.GetStringProperty("areaChoice", null);
				if (areaChoice != null && areaChoice == desiredArea)
				{
					// now see if it's one of the right tools
					string[] allowedTools = {"simpleLexiconEdit", "lexiconTestEdit",
							"lexiconEdit", "lexiconEdit", "lexiconFullEdit"};
					string toolChoice = m_propertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null);
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
				LcmCache cache = m_dataEntryForm.Cache;
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

		public virtual bool OnSwapAllomorphWithLexeme(object cmd)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			ILexEntry entry = m_dataEntryForm.Root as ILexEntry;
			IMoForm allomorph = slice.Object as IMoForm;
			if (entry != null && allomorph != null)
			{
				var command = (Command)cmd;
				LexemeFormConversion.Swap(entry, allomorph, command.UndoText, command.RedoText);
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
			LcmCache cache = m_dataEntryForm.Cache;
			if (entry != null)
			{
				Form mainWindow = m_propertyTable.GetValue<Form>("window");
				using (new WaitCursor(mainWindow))
				{
					using (SwapLexemeWithAllomorphDlg dlg = new SwapLexemeWithAllomorphDlg())
					{
						dlg.SetDlgInfo(cache, m_propertyTable, entry);
						if (DialogResult.OK == dlg.ShowDialog(mainWindow))
						{
							var command = (Command)cmd;
							LexemeFormConversion.Swap(entry, dlg.SelectedAllomorph, command.UndoText, command.RedoText);
						}
					}
				}
			}
			return true;
		}

		public virtual bool OnDisplaySwapLexemeWithAllomorph(object commandObject, ref UIItemDisplayProperties display)
		{
			bool enable = LexemeFormConversion.CanSwap(m_dataEntryForm.Root as ILexEntry);
			display.Visible = enable;
			display.Enabled = enable;
			return true;
		}

		public virtual bool OnDisplayConvertLexemeForm(object commandObject, ref UIItemDisplayProperties display)
		{
			Command cmd = commandObject as Command;
			int fromClsid = m_dataEntryForm.Cache.MetaDataCacheAccessor.GetClassId(cmd.GetParameter("fromClassName"));
			ILexEntry entry = m_dataEntryForm.Root as ILexEntry;
			bool enable = entry != null && LexemeFormConversion.CanConvert(entry.LexemeFormOA, fromClsid);
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
				ConvertForm(entry, entry.LexemeFormOA, toClsid, command);
			return true;
		}

		public virtual bool OnDisplayConvertAllomorph(object commandObject, ref UIItemDisplayProperties display)
		{
			Command cmd = commandObject as Command;
			int fromClsid = m_dataEntryForm.Cache.MetaDataCacheAccessor.GetClassId(cmd.GetParameter("fromClassName"));
			Slice slice = m_dataEntryForm.CurrentSlice;
			IMoForm allomorph = slice.Object as IMoForm;
			bool enable = LexemeFormConversion.CanConvert(allomorph, fromClsid);
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
				ConvertForm(entry, allomorph, toClsid, command);
			return true;
		}

		// Converts the form after the user accepts any data loss, then shows and selects the
		// new form's slice.
		private void ConvertForm(ILexEntry entry, IMoForm form, int toClsid, Command command)
		{
			if (!CheckForFormDataLoss(form))
				return;
			var mainWindow = m_propertyTable.GetValue<Form>("window");
			IMoForm newForm;
			using (new WaitCursor(mainWindow))
			{
				newForm = LexemeFormConversion.Convert(entry, form, toClsid, command.UndoText, command.RedoText);
				m_dataEntryForm.RefreshList(false);
			}
			SelectNewFormSlice(newForm);
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

		bool CheckForFormDataLoss(IMoForm origForm)
		{
			string msg = LexemeFormConversion.DataLossMessage(origForm);
			if (msg != null)
			{
				DialogResult result = MessageBox.Show(msg, LexemeFormConversion.DataLossCaption,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				return result == DialogResult.Yes;
			}

			return true;
		}
	}
}
