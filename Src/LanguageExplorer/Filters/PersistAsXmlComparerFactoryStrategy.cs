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
	internal sealed class PersistAsXmlComparerFactoryStrategy : IPersistAsXmlFactoryStrategy
	{
		/// <inheritdoc />
		FactoryNames IPersistAsXmlFactoryStrategy.Name => FactoryNames.comparer;

		/// <inheritdoc />
		public IPersistAsXml Create(IPersistAsXmlFactory persistAsXmlFactory, XElement element)
		{
			var className = element.Attribute("class").Value.Split('.').Last().Trim();
			switch (className)
			{
				case "StringFinderCompare":
					return new StringFinderCompare(persistAsXmlFactory, element);
				case "IcuComparer":
					return new IcuComparer(element);
				case "IntStringComparer":
					return new IntStringComparer();
				case "LcmCompare":
					return new LcmCompare(element);
				case "ReverseComparer":
					return new ReverseComparer(persistAsXmlFactory, element);
				case "WritingSystemComparer":
					return new WritingSystemComparer(element);
				default:
					throw new InvalidEnumArgumentException($"{className} not recognized by compare factory.");
			}
		}
	}
}