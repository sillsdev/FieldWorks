// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaVariant : IPaVariant
	{
		/// ------------------------------------------------------------------------------------
		public PaVariant()
		{
		}

		/// ------------------------------------------------------------------------------------
		internal PaVariant(ILexEntryRef lxEntryRef)
		{
			var lx = lxEntryRef.OwnerOfClass<ILexEntry>();
			xVariantForm = PaMultiString.Create(lx.LexemeFormOA.Form, lxEntryRef.Cache.ServiceLocator);
			xVariantInfo = new PaVariantOfInfo(lxEntryRef);
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xVariantForm { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString VariantForm
		{
			get { return xVariantForm; }
		}

		/// ------------------------------------------------------------------------------------
		public PaVariantOfInfo xVariantInfo { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaCmPossibility> VariantType
		{
			get { return xVariantInfo.VariantType; }
		}

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString VariantComment
		{
			get { return xVariantInfo.VariantComment; }
		}
	}
}
