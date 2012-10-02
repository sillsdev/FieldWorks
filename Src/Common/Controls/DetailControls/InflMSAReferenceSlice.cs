// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InflMSAReferenceSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using XCore;


namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This class is used to manage handling of the PartOfSpeech property of a MoInflAffMsa.
	/// Depending on the value of that property, the Slot property needs to be kept valid.
	/// </summary>
	public class InflMSAReferenceSlice : AtomicReferenceSlice
	{
		public InflMSAReferenceSlice()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AtomicReferenceSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public InflMSAReferenceSlice(FdoCache cache, ICmObject obj, int flid,
			XmlNode configurationNode, IPersistenceProvider persistenceProvider,
			Mediator mediator, StringTable stringTbl)
			: base(cache, obj, flid, configurationNode, persistenceProvider, mediator, stringTbl)
		{
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				AtomicReferenceLauncher arl = Control as AtomicReferenceLauncher;
				arl.ReferenceChanged -= new FwSelectionChangedEventHandler(OnReferenceChanged);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		///
		/// </summary>
		/// <param name="persistenceProvider"></param>
		/// <param name="stringTbl"></param>
		protected override void SetupControls(IPersistenceProvider persistenceProvider,
			Mediator mediator, StringTable stringTbl)
		{
			base.SetupControls(persistenceProvider, mediator, stringTbl);
			AtomicReferenceLauncher arl = Control as AtomicReferenceLauncher;
			arl.ReferenceChanged += new FwSelectionChangedEventHandler(OnReferenceChanged);
		}

		#region Event handlers

		/// <summary>
		/// Handle interaction between POS and Slot ptoeprties for a inflectional affix MSA.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>
		/// If the new value is zero, then set the Slot prop to zero.
		/// If the new value is not zero, then make sure the Slot prop is valid.
		///		If the current slot is not legal for the new POS value, then set it to zero.
		///		Otherwise leave the slot value alone.
		/// </remarks>
		protected void OnReferenceChanged(object sender, SIL.FieldWorks.Common.Utils.FwObjectSelectionEventArgs e)
		{
			Debug.Assert(sender is AtomicReferenceLauncher);
			AtomicReferenceLauncher source = (AtomicReferenceLauncher)sender;
			Debug.Assert(Control == source);
			Debug.Assert(Object is MoInflAffMsa);

			int idxSender = Parent.Controls.IndexOf(this);

			int otherFlid = (int)MoInflAffMsa.MoInflAffMsaTags.kflidSlots;
			Slice otherSlice = null;
			int idxOther;

			// Try to get the Slots slice.
			// Check for slices before this one.
			if (idxSender > 0)
			{
				idxOther = idxSender - 1;
				while (idxOther >= 0
					&& (otherSlice == null
						|| (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.Object == Object)))
				{
					otherSlice = (Slice)Parent.Controls[idxOther--];
					if (otherSlice is ReferenceVectorSlice && (otherSlice as ReferenceVectorSlice).Flid == otherFlid)
						break;
					otherSlice = null;
				}
			}

			// Check for following slices, if we didn't get one earlier.
			if (otherSlice == null && idxSender < Parent.Controls.Count)
			{
				idxOther = idxSender + 1;
				while (idxOther < Parent.Controls.Count
					&& (otherSlice == null
						|| (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.Object == Object)))
				{
					otherSlice = (Slice)Parent.Controls[idxOther++];
					if (otherSlice is ReferenceVectorSlice && (otherSlice as ReferenceVectorSlice).Flid == otherFlid)
						break;
					otherSlice = null;
				}
			}

			VectorReferenceLauncher otherControl = null;
			if (otherSlice != null)
			{
				Debug.Assert((otherSlice as ReferenceVectorSlice).Flid == otherFlid);
				Debug.Assert(otherSlice.Object == Object);
				otherControl = otherSlice.Control as VectorReferenceLauncher;
				Debug.Assert(otherControl != null);
			}

			MoInflAffMsa msa = Object as MoInflAffMsa;
			int slotHvo = 0;
			if (msa.SlotsRC.Count > 0)
			{
				int[] hvos = msa.SlotsRC.HvoArray;
				slotHvo = hvos[0];
			}
			if (e.Hvo == 0 || slotHvo > 0)
			{
				IPartOfSpeech pos = msa.PartOfSpeechRA;
				List<int> slotIDs = new List<int>();
				if (pos != null)
					slotIDs = pos.AllAffixSlotIDs;
				bool clearSlot = (   (e.Hvo == 0)
								  || (!slotIDs.Contains(slotHvo)));
				if (clearSlot)
				{
					if (otherControl == null)
						msa.ClearAllSlots(); // The slot slice is not showing, so directly set the object's Slot property.
					else
						otherControl.AddItem(0); // Reset it using the other slice, so it gets refreshed.
				}
			}
		}

		#endregion Event handlers
	}
}
