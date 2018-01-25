// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class contains the object that is used for containing the data that is
	/// used in the content mapping display of the data.  It lives in the MarkerPresenter
	/// class as a public subclass.
	/// </summary>
	public class ContentMapping
	{
		private string m_marker;            // marker
		private string m_description;       // description
		private string m_destinationClass;  // FW destination class (Sense, Entry, ...)
		private string m_FWDestination;     // FW destination field (Date Created, ...)
		private string m_writingSystem;     // writing system
		private string m_langDescriptor;    // the display language descriptor for the UI
		private int m_srcOrder;             // the relative order of the sfm in the src file
		private int m_count;                // the number of times this sfm is in the src file

		private Sfm2Xml.ClsFieldDescription m_clsFieldDescription;
		private Sfm2Xml.LexImportField m_LexImportField;
		private bool m_isBeginMarker;       // true if this is a begin marker for the FW Dest class


		public static string Ignore() { return LexTextControls.ksIgnore; }
		public static string Unknown() { return LexTextControls.ksUnknown; }

		public string Marker { get { return m_marker; } }
		public string Description
		{
			get { return m_description; }
			set { m_description = value; }
		}
		public string DestinationClass
		{
			get { return m_destinationClass; }
			set { m_destinationClass = value; }
		}
		public string rawDestinationField
		{
			get { return m_FWDestination; }
			set { m_FWDestination = value; }
		}
		public string LanguageDescriptorRaw
		{
			get { return m_langDescriptor; }
		}
		public string LanguageDescriptor
		{
			get
			{
				if (IsLangIgnore)
					return String.Format(LexTextControls.ksX_Ignored, m_langDescriptor);
				return m_langDescriptor;
			}
			set { m_langDescriptor = value; }
		}
		public string WritingSystem => m_writingSystem;

		public bool IsBeginMarker
		{
			get { return m_isBeginMarker; }
			set { m_isBeginMarker = value; }
		}
		public void UpdateLangaugeValues(string writingsystemName, string shortName, string langDescriptor)
		{
			m_writingSystem = writingsystemName;
			m_langDescriptor = langDescriptor;
			m_clsFieldDescription.UpdateLanguageValues(m_langDescriptor, shortName);
		}


		public int Count { get { return m_count; } set { m_count = value; } }
		public int Order { get { return m_srcOrder; } set { m_srcOrder = value; } }

		public void AddLexImportCustomField(Sfm2Xml.ILexImportField field, string uiClass)
		{
			AddLexImportField(field);
			(m_LexImportField as Sfm2Xml.ILexImportCustomField).UIClass = uiClass;
			Sfm2Xml.ClsFieldDescription xyz = LexImportField.ClsFieldDescriptionWith(ClsFieldDescription);
			m_clsFieldDescription = xyz;
		}

		public void AddLexImportField(Sfm2Xml.ILexImportField field)
		{
			m_LexImportField = field as Sfm2Xml.LexImportField;
			m_clsFieldDescription.Type = field != null ? field.DataType : "string";
		}

		public Sfm2Xml.ILexImportField LexImportField
		{
			get { return m_LexImportField; }
		}

		public bool Exclude
		{
			get { return m_clsFieldDescription.IsExcluded; }
			set { m_clsFieldDescription.IsExcluded = value; }
		}

		public bool AutoImport
		{
			get { return m_clsFieldDescription.IsAutoImportField; }
			set { m_clsFieldDescription.IsAutoImportField = value; }
		}

		public void ClearRef()
		{
			m_clsFieldDescription.ClearRef();
		}
		public bool IsRefField => m_clsFieldDescription.IsRef;

		public string RefField
		{
			get { return m_clsFieldDescription.RefFunc; }
			set { m_clsFieldDescription.RefFunc = value; }
		}

		public string RefFieldWS
		{
			get { return m_clsFieldDescription.RefFuncWS; }
			set { m_clsFieldDescription.RefFuncWS = value; }
		}

		public string FwId
		{
			get { return m_clsFieldDescription.MeaningID; }
			set { m_clsFieldDescription.MeaningID = value; }
		}

		public bool IsAbbrvField
		{
			get
			{
				bool defaultValue = false;
				if (m_LexImportField == null)
					return defaultValue;
				return m_LexImportField.IsAbbrField;
			}
		}

		public bool IsAbbr
		{
			get
			{
				bool defaultValue = false;
				if (m_LexImportField == null)
					return defaultValue;
				return m_clsFieldDescription.IsAbbr;
			}
		}

		public void UpdateAbbrValue(bool isAbbr)
		{
			if (!IsAbbrvField)
				return;

			m_clsFieldDescription.IsAbbr = isAbbr;
		}

		public string[] ListViewStrings()
		{
			string customTag = "";
			if (this.LexImportField is Sfm2Xml.LexImportCustomField)
			{
				customTag = " (Custom " + (this.LexImportField as Sfm2Xml.LexImportCustomField).UIClass + ")";
			}
			else if (this.IsRefField)
			{
				customTag = " (" + this.RefField + ")";
			}
			return new string[] {"\\"+Marker,						// col 1
				System.Convert.ToString(Order), // col 2
				System.Convert.ToString(Count),	// col 3
				Description,					// col 4
				DestinationField + customTag,	// col 5
				LanguageDescriptor };           // col 6
		}

		public bool IsLangIgnore
		{
			get
			{
				if (m_writingSystem == Ignore())
					return true;
				return false;
			}
		}

		public string DestinationField
		{
			get
			{
				//					// ws level exclusion
				//					if (m_writingSystem == Ignore())
				//						return Ignore();
				// field level exclusion
				if (Exclude)
					return LexTextControls.ksDoNotImport;   // "Not imported";
				if (AutoImport)
					return MarkerPresenter.AutoDescriptionText();
				// fw destination field
				return m_FWDestination;
			}
		}

		public ContentMapping(string marker, string desc, string className, string fwDest,
			string ws, string langDescriptor, int count, int order, Sfm2Xml.ClsFieldDescription fdesc, bool isCustom)
		{
			m_marker = marker;
			m_description = desc;
			m_FWDestination = fwDest;
			m_writingSystem = ws;
			m_langDescriptor = langDescriptor;
			m_count = count;
			m_srcOrder = order;
			//m_excluded = false;
			m_destinationClass = className;
			m_clsFieldDescription = fdesc;  // saved for now, used at end for producing map file
			m_LexImportField = null;
			if (m_clsFieldDescription == null)
			{
				if (!isCustom)
					m_clsFieldDescription = ClsFieldDescription;
				else
				{
					int shouldNotBeHere = 0;
					shouldNotBeHere++;
					m_clsFieldDescription = new Sfm2Xml.ClsCustomFieldDescription("", "", /*System.Guid.NewGuid(),*/ 0, false, 0,
						m_marker, " ", "string", this.m_langDescriptor, this.IsAbbrvField, this.m_FWDestination);
				}
			}
			m_isBeginMarker = false;
		}

		public Sfm2Xml.ClsFieldDescription ClsFieldDescription
		{
			get
			{
				if (m_clsFieldDescription != null)
					return m_clsFieldDescription;

				string dataType = "string";
				if (m_LexImportField != null)
					dataType = m_LexImportField.DataType;

				Sfm2Xml.ClsFieldDescriptionWrapper wrapper = new Sfm2Xml.ClsFieldDescriptionWrapper(m_marker,
					" ", dataType, this.m_langDescriptor, this.IsAbbrvField, this.m_FWDestination);
				return wrapper as Sfm2Xml.ClsFieldDescription;
			}
		}

		public void DoAutoImportWork()
		{
			m_LexImportField = null;
		}
	}
}