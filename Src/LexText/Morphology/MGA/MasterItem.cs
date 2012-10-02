using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	public class MasterItem : IFWDisposable
	{
		protected GlossListTreeView.ImageKind m_eKind;
		protected string m_abbrev;
		protected string m_abbrevWs;
		protected string m_term;
		protected string m_termWs;
		protected string m_def;
		protected string m_defWs;
		protected List<MasterItemCitation> m_citations;
		protected XmlNode m_node;
		protected bool m_fInDatabase = false;
		protected IFsFeatDefn m_featDefn = null;

		public MasterItem()
		{
			m_citations = new List<MasterItemCitation>();
		}
		public MasterItem(XmlNode node, GlossListTreeView.ImageKind kind, string sTerm)
		{
			m_node = node;
			m_eKind = kind;
			m_term = sTerm;

			m_citations = new List<MasterItemCitation>();

			XmlNode nd = node.SelectSingleNode("abbrev");
			m_abbrevWs = XmlUtils.GetManditoryAttributeValue(nd, "ws");
			m_abbrev = nd.InnerText;

			nd = node.SelectSingleNode("term");
			m_termWs = XmlUtils.GetManditoryAttributeValue(nd, "ws");
			m_term = nd.InnerText;

			nd = node.SelectSingleNode("def");
			if (nd != null)
			{
				m_defWs = XmlUtils.GetManditoryAttributeValue(nd, "ws");
				m_def = nd.InnerText;
			}

			foreach (XmlNode citNode in node.SelectNodes("citation"))
			{
				string sWs = XmlUtils.GetOptionalAttributeValue(citNode, "ws");
				if (sWs == null)
					sWs = "en";
				m_citations.Add(new MasterItemCitation(sWs, citNode.InnerText));
			}
		}

		/// <summary>
		/// figure out if the feature represented by the node is already in the database
		/// </summary>
		/// <param name="cache">database cache</param>
		public virtual void DetermineInDatabase(FdoCache cache)
		{
		}
		public virtual bool KindCanBeInDatabase()
		{
			CheckDisposed();

			return (m_eKind == GlossListTreeView.ImageKind.radio ||
				m_eKind == GlossListTreeView.ImageKind.radioSelected ||
				m_eKind == GlossListTreeView.ImageKind.checkBox ||
				m_eKind == GlossListTreeView.ImageKind.checkedBox ||
				m_eKind == GlossListTreeView.ImageKind.userChoice ||
				m_eKind == GlossListTreeView.ImageKind.complex);
		}

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
		~MasterItem()
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

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public virtual void AddToDatabase(FdoCache cache)
		{
		}

		public IFsFeatDefn FeatureDefn
		{
			get
			{
				CheckDisposed();

				return m_featDefn;
			}
		}

		public XmlNode Node
		{
			get
			{
				CheckDisposed();
				return m_node;
			}
		}

		public bool InDatabase
		{
			get
			{
				CheckDisposed();
				return m_fInDatabase;
			}
		}
		public bool IsChosen
		{
			get
			{
				CheckDisposed();

				return (m_eKind == GlossListTreeView.ImageKind.radioSelected ||
					m_eKind == GlossListTreeView.ImageKind.checkedBox);
			}
		}

		public override string ToString()
		{
			CheckDisposed();
			if (InDatabase)
				return String.Format(MGAStrings.ksX_InFwProject, m_term);
			else
				return m_term;
		}
		public void ResetDescription(RichTextBox rtbDescription)
		{
			CheckDisposed();

			rtbDescription.Clear();

			Font original = rtbDescription.SelectionFont;
			Font fntBold = new Font(original.FontFamily, original.Size, FontStyle.Bold);
			Font fntItalic = new Font(original.FontFamily, original.Size, FontStyle.Italic);
			rtbDescription.SelectionFont = fntBold;
			rtbDescription.AppendText(m_term);
			rtbDescription.AppendText("\n\n");

			rtbDescription.SelectionFont = (string.IsNullOrEmpty(m_def)) ?
				fntItalic : original;
			rtbDescription.AppendText((string.IsNullOrEmpty(m_def)) ?
				MGAStrings.ksNoDefinitionForItem : m_def);
			rtbDescription.AppendText("\n\n");

			if (m_citations.Count > 0)
			{
				rtbDescription.SelectionFont = fntItalic;
				rtbDescription.AppendText(MGAStrings.ksReferences);
				rtbDescription.AppendText("\n\n");

				rtbDescription.SelectionFont = original;
				foreach (MasterItemCitation mifc in m_citations)
					mifc.ResetDescription(rtbDescription);
			}
		}

	}
	public class MasterItemCitation
	{
		private string m_ws;
		private string m_citation;

		public string WS
		{
			get { return m_ws; }
		}

		public string Citation
		{
			get { return m_citation; }
		}

		public MasterItemCitation(string ws, string citation)
		{
			m_ws = ws;
			m_citation = citation;
		}

		public void ResetDescription(RichTextBox rtbDescription)
		{
			rtbDescription.AppendText(String.Format(MGAStrings.ksBullettedItem, m_citation,
				System.Environment.NewLine));
		}
	}
}
