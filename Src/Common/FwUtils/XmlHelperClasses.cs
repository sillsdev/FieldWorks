// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlHelperClasses.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Represents an Integer
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct Integer
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Encapsulates an integer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public struct InternalInteger
		{

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="v"></param>
			/// --------------------------------------------------------------------------------
			public InternalInteger(int v)
			{
				val = v;
			}

			/// <summary>
			/// The value
			/// </summary>
			[XmlAttribute("val")]
			public int val;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="v"></param>
		/// --------------------------------------------------------------------------------
		public Integer(int v)
		{
			integer = new InternalInteger(v);
		}

		/// <summary>
		///
		/// </summary>
		[XmlElement("Integer")]
		public InternalInteger integer;
	}
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a Boolean value. Added by JohnT, following the model of Integer, which
	/// appears to be a trick to get the serializer to output the value in the form our
	/// XML representation requires.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct Boolean
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Encapsulates a Boolean
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public struct InternalBoolean
		{

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="v"></param>
			/// --------------------------------------------------------------------------------
			public InternalBoolean(bool v)
			{
				val = v;
			}

			/// <summary>
			/// An InternalBoolean is serialized as the attribute (val = "true") of the
			/// Boolean element.
			/// </summary>
			[XmlAttribute("val")]
			public bool val;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="v"></param>
		/// --------------------------------------------------------------------------------
		public Boolean(bool v)
		{
			boolean = new InternalBoolean(v);
		}

		/// <summary>
		/// This tells the serializer that an object of type Boolean should be serialized
		/// as the element 'Boolean'.
		/// </summary>
		[XmlElement("Boolean")]
		public InternalBoolean boolean;
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Represents an unicode string for XML serialization
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct UniString
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="s"></param>
		/// --------------------------------------------------------------------------------
		public UniString(string s)
		{
			str = s;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("Uni")]
		public string str;
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a string with associated writing system id
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(false)]
	[XmlType("AUni")]
	public struct StringWithWs
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new StringWithWs struct
		/// </summary>
		/// <param name="s">String</param>
		/// <param name="locale">ICU locale</param>
		/// ------------------------------------------------------------------------------------
		public StringWithWs(string s, string locale)
		{
			text = s;
			icuLocale = locale;
		}

		/// <summary>String</summary>
		[XmlText]
		public string text;
		/// <summary>The writing system id</summary>
		[XmlAttribute(AttributeName="ws")]
		public string icuLocale;
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Multi unicode strings for collation name
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public class CollationNameMultiUnicode: BaseMultiString
	{
		/// <summary></summary>
		protected ICollation m_coll;
		private int[] m_ws;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// --------------------------------------------------------------------------------
		public CollationNameMultiUnicode(): base()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="coll"></param>
		/// --------------------------------------------------------------------------------
		public CollationNameMultiUnicode(ICollation coll): base()
		{
			m_coll = coll;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a string in the given collation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override StringWithWs this[int i]
		{
			get
			{
				int ws = WritingSystems[i];
				return new StringWithWs(m_coll.get_Name(ws),
					m_coll.WritingSystemFactory.GetStrFromWs(ws));
			}
			set
			{
				// This is tricky in that it ignores the index parameter.  Which name is set
				// depends only on value.icuLocale.
				int ws = GetWsFromStr(value.icuLocale, m_coll.WritingSystemFactory);
				m_coll.set_Name(ws, value.text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of strings stored
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int Count
		{
			get
			{
				return m_coll.NameWsCount;
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system ids that this name is defined in
		/// </summary>
		/// --------------------------------------------------------------------------------
		private int[] WritingSystems
		{
			get
			{
				if (m_ws == null)
				{
					int c = m_coll.NameWsCount;
					using (ArrayPtr wsPtr = MarshalEx.ArrayToNative(c, typeof(int)))
					{
						m_coll.get_NameWss(c, wsPtr);
						m_ws = (int[])MarshalEx.NativeToArray(wsPtr, c, typeof(int));
					}
				}
				return m_ws;
			}
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Represents the name of the writing system
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public class NameMultiUnicode: BaseMultiUnicode
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// --------------------------------------------------------------------------------
		public NameMultiUnicode(): base()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// --------------------------------------------------------------------------------
		public NameMultiUnicode(IWritingSystem ws): base(ws)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of writing systems that implement this property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int GetCount
		{
			get { return m_LgWritingSystem.NameWsCount; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing systems that implement this property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void GetWs(int c, ArrayPtr ptr)
		{
			m_LgWritingSystem.get_NameWss(c, ptr);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string in the particular writing system
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override string GetString(int ws)
		{
			return m_LgWritingSystem.get_Name(ws);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the string in the particular writing system
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="text"></param>
		/// ------------------------------------------------------------------------------------
		protected override void SetString(int ws, string text)
		{
			m_LgWritingSystem.set_Name(ws, text);
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Represents the abbreviation of the writing system
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public class AbbrMultiUnicode: BaseMultiUnicode
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// --------------------------------------------------------------------------------
		public AbbrMultiUnicode(): base()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// --------------------------------------------------------------------------------
		public AbbrMultiUnicode(IWritingSystem ws): base(ws)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of writing systems that implement this property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int GetCount
		{
			get { return m_LgWritingSystem.AbbrWsCount; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing systems that implement this property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void GetWs(int c, ArrayPtr ptr)
		{
			m_LgWritingSystem.get_AbbrWss(c, ptr);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string in the particular writing system
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override string GetString(int ws)
		{
			return m_LgWritingSystem.get_Abbr(ws);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the string in the particular writing system
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="text"></param>
		/// ------------------------------------------------------------------------------------
		protected override void SetString(int ws, string text)
		{
			m_LgWritingSystem.set_Abbr(ws, text);
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for multi unicode strings
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public abstract class BaseMultiUnicode: BaseMultiString
	{
		/// <summary></summary>
		protected IWritingSystem m_LgWritingSystem;
		private int[] m_ws;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// --------------------------------------------------------------------------------
		public BaseMultiUnicode(): base()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// --------------------------------------------------------------------------------
		public BaseMultiUnicode(IWritingSystem ws): base()
		{
			m_LgWritingSystem = ws;
		}

		#region Methods derived class has to override
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of writing systems that implement this property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract int GetCount
		{
			get;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing systems that implement this property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract void GetWs(int ws, ArrayPtr ptr);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string in the particular writing system
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected abstract string GetString(int ws);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the string in the particular writing system
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="text"></param>
		/// ------------------------------------------------------------------------------------
		protected abstract void SetString(int ws, string text);
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a string in the given writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override StringWithWs this[int ihvo]
		{
			get
			{
				int ws = WritingSystems[ihvo];
				return new StringWithWs(GetString(ws),
					m_LgWritingSystem.WritingSystemFactory.GetStrFromWs(ws));
			}
			set
			{
				// This is tricky in that it ignores the index parameter.  Which name is set
				// depends only on value.icuLocale.
				int ws = GetWsFromStr(value.icuLocale,
					m_LgWritingSystem.WritingSystemFactory);
				SetString(ws, value.text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of strings stored
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int Count
		{
			get
			{
				return GetCount;
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system ids that this name is defined in
		/// </summary>
		/// --------------------------------------------------------------------------------
		private int[] WritingSystems
		{
			get
			{
				if (m_ws == null)
				{
					int c = GetCount;
					using (ArrayPtr wsPtr = MarshalEx.ArrayToNative(c, typeof(int)))
					{
						GetWs(c, wsPtr);
						m_ws = (int[])MarshalEx.NativeToArray(wsPtr, c, typeof(int));
					}
				}
				return m_ws;
			}
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for multi string/unicode
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public class BaseMultiString: IEnumerable, ICollection
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for XML serialization - don't use it otherwise
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BaseMultiString()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a string in the given writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual StringWithWs this[int ihvo]
		{
			get { return new StringWithWs(); }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new string with a ws
		/// </summary>
		/// <param name="value"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void Add(StringWithWs value)
		{
			this[0] = value;
		}

		/// <summary>
		/// Translate the ICU locale string into the writing system id for the given
		/// writing system factory, adding it to the factory as needed.
		/// </summary>
		/// <param name="icuLocale"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		static public int GetWsFromStr(string icuLocale, ILgWritingSystemFactory wsf)
		{
			int ws = wsf.GetWsFromStr(icuLocale);
			if (ws == 0)
			{
				// This adds the icuLocale as a new writing system to the factory.
				IWritingSystem lws = wsf.get_Engine(icuLocale);
				ws = lws.WritingSystem;
				lws = null;
			}
			return ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of strings stored
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int Count
		{
			get { return 0; }
		}

		#region ICollection Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsSynchronized
		{
			get
			{
				// TODO:  Add BaseMultiString.IsSynchronized getter implementation
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(Array array, int index)
		{
			// TODO:  Add BaseMultiString.CopyTo implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object SyncRoot
		{
			get
			{
				// TODO:  Add BaseMultiString.SyncRoot getter implementation
				return null;
			}
		}

		#endregion

		#region IEnumerable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumerator
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator()
		{
			return new BaseMultiStringEnumerator(this);
		}

		#endregion

		#region Enumerator
		/// <summary>
		/// Enumerator for BaseMultiString
		/// </summary>
		[ComVisible(false)]
		public class BaseMultiStringEnumerator : IEnumerator
		{

			private IEnumerator baseEnumerator;

			/// <summary>
			/// Initializes a new instance of SideBarButtonEnumerator class
			/// </summary>
			/// <param name="mappings"></param>
			public BaseMultiStringEnumerator(BaseMultiString mappings)
			{
				IEnumerable temp = (IEnumerable)mappings;
				this.baseEnumerator = temp.GetEnumerator();
			}

			/// <summary>
			/// Gets the current element
			/// </summary>
			public StringWithWs Current
			{
				get
				{
					return (StringWithWs)baseEnumerator.Current;
				}
			}

			object IEnumerator.Current
			{
				get
				{
					return baseEnumerator.Current;
				}
			}

			/// <summary>
			/// Moves to the next element in the collection
			/// </summary>
			/// <returns>True if next element exists</returns>
			public bool MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			/// <summary>
			/// Resets the collection
			/// </summary>
			public void Reset()
			{
				baseEnumerator.Reset();
			}
		}
		#endregion // Enumerator
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a PUA character definition
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct CharDef
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="_code"></param>
		/// <param name="_data"></param>
		/// ------------------------------------------------------------------------------------
		public CharDef(int _code, string _data)
		{
			data = _data;
			// PUA character definitions in xml file need to have at least 4 digits
			// or InstallLanguage will fail.
			code = string.Format("{0:x4}", _code).ToUpper();
		}

		/// <summary></summary>
		[XmlAttribute]
		public string code;

		/// <summary></summary>
		[XmlAttribute]
		public string data;
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct FileName
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="_file"></param>
		/// ------------------------------------------------------------------------------------
		public FileName(string _file)
		{
			file = _file;
		}

		/// <summary></summary>
		[XmlAttribute]
		public string file;
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents an encoding converter
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct EncodingConverter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="_install"></param>
		/// <param name="_file"></param>
		/// ------------------------------------------------------------------------------------
		public EncodingConverter(string _install, string _file)
		{
			install = _install;
			file = _file;
		}

		/// <summary></summary>
		[XmlAttribute]
		public string install;

		/// <summary></summary>
		[XmlAttribute]
		public string file;
	}
}
