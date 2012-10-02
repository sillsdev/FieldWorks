using System.Linq;
using SIL.CoreImpl;
using SIL.PaToFdoInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaWritingSystem : IPaWritingSystem
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads all the writing systems from the specified service locator into a
		/// collection of PaWritingSystem objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static string GetWritingSystemsAsXml(IFdoServiceLocator svcloc)
		{
			var wsList = svcloc.WritingSystems.VernacularWritingSystems.Select(ws =>
				new PaWritingSystem(ws, svcloc, true, false)).ToList();

			wsList.AddRange(svcloc.WritingSystems.AnalysisWritingSystems.Select(ws =>
				new PaWritingSystem(ws, svcloc, false, true)));

			// Add the rest of the writing systems that are not vern. or analysis.
			wsList.AddRange(from ws in svcloc.WritingSystems.AllWritingSystems
							where wsList.SingleOrDefault(paws => paws.Id == ws.Id) == null
							select new PaWritingSystem(ws, svcloc, false, false));

			return XmlSerializationHelper.SerializeToString(wsList);
		}

		/// ------------------------------------------------------------------------------------
		public PaWritingSystem()
		{
		}

		/// ------------------------------------------------------------------------------------
		private PaWritingSystem(IWritingSystem lgws, IFdoServiceLocator svcloc, bool isVern,
			bool isAnal)
		{
			Id = lgws.Id;
			DisplayName = lgws.DisplayLabel;
			LanguageName = lgws.LanguageName;
			Abbreviation = lgws.Abbreviation;
			IcuLocale = lgws.IcuLocale;
			Hvo = lgws.Handle;
			DefaultFontName = lgws.DefaultFontName;
			IsVernacular = isVern;
			IsAnalysis = isAnal;
			IsDefaultAnalysis = (lgws == svcloc.WritingSystems.DefaultAnalysisWritingSystem);
			IsDefaultVernacular = (lgws == svcloc.WritingSystems.DefaultVernacularWritingSystem);
			IsDefaultPronunciation = (lgws == svcloc.WritingSystems.DefaultPronunciationWritingSystem);
		}

		#region IPaWritingSystem Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ISO-blah, blah code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Id { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DisplayName { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system language name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LanguageName { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Abbreviation { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ICU locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string IcuLocale { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Hvo { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the writing system is a vernacular writing
		/// system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsVernacular { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the writing system is a analysis writing
		/// system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsAnalysis { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the writing system is the default
		/// analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDefaultAnalysis { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the writing system is the default
		/// pronunciation writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDefaultPronunciation { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the writing system is the default
		/// vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDefaultVernacular { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultFontName { get; set; }

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return DisplayName ?? (Id ?? string.Empty);
		}
	}
}
