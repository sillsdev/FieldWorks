// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Preview;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// Preview/sample data provider for the lexical-edit POC window. Keeps the preview host detached
	/// from LCModel by returning DTO/sample data only.
	/// </summary>
	public sealed class PocPreviewDataProvider : IFwPreviewDataProvider
	{
		public object CreateDataContext(string dataMode)
		{
			if (string.Equals(dataMode, "sample", System.StringComparison.OrdinalIgnoreCase))
			{
				return PocEntryDto.CreateSample();
			}

			return new PocEntryDto(
				new List<WsAlternative>
				{
					new WsAlternative("seh", string.Empty),
					new WsAlternative("en", string.Empty, "Times New Roman")
				},
				new List<MorphTypeOption>
				{
					new MorphTypeOption("stem", "stem"),
					new MorphTypeOption("root", "root"),
					new MorphTypeOption("prefix", "prefix"),
					new MorphTypeOption("suffix", "suffix")
				},
				"stem",
				new List<WsAlternative>
				{
					new WsAlternative("en", string.Empty, "Times New Roman"),
					new WsAlternative("pt", string.Empty, "Times New Roman")
				});
		}
	}
}
