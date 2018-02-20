// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// A slice to show the IReversalIndexEntry objects.
	/// </summary>
	internal sealed class ReversalIndexEntrySlice : ViewPropertySlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications.
		/// </summary>
		private ISilDataAccess m_sda;

		#region ReversalIndexEntrySlice class info
		/// <summary>
		/// Constructor.
		/// </summary>
		public ReversalIndexEntrySlice()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ReversalIndexEntrySlice(ICmObject obj) :
			base(new ReversalIndexEntrySliceView(obj.Hvo), obj, obj.Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseReversalIndexEntryBackRefs)
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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				m_sda?.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Therefore this method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			var ctrl = new ReversalIndexEntrySliceView(Object.Hvo)
			{
				Cache = PropertyTable.GetValue<LcmCache>("cache")
			};
			ctrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			Control = ctrl;
			m_sda = ctrl.Cache.DomainDataByFlid;
			m_sda.AddNotification(this);

			if (ctrl.RootBox == null)
			{
				ctrl.MakeRoot();
			}
		}

		#endregion ReversalIndexEntrySlice class info

		#region IVwNotifyChange methods
		/// <summary>
		/// The dafault behavior is for change watchers to call DoEffectsOfPropChange if the
		/// data for the tag being watched has changed.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == Object.Hvo && tag == Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseReversalIndexEntryBackRefs)
			{
				((ReversalIndexEntrySliceView)Control).ResetEntries();
			}
		}

		#endregion
	}
}