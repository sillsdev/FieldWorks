using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaCmPossibility : IPaCmPossibility
	{
		/// ------------------------------------------------------------------------------------
		public PaCmPossibility()
		{
		}

		/// ------------------------------------------------------------------------------------
		internal static PaCmPossibility Create(ICmPossibility poss)
		{
			return (poss == null ? null : new PaCmPossibility(poss));
		}

		/// ------------------------------------------------------------------------------------
		private PaCmPossibility(ICmPossibility poss)
		{
			var svcloc = poss.Cache.ServiceLocator;
			xAbbreviation = PaMultiString.Create(poss.Abbreviation, svcloc);
			xName = PaMultiString.Create(poss.Name, svcloc);
		}

		#region IPaCmPossibility Members
		/// ------------------------------------------------------------------------------------
		public PaMultiString xAbbreviation { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Abbreviation
		{
			get { return xAbbreviation; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xName { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Name
		{
			get { return xName; }
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return string.Format("{0} ({1})", Name, Abbreviation);
		}
	}
}
