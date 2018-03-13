// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.Xml;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A comparer which uses the writing system collator to compare strings.
	/// </summary>
	public class WritingSystemComparer : IComparer, IPersistAsXml, IStoresLcmCache
	{
		private LcmCache m_cache;
		private CoreWritingSystemDefinition m_ws;

		#region Constructors, etc.
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		public WritingSystemComparer(CoreWritingSystemDefinition ws)
		{
			m_ws = ws;
			WsId = ws.Id;
		}

		/// <summary>
		/// Default constructor for use with IPersistAsXml
		/// </summary>
		public WritingSystemComparer()
		{
		}

		public LcmCache Cache
		{
			set { m_cache = value; }
		}

		#endregion

		public string WsId { get; private set; }

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
			if (m_ws == null)
			{
				m_ws = m_cache.ServiceLocator.WritingSystemManager.Get(WsId);
			}
			return m_ws.DefaultCollation.Collator.Compare(x, y);
		}
		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement node)
		{
			XmlUtils.SetAttribute(node, "ws", WsId);
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement node)
		{
			WsId = XmlUtils.GetMandatoryAttributeValue(node, "ws");
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			var comparer = obj as WritingSystemComparer;
			if (comparer == null)
			{
				return false;
			}
			return WsId == comparer.WsId;
		}

		public override int GetHashCode()
		{
			return WsId.GetHashCode();
		}
	}
}