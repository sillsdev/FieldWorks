// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Test an item by following the specified flid (an atomic object property).
	/// Pass if the destination is in the validHvos set.
	/// </summary>
	internal class TestItemAtomicFlid : ITestItem
	{
		private readonly int m_flid;

		internal TestItemAtomicFlid(int flid)
		{
			m_flid = flid;
		}

		public bool Test(int hvo, ISilDataAccess sda, ISet<int> validHvos)
		{
			return validHvos.Contains(sda.get_ObjectProp(hvo, m_flid));
		}
	}
}