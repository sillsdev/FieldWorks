// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Language;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary />
	public class IcuComparer : IComparer, IPersistAsXml
	{
		/// <summary />
		protected ILgCollatingEngine m_lce;

		/// <summary>
		/// Key for the Hashtable is a string. Value is a byte[].
		/// </summary>
		protected Hashtable m_htskey = new Hashtable();

		/// <summary>
		/// Made accessible for testing.
		/// </summary>
		public string WsCode { get; protected set; }

		#region Constructors, etc.

		/// <summary />
		public IcuComparer(string sWs)
		{
			WsCode = sWs;
		}

		/// <summary>
		/// Default constructor for use with IPersistAsXml
		/// </summary>
		public IcuComparer() : this(null)
		{
		}

		/// <summary>
		/// Opens the collating engine.
		/// </summary>
		public void OpenCollatingEngine()
		{
			if (m_lce == null)
			{
				m_lce = new ManagedLgIcuCollator();
			}
			else
			{
				m_lce.Close();
			}
			m_lce.Open(WsCode);
		}

		/// <summary>
		/// Closes the collating engine.
		/// </summary>
		public void CloseCollatingEngine()
		{
			if (m_lce != null)
			{
				m_lce.Close();
				//Marshal.ReleaseComObject(m_lce);
				var disposable = m_lce as IDisposable;
				disposable?.Dispose();
				m_lce = null;
			}
			m_htskey?.Clear();
		}
		#endregion

		#region IComparer Members

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x, object y)
		{
			var a = x as string;
			var b = y as string;
			if (a == b)
			{
				return 0;
			}
			if (a == null)
			{
				return 1;
			}
			if (b == null)
			{
				return -1;
			}
			byte[] ka;
			byte[] kb;
			if (m_lce != null)
			{
				var kaObj = m_htskey[a];
				if (kaObj != null)
				{
					ka = (byte[])kaObj;
				}
				else
				{
					ka = (byte[])m_lce.get_SortKeyVariant(a, LgCollatingOptions.fcoDefault);
					m_htskey.Add(a, ka);
				}
				var kbObj = m_htskey[b];
				if (kbObj != null)
				{
					kb = (byte[])kbObj;
				}
				else
				{
					kb = (byte[])m_lce.get_SortKeyVariant(b, LgCollatingOptions.fcoDefault);
					m_htskey.Add(b, kb);
				}
			}
			else
			{
				OpenCollatingEngine();
				ka = (byte[])m_lce.get_SortKeyVariant(a, LgCollatingOptions.fcoDefault);
				kb = (byte[])m_lce.get_SortKeyVariant(b, LgCollatingOptions.fcoDefault);
				CloseCollatingEngine();
			}
			// This is what m_lce.CompareVariant(ka,kb,...) would do.
			// Simulate strcmp on the two NUL-terminated byte strings.
			// This avoids marshalling back and forth.
			int nVal;
			if (ka.Length == 0)
			{
				nVal = -kb.Length; // zero if equal, neg if b is longer (considered larger)
			}
			else if (kb.Length == 0)
			{
				nVal = 1; // ka is longer and considered larger.
			}
			else
			{
				// Normal case, null termination should be present.
				int ib;
				for (ib = 0; ka[ib] == kb[ib] && ka[ib] != 0; ++ib)
				{
					// skip merrily along until strings differ or end.
				}
				nVal = ka[ib] - kb[ib];
			}
			return nVal;
		}
		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement element)
		{
			XmlUtils.SetAttribute(element, "ws", WsCode);
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement element)
		{
			WsCode = XmlUtils.GetMandatoryAttributeValue(element, "ws");
		}

		#endregion

		/// <summary />
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (GetType() != obj.GetType())
			{
				return false;
			}
			var that = (IcuComparer)obj;
			if (m_htskey == null)
			{
				if (that.m_htskey != null)
				{
					return false;
				}
			}
			else
			{
				if (that.m_htskey == null)
				{
					return false;
				}
				if (m_htskey.Count != that.m_htskey.Count)
				{
					return false;
				}
				var ie = that.m_htskey.GetEnumerator();
				while (ie.MoveNext())
				{
					if (!m_htskey.ContainsKey(ie.Key) || m_htskey[ie.Key] != ie.Value)
					{
						return false;
					}
				}
			}
			if (m_lce != that.m_lce)
			{
				return false;
			}
			return WsCode == that.WsCode;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (m_htskey != null)
			{
				hash += m_htskey.Count * 53;
			}
			if (m_lce != null)
			{
				hash += m_lce.GetHashCode();
			}
			if (WsCode != null)
			{
				hash *= WsCode.GetHashCode();
			}
			return hash;
		}
	}
}