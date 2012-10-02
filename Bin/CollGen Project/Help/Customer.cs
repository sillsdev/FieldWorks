	using System;

namespace YourSpace {
	public class Customer {

		int _id;
		string _name;
		string _address;
		string _phone;
		DateTime _birthDate;

		public Customer(int id, string name, string address,
					string phone, DateTime birthDate) {
			_id = id;
			_name = name;
			_address = address;
			_phone = phone;
			_birthDate = birthDate;
		}

		public int Identification {
			get {
				return _id;
			}

			set {
				_id = value;
			}
		}

		public string Name {
			get {
				return _name;
			}

			set {
				_name = value;
			}
		}

		public string Address {
			get {
				return _address;
			}

			set {
				_address = value;
			}
		}

		public string Phone {
			get {
				return _phone;
			}

			set {
				_phone = value;
			}
		}

		public DateTime BirthDate {
			get {
				return _birthDate;
			}

			set {
				_birthDate = value;
			}
		}

		public TimeSpan GetAge() {
			return DateTime.Now.Subtract(_birthDate);
		}
	}
}