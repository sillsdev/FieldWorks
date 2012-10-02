// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: IGAFAWS.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Declaration of the IGAFAWSConverter interface, and the abstract class GafawsProcessor,
// which can be used as a superclass for C# code that needs an instance of GAFAWSData.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Interface declaration.
	/// Data converters should implement this interface, if they want to take source data and
	/// convert it into data to be fed into the PositionAnalyzer class in the GAFAWSAnalysis
	/// assembly (which does the work of analyzing the affix positions)
	/// </summary>
	/// <returns>
	/// Path to the converted file. This file is expected to be
	/// </returns>
	/// ---------------------------------------------------------------------------------------
	public interface IGAFAWSConverter
	{
		/// <summary>
		/// Do whatever it takes to convert the input this processor knows about.
		/// </summary>
		void Convert();

		/// <summary>
		/// Gets the name of the converter that is suitable for display in a list
		/// of other converts.
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// Gets a description of the converter that is suitable for display.
		/// </summary>
		string Description
		{
			get;
		}

		/// <summary>
		/// Gets the pathname of the XSL file used to turn the XML into HTML.
		/// </summary>
		string XSLPathname
		{
			get;
		}
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Abstract class that knows how to register itself.
	/// Implementers of IGAFAWS should derive from this class,
	/// and implement the IGAFAWS interface.
	/// This class supplies an instance of GAFAWSData in its m_gd data member.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public abstract class GafawsProcessor
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// An instance of GAFAWSData.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected GAFAWSData m_gd;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public GafawsProcessor()
		{
			m_gd = GAFAWSData.Create();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the output name, which is based on the original input name.
		/// </summary>
		/// <param name="pathInput">The input pathname.</param>
		/// <returns>The output pathanme.</returns>
		/// -----------------------------------------------------------------------------------
		protected string GetOutputPathname(string pathInput)
		{
			return Path.GetDirectoryName(pathInput) + "\\OUT" + Path.GetFileName(pathInput);
		}
	}
}
