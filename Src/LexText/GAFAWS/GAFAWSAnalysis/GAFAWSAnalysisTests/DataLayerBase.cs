// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: DataLayerBase.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Superclass for unit tests that work with a GAFAWSData object.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.IO;

using NUnit.Framework;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for unit tests that work with a GAFAWSData object.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class DataLayerBase
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Main Data layer object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected GAFAWSData m_gd;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DataLayerBase()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make a temporary file with the given contents.
		/// </summary>
		/// <param name="contents">The file's contents.</param>
		/// <returns>Pathname for the new file.</returns>
		/// -----------------------------------------------------------------------------------
		protected string MakeFile(string contents)
		{
			string fileName = null;
			StreamWriter sw = null;
			try
			{
				fileName = Path.GetTempFileName();
				sw = new StreamWriter(fileName);
				sw.WriteLine(contents);
			}
			finally
			{
				if (sw != null)
					sw.Close();
			}

			return fileName;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make an empty temporary file.
		/// </summary>
		/// <returns>Pathname for the new file.</returns>
		/// -----------------------------------------------------------------------------------
		protected string MakeFile()
		{
			return Path.GetTempFileName();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Delete a file.
		/// </summary>
		/// <param name="fileName">Name of file to delete.</param>
		/// -----------------------------------------------------------------------------------
		protected void DeleteFile(string fileName)
		{
			if (fileName != null)
			{
				File.Delete(fileName);
				Assert.IsFalse(File.Exists(fileName));
			}
		}
	}
}
