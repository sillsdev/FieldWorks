// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SubProcessorBase.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class SubProcessorBase
	{
		#region Member variables
		protected BinaryReader m_reader;
		protected BinaryWriter m_writer;
		protected Stream m_stream;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SubProcessorBase"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="reader">The reader.</param>
		/// <param name="writer">The writer.</param>
		/// ------------------------------------------------------------------------------------
		internal SubProcessorBase(Stream stream, BinaryReader reader, BinaryWriter writer)
		{
			m_stream = stream;
			m_reader = reader;
			m_writer = writer;
		}
	}
}
