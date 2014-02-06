// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A selection of dictionary elements and options, for configuring a dictionary publication.
	/// </summary>
	public class DictionaryConfigurationModel
	{
		/// <summary>
		/// Tree of dictionary elements
		/// </summary>
		public ConfigurableDictionaryNode PartTree;

		/// <summary>
		/// File where data is stored
		/// </summary>
		private string _file;

		/// <summary>
		/// Name of this dictionary configuration. eg "Stem-based"
		/// </summary>
		public string Label;

		/// <summary></summary>
		public void Save()
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void Load(string path)
		{
			PartTree = Deserialize(_file);
			Label = PartTree.Label; // If the root node's label is the alternate Dictionary name.
		}

		// Constructor could alternatively take a label or some other identifier
		/// <summary></summary>
		public DictionaryConfigurationModel(string path)
		{
			Load(path);
		}

		/// <summary>
		/// Process datafile into objects
		/// </summary>
		private ConfigurableDictionaryNode Deserialize(string file)
		{
			throw new NotImplementedException();
		}
	}
}
