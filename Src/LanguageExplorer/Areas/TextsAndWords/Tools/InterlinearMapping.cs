// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml.Serialization;

namespace LanguageExplorer.Areas.TextsAndWords.Tools
{
	/// <summary>
	/// Simple class to record the bits of information we want about how one marker maps onto FieldWorks.
	/// This is serialized to form the .map file, so change with care.
	/// It is public only because XmlSerializer requires everything to be.
	/// </summary>
	[Serializable]
	public class InterlinearMapping
	{
		public InterlinearMapping()
		{
		}

		public InterlinearMapping(InterlinearMapping copyFrom)
		{
			Marker = copyFrom.Marker;
			Destination = copyFrom.Destination;
			Converter = copyFrom.Converter;
			WritingSystem = copyFrom.WritingSystem;
			Count = copyFrom.Count;
		}

		public string Marker;

		public InterlinDestination Destination;

		public string WritingSystem;

		public string Converter;

		[XmlIgnore]
		public string Count;
	}
}