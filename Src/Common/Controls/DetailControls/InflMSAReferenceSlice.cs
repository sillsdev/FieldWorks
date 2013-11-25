// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: InflMSAReferenceSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Diagnostics;
using System.Linq;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;


namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This class is used to manage handling of the PartOfSpeech property of a MoInflAffMsa.
	/// Depending on the value of that property, the Slot property needs to be kept valid.
	/// </summary>
	public class InflMSAReferenceSlice : AtomicReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InflMSAReferenceSlice"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">The flid.</param>
		public InflMSAReferenceSlice(FdoCache cache, ICmObject obj, int flid)
			: base(cache, obj, flid)
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
				var arl = (AtomicReferenceLauncher)Control;
				arl.ReferenceChanged -= OnReferenceChanged;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void FinishInit()
		{
			base.FinishInit();
			var arl = (AtomicReferenceLauncher)Control;
			arl.ReferenceChanged += OnReferenceChanged;
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
		protected void OnReferenceChanged(object sender, FwObjectSelectionEventArgs e)
		{
			Debug.Assert(sender is AtomicReferenceLauncher);
			var source = (AtomicReferenceLauncher)sender;
			Debug.Assert(Control == source);
			Debug.Assert(Object is IMoInflAffMsa);

			int idxSender = ContainingDataTree.Slices.IndexOf(this);

			int otherFlid = MoInflAffMsaTags.kflidSlots;
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
					otherSlice = ContainingDataTree.Slices[idxOther--];
					if (otherSlice is ReferenceVectorSlice && (otherSlice as ReferenceVectorSlice).Flid == otherFlid)
						break;
					otherSlice = null;
				}
			}

			// Check for following slices, if we didn't get one earlier.
			if (otherSlice == null && idxSender < ContainingDataTree.Slices.Count)
			{
				idxOther = idxSender + 1;
				while (idxOther < ContainingDataTree.Slices.Count
					&& (otherSlice == null
						|| (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.Object == Object)))
				{
					otherSlice = ContainingDataTree.Slices[idxOther++];
					if (otherSlice is ReferenceVectorSlice && (otherSlice as ReferenceVectorSlice).Flid == otherFlid)
						break;
					otherSlice = null;
				}
			}

			VectorReferenceLauncher otherControl = null;
			if (otherSlice != null)
			{
				Debug.Assert(otherSlice.Flid == otherFlid);
				Debug.Assert(otherSlice.Object == Object);
				otherControl = otherSlice.Control as VectorReferenceLauncher;
				Debug.Assert(otherControl != null);
			}

			var msa = Object as IMoInflAffMsa;
			IMoInflAffixSlot slot = null;
			if (msa.SlotsRC.Count > 0)
			{
				slot = msa.SlotsRC.First();
			}
			if (e.Hvo == 0 || slot != null)
			{
				var pos = msa.PartOfSpeechRA;
				var slots = pos != null ? pos.AllAffixSlots : Enumerable.Empty<IMoInflAffixSlot>();
				bool clearSlot = e.Hvo == 0 || !slots.Contains(slot);
				if (clearSlot)
				{
					if (otherControl == null)
						msa.SlotsRC.Clear(); // The slot slice is not showing, so directly set the object's Slot property.
					else
						otherControl.AddItem(null); // Reset it using the other slice, so it gets refreshed.
				}
			}
		}

		#endregion Event handlers
	}
}
