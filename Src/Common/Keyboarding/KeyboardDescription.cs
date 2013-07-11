// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;
using SIL.FieldWorks.Common.Keyboarding.InternalInterfaces;

namespace SIL.FieldWorks.Common.Keyboarding
{
	/// <summary>
	/// Default implementation for a keyboard layout/language description.
	/// </summary>
	public class KeyboardDescription: IKeyboardDescription
	{
		/// <summary>
		/// The null keyboard description
		/// </summary>
		public static IKeyboardDescription Zero = new KeyboardDescriptionNull();

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescription"/> class.
		/// </summary>
		internal KeyboardDescription(string name, string locale, IKeyboardAdaptor engine)
			: this(name, locale, engine, KeyboardType.System)
		{
		}

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescription"/> class.
		/// </summary>
		internal KeyboardDescription(string name, string locale,
			IKeyboardAdaptor engine, KeyboardType type)
		{
			Name = name;
			Locale = locale;
			Engine = engine;
			Type = type;
		}

		/// <summary>
		/// Gets an identifier of the language/keyboard layout.
		/// </summary>
		public string Id
		{
			get
			{
				return GetId(Locale, Name);
			}
		}

		/// <summary>
		/// Gets the identifier for the keyboard based on the provided locale and layout.
		/// </summary>
		public static string GetId(string locale, string layout)
		{
			return string.Format("{0}_{1}", locale, layout);
		}

		/// <summary>
		/// Gets the type of this keyboard (system or other)
		/// </summary>
		public KeyboardType Type { get; private set;}

		/// <summary>
		/// Gets a human-readable name of the language.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The Locale of the keyboard in the format languagecode2-country/regioncode2.
		/// This is mainly significant on Windows, which distinguishes (for example)
		/// a German keyboard used in Germany, Switzerland, and Holland.
		/// </summary>
		public string Locale { get; private set; }

		/// <summary>
		/// Gets the keyboard adaptor that handles this keyboard.
		/// </summary>
		internal IKeyboardAdaptor Engine { get; private set; }

		/// <summary>
		/// Activate this keyboard.
		/// </summary>
		public void Activate()
		{
			if (Engine != null)
				Engine.ActivateKeyboard(this);
		}

		/// <summary>
		/// Deactivate this keyboard.
		/// </summary>
		public void Deactivate()
		{
			if (Engine != null)
				Engine.DeactivateKeyboard(this);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescription"/>.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescription"/>.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is IKeyboardDescription))
				return false;
			var other = (IKeyboardDescription)obj;
			return other.Name == Name && other.Locale == Locale;
		}

		/// <summary>
		/// Serves as a hash function for a
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescription"/> object.
		/// </summary>
		public override int GetHashCode()
		{
			return Name.GetHashCode() ^ Locale.GetHashCode();
		}
	}
}
