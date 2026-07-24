// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>Which at-rest source a view definition was loaded from.</summary>
	public enum ViewDefinitionSourceKind
	{
		/// <summary>Compiled from runtime layout XML via <see cref="XmlLayoutImporter"/>.</summary>
		Xml,

		/// <summary>Loaded from the committed canonical JSON definition.</summary>
		Json
	}

	/// <summary>The model plus which source produced it and any load-path diagnostics (e.g. fallback reasons).</summary>
	public sealed class ViewDefinitionLoadResult
	{
		public ViewDefinitionLoadResult(
			ViewDefinitionModel model, ViewDefinitionSourceKind sourceKind, IReadOnlyList<ViewDiagnostic> loadDiagnostics)
		{
			Model = model;
			SourceKind = sourceKind;
			LoadDiagnostics = loadDiagnostics ?? (IReadOnlyList<ViewDiagnostic>)Array.Empty<ViewDiagnostic>();
		}

		public ViewDefinitionModel Model { get; }
		public ViewDefinitionSourceKind SourceKind { get; }
		public IReadOnlyList<ViewDiagnostic> LoadDiagnostics { get; }
	}

	/// <summary>
	/// Resolves a view definition for a surface, disabling runtime XML for a gated (migrated) surface in
	/// favor of the committed canonical JSON, while retaining the XML import as the audit/fallback path
	/// (task 9.4, per `canonical-view-definition-design.md` §4 step 4). For a non-gated surface, or when the
	/// JSON is missing or unreadable, it falls back to compiling the layout XML and records a diagnostic so
	/// the fallback is explicit, never silent. The actual gate source (PropertyTable/region manifest) and
	/// JSON file location are the thin XCore wrapper above this framework-neutral core.
	/// </summary>
	public sealed class ViewDefinitionLoader
	{
		private readonly ViewDefinitionCompiler _xmlCompiler;
		private readonly Func<ViewDefinitionSourceSnapshot, bool> _isGated;
		private readonly Func<ViewDefinitionSourceSnapshot, string> _jsonProvider;

		/// <param name="xmlCompiler">The XML import/compile path (audit + fallback). Required.</param>
		/// <param name="isGated">True when the surface should load JSON instead of runtime XML. Defaults to never.</param>
		/// <param name="jsonProvider">Returns the committed canonical JSON for the surface, or null/empty if none.</param>
		public ViewDefinitionLoader(
			ViewDefinitionCompiler xmlCompiler,
			Func<ViewDefinitionSourceSnapshot, bool> isGated = null,
			Func<ViewDefinitionSourceSnapshot, string> jsonProvider = null)
		{
			_xmlCompiler = xmlCompiler ?? throw new ArgumentNullException(nameof(xmlCompiler));
			_isGated = isGated ?? (_ => false);
			_jsonProvider = jsonProvider ?? (_ => null);
		}

		public ViewDefinitionLoadResult Load(ViewDefinitionSourceSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
			var nodePath = snapshot.ClassName + "/" + snapshot.LayoutName;
			var diagnostics = new List<ViewDiagnostic>();

			if (_isGated(snapshot))
			{
				var json = _jsonProvider(snapshot);
				if (string.IsNullOrEmpty(json))
				{
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "json-source-missing",
						"no canonical JSON definition for the gated surface; falling back to XML import", nodePath));
				}
				else
				{
					try
					{
						var model = ViewDefinitionJsonSerializer.Deserialize(json);
						return new ViewDefinitionLoadResult(model, ViewDefinitionSourceKind.Json, diagnostics);
					}
					catch (Exception ex)
					{
						diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "json-load-failed",
							$"canonical JSON for the gated surface could not be read ({ex.Message}); falling back to XML import",
							nodePath));
					}
				}
			}

			var xmlModel = _xmlCompiler.Compile(snapshot);
			return new ViewDefinitionLoadResult(xmlModel, ViewDefinitionSourceKind.Xml, diagnostics);
		}
	}
}
