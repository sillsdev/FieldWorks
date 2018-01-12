// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Allows a guicontrol to dynamically initialize with a configuration node with respect
	/// to the given sourceObject.
	/// </summary>
	public interface IFwGuiControl : IFlexComponent, IDisposable
	{
		void Launch();
	}
}