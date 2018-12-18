// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LCMBrowser
{
	/// <summary />
	public class TextIntPropInfo
	{
		/// <summary />
		public FwTextPropVar Variant { get; set; }
		/// <summary />
		public FwTextPropType Type { get; set; }
		/// <summary />
		public int Value { get; set; }

		private readonly string m_toStringValue;

		/// <summary />
		public TextIntPropInfo(ITsTextProps props, int iprop, LcmCache cache)
		{
			int nvar;
			int tpt;
			Value = props.GetIntProp(iprop, out tpt, out nvar);
			Type = (FwTextPropType)tpt;
			Variant = (FwTextPropVar)nvar;

			m_toStringValue = $"{Value}  ({Type})";

			if (tpt != (int)FwTextPropType.ktptWs)
			{
				return;
			}
			var ws = cache.ServiceLocator.WritingSystemManager.Get(Value);
			m_toStringValue += $"  {{{ws}}}";
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return m_toStringValue;
		}
	}
}