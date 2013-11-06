// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.ObjectModel;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;

namespace SIL.FieldWorks.Common.Keyboarding.Types
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A collection of keyboard descriptions
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyboardCollection: KeyedCollection<string, IKeyboardDescription>
	{
		#region Overrides of KeyedCollection<string,IKeyboardDescription>
		/// <summary>
		///Returns the key from the specified <paramref name="item"/>.
		/// </summary>
		protected override string GetKeyForItem(IKeyboardDescription item)
		{
			return item.Id;
		}
		#endregion

		public bool Contains(string layout, string locale)
		{
			return Contains(KeyboardDescription.GetId(layout, locale));
		}

		public IKeyboardDescription this[string layout, string locale]
		{
			get { return this[KeyboardDescription.GetId(layout, locale)]; }
		}
	}
}
