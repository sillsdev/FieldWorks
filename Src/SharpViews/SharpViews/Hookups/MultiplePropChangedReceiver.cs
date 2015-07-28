// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This class handles the unusual case where we want multiple hookups receiving PropChanged
	/// for the same Hvo, tag pair. It replaces the first two and can have more added.
	/// </summary>
	class MultiplePropChangedReceiver : IReceivePropChanged
	{
		IList<IReceivePropChanged> m_targets = new List<IReceivePropChanged>();
		public MultiplePropChangedReceiver(IReceivePropChanged target1, IReceivePropChanged target2)
		{
			Add(target1);
			Add(target2);
		}

		public void Add(IReceivePropChanged target)
		{
			m_targets.Add(target);
		}

		public void Remove(IReceivePropChanged target)
		{
			m_targets.Remove(target);
		}

		public int Count { get { return m_targets.Count; } }

		/// <summary>
		/// Pass it on to all the targets.
		/// </summary>
		public void PropChanged(object sender, EventArgs args)
		{
			foreach (var target in m_targets)
				target.PropChanged(sender, args);
		}

		public void PropChanged(object sender, ObjectSequenceEventArgs args)
		{
			foreach (var target in m_targets)
			{
				target.PropChanged(sender, args);
			}
		}
	}
}
