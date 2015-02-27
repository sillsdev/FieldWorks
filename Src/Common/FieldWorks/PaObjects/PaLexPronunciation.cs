using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using SIL.PaToFdoInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaLexPronunciation : IPaLexPronunciation
	{
		/// ------------------------------------------------------------------------------------
		public PaLexPronunciation()
		{
		}

		/// ------------------------------------------------------------------------------------
		internal PaLexPronunciation(ILexPronunciation lxPro)
		{
			xForm = PaMultiString.Create(lxPro.Form, lxPro.Cache.ServiceLocator);
			xLocation = PaCmPossibility.Create(lxPro.LocationRA);
			CVPattern = lxPro.CVPattern.Text;
			Tone = lxPro.Tone.Text;
			xGuid = lxPro.Guid;

			xMediaFiles = (from x in lxPro.MediaFilesOS
						   where x != null && x.MediaFileRA != null
						   select new PaMediaFile(x)).ToList();
		}

		/// ------------------------------------------------------------------------------------
		public string CVPattern { get; set; }

		/// ------------------------------------------------------------------------------------
		public string Tone { get; set; }

		/// ------------------------------------------------------------------------------------
		public PaMultiString xForm { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Form
		{
			get { return xForm; }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaMediaFile> xMediaFiles { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaMediaFile> MediaFiles
		{
			get { return xMediaFiles.Select(x => (IPaMediaFile)x); }
		}

		/// ------------------------------------------------------------------------------------
		public PaCmPossibility xLocation { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaCmPossibility Location
		{
			get { return xLocation; }
		}

		/// ------------------------------------------------------------------------------------
		public Guid xGuid { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public Guid Guid
		{
			get { return xGuid; }
		}
	}
}
