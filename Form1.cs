using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace insertintodb12
{
    public partial class Form1 : Form
    {
        private DataTable dataTable;
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
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;
            Excel.Range range = null;

            try
            {
                excelApp = new Excel.Application();
                workbook = excelApp.Workbooks.Open(filePath);
                worksheet = workbook.Sheets[1];
                range = worksheet.UsedRange;

                using (SQLiteConnection conn = new SQLiteConnection("Data Source=E:\\database\\sms.db;Version=3;"))
                {
                    conn.Open();

                    // Create table based on Excel columns
                    string createTableQuery = "CREATE TABLE IF NOT EXISTS students (";
                    for (int col = 1; col <= range.Columns.Count; col++)
                    {
                        string columnName = (range.Cells[1, col] as Excel.Range).Value2.ToString();
                        createTableQuery += $"{columnName} TEXT";
                        if (col < range.Columns.Count)
                        {
                            createTableQuery += ", ";
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
                            string insertQuery = "INSERT INTO students VALUES (";
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
                                    insertCmd.Parameters.AddWithValue($"@param{col}", (range.Cells[row, col] as Excel.Range).Value2.ToString());
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
                if (workbook != null)
                {
                    workbook.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                }
                if (excelApp != null)
                {
                    excelApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                }
            }
        }

        private void DisplayDataFromSQLite()
        {
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=E:\\database\\sms.db;Version=3;"))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM students", conn))
                {
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        dataGridView1.DataSource = dataTable;
                    }
                }
            }
        }
    }
}
