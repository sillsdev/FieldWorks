// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// A light weight object used in the merge dlg to show relevant objects.
	/// </summary>
	internal sealed class DummyCmObject : IComparable
	{
		private readonly ICmObject _cmObject;
		private readonly string _displayName;

		/// <summary>
		/// Get the HVO for the dummy object.
		/// </summary>
		internal int Hvo => _cmObject.Hvo;

		/// <summary>
		/// Get the writing system integer.
		/// </summary>
		internal int WS { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		internal DummyCmObject(ICmObject cmObject, string displayName, int ws)
		{
			_cmObject = cmObject;
			_displayName = displayName ?? " ";
			WS = ws;
		}

		/// <summary>
		/// Override to show the object in the list view.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _displayName;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return !(obj is DummyCmObject otherGuy) ? -1 : _displayName.CompareTo(otherGuy._displayName);
		}

		#endregion
	}
}