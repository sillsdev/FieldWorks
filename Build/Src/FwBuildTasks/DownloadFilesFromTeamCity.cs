// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Build.Framework;

namespace FwBuildTasks
{
	/// <summary>
	///	<DownloadFilesFromTeamCity
	///   Address="http://build.palaso.org/"
	///   FlexBridgeBuildType="$(FLExBridgeBuildType)"
	///   ProjectId="Chorus"
	///   VersionInfo="$(MasterVersionInfo)"
	///   BuildType="bt2"
	///   Tag=".lastSuccessful"
	///   Query="?branch=%3Cdefault%3E"
	///   DownloadsDir="$(fwrt)/Downloads"
	///   Artifacts="@(ChorusFiles)"/>
	/// </summary>
	public class DownloadFilesFromTeamCity : DownloadFile
	{
		private const string ArtifactsUrlPart = "guestAuth/repository/download/";
		private const string BuildTypeUrlPart = "guestAuth/app/rest/10.0/buildTypes/id:{0}/";
		private const string BuildTagsUrlPart = "builds?locator=pinned:true&fields=build(tags(tag))";
		private const string DefaultTag = ".lastSuccessful";
		private const string TagSuffix = ".tcbuildtag";
		private const string QueryFormat = "?branch={0}";

		/// <summary>
		/// TeamCity BuildType that contains the Artifacts.
		/// Used with a specified Tag or if it is not possible to determine the correct BuildType from FlexBridgeBuildType and ProjectId alone.
		/// </summary>
		public string BuildType { get; set; }

		/// <summary>Build Tag. Default is '.lastSuccessful'.</summary>
		public string Tag { get; set; }

		/// <summary>URL Query (e.g. ?branch=%3Cdefault%3E). Used only if FlexBridgeBuildType has no matching dependency.</summary>
		public string Query { get; set; }

		/// <summary>
		/// The TeamCity "Build Type" of the FLExBridge build associated with this FLEx build.
		/// Its dependencies are used to determine relevant Build Types etc. from which to download Artifacts.
		/// Used only if BuildType and Tag are not both specified.
		/// </summary>
		public string FlexBridgeBuildType { get; set; }

		/// <summary>The Artifacts' ProjectID on TeamCity. Used to determine which of the FLExBridge build's dependencies to use.</summary>
		public string ProjectId { get; set; }

		/// <summary>Path to FLEx's MasterVersionInfo.txt. Used to guess a build tag if unavailable through the FLExBridge BT configuration.</summary>
		public string VersionInfo { get; set; }

		/// <summary>(Semicolon-delimited) list of artifacts to download</summary>
		[Required]
		public string[] Artifacts { get; set; }

		public override bool Execute()
		{
			if (!string.IsNullOrEmpty(Tag))
			{
				if (!string.IsNullOrEmpty(BuildType))
					return DownloadAllFiles();
				Log.LogError("Cannot use a Tag without a BuildType");
				return false;
			}

			switch (QueryTeamCity())
			{
				case TeamCityQueryResult.Found:
					return DownloadAllFiles();
				case TeamCityQueryResult.Failed:
					return false;
				case TeamCityQueryResult.NoResult:
					if (string.IsNullOrEmpty(BuildType))
					{
						Log.LogError("Insufficient information to identify a build:{0}FlexBridgeBuildType: {1}{0}ProjectId: {2}{0}BuildType: {3}"
							+ "{0}Please specify a BuildType or verify that FBBT {1} has a dependency on Project {2}",
							Environment.NewLine, FlexBridgeBuildType, ProjectId, BuildType);
						return false;
					}
					return DownloadAllFiles();
				default:
					Log.LogError("Unknown TeamCity Query Result. This is not necessarily bad, but this FwBuildTask doesn't know that.");
					return false;
			}
		}

		protected bool DownloadAllFiles()
		{
			var addressBase = CombineUrl(Address, ArtifactsUrlPart, BuildType, Tag);
			Log.LogMessage("Downloading artifacts from {0}{1}", addressBase, Query == null ? null : string.Format(" with Query {0}", Query));
			// Return success iff all files download successfully
			return Artifacts.Aggregate(true, (successSoFar, file) => successSoFar
				&& ProcessDownloadFile(CombineUrl(addressBase, file) + Query, Path.Combine(DownloadsDir, file)));
		}

		protected TeamCityQueryResult QueryTeamCity()
		{
			if (!string.IsNullOrEmpty(FlexBridgeBuildType))
			{
				if (string.IsNullOrEmpty(ProjectId) && string.IsNullOrEmpty(BuildType))
				{
					Log.LogError("FlexBridgeBuildType given, but no dependency ProjectId or build type."
						+ " What am I supposed to do; download all dependencies for this FlexBridgeBuildType?");
					return TeamCityQueryResult.Failed;
				}

				// Read dependencies from the FlexBridgeBuildType in TeamCity
				var relevantDeps = GetDependenciesFromFlexBridgeBuildType();
				if (relevantDeps == null)
				{
					Log.LogError("Unable to retrieve dependencies for BuildType {0}. Check your connection and whether the BuildType exists",
						FlexBridgeBuildType);
					return TeamCityQueryResult.Failed;
				}
				switch (relevantDeps.Length)
				{
					case 0:
						Log.LogMessage("FlexBridgeBuildType {0} has no dependencies on {1}.", FlexBridgeBuildType, ProjectId ?? BuildType);
						break;
					case 1:
						BuildType = relevantDeps[0].BuildTypeId;
						Tag = relevantDeps[0].RevisionValue;
						Query = string.IsNullOrEmpty(relevantDeps[0].RevisionBranch) ? null : string.Format(QueryFormat, relevantDeps[0].RevisionBranch);
						// REVIEW pH 2016.10: would it be a good idea to read the files list here? Override specified list, subordinate to, or Union?
						Log.LogMessage("Found matching dependency in FLExBridge: BuildType {0}, Tag {1}{2}",
							relevantDeps[0].BuildTypeName, Tag, Query == null ? null : string.Format(", Branch: {0}", relevantDeps[0].RevisionBranch));
						return TeamCityQueryResult.Found;
					default:
						// There is more than one artifact dependency on the specified ProjectId.
						// Select one(s) that matches the specified buildType, if any.
						var depsOfHardcodedBt = relevantDeps.Where(tcd => tcd.BuildTypeId == BuildType).ToArray();
						if (depsOfHardcodedBt.Any())
							relevantDeps = depsOfHardcodedBt;
						if (relevantDeps.Length == 1)
							goto case 1; // we found a unique dependency; use it
						// Filter out, e.g., Chorus-Documentation
						relevantDeps = relevantDeps.Where(tcd => tcd.BuildTypeName == null || !tcd.BuildTypeName.Contains("Documentation")).ToArray();
						if (relevantDeps.Length == 1)
							goto case 1; // we found a unique dependency; use it
						// If we still have multiple dependencies, give up. (TeamCity should never depend on two different tags of the same build)
						Log.LogError("FLExBridge :: {0} has multiple possibly-relevant dependencies on {1}: {2}."
							+ " Try making this FwBuildTask smarter or specifying a BuildType.",
							FlexBridgeBuildType, ProjectId ?? BuildType, string.Join(", ", relevantDeps));
						return TeamCityQueryResult.Failed;
				}
			}

			if (string.IsNullOrEmpty(BuildType))
			{
				if(string.IsNullOrEmpty(FlexBridgeBuildType))
					Log.LogError("Insufficient information to identify a build."
						+ " Please specify a Build Type or a FLExBridge Build Type and a Project ID");
				else
					Log.LogError("Insufficient information to identify a build."
						+ " FLExBridge Build Type {0} has no dependencies on {1}, and no Build Type was specified.",
						FlexBridgeBuildType, ProjectId);
				return TeamCityQueryResult.Failed;
			}

			// Didn't find a matching dependency in FLExBridge; check for the most-specific version-tagged build, if any (e.g. fw-8.2.8~beta2~nightly)
			var availableTags = GetTagsFromBuildType();
			if (availableTags == null)
			{
				Log.LogError("Unable to retrieve dependencies for BuildType {0}. Check your connection and whether the BuildType exists", BuildType);
				return TeamCityQueryResult.Failed;
			}
			if (availableTags.Any())
			{
				Dictionary<string, string> versionParts;
				if (Substitute.ParseSymbolFile(VersionInfo, Log, out versionParts))
				{
					var tempTag = string.Format("fw-{0}.{1}.{2}~{3}",
						versionParts["FWMAJOR"], versionParts["FWMINOR"], versionParts["FWREVISION"], versionParts["FWBETAVERSION"]);
					tempTag = tempTag.Replace(" ", "").ToLowerInvariant(); // TC tags are spaceless and lowercase
					var versionDelims = new[] {'.', '~'};
					var idxDelim = tempTag.LastIndexOfAny(versionDelims);
					while (idxDelim > 0 && !availableTags.Contains(tempTag))
					{
						tempTag = tempTag.Remove(idxDelim);
						idxDelim = tempTag.LastIndexOfAny(versionDelims);
					}
					if (availableTags.Contains(tempTag))
					{
						Tag = tempTag + TagSuffix;
						Log.LogMessage("Found matching tag for BuildType {0}: {1}", BuildType, Tag);
						if (!string.IsNullOrEmpty(Query))
						{
							Log.LogWarning("Guessing Tags doesn't check queries. Guessed tag '{0}' for BuildType {1}, but it may not match {2}",
								Tag, BuildType, Query);
						}
						return TeamCityQueryResult.Found;
					}
				}
			}

			// REVIEW (Hasso) 2016.10: using .lastSuccessful should be a WARNING on package builds (may lead to bit rot)
			// If all else fails, use the default "tag" .lastSuccessful
			Tag = DefaultTag;
			return TeamCityQueryResult.NoResult;
		}

		/// <returns>an array of FlexBridgeBuildType's TcDependencies, filtered by ProjectId (or BuildType); null on any error</returns>
		protected TcDependency[] GetDependenciesFromFlexBridgeBuildType()
		{
			string fbbtXml;
			if (!MakeWebRequest(string.Format(CombineUrl(Address, BuildTypeUrlPart), FlexBridgeBuildType), out fbbtXml))
				return null;
			var fbbtXDocRoot = XDocument.Load(new StringReader(fbbtXml)).Root;
			if (fbbtXDocRoot == null)
				return null;
			var dependenciesElt = fbbtXDocRoot.Element("artifact-dependencies");
			if (dependenciesElt == null)
				return null;
			return string.IsNullOrEmpty(ProjectId)
				? dependenciesElt.Elements("artifact-dependency").Where(XEltMatchesBuildType).Select(TcDependencyFromXml).Distinct().ToArray()
				: dependenciesElt.Elements("artifact-dependency").Where(XEltMatchesProjectId).Select(TcDependencyFromXml).Distinct().ToArray();
		}

		protected bool XEltMatchesProjectId(XElement artifactDependency)
		{
			var typeElt = artifactDependency.Element("source-buildType");
			if (typeElt == null)
				return false;
			var xAtt = typeElt.Attribute("projectId");
			return xAtt != null && xAtt.Value == ProjectId;
		}

		protected bool XEltMatchesBuildType(XElement artifactDependency)
		{
			var typeElt = artifactDependency.Element("source-buildType");
			if (typeElt == null)
				return false;
			var xAtt = typeElt.Attribute("id"); // the BuildType ID
			return xAtt != null && xAtt.Value == BuildType;
		}

		protected TcDependency TcDependencyFromXml(XElement artifactDependency)
		{
			var tcd = new TcDependency();
			var typeElt = artifactDependency.Element("source-buildType");
			if (typeElt != null)
			{
				var xAtt = typeElt.Attribute("id");
				if (xAtt != null)
					tcd.BuildTypeId = xAtt.Value;
				xAtt = typeElt.Attribute("name");
				if (xAtt != null)
					tcd.BuildTypeName = xAtt.Value;
			}
			var propsElt = artifactDependency.Element("properties");
			if (propsElt == null)
				return tcd;
			foreach (var propElt in propsElt.Elements())
			{
				var nameAtt = propElt.Attribute("name");
				var valueAtt = propElt.Attribute("value");
				if (nameAtt == null || valueAtt == null)
					continue;
				switch (nameAtt.Value)
				{
					case "revisionValue":
						tcd.RevisionValue = valueAtt.Value;
						break;
					case "revisionBranch":
						tcd.RevisionBranch = valueAtt.Value;
						break;
				}
			}
			// strip leading "latest" from, e.g., latest.LastSuccessful or latest.lastPinned
			if (tcd.RevisionValue.StartsWith("latest."))
				tcd.RevisionValue = tcd.RevisionValue.Substring("latest".Length);
			return tcd;
		}

		/// <returns>an array of tags on BuildType's pinned builds; null on any error</returns>
		protected string[] GetTagsFromBuildType()
		{
			string bXml;
			if (!MakeWebRequest(string.Format(CombineUrl(Address, BuildTypeUrlPart, BuildTagsUrlPart), BuildType), out bXml))
				return null;
			var buildsElt = XDocument.Load(new StringReader(bXml)).Element("builds");
			return buildsElt == null ? null : buildsElt.Elements("build").SelectMany(GetTagsFromBuildElt).ToArray();
		}

		protected IEnumerable<string> GetTagsFromBuildElt(XElement buildElt)
		{
			var tagsElt = buildElt.Element("tags");
			if (tagsElt == null)
				return new string[0];
			return from tagElt in tagsElt.Elements("tag") select tagElt.Attribute("name") into nameAtt where nameAtt != null select nameAtt.Value;
		}

		public bool MakeWebRequest(string url, out string response)
		{
			response = null;
			for (var retries = Retries; retries >= 0; --retries)
			{
				// Assign values to these objects here so that they can be referenced in the finally block
				HttpWebResponse webResponse = null;
				Stream remoteStream = null;
				Stream errorResponseStream = null;
				try
				{
					// Create a request for the specified remote file name
					var request = WebRequest.Create(url);
					// If a username or password have been given, use them
					if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
						request.Credentials = new NetworkCredential(Username, Password);

					// Prevent caching of requests so that we always download latest
					request.Headers[HttpRequestHeader.CacheControl] = "no-cache";

					// Send the request to the server and retrieve the WebResponse object
					webResponse = (HttpWebResponse) request.GetResponse();
					remoteStream = webResponse.GetResponseStream();
					if (webResponse.StatusCode != HttpStatusCode.OK || remoteStream == null)
					{
						if (webResponse.StatusCode == HttpStatusCode.OK)
							Log.LogWarning("No data in response to request {0}", url);
						else
							Log.LogWarning("Unexpected Server Response[{0}] to request {1}", webResponse.StatusCode, url);
						if (retries > 0)
						{
							Log.LogMessage(MessageImportance.High, "Could not retrieve {0}. Trying {1} more times.", url, retries);
							Thread.Sleep(RetryWaitTime); // wait a minute
						}
						continue;
					}
					// Once the WebResponse object has been retrieved, get the stream object associated with the response's data
					using (var localStream = new StreamReader(remoteStream))
						response = localStream.ReadToEnd();
					return true;
				}
				catch (WebException e)
				{
					if (e.Response != null && (errorResponseStream = e.Response.GetResponseStream()) != null)
					{
						string html;
						using (var sr = new StreamReader(errorResponseStream))
							html = sr.ReadToEnd();
						Log.LogWarning("Unexpected response from {0}. Server responds {1}", url, html);
					}
					else
					{
						Log.LogWarning("No response from {0}. Exception {1}. Status {2}.", url, e.Message, e.Status);
					}
					if (retries > 0)
					{
						Log.LogMessage(MessageImportance.High, "Could not retrieve {0}. Trying {1} more times.", url, retries);
						Thread.Sleep(RetryWaitTime); // wait a minute
					}
				}
				finally
				{
					// Close the response and streams objects here to make sure they're closed even if an exception is thrown at some point
					if (webResponse != null) webResponse.Close();
					if (remoteStream != null) remoteStream.Close();
					if (errorResponseStream != null) errorResponseStream.Close();
				}
			}
			return false;
		}

		public static string CombineUrl(params string[] args)
		{
			return Path.Combine(args).Replace('\\', '/');
		}

		public enum TeamCityQueryResult
		{
			Found,
			Failed,
			NoResult
		}

		public struct TcDependency
		{
			public string BuildTypeId;
			public string BuildTypeName;
			public string RevisionValue;
			public string RevisionBranch;

			public override string ToString()
			{
				return string.Format("{0}/{1}{2}", BuildTypeId, RevisionValue,
					string.IsNullOrEmpty(RevisionBranch) ? null : string.Format(QueryFormat, RevisionBranch));
			}
		}
	}
}
