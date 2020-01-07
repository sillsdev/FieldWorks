// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LCMBrowser
{
	/// <summary />
	public class TsStringRunInfo
	{
		/// <summary />
		public string Text { get; set; }
		/// <summary />
		public TextProps TextProps { get; set; }

		/// <summary />
		public TsStringRunInfo(int irun, ITsString tss, LcmCache cache)
		{
			Text = "\"" + (tss.get_RunText(irun) ?? string.Empty) + "\"";
			TextProps = new TextProps(irun, tss, cache);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return (Text ?? string.Empty);
		}
	}
}