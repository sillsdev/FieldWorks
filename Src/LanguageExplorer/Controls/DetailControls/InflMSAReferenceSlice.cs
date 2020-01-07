// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Linq;
using SIL.LCModel;


namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class is used to manage handling of the PartOfSpeech property of a MoInflAffMsa.
	/// Depending on the value of that property, the Slot property needs to be kept valid.
	/// </summary>
	internal class InflMSAReferenceSlice : AtomicReferenceSlice
	{
		/// <summary />
		internal InflMSAReferenceSlice(LcmCache cache, ICmObject obj, int flid)
			: base(cache, obj, flid)
		{
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				((AtomicReferenceLauncher)Control).ReferenceChanged -= OnReferenceChanged;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void FinishInit()
		{
			base.FinishInit();
			((AtomicReferenceLauncher)Control).ReferenceChanged += OnReferenceChanged;
		}

		#region Event handlers

		/// <summary>
		/// Handle interaction between POS and Slot properties for a inflectional affix MSA.
		/// </summary>
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
			Debug.Assert(MyCmObject is IMoInflAffMsa);
			var idxSender = ContainingDataTree.Slices.IndexOf(this);
			const int otherFlid = MoInflAffMsaTags.kflidSlots;
			Slice otherSlice = null;
			int idxOther;
			// Try to get the Slots slice.
			// Check for slices before this one.
			if (idxSender > 0)
			{
				idxOther = idxSender - 1;
				while (idxOther >= 0 && (otherSlice == null || (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.MyCmObject == MyCmObject)))
				{
					otherSlice = ContainingDataTree.Slices[idxOther--];
					if (otherSlice is ReferenceVectorSlice && (otherSlice as ReferenceVectorSlice).Flid == otherFlid)
					{
						break;
					}
					otherSlice = null;
				}
			}
			// Check for following slices, if we didn't get one earlier.
			if (otherSlice == null && idxSender < ContainingDataTree.Slices.Count)
			{
				idxOther = idxSender + 1;
				while (idxOther < ContainingDataTree.Slices.Count && (otherSlice == null || (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.MyCmObject == MyCmObject)))
				{
					otherSlice = ContainingDataTree.Slices[idxOther++];
					if (otherSlice is ReferenceVectorSlice && (otherSlice as ReferenceVectorSlice).Flid == otherFlid)
					{
						break;
					}
					otherSlice = null;
				}
			}
			VectorReferenceLauncher otherControl = null;
			if (otherSlice != null)
			{
				Debug.Assert(otherSlice.Flid == otherFlid);
				Debug.Assert(otherSlice.MyCmObject == MyCmObject);
				otherControl = otherSlice.Control as VectorReferenceLauncher;
				Debug.Assert(otherControl != null);
			}
			var msa = (IMoInflAffMsa)MyCmObject;
			IMoInflAffixSlot slot = null;
			if (msa.SlotsRC.Count > 0)
			{
				slot = msa.SlotsRC.First();
			}
			if (e.Hvo != 0 && slot == null)
			{
				return;
			}
			var pos = msa.PartOfSpeechRA;
			var slots = pos != null ? pos.AllAffixSlots : Enumerable.Empty<IMoInflAffixSlot>();
			var clearSlot = e.Hvo == 0 || !slots.Contains(slot);
			if (!clearSlot)
			{
				return;
			}
			if (otherControl == null)
			{
				msa.SlotsRC.Clear(); // The slot slice is not showing, so directly set the object's Slot property.
			}
			else
			{
				otherControl.AddItem(null); // Reset it using the other slice, so it gets refreshed.
			}
		}

		#endregion Event handlers
	}
}