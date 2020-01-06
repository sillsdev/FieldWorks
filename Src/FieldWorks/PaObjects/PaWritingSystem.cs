// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaWritingSystem : IPaWritingSystem
	{
		/// <summary>
		/// Loads all the writing systems from the specified service locator into a
		/// collection of PaWritingSystem objects.
		/// </summary>
		internal static string GetWritingSystemsAsXml(ILcmServiceLocator svcloc)
		{
			var wsList = new List<PaWritingSystem>();

			foreach (var ws in svcloc.WritingSystems.AllWritingSystems)
			{
				if (!wsList.Any(w => w.Id == ws.Id))
				{
					var isVern = svcloc.WritingSystems.VernacularWritingSystems.Contains(ws);
					var isAnal = svcloc.WritingSystems.AnalysisWritingSystems.Contains(ws);
					wsList.Add(new PaWritingSystem(ws, svcloc, isVern, isAnal));
				}
			}

			return XmlSerializationHelper.SerializeToString(wsList);
		}

		/// <summary />
		public PaWritingSystem()
		{
		}

		/// <summary />
		private PaWritingSystem(CoreWritingSystemDefinition lgws, ILcmServiceLocator svcloc, bool isVern, bool isAnal)
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

		/// <inheritdoc />
		public string Id { get; set; }

		/// <inheritdoc />
		public string DisplayName { get; set; }

		/// <inheritdoc />
		public string LanguageName { get; set; }

		/// <inheritdoc />
		public string Abbreviation { get; set; }

		/// <inheritdoc />
		public string IcuLocale { get; set; }

		/// <inheritdoc />
		public int Hvo { get; set; }

		/// <inheritdoc />
		public bool IsVernacular { get; set; }

		/// <inheritdoc />
		public bool IsAnalysis { get; set; }

		/// <inheritdoc />
		public bool IsDefaultAnalysis { get; set; }

		/// <inheritdoc />
		public bool IsDefaultPronunciation { get; set; }

		/// <inheritdoc />
		public bool IsDefaultVernacular { get; set; }

		/// <inheritdoc />
		public string DefaultFontName { get; set; }

		#endregion

		/// <inheritdoc />
		public override string ToString()
		{
			return DisplayName ?? (Id ?? string.Empty);
		}
	}
}
