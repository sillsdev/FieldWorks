// Copyright (c) 2016-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

// ReSharper disable once CheckNamespace
namespace FwBuildTasks
{
	/// <summary>
	/// Downloads artifacts from TeamCity for the given BuildType.
	/// If Tag or Query is specified, use them; otherwise, download .lastSuccessful
	///
	/// Usage:
	///	<DownloadFilesFromTeamCity
	///   Address="http://build.palaso.org/"
	///   BuildType="bt2"
	///   Tag=".lastSuccessful"
	///   Query="?branch=%3Cdefault%3E"
	///   DownloadsDir="$(fwrt)/Downloads"
	///   Artifacts="@(ChorusFiles)"/>
	/// </summary>
	public class DownloadFilesFromTeamCity : DownloadFile
	{
		private const string ArtifactsUrlPart = "guestAuth/repository/download/";
		private const string DefaultTag = ".lastSuccessful";

		/// <summary>
		/// TeamCity BuildType that contains the Artifacts.
		/// Used with a specified Tag or if it is not possible to determine the correct BuildType from FlexBridgeBuildType and ProjectId alone.
		/// </summary>
		[Required]
		public string BuildType { get; set; }

		/// <summary>Build Tag. Default is '.lastSuccessful'.</summary>
		public string Tag { get; set; }

		/// <summary>URL Query (e.g. ?branch=%3Cdefault%3E). Used only if FlexBridgeBuildType has no matching dependency.</summary>
		public string Query { get; set; }

		/// <summary>(Semicolon-delimited) list of artifacts to download</summary>
		[Required]
		public string[] Artifacts { get; set; }

		public override bool Execute()
		{
			if (string.IsNullOrEmpty(Tag))
				Tag = DefaultTag;

			var addressBase = CombineUrl(Address, ArtifactsUrlPart, BuildType, Tag);
			Log.LogMessage("Downloading artifacts from {0}{1}", addressBase, Query == null ? null : $" with Query {Query}");
			// Return success iff all files download successfully
			return Artifacts.Aggregate(true, (successSoFar, file) => successSoFar
				&& ProcessDownloadFile(CombineUrl(addressBase, file) + Query, Path.Combine(DownloadsDir, file)));
		}

		public static string CombineUrl(params string[] args)
		{
			return Path.Combine(args).Replace('\\', '/');
		}
	}
}
