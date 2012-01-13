// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.Common.Keyboarding
{
	internal class KeyboardErrorDescription: IKeyboardErrorDescription
	{
		public KeyboardErrorDescription(object details): this(KeyboardType.System, details)
		{
		}

		public KeyboardErrorDescription(KeyboardType type, object details)
		{
			Type = type;
			Details = details;
		}

		#region IKeyboardErrorDescription implementation
		public KeyboardType Type { get; private set; }

		public object Details { get; private set; }
		#endregion
	}
}
