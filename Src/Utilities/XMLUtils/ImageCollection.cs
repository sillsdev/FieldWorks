// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImageCollection.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Reflection;

namespace SIL.Utils
{
	public class ImageCollection
	{
		protected ImageList m_images;
		protected System.Collections.Specialized.StringCollection m_labels;
		public ImageCollection(bool isLarge)
		{
			m_images = new ImageList();
			m_images.ColorDepth = ColorDepth.Depth24Bit;
			if(isLarge)
				m_images.ImageSize = new System.Drawing.Size(32,32);
			m_labels = new System.Collections.Specialized.StringCollection();
		}

		/// <summary>
		/// append the images in this list to our ImageList
		/// </summary>
		/// <param name="list">the ImageList to append</param>
		/// <param name="labels">the labels, in order, for each imaging the list</param>
		public void AddList (ImageList list, string[] labels)
		{
			//Debug.Assert(list == labels.Length);
			foreach(Image image in list.Images)
			{
				m_images.Images.Add(image);
			}
			foreach(string label in labels)
			{
				m_labels.Add(label.Trim()); //it's easy to lead a extra space sneak in there and ruin your day
			}


			//note that we can only handle one transparent color,
			//which is set here. Therefore, ImageList's added via this function should have the same transparency color.
			m_images.TransparentColor = list.TransparentColor;
		}

		public Image GetImage(string label)
		{
			int i = m_labels.IndexOf(label);
			if(i>=0)
				return m_images.Images[i];
			else if(label !=null && label.Length>0 &&  m_images.Images.Count>0)
				return m_images.Images[0];		//let the first one be the default
			else
				return null;
		}
		public int GetImageIndex(string label)
		{
			int i = m_labels.IndexOf(label);
			if(i>=0)
				return i;
			else
				return 0;//let 0 be the default in case something goes wrong
		}

		public ImageList ImageList
		{
			get
			{
				return m_images;
			}
		}

		public void AddList(XmlNodeList nodes)
		{
			foreach(XmlNode node in nodes)
			{
				string assemblyName = XmlUtils.GetAttributeValue(node, "assemblyPath").Trim();
				// Prepend the directory where the current DLL lives.  This should fix
				// LT-1541 (and similar bugs) once and for all!
				// (Note that CodeBase prepends "file:/", which must be removed.)
				string baseDir = System.IO.Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().CodeBase).Substring(6);
				string assemblyPath = System.IO.Path.Combine(baseDir, assemblyName);
				string className = XmlUtils.GetAttributeValue(node, "class").Trim();
				string field = XmlUtils.GetAttributeValue(node, "field").Trim();

				Assembly assembly=null;
				try
				{
					assembly = Assembly.LoadFrom(assemblyPath);
					if (assembly == null)
						throw new ApplicationException(); //will be caught and described in the catch
				}
				catch (Exception error)
				{
					throw new RuntimeConfigurationException("XCore Could not load the  DLL at :"+assemblyPath, error);
				}

				//make the  holder
				object holder = assembly.CreateInstance(className);
				if(holder == null)
				  throw new RuntimeConfigurationException("XCore could not create the class: "+className+". Make sure capitalization is correct and that you include the name space (e.g. XCore.ImageHolder).");

				//get the named ImageList
				FieldInfo info = holder.GetType().GetField(field);

				if(info== null)
				  throw new RuntimeConfigurationException("XCore could not find the field '"+field+"' in the class: "+className+". Make sure that the field is marked 'public' and that capitalization is correct.");

				ImageList images = (ImageList) info.GetValue(holder);

				string[] labels = XmlUtils.GetAttributeValue(node, "labels").Split(new char[]{','});
				if(labels.Length != images.Images.Count)
					throw new ConfigurationException("The number of image labels does not match the number of images in this <imageList>: "+node.OuterXml);
				this.AddList(images, labels);
			}
		}
	}
}
