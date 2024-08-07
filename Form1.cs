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
        string tableName = "db_1";
        private string db = "Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True";
       
        public static DataTable dt = new DataTable();

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
                    ReadAndInsertExcelData1();   
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
                    dt = new DataTable();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                     dt.Columns.Add(worksheet.Cells[1, col].Text);
                    }

                    // Add rows
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        DataRow dataRow = dt.NewRow();
                        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Text;
                        }
                       dt.Rows.Add(dataRow);
                    }

                    dataGridView1.DataSource = dt;
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
                   
                            // Create table query
                            string createTableQuery = $"CREATE TABLE [{tableName}] (";
                            for (int col = 0; col < dataGridView1.Columns.Count; col++)
                            {
                                string columnName = dataGridView1.Columns[col].HeaderText;
                                if (col == 0)
                                {
                                    createTableQuery += $"[{columnName}] INT PRIMARY KEY";
                                }
                                else
                                {
                                    createTableQuery += $", [{columnName}] TEXT";
                                }
                            }
                            createTableQuery += ")";

                            // Execute the create table query
                            using (SqlCommand createTableCmd = new SqlCommand(createTableQuery, conn))
                            {
                                createTableCmd.ExecuteNonQuery();
                            }
                       
                    }
                
                bulkcopy();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
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




        //Update the current data

        private void Update_Data(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(db))
                {
                    conn.Open(); 
                    string TruncateQuery = $"TRUNCATE TABLE [{tableName}]";
                    using (SqlCommand truncatatecmd = new SqlCommand(TruncateQuery,conn))
                    {
                        truncatatecmd.ExecuteNonQuery();
                    }
                    bulkcopy();
                }
                
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
        HashSet<int> rowIds = new HashSet<int>();
        
        private void Update2(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                string id = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells[0].Value.ToString();
                int number = int.Parse(id);
                rowIds.Add(number);
                MessageBox.Show($"{number}");
            }
        }
    }
}
