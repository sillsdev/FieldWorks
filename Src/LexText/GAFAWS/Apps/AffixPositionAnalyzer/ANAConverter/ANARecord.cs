// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2007' company='SIL International'>
//    Copyright (c) 2007, SIL International. All Rights Reserved.
// </copyright>
//
// File: ANARecord.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of ANARecord and ANAObject classes.
// </remarks>
//
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;

using SIL.WordWorks.GAFAWS;

namespace SIL.WordWorks.GAFAWS.ANAConverter
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Enumeration of field types that get processed.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal enum LineType
	{
		kUnderlyingForm,
		kDecomposition,
		kCategory
	};

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Abstract superclass of all other ANA objects, which serves to hold the data layer.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal abstract class ANAObject
	{
		static protected GAFAWSData s_gd;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the data layer object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal static GAFAWSData DataLayer
		{
			set { s_gd = value; }
			get { return s_gd; }
		}

		internal static void Reset()
		{
			s_gd = null;
			ANARecord.Reset();
			ANAAnalysis.Reset();
		}
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Representation of one ANA record in the file.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal class ANARecord : ANAObject
	{

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Ambiguity character, default is: '%'.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		static private char s_ambiguityCharacter = '%';
		protected List<ANAAnalysis> m_analyses;

		new internal static void Reset()
		{
			s_ambiguityCharacter = '%';
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the input parameters.
		/// </summary>
		/// <param name="parms">The pathname to the parameters file.</param>
		/// <exception cref="FileLoadException">
		/// Thrown when the input file is empty.
		/// </exception>
		/// -----------------------------------------------------------------------------------
		internal static void SetParameters(string parms)
		{
			if (parms == null)
				return;
			FileStream parameterReader = null;
			try
			{
				Parameters pams = Parameters.DeSerialize(parms);
				s_ambiguityCharacter = pams.Marker.Ambiguity;
				ANAAnalysis.OpenRootDelimiter = pams.RootDelimiter.OpenDelimiter;
				ANAAnalysis.CloseRootDelimiter = pams.RootDelimiter.CloseDelimiter;
				ANAAnalysis.SeparatorCharacter = pams.Marker.Decomposition;
				ANAAnalysis.PartsOfSpeech = pams.Categories;
			}
			catch
			{
				throw;
			}
			finally
			{
				if (parameterReader != null)
					parameterReader.Close();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="line">The \a field without the field marker.</param>
		/// -----------------------------------------------------------------------------------
		internal ANARecord(string line)
		{
			m_analyses = new List<ANAAnalysis>();
			ProcessALine(line);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert the object to the data layer.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void Convert()
		{
			for(int i = 0; i < m_analyses.Count; ++i)
				m_analyses[i].Convert();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the \a field.
		/// </summary>
		/// <param name="line">The complete \a field, without the field marker.</param>
		/// -----------------------------------------------------------------------------------
		protected void ProcessALine(string line)
		{
			if (IsCorrectAmbiguityCharacter(line))
			{
				string[] contents = TokenizeLine(line, true);
				for (int i = 2; i < contents.Length - 1; ++i)
					m_analyses.Add(new ANAAnalysis(contents[i]));
				return;
			}
			throw new ApplicationException("Incorrect ambiguity character.");
		}

		/// <summary>
		/// IsCorrectAmbiguityCharacter
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		private bool IsCorrectAmbiguityCharacter(string line)
		{
			// Not ambiguous.
			if (line.Length < 5) // Too short to be ambiguous.
				return true;
			char possibleAmbChar = line[line.Length - 1];
			if (possibleAmbChar != line[0]) // First and last not the same.
				return true;
			if (!Char.IsDigit(line[1])) // At least one digit
				return true;

			// Maybe ambiguous - find closing ambig char if there is one.
			for (int i = 2; i < line.Length - 2; ++i)
				if(!Char.IsDigit(line[i]))
				{
					if ((line[i] == possibleAmbChar)
						&& (possibleAmbChar == s_ambiguityCharacter))
						return true;
					return false;
				}
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the \w field.
		/// </summary>
		/// <param name="originalForm">The original wordform.</param>
		/// -----------------------------------------------------------------------------------
		internal void ProcessWLine(string originalForm)
		{
			for(int i = 0; i < m_analyses.Count; ++i)
				m_analyses[i].OriginalForm = originalForm;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the \d, \u, and \cat fields.
		/// </summary>
		/// <param name="type">The field being processed.</param>
		/// <param name="line">The complete line, without the field marker.</param>
		/// -----------------------------------------------------------------------------------
		internal void ProcessOtherLine(LineType type, string line)
		{
			string[] contents = TokenizeLine(line, false);
			if (m_analyses.Count != contents.Length - 3)
				return;
			for (int i = 0; i < m_analyses.Count; ++i)
				m_analyses[i].ProcessContent(type, contents[i + 2]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tokenize the given line.
		/// </summary>
		/// <param name="line">The line to tokenize.</param>
		/// <param name="isALine">True, if the line being processed is the \a line, otherwise false.</param>
		/// <returns>An array of tokenized strings.
		/// [Note: The array is returned, as if it all were ambiguous.]</returns>
		/// -----------------------------------------------------------------------------------
		protected string[] TokenizeLine(string line, bool isALine)
		{
			string[] contents = line.Trim().Split(s_ambiguityCharacter);
			if (contents.Length == 4 && isALine)
				contents[2] = null; // Analysis failure.
			else if (contents.Length == 1)
			{
				// Treat it the same as if it were ambiguous.
				// This makes other processing easier.
				string tempLine = contents[0];
				contents = new String[4];
				contents[2] = tempLine;
			}
			return contents;
		}

	}
}
