// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Resolves a layout <c>&lt;part ref="X"&gt;</c> reference to the part's content element
	/// (the <c>&lt;slice&gt;</c>/<c>&lt;obj&gt;</c>/<c>&lt;seq&gt;</c> inside the part). This is the
	/// seam over the legacy <c>Inventory</c> part lookup, kept framework-neutral so the importer can
	/// be unit tested with inline XML.
	/// </summary>
	public interface IPartResolver
	{
		/// <summary>
		/// Returns the content element of the part identified by class/layout-type/ref, or null if
		/// the part cannot be resolved.
		/// </summary>
		XElement ResolvePart(string className, string layoutType, string refName);

		/// <summary>
		/// Returns the content element of a part by its ref name alone, used for caller-injected
		/// children under object/sequence nodes whose destination class is not known from XML alone.
		/// Returns null if the ref is missing or ambiguous.
		/// </summary>
		XElement ResolvePartByRef(string refName);
	}

	/// <summary>Imports legacy XML Parts/Layout into the typed <see cref="ViewDefinitionModel"/>.</summary>
	public interface IViewDefinitionImporter
	{
		/// <summary>
		/// Imports a single <c>&lt;layout&gt;</c> element using <paramref name="parts"/> to resolve
		/// part references. Never throws on unsupported constructs; it records diagnostics instead.
		/// </summary>
		ViewDefinitionModel Import(XElement layoutElement, IPartResolver parts);
	}
}
