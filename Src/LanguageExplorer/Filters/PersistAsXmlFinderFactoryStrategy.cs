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
	internal sealed class PersistAsXmlFinderFactoryStrategy : IPersistAsXmlFactoryStrategy
	{
		/// <inheritdoc />
		FactoryNames IPersistAsXmlFactoryStrategy.Name => FactoryNames.finder;

		/// <inheritdoc />
		public IPersistAsXml Create(IPersistAsXmlFactory persistAsXmlFactory, XElement element)
		{
			var className = element.Attribute("class").Value.Split('.').Last().Trim();
			switch (className)
			{
				case "MultiIndirectMlPropFinder":
					return new MultiIndirectMlPropFinder(element);
				case "OneIndirectAtomMlPropFinder":
					return new OneIndirectAtomMlPropFinder(element);
				case "OneIndirectMlPropFinder":
					return new OneIndirectMlPropFinder(element);
				case "OwnIntPropFinder":
					return new OwnIntPropFinder(element);
				case "OwnMlPropFinder":
					return new OwnMlPropFinder(element);
				case "OwnMonoPropFinder":
					return new OwnMonoPropFinder(element);
				case "LayoutFinder":
					return new LayoutFinder(element);
				case "SortMethodFinder":
					return new SortMethodFinder(element);
				case "IntCompareFinder":
					return new IntCompareFinder(element);
				default:
					throw new InvalidEnumArgumentException($"{className} not recognized by finder factory.");
			}
		}
	}
}