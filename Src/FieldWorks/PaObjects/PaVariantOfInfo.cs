// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaVariantOfInfo : IPaVariantOfInfo
	{
		/// <summary />
		internal PaVariantOfInfo(ILexEntryRef lxEntryRef)
		{
			VariantComment = PaMultiString.Create(lxEntryRef.Summary, lxEntryRef.Cache.ServiceLocator);
			VariantType = lxEntryRef.VariantEntryTypesRS.Select(x => PaCmPossibility.Create(x));
		}

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaCmPossibility> VariantType { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString VariantComment { get; }
	}
}
