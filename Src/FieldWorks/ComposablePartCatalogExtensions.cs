// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

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
	}
}
