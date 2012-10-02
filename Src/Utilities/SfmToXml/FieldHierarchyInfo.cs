/// This is a very simple class and has no 'using' statements.
///

namespace Sfm2Xml
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
		private string srcMarker;	// marker in the data
		private string fwDestID;	// fw destination ID "lex", "eires", etc.
		private string fwLang;		// "English", etc
		private string refFunc;     // "syn" or something like that, or empty, or null
		private string refFuncWS;	// writing system for the refFunc
		private bool excluded;      // true if it's exclueded -default to false
		private bool abbrField;     // true if it's an abbr field, false by default
		private bool isAbbr;		// if it's an abbr field, is it an abbr (or a name)
		private bool beginMarker;	// true if this is a begin marker
		private bool autoImport;	// true if this marker is auto imported based on the owning class in the data (dynamically determined)
		private string classDest;	// "Entry", "Subentry", "Variant", "Sense"

		/// <summary>
		/// Constructor for non-auto import items
		/// </summary>
		/// <param name="marker">sfm marker</param>
		/// <param name="dest">fw dest</param>
		/// <param name="lang">language</param>
		/// <param name="begin">true if begin marker</param>
		public FieldHierarchyInfo(string marker, string dest, string lang, bool begin, string destClass)
		{
			srcMarker = marker;
			fwDestID = dest;
			fwLang = lang;
			beginMarker = begin;
			classDest = destClass;
			autoImport = false;
			refFunc = "";
			refFuncWS = "";
			excluded = false;
			abbrField = false;
			isAbbr = false;
		}

		/// <summary>
		/// Constructor for auto import items
		/// </summary>
		/// <param name="marker">sfm marker</param>
		/// <param name="lang">language</param>
		public FieldHierarchyInfo(string marker, string lang)
		{
			srcMarker = marker;
			fwLang = lang;

			autoImport = true;
			fwDestID = "Determined by location in Data";
			beginMarker = false;
			excluded = false;
			abbrField = false;
			isAbbr = false;
			refFunc = "";
			refFuncWS = "";
			classDest = "";
		}

		/// <summary>
		/// Public properties
		/// </summary>
		public string SFM { get { return srcMarker; } }
		public string FwDestID { get { return fwDestID; } }
		public string FwDestID_Changed { set { fwDestID = value; } }
		public string Lang { get { return fwLang; } }
		public bool IsBegin { get { return beginMarker; } }
		public bool IsAuto { get { return autoImport; } }
		public string FwDestClass { get { return classDest; } }

		public string RefFunc
		{
			get { return refFunc; }
			set { refFunc = value; }
		}

		public string RefFuncWS
		{
			get { return refFuncWS; }
			set { refFuncWS = value; }
		}

		public bool IsExcluded
		{
			get { return excluded; }
			set { excluded = value; }
		}

		public bool IsAbbrvField
		{
			get { return abbrField; }
			set { abbrField = value; }
		}

		public bool IsAbbr
		{
			get { return isAbbr; }
			set { isAbbr = value; }
		}
	}
}
