// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml.Linq;

namespace LanguageExplorer.Controls.DetailControls
{
	internal static class RecordChangeHandlerFactory
	{
		internal static IRecordChangeHandler CreateRecordChangeHandler(XElement recordChangeHandlerElement)
		{
			var classToCreate = recordChangeHandlerElement.Attribute("class").Value;
			switch (classToCreate)
			{
				case "ReversalIndexEntryChangeHandler":
					return new ReversalIndexEntryChangeHandler();
				case "LexEntryChangeHandler":
					return new LexEntryChangeHandler();
				default:
					throw new ArgumentException($"Don't know how to create '{classToCreate}'.");
			}
		}
	}
}