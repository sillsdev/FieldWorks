using ECInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary></summary>
	public enum ConverterType
	{
		/// <summary></summary>
		ktypeCC,
		/// <summary></summary>
		ktypeIcuConvert,
		/// <summary></summary>
		ktypeIcuTransduce,
		/// <summary></summary>
		ktypeTecKitTec,
		/// <summary></summary>
		ktypeTecKitMap,
		/// <summary></summary>
		ktypeCodePage,
		/// <summary>
		/// ICU regular expression
		/// </summary>
		ktypeRegEx
	}

	/// <summary>
	/// Summary description for CnvtrTypeComboClass.
	/// </summary>
	public class CnvtrTypeComboItem
	{
		string m_itemName;
		ConverterType m_type;
		string m_implementType;

		/// <summary></summary>
		public CnvtrTypeComboItem(string name, ConverterType type, string implementType)
		{
			m_itemName = name;
			m_type = type;
			m_implementType = implementType;
		}

		/// <summary></summary>
		public string Name
		{
			get {return m_itemName;}
		}

		/// <summary></summary>
		public ConverterType Type
		{
			get {return m_type;}
		}

		/// <summary></summary>
		public string ImplementType
		{
			get {return m_implementType;}
		}

		/// <summary></summary>
		public override string ToString()
		{
			return m_itemName;
		}
	}

	/// <summary></summary>
	public class CnvtrDataComboItem
	{
		string m_itemName;
		ConvType m_type;

		/// <summary></summary>
		public CnvtrDataComboItem(string name, ConvType type)
		{
			m_itemName = name;
			m_type = type;
		}

		/// <summary></summary>
		public string Name
		{
			get {return m_itemName;}
			set {m_itemName = value;}
		}

		/// <summary></summary>
		public ConvType Type
		{
			get {return m_type;}
			set {m_type = value;}
		}

		/// <summary></summary>
		public override string ToString()
		{
			return m_itemName;
		}
	}

	/// <summary></summary>
	public class CnvtrSpecComboItem
	{
		string m_itemName;
		string m_specs; // the specs that should be used to create the converter.

		/// <summary></summary>
		public CnvtrSpecComboItem(string name, string specs)
		{
			m_itemName = name;
			m_specs = specs;
		}

		/// <summary></summary>
		public string Name
		{
			get {return m_itemName;}
			set {m_itemName = value;}
		}

		/// <summary></summary>
		public string Specs
		{
			get {return m_specs;}
			set {m_specs = value;}
		}

		/// <summary></summary>
		public override string ToString()
		{
			return m_itemName;
		}
	}
}
