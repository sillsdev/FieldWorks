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
		public KeyboardDescription(int id, string name, IKeyboardAdaptor engine)
			: this(id, name, engine, KeyboardType.System)
		{
		}

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescription"/> class.
		/// </summary>
		public KeyboardDescription(int id, string name, IKeyboardAdaptor engine, KeyboardType type)
		{
			Id = id;
			Name = name;
			Engine = engine;
			Type = type;
		}

		/// <summary>
		/// Gets an identifier of the language/keyboard layout (LCID).
		/// </summary>
		public int Id { get; private set; }

		/// <summary>
		/// Gets the type of this keyboard (system or other)
		/// </summary>
		public KeyboardType Type { get; private set;}

		/// <summary>
		/// Gets a human-readable name of the language.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the keyboard adaptor that handles this keyboard.
		/// </summary>
		public IKeyboardAdaptor Engine { get; private set; }

		/// <summary>
		/// Activate this keyboard.
		/// </summary>
		public void Activate()
		{
			if (Engine != null)
				Engine.ActivateKeyboard(this, null);
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
			var other = obj as IKeyboardDescription;
			if (other == null)
				return false;
			return other.Id == Id && other.Name == Name;
		}

		/// <summary>
		/// Serves as a hash function for a
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescription"/> object.
		/// </summary>
		public override int GetHashCode()
		{
			return Id.GetHashCode() ^ Name.GetHashCode() ^ base.GetHashCode();
		}
	}
}
