private void Update_Data(object sender, EventArgs e)
{
    // Update logic
    using (conn)
    {
        conn.Open();

        foreach (var cameraId in set)
        {
            for (int row = 0; row < dataGridView1.Rows.Count - 1; row++)
            {
                var cameraIdCellValue = dataGridView1.Rows[row].Cells["camera_id"].Value;

                // Update based on camera_id
                if (cameraIdCellValue != null && int.TryParse(cameraIdCellValue.ToString(), out int rowCameraId) && rowCameraId == cameraId)
                {
                    string updateQuery = $"UPDATE [{tableName}] SET ";

                    for (int col = 0; col < dataGridView1.Columns.Count; col++)
                    {
                        string columnName = dataGridView1.Columns[col].HeaderText;

                        // Ensure to skip camera_id column if needed
                        if (columnName == "camera_id") continue;

                        updateQuery += $"{columnName}=@param{col}";

                        if (col < dataGridView1.Columns.Count - 1)
                        {
                            updateQuery += ",";
                        }
                    }

                    updateQuery += " WHERE camera_id=@camera_id";

                    using (SqlCommand updatecmd = new SqlCommand(updateQuery, conn))
                    {
                        for (int col = 0; col < dataGridView1.Columns.Count; col++)
                        {
                            if (dataGridView1.Columns[col].HeaderText == "camera_id") continue;
                            var cellValue = dataGridView1.Rows[row].Cells[col].Value;
                            updatecmd.Parameters.AddWithValue($"@param{col}", cellValue ?? DBNull.Value);
                        }
                        updatecmd.Parameters.AddWithValue("@camera_id", cameraId);
                        updatecmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Update successful");
                    break;
                }
            }
        }
    }
}


using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SqlClient;
using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
using Excel = Microsoft.Office.Interop.Excel;
using ExcelDataReader;

namespace Exceldatatodb
{
    public partial class Form1 : Form
    {
        private string excelFilePath;
        string tableName = "db_1";
        private string db = "Data Source=VENOM\\SQLEXPRESS;Initial Catalog=Studentdb;Integrated Security=True;Encrypt=False";

        public static DataTable dt = new DataTable();
        HashSet<int> set = new HashSet<int>();

        public Form1()
        {
            InitializeComponent();
        }

        //import the excel file

        private void Import_Excel(object sender, EventArgs e)
        {
           // ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
          
                  using (var openFileDialog1 = new OpenFileDialog { Filter = "Excel Workbook|*.xls;*.xlsx;*.xlsm", ValidateNames = true })
                    {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    using (var fs = File.Open(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                    {
                        var reader = ExcelReaderFactory.CreateReader(fs);
                        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true // Use first row is ColumnName here :D
                            }
                        });
                        if (dataSet.Tables.Count > 0)
                        {
                            var dtData = dataSet.Tables[0];
                            // Do Something
                        }
                    }
                }
              }
           
              
            

        }

      
    


        private void ReadAndInsertExcelData()
        {
            Excel.Application excelApp = new Excel.Application();
            Excel.Workbook workbook = excelApp.Workbooks.Open(excelFilePath);
            Excel.Worksheet worksheet = workbook.Sheets[1];
            Excel.Range range = worksheet.UsedRange;
            string tableName = worksheet.Name;

            try
            {
                using (SqlConnection conn = new SqlConnection(db))
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
                            createTableQuery += $"{columnName} TEXT";
                        }

                        if (col < range.Columns.Count)
                        {
                            createTableQuery += ", ";
                        }
                    }
                    createTableQuery += ")";

                    using (SqlCommand createTableCmd = new SqlCommand(createTableQuery, conn))
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

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                for (int col = 1; col <= range.Columns.Count; col++)
                                {
                                    var cellValue = (range.Cells[row, col] as Excel.Range).Value2;
                                    if (col == 1) // Assuming first column is the primary key and integer
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

        
    




    private void bulkcopy()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(db))
                {
                    conn.Open();
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.DestinationTableName = tableName;
                        bulkCopy.WriteToServer(dt);
                    }
                    MessageBox.Show("Successfully Inserted");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bulk copy error: {ex.Message}");
            }
        }


        //edit Mode
        private void Edit_Row(object sender, EventArgs e)
        {

            getid();
        }

        private void getid()
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                string id = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells[0].Value.ToString();
                if (int.TryParse(id, out int number))
                {
                    set.Add(number);

                }

            }
        }




        //Update the current data

        private void Update_Data(object sender, EventArgs e)
        {
            //updation
            using (SqlConnection conn = new SqlConnection(db))
            {
                conn.Open();


                foreach (var id in set)
                {

                    for (int row = 0; row < dataGridView1.Rows.Count - 1; row++)
                    {
                        var id1 = dataGridView1.Rows[row].Cells[0].Value;
                        //Update
                        if (id1 != null && int.TryParse(id1.ToString(), out int rowId) && rowId == id)
                        {
                            string updateQuery = $"UPDATE [{tableName}] SET ";
                            for (int col = 0; col < dataGridView1.Columns.Count; col++)
                            {

                                string columnName = dataGridView1.Columns[col].HeaderText;
                                updateQuery += $"{columnName}=@param{col}";
                                if (col < dataGridView1.Columns.Count - 1)
                                {
                                    updateQuery += ",";
                                }
                            }

                            updateQuery += $" WHERE ID=@id";

                            using (SqlCommand updatecmd = new SqlCommand(updateQuery, conn))
                            {
                                for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                {
                                    if (dataGridView1.Columns[col].HeaderText == "ID") continue;
                                    var cellValue = dataGridView1.Rows[row].Cells[col].Value;
                                    updatecmd.Parameters.AddWithValue($"@param{col}", cellValue ?? DBNull.Value);
                                }
                                updatecmd.Parameters.AddWithValue("@id", id);
                                updatecmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("update successfully");
                            break;
                        }

                    }

                }

            }

            List<string> existingIds = new List<string>();
            using (SqlConnection conn = new SqlConnection(db))
            {
                conn.Open();


                string selectQuery = $"SELECT ID FROM {tableName}";
                using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                {
                    using (SqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingIds.Add(reader["ID"].ToString());
                        }
                    }
                }


                // Step 3: Insert new records
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;

                    string idValue = row.Cells["ID"].Value.ToString();

                    if (!existingIds.Contains(idValue))
                    {
                        string insertQuery = $"INSERT INTO {tableName} (";

                        // Columns
                        for (int col = 0; col < dataGridView1.Columns.Count; col++)
                        {
                            string columnName = dataGridView1.Columns[col].HeaderText;
                            insertQuery += $"{columnName}";
                            if (col < dataGridView1.Columns.Count - 1)
                            {
                                insertQuery += ", ";
                            }
                        }

                        insertQuery += ") VALUES (";

                        // Parameters
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
                                var cellValue = row.Cells[col].Value;
                                insertCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? (object)DBNull.Value);
                            }

                            insertCmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Data updated successfully");
                        break;
                    }
                }
            }


        }





        //finalize the  into data

        private void Finalize_btn(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(db))
            {
                try
                {
                    conn.Open();
                    string createTableQuery = $"SELECT * INTO finalize FROM [{tableName}] WHERE 1 = 0";
                    using (SqlCommand createTableCmd = new SqlCommand(createTableQuery, conn))
                    {
                        createTableCmd.ExecuteNonQuery();
                    }


                    string insertDataQuery = $"INSERT INTO finalize SELECT * FROM [{tableName}]";
                    using (SqlCommand insertCmd = new SqlCommand(insertDataQuery, conn))
                    {
                        insertCmd.ExecuteNonQuery();
                    }

                    string TruncateQuery = $"Truncate table [{tableName}] ";
                    using (SqlCommand TruncateCmd = new SqlCommand(TruncateQuery, conn))
                    {
                        TruncateCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Finalized Successfully");
                    showupdateddata(tableName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }

            }
        }








        //Show Updated database
        private void Show_Updated(object sender, EventArgs e)
        {
            showupdateddata(tableName);
        }

        private void Showfinalized(object sender, EventArgs e)
        {
            string tablename = "finalize";
            showupdateddata(tablename);
        }




        public void showupdateddata(string tablename)
        {
            string connectionString = db;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    // Query to select all data from the first table
                    string selectQuery = $"SELECT * FROM [{tablename}]";
                    using (SqlDataAdapter da = new SqlDataAdapter(selectQuery, conn))
                    {
                        SqlCommandBuilder commandBuilder = new SqlCommandBuilder(da);
                        DataTable dataTable = new DataTable();
                        da.Fill(dataTable);
                        dataGridView1.DataSource = dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }



    }

}







