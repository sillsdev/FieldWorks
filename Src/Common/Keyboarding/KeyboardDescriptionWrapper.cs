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
	/// Wraps one or two keyboard descriptions. This allows to have a OtherIM type keyboard that
	/// on activation also activates the system keyboard. The current implementation gives
	/// precedence to the other keyboard; it's up to the respective keyboard adapter to deal with
	/// the second keyboard (e.g. on Windows we might simply ignore it).
	/// </summary>
	/// <remarks>It would be possible to implement this by adding an extra property on
	/// IKeyboardDescription, but that seems wrong because the association between the two
	/// keyboards gets set each time we access the keyboard.</remarks>
	internal class KeyboardDescriptionWrapper: IKeyboardDescription
	{
		private IKeyboardDescription m_SystemKeyboard;
		private IKeyboardDescription m_OtherImKeyboard;

		public KeyboardDescriptionWrapper(IKeyboardDescription systemKeyboard,
			IKeyboardDescription otherImKeyboard)
		{
			m_SystemKeyboard = systemKeyboard;
			m_OtherImKeyboard = otherImKeyboard;
		}

		private IKeyboardDescription PrimaryKeyboard
		{
			get { return m_OtherImKeyboard ?? m_SystemKeyboard; }
		}

		#region IKeyboardDescription implementation
		public void Activate()
		{
			Engine.ActivateKeyboard(PrimaryKeyboard, m_SystemKeyboard);
		}

		public void Deactivate()
		{
			Engine.DeactivateKeyboard(PrimaryKeyboard);
		}

		public int Id
		{
			get { return PrimaryKeyboard.Id; }
		}

		public KeyboardType Type
		{
			get { return PrimaryKeyboard.Type; }
		}

		public string Name
		{
			get { return PrimaryKeyboard.Name; }
		}

		public IKeyboardAdaptor Engine
		{
			get { return PrimaryKeyboard.Engine; }
		}
		#endregion

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescriptionWrapper"/>.
		/// </summary>
		public override string ToString()
		{
			return string.Format("{0} ({1})", PrimaryKeyboard.Name, m_SystemKeyboard.Name);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescriptionWrapper"/>.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is KeyboardDescriptionWrapper))
				return false;
			var other = (KeyboardDescriptionWrapper)obj;
			return other.m_OtherImKeyboard == m_OtherImKeyboard && other.m_SystemKeyboard == m_SystemKeyboard;
		}

		/// <summary>
		/// Serves as a hash function for a
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.KeyboardDescriptionWrapper"/> object.
		/// </summary>
		public override int GetHashCode()
		{
			return Id.GetHashCode() ^ Name.GetHashCode() ^ m_SystemKeyboard.GetHashCode() ^ base.GetHashCode();
		}
	}
}
