// Copyright (c) 2015-2018 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// A property class used in the PropertyTable.
	/// </summary>
	[Serializable]
	//TODO: we can't very well change this source code every time someone adds a new value type!!!
	[XmlInclude(typeof(System.Drawing.Point))]
	[XmlInclude(typeof(System.Drawing.Size))]
	[XmlInclude(typeof(FormWindowState))]
	public class Property
	{
		/// <summary>
		/// Name of the property.
		/// </summary>
		public string name;
		/// <summary>
		/// Value of the property.
		/// </summary>
		public object value;

		/// <summary>
		/// it is not clear yet what to do about default persistence;
		/// normally we would want to say false, but we don't you have
		/// a good way to indicate that the property should be saved except for beer code.
		/// therefore, for now, the default will be true said that properties which are introduced
		/// in the configuration file will still be persisted.
		/// </summary>
		[XmlIgnore]
		public bool doPersist = true;

		/// <summary>
		/// Up until now there was no way to pass ownership of the object/property
		/// to the property table so that the objects would be disposed of at the
		/// time the property table goes away.
		/// </summary>
		[XmlIgnore]
		public bool doDispose = false;

		/// <summary>
		/// required for XML serialization
		/// </summary>
		public Property()
		{
		}

		/// <summary>
		/// Normally used constructor.
		/// </summary>
		public Property(string name, object value)
		{
			this.name = name;
			this.value = value;
		}

		/// <summary>
		/// Override to make sensible string representation of a property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (value == null)
			{
				return name + "= null";
			}

			return name + "= " + value;
		}
	}
}
