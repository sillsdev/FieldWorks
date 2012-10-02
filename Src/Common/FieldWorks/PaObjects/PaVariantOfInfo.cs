using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaVariantOfInfo : IPaVariantOfInfo
	{
		/// ------------------------------------------------------------------------------------
		public PaVariantOfInfo()
		{
		}

		/// ------------------------------------------------------------------------------------
		internal PaVariantOfInfo(ILexEntryRef lxEntryRef)
		{
			xVariantComment = PaMultiString.Create(lxEntryRef.Summary, lxEntryRef.Cache.ServiceLocator);
			xVariantType = lxEntryRef.VariantEntryTypesRS.Select(x => PaCmPossibility.Create(x)).ToList();
		}

		/// ------------------------------------------------------------------------------------
		public List<PaCmPossibility> xVariantType { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaCmPossibility> VariantType
		{
			get { return xVariantType.Cast<IPaCmPossibility>(); }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xVariantComment { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString VariantComment
		{
			get { return xVariantComment; }
		}
	}
}
