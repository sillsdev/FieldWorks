// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using LanguageExplorer;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Partial test of FlexStylesXmlAccessor.
	/// </summary>
	[TestFixture]
	public class FlexStylesXmlAccessorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// This is not a very comprehensive test. There are important behaviors it does not test, such as the renaming
		/// when the imported style has different values for the context-related properties. This test just covers
		/// the new behaviors for one particular issue, in particular, that when replacing a style with a substitute guid,
		/// the Rules are preserved and the Next and BasedOn are successfully adjusted.
		/// </summary>
		[Test]
		public void FindOrCreateStyle_CopiesAllProperties()
		{
			var style1 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style1);
			var style2 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style2);
			var style3 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style3);

			style1.Name = "testA";
			style1.Type = StyleType.kstParagraph;
			style1.Context = ContextValues.General;
			style1.Structure = StructureValues.Undefined;
			style1.Function = FunctionValues.Prose;
			style1.IsBuiltIn = true;
			style1.IsModified = true;
			style1.NextRA = style1;
			var props1 = TsStringUtils.MakeProps("mystyle", Cache.DefaultAnalWs);
			style1.Rules = props1;
			style1.UserLevel = 5;

			style2.Name = "testB";
			style2.Type = StyleType.kstParagraph;
			style2.Context = ContextValues.General;
			style2.Structure = StructureValues.Undefined;
			style2.Function = FunctionValues.Prose;
			style2.IsBuiltIn = true;
			style2.IsModified = true;
			style2.NextRA = style1;
			style2.BasedOnRA = style1;

			style3.Name = "testC";
			style3.BasedOnRA = style2;

			var factoryGuid1 = Guid.NewGuid();
			var factoryGuid2 = Guid.NewGuid();

			var sut = new FlexStylesXmlAccessor(Cache.LanguageProject.LexDbOA, prepareForTests: true);

			sut.FindOrCreateStyle("testA", StyleType.kstParagraph, ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, factoryGuid1);
			sut.FindOrCreateStyle("testB", StyleType.kstParagraph, ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, factoryGuid2);

			Assert.That(style1.IsValidObject, Is.False, "should have deleted original style in course of changing guid");

			var newStyle1 = Cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(factoryGuid1); // will throw if not found
			Assert.That(newStyle1.Name, Is.EqualTo("testA"));
			Assert.That(newStyle1.IsBuiltIn, Is.True);
			Assert.That(newStyle1.IsModified, Is.True);
			Assert.That(newStyle1.NextRA, Is.EqualTo(newStyle1), "should have kept the self-referential next style");
			Assert.That(newStyle1.Rules, Is.EqualTo(props1));
			Assert.That(newStyle1.UserLevel, Is.EqualTo(5));

			var newStyle2 = Cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(factoryGuid2); // will throw if not found
			Assert.That(newStyle2.Name, Is.EqualTo("testB"));
			Assert.That(newStyle2.IsBuiltIn, Is.True);
			Assert.That(newStyle2.IsModified, Is.True);
			Assert.That(newStyle2.NextRA, Is.EqualTo(newStyle1), "should have transferred the next style ref to the replacement");
			Assert.That(newStyle2.BasedOnRA, Is.EqualTo(newStyle1), "should have transferred the base style ref to the replacement");

			Assert.That(style3.BasedOnRA, Is.EqualTo(newStyle2), "should have transferred the base style ref to the replacement, even for style not processed");
		}

		/// <summary>
		/// There is a problem when we try to copy the 'next style' info while setting the guid of a style,
		/// and the next style is not currently valid: we trigger an exception in the NextRA setter. Make sure this is dealt with.
		/// </summary>
		[Test]
		public void FindOrCreateStyle_DoesNotCopyInvalidNextStyle()
		{
			var style1 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style1);
			var style2 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style2);

			style1.Name = "testA";
			style1.Type = StyleType.kstParagraph;
			style1.Context = ContextValues.Text;
			style1.Structure = StructureValues.Body;
			style1.Function = FunctionValues.Prose;
			style1.IsBuiltIn = true;
			style1.IsModified = true;
			style1.NextRA = style1;
			var props1 = TsStringUtils.MakeProps("mystyle", Cache.DefaultAnalWs);
			style1.Rules = props1;
			style1.UserLevel = 5;

			style2.Name = "testB";
			style2.Type = StyleType.kstParagraph;
			style2.Context = ContextValues.Text;
			style2.Structure = StructureValues.Body;
			style2.Function = FunctionValues.Prose;
			style2.IsBuiltIn = true;
			style2.IsModified = true;
			style2.NextRA = style1;
			style2.BasedOnRA = style1;

			// Now that we've created testB, change the category of testA. This creates a state that the system tries to prevent.
			// The 'next' style of a body style should not be a heading style. We catch this when setting NextRA, but not
			// when changing the Structure, since that normally never happens.
			style1.Structure = StructureValues.Heading;

			var factoryGuid1 = Guid.NewGuid();
			var factoryGuid2 = Guid.NewGuid();

			var sut = new FlexStylesXmlAccessor(Cache.LanguageProject.LexDbOA, prepareForTests: true);

			sut.FindOrCreateStyle("testA", StyleType.kstParagraph, ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, factoryGuid1);
			sut.FindOrCreateStyle("testB", StyleType.kstParagraph, ContextValues.Text, StructureValues.Body, FunctionValues.Prose, factoryGuid2);

			Assert.That(style1.IsValidObject, Is.False, "should have deleted original style in course of changing guid");

			IStStyle newStyle1;
			Assert.That(Cache.ServiceLocator.GetInstance<IStStyleRepository>().TryGetObject(factoryGuid1, out newStyle1), Is.True, "should have created a new style with the specified guid");
			Assert.That(newStyle1.Name, Is.EqualTo("testA"));
			Assert.That(newStyle1.IsBuiltIn, Is.True);
			Assert.That(newStyle1.IsModified, Is.True);
			Assert.That(newStyle1.NextRA, Is.EqualTo(newStyle1), "should have kept the self-referential next style");
			Assert.That(newStyle1.Rules, Is.EqualTo(props1));
			Assert.That(newStyle1.UserLevel, Is.EqualTo(5));

			IStStyle newStyle2;
			Assert.That(Cache.ServiceLocator.GetInstance<IStStyleRepository>().TryGetObject(factoryGuid2, out newStyle2), Is.True, "should have created second new style with the specified guid");
			Assert.That(newStyle2.Name, Is.EqualTo("testB"));
			Assert.That(newStyle2.IsBuiltIn, Is.True);
			Assert.That(newStyle2.IsModified, Is.True);
			Assert.That(newStyle2.NextRA, Is.Null, "should have cleared the invalid next style");
			Assert.That(newStyle2.BasedOnRA, Is.EqualTo(newStyle1), "should have transferred the base style ref to the replacement");
		}

		/// <summary />
		[Test]
		public void FindOrCreateStyle_HandlesConflictingUserStyles()
		{
			var userStyle = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(userStyle);
			const string styleName = "StyleName";

			userStyle.Name = styleName;
			userStyle.Type = StyleType.kstParagraph;
			userStyle.Context = ContextValues.General;
			userStyle.Structure = StructureValues.Undefined;
			userStyle.Function = FunctionValues.Prose;
			userStyle.IsBuiltIn = false;
			userStyle.IsModified = true;
			userStyle.NextRA = userStyle;
			var propsFactory = TsPropsFactoryClass.Create();
			var props1 = propsFactory.MakeProps("mystyle", Cache.DefaultAnalWs, 0);
			userStyle.Rules = props1;

			var userGuid = userStyle.Guid;
			var factoryGuid = Guid.NewGuid();

			var sut = new FlexStylesXmlAccessor(Cache.LanguageProject.LexDbOA, prepareForTests: true);

			sut.FindOrCreateStyle(styleName, StyleType.kstCharacter, ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, factoryGuid);

			var userStyle1 = Cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(userGuid); // will throw if not found
			Assert.That(userStyle1, Is.SameAs(userStyle));
			Assert.That(userStyle1.IsValidObject, "should still be valid");
			Assert.That(userStyle1.Name, Is.Not.EqualTo(styleName));
			Assert.That(userStyle1.IsBuiltIn, Is.False, "User style built in");
			Assert.That(userStyle1.IsModified, Is.True, "user style modified");
			Assert.That(userStyle1.NextRA, Is.EqualTo(userStyle1), "should have kept the self-referential next style");
			Assert.That(userStyle1.Rules, Is.EqualTo(props1));
			Assert.That(userStyle1.Guid, Is.EqualTo(userGuid), "Should have maintained its GUID");

			var factoryStyle = Cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(factoryGuid); // will throw if not found
			Assert.That(factoryStyle.Name, Is.EqualTo(styleName));
			Assert.That(factoryStyle.IsBuiltIn, Is.True, "factory style built in");
			Assert.That(factoryStyle.IsModified, Is.False, "factory style modified");
			Assert.That(factoryStyle.Owner, Is.Not.Null, "factory style owner");
			Assert.That(factoryStyle.Guid, Is.EqualTo(factoryGuid), "Should have the factory-specifiied GUID");
		}

		/// <summary>
		/// Test that scripture styles get moved to the language project now that TE has been removed.
		/// </summary>
		[Test]
		public void FindOrCreateStyles_SucceedsAfterStylesMovedFromScripture()
		{
			var scr = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var scriptureStyle = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			scr.StylesOC.Add(scriptureStyle);
			var styleName = "Scripture Style";
			scriptureStyle.Name = styleName;

			scriptureStyle.Type = StyleType.kstParagraph;
			scriptureStyle.Context = ContextValues.Text;
			scriptureStyle.Structure = StructureValues.Body;
			scriptureStyle.Function = FunctionValues.Prose;
			scriptureStyle.IsBuiltIn = true;
			scriptureStyle.IsModified = true;
			scriptureStyle.NextRA = scriptureStyle;
			var props1 = TsStringUtils.MakeProps("mystyle", Cache.DefaultAnalWs);
			scriptureStyle.Rules = props1;
			scriptureStyle.UserLevel = 5;

			var scrStyleGuid = scriptureStyle.Guid;
			var sut = new FlexStylesXmlAccessor(Cache.LanguageProject.LexDbOA, prepareForTests: true);
			Assert.That(scr.StylesOC.Count, Is.EqualTo(0), "Style should have been removed from Scripture.");
			Assert.That(Cache.LangProject.StylesOC.Count, Is.EqualTo(1), "Style should have been added to language project.");
			Assert.That(Cache.LangProject.StylesOC.First().Name, Is.EqualTo(styleName), "Style name should not have changed.");
			var movedStyle = sut.FindOrCreateStyle(styleName, StyleType.kstParagraph, ContextValues.Text, StructureValues.Body, FunctionValues.Prose, scrStyleGuid);

			Assert.That(movedStyle, Is.EqualTo(scriptureStyle));
			Assert.That(movedStyle.Name, Is.EqualTo(styleName), "Style name should not have changed.");
			Assert.That(movedStyle.Owner, Is.EqualTo(Cache.LangProject), "The style owner should be the language project.");
		}
	}
}