
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using ExcelDataReader;


namespace Cameradetailstodb
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
               

                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xls";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    excelFilePath = openFileDialog.FileName;
                   if(Validate_ExcelSheet_with_dbTable())
                    {
                        ReadAndStoreExcelData();
                        dataGridView1.DataSource = dt;
                       // InsertDataIntoDb();
                    }
                    else
                    {
                        MessageBox.Show("Excel Sheet Columns does not match with our table columns");
                    }
                }
                else
                {
                    MessageBox.Show("Please select an Excel File.");
                }

            }
        }

        //Create databse 
        private void create_database()
        {
            string directoryPath = @"E:\CATRAT\database";
            string databaseName = "MyDatabase.sqlite";
            string databasePath = Path.Combine(directoryPath, databaseName);

            connectionString = $"Data Source={databasePath};Version=3;";
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string sql = $"CREATE TABLE IF NOT EXISTS [{tableName}] (" +
                             "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                             "Camera_ID TEXT NOT NULL, Block_ID INTEGER NOT NULL, Long_D REAL NOT NULL, " +
                             "Long_M REAL NOT NULL, Long_S REAL NOT NULL, Lat_D REAL NOT NULL, Lat_M REAL NOT NULL, " +
                             "Lat_S REAL NOT NULL, Remarks TEXT , D1 TEXT NOT NULL, D2 TEXT NOT NULL, D3 TEXT NOT NULL, " +
                             "D4 TEXT NOT NULL, D5 TEXT NOT NULL)";

                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    command.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        //Read and store Excel data into database temporary table
        private void ReadAndStoreExcelData()
        {


            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read))
            {
                // Create an IExcelDataReader instance based on the file format
                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                {
                    // Convert the Excel data to a DataSet
                    var configuration1 = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = false
                        }
                    };

                    var dataSet = reader.AsDataSet(configuration1);
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

                    for (int i = 0; i < dataGridView1.Rows.Count-1; i++)
                    {
                        string query = $@"INSERT INTO [{tableName}]  ( Camera_ID, Block_ID, Long_D, Long_M, Long_S, Lat_D, Lat_M, Lat_S, Remarks, D1, D2, D3, D4, D5) 
                                 VALUES ( @Camera_ID, @Block_ID, @Long_D, @Long_M, @Long_S, @Lat_D, @Lat_M, @Lat_S, @Remarks, @D1, @D2, @D3, @D4, @D5)";

                        using (SQLiteCommand comm = new SQLiteCommand(query, conn))
                        {
                           
                            comm.Parameters.AddWithValue("@Camera_ID", dataGridView1.Rows[i].Cells["Camera_ID"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Block_ID", dataGridView1.Rows[i].Cells["Block_ID"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Long_D", dataGridView1.Rows[i].Cells["Long_D"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Long_M", dataGridView1.Rows[i].Cells["Long_M"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Long_S", dataGridView1.Rows[i].Cells["Long_S"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Lat_D", dataGridView1.Rows[i].Cells["Lat_D"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Lat_M", dataGridView1.Rows[i].Cells["Lat_M"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Lat_S", dataGridView1.Rows[i].Cells["Lat_S"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@Remarks", dataGridView1.Rows[i].Cells["Remarks"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D1", dataGridView1.Rows[i].Cells["D1"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D2", dataGridView1.Rows[i].Cells["D2"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D3", dataGridView1.Rows[i].Cells["D3"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D4", dataGridView1.Rows[i].Cells["D4"].Value ?? DBNull.Value);
                            comm.Parameters.AddWithValue("@D5", dataGridView1.Rows[i].Cells["D5"].Value ?? DBNull.Value);

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
            

            
            //store all headers of excel to list (Excel_headers)
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


            //store all headers of database table to list (Table_headers1)

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


            //comapre that excel sheet have same headers as the table have otherwise do not accept excel sheet
            bool allHeadersContained = Table_headers1.All(h1 => Excel_headers.Contains(h1));
            if (allHeadersContained)
            {
                return true;
            }
            return false;
           

        }



       
     
    }


}
