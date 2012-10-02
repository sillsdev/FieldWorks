using System;
using System.Collections;
using System.IO;

namespace InstallLanguage
{
	/// <summary>
	/// ICUDataNode represents a single "node" of the ICU data files, such as icu\data\locales\root.txt
	/// Each "node" is the item that appears in the file as follows:
	/// <code>
	/// preSpace
	/// name afterNameSpace { braceSpace
	///		children or data
	///	} postSpace
	///
	///	All the "space" variable inlude their own newlines when necessary.
	///	preSpace,braceSpace, postSpace
	///
	///	</code>
	///	where <code>children</code> are child nodes of the given node.
	/// </summary>
	/// <remarks>
	/// NOTE: the terminology here is different that the terminology used to describe the regexp parsing of the file.
	///
	/// Regexp			IcuDataFile
	/// "key"			"node" or "attribute"
	///					(depending on which it is, nodes have {}, attributes are just the ""s, or just a number)
	/// "value"			"attritute's value"
	/// "comment"		"comment"
	///					(or "whitespace"/"space" if you are including newlines and indentation)
	///
	///
	///	<assumptions>
	///	General Assumptions:
	///		There will be no lone Environment.NewLine characters,
	///			they will always be accompanied by the rest of Environment.NewLine string.
	///	Assumptions on the format:
	///		Assume that there is only one root node in a file,
	///			i.e., the file doesn't contain any nodes that aren't held together by some parent up above.
	///		Tokens may not include the slash ('/') character except when embedded in a string (e.g. they must be used in comments)
	///		Assume that strings are only made with " " not ' '
	///		All non strings (i.e values without the "") are numeric integer values.
	///			(note: all values will be stored and re-written correctly regardless of this assumption)
	///		Strings may not be broken across new lines.
	///	Things we must support:
	///		Must allow nodes with empty string names.
	///		Can't assume comments are all C-style: '//'
	///			We must support C++ style: /* */
	///		Comments and '}' don't have any structural significance inside " "s
	///		Must check for character escapes
	///			\" is a legal character escape, our parser must allow it.
	///		Multi-line data doesn't always end with a ',' (ignoring whitespace and comments).  All ',' are optional.
	///		We cannot assume that new lines have meaning other than termination of single line comments.
	///	</assumptions>
	/// </remarks>
	public class IcuDataNode
	{
		/// <summary>
		/// The default tab to use for adding new elements.
		/// </summary>
		public static string tab = "    ";

		#region member_variables
		/// <summary>
		/// <code>
		/// preSpace
		/// name { braceSpace
		///		children or data
		///	} postSpace
		///	</code>
		/// </summary>
		private string name = "";
		/// <summary>
		/// <code>
		/// preSpace
		/// name { braceSpace
		///		children or data
		///	} postSpace
		///	</code>
		/// </summary>
		private string[] preSpace={""};
		/// <summary>
		/// <code>
		/// preSpace
		/// name { braceSpace
		///		children or data
		///	} postSpace
		///	</code>
		/// </summary>
		private string[] postSpace={""};
		/// <summary>
		/// <code>
		/// preSpace
		/// name { braceSpace
		///		children or data
		///	} postSpace
		///	</code>
		/// </summary>
		private string[] braceSpace={""};
		/// <summary>
		/// <code>
		/// preSpace
		/// name afterNameSpace { braceSpace
		///		children or data
		///	} postSpace
		///	</code>
		/// </summary>
		private string[] afterNameSpace={""};

		/// <summary>
		/// The indent of the given node, in <c>IcuDataNode.tab</c>s
		/// The value UNDEFINED_INDENT means the indent is unset.
		/// It is not always possible to set the indent when the tab is first made because
		/// often a node will have its children added before it is added to the tree
		/// </summary>
		private short indentCount = UNDEFINED_INDENT;
		/// <summary>
		/// See this.indentCount
		/// </summary>
		private const short UNDEFINED_INDENT = -1;

		/// <summary>
		/// The parent of this data node
		/// </summary>
		private IcuDataNode parent = null;

		/// <summary>
		/// A list of ICUDataNodes contained in this node.
		/// </summary>
		private ArrayList children = new ArrayList();

		/// <summary>
		/// The children IcuDataNodes of this node
		/// </summary>
		public IList Children
		{
			get { return (IList)children; }
		}

		/// <summary>
		/// A list of attributes containded in this node.
		/// </summary>
		private ArrayList attributes = new ArrayList();

		public IList Attributes
		{
			get { return (IList)attributes; }
		}
		#endregion

		# region member variable accessing

		private string PreSpace
		{
			get{ return GetSpace(preSpace); }
			set{ preSpace = SetSpace(value); }
		}
		private string PostSpace
		{
			get{ return GetSpace(postSpace); }
			set{ postSpace = SetSpace(value); }
		}
		private string BraceSpace
		{
			get{ return GetSpace(braceSpace); }
			set{ braceSpace = SetSpace(value); }
		}
		private string AfterNameSpace
		{
			get{ return GetSpace(afterNameSpace); }
			set{ afterNameSpace = SetSpace(value); }
		}

		/// <summary>
		/// Turns the underlying string array into the public representation.
		/// </summary>
		/// <param name="space"></param>
		/// <returns></returns>
		private static string GetSpace( string[] space )
		{
			StringWriter writer = new StringWriter();
			foreach( string line in space )
			{
				writer.Write(line);
			}
			return writer.ToString();
		}

		/// <summary>
		/// Parses the given String into the underlying string array
		/// </summary>
		/// <param name="inputValue">The spacing as it would appear in the file,
		/// or as it is returned by <c>GetSpace</c></param>
		/// <returns></returns>
		private static string[] SetSpace(string inputValue)
		{
			StringReader reader = new StringReader(inputValue);
			ArrayList list = new ArrayList();
			bool firstLine = true;
			while( true )
			{
				StringWriter writer = new StringWriter();
				// ASSUME: if we hit the first char of the newline the rest of the newline will be there.
				while( reader.Peek() != Environment.NewLine[0]
					&& reader.Peek() != -1 )
					writer.Write((char)reader.Read());
				// If we are at the end of a line, add the line
				if( reader.Peek() == Environment.NewLine[0] )
				{
					foreach( char newlineChar in Environment.NewLine )
					{
						if( reader.Read() != newlineChar)
						{
							LogFile.AddErrorLine("Unpaired Environment.Newline");
							throw new InstallLanguage.Errors.LDExceptions(
								Errors.ErrorCodes.ICUDataParsingError);
						}
						writer.Write(newlineChar);
					}
					// Don't trim the first line
					if( firstLine )
						list.Add(writer.ToString());
					else
						list.Add(writer.ToString().TrimStart(new char[]{' ','\t'}));
					// We are done with the first line
					firstLine = false;
				}
					// if we are at the end of the string
				else if( reader.Peek() == -1 )
				{
					// If the string doesn't contain any newlines add it without any modification.
					if( firstLine )
						list.Add(writer.ToString());
					else if( writer.ToString().Trim() != "" )
						list.Add(writer.ToString().Trim());
					return (string [])(list.ToArray(Type.GetType("System.String")));
				}
				else
				{
					LogFile.AddErrorLine("This will never happen - in SetSpace");
					throw new InstallLanguage.Errors.LDExceptions(
						InstallLanguage.Errors.ErrorCodes.NonspecificError);
				}
			}
		}

		/// <summary>
		/// How many levels of indent this node should have.
		/// Will not return UNDEFINED_INDENT
		/// </summary>
		private int IndentCount
		{
			get
			{
				if( indentCount == UNDEFINED_INDENT )
				{
					if( parent == null )
						indentCount = 0;
					else
						indentCount = (short)(parent.IndentCount + 1);
				}
				return indentCount;
			}
		}

		/// <summary>
		/// The indent string of this node.
		/// And additional tab is needed for any attributes or children.
		/// </summary>
		private string Indent
		{
			get
			{
				StringWriter writer = new StringWriter();
				for (int i = 0; i < IndentCount ; i++)
				{
					writer.Write(tab);
				}
				return writer.ToString();
			}
		}
		/// <summary>
		/// The base addition of an attribute, all additions of attributes should come through here.
		/// Unless an index is given to replace an attribute,
		/// adds the attribute at end.
		///
		/// (except for a special, compatability feature:
		///	Adds the attribute before the last one if the last attribute is empty, ""
		///	 (unless the new attribute is empty as well)
		/// </summary>
		/// <param name="newAttribute">The attribute to add to this node</param>
		/// <param name="index">The index of the attribute to replace.  Add at the end if the value is -1</param>
		public void AddAttribute(IcuDataAttribute newAttribute, int index)
		{
			newAttribute.ContainingNode = this;
			if( index == -1 )
			{
				if( attributes.Count > 0 && newAttribute.Value != "\"\"" &&
					((IcuDataAttribute)attributes[attributes.Count-1]).Value == "\"\"")
					attributes.Insert(attributes.Count-1,newAttribute);
				else
					attributes.Add(newAttribute);
			}
			else
				attributes[index] = newAttribute;
		}

		public bool removeChild(string childName)
		{
			IcuDataNode childNode = Child(childName);
			if (childNode != null)
			{
				children.Remove(childNode);
				return true;
			}
			return false;
		}

		public int removeAttribute(string dataValue)
		{
			for (int indx=0; indx<attributes.Count; indx++)
			{
				IcuDataAttribute attribute = attributes[indx] as IcuDataAttribute;
				// Check both with and without the quotes.
				if( attribute.StringValue == dataValue || attribute.Value == dataValue )
				{
					attributes.Remove(attribute);
					break;
				}
			}
			return attributes.Count;
		}

		public void ChangeRootName(string newName)
		{
			// [SIL-Corp] xkal.xml Added  Tuesday, March 21, 2006

			if (preSpace.Length == 1 && preSpace[0].IndexOf("//") == 0)
			{
				System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
				preSpace[0] = "// [SIL-Corp] " + newName + ".txt Derived from " + name + ".txt  " + DateTime.Now.ToString("D", ci) + Environment.NewLine;
			}
			name = newName;
		}

		/// <summary>
		/// Adds the attribute at end.
		///
		/// (except for a special, compatability feature:
		///	Adds the attribute before the last one if the last attribute is empty, ""
		///	 (unless the new attribute is empty as well
		/// </summary>
		/// <param name="newAttribute"></param>
		public void AddAttribute(IcuDataAttribute newAttribute)
		{
			// Add the value at the end
			AddAttribute(newAttribute, -1);
		}

		/// <summary>
		/// Adds an attribute with no special formatting checking.
		/// </summary>
		/// <param name="dataValue"></param>
		/// <param name="postSpace"></param>
		public void AddAttribute(string dataValue, string postSpace, bool ensureQuotes)
		{
			AddAttribute(new IcuDataAttribute(dataValue, postSpace, ensureQuotes));
		}

		/// <summary>
		/// Fix the naming/parameters of these AddAttribute methods
		/// Add a new attribute, overwriting any existing attributes
		/// </summary>
		/// <param name="singleLine">Currently ignored</param>
		public void AddAttributeSmart(IcuDataAttribute newAttribute)
		{
			// Read the current value of the newAttribute
			// This is used becuase you cannot pass an attribute by referencce
			string attributePostSpace = newAttribute.PostSpace;

			// If there are no attributes,
			if( attributes.Count == 0 )
			{
				// This is the first attribute, it must go on a new line.
				// NOTE: Brace space will usuaally be an empty string when the node has children
				// and a newline when the node has several attributes.
				this.BraceSpace = Environment.NewLine;
				AddAttribute(newAttribute);
				return;
			}
			for(int index=0; index < attributes.Count; index++)
			{
				// Compare with and without the quotes
				if( ((IcuDataAttribute)attributes[index]).Value == newAttribute.StringValue ||
					((IcuDataAttribute)attributes[index]).Value == newAttribute.Value )
				{
					// overwrite the existing attribute
					AddAttribute(newAttribute, index);
					return;
				}
			}
			// Add the attribute
			AddAttribute(newAttribute);
		}


		/// <summary>
		/// Looks for any attribute that matches the given IcuDataAttribute.Value
		/// </summary>
		/// <param name="dataValue">The value to match</param>
		/// <returns><c>true</c> if this contains an IcuDataAttribute with the same value.</returns>
		public bool containsAttribute(string dataValue)
		{
			foreach( IcuDataAttribute attribute in attributes)
			{
				// If there is a match, then the value is contained
				// Check both with and without the quotes.
				if( attribute.StringValue == dataValue || attribute.Value == dataValue )
					return true;
			}
			return false;
		}

		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Returns the first child matching the given name.
		/// If no child is found, returns <c>null</c>.
		/// </summary>
		/// <param name="childName"></param>
		/// <returns></returns>
		public IcuDataNode Child(string childName)
		{
			foreach( IcuDataNode child in children )
				if( child.name == childName )
					return child;
			return null;
		}

		/// <summary>
		/// Adds the given child at the end.
		/// (with no adjustment of spacing or overwriting of existing nodes.)
		/// </summary>
		/// <param name="child"></param>
		public void AddChildSimple(IcuDataNode newChild)
		{
			AddChildSimple(newChild, false);
		}

		/// <summary>
		/// Adds the child at the beginning or end with no overwriting of existing nodes or adjusment of spacing.
		/// </summary>
		/// <param name="newChild"></param>
		/// <param name="addAtTop"><c>true</c> if the child should be added before the rest.</param>
		public void AddChildSimple(IcuDataNode newChild, bool addAtTop)
		{
			// Indent the child one more than this;
			if( this.indentCount != UNDEFINED_INDENT )
				newChild.indentCount = (short)(this.indentCount + 1);
			newChild.parent = this;
			if( addAtTop )
			{
				children.Insert(0,newChild);
			}
			else
			{
				children.Add(newChild);
			}
		}

		/// <summary>
		/// Adds the given child, overwriting the first existing child with the same name, if there is one.
		/// Properly handles the indenting in the "space" member variables.
		/// </summary>
		/// <param name="child">The child to add.</param>
		/// <param name="addAtTop">Should the child be added before the rest.</param>
		public void AddChildSmart(IcuDataNode newChild, bool addAtTop)
		{
			if( children.Count == 0 )
			{
				AddChildSimple(newChild);
				// Add a new line so that this nodes '}' will be on it's own line
				newChild.PostSpace += Environment.NewLine;
				return;
			}
			for(int index=0; index < children.Count; index++)
			{
				if( ((IcuDataNode)children[index]).name == newChild.name )
				{
					// overwrite the existing child
					newChild.parent = this;
					// If this is the last child we need to move the new line from the old last child to this new one.
					if( index == children.Count - 1)
					{
						// Move the newline from the post comment of the last child to the new last child
						((IcuDataNode)children[children.Count-1]).PostSpace =
							RemoveNewlineFromEnd(((IcuDataNode)children[children.Count-1]).PostSpace);
						newChild.PostSpace += Environment.NewLine;
					}
					children[index]=newChild;
					return;
				}
			}
			if( addAtTop )
				// Add the child at the beginning
				AddChildSimple(newChild,true);
			else
			{
				// Move the newline from the post comment of the last child to the new last child
				((IcuDataNode)children[children.Count-1]).PostSpace =
					RemoveNewlineFromEnd(((IcuDataNode)children[children.Count-1]).PostSpace);
				newChild.PostSpace += Environment.NewLine;
				// Add the child at the end
				AddChildSimple(newChild);
			}
		}

		/// <summary>
		/// Safely removes a newline from the end of the string, if there is one.
		/// </summary>
		/// <param name="multiLine"></param>
		/// <returns></returns>
		private string RemoveNewlineFromEnd( string multiLine )
		{
			int lastIndex = multiLine.LastIndexOf(Environment.NewLine);
			// If there is a newline at the end
			if( lastIndex >= 0 && lastIndex == multiLine.Length - Environment.NewLine.Length)
			{
				return multiLine.Substring(0,lastIndex);
			}
			else
				return multiLine;
		}


		#endregion

		#region constructors
		/// <summary>
		/// Creates a new empty IcuDataNode
		/// </summary>
		public IcuDataNode()
		{
		}

		/// <summary>
		/// Creates a new one-line IcuDataNode
		/// </summary>
		public IcuDataNode(string name, string attribute, string comment)
		{
			// Set the name
			this.name = name;
			// Adds a space after the name
			this.AfterNameSpace = " ";

			// Remove the spacing around the attribute value if it is an empty string.
			string spacing = " ";
			if( attribute == "" )
				spacing =  "";

			// Put a newline before this new node
			this.PreSpace = Environment.NewLine;
			// Put a single space between the brace and the attribute.
			this.BraceSpace = spacing;
			// Add a simple single attribute, with a single space after the attribute.
			this.AddAttribute(attribute, spacing, true);
			// Add the comment with simple non-indented newline
			this.PostSpace = comment;
		}

		/// <summary>
		/// Creates a new IcuDataNode with no attributes or children.
		///
		/// All comments should not be indented and should not end with a newline.
		/// </summary>
		/// <param name="name">The name of this new node.</param>
		/// <param name="parent">The parent this will be a child of, for indent information.</param>
		/// <param name="preComment">The comment that appears before this node.</param>
		/// <param name="newline">Whether the node should appear on more than one line.</param>
		/// <param name="postComment">The comment that appears after the node.</param>
		public IcuDataNode(string name, IcuDataNode parent, string preComment, string postComment)
		{
			// Set the name
			this.name = name;
			// Adds a space after the name
			this.AfterNameSpace = " ";

			// The node must put itself on a new line.
			this.PreSpace = Environment.NewLine;
			// Add the comments on new lines, if there are comments
			if( preComment != "")
				this.PreSpace += preComment + Environment.NewLine;
			if( postComment != "")
				this.PostSpace = Environment.NewLine + postComment;
		}
		#endregion

		#region Parse Methods

		/// <summary>
		/// Parses a file returning the root node.
		/// If there is more than one root node in the file.
		///  these nodes will be treated as white space or comments.
		/// </summary>
		/// <param name="reader">The file to be read.</param>
		/// <param name="rootNode">The "root" node of the file. (not neccessarily named "root")</param>
		/// <returns></returns>
		public static void Parse(TextReader reader, out IcuDataNode rootNode)
		{
			Parse(reader,out rootNode,null);
			rootNode.PostSpace += reader.ReadToEnd();
		}

		/// <summary>
		/// Finds one new node and its chilren.
		///
		/// (If it find attributes instead of a new node, it will add the attributes to the <c>parent</c>)
		///
		/// Parses the reader assuming that the first line of the reader will be either comments before the
		/// first line of the node, or the first line of the node itself.
		///
		/// This will read either to the closing '}' of the parent who called it, and return <c>true</c>
		/// or it will read to the last line of itself, and return <c>false</c>
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="writer">The reader to parse from.</param>
		/// <param name="node">The new node, will be null if there was no data given.</param>
		/// <param name="parent">The parent, used to add data if this wasn't really a new node.
		///		parent may be null.</param>
		/// <returns><code>true</code> if we have parsed the ending '}' for the parent node.</returns>
		public static bool Parse( TextReader reader, out IcuDataNode newNode, IcuDataNode parent)
		{
			newNode = null;
			string firstWhitespace = "";
			string secondWhitespace = "";
			string firstToken = "";
			string secondToken = "";
			// 1. Find name of a new child node of the parent
			//    e.g. "name { [ // comment ]" not " // comment { comment
			// (or an attribute of parent)
			while(true)
			{
				// Parse a single whitespace and token
				ParseToken( reader, out firstWhitespace, out firstToken );
				// if we find the end of the parent return "true"
				if( firstToken == "}" )
				{
					// If it is an empty node, add the comments as braceSpace
					if( parent.children.Count < 1 )
					{
						parent.BraceSpace = firstWhitespace;
					}
					else
					{
						// Add the whitespace after the last brace
						((IcuDataNode)parent.children[parent.children.Count-1]).PostSpace += firstWhitespace;
					}
					// we found the parent's '}'
					return true;
				}
					// If we find a child node with no name, allow it
				else if( firstToken == "{" )
				{
					newNode = new IcuDataNode();
					newNode.name = "";
					newNode.PreSpace = firstWhitespace;
					ParseChildren(reader,newNode);
					// we did not find the parent's '}'
					return false;
				}
				// Pares a second whitespace and token
				ParseToken( reader, out secondWhitespace, out secondToken );
				// if we find a new child node
				if( secondToken == "{" )
				{
					newNode = new IcuDataNode();
					newNode.name = firstToken;
					newNode.PreSpace = firstWhitespace;
					newNode.AfterNameSpace = secondWhitespace;
					ParseChildren(reader,newNode);
					// If we made it to this point,
					//		then we have parsed all the sub-nodes and the last one found our '}'
					// At this point we have not found our parent's '}'
					return false;
				}
					// We found only one attribute for the parent.
				else if( secondToken == "}" )
				{
					parent.BraceSpace += firstWhitespace;
					parent.AddAttribute(firstToken, secondWhitespace, false);
					// We found the parent's ending '}'
					return true;
				}
					// There are two data elements in a row
				else
				{
					parent.BraceSpace += firstWhitespace;
					parent.AddAttribute(firstToken,secondWhitespace, false);
					string lastToken = secondToken;
					string currentToken;
					// Add children until we find the "}"
					while( lastToken != "}" )
					{
						ParseToken( reader, out firstWhitespace, out currentToken);
						parent.AddAttribute( lastToken, firstWhitespace, false );
						lastToken = currentToken;
					}
					// found the end of the parent
					return true;
				}
			}
		}

		/// <summary>
		///  Special characters that are tokens on their own, not a continuation of a previous token.
		/// </summary>
		private static string specialTokenCharacters = "{}/";

		/// <summary>
		/// Parses one token and the preceding whitespace.
		/// </summary>
		///<param name="whitespace">The whitespace (including comments) before the next token found</param>
		///<param name="token">The token we wish to find.</param>
		private static void ParseToken(TextReader reader, out string whitespace, out string token)
		{
			// If we are not reading a token by definition we are reading whitespace.
			bool readingToken = false;

			whitespace = "";
			token = "";

			// This is the unicode character that we are currently reading from the file
			char character;
			while( reader.Peek() != -1)
			{
				character = (char)reader.Peek();

				// We are treating commas as white space because they add no special meaning in this file.
				if( Char.IsWhiteSpace(character) || character == ',')
				{
					if( !readingToken )
						whitespace += (char)reader.Read();
					else
						// If we found whitespace while reading a token we found the complete token.
						return;
				}
					// Handle comments
				else if ( character == '/')
				{
					if( !readingToken )
					{
						whitespace += (char)reader.Read();
						character = (char)reader.Peek();
						// Add single line comments to the whitespace
						if( character == '/' )
						{
							whitespace += reader.ReadLine() + System.Environment.NewLine;
						}
							// Add multi-line comments to the whitespace
						else if (character == '*')
						{
							// Read until we find the "*/", or the end of the file,
							// Adding to the line as necessary
							while( (reader.Peek()!= -1 ) &&
								!( ((character = (char)reader.Read()) == '*') && ((char)reader.Peek() == '/') ) )
								whitespace += character;
							// Add the '*'
							whitespace += character;
							// Throw an exception if we've hit the end of the file.
							if( reader.Peek() == -1 )
							{
								LogFile.AddErrorLine("Unexpected end of file while parsing ICU locale file");
								LogFile.AddErrorLine("Last token and whitespace: " + token + ":" + whitespace);
								throw new InstallLanguage.Errors.LDExceptions(
									InstallLanguage.Errors.ErrorCodes.ICUDataParsingError);
							}
							// Add the '/'
							whitespace += (char)reader.Read();
						}
						else
						{
							LogFile.AddErrorLine("Unexpected '/'.  This should only be used in comments or " +
								"embedded in a string: e.g. \"blah/blah\"");
							LogFile.AddErrorLine("Last token and whitespace: " + token + ":" + whitespace);
							throw new InstallLanguage.Errors.LDExceptions(
								InstallLanguage.Errors.ErrorCodes.ICUDataParsingError);
						}
					}
						// Return, because this will be parsed next time, it is not part of this token
					else
						return;
				}
				else if ( character == '"' )
				{
					if( !readingToken )
					{
						token += (char)reader.Read();

						// Handle empty string
						if( reader.Peek() == '"' )
						{
							token += (char)reader.Read();
							return;
						}
						// Read until we find a '"' without a '\' before it
						while( (reader.Peek()!= -1 ) &&
							!( ((character = (char)reader.Read()) != '\\') && (reader.Peek() == '"' )))
						{
							if (System.Environment.NewLine.IndexOf(character) != -1)
							{
								;	// just eat the newline characters
							}
							token += character;
						}
						// add the character before the '"'
						token += character;
						// Throw an exception if we've hit the end of the file.
						if( reader.Peek() == -1 )
						{
							LogFile.AddErrorLine("Unexpected end of file while parsing ICU locale file");
							LogFile.AddErrorLine("Last token and whitespace: " + token + ":" + whitespace);
							throw new InstallLanguage.Errors.LDExceptions(
								InstallLanguage.Errors.ErrorCodes.ICUDataParsingError);
						}
						// Add the '"'
						token += (char)reader.Read();
						return;
					}
					else
					{
						LogFile.AddErrorLine("Unexpected '\"'.  quoted tokens must begin with the double quote. " +
							"e.g. \"blahblah\" NOT: blah\"blah\"");
						LogFile.AddErrorLine("Last token and whitespace: " + token + ":" + whitespace);
						throw new InstallLanguage.Errors.LDExceptions(
							InstallLanguage.Errors.ErrorCodes.ICUDataParsingError);
					}
				}
				else
				{
					if ( readingToken )
					{
						// If character is a speacial token character on its own we have found a complete token.
						foreach(char specialCharacter in specialTokenCharacters)
							if( character == specialCharacter )
								return;
						// Add the character to the token
						token += (char)reader.Read();
					}
					else
					{
						readingToken = true;
						token += (char)reader.Read();
						// If character is a speacial token character on its own we have found a complete token.
						foreach(char specialCharacter in specialTokenCharacters)
							if( character == specialCharacter )
								return;
					}
				}
			}
			// Throw an exception if we read the end of the file, because we should stop when we
			// find the root node's '}'
			if( reader.Peek() == -1 )
			{
				LogFile.AddErrorLine("Unexpected end of file while parsing ICU locale file");
				LogFile.AddErrorLine("Last token and whitespace: " + token + ":" + whitespace);
				throw new InstallLanguage.Errors.LDExceptions(
					InstallLanguage.Errors.ErrorCodes.ICUDataParsingError);
			}

		}

		/// <summary>
		/// Takes the given readers which begins on or before the name of the first child and parses children
		/// until it its ParseChildren attempt finds its own '}'
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="node"></param>
		private static void ParseChildren(TextReader reader, IcuDataNode node)
		{
			IcuDataNode childNode;

			// Keep parsing children until one of them finds our end '}'
			while(Parse(reader,out childNode,node)==false)
			{
				if(childNode != null)
				{
					node.AddChildSimple(childNode);
				}
			}
		}

		#endregion

		#region Writing

		/// <summary>
		/// Writes the node as it appears in the file.
		/// </summary>
		/// <param name="writer"></param>
		public void Write(TextWriter writer)
		{
			Write( writer, false);
		}

		/// <summary>
		/// Writes the node as it appears in the file.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="startsNewLine">Whether the node should begin on a new line</param>
		/// <returns><c>true</c> if the node ends with a new line.</returns>
		public bool Write(TextWriter writer, bool startsNewLine)
		{
			if( startsNewLine )
				writer.Write(Indent);
			// Write the comments, they include their own endlines, even the last one
			if( WriteSpace(preSpace,writer,"") )
				writer.Write(Indent);
			// Write the opening line with the name and any needed comments
			writer.Write(name);
			if( WriteSpace(afterNameSpace,writer,"") )
				writer.Write(Indent);
			writer.Write("{");
			startsNewLine = WriteSpace(braceSpace,writer,tab);
			// Don't write the new line if the attribute data starts on the same line
			// Write out the children
			foreach( IcuDataNode child in this.children )
			{
				if( startsNewLine )
					writer.Write(Indent + tab);
				startsNewLine = child.Write(writer, startsNewLine);
			}
			// loop through the attribues
			foreach( IcuDataAttribute attribute in attributes )
			{
				startsNewLine = attribute.Write(writer,startsNewLine);
			}
			// Write the closing "}"
			if( startsNewLine )
				writer.Write(Indent);
			writer.Write("}");

			return WriteSpace(postSpace,writer,"");
		}

		/// <summary>
		/// Write out the space correctly (indenting etc.)
		/// </summary>
		/// <param name="space">The underlying array of string that represent the space.</param>
		/// <param name="writer">The writer to write to.</param>
		/// <returns><c>true</c> if the string ended with a new line,
		/// so the next line can indent itsself properly.</returns>
		private bool WriteSpace(string [] space, TextWriter writer, string extraTab)
		{
			// Write each line, indenting all but the first
			bool firstTime = true;
			foreach( string line in space )
			{
				writer.Write((firstTime?"":Indent + extraTab) + line);
				firstTime = false;
			}
			// If the string ends with a newline return true,
			// so that whoever follows can indent itself however much it wants.
			// Note: space will always have at least one element, containing an empty string
			string lastLine = space[space.Length-1];
			int index = lastLine.LastIndexOf(Environment.NewLine);
			if( (index != -1) && (index == lastLine.Length - Environment.NewLine.Length) )
				return true;
			else
				return false;
		}
		#endregion

		#region test methods
		/// <summary>
		/// Modifies the locales/root.txt file, making a backup (_ORIGINAL) if one doesn't exist.
		/// It reads the file into a structured hierarchy of ICUDataNodes, then writes the file back out.
		/// This should leave the file entirely unmodified.
		/// This does not check to see if the file is unmodified, that is left to the tests.
		///
		/// (This is used by a unit test in InstallLanguageTests)
		/// </summary>
		/// <param name="icuDataFile">The name of the icu data file in data\locales\ without the ".txt"</param>
		public static void TestIcuDataNode(string icuDataFile)
		{
			// Set up our readers and writers to work from the original into a copy
			string root = Generic.GetIcuDir() + @"data\locales\" + icuDataFile + ".txt";
			string tempRoot = Generic.CreateTempFile(root);
			Generic.BackupOrig(root);
			// Read in and parse the file
			IcuDataNode dataNode;
			StreamReader reader  = new StreamReader(root,
				System.Text.Encoding.Default, true);	// Check for Unicode BOM chars.
			Parse(reader, out dataNode);
			StreamWriter writer = new StreamWriter(tempRoot);
			// Write to the file
			dataNode.Write(writer);
			// Copy the file over the original and close
			writer.Flush();
			writer.Close();
			reader.Close();
			Generic.SafeFileCopyWithLogging(tempRoot,root,true);
			Generic.SafeDeleteFile(tempRoot);
		}
		/// <summary>
		/// Test how well we are breaking it into tokens by separating and switching them.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="reader"></param>
		private static void TestTokens(TextWriter writer, TextReader reader)
		{
			try
			{
				string whitespace;
				string token;
				while(true)
				{
					ParseToken(reader, out whitespace, out token);
					writer.Write(whitespace + "#" + token + "|");
				}
			}
			catch( InstallLanguage.Errors.LDExceptions ignored )
			{
				ignored.GetType();
			}
		}

		#endregion

		/// <summary>
		/// Represents a single data element of the icu file
		/// </summary>
		public class IcuDataAttribute
		{
			/// <summary>
			/// Makes a new IcuDataAttribute with a string value.
			/// Note that this is also used by the parser to make numeric properties
			///		by turning off the <c>ensureQuotes</c>
			/// </summary>
			/// <param name="dataValue">AttributeValue</param>
			/// <param name="postSpace">Space following the AttributeValue</param>
			/// <param name="ensureQuotes">This ensures the attribute will always be surrounded by double quotes.</param>
			public IcuDataAttribute(string dataValue, string postSpace, bool ensureQuotes)
			{
				//Checks for double quotes and adds them if needed
				// (for parsing of the file check to be sure that they want the quotes added)
				if( !ensureQuotes ||
					(dataValue.Length > 0 && dataValue[0] == '"' && dataValue[dataValue.Length-1] == '"' ) )
					this.Value = dataValue;
				else
					this.StringValue = dataValue;
				this.PostSpace = postSpace;
			}

			/// <summary>
			/// Constructs an IcuDataAttribute with a numeric value.
			/// </summary>
			/// <param name="dataValue"></param>
			/// <param name="postSpace"></param>
			public IcuDataAttribute(int dataValue, string postSpace)
			{
				this.NumericValue = dataValue;
				this.PostSpace = postSpace;
			}

			private string dataValue = "";
			private string[] postSpace = {""};
			private IcuDataNode containingNode = null;


			/// <summary>
			/// The value of the data, as it appears in the file.
			/// </summary>
			public string Value
			{
				get{ return dataValue; }
				set{ dataValue = value; }
			}

			/// <summary>
			/// Represents the "string" value, without the quotes.
			///
			/// Returns null if the value is not a "string" in the dataFile.
			/// That is, if the value is not surrounded by quotes ("") is it probably a numeric value
			/// and this will not return that value
			/// </summary>
			public string StringValue
			{
				get
				{
					// Don't try to return numeric values this way.
					if(dataValue.Length < 1 || dataValue[0] != '"' || dataValue[dataValue.Length-1] != '"' )
					{
						return null;
					}
					// trim off the quotes right on the ends
					return dataValue.Substring(1,dataValue.Length-2);
				}
				set
				{
					dataValue = "\"" + value + "\"";
				}
			}

			/// <summary>
			/// The numeric value of the attribute.
			/// This will throw a FormatException if the value is actually a string value
			/// </summary>
			public int NumericValue
			{
				get
				{
					return Int32.Parse(dataValue);
				}
				set
				{
					dataValue = value.ToString();
				}
			}

			/// <summary>
			/// The spacing that follows the data, including the next line.
			/// </summary>
			public string PostSpace
			{
				get{ return GetSpace(postSpace); }
				set{ postSpace = SetSpace(value);}
			}
			/// <summary>
			/// The node that contains this attribute
			/// </summary>
			public IcuDataNode ContainingNode
			{
				get { return containingNode; }
				set { containingNode = value; }
			}

			/// <summary>
			/// Writes this attribute
			/// </summary>
			/// <param name="writer">Writer to write ICU datafile style attributes to.</param>
			/// <param name="startsNewLine">Indicates if the first thing written is on it's own line.</param>
			/// <param name="node">The node that this is contained in, so that</param>
			/// <returns></returns>
			public bool Write(TextWriter writer, bool startsNewLine)
			{
				if( startsNewLine )
					writer.Write( containingNode.Indent + tab );
				writer.Write(dataValue);
				return containingNode.WriteSpace(postSpace,writer,tab);
			}
		}
	}
	/// <summary>
	/// Manages ICU data locale files so that they are opened and parsed only once.
	///
	/// When any of the access functions are called, if the associated file has not yet been opened,
	/// it is opened, parsed, and stored internally for later use.
	///
	/// When all the accessing is done, call <c>WriteFiles</c> to write all the files that have been read and parsed.
	/// </summary>
	public class ICUDataFiles
	{
		#region storedFile access
		/// <summary>
		/// Stores files that have been opened as <c>IcuDataNode</c>s.
		/// </summary>
		private static Hashtable parsedFiles = new Hashtable();

		/// <summary>
		/// Opens the file or finds the already opened one in the hashtable.
		///
		/// No attempt is made to be thread safe.
		/// </summary>
		/// <param name="filename">The complete path of the file to open</param>
		/// <returns>The IcuDataNode, which may already have been opened.</returns>
		public static IcuDataNode ParsedFile( string filename )
		{
			if( parsedFiles.ContainsKey( filename ) )
				return (IcuDataNode)parsedFiles[ filename ];
			else
			{
				// Create a backup of the original file
				Generic.BackupOrig(filename);
				// Read in and parse the file
				IcuDataNode newFileRoot;
				LogFile.AddVerboseLine("StreamReader on <" + filename + ">");
				StreamReader reader  = new StreamReader(filename,
					System.Text.Encoding.Default, true);	// Check for Unicode BOM chars.
				IcuDataNode.Parse(reader, out newFileRoot);
				// Add the file to the root so that it doesn't need to be used again.
				parsedFiles.Add(filename,newFileRoot);
				reader.Close();
				return newFileRoot;
			}
		}

		#endregion

		/// <summary>
		/// Finds whether node contains the value "value"
		/// Actually "value in this case is a sub-node
		/// </summary>
		/// <param name="file">The full file pathname of the file to check.</param>
		/// <param name="nodeSpec">The node we wish to check in the file.</param>
		/// <param name="attributeValue">An attribute that should be in the given node.</param>
		/// <returns>The <c>InstallLanguage.LocaleFileClass.eAction</c> to perform on this node</returns>
		public static bool AttributeExists(string file,
			NodeSpecification nodeSpec, String attributeValue)
		{
			IcuDataNode specifiedNode = nodeSpec.FindNode(ParsedFile(file),false);
			if( specifiedNode == null )
				return false;
			return specifiedNode.containsAttribute(attributeValue);
		}

		/// <summary>
		/// Finds whether node contains the value "value"
		/// Actually "value in this case is a sub-node
		/// </summary>
		/// <param name="file">The full file pathname of the file to check.</param>
		/// <param name="nodeSpec">The node we wish to check in the file.</param>
		/// <returns>The <c>InstallLanguage.LocaleFileClass.eAction</c> to perform on this node</returns>
		public static bool NodeExists(string file,
			NodeSpecification nodeSpec)
		{
			IcuDataNode specifiedNode = nodeSpec.FindNode(ParsedFile(file),false);
			if( specifiedNode == null )
				return false;
			return true;
		}

		/// <summary>
		/// Finds and sets the childNode, inserting or replacing as necessary.
		/// </summary>
		/// <param name="file">The complete file path to open</param>
		/// <param name="nodeSpec">The path to the node that will have a new child.</param>
		/// <param name="newChild">The new child to add, replacing existing children as necessary.</param>
		public static void SetICUDataFileNode(string file, NodeSpecification nodeSpec,
			IcuDataNode newChild, bool addAtTop)
		{
			// Get the node they chose
			IcuDataNode chosenNode = nodeSpec.FindNode( ParsedFile(file), false );
			// Add the child
			if (chosenNode != null)
				chosenNode.AddChildSmart(newChild, addAtTop);
		}


		public static int RemoveICUDataFileAttribute(string file, NodeSpecification nodeSpec, string attributeValue)
		{
			IcuDataNode specifiedNode = nodeSpec.FindNode(ParsedFile(file),false);
			if( specifiedNode == null )
				return -1;
			return specifiedNode.removeAttribute(attributeValue);
		}

		public static int RemoveICUDataFileAttribute(IcuDataNode rootNode, NodeSpecification nodeSpec, string attributeValue)
		{
			IcuDataNode specifiedNode = nodeSpec.FindNode(rootNode,false);
			if( specifiedNode == null )
				return -1;
			return specifiedNode.removeAttribute(attributeValue);
		}

		public static bool RemoveICUDataFileChild(IcuDataNode rootNode, NodeSpecification nodeSpec, string childName)
		{
			IcuDataNode specifiedNode = nodeSpec.FindNode(rootNode,false);
			if( specifiedNode == null )
				return false;
			return specifiedNode.removeChild(childName);
		}


		/// <summary>
		/// Finds and sets the value, inserting or replacing as necessary
		/// </summary>
		/// <param name="file">The complete file path to open</param>
		/// <param name="nodeSpec">The path to the node that will have a new child.</param>
		/// <param name="attributeValue">The attribute to add or replace.</param>
		/// <param name="comment">The text of the comment (not including the //)</param>
		public static void SetICUDataFileAttribute(string file, NodeSpecification nodeSpec,
			String attributeValue, string comment)
		{
			// Get the node they chose
			IcuDataNode chosenNode = nodeSpec.FindNode( ParsedFile(file), true );
			// Add the attribute
			chosenNode.AddAttributeSmart(
				new IcuDataNode.IcuDataAttribute(attributeValue, ", //" + comment + Environment.NewLine, true));
		}

		/// <summary>
		/// Writes out all the files that have been modified
		/// </summary>
		public static void WriteFiles()
		{
			foreach( string filename in parsedFiles.Keys)
				WriteFile(filename);
		}

		/// <summary>
		/// Write a single ICU data file to a new name using a backup
		/// </summary>
		/// <param name="origName">filename used originally</param>
		/// <param name="newName">new output file name</param>
		public static void WriteFileAs(string origName, string newName)
		{
			// default to UTF-8 output.
			System.Text.Encoding enc = System.Text.Encoding.UTF8;
			// Create a temporary file, in case there is an error while writing.
			string tempFilename = Generic.CreateNewFileName(newName,"_ICUTEMP");
			StreamWriter writer = new StreamWriter(tempFilename, false, enc);

			// Get the root node to write the file.
			IcuDataNode fileRoot = ParsedFile( origName );

			// Write to the file
			fileRoot.Write(writer);

			// Copy the file over the original and close
			writer.Close();
			Generic.FileCopyWithLogging(tempFilename,newName,true);
			Generic.DeleteFile(tempFilename);
			parsedFiles.Remove(origName);	// so that it doesn't get put out later
		}

		/// <summary>
		/// Writes a single ICU data file using a temporary backup
		/// </summary>
		/// <param name="filename"></param>
		public static void WriteFile( string filename )
		{
			// default to UTF-8 output.
			System.Text.Encoding enc = System.Text.Encoding.UTF8;
			// Create a temporary file, in case there is an error while writing.
			string tempFilename = Generic.CreateNewFileName(filename,"_ICUTEMP");
			StreamWriter writer = new StreamWriter(tempFilename, false, enc);

			// Get the root node to write the file.
			IcuDataNode fileRoot = ParsedFile( filename );

			// Write to the file
			fileRoot.Write(writer);

			// Copy the file over the original and close
			writer.Close();
			Generic.FileCopyWithLogging(tempFilename,filename,true);
			Generic.DeleteFile(tempFilename);
		}

		/// <summary>
		/// Reset the internal storage so that we can start over with a clean slate of files.
		/// This is needed to handle collation files separately from locale files.
		/// </summary>
		public static void Reset()
		{
			parsedFiles.Clear();
		}
	}

	/// <summary>
	/// Specifies a single node in a file.
	/// </summary>
	public class NodeSpecification
	{
		private string[] nodePath;
		/// <summary>
		/// Specifies the path to a given node.
		/// </summary>
		/// <example>
		/// <code>
		/// SpecifyNode( "root","Languages" )
		/// </code>
		/// returns the <c>Languages</c> node that is a child of the root node.
		/// </example>
		/// <param name="nodes"></param>
		public NodeSpecification( params string[] nodes )
		{
			nodePath = nodes;
		}

		/// <summary>
		/// Find the specified node in the given root.
		/// </summary>
		/// <param name="root">The root node to search, must match the first word in the specification.</param>
		/// <param name="mustExist">
		///  If this is <c>true</c>,
		///		then throw an exception if the path doesn't specify and existing node.
		///  If this is <c>false</c>,
		///		the IcuDataNode returned will be <c>null</c> if none is found.</param>
		/// <returns></returns>
		public IcuDataNode FindNode( IcuDataNode root, bool mustExist)
		{
			// The root node's name must match the given root node.
			if( root.Name != nodePath[0] )
			{
				if( mustExist == true )
				{
					LogFile.AddErrorLine("Error finding node: root name does not match requested name");
					throw new InstallLanguage.Errors.LDExceptions(
						InstallLanguage.Errors.ErrorCodes.ICUNodeAccessError);
				}
				else
					return null;
			}
			IcuDataNode currentNode = root;
			// Find each node in the path, assuming that it is the child of the previous node.
			for( int index = 1; index < nodePath.Length; index++)
			{
				currentNode = currentNode.Child(((string)nodePath[index]));
				// If the node is not a child as expected throw an exception.
				if( currentNode == null )
				{
					if( mustExist )
					{
						// Log a detailed description and exit
						LogFile.AddErrorLine("Error finding node: " + ToString());
						LogFile.AddErrorLine("Node does not exists: " + nodePath[index]);
						throw new InstallLanguage.Errors.LDExceptions(
							InstallLanguage.Errors.ErrorCodes.ICUNodeAccessError);
					}
					else
						return null;
				}
			}
			return currentNode;
		}

		/// <summary>
		/// Returns the path of nodes as a human readable string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{

			String nodePathAsSingleString = "";
			foreach(string nodeName in nodePath)
			{
				nodePathAsSingleString += nodeName + ".";
			}
			return nodePathAsSingleString;
		}
	}

}
