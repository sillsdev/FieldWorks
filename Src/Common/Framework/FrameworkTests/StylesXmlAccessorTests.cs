using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Partial test of the StylesXmlAccessor. More tests for this class are in TeStylesXmlAccessorTests.cs.
	/// </summary>
	[TestFixture]
	public class StylesXmlAccessorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{

		/// <summary>
		/// This is not a very comprehensive test. There are important behaviors it does not test, such as the renaming
		/// when the imported style has different values for the context-releated properties. This test just covers
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
			var propsFactory = TsPropsFactoryClass.Create();
			var props1 = propsFactory.MakeProps("mystyle", Cache.DefaultAnalWs, 0);
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

			var sut = new TestAccessorForFindOrCreateStyle(Cache);

			sut.FindOrCreateStyle("testA", StyleType.kstParagraph, ContextValues.General, StructureValues.Undefined,
				FunctionValues.Prose, factoryGuid1);
			sut.FindOrCreateStyle("testB", StyleType.kstParagraph, ContextValues.General, StructureValues.Undefined,
				FunctionValues.Prose, factoryGuid2);

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

			Assert.That(style3.BasedOnRA, Is.EqualTo(newStyle2),
				"should have transferred the base style ref to the replacement, even for style not processed");
		}
	}

	/// <summary>
	/// StylesXmlAccessor is an abstract class designed to be subclassed for a specific stylesheet.
	/// Here, so far, we just want to test one method of the base class, so we make a trivial subclass which trivially
	/// implements (by throwing) abstract methods not needed by the one we want to test.
	/// The FindOrCreateStyle method is normally called (indirectly) by CreateStyles(), which initializes m_databaseStyles
	/// to contain all the pre-existing styles. For this test we just do this in the constructor of our private subclass.
	/// </summary>
	class TestAccessorForFindOrCreateStyle : StylesXmlAccessor
	{
		public TestAccessorForFindOrCreateStyle(FdoCache cache) : base(cache)
		{
			m_databaseStyles = cache.LangProject.StylesOC;
			// see class comment. This would not be normal behavior for a StylesXmlAccessor subclass constructor.
			foreach (var sty in m_databaseStyles)
				m_htOrigStyles[sty.Name] = sty;
		}
		protected override string ResourceFilePathFromFwInstall
		{
			get { throw new NotImplementedException(); }
		}

		protected override string ResourceName
		{
			get { throw new NotImplementedException(); }
		}

		protected override FdoCache Cache
		{
			get { return m_cache; }
		}

		protected override IFdoOwningCollection<ICmResource> ResourceList
		{
			get { throw new NotImplementedException(); }
		}

		protected override IFdoOwningCollection<IStStyle> StyleCollection
		{
			get { throw new NotImplementedException(); }
		}
	}
}
