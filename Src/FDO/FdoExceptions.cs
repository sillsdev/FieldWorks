// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoExceptions.cs
// Responsibility: Randy Regnier
// Last reviewed: never

using System;
using SIL.FieldWorks.Common.COMInterfaces;

// This file holds all FDO Exceptions
namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// An exception that is used when an item is already in a set.
	/// </summary>
	public class DuplicateItemException : InvalidOperationException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Error message.</param>
		public DuplicateItemException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// An exception for when a deleted FDO item is used.
	/// </summary>
	public class FDOObjectDeletedException : InvalidOperationException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Error message.</param>
		public FDOObjectDeletedException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// An exception for when an unitialized FDO item is used.
	/// </summary>
	public class FDOObjectUninitializedException : InvalidOperationException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Error message.</param>
		public FDOObjectUninitializedException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// An exception for when an CmObject has no FdoCache, or it is disposed.
	/// </summary>
	public class FDOCacheUnusableException : InvalidOperationException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Error message.</param>
		public FDOCacheUnusableException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// An exception for when an invalid field type is used.
	/// </summary>
	public class FDOInvalidFieldTypeException : InvalidOperationException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Error message.</param>
		public FDOInvalidFieldTypeException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// An exception for when an invalid field id is requested.
	/// </summary>
	public class FDOInvalidFieldException : ArgumentException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Error message.</param>
		public FDOInvalidFieldException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public FDOInvalidFieldException(string message, string paramName)
			: base(message, paramName)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">Error message.</param>
		/// <param name="innerException">Inner exception.</param>
		public FDOInvalidFieldException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public FDOInvalidFieldException(string message, string paramName, Exception innerException)
			: base(message, paramName, innerException)
		{
		}
	}

	/// <summary>
	/// An exception for when an invalid class id is requested.
	/// </summary>
	public class FDOInvalidClassException : ArgumentException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public FDOInvalidClassException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public FDOInvalidClassException(string message, string paramName)
			: base(message, paramName)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public FDOInvalidClassException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public FDOInvalidClassException(string message, string paramName, Exception innerException)
			: base(message, paramName, innerException)
		{
		}
	}

	#region Invalid chapter and verse exceptions
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class InvalidScriptureNumberException : ApplicationException
	{
		private readonly string m_badNumber;
		private readonly TsRunInfo m_runInfo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="badNumber">String with the invalid chapter or verse number</param>
		/// <param name="runInfo">Info about the run</param>
		/// ------------------------------------------------------------------------------------
		protected InvalidScriptureNumberException(string badNumber, TsRunInfo runInfo)
		{
			m_badNumber = badNumber;
			m_runInfo = runInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the invalid chapter or verse number string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InvalidNumber
		{
			get { return m_badNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the info about the run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsRunInfo RunInfo
		{
			get { return m_runInfo; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvalidChapterException : InvalidScriptureNumberException
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="badChapterNumber">String with the invalid chapter number</param>
		/// <param name="runInfo">Info about the run</param>
		/// ------------------------------------------------------------------------------------
		public InvalidChapterException(string badChapterNumber, TsRunInfo runInfo)
			: base(badChapterNumber, runInfo)
		{
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvalidVerseException : InvalidScriptureNumberException
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="badVerseNumber">String with the invalid verse number</param>
		/// <param name="runInfo">Info about the run</param>
		/// ------------------------------------------------------------------------------------
		public InvalidVerseException(string badVerseNumber, TsRunInfo runInfo)
			: base(badVerseNumber, runInfo)
		{
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Thrown when the style of the paragraph is not consistent with its structure (body or
	/// heading).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvalidStructureException : Exception
	{
		private string m_styleName;
		private StructureValues m_expectedStructure;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="styleName">Name of style</param>
		/// <param name="expectedStructure">Info about the run</param>
		/// ------------------------------------------------------------------------------------
		public InvalidStructureException(string styleName, StructureValues expectedStructure)
		{
			m_styleName = styleName;
			m_expectedStructure = expectedStructure;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the style which is invalid for the expected structure.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleName
		{
			get { return m_styleName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the expected structure (either heading or body).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StructureValues ExpectedStructure
		{
			get { return m_expectedStructure; }
		}
	}
	#endregion

	#region NullStyleRulesException
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Special exception to allow us to catch the condition where StyleRules is null in a
	/// common way.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NullStyleRulesException : NullReferenceException
	{
	}
	#endregion
}
