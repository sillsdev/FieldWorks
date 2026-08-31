// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	[TestFixture]
	public class IdentityStabilityTests
	{
		[Test]
		public void DetailLayoutIdentity_UsesCanonicalNullAndEmptyChoiceSemantics()
		{
			var detailNull = new DetailLayoutIdentity("LexEntry", "detail", "Normal", null);
			var detailEmpty = new DetailLayoutIdentity("lexentry", "DETAIL", "normal", string.Empty);
			var canonicalNull = new ViewDefinitionIdentity("LexEntry", "detail", "Normal", null);
			var canonicalEmpty = new ViewDefinitionIdentity("lexentry", "DETAIL", "normal", string.Empty);

			Assert.That(detailNull, Is.Not.EqualTo(detailEmpty));
			Assert.That(new HashSet<DetailLayoutIdentity> { detailNull, detailEmpty }, Has.Count.EqualTo(2));
			Assert.That(detailNull.GetHashCode(), Is.EqualTo(canonicalNull.GetHashCode()));
			Assert.That(detailEmpty.GetHashCode(), Is.EqualTo(canonicalEmpty.GetHashCode()));
		}

		[Test]
		public void DetailLayoutPartIdentity_FreezesCallerLayoutPathForSetMembership()
		{
			var layout = new DetailLayoutIdentity("LexEntry", "detail", "Normal", null);
			var replacement = new DetailLayoutIdentity("LexSense", "detail", "Normal", "choice");
			var source = new List<DetailLayoutIdentity> { layout };
			var identity = new DetailLayoutPartIdentity(layout, "part[0]", source);
			var expected = new DetailLayoutPartIdentity(layout, "part[0]", new[] { layout });
			var hash = identity.GetHashCode();
			var set = new HashSet<DetailLayoutPartIdentity> { identity };

			source[0] = replacement;
			source.Add(replacement);

			Assert.That(identity.LayoutPath, Has.Count.EqualTo(1));
			Assert.That(identity.LayoutPath[0], Is.EqualTo(layout));
			var list = (IList<DetailLayoutIdentity>)identity.LayoutPath;
			Assert.That(list.IsReadOnly, Is.True);
			Assert.Throws<NotSupportedException>(() => list[0] = replacement);
			Assert.That(identity.GetHashCode(), Is.EqualTo(hash));
			Assert.That(set.Contains(identity), Is.True);
			Assert.That(identity, Is.EqualTo(expected));
		}

		[Test]
		public void DetailField_LayoutPathSetterFreezesSourceList()
		{
			var layout = new DetailLayoutIdentity("LexEntry", "detail", "Normal", null);
			var replacement = new DetailLayoutIdentity("LexSense", "detail", "Normal", "choice");
			var source = new List<DetailLayoutIdentity> { layout };
			var field = new DetailField("id", "Label", "Field", null, DetailFieldKind.Text,
				EditorClassification.Known, null, null, HostRouting.Product, null, null, null);
			field.ClassName = layout.ClassName;
			field.LayoutType = layout.LayoutType;
			field.LayoutName = layout.LayoutName;
			field.LayoutPath = source;
			var identity = field.LayoutPartIdentity;
			var hash = identity.GetHashCode();
			var set = new HashSet<DetailLayoutPartIdentity> { identity };

			source[0] = replacement;
			source.Add(replacement);

			Assert.That(field.LayoutPath, Has.Count.EqualTo(1));
			Assert.That(field.LayoutPath[0], Is.EqualTo(layout));
			var list = (IList<DetailLayoutIdentity>)field.LayoutPath;
			Assert.That(list.IsReadOnly, Is.True);
			Assert.Throws<NotSupportedException>(() => list[0] = replacement);
			Assert.That(field.LayoutPartIdentity.GetHashCode(), Is.EqualTo(hash));
			Assert.That(set.Contains(field.LayoutPartIdentity), Is.True);
		}

		[Test]
		public void DetailLayoutPartIdentity_NullPathDefaultsToLayoutButEmptyPathStaysEmpty()
		{
			var layout = new DetailLayoutIdentity("LexEntry", "detail", "Normal", null);
			var defaultPath = new DetailLayoutPartIdentity(layout, "part[0]");
			var emptyPath = new DetailLayoutPartIdentity(layout, "part[0]", new List<DetailLayoutIdentity>());

			Assert.That(defaultPath.LayoutPath, Has.Count.EqualTo(1));
			Assert.That(defaultPath, Is.Not.EqualTo(emptyPath));
		}

		[Test]
		public void ViewDefinitionCacheKey_DistinguishesNullAndEmptyForEveryIdentityPart()
		{
			var nullClass = new ViewDefinitionCacheKey(null, "Normal", "detail", "choice", "fp");
			var emptyClass = new ViewDefinitionCacheKey(string.Empty, "Normal", "detail", "choice", "fp");
			var nullName = new ViewDefinitionCacheKey("LexEntry", null, "detail", "choice", "fp");
			var emptyName = new ViewDefinitionCacheKey("LexEntry", string.Empty, "detail", "choice", "fp");
			var nullType = new ViewDefinitionCacheKey("LexEntry", "Normal", null, "choice", "fp");
			var emptyType = new ViewDefinitionCacheKey("LexEntry", "Normal", string.Empty, "choice", "fp");
			var nullChoice = new ViewDefinitionCacheKey("LexEntry", "Normal", "detail", null, "fp");
			var emptyChoice = new ViewDefinitionCacheKey("LexEntry", "Normal", "detail", string.Empty, "fp");

			Assert.That(nullClass, Is.Not.EqualTo(emptyClass));
			Assert.That(nullName, Is.Not.EqualTo(emptyName));
			Assert.That(nullType, Is.Not.EqualTo(emptyType));
			Assert.That(nullChoice, Is.Not.EqualTo(emptyChoice));
			Assert.That(new HashSet<ViewDefinitionCacheKey> { nullClass, emptyClass },
				Has.Count.EqualTo(2));
			Assert.That(new HashSet<ViewDefinitionCacheKey> { nullName, emptyName },
				Has.Count.EqualTo(2));
			Assert.That(new HashSet<ViewDefinitionCacheKey> { nullType, emptyType },
				Has.Count.EqualTo(2));
			Assert.That(new HashSet<ViewDefinitionCacheKey> { nullChoice, emptyChoice },
				Has.Count.EqualTo(2));
			Assert.That(nullClass.ClassName, Is.Null);
			Assert.That(nullName.LayoutName, Is.Null);
			Assert.That(nullType.LayoutType, Is.Null);
			Assert.That(nullChoice.ChoiceGuid, Is.Null);
		}
	}
}
