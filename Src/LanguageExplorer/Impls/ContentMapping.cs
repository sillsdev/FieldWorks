// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls;
using LanguageExplorer.SfmToXml;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// This class contains the object that is used for containing the data that is
	/// used in the content mapping display of the data.  It lives in the MarkerPresenter
	/// class as a public subclass.
	/// </summary>
	internal sealed class ContentMapping
	{
		private ClsFieldDescription m_clsFieldDescription;
		private LexImportField m_LexImportField;

		internal static string Ignore => LanguageExplorerControls.ksIgnore;

		internal static string Unknown => LanguageExplorerControls.ksUnknown;

		internal string Marker { get; }

		internal string Description { get; set; }

		internal string DestinationClass { get; set; }

		internal string RawDestinationField { get; set; }

		internal string LanguageDescriptorRaw { get; private set; }

		internal string LanguageDescriptor
		{
			get => IsLangIgnore ? string.Format(LanguageExplorerControls.ksX_Ignored, LanguageDescriptorRaw) : LanguageDescriptorRaw;
			set => LanguageDescriptorRaw = value;
		}

		internal string WritingSystem { get; private set; }

		internal bool IsBeginMarker { get; set; }

		internal void UpdateLanguageValues(string writingSystemName, string shortName, string langDescriptor)
		{
			WritingSystem = writingSystemName;
			LanguageDescriptorRaw = langDescriptor;
			m_clsFieldDescription.UpdateLanguageValues(LanguageDescriptorRaw, shortName);
		}

		internal int Count { get; set; }

		internal int Order { get; set; }

		internal void AddLexImportCustomField(ILexImportField field, string uiClass)
		{
			AddLexImportField(field);
			((ILexImportCustomField)m_LexImportField).UIClass = uiClass;
			var xyz = LexImportField.ClsFieldDescriptionWith(ClsFieldDescription);
			m_clsFieldDescription = xyz;
		}

		internal void AddLexImportField(ILexImportField field)
		{
			m_LexImportField = field as LexImportField;
			m_clsFieldDescription.Type = field != null ? field.DataType : "string";
		}

		internal ILexImportField LexImportField => m_LexImportField;

		internal bool Exclude
		{
			get => m_clsFieldDescription.IsExcluded;
			set => m_clsFieldDescription.IsExcluded = value;
		}

		internal bool AutoImport
		{
			get => m_clsFieldDescription.IsAutoImportField;
			set => m_clsFieldDescription.IsAutoImportField = value;
		}

		internal void ClearRef()
		{
			m_clsFieldDescription.ClearRef();
		}

		internal bool IsRefField => m_clsFieldDescription.IsRef;

		internal string RefField
		{
			get => m_clsFieldDescription.RefFunc;
			set => m_clsFieldDescription.RefFunc = value;
		}

		internal string RefFieldWS
		{
			get => m_clsFieldDescription.RefFuncWS;
			set => m_clsFieldDescription.RefFuncWS = value;
		}

		internal string FwId
		{
			get => m_clsFieldDescription.MeaningID;
			set => m_clsFieldDescription.MeaningID = value;
		}

		internal bool IsAbbrvField => m_LexImportField?.IsAbbrField ?? false;

		internal bool IsAbbr => m_LexImportField != null && m_clsFieldDescription.IsAbbr;

		internal void UpdateAbbrValue(bool isAbbr)
		{
			if (!IsAbbrvField)
			{
				return;
			}
			m_clsFieldDescription.IsAbbr = isAbbr;
		}

		internal string[] ListViewStrings()
		{
			var customTag = string.Empty;
			if (LexImportField is LexImportCustomField lexImportCustomField)
			{
				customTag = $" (Custom {lexImportCustomField.UIClass})";
			}
			else if (IsRefField)
			{
				customTag = $" ({RefField})";
			}
			return new[] {"\\" + Marker,						// col 1
				Convert.ToString(Order), // col 2
				Convert.ToString(Count),	// col 3
				Description,					// col 4
				DestinationField + customTag,	// col 5
				LanguageDescriptor };           // col 6
		}

		internal bool IsLangIgnore => WritingSystem == Ignore;

		// field level exclusion
		internal string DestinationField => Exclude ? LanguageExplorerControls.ksDoNotImport : AutoImport ? LanguageExplorerControls.ksImportResidue_Auto : RawDestinationField;

		internal ContentMapping(string marker, string desc, string className, string fwDest, string ws, string langDescriptor, int count, int order, ClsFieldDescription fdesc, bool isCustom)
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
					m_clsFieldDescription = new ClsCustomFieldDescription(string.Empty, string.Empty, 0, false, 0,
						Marker, " ", "string", LanguageDescriptorRaw, IsAbbrvField, RawDestinationField);
				}
			}
			IsBeginMarker = false;
		}

		internal ClsFieldDescription ClsFieldDescription
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

		internal void DoAutoImportWork()
		{
			m_LexImportField = null;
		}
	}
}