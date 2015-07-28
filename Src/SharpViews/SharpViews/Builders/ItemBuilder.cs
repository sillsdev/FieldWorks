// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// ItemBuilder is returned from Display.Of methods which require the next method to be
	/// a specification of how to display one item. It implements the part of the fluent
	/// language that has to do with how to display items.
	/// </summary>
	public class ItemBuilder<T> where T: class
	{
		internal Flow<T> m_flow;
		internal Action<ViewBuilder, T> m_displayOneItem;

		internal ItemBuilder(Flow<T> flow)
		{
			m_flow = flow;
		}

		/// <summary>
		/// Specify how to display one item in a previously indicated sequence (or atomic property)
		/// </summary>
		public Flow<T> Using(Action<ViewBuilder, T> displayOneItem)
		{
			//return m_builder.AddObjSeq(m_target, m_fetcher, m_hookEvent, m_unhookEvent, displayOneT, Tag);
			m_displayOneItem = displayOneItem;
			return m_flow;
		}
	}
}
