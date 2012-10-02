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
// File: DerivMSAReferenceSlice.cs
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
	/// Summary description for DerivMSAReferenceSlice.
	/// </summary>
	public class DerivMSAReferenceSlice : AtomicReferenceSlice
	{
		public DerivMSAReferenceSlice()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AtomicReferenceSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DerivMSAReferenceSlice(FdoCache cache, ICmObject obj, int flid,
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
		/// Handle interaction between to and from POS for a derivational affix MSA.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>
		/// If the new value is zero, then set the other one's value to zero, as well.
		/// If the other one's value is zero, then set it to the new value.
		/// In all cases, set this one's value to the new value.
		/// </remarks>
		protected void OnReferenceChanged(object sender, SIL.FieldWorks.Common.Utils.FwObjectSelectionEventArgs e)
		{
			Debug.Assert(sender is AtomicReferenceLauncher);
			AtomicReferenceLauncher source = (AtomicReferenceLauncher)sender;
			Debug.Assert(Control == source);
			Debug.Assert(Object is MoDerivAffMsa);

			AtomicReferenceLauncher otherControl = null;
			int idxSender = Parent.Controls.IndexOf(this);
			int otherFlid;
			bool myIsFromPOS = true;
			if (m_flid == (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromPartOfSpeech)
				otherFlid = (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToPartOfSpeech;
			else
			{
				otherFlid = (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromPartOfSpeech;
				myIsFromPOS = false;
			}
			int otherHvo = 0;
			Slice otherSlice = null;
			int idxOther;
			if (idxSender > 0)
			{
				idxOther = idxSender - 1;
				while (otherSlice == null || (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.Object == Object))
				{
					otherSlice = (Slice)Parent.Controls[idxOther--];
					if (otherSlice is AtomicReferenceSlice && (otherSlice as AtomicReferenceSlice).Flid == otherFlid)
						break;
				}
				if (otherSlice != null && otherSlice is AtomicReferenceSlice)
					otherHvo = GetOtherHvo(otherSlice as AtomicReferenceSlice, otherFlid, myIsFromPOS, out otherControl);
				else
					otherSlice = null;
			}
			if (otherControl == null && idxSender < Parent.Controls.Count)
			{
				idxOther = idxSender + 1;
				while (otherSlice == null
					|| (otherSlice.Indent == Indent && idxOther > 0 && otherSlice.Object == Object))
				{
					otherSlice = (Slice)Parent.Controls[idxOther++];
					if (otherSlice is AtomicReferenceSlice && (otherSlice as AtomicReferenceSlice).Flid == otherFlid)
						break;
				}
				if (otherSlice != null && otherSlice is AtomicReferenceSlice)
					otherHvo = GetOtherHvo(otherSlice as AtomicReferenceSlice, otherFlid, myIsFromPOS, out otherControl);
				else
					otherSlice = null;
			}

			MoDerivAffMsa msa = Object as MoDerivAffMsa;
			if (e.Hvo == 0 && otherHvo != 0)
			{
				if (otherControl == null)
				{
					otherControl.AddItem(0); // Clear the other one, as well.
				}
				else
				{
					if (m_flid == (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromPartOfSpeech)
						msa.ToPartOfSpeechRAHvo = 0;
					else
						msa.FromPartOfSpeechRAHvo = 0;
				}
			}
			else if (otherHvo == 0 && e.Hvo > 0)
			{
				if (otherControl == null)
				{
					// The other one is not available (filtered out?),
					// so set it directly using the msa.
					if (m_flid == (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromPartOfSpeech)
						msa.ToPartOfSpeechRAHvo = e.Hvo;
					else
						msa.FromPartOfSpeechRAHvo = e.Hvo;
				}
				else
				{
					otherControl.AddItem(e.Hvo); // Set the other guy to this value.
				}
			}
		}

		private int GetOtherHvo(AtomicReferenceSlice otherSlice, int otherFlid, bool myIsFromPOS, out AtomicReferenceLauncher otherOne)
		{
			otherOne = null;
			int otherHvo = 0;

			if (otherSlice != null)
			{
				AtomicReferenceLauncher otherControl = otherSlice.Control as AtomicReferenceLauncher;
				int of = otherSlice.Flid;
				if (otherSlice.Object == Object
					&& (otherSlice.Flid == otherFlid))
				{
					otherOne = otherControl;
					if (myIsFromPOS)
						otherHvo = (Object as MoDerivAffMsa).ToPartOfSpeechRAHvo;
					else
						otherHvo = (Object as MoDerivAffMsa).FromPartOfSpeechRAHvo;
				}
			}
			return otherHvo;
		}

		#endregion Event handlers
	}
}
