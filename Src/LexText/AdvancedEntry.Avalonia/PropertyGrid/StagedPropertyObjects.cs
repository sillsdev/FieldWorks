using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Staging;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.PropertyGrid;

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class StagedObjectView : ICustomTypeDescriptor, INotifyPropertyChanged
{
	private PropertyDescriptorCollection? _cached;

	public StagedObjectView(
		string displayName,
		string className,
		StagedObjectState state,
		IReadOnlyList<PresentationNode> schema
	)
	{
		DisplayName = displayName;
		ClassName = className;
		State = state;
		Schema = schema;
	}

	public string DisplayName { get; }
	public string ClassName { get; }
	public StagedObjectState State { get; }
	public IReadOnlyList<PresentationNode> Schema { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	internal void NotifyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public override string ToString() => DisplayName;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => AttributeCollection.Empty;

	string? ICustomTypeDescriptor.GetClassName() => GetType().FullName;

	string? ICustomTypeDescriptor.GetComponentName() => DisplayName;

	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);

	EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;

	PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;

	object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) =>
		EventDescriptorCollection.Empty;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() =>
		EventDescriptorCollection.Empty;

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) =>
		BuildProperties();

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() =>
		BuildProperties();

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;

	private PropertyDescriptorCollection BuildProperties()
	{
		if (_cached is not null)
			return _cached;

		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		var props = new List<PropertyDescriptor>();

		BuildPropertiesRecursive(Schema, category: null);
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
							new StagedFieldPropertyDescriptor(
								name: PropertyName.From(field.Id.Value, usedNames),
								displayName: field.Label ?? field.Field,
								category: category,
								owner: this,
								fieldName: field.Field,
								isRequired: field.IsRequired
							)
						);
						break;
					case PresentationObject obj:
						{
							var childClass =
								FieldClassMap.GetItemClass(ClassName, obj.Field, obj.Ghost) ?? "Unknown";

							props.Add(
								new StagedObjectPropertyDescriptor(
									name: PropertyName.From(obj.Id.Value, usedNames),
									displayName: obj.Label ?? obj.Field,
									category: category,
									owner: this,
									fieldName: obj.Field,
									childClass: childClass,
									schema: obj.Children
								)
							);
							break;
						}
					case PresentationSequence seq:
						{
							var itemClass =
								FieldClassMap.GetItemClass(ClassName, seq.Field, seq.Ghost) ?? "Unknown";

							props.Add(
								new StagedSequencePropertyDescriptor(
									name: PropertyName.From(seq.Id.Value, usedNames),
									displayName: seq.Label ?? seq.Field,
									category: category,
									owner: this,
									fieldName: seq.Field,
									itemClass: itemClass,
									itemSchema: seq.ItemTemplate,
									isVirtualized: seq.IsVirtualized
								)
							);
							break;
						}
					default:
						break;
				}
			}
		}
	}
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class StagedSequenceView : ICustomTypeDescriptor
{
	public StagedSequenceView(
		string displayName,
		string itemClass,
		StagedSequenceState state,
		IReadOnlyList<PresentationNode> itemSchema,
		bool isVirtualized
	)
	{
		DisplayName = displayName;
		ItemClass = itemClass;
		State = state;
		ItemSchema = itemSchema;
		IsVirtualized = isVirtualized;
	}

	public string DisplayName { get; }

	public string ItemClass { get; }

	public StagedSequenceState State { get; }

	public IReadOnlyList<PresentationNode> ItemSchema { get; }

	public bool IsVirtualized { get; }

	public override string ToString() => DisplayName;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => AttributeCollection.Empty;

	string? ICustomTypeDescriptor.GetClassName() => GetType().FullName;

	string? ICustomTypeDescriptor.GetComponentName() => DisplayName;

	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);

	EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;

	PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;

	object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) =>
		EventDescriptorCollection.Empty;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() =>
		EventDescriptorCollection.Empty;

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) =>
		BuildProperties();

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() =>
		BuildProperties();

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;

	private PropertyDescriptorCollection BuildProperties()
	{
		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		var props = new List<PropertyDescriptor>();

		for (var i = 0; i < State.Count; i++)
		{
			props.Add(new StagedSequenceItemPropertyDescriptor(
				name: PropertyName.From($"{DisplayName}_{i}", usedNames),
				displayName: $"{DisplayName} {i + 1}",
				owner: this,
				index: i,
				itemClass: ItemClass,
				itemSchema: ItemSchema
			));
		}

		return new PropertyDescriptorCollection(props.ToArray(), true);
	}
}

internal sealed class StagedFieldPropertyDescriptor : PropertyDescriptor
{
	private readonly StagedObjectView _owner;
	private readonly string _fieldName;

	public StagedFieldPropertyDescriptor(
		string name,
		string displayName,
		string? category,
		StagedObjectView owner,
		string fieldName,
		bool isRequired
	)
		: base(name, BuildAttributes(displayName, category, isRequired))
	{
		_owner = owner;
		_fieldName = fieldName;
	}

	public override Type ComponentType => typeof(StagedObjectView);
	public override bool IsReadOnly => false;
	public override Type PropertyType => typeof(string);

	public override object? GetValue(object? component)
	{
		_owner.State.Fields.TryGetValue(_fieldName, out var value);
		return value ?? string.Empty;
	}

	public override void SetValue(object? component, object? value)
	{
		var s = value?.ToString() ?? string.Empty;
		_owner.State.Fields[_fieldName] = s;
		_owner.NotifyChanged(Name);
	}

	public override void ResetValue(object? component) => SetValue(component, string.Empty);
	public override bool CanResetValue(object? component) => true;
	public override bool ShouldSerializeValue(object? component) => false;

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

internal sealed class StagedObjectPropertyDescriptor : PropertyDescriptor
{
	private readonly StagedObjectView _owner;
	private readonly string _fieldName;
	private readonly string _childClass;
	private readonly IReadOnlyList<PresentationNode> _schema;

	public StagedObjectPropertyDescriptor(
		string name,
		string displayName,
		string? category,
		StagedObjectView owner,
		string fieldName,
		string childClass,
		IReadOnlyList<PresentationNode> schema
	)
		: base(name, BuildAttributes(displayName, category))
	{
		_owner = owner;
		_fieldName = fieldName;
		_childClass = childClass;
		_schema = schema;
	}

	public override Type ComponentType => typeof(StagedObjectView);
	public override bool IsReadOnly => true;
	public override Type PropertyType => typeof(StagedObjectView);

	public override object GetValue(object? component)
	{
		var child = _owner.State.GetOrCreateObject(_fieldName, _childClass);
		return new StagedObjectView(DisplayName, _childClass, child, _schema);
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

internal sealed class StagedSequencePropertyDescriptor : PropertyDescriptor
{
	private readonly StagedObjectView _owner;
	private readonly string _fieldName;
	private readonly string _itemClass;
	private readonly IReadOnlyList<PresentationNode> _itemSchema;
	private readonly bool _isVirtualized;

	public StagedSequencePropertyDescriptor(
		string name,
		string displayName,
		string? category,
		StagedObjectView owner,
		string fieldName,
		string itemClass,
		IReadOnlyList<PresentationNode> itemSchema,
		bool isVirtualized
	)
		: base(name, BuildAttributes(displayName, category))
	{
		_owner = owner;
		_fieldName = fieldName;
		_itemClass = itemClass;
		_itemSchema = itemSchema;
		_isVirtualized = isVirtualized;
	}

	public override Type ComponentType => typeof(StagedObjectView);
	public override bool IsReadOnly => true;
	public override Type PropertyType => typeof(StagedSequenceView);

	public override object GetValue(object? component)
	{
		var seq = _owner.State.GetOrCreateSequence(_fieldName, _itemClass);
		return new StagedSequenceView(DisplayName, _itemClass, seq, _itemSchema, _isVirtualized);
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

internal sealed class StagedSequenceItemPropertyDescriptor : PropertyDescriptor
{
	private readonly StagedSequenceView _owner;
	private readonly int _index;
	private readonly string _itemClass;
	private readonly IReadOnlyList<PresentationNode> _itemSchema;
	private StagedSequenceItemView? _cached;

	public StagedSequenceItemPropertyDescriptor(
		string name,
		string displayName,
		StagedSequenceView owner,
		int index,
		string itemClass,
		IReadOnlyList<PresentationNode> itemSchema
	)
		: base(name, new Attribute[] { new DisplayNameAttribute(displayName) })
	{
		_owner = owner;
		_index = index;
		_itemClass = itemClass;
		_itemSchema = itemSchema;
	}

	public override Type ComponentType => typeof(StagedSequenceView);
	public override bool IsReadOnly => true;
	public override Type PropertyType => typeof(StagedSequenceItemView);

	public override object GetValue(object? component)
	{
		// Virtualization boundary: don't force materialization of the staged item
		// (and therefore don't create nested editor trees) unless/ until expanded.
		return _cached ??= new StagedSequenceItemView(DisplayName, _owner, _index, _itemClass, _itemSchema);
	}

	public override void SetValue(object? component, object? value) { }
	public override void ResetValue(object? component) { }
	public override bool CanResetValue(object? component) => false;
	public override bool ShouldSerializeValue(object? component) => false;
}

[TypeConverter(typeof(ExpandableObjectConverter))]
internal sealed class StagedSequenceItemView : ICustomTypeDescriptor
{
	private readonly StagedSequenceView _owner;
	private readonly int _index;
	private readonly string _itemClass;
	private readonly IReadOnlyList<PresentationNode> _itemSchema;
	private StagedObjectView? _materialized;

	public StagedSequenceItemView(
		string displayName,
		StagedSequenceView owner,
		int index,
		string itemClass,
		IReadOnlyList<PresentationNode> itemSchema
	)
	{
		DisplayName = displayName;
		_owner = owner;
		_index = index;
		_itemClass = itemClass;
		_itemSchema = itemSchema;
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

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) =>
		EventDescriptorCollection.Empty;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) =>
		attributes is null
			? TypeDescriptor.GetProperties(GetMaterialized())
			: TypeDescriptor.GetProperties(GetMaterialized(), attributes);

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() =>
		TypeDescriptor.GetProperties(GetMaterialized());

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => GetMaterialized();

	private StagedObjectView GetMaterialized()
	{
		if (_materialized is not null)
			return _materialized;

		// Materialize only when expanded/inspected.
		var item = _owner.State.EnsureItem(_index);
		_materialized = new StagedObjectView(DisplayName, _itemClass, item, _itemSchema);
		return _materialized;
	}
}