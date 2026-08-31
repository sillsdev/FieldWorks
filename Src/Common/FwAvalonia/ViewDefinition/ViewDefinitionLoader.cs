// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>The model plus any diagnostics produced while importing runtime layout
	/// XML.</summary>
	public sealed class ViewDefinitionLoadResult
	{
		public ViewDefinitionLoadResult(ViewDefinitionModel model)
		{
			Model = model;
		}

		public ViewDefinitionModel Model { get; }
	}

	/// <summary>
	/// Compiles a view definition from effective runtime layout XML. Canonical JSON is only an
	/// explicit interchange format; it is not an at-rest source and never participates in runtime
	/// loading.
	/// </summary>
	public sealed class ViewDefinitionLoader
	{
		private readonly ViewDefinitionCompiler _xmlCompiler;

		public ViewDefinitionLoader(ViewDefinitionCompiler xmlCompiler)
		{
			_xmlCompiler = xmlCompiler ?? throw new ArgumentNullException(nameof(xmlCompiler));
		}

		public ViewDefinitionLoadResult Load(ViewDefinitionSourceSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
			return new ViewDefinitionLoadResult(_xmlCompiler.Compile(snapshot));
		}
	}
}
