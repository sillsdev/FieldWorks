using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests special algorithm for sorting big files.
	/// </summary>
	[TestFixture]
	public class TestBigSorting
	{
		/// <summary>
		/// See if if can sort an empty list.
		/// </summary>
		[Test]
		public void EmptyList()
		{
			var output = new List<byte[]>();
			var sorter = new BigDataSorter();
			sorter.WriteResults(val => output.Add(val));
			Assert.That(output, Has.Count.EqualTo(0));
		}
		/// <summary>
		/// See if if can sort a list of one thing.
		/// </summary>
		[Test]
		public void OneItem()
		{
			var output = new List<byte[]>();
			var sorter = new BigDataSorter();
			sorter.Add("a", new byte[] {1});
			sorter.WriteResults(val => output.Add(val));
			Assert.That(output, Has.Count.EqualTo(1));
			VerifyByteArray(output[0], new byte[] {1});
		}

		private void VerifyByteArray(byte[] actual, byte[] expected)
		{
			Assert.That(actual.Length, Is.EqualTo(expected.Length));
			for (int i = 0; i < actual.Length; i++)
				Assert.That(actual[i], Is.EqualTo(expected[i]));
		}

		/// <summary>
		/// See if it can sort a short list correctly.
		/// </summary>
		[Test]
		public void TenItems()
		{
			var output = new List<byte[]>();
			var sorter = new BigDataSorter();
			var input = MakeInput(10);
			ShuffleInputs(sorter, input);
			sorter.WriteResults(val => output.Add(val));
			VerifyOutput(output, input);
		}

		/// <summary>
		/// Test with enough items to force it to save in files.
		/// </summary>
		[Test]
		public void MultiFileSort()
		{
			var output = new List<byte[]>();
			var sorter = new BigDataSorter();
			var input = MakeInput(100);
			var totalLength = input.Sum(kvp => kvp.Value.Length);
			sorter.MaxBytes = totalLength / 5; // force it to use about 5 files.
			ShuffleInputs(sorter, input);
			sorter.WriteResults(val => output.Add(val));
			VerifyOutput(output, input);
		}

		/// <summary>
		/// Insert the items in the input into the sorter in a mixed-up order.
		/// </summary>
		/// <param name="sorter"></param>
		/// <param name="input"></param>
		private void ShuffleInputs(BigDataSorter sorter, SortedDictionary<string, byte[]> input)
		{
			// Insert all the even items in oder.
			for (int i = 0; i < input.Count/2; i++)
			{
				sorter.Add(input.ElementAt(i * 2).Key, input.ElementAt(i * 2).Value);
			}
			// And all the odd items in reverse order.
			for (int i =  input.Count - 1; i > 0; i--)
			{
				if (i%2 == 1)
					sorter.Add(input.ElementAt(i).Key, input.ElementAt(i).Value);
			}
		}

		private static SortedDictionary<string, byte[]> MakeInput(int howMany)
		{
			var result = new SortedDictionary<string, byte[]>();
			for (int i = 0; i < howMany; i++)
			{
				var value = new byte[10 + i];
				for (int j = 0; j < 10 + i; j++)
					value[j] = (byte)j;
				var key = i.ToString();
				result[key] = value;
			}
			return result;
		}

		private void VerifyOutput(List<byte[]> output, SortedDictionary<string, byte[]> input)
		{
			Assert.That(output.Count, Is.EqualTo(input.Count));
			for (int i = 0; i < input.Count; i++)
				VerifyByteArray(output[i], input.ElementAt(i).Value);
		}
	}
}
