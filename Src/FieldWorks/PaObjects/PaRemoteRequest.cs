// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public sealed class PaRemoteRequest : RemoteRequest
	{
		/// <summary>
		/// Determines whether [is same project] [the specified name].
		/// </summary>
		public bool ShouldWait(string name, string server)
		{
			var matchStatus = FieldWorks.GetProjectMatchStatus(new ProjectId(name));
			return matchStatus == ProjectMatch.DontKnowYet
				   || matchStatus == ProjectMatch.WaitingForUserOrOtherFw
				   || matchStatus == ProjectMatch.SingleProcessMode;
		}

		/// <summary />
		public bool IsMyProject(string name, string server) => FieldWorks.GetProjectMatchStatus(new ProjectId(name)) == ProjectMatch.ItsMyProject;

		/// <summary />
		public string GetWritingSystems() => PaWritingSystem.GetWritingSystemsAsXml(FieldWorks.Cache.ServiceLocator);

		/// <summary />
		public string GetLexEntries() => PaLexEntry.GetAllAsXml(FieldWorks.Cache.ServiceLocator);

		/// <summary />
		public void ExitProcess()
		{
			Application.Exit();
		}

		/// <summary />
		private sealed class PaWritingSystem : IPaWritingSystem
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
					if (wsList.Any(w => w.Id == ws.Id))
					{
						continue;
					}
					var isVern = svcloc.WritingSystems.VernacularWritingSystems.Contains(ws);
					var isAnal = svcloc.WritingSystems.AnalysisWritingSystems.Contains(ws);
					wsList.Add(new PaWritingSystem(ws, svcloc, isVern, isAnal));
				}
				return XmlSerializationHelper.SerializeToString(wsList);
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
			public string Id { get; }

			/// <inheritdoc />
			public string DisplayName { get; }

			/// <inheritdoc />
			public string LanguageName { get; }

			/// <inheritdoc />
			public string Abbreviation { get; }

			/// <inheritdoc />
			public string IcuLocale { get; }

			/// <inheritdoc />
			public int Hvo { get; }

			/// <inheritdoc />
			public bool IsVernacular { get; }

			/// <inheritdoc />
			public bool IsAnalysis { get; }

			/// <inheritdoc />
			public bool IsDefaultAnalysis { get; }

			/// <inheritdoc />
			public bool IsDefaultPronunciation { get; }

			/// <inheritdoc />
			public bool IsDefaultVernacular { get; }

			/// <inheritdoc />
			public string DefaultFontName { get; }

			#endregion

			/// <inheritdoc />
			public override string ToString()
			{
				return DisplayName ?? Id ?? string.Empty;
			}
		}
	}
}
