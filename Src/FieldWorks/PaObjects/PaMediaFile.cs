// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml.Serialization;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaMediaFile : IPaMediaFile
	{
		/// <summary />
		internal PaMediaFile(ICmMedia mediaFile)
		{
			OriginalPath = mediaFile.MediaFileRA.OriginalPath;
			AbsoluteInternalPath = mediaFile.MediaFileRA.AbsoluteInternalPath;
			InternalPath = mediaFile.MediaFileRA.InternalPath;
			Label = PaMultiString.Create(mediaFile.Label, mediaFile.Cache.ServiceLocator);
		}

		#region IPaMediaFile Members

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString Label { get; }

		/// <inheritdoc />
		public string AbsoluteInternalPath { get; set; }

		/// <inheritdoc />
		public string InternalPath { get; set; }

		/// <inheritdoc />
		public string OriginalPath { get; set; }

		#endregion

		/// <inheritdoc />
		public override string ToString()
		{
			return Path.GetFileName(AbsoluteInternalPath);
		}
	}
}
