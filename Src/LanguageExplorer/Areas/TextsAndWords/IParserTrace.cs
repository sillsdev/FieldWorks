// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// Interface for parser trace processing
	/// </summary>
	internal interface IParserTrace
	{
		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		string CreateResultPage(IPropertyTable propertyTable, XDocument result, bool isTrace);
	}
}