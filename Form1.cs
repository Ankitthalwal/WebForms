using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SqlClient;
using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
namespace Exceldatatodb
{
    public partial class Form1 : Form
    {
        private string excelFilePath;
        string tableName = "temporary";
        private string db = "Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True";
        bool  new_importedfile=false;

        public Form1()
        {
            InitializeComponent();
        }

        //import the excel file

        private void Import_Excel(object sender, EventArgs e)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xls";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    excelFilePath = openFileDialog.FileName;
                    DisplayData();
                    if (!new_importedfile)
                    {
                        ReadAndInsertExcelData1();
                        new_importedfile = true;
                    }
                }
                else
                {
                    MessageBox.Show("Please select an Excel File.");
                    return;
                }
            }

        }



        //display excel data
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
                    MessageBox.Show("Imported successfully");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }

        }




        //save data into db using Officeopenxml
        private void ReadAndInsertExcelData1()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(db))
                {
                    conn.Open();
                    string CreateTableQuery = $"CREATE TABLE [{tableName}] (";
                    for (int col = 0; col < dataGridView1.Columns.Count; col++)
                    {
                        string columnName = dataGridView1.Columns[col].HeaderText;
                        if (col == 0)
                        {
                            CreateTableQuery += $"{columnName} INT PRIMARY KEY";
                        }
                        else
                        {
                            CreateTableQuery += $", {columnName} TEXT";
                        }
                    }
                    CreateTableQuery += ")";

                    using (SqlCommand createTableCmd = new SqlCommand(CreateTableQuery, conn))
                    {
                        createTableCmd.ExecuteNonQuery();
                    }

                    for (int row = 0; row < dataGridView1.Rows.Count; row++)
                    {
                        if (dataGridView1.Rows[row].IsNewRow) continue;

                        string insertQuery = $"INSERT INTO [{tableName}] VALUES (";
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
                                    insertCmd.Parameters.AddWithValue($"@param{col}", cellValue.ToString());
                                }
                            }
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
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
                    showupdateddata();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }

            }
        }








        //Show Updated database
        private  void Show_Updated(object sender, EventArgs e)
        {
            showupdateddata();
        }
        public async  void showupdateddata()
        {
            string connectionString = db;

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


       

    }


}

