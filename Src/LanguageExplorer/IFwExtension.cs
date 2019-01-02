// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// A small interface to allow for plugin stuff, like import dialogs
	/// </summary>
	internal interface IFwExtension
	{
		/// <summary>
		/// Called instead of a constructor with parameters
		/// </summary>
		void Init(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher);
	}
}