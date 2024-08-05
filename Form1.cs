using System;
using System.Data;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data.SqlClient;
using OfficeOpenXml;
using System.IO;
using DocumentFormat.OpenXml.Wordprocessing;
namespace Exceldatatodb
{
    public partial class Form1 : Form
    {
        private string excelFilePath;
        private string tableName = "temporaray";
        bool newimport = true;
        private string db = "Data Source=VENOM\\SQLEXPRESS;Initial Catalog=Studentdb;Integrated Security=True;Encrypt=False";
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

            {
                try
                {

                    using (SqlConnection conn = new SqlConnection(db))
                    {
                        conn.Open();
                        string createQuery = $"CREATE TABLE [{tableName}] (";
                        for (int col = 1; col <= range.Columns.Count; col++)
                        {
                            string columnName = (range.Cells[1, col] as Excel.Range).Value2.ToString().Replace(" ", "_");

                            if (col == 1)
                            {
                                createQuery += $"{columnName} INTEGER PRIMARY KEY ";
                            }
                            else
                            {
                                createQuery += $", {columnName} TEXT";
                            }
                        }
                        createQuery += ")";

                        using (SqlCommand createTablecmd = new SqlCommand(createQuery, conn))
                        {
                            createTablecmd.ExecuteNonQuery();
                        }

                        //Insert into table

                        for (int row = 2; row <= range.Rows.Count; row++)
                        {
                            string insertQuery = $"INSERT INTO [{tableName}] VALUES (";
                            for (int col = 1; col <= range.Columns.Count; col++)
                            {
                                insertQuery += $"@param{col}";
                                if (col < range.Columns.Count)
                                {
                                    insertQuery += ",";
                                }
                            }
                            insertQuery += ")";


                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                            {
                                for (int col = 1; col <= range.Columns.Count; col++)
                                {
                                    var cellValue = (range.Cells[row, col] as Excel.Range).Value2;
                                    if (cellValue == null)
                                    {
                                        cmd.Parameters.AddWithValue($"@param{col}", DBNull.Value);
                                    }
                                    else if (col == 1)
                                    {
                                        cmd.Parameters.AddWithValue($"@param{col}", Convert.ToInt32(cellValue));
                                    }
                                    else
                                    {
                                        cmd.Parameters.AddWithValue($"@param{col}", cellValue.ToString());
                                    }
                                }
                                cmd.ExecuteNonQuery();

                            }


                        }
                    }
                    MessageBox.Show("saved Succesfully");

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error :" + ex.Message);
                }
                finally
                {

                    workbook.Close(false);
                    excelApp.Quit();
                }
            }

        }




        //Display excel data from database to Datagridview

        private void DisplayData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(db))
                {
                    string selectQuery = $"SELECT * FROM [{tableName}]";
                    using (SqlDataAdapter da = new SqlDataAdapter(selectQuery, conn))
                    {
                        //**     SqlCommandBuilder commandBuilder = new SqlCommandBuilder(da);
                        DataTable dataTable = new DataTable();
                        da.Fill(dataTable);
                        dataGridView1.DataSource = dataTable;
                        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    }
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
                using (SqlConnection conn = new SqlConnection(db))
                {
                    conn.Open();

                    for (int row = 0; row < dataGridView1.Rows.Count - 1; row++)
                    {
                        var idValue = dataGridView1.Rows[row].Cells["ID"].Value;
                        bool isNewRecord = idValue == null || string.IsNullOrEmpty(idValue.ToString());

                        if (isNewRecord)
                        {
                            // Insert new record
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
                                    insertCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? DBNull.Value);
                                }
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Update existing record
                            string updateQuery = $"UPDATE [{tableName}] SET ";
                            for (int col = 0; col < dataGridView1.Columns.Count; col++)
                            {
                                if (dataGridView1.Columns[col].HeaderText == "ID") continue;
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
                                for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                {
                                    if (dataGridView1.Columns[col].HeaderText == "ID") continue;

                                    var cellValue = dataGridView1.Rows[row].Cells[col].Value;
                                    updateCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? DBNull.Value);
                                }
                                updateCmd.Parameters.AddWithValue("@id", idValue);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    MessageBox.Show("Updated Successfully");
                }
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
                try
                {
                    conn.Open();

                    // Step 1: Create the 'dummy' table with the same structure as the original table
                    string createTableQuery = $"SELECT * INTO dummy FROM [{tableName}] WHERE 1 = 0";
                    using (SqlCommand createTableCmd = new SqlCommand(createTableQuery, conn))
                    {
                        createTableCmd.ExecuteNonQuery();
                    }

                    // Step 2: Copy all data from the original table to the 'dummy' table
                    string insertDataQuery = $"INSERT INTO dummy SELECT * FROM [{tableName}]";
                    using (SqlCommand insertCmd = new SqlCommand(insertDataQuery, conn))
                    {
                        insertCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Data successfully copied to 'dummy' table!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    excelFilePath = openFileDialog.FileName;
                    ReadAndInsertExcelData1(excelFilePath);
                }
                else
                {
                    MessageBox.Show("Please select an Excel File.");
                    return;
                }
            }
            displaydata();


        }
        public void displaydata()
        {
            // Set the license context for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Create a DataTable to store the Excel data
            DataTable dt = new DataTable();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(excelFilePath)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // Load the first worksheet
                    int colCount = worksheet.Dimension.Columns; // Get the number of columns
                    int rowCount = worksheet.Dimension.Rows; // Get the number of rows

                    // Add columns to DataTable
                    for (int col = 1; col <= colCount; col++)
                    {
                        dt.Columns.Add(worksheet.Cells[1, col].Text); // Add header row as columns
                    }

                    // Add rows to DataTable
                    for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip the header
                    {
                        DataRow dr = dt.NewRow();
                        for (int col = 1; col <= colCount; col++)
                        {
                            dr[col - 1] = worksheet.Cells[row, col].Text;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                // Set the DataGridView DataSource to the DataTable
                dataGridView1.DataSource = dt;
                MessageBox.Show("saved: ");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


     





        //Create the snapshot of current datagridview






    


    }
}
