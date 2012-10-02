using System.IO;
using System.Xml.Serialization;
using SIL.PaToFdoInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaMediaFile : IPaMediaFile
	{
		/// ------------------------------------------------------------------------------------
		public PaMediaFile()
		{
		}

		/// ------------------------------------------------------------------------------------
		internal PaMediaFile(ICmMedia mediaFile)
		{
			OriginalPath = mediaFile.MediaFileRA.OriginalPath;
			AbsoluteInternalPath = mediaFile.MediaFileRA.AbsoluteInternalPath;
			InternalPath = mediaFile.MediaFileRA.InternalPath;
			xLabel = PaMultiString.Create(mediaFile.Label, mediaFile.Cache.ServiceLocator);
		}

		#region IPaMediaFile Members
		/// ------------------------------------------------------------------------------------
		public PaMultiString xLabel { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString Label
		{
			get { return xLabel; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the media absolute, internal file path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AbsoluteInternalPath { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the internal media file path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InternalPath { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the original media file path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string OriginalPath { get; set; }

		#endregion

		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Path.GetFileName(AbsoluteInternalPath);
		}
	}
}
