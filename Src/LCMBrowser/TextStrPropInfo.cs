// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LCMBrowser
{
	/// <summary />
	public class TextStrPropInfo
	{
		/// <summary />
		public FwTextPropType Type { get; set; }
		/// <summary />
		public string Value { get; set; }

		/// <summary />
		public TextStrPropInfo(ITsTextProps props, int iprop)
		{
			int tpt;
			Value = props.GetStrProp(iprop, out tpt);
			Type = (FwTextPropType)tpt;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return $"{Value}  ({Type})";
		}
	}
}