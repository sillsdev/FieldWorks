//-------------------------------------------------------------------------------------------------
// <copyright file="SourceLineNumberCollection.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Hold information about a collection of source lines.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Text;

	/// <summary>
	/// Hold information about a collection of source lines.
	/// </summary>
	public class SourceLineNumberCollection : ICollection
	{
		private string encodedSourceLineNumbers;
		private SourceLineNumber[] sourceLineNumbers;

		/// <summary>
		/// Instantiate a new SourceLineNumberCollection from encoded source line numbers.
		/// </summary>
		/// <param name="encodedSourceLineNumbers">The encoded source line numbers.</param>
		public SourceLineNumberCollection(string encodedSourceLineNumbers)
		{
			if (null == encodedSourceLineNumbers)
			{
				throw new ArgumentNullException("encodedSourceLineNumbers");
			}

			this.encodedSourceLineNumbers = encodedSourceLineNumbers;
			this.sourceLineNumbers = null;
		}

		/// <summary>
		/// Instantiate a new SourceLineNumberCollection from an array of SourceLineNumber objects.
		/// </summary>
		/// <param name="sourceLineNumbers">The SourceLineNumber objects.</param>
		public SourceLineNumberCollection(SourceLineNumber[] sourceLineNumbers)
		{
			if (null == sourceLineNumbers)
			{
				throw new ArgumentNullException("sourceLineNumbers");
			}

			this.encodedSourceLineNumbers = null;
			this.sourceLineNumbers = sourceLineNumbers;
		}

		/// <summary>
		/// Gets a 32-bit integer that represents the total number of elements in the SourceLineNumberCollection.
		/// </summary>
		/// <value>A 32-bit integer that represents the total number of elements in the SourceLineNumberCollection.</value>
		public int Count
		{
			get { return this.SourceLineNumbers.Length; }
		}

		/// <summary>
		/// The SourceLineNumberCollection encoded in a string.
		/// </summary>
		public string EncodedSourceLineNumbers
		{
			get
			{
				if (null == this.encodedSourceLineNumbers)
				{
					StringBuilder sb = new StringBuilder();

					for (int i = 0; i < this.SourceLineNumbers.Length; ++i)
					{
						if (0 < i)
						{
							sb.Append("|");
						}

						sb.Append(this.sourceLineNumbers[i].QualifiedFileName);
					}

					this.encodedSourceLineNumbers = sb.ToString();
				}

				return this.encodedSourceLineNumbers;
			}
		}

		/// <summary>
		/// Gets a value indicating whether access to the SourceLineNumberCollection is syncronized.
		/// </summary>
		/// <value>A value indicating whether access to the SourceLineNumberCollection is syncronized.</value>
		public bool IsSynchronized
		{
			get { return this.SourceLineNumbers.IsSynchronized; }
		}

		/// <summary>
		/// Gets an object that can be used to syncronize access to the SourceLineNumberCollection.
		/// </summary>
		/// <value>An object that can be used to syncronize access to the SourceLineNumberCollection.</value>
		public object SyncRoot
		{
			get { return this.SourceLineNumbers.SyncRoot; }
		}

		/// <summary>
		/// The (possibly generated) SourceLineNumber array.
		/// </summary>
		private SourceLineNumber[] SourceLineNumbers
		{
			get
			{
				if (null == this.sourceLineNumbers)
				{
					string[] encodedSplit = this.encodedSourceLineNumbers.Split("|".ToCharArray());
					this.sourceLineNumbers = new SourceLineNumber[encodedSplit.Length];

					for (int i = 0; i < encodedSplit.Length; ++i)
					{
						string[] fileLineNumber = encodedSplit[i].Split("*".ToCharArray());
						if (2 == fileLineNumber.Length)
						{
							this.sourceLineNumbers[i] = new SourceLineNumber(fileLineNumber[0], Convert.ToInt32(fileLineNumber[1]));
						}
						else
						{
							this.sourceLineNumbers[i] = new SourceLineNumber(fileLineNumber[0]);
						}
					}
				}

				return this.sourceLineNumbers;
			}
		}

		/// <summary>
		/// Get the SourceLineNumber object at a particular index.
		/// </summary>
		/// <param name="index">Index of the SourceLineNumber.</param>
		public SourceLineNumber this[int index]
		{
			get { return this.SourceLineNumbers[index]; }
			set { this.SourceLineNumbers[index] = value; }
		}

		/// <summary>
		/// Create a new SourceLineNumberCollection from a fileName.
		/// </summary>
		/// <param name="fileName">The fileName.</param>
		/// <returns>The new SourceLineNumberCollection.</returns>
		public static SourceLineNumberCollection FromFileName(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException("fileName");
			}

			return new SourceLineNumberCollection(fileName);
		}

		/// <summary>
		/// Copies all elements of the SourceLineNumberCollection to the specified one-dimensional array starting
		/// at the specified destination index.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the elements copied from the SourceLineNumberCollection.</param>
		/// <param name="index">A 32-bit integer which represents the index in the destination array at which the copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			this.SourceLineNumbers.CopyTo(array, index);
		}

		/// <summary>
		/// Returns an IEnumerator for the SourceLineNumberCollection.
		/// </summary>
		/// <returns>An IEnumerator for the SourceLineNumberCollection.</returns>
		public IEnumerator GetEnumerator()
		{
			return this.SourceLineNumbers.GetEnumerator();
		}
	}
}