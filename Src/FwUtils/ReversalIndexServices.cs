// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Utility services relating to Reversal Indexes
	/// </summary>
	public static class ReversalIndexServices
	{
		internal const string ConfigFileExtension = ".fwdictconfig";
		internal const string RevIndexDir = "ReversalIndex";
		internal const string ConfigDir = "ConfigurationSettings";
		internal const string AllIndexesFileName = "AllReversalIndexes";
		internal const string DictConfigElement = "DictionaryConfiguration";
		internal const string WsAttribute = "writingSystem";
		private static bool s_reversalIndicesAreKnownToExist;


		/// <summary>
		/// Create configuration file for analysis writing systems in Reversal Index
		/// </summary>
		public static void CreateOrRemoveReversalIndexConfigurationFiles(WritingSystemManager wsMgr, LcmCache cache, string defaultConfigDir, string projectsDir, string originalProjectName)
		{
			var defaultWsFilePath = Path.Combine(defaultConfigDir, RevIndexDir, AllIndexesFileName + ConfigFileExtension);
			var newWsFilePath = Path.Combine(projectsDir, originalProjectName, ConfigDir, RevIndexDir);
			var analysisWsArray = cache.LangProject.AnalysisWss.Trim().Replace("  ", " ").Split(' ');
			if (Directory.Exists(newWsFilePath))
			{
				//Delete old Configuration Files for WS's that are no longer part of this project
				var files = Directory.GetFiles(newWsFilePath, "*" + ConfigFileExtension, SearchOption.AllDirectories);
				foreach (var file in files)
				{
					XAttribute wsAtt;
					GetWsAttributeFromFile(file, out wsAtt);
					if (wsAtt != null && !string.IsNullOrEmpty(wsAtt.Value) && !analysisWsArray.Contains(wsAtt.Value))
					{
						File.Delete(file);
					}
				}
			}
			else
			{
				// Ensure the directory exists
				Directory.CreateDirectory(newWsFilePath);
			}
			//Create new Configuration File
			foreach (var curWs in analysisWsArray)
			{
				if (curWs.ToLower().Contains("audio"))
				{
					continue;
				}
				var curWsLabel = wsMgr.Get(curWs).DisplayLabel;
				var newWsCompleteFilePath = Path.Combine(newWsFilePath, curWs + ConfigFileExtension);
				if (File.Exists(newWsCompleteFilePath))
				{
					XAttribute wsAtt;
					var configDoc = GetWsAttributeFromFile(newWsCompleteFilePath, out wsAtt);
					if (wsAtt == null)
					{
						// How did we get here??? Only AllReversalIndexes should have no WS! Best I can figure, this is a pre-wsAtt config
						var config = configDoc.Element(DictConfigElement);
						if (config == null || !config.HasAttributes)
						{
							File.Delete(newWsCompleteFilePath); // the file is corrupt; delete it and start over
						}
						else
						{
							config.SetAttributeValue(WsAttribute, curWs);
							configDoc.Save(newWsCompleteFilePath);
							continue;
						}
					}
					else if (wsAtt.Value != curWs)
					{
						// REVIEW (Hasso) 2016.09: what to do? Rename the conflicting file, or re-WS the config? Can't ask, b/c LCM has no UI
						// If the user has duplicated some other Reversal Index Config and given it this name, it is possible they were trying
						// to configure the RI for this WS. Update the Config to point to this WS
						wsAtt.Value = curWs;
						configDoc.Save(newWsCompleteFilePath);
						continue;
					}
					else
					{
						continue; // the file was already in the correct state; nothing to do
					}
				}

				File.Copy(defaultWsFilePath, newWsCompleteFilePath, false);
				File.SetAttributes(newWsCompleteFilePath, FileAttributes.Normal);
				var xmldoc = XDocument.Load(newWsCompleteFilePath);
				var xAttribute = xmldoc.XPathSelectElement(DictConfigElement).Attribute("name");
				xAttribute.Value = curWsLabel;
				xAttribute = xmldoc.XPathSelectElement(DictConfigElement).Attribute(WsAttribute);
				xAttribute.Value = curWs;
				xmldoc.Save(newWsCompleteFilePath);

				var wsObj = wsMgr.Get(curWs);
				if (wsObj != null && wsObj.DisplayLabel.ToLower().IndexOf("audio", StringComparison.Ordinal) == -1)
				{
					UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Undo Adding reversal Guid", "Redo Adding reversal Guid",
						cache.ActionHandlerAccessor, () => cache.ServiceLocator.GetInstance<IReversalIndexRepository>().GetOrCreateWsGuid(wsObj, cache));
				}
			}
		}

		private static XDocument GetWsAttributeFromFile(string filename, out XAttribute wsAtt)
		{
			var doc = XDocument.Load(filename);
			var config = doc.Element(DictConfigElement);
			wsAtt = config == null || !config.HasAttributes ? null : config.Attribute(WsAttribute);
			return doc;
		}

		/// <summary>
		/// Fetches the GUID value of the given property, having checked it is a valid object.
		/// If it is not a valid object, the property is removed.
		/// </summary>
		public static Guid GetObjectGuidIfValid(IPropertyTable propertyTable, string key)
		{
			var sGuid = propertyTable.GetValue<string>(key);
			if (string.IsNullOrEmpty(sGuid))
			{
				return Guid.Empty;
			}
			Guid guid;
			if (!Guid.TryParse(sGuid, out guid))
			{
				return Guid.Empty;
			}
			var cache = propertyTable.GetValue<LcmCache>(FwUtils.cache);
			if (cache.ServiceLocator.ObjectRepository.IsValidObjectId(guid))
			{
				return guid;
			}
			propertyTable.RemoveProperty(key);
			return Guid.Empty;
		}
	}
}