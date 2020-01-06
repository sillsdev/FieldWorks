// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.CacheLight
{
	/// <summary>
	/// Interface for RealDataCache that combines the different interfaces that RealDataCache
	/// implements. This more easily allows to use a substitute implementation in unit tests.
	/// </summary>
	public interface IRealDataCache : ISilDataAccess, IVwCacheDa, IStructuredTextDataAccess, IDisposable
	{
		/// <summary>
		/// Gets or sets the paragraph contents field id.
		/// </summary>
		new int ParaContentsFlid { get; set; }

		/// <summary>
		/// Gets or sets the paragraph properties field id.
		/// </summary>
		new int ParaPropertiesFlid { get; set; }

		/// <summary>
		/// Gets or sets the text paragraphs field id.
		/// </summary>
		new int TextParagraphsFlid { get; set; }
	}
}