// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using LanguageExplorer.Works;
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
			:base(configurationParametersElement, cache, recordList)
		{
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailability="Required"), then use this.
		/// </summary>
		protected override TreebarAvailability DefaultTreeBarAvailability => TreebarAvailability.NotAllowed;
	}
}
