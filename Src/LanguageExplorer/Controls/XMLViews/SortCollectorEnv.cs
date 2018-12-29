// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This is a marker class used when building a sort key.
	/// </summary>
	internal class SortCollectorEnv : TsStringCollectorEnv
	{
		public SortCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{ }
	}
}