using System;
using System.Data;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;

namespace ExcelToSqlApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx;*.xlsm",
                Title = "Select an Excel File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string excelFilePath = openFileDialog.FileName;
                DataTable dataTable = GetDataTableFromExcel(excelFilePath);

                if (dataTable != null)
                {
                    InsertDataIntoMySqlDatabase(dataTable);
                }
            }
        }

        private DataTable GetDataTableFromExcel(string path)
        {
            try
            {
                Excel.Application xlApp = new Excel.Application();
                Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(path);
                Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
                Excel.Range xlRange = xlWorksheet.UsedRange;

                DataTable dt = new DataTable();

                // Assuming the first row contains the column names
                for (int i = 1; i <= xlRange.Columns.Count; i++)
                {
                    dt.Columns.Add(xlRange.Cells[1, i].Value2.ToString());
                }

                for (int i = 2; i <= xlRange.Rows.Count; i++)
                {
                    DataRow row = dt.NewRow();
                    for (int j = 1; j <= xlRange.Columns.Count; j++)
                    {
                        row[j - 1] = xlRange.Cells[i, j].Value2?.ToString();
                    }
                    dt.Rows.Add(row);
                }

                xlWorkbook.Close();
                xlApp.Quit();

                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading Excel file: " + ex.Message);
                return null;
            }
        }

        private void InsertDataIntoMySqlDatabase(DataTable dataTable)
        {
            string connectionString = "Server=Venom;Database=Mydb;Uid=root;Pwd=1234;Port=3306;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                foreach (DataRow row in dataTable.Rows)
                {
                    string query = "INSERT INTO customers (customer_id, name, country) VALUES (@customer_id, @name, @country)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        // Assuming the columns in the Excel file are in the order: customer_id, name, country
                        cmd.Parameters.AddWithValue("@customer_id", row["customer_id"]);
                        cmd.Parameters.AddWithValue("@name", row["name"]);
                        cmd.Parameters.AddWithValue("@country", row["country"]);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MessageBox.Show("Data inserted successfully!");
        }
    }
}
