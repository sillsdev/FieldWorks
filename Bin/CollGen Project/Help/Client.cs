using System;
using YourSpace;

namespace MySpace {
	class client {
		public static void Main() {

			CustomerCollection cc = new CustomerCollection();

			cc.Add(new Customer(98302, "Bob Harroway", "1 NonExistent Place, Chicago", "999-999-9999",
					new DateTime(1943, 11, 29)));
			cc.Add(new Customer(12948, "Sarah Fawkes", "3 NonExistent Place, Chicago", "999-999-9992",
					new DateTime(1967, 2, 3)));
			cc.Add(new Customer(38291, "Joseph Collins", "11 NonExistent Place, Chicago", "999-999-9991",
					new DateTime(1979, 8, 15)));
			cc.Add(new Customer(13849, "Michelle Dover", "16 NonExistent Place, Chicago", "999-999-9985",
					new DateTime(1952, 12, 27)));
			cc.Add(new Customer(13849, "Abigail Rivers", "22 NonExistent Place, Chicago", "999-999-9921",
					new DateTime(1960, 4, 6)));

			foreach (Customer c in cc) {

				Console.WriteLine("\r\n------------------");
				Console.WriteLine("{0,-20}{1}", "Customer Name:", c.Name);
				Console.WriteLine("{0,-20}{1}", "Customer ID:", c.Identification);
				Console.WriteLine("{0,-20}{1}", "Customer Address:", c.Address);
				Console.WriteLine("{0,-20}{1}", "Customer Phone:", c.Phone);
				Console.WriteLine("{0,-20}{1}", "Customer Birthday:", c.BirthDate);
				DateTime dt = new DateTime(c.GetAge().Ticks);
				Console.WriteLine("{0,-20}{1}", "Customer Age:",
						dt.Year + " years, " + dt.Day + " days");
			}
		}
	}
}