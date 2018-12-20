// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class DerivMSAReferenceSlice : AtomicReferenceSlice
	{
		/// <summary />
		internal DerivMSAReferenceSlice(LcmCache cache, ICmObject obj, int flid)
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
		/// Handle interaction between to and from POS for a derivational affix MSA.
		/// </summary>
		/// <remarks>
		/// If the new value is zero, then set the other one's value to zero, as well.
		/// If the other one's value is zero, then set it to the new value.
		/// In all cases, set this one's value to the new value.
		/// </remarks>
		protected void OnReferenceChanged(object sender, FwObjectSelectionEventArgs e)
		{
			Debug.Assert(sender is AtomicReferenceLauncher);
			var source = (AtomicReferenceLauncher)sender;
			Debug.Assert(Control == source);
			Debug.Assert(MyCmObject is IMoDerivAffMsa);
			AtomicReferenceLauncher otherControl = null;
			var idxSender = ContainingDataTree.Slices.IndexOf(this);
			int otherFlid;
			var myIsFromPOS = true;
			if (m_flid == MoDerivAffMsaTags.kflidFromPartOfSpeech)
			{
				otherFlid = MoDerivAffMsaTags.kflidToPartOfSpeech;
			}
			else
			{
				otherFlid = MoDerivAffMsaTags.kflidFromPartOfSpeech;
				myIsFromPOS = false;
			}
			var otherHvo = 0;
			Slice otherSlice = null;
			int idxOther;
			if (idxSender > 0)
			{
				idxOther = idxSender - 1;
				while (otherSlice == null || (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.MyCmObject == MyCmObject))
				{
					otherSlice = ContainingDataTree.Slices[idxOther--];
					if (otherSlice is AtomicReferenceSlice && (otherSlice as AtomicReferenceSlice).Flid == otherFlid)
					{
						break;
					}
				}
				if (otherSlice is AtomicReferenceSlice)
				{
					otherHvo = GetOtherHvo((AtomicReferenceSlice)otherSlice, otherFlid, myIsFromPOS, out otherControl);
				}
				else
				{
					otherSlice = null;
				}
			}
			if (otherControl == null && idxSender < ContainingDataTree.Slices.Count)
			{
				idxOther = idxSender + 1;
				while (otherSlice == null || (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.MyCmObject == MyCmObject))
				{
					otherSlice = ContainingDataTree.Slices[idxOther++];
					if (otherSlice is AtomicReferenceSlice && ((AtomicReferenceSlice)otherSlice).Flid == otherFlid)
					{
						break;
					}
				}
				if (otherSlice is AtomicReferenceSlice)
				{
					otherHvo = GetOtherHvo((AtomicReferenceSlice)otherSlice, otherFlid, myIsFromPOS, out otherControl);
				}
			}
			var msa = MyCmObject as IMoDerivAffMsa;
			if (e.Hvo == 0 && otherHvo != 0)
			{
				if (otherControl != null)
				{
					if (m_flid == MoDerivAffMsaTags.kflidFromPartOfSpeech)
					{
						msa.ToPartOfSpeechRA = null;
					}
					else
					{
						msa.FromPartOfSpeechRA = null;
					}
				}
			}
			else if (otherHvo == 0 && e.Hvo > 0)
			{
				if (otherControl == null)
				{
					// The other one is not available (filtered out?),
					// so set it directly using the msa.
					if (m_flid == MoDerivAffMsaTags.kflidFromPartOfSpeech)
					{
						msa.ToPartOfSpeechRA = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(e.Hvo);
					}
					else
					{
						msa.FromPartOfSpeechRA = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(e.Hvo);
					}
				}
				else
				{
					otherControl.AddItem(Cache.ServiceLocator.GetObject(e.Hvo)); // Set the other guy to this value.
				}
			}
		}

		private int GetOtherHvo(AtomicReferenceSlice otherSlice, int otherFlid, bool myIsFromPOS, out AtomicReferenceLauncher otherOne)
		{
			otherOne = null;
			var otherHvo = 0;
			if (otherSlice != null)
			{
				var otherControl = otherSlice.Control as AtomicReferenceLauncher;
				if (otherSlice.MyCmObject == MyCmObject && (otherSlice.Flid == otherFlid))
				{
					otherOne = otherControl;
					otherHvo = myIsFromPOS ? ((IMoDerivAffMsa)MyCmObject).ToPartOfSpeechRA.Hvo : ((IMoDerivAffMsa)MyCmObject).FromPartOfSpeechRA.Hvo;
				}
			}
			return otherHvo;
		}

		#endregion Event handlers
	}
}