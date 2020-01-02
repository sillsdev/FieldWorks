// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This is so that we can use InterlinMaster in VS Designer.
	/// Designer won't work with classes that have abstract base classes.
	/// </summary>
	internal class InterlinMasterBase : RecordView
	{
		internal InterlinMasterBase()
		{
		}

		internal InterlinMasterBase(XElement configurationParametersElement, LcmCache cache, IRecordList recordList)
			: base(configurationParametersElement, cache, recordList)
		{
		}
	}
}
