// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This is a subclass of MainCallerDisplayCommand, necessary for sequences.
	/// When a display of a sequence is regenerated, we must restore m_stackPartRef to the correct state.
	/// </summary>
	internal class MainCallerDisplayCommandSeq : MainCallerDisplayCommand
	{
		private XElement[] m_stackPartRef;

		internal MainCallerDisplayCommandSeq(XElement mainElement, XElement caller, bool fUserMainAsFrag, int wsForce, List<XElement> stackPartRef)
			: base(mainElement, caller, fUserMainAsFrag, wsForce)
		{
			m_stackPartRef = stackPartRef.ToArray();
		}

		/// <summary>
		/// Two of these are equal if everything inherited is equal, and they have the same saved stack items.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (!base.Equals(obj))
			{
				return false;
			}
			var other = obj as MainCallerDisplayCommandSeq;
			if (other == null || other.m_stackPartRef.Length != m_stackPartRef.Length)
			{
				return false;
			}
			for (var i = 0; i < m_stackPartRef.Length; i++)
			{
				if (m_stackPartRef[i] != other.m_stackPartRef[i])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Hash code must incorporate the stack items.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode() + m_stackPartRef.Aggregate(0, (sum, node) => (sum + node.GetHashCode()) % int.MaxValue);
		}

		/// <summary>
		/// Base version wrapped in making the stack what it needs to be.
		/// </summary>
		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			var save = vc.m_stackPartRef.ToArray();
			vc.m_stackPartRef.Clear();
			vc.m_stackPartRef.AddRange(m_stackPartRef);
			base.PerformDisplay(vc, fragId, hvo, vwenv);
			vc.m_stackPartRef.Clear();
			vc.m_stackPartRef.AddRange(save);
		}
	}
}