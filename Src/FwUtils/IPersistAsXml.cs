// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwUtils
{
	public interface IPersistAsXml
	{
		/// <summary>
		/// Add to the specified XML element information required to create a new
		/// object equivalent to yourself. The element already contains information
		/// sufficient to create an instance of the proper class.
		/// </summary>
		void PersistAsXml(XElement element);

		/// <summary>
		/// Initialize an instance into the state indicated by the element, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		void InitXml(XElement element);
	}
}