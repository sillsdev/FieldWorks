// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Base class for ConfigurableDictionaryNode options
	/// <note>This would be an interface, but the XMLSerialization doesn't like those</note>
	/// </summary>
	[XmlInclude(typeof(DictionaryNodeSenseOptions))]
	[XmlInclude(typeof(DictionaryNodeListOptions))]
	[XmlInclude(typeof(DictionaryNodeWritingSystemOptions))]
	[XmlInclude(typeof(DictionaryNodeListAndParaOptions))]
	[XmlInclude(typeof(DictionaryNodeWritingSystemAndParaOptions))]
	[XmlInclude(typeof(DictionaryNodePictureOptions))]
	[XmlInclude(typeof(DictionaryNodeGroupingOptions))]
	public abstract class DictionaryNodeOptions
	{
		/// <summary>
		/// Deeply clones all members of this <see cref="DictionaryNodeOptions"/>
		/// </summary>
		/// <returns>an identical but independent instance of this <see cref="DictionaryNodeOptions"/></returns>
		public abstract DictionaryNodeOptions DeepClone();

		/// <summary>
		/// Clones all writable properties, importantly handling strings and primitives
		/// </summary>
		/// <param name="target"></param>
		/// <returns><see cref="target"/>, as a convenience</returns>
		protected virtual DictionaryNodeOptions DeepCloneInto(DictionaryNodeOptions target)
		{
			var properties = GetType().GetProperties();
			foreach (var property in properties.Where(prop => prop.CanWrite)) // Skip any read-only properties
			{
				var originalValue = property.GetValue(this, null);
				property.SetValue(target, originalValue, null);
			}
			return target;
		}
	}
}
