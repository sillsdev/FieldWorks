// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class BooleanChooserBEditControl : IntChooserBEditControl
	{
		internal BooleanChooserBEditControl(string itemList, int flid)
			: base(itemList, flid)
		{}

		protected override int GetBasicPropertyValue(ISilDataAccess sda, int hvoOwner)
		{
			return Convert.ToInt32(IntBoolPropertyConverter.GetBoolean(m_sda, hvoOwner, m_flid));
		}

		protected override void SetBasicPropertyValue(ISilDataAccess sda, int newVal, int hvoOwner)
		{
			Debug.Assert(newVal == 0 || newVal == 1, $"Expected value {newVal} to be boolean.");
			IntBoolPropertyConverter.SetValueFromBoolean(m_sda, hvoOwner, m_flid, Convert.ToBoolean(newVal));
		}
	}
}