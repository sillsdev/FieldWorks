// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Settings used while generating XHTML output by ConfiguredXHTMLGenerator
	/// </summary>
	internal sealed class GeneratorSettings
	{
		internal LcmCache Cache { get; }
		internal IReadonlyPropertyTable ReadOnlyPropertyTable { get; }
		internal bool UseRelativePaths { get; }
		internal bool CopyFiles { get; }
		internal string ExportPath { get; }
		internal bool RightToLeft { get; }
		internal bool IsWebExport { get; }

		internal GeneratorSettings(LcmCache cache, IPropertyTable propertyTable, bool relativePaths, bool copyFiles, string exportPath, bool rightToLeft = false, bool isWebExport = false)
			: this(cache, propertyTable == null ? null : new ReadOnlyPropertyTable(propertyTable), relativePaths, copyFiles, exportPath, rightToLeft, isWebExport)
		{
		}

		internal GeneratorSettings(LcmCache cache, IReadonlyPropertyTable readOnlyPropertyTable, bool relativePaths, bool copyFiles, string exportPath, bool rightToLeft = false, bool isWebExport = false)
		{
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(readOnlyPropertyTable, nameof(readOnlyPropertyTable));

			Cache = cache;
			ReadOnlyPropertyTable = readOnlyPropertyTable;
			UseRelativePaths = relativePaths;
			CopyFiles = copyFiles;
			ExportPath = exportPath;
			RightToLeft = rightToLeft;
			IsWebExport = isWebExport;
		}
	}
}