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
	internal sealed class FieldHierarchyInfo
	{
		/// <summary>
		/// Constructor for non-auto import items
		/// </summary>
		/// <param name="marker">sfm marker</param>
		/// <param name="dest">fw dest</param>
		/// <param name="lang">language</param>
		/// <param name="begin">true if begin marker</param>
		/// <param name="destClass"></param>
		internal FieldHierarchyInfo(string marker, string dest, string lang, bool begin, string destClass)
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
		internal FieldHierarchyInfo(string marker, string lang)
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

		internal string SFM { get; }

		internal string FwDestID { get; private set; }

		internal string FwDestID_Changed { set => FwDestID = value; }

		internal string Lang { get; }

		internal bool IsBegin { get; }

		internal bool IsAuto { get; }

		internal string FwDestClass { get; }

		internal string RefFunc { get; set; }

		internal string RefFuncWS { get; set; }

		internal bool IsExcluded { get; set; }

		internal bool IsAbbrvField { get; set; }

		internal bool IsAbbr { get; set; }
	}
}