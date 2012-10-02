using System;
using System.Collections;               // for base class (Hashtable)
using System.Runtime.InteropServices;   // for the class attributes

namespace ECInterfaces
{
	/// <summary>
	/// Arbitrary properties for EncodingConverter's repository
	/// </summary>
	[ClassInterface(ClassInterfaceType.AutoDual)]
	public class ECAttributes : Hashtable
	{
		private AttributeType   m_eAttributeType;
		private string          m_strItem;
		private IEncConverters  m_aECs;

		public ECAttributes(IEncConverters aECs, string sItem, AttributeType repositoryItem)
		{
			m_aECs = aECs;
			m_strItem = sItem;
			m_eAttributeType = repositoryItem;
		}

		public AttributeType Type
		{
			get{ return m_eAttributeType; }
		}

		public string   RepositoryItem
		{
			get{ return m_strItem; }
		}

		public new string this[object key]
		{
			get
			{
				return (string)base[key];
			}
			set
			{
				base[key] = value.ToString();
			}
		}

		public override void Add(object Key, object Value)
		{
			System.Diagnostics.Debug.Assert(m_aECs != null);
			m_aECs.AddAttribute(this, Key, Value);
			AddNonPersist(Key,Value);
		}

		public void AddNonPersist(object Key, object Value)
		{
			base.Add(Key,Value);
		}

		public override void Remove(object Key)
		{
			System.Diagnostics.Debug.Assert(m_aECs != null);
			m_aECs.RemoveAttribute(this, Key);
		}

		public void RemoveNonPersist(object Key)
		{
			base.Remove(Key);
		}
	}
}
