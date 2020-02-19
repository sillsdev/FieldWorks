// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Serialization;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaVariant : IPaVariant
	{
		private readonly PaVariantOfInfo _variantInfo;

		/// <summary />
		internal PaVariant(ILexEntryRef lxEntryRef)
		{
			var lx = lxEntryRef.OwnerOfClass<ILexEntry>();
			VariantForm = PaMultiString.Create(lx.LexemeFormOA.Form, lxEntryRef.Cache.ServiceLocator);
			_variantInfo = new PaVariantOfInfo(lxEntryRef);
		}

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString VariantForm { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaCmPossibility> VariantType => _variantInfo.VariantType;

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString VariantComment => _variantInfo.VariantComment;
	}
}