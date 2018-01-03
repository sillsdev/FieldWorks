// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Serialization;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer
{
	/// <summary>
	/// Provides per configuration serialization of the HomographConfiguration data (which is a singleton for views purposes)
	/// </summary>
	public class DictionaryHomographConfiguration
	{
		internal DictionaryHomographConfiguration() {}

		internal DictionaryHomographConfiguration(HomographConfiguration config)
		{
			HomographNumberBefore = config.HomographNumberBefore;
			ShowSenseNumber = config.ShowSenseNumberRef;
			ShowSenseNumberReversal = config.ShowSenseNumberReversal;
			ShowHwNumber = config.ShowHomographNumber(HomographConfiguration.HeadwordVariant.Main);
			ShowHwNumInCrossRef = config.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef);
			ShowHwNumInReversalCrossRef = config.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef);
			HomographWritingSystem = config.WritingSystem;
			CustomHomographNumberList = config.CustomHomographNumbers;
		}

		/// <summary>
		/// Intended to be used to set the singleton HomographConfiguration in FLEx to the settings from this model
		/// </summary>
		internal void ExportToHomographConfiguration(HomographConfiguration config)
		{
			config.HomographNumberBefore = HomographNumberBefore;
			config.ShowSenseNumberRef = ShowSenseNumber;
			config.ShowSenseNumberReversal = ShowSenseNumberReversal;
			config.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, ShowHwNumber);
			config.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, ShowHwNumInCrossRef);
			config.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef, ShowHwNumInReversalCrossRef);
			config.WritingSystem = HomographWritingSystem;
			config.CustomHomographNumbers = CustomHomographNumberList;
		}

		[XmlIgnore]
		public List<string> CustomHomographNumberList { get; internal set; }

		[XmlAttribute("showHwNumInReversalCrossRef")]
		public bool ShowHwNumInReversalCrossRef { get; set; }

		[XmlAttribute("showHwNumInCrossRef")]
		public bool ShowHwNumInCrossRef { get; set; }

		[XmlAttribute("showHwNumber")]
		public bool ShowHwNumber { get; set; }

		[XmlAttribute("showSenseNumberReversal")]
		public bool ShowSenseNumberReversal { get; set; }

		[XmlAttribute("showSenseNumber")]
		public bool ShowSenseNumber { get; set; }

		[XmlAttribute("homographNumberBefore")]
		public bool HomographNumberBefore { get; set; }

		[XmlAttribute("customHomographNumbers")]
		public string CustomHomographNumbers
		{
			get
			{
				return CustomHomographNumberList == null ? string.Empty : WebUtility.HtmlEncode(string.Join(",", CustomHomographNumberList));
			}
			set
			{
				CustomHomographNumberList = new List<string>(WebUtility.HtmlDecode(value).Split(new []{','}, StringSplitOptions.RemoveEmptyEntries));
			}
		}

		[XmlAttribute("homographWritingSystem")]
		public string HomographWritingSystem { get; set; }
	}
}