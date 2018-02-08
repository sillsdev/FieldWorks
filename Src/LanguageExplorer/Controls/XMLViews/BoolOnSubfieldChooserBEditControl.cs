// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This adapts IntOnSubfield chooser to use 0 for a boolean false and 1 for a boolean true
	/// </summary>
	internal class BoolOnSubfieldChooserBEditControl : IntOnSubfieldChooserBEditControl
	{
		public BoolOnSubfieldChooserBEditControl(string itemList, int flid, int flidSub) : base(itemList, flid, flidSub)
		{
			if (m_combo.Items.Count != 2)
			{
				throw new ArgumentException("BoolOnSubfieldChooserBEditControl must be created with a two-item list of options");
			}
		}

		internal override int GetValueOfField(ISilDataAccess sda, int hvoField)
		{
			return sda.get_BooleanProp(hvoField, m_flidSub) ? 1 : 0;
		}

		internal override void SetValueOfField(ISilDataAccess sda, int hvoField, int val)
		{
			sda.SetBoolean(hvoField, m_flidSub, val == 1);
		}
	}
}