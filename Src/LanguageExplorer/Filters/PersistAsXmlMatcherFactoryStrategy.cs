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
	internal sealed class PersistAsXmlMatcherFactoryStrategy : IPersistAsXmlFactoryStrategy
	{
		/// <inheritdoc />
		FactoryNames IPersistAsXmlFactoryStrategy.Name => FactoryNames.matcher;

		/// <inheritdoc />
		public IPersistAsXml Create(IPersistAsXmlFactory persistAsXmlFactory, XElement element)
		{
			var className = element.Attribute("class").Value.Split('.').Last().Trim();
			switch (className)
			{
				// All derive from BaseMatcher (abstract class)
				case "BadSpellingMatcher":
					return new BadSpellingMatcher(element);
				case "BlankMatcher":
					return new BlankMatcher(element);
				case "DateTimeMatcher":
					return new DateTimeMatcher(element);
				case "InvertMatcher":
					return new InvertMatcher(persistAsXmlFactory, element);
				case "NonBlankMatcher":
					return new NonBlankMatcher(element);
				case "NotEqualIntMatcher":
					return new NotEqualIntMatcher(element);
				case "RangeIntMatcher":
					return new RangeIntMatcher(element);
				// All derive from SimpleStringMatcher (abstract class), which derives from BaseMatcher (abstract class)
				case "AnywhereMatcher":
					return new AnywhereMatcher(element);
				case "BeginMatcher":
					return new BeginMatcher(element);
				case "EndMatcher":
					return new EndMatcher(element);
				case "ExactMatcher":
					return new ExactMatcher(element);
				case "RegExpMatcher":
					return new RegExpMatcher(element);
				default:
					throw new InvalidEnumArgumentException($"{className} not recognized by matcher factory.");
			}
		}
	}
}