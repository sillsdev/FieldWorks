// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.SfmToXml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class contains the object that is used for containing the data that is
	/// used in the content mapping display of the data.  It lives in the MarkerPresenter
	/// class as a public subclass.
	/// </summary>
	public class ContentMapping
	{
		private ClsFieldDescription m_clsFieldDescription;
		private LexImportField m_LexImportField;

		public static string Ignore() { return LexTextControls.ksIgnore; }
		public static string Unknown() { return LexTextControls.ksUnknown; }

		public string Marker { get; }

		public string Description { get; set; }

		public string DestinationClass { get; set; }

		public string RawDestinationField { get; set; }

		public string LanguageDescriptorRaw { get; private set; }

		public string LanguageDescriptor
		{
			get
			{
				return IsLangIgnore ? string.Format(LexTextControls.ksX_Ignored, LanguageDescriptorRaw) : LanguageDescriptorRaw;
			}
			set { LanguageDescriptorRaw = value; }
		}
		public string WritingSystem { get; private set; }

		public bool IsBeginMarker { get; set; }

		public void UpdateLanguageValues(string writingSystemName, string shortName, string langDescriptor)
		{
			WritingSystem = writingSystemName;
			LanguageDescriptorRaw = langDescriptor;
			m_clsFieldDescription.UpdateLanguageValues(LanguageDescriptorRaw, shortName);
		}


		public int Count { get; set; }

		public int Order { get; set; }

		public void AddLexImportCustomField(ILexImportField field, string uiClass)
		{
			AddLexImportField(field);
			((ILexImportCustomField)m_LexImportField).UIClass = uiClass;
			var xyz = LexImportField.ClsFieldDescriptionWith(ClsFieldDescription);
			m_clsFieldDescription = xyz;
		}

		public void AddLexImportField(ILexImportField field)
		{
			m_LexImportField = field as LexImportField;
			m_clsFieldDescription.Type = field != null ? field.DataType : "string";
		}

		public ILexImportField LexImportField => m_LexImportField;

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

		public bool IsAbbrvField => m_LexImportField?.IsAbbrField ?? false;

		public bool IsAbbr => m_LexImportField != null && m_clsFieldDescription.IsAbbr;

		public void UpdateAbbrValue(bool isAbbr)
		{
			if (!IsAbbrvField)
			{
				return;
			}

			m_clsFieldDescription.IsAbbr = isAbbr;
		}

		public string[] ListViewStrings()
		{
			var customTag = string.Empty;
			if (LexImportField is LexImportCustomField)
			{
				customTag = " (Custom " + ((LexImportCustomField)LexImportField).UIClass + ")";
			}
			else if (IsRefField)
			{
				customTag = " (" + RefField + ")";
			}
			return new[] {"\\"+Marker,						// col 1
				Convert.ToString(Order), // col 2
				Convert.ToString(Count),	// col 3
				Description,					// col 4
				DestinationField + customTag,	// col 5
				LanguageDescriptor };           // col 6
		}

		public bool IsLangIgnore => WritingSystem == Ignore();

		public string DestinationField
		{
			get
			{
				// field level exclusion
				if (Exclude)
				{
					return LexTextControls.ksDoNotImport;   // "Not imported";
				}
				return AutoImport ? MarkerPresenter.AutoDescriptionText() : RawDestinationField;
			}
		}

		public ContentMapping(string marker, string desc, string className, string fwDest,
			string ws, string langDescriptor, int count, int order, ClsFieldDescription fdesc, bool isCustom)
		{
			Marker = marker;
			Description = desc;
			RawDestinationField = fwDest;
			WritingSystem = ws;
			LanguageDescriptorRaw = langDescriptor;
			Count = count;
			Order = order;
			DestinationClass = className;
			m_clsFieldDescription = fdesc;  // saved for now, used at end for producing map file
			m_LexImportField = null;
			if (m_clsFieldDescription == null)
			{
				if (!isCustom)
				{
					m_clsFieldDescription = ClsFieldDescription;
				}
				else
				{
					m_clsFieldDescription = new ClsCustomFieldDescription(string.Empty, string.Empty, /*System.Guid.NewGuid(),*/ 0, false, 0,
						Marker, " ", "string", LanguageDescriptorRaw, IsAbbrvField, RawDestinationField);
				}
			}
			IsBeginMarker = false;
		}

		public ClsFieldDescription ClsFieldDescription
		{
			get
			{
				if (m_clsFieldDescription != null)
				{
					return m_clsFieldDescription;
				}

				var dataType = "string";
				if (m_LexImportField != null)
				{
					dataType = m_LexImportField.DataType;
				}

				return new ClsFieldDescription(Marker, " ", dataType, LanguageDescriptorRaw, IsAbbrvField, RawDestinationField);
			}
		}

		public void DoAutoImportWork()
		{
			m_LexImportField = null;
		}
	}
}