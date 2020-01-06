// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaCmPossibility : IPaCmPossibility
	{
		/// <summary />
		internal static PaCmPossibility Create(ICmPossibility poss)
		{
			return (poss == null ? null : new PaCmPossibility(poss));
		}

		/// <summary />
		private PaCmPossibility(ICmPossibility poss)
		{
			var svcloc = poss.Cache.ServiceLocator;
			Abbreviation = PaMultiString.Create(poss.Abbreviation, svcloc);
			Name = PaMultiString.Create(poss.Name, svcloc);
		}

		#region IPaCmPossibility Members

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Abbreviation { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Name { get; }
		#endregion

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{Name} ({Abbreviation})";
		}
	}
}