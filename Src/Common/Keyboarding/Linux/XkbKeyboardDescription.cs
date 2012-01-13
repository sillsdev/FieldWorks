// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using SIL.FieldWorks.Common.Keyboarding;

namespace SIL.FieldWorks.Common.Keyboarding.Linux
{
	/// <summary>
	/// Keyboard description for a XKB keyboard layout.
	/// </summary>
	public class XkbKeyboardDescription: KeyboardDescription
	{
		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.Linux.XkbKeyboardDescription"/> class.
		/// </summary>
		/// <param name='id'>Keyboard identifier.</param>
		/// <param name='name'>Name of the keyboard layout</param>
		/// <param name='engine'>The keyboard adaptor that will handle this keyboard</param>
		/// <param name='groupIndex'>The group index of this xkb keyboard</param>
		public XkbKeyboardDescription(int id, string name, IKeyboardAdaptor engine,
			int groupIndex): base(id, name, engine)
		{
			GroupIndex = groupIndex;
		}

		/// <summary>
		/// Gets the group index of this keyboard.
		/// </summary>
		public int GroupIndex { get; private set; }
	}
}
#endif
