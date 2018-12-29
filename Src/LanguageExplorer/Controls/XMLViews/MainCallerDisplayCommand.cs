// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	[DebuggerDisplay("main={MainElement.Name},caller={Caller.Name}")]
	internal class MainCallerDisplayCommand : DisplayCommand
	{
		// The main node, usually one that has a "layout" attribute; obj or seq.
		//
		// A calling node, which may (if non-null) have a "param" attribute that overrides
		// the "layout" one in m_mainNode; part ref.
		//
		// If true, bypass the normal strategy, and call ProcessFrag
		// using the given hvo and m_mainNode as the fragment.
		//
		/// <summary>
		/// The value of wsForce for the vc when the MainCallerDisplayCommand was needed (restored
		/// for the duration of building its parts).
		/// </summary>
		private readonly int m_wsForce;

		internal MainCallerDisplayCommand(XElement mainElement, XElement caller, bool fUserMainAsFrag, int wsForce)
		{
			MainElement = mainElement;
			Caller = caller;
			UseMainAsFrag = fUserMainAsFrag;
			m_wsForce = wsForce;
		}

		internal XElement MainElement { get; }

		internal XElement Caller { get; }

		internal bool UseMainAsFrag { get; }

		// Make it work sensibly as a hash key
		public override bool Equals(object obj)
		{
			var other = obj as MainCallerDisplayCommand;
			if (other == null)
			{
				return false;
			}
			return other.MainElement == MainElement && other.Caller == Caller && other.UseMainAsFrag == UseMainAsFrag && other.m_wsForce == m_wsForce;
		}

		// Make it work sensibly as a hash key.
		public override int GetHashCode()
		{
			return MainElement.GetHashCode() + (UseMainAsFrag ? 1 : 0) + (Caller == null ? 0 : Caller.GetHashCode()) + m_wsForce;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			string layoutName;
			var node = GetNodeForChild(out layoutName, fragId, vc, hvo);
			var oldWsForce = vc.WsForce;
			var logStream = vc.LogStream;
			try
			{
				// Force the correct WsForce for the duration of the command.
				// Normally, this is already correct, since the display command is invoked
				// as a result of calling AddObjVecItems or similar in a context where WsForce is already active.
				// However, when it is invoked independently as a result of PropChanged, that setting may need to be restored.
				if (vc.WsForce != m_wsForce) // important because some VCs don't allow setting this.
				{
					vc.WsForce = m_wsForce;
				}
				if (logStream != null)
				{
					logStream.WriteLine($"Display {hvo} using layout {layoutName} which found {node}");
					logStream.IncreaseIndent();
				}
				string flowType = null;
				string style = null;
				if (node.Name == "layout")
				{
					// layouts may have flowType and/or style specified.
					flowType = XmlUtils.GetOptionalAttributeValue(node, "flowType", null);
					style = XmlUtils.GetOptionalAttributeValue(node, "style", null);
					if (style != null && flowType == null)
					{
						flowType = "span";
					}
					if (flowType == "para")
					{
						vc.GetParagraphStyleIfPara(hvo, ref style);
					}
				}
				if (flowType != null)
				{
					// Surround the processChildren call with an appropriate flow object, and
					// if requested apply a style to it.
					if (style != null && flowType != "divInPara")
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, style);
					}
					switch (flowType)
					{
						case "span":
							vwenv.OpenSpan();
							break;
						case "para":
							vwenv.OpenParagraph();
							break;
						case "div":
							vwenv.OpenDiv();
							break;
						case "none":
							break;
						case "divInPara":
							vwenv.CloseParagraph();
							vwenv.OpenDiv();
							break;
					}
					PrintNodeTreeStep(hvo, node);
					ProcessChildren(fragId, vc, vwenv, node, hvo);
					switch (flowType)
					{
						default:
							vwenv.CloseSpan();
							break;
						case "para":
							vwenv.CloseParagraph();
							break;
						case "div":
							vwenv.CloseDiv();
							break;
						case "none":
							break;
						case "divInPara":
							vwenv.CloseDiv();
							vwenv.OpenParagraph();
							// If we end up with an empty paragraph, try to make it disappear.
							vwenv.EmptyParagraphBehavior(1);
							break;
					}
				}
				else
				{
					// no flow/style specified
					PrintNodeTreeStep(hvo, node);
					ProcessChildren(fragId, vc, vwenv, node, hvo, Caller);
				}
			}
			finally
			{
				if (vc.WsForce != oldWsForce) // important because some VCs don't allow setting this.
				{
					vc.WsForce = oldWsForce; // restore.
				}
			}
			logStream?.DecreaseIndent();
		}

		internal static void PrintNodeTreeStep(int hvo, XElement node)
		{
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			var clsid = info.TargetClass(vc);
			if (clsid == 0)
			{
				return; // or assert? an object prop should have a dest class.
			}
			string layoutName;
			var node = GetNodeForChildClass(out layoutName, fragId, vc, clsid);
			DetermineNeededFieldsForChildren(vc, node, null, info);
		}

		internal XElement GetNodeForChild(out string layoutName, int fragId, XmlVc vc, int hvo)
		{
			XElement node;
			XElement callingFrag;
			layoutName = null;
			layoutName = GetLayoutName(out callingFrag, out node);
			if (node == null)
			{
				node = vc.GetNodeForPart(hvo, layoutName, true);
			}
			node = XmlVc.GetDisplayNodeForChild(node, callingFrag, vc.LayoutCache);
			return node;
		}

		/// <summary>
		/// Almost the same as GetDisplayNodeForChild, but depends on knowing the class of child
		/// rather than the actual child instance.
		/// </summary>
		internal XElement GetNodeForChildClass(out string layoutName, int fragId, XmlVc vc, int clsid)
		{
			XElement node;
			XElement callingFrag;
			layoutName = null;
			layoutName = GetLayoutName(out callingFrag, out node);
			if (node == null)
			{
				node = vc.GetNodeForPart(layoutName, true, clsid);
			}
			node = XmlVc.GetDisplayNodeForChild(node, callingFrag, vc.LayoutCache);
			return node;
		}

		internal virtual string GetLayoutName(out XElement callingFrag, out XElement node)
		{
			string layoutName = null;
			node = null;
			callingFrag = MainElement;
			if (UseMainAsFrag)
			{
				node = callingFrag;
			}
			else
			{
				var caller = Caller;
				layoutName = XmlVc.GetLayoutName(callingFrag, caller);
			}
			return layoutName;
		}
	}
}