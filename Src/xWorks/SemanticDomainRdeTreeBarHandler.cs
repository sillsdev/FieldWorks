using System.Drawing;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class is instantiated by reflection, based on the setting of the treeBarHandler in the
	/// SemanticDomainList clerk in the RDE toolConfiguration.xml.
	/// </summary>
	class SemanticDomainRdeTreeBarHandler : PossibilityTreeBarHandler
	{
		/// <summary>
		/// Need a constructor with no parameters for use with DynamicLoader
		/// </summary>
		public SemanticDomainRdeTreeBarHandler()
		{
		}

		internal override void Init(Mediator mediator, XmlNode node)
		{
			base.Init(mediator, node);

			var window = (XWindow)mediator.PropertyTable.GetValue("window");
			var informationBar = new PaneBar { Visible = false };
			window.TreeBarControl.AddHeaderControl(informationBar);
			SetInfoBarText(node, informationBar);
			window.TreeBarControl.ShowHeaderControl();
		}

		private void SetInfoBarText(XmlNode handlerNode, PaneBar infoBar)
		{
			var stringTable = m_mediator.StringTbl;

			var titleStr = string.Empty;
			// See if we have an AlternativeTitle string table id for an alternate title.
			var titleId = XmlUtils.GetAttributeValue(handlerNode, "altTitleId");
			if (titleId != null)
			{
				XmlViewsUtils.TryFindString(stringTable, "AlternativeTitles", titleId, out titleStr);
				// if they specified an altTitleId, but it wasn't found, they need to do something,
				// so just return *titleId*
				if (titleStr == null)
					titleStr = titleId;
			}
			infoBar.Text = titleStr;
		}

		/// <summary>
		/// If we are controlling the RecordBar, we want the optional info bar visible.
		/// </summary>
		protected override void UpdateHeaderVisibility()
		{
			var window = (XWindow)m_mediator.PropertyTable.GetValue("window");
			if (window == null || window.IsDisposed)
				return;

			if (IsShowing)
				window.TreeBarControl.ShowHeaderControl();
		}

		/// <summary>
		/// A trivial override to use a special method to get the names of items.
		/// For semantic domain in this tool we want to display a sense count (if non-zero).
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="font"></param>
		/// <returns></returns>
		protected override string GetTreeNodeLabel(ICmObject obj, out Font font)
		{
			var baseName = base.GetTreeNodeLabel(obj, out font);
			var sd = obj as ICmSemanticDomain;
			if (sd == null)
				return baseName; // pathological defensive programming
			int senseCount = (from item in sd.ReferringObjects where item is ILexSense select item).Count();
			if (senseCount == 0)
				return baseName;
			return baseName + " (" + senseCount + ")";
		}
	}
}
