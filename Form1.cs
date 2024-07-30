using Excel = Microsoft.Office.Interop.Excel;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Collections.Generic;


namespace Insertdata1
{
    public partial class Form1 : Form
    {
        private List<DataTable> dataSnapshots = new List<DataTable>();
        private int currentSnapshotIndex = -1; // Keeps track of the current snapshot index

        SqlDataAdapter da;
        private DataTable dataTable;
        private string excelFilePath;
        string tableName;

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
             tableName = worksheet.Name;

            try
            {
                using (SqlConnection conn = new SqlConnection("Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True"))
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
            using (SqlConnection conn = new SqlConnection("Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True"))
            {
                try
                {
                    conn.Open();

                    string getTablesQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

                    string tableName = null;

                    using (SqlCommand cmd = new SqlCommand(getTablesQuery, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tableName = reader.GetString(0);
                            }
                        }
                    }

                    if (tableName != null)
                    {
                        string selectQuery = $"SELECT * FROM [{tableName}]";
                        using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                        {
                            da = new SqlDataAdapter(selectCmd);
                            SqlCommandBuilder commandBuilder = new SqlCommandBuilder(da);
                            dataTable = new DataTable();
                            da.Fill(dataTable);
                            dataGridView1.DataSource = dataTable;
                        
                        }
                    }
                    else
                    {
                        MessageBox.Show("No tables found in the database.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
        }




        private void CreateDataSnapshot()
        {
            DataTable dataTable = new DataTable();

            // Copy column structure
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                dataTable.Columns.Add(column.HeaderText, column.ValueType);
            }

            // Copy rows
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow)
                {
                    DataRow dataRow = dataTable.NewRow();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        dataRow[cell.ColumnIndex] = cell.Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            // Add to list and update index
            dataSnapshots.Add(dataTable);
            currentSnapshotIndex = dataSnapshots.Count - 1;
        }



        //Load snapshot
        private void LoadCurrentSnapshot()
        {
            if (currentSnapshotIndex >= 0 && currentSnapshotIndex < dataSnapshots.Count)
            {
                dataGridView1.DataSource = dataSnapshots[currentSnapshotIndex];
            }
            else
            {
                MessageBox.Show("No snapshot available.");
            }
        }


        //create left right button
        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (dataSnapshots.Count > 0 && currentSnapshotIndex > 0)
            {
                currentSnapshotIndex--;
                LoadCurrentSnapshot();
            }
            else
            {
                MessageBox.Show("No previous snapshot available.");
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (dataSnapshots.Count > 0 && currentSnapshotIndex < dataSnapshots.Count - 1)
            {
                currentSnapshotIndex++;
                LoadCurrentSnapshot();
            }
            else
            {
                MessageBox.Show("No next snapshot available.");
            }
        }

        private void btn2(object sender, EventArgs e)
        {

            // Create a snapshot before making changes
            
            CreateDataSnapshot();

            // Optionally display the snapshot in a separate DataGridView or other control
            // LoadDataSnapshot(snapshotKey);


            string connectionString = "Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True";

            try
            {
                dataGridView1.EndEdit(); // Commit any pending edits

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Assuming the DataGridView contains an "ID" column to identify the rows
                            for (int row = 0; row < dataGridView1.Rows.Count; row++) // Starting at 0 to include all rows
                            {
                                if (dataGridView1.Rows[row].IsNewRow) continue; // Skip new row placeholder

                                // Adjust this query to match your table's schema and primary key
                                string updateQuery = $"UPDATE {tableName} SET ";

                                // Build the SET clause dynamically
                                for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                {
                                    // Assuming the first column is the primary key
                                    if (dataGridView1.Columns[col].HeaderText == "ID") continue;

                                    string columnName = dataGridView1.Columns[col].HeaderText;
                                    updateQuery += $"{columnName} = @param{col}";

                                    if (col < dataGridView1.Columns.Count - 1)
                                    {
                                        updateQuery += ", ";
                                    }
                                }

                                // Append the WHERE clause to target the correct row
                                // Assumes that the first column is the primary key
                                string idColumn = dataGridView1.Columns[0].HeaderText;
                                string idValue = dataGridView1.Rows[row].Cells[0].Value.ToString();
                                updateQuery += $" WHERE {idColumn} = @id";

                                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction))
                                {
                                    // Add parameters for each column value
                                    for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                    {
                                        if (dataGridView1.Columns[col].HeaderText == "ID") continue;

                                        var cellValue = dataGridView1.Rows[row].Cells[col].Value;
                                        updateCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? (object)DBNull.Value);
                                    }

                                    // Add parameter for the ID column
                                    updateCmd.Parameters.AddWithValue("@id", idValue);

                                    updateCmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Error during data update: {ex.Message}");
                        }
                    }
                }

                MessageBox.Show("Data updated successfully!");
                DisplayDataFromSQLServer(); // Refresh the data display
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }


        //Finalize the current datagridview

        private void btn2_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection("Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True"))
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

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn, transaction))
                            {
                                for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                {
                                    var cellValue = dataGridView1.Rows[row].Cells[col].Value;
                                    insertCmd.Parameters.AddWithValue($"@param{col}", cellValue?.ToString() ?? string.Empty);
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
