// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// A slice to show the IReversalIndexEntry objects.
	/// </summary>
	internal class ReversalIndexEntrySlice : ViewPropertySlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications.
		/// </summary>
		private ISilDataAccess m_sda;

		/// <summary />
		public ReversalIndexEntrySlice()
		{
		}

		/// <summary />
		public ReversalIndexEntrySlice(ICmObject obj)
			: base(new ReversalIndexEntrySliceView(obj.Hvo), obj, obj.Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseReversalIndexEntryBackRefs)
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
			var ctrl = new ReversalIndexEntrySliceView(MyCmObject.Hvo)
			{
				Cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache)
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

		#region IVwNotifyChange methods
		/// <summary>
		/// The default behavior is for change watchers to call DoEffectsOfPropChange if the
		/// data for the tag being watched has changed.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == MyCmObject.Hvo && tag == Cache.ServiceLocator.GetInstance<Virtuals>().LexSenseReversalIndexEntryBackRefs)
			{
				((ReversalIndexEntrySliceView)Control).ResetEntries();
			}
		}

		#endregion
	}
}