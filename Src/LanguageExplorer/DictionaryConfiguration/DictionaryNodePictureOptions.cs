// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>Options for formatting Pictures</summary>
	public class DictionaryNodePictureOptions : DictionaryNodeOptions
	{
		[XmlAttribute(AttributeName = "minimumHeight")]
		public float MinimumHeight { get; set; }

		[XmlAttribute(AttributeName = "minimumWidth")]
		public float MinimumWidth { get; set; }

		[XmlAttribute(AttributeName = "maximumHeight")]
		public float MaximumHeight { get; set; }

		[XmlAttribute(AttributeName = "maximumWidth")]
		public float MaximumWidth { get; set; }

		[XmlAttribute(AttributeName = "pictureLocation")]
		public AlignmentType PictureLocation { get; set; }

		[XmlAttribute(AttributeName = "stackPictures")]
		public bool StackMultiplePictures { get; set; }

		public override DictionaryNodeOptions DeepClone()
		{
			return DeepCloneInto(new DictionaryNodePictureOptions());
		}
	}
}