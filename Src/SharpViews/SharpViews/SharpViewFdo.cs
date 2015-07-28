// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// Subclass of SharpView for stuff that depends on FieldWorks
	/// </summary>
	public class SharpViewFdo : SharpView
	{
		public SharpViewFdo()
		{
			m_wsf = new PalasoWritingSystemManager();
			//m_wsf.get_CharPropEngine(m_wsf.UserWs); // This is a device for getting InitIcuDataDirectory called.
		}
	}
}
