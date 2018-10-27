// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Extend the composition model
	/// </summary>
	public static class ComposablePartCatalogExtensions
	{
		/// <summary>
		/// Create a scoped definition of parts.
		/// </summary>
		/// <param name="catalog">The parent catalog</param>
		/// <param name="children">Scope catalog of parts.</param>
		/// <returns>A new CompositionScopeDefinition instance with <paramref name="catalog"/> as the parent of <paramref name="children"/>.</returns>
		public static CompositionScopeDefinition AsScope(this ComposablePartCatalog catalog, params CompositionScopeDefinition[] children)
		{
			return new CompositionScopeDefinition(catalog, children);
		}

		/// <summary>
		/// Create a globally scoped definition of parts.
		/// </summary>
		public static CompositionScopeDefinition AsScopeWithPublicSurface<T>(this ComposablePartCatalog catalog, params CompositionScopeDefinition[] children)
		{
			IEnumerable<ExportDefinition> definitions = catalog.Parts.SelectMany((p) => p.ExportDefinitions.Where((e) => e.ContractName == AttributedModelServices.GetContractName(typeof(T))));
			return new CompositionScopeDefinition(catalog, children, definitions);
		}
	}
}
