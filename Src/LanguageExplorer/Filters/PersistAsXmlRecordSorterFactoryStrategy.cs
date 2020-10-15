// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;

namespace LanguageExplorer.Filters
{
	[Export(typeof(IPersistAsXmlFactoryStrategy))]
	internal sealed class PersistAsXmlRecordSorterFactoryStrategy : IPersistAsXmlFactoryStrategy
	{
		/// <inheritdoc />
		FactoryNames IPersistAsXmlFactoryStrategy.Name => FactoryNames.sorter;

		/// <inheritdoc />
		public IPersistAsXml Create(IPersistAsXmlFactory persistAsXmlFactory, XElement element)
		{
			var className = element.Attribute("class").Value.Split('.').Last().Trim();
			switch (className)
			{
				// All derive from RecordSorter (abstract class)
				case "AndSorter":
					return new AndSorter(persistAsXmlFactory, element);
				case "PropertyRecordSorter":
					return new PropertyRecordSorter(element);
				// All derive from GenRecordSorter, which derives from RecordFilter (abstract class)
				case "GenRecordSorter":
					return new GenRecordSorter(persistAsXmlFactory, element);
				case "FindResultSorter":
					return new FindResultSorter(persistAsXmlFactory, element);
				default:
					throw new InvalidEnumArgumentException($"{className} not recognized by matcher factory.");
			}
		}
	}
}