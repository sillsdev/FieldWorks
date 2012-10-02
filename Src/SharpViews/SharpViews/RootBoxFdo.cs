using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// This subclass adds functionality specific to FDO.
	/// </summary>
	public class RootBoxFdo : RootBox, IVwNotifyChange
	{
		// Dictionary used to record where we need to send FDO PropChange notifications.
		// Key is Hvo, flid; value is one of our hookups.
		private Dictionary<Tuple<int, int>, IReceivePropChanged> m_propChangeTargets;
		private ISilDataAccess m_sda; // currently used only to hook and remove PropChanged notifications (for FDO).
		public RootBoxFdo(AssembledStyles styles)
			: base(styles)
		{
		}
		/// <summary>
		/// Answer a viewbuilder for adding stuff to the specified box. Overriden to give an FDO-specific
		/// ViewBuilder.
		/// </summary>
		internal override ViewBuilder GetBuilder(GroupBox destination)
		{
			return new ViewBuilderFdo(destination);
		}
		#region IVwNotifyChange Members

		void IVwNotifyChange.PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			IReceivePropChanged target;
			if (m_propChangeTargets == null)
				return; // not interested in any
			if (!m_propChangeTargets.TryGetValue(new Tuple<int, int>(hvo, tag), out target))
				return;
			target.PropChanged(this, new EventArgs());
		}

		#endregion

		internal Dictionary<Tuple<int, int>, IReceivePropChanged> PropChangeTargets
		{
			get
			{
				if (m_propChangeTargets == null)
					m_propChangeTargets = new Dictionary<Tuple<int, int>, IReceivePropChanged>();
				return m_propChangeTargets;
			}
		}

		internal void AddHookupToPropChanged(Tuple<int, int> key, IReceivePropChanged hookup)
		{
			IReceivePropChanged oldTarget;
			if (PropChangeTargets.TryGetValue(key, out oldTarget))
			{
				// multiple targets. Is this the second?
				var multiReceiver = oldTarget as MultiplePropChangedReceiver;
				if (multiReceiver != null)
					multiReceiver.Add(hookup);
				else
					PropChangeTargets[key] = new MultiplePropChangedReceiver(oldTarget, hookup);
			}
			else
			{
				// Common case, only display of this property, just store it.
				PropChangeTargets[key] = hookup;
			}
		}

		internal void RemoveHookupFromPropChanged(Tuple<int, int> key, IReceivePropChanged hookup)
		{
			IReceivePropChanged oldTarget;
			if (PropChangeTargets.TryGetValue(key, out oldTarget) && oldTarget is MultiplePropChangedReceiver)
			{
				var multiReceiver = (MultiplePropChangedReceiver)oldTarget;
				multiReceiver.Remove(hookup);
				if (multiReceiver.Count != 0)
					return; // if its down to zero go ahead and remove it.
			}
			PropChangeTargets.Remove(key);
		}
		internal ISilDataAccess DataAccess
		{
			get
			{
				return m_sda;
			}
			set
			{
				if (m_sda == value)
					return;
				if (m_sda != null)
					throw new ArgumentException("Cannot change DataAccess once it has been set");
				m_sda = value;
				m_sda.AddNotification(this);
			}
		}
		internal override void Dispose(bool beforeDestructor)
		{
			if (beforeDestructor)
			{
				if (m_sda != null)
				{
					m_sda.RemoveNotification(this);
					m_sda = null;
				}
			}
			base.Dispose(beforeDestructor);
		}
	}
}
