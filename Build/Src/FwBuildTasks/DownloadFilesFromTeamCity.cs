// Copyright (c) 2016-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// Downloads artifacts from TeamCity for the given BuildType. Select in the following order:
	///  - If Tag or Query is specified, use them
	///  - If VersionInfo is specified, look for a matching tag (fw-9.0.0 before fw-9.0)
	///  - Otherwise, download .lastSuccessful
	///
	/// Usage:
	///	<DownloadFilesFromTeamCity
	///   Address="http://build.palaso.org/"
	///   BuildType="bt2"
	///   Tag=".lastSuccessful"
	///   Query="?branch=%3Cdefault%3E"
	///   VersionInfo="$(fwrt)/Src/MasterVersionInfo.txt"
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
		[Required]
		public string BuildType { get; set; }

		/// <summary>Build Tag. Default is '.lastSuccessful'.</summary>
		public string Tag { get; set; }

		/// <summary>URL Query (e.g. ?branch=%3Cdefault%3E). Used only if FlexBridgeBuildType has no matching dependency.</summary>
		public string Query { get; set; }

		/// <summary>Path to FLEx's MasterVersionInfo.txt. Used to guess a build tag if unavailable through the FLExBridge BT configuration.</summary>
		public string VersionInfo { get; set; }

		/// <summary>(Semicolon-delimited) list of artifacts to download</summary>
		[Required]
		public string[] Artifacts { get; set; }

		public override bool Execute()
		{
			// If the user specified a tag or query, it overrides whatever we might find by querying TeamCity
			if (!string.IsNullOrEmpty(Tag) || !string.IsNullOrEmpty(Query))
			{
				if (string.IsNullOrEmpty(Tag))
					Tag = DefaultTag;
				if (!string.IsNullOrEmpty(BuildType))
					return DownloadAllFiles();
				Log.LogError("Cannot use a Tag or Query without a BuildType");
				return false;
			}

			switch (QueryTeamCity())
			{
				case TeamCityQueryResult.Found:
				case TeamCityQueryResult.FellThrough:
					return DownloadAllFiles();
				case TeamCityQueryResult.Failed:
					return false;
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
				if (!string.IsNullOrEmpty(VersionInfo) && Substitute.ParseSymbolFile(VersionInfo, Log, out versionParts))
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
			return TeamCityQueryResult.FellThrough;
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
							Log.LogMessage(MessageImportance.High, "Could not retrieve {0}. Trying {1} more times in {2}-minute intervals.",
								url, retries, RetryWaitTime / MillisPerMinute);
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
						return false; // The server is available, but it is likely the requested resource does not exist; don't keep trying
					}
					else
					{
						// Possibly a DNS error or some network outage between us and the server.
						Log.LogWarning("No response from {0}. Exception {1}. Status {2}.", url, e.Message, e.Status);
					}
					if (retries > 0)
					{
						Log.LogMessage(MessageImportance.High, "Could not retrieve {0}. Trying {1} more times in {2}-minute intervals.",
							url, retries, RetryWaitTime / MillisPerMinute);
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
			FellThrough
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
