// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Implement (Fake)DoIt by replacing the current value with an empty string.
	/// </summary>
	internal class ClearMethod : DoItMethod
	{
		private readonly ITsString m_newValue;
		internal ClearMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XElement spec)
			: base(cache, sda, accessor, spec)
		{
			m_newValue = TsStringUtils.EmptyString(accessor.WritingSystem);
		}

		/// <summary>
		/// We can do a replace if the current value is not empty.
		/// </summary>
		protected override bool OkToChange(int hvo)
		{
			if (!base.OkToChange(hvo))
			{
				return false;
			}
			var tss = OldValue(hvo);
			return (tss != null && tss.Length != 0);
		}

		/// <summary>
		/// Actually produce the replacement string.
		/// </summary>
		protected override ITsString NewValue(int hvo)
		{
			return m_newValue;
		}
	}
}