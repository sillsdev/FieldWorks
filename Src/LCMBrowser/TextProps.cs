// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LCMBrowser
{
	/// <summary />
	public class TextProps
	{
		/// <summary />
		public int StrPropCount { get; set; }
		/// <summary />
		public int IntPropCount { get; set; }
		/// <summary />
		public int IchMin { get; set; }
		/// <summary />
		public int IchLim { get; set; }
		/// <summary />
		public TextStrPropInfo[] StrProps { get; set; }
		/// <summary />
		public TextIntPropInfo[] IntProps { get; set; }

		/// <summary />
		public TextProps(int irun, ITsString tss, LcmCache cache)
		{
			TsRunInfo runinfo;
			var ttp = tss.FetchRunInfo(irun, out runinfo);
			IchMin = runinfo.ichMin;
			IchLim = runinfo.ichLim;
			SetProps(ttp, cache);
		}

		/// <summary />
		public TextProps(ITsTextProps ttp, LcmCache cache)
		{
			StrPropCount = ttp.StrPropCount;
			IntPropCount = ttp.IntPropCount;
			SetProps(ttp, cache);
		}

		/// <summary>
		/// Sets the int and string properties.
		/// </summary>
		private void SetProps(ITsTextProps ttp, LcmCache cache)
		{
			// Get the string properties.
			StrPropCount = ttp.StrPropCount;
			StrProps = new TextStrPropInfo[StrPropCount];
			for (var i = 0; i < StrPropCount; i++)
			{
				StrProps[i] = new TextStrPropInfo(ttp, i);
			}

			// Get the integer properties.
			IntPropCount = ttp.IntPropCount;
			IntProps = new TextIntPropInfo[IntPropCount];
			for (var i = 0; i < IntPropCount; i++)
			{
				IntProps[i] = new TextIntPropInfo(ttp, i, cache);
			}
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return $"{{IchMin={IchMin}, IchLim={IchLim}, StrPropCount={StrPropCount}, IntPropCount={IntPropCount}}}";
		}
	}
}