// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.XWorks;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This is so that we can use InterlinMaster in VS Designer.
	/// Designer won't work with classes that have abstract base classes.
	/// </summary>
	public class InterlinMasterBase : RecordView
	{
		internal InterlinMasterBase()
		{
		}

		internal InterlinMasterBase(XElement configurationParametersElement, LcmCache cache, RecordClerk recordClerk)
			:base(configurationParametersElement, cache, recordClerk)
		{
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailability="Required"), then use this.
		/// </summary>
		protected override TreebarAvailability DefaultTreeBarAvailability => TreebarAvailability.NotAllowed;
	}
}
