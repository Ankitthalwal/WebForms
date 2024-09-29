using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ExcelDataReader;

namespace Cmaeradetailstodb
{
 
        public partial class Form1 : Form
        {
            string connectionString;
            string tableName = "T01_tempdata";
            private string excelFilePath;
           private string folderFilePath;
            private DataTable ExcelData = new DataTable();
            List<string> Table_headers1 = new List<string>();
           bool WorkingDirectory_Set = false;
    
          
            SQLiteConnection conn;

            public Form1()
            {
                InitializeComponent();       
            }

        

        private void Select_Dir(object sender, EventArgs e)
        {

           
                using (FolderBrowserDialog openFolderDialog = new FolderBrowserDialog())
                {
                    {

                        var result = openFolderDialog.ShowDialog();
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFolderDialog.SelectedPath))
                    {
                        string folderFilePath = openFolderDialog.SelectedPath;
                        string databaseName = "MyDatabase.sqlite";

                        //create database file
                        string databasePath = Path.Combine(folderFilePath, databaseName);

                        //store connection string
                         connectionString = $"Data Source={databasePath};Version=3;";
                         conn = new SQLiteConnection(connectionString);
                        
                        WorkingDirectory_Set = true;

                        MessageBox.Show("Working Directory has been set");


                        if (!File.Exists(databasePath))
                        {
                            SQLiteConnection.CreateFile(databasePath);
                        }

                    }
                    else
                    {
                        MessageBox.Show("Database creation failed");
                    }
                }

                
            }

                
        }


        private void Import_Excel(object sender, EventArgs e)
            {


            using (conn)
            {
                if (WorkingDirectory_Set)
                {
                    conn.Open();
                    create_Db_Table();
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            excelFilePath = openFileDialog.FileName;
                            if (Validate_ExcelSheet_with_dbTable())
                            {
                                {
                                    ReadAndStoreExcelData();
                                    dataGridView1.DataSource = ExcelData;

                                }
                            }
                           
                        }
                        else
                        {
                            MessageBox.Show("Please select an Excel File.");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Choose the Working Directory First");
                    return;
                }

                    conn.Close();
                }

            }

            private void create_Db_Table()
            {

          
                string sql = $"CREATE TABLE IF NOT EXISTS [{tableName}] (" +
                             "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                             "Camera_ID TEXT, Block_ID INTEGER NOT NULL, Long_D REAL NOT NULL, " +
                             "Long_M REAL NOT NULL, Long_S REAL NOT NULL, Lat_D REAL NOT NULL, lat_M REAL NOT NULL, " +
                             "Lat_S REAL NOT NULL, Remarks TEXT, D1 TEXT NOT NULL, D2 TEXT NOT NULL, D3 TEXT NOT NULL, " +
                             "D4 TEXT NOT NULL, D5 TEXT NOT NULL)";
                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    command.ExecuteNonQuery();
                }
               

            
               
            }

            private void ReadAndStoreExcelData()
            {
              DataTable dt = new DataTable();

            using (var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                    var configuration1 = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true
                        }
                    };

                    var dataTable = new DataTable();
                  //  var columnIndexMap = new System.Collections.Generic.List<int>();

                    // Read the first row to get headers
                    if (reader.Read()) // Read the first row
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var header = reader.GetString(i);
                            if (!string.IsNullOrWhiteSpace(header))
                            {
                                dataTable.Columns.Add(header); // Add the column with a valid header
                              //  columnIndexMap.Add(i); // Store the index of the valid column
                            }
                        }
                    }

                    for(int i = 0;i< dataTable.Columns.Count; i++)
                    {
                        MessageBox.Show(dataTable.Columns.Count.ToString());
                    }

                    // Read the remaining rows
                    //while (reader.Read())
                    //{
                    //    var newRow = dataTable.NewRow();
                    //    foreach (var index in columnIndexMap)
                    //    {
                    //        newRow[dataTable.Columns[index].ColumnName] = reader.GetValue(index) ?? DBNull.Value; // Insert DBNull for null values
                    //    }
                    //    dataTable.Rows.Add(newRow);
                    //}


                    // MessageBox.Show(dt.Columns.Count.ToString());



                    var dataSet = reader.AsDataSet(configuration1);
                       
                       

                        if (dataSet.Tables.Count > 0)
                        {
                            var sheet = dataSet.Tables[0];
                        MessageBox.Show(sheet.Columns.Count.ToString());

                            // Add columns to the filtered table only if they are in Table_headers1
                            foreach (DataColumn column in sheet.Columns)
                            {
                                var headerString = column.ColumnName;
                                if (Table_headers1.Contains(headerString.ToLower()))
                                {
                                    ExcelData.Columns.Add(headerString);
                                }
                            }
                            // Add rows to the filtered table


                            using (var transaction = conn.BeginTransaction())
                            {
                                foreach (DataRow row in sheet.Rows)
                                {
                                    var newRow = ExcelData.NewRow();
                                    foreach (DataColumn column in ExcelData.Columns)
                                    {
                                        var headerString = column.ColumnName;
                                        var originalColumnIndex = sheet.Columns.IndexOf(headerString);
                                        if (originalColumnIndex >= 0)
                                        {
                                            newRow[headerString] = row[originalColumnIndex];
                                        }
                                    }
                                    ExcelData.Rows.Add(newRow);

                                //Insert data into database
                                    string query = $@"INSERT INTO [{tableName}] (Camera_ID, Block_ID, Long_D, Long_M, Long_S, Lat_D, Lat_M, Lat_S, Remarks, D1, D2, D3, D4, D5) 
                                VALUES (@Camera_ID, @Block_ID, @Long_D, @Long_M, @Long_S, @Lat_D, @Lat_M, @Lat_S, @Remarks, @D1, @D2, @D3, @D4, @D5)";

                                    using (SQLiteCommand comm = new SQLiteCommand(query, conn))
                                    {
                                        comm.Parameters.AddWithValue("@Camera_ID", newRow["Camera_ID"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Block_ID", newRow["Block_ID"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Long_D", newRow["Long_D"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Long_M", newRow["Long_M"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Long_S", newRow["Long_S"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Lat_D", newRow["Lat_D"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Lat_M", newRow["Lat_M"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Lat_S", newRow["Lat_S"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@Remarks", newRow["Remarks"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@D1", newRow["D1"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@D2", newRow["D2"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@D3", newRow["D3"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@D4", newRow["D4"] ?? DBNull.Value);
                                        comm.Parameters.AddWithValue("@D5", newRow["D5"] ?? DBNull.Value);
                                        comm.ExecuteNonQuery();
                                    }
                                }
                                transaction.Commit();
                            }
                        }
                        reader.Close();
                    }
                    stream.Close();
                }
            }
            private bool Validate_ExcelSheet_with_dbTable()
            {
                List<string> Excel_headers = new List<string>();
                //add excel headers into the  Excel_headers list

                using (var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {


                    var configuration1 = new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true
                            }
                        };

                    DataTable dt = new DataTable();
                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var header = reader.GetString(i);
                            if (!string.IsNullOrWhiteSpace(header))
                            {

                                Excel_headers.Add(header.ToLower());
                            }
                        }
                    }


                        //var dataSet = reader.AsDataSet(configuration1);

                        //if (dataSet.Tables.Count > 0)
                        //{
                        //    var sheet = dataSet.Tables[0]; //Take the first sheet



                        //    // Add columns to the filtered table only if they are in Table_headers1
                        //    foreach (DataColumn column in sheet.Columns)
                        //    {
                        //        var headerString = column.ColumnName.ToString().ToLower();
                        //        Excel_headers.Add(headerString);
                        //    }
                        //}
                    }
                }







                // Store all headers of database table to list (Table_headers1)

                string sql = $"PRAGMA table_info('{tableName}');";
                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        Table_headers1.Clear();
                        Table_headers1.AddRange(reader.Cast<DbDataRecord>()
                            .Select(record => record["name"].ToString().ToLower()));
                    }

                }

                // Compare that Excel sheet has the same headers as the table has otherwise do not accept Excel sheet
                bool allHeadersContained = Table_headers1.All(h1 => Excel_headers.Contains(h1));
                if (allHeadersContained)
                {
                    MessageBox.Show("Excel headers match database table columns.");
                    return true;
                }
                else
                {
                    MessageBox.Show("Excel headers do not match database table columns.");
                    return false;
                }
            }

       
    }
}
