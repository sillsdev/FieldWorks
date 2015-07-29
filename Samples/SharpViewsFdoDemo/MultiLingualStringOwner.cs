// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SharpViewsDemo
{
	/// <summary>
	/// Demonstrates a class that has a multilingual string property.
	/// </summary>
	class MultiLingualStringOwner
	{
		public IViewMultiString MyMultiString { get; private set; }

		public MultiLingualStringOwner()
		{
			MyMultiString = new MultiAccessor(1, 2); // default WSs are not relevant for this demo
		}
	}
}
