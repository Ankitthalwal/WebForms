using Excel = Microsoft.Office.Interop.Excel;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Reflection.Emit;
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

            // Display data from SQLite database
            DisplayDataFromSQLite();
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
                using (SQLiteConnection conn = new SQLiteConnection("Data Source=E:\\database\\sms.db;Version=3;"))
                {
                    conn.Open();

                    // Create table based on Excel columns
                    string createTableQuery = $"CREATE TABLE IF NOT EXISTS [{tableName}] (";
                    for (int col = 1; col <= range.Columns.Count; col++)
                    {
                        string columnName = (range.Cells[1, col] as Excel.Range).Value2.ToString().Replace(" ", "_");

                        if (col == 1)
                        {
                            // Assuming the first column is the primary key and it is an integer
                            createTableQuery += $"{columnName} INTEGER PRIMARY KEY";
                        }
                        else
                        {
                            createTableQuery += $", {columnName} TEXT";
                        }
                    }
                    createTableQuery += ")";

                    using (SQLiteCommand createTableCmd = new SQLiteCommand(createTableQuery, conn))
                    {
                        createTableCmd.ExecuteNonQuery();
                    }

                    using (SQLiteTransaction transaction = conn.BeginTransaction())
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

                            using (SQLiteCommand insertCmd = new SQLiteCommand(insertQuery, conn))
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

        private void DisplayDataFromSQLite()
        {
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=E:\\database\\sms.db;Version=3;"))
            {
                conn.Open();

                // You may need to adjust this query to dynamically select the table
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table';", conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            using (SQLiteCommand selectCmd = new SQLiteCommand($"SELECT * FROM [{tableName}]", conn))
                            {
                                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(selectCmd))
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


        //update the data of in the current database 
        private void btn2(object sender,EventArgs e)
        {

        }
        private void btn2_Click(object sender, EventArgs e)
        {
            
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=E:\\database\\sms.db;Version=3;"))
            {
                conn.Open();

                // Create table based on DataGridView columns
                string createTableQuery = "CREATE TABLE IF NOT EXISTS dummy (";
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    string columnName = dataGridView1.Columns[i].HeaderText.Replace(" ", "_");

                    createTableQuery += $"{columnName} TEXT";

                    if (i < dataGridView1.Columns.Count - 1)
                    {
                        createTableQuery += ", ";
                    }
                }
                createTableQuery += ");";



            

                using (SQLiteCommand createTableCmd = new SQLiteCommand(createTableQuery, conn))
                {
                    createTableCmd.ExecuteNonQuery();
                }

                using (SQLiteTransaction transaction = conn.BeginTransaction())
                {
                    using (SQLiteCommand comm = new SQLiteCommand())
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

                                using (SQLiteCommand insertCmd = new SQLiteCommand(insertQuery, conn))
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
