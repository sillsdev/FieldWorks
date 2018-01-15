// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Test an item in a filtered property to see whether it should be included.
	/// </summary>
	interface ITestItem
	{
		bool Test(int hvo, ISilDataAccess sda, ISet<int> validHvos);
	}
}