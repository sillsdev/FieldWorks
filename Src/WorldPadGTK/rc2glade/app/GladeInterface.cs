using System;
using System.Collections;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
[XmlRoot(ElementName="glade-interface")]
public class GladeInterface
{
	[XmlElement(ElementName="widget")]
	public Widget[] m_dialogs;
	[XmlIgnore]
	public ArrayList m_dialogsList = new ArrayList();
	[XmlIgnore]
	public ArrayList m_containers = new ArrayList();
	[XmlIgnore]
	private WidgetFactory m_factory;
	[XmlIgnore]
	public Stack m_ancestors = new Stack();
	[XmlIgnore]
	public double m_hScale;
	[XmlIgnore]
	public double m_vScale;

	// Default constructor required for serialization
	public GladeInterface()
	{
	}

	public GladeInterface(double _hScale, double _vScale)
	{
		m_hScale = _hScale;
		m_vScale = _vScale;
		m_factory = new WidgetFactory(this);
	}

	public void CloseContainer()
	{
		//Console.WriteLine("CloseContainer called");

		// convert each container widget's children ArrayList to an array
		foreach (Container container in m_containers)
		{
			//Console.WriteLine("container.m_name: {0}", container.m_name);
			container.ChildrenToArray();
		}

		m_containers.Clear();
		m_ancestors.Clear();
	}

	public void AddWidget(Match _match)
	{
		Widget w = m_factory.CreateWidget(_match);

		w.AddProperties();

		w.AddToParent();

		w.AddChildren();
	}

	public void DialogsToArray()
	{
		m_dialogs = (Widget[])m_dialogsList.ToArray(typeof(Widget));
	}

	public void SetDialogTitle(string _title)
	{
		//int items = m_dialogsList.Count;
		//GtkDialog dialog = (GtkDialog)m_dialogsList[items - 1];
		GtkDialog dialog = (GtkDialog)m_dialogsList[m_dialogsList.Count - 1];
		dialog.AddTitleProperty(_title);
	}
}


[Serializable]
[XmlInclude(typeof(GtkFrame))]
[XmlInclude(typeof(GtkLabel))]
[XmlInclude(typeof(GtkLabelForGtkFrame))]
[XmlInclude(typeof(GtkButton))]
[XmlInclude(typeof(GtkButtonWithGtkImage))]
[XmlInclude(typeof(GtkImageForGtkButton))]
[XmlInclude(typeof(GtkColorButton))]
[XmlInclude(typeof(GtkEntry))]
[XmlInclude(typeof(GtkTreeView))]
[XmlInclude(typeof(GtkComboBox))]
[XmlInclude(typeof(GtkComboBoxEntry))]
[XmlInclude(typeof(GtkCheckButton))]
[XmlInclude(typeof(GtkDialog))]
[XmlInclude(typeof(GtkAlignment))]
[XmlInclude(typeof(GtkFixed))]
[XmlInclude(typeof(GtkImage))]
[XmlInclude(typeof(GtkRadioButton))]
[XmlInclude(typeof(GtkProgressBar))]
[XmlInclude(typeof(GtkSpinButton))]
[XmlInclude(typeof(GtkCalendar))]
[XmlInclude(typeof(GtkHSeparator))]
[XmlInclude(typeof(GtkNotebook))]
[XmlInclude(typeof(GtkLabelForGtkNotebook))]
//[XmlInclude(typeof(GtkFixedForGtkNotebook))]
[XmlInclude(typeof(Unknown))]
abstract public class Widget
{
	[XmlAttribute(AttributeName="class")]
	public string m_class;
	[XmlAttribute(AttributeName="id")]
	public string m_name;

	[XmlElement(ElementName="property")]
	public Property[] m_properties;
	[XmlElement(ElementName="child")]
	public Child[] m_children;

	[XmlIgnore]
	protected Container m_parent;
	[XmlIgnore]
	private Rectangle m_rectangle;
	[XmlIgnore]
	private int m_newLeft;
	[XmlIgnore]
	private int m_newTop;
	[XmlIgnore]
	public int m_yOffset;

	[XmlIgnore]
	protected GladeInterface m_root;
	[XmlIgnore]
	protected Match m_match;
	[XmlIgnore]
	protected bool m_isContainer;
	[XmlIgnore]
	private ArrayList m_propsList = new ArrayList();

	// Default constructor required for serialization
	public Widget()
	{
	}

	public Widget(GladeInterface _root, Match _match)
	{
		//Console.WriteLine("A Widget has been created");

		m_root = _root;
		m_match = _match;

		m_rectangle = new Rectangle(Convert.ToInt32(_match.Groups["left"].ToString()),
			Convert.ToInt32(_match.Groups["top"].ToString()),
			Convert.ToInt32(_match.Groups["width"].ToString()),
			Convert.ToInt32(_match.Groups["height"].ToString()));

		m_newLeft = Convert.ToInt32(AdjustSize(_match.Groups["left"], m_root.m_hScale));
		m_newTop = Convert.ToInt32(AdjustSize(_match.Groups["top"], m_root.m_vScale));

		m_name = m_match.Groups["name"].ToString();
	}

	abstract public void AddProperties();

	public virtual void AddChildren()
	{
		//Console.WriteLine("Widget::AddChildren has been called");
	}

	public virtual void AddToParent()
	{
		Console.WriteLine("Widget::AddToParent has been called");

		while (!ContainedWithin((Container)m_root.m_ancestors.Peek()))
		{
			m_root.m_ancestors.Pop();
		}

		m_parent = (Container)m_root.m_ancestors.Peek();

		Child c = new Child();

		c.AddWidget(this);

		Packing p = new Packing();

		int left = m_newLeft - m_parent.m_newLeft;
		p.AddProperty("x", left.ToString());
		int top = m_newTop - m_parent.m_newTop + m_parent.m_yOffset;
		p.AddProperty("y", top.ToString());
		Console.WriteLine("{0} m_newLeft={1}, m_parent.m_newLeft={2}", m_name, m_newLeft, m_parent.m_newLeft);
		Console.WriteLine("{0} m_newTop={1}, m_parent.m_newTop={2}, m_parent.m_yOffset={3}", m_name, m_newTop, m_parent.m_newTop, m_parent.m_yOffset);
		Console.WriteLine("{0} x={1}, y={2}", m_name, left, top);

		// convert packing's properties ArrayList to an array
		p.PropertiesToArray();

		c.AddPacking(p);

		m_parent.AddChild(c);
	}

	public virtual void AddToParent(GtkNotebook _parent)
	{
		//Console.WriteLine("Widget::AddToParent has been called");
	}

	private bool ContainedWithin(Container container)
	{
		if (m_rectangle.IntersectsWith(container.m_rectangle))
		{
			Console.WriteLine("<<< {0} IS within {1} >>>", m_name, container.m_name);
			return true;
		}
		else
		{
			/*Console.WriteLine("<<< {0} IS NOT within {1} >>>", m_name, container.m_name);
			Console.WriteLine("This: {0}", m_rectangle.ToString());
			Console.WriteLine("Cont: {0}", container.m_rectangle.ToString());*/
			return false;
		}
	}

	protected void AddProperty(string _name, string _value, bool _translatable)
	{
		Property p = new Property();
		p._name = _name;
		p._translatable = _translatable ? new string[] {"yes"} : null;
		p._value = _value;
		m_propsList.Add(p);
	}

	protected string AdjustSize(object size, double scale)
	{
		string strSize = size.ToString();
		double doubleSize = Convert.ToDouble(strSize);
		doubleSize *= scale;
		int intSize = (int)Math.Round(doubleSize);
		return Convert.ToString(intSize);
	}

	protected void PropertiesToArray()
	{
		m_properties = (Property[])m_propsList.ToArray(typeof(Property));
	}
}

public class WidgetFactory
{
	private GladeInterface m_root;

	public WidgetFactory(GladeInterface _root)
	{
		m_root = _root;
	}

	public Widget CreateWidget(Match _match)
	{
		Widget w;
		string widgetType = _match.Groups["type"].ToString();

		if (widgetType == "GROUPBOX")
		{
			w = new GtkFrame(m_root, _match);
		}
		else if (widgetType == "LTEXT")
		{
			string labelOptions = _match.Groups["options"].ToString();
			if (labelOptions.IndexOf("SS_SUNKEN") >= 0
				&& _match.Groups["label"].ToString() == String.Empty)
			{
				w = new GtkHSeparator(m_root, _match);
			}
			else
			{
				w = new GtkLabel(m_root, _match);
			}
		}
		else if (widgetType == "CTEXT")
		{
			w = new GtkLabel(m_root, _match);
		}
		else if (widgetType == "RTEXT")
		{
			w = new GtkLabel(m_root, _match);
		}
		else if (widgetType == "PUSHBUTTON")
		{
			if (_match.Groups["label"].ToString() == String.Empty)
			{
				string options = _match.Groups["options"].ToString();
				if (options.IndexOf("BS_BITMAP") >= 0)
				{
					w = new GtkButtonWithGtkImage(m_root, _match);
				}
				else
				{
					w = new GtkColorButton(m_root, _match);
				}
			}
			else
			{
				w = new GtkButton(m_root, _match);
			}
		}
		else if (widgetType == "DEFPUSHBUTTON")
		{
			w = new GtkButton(m_root, _match);
		}
		else if (widgetType == "EDITTEXT")
		{
			w = new GtkEntry(m_root, _match);
		}
		else if (widgetType == "LISTBOX")
		{
			w = new GtkTreeView(m_root, _match);
		}
		else if (widgetType == "COMBOBOX")
		{
			//w = new GtkComboBoxEntry(m_root, _match);
			w = new GtkComboBox(m_root, _match);
		}
		else if (widgetType == "CHECKBOX")
		{
			w = new GtkCheckButton(m_root, _match);
		}
		else if (widgetType == "ICON")
		{
			w = new GtkImage(m_root, _match);
		}
		else if (widgetType == "Button")
		{
			string buttonOptions = _match.Groups["options"].ToString();
			if (buttonOptions.IndexOf("BS_AUTOCHECKBOX") >= 0)
			{
				w = new GtkCheckButton(m_root, _match);
			}
			else if (buttonOptions.IndexOf("BS_3STATE") >= 0)
			{
				w = new GtkCheckButton(m_root, _match);
			}
			else if (buttonOptions.IndexOf("BS_AUTO3STATE") >= 0)
			{
				w = new GtkCheckButton(m_root, _match);
			}
			else if (buttonOptions.IndexOf("BS_AUTORADIOBUTTON") >= 0)
			{
				w = new GtkRadioButton(m_root, _match);
			}
			else
			{
				// what to do if type is unknown?
				w = new Unknown(m_root, _match);
			}
		}
		else if (widgetType == "Static")
		{
			string staticOptions = _match.Groups["options"].ToString();
			if (staticOptions.IndexOf("SS_BITMAP") >= 0)
			{
				w = new GtkImage(m_root, _match);
			}
			else if (staticOptions.IndexOf("SS_ICON") >= 0)
			{
				w = new GtkImage(m_root, _match);
			}
			else
			{
				// what to do if type is unknown?
				w = new GtkLabel(m_root, _match);
			}
		}
		else if (widgetType == "msctls_progress32")
		{
			w = new GtkProgressBar(m_root, _match);
		}
		else if (widgetType == "msctls_updown32")
		{
			w = new GtkSpinButton(m_root, _match);
		}
		else if (widgetType == "SysListView32")
		{
			w = new GtkTreeView(m_root, _match);
		}
		else if (widgetType == "SysMonthCal32")
		{
			w = new GtkCalendar(m_root, _match);
		}
		else if (widgetType == "SysTabControl32")
		{
			w = new GtkNotebook(m_root, _match);
		}
		else if (widgetType.StartsWith("DIALOG"))
		{
			w = new GtkDialog(m_root, _match);
		}
		else
		{
			// what to do if type is unknown?
			w = new Unknown(m_root, _match);
		}
		return w;
	}
}

[Serializable]
abstract public class Container : Widget
{
	[XmlIgnore]
	private ArrayList m_childList = new ArrayList();

	// Default constructor required for serialization
	public Container()
	{
	}

	public Container(GladeInterface _root, Match _match) : base(_root, _match)
	{
		//Console.WriteLine("A Container has been created");

		m_isContainer = true;
	}

	public void AddChild(Child c)
	{
		m_childList.Add(c);
	}

	public override void AddToParent()
	{
		//Console.WriteLine("Container::AddToParent has been called");

		base.AddToParent();
		m_root.m_containers.Add(this);
		m_root.m_ancestors.Push(this);
	}

	protected void AddFixed()
	{
		//Console.WriteLine("Container::AddFixed has been called");

		Container w = new GtkFixed(m_root, m_match);

		//w.m_name = "fixed1";
		w.m_name = "fixed" + String.Format("{0:d2}", GtkDialog.sfx);

		w.AddProperties();

		w.InternalAddToParent();
	}

	public void ChildrenToArray()
	{
		m_children = (Child[])m_childList.ToArray(typeof(Child));
	}

	public virtual void InternalAddToParent()
	{
		//Console.WriteLine("Container::InternalAddToParent has been called");

		m_parent = (Container)m_root.m_ancestors.Peek();

		m_yOffset = m_parent.m_yOffset;

		Child c = new Child();

		c.AddWidget(this);

		m_parent.AddChild(c);

		m_root.m_containers.Add(this);
		m_root.m_ancestors.Push(this);
	}
}

[Serializable]
abstract public class NonContainer : Widget
{
	// Default constructor required for serialization
	public NonContainer()
	{
	}

	public NonContainer(GladeInterface _root, Match _match) : base(_root, _match)
	{
		//Console.WriteLine("A NonContainer has been created");
	}
}

[Serializable]
public class GtkFrame : Container
{
	// Default constructor required for serialization
	public GtkFrame()
	{
	}

	public GtkFrame(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkFrame has been created");

		m_class = "GtkFrame";
		m_yOffset = -16;
		GtkDialog.sfx += 1;
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkFrame::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		AddProperty("label_xalign", "0", false);
		AddProperty("label_yalign", "0.5", false);
		AddProperty("shadow_type", "GTK_SHADOW_ETCHED_IN", false);

		PropertiesToArray();
	}

	public override void AddChildren()
	{
		//Console.WriteLine("GtkFrame::AddChildren has been called");

		AddLabel();

		AddAlignment();
	}

	private void AddAlignment()
	{
		//Console.WriteLine("GtkFrame::AddAlignment has been called");

		Container w = new GtkAlignment(m_root, m_match);

		//w.m_name = "alignment1";
		w.m_name = "alignment" + String.Format("{0:d2}", GtkDialog.sfx);

		w.AddProperties();

		w.InternalAddToParent();

		w.AddChildren();
	}

	private void AddLabel()
	{
		//Console.WriteLine("GtkFrame::AddLabel has been called");

		Widget w = new GtkLabelForGtkFrame(m_root, m_match);

		//w.m_name = "label1";
		w.m_name = "label" + String.Format("{0:d2}", GtkDialog.sfx);

		w.AddProperties();

		w.AddToParent();
	}
}

[Serializable]
public class GtkLabel : NonContainer
{
	// Default constructor required for serialization
	public GtkLabel()
	{
	}

	public GtkLabel(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkLabel has been created");

		m_class = "GtkLabel";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkLabel::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);
		AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);
		//AddProperty("xalign", "0.0", false);
		switch (m_match.Groups["type"].ToString())
		{
			case "CTEXT":
				AddProperty("xalign", "0.5", false);
				break;
			case "RTEXT":
				AddProperty("xalign", "1.0", false);
				break;
			default:
				AddProperty("xalign", "0.0", false);
				break;
		}
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkButton : NonContainer
{
	// Default constructor required for serialization
	public GtkButton()
	{
	}

	public GtkButton(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkButton has been created");

		m_class = "GtkButton";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkButton::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);

		string options = m_match.Groups["options"].ToString();

		//AddProperty("visible", "True", false);
		if (options.IndexOf("NOT WS_VISIBLE") >= 0)
		{
			AddProperty("visible", "False", false);
		}
		else
		{
			AddProperty("visible", "True", false);
		}

		if (m_match.Groups["type"].ToString() == "DEFPUSHBUTTON")
		{
			AddProperty("can_default", "True", false);
			AddProperty("has_default", "True", false);
		}
		else
		{
			AddProperty("can_default", "False", false);
			AddProperty("has_default", "False", false);
		}

		StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);
		/*AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);*/
		AddProperty("xalign", "0.5", false);
		AddProperty("yalign", "0.5", false);
		/*AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);*/

		PropertiesToArray();
	}
}

[Serializable]
public class GtkColorButton : NonContainer
{
	// Default constructor required for serialization
	public GtkColorButton()
	{
	}

	public GtkColorButton(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkColorButton has been created");

		m_class = "GtkColorButton";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkColorButton::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		/*if (m_match.Groups["type"].ToString() == "DEFPUSHBUTTON")
		{
			AddProperty("can_default", "True", false);
		}
		StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);
		AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);
		AddProperty("xalign", "0.5", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);*/

		PropertiesToArray();
	}
}

[Serializable]
public class GtkButtonWithGtkImage : Container
{
	// Default constructor required for serialization
	public GtkButtonWithGtkImage()
	{
	}

	public GtkButtonWithGtkImage(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkButtonWithGtkImage has been created");

		m_class = "GtkButton";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkButtonWithGtkImage::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		/*if (m_match.Groups["type"].ToString() == "DEFPUSHBUTTON")
		{
			AddProperty("can_default", "True", false);
		}
		StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);
		AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);
		AddProperty("xalign", "0.5", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);*/

		PropertiesToArray();
	}

	public override void AddChildren()
	{
		//Console.WriteLine("GtkButtonWithGtkImage::AddChildren has been called");

		AddImage();
	}

	private void AddImage()
	{
		//Console.WriteLine("GtkButtonWithGtkImage::AddImage has been called");

		Widget w = new GtkImageForGtkButton(m_root, m_match);

		w.m_name = "image1";

		w.AddProperties();

		w.AddToParent();
	}
}

[Serializable]
public class GtkImageForGtkButton : NonContainer
{
	// Default constructor required for serialization
	public GtkImageForGtkButton()
	{
	}

	public GtkImageForGtkButton(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkImageForGtkButton has been created");

		m_class = "GtkImage";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkImageForGtkButton::AddProperties has been called");

		AddProperty("visible", "True", false);
		/*AddProperty("selectable", "False", false);
		AddProperty("pixbuf", "FilPgSetDlgPgNum.bmp", false);*/
		AddProperty("xalign", "0.5", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);

		PropertiesToArray();
	}

	public override void AddToParent()
	{
		//Console.WriteLine("GtkImageForGtkButton::AddToParent has been called");

		m_parent = (Container)m_root.m_ancestors.Peek();

		Child c = new Child();

		c.AddWidget(this);

		/*Packing p = new Packing();

		p.AddProperty("type", "label_item");

		// convert packing's properties ArrayList to an array
		p.PropertiesToArray();

		c.AddPacking(p);*/

		m_parent.AddChild(c);
	}
}

[Serializable]
public class GtkEntry : NonContainer
{
	// Default constructor required for serialization
	public GtkEntry()
	{
	}

	public GtkEntry(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkEntry has been created");

		m_class = "GtkEntry";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkEntry::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);

		string options = m_match.Groups["options"].ToString();

		if (options.IndexOf("WS_DISABLED") >= 0)
		{
			AddProperty("sensitive", "False", false);
		}
		else
		{
			AddProperty("sensitive", "True", false);
		}

		AddProperty("can_focus", "True", false);

		//AddProperty("editable", "True", false);
		if (options.IndexOf("ES_READONLY") >= 0)
		{
			AddProperty("editable", "False", false);
		}
		else
		{
			AddProperty("editable", "True", false);
		}

		AddProperty("visibility", "True", false);
		AddProperty("max_length", "0", false);
		AddProperty("text", "", true);
		AddProperty("has_frame", "True", false);
		AddProperty("invisible_char", "●", false);
		AddProperty("activates_default", "False", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkTreeView : NonContainer
{
	// Default constructor required for serialization
	public GtkTreeView()
	{
	}

	public GtkTreeView(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkTreeView has been created");

		m_class = "GtkTreeView";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkTreeView::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		AddProperty("can_focus", "True", false);
		AddProperty("headers_visible", "False", false);
		AddProperty("rules_hint", "False", false);
		AddProperty("reorderable", "False", false);
		AddProperty("enable_search", "True", false);
		AddProperty("fixed_height_mode", "False", false);
		AddProperty("hover_selection", "False", false);
		AddProperty("hover_expand", "False", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkComboBox : NonContainer
{
	// Default constructor required for serialization
	public GtkComboBox()
	{
	}

	public GtkComboBox(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkComboBox has been created");

		m_class = "GtkComboBox";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkComboBox::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize("13", m_root.m_vScale), false);
		AddProperty("visible", "True", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkComboBoxEntry : NonContainer
{
	// Default constructor required for serialization
	public GtkComboBoxEntry()
	{
	}

	public GtkComboBoxEntry(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkComboBoxEntry has been created");

		m_class = "GtkComboBoxEntry";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkComboBoxEntry::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize("13", m_root.m_vScale), false);
		AddProperty("visible", "True", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkCheckButton : NonContainer
{
	// Default constructor required for serialization
	public GtkCheckButton()
	{
	}

	public GtkCheckButton(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkCheckButton has been created");

		m_class = "GtkCheckButton";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkCheckButton::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkDialog : Container
{
	public static int sfx;

	// Default constructor required for serialization
	public GtkDialog()
	{
	}

	public GtkDialog(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkDialog has been created");

		m_class = "GtkDialog";
		GtkDialog.sfx = 0;
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkDialog::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "False", false);
		//AddProperty("title", "", true);
		AddProperty("type", "GTK_WINDOW_TOPLEVEL", false);
		AddProperty("window_position", "GTK_WIN_POS_CENTER_ON_PARENT", false);
		AddProperty("modal", "False", false);
		AddProperty("resizable", "False", false);
		AddProperty("destroy_with_parent", "False", false);
		AddProperty("icon", "WorldPad.ico", false);
		AddProperty("decorated", "True", false);
		AddProperty("skip_taskbar_hint", "False", false);
		AddProperty("skip_pager_hint", "False", false);
		AddProperty("type_hint", "GDK_WINDOW_TYPE_HINT_DIALOG", false);
		AddProperty("gravity", "GDK_GRAVITY_NORTH_WEST", false);
		AddProperty("focus_on_map", "True", false);
		AddProperty("urgency_hint", "False", false);
		AddProperty("has_separator", "False", false);

		PropertiesToArray();
	}

	public override void AddChildren()
	{
		//Console.WriteLine("GtkDialog::AddChildren has been called");

		AddFixed();
	}

	public override void AddToParent()
	{
		//Console.WriteLine("GtkDialog::AddToParent has been called");

		//m_root.m_widget = this;
		m_root.m_dialogsList.Add(this);
		m_root.m_containers.Add(this);
		m_root.m_ancestors.Push(this);
	}

	public void AddTitleProperty(string _title)
	{
		//Console.WriteLine("GtkDialog::AddTitleProperty has been called");

		AddProperty("title", _title, true);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkAlignment : Container
{
	// Default constructor required for serialization
	public GtkAlignment()
	{
	}

	public GtkAlignment(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkAlignment has been created");

		m_class = "GtkAlignment";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkAlignment::AddProperties has been called");

		AddProperty("visible", "True", false);
		AddProperty("xalign", "0.5", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xscale", "1", false);
		AddProperty("yscale", "1", false);
		AddProperty("top_padding", "0", false);
		AddProperty("bottom_padding", "0", false);
		AddProperty("left_padding", "0", false);
		AddProperty("right_padding", "0", false);

		PropertiesToArray();
	}

	public override void AddChildren()
	{
		//Console.WriteLine("GtkAlignment::AddChildren has been called");

		AddFixed();
	}
}

[Serializable]
public class GtkLabelForGtkFrame : NonContainer
{
	// Default constructor required for serialization
	public GtkLabelForGtkFrame()
	{
	}

	public GtkLabelForGtkFrame(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkLabelForGtkFrame has been created");

		m_class = "GtkLabel";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkLabelForGtkFrame::AddProperties has been called");

		AddProperty("visible", "True", false);
		StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);
		AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);
		AddProperty("xalign", "0.0", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);

		PropertiesToArray();
	}

	public override void AddToParent()
	{
		//Console.WriteLine("GtkLabelForGtkFrame::AddToParent has been called");

		m_parent = (Container)m_root.m_ancestors.Peek();

		Child c = new Child();

		c.AddWidget(this);

		Packing p = new Packing();

		p.AddProperty("type", "label_item");

		// convert packing's properties ArrayList to an array
		p.PropertiesToArray();

		c.AddPacking(p);

		m_parent.AddChild(c);
	}
}

[Serializable]
public class GtkFixed : Container
{
	// Default constructor required for serialization
	public GtkFixed()
	{
	}

	public GtkFixed(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkFixed has been created");

		m_class = "GtkFixed";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkFixed::AddProperties has been called");

		AddProperty("visible", "True", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkImage : NonContainer
{
	// Default constructor required for serialization
	public GtkImage()
	{
	}

	public GtkImage(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkImage has been created");

		m_class = "GtkImage";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkImage::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		/*StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);
		AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);
		AddProperty("xalign", "0.5", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);*/

		PropertiesToArray();
	}
}

[Serializable]
public class GtkRadioButton : NonContainer
{
	// Default constructor required for serialization
	public GtkRadioButton()
	{
	}

	public GtkRadioButton(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkRadioButton has been created");

		m_class = "GtkRadioButton";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkRadioButton::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkProgressBar : NonContainer
{
	// Default constructor required for serialization
	public GtkProgressBar()
	{
	}

	public GtkProgressBar(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkProgressBar has been created");

		m_class = "GtkProgressBar";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkProgressBar::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		/*StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);*/
		AddProperty("orientation", "GTK_PROGRESS_LEFT_TO_RIGHT", false);
		AddProperty("fraction", "0", false);
		AddProperty("pulse_step", "0.10000000149", false);
		AddProperty("text", "", true);
		AddProperty("ellipsize", "PANGO_ELLIPSIZE_NONE", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkSpinButton : NonContainer
{
	// Default constructor required for serialization
	public GtkSpinButton()
	{
	}

	public GtkSpinButton(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkSpinButton has been created");

		m_class = "GtkSpinButton";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkSpinButton::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		/*StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');
		AddProperty("label", sb.ToString(), true);
		AddProperty("use_underline", "True", false);*/

		PropertiesToArray();
	}
}

[Serializable]
public class GtkCalendar : NonContainer
{
	// Default constructor required for serialization
	public GtkCalendar()
	{
	}

	public GtkCalendar(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkCalendar has been created");

		m_class = "GtkCalendar";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkCalendar::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		AddProperty("can_focus", "True", false);
		AddProperty("display_options", "GTK_CALENDAR_SHOW_HEADING|GTK_CALENDAR_SHOW_DAY_NAMES", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkHSeparator : NonContainer
{
	// Default constructor required for serialization
	public GtkHSeparator()
	{
	}

	public GtkHSeparator(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkHSeparator has been created");

		m_class = "GtkHSeparator";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkHSeparator::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);

		PropertiesToArray();
	}
}

[Serializable]
public class GtkNotebook : Container
{
	// Default constructor required for serialization
	public GtkNotebook()
	{
	}

	public GtkNotebook(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkNotebook has been created");

		m_class = "GtkNotebook";
		GtkDialog.sfx += 1;
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkNotebook::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);

		PropertiesToArray();
	}

	public override void AddChildren()
	{
		//Console.WriteLine("GtkNotebook::AddChildren has been called");

		AddPage();
	}

	private void AddPage()
	{
		//Console.WriteLine("GtkNotebook::AddPage has been called");

		AddFixed();

		AddLabel();
	}

	private new void AddFixed()
	{
		//Console.WriteLine("GtkNotebook::AddFixed has been called");

		Widget w = new GtkFixed(m_root, m_match);

		//int sfx = 0;
		//w.m_name = "fixed" + String.Format("{0:d2}_{1:d2}", GtkDialog.sfx, sfx);
		w.m_name = "fixed" + String.Format("{0:d2}", GtkDialog.sfx);
		w.m_yOffset = -32;

		w.AddProperties();

		w.AddToParent();
	}

	private void AddLabel()
	{
		//Console.WriteLine("GtkNotebook::AddLabel has been called");

		Widget w = new GtkLabelForGtkNotebook(m_root, m_match);

		//int sfx = 0;
		//w.m_name = "label" + String.Format("{0:d2}_{1:d2}", GtkDialog.sfx, sfx);
		//w.m_name = "label" + String.Format("{0:d2}", GtkDialog.sfx);
		w.m_name = m_name + "label";

		w.AddProperties();

		w.AddToParent(this);
	}
}

/*[Serializable]
public class GtkFixedForGtkNotebook : NonContainer
{
	// Default constructor required for serialization
	public GtkFixedForGtkNotebook()
	{
	}

	public GtkFixedForGtkNotebook(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkFixedForGtkNotebook has been created");

		m_class = "GtkFixed";
		m_yOffset = -16;
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkFixedForGtkNotebook::AddProperties has been called");

		AddProperty("visible", "True", false);

		PropertiesToArray();
	}

	public override void AddToParent()
	{
		//Console.WriteLine("GtkFixedForGtkNotebook::AddToParent has been called");

		m_parent = (Container)m_root.m_ancestors.Peek();

		Child c = new Child();

		c.AddWidget(this);

		Packing p = new Packing();

		p.AddProperty("tab_expand", "False");
		p.AddProperty("tab_fill", "True");

		// convert packing's properties ArrayList to an array
		p.PropertiesToArray();

		c.AddPacking(p);

		m_parent.AddChild(c);
	}
}*/

[Serializable]
public class GtkLabelForGtkNotebook : NonContainer
{
	// Default constructor required for serialization
	public GtkLabelForGtkNotebook()
	{
	}

	public GtkLabelForGtkNotebook(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("A GtkLabelForGtkNotebook has been created");

		m_class = "GtkLabel";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("GtkLabelForGtkNotebook::AddProperties has been called");

		AddProperty("visible", "True", false);
		AddProperty("label", m_name, true);
		AddProperty("use_underline", "True", false);
		AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);
		AddProperty("xalign", "0.0", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);

		PropertiesToArray();
	}

	//public override void AddToParent()
	public override void AddToParent(GtkNotebook _parent)
	{
		//Console.WriteLine("GtkLabelForGtkNotebook::AddToParent has been called");

		//Container parent = (Container)m_root.m_ancestors.Peek();
		m_parent = _parent;

		Child c = new Child();

		c.AddWidget(this);

		Packing p = new Packing();

		p.AddProperty("type", "tab");

		// convert packing's properties ArrayList to an array
		p.PropertiesToArray();

		c.AddPacking(p);

		m_parent.AddChild(c);
	}
}

[Serializable]
public class Unknown : NonContainer
{
	// Default constructor required for serialization
	public Unknown()
	{
	}

	public Unknown(GladeInterface _root, Match _match) : base(_root, _match)
	{
		Console.WriteLine("An Unknown has been created");

		m_class = "GtkLabel";
	}

	public override void AddProperties()
	{
		//Console.WriteLine("Unknown::AddProperties has been called");

		AddProperty("width_request", AdjustSize(m_match.Groups["width"], m_root.m_hScale), false);
		AddProperty("height_request", AdjustSize(m_match.Groups["height"], m_root.m_vScale), false);
		AddProperty("visible", "True", false);
		/*StringBuilder sb = new StringBuilder(m_match.Groups["label"].ToString());
		sb.Replace('&', '_');*/
		AddProperty("label", "UNKNOWN", true);
		AddProperty("use_underline", "False", false);
		AddProperty("use_markup", "False", false);
		AddProperty("justify", "GTK_JUSTIFY_LEFT", false);
		AddProperty("wrap", "False", false);
		AddProperty("selectable", "False", false);
		AddProperty("xalign", "0.5", false);
		AddProperty("yalign", "0.5", false);
		AddProperty("xpad", "0", false);
		AddProperty("ypad", "0", false);

		PropertiesToArray();
	}
}


[Serializable]
public class Property
{
	[XmlAttribute(AttributeName="name")]
	public string _name;
	[XmlAttribute(AttributeName="translatable")]
	public string[] _translatable;

	[XmlText]
	public string _value;
}


[Serializable]
public class Child
{
	[XmlElement(ElementName="widget")]
	public Widget _widget;
	[XmlElement(ElementName="packing")]
	public Packing[] _packing;

	public void AddWidget(Widget w)
	{
		_widget = w;
	}

	public void AddPacking(Packing p)
	{
		_packing = new Packing[] {p};
	}
}


[Serializable]
public class Packing
{
	[XmlElement(ElementName="property")]
	public Property[] _properties;

	[XmlIgnore]
	private ArrayList _propsList = new ArrayList();

	public void AddProperty(string _name, string _value)
	{
		Property p = new Property();
		p._name = _name;
		p._value = _value;
		_propsList.Add(p);
	}

	public void PropertiesToArray()
	{
		_properties = (Property[])_propsList.ToArray(typeof(Property));
	}
}
