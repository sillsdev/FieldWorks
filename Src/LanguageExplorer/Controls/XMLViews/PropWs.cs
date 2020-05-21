// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class PropWs
	{
		/// <summary />
		public PropWs(int xflid, int xws)
		{
			Flid = xflid;
			Ws = xws;
		}

		/// <summary />
		public int Flid;

		/// <summary>0 if not applicable</summary>
		public int Ws;

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			return obj is PropWs propWs && propWs.Flid == Flid && propWs.Ws == Ws;
		}

		/// <summary>
		/// Probably not used but should have some overide when Equals overridden.
		/// </summary>
		public override int GetHashCode()
		{
			return Flid * (Ws + 11);
		}
	}
}