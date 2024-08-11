 private void Import_Excel(object sender, EventArgs e)
 {
     using (OpenFileDialog openFileDialog = new OpenFileDialog())
     {
         openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xls";
         if (openFileDialog.ShowDialog() == DialogResult.OK)
         {
             excelFilePath = openFileDialog.FileName;
             LoadDataFromExcelToDataTable();
             InsertDataIntoDatabase();
             DisplayDataFromDatabase();
         }
         else
         {
             MessageBox.Show("Please select an Excel File.");
             return;
         }
     }
 }

 // Display Excel data in DataGridView
 private void LoadDataFromExcelToDataTable()
 {
     Excel.Application excelApp = null;
     Excel.Workbook workbook = null;
     Excel.Worksheet worksheet = null;
     Excel.Range range = null;

     try
     {
         excelApp = new Excel.Application();
         workbook = excelApp.Workbooks.Open(excelFilePath);
         worksheet = workbook.Sheets[1];
         range = worksheet.UsedRange;

         dt = new DataTable();

         // Add columns
         for (int col = 1; col <= range.Columns.Count; col++)
         {
             dt.Columns.Add((range.Cells[1, col] as Excel.Range).Value2.ToString());
         }

         // Add rows
         for (int row = 2; row <= range.Rows.Count; row++)
         {
             DataRow dataRow = dt.NewRow();
             for (int col = 1; col <= range.Columns.Count; col++)
             {
                 dataRow[col - 1] = (range.Cells[row, col] as Excel.Range).Value2?.ToString() ?? string.Empty;
             }
             dt.Rows.Add(dataRow);
         }
     }
     catch (Exception ex)
     {
         MessageBox.Show($"An error occurred: {ex.Message}");
     }
     finally
     {
         // Release Excel objects to prevent memory leaks
         if (range != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(range);
         if (worksheet != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
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

         GC.Collect();
         GC.WaitForPendingFinalizers();
     }
 }


 private void InsertDataIntoDatabase()
 {
     try
     {
         using (SqlConnection conn = new SqlConnection(db))
         {
             conn.Open();

             // Create table query
             string createTableQuery = $"CREATE TABLE [{tableName}] (";
             for (int col = 0; col < dt.Columns.Count; col++)
             {
                 string columnName = dt.Columns[col].ColumnName;
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

             // Bulk insert the data from DataTable to SQL Server
             using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
             {
                 bulkCopy.DestinationTableName = tableName;
                 bulkCopy.WriteToServer(dt);
             }

             MessageBox.Show("Data inserted successfully");
         }
     }
     catch (Exception ex)
     {
         MessageBox.Show($"Bulk copy error: {ex.Message}");
     }
 }



 private void DisplayDataFromDatabase()
 {
     try
     {
         using (SqlConnection conn = new SqlConnection(db))
         {
             conn.Open();
             string selectQuery = $"SELECT * FROM [{tableName}]";
             using (SqlDataAdapter da = new SqlDataAdapter(selectQuery, conn))
             {
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
