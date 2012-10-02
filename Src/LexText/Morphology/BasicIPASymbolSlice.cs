using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public partial class BasicIPASymbolSlice : StringSlice, IVwNotifyChange
	{
		private int m_ws = -1;
		private ISilDataAccess m_sda;
		private bool m_fJustChangedDescription = false;
		private bool m_fJustChangedFeatures = false;
		private XmlDocument m_IPAMapperDocument;

		public BasicIPASymbolSlice()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Constructor invoked via the editor="customWithParams" slice XML configuration
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="editor"></param>
		/// <param name="flid"></param>
		/// <param name="node"></param>
		/// <param name="obj"></param>
		/// <param name="stringTbl"></param>
		/// <param name="persistenceProvider"></param>
		/// <param name="ws"></param>
		public BasicIPASymbolSlice(FdoCache cache, string editor, int flid,
						System.Xml.XmlNode node, ICmObject obj, StringTable stringTbl,
						IPersistenceProvider persistenceProvider, int ws)
			: base(obj.Hvo, flid, ws)
		{
			m_obj = obj; // is PhPhoneme
			//m_persistenceProvider = persistenceProvider;
			m_ws = ws;
			//m_node = node;
			m_configurationNode = node;
			m_sda = cache.MainCacheAccessor;
			m_sda.AddNotification(this);
			this.Disposed += new EventHandler(BasicIPASymbolSlice_Disposed);
			m_IPAMapperDocument = new XmlDocument();
			string sIPAMapper = Path.Combine(DirectoryFinder.TemplateDirectory, PhPhoneme.ksBasicIPAInfoFile);
			m_IPAMapperDocument.Load(sIPAMapper);
		}

		void BasicIPASymbolSlice_Disposed(object sender, EventArgs e)
		{
			m_sda.RemoveNotification(this);
			m_IPAMapperDocument = null;
		}

		/// <summary>
		/// Listen for change to basic IPA symbol
		/// If description and/or features are empty, try to supply the values associated with the symbol
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// We only want to do something when the basic IPA symbol changes
			if (tag != (int)PhPhoneme.PhPhonemeTags.kflidBasicIPASymbol)
				return;
			IPhPhoneme phoneme = m_obj as IPhPhoneme;
			if (phoneme == null)
				return; // something went wrong...

			m_fJustChangedDescription = phoneme.SetDescriptionBasedOnIPA(m_IPAMapperDocument, m_fJustChangedDescription);

			m_fJustChangedFeatures = phoneme.SetFeaturesBasedOnIPA(m_IPAMapperDocument, m_fJustChangedFeatures);
		}

	}
}
