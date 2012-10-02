using System.Data;
using System.Data.SqlClient;

namespace NMockSample.Order
{
	/// <summary>
	/// Details of Order stored directly in database.
	/// </summary>
	public class Order
	{
		private DataRow row;

		public virtual int Number
		{
			get { return (int)row["number"]; }
		}

		public virtual double Amount
		{
			get { return (double)row["amount"]; }
		}

		public virtual bool Urgent
		{
			get { return (bool)row["urgent"]; }
		}

		public virtual string User
		{
			get { return (string)row["user"]; }
		}

		public virtual void Load(SqlConnection con, int id)
		{
			string sql = "SELECT * FROM orders WHERE id = " + id;
			using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
			{
				DataTable table = new DataTable();
				adapter.Fill(table);
				row = table.Rows[0];
			}
		}
	}
}
