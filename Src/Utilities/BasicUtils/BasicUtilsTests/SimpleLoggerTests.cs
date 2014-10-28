// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.Utils
{
	[TestFixture]
	class SimpleLoggerTests
	{
		[Test]
		public void HasContent_ReturnsTrueIfAny()
		{
			using (var logger = new SimpleLogger())
			{
				logger.WriteLine(""); // should append a newline
				Assert.That(logger.HasContent);
			}
		}

		[Test]
		public void HasContent_ReturnsFalseIfNone()
		{
			using (var logger = new SimpleLogger())
			{
				Assert.False(logger.HasContent);
			}
		}

		[Test]
		public void Content_ReturnsContent()
		{
			using (var logger = new SimpleLogger())
			{
				logger.WriteLine("Sample Text");
				Assert.AreEqual("Sample Text" + Environment.NewLine, logger.Content);
			}
		}

		[Test]
		public void Content_DisposesLogger()
		{
			using (var logger = new SimpleLogger())
			{
				var foo = logger.Content;
				Assert.That(logger.IsDisposed);
				Assert.Throws<ObjectDisposedException>(() => foo = logger.Content);
			}
		}
	}
}
