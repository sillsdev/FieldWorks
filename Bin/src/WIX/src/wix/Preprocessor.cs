//-------------------------------------------------------------------------------------------------
// <copyright file="Preprocessor.cs" company="Microsoft">
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
//     Preprocessor for WiX v2.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Preprocessor object
	/// </summary>
	public class Preprocessor : IExtensionMessageHandler
	{
		private ExtensionMessages extensionMessages;
		private Hashtable variables;
		private StringCollection includeSearchPaths;
		private Hashtable extensionTypes;

		private bool currentLineNumberWritten;
		private SourceLineNumber currentLineNumber;
		private Stack sourceStack;

		private bool foundError;
		private TextWriter preprocessOut;

		private Stack includeNextStack;

		/// <summary>
		/// Creates a new preprocesor.
		/// </summary>
		public Preprocessor()
		{
			this.extensionMessages = new ExtensionMessages(this);
			this.variables = new Hashtable();
			this.includeSearchPaths = new StringCollection();
			this.extensionTypes = new Hashtable();

			this.currentLineNumber = null;
			this.sourceStack = new Stack();

			this.includeNextStack = new Stack();

			this.foundError = false;
			this.preprocessOut = null;

			// add the system variables
			this.variables.Add("sys.CURRENTDIR", String.Concat(Directory.GetCurrentDirectory(), "\\"));
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// Enumeration for preprocessor operations in if statements.
		/// </summary>
		private enum PreprocessorOperation
		{
			/// <summary>The and operator.</summary>
			And,

			/// <summary>The or operator.</summary>
			Or,

			/// <summary>The not operator.</summary>
			Not
		}

		/// <summary>
		/// The line number info processing instruction name.
		/// </summary>
		/// <value>String for line number info processing instruction name.</value>
		public static string LineNumberElementName
		{
			get { return "ln"; }
		}

		/// <summary>
		/// Returns the hash table for parameters.
		/// </summary>
		/// <value>Hashtable of parameters used during precompiling.</value>
		public Hashtable Parameters
		{
			get { return this.variables; }
		}

		/// <summary>
		/// Ordered list of search paths that the precompiler uses to find included files.
		/// </summary>
		/// <value>ArrayList of ordered search paths to use during precompiling.</value>
		public StringCollection IncludeSearchPaths
		{
			get { return this.includeSearchPaths; }
		}

		/// <summary>
		/// Specifies the text stream to display the postprocessed data to.
		/// </summary>
		/// <value>TextWriter to write preprocessed xml to.</value>
		public TextWriter PreprocessOut
		{
			get { return this.preprocessOut; }
			set { this.preprocessOut = value; }
		}

		/// <summary>
		/// Gets and sets the variables hashtable for the preprocessor.
		/// </summary>
		/// <value>The hashtable to use for variable lookups.</value>
		public Hashtable Variables
		{
			get { return this.variables; }
			set
			{
				this.variables = value;

				foreach (PreprocessorExtension extension in this.extensionTypes.Values)
				{
					extension.Variables = this.variables;
				}
			}
		}

		/// <summary>
		/// Returns the value of a parameter in the hash table.
		/// </summary>
		/// <param name="parameterName">The parameter to set for the precompiler.</param>
		public string this[string parameterName]
		{
			get { return (string)this.variables[parameterName]; }
			set { this.variables[parameterName] = value; }
		}

		/// <summary>
		/// Resets the parameters hashtable to only include the system variables.
		/// </summary>
		public void ResetParameters()
		{
			Hashtable resetVariables = new Hashtable();
			foreach (string param in this.variables.Keys)
			{
				if (param.StartsWith("sys"))
				{
					resetVariables.Add(param, this.variables[param]);
				}
			}

			this.Variables = resetVariables;
		}

		/// <summary>
		/// Adds an extension to the preprocessor.
		/// </summary>
		/// <param name="extension">preprocessor extension to add to preprocessor.</param>
		public void AddExtension(PreprocessorExtension extension)
		{
			extension.Messages = this.extensionMessages;

			// check if this extension is adding an extension type that already exists
			if (this.extensionTypes.Contains(extension.Type))
			{
				throw new WixExtensionTypeConflictException(extension, (PreprocessorExtension)this.extensionTypes[extension.Type]);
			}

			this.extensionTypes.Add(extension.Type, extension);
		}

		/// <summary>
		/// Preprocesses a file.
		/// </summary>
		/// <param name="sourcePath">Path to the file to preprocess.</param>
		/// <returns>XmlDocument with the postprocessed data validated by any schemas set in the preprocessor.</returns>
		public XmlDocument Process(string sourcePath)
		{
			FileInfo sourceFile = new FileInfo(sourcePath);
			StringWriter processed = new StringWriter();

			this.currentLineNumberWritten = false;
			this.currentLineNumber = new SourceLineNumber(sourceFile.FullName);
			this.foundError = false;

			// add the current source file and current source path to the system variables
			this.variables["sys.SOURCEFILEPATH"] = sourceFile.FullName;
			this.variables["sys.SOURCEFILEDIR"] = String.Concat(sourceFile.DirectoryName, "\\");

			// open the source file for processing
			using (Stream sourceStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				XmlReader reader = new XmlTextReader(sourceStream);
				XmlTextWriter writer = new XmlTextWriter(processed);
				writer.Formatting = Formatting.Indented;

				// process the reader into the writer
				try
				{
					foreach (PreprocessorExtension extension in this.extensionTypes.Values)
					{
						extension.Variables = this.variables;
						extension.Messages = this.extensionMessages;
						extension.InitializePreprocess();
					}
					this.PreprocessReader(false, reader, writer, 0);
				}
				catch (XmlException e)
				{
					this.UpdateInformation(reader, 0);
					throw new WixInvalidXmlException(this.GetCurrentSourceLineNumbers(), e);
				}
				writer.Close();
			}

			foreach (PreprocessorExtension extension in this.extensionTypes.Values)
			{
				processed = extension.PreprocessDocument(processed);
			}

			// do not continue processing if an error was encountered in one of the extensions
			if (this.foundError)
			{
				return null;
			}

			if (this.preprocessOut != null)
			{
				this.preprocessOut.WriteLine(processed.ToString());
				this.preprocessOut.Flush();
			}

			// create an XML Document from the post-processed memory stream
			XmlDocument sourceDocument = new XmlDocument();
			using (StringReader reader = new StringReader(processed.ToString()))
			{
				try
				{
					sourceDocument.Load(reader);
				}
				catch (XmlException)
				{
					this.OnMessage(WixErrors.SP1ProbablyNotInstalled());
				}
			}

			return (this.foundError ? null : sourceDocument);
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnExtensionError(SourceLineNumberCollection sourceLineNumbers, ErrorLevel errorLevel, string errorMessage)
		{
			this.OnMessage(WixErrors.PreprocessorExtensionError(sourceLineNumbers, errorLevel, errorMessage));
		}

		/// <summary>
		/// Sends a warning to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnExtensionWarning(SourceLineNumberCollection sourceLineNumbers, WarningLevel warningLevel, string warningMessage)
		{
			this.OnMessage(WixWarnings.PreprocessorExtensionWarning(sourceLineNumbers, warningLevel, warningMessage));
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnExtensionVerbose(SourceLineNumberCollection sourceLineNumbers, VerboseLevel verboseLevel, string verboseMessage)
		{
			this.OnMessage(WixVerboses.PreprocessorExtensionVerbose(sourceLineNumbers, verboseLevel, verboseMessage));
		}

		/// <summary>
		/// Determines whether a variable has a standard prefix (var, env or sys).
		/// </summary>
		/// <param name="variableName">Name of the variable to check for a prefix.</param>
		/// <returns>True if the variable has a standard prefix, false otherwise.</returns>
		private static bool HasStandardPrefix(string variableName)
		{
			return variableName.StartsWith("var.") || variableName.StartsWith("env.") || variableName.StartsWith("sys.");
		}

		/// <summary>
		/// Processes an xml reader into an xml writer.
		/// </summary>
		/// <param name="include">Specifies if reader is from an included file.</param>
		/// <param name="reader">Reader for the source document.</param>
		/// <param name="writer">Writer where postprocessed data is written.</param>
		/// <param name="offset">Original offset for the line numbers being processed.</param>
		private void PreprocessReader(bool include, XmlReader reader, XmlWriter writer, int offset)
		{
			Stack stack = new Stack(5);
			IfContext context = new IfContext(true, true, IfState.Unknown); // start by assuming we want to keep the nodes in the source code

			// process the reader into the writer
			while (reader.Read())
			{
				// update information here in case an error occurs before the next read
				this.UpdateInformation(reader, offset);

				// check for changes in conditional processing
				if (XmlNodeType.ProcessingInstruction == reader.NodeType)
				{
					bool ignore = false;
					string name = null;
					int index = 0;

					switch (reader.LocalName)
					{
						case "if":
							stack.Push(context);
							context = new IfContext(context.IsTrue & context.Active, this.EvaluateExpression(reader.Value), IfState.If);
							ignore = true;
							break;
						case "ifdef":
							stack.Push(context);
							name = reader.Value.Trim();
							index = name.IndexOf('.');
							if (0 > index || (!Preprocessor.HasStandardPrefix(name) && null == this.FindExtension(name.Substring(0, index))))
							{
								name = String.Concat("var.", name);
							}
							context = new IfContext(context.IsTrue & context.Active, this.IsVariableDefined(name), IfState.If);
							ignore = true;
							break;
						case "ifndef":
							stack.Push(context);
							name = reader.Value.Trim();
							index = name.IndexOf('.');
							if (0 > index || (!Preprocessor.HasStandardPrefix(name) && null == this.FindExtension(name.Substring(0, index))))
							{
								name = String.Concat("var.", name);
							}
							context = new IfContext(context.IsTrue & context.Active, !this.IsVariableDefined(name), IfState.If);
							ignore = true;
							break;
						case "elseif":
							if (0 == stack.Count)
							{
								throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Missing <?if?> for <?elseif?>");
							}

							if (IfState.If != context.IfState && IfState.ElseIf != context.IfState)
							{
								throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Specified <?elseif?> without matching <?if?>");
							}

							context.IfState = IfState.ElseIf;   // we're now in an elseif
							if (!context.WasEverTrue)   // if we've never evaluated the if context to true, then we can try this test
							{
								context.IsTrue = this.EvaluateExpression(reader.Value);
							}
							else if (context.IsTrue)
							{
								context.IsTrue = false;
							}
							ignore = true;
							break;
						case "else":
							if (0 == stack.Count)
							{
								throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Missing <?if?> for <?else?>");
							}

							if (IfState.If != context.IfState && IfState.ElseIf != context.IfState)
							{
								throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Specified <?else?> without matching <?if?>");
							}

							context.IfState = IfState.Else;   // we're now in an else
							context.IsTrue = !context.WasEverTrue;   // if we were never true, we can be true now
							ignore = true;
							break;
						case "endif":
							if (0 == stack.Count)
							{
								throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Missing <?if?> for <?endif?>");
							}

							context = (IfContext)stack.Pop();
							ignore = true;
							break;
					}

					if (ignore)   // ignore this node since we just handled it above
					{
						continue;
					}
				}

				if (!context.Active || !context.IsTrue)   // if our context is not true then skip the rest of the processing and just read the next thing
				{
					continue;
				}

				switch (reader.NodeType)
				{
					case XmlNodeType.ProcessingInstruction:
						switch (reader.LocalName)
						{
							case "define":
								this.PreprocessDefine(reader.Value);
								break;
							case "undef":
								this.PreprocessUndef(reader.Value);
								break;
							case "include":
								this.UpdateInformation(reader, offset);
								this.PreprocessInclude(reader.Value, writer);
								break;
							case "foreach":
								this.PreprocessForeach(reader, writer, offset);
								break;
							case "endforeach": // endforeach is handled in PreprocessForeach, so seeing it here is an error
								throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Cannot have a <?endforeach?> processing instruction without a matching <?foreach?>.");
							default:
								// Console.WriteLine("processing instruction: {0}, {1}", reader.Name, reader.Value);
								break;
						}
						break;
					case XmlNodeType.Element:
						bool empty = reader.IsEmptyElement;

						if (0 < this.includeNextStack.Count && (bool)this.includeNextStack.Peek())
						{
							if (reader.Name != "Include")
							{
								throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Included files must have <Include /> document element.");
							}

							this.includeNextStack.Pop();
							this.includeNextStack.Push(false);
							break;
						}

						// output any necessary preprocessor processing instructions then write the start of the element
						this.WriteProcessingInstruction(reader, writer, offset);
						writer.WriteStartElement(reader.Name);

						while (reader.MoveToNextAttribute())
						{
							string value = this.PreprocessVariables(reader.Value);
							writer.WriteAttributeString(reader.Name, value);
						}

						if (empty)
						{
							writer.WriteEndElement();
						}
						break;
					case XmlNodeType.EndElement:
						if (0 < reader.Depth || !include)
						{
							writer.WriteEndElement();
						}
						break;
					case XmlNodeType.Text:
					case XmlNodeType.CDATA:
						string postprocessedValue = this.PreprocessVariables(reader.Value);
						writer.WriteCData(postprocessedValue);
						break;
					default:
						//Console.WriteLine("processing instruction: {0}, {1}", reader.Name, reader.Value);
						break;
				}
			}

			if (0 != stack.Count)
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Missing <?endif?> for <?if?>");
			}
		}

		/// <summary>
		/// Replaces parameters in the source text.
		/// </summary>
		/// <param name="value">Text that may contain parameters to replace.</param>
		/// <returns>Text after parameters have been replaced.</returns>
		private string PreprocessVariables(string value)
		{
			StringBuilder sb = new StringBuilder();
			int i = 0;
			int end = 0;

			while (-1 != (i = value.IndexOf('$', end)))
			{
				if (end < i)
				{
					sb.Append(value, end, i - end);
				}

				end = i + 1;
				string remainder = value.Substring(end);
				if (remainder.StartsWith("$"))
				{
					sb.Append("$");
					end++;
				}
				else if (remainder.StartsWith("(env."))
				{
					i = remainder.IndexOf(')');
					if (-1 == i)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Ill-formed environment variable in expression: '${0}'", remainder));
					}

					string variable = remainder.Substring(5, i - 5).Trim();
					string envVariable = Environment.GetEnvironmentVariable(variable);
					if (null == envVariable)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Undefined environment variable: $(env.{0}).", variable));
					}

					sb.Append(envVariable);

					end += i + 1;
				}
				else if (remainder.StartsWith("(var.") || remainder.StartsWith("(sys."))
				{
					i = remainder.IndexOf(')');
					if (-1 == i)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Ill-formed variable in expression: '${0}'", remainder));
					}

					string variable = remainder.Substring(1, i - 1).Trim();
					string varParameter = (string)this.variables[variable];
					if (null == varParameter)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Undefined variable: $({0}).", variable));
					}

					sb.Append(varParameter);

					end += i + 1;
				}
				else if (remainder.StartsWith("(loc."))
				{
					i = remainder.IndexOf(')');
					if (-1 == i)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Ill-formed resource variable in expression: '${0}'", remainder));
					}

					sb.Append("$");   // just put the resource reference back as was
					sb.Append(remainder, 0, i + 1);

					end += i + 1;
				}
				else if (remainder.StartsWith("("))
				{
					i = remainder.IndexOf(')');
					if (-1 == i)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Ill-formed variable in expression: '${0}'", remainder));
					}
					string parameterName = remainder.Substring(1, i - 1);
					string result = this.TryExtensionPreprocessParameter(parameterName);
					if (null == result)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Undefined parameter: $({0}).", parameterName));
					}
					sb.Append(result);
					end += i + 1;
				}
				else   // just a floating "$" so put it in the final string (i.e. leave it alone) and keep processing
				{
					sb.Append('$');
				}
			}

			if (end < value.Length)
			{
				sb.Append(value.Substring(end));
			}

			return sb.ToString();
		}

		/// <summary>
		/// Processes a define processing instruction and creates the appropriate parameter.
		/// </summary>
		/// <param name="originalDefine">Text from source.</param>
		private void PreprocessDefine(string originalDefine)
		{
			string defineName;
			string defineValue;
			string operation; //ignore
			string define = this.PreprocessVariables(originalDefine);
			this.GetNameValuePair(originalDefine, ref define, out defineName, out operation, out defineValue);
			if (operation.Length > 0 && operation != "=")
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Unexpected operator type in define: {0}", originalDefine));
			}

			if (define.Length > 0)
			{
				// if they used a space but not quotes, we need to append what's left over
				defineValue = String.Concat(defineValue, define);
			}

			int index = defineName.IndexOf('.');
			if (0 > index || (!defineName.StartsWith("var.") && null == this.FindExtension(defineName.Substring(0, index))))
			{
				defineName = String.Concat("var.", defineName);
			}

			// add the variable, but throw an exception if it already exists
			if (!this.variables.Contains(defineName))
			{
				this.variables.Add(defineName, defineValue);
			}
			else
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Cannot define previously defined user variable: '{0}'.", defineName));
			}
		}

		/// <summary>
		/// Processes an undef processing instruction and creates the appropriate parameter.
		/// </summary>
		/// <param name="originalDefine">Text from source.</param>
		private void PreprocessUndef(string originalDefine)
		{
			string variable = this.PreprocessVariables(originalDefine.Trim());
			int index = variable.IndexOf('.');
			if (0 > index || (!variable.StartsWith("var.") && null == this.FindExtension(variable.Substring(0, index))))
			{
				variable = String.Concat("var.", variable);
			}

			// remove the variable, but throw an exception if it does not exist
			if (this.variables.Contains(variable))
			{
				this.variables.Remove(variable);
			}
			else
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Cannot undef undefined user variable: '{0}'.", variable));
			}
		}

		/// <summary>
		/// Processes an included file.
		/// </summary>
		/// <param name="includePath">Path to included file.</param>
		/// <param name="writer">Writer to output postprocessed data to.</param>
		private void PreprocessInclude(string includePath, XmlWriter writer)
		{
			FileInfo includeFile = this.GetIncludeFile(includePath);

			if (null == includeFile)
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Could not find include file: {0}", includePath));
			}

			using (Stream includeStream = new FileStream(includeFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				XmlReader reader = new XmlTextReader(includeStream);

				this.PushInclude(includeFile.FullName);

				// process the included reader into the writer
				try
				{
					this.PreprocessReader(true, reader, writer, 0);
				}
				catch (XmlException e)
				{
					this.UpdateInformation(reader, 0);
					throw new WixInvalidXmlException(this.GetCurrentSourceLineNumbers(), e);
				}

				this.PopInclude();
			}
		}

		/// <summary>
		/// Preprocess a foreach processing instruction.
		/// </summary>
		/// <param name="reader">The xml reader.</param>
		/// <param name="writer">The xml writer.</param>
		/// <param name="offset">Offset for the line numbers.</param>
		private void PreprocessForeach(XmlReader reader, XmlWriter writer, int offset)
		{
			// find the "in" token
			int indexOfInToken = reader.Value.IndexOf(" in ");
			if (0 > indexOfInToken)
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "Error: The proper format for foreach is <?foreach varName in valueList?>.");
			}

			// parse out the variable name
			string varName = reader.Value.Substring(0, indexOfInToken).Trim();
			string varValuesString = reader.Value.Substring(indexOfInToken + 4).Trim();

			if (!this.TryExtensionIsForeachVariableAllowed(varName))
			{
				this.OnMessage(WixErrors.PreprocessorIllegalForeachVariable(this.GetCurrentSourceLineNumbers(), varName));
			}

			// preprocess the variable values string because it might be a variable itself
			varValuesString = this.PreprocessVariables(varValuesString);

			string[] varValues = varValuesString.Split(";".ToCharArray());

			// go through all the empty strings
			while (reader.Read() && XmlNodeType.Whitespace == reader.NodeType)
			{
			}

			// get the offset of this xml fragment (for some reason its always off by 1)
			if (reader is IXmlLineInfo)
			{
				offset += ((IXmlLineInfo)reader).LineNumber - 1;
			}

			// restrict the foreach to contain only one Fragment element or a nested foreach
			if ("Fragment" != reader.LocalName && "foreach" != reader.LocalName)
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Cannot nest {0} inside of <?foreach?>.  Foreach only supports direct nesting of a single Fragment element or nested foreach statements.", reader.Name));
			}

			// dump the Fragment xml to a string (maintaining whitespace if possible)
			if (reader is XmlTextReader)
			{
				((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.All;
			}

			string fragment = null;
			bool nestedForeach = false;
			if ("foreach" != reader.LocalName)
			{
				fragment = reader.ReadOuterXml();
			}
			else
			{
				StringBuilder fragmentBuilder = new StringBuilder();
				nestedForeach = true;
				int numForEach = 0; // Number of endforeach statements to look for
				while (reader.NodeType != XmlNodeType.Element)
				{
					if (reader.LocalName == "foreach")
					{
						numForEach++;

						// Output the foreach statement
						fragmentBuilder.AppendFormat("<?foreach {0}?>", reader.Value);
					}
					else
					{
						// Or output the whitespace
						fragmentBuilder.Append(reader.Value);
					}

					reader.Read();
				}

				// We can only have a foreach element under the foreach. This is the center of the nesting.
				if ("Fragment" != reader.LocalName)
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Cannot nest {0} inside of <?foreach?>.  Foreach only supports direct nesting of a single Fragment element or nested foreach statements.", reader.Name));
				}

				fragmentBuilder.Append(reader.ReadOuterXml());

				// Now consume the endforeach statements
				while (numForEach > 0)
				{
					// Grab any whitespace
					while (reader.NodeType == XmlNodeType.Whitespace)
					{
						fragmentBuilder.Append(reader.Value);
						reader.Read();
					}

					if ("endforeach" != reader.LocalName)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "<?endforeach?> not in the expected place: right after the end of the Fragment element.");
					}

					fragmentBuilder.Append("<?endforeach?>");
					reader.Read();
					numForEach--;
				}

				fragment = fragmentBuilder.ToString();
			}

			// store the previous value of the variable we are iterating through
			Hashtable oldVariables = (Hashtable)this.variables.Clone();

			// process each iteration, updating the variable's value each time
			for (long i = 0; i < varValues.Length; i++)
			{
				// squirrel the old variable context out of the way
				this.Variables = (Hashtable)oldVariables.Clone();

				int indexOfPeriod = varName.IndexOf('.');
				if (0 > indexOfPeriod || (!varName.StartsWith("var.") && null == this.FindExtension(varName.Substring(0, indexOfPeriod))))
				{
					varName = String.Concat("var.", varName);
				}

				if (!this.variables.Contains(varName))
				{
					this.variables.Add(varName, varValues[i]);
				}
				else
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Cannot define previously defined user variable: '{0}'.", varName));
				}

				XmlTextReader loopReader;
				if (!nestedForeach)
				{
					loopReader = new XmlTextReader(fragment, XmlNodeType.Element, new XmlParserContext(null, null, null, XmlSpace.Default));
				}
				else
				{
					loopReader = new XmlTextReader(fragment, XmlNodeType.Document, new XmlParserContext(null, null, null, XmlSpace.Default));
				}

				try
				{
					this.PreprocessReader(false, loopReader, writer, offset);
				}
				catch (XmlException e)
				{
					this.UpdateInformation(loopReader, offset);
					throw new WixInvalidXmlException(this.GetCurrentSourceLineNumbers(), e);
				}
			}

			// reset the previous value of the variable
			this.Variables = oldVariables;

			// go through all the empty strings
			while (reader.Read() && XmlNodeType.Whitespace == reader.NodeType)
			{
			}

			// check for the endforeach
			if ("endforeach" != reader.LocalName)
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), "<?endforeach?> not in the expected place: right after the end of the Fragment element.");
			}
		}

		/// <summary>
		/// Gets the next token in an expression.
		/// </summary>
		/// <param name="originalExpression">Expression to parse.</param>
		/// <param name="expression">Expression with token removed.</param>
		/// <param name="stringLiteral">Flag if token is a string literal instead of a variable.</param>
		/// <returns>Next token.</returns>
		private string GetNextToken(string originalExpression, ref string expression, out bool stringLiteral)
		{
			stringLiteral = false;
			string token = String.Empty;
			expression = expression.Trim();
			if (0 == expression.Length)
			{
				return String.Empty;
			}

			if (expression.StartsWith("\""))
			{
				stringLiteral = true;
				int endingQuotes = expression.IndexOf('\"', 1);
				if (-1 == endingQuotes)
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Quotes don't match in expression: {0}", originalExpression));
				}

				// cut the quotes off the string
				token = this.PreprocessVariables(expression.Substring(1, endingQuotes - 1));

				// advance past this string
				expression = expression.Substring(endingQuotes + 1).Trim();
			}
			else if (expression.StartsWith("$("))
			{
				//Find the end of the variable
				int endingParen = expression.IndexOf(')');
				if (-1 == endingParen)
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Unmatched or unexpected parenthesis in expression: {0}", originalExpression));
				}
				token = expression.Substring(0, endingParen + 1);

				//Advance past this variable
				expression = expression.Substring(endingParen + 1).Trim();
			}
			else
			{
				// Cut the token off at the next equal, space, inequality operator,
				// or end of string, whichever comes first
				int space             = expression.IndexOf(" ");
				int equals            = expression.IndexOf("=");
				int lessThan          = expression.IndexOf("<");
				int lessThanEquals    = expression.IndexOf("<=");
				int greaterThan       = expression.IndexOf(">");
				int greaterThanEquals = expression.IndexOf(">=");
				int notEquals         = expression.IndexOf("!=");
				int equalsNoCase      = expression.IndexOf("~=");
				int closingIndex;

				if (space == -1)
				{
					space = Int32.MaxValue;
				}

				if (equals == -1)
				{
					equals = Int32.MaxValue;
				}

				if (lessThan == -1)
				{
					lessThan = Int32.MaxValue;
				}

				if (lessThanEquals == -1)
				{
					lessThanEquals = Int32.MaxValue;
				}

				if (greaterThan == -1)
				{
					greaterThan = Int32.MaxValue;
				}

				if (greaterThanEquals == -1)
				{
					greaterThanEquals = Int32.MaxValue;
				}

				if (notEquals == -1)
				{
					notEquals = Int32.MaxValue;
				}

				if (equalsNoCase == -1)
				{
					equalsNoCase = Int32.MaxValue;
				}

				closingIndex = Math.Min(space, Math.Min(equals, Math.Min(lessThan, Math.Min(lessThanEquals, Math.Min(greaterThan, Math.Min(greaterThanEquals, Math.Min(equalsNoCase, notEquals)))))));

				if (Int32.MaxValue == closingIndex)
				{
					closingIndex = expression.Length;
				}

				//If the index is 0, we hit an operator, so return it
				if (0 == closingIndex)
				{
					//Length 2 operators
					if (closingIndex == lessThanEquals || closingIndex == greaterThanEquals || closingIndex == notEquals || closingIndex == equalsNoCase)
					{
						closingIndex = 2;
					}
					else //Length 1 operators
					{
						closingIndex = 1;
					}
				}

				//Cut out the new token
				token = expression.Substring(0, closingIndex).Trim();
				expression = expression.Substring(closingIndex).Trim();
			}

			return token;
		}

		/// <summary>
		/// Determins if string is an operator.
		/// </summary>
		/// <param name="operation">String to check.</param>
		/// <returns>true if string is an operator.</returns>
		private bool IsOperator(string operation)
		{
			if (operation == null)
			{
				return false;
			}

			operation = operation.Trim();
			if (0 == operation.Length)
			{
				return false;
			}

			if ("=" == operation ||
				"!=" == operation ||
				"<" == operation ||
				"<=" == operation ||
				">" == operation ||
				">=" == operation ||
				"~=" == operation)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets the value for a variable.
		/// </summary>
		/// <param name="originalExpression">Original expression for error message.</param>
		/// <param name="variable">Variable to evaluate.</param>
		/// <returns>Value of variable.</returns>
		private string EvaluateVariable(string originalExpression, string variable)
		{
			// By default it's a literal and will only be evaluated if it
			// matches the variable format
			string varValue = variable;

			if (variable.StartsWith("$("))
			{
				try
				{
					varValue = this.PreprocessVariables(variable);
				}
				catch (ArgumentNullException)
				{
					// non-existent variables are expected
					varValue = null;
				}
			}
			else if (variable.IndexOf("(") != -1 || variable.IndexOf(")") != -1)
			{
				// make sure it doesn't contain parenthesis
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Unmatched or unexpected parenthesis in expression: {0}", originalExpression));
			}
			else if (variable.IndexOf("\"") != -1)
			{
				// shouldn't contain quotes
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Quotes don't match in expression: {0}", originalExpression));
			}

			return varValue;
		}

		/// <summary>
		/// Gets the left side value, operator, and right side value of an expression.
		/// </summary>
		/// <param name="originalExpression">Original expression to evaluate.</param>
		/// <param name="expression">Expression modified while processing.</param>
		/// <param name="leftValue">Left side value from expression.</param>
		/// <param name="operation">Operation in expression.</param>
		/// <param name="rightValue">Right side value from expression.</param>
		private void GetNameValuePair(string originalExpression, ref string expression, out string leftValue, out string operation, out string rightValue)
		{
			bool stringLiteral;
			leftValue = this.GetNextToken(originalExpression, ref expression, out stringLiteral);

			//If it wasn't a string literal, evaluate it
			if (!stringLiteral)
			{
				leftValue = this.EvaluateVariable(originalExpression, leftValue);
			}

			//Get the operation
			operation = this.GetNextToken(originalExpression, ref expression, out stringLiteral);
			if (this.IsOperator(operation))
			{
				if (stringLiteral)
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Unexpected quotes around operator in expression: {0}", originalExpression));
				}

				rightValue = this.GetNextToken(originalExpression, ref expression, out stringLiteral);

				//If it wasn't a string literal, evaluate it
				if (!stringLiteral)
				{
					rightValue = this.EvaluateVariable(originalExpression, rightValue);
				}
			}
			else
			{
				//Prepend the token back on the expression since it wasn't an operator
				// and put the quotes back on the literal if necessary

				if (stringLiteral)
				{
					operation = "\"" + operation + "\"";
				}
				expression = (operation + " " + expression).Trim();

				//If no operator, just check for existence
				operation = "";
				rightValue = "";
			}
		}

		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <param name="originalExpression">Original expression to evaluate.</param>
		/// <param name="expression">Expression modified while processing.</param>
		/// <returns>true if expression evaluates to true.</returns>
		private bool EvaluateAtomicExpression(string originalExpression, ref string expression)
		{
			//Quick test to see if the first token is a variable
			bool startsWithVariable = expression.StartsWith("$(");

			string leftValue;
			string rightValue;
			string operation;
			this.GetNameValuePair(originalExpression, ref expression, out leftValue, out operation, out rightValue);

			bool expressionValue = false;

			//If the variables don't exist, they were evaluated to null
			if (null == leftValue || null == rightValue)
			{
				if (operation.Length > 0)
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Required variable missing in expression: {0}", originalExpression));
				}

				//false expression
			}
			else if (operation.Length == 0)
			{
				//There is no right side of the equation.
				// If the variable was evaluated, it exists, so the expression is true
				if (startsWithVariable)
				{
					expressionValue = true;
				}
				else
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Unexpected literal in expression: {0}", originalExpression));
				}
			}
			else
			{
				leftValue = leftValue.Trim();
				rightValue = rightValue.Trim();
				if ("=" == operation)
				{
					if (leftValue == rightValue)
					{
						expressionValue = true;
					}
				}
				else if ("!=" == operation)
				{
					if (leftValue != rightValue)
					{
						expressionValue = true;
					}
				}
				else if ("~=" == operation)
				{
					if (0 == String.Compare(leftValue, rightValue, true))
					{
						expressionValue = true;
					}
				}
				else
				{
					//Convert the numbers from strings
					int rightInt;
					int leftInt;
					try
					{
						rightInt = Int32.Parse(rightValue);
						leftInt = Int32.Parse(leftValue);
					}
					catch (FormatException)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Invalid number in expression: {0}", originalExpression));
					}

					//Compare the numbers
					if ("<" == operation && leftInt < rightInt ||
						"<=" == operation && leftInt <= rightInt ||
						">" == operation && leftInt > rightInt ||
						">=" == operation && leftInt >= rightInt)
					{
						expressionValue = true;
					}
				}
			}

			return expressionValue;
		}

		/// <summary>
		/// Determines if expression is currently inside quotes.
		/// </summary>
		/// <param name="expression">Expression to evaluate.</param>
		/// <param name="index">Index to start searching in expression.</param>
		/// <returns>true if expression is inside in quotes.</returns>
		private bool InsideQuotes(string expression, int index)
		{
			if (index == -1)
			{
				return false;
			}

			int numQuotes = 0;
			int tmpIndex = 0;
			while (-1 != (tmpIndex = expression.IndexOf('\"', tmpIndex, index - tmpIndex)))
			{
				numQuotes++;
				tmpIndex++;
			}

			// found an even number of quotes before the index, so we're not inside
			if (numQuotes % 2 == 0)
			{
				return false;
			}

			// found an odd number of quotes, so we are inside
			return true;
		}

		/// <summary>
		/// Gets a sub-expression in parenthesis.
		/// </summary>
		/// <param name="originalExpression">Original expression to evaluate.</param>
		/// <param name="expression">Expression modified while processing.</param>
		/// <param name="endSubExpression">Index of end of sub-expression.</param>
		/// <returns>Sub-expression in parenthesis.</returns>
		private string GetParenthesisExpression(string originalExpression, string expression, out int endSubExpression)
		{
			endSubExpression = 0;

			// if the expression doesn't start with parenthesis, leave it alone
			if (!expression.StartsWith("("))
			{
				return expression;
			}

			// search for the end of the expression with the matching paren
			int openParenIndex = 0;
			int closeParenIndex = 1;
			while (openParenIndex != -1 && openParenIndex < closeParenIndex)
			{
				closeParenIndex = expression.IndexOf(')', closeParenIndex);
				if (closeParenIndex == -1)
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Parenthesis do not match in: {0}", originalExpression));
				}

				if (this.InsideQuotes(expression, closeParenIndex))
				{
					// ignore stuff inside quotes (it's a string literal)
				}
				else
				{
					//Look to see if there is another open paren before the close paren
					//and skip over the open parens while they are in a string literal
					do
					{
						openParenIndex++;
						openParenIndex = expression.IndexOf('(', openParenIndex, closeParenIndex - openParenIndex);
					}
					while (this.InsideQuotes(expression, openParenIndex));
				}

				//Advance past the closing paren
				closeParenIndex++;
			}

			endSubExpression = closeParenIndex;

			//Return the expression minus the parenthesis
			return expression.Substring(1, closeParenIndex - 2);
		}

		/// <summary>
		/// Updates expression based on operation.
		/// </summary>
		/// <param name="currentValue">State to update.</param>
		/// <param name="operation">Operation to apply to current value.</param>
		/// <param name="prevResult">Previous result.</param>
		private void UpdateExpressionValue(ref bool currentValue, PreprocessorOperation operation, bool prevResult)
		{
			switch (operation)
			{
				case PreprocessorOperation.And:
					currentValue = currentValue && prevResult;
					break;
				case PreprocessorOperation.Or:
					currentValue = currentValue || prevResult;
					break;
				case PreprocessorOperation.Not:
					currentValue = !currentValue;
					break;
				default:
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Unexpected operator: {0}", operation.ToString()));
			}
		}

		/// <summary>
		/// Evaluate an expression.
		/// </summary>
		/// <param name="expression">Expression to evaluate.</param>
		/// <returns>Boolean result of expression.</returns>
		private bool EvaluateExpression(string expression)
		{
			string tmpExpression = expression;
			return this.EvaluateExpressionRecurse(expression, ref tmpExpression, PreprocessorOperation.And, true);
		}

		/// <summary>
		/// Recurse through the expression to evaluate if it is true or false.
		/// The expression is evaluated left to right.
		/// The expression is case-sensitive (converted to upper case) with the
		/// following exceptions: variable names and keywords (and, not, or).
		/// Comparisons with = and != are string comparisons.
		/// Comparisons with inequality operators must be done on valid integers.
		///
		/// The operator precedence is:
		///    ""
		///    ()
		///    &lt;, &gt;, &lt;=, &gt;=, =, !=
		///    Not
		///    And, Or
		///
		/// Valid expressions include:
		///   not $(var.B) or not $(var.C)
		///   (($(var.A))and $(var.B) ="2")or Not((($(var.C))) and $(var.A))
		///   (($(var.A)) and $(var.B) = " 3 ") or $(var.C)
		///   $(var.A) and $(var.C) = "3" or $(var.C) and $(var.D) = $(env.windir)
		///   $(var.A) and $(var.B)>2 or $(var.B) &lt;= 2
		///   $(var.A) != "2"
		/// </summary>
		/// <param name="originalExpression">The original expression</param>
		/// <param name="expression">The expression currently being evaluated</param>
		/// <param name="prevResultOperation">The operation to apply to this result</param>
		/// <param name="prevResult">The previous result to apply to this result</param>
		/// <returns>Boolean to indicate if the expression is true or false</returns>
		private bool EvaluateExpressionRecurse(string originalExpression,  ref string expression,  PreprocessorOperation prevResultOperation,  bool prevResult)
		{
			//Console.WriteLine("EvaluateExpression: " + expression);

			bool expressionValue = false;
			expression = expression.Trim();
			if (expression.Length == 0)
			{
				throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Unexpected empty subexpression in: {0}", originalExpression));
			}

			//If the expression starts with parenthesis, evaluate it
			if (expression.IndexOf('(') == 0)
			{
				int endSubExpressionIndex;
				string subExpression = this.GetParenthesisExpression(originalExpression, expression, out endSubExpressionIndex);
				expressionValue = this.EvaluateExpressionRecurse(originalExpression, ref subExpression, PreprocessorOperation.And, true);

				//Now get the rest of the expression that hasn't been evaluated
				expression = expression.Substring(endSubExpressionIndex).Trim();
			}
			else
			{
				//Check for NOT
				if (this.StartsWithKeyword(expression, PreprocessorOperation.Not))
				{
					expression = expression.Substring(3).Trim();
					if (expression.Length == 0)
					{
						throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Expecting argument for NOT in expression: {0}", originalExpression));
					}

					expressionValue = this.EvaluateExpressionRecurse(originalExpression, ref expression, PreprocessorOperation.Not, true);
				}
				else //Expect a literal
				{
					expressionValue = this.EvaluateAtomicExpression(originalExpression, ref expression);

					//Expect the literal that was just evaluated to already be cut off
				}
			}
			this.UpdateExpressionValue(ref expressionValue, prevResultOperation, prevResult);

			//If there's still an expression left, it must start with AND or OR.
			if (expression.Trim().Length > 0)
			{
				if (this.StartsWithKeyword(expression, PreprocessorOperation.And))
				{
					expression = expression.Substring(3);
					return this.EvaluateExpressionRecurse(originalExpression, ref expression, PreprocessorOperation.And, expressionValue);
				}
				else if (this.StartsWithKeyword(expression, PreprocessorOperation.Or))
				{
					expression = expression.Substring(2);
					return this.EvaluateExpressionRecurse(originalExpression, ref expression, PreprocessorOperation.Or, expressionValue);
				}
				else
				{
					throw new WixPreprocessorException(this.GetCurrentSourceLineNumbers(), String.Format("Invalid subexpression: {0} in expression: {1}", expression, originalExpression));
				}
			}

			return expressionValue;
		}

		/// <summary>
		/// Tests expression to see if it starts with a keyword.
		/// </summary>
		/// <param name="expression">Expression to test.</param>
		/// <param name="operation">Operation to test for.</param>
		/// <returns>true if expression starts with a keyword.</returns>
		private bool StartsWithKeyword(string expression, PreprocessorOperation operation)
		{
			expression = expression.ToUpper();
			switch (operation)
			{
				case PreprocessorOperation.Not:
					if (expression.StartsWith("NOT ") || expression.StartsWith("NOT("))
					{
						return true;
					}
					break;
				case PreprocessorOperation.And:
					if (expression.StartsWith("AND ") || expression.StartsWith("AND("))
					{
						return true;
					}
					break;
				case PreprocessorOperation.Or:
					if (expression.StartsWith("OR ") || expression.StartsWith("OR("))
					{
						return true;
					}
					break;
				default:
					break;
			}
			return false;
		}

		/// <summary>
		/// Update the current line number with the reader's current state.
		/// </summary>
		/// <param name="reader">The xml reader for the preprocessor.</param>
		/// <param name="offset">This is the artificial offset of the line numbers from the reader.  Used for the foreach processing.</param>
		private void UpdateInformation(XmlReader reader, int offset)
		{
			if (reader is IXmlLineInfo)
			{
				int lineNumber = ((IXmlLineInfo)reader).LineNumber + offset;

				if (this.currentLineNumber.LineNumber != lineNumber)
				{
					this.currentLineNumber.LineNumber = lineNumber;
					this.currentLineNumberWritten = false;
				}
			}
		}

		/// <summary>
		/// Write out the processing instruction that contains the line number information.
		/// </summary>
		/// <param name="reader">The xml reader.</param>
		/// <param name="writer">The xml writer.</param>
		/// <param name="offset">The artificial offset to use when writing the current line number.  Used for the foreach processing.</param>
		private void WriteProcessingInstruction(XmlReader reader, XmlWriter writer, int offset)
		{
			this.UpdateInformation(reader, offset);

			if (!this.currentLineNumberWritten)
			{
				SourceLineNumberCollection sourceLineNumbers = this.GetCurrentSourceLineNumbers();

				// write the encoded source line numbers as a string into the "ln" processing instruction
				writer.WriteProcessingInstruction(Preprocessor.LineNumberElementName, sourceLineNumbers.EncodedSourceLineNumbers);
			}
		}

		/// <summary>
		/// Pushes a file name on the stack of included files.
		/// </summary>
		/// <param name="fileName">Name to push on to the stack of included files.</param>
		private void PushInclude(string fileName)
		{
			this.sourceStack.Push(this.currentLineNumber);
			this.currentLineNumber = new SourceLineNumber(fileName);
			this.includeNextStack.Push(true);
		}

		/// <summary>
		/// Pops a file name from the stack of included files.
		/// </summary>
		private void PopInclude()
		{
			this.currentLineNumber = (SourceLineNumber)this.sourceStack.Pop();

			this.includeNextStack.Pop();
		}

		/// <summary>
		/// Go through search paths, looking for a matching include file.
		/// Start the search in the directory of the source file, then go
		/// through the search paths in the order given on the command line
		/// (leftmost first, ...).
		/// </summary>
		/// <param name="includePath">User-specified path to the included file (usually just the file name).</param>
		/// <returns>Returns a FileInfo for the found include file, or null if the file cannot be found.</returns>
		private FileInfo GetIncludeFile(string includePath)
		{
			string finalIncludePath = null;
			FileInfo includeFile = null;

			includePath = includePath.Trim();

			// remove quotes (only if they match)
			if ((includePath.StartsWith("\"") && includePath.EndsWith("\"")) ||
				(includePath.StartsWith("'") && includePath.EndsWith("'")))
			{
				includePath = includePath.Substring(1, includePath.Length - 2);
			}

			// preprocess variables in the path
			includePath = this.PreprocessVariables(includePath);

			// check if the include file is a full path
			if (Path.IsPathRooted(includePath))
			{
				if (File.Exists(includePath))
				{
					finalIncludePath = includePath;
				}
			}
			else // relative path
			{
				// build a string to test the directory containing the source file first
				string includeTestPath = String.Concat(this.variables["sys.SOURCEFILEDIR"], includePath);

				// test the source file directory
				if (File.Exists(includeTestPath))
				{
					finalIncludePath = includeTestPath;
				}
				else // test all search paths in the order specified on the command line
				{
					foreach (string includeSearchPath in this.includeSearchPaths)
					{
						includeTestPath = includeSearchPath;

						// put the slash at the end of the path if its missing
						if (!includeSearchPath.EndsWith("/") && !includeSearchPath.EndsWith("\\"))
						{
							includeTestPath = String.Concat(includeTestPath, "\\");
						}

						// append the relative path to the included file
						includeTestPath = String.Concat(includeTestPath, includePath);

						// if the path exists, we have found the final string
						if (File.Exists(includeTestPath))
						{
							finalIncludePath = includeTestPath;
							break;
						}
					}
				}
			}

			// create the FileInfo if the path exists
			if (null != finalIncludePath)
			{
				includeFile = new FileInfo(finalIncludePath);
			}

			return includeFile;
		}

		/// <summary>
		/// Get the current source line numbers array for use in exceptions and processing instructions.
		/// </summary>
		/// <returns>Returns an array of SourceLineNumber objects.</returns>
		private SourceLineNumberCollection GetCurrentSourceLineNumbers()
		{
			int i = 0;
			SourceLineNumberCollection sourceLineNumbers = new SourceLineNumberCollection(new SourceLineNumber[1 + this.sourceStack.Count]);   // create an array of source line numbers to encode

			sourceLineNumbers[i++] = this.currentLineNumber;
			foreach (SourceLineNumber sourceLineNumber in this.sourceStack)
			{
				sourceLineNumbers[i++] = sourceLineNumber;
			}

			return sourceLineNumbers;
		}

		/// <summary>
		/// Sends a message to the message delegate if there is one.
		/// </summary>
		/// <param name="mea">Message event arguments.</param>
		private void OnMessage(MessageEventArgs mea)
		{
			if (mea is WixError)
			{
				this.foundError = true;
			}

			if (null != this.Message)
			{
				this.Message(this, mea);
			}
		}

		/// <summary>
		/// Attempts to use an extension to preprocess the parameter.
		/// </summary>
		/// <param name="parameterName">Name of parameter to process (including extension prefix).</param>
		/// <returns>Resulting string after preprocessing.</returns>
		private string TryExtensionPreprocessParameter(string parameterName)
		{
			int indexOfPeriod = parameterName.IndexOf('.');

			// ensure the extension type is supplied
			if (0 > indexOfPeriod)
			{
				this.OnMessage(WixErrors.PreprocessorMissingParameterPrefix(this.GetCurrentSourceLineNumbers(), parameterName));
			}

			string prefix = parameterName.Substring(0, indexOfPeriod);
			PreprocessorExtension extension = this.FindExtension(prefix);
			if (null == extension)
			{
				this.OnMessage(WixErrors.PreprocessorExtensionForParameterMissing(this.GetCurrentSourceLineNumbers(), parameterName, prefix));
			}
			else // retrieve the parameter's value from the extension
			{
				return extension.PreprocessParameter(parameterName.Substring(indexOfPeriod + 1));
			}

			return null;
		}

		/// <summary>
		/// Determines if the variable used in a foreach is legal.
		/// </summary>
		/// <param name="variableName">The name of the variable.</param>
		/// <returns>true if the variable is allowed; false otherwise.</returns>
		/// <remarks>
		/// We use this to lockdown the foreach loops to certain variable
		/// values since they can easily be abused to obfuscate code.
		/// </remarks>
		private bool TryExtensionIsForeachVariableAllowed(string variableName)
		{
			foreach (PreprocessorExtension preprocessorExtension in this.extensionTypes.Values)
			{
				if (preprocessorExtension.IsForeachVariableAllowed(variableName))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Finds a preprocessor extension by namespace URI.
		/// </summary>
		/// <param name="extensionType">type prefix for extension.</param>
		/// <returns>Found preprocessor extension or null if nothing matches namespace URI.</returns>
		private PreprocessorExtension FindExtension(string extensionType)
		{
			return (PreprocessorExtension)this.extensionTypes[extensionType];
		}

		/// <summary>
		/// Determines whether a variable is defined.
		/// </summary>
		/// <param name="variable">Variable to determine the existance of.</param>
		/// <returns>True if the variable is defined, false otherwise.</returns>
		private bool IsVariableDefined(string variable)
		{
			if (variable.StartsWith("env."))
			{
				return null != Environment.GetEnvironmentVariable(variable.Substring(4, variable.Length - 4));
			}

			return this.variables.ContainsKey(variable);
		}
	}
}
