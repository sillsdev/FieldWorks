// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, 2006 SIL International. All Rights Reserved.
// <copyright from='2003' to='2006' company='SIL International'>
//		Copyright (c) 2003, 2006 SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XDumper.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Web;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using System.Text;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// JohnT: filling in the little I know...XDumper is part of the implementation of FXT XML export.
	/// I have optimized by adding various caches, such as one that knows all the custom fields for each class,
	/// and one that knows which writing systems have data for which multilingual properties. For this reason
	/// a new XDumper should be created for each export, unless you know that nothing has changed in the database
	/// in between.
	/// </summary>
	public class XDumper : IFWDisposable
	{
		/// <summary>
		///
		/// </summary>
		public delegate void ProgressHandler(object sender);

		/// <summary>
		///
		/// </summary>
		public event ProgressHandler UpdateProgress;

		/// <summary>
		///
		/// </summary>
		public event EventHandler<MessageArgs> SetProgressMessage;
		/// <summary>
		/// Class for passing progress message and new maximum for range.
		/// </summary>
		public class MessageArgs : EventArgs
		{
			private string _msgid;
			private int _max;
			/// <summary>
			/// The resource id of the progress message to display.
			/// </summary>
			public string MessageId
			{
				get { return this._msgid; }
				set { _msgid = value; }
			}
			public int Max
			{
				get { return this._max; }
				set { _max = value; }
			}
		}

		private XmlDocument m_fxtDocument;
		protected FdoCache m_cache;
		protected string m_format; //"xml", "sf"
		protected XmlNode m_templateRootNode;
		protected CmObject m_rootObject;
		protected bool m_outputGuids;
		protected IFilterStrategy[] m_filters;
		protected bool m_cancelNow = false;
		protected Dictionary<string, XmlNode> m_classNameToclassNode = new Dictionary<string, XmlNode>(100);
		protected Icu.UNormalizationMode m_eIcuNormalizationMode = Icu.UNormalizationMode.UNORM_NFD;
		private WritingSystemAttrStyles m_writingSystemAttrStyle = WritingSystemAttrStyles.FieldWorks;
		private StringFormatOutputStyle m_eStringFormatOutput = StringFormatOutputStyle.None;
		/// <summary>
		/// Store the pathname of the output file, if there is one.
		/// </summary>
		private string m_sOutputFilePath = null;

		/// <summary>
		/// When true, if and object *would* be output but is missing a matching <class>, an exception is thrown
		/// </summary>
		protected bool m_requireClassTemplatesForEverything=false;

		/// <summary>
		/// This is another one that should be true by default, but it breaks some old templates (mdf.xml in Nov 2006)
		/// </summary>
		protected bool m_doUseBaseClassTemplatesIfNeeded = false;

		protected string m_sTemplateFilePath = null;
		/// <summary>
		/// This stores the filename of an auxiliary FXT file in case a secondary file must be
		/// written.  This is done to support LIFT 0.11 export.
		/// </summary>
		protected string m_sAuxiliaryFxtFile = null;
		/// <summary>
		/// This stores the filename extension of the secondary file (if any).  This is done to
		/// support LIFT 0.11 export.
		/// </summary>
		protected string m_sAuxiliaryExtension = null;
		/// <summary>
		/// This stores the filename of the secondary file (if any).
		/// </summary>
		protected string m_sAuxiliaryFilename = null;

		/// <summary>
		/// When processing virtual properties like lexical relations, we need to keep track of
		/// the original object being referenced in order to know how to handle the relation.
		/// </summary>
		protected Stack<int> m_openForRefStack = new Stack<int>(4);

		/// <summary>
		/// This caches the mapping from the UserLabel of custom fields to their ids.
		/// </summary>
		private Dictionary<string, int> m_htCustomFields = new Dictionary<string, int>(8);

		/// <summary>
		/// Map from real flids to virtual flids to allow various niceties in output.
		/// Inspired by LT-9741.
		/// </summary>
		private Dictionary<int, int> m_mapFlids = new Dictionary<int, int>();

		/// <summary>
		/// Maintain a list of boolean variables that can be set by the caller, and tested
		/// by the FXT file.  If a variable doesn't exist, its "value" is false.
		/// </summary>
		protected Dictionary<string, bool> m_dictTestVars = new Dictionary<string, bool>();

		/// <summary>
		/// Flag whether to actually write out the auxiliary file indicated in the primary
		/// FXT file.
		/// </summary>
		private bool m_fSkipAuxFileOutput = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XDumper"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public XDumper(FdoCache cache)
		{
			m_cache = cache;
		}

		public XDumper()
		{

		}
		/// <summary>
		///
		/// </summary>
		public void Cancel()
		{
			CheckDisposed();

			//review: if we do something more complicated, will have to pay attention to thread issues
			m_cancelNow = true;
		}

		public enum WritingSystemAttrStyles
		{
			FieldWorks,
			LIFT
		};

		public enum StringFormatOutputStyle
		{
			None,
			FieldWorks,
			LIFT
		};

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~XDumper()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			m_rootObject = null;
			m_format = null;
			m_templateRootNode = null;
			m_filters = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		private void DetermineFormat(XmlDocument document)
		{
			XmlNode node = document.SelectSingleNode("//template");
			m_format = XmlUtils.GetOptionalAttributeValue(node,"format", "xml");

		}

		public void Go(CmObject rootObject, string templateFilePath)
		{
			CheckDisposed();

			//TODO: create a file with a random name
			string path =System.Environment.ExpandEnvironmentVariables(@"%temp%\testXDumpOutput.xml");
			TextWriter writer = File.CreateText(path);
			Go(rootObject, templateFilePath, writer);
		}

		public void Go(CmObject rootObject, string templateFilePath, TextWriter writer)
		{
			CheckDisposed();

			Go(rootObject, templateFilePath, writer, new IFilterStrategy[] { });
		}

		public void Go(CmObject rootObject, string templateFilePath, TextWriter writer, IFilterStrategy[] filters)
		{
			CheckDisposed();

			m_sTemplateFilePath = templateFilePath;
			XmlDocument document = new XmlDocument();
			document.Load(templateFilePath);
			FxtDocument = document;
			Go(rootObject,writer, filters);
		}

		public void Go(CmObject rootObject, TextWriter writer, IFilterStrategy[] filters)
		{
			CheckDisposed();

			try
			{
				m_rootObject = rootObject;
				m_filters = filters;
				// Get the output filename from the writer.
				if (writer is StreamWriter)
				{
					StreamWriter sw = writer as StreamWriter;
					if (sw.BaseStream is System.IO.FileStream)
						m_sOutputFilePath = (sw.BaseStream as System.IO.FileStream).Name;
				}
				////This allows the template to be somewhere other than the root of the xml
				////document, which is useful if the document is, for example, an xhtml doc.
				//m_templateRootNode =document.SelectSingleNode("//template");
				//if (m_templateRootNode == null)
				//	throw new ConfigurationException ("Could not find the <template> element.");
				//DumpObject(writer, rootObject);

				// Ensure that ICU has been initialzed before we dump anything.  This should
				// help fix LT-3970.
				Icu.InitIcuDataDir();

				Go(writer);
			}
			finally
			{
				writer.Close();
			}

			if (!m_fSkipAuxFileOutput &&
				!String.IsNullOrEmpty(m_sAuxiliaryFxtFile) && !String.IsNullOrEmpty(m_sAuxiliaryFilename))
			{
				using (TextWriter w = new StreamWriter(m_sAuxiliaryFilename))
				{
					XmlDocument document = new XmlDocument();
					string sTemplatePath = Path.Combine(Path.GetDirectoryName(m_sTemplateFilePath), m_sAuxiliaryFxtFile);
					document.Load(sTemplatePath);
					FxtDocument = document;
					Go(w);
				}
			}
		}

		/// <summary>
		/// Get/set the flag that tells XDumper to skip writing out any auxiliary file requested
		/// by the primary FXT file.
		/// </summary>
		public bool SkipAuxFileOutput
		{
			get { return m_fSkipAuxFileOutput; }
			set { m_fSkipAuxFileOutput = value; }
		}

		private void Go(TextWriter writer)
		{
			//TODO: foreach through the children until you get to the template

			DetermineFormat(m_fxtDocument);
			AutoloadPolicies oldPolicy = m_cache.VwOleDbDaAccessor.AutoloadPolicy;
			try
			{

				// Changed from kalpLoadAllOfClassForReadOnly to kalpLoadForAllOfObjectClass
				// This fixes LT-3984 in the 3.1 branch.  This also fixes LT-10160 in the 6.0 branch!
				// (I had changed it back to speed up LIFT export, since I couldn't reproduce LT-3984.)
				m_cache.VwOleDbDaAccessor.AutoloadPolicy = AutoloadPolicies.kalpLoadForAllOfObjectClass;
				foreach (XmlNode node in m_fxtDocument.ChildNodes)
				{
					//if(node.NodeType ==XmlNodeType.ProcessingInstruction
					//	|| node  is XmlDeclaration)
					if (!(node is XmlElement)) // processing instructions, comments, etc.
					{
						if (m_format == "xml") //else skip it
							writer.WriteLine(node.OuterXml);
					}
					else if (node.Name == "template")
					{
						DoTemplateElement(writer, node);
					}
					else
					{
						DoLiteralElement(writer, m_rootObject, node);
					}
				}
			}
			finally
			{
				m_cache.VwOleDbDaAccessor.AutoloadPolicy = oldPolicy;
			}
		}

		protected void DoTemplateElement(TextWriter contentsStream, XmlNode node)
		{
			m_templateRootNode = node;
			string sIcuNormalizationMode = XmlUtils.GetOptionalAttributeValue(m_templateRootNode, "normalization", "NFC");
			if (sIcuNormalizationMode == "NFD")
				m_eIcuNormalizationMode = Icu.UNormalizationMode.UNORM_NFD;
			else
				m_eIcuNormalizationMode = Icu.UNormalizationMode.UNORM_NFC;
			string style = XmlUtils.GetOptionalAttributeValue(m_templateRootNode, "writingSystemAttributeStyle", WritingSystemAttrStyles.FieldWorks.ToString());
			m_writingSystemAttrStyle = (WritingSystemAttrStyles) System.Enum.Parse(typeof(WritingSystemAttrStyles), style);
			string sFormatOutput = XmlUtils.GetOptionalAttributeValue(m_templateRootNode, "stringFormatOutputStyle", StringFormatOutputStyle.None.ToString());
			m_eStringFormatOutput = (StringFormatOutputStyle)System.Enum.Parse(typeof(StringFormatOutputStyle), sFormatOutput);
			m_requireClassTemplatesForEverything = XmlUtils.GetBooleanAttributeValue(node,"requireClassTemplatesForEverything");
			m_doUseBaseClassTemplatesIfNeeded = XmlUtils.GetBooleanAttributeValue(node, "doUseBaseClassTemplatesIfNeeded");

			// kalpLoadForAllOfObjectClass is much slower in the presence of null data,
			// especially for multiple vernacular and analysis writing systems!  Use the faster
			// policy if requested.
			string sAutoloadPolicy = XmlUtils.GetOptionalAttributeValue(m_templateRootNode, "autoloadPolicy", String.Empty);
			if (sAutoloadPolicy.ToLowerInvariant() == "readonly")
				m_cache.VwOleDbDaAccessor.AutoloadPolicy = AutoloadPolicies.kalpLoadAllOfClassForReadOnly;

			if (UpdateProgress != null)
				UpdateProgress(this);
			string sProgressMsgId = XmlUtils.GetOptionalAttributeValue(m_templateRootNode, "messageId");
			if (!String.IsNullOrEmpty(sProgressMsgId) && SetProgressMessage != null)
			{
				MessageArgs ma = new MessageArgs();
				ma.MessageId = sProgressMsgId;
				ma.Max = XmlUtils.GetOptionalIntegerValue(m_templateRootNode, "progressMax", 20);
				SetProgressMessage.Invoke(this, ma);
			}
			if (String.IsNullOrEmpty(m_sAuxiliaryFxtFile))	// don't recurse in Go() more than once.
				ComputeAuxiliaryFilename(contentsStream, node);

			DumpObject(contentsStream, m_rootObject, null);
		}

		private void ComputeAuxiliaryFilename(TextWriter contentsStream, XmlNode node)
		{
			m_sAuxiliaryFxtFile = XmlUtils.GetOptionalAttributeValue(node, "auxiliaryFxt");
			if (String.IsNullOrEmpty(m_sAuxiliaryFxtFile))
				return;
			m_sAuxiliaryExtension = XmlUtils.GetOptionalAttributeValue(node, "auxiliaryExtension");
			if (String.IsNullOrEmpty(m_sAuxiliaryExtension))
				m_sAuxiliaryExtension = "aux.xml";
			string sBasename = m_sOutputFilePath;
			if (String.IsNullOrEmpty(sBasename))
				sBasename = "DUMMYFILENAME.XML";
			m_sAuxiliaryFilename = Path.ChangeExtension(sBasename, m_sAuxiliaryExtension);
		}

		protected XmlNode FindClassTemplateNode(Type type, string sClassTag)
		{
			/* (Hatton) For years, if the exact class wasn't found, this returned with no error.
			 * The meaning of just returning
			 * is that if you come across an object, say, in a vector, that has no <class>
			 * in the template, then you just don't output that object (I think).
			 * Now this seems prone to error, but I'm afraid of changing this now as it is
			 * a change in semantics that would require testing all the fxts we have with
			 * various databases.  So, the default is the return block, and wise fxt writers
			 * Should opt for the exception by doing <template requireClassTemplatesForEverything='true'>
			 *
			 * In addition, I'm going to make it try to find a class template for the super class.
			 */

			Type searchType = type;
			string sSearchKey;
			do
			{
				sSearchKey = searchType.Name;
				if (!String.IsNullOrEmpty(sClassTag))
					sSearchKey = sSearchKey + "-" + sClassTag;
				if (!m_classNameToclassNode.ContainsKey(sSearchKey))
				{
					XmlNode node = m_templateRootNode.SelectSingleNode("class[@name='" + sSearchKey + "']");
					if (node != null)
					{
						m_classNameToclassNode[sSearchKey] = node;
					}
					else if (m_doUseBaseClassTemplatesIfNeeded)
					{
						searchType = searchType.BaseType;
					}
				}
			} while (m_doUseBaseClassTemplatesIfNeeded && searchType != typeof(object) && !m_classNameToclassNode.ContainsKey(sSearchKey));


			if (!m_classNameToclassNode.ContainsKey(sSearchKey))
			{

				if (m_requireClassTemplatesForEverything)
				{
					StringBuilder sbldr = new StringBuilder(
						"Did not find a <class> element matching the type or any ancestor type of ");
					sbldr.Append(type.Name);
					if (!String.IsNullOrEmpty(sClassTag))
						sbldr.AppendFormat(" marked with the tag {0}", sClassTag);
					sbldr.Append(".");
					throw new RuntimeConfigurationException(sbldr.ToString());
				}
				else
				{
					return null;
				}
			}
			XmlNode classNode = m_classNameToclassNode[sSearchKey];

			return classNode;
		}

		protected XmlNode GetClassTemplateNode(string className)
		{
			if (!m_classNameToclassNode.ContainsKey(className))
			{
				XmlNode node = m_templateRootNode.SelectSingleNode("class[@name='" + className + "']");
				if (node != null)
				{
					m_classNameToclassNode[className] = node;
					return node;
				}
				else
				{
					return null;
				}
			}
			return m_classNameToclassNode[className];
		}

		protected void DumpObject(TextWriter contentsStream, CmObject currentObject, string sClassTag)
		{
			string className = m_cache.GetClassName((uint)currentObject.ClassID);
			XmlNode classNode = null;
			if (sClassTag != null && sClassTag.Length > 0)
			{
				className = className + "-" + sClassTag;
				classNode = GetClassTemplateNode(className);
			}
			if (classNode == null)
			{
				classNode = FindClassTemplateNode(currentObject.GetType(), sClassTag);
			}
			if (classNode == null)
			{
				return; // would have thrown an exception if that's what the template wanted
			}

			string sPreload = XmlUtils.GetOptionalAttributeValue(classNode, "preload");
			bool previousAssumeCacheSetting = m_cache.TestingOnly_AssumeCacheFullyLoaded;
			if (sPreload != null && sPreload.Length > 0)
			{
				m_cache.TestingOnly_AssumeCacheFullyLoaded = false;
				currentObject.GetType().InvokeMember(sPreload,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
					BindingFlags.InvokeMethod, null, currentObject, null);
				m_cache.TestingOnly_AssumeCacheFullyLoaded = true;
			}



			if (m_filters != null)
			{
				foreach(IFilterStrategy filter in m_filters)
				{
					string explanation;
					if (!filter.DoInclude (currentObject, out explanation))
					{
						if (explanation==null)
							explanation = "none";

						XmlTextWriter writer = new XmlTextWriter(contentsStream);
						writer.WriteComment(String.Format(" Object filtered out by filter {0}, reason: {1} ", filter.Label, explanation));

						// would choke the parser later if there were reserved chars in there
						//contentsStream.Write("<!-- Object filtered out by filter " + filter.Label + ", reason: "+ explanation + " ");
						//	contentsStream.Write(" -->");

						return;
					}
				}
			}

			DoChildren(/*null,*/ contentsStream, currentObject, classNode, null);

			m_cache.TestingOnly_AssumeCacheFullyLoaded = previousAssumeCacheSetting;

		}

		protected void CollectCallElementAttributes(List<string> rgsAttrs,
			CmObject currentObject, XmlNode node)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name").Trim();
			XmlNode classNode = GetClassTemplateNode(name);
			if (classNode == null)
				return;//	throw new RuntimeConfigurationException("Did not find a <class> element matching the root object type of "+className+".");

			string flagsList = XmlUtils.GetOptionalAttributeValue(node, "flags");
			CollectAttributes(rgsAttrs, currentObject, classNode, flagsList);
		}
		/// <summary>
		/// invoking another template (for now, just in other &lt;class&gt; template)
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoCallElement(TextWriter contentsStream, CmObject currentObject, XmlNode node)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name").Trim();
			XmlNode classNode = GetClassTemplateNode(name);
			if ( classNode ==null)
				return;//	throw new RuntimeConfigurationException("Did not find a <class> element matching the root object type of "+className+".");

			string flagsList = XmlUtils.GetOptionalAttributeValue(node, "flags");
			DoChildren(contentsStream, currentObject, classNode, flagsList);
		}

		/// <summary>
		/// Conditionally process the children.
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoIfElement(TextWriter contentsStream, CmObject currentObject,
			XmlNode node, bool fExpected)
		{
			if (TestPasses(currentObject, node) == fExpected)
				DoChildren(/*null,*/ contentsStream, currentObject, node, null);
		}

		private bool TestPasses(CmObject currentObject, XmlNode node)
		{
			if (!VariableTestsPass(node))
				return false;
			if (!ValueEqualityTestsPass(currentObject, node))
				return false;
			return true; // All conditions present passed.
		}

		private bool VariableTestsPass(XmlNode node)
		{
			string variableName = XmlUtils.GetAttributeValue(node, "variableistrue");
			if (!String.IsNullOrEmpty(variableName))
				return TestVariable(variableName, true);
			variableName = XmlUtils.GetAttributeValue(node, "variableisfalse");
			if (!String.IsNullOrEmpty(variableName))
				return TestVariable(variableName, false);
			return true;
		}

		private bool TestVariable(string variableName, bool fWanted)
		{
			bool fValue;
			if (!m_dictTestVars.TryGetValue(variableName, out fValue))
				fValue = false;
			return fValue == fWanted;
		}

		/// <summary>
		/// Set a boolean variable that can be tested in the FXT XML.
		/// </summary>
		public void SetTestVariable(string sName, bool fValue)
		{
			if (m_dictTestVars.ContainsKey(sName))
				m_dictTestVars[sName] = fValue;
			else
				m_dictTestVars.Add(sName, fValue);
		}

		private bool ValueEqualityTestsPass(CmObject currentObject, XmlNode node)
		{
			if (!IntEqualsTestPasses(currentObject, node))
				return false;
			else if (!LengthEqualsTestPasses(currentObject, node))
				return false;
			else if (!StringEqualsTestPasses(currentObject, node))
				return false;
			return true;
		}

		private bool IntEqualsTestPasses(CmObject currentObject, XmlNode node)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(node, "intequals", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(node, "intequals", -3);	// -3 might be valid
			if (intValue == intValue2)
			{
				int value = GetIntValueForTest(currentObject, node);
				if (value != intValue)
					return false;
			}
			return true;
		}

		private int GetIntValueForTest(CmObject currentObject, XmlNode node)
		{
			int hvo = 0;
			int flid = GetFlidAndHvo(currentObject, node, ref hvo);
			if (flid == 0 || hvo == 0)
			{
				string sField = XmlUtils.GetOptionalAttributeValue(node, "field");
				if (String.IsNullOrEmpty(sField))
					return 0; // This is rather arbitrary...objects missing, what should each test do?
				try
				{
					Type type = currentObject.GetType();
					PropertyInfo info = type.GetProperty(sField,
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
					if (info != null)
					{
						object result = info.GetValue(currentObject, null);
						if (typeof(bool) == result.GetType())
							return ((bool)result) ? 1 : 0;
						else
							return (int)result;
					}
				}
				catch (Exception error)
				{
					throw new ApplicationException(string.Format("There was an error while trying to get the property {0}. One thing that has caused this in the past has been a database which was not migrated properly.", sField), error);
				}
				return 0; // This is rather arbitrary...objects missing, what should each test do?
			}
			switch (currentObject.Cache.GetFieldType(flid))
			{
				case FieldType.kcptBoolean:
					if (currentObject.Cache.GetBoolProperty(hvo, flid))
						return 1;
					else
						return 0;
				case FieldType.kcptInteger:
					return currentObject.Cache.GetIntProperty(hvo, flid);
				case FieldType.kcptOwningAtom:
				case FieldType.kcptReferenceAtom:
					return currentObject.Cache.GetObjProperty(hvo, flid);
				default:
					return 0;
			}
		}

		private bool LengthEqualsTestPasses(CmObject currentObject, XmlNode node)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(node, "lengthequals", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(node, "lengthequals", -3);	// -3 might be valid
			if (intValue == intValue2)
			{
				int value = GetLengthFromCache(currentObject, node);
				if (value != intValue)
					return false;
			}
			return true;
		}

		private int GetLengthFromCache(CmObject currentObject, XmlNode node)
		{
			int hvo = 0;
			int flid = GetFlidAndHvo(currentObject, node, ref hvo);
			if (flid == 0 || hvo == 0)
				return 0; // This is rather arbitrary...objects missing, what should each test do?
			if (m_mapFlids.ContainsKey(flid))
				flid = m_mapFlids[flid];
			return currentObject.Cache.GetVectorSize(hvo, flid);
		}

		private bool StringEqualsTestPasses(CmObject currentObject, XmlNode node)
		{
			string sValue = XmlUtils.GetOptionalAttributeValue(node, "stringequals");
			if (sValue != null)
			{
				string value = GetStringValueForTest(currentObject, node);
				if (value == null)
					value = String.Empty;
				return sValue == value;
			}
			return true;
		}

		private string GetStringValueForTest(CmObject currentObject, XmlNode node)
		{
			int hvo = 0;
			int flid = GetFlidAndHvo(currentObject, node, ref hvo);
			if (flid == 0 || hvo == 0)
			{
				// Try for a property on the object.
				string sField = XmlUtils.GetOptionalAttributeValue(node, "field");
				if (String.IsNullOrEmpty(sField))
					return null;
				try
				{
					Type type = currentObject.GetType();
					PropertyInfo info = type.GetProperty(sField,
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
					if (info != null)
					{
						object result = info.GetValue(currentObject, null);
						return result.ToString();
					}
				}
				catch (Exception error)
				{
					throw new ApplicationException(string.Format("There was an error while trying to get the property {0}. One thing that has caused this in the past has been a database which was not migrated properly.", sField), error);
				}
				return null; // This is rather arbitrary...objects missing, what should each test do?
			}
			switch (currentObject.Cache.GetFieldType(flid))
			{
				case FieldType.kcptUnicode:
				case FieldType.kcptBigUnicode:
					return currentObject.Cache.GetUnicodeProperty(hvo, flid);
				case FieldType.kcptString:
				case FieldType.kcptBigString:
					return currentObject.Cache.GetTsStringProperty(hvo, flid).Text;
				case FieldType.kcptMultiUnicode:
				case FieldType.kcptMultiBigUnicode:
					return currentObject.Cache.GetMultiUnicodeAlt(hvo, flid,
						GetSingleWritingSystemDescriptor(node),
						ViewNameForFlid(flid, currentObject.Cache.MetaDataCacheAccessor));
				case FieldType.kcptMultiString:
				case FieldType.kcptMultiBigString:
					return currentObject.Cache.GetMultiStringAlt(hvo, flid,
						GetSingleWritingSystemDescriptor(node)).Text;
				default:
					return null;
			}
		}

		private string ViewNameForFlid(int flid, IFwMetaDataCache mdc)
		{
			string sField = mdc.GetFieldName((uint)flid);
			string sClass = mdc.GetOwnClsName((uint)flid);
			return sClass + "_" + sField;
		}

		private int GetFlidAndHvo(CmObject currentObject, XmlNode node, ref int hvo)
		{
			Debug.Assert(currentObject != null);
			hvo = currentObject.Hvo;
			int flid = 0;
			if (currentObject.Cache == null)
				return flid;
			IFwMetaDataCache mdc = currentObject.Cache.MetaDataCacheAccessor;
			if (mdc == null)
				return flid;
			XmlAttribute xa = node.Attributes["flid"];
			if (xa == null)
			{
				string sClass = XmlUtils.GetOptionalAttributeValue(node, "class");
				string sFieldPath = XmlUtils.GetOptionalAttributeValue(node, "field");
				string[] rgsFields = sFieldPath.Split(new char[] { '/' });
				for (int i = 0; i < rgsFields.Length; i++)
				{
					if (i > 0)
					{
						hvo = currentObject.Cache.GetObjProperty(hvo, flid);
						if (hvo == 0)
							return -1;
					}
					if (sClass == null || sClass.Length == 0)
					{
						uint clsid = (uint)currentObject.Cache.GetClassOfObject(hvo);
						flid = (int)mdc.GetFieldId2(clsid, rgsFields[i], true);
					}
					else
					{
						flid = (int)mdc.GetFieldId(sClass, rgsFields[i], true);
						if (flid != 0)
						{
							// And cache it for next time if possible...
							// Can only do this if it doesn't depend on the current object.
							// (Hence we only do this here where there was an explicit "class" attribute,
							// not in the branch where we looked up the class on the object.)
							XmlNode xmldocT = node;
							while (xmldocT != null && !(xmldocT is XmlDocument))
								xmldocT = xmldocT.ParentNode;
							if (xmldocT != null)
							{
								XmlDocument xmldoc = (XmlDocument)xmldocT;
								XmlAttribute xaT = xmldoc.CreateAttribute("flid");
								xaT.Value = flid.ToString();
								node.Attributes.Prepend(xaT);
							}
						}
					}
					sClass = null;
				}
			}
			else
			{
				flid = Convert.ToInt32(xa.Value, 10);
			}
			return flid;
		}

		/// <summary>
		/// Get the SFM tag extension for the given writing system.
		/// </summary>
		private string LabelString(ILgWritingSystem ws)
		{
			string sTagExt = null;
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;

			// MDF labels are based on English, so try that first.
			int wsTag = wsf.GetWsFromStr("en");
			sTagExt = ws.Abbr.GetAlternative(wsTag);
			if (sTagExt != null && sTagExt != string.Empty)
				return sTagExt;

			// If that doesn't work, try the user interface writing system.
			sTagExt = ws.Abbr.GetAlternative(wsf.UserWs);
			if (sTagExt != null && sTagExt != string.Empty)
				return sTagExt;

			// If that doesn't work, try the cache's fallback writing system.
			sTagExt = ws.Abbr.GetAlternative(m_cache.FallbackUserWs);
			if (sTagExt != null && sTagExt != string.Empty)
				return sTagExt;

			// If that doesn't work, try the first analysis writing system, or the ICU locale.
			return ws.Abbreviation;
		}

		/// <summary>
		/// Cache results of looking up custom flids for a given class (and prop type) so we only do each query once per dump,
		/// not once per object!
		/// </summary>
		Dictionary<string, int[]> m_customFlids = new Dictionary<string, int[]>();

		protected void DoCustomElements(TextWriter contentsStream, CmObject currentObject, XmlNode node)
		{
			string sClass = XmlUtils.GetManditoryAttributeValue(node, "class");
			string sType = XmlUtils.GetOptionalAttributeValue(node, "fieldType", "");
			int[] flids;
			if (!m_customFlids.TryGetValue(sClass + sType, out flids))
			{
				int clid = 0;
				try
				{
					clid = (int)m_cache.MetaDataCacheAccessor.GetClassId(sClass);
				}
				catch
				{
					clid = 0;
				}
				if (clid == 0)
				{
					m_customFlids[sClass + sType] = new int[0];
					return;		// we don't know what to do!
				}
				StringBuilder sbTypes = new StringBuilder();
				switch (sType)
				{
					case "mlstring":
						sbTypes.AppendFormat(" AND Type IN ({0}, {1}, {2}, {3})",
							(int)CellarModuleDefns.kcptMultiUnicode,
							(int)CellarModuleDefns.kcptMultiBigUnicode,
							(int)CellarModuleDefns.kcptMultiString,
							(int)CellarModuleDefns.kcptMultiBigString);
						break;
					case "simplestring":
						sbTypes.AppendFormat(" AND Type IN ({0}, {1})",
							(int)CellarModuleDefns.kcptString,
							(int)CellarModuleDefns.kcptBigString);
						break;
				}
				StringBuilder sb = new StringBuilder("SELECT Id From Field$ WHERE Custom=1 AND Class=");
				sb.Append(clid.ToString());
				if (sbTypes.Length > 0)
					sb.Append(sbTypes.ToString());
				string sql = sb.ToString();
				flids = DbOps.ReadIntArrayFromCommand(m_cache, sql, null);
				m_customFlids[sClass + sType] = flids;
			}
			if (flids.Length == 0)
				return;		// nothing to do.
			for (int i = 0; i < flids.Length; ++i)
			{
				XmlNode parentNode = node.Clone();
				uint flid = (uint)flids[i];
				string labelName = m_cache.MetaDataCacheAccessor.GetFieldLabel(flid);
				string fieldName = m_cache.MetaDataCacheAccessor.GetFieldName(flid);
				string className = m_cache.MetaDataCacheAccessor.GetOwnClsName(flid);
				if (String.IsNullOrEmpty(labelName))
					labelName = fieldName;
				string sfMarker = "zz";
				if (fieldName.StartsWith("custom"))
				{
					sfMarker = String.Format("z{0}", fieldName.Substring(6));
					if (sfMarker == "z")
						sfMarker = "z0";
				}
				ReplaceSubstringInAttr visitorFn = new ReplaceSubstringInAttr("${fieldName}", fieldName);
				ReplaceSubstringInAttr visitorLab = new ReplaceSubstringInAttr("${label}", labelName);
				ReplaceSubstringInAttr visitorSfm = new ReplaceSubstringInAttr("${sfm}", sfMarker);
				foreach (XmlNode xn in parentNode.ChildNodes)
				{
					XmlUtils.VisitAttributes(xn, visitorFn);
					XmlUtils.VisitAttributes(xn, visitorLab);
					XmlUtils.VisitAttributes(xn, visitorSfm);
				}
				if (parentNode.InnerText.Contains("${definition}"))
					FillInCustomFieldDefinition(parentNode, flid);
				if (parentNode.InnerText.Contains("${description}"))
					FillInCustomFieldDescription(parentNode, flid);
				DoChildren(contentsStream, currentObject, parentNode, null);
			}
		}

		/// <summary>
		/// This is a temporary (I HOPE!) hack to get something out to the LIFT file until
		/// the LIFT spec allows a better form of field definition.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="flid"></param>
		private void FillInCustomFieldDefinition(XmlNode node, uint flid)
		{
			if (node.NodeType == XmlNodeType.Text)
			{
				StringBuilder sb = new StringBuilder();
				int type = m_cache.MetaDataCacheAccessor.GetFieldType(flid);
				string sType = type.ToString();
				// unfortunately, the kcpt values coincide with some kclid values, so we can't
				// use the neat, easy trick to convert from the integer to the string that we
				// use for the ws value.  :-(
				switch (type)
				{
					case 0: sType = "kcptNil"; break;
					case 1: sType = "kcptBoolean"; break;
					case 2: sType = "kcptInteger"; break;
					case 3: sType = "kcptNumeric"; break;
					case 4: sType = "kcptFloat"; break;
					case 5: sType = "kcptTime"; break;
					case 6: sType = "kcptGuid"; break;
					case 7: sType = "kcptImage"; break;
					case 8: sType = "kcptGenDate"; break;
					case 9: sType = "kcptBinary"; break;
					case 13: sType = "kcptString"; break;
					case 14: sType = "kcptMultiString"; break;
					case 15: sType = "kcptUnicode"; break;
					case 16: sType = "kcptMultiUnicode"; break;
					case 17: sType = "kcptBigString"; break;
					case 18: sType = "kcptMultiBigString"; break;
					case 19: sType = "kcptBigUnicode"; break;
					case 20: sType = "kcptMultiBigUnicode"; break;
					case 23: sType = "kcptOwningAtom"; break;
					case 24: sType = "kcptReferenceAtom"; break;
					case 25: sType = "kcptOwningCollection"; break;
					case 26: sType = "kcptReferenceCollection"; break;
					case 27: sType = "kcptOwningSequence"; break;
					case 28: sType = "kcptReferenceSequence"; break;
				}
				sb.AppendFormat("Type={0}", sType);
				int ws = m_cache.MetaDataCacheAccessor.GetFieldWs(flid);
				if (ws < 0)
				{
					sb.AppendFormat("; WsSelector={0}", ((CellarModuleDefns)ws).ToString());
				}
				else if (ws > 0)
				{
					sb.AppendFormat("; WsSelector={0}",
						m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws));
				}
				uint clidDst = m_cache.MetaDataCacheAccessor.GetDstClsId(flid);
				if ((int)clidDst > 0)
				{
					sb.AppendFormat("; DstCls={0}", m_cache.MetaDataCacheAccessor.GetClassName(clidDst));
				}
				node.Value = node.Value.Replace("${definition}", sb.ToString());
			}
			else
			{
				foreach (XmlNode xn in node.ChildNodes)
					FillInCustomFieldDefinition(xn, flid);
			}
		}

		private void FillInCustomFieldDescription(XmlNode node, uint flid)
		{
			if (node.NodeType == XmlNodeType.Text)
			{
				string sHelp = m_cache.MetaDataCacheAccessor.GetFieldHelp(flid);
				if (sHelp == null)
					sHelp = String.Empty;
				node.Value = node.Value.Replace("${description}", sHelp);
			}
			else
			{
				foreach (XmlNode xn in node.ChildNodes)
					FillInCustomFieldDescription(xn, flid);
			}
		}

		/// <summary>
		/// Initialized when first needed, this table records, for each multilingual flid, which writing systems
		/// have ANY data. It is based on retrieving flid/ws combinations from MultiStr$ and MultiBigStr$.
		/// We must remember that MultiUnicode properties have their own tables, while MultiBigString
		/// and MultiString are views on the general tables.
		/// (types: kcptMultiString(14: 30), kcptMultiUnicode (16:82), kcptMultiBigString (18: 17), and
		/// kcptMultiBigUnicode (20: 0).
		/// </summary>
		Dictionary<int, Set<int>> m_fieldWsData;

		/// <summary>
		/// Return a set of writing systems that have data for at least some objects for this flid;
		/// or null, if this cannot be determined (in testing, with no database) and we need to try
		/// them all.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		Set<int> GetWssWithData(int flid)
		{
			if (m_fieldWsData == null)
			{
				// This is an unfortunate kludge for testing.
				if (m_cache.DatabaseAccessor == null)
					return null;
				string sql = @"select distinct flid, ws from MultiStr$
					union select distinct flid, ws from MultiBigStr$
					union select distinct flid, ws from MultiBigTxt$
					order by flid";
				Dictionary<int, List<int>> values = new Dictionary<int,List<int>>();
				DbOps.LoadDictionaryFromCommand(m_cache, sql, null, values);
				m_fieldWsData = new Dictionary<int, Set<int>>();
				foreach (KeyValuePair<int, List<int>> pair in values)
					m_fieldWsData[pair.Key] = new Set<int>(pair.Value);
			}
			Set<int> result;
			if (m_fieldWsData.TryGetValue(flid, out result))
				return result;
			FieldType cpt = m_cache.GetFieldType(flid);
			if (cpt != FieldType.kcptMultiUnicode)
			{
				// no data at all for this property, since the query didn't match, and it's one of
				// the ones that shares those tables. Remember for next time.
				result = new Set<int>();
			}
			else
			{
				string className = m_cache.MetaDataCacheAccessor.GetOwnClsName((uint)flid);
				string fieldName = m_cache.MetaDataCacheAccessor.GetFieldName((uint)flid);
				string sql2 = "select distinct ws from " + className + "_" + fieldName;
				List<int> writingSystems = DbOps.ReadIntsFromCommand(m_cache, sql2, null);
				result = new Set<int>(writingSystems);
			}
			m_fieldWsData[flid] = result;
			return result;
		}

		/// <summary>
		/// Return true if the database contains values for this flid for ANY object.
		/// Checking this first prevents a lot of queries and a lot of empty strings
		/// stored in the cache.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="alternative"></param>
		/// <returns></returns>
		private bool MightHaveAlternative(int flid, int alternative)
		{
			Set<int> writingSystems = GetWssWithData(flid);
			if (writingSystems == null)
				return true;
			return writingSystems.Contains(alternative);
		}


		/// <summary>
		/// Output an element for each requested ws that is also in the data.
		/// </summary>
		/// <example>For xml, <form ws='eng'>foo</form><form ws='es'>fos</form> </example>
		/// <example>For sfm output of the form \gEng (english) \gChn (chinese) \gFrn (french), etc.</example>
		protected void DoMultilingualStringElement(TextWriter contentsStream,
			CmObject currentObject, XmlNode node, string flags)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name");
			string propertyName = XmlUtils.GetManditoryAttributeValue(node, "simpleProperty");
			string methodName = XmlUtils.GetOptionalAttributeValue(node, "method");
			string writingSystems = XmlUtils.GetOptionalAttributeValue(node, "ws", "all");
			object propertyObject = GetProperty(currentObject, propertyName);
			if (!HasAtLeastOneMatchingString(propertyObject, writingSystems))
				return;
			string wrappingElementName = XmlUtils.GetOptionalAttributeValue(node, "wrappingElementName", null);
			string internalElementName = XmlUtils.GetOptionalAttributeValue(node, "internalElementName", null);
			bool fWriteAsField = XmlUtils.GetOptionalBooleanAttributeValue(node, "writeAsField", false);
			bool fLeadingNewline = false;
			if (m_format == "xml")
			{
				WriteWrappingStartElement(contentsStream, wrappingElementName, fWriteAsField, node);
				fLeadingNewline = !String.IsNullOrEmpty(wrappingElementName);
			}
			else
			{
				Debug.Assert(wrappingElementName == null && internalElementName == null && !fWriteAsField);
			}
			Set<int> possibleWss = null;
			if (propertyObject is MultiAccessor)
			{
				possibleWss = GetWssWithData((propertyObject as MultiAccessor).Flid);
			}
			foreach (ILgWritingSystem ws in GetDesiredWritingSystemsSet(writingSystems, possibleWss))
			{
				bool fHasData = false;
				if (methodName != null)
				{
					object obj = GetMethodResult(currentObject, methodName, new object[] { ws.Hvo });
					if (obj != null)
						fHasData = TryWriteStringAlternative(obj, ws, name, internalElementName, contentsStream, fLeadingNewline);
				}
				else
				{
					fHasData = TryWriteStringAlternative(propertyObject, ws, name, internalElementName, contentsStream, fLeadingNewline);
				}
				if (fHasData)
				{
					fLeadingNewline = false;
					if (m_format == "xml")
						contentsStream.WriteLine();
				}
			}
			if (m_format == "xml")
			{
				WriteWrappingEndElement(contentsStream, wrappingElementName, fWriteAsField);
			}
		}

		private static void WriteWrappingStartElement(TextWriter contentsStream, string sName,
			bool fWriteAsField, XmlNode node)
		{
			if (!String.IsNullOrEmpty(sName))
			{
				string sAttrName = XmlUtils.GetOptionalAttributeValue(node, "attrName");
				string sAttrValue = XmlUtils.GetOptionalAttributeValue(node, "attrValue");
				if (fWriteAsField)
					contentsStream.Write("<field type=\"{0}\">", sName);
				else if (String.IsNullOrEmpty(sAttrName) || String.IsNullOrEmpty(sAttrValue))
					contentsStream.Write("<{0}>", sName);
				else
					contentsStream.Write("<{0} {1}=\"{2}\">", sName, sAttrName, sAttrValue);
			}
		}

		private static void WriteWrappingEndElement(TextWriter contentsStream, string sName, bool fWriteAsField)
		{
			if (!String.IsNullOrEmpty(sName))
			{
				if (fWriteAsField)
					contentsStream.WriteLine("</field>");
				else
					contentsStream.WriteLine("</{0}>", sName);
			}
		}

		private bool HasAtLeastOneMatchingString(object propertyObject, string writingSystems)
		{
			Set<int> possibleWss = null;
			if (propertyObject is MultiAccessor)
				possibleWss = GetWssWithData((propertyObject as MultiAccessor).Flid);

			foreach (ILgWritingSystem ws in GetDesiredWritingSystemsSet(writingSystems, possibleWss))
			{
				if (possibleWss != null && !possibleWss.Contains(ws.Hvo))
					continue;
				if (GetStringOfProperty(propertyObject, ws.Hvo) != null)
					return true;
			}
			return false;
		}

		private Set<ILgWritingSystem> GetDesiredWritingSystemsSet(string writingSystemsDescriptor, Set<int> possibleWss)
		{
			Set<ILgWritingSystem> wsSet = new Set<ILgWritingSystem>();

			if (writingSystemsDescriptor == "all" || writingSystemsDescriptor == "all analysis")
			{
				foreach (ILgWritingSystem ws in m_cache.LangProject.CurAnalysisWssRS)
				{
					if (possibleWss == null || possibleWss.Contains(ws.Hvo))
						wsSet.Add(ws);
				}
			}
			if (writingSystemsDescriptor == "all" || writingSystemsDescriptor == "all vernacular")
			{
				foreach (ILgWritingSystem ws in m_cache.LangProject.CurVernWssRS)
				{
					if (possibleWss == null || possibleWss.Contains(ws.Hvo))
						wsSet.Add(ws);
				}
			}
			if (writingSystemsDescriptor == "every")
			{
				foreach (ILgWritingSystem ws in m_cache.LanguageEncodings)
				{
					if (possibleWss == null || possibleWss.Contains(ws.Hvo))
						wsSet.Add(ws);
				}
			}
			return wsSet;
		}

		/// <summary>
		/// Form sfm output of the form \z1_Eng (english) \z1_Chn (chinese) \z1_Frn (french), etc.
		/// </summary>
		protected void doCustomMultilingualStringElementSFM(TextWriter contentsStream,
			CmObject currentObject, XmlNode node, string flags)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name");
			int flid = GetCustomFlid(node, currentObject.ClassID);

			string writingSystems = XmlUtils.GetOptionalAttributeValue(node, "ws", "all");
			Set<int> alreadyOutput = new Set<int>();
			if (writingSystems == "all" || writingSystems == "all analysis")
			{
				foreach (ILgWritingSystem ws in m_cache.LangProject.CurAnalysisWssRS)
				{
					writeCustomStringAlternativeToSFM(currentObject, flid, ws, name, contentsStream);
					alreadyOutput.Add(ws.Hvo);
				}
			}
			if (writingSystems == "all" || writingSystems == "all vernacular")
			{
				foreach (ILgWritingSystem ws in m_cache.LangProject.CurVernWssRS)
				{
					if (!alreadyOutput.Contains(ws.Hvo ))
						writeCustomStringAlternativeToSFM(currentObject, flid, ws, name, contentsStream);
				}
			}

		}

		/// <summary>
		/// Write one writing system alternative from a custom multilingual field.
		/// </summary>
		/// <param name="currentObject"></param>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		/// <param name="name"></param>
		/// <param name="contentsStream"></param>
		protected void writeCustomStringAlternativeToSFM(CmObject currentObject, int flid, ILgWritingSystem ws,
			string name, TextWriter contentsStream)
		{
			if (m_mapFlids.ContainsKey(flid))
				flid = m_mapFlids[flid];
			FieldType cpt = m_cache.GetFieldType(flid);
			switch (cpt)
			{
			case FieldType.kcptMultiUnicode:
			case FieldType.kcptMultiBigUnicode:
			case FieldType.kcptMultiString:
			case FieldType.kcptMultiBigString:
				break;
			default:
				return; // not a valid type.
			}
			ITsString tss = m_cache.GetMultiStringAlt(currentObject.Hvo, flid, ws.Hvo);
			WriteStringSFM(tss.Text, name, ws, contentsStream);
		}

		/// <summary>
		/// Form sfm output of the form \z1.
		/// </summary>
		protected void DoCustomStringElement(TextWriter contentsStream, CmObject currentObject,
			XmlNode node, string flags)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name");
			int flid = GetCustomFlid(node, currentObject.ClassID);

			string s = null;
			FieldType cpt = m_cache.GetFieldType(flid);
			switch (cpt)
			{
			case FieldType.kcptUnicode:
			case FieldType.kcptBigUnicode:
				s = m_cache.GetUnicodeProperty(currentObject.Hvo, flid);
				break;
			case FieldType.kcptString:
			case FieldType.kcptBigString:
				ITsString tss = m_cache.GetTsStringProperty(currentObject.Hvo, flid);
				if (tss != null)
					s = tss.Text;
				break;
			}
			WriteStringSFM(s, name, null, contentsStream);
		}

		/// <summary>
		/// Get the field id for a custom field, trying the "field" and "custom" attributes in
		/// turn.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="clid">class id of the owning object</param>
		/// <returns></returns>
		private int GetCustomFlid(XmlNode node, int clid)
		{
			int flid = 0;
			string field = XmlUtils.GetOptionalAttributeValue(node, "field");
			string custom = XmlUtils.GetOptionalAttributeValue(node, "custom");
			if (field != null)
			{
				flid = (int)m_cache.MetaDataCacheAccessor.GetFieldId2((uint)clid, field, true);
			}
			else if (custom != null)
			{
				if (!m_htCustomFields.TryGetValue(custom, out flid))
				{
					DbOps.ReadOneIntFromCommand(m_cache,
						"SELECT [Id] FROM Field$ WHERE UserLabel=?", custom, out flid);
					if (flid != 0)
						m_htCustomFields.Add(custom, flid);
				}
			}
			if (flid == 0)
			{
				string sMsg = string.Format("Invalid {0}", node.Name);
				throw new ConfigurationException(sMsg, node);
			}
			return flid;
		}

		private bool TryWriteStringAlternative(object orange, ILgWritingSystem ws, string name,
			string internalElementName, TextWriter contentsStream, bool fLeadingNewline)
		{
			ITsString tss = null;
			if (m_eStringFormatOutput != StringFormatOutputStyle.None)
				tss = GetTsStringOfProperty(orange, ws.Hvo);
			if (tss == null)
			{
				string s = GetStringOfProperty(orange, ws.Hvo);
				if (fLeadingNewline && !String.IsNullOrEmpty(s))
					contentsStream.WriteLine();
				WriteString(s, name, ws, internalElementName, contentsStream);
				return !String.IsNullOrEmpty(s);
			}
			else
			{
				Debug.Assert(m_format == "xml");
				if (fLeadingNewline && tss.Length > 0)
					contentsStream.WriteLine();
				WriteTsStringXml(tss, name, ws, internalElementName, contentsStream);
				return tss.Length > 0;
			}
		}

		private void WriteString(string s, string name, ILgWritingSystem ws, string internalElementName,
			TextWriter contentsStream)
		{
			if (m_format == "xml")
				WriteStringXml(s, name, ws, internalElementName, contentsStream);
			else
				WriteStringSFM(s, name, ws, contentsStream);
		}

		private void WriteString(string s, string name, ILgWritingSystem ws, TextWriter contentsStream)
		{
			WriteString(s, name, ws, null, contentsStream);
		}

		private void WriteStringSFM(string s, string name, ILgWritingSystem ws, TextWriter contentsStream)
		{
			if (s != null && s.Trim().Length > 0)
			{
				s = Icu.Normalize(s, m_eIcuNormalizationMode);
				string elname = name;
				if (ws != null)
				{
					elname = String.Format("{0}_{1}", name, LabelString(ws));  //e.g. lxEn
				}

				WriteOpeningOfStartOfComplexElementTag(contentsStream, elname);
				WriteClosingOfStartOfComplexElementTag(contentsStream);
				contentsStream.Write(s);
				WriteEndOfComplexElementTag(contentsStream, elname);
			}
		}

		private void WriteStringXml(string s, string name, ILgWritingSystem ws, TextWriter contentsStream)
		{
			WriteStringXml(s, name, ws, null, contentsStream);
		}

		private void WriteStringXml(string s, string name, ILgWritingSystem ws,
			string internalElementName, TextWriter contentsStream)
		{
			if (s == null || s.Trim().Length == 0)
				return;

			XmlTextWriter writer = new XmlTextWriter(contentsStream);
			s = Icu.Normalize(s, m_eIcuNormalizationMode);
			WriteStringStartElements(writer, name, ws, internalElementName);
			writer.WriteString(s);
			WriteStringEndElements(writer, internalElementName);
		}

		private void WriteTsStringXml(ITsString tss, string name, ILgWritingSystem ws,
			string internalElementName, TextWriter contentsStream)
		{
			if (tss == null || tss.Length == 0)
				return;
			XmlTextWriter writer = new XmlTextWriter(contentsStream);
			WriteStringStartElements(writer, name, ws, internalElementName);
			if (m_eStringFormatOutput == StringFormatOutputStyle.FieldWorks)
			{
				WriteFieldWorksTsStringContent(tss, writer);
			}
			else
			{
				Debug.Assert(m_eStringFormatOutput == StringFormatOutputStyle.LIFT);
				WriteLiftTsStringContent(tss, writer, ws.Hvo);
			}
			WriteStringEndElements(writer, internalElementName);
		}

		private void WriteStringStartElements(XmlTextWriter writer, string name,
			ILgWritingSystem ws, string internalElementName)
		{
			writer.WriteStartElement(name);
			if (ws != null)
			{
				switch (m_writingSystemAttrStyle)
				{
					case WritingSystemAttrStyles.LIFT:
						writer.WriteAttributeString("lang", ws.RFC4646bis);
						break;
					case WritingSystemAttrStyles.FieldWorks:
						writer.WriteAttributeString("ws", ws.Abbreviation);
						break;
				}
			}
			if (!String.IsNullOrEmpty(internalElementName))
				writer.WriteStartElement(internalElementName);
		}

		private void WriteStringEndElements(XmlTextWriter writer, string internalElementName)
		{
			if (!String.IsNullOrEmpty(internalElementName))
				writer.WriteEndElement();
			writer.WriteEndElement();
		}

		/// <summary>
		///
		/// </summary>
		protected void DoNumberElement(TextWriter contentsStream, CmObject currentObject,
			XmlNode node)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name");
			string propertyName = XmlUtils.GetManditoryAttributeValue(node, "simpleProperty");
			object val = GetProperty(currentObject, propertyName);
			Debug.Assert(val is int);
			int nVal = (int)val;
			string ifnotequalVal = XmlUtils.GetOptionalAttributeValue(node, "ifnotequal");
			if (ifnotequalVal != null)
			{
				int n = Convert.ToInt32(ifnotequalVal, 10);
				if (nVal == n)
					return;
			}
			string iflessVal = XmlUtils.GetOptionalAttributeValue(node, "ifless");
			if (iflessVal != null)
			{
				int n = Convert.ToInt32(iflessVal, 10);
				if (nVal >= n)
					return;
			}
			string ifgreaterVal = XmlUtils.GetOptionalAttributeValue(node, "ifgreater");
			if (ifgreaterVal != null)
			{
				int n = Convert.ToInt32(ifgreaterVal, 10);
				if (nVal <= n)
					return;
			}
			if (m_format == "xml")
			{
				bool fWriteAsTrait = XmlUtils.GetOptionalBooleanAttributeValue(node, "writeAsTrait", false);
				if (fWriteAsTrait)
				{
					contentsStream.WriteLine("<trait name=\"{0}\" value=\"{1}\"/>",
						XmlUtils.MakeSafeXmlAttribute(name), val.ToString());
					return;
				}
			}
			WriteOpeningOfStartOfComplexElementTag(contentsStream, name);
			WriteClosingOfStartOfComplexElementTag(contentsStream);
			contentsStream.Write(val.ToString());
			WriteEndOfComplexElementTag(contentsStream, name);
		}

		protected void DoBooleanElement(TextWriter contentsStream, CmObject currentObject,
			XmlNode node)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name");
			string propertyName = XmlUtils.GetManditoryAttributeValue(node, "simpleProperty");
			object val = GetProperty(currentObject, propertyName);
			Debug.Assert(val is bool);
			bool fVal = (bool)val;
			bool fOptional = XmlUtils.GetOptionalBooleanAttributeValue(node, "optional", false);
			if (fOptional && !fVal)
				return;
			if (m_format == "xml")
			{
				bool fTrait = XmlUtils.GetOptionalBooleanAttributeValue(node, "writeAsTrait", false);
				if (fTrait)
				{
					contentsStream.Write("<trait name=\"{0}\" value=\"{1}\"></trait>", name, fVal.ToString());
				}
				else
				{
					contentsStream.Write("<{0}>{1}</{0}>", name, fVal.ToString());
				}
			}
			else
			{
				if (fOptional)
					contentsStream.Write("\r\n\\{0}", name);	// no need to explicitly show "true"
				else
					contentsStream.Write("\r\n\\{0} {1}", name, fVal.ToString());
			}
		}

		protected void DoStringElement(TextWriter contentsStream, CmObject currentObject,
			XmlNode node)
		{
			string name = XmlUtils.GetManditoryAttributeValue(node, "name");
			string sVal = null;
			ITsString tssVal = null;
			if (m_eStringFormatOutput == StringFormatOutputStyle.None)
			{
				sVal = GetSimplePropertyString(node, currentObject);
			}
			else
			{
				tssVal = GetSimplePropertyTsString(node, currentObject);
				if (tssVal == null)
					sVal = GetSimplePropertyString(node, currentObject);
			}
			if (String.IsNullOrEmpty(sVal) && (tssVal == null || String.IsNullOrEmpty(tssVal.Text)))
				return;
			if (m_format == "xml")
			{
				bool fWriteAsField = XmlUtils.GetOptionalBooleanAttributeValue(node, "writeAsField", false);
				string sWrappingName = XmlUtils.GetOptionalAttributeValue(node, "wrappingElementName");
				string sInternalName = XmlUtils.GetOptionalAttributeValue(node, "internalElementName");
				WriteWrappingStartElement(contentsStream, sWrappingName, fWriteAsField, node);
				XmlTextWriter writer = new XmlTextWriter(contentsStream);
				writer.WriteStartElement(name);
				int wsFake;
				if (tssVal != null)
					wsFake = FirstWsOfTsString(tssVal);
				else
					wsFake = m_cache.DefaultAnalWs;
				if (m_writingSystemAttrStyle == WritingSystemAttrStyles.LIFT && name == "form")
				{
					ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, wsFake);
					writer.WriteAttributeString("lang", lgws.RFC4646bis);	// keep LIFT happy with bogus ws.
				}
				if (!String.IsNullOrEmpty(sInternalName))
					writer.WriteStartElement(sInternalName);
				string before = XmlUtils.GetOptionalAttributeValue(node, "before");
				string after = XmlUtils.GetOptionalAttributeValue(node, "after");
				if (!String.IsNullOrEmpty(before))
				{
					before = Icu.Normalize(before, m_eIcuNormalizationMode);
					writer.WriteString(before);
				}
				if (sVal != null)
				{
					sVal = Icu.Normalize(sVal, m_eIcuNormalizationMode);
					writer.WriteString(sVal);
				}
				else if (tssVal != null)
				{
					if (m_eStringFormatOutput == StringFormatOutputStyle.FieldWorks)
					{
						WriteFieldWorksTsStringContent(tssVal, writer);
					}
					else
					{
						Debug.Assert(m_eStringFormatOutput == StringFormatOutputStyle.LIFT);
						WriteLiftTsStringContent(tssVal, writer, wsFake);
					}
				}
				if (!String.IsNullOrEmpty(after))
				{
					after = Icu.Normalize(after, m_eIcuNormalizationMode);
					writer.WriteString(after);
				}
				if (!String.IsNullOrEmpty(sInternalName))
					writer.WriteEndElement();
				writer.WriteEndElement();
				if (m_writingSystemAttrStyle == WritingSystemAttrStyles.LIFT && name == "form")
					writer.WriteWhitespace(Environment.NewLine);
				WriteWrappingEndElement(contentsStream, sWrappingName, fWriteAsField);
			}
			else
			{
				WriteStringSFM(sVal, name, null, contentsStream);
			}
		}

		private int FirstWsOfTsString(ITsString tssVal)
		{
			ITsTextProps ttp = tssVal.get_Properties(0);
			int nVar;
			int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			if (ws > 0)
				return ws;
			else
				return m_cache.DefaultAnalWs;
		}

		private void WriteFieldWorksTsStringContent(ITsString tssVal, XmlTextWriter writer)
		{
			int crun = tssVal.RunCount;
			int nVar;
			int tpt;
			int nProp;
			string sProp;
			for (int irun = 0; irun < crun; ++irun)
			{
				StringBuilder sbComment = new StringBuilder();
				writer.WriteStartElement("run");
				ITsTextProps ttp = tssVal.get_Properties(irun);
				int cprop = ttp.IntPropCount;
				for (int iprop = 0; iprop < cprop; ++iprop)
				{
					nProp = ttp.GetIntProp(iprop, out tpt, out nVar);
					if (tpt == (int)FwTextPropType.ktptWs)
					{
						string sLang = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(nProp);
						writer.WriteAttributeString("ws", sLang);
					}
					else
					{
						AddIntPropToBuilder(tpt, nProp, nVar, sbComment);
					}
				}
				cprop = ttp.StrPropCount;
				for (int iprop = 0; iprop < cprop; ++iprop)
				{
					sProp = ttp.GetStrProp(iprop, out tpt);
					if (tpt == (int)FwTextPropType.ktptNamedStyle)
						writer.WriteAttributeString("namedStyle", sProp);
					else
						AddStrPropToBuilder(tpt, sProp, sbComment);
				}
				if (sbComment.Length > 0)
					writer.WriteComment(sbComment.ToString());
				string sRun = tssVal.get_RunText(irun);
				writer.WriteString(Icu.Normalize(sRun, m_eIcuNormalizationMode));
				writer.WriteEndElement();
			}
		}

		private void AddIntPropToBuilder(int tpt, int nProp, int nVar, StringBuilder sbComment)
		{
			string sTpt = DecodeTpt(tpt);
			sbComment.AppendFormat(" {0}=\"{1}/{2}\" ", sTpt, nProp, nVar);
		}

		private void AddStrPropToBuilder(int tpt, string sProp, StringBuilder sbComment)
		{
			string sTpt = DecodeTpt(tpt);
			sbComment.AppendFormat(" {0}=\"{1}\" ", sTpt, sProp);
		}

		private string DecodeTpt(int tpt)
		{
			if (System.Enum.IsDefined(typeof(FwTextPropType), tpt))
				return System.Enum.GetName(typeof(FwTextPropType), tpt);
			else
				return tpt.ToString();
		}

		/// <summary>
		/// For LIFT, span is the name of the run element, and it's output only with "interesting"
		/// values, that is, attributes that aren't the writing system of the overall string.
		/// </summary>
		/// <param name="tssVal"></param>
		/// <param name="writer"></param>
		/// <param name="wsString"></param>
		private void WriteLiftTsStringContent(ITsString tssVal, XmlTextWriter writer, int wsString)
		{
			int crun = tssVal.RunCount;
			int nVar;
			int tpt;
			int nProp;
			string sProp;
			bool fSpan;
			for (int irun = 0; irun < crun; ++irun)
			{
				ITsTextProps ttp = tssVal.get_Properties(irun);
				fSpan = true;
				if (ttp.IntPropCount == 1 && ttp.StrPropCount == 0)
				{
					nProp = ttp.GetIntProp(0, out tpt, out nVar);
					if (tpt == (int)FwTextPropType.ktptWs && nProp == wsString)
						fSpan = false;
				}
				if (fSpan)
				{
					writer.WriteStartElement("span");
					int cprop = ttp.IntPropCount;
					for (int iprop = 0; iprop < cprop; ++iprop)
					{
						nProp = ttp.GetIntProp(iprop, out tpt, out nVar);
						if (tpt == (int)FwTextPropType.ktptWs && nProp != wsString)
						{
							ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, nProp);
							writer.WriteAttributeString("lang", lgws.RFC4646bis);
						}
					}
					cprop = ttp.StrPropCount;
					for (int iprop = 0; iprop < cprop; ++iprop)
					{
						sProp = ttp.GetStrProp(iprop, out tpt);
						if (tpt == (int)FwTextPropType.ktptNamedStyle)
							writer.WriteAttributeString("class", sProp);
						else
							StringUtils.WriteHref(tpt, sProp, writer);
					}
				}
				string sRun = tssVal.get_RunText(irun);
				writer.WriteString(Icu.Normalize(sRun, m_eIcuNormalizationMode));
				if (fSpan)
					writer.WriteEndElement();
			}
		}

		protected void CollectElementElementAttributes(List<string> rgsAttrs, CmObject currentObject,
			XmlNode node, string flags)
		{
			string hideFlag = XmlUtils.GetOptionalAttributeValue(node, "hideFlag");
			// If no hideflag was specified, or it was specified but is not in the list of flags
			// we were called with.
			if (flags == null || hideFlag == null || flags.IndexOf(hideFlag) < 0)
				return;
			CollectAttributes(rgsAttrs, currentObject, node, null);
		}

		protected void DoElementElement(TextWriter contentsStream, CmObject currentObject,
			XmlNode node, string flags)
		{
			string hideFlag = XmlUtils.GetOptionalAttributeValue(node, "hideFlag");
			string name = XmlUtils.GetManditoryAttributeValue(node, "name");
			// If no hideflag was specified, or it was specified but is not in the list of flags
			// we were called with
			if (flags == null || hideFlag == null || flags.IndexOf(hideFlag) < 0)
			{
				WriteOpeningOfStartOfComplexElementTag(contentsStream, name);
				List<string> rgsAttrs = new List<string>();
				CollectAttributes(rgsAttrs, currentObject, node, flags);
				if (rgsAttrs.Count > 0)
				{
					rgsAttrs.Sort();
					for (int i = 0; i < rgsAttrs.Count; ++i)
						contentsStream.Write("{0}", rgsAttrs[i]);
				}
				WriteClosingOfStartOfComplexElementTag(contentsStream);
				DoChildren(contentsStream, currentObject, node, null);
				WriteEndOfComplexElementTag(contentsStream, name);
			}
			else
			{
				DoChildren(contentsStream, currentObject, node, null);
			}
			CheckForProgressAttribute(node);
		}

		private void CheckForProgressAttribute(XmlNode node)
		{
			string progressIncrement = XmlUtils.GetOptionalAttributeValue(node, "progressIncrement");
			if(progressIncrement != null)
			{
				if(UpdateProgress != null)
					UpdateProgress(this);
			}
		}

		private void WriteClosingOfStartOfComplexElementTag(TextWriter contentsStream)
		{
			if(m_format == "xml")
				contentsStream.Write(">");
			else
				contentsStream.Write(" ");
		}

		private void WriteEndOfComplexElementTag(TextWriter contentsStream, string name)
		{
			if (m_format == "xml")
			{
				contentsStream.WriteLine("</{0}>", name);
			}
			else
			{
			}
		}

		private void WriteOpeningOfStartOfComplexElementTag(TextWriter contentsStream, string name)
		{
			if(m_format == "xml")
				contentsStream.Write(String.Format("<{0}", name));
			else
				contentsStream.Write(String.Format("\r\n\\{0}", name));
		}

		/// <summary>
		/// Handle an element that does not need any processing, e.g. "&lt;MyElement&gt;", except for what is inside of it
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoLiteralElement(TextWriter contentsStream, CmObject currentObject, XmlNode node)
		{
			if (m_format == "xml")
			{
				string name = node.Name;
				contentsStream.Write(String.Format("<{0}", name));
				List<string> rgsAttrs = new List<string>();
				//add any literal attributes of the tag
				foreach (XmlAttribute attr in node.Attributes) // for Larry and PA
				{
					 //contentsStream.Write(String.Format(" {0}", attr.OuterXml));
					rgsAttrs.Add(String.Format(" {0}", attr.OuterXml));
				}
				CollectAttributes(rgsAttrs, currentObject, node, null);
				if (rgsAttrs.Count > 0)
				{
					rgsAttrs.Sort();
					for (int i = 0; i < rgsAttrs.Count; ++i)
						contentsStream.Write("{0}", rgsAttrs[i]);	// protect against embedded {n}.
				}
				contentsStream.Write(">");
				DoChildren(contentsStream, currentObject, node, null);
				contentsStream.WriteLine("</{0}>", name);
			}
			else
			{
				// what should we do for standard format output with literal XML stuff?
			}
		}

		/// <summary>
		/// handle an element which will appear only if the atomic reference attribute is not
		/// empty, and which only points to the referenced element.
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		/// <param name="flags"></param>
		protected void DoAtomicRefElement ( TextWriter contentsStream, CmObject currentObject,
			XmlNode node, string flags)
		{
			string property = XmlUtils.GetManditoryAttributeValue(node, "simpleProperty");
			//fix up the property name (should be fooOAHvo or fooRAHvo)
			if (property != "Owner" && property.LastIndexOf("Hvo") < 0)
				property = String.Format("{0}Hvo", property);

			// Andy's hack to make it work:
			// GetProperty returns the hvo as a string if there is a reference
			// Otherwise, it returns an Int32 object.
			object obj = GetProperty(currentObject, property);
			Type t = obj.GetType();
			if (t.FullName == "System.String")
			{
				string name = XmlUtils.GetManditoryAttributeValue(node, "name");
				if (m_format == "xml")
				{
					contentsStream.Write(String.Format("<{0} dst=\"{1}\"/>", name, obj.ToString()));
				}
				else
				{
					// We want something other than the Hvo number for standard format output...
					// Try ShortName.  (See LT-
					int hvo = Int32.Parse(obj.ToString());
					ICmObject cmo = CmObject.CreateFromDBObject(m_cache, hvo);
					string s = Icu.Normalize(cmo.ShortName, m_eIcuNormalizationMode);
					//string s = Icu.Normalize(obj.ToString(), m_eIcuNormalizationMode);
					contentsStream.WriteLine(String.Format("\r\n\\{0} {1}", name, s));
				}
			}
			else if (obj is CmObject)
			{
				string itemLabel = XmlUtils.GetAttributeValue(node, "itemLabel");
				string itemProperty = XmlUtils.GetAttributeValue(node, "itemProperty");
				if (!String.IsNullOrEmpty(itemLabel) && !String.IsNullOrEmpty(itemProperty))
				{
					object x = GetProperty(obj as CmObject, itemProperty);
					if (m_format == "xml")
						contentsStream.Write(String.Format("<{0} value=\"{1}\"/>", itemLabel, x.ToString()));
					else
						contentsStream.Write(String.Format("\\{0} {1}", itemLabel, x.ToString()));
				}
			}
		}

		private bool m_fExportPicturesAndMedia = false;

		/// <summary>
		/// Get/set whether or not pictures and media files should be exported (copied to the appropriate
		/// export directory).
		/// </summary>
		public bool ExportPicturesAndMedia
		{
			get { return m_fExportPicturesAndMedia; }
			set { m_fExportPicturesAndMedia = value; }
		}

		/// <summary>
		/// Add a mapping from one flid (real?) to another flid (virtual?).
		/// </summary>
		public void MapFlids(int flidReal, int flidVirtual)
		{
			m_mapFlids.Add(flidReal, flidVirtual);
		}

		/// <summary>
		/// Used to make an attribute which gives the GUID, hvo, or other simple property of a referenced or owned atomic object
		/// </summary>
		protected void DoAttributeIndirectElement(List<string> rgsAttrs, CmObject currentObject, XmlNode node)
		{

			string targetProperty = XmlUtils.GetManditoryAttributeValue(node, "target");
			CmObject target = GetProperty(currentObject, targetProperty) as CmObject;
			if (target != null)
			{
				DoAttributeElement(rgsAttrs, target, node);
				// If we're pointing to a picture or media file, copy the file if so desired.
				if (target is CmFile && m_fExportPicturesAndMedia)
				{
					string sSubdir = null;
					if (currentObject is CmPicture)
						sSubdir = "pictures";
					else if (currentObject is CmMedia)
						sSubdir = "audio";
					if (sSubdir != null && !String.IsNullOrEmpty(m_sOutputFilePath))
					{
						string sDir = Path.Combine(Path.GetDirectoryName(m_sOutputFilePath), sSubdir);
						if (!Directory.Exists(sDir))
							Directory.CreateDirectory(sDir);
						string sOldFilePath = (target as CmFile).AbsoluteInternalPath;
						string sNewFilePath = Path.Combine(sDir, Path.GetFileName(sOldFilePath));
						if (sOldFilePath != sNewFilePath &&
							!File.Exists(sNewFilePath) && File.Exists(sOldFilePath))
						{
							File.Copy(sOldFilePath, sNewFilePath);
						}
					}
				}
			}
		}

		protected void DoAttributeElement(List<string> rgsAttrs, CmObject currentObject, XmlNode node)
		{
			Debug.Assert(m_format == "xml");
			string propertyName = XmlUtils.GetManditoryAttributeValue(node, "simpleProperty");
			string attrName = XmlUtils.GetOptionalAttributeValue(node, "name");
			bool fOptional = XmlUtils.GetOptionalBooleanAttributeValue(node, "optional", false);
			string x = null;
			if (String.IsNullOrEmpty(propertyName))
			{
				Debug.Assert(!String.IsNullOrEmpty(attrName));
				string sValue = XmlUtils.GetOptionalAttributeValue(node, "value", String.Empty);
				x = GetAdjustedValueString(sValue, currentObject);
			}
			else
			{
				if (String.IsNullOrEmpty(attrName))
					attrName = propertyName;
				object obj;
				if (PropertyIsVirtual(currentObject, propertyName))
					obj = GetVirtualString(currentObject, propertyName, GetSingleWritingSystemDescriptor(node));
				else
					obj = GetProperty(currentObject, propertyName);
				if (fOptional && IsEmptyObject(obj))
					return;
				x = GetStringOfProperty(obj, node);
			}
			if (x == null)//review (zpu moform 9238)
				x = String.Empty;
			string sBefore = XmlUtils.GetOptionalAttributeValue(node, "before");
			if (!String.IsNullOrEmpty(sBefore))
				x = sBefore + x;
			string sAfter = XmlUtils.GetOptionalAttributeValue(node, "after");
			if (!String.IsNullOrEmpty(sAfter))
				x = x + sAfter;
			if (fOptional && String.IsNullOrEmpty(x))
				return;
			x = Icu.Normalize(x, m_eIcuNormalizationMode);
			x = XmlUtils.MakeSafeXmlAttribute(x);
			rgsAttrs.Add(String.Format(" {0}=\"{1}\"", attrName.Trim(), x));
		}

		private string GetAdjustedValueString(string sValue, CmObject currentObject)
		{
			if (String.IsNullOrEmpty(sValue))
				return sValue;
			if (sValue.Contains("${owner}"))
			{
				sValue = sValue.Replace("${owner}", currentObject.Owner.ShortName);
			}
			if (sValue.Contains("${version}"))
			{
				Assembly assembly = Assembly.GetEntryAssembly();
				if (assembly != null)
				{
					// Set the application version text
					object[] attributes = assembly.GetCustomAttributes(
						typeof(AssemblyFileVersionAttribute), false);
					string sVersion = (attributes != null && attributes.Length > 0) ?
						((AssemblyFileVersionAttribute)attributes[0]).Version :
						System.Windows.Forms.Application.ProductVersion;
					sValue = sValue.Replace("${version}", sVersion);
				}
			}
			if (sValue.Contains("${auxiliary-file}") && !String.IsNullOrEmpty(m_sAuxiliaryFilename))
			{
				sValue = sValue.Replace("${auxiliary-file}", "file://" + m_sAuxiliaryFilename.Replace('\\', '/'));
			}
			// TODO: any other special values to substitute?
			if (sValue.Contains("${dollar}"))
			{
				sValue = sValue.Replace("${dollar}", "$");
			}
			return sValue;
		}

		private bool IsEmptyObject(object obj)
		{
			if (obj == null)
				return true;
			else if (String.IsNullOrEmpty(obj.ToString()))
				return true;
			Type type = obj.GetType();
			if (type == typeof(int))
				return (int)obj == 0;
			else if (type == typeof(bool))
				return (bool)obj == false;
			else
				return false;
		}

		/// <summary>
		/// Just output the property as a string, without any regard to elements or attributes.
		/// Useful when just building an XHtml page.
		/// </summary>
		protected void DoStringOutput (TextWriter outputStream, CmObject currentObject,
			XmlNode node)
		{
			string x = GetSimplePropertyString(node, currentObject);
			if (x != null)
				WriteStringOutput(outputStream, node, x);
		}

		private void WriteStringOutput(TextWriter outputStream, XmlNode node, string x)
		{
			XmlTextWriter writer = new XmlTextWriter(outputStream);
			string before = XmlUtils.GetOptionalAttributeValue(node, "before");
			string after = XmlUtils.GetOptionalAttributeValue(node, "after");
			bool fIsXml = XmlUtils.GetOptionalBooleanAttributeValue(node, "isXml", false);
			if (before != null)
			{
				before = Icu.Normalize(before, m_eIcuNormalizationMode);
				writer.WriteString(before);
			}
			x = Icu.Normalize(x, m_eIcuNormalizationMode);
			if (fIsXml)
				writer.WriteRaw(x);
			else
				writer.WriteString(x);
			if (after != null)
			{
				after = Icu.Normalize(after, m_eIcuNormalizationMode);
				writer.WriteString(after);
			}
		}

		protected string GetSimplePropertyString(XmlNode node, CmObject currentObject)
		{
			string propertyName = XmlUtils.GetManditoryAttributeValue(node,"simpleProperty");
			string x;
			if (PropertyIsVirtual(currentObject, propertyName))
			{
				x = GetVirtualString(currentObject, propertyName, GetSingleWritingSystemDescriptor(node));
			}
			else
			{
				x = GetStringOfProperty(GetProperty(currentObject, propertyName), node);
			}
			return x;
		}

		private ITsString GetSimplePropertyTsString(XmlNode node, CmObject currentObject)
		{
			string propertyName = XmlUtils.GetManditoryAttributeValue(node, "simpleProperty");
			ITsString x;
			if (PropertyIsVirtual(currentObject, propertyName))
			{
				x = GetVirtualTsString(currentObject, propertyName, GetSingleWritingSystemDescriptor(node));
			}
			else
			{
				x = GetTsStringOfProperty(GetProperty(currentObject, propertyName), node);
			}
			return x;
		}

		/// <summary>
		/// Obtain a string value from a virtual object, or from a virtual property of a real
		/// object.
		/// </summary>
		/// <param name="currentObject"></param>
		/// <param name="propertyName"></param>
		/// <param name="ws">database id for a writing system</param>
		/// <returns></returns>
		private string GetVirtualString(CmObject currentObject, string propertyName, int ws)
		{
			string x = null;
			if (currentObject is SingleLexReference)
			{
				if (propertyName == "TypeAbbreviation")
				{
					x = (currentObject as SingleLexReference).TypeAbbreviation(ws,
						m_openForRefStack.Peek());
				}
				else if (propertyName == "TypeName")
				{
					x = (currentObject as SingleLexReference).TypeName(ws,
						m_openForRefStack.Peek());
				}
				else if (propertyName == "CrossReference")
				{
					x = (currentObject as SingleLexReference).CrossReference(ws);
				}
				else if (propertyName == "CrossReferenceGloss")
				{
					x = (currentObject as SingleLexReference).CrossReferenceGloss(ws);
				}
				else throw new RuntimeConfigurationException("'" + propertyName + "' is not handled by the code.");
			}
			else
			{
				// use reflection to obtain method and then the value?
			}
			return x;
		}

		private ITsString GetVirtualTsString(CmObject currentObject, string propertyName, int p)
		{
			return null;
		}

		/// <summary>
		/// For getting xml comments into the output
		/// </summary>
		/// <param name="outputStream"></param>
		/// <param name="node"></param>
		protected void DoCommentOutput(TextWriter outputStream, XmlNode node)
		{
			string x = node.InnerText;
			if (x != null)
			{
				outputStream.Write("<!--"+x+"-->");
			}
		}

		/// <summary>
		/// Just output the property as a string, without any regard to elements or attributes.
		/// Useful when just building an XHtml page.
		/// </summary>
		protected void DoXmlStringOutput (TextWriter outputStream, CmObject currentObject, XmlNode node)
		{
			string x = GetSimplePropertyString(node, currentObject);
			if (x != null)
			{
				// we want the real xml deal for things like MoMorphData:ParserParameters
				// outputStream.Write(MakeXmlSafe(x));
				x = Icu.Normalize(x, m_eIcuNormalizationMode);
				outputStream.Write(x);
			}
		}

		/// <summary>
		/// Get a formattable date string out as an attribute
		/// </summary>
		/// <param name="rgsAttrs"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoDateAttributeOutput(List<string> rgsAttrs, CmObject currentObject, XmlNode node)
		{
			Debug.Assert(m_format == "xml");

			string attrName = XmlUtils.GetManditoryAttributeValue(node, "name");
			string format = XmlUtils.GetManditoryAttributeValue(node, "format");
			string propertyName = XmlUtils.GetManditoryAttributeValue(node, "property");
			DateTime t = (DateTime)GetProperty(currentObject, propertyName);
			rgsAttrs.Add(String.Format(" {0}=\"{1}\"", attrName.Trim(), t.ToString(format)));
		}

		protected int[] LoadVirtualField(CmObject currentObject, string field)
		{
			object obj = GetProperty(currentObject, field);
			if (obj is int[])
			{
				return obj as int[];
			}
			else if (obj is System.Collections.ArrayList)
			{
				throw new InvalidOperationException("No array lists are to be used. in fact, as of this writing, FDO uses none at all.");
			}
			else if (obj is List<int>)
			{
				return (obj as List<int>).ToArray();
			}
			else if (obj is Set<int>)
			{
				return (obj as Set<int>).ToArray();
			}
			else if (obj is int)
			{
				int[] hvos = new int[1];
				hvos[0] = (int)obj;
				return hvos;
			}
			else
			{
				return new int[0];
			}
		}

		/// <summary>
		/// The &lt;refObjVector&gt; element is used when you want to expand the referenced
		/// objects as subparts of the same element in the output file.
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoReferenceObjVectorElement(TextWriter contentsStream,
			CmObject currentObject, XmlNode node)
		{
			if (m_format != "sf")
				throw new ConfigurationException("<refObjVector> is supported only for standard format output.");

			bool ordered = XmlUtils.GetBooleanAttributeValue(node, "ordered");
			string label = XmlUtils.GetOptionalAttributeValue(node, "itemLabel");
			if (label == null)
				label = "subobject";
			string field = XmlUtils.GetManditoryAttributeValue(node, "field");
			string sVirtual = XmlUtils.GetOptionalAttributeValue(node, "virtual");
			bool fVirtual = false;
			if (sVirtual != null)
			{
				sVirtual = sVirtual.ToLower();
				if (sVirtual == "true" || sVirtual == "t" || sVirtual == "yes" || sVirtual == "y")
					fVirtual = true;
			}
			int flid = (int)m_cache.MetaDataCacheAccessor.GetFieldId2(
				(uint)currentObject.ClassID, field, true);
			int[] hvos;
			if (flid <= 0)
			{
				if (fVirtual)
				{
					hvos = LoadVirtualField(currentObject, field);
				}
				else
				{
					throw new ConfigurationException("There is no field named '" + field + "' in " + currentObject.GetType().ToString() + ". Remember that fields are the actual CELLAR names, so they do not have FDO suffixes like OA or RS.");
				}
			}
			else
			{
				if (m_mapFlids.ContainsKey(flid))
					flid = m_mapFlids[flid];
				hvos = m_cache.GetVectorProperty(currentObject.Hvo, flid, false);
			}
			string property = XmlUtils.GetOptionalAttributeValue(node, "itemProperty");
			if (property == null)
				property = "ShortName";
			string wsProp = XmlUtils.GetOptionalAttributeValue(node, "itemWsProp");
			string sClassTag = XmlUtils.GetOptionalAttributeValue(node, "classtag");
			string labelWs = "";
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			foreach (int hvo in hvos)
			{
				CmObject co = (CmObject)CmObject.CreateFromDBObject(m_cache, hvo);
				if (String.IsNullOrEmpty(label) || String.IsNullOrEmpty(property))
				{
					contentsStream.WriteLine();
				}
				else
				{
					object obj = GetProperty(co, property);
					if (obj == null)
						continue;
					string s = Icu.Normalize(obj.ToString(), m_eIcuNormalizationMode);
					string separator = "";
					if (wsProp != null)
					{
						obj = GetProperty(co, wsProp);
						if (obj != null)
						{
							ILgWritingSystem ws = obj as ILgWritingSystem;
							if (ws == null)
							{
								int wsHvo = (int)obj;
								ws = LgWritingSystem.CreateFromDBObject(m_cache, wsHvo);
							}
							if (ws != null)
								labelWs = LabelString(ws);
						}
					}
					if (!String.IsNullOrEmpty(labelWs))
						separator = "_";
					string sTmp = String.Format("\r\n\\{0}{1}{2} {3}", label, separator, labelWs, s);
					contentsStream.Write(sTmp);
				}
				DumpObject(contentsStream, co, sClassTag);
			}
		}

		// Used to optimize certain virtual sequence properties.
		Dictionary<int, Dictionary<int, List<int>>> m_fastVirtuals = new Dictionary<int, Dictionary<int, List<int>>>();

		/// <summary>
		/// The &lt;refVector&gt; element is used when you just want to make a list of
		/// references to other elements that will be in the output file.
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoReferenceVectorElement(TextWriter contentsStream,
			CmObject currentObject, XmlNode node)
		{
			bool ordered = XmlUtils.GetBooleanAttributeValue(node, "ordered");
			string label = XmlUtils.GetOptionalAttributeValue(node, "itemLabel");
			if (label == null)
				label ="object";
			string field = XmlUtils.GetManditoryAttributeValue(node, "field");
			bool fXmlVirtual = XmlUtils.GetOptionalBooleanAttributeValue(node, "virtual", false);
			bool fWriteAsRelation = XmlUtils.GetOptionalBooleanAttributeValue(node, "writeAsRelation", false);
			//Debug.WriteLine ("<refVector field ="+field+">");

			int flid = (int)m_cache.MetaDataCacheAccessor.GetFieldId2(
				(uint)currentObject.ClassID, field, true);
			if (m_mapFlids.ContainsKey(flid))
				flid = m_mapFlids[flid];
			int[] hvos;
			if (flid <= 0)
			{
				if (fXmlVirtual)
				{
					hvos = LoadVirtualField(currentObject, field);
				}
				else
				{
					throw new ConfigurationException ("There is no field named '" + field + "' in "+currentObject.GetType().ToString()+". Remember that fields are the actual CELLAR names, so they do not have FDO suffixes like OA or RS.");
				}
			}
			else
			{
				Dictionary<int, List<int>> values = null;
				bool fMdcVirtual = m_cache.MetaDataCacheAccessor.get_IsVirtual((uint)flid);
				if (fMdcVirtual)
					values = GetCachedVirtuals(node, flid);
				if (values == null)
				{
					//int[] hvos = m_cache.GetVectorProperty(currentObject.Hvo, flid, true);
					// changed to false since for some reason, the data for POS slots and natural
					// classes was no longer being saved in the cache.
					hvos = m_cache.GetVectorProperty(currentObject.Hvo, flid, false);
				}
				else
				{
					List<int> hvoList;
					if (values.TryGetValue(currentObject.Hvo, out hvoList))
						hvos = hvoList.ToArray();
					else
						hvos = new int[0]; // assumes values is complete, if not found, empty.
				}
			}
			string property = XmlUtils.GetOptionalAttributeValue(node, "itemProperty");
			if (property == null)
				property = "ShortName";
			string wsProp = XmlUtils.GetOptionalAttributeValue(node, "itemWsProp");
			if (m_format == "xml")
			{
				int index = 0;
				bool fInternalTraits = XmlUtils.GetOptionalBooleanAttributeValue(node, "internalTraits", false);
				string sFieldMemberOf = XmlUtils.GetOptionalAttributeValue(node, "fieldMemberOf");
				string sFieldMemberOfTrait = XmlUtils.GetOptionalAttributeValue(node, "fieldMemberOfTrait");
				int flidMemberOf = 0;
				if (!String.IsNullOrEmpty(sFieldMemberOf) && !String.IsNullOrEmpty(sFieldMemberOfTrait))
				{
					flidMemberOf = (int)m_cache.MetaDataCacheAccessor.GetFieldId2((uint)currentObject.ClassID,
						sFieldMemberOf, true);
				}
				foreach (int hvo in hvos)
				{
					if (fWriteAsRelation)
					{
						string labelWs;
						string s;
						if (GetRefPropertyData(property, wsProp, hvo, out labelWs, out s))
						{
							if (ordered && hvos.Length > 1)
							{
								contentsStream.Write("<relation type=\"{0}\" ref=\"{1}\" order=\"{2}\">",
									XmlUtils.MakeSafeXmlAttribute(label), XmlUtils.MakeSafeXmlAttribute(s), index);
								++index;
							}
							else
							{
								contentsStream.Write("<relation type=\"{0}\" ref=\"{1}\">",
									XmlUtils.MakeSafeXmlAttribute(label), XmlUtils.MakeSafeXmlAttribute(s));
							}
							if (fInternalTraits)
							{
								if (!ordered || index <= 1)
								{
									contentsStream.WriteLine();
									DoChildren(contentsStream, currentObject, node, String.Empty);
								}
								if (flidMemberOf != 0)
								{
									int[] rghvoT = m_cache.GetVectorProperty(currentObject.Hvo, flidMemberOf, false);
									for (int i = 0; i < rghvoT.Length; ++i)
									{
										if (rghvoT[i] == hvo)
										{
											if (ordered && index > 1)
												contentsStream.WriteLine();
											contentsStream.WriteLine("<trait name=\"{0}\" value=\"true\"/>",
												XmlUtils.MakeSafeXmlAttribute(sFieldMemberOfTrait));
											break;
										}
									}
								}
							}
							contentsStream.WriteLine("</relation>");
						}
					}
					else
					{
						if (ordered)
						{
							contentsStream.WriteLine(String.Format("<{0} dst=\"{1}\" ord=\"{2}\"/>", label, GetIdString(hvo), index.ToString()));
							++index;
						}
						else
						{
							contentsStream.WriteLine(String.Format("<{0} dst=\"{1}\"/>", label, GetIdString(hvo)));
						}
					}
				}
				if (fWriteAsRelation && fInternalTraits)
					return;
			}
			else if (m_format == "sf")
			{
				foreach (int hvo in hvos)
				{
					string labelWs;
					string s;
					if (GetRefPropertyData(property, wsProp, hvo, out labelWs, out s))
					{
						string separator = String.Empty;
						if (!String.IsNullOrEmpty(labelWs))
							separator = "_";
						contentsStream.Write(String.Format("\r\n\\{0}{1}{2} {3}", label, separator, labelWs, s));
					}
				}
			}
			Debug.Assert(node.ChildNodes.Count == 0, "Child nodes are not supported in refVector elements");
		}

		private Dictionary<int, List<int>> GetCachedVirtuals(XmlNode node, int flid)
		{
			Dictionary<int, List<int>> values = null;
			// Some of these have an optimized way of loading.
			if (!m_fastVirtuals.TryGetValue(flid, out values))
			{
				string assembly = XmlUtils.GetOptionalAttributeValue(node, "assembly", null);
				string className = XmlUtils.GetOptionalAttributeValue(node, "class", null);
				string methodName = XmlUtils.GetOptionalAttributeValue(node, "method", null);
				if (!String.IsNullOrEmpty(assembly) && !String.IsNullOrEmpty(className)
					&& !String.IsNullOrEmpty(methodName))
				{
					values = new Dictionary<int, List<int>>();
					ReflectionHelper.CallStaticMethod(assembly, className, methodName,
						new object[] { m_cache, values });
				}
				// May still be null, but from now on, there's a definite value for this flid.
				m_fastVirtuals[flid] = values;
			}
			return values;
		}

		private bool GetRefPropertyData(string property, string wsProp, int hvo, out string labelWs, out string sData)
		{
			labelWs = String.Empty;
			sData = String.Empty;
			CmObject co = (CmObject)CmObject.CreateFromDBObject(m_cache, hvo);
			object obj = GetProperty(co, property);
			if (obj == null)
				return false;
			sData = Icu.Normalize(obj.ToString(), m_eIcuNormalizationMode);
			if (!String.IsNullOrEmpty(wsProp))
			{
				obj = GetProperty(co, wsProp);
				if (obj != null)
				{
					ILgWritingSystem ws = obj as ILgWritingSystem;
					if (ws == null)
					{
						int wsHvo = (int)obj;
						ws = LgWritingSystem.CreateFromDBObject(m_cache, wsHvo);
					}
					if (ws != null)
						labelWs = LabelString(ws);
				}
			}
			return true;
		}

		/// <summary>
		/// This is a "virtual" class that implements one member of a lexical relation in
		/// a form that is tractable for dumping out to MDF.
		/// </summary>
		class SingleLexReference : CmObject
		{
			protected LexReference m_lexRef;
			protected int m_hvoCrossRef;
			protected ICmObject m_coRef = null;
			protected int m_nMappingType = -1;

			public SingleLexReference(CmObject lexRef, int hvoCrossRef)
			{
				m_lexRef = lexRef as LexReference;
				m_hvoCrossRef = hvoCrossRef;
				m_cache = lexRef.Cache;
			}

			protected ICmObject CrossRefObject
			{
				get
				{
					if (m_coRef == null)
						m_coRef = CmObject.CreateFromDBObject(m_cache, m_hvoCrossRef);
					return m_coRef;
				}
			}

			/// <summary>
			/// Obtain the type of the current lex reference.
			/// </summary>
			public int MappingType
			{
				get
				{
					if (m_nMappingType < 0)
					{
						ILexRefType lrt =
							LexRefType.CreateFromDBObject(m_cache, m_lexRef.OwnerHVO);
						m_nMappingType = lrt.MappingType;
					}
					return m_nMappingType;
				}
			}

			public string MappingTypeName
			{
				get
				{
					LexRefType.MappingTypes maptype = (LexRefType.MappingTypes)MappingType;
					string s = Enum.Format(typeof(LexRefType.MappingTypes), maptype, "G");
					if (s.StartsWith("kmt"))
						return s.Substring(3);
					else
						return s;
				}
			}
			/// <summary>
			/// Access the database id of the specific reference element represented by the
			/// object.
			/// </summary>
			public int CrossRefHvo
			{
				get { return m_hvoCrossRef; }
				set
				{
					if (m_hvoCrossRef != value)
					{
						m_hvoCrossRef = value;
						m_coRef = null;
					}
				}
			}

			/// <summary>
			/// Obtain the value for the \lf field for the current LexEntry or LexSense as given
			/// by hvo, in the given writing system.
			/// </summary>
			/// <param name="ws">database id for a writing system</param>
			/// <param name="hvo">database id for a LexEntry or LexSense</param>
			/// <returns></returns>
			public string TypeAbbreviation(int ws, int hvo)
			{
				return m_lexRef.TypeAbbreviation(ws, hvo);
			}

			/// <summary>
			/// Obtain the value for the \lf field for the current LexEntry or LexSense as given
			/// by hvo, in the given writing system.
			/// </summary>
			/// <param name="ws">database id for a writing system</param>
			/// <param name="hvo">database id for a LexEntry or LexSense</param>
			/// <returns></returns>
			public string TypeName(int ws, int hvo)
			{
				return m_lexRef.TypeName(ws, hvo);
			}

			/// <summary>
			/// Obtain the value for the \lv field in the given writing system.
			/// </summary>
			/// <param name="ws">database id for a writing system</param>
			/// <returns></returns>
			public string CrossReference(int ws)
			{
				if (CrossRefObject is LexEntry)
				{
					return (CrossRefObject as LexEntry).HeadWordForWs(ws).Text;
				}
				else if (CrossRefObject is LexSense)
				{
					return (CrossRefObject as LexSense).OwnerOutlineNameForWs(ws).Text;
				}
				else
				{
					return "???";
				}
			}

			/// <summary>
			/// Obtain the value for the \le field in the given writing system.
			/// </summary>
			/// <param name="ws">database id for a writing system</param>
			/// <returns></returns>
			public string CrossReferenceGloss(int ws)
			{
				if (CrossRefObject is LexEntry)
				{
					LexEntry le = CrossRefObject as LexEntry;
					if (le.SensesOS.Count > 0)
					{
						return (le.SensesOS[0] as LexSense).Gloss.GetAlternative(ws);
					}
					else
					{
						return "";
					}
				}
				else if (CrossRefObject is LexSense)
				{
					return (CrossRefObject as LexSense).Gloss.GetAlternative(ws);
				}
				else
				{
					return "";
				}
			}

			/// <summary>
			/// Returns the order number, or an empty string if irrelevant.
			/// </summary>
			/// <param name="hvoMember"></param>
			/// <returns></returns>
			public string RefOrder
			{
				get
				{
					int nOrd = m_lexRef.SequenceIndex(CrossRefObject.Hvo);
					if (nOrd > 0)
						return nOrd.ToString();
					else
						return String.Empty;
				}
			}

			/// <summary>
			/// Return the Guid of the cross reference object.
			/// </summary>
			public Guid RefGuid
			{
				get { return m_cache.GetGuidFromId(m_hvoCrossRef); }
			}

			public string RefLIFTid
			{
				get
				{
					if (CrossRefObject is LexEntry)
					{
						return (CrossRefObject as LexEntry).LIFTid;
					}
					else if (CrossRefObject is LexSense)
					{
						return (CrossRefObject as LexSense).LIFTid;
					}
					else
					{
						return "???";
					}
				}
			}

			/// <summary>
			/// Return the Guid of the specific LexReference object.
			/// </summary>
			public Guid RelationGuid
			{
				get { return m_lexRef.Guid; }
			}

			/// <summary>
			/// Return the LiftResidueContent of the specific LexReference object.
			/// </summary>
			public string LiftResidueContent
			{
				get { return m_lexRef.LiftResidueContent; }
			}

			/// <summary>
			/// Return the dateCreated attribute value from the LiftResidue.
			/// </summary>
			public string LiftDateCreated
			{
				get { return m_lexRef.LiftDateCreated; }
			}

			/// <summary>
			/// Return the dateModified attribute value from the LiftResidue.
			/// </summary>
			public string LiftDateModified
			{
				get { return m_lexRef.LiftDateModified; }
			}

			/// <summary>
			/// Return the Name of the specific LexReference object.
			/// </summary>
			public MultiUnicodeAccessor Name
			{
				get { return m_lexRef.Name; }
			}

			/// <summary>
			/// Return the Comment of the specific LexReference object.
			/// </summary>
			public MultiStringAccessor Comment
			{
				get { return m_lexRef.Comment; }
			}
		}

		/// <summary>
		/// Process the vector of objects targeted by a LexReference object.
		/// </summary>
		/// <param name="rghvo">vector of database object ids</param>
		/// <param name="currentObject">current CmObject (a LexReference)</param>
		/// <param name="contentsStream">output stream wrapper</param>
		/// <param name="classNode">XML descriptor of how to output each object</param>
		protected void ProcessSingleLexReferences(int[] rghvo, CmObject currentObject,
			TextWriter contentsStream, XmlNode classNode)
		{
			Debug.Assert(rghvo.Rank == 1);
			SingleLexReference slr = new SingleLexReference(currentObject, rghvo[0]);
			int nMappingType = slr.MappingType;
			int hvoOpen = m_openForRefStack.Peek();
			for (int i = 0; i < rghvo.Length; ++i)
			{
				// If the LexReference vector element is the currently open object, ignore
				// it unless it's a sequence type relation.
				if (nMappingType != (int)LexRefType.MappingTypes.kmtSenseSequence &&
					nMappingType != (int)LexRefType.MappingTypes.kmtEntrySequence &&
					nMappingType != (int)LexRefType.MappingTypes.kmtEntryOrSenseSequence)
				{
					if (rghvo[i] == hvoOpen)
						continue;
				}
				slr.CrossRefHvo = rghvo[i];
				DoChildren(/*null,*/ contentsStream, slr, classNode, null);

				// If this is a tree type relation, show only the first element if the
				// currently open object is not the first element.
				if (nMappingType == (int)LexRefType.MappingTypes.kmtSenseTree ||
					nMappingType == (int)LexRefType.MappingTypes.kmtEntryTree ||
					nMappingType == (int)LexRefType.MappingTypes.kmtEntryOrSenseTree)
				{
					if (hvoOpen != rghvo[0])
						break;
				}
			}
		}

		/// <summary>
		/// If property is a string ending in 'OC' or 'OS', and the	part before that is a
		/// low-level property of currentObject, return true (and indicate the property ID).
		/// Otherwise return false.
		/// </summary>
		/// <param name="currentObject"></param>
		/// <param name="property"></param>
		/// <param name="?"></param>
		/// <returns></returns>
		bool PropertyIsRealSeqOrCollection(CmObject currentObject, string property,
			out int flid)
		{
			flid = 0; // in case we return false, makes compiler happy
			if (property == null || property.Length < 3)
				return false;
			string suffix = property.Substring(property.Length - 2, 2);
			if (suffix != "OC" && suffix != "OS")
				return false;
			string propName = property.Substring(0, property.Length - 2);
			IFwMetaDataCache mdc = currentObject.Cache.MetaDataCacheAccessor;
			flid = (int)mdc.GetFieldId2((uint)currentObject.ClassID, propName, true);
			return flid != 0;
		}

		/// <summary>
		/// Check whether the property name is a virtual property that we recognize.
		/// </summary>
		/// <param name="currentObject"></param>
		/// <param name="property"></param>
		/// <returns>true for a known virtual property, otherwise false</returns>
		bool PropertyIsVirtual(CmObject currentObject, string property)
		{
			if (currentObject is SingleLexReference)
			{
				switch (property)
				{
					case "TypeName":
					case "TypeAbbreviation":
					case "CrossReference":
					case "CrossReferenceGloss":
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Process a known virtual property for the given object.
		/// </summary>
		/// <param name="currentObject"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		protected void ProcessVirtualClassVector(TextWriter contentsStream,
			CmObject currentObject, string property, string virtClass)
		{
			XmlNode classNode = GetClassTemplateNode(virtClass);
			if (classNode == null)
			{
				throw new ConfigurationException("Unknown virtual class: " + virtClass);
			}
			if (virtClass == "SingleLexReference")
			{
				Debug.Assert(currentObject is LexReference);
				int flid = (int)m_cache.MetaDataCacheAccessor.GetFieldId2(
					(uint)currentObject.ClassID, property, true);
				if (m_mapFlids.ContainsKey(flid))
					flid = m_mapFlids[flid];
				int[] rghvo;
				if (flid <= 0)
					rghvo = LoadVirtualField(currentObject, property);
				else
					rghvo = m_cache.GetVectorProperty(currentObject.Hvo, flid, false);
				ProcessSingleLexReferences(rghvo, currentObject, contentsStream, classNode);
			}
			else
			{
				throw new ConfigurationException("Unsupported virtual class: " + virtClass);
			}
		}

		/// <summary>
		/// The &lt;objVector&gt; element is used when you want to embed the objects of the vector in
		/// this element.
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoObjectVectorElement(TextWriter contentsStream, CmObject currentObject,
			XmlNode node)
		{
			string property = XmlUtils.GetManditoryAttributeValue(node, "objProperty");
			string virtClass = XmlUtils.GetOptionalAttributeValue(node, "virtualclass");
			string countSpecifier = XmlUtils.GetOptionalAttributeValue(node, "count");
			string sField = XmlUtils.GetOptionalAttributeValue(node, "field");
			string sClassTag = XmlUtils.GetOptionalAttributeValue(node, "classtag");
			string sSep = XmlUtils.GetOptionalAttributeValue(node, "sep", String.Empty);
			//Debug.WriteLine ("<objVector property ="+property+">");
			int flid;
			if (virtClass != null)
			{
				ProcessVirtualClassVector(contentsStream, currentObject, property, virtClass);
			}
			else if (PropertyIsRealSeqOrCollection(currentObject, property, out flid))
			{
				if (m_mapFlids.ContainsKey(flid))
					flid = m_mapFlids[flid];
				// This avoids reloading a lot of stuff for simple vectors.
				ISilDataAccess sda = currentObject.Cache.MainCacheAccessor;
				int hvoObject = currentObject.Hvo;
				int chvo = sda.get_VecSize(hvoObject, flid);
				if (countSpecifier != null)
				{
					chvo = Math.Min(chvo, int.Parse(countSpecifier));
				}
				for (int ihvo = 0; ihvo < chvo; ihvo++)
				{
					if (ihvo > 0)
						contentsStream.Write(sSep);
					int hvo = sda.get_VecItem(currentObject.Hvo, flid, ihvo);
					CmObject obj = (CmObject)CmObject.CreateFromDBObject(currentObject.Cache, hvo, false);
					if (!String.IsNullOrEmpty(sField))
						contentsStream.Write("<field type=\"{0}\">", sField);
					DumpObject(contentsStream, obj, sClassTag);
					if (!String.IsNullOrEmpty(sField))
						contentsStream.WriteLine("</field>");
				}
			}
			else
			{
				IEnumerable vector = null;
				flid = (int)m_cache.MetaDataCacheAccessor.GetFieldId2(
					(uint)currentObject.ClassID, property, true);
				if (flid == 0)
					flid = XmlUtils.GetOptionalIntegerValue(node, "virtualflid", 0);
				if (flid != 0)
				{
					Dictionary<int, List<int>> values = GetCachedVirtuals(node, flid);
					if (values != null)
					{
						List<int> list;
						if (values.TryGetValue(currentObject.Hvo, out list))
							vector = list as IEnumerable;
						else
							vector = new int[0] as IEnumerable;
					}
				}
				if (vector == null)
				{
					vector = GetEnumerableFromProperty(currentObject, property);
				}
				m_openForRefStack.Push(currentObject.Hvo);
				try
				{
					bool fFirst = true;
					foreach (object orange in vector)
					{
						if (!fFirst)
							contentsStream.Write(sSep);
						if (!String.IsNullOrEmpty(sField))
							contentsStream.Write("<field type=\"{0}\">", sField);
						CmObject obj = orange as CmObject;
						if (obj == null)
						{
							obj = (CmObject)CmObject.CreateFromDBObject(currentObject.Cache, (int)orange, false);
						}
						DumpObject(contentsStream, obj, sClassTag);
						if (!String.IsNullOrEmpty(sField))
							contentsStream.WriteLine("</field>");
						fFirst = false;
					}
				}
				finally
				{
					m_openForRefStack.Pop();
				}
			}
			Debug.Assert(node.ChildNodes.Count ==0,
				"child nodes are not supported in objVector elements");
		}

		/// <summary>
		/// The &lt;objVector&gt; element is used when you want to embed the object of an atomic element, usually owned but works if only referenced.
		/// </summary>
		/// <param name="contentsStream"></param>
		/// <param name="currentObject"></param>
		/// <param name="node"></param>
		protected void DoObjectAtomicElement (TextWriter contentsStream,CmObject currentObject, XmlNode node)
		{
			//string mode = XmlUtils.GetOptionalAttributeValue(node, "mode");
			string property =XmlUtils.GetManditoryAttributeValue(node, "objProperty");
			string sClassTag = XmlUtils.GetOptionalAttributeValue(node, "classtag");
			//Debug.WriteLine ("<objAtomic property ="+property+">");
			currentObject = GetObjectFromProperty(currentObject, property);
			if (currentObject != null)
			{
				DumpObject(contentsStream, currentObject, sClassTag);
			}
			Debug.Assert(node.ChildNodes.Count ==0, "child nodes are not supported in objAtomic elements");
		}

		protected void DoGroupElement (TextWriter contentsStream,CmObject currentObject, XmlNode node)
		{
			string property =XmlUtils.GetManditoryAttributeValue(node, "objProperty");
			//Debug.WriteLine ("<group "+" "+property+">");
			CmObject ownedObject = GetObjectFromProperty(currentObject, property);

			if (ownedObject == null)	//nb: this code a late addition
				return;

			bool previousAssumeCacheSetting = false;//disabled because parserDump test fails 'cause morphtypes aren't preloaded m_cache.TestingOnly_AssumeCacheFullyLoaded;
			string sPreload = XmlUtils.GetOptionalAttributeValue(node, "preload");
			if (sPreload != null && sPreload.Length > 0)
			{
				m_cache.TestingOnly_AssumeCacheFullyLoaded = false;
				ownedObject.GetType().InvokeMember(sPreload,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
					BindingFlags.InvokeMethod, null, ownedObject, null);
				m_cache.TestingOnly_AssumeCacheFullyLoaded = true;
			}
			DoChildren(/*null,*/ contentsStream, ownedObject, node, null);
			m_cache.TestingOnly_AssumeCacheFullyLoaded = previousAssumeCacheSetting;
		}

		/// <summary>
		/// Handle attributes separately so that they can be sorted deterministically and fully
		/// written to the parent stream.
		/// </summary>
		/// <param name="rgsAttrs"></param>
		/// <param name="currentObject"></param>
		/// <param name="parentNode"></param>
		/// <param name="flags"></param>
		protected void CollectAttributes(List<string> rgsAttrs,
			CmObject currentObject, XmlNode parentNode, string flags)
		{
			foreach (XmlNode node in parentNode)
			{
				if (m_cancelNow)
					return;
				switch (node.Name)
				{
					case "attribute":
						DoAttributeElement(rgsAttrs, currentObject, node);
						break;
					case "attributeIndirect":
						DoAttributeIndirectElement(rgsAttrs, currentObject, node);
						break;
					case "dateAttribute":
						DoDateAttributeOutput(rgsAttrs, currentObject, node);
						break;
					case "element":
						CollectElementElementAttributes(rgsAttrs, currentObject, node, flags);
						break;
					case "call":
						CollectCallElementAttributes(rgsAttrs, currentObject, node);
						break;
					case "if":
						if (TestPasses(currentObject, node))
							CollectAttributes(rgsAttrs, currentObject, node, flags);
						break;
					case "ifnot":
						if (!TestPasses(currentObject, node))
							CollectAttributes(rgsAttrs, currentObject, node, flags);
						break;
					default:
						break;
				}
			}
		}

		protected void DoChildren(TextWriter contentsStream, CmObject currentObject,
			XmlNode parentNode, string flags)
		{
			foreach (XmlNode node in parentNode)
			{
				if (m_cancelNow)
					return;

				string s;
				switch (node.Name)
				{
				case "attribute":
				case "attributeIndirect":
				case "dateAttribute":
					break;	// handled in CollectAttributes().

				case "element":
					DoElementElement(contentsStream, currentObject, node, flags);
					break;
				case "call":
					DoCallElement(contentsStream, currentObject, node);
					break;

				case "numberElement":
					DoNumberElement(contentsStream, currentObject, node);
					break;
				case "booleanElement":
					DoBooleanElement(contentsStream, currentObject, node);
					break;
				case "multilingualStringElement":
					DoMultilingualStringElement(contentsStream, currentObject, node, flags);
					break;
				case "stringElement":
					DoStringElement(contentsStream, currentObject, node);
					break;
				case "group":
					DoGroupElement(contentsStream, currentObject, node);
					break;
				case "refVector":
					DoReferenceVectorElement(contentsStream, currentObject, node);
					break;
				case "refObjVector":
					DoReferenceObjVectorElement(contentsStream, currentObject, node);
					break;
				case "objVector":
					DoObjectVectorElement(contentsStream, currentObject, node);
					break;
				case "refAtomic":
					DoAtomicRefElement(contentsStream, currentObject, node, flags);
					break;
				case "objAtomic":
					DoObjectAtomicElement(contentsStream, currentObject, node);
					break;
				case "string":
					DoStringOutput(contentsStream, currentObject, node);
					break;
				case "xmlstring":
					DoXmlStringOutput(contentsStream, currentObject, node);
					break;
				/* I don't think these can be used for anything as # is illegal in xml element name. */
				case "#comment":
					break;
				case "#text":
					s = Icu.Normalize(node.InnerText, m_eIcuNormalizationMode);
					contentsStream.Write(s);
					break;
				case "comment":
					DoCommentOutput(contentsStream, node);
					break;
				case "text":
					s = Icu.Normalize(node.InnerText, m_eIcuNormalizationMode);
					contentsStream.Write(s);
					break;
				case "template":
					DoTemplateElement(contentsStream, node);
					break;
				case "newLine":
					contentsStream.WriteLine("");
					break;
				case "tab":
					contentsStream.Write("\x009");
					break;
				case "progress":
					CheckForProgressAttribute(node);
					break;
				case "space":
					contentsStream.Write(" ");
					break;
				case "customMultilingualStringElement":
					doCustomMultilingualStringElementSFM(contentsStream, currentObject, node,
						flags);
					break;
				case "customStringElement":
					DoCustomStringElement(contentsStream, currentObject, node, flags);
					break;
				case "if":
					DoIfElement(contentsStream, currentObject, node, true);
					break;
				case "ifnot":
					DoIfElement(contentsStream, currentObject, node, false);
					break;
				case "generateCustom":
					DoCustomElements(contentsStream, currentObject, node);
					break;
				default:
					DoLiteralElement(contentsStream, currentObject, node);
					break;
				}
			}
		}

		/// <summary>
		/// used for attributes, where you can't have multiple values coming out, as you can for elements
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected int GetSingleWritingSystemDescriptor (XmlNode node)
		{
			string wsSpec = XmlUtils.GetManditoryAttributeValue(node, "ws");
			if (wsSpec == "vernacular")
				return m_cache.LangProject.DefaultVernacularWritingSystem;
			else if (wsSpec == "analysis")
				return m_cache.LangProject.DefaultAnalysisWritingSystem;
			else
			{
				try
				{
					SpecialWritingSystemCodes code = (SpecialWritingSystemCodes)
													 Enum.Parse(typeof(SpecialWritingSystemCodes), wsSpec);
					return (int)code;
				}
				catch (Exception)
				{
					throw new ConfigurationException(
						"Cannot understand this writing system name. Use 'analysis', 'vernacular', or one of the SpecialWritingSystemCodes.");
				}
			}
		}

		protected object GetProperty(CmObject target, string property)
		{
			if (target == null)
			{
				return null;
			}

			if (property == "Hvo")
			{
				return GetIdString(target.Hvo);
			}
			else if (property == "Guid")
			{
				return (target.Guid.ToString());
			}
			else if (property == "Owner")
			{
				return (CmObject.CreateFromDBObject(m_cache, m_cache.GetOwnerOfObject(target.Hvo)));
			}
			else if (property == "IndexInOwner")
			{
				return target.IndexInOwner.ToString();
			}
			Type type = target.GetType();
			PropertyInfo  info = type.GetProperty(property,BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy  );
			if (property.StartsWith("custom"))
			{
				return GetCustomFieldValue(target, property);
			}
			if (info == null)
			{
				throw new ConfigurationException ("There is no public property named '" + property + "' in "+type.ToString()+". Remember, properties often end in a two-character suffix such as OA,OS,RA, or RS.");
			}
			object result = null;
			try
			{
				result = info.GetValue(target,null);
				if (property.EndsWith("Hvo"))
				{
					int hvo = (int)result;
					if (hvo > 0)
						return GetIdString(hvo);
				}
			}
			catch (Exception error)
			{
				throw new ApplicationException (string.Format("There was an error while trying to get the property {0}. One thing that has caused this in the past has been a database which was not migrated properly.", property), error);
			}
			return result;
		}

		private object GetCustomFieldValue(CmObject target, string property)
		{
			try
			{
				string sClass = m_cache.MetaDataCacheAccessor.GetClassName((uint)target.ClassID);
				uint flid = m_cache.MetaDataCacheAccessor.GetFieldId(sClass, property, true);
				if (flid == 0)
					return null;
				int type = m_cache.MetaDataCacheAccessor.GetFieldType(flid);
				string sView;
				switch (type)
				{
					case (int)CellarModuleDefns.kcptString:
					case (int)CellarModuleDefns.kcptBigString:
						TsStringAccessor tsa = new TsStringAccessor(m_cache, target.Hvo, (int)flid);
						return tsa;
					case (int)CellarModuleDefns.kcptMultiUnicode:
					case (int)CellarModuleDefns.kcptMultiBigUnicode:
						sView = sClass + '_' + property;
						MultiUnicodeAccessor mua = new MultiUnicodeAccessor(m_cache, target.Hvo, (int)flid, sView);
						return mua;
					case (int)CellarModuleDefns.kcptMultiString:
					case (int)CellarModuleDefns.kcptMultiBigString:
						sView = sClass + '_' + property;
						MultiStringAccessor msa = new MultiStringAccessor(m_cache, target.Hvo, (int)flid, sView);
						return msa;
				}
			}
			catch
			{
			}
			return null;
		}

		protected object GetMethodResult(CmObject target, string methodName, object[] args)
		{
			Type type = target.GetType();
			MethodInfo mi = type.GetMethod(methodName);
			if (mi == null)
				throw new ConfigurationException ("There is no public method named '" + methodName + ".");
			object result = null;
			try
			{
				result = mi.Invoke(target, args);
			}
			catch (Exception error)
			{
				throw new ApplicationException (string.Format("There was an error while executing the method {0}.", methodName), error);
			}
			return result;
		}

		protected string GetStringOfProperty (Object propertyObject, XmlNode node)
		{
			if (propertyObject == null)
				return "";
			else if (propertyObject is MultiUnicodeAccessor || propertyObject is MultiStringAccessor)
				return GetStringOfProperty(propertyObject, GetSingleWritingSystemDescriptor(node));
			else
				return GetStringOfProperty(propertyObject, -1);
		}

		protected string GetStringOfProperty (Object propertyObject, int alternative)
		{
			if (propertyObject == null)
				return null;
			Type type =propertyObject.GetType();
			if (type == typeof(MultiUnicodeAccessor))
			{
				MultiUnicodeAccessor accessor = (MultiUnicodeAccessor) propertyObject;

				if (alternative <= (int)SpecialWritingSystemCodes.BestAnalysis)
				{
					SpecialWritingSystemCodes code = (SpecialWritingSystemCodes)Enum.Parse(typeof (SpecialWritingSystemCodes), alternative.ToString());
					return ConvertNoneFoundStringToBlank(accessor.GetAlternative(code));
				}
				else
				{
					if (MightHaveAlternative(accessor.Flid, alternative))
						return accessor.GetAlternative(alternative);
					else return null;
				}
			}
			else if (type == typeof(MultiStringAccessor))
			{
				MultiStringAccessor accessor = (MultiStringAccessor) propertyObject;
				if (alternative <= (int)SpecialWritingSystemCodes.BestAnalysis)
				{
					return ConvertNoneFoundStringToBlank(accessor.BestAnalysisAlternative.Text);
				}
				else
				{
					if (MightHaveAlternative(accessor.Flid, alternative))
						return accessor.GetAlternative(alternative).Text;
					else
						return null;
				}
			}
			else if (type == typeof(Dictionary<string, string>))
			{
				Dictionary<string, string> dict = (Dictionary<string, string>)propertyObject;
				string value;

				ILgWritingSystem ws = FindWritingSystem(alternative);
				if (dict.TryGetValue(ws.Abbreviation, out value))
				{
					return value;
				}
				else
				{
					return null;
				}
			}
			else if (type == typeof(String))
			{
				return propertyObject.ToString();
			}
			else if (type == typeof(TsStringAccessor))
			{
				TsStringAccessor contents = (TsStringAccessor)propertyObject;
				return contents.Text;
			}
			else if (type == typeof(int))
			{
				return propertyObject.ToString();
			}
			else if (type == typeof(bool))
			{
				return ((bool)propertyObject) ? "1" : "0";
			}
			else if (type == typeof(System.DateTime))
			{
				System.DateTime dt = (System.DateTime)propertyObject;
				if (dt.Year > 1900)		// Converting 1/1/1 to local time crashes.
					dt = dt.ToLocalTime();
				string s;
				if(alternative==1)
					s = dt.ToString("yyyy-MM-dd");
				else
					s = dt.ToString("dd/MMM/yyyy");
				return s;
			}
			else if (type == typeof(System.Guid))
			{
				return propertyObject.ToString();
			}
			else if (type == typeof(LgWritingSystem))
			{
				return (propertyObject as LgWritingSystem).ICULocale;
			}
			throw new ConfigurationException ("Sorry, XDumper can not yet handle attributes of this class: '"+type.ToString()+"'.");
		}

		private string ConvertNoneFoundStringToBlank(string str)
		{
			// at least for the Morph Sketch, we do not want to see lots of asterisks.
			// rather, we check for blanks and convert blanks to appropriate text
			if (str == "***")
				str = "";
			return str;
		}

		private ITsString GetTsStringOfProperty(object propertyObject, int ws)
		{
			if (propertyObject == null)
				return null;
			if (propertyObject is MultiStringAccessor)
			{
				MultiStringAccessor accessor = (MultiStringAccessor)propertyObject;
				return accessor.GetAlternative(ws).UnderlyingTsString;
			}
			else if (propertyObject is TsStringAccessor)
			{
				TsStringAccessor contents = (TsStringAccessor)propertyObject;
				return contents.UnderlyingTsString;
			}
			else if (propertyObject is ITsString)
			{
				return propertyObject as ITsString;
			}
			else
			{
				return null;
			}
		}

		private ITsString GetTsStringOfProperty(object propertyObject, XmlNode node)
		{
			if (propertyObject == null)
				return null;
			if (propertyObject is MultiStringAccessor)
				return GetTsStringOfProperty(propertyObject, GetSingleWritingSystemDescriptor(node));
			else
				return GetTsStringOfProperty(propertyObject, -1);
		}

		protected ILgWritingSystem FindWritingSystem(int hvo)
		{
			return LgWritingSystem.CreateFromDBObject(m_cache, hvo) as ILgWritingSystem;
		}

		protected CmObject GetObjectFromProperty(CmObject target,string property)
		{
			return (CmObject)GetProperty(target, property);
		}

		protected IEnumerable GetEnumerableFromProperty(CmObject target,string property)
		{
			return (IEnumerable)GetProperty(target, property);
		}

		/// <summary>
		/// When true, causes FXT to produce guids instead of database IDs
		/// </summary>
		/// <returns></returns>
		public bool OutputGuids
		{
			set
			{
				CheckDisposed();

				m_outputGuids= value;
			}
		}

		public XmlDocument FxtDocument
		{
			get
			{
				CheckDisposed();
				return m_fxtDocument;
			}
			set
			{
				CheckDisposed();
				m_fxtDocument = value;
				// Clear collections based on the document.
				m_classNameToclassNode.Clear();
				m_openForRefStack.Clear();
				m_htCustomFields.Clear();
			}
		}

		/// <summary>
		/// outputs the normal database id, or the guid, depending on the OutputGuids setting.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected string GetIdString(int hvo)
		{
			if (!m_outputGuids)
			{
				return hvo.ToString();
			}
			else
			{
				return m_cache.GetGuidFromId(hvo).ToString();
			}

		}

		public int GetProgressMaximum()
		{
			CheckDisposed();

			//hack
			return m_cache.LangProject.LexDbOA.EntriesOC.Count + 1;
		}
	}
}
