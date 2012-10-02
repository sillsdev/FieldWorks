using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace SILConvertersWordML
{
	public class XPathIterator
	{
		protected XPathNodeIterator m_ni = null;
		protected bool m_bConvertAsCharValue;

		public XPathIterator(XPathNodeIterator ni, bool bConvertAsCharValue)
		{
			NodeIterator = ni;
			ConvertAsCharValue = bConvertAsCharValue;
		}

		protected XPathNodeIterator NodeIterator
		{
			get { return m_ni; }
			set { m_ni = value; }
		}

		public bool ConvertAsCharValue
		{
			get { return m_bConvertAsCharValue; }
			set { m_bConvertAsCharValue = value; }
		}

		public XPathIterator Clone()
		{
			return new XPathIterator(NodeIterator.Clone(), ConvertAsCharValue);
		}

		public string CurrentValue
		{
			get
			{
				string strValue;
				if (ConvertAsCharValue)
				{
					try
					{
						char ch = (char)Convert.ToInt32(NodeIterator.Current.Value, 16);
						strValue = ch.ToString();
					}
					catch(Exception ex)
					{
						throw new ApplicationException(String.Format("Can't convert inserted symbol value '{0}'. Contact silconverters_support@sil.org", NodeIterator.Current.Value), ex);
					}
				}
				else
					strValue = NodeIterator.Current.Value;
				return strValue;
			}
		}

		public void SetCurrentValue(string str)
		{
			if (ConvertAsCharValue)
				str = String.Format("{0:X4}", (int)str[0]);
			NodeIterator.Current.SetValue(str);
		}

		public bool MoveNext()
		{
			return NodeIterator.MoveNext();
		}
	}

	public class IteratorMap : Dictionary<string, XPathIterator>
	{
		public bool IsInitialized = false;

		public new void Clear()
		{
			IsInitialized = false;
			base.Clear();
		}
	}
}
