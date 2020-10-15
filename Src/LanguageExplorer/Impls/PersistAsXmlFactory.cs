// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer.Impls
{
	internal sealed class PersistAsXmlFactory : IPersistAsXmlFactory
	{
		[ImportMany(typeof(IPersistAsXmlFactoryStrategy))]
		private List<IPersistAsXmlFactoryStrategy> _strategies;

		internal PersistAsXmlFactory()
		{
			// Use MEF to create all IPersistAsXmlFactory implementations.
			var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
			using (var container = new CompositionContainer(catalog))
			{
				container.ComposeParts(this);
			}
		}

		/// <inheritdoc />
		public T Create<T>(XElement element)
		{
			return (T)_strategies.First(factory => factory.Name == (FactoryNames)Enum.Parse(typeof(FactoryNames), element.Name.LocalName)).Create(this, element);
		}
	}
}