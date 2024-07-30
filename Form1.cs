using Excel = Microsoft.Office.Interop.Excel;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Exceldata
{
    public partial class Form1 : Form
    {
        private System.Data.DataTable dataTable;
        private string excelFilePath;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Open the Excel file
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    excelFilePath = openFileDialog.FileName;
                    ReadAndInsertExcelData(excelFilePath);
                }
                else
                {
                    MessageBox.Show("Please select an Excel file.");
                    return;
                }
            }

            // Display data from SQL Server database
            DisplayDataFromSQLServer();
        }

        private void ReadAndInsertExcelData(string filePath)
        {
            Excel.Application excelApp = new Excel.Application();
            Excel.Workbook workbook = excelApp.Workbooks.Open(filePath);
            Excel.Worksheet worksheet = workbook.Sheets[1];
            Excel.Range range = worksheet.UsedRange;
            string tableName = worksheet.Name;

            try
            {
                using (SqlConnection conn = new SqlConnection("Data Source=VENOM\\SQLEXPRESS;Initial Catalog=Studentdb;Integrated Security=True;Encrypt=False"))
                {
                    conn.Open();

                    // Check if the table exists, if not create it
                    string checkTableQuery = $"IF OBJECT_ID(N'{tableName}', 'U') IS NULL " +
                                             "BEGIN " +
                                             $"CREATE TABLE [{tableName}] (";

                    for (int col = 1; col <= range.Columns.Count; col++)
                    {
                        string columnName = (range.Cells[1, col] as Excel.Range).Value2.ToString().Replace(" ", "_");

                        if (col == 1)
                        {
                            // Assuming the first column is the primary key and it is an integer
                            checkTableQuery += $"{columnName} INTEGER PRIMARY KEY";
                        }
                        else
                        {
                            checkTableQuery += $", {columnName} TEXT";
                        }
                    }
                    checkTableQuery += ") END";

                    using (SqlCommand createTableCmd = new SqlCommand(checkTableQuery, conn))
                    {
                        createTableCmd.ExecuteNonQuery();
                    }

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        for (int row = 2; row <= range.Rows.Count; row++)
                        {
                            string insertQuery = $"INSERT INTO [{tableName}] VALUES (";
                            for (int col = 1; col <= range.Columns.Count; col++)
                            {
                                insertQuery += $"@param{col}";
                                if (col < range.Columns.Count)
                                {
                                    insertQuery += ", ";
                                }
                            }
                            insertQuery += ")";

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn, transaction))
                            {
                                for (int col = 1; col <= range.Columns.Count; col++)
                                {
                                    var cellValue = (range.Cells[row, col] as Excel.Range).Value2;
                                    if (cellValue == null)
                                    {
                                        insertCmd.Parameters.AddWithValue($"@param{col}", DBNull.Value);
                                    }
                                    else if (col == 1) // Assuming first column is the primary key and integer
                                    {
                                        insertCmd.Parameters.AddWithValue($"@param{col}", Convert.ToInt32(cellValue));
                                    }
                                    else
                                    {
                                        insertCmd.Parameters.AddWithValue($"@param{col}", cellValue.ToString());
                                    }
                                }
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }

                    MessageBox.Show("Data imported successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                // Clean up
                workbook.Close(false);
                excelApp.Quit();
            }
        }

        private void DisplayDataFromSQLServer()
        {
            using (SqlConnection conn = new SqlConnection("Data Source=VENOM\\SQLEXPRESS;Initial Catalog=Studentdb;Integrated Security=True;Encrypt=False"))
            {
                conn.Open();

                // Query to get the first user table
                string getTablesQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

                using (SqlCommand cmd = new SqlCommand(getTablesQuery, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            using (SqlCommand selectCmd = new SqlCommand($"SELECT * FROM [{tableName}]", conn))
                            {
                                using (SqlDataAdapter adapter = new SqlDataAdapter(selectCmd))
                                {
                                    System.Data.DataTable dataTable = new System.Data.DataTable();
                                    adapter.Fill(dataTable);
                                    dataGridView1.DataSource = dataTable;
                                }
                            }
                        }
                    }
                }
            }
        }
        private void btn2(object sender, EventArgs e)
        {

        }

        private void btn2_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection("Data Source=VENOM\\SQLEXPRESS;Initial Catalog=Studentdb;Integrated Security=True;Encrypt=False"))
            {
                conn.Open();

                // Create table based on DataGridView columns
                string createTableQuery = "IF OBJECT_ID(N'dummy', 'U') IS NULL " +
                                          "BEGIN " +
                                          "CREATE TABLE dummy (";
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    string columnName = dataGridView1.Columns[i].HeaderText.Replace(" ", "_");

                    createTableQuery += $"{columnName} TEXT";

                    if (i < dataGridView1.Columns.Count - 1)
                    {
                        createTableQuery += ", ";
                    }
                }
                createTableQuery += ") END";

                using (SqlCommand createTableCmd = new SqlCommand(createTableQuery, conn))
                {
                    createTableCmd.ExecuteNonQuery();
                }

                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    using (SqlCommand comm = new SqlCommand())
                    {
                        comm.Connection = conn;
                        comm.Transaction = transaction;

                        try
                        {
                            for (int row = 0; row < dataGridView1.Rows.Count - 1; row++) // Exclude the new row placeholder
                            {
                                string insertQuery = "INSERT INTO dummy VALUES (";
                                for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                {
                                    insertQuery += $"@param{col}";
                                    if (col < dataGridView1.Columns.Count - 1)
                                    {
                                        insertQuery += ", ";
                                    }
                                }
                                insertQuery += ")";

                                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                                {
                                    for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                    {
                                        var cellValue = dataGridView1.Rows[row].Cells[col].Value;
                                        if (cellValue == null)
                                        {
                                            insertCmd.Parameters.AddWithValue($"@param{col}", DBNull.Value);
                                        }
                                        else
                                        {
                                            insertCmd.Parameters.AddWithValue($"@param{col}", cellValue);
                                        }
                                    }
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Error: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
