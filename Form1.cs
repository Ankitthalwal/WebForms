using System;
using System.Data;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data.SqlClient;
namespace Exceldatatodb
{
    public partial class Form1 : Form
    {
        private string excelFilePath;
        string tableName;
        bool newimport = true;
        private string db  = "Data Source=SCIENCE-04\\SQLEXPRESS;Initial Catalog=db;Integrated Security=True";
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
            if (newimport)
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
            else
            {
                dataupdated();
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
            catch(Exception ex)
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
                    try
                    {
                        for (int row = 0; row < dataGridView1.Rows.Count; row++)
                        {

                            string updateQuery = $"UPDATE {tableName} SET  ";
                            for (int col = 0; col < dataGridView1.Columns.Count; col++)
                            {
                                if (dataGridView1.Columns[col].HeaderText == "ID") continue;
                                string columnName = dataGridView1.Columns[col].HeaderText;
                                updateQuery += $"{columnName}=@param{col}";
                                if (col < dataGridView1.Columns.Count - 1)
                                {
                                    updateQuery += ",";
                                }
                            }

                            string idColumn = dataGridView1.Columns[0].HeaderText;
                            string idValue = dataGridView1.Rows[row].Cells[0].Value.ToString();
                            updateQuery += $" WHERE {idColumn} = @id";

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {

                                for (int col = 0; col <dataGridView1.Columns.Count; col++)
                                {
                                    if (dataGridView1.Columns[col].HeaderText == "ID") continue;

                                    var cellValue = dataGridView1.Rows[row].Cells[col].Value;
                                    updateCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? (object)DBNull.Value);
                                }


                                updateCmd.Parameters.AddWithValue("@id", idValue);

                                updateCmd.ExecuteNonQuery();
                            }
                            
                        }

                    } catch (Exception ex)
                    {
                        
                    }
                    
                }
                MessageBox.Show("updated Succesfully");
            }
            catch(Exception ex)
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
                    newimport = false;
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Error: {ex.Message}");
                }
            }

        }



        private void dataupdated()
        {
            using (SqlConnection conn = new SqlConnection(db))
            {
                conn.Open();
                try
                {
                    for (int row = 0; row < dataGridView1.Rows.Count - 1; row++)
                    {
                        string insertQuery =  $"INSERT INTO[{tableName}]VALUES (";
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
                    newimport = false;
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

    
        //Cache data








    }
}
