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
	internal sealed class PersistAsXmlRecordFilterFactoryStrategy : IPersistAsXmlFactoryStrategy
	{
		/// <inheritdoc />
		FactoryNames IPersistAsXmlFactoryStrategy.Name => FactoryNames.filter;

		/// <inheritdoc />
		public IPersistAsXml Create(IPersistAsXmlFactory persistAsXmlFactory, XElement element)
		{
			var className = element.Attribute("class").Value.Split('.').Last().Trim();
			switch (className)
			{
				// All derive from RecordFilter (abstract class)
				case "AndFilter":
					return new AndFilter(persistAsXmlFactory, element);
				case "FilterBarCellFilter":
					return new FilterBarCellFilter(persistAsXmlFactory, element);
				case "ProblemAnnotationFilter":
					return new ProblemAnnotationFilter(element);
				case "WordSetFilter":
					return new WordSetFilter(element);
				case "WordsUsedOnlyElsewhereFilter":
					return new WordsUsedOnlyElsewhereFilter(element);
				// All derive from NullFilter, which derives from RecordFilter (abstract class)
				case "NullFilter":
					return new NullFilter(element);
				case "NoFilters":
					return new NoFilters(element);
				case "UncheckAll":
					return new UncheckAll(element);
				// All derive from ListChoiceFilter (abstract class), which derives from RecordFilter (abstract class)
				case "EntryPosFilter":
					return new EntryPosFilter(element);
				case "ColumnSpecFilter":
					return new ColumnSpecFilter(element);
				case "PosFilter":
					return new PosFilter(element);
				default:
					throw new InvalidEnumArgumentException($"{className} not recognized by record filter factory.");
			}
		}
	}
}