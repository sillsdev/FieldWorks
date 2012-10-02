using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class TextSourceTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		ITsStrFactory tsf ;
		ILgWritingSystemFactory wsf;
		int wsEn;
		private int wsFrn;
		private ITsTextProps ttpFrn;

		[TestFixtureSetUp]
		public void Setup()
		{
			tsf = TsStrFactoryClass.Create();
			wsf = new MockWsf();
			wsEn = wsf.GetWsFromStr("en");
			wsFrn = wsf.GetWsFromStr("fr");
			ITsPropsBldr propBldr = TsPropsBldrClass.Create();
			propBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsFrn);
			ttpFrn = propBldr.GetTextProps();
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			tsf = null;
			wsf = null;
		}
		[Test]
		public void StringsInSource()
		{
			ITsString tss = tsf.MakeString("abc def", wsEn);
			AssembledStyles styles = new AssembledStyles();
			TssClientRun clientRun = new TssClientRun(tss, styles);
			var clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun);
			TextSource ts = new TextSource(clientRuns);
		}

		[Test]
		public void TssRuns()
		{
			string part1 = "abc def";
			ITsString tss = tsf.MakeString(part1, wsEn);
			AssembledStyles styles = new AssembledStyles();
			TssClientRun clientRun = new TssClientRun(tss, styles);
			Assert.AreEqual(1, clientRun.UniformRunCount);
			Assert.AreEqual(part1, clientRun.UniformRunText(0));
			AssembledStyles style1 = clientRun.UniformRunStyles(0);
			Assert.AreEqual(wsEn, style1.Ws);
			Assert.AreEqual(0, clientRun.UniformRunStart(0));
			Assert.AreEqual(part1.Length, clientRun.UniformRunLength(0));

			string part2 = " ghi";
			ITsStrBldr bldr = tss.GetBldr();
			bldr.Replace(part1.Length, part1.Length, part2, ttpFrn);
			TssClientRun clientRun2 = new TssClientRun(bldr.GetString(), styles);
			Assert.AreEqual(2, clientRun2.UniformRunCount);
			Assert.AreEqual(part1, clientRun2.UniformRunText(0));
			Assert.AreEqual(part2, clientRun2.UniformRunText(1));
			style1 = clientRun2.UniformRunStyles(0);
			Assert.AreEqual(wsEn, style1.Ws);
			AssembledStyles style2 = clientRun2.UniformRunStyles(1);
			Assert.AreEqual(wsFrn, style2.Ws);
			Assert.AreEqual(0, clientRun2.UniformRunStart(0));
			Assert.AreEqual(part1.Length, clientRun2.UniformRunLength(0));
			Assert.AreEqual(part1.Length, clientRun2.UniformRunStart(1));
			Assert.AreEqual(part2.Length, clientRun2.UniformRunLength(1));

			var source = new TextSource(new List<IClientRun>(new [] {clientRun2}));
			var runs = source.Runs;
			Assert.That(runs.Length, Is.EqualTo(2));
			Assert.That(runs[0].LogLength, Is.EqualTo(part1.Length));
			Assert.That(runs[1].LogLength, Is.EqualTo(part2.Length));
			Assert.That(runs[1].LogStart, Is.EqualTo(part1.Length));
			Assert.That(runs[1].Offset, Is.EqualTo(0)); // nothing fancy with ORCs, run starts at 0 in uniform run.
		}

		[Test]
		public void MlsRuns()
		{
			string part1 = "abc def";
			IViewMultiString mls = new MultiAccessor(wsEn, wsEn);
			mls.set_String(wsEn, tsf.MakeString(part1, wsEn));
			AssembledStyles styles = new AssembledStyles();
			MlsClientRun clientRun = new MlsClientRun(mls, styles.WithWs(wsEn));
			Assert.AreEqual(1, clientRun.UniformRunCount);
			Assert.AreEqual(part1, clientRun.UniformRunText(0));
			AssembledStyles style1 = clientRun.UniformRunStyles(0);
			Assert.AreEqual(wsEn, style1.Ws);
			Assert.AreEqual(0, clientRun.UniformRunStart(0));
			Assert.AreEqual(part1.Length, clientRun.UniformRunLength(0));

			string part2 = " ghi";
			ITsStrBldr bldr = mls.get_String(wsEn).GetBldr();
			bldr.Replace(part1.Length, part1.Length, part2, ttpFrn);
			IViewMultiString multibldr = new MultiAccessor(wsEn, wsEn);
			multibldr.set_String(wsFrn, bldr.GetString());
			MlsClientRun clientRun2 = new MlsClientRun(multibldr, styles.WithWs(wsFrn));
			Assert.AreEqual(2, clientRun2.UniformRunCount);
			Assert.AreEqual(part1, clientRun2.UniformRunText(0));
			Assert.AreEqual(part2, clientRun2.UniformRunText(1));
			style1 = clientRun2.UniformRunStyles(0);
			Assert.AreEqual(wsEn, style1.Ws);
			AssembledStyles style2 = clientRun2.UniformRunStyles(1);
			Assert.AreEqual(wsFrn, style2.Ws);
			Assert.AreEqual(0, clientRun2.UniformRunStart(0));
			Assert.AreEqual(part1.Length, clientRun2.UniformRunLength(0));
			Assert.AreEqual(part1.Length, clientRun2.UniformRunStart(1));
			Assert.AreEqual(part2.Length, clientRun2.UniformRunLength(1));
		}

		[Test]
		public void StringRun()
		{
			string part1 = "abc def";
			AssembledStyles styles = new AssembledStyles().WithWs(wsEn);
			StringClientRun clientRun = new StringClientRun(part1, styles);
			Assert.AreEqual(1, clientRun.UniformRunCount);
			Assert.AreEqual(part1, clientRun.UniformRunText(0));
			AssembledStyles style1 = clientRun.UniformRunStyles(0);
			Assert.AreEqual(wsEn, style1.Ws);
			Assert.AreEqual(0, clientRun.UniformRunStart(0));
			Assert.AreEqual(part1.Length, clientRun.UniformRunLength(0));
		}

		private string orcText = "xyz";
		IClientRun MockInterpretOrc(TextClientRun run, int offset)
		{
			return new StringClientRun(orcText, run.UniformRunStyles(0).WithWs(wsFrn));
		}

		[Test]
		public void MultiUniformRuns()
		{
			// Run 0
			string part0 = "abc def";
			AssembledStyles styles = new AssembledStyles().WithWs(wsEn);
			StringClientRun clientRun0 = new StringClientRun(part0, styles);
			var clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);

			// Run 1
			string part1 = " ghijk";
			string part2 = " lmno";
			ITsString tss = tsf.MakeString(part2, wsEn);
			ITsStrBldr bldr = tss.GetBldr();
			bldr.Replace(0, 0, part1, ttpFrn);
			TssClientRun clientRun1 = new TssClientRun(bldr.GetString(), styles);
			clientRuns.Add(clientRun1);

			// Run 2
			string part3 = " pq";
			string part4 = "\xfffc";
			string part5 = "r";
			StringClientRun clientRun2 = new StringClientRun(part3+part4+part5, styles);
			clientRuns.Add(clientRun2);

			TextSource source = new TextSource(clientRuns, MockInterpretOrc);

			MapRun[] runs = source.Runs;
			Assert.AreEqual(6, runs.Length);
			VerifyRun(0, clientRun0, 0, 0, part0, runs[0], "first run of complex source (abcdef)");
			int len = part0.Length;
			VerifyRun(len, clientRun1, len, 0, part1, runs[1], "2nd run of complex source( ghijk)");
			len += part1.Length;
			VerifyRun(len, clientRun1, len, 0, 1, part2, runs[2], "3rd run of complex source( lmno)");
			len += part2.Length;
			VerifyRun(len, clientRun2, len, 0, part3, runs[3], "4th run of complex source (pq)");
			len += part3.Length;
			int orcPos = len;
			VerifyRun(len, clientRun2, len, part3.Length, orcText, runs[4], "5th run of complex source (orc->xyz)");
			int render = len + orcText.Length;
			len += 1;
			VerifyRun(len, clientRun2, render, part3.Length + part4.Length, part5, runs[5], "6th run of complex source(r)");
			len += part5.Length;
			render += part5.Length;
			Assert.AreEqual(render, source.Length, "Length of complex source");

			// LogToRen
			Assert.AreEqual(0, source.LogToRen(0));
			Assert.AreEqual(1, source.LogToRen(1));
			Assert.AreEqual(part1.Length - 1, source.LogToRen(part1.Length - 1));
			Assert.AreEqual(part1.Length, source.LogToRen(part1.Length));
			Assert.AreEqual(part1.Length + 1, source.LogToRen(part1.Length + 1));
			Assert.AreEqual(orcPos - 1, source.LogToRen(orcPos - 1));
			Assert.AreEqual(orcPos, source.LogToRen(orcPos));
			int delta = orcText.Length - 1;
			Assert.AreEqual(orcPos + 1 + delta, source.LogToRen(orcPos + 1));
			Assert.AreEqual(len + delta, source.LogToRen(len));
			Assert.AreEqual(len - 1 + delta, source.LogToRen(len - 1));

			//RenToLog
			Assert.AreEqual(0, source.RenToLog(0));
			Assert.AreEqual(1, source.RenToLog(1));
			Assert.AreEqual(part1.Length - 1, source.RenToLog(part1.Length - 1));
			Assert.AreEqual(part1.Length, source.RenToLog(part1.Length));
			Assert.AreEqual(part1.Length + 1, source.RenToLog(part1.Length + 1));
			Assert.AreEqual(orcPos - 1, source.RenToLog(orcPos - 1));
			Assert.AreEqual(orcPos, source.RenToLog(orcPos));
			Assert.AreEqual(orcPos, source.RenToLog(orcPos + orcText.Length - 1));
			Assert.AreEqual(orcPos + 1, source.RenToLog(orcPos + orcText.Length));
			Assert.AreEqual(len, source.RenToLog(len + delta));
			Assert.AreEqual(len - 1, source.RenToLog(len + delta - 1));

			// Fetch
			VerifyFetch(source, 0, 0, "");
			VerifyFetch(source, 0, 1, "a");
			VerifyFetch(source, 0, part0.Length, part0);
			VerifyFetch(source, orcPos, orcPos + orcText.Length, orcText);
			VerifyFetch(source, part0.Length, part0.Length + part1.Length, part1);
			VerifyFetch(source, part0.Length + part1.Length - 2, part0.Length + part1.Length + 2, part1.Substring(part1.Length - 2) + part2.Substring(0, 2));
			VerifyFetch(source, part0.Length, part0.Length + part1.Length + part2.Length + 1,
				part1 + part2 + part3.Substring(0, 1));
			VerifyFetch(source, orcPos + orcText.Length - 1, orcPos + orcText.Length + 1,
				orcText.Substring(orcText.Length - 1) + part5);

			// GetCharProps. (This test is too restrictive. In several cases, a larger range could be returned. OTOH it is weak
			// in only verifying the writing system to check the properties returned.)
			VerifyCharProps(source, 0, wsEn, 0, part0.Length, "props at 0 in complex string");
			VerifyCharProps(source, 2, wsEn, 0, part0.Length, "props in middle of first run in complex string");
			VerifyCharProps(source, part0.Length - 1, wsEn, 0, part0.Length, "props of last char of first run in complex string");
			VerifyCharProps(source, part0.Length, wsFrn, part0.Length, part0.Length + part1.Length, "props at start of second run in complex string");
			VerifyCharProps(source, orcPos - 1, wsEn, orcPos - part3.Length, orcPos, "props of last char before ORC");
			VerifyCharProps(source, orcPos, wsFrn, orcPos, orcPos + orcText.Length, "props of first char of ORC expansion");
			VerifyCharProps(source, orcPos + 1, wsFrn, orcPos, orcPos + orcText.Length, "props of mid char of ORC expansion");
			VerifyCharProps(source, orcPos + orcText.Length - 1, wsFrn, orcPos, orcPos + orcText.Length, "props of last char of ORC expansion");
			VerifyCharProps(source, orcPos + orcText.Length, wsEn, orcPos + orcText.Length, orcPos + orcText.Length + part5.Length,
				"props of first char after ORC expansion");
		}

		// Verify that source.Fetch(min, lim) produces the specified string
		private void VerifyFetch(TextSource source, int min, int lim, string expected)
		{
			string got;
			using (ArrayPtr ptr = new ArrayPtr((lim - min) * 2 + 2))
			{
				source.Fetch(min, lim, ptr.IntPtr);
				got = MarshalEx.NativeToString(ptr, lim - min, true);
			}
			Assert.AreEqual(expected, got, "fetch from " + min + " to " + lim);
		}

		void VerifyCharProps(TextSource source, int ich, int ws, int ichMinExpected, int ichLimExpected, string label)
		{
			LgCharRenderProps props;
			int ichMin, ichLim;
			source.GetCharProps(ich, out props, out ichMin, out ichLim);
			Assert.AreEqual(ichMinExpected, ichMin, label + " - ichMin");
			Assert.AreEqual(ichLimExpected, ichLim, label + " - ichLim");
			Assert.AreEqual(ws, props.ws, label + " - ws");
		}


		[Test]
		public void OrcBoxRun()
		{
			// Run 0
			string part0 = "abc";
			string part1 = "\xfffc";
			string part2 = "defg";

			AssembledStyles styles = new AssembledStyles().WithWs(wsEn);
			StringClientRun clientRun0 = new StringClientRun(part0+part1+part2, styles);
			var clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);

			BlockBox box = new BlockBox(styles.WithWs(wsFrn), Color.Red, 72000, 36000);

			TextSource source = new TextSource(clientRuns, (run, offset) => box);

			MapRun[] runs = source.Runs;
			Assert.AreEqual(3, runs.Length);
			VerifyRun(0, clientRun0, 0, 0, part0, runs[0], "first run of complex source with box");
			int len = part0.Length;
			VerifyRun(len, box, len, part0.Length, part1, runs[1], "2nd run of complex source with box");
			len += 1;
			VerifyRun(len, clientRun0, len, part0.Length+1, part2, runs[2], "3rd run of complex source with box");
			len += part2.Length;
			Assert.AreEqual(len, source.Length, "length of complex source with box");
			VerifyCharProps(source, part0.Length, wsFrn, part0.Length, part0.Length + 1, "props of box");
		}
		[Test]
		public void EmptyRuns()
		{
			// Run 0
			string part0 = "";
			AssembledStyles styles = new AssembledStyles().WithWs(wsEn);
			StringClientRun clientRun0 = new StringClientRun(part0, styles);
			var clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);

			// We want an empty run if it's the only thing in the paragraph.
			TextSource source = new TextSource(clientRuns, MockInterpretOrc);
			MapRun[] runs = source.Runs;
			Assert.AreEqual(1, runs.Length);
			VerifyRun(0, clientRun0, 0, 0, "", runs[0], "first run of empty source");
			Assert.AreEqual(0, source.Length, "length of empty source");
			VerifyCharProps(source, 0, wsEn, 0, 0, "props at 0 in empty string");

			// We don't want an empty run adjacent to a non-empty one.

			string part1 = "abc";
			StringClientRun clientRun1 = new StringClientRun(part1, styles);
			clientRuns.Add(clientRun1);
			source = new TextSource(clientRuns, MockInterpretOrc);
			runs = source.Runs;
			Assert.AreEqual(1, runs.Length);
			VerifyRun(0, clientRun1, 0, 0, part1, runs[0], "first run of (empty, abc) source");
			Assert.AreEqual(part1.Length, source.Length, "length of (empty, abc) source");

			// (empty, box) keeps empty
			BlockBox box = new BlockBox(styles, Color.Red, 72000, 36000);
			clientRuns[1] = box;
			source = new TextSource(clientRuns, MockInterpretOrc);
			runs = source.Runs;
			Assert.AreEqual(2, runs.Length);
			VerifyRun(0, clientRun0, 0, 0, "", runs[0], "first run of (empty, box) source");
			VerifyRun(0, box, 0, 0, "\xfffc", runs[1], "2nd run of (empty, box) source");
			Assert.AreEqual(1, source.Length, "length of (empty, box) source");

			// Two adjacent empty strings produce a single run for the first client run.

			clientRuns.RemoveAt(1);
			StringClientRun clientRun1e = new StringClientRun(part0, styles);
			clientRuns.Add(clientRun1e);
			source = new TextSource(clientRuns, MockInterpretOrc);
			runs = source.Runs;
			Assert.AreEqual(1, runs.Length);
			VerifyRun(0, clientRun0, 0, 0, "", runs[0], "first run of (empty, empty) source");
			Assert.AreEqual(0, source.Length, "length of (empty, empty) source");

			// (something,empty) keeps the something.
			clientRuns[0] = clientRun1;
			source = new TextSource(clientRuns, MockInterpretOrc);
			runs = source.Runs;
			Assert.AreEqual(1, runs.Length);
			VerifyRun(0, clientRun1, 0, 0, part1, runs[0], "first run of (abc, empty) source");
			Assert.AreEqual(part1.Length, source.Length, "length of (abc, empty) source");

			// (box, empty) keeps empty
			clientRuns[0] = box;
			source = new TextSource(clientRuns, MockInterpretOrc);
			runs = source.Runs;
			Assert.AreEqual(2, runs.Length);
			VerifyRun(0, box, 0, 0, "\xfffc", runs[0], "first run of (box, empty) source");
			VerifyRun(1, clientRun1e, 1, 0, "", runs[1], "2nd run of (box, empty) source");
			Assert.AreEqual(1, source.Length, "length of (box, empty) source");


		}

		[Test]
		public void SingleUniformRuns()
		{
			string part1 = "abc def";
			var styles = new AssembledStyles().WithWs(wsEn);
			var clientRun = new StringClientRun(part1, styles);
			var clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun);
			TextSource source = new TextSource(clientRuns);
			MapRun[] runs = source.Runs;
			Assert.AreEqual(1, runs.Length);
			VerifyRun(0, clientRun, 0, 0, part1, runs[0], "first run of simple source");
			Assert.AreEqual(part1.Length, source.Length, "length of simple source");
		}
		void VerifyRun(int logical, IClientRun clientRun, int render, int offset, string runText, MapRun run, string label)
		{
			VerifyRun(logical, clientRun, render, offset, 0, runText, run, label);
		}

		void VerifyRun(int logical, IClientRun clientRun, int render, int offset, int irun, string runText, MapRun run, string label)
		{
			Assert.AreEqual(logical, run.LogStart, label + " - logical");
			Assert.AreEqual(clientRun, run.ClientRun, label + " - cllient run");
			Assert.AreEqual(render, run.RenderStart, label + " - render");
			Assert.AreEqual(offset, run.Offset, label + " - offset");
			Assert.AreEqual(irun, run.ClientUniformRunIndex, label + "- ClientUniformRunIndex");
			Assert.AreEqual(runText, run.RenderText, label + " - RenderText");
		}

		[Test]
		public void RenderRuns()
		{
			string part0 = "abc def";
			AssembledStyles styles = new AssembledStyles().WithWs(wsEn);
			StringClientRun clientRun0 = new StringClientRun(part0, styles);
			var clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);

			string part0a = " frn";
			StringClientRun clientRun0a = new StringClientRun(part0a, styles.WithWs(wsFrn));
			clientRuns.Add(clientRun0a);

			// Run 1
			string part1 = " ghijk"; // english
			string part2 = " lmno"; // french
			ITsString tss = tsf.MakeString(part1, wsEn);
			ITsStrBldr bldr = tss.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, part2, ttpFrn);
			TssClientRun clientRun1 = new TssClientRun(bldr.GetString(), styles);
			clientRuns.Add(clientRun1);

			// Run 2a
			string part2a = " french insert";
			string part2b = " insert"; // english
			ITsString tssInsert = tsf.MakeString(part2b, wsEn);
			bldr = tssInsert.GetBldr();
			bldr.Replace(0, 0, part2a, ttpFrn);
			TssClientRun clientRun2b = new TssClientRun(bldr.GetString(), styles);
			clientRuns.Add(clientRun2b);

			// IRuntem 2
			string part3 = " pq";
			string part4 = "\xfffc";
			StringClientRun clientRun2 = new StringClientRun(part3 + part4, styles);
			clientRuns.Add(clientRun2);

			// Run 3
			string part5 = "more french";
			string part6 = "\xfffc";
			StringClientRun clientRun3 = new StringClientRun(part5 + part6, styles.WithWs(wsFrn));
			clientRuns.Add(clientRun3);

			// Run 4
			string part7 = "English";
			StringClientRun clientRun4 = new StringClientRun(part7, styles.WithWs(wsFrn));
			clientRuns.Add(clientRun4);

			BlockBox box = new BlockBox(styles.WithWs(wsFrn), Color.Red, 72000, 36000);

			TextSource source = new TextSource(clientRuns, (run, offset) => (run == clientRun2 ? new StringClientRun(orcText, run.UniformRunStyles(0).WithWs(wsFrn)) : (IClientRun)box));

			List<IRenderRun> renderRuns = source.RenderRuns;
			VerifyRenderRun(renderRuns[0], 0, part0.Length, "first - en");
			int len = part0.Length;
			VerifyRenderRun(renderRuns[1], len, part0a.Length, "0a - frn");
			len += part0a.Length;
			VerifyRenderRun(renderRuns[2], len, part1.Length, "part1 - en");
			len += part1.Length;
			VerifyRenderRun(renderRuns[3], len, part2.Length + part2a.Length, "part2 & 2a (french)");
			len += part2.Length + part2a.Length;
			VerifyRenderRun(renderRuns[4], len, part2b.Length + part3.Length, "2b and 2 (Eng)");
			len += part2b.Length + part3.Length;
			VerifyRenderRun(renderRuns[5], len, orcText.Length + part5.Length, "orc + other french");
			len += orcText.Length + part5.Length;
			VerifyRenderRun(renderRuns[6], len, 1, "single box");
			len += 1;
			VerifyRenderRun(renderRuns[7], len, part7.Length, "run with same props as preceding box");
			Assert.AreEqual(8, renderRuns.Count);

		}

		void VerifyRenderRun(IRenderRun run, int start, int length, string label)
		{
			Assert.AreEqual(start, run.RenderStart, label + " - start");
			Assert.AreEqual(length, run.RenderLength, label + " - length");
		}

		/// <summary>
		/// Test creating a new text source in which one item is replace by a new one.
		/// </summary>
		[Test]
		public void StringChangedTests()
		{
			string part0 = "abc";
			AssembledStyles styles = new AssembledStyles().WithWs(wsEn);
			StringClientRun clientRun0 = new StringClientRun(part0, styles);
			var clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);
			TextSource source = new TextSource(clientRuns);
			List<IRenderRun> renderRuns = source.RenderRuns;
			VerifyRenderRun(renderRuns[0], 0, 3, "initial state has all in one run");
			var clientRun1 = new StringClientRun("abcd", styles);
			var output1 = source.ClientRunChanged(0, clientRun1);
			Assert.AreEqual(1, output1.NewSource.RenderRuns.Count, "replacing single run with simple string should produce single render run.");
			VerifyRenderRun(output1.NewSource.RenderRuns[0], 0, 4, "replacing client run should make a new source with modified single run");
			Assert.AreEqual(3, output1.StartChange);
			Assert.AreEqual(0, output1.DeleteCount);
			Assert.AreEqual(1, output1.InsertCount);

			// try changing the middle of three runs, from a simple one to a complex one.
			clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);
			string part2 = "def";
			var clientRun2 = new StringClientRun(part2, styles);
			clientRuns.Add(clientRun2);
			string part3 = " mnop";
			var clientRun3 = new StringClientRun(part3, styles);
			clientRuns.Add(clientRun3);
			source = new TextSource(clientRuns, MockInterpretOrc);

			string part4 = "q\xfffc";
			var clientRun4 = new StringClientRun(part4, styles);
			var output2 = source.ClientRunChanged(1, clientRun4);
			Assert.AreEqual(3, output2.NewSource.RenderRuns.Count,
							"three render runs because ORC interprets as french string.");
			VerifyRenderRun(output2.NewSource.RenderRuns[0], 0, part0.Length + 1, "first run up to ORC");
			VerifyRenderRun(output2.NewSource.RenderRuns[1], part0.Length + 1, orcText.Length, "second run is French from ORC");
			VerifyRenderRun(output2.NewSource.RenderRuns[2], part0.Length + 1 + orcText.Length, part3.Length, "third run is  stuff after ORC");
			VerifyFetch(output2.NewSource, 0, output2.NewSource.Length, part0 + "q" + orcText + part3);
			Assert.AreEqual(part0.Length, output2.StartChange);
			Assert.AreEqual(part2.Length, output2.DeleteCount);
			Assert.AreEqual(1 + orcText.Length, output2.InsertCount);

			// Now do a variation where some of the new run survives at each end.
			// To catch a tricky special case, we want to replace some regular text with an ORC
			// that expands to the same thing.
			var bldr = tsf.MakeString("de" + orcText, wsEn).GetBldr();
			bldr.SetIntPropValues(2, 2 + orcText.Length, (int)FwTextPropType.ktptWs,
				(int) FwTextPropVar.ktpvDefault, wsFrn);
			var clientRunFakeOrc = new TssClientRun(bldr.GetString(), styles);
			clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);
			clientRuns.Add(clientRunFakeOrc);
			clientRuns.Add(clientRun3);
			source = new TextSource(clientRuns, MockInterpretOrc);

			string partDeqOrc = "deq\xfffc";
			var clientRun5 = new StringClientRun(partDeqOrc, styles);
			var output3 = source.ClientRunChanged(1, clientRun5);
			Assert.AreEqual(3, output3.NewSource.RenderRuns.Count,
							"three render runs because ORC interprets as french string.");
			VerifyRenderRun(output3.NewSource.RenderRuns[0], 0, part0.Length + 3, "first run up to ORC");
			VerifyRenderRun(output3.NewSource.RenderRuns[1], part0.Length + 3, orcText.Length, "second run is French from ORC");
			VerifyRenderRun(output3.NewSource.RenderRuns[2], part0.Length + 3 + orcText.Length, part3.Length, "third run is  stuff after ORC");
			VerifyFetch(output3.NewSource, 0, output3.NewSource.Length, part0 + "deq" + orcText + part3);
			// This should be interpreted as inserting the "q" after the "de" and before the orc text.
			Assert.AreEqual(part0.Length + 2, output3.StartChange);
			Assert.AreEqual(0, output3.DeleteCount);
			Assert.AreEqual(1, output3.InsertCount);

			// special case where nothing changes.
			clientRuns = new List<IClientRun>();
			clientRuns.Add(clientRun0);
			clientRuns.Add(clientRun2);
			source = new TextSource(clientRuns, MockInterpretOrc);
			var output4 = source.ClientRunChanged(1, clientRun2);
			Assert.AreEqual(1, output4.NewSource.RenderRuns.Count, "two client runs collapse to one render");
			VerifyRenderRun(output4.NewSource.RenderRuns[0], 0, part0.Length + part2.Length, "run has expected length");
			VerifyFetch(output4.NewSource, 0, output4.NewSource.Length, part0 + part2);
			Assert.AreEqual(part0.Length, output4.StartChange);
			Assert.AreEqual(0, output4.DeleteCount);
			Assert.AreEqual(0, output4.InsertCount);

		}
	}
}
