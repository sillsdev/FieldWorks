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
	/// <summary>
	/// This implements a no-op keyboard that can be used where we don't know what keyboard to use
	/// </summary>
	internal class KeyboardDescriptionNull: IKeyboardDescription
	{
		public KeyboardDescriptionNull()
		{
		}

		#region IKeyboardDescription implementation
		public void Activate()
		{
		}

		public void Deactivate()
		{
		}

		public int Id
		{
			get { return 0; }
		}

		public KeyboardType Type
		{
			get { return KeyboardType.System; }
		}

		public string Name
		{
			get { return string.Empty; }
		}

		public IKeyboardAdaptor Engine
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}
}
