// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// A light weight object used in the merge dlg to show relevant objects.
	/// </summary>
	public class DummyCmObject : IComparable
	{
		private string m_displayName;

		/// <summary>
		/// Get the HVO for the dummy object.
		/// </summary>
		public int Hvo { get; }

		/// <summary>
		/// Get the writing system integer.
		/// </summary>
		public int WS { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public DummyCmObject(int hvo, string displayName, int ws)
		{
			Hvo = hvo;
			m_displayName = displayName ?? " ";
			WS = ws;
		}

		/// <summary>
		/// Override to show the object in the list view.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return m_displayName;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			var that = obj as DummyCmObject;
			return that == null ? -1 : m_displayName.CompareTo(that.m_displayName);
		}

		#endregion
	}
}