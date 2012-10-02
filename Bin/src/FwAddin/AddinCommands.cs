using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Extensibility;
using EnvDTE;
using EnvDTE80;

namespace FwAddin
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class AddinCommands
	{
		/// <summary></summary>
		private DTE2 m_applicationObject;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:AddinCommands"/> class.
		/// </summary>
		/// <param name="applicationObject">The application object.</param>
		/// ------------------------------------------------------------------------------------
		public AddinCommands(DTE2 applicationObject)
		{
			m_applicationObject = applicationObject;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the previous function header.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GotoFunctionHeaderUp()
		{
			TextSelection sel = (TextSelection)m_applicationObject.ActiveDocument.Selection;
			TextPoint curPoint = sel.ActivePoint;
			CodeElement codeElement = GetMethodOrProperty(sel);
			bool fGotoPrevMethod = true;
			if (codeElement != null)
			{
				TextPoint startPoint = codeElement.StartPoint;
				if (curPoint.AbsoluteCharOffset != startPoint.AbsoluteCharOffset)
					fGotoPrevMethod = false;
				curPoint = startPoint;
			}
			if (fGotoPrevMethod)
			{
				CodeElement newElement = codeElement;
				while ((newElement == codeElement || newElement == null) && sel.CurrentLine > 1)
				{
					sel.LineUp(false, 1);
					newElement = GetMethodOrProperty(sel);
				}
				if (newElement != null)
					curPoint = newElement.StartPoint;
			}
			sel.MoveToPoint(curPoint, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gotoes the function header down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GotoFunctionHeaderDown()
		{
			TextSelection sel = (TextSelection)m_applicationObject.ActiveDocument.Selection;
			TextPoint curPoint = sel.ActivePoint;
			try
			{
				CodeElement codeElement = GetMethodOrProperty(sel);
				if (codeElement != null)
					sel.MoveToPoint(codeElement.EndPoint, false);

				codeElement = null;
				int prevLine = 0;
				while (codeElement == null && prevLine != sel.CurrentLine)
				{
					prevLine = sel.CurrentLine;
					sel.LineDown(false, 1);
					codeElement = GetMethodOrProperty(sel);
				}
				if (codeElement != null)
					sel.MoveToPoint(codeElement.StartPoint, false);
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Got exception in GotoFunctionHeaderDown: " + e.Message);
			}
		}

		/// <summary></summary>
		private const int kLineLen = 97;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the method header.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertMethodHeader()
		{
			TextSelection sel = (TextSelection)m_applicationObject.ActiveDocument.Selection;

			CodeElement codeElement = GetMethodOrProperty(sel);
			if (codeElement == null)
			{
				codeElement = sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementClass);
				if (codeElement == null)
					codeElement = sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementInterface);
				if (codeElement == null || codeElement.StartPoint.Line != sel.ActivePoint.Line)
				{
					// not a function or property, so just insert /// <summary/>
					sel.LineUp(false, 1);
					if (!IsXmlCommentLine)
					{
						sel.EndOfLine(false);
						sel.NewLine(1);
						sel.Text = "///";
						sel.LineDown(true, 1);
						sel.Delete(1);
						sel.LineUp(false, 1);
						sel.EndOfLine(false);
						sel.WordRight(true, 2);
						sel.Delete(1);
					}
					else
						sel.LineDown(false, 1);
					return;
				}
			}

			sel.MoveToPoint(codeElement.StartPoint, false);

			// Figure out indentation and build dashed line
			sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstColumn, false);
			sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, true);
			string indent = sel.Text;
			sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
			string dashedLine = indent + "/// " +
				new string('-', kLineLen - sel.ActivePoint.VirtualDisplayColumn - 4);

			bool fGhostDoc = true;
			try
			{
				// Use GhostDoc if available
				m_applicationObject.ExecuteCommand("Weigelt.GhostDoc.AddIn.DocumentThis", string.Empty);
			}
			catch
			{
				fGhostDoc = false;
			}

			if (fGhostDoc)
			{
				int nLine = sel.ActivePoint.Line;
				int nLineOffset = sel.ActivePoint.LineCharOffset;

				// Check to see if we're in the middle of the comment or at the beginning of
				// the method.
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
				if (GetMethodOrProperty(sel) == null)
				{
					// we're in the middle of the comment - move to the end of the comment
					MoveDownAfterComment(sel);

					// we're inserting one line (//---) above
					nLine++;
				}
				else
				{
					// We are at the beginning of the method.
					// Check to see if the line above the current line is an attribute. If it is we want to
					// start there, otherwise we start at the current line.
					sel.LineUp(false, 1);
					sel.CharRight(false, 1);
					if (sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementAttribute) == null)
						sel.MoveToLineAndOffset(nLine, 1, false);

					// we're inserting two lines above
					nLine += 2;
				}
				// In case the line is wrapped, we want to go to the real beginning of the line
				sel.MoveToLineAndOffset(sel.ActivePoint.Line, 1, false);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstColumn, false);

				// Insert a new line and then insert our dashed line.
				sel.Insert(dashedLine + Environment.NewLine, (int)vsInsertFlags.vsInsertFlagsCollapseToEnd);

				sel.LineUp(false, 1);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
				MoveUpBeforeComment(sel);

				sel.Insert(Environment.NewLine + dashedLine, (int)vsInsertFlags.vsInsertFlagsCollapseToEnd);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);

				// put IP at previous location
				sel.MoveToLineAndOffset(nLine, nLineOffset, false);
			}
			else
			{
				// check if we already have a comment
				sel.LineUp(false, 1);
				if (!IsXmlCommentLine)
				{
					// Insert comment
					sel.EndOfLine(false);
					sel.NewLine(1);
					sel.Text = "///";
				}

				// Insert line above
				MoveUpBeforeComment(sel);
				sel.EndOfLine(false);
				sel.NewLine(1);
				sel.Text = dashedLine;
				int upperLine = sel.ActivePoint.Line;
				sel.LineDown(false, 1);

				// reformat text
				for (; IsXmlCommentLine; )
				{
					int curLine = sel.CurrentLine;
					// go through all words in this line
					for (; sel.CurrentLine == curLine; sel.WordRight(false, 1))
					{
						if (sel.ActivePoint.VirtualDisplayColumn > kLineLen)
						{
							// we have to break before this word
							sel.WordLeft(true, 1);
							// skip all punctuation characters
							for (; sel.Text.Length == 1 && char.IsPunctuation(sel.Text[0]); )
							{
								sel.CharLeft(false, 1); // removes selection
								sel.WordLeft(true, 1);
							}
							sel.CharLeft(false, 1); // removes selection

							// break the line
							sel.NewLine(1);

							// join next line with remainder of current line
							sel.EndOfLine(false);
							sel.LineDown(true, 1);
							sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, true);
							sel.WordRight(true, 1);
							sel.Delete(1);

							// insert a space between the two lines
							sel.Text = " ";
						}
					}
				}

				// Insert line below
				sel.GotoLine(upperLine + 1, false);
				MoveDownAfterComment(sel);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
				sel.NewLine(1);
				sel.LineUp(false, 1);
				sel.Text = dashedLine;
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
				sel.LineDown(false, 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is XML comment line.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is XML comment line; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		private bool IsXmlCommentLine
		{
			get
			{
				TextSelection sel = (TextSelection)m_applicationObject.ActiveDocument.Selection;
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
				sel.EndOfLine(true);
				return sel.Text.StartsWith("///");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the up before comment.
		/// </summary>
		/// <param name="sel">The sel.</param>
		/// ------------------------------------------------------------------------------------
		private void MoveUpBeforeComment(TextSelection sel)
		{
			TextRanges textRanges = null;
			for (; true; )
			{
				if (sel.FindPattern("\\<summary\\>|/// ---", (int)(vsFindOptions.vsFindOptionsBackwards | vsFindOptions.vsFindOptionsRegularExpression),
					ref textRanges))
				{
					// GhostDoc messes up dashed lines from inherited comments from base class,
					// so delete those
					if (sel.Text.StartsWith("/// ---"))
					{
						sel.EndOfLine(true);
						sel.WordRight(true, 1);
						sel.Delete(1);
					}
					else if (sel.Text.StartsWith("<summary>"))
					{
						while (true)
						{
							sel.MoveToLineAndOffset(sel.ActivePoint.Line - 1, 1, false);
							sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
							sel.EndOfLine(true);
							if (!sel.Text.StartsWith("///"))
							{
								if (sel.Text.Length > 0)
								{
									// there is a non-empty comment line. We want to start at the end
									// of it
									sel.EndOfLine(false);
								}
								else
									sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
								break;
							}

							// GhostDoc messes up dashed lines from inherited comments from base class,
							// so delete those
							if (sel.Text.StartsWith("/// -----"))
							{
								sel.WordRight(true, 1);
								sel.Delete(1);
							}
						}
						return;
					}
				}
				else
					return;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the down after comment.
		/// </summary>
		/// <param name="sel">The sel.</param>
		/// ------------------------------------------------------------------------------------
		private void MoveDownAfterComment(TextSelection sel)
		{
			// Go to the beginning of the line and move down until we find a line that doesn't
			// start with ///.
			while (true)
			{
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, false);
				sel.EndOfLine(true);
				if (!sel.Text.StartsWith("///"))
					break;

				// GhostDoc messes up dashed lines from inherited comments from base class,
				// so delete those
				if (sel.Text.StartsWith("/// -----"))
				{
					sel.WordRight(true, 1);
					sel.Delete(1);
					sel.LineUp(false, 1);
				}
				sel.MoveToLineAndOffset(sel.ActivePoint.Line + 1, 1, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the method or property.
		/// </summary>
		/// <param name="sel">The sel.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static CodeElement GetMethodOrProperty(TextSelection sel)
		{
			CodeElement codeElement;
			codeElement = sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementFunction);
			if (codeElement == null)
				codeElement = sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementProperty);
			return codeElement;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggle the H and CPP file
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public void ToggleHAndCpp()
		{
			string newFile;
			if (Path.GetExtension(m_applicationObject.ActiveDocument.FullName) == ".cpp")
			{
				newFile = Path.ChangeExtension(m_applicationObject.ActiveDocument.FullName, ".h");
			}
			else if (Path.GetExtension(m_applicationObject.ActiveDocument.FullName) == ".h")
			{
				newFile = Path.ChangeExtension(m_applicationObject.ActiveDocument.FullName, ".cpp");
			}
			else
				return;

			// Try to activate the file if it is already open, otherwise open it.
			if (m_applicationObject.get_IsOpenFile(null, newFile))
			{
				Document doc = m_applicationObject.Documents.Item(Path.GetFileName(newFile));
				doc.Activate();
			}
			else
				m_applicationObject.OpenFile(null, newFile);
		}
	}
}
