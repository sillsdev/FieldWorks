using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.PropertyGrid;

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class LcmObjectView : ICustomTypeDescriptor, INotifyPropertyChanged
{
	private readonly LcmCache _cache;
	private readonly ICmObject _obj;
	private readonly IReadOnlyList<PresentationNode> _schema;
	private PropertyDescriptorCollection? _cached;

	public LcmObjectView(string displayName, LcmCache cache, ICmObject obj, IReadOnlyList<PresentationNode> schema)
	{
		DisplayName = displayName;
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_obj = obj ?? throw new ArgumentNullException(nameof(obj));
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
	}

	public string DisplayName { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	internal void NotifyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public override string ToString() => DisplayName;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => AttributeCollection.Empty;

	string? ICustomTypeDescriptor.GetClassName() => GetType().FullName;

	string? ICustomTypeDescriptor.GetComponentName() => DisplayName;

	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);

	EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;

	PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;

	object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) => EventDescriptorCollection.Empty;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) => BuildProperties();

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => BuildProperties();

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;

	private PropertyDescriptorCollection BuildProperties()
	{
		if (_cached is not null)
			return _cached;

		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		var props = new List<PropertyDescriptor>();

		BuildPropertiesRecursive(_schema, category: null);
		_cached = new PropertyDescriptorCollection(props.ToArray(), true);
		return _cached;

		void BuildPropertiesRecursive(IReadOnlyList<PresentationNode> nodes, string? category)
		{
			foreach (var node in nodes)
			{
				switch (node)
				{
					case PresentationSection section:
						BuildPropertiesRecursive(section.Children, section.Label ?? category);
						break;
					case PresentationField field:
						props.Add(
							new LcmFieldPropertyDescriptor(
								name: PropertyName.From(field.Id.Value, usedNames),
								displayName: field.Label ?? field.Field,
								category: category,
								cache: _cache,
								owner: this,
								obj: _obj,
								fieldName: field.Field,
								isRequired: field.IsRequired
							)
						);
						break;
					case PresentationObject objNode:
						props.Add(
							new LcmObjectPropertyDescriptor(
								name: PropertyName.From(objNode.Id.Value, usedNames),
								displayName: objNode.Label ?? objNode.Field,
								category: category,
								cache: _cache,
								obj: _obj,
								fieldName: objNode.Field,
								ghostClass: objNode.Ghost?.GhostClass,
								schema: objNode.Children
							)
						);
						break;
					case PresentationSequence seq:
						props.Add(
							new LcmSequencePropertyDescriptor(
								name: PropertyName.From(seq.Id.Value, usedNames),
								displayName: seq.Label ?? seq.Field,
								category: category,
								cache: _cache,
								obj: _obj,
								fieldName: seq.Field,
								ghost: seq.Ghost,
								itemSchema: seq.ItemTemplate
							)
						);
						break;
				}
			}
		}
	}
}

internal sealed class LcmFieldPropertyDescriptor : PropertyDescriptor
{
	private readonly LcmCache _cache;
	private readonly LcmObjectView _owner;
	private readonly ICmObject _obj;
	private readonly string _fieldName;
	private readonly PropertyInfo? _accessorProperty;
	private readonly int _flid;
	private readonly CellarPropertyType _type;
	private readonly int _wsSelector;
	private readonly bool _isRequired;

	public LcmFieldPropertyDescriptor(
		string name,
		string displayName,
		string? category,
		LcmCache cache,
		LcmObjectView owner,
		ICmObject obj,
		string fieldName,
		bool isRequired
	)
		: base(name, BuildAttributes(displayName, category, isRequired))
	{
		_cache = cache;
		_owner = owner;
		_obj = obj;
		_fieldName = fieldName;
		_isRequired = isRequired;

		_flid = _cache.DomainDataByFlid.MetaDataCache.GetFieldId(_obj.ClassName, fieldName, true);
		_type = (CellarPropertyType)_cache.DomainDataByFlid.MetaDataCache.GetFieldType(_flid);
		_wsSelector = _cache.DomainDataByFlid.MetaDataCache.GetFieldWs(_flid);
		_accessorProperty = _obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
	}

	public override Type ComponentType => typeof(LcmObjectView);
	public override bool IsReadOnly => false;
	public override Type PropertyType => typeof(string);

	public override object? GetValue(object? component)
	{
		var sda = _cache.DomainDataByFlid;
		var hvo = _obj.Hvo;

		return _type switch
		{
			CellarPropertyType.Unicode => sda.get_UnicodeProp(hvo, _flid) ?? string.Empty,
			CellarPropertyType.String => sda.get_StringProp(hvo, _flid)?.Text ?? string.Empty,
			CellarPropertyType.MultiString => GetMultiAltText(hvo) ?? string.Empty,
			CellarPropertyType.MultiUnicode => GetMultiAltText(hvo) ?? string.Empty,
			_ => string.Empty,
		};
	}

	public override void SetValue(object? component, object? value)
	{
		var s = value?.ToString() ?? string.Empty;
		var sda = _cache.DomainDataByFlid;
		var hvo = _obj.Hvo;

		switch (_type)
		{
			case CellarPropertyType.Unicode:
				sda.SetUnicode(hvo, _flid, s, s.Length);
				break;
			case CellarPropertyType.String:
				sda.SetString(hvo, _flid, TsStringUtils.MakeString(s, GetDefaultWs()));
				break;
			case CellarPropertyType.MultiString:
					SetMultiAltText(hvo, s);
					break;
				case CellarPropertyType.MultiUnicode:
					SetMultiAltText(hvo, s);
				break;
		}

		_owner.NotifyChanged(Name);
	}

	public override void ResetValue(object? component) => SetValue(component, string.Empty);
	public override bool CanResetValue(object? component) => true;
	public override bool ShouldSerializeValue(object? component) => false;

	private int GetDefaultWs() => _cache.DefaultAnalWs > 0 ? _cache.DefaultAnalWs : _cache.DefaultVernWs;

	private object? GetFieldAccessor()
	{
		if (_accessorProperty is null)
			return null;

		try
		{
			return _accessorProperty.GetValue(_obj);
		}
		catch
		{
			return null;
		}
	}

	private string? GetMultiAltText(int hvo)
	{
		var ws = GetPreferredMultiStringWs(hvo);

		var accessor = GetFieldAccessor();
		if (accessor is not null)
		{
			var getString = accessor.GetType().GetMethod("get_String", new[] { typeof(int) });
			if (getString is not null)
			{
				var result = getString.Invoke(accessor, new object[] { ws });
				switch (result)
				{
					case ITsString tss:
						return tss.Text;
					case string s:
						return s;
				}
			}
		}

		// Fallback for cases where the generated accessor isn't available.
		return _cache.DomainDataByFlid.get_MultiStringAlt(hvo, _flid, ws)?.Text;
	}

	private void SetMultiAltText(int hvo, string value)
	{
		var ws = GetPreferredMultiStringWs(hvo);

		var accessor = GetFieldAccessor();
		if (accessor is not null)
		{
			var setStringTss = accessor.GetType().GetMethod("set_String", new[] { typeof(int), typeof(ITsString) });
			if (setStringTss is not null)
			{
				setStringTss.Invoke(accessor, new object[] { ws, TsStringUtils.MakeString(value, ws) });
				return;
			}

			var setStringPlain = accessor.GetType().GetMethod("set_String", new[] { typeof(int), typeof(string) });
			if (setStringPlain is not null)
			{
				setStringPlain.Invoke(accessor, new object[] { ws, value });
				return;
			}
		}

		// Fallback for cases where the generated accessor isn't available.
		_cache.DomainDataByFlid.SetMultiStringAlt(hvo, _flid, ws, TsStringUtils.MakeString(value, ws));
	}

	private int GetPreferredMultiStringWs(int hvo)
	{
		var sda = _cache.DomainDataByFlid;

		// If metadata provides a concrete WS id, use it.
		if (_wsSelector > 0)
			return _wsSelector;

		// Otherwise, choose based on existing content: prefer the default WS that already has text.
		var vernWs = _cache.DefaultVernWs;
		if (vernWs > 0)
		{
			var existing = GetMultiAltTextAtWs(sda, hvo, vernWs);
			if (!string.IsNullOrEmpty(existing))
				return vernWs;
		}

		var analWs = _cache.DefaultAnalWs;
		if (analWs > 0)
		{
			var existing = GetMultiAltTextAtWs(sda, hvo, analWs);
			if (!string.IsNullOrEmpty(existing))
				return analWs;
		}

		// Fall back to a stable default.
		return vernWs > 0 ? vernWs : analWs;
	}

	private string? GetMultiAltTextAtWs(object sda, int hvo, int ws)
	{
		var accessor = GetFieldAccessor();
		if (accessor is not null)
		{
			var getString = accessor.GetType().GetMethod("get_String", new[] { typeof(int) });
			if (getString is not null)
			{
				var result = getString.Invoke(accessor, new object[] { ws });
				return result switch
				{
					ITsString tss => tss.Text,
					string s => s,
					_ => null,
				};
			}
		}

		// Fall back to the SDA API if available.
		var getAlt = sda.GetType().GetMethod("get_MultiStringAlt", new[] { typeof(int), typeof(int), typeof(int) });
		if (getAlt is not null)
		{
			var result = getAlt.Invoke(sda, new object[] { hvo, _flid, ws });
			return result is ITsString tss ? tss.Text : null;
		}

		return null;
	}

	private static Attribute[] BuildAttributes(string displayName, string? category, bool isRequired)
	{
		var list = new List<Attribute>
		{
			new DisplayNameAttribute(displayName),
		};

		if (!string.IsNullOrWhiteSpace(category))
			list.Add(new CategoryAttribute(category));
		if (isRequired)
			list.Add(new RequiredAttribute());

		return list.ToArray();
	}

	private sealed class RequiredAttribute : Attribute { }
}

internal sealed class LcmObjectPropertyDescriptor : PropertyDescriptor
{
	private readonly LcmCache _cache;
	private readonly ICmObject _obj;
	private readonly int _flid;
	private readonly string? _ghostClass;
	private readonly IReadOnlyList<PresentationNode> _schema;

	public LcmObjectPropertyDescriptor(
		string name,
		string displayName,
		string? category,
		LcmCache cache,
		ICmObject obj,
		string fieldName,
		string? ghostClass,
		IReadOnlyList<PresentationNode> schema
	)
		: base(name, BuildAttributes(displayName, category))
	{
		_cache = cache;
		_obj = obj;
		_schema = schema;
		_ghostClass = ghostClass;

		_flid = _cache.DomainDataByFlid.MetaDataCache.GetFieldId(_obj.ClassName, fieldName, true);
	}

	public override Type ComponentType => typeof(LcmObjectView);
	public override bool IsReadOnly => true;
	public override Type PropertyType => typeof(ICustomTypeDescriptor);

	public override object? GetValue(object? component)
	{
		var hvo = _cache.DomainDataByFlid.get_ObjectProp(_obj.Hvo, _flid);
		if (hvo == 0)
		{
			var created = TryCreateGhostObject();
			if (created is null)
				return new LcmEmptyObjectView(DisplayName);

			_cache.DomainDataByFlid.SetObjProp(_obj.Hvo, _flid, created.Hvo);
			hvo = created.Hvo;
		}

		var repo = _cache.ServiceLocator.GetInstance<ICmObjectRepository>();
		var child = repo.GetObject(hvo);
		return new LcmObjectView(DisplayName, _cache, child, _schema);
	}

	private ICmObject? TryCreateGhostObject()
	{
		if (string.IsNullOrWhiteSpace(_ghostClass))
			return null;

		return _ghostClass switch
		{
			"MoStemAllomorph" => _cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create(),
			"MoAffixAllomorph" => _cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create(),
			"LexSense" => _cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(),
			"LexExampleSentence" => _cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create(),
			_ => null,
		};
	}

	public override void SetValue(object? component, object? value) { }
	public override void ResetValue(object? component) { }
	public override bool CanResetValue(object? component) => false;
	public override bool ShouldSerializeValue(object? component) => false;

	private static Attribute[] BuildAttributes(string displayName, string? category)
	{
		var list = new List<Attribute>
		{
			new DisplayNameAttribute(displayName),
		};

		if (!string.IsNullOrWhiteSpace(category))
			list.Add(new CategoryAttribute(category));

		return list.ToArray();
	}
}

internal sealed class LcmSequencePropertyDescriptor : PropertyDescriptor
{
	private readonly LcmCache _cache;
	private readonly ICmObject _obj;
	private readonly int _flid;
	private readonly IReadOnlyList<PresentationNode> _itemSchema;
	private readonly string? _itemClass;

	public LcmSequencePropertyDescriptor(
		string name,
		string displayName,
		string? category,
		LcmCache cache,
		ICmObject obj,
		string fieldName,
		GhostSpec? ghost,
		IReadOnlyList<PresentationNode> itemSchema
	)
		: base(name, BuildAttributes(displayName, category))
	{
		_cache = cache;
		_obj = obj;
		_itemSchema = itemSchema;

		_flid = _cache.DomainDataByFlid.MetaDataCache.GetFieldId(_obj.ClassName, fieldName, true);
		_itemClass = Layout.Compilation.FieldClassMap.GetItemClass(_obj.ClassName, fieldName, ghost);
	}

	public override Type ComponentType => typeof(LcmObjectView);
	public override bool IsReadOnly => true;
	public override Type PropertyType => typeof(IList<LcmSequenceItem>);

	public override object GetValue(object? component)
		=> new LcmSequenceList(DisplayName, _cache, _obj, _flid, _itemClass, _itemSchema);

	public override void SetValue(object? component, object? value) { }
	public override void ResetValue(object? component) { }
	public override bool CanResetValue(object? component) => false;
	public override bool ShouldSerializeValue(object? component) => false;

	private static Attribute[] BuildAttributes(string displayName, string? category)
	{
		var list = new List<Attribute>
		{
			new DisplayNameAttribute(displayName),
		};

		if (!string.IsNullOrWhiteSpace(category))
			list.Add(new CategoryAttribute(category));

		return list.ToArray();
	}
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class LcmSequenceItem : ICustomTypeDescriptor
{
	private readonly LcmObjectView? _inner;
	internal int Hvo { get; }

	// Public parameterless constructor is required for Avalonia.PropertyGrid's ObjectCreator.
	public LcmSequenceItem()
	{
		_inner = null;
		Hvo = 0;
	}

	internal LcmSequenceItem(int hvo, LcmObjectView inner)
	{
		Hvo = hvo;
		_inner = inner;
	}

	public override string ToString() => _inner?.ToString() ?? string.Empty;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => AttributeCollection.Empty;
	string? ICustomTypeDescriptor.GetClassName() => GetType().FullName;
	string? ICustomTypeDescriptor.GetComponentName() => _inner?.DisplayName;
	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);
	EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;
	PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;
	object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) => EventDescriptorCollection.Empty;
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
		=> _inner is null ? PropertyDescriptorCollection.Empty : TypeDescriptor.GetProperties(_inner, true);
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
		=> _inner is null ? PropertyDescriptorCollection.Empty : TypeDescriptor.GetProperties(_inner, true);
	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;
}

public sealed class LcmSequenceList : IList<LcmSequenceItem>, System.Collections.IList
{
	private readonly LcmCache _cache;
	private readonly ICmObject _owner;
	private readonly int _flid;
	private readonly string? _itemClass;
	private readonly IReadOnlyList<PresentationNode> _itemSchema;
	private readonly string _displayName;

	public LcmSequenceList(
		string displayName,
		LcmCache cache,
		ICmObject owner,
		int flid,
		string? itemClass,
		IReadOnlyList<PresentationNode> itemSchema
	)
	{
		_displayName = displayName;
		_cache = cache;
		_owner = owner;
		_flid = flid;
		_itemClass = itemClass;
		_itemSchema = itemSchema;
	}

	public int Count => _cache.DomainDataByFlid.get_VecSize(_owner.Hvo, _flid);
	public bool IsReadOnly => false;
	public bool IsFixedSize => false;
	public object SyncRoot => this;
	public bool IsSynchronized => false;

	public LcmSequenceItem this[int index]
	{
		get => GetItem(index);
		set => SetItem(index, value);
	}

	object? System.Collections.IList.this[int index]
	{
		get => this[index];
		set
		{
			if (value is LcmSequenceItem item)
				this[index] = item;
			else
				throw new ArgumentException("Value must be a LcmSequenceItem.", nameof(value));
		}
	}

	public int Add(LcmSequenceItem item)
	{
		Insert(Count, item);
		return Count - 1;
	}

	int System.Collections.IList.Add(object? value)
	{
		if (value is not LcmSequenceItem item)
			throw new ArgumentException("Value must be a LcmSequenceItem.", nameof(value));

		return Add(item);
	}

	public void Clear() => _cache.DomainDataByFlid.Replace(_owner.Hvo, _flid, 0, Count, null, 0);

	public bool Contains(LcmSequenceItem item) => IndexOf(item) >= 0;
	bool System.Collections.IList.Contains(object? value) => value is LcmSequenceItem item && Contains(item);

	public void CopyTo(LcmSequenceItem[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);
		for (var i = 0; i < Count; i++)
			array[arrayIndex + i] = this[i];
	}

	void System.Collections.ICollection.CopyTo(Array array, int index)
	{
		ArgumentNullException.ThrowIfNull(array);
		for (var i = 0; i < Count; i++)
			array.SetValue(this[i], index + i);
	}

	public IEnumerator<LcmSequenceItem> GetEnumerator()
	{
		for (var i = 0; i < Count; i++)
			yield return this[i];
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

	public int IndexOf(LcmSequenceItem item)
	{
		if (item.Hvo == 0)
			return -1;

		for (var i = 0; i < Count; i++)
		{
			var hvo = _cache.DomainDataByFlid.get_VecItem(_owner.Hvo, _flid, i);
			if (hvo == item.Hvo)
				return i;
		}

		return -1;
	}

	int System.Collections.IList.IndexOf(object? value) => value is LcmSequenceItem item ? IndexOf(item) : -1;

	public void Insert(int index, LcmSequenceItem item)
	{
		var hvo = EnsureHvo(item);
		_cache.DomainDataByFlid.Replace(_owner.Hvo, _flid, index, index, new[] { hvo }, 1);
	}

	void System.Collections.IList.Insert(int index, object? value)
	{
		if (value is not LcmSequenceItem item)
			throw new ArgumentException("Value must be a LcmSequenceItem.", nameof(value));

		Insert(index, item);
	}

	public bool Remove(LcmSequenceItem item)
	{
		var index = IndexOf(item);
		if (index < 0)
			return false;

		RemoveAt(index);
		return true;
	}

	void System.Collections.IList.Remove(object? value)
	{
		if (value is LcmSequenceItem item)
			Remove(item);
	}

	public void RemoveAt(int index) => _cache.DomainDataByFlid.Replace(_owner.Hvo, _flid, index, index + 1, null, 0);

	private LcmSequenceItem GetItem(int index)
	{
		var hvo = _cache.DomainDataByFlid.get_VecItem(_owner.Hvo, _flid, index);
		if (hvo == 0)
			return new LcmSequenceItem();

		var repo = _cache.ServiceLocator.GetInstance<ICmObjectRepository>();
		var item = repo.GetObject(hvo);
		var view = new LcmObjectView($"{_displayName} {index + 1}", _cache, item, _itemSchema);
		return new LcmSequenceItem(hvo, view);
	}

	private void SetItem(int index, LcmSequenceItem item)
	{
		var hvo = EnsureHvo(item);
		_cache.DomainDataByFlid.Replace(_owner.Hvo, _flid, index, index + 1, new[] { hvo }, 1);
	}

	private int EnsureHvo(LcmSequenceItem item)
	{
		if (item.Hvo != 0)
			return item.Hvo;

		var created = CreateNewItem();
		return created.Hvo;
	}

	private ICmObject CreateNewItem()
	{
		if (string.IsNullOrWhiteSpace(_itemClass))
			throw new NotSupportedException($"Cannot create items for sequence '{_displayName}' because item class is unknown.");

		return _itemClass switch
		{
			"MoStemAllomorph" => _cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create(),
			"MoAffixAllomorph" => _cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create(),
			"LexSense" => _cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(),
			"LexExampleSentence" => _cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create(),
			"LexPronunciation" => _cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create(),
			"LexEtymology" => _cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create(),
			"LexEntryRef" => _cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create(),
			_ => throw new NotSupportedException($"Unsupported sequence item class '{_itemClass}'.")
		};
	}
}

internal sealed class LcmEmptyObjectView : ICustomTypeDescriptor
{
	public LcmEmptyObjectView(string displayName)
	{
		DisplayName = displayName;
	}

	public string DisplayName { get; }

	public override string ToString() => DisplayName;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => AttributeCollection.Empty;
	string? ICustomTypeDescriptor.GetClassName() => GetType().FullName;
	string? ICustomTypeDescriptor.GetComponentName() => DisplayName;
	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);
	EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;
	PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;
	object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) => EventDescriptorCollection.Empty;
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) => PropertyDescriptorCollection.Empty;
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => PropertyDescriptorCollection.Empty;
	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;
}
