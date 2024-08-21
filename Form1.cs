using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using ExcelDataReader;

namespace Cmaeradetailstodb
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string connectionString;
        string tableName = "T01_tempdata";
        private string excelFilePath;
        private DataTable dt = new DataTable();
        List<string> Table_headers1 = new List<string>();

        private void Import_Excel(object sender, EventArgs e)
        {
            create_database();

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    excelFilePath = openFileDialog.FileName;
                    if (Validate_ExcelSheet_with_dbTable())
                    {
                        ReadAndStoreExcelData();
                        dataGridView1.DataSource = dt;
                        InsertDataIntoDb();
                    }
                    else
                    {
                        MessageBox.Show("Excel Sheet Columns do not match with our table columns");
                    }
                }
                else
                {
                    MessageBox.Show("Please select an Excel File.");
                }
            }
        }

        private void create_database()
        {
            string directoryPath = @"E:\catrat\db";
            string databaseName = "MyDatabase.sqlite";
            string databasePath = Path.Combine(directoryPath, databaseName);

            connectionString = $"Data Source={databasePath};Version=3;";
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = $"CREATE TABLE IF NOT EXISTS [{tableName}] (" +
                             "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                             "Camera_ID TEXT, Block_ID INTEGER NOT NULL, Long_D REAL NOT NULL, " +
                             "Long_M REAL NOT NULL, Long_S REAL NOT NULL, Lat_D REAL NOT NULL, Lat_M REAL NOT NULL, " +
                             "Lat_S REAL NOT NULL, Remarks TEXT, D1 TEXT NOT NULL, D2 TEXT NOT NULL, D3 TEXT NOT NULL, " +
                             "D4 TEXT NOT NULL, D5 TEXT NOT NULL)";
                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    command.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        private void ReadAndStoreExcelData()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                {
                    var configuration1 = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true // Use header row for column names
                        }
                    };

                    var dataSet = reader.AsDataSet(configuration1);

                    if (dataSet.Tables.Count > 0)
                    {
                        var sheet = dataSet.Tables[0]; // Assuming you want to work with the first table

                        // Create a new DataTable to hold the filtered data
                        var filteredTable = new DataTable();

                        // Add columns to the filtered table only if they are in Table_headers1
                        foreach (DataColumn column in sheet.Columns)
                        {
                            var headerString = column.ColumnName;
                            if (Table_headers1.Contains(headerString.ToLower()))
                            {
                                filteredTable.Columns.Add(headerString);
                            }
                        }

                        // Add rows to the filtered table
                        foreach (DataRow row in sheet.Rows)
                        {
                            var newRow = filteredTable.NewRow();
                            foreach (DataColumn column in filteredTable.Columns)
                            {
                                var headerString = column.ColumnName;
                                var originalColumnIndex = sheet.Columns.IndexOf(headerString);
                                if (originalColumnIndex >= 0)
                                {
                                    newRow[headerString] = row[originalColumnIndex];
                                }
                            }
                            filteredTable.Rows.Add(newRow);
                        }

                        dt = filteredTable; // Assign the filtered DataTable to dt
                    }
                }
            }
        }

        private void InsertDataIntoDb()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string query = $@"INSERT INTO [{tableName}]  (Camera_ID, Block_ID, Long_D, Long_M, Long_S, Lat_D, Lat_M, Lat_S, Remarks, D1, D2, D3, D4, D5) 
                                         VALUES (@Camera_ID, @Block_ID, @Long_D, @Long_M, @Long_S, @Lat_D, @Lat_M, @Lat_S, @Remarks, @D1, @D2, @D3, @D4, @D5)";

                        using (SQLiteCommand comm = new SQLiteCommand(query, conn))
                        {
                            comm.Parameters.AddWithValue("@Camera_ID", row.Cells["Camera_ID"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Block_ID", row.Cells["Block_ID"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Long_D", row.Cells["Long_D"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Long_M", row.Cells["Long_M"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Long_S", row.Cells["Long_S"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Lat_D", row.Cells["Lat_D"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Lat_M", row.Cells["Lat_M"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Lat_S", row.Cells["Lat_S"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Remarks", row.Cells["Remarks"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D1", row.Cells["D1"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D2", row.Cells["D2"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D3", row.Cells["D3"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D4", row.Cells["D4"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D5", row.Cells["D5"].Value ?? DBNull.Value);

                            comm.ExecuteNonQuery();
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private bool Validate_ExcelSheet_with_dbTable()
        {
            List<string> Excel_headers = new List<string>();

            // Store all headers of Excel to list (Excel_headers)
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(excelFilePath);
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            // Get the first row (headers)
            Excel.Range headerRange = xlRange.Rows[1];
            object[,] headerValues = headerRange.Value;

            // Convert to list
            Excel_headers.Clear();
            Excel_headers.AddRange(Enumerable.Range(1, headerValues.GetLength(1))
                .Select(i => headerValues[1, i]?.ToString().ToLower() ?? string.Empty));

            // Cleanup
            xlWorkbook.Close(false);
            xlApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorkbook);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorksheet);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlRange);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Store all headers of database table to list (Table_headers1)
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = $"PRAGMA table_info('{tableName}');";
                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        Table_headers1.Clear();
                        Table_headers1.AddRange(reader.Cast<DbDataRecord>()
                            .Select(record => record["name"].ToString().ToLower()));
                    }
                }
            }

            // Compare that Excel sheet has the same headers as the table has otherwise do not accept Excel sheet
            bool allHeadersContained = Table_headers1.All(h1 => Excel_headers.Contains(h1));
            if (allHeadersContained)
            {
                MessageBox.Show("Excel headers match database table columns.");
                return true;
            }
            MessageBox.Show("Excel headers do not match database table columns.");
            return false;


        }
    }
}
