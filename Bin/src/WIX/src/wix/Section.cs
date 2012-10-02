//-------------------------------------------------------------------------------------------------
// <copyright file="Section.cs" company="Microsoft">
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
// Section in an object file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Text;

	/// <summary>
	/// Type of section.
	/// </summary>
	public enum SectionType
	{
		/// <summary>Unknown section type, default and invalid.</summary>
		Unknown,
		/// <summary>Fragment section type.</summary>
		Fragment,
		/// <summary>Module section type.</summary>
		Module,
		/// <summary>Product section type.</summary>
		Product,
		/// <summary>Patch creation section type.</summary>
		PatchCreation
	}

	/// <summary>
	/// Section in an object file.
	/// </summary>
	public sealed class Section
	{
		private Intermediate intermediate;

		private string id;
		private SectionType type;
		private int codepage;

		private TableCollection tables;

		private ReferenceCollection references;
		private IgnoreModularizationCollection ignoreModularizations;
		private ComplexReferenceCollection complexReferences;
		private FeatureBacklinkCollection featureBacklinks;

		private SymbolCollection symbols;

		/// <summary>
		/// Creates a new section as part of an intermediate.
		/// </summary>
		/// <param name="intermediate">Parent intermediate.</param>
		/// <param name="id">Identifier for section.</param>
		/// <param name="type">Type of section.</param>
		/// <param name="codepage">Codepage for resulting database.</param>
		public Section(Intermediate intermediate, string id, SectionType type, int codepage)
		{
			this.intermediate = intermediate;
			this.id = id;
			this.type = type;
			this.codepage = codepage;

			this.tables = new TableCollection();
			this.references = new ReferenceCollection();
			this.ignoreModularizations = new IgnoreModularizationCollection();
			this.complexReferences = new ComplexReferenceCollection();
			this.featureBacklinks = new FeatureBacklinkCollection();
		}

		/// <summary>
		/// Gets the parent intermediate for section.
		/// </summary>
		/// <value>Parent intermediate.</value>
		public Intermediate Intermediate
		{
			get { return this.intermediate; }
		}

		/// <summary>
		/// Gets the identifier for the section.
		/// </summary>
		/// <value>Section identifier.</value>
		public string Id
		{
			get { return this.id; }
		}

		/// <summary>
		/// Gets the type of the section.
		/// </summary>
		/// <value>Type of section.</value>
		public SectionType Type
		{
			get { return this.type; }
		}

		/// <summary>
		/// Gets the codepage for the section.
		/// </summary>
		/// <value>Codepage for the section.</value>
		public int Codepage
		{
			get { return this.codepage; }
		}

		/// <summary>
		/// Gets the tables in the section.
		/// </summary>
		/// <value>Tables in section.</value>
		public TableCollection Tables
		{
			get { return this.tables; }
		}

		/// <summary>
		/// Gets the references in this section.
		/// </summary>
		/// <value>References in section.</value>
		public ReferenceCollection References
		{
			get { return this.references; }
		}

		/// <summary>
		/// Gets the collection of strings to ignore modularization.
		/// </summary>
		/// <value>Collection to ignore modularizations.</value>
		public IgnoreModularizationCollection IgnoreModularizations
		{
			get { return this.ignoreModularizations; }
		}

		/// <summary>
		/// Gets the collection of the feature backlinks in the section.
		/// </summary>
		/// <value>Feature backlinks.</value>
		public FeatureBacklinkCollection FeatureBacklinks
		{
			get { return this.featureBacklinks; }
		}

		/// <summary>
		/// Gets the collection of complex references in section.
		/// </summary>
		/// <value>Complex references in section.</value>
		internal ComplexReferenceCollection ComplexReferences
		{
			get { return this.complexReferences; }
		}

		/// <summary>
		/// Gets the symbols for this section.
		/// </summary>
		/// <param name="messageHandler">The message handler.</param>
		/// <returns>Collection of symbols for this section.</returns>
		internal SymbolCollection GetSymbols(IMessageHandler messageHandler)
		{
			if (null == this.symbols)
			{
				this.symbols = new SymbolCollection();

				// add a symbol for this section
				if (null != this.id)
				{
					this.symbols.Add(new Symbol(this, this.type.ToString(), this.id));
				}
				else
				{
					Debug.Assert(SectionType.Fragment == this.type, "Only Fragment sections can have a null Id.");
				}

				foreach (Table table in this.tables)
				{
					foreach (Row row in table.Rows)
					{
						Symbol symbol = row.Symbol;
						if (null != symbol)
						{
							try
							{
								this.symbols.Add(symbol);
							}
							catch (ArgumentException)
							{
								Symbol existingSymbol = this.symbols[symbol.Name];

								messageHandler.OnMessage(WixErrors.DuplicateSymbol(existingSymbol.Row.SourceLineNumbers, existingSymbol.Name));
								messageHandler.OnMessage(WixErrors.DuplicateSymbol2(symbol.Row.SourceLineNumbers));
							}
						}
					}
				}
			}

			return this.symbols;
		}
	}
}
