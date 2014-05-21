// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class groups the static methods used for generating XHTML, according to specified configurations, from Fieldworks model objects
	/// </summary>
	public static class ConfiguredXHTMLGenerator
	{
		private const string PublicIdentifier = @"-//W3C//DTD XHTML 1.1//EN";
		private const string SystemIdentifier = @"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd";

		public static string GenerateEntryHtmlWithStyles(ICmObject entry, DictionaryConfigurationModel configuration, Mediator mediator)
		{
			var projectPath = Path.Combine(DirectoryFinder.GetConfigSettingsDir(entry.Cache.ProjectId.ProjectFolder),
													 DictionaryConfigurationListener.GetDictionaryConfigurationType(mediator));
			var previewCssPath = Path.Combine(projectPath, "Preview.css");
			var stringBuilder = new StringBuilder();
			using(var XHTMLWriter = XmlWriter.Create(stringBuilder))
			using(var CssWriter = new StreamWriter(previewCssPath, false))
			{
				XHTMLWriter.WriteDocType("html", PublicIdentifier, null, null);
				XHTMLWriter.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
				XHTMLWriter.WriteAttributeString("lang", "utf-8");
				XHTMLWriter.WriteStartElement("head");
				XHTMLWriter.WriteStartElement("link");
				XHTMLWriter.WriteAttributeString("href", "file:///" + previewCssPath);
				XHTMLWriter.WriteAttributeString("rel", "stylesheet");
				XHTMLWriter.WriteEndElement();//</link>
				XHTMLWriter.WriteEndElement(); //</head>
				XHTMLWriter.WriteStartElement("body");
				GenerateXHTMLForEntry(entry, configuration.Parts[0], XHTMLWriter, (FdoCache)mediator.PropertyTable.GetValue("cache"));
				XHTMLWriter.WriteEndElement();//</body>
				XHTMLWriter.WriteEndElement();//</html>
				XHTMLWriter.Flush();
				CssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, mediator));
				CssWriter.Flush();
			}

			return stringBuilder.ToString();
		}

		/// <summary>
		/// Generating the xhtml representation for the given ICmObject using the given configuration to select which data to write out
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="configuration"><remarks>this configuration node must match the entry type</remarks></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		public static void GenerateXHTMLForEntry(ICmObject entry, ConfigurableDictionaryNode configuration, XmlWriter writer, FdoCache cache)
		{
			if(writer == null || entry == null || configuration == null || cache == null)
			{
				throw new ArgumentNullException();
			}
			if(String.IsNullOrEmpty(configuration.FieldDescription))
			{
				throw new ArgumentException(@"Invalid configuration: FieldDescription can not be null", @"configuration");
			}
			if(entry.ClassID != cache.MetaDataCacheAccessor.GetClassId(configuration.FieldDescription))
			{
				throw new ArgumentException(@"The given argument doesn't configure this type", @"configuration");
			}
			if(!configuration.IsEnabled)
			{
				throw new ArgumentException(@"You must use an enabled configuration node to get any content.", @"configuration");
			}

			writer.WriteStartElement("div");
			WriteClassNameAttribute(writer, configuration);
			writer.WriteAttributeString("id", "hvo" + entry.Hvo);
			foreach(var config in configuration.Children)
			{
				GenerateXHTMLForFieldByReflection(entry, config, writer, cache);
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// This method will write out the class name attribute into the xhtml for the given configuration node
		/// taking into account the current information in CssGenerator.ClassMappingOverrides
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="configNode">used to look up any mapping overrides</param>
		private static void WriteClassNameAttribute(XmlWriter writer, ConfigurableDictionaryNode configNode)
		{
			writer.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(configNode));
		}

		/// <summary>
		/// This method will use reflection to pull data out of the given object based on the given configuration and
		/// write out appropriate XHTML.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForFieldByReflection(object field, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			if(!config.IsEnabled)
			{
				return;
			}
			var entryType = field.GetType();
			var property = entryType.GetProperty(config.FieldDescription);
			if(property == null)
			{
				Debug.WriteLine("Issue with finding {0}", (object)config.FieldDescription);
				return;
			}
			var propertyValue = property.GetValue(field, new object[] { });
			// If the property value is null there is nothing to generate
			if(propertyValue == null)
			{
				return;
			}
			if(!String.IsNullOrEmpty(config.SubField))
			{
				var subType = propertyValue.GetType();
				var subProp = subType.GetProperty(config.SubField);
				propertyValue = subProp.GetValue(propertyValue, new object[] { });
			}
			if(IsCollectionType(propertyValue.GetType()))
			{
				if(!IsCollectionEmpty(propertyValue))
				{
					GenerateXHTMLForCollection(propertyValue, config, writer, cache);
				}
				return;
			}
			if(propertyValue is IMoForm)
			{
				GenerateXHTMLForMoForm(propertyValue as IMoForm, config, writer, cache);
			}
			else if (propertyValue is ICmObject)
			{
				GenerateXHTMLForICmObject(propertyValue as ICmObject, config, writer, cache);
				return;
			}

			GenerateXHTMLForValue(propertyValue, config, writer, cache);
			if(config.Children != null)
			{
				foreach(var child in config.Children)
				{
					if(child.IsEnabled)
					{
						GenerateXHTMLForFieldByReflection(propertyValue, child, writer, cache);
					}
				}
			}
		}

		private static void GenerateXHTMLForMoForm(IMoForm moForm, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			// Don't export if there is no such data
			if(moForm == null)
				return;
			GenerateXHTMLForStrings(moForm.Form, config, writer, cache);
			if(config.Children != null && config.Children.Any())
			{
				throw new NotImplementedException("Children for MoForm types not yet supported.");
			}
		}

		/// <summary>
		/// This method will generate the XHTML that represents a collection and its contents
		/// </summary>
		/// <param name="collectionField"></param>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForCollection(object collectionField, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			writer.WriteStartElement("span");
			WriteClassNameAttribute(writer, config);
			IEnumerable collection;
			if(collectionField is IEnumerable)
			{
				collection = collectionField as IEnumerable;
			}
			else if(collectionField is IFdoVector)
			{
				collection = (collectionField as IFdoVector).Objects;
			}
			else
			{
				throw new ArgumentException("The given field is not a recognized collection");
			}
			foreach(var item in collection)
			{
				writer.WriteStartElement("span");
				WriteCollectionItemClassAttribute(config, writer);
				if(config.Children != null)
				{
					foreach(var child in config.Children)
					{
						GenerateXHTMLForFieldByReflection(item, child, writer, cache);
					}
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		private static void GenerateXHTMLForICmObject(ICmObject propertyValue, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			// Don't export if there is no such data
			if (propertyValue == null)
				return;
			writer.WriteStartElement("span");
			// Trim trailing "RA" or "OA".
			var fieldDescription = CssGenerator.GetClassAttributeForConfig(config);
			fieldDescription = fieldDescription.Remove(fieldDescription.Length - 2);
			writer.WriteAttributeString("class", fieldDescription);

			if (config.Children != null)
			{
				foreach (var child in config.Children)
				{
					if (child.IsEnabled)
					{
						GenerateXHTMLForFieldByReflection(propertyValue, child, writer, cache);
					}
				}
			}

			writer.WriteEndElement();
		}

		/// <summary>
		///  Write out the class element to use in the span for the individual items in the collection
		/// </summary>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		private static void WriteCollectionItemClassAttribute(ConfigurableDictionaryNode config, XmlWriter writer)
		{
			// chop the pluralization off the parent class
			var collectionName = CssGenerator.GetClassAttributeForConfig(config);
			writer.WriteAttributeString("class", collectionName.Substring(0, collectionName.Length - 1).ToLower());
		}

		/// <summary>
		/// This method is used to determine if we need to iterate through a property and generate xhtml for each item
		/// </summary>
		/// <param name="entryType"></param>
		/// <returns></returns>
		private static bool IsCollectionType(Type entryType)
		{
			//Some of our string types smell like collections but don't really act like them, so we handle them seperately
			return !typeof(IMultiStringAccessor).IsAssignableFrom(entryType) &&
				(typeof(IEnumerable).IsAssignableFrom(entryType) || typeof(IFdoVector).IsAssignableFrom(entryType));
		}

		/// <summary>
		/// Returns true if the given collection is empty (type determined at runtime)
		/// </summary>
		/// <param name="collection"></param>
		/// <exception cref="ArgumentException">if the object given is null, or not a handled collection</exception>
		/// <returns></returns>
		private static bool IsCollectionEmpty(object collection)
		{
			if(collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if(collection is IEnumerable)
			{
				return !(((IEnumerable)collection).Cast<object>().Any());
			}
			if(collection is IFdoVector)
			{
				return ((IFdoVector)collection).ToHvoArray().Length == 0;
			}
			throw new ArgumentException(@"Can not test something that isn't a collection", "collection");
		}

		/// <summary>
		/// This method generates XHTML content for a given object
		/// </summary>
		/// <param name="propertyValue"></param>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForValue(object propertyValue, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			if(propertyValue is ITsString)
			{
				writer.WriteStartElement("span");
				WriteClassNameAttribute(writer, config);
				GenerateXHTMLForString((ITsString)propertyValue, config, writer, cache);
				writer.WriteEndElement();
			}
			else if(propertyValue is IMultiStringAccessor)
			{
				GenerateXHTMLForStrings((IMultiStringAccessor)propertyValue, config,
											  writer, cache);
			}
			else if(propertyValue is int)
			{
				WriteElementContents(propertyValue, config, writer);
			}
			else if(propertyValue is DateTime)
			{
				WriteElementContents(((DateTime)propertyValue).ToLongDateString(), config, writer);
			}
			else
			{
				Debug.WriteLine("What do I do with {0}?", (object)propertyValue.GetType().Name);
			}
		}

		private static void WriteElementContents(object propertyValue, ConfigurableDictionaryNode config,
															  XmlWriter writer)
		{
			writer.WriteStartElement(GetElementNameForProperty(propertyValue, config));
			WriteClassNameAttribute(writer, config);
			writer.WriteString(propertyValue.ToString());
			writer.WriteEndElement();
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiStringAccessor
		/// </summary>
		/// <param name="multiStringAccessor"></param>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if(wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			}
			foreach(var option in wsOptions.Options)
			{
				if(!option.IsEnabled)
				{
					continue;
				}
				var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
				// The string for the specific wsId in the option, or the best string option in the accessor if the wsId is magic
				ITsString bestString;
				if(wsId == 0)
				{
					// This is not a magic writing system, so grab the user requested string
					wsId = cache.WritingSystemFactory.GetWsFromStr(option.Id);
					bestString = multiStringAccessor.get_String(wsId);
				}
				else
				{
					// Writing system is magic i.e. 'best vernacular' or 'first pronunciation'
					// use the method in the multi-string to get the right string and set wsId to the used one
					bestString = multiStringAccessor.GetAlternativeOrBestTss(wsId, out wsId);
				}
				if(String.IsNullOrEmpty(bestString.Text))
				{
					continue;
				}
				if(wsOptions.DisplayWritingSystemAbbreviations)
				{
					writer.WriteStartElement("span");
					writer.WriteStartAttribute("class", "writingsystemprefix");
					var prefix = ((IWritingSystem)cache.WritingSystemFactory.get_EngineOrNull(wsId)).Abbreviation;
					writer.WriteString(prefix);
					writer.WriteEndElement();
				}
				writer.WriteStartElement("span");
				WriteClassNameAttribute(writer, config);
				var wsName = cache.WritingSystemFactory.get_EngineOrNull(wsId).Id;
				GenerateXHTMLForString(bestString, config, writer, cache, wsName);
				writer.WriteEndElement();
			}
		}

		private static void GenerateXHTMLForString(ITsString fieldValue,
																 ConfigurableDictionaryNode config,
																 XmlWriter writer, FdoCache cache, string writingSystem = null)
		{
			//use the passed in writing system unless null
			//otherwise use the first option from the DictionaryNodeWritingSystemOptions or english if the options are null
			writingSystem = writingSystem ?? GetLanguageFromFirstOption(config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions, cache);
			writer.WriteAttributeString("lang", writingSystem);
			writer.WriteString(fieldValue.Text);
		}

		/// <summary>
		/// This method is intended to produce the xhtml element that we want for given configuration objects. It may prove to be unnecessary
		/// upon an evaluation of the XHTML that the old export produced.
		/// TODO: Figure out if we actually need this
		/// </summary>
		/// <param name="propertyValue"></param>
		/// <param name="config"></param>
		/// <returns></returns>
		private static string GetElementNameForProperty(object propertyValue, ConfigurableDictionaryNode config)
		{
			//TODO: Improve this logic to deal with subentries if necessary
			if(config.FieldDescription.Equals("LexEntry"))
			{
				return "div";
			}
			return "span";
		}

		/// <summary>
		/// This method returns the lang attribute value from the first selected writing system in the given options.
		/// </summary>
		/// <param name="wsOptions"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static string GetLanguageFromFirstOption(DictionaryNodeWritingSystemOptions wsOptions, FdoCache cache)
		{
			const string defaultLang = "en";
			if(wsOptions == null)
				return defaultLang;
			foreach(var option in wsOptions.Options)
			{
				if(option.IsEnabled)
				{
					var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
					// if the writing system isn't a magic name just use it
					if( wsId == 0)
					{
						return option.Id;
					}
					// otherwise get a list of the writing systems for the magic name, and use the first one
					return WritingSystemServices.GetWritingSystemList(cache, wsId, true).First().Id;
				}
			}
			// paranoid fallback to first option of the list in case there are no enabled options
			return wsOptions.Options[0].Id;
		}
	}
}
