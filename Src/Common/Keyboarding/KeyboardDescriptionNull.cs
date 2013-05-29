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

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescriptionNull"/>.
		/// </summary>
		public override string ToString()
		{
			return "<no keyboard>";
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescriptionNull"/>.
		/// </summary>
		public override bool Equals(object obj)
		{
			return obj is KeyboardDescriptionNull;
		}

		/// <summary>
		/// Serves as a hash function for a
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescriptionNull"/> object.
		/// </summary>
		public override int GetHashCode()
		{
			return 0;
		}
	}
}
