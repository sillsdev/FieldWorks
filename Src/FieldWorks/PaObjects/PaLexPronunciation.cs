// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaLexPronunciation : IPaLexPronunciation
	{
		/// <summary />
		public PaLexPronunciation()
		{
		}

		/// <summary />
		internal PaLexPronunciation(ILexPronunciation lxPro)
		{
			Form = PaMultiString.Create(lxPro.Form, lxPro.Cache.ServiceLocator);
			Location = PaCmPossibility.Create(lxPro.LocationRA);
			CVPattern = lxPro.CVPattern.Text;
			Tone = lxPro.Tone.Text;
			Guid = lxPro.Guid;
			MediaFiles = lxPro.MediaFilesOS.Where(x => x?.MediaFileRA != null).Select(x => new PaMediaFile(x));
		}

		/// <inheritdoc />
		public string CVPattern { get; set; }

		/// <inheritdoc />
		public string Tone { get; set; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Form { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaMediaFile> MediaFiles { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaCmPossibility Location { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public Guid Guid { get; }
	}
}