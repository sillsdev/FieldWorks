// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class was created to contain the minimum data needed to create ClsFieldHierarchy
	/// object.  It is used in the non-FW code and isn't tied to any UI.
	///
	/// There are two constructors for this object:
	/// - one for non-AutoImport fields and
	/// - one for AutoImport fields
	/// </summary>
	public class FieldHierarchyInfo
	{
		/// <summary>
		/// Constructor for non-auto import items
		/// </summary>
		/// <param name="marker">sfm marker</param>
		/// <param name="dest">fw dest</param>
		/// <param name="lang">language</param>
		/// <param name="begin">true if begin marker</param>
		/// <param name="destClass"></param>
		public FieldHierarchyInfo(string marker, string dest, string lang, bool begin, string destClass)
		{
			SFM = marker;
			FwDestID = dest;
			Lang = lang;
			IsBegin = begin;
			FwDestClass = destClass;
			IsAuto = false;
			RefFunc = string.Empty;
			RefFuncWS = string.Empty;
			IsExcluded = false;
			IsAbbrvField = false;
			IsAbbr = false;
		}

		/// <summary>
		/// Constructor for auto import items
		/// </summary>
		/// <param name="marker">sfm marker</param>
		/// <param name="lang">language</param>
		public FieldHierarchyInfo(string marker, string lang)
		{
			SFM = marker;
			Lang = lang;
			IsAuto = true;
			FwDestID = "Determined by location in Data";
			IsBegin = false;
			IsExcluded = false;
			IsAbbrvField = false;
			IsAbbr = false;
			RefFunc = string.Empty;
			RefFuncWS = string.Empty;
			FwDestClass = string.Empty;
		}

		public string SFM { get; }

		public string FwDestID { get; private set; }

		public string FwDestID_Changed { set { FwDestID = value; } }

		public string Lang { get; }

		public bool IsBegin { get; }

		public bool IsAuto { get; }

		public string FwDestClass { get; }

		public string RefFunc { get; set; }

		public string RefFuncWS { get; set; }

		public bool IsExcluded { get; set; }

		public bool IsAbbrvField { get; set; }

		public bool IsAbbr { get; set; }
	}
}