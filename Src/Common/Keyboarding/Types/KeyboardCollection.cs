// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
