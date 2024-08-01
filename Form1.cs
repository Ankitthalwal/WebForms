using System;
using System.Data;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data.SqlClient;
using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
namespace Exceldatatodb
{
    public partial class Form1 : Form
    {
        private string excelFilePath;
        string tableName;
       private int newimport = 0;
        private string db = "Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True";

        public Form1()
        {
            InitializeComponent();
        }

        //import the excel File

        private void import_excel(object sender, EventArgs e)
        {
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
                    MessageBox.Show("Please select an Excel File.");
                    return;
                }
            }
            DisplayData();
        }




        //save the excel file data into database
        private void ReadAndInsertExcelData(string filePath)
        {
            Excel.Application excelApp = new Excel.Application();
            Excel.Workbook workbook = excelApp.Workbooks.Open(filePath);
            Excel.Worksheet worksheet = workbook.Sheets[1];
            Excel.Range range = worksheet.UsedRange;
            tableName = worksheet.Name;
            if (newimport==0)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(db))
                    {
                        conn.Open();

                        // Check if the table exists, if not create it
                        string checkTableQuery = $"CREATE TABLE [{tableName}] (";

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
                        checkTableQuery += ")";

                        using (SqlCommand createTableCmd = new SqlCommand(checkTableQuery, conn))
                        {
                            createTableCmd.ExecuteNonQuery();
                        }

                       
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
                        }

                        MessageBox.Show("Data imported successfully!");
                        newimport = 1;
                    
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

        }






        //Display excel data from excel to Datagridview

        private void DisplayData()
        {
            if (string.IsNullOrEmpty(excelFilePath))
            {
                MessageBox.Show("No Excel file has been selected.");
                return;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(excelFilePath);

                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; 
                    DataTable dataTable = new DataTable();

                  
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        dataTable.Columns.Add(worksheet.Cells[1, col].Text);
                    }

                    // Add rows
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        DataRow dataRow = dataTable.NewRow();
                        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Text;
                        }
                        dataTable.Rows.Add(dataRow);
                    }

                    dataGridView1.DataSource = dataTable;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }

        }





        //Update the current data

        private void Update_Data(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.EndEdit();

                // Step 1: Fetch existing IDs from the database
                List<string> existingIds = new List<string>();
                using (SqlConnection conn = new SqlConnection(db))
                {
                    conn.Open();

                    // Retrieve existing IDs
                    string selectQuery = $"SELECT ID FROM [{tableName}]";
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

                    // Step 2: Update existing records
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string idValue = row.Cells["ID"].Value.ToString();

                        if (existingIds.Contains(idValue))
                        {
                            string updateQuery = $"UPDATE {tableName} SET ";

                            for (int col = 1; col < dataGridView1.Columns.Count; col++)
                            {
                                string columnName = dataGridView1.Columns[col].HeaderText;
                                updateQuery += $"{columnName}=@param{col}";
                                if (col < dataGridView1.Columns.Count - 1)
                                {
                                    updateQuery += ", ";
                                }
                            }

                            updateQuery += $" WHERE ID = @id";

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                for (int col = 1; col < dataGridView1.Columns.Count; col++)
                                {
                                    var cellValue = row.Cells[col].Value;
                                    updateCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? (object)DBNull.Value);
                                }

                                updateCmd.Parameters.AddWithValue("@id", idValue);
                                updateCmd.ExecuteNonQuery();
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
                        }
                    }
                }

                MessageBox.Show("Data updated successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }




        //finalize the data of datagridview into database

        private void Finalize_btn(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(db))
            {
                conn.Open();

                string createTableQuery = "CREATE TABLE dummy (";
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    string columnName = dataGridView1.Columns[i].HeaderText.Replace(" ", "_");
                    createTableQuery += $"{columnName} TEXT";
                    if (i < dataGridView1.Columns.Count - 1)
                    {
                        createTableQuery += ", ";
                    }
                }
                createTableQuery += ")";
                //first delete the base table content


                using (SqlCommand createTableCmd = new SqlCommand(createTableQuery, conn))
                {
                    string TruncateTableQuery = $"TRUNCATE TABLE [{tableName}]";
                    using (SqlCommand TruncateCmd = new SqlCommand(TruncateTableQuery, conn))
                    {
                        TruncateCmd.ExecuteNonQuery();
                        createTableCmd.ExecuteNonQuery();
                    }
                }

                try
                {
                    for (int row = 0; row < dataGridView1.Rows.Count - 1; row++)
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
                                insertCmd.Parameters.AddWithValue($"@param{col}", cellValue?.ToString() ?? string.Empty);
                            }
                            insertCmd.ExecuteNonQuery();
                        }
                    }


                    MessageBox.Show("Finalize Successfully");
                 
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Error: {ex.Message}");
                }
            }

        }


        private  async void Show_Updated(object sender, EventArgs e)
        {
            string connectionString = "Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string getTablesQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                    string tableName = null;

                    using (SqlCommand cmd = new SqlCommand(getTablesQuery, conn))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                tableName = reader.GetString(0);
                            }
                        }
                    }

                    if (tableName != null)
                    {
                        // Query to select all data from the first table
                        string selectQuery = $"SELECT * FROM [{tableName}]";
                        using (SqlDataAdapter da = new SqlDataAdapter(selectQuery, conn))
                        {
                            SqlCommandBuilder commandBuilder = new SqlCommandBuilder(da);
                            DataTable dataTable = new DataTable();
                            da.Fill(dataTable);
                            dataGridView1.DataSource = dataTable;
                        }
                    }
                  
                    else
                    {
                        MessageBox.Show("No tables found in the database.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }



        private void Clearscreen_btn(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;

        }

        
    }
}
