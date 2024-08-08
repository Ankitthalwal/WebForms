HashSet<int> set = new HashSet<int>();

private void button5_Click(object sender, EventArgs e)
{
    getid();  // Get the ID of the selected row and store it in the HashSet
}

private void getid()
{
    if (dataGridView1.SelectedCells.Count > 0)
    {
        string id = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells[0].Value.ToString();
        int number = int.Parse(id);
        set.Add(number);
        MessageBox.Show($"ID {number} added to update list.");
    }
}

public void updatesParticularColumn()
{
    using (SqlConnection conn = new SqlConnection(db))
    {
        conn.Open();

        foreach (var id in set)
        {
            // First, check if the ID exists in the table
            string checkQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE ID = @id";
            using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@id", id);
                int count = (int)checkCmd.ExecuteScalar();

                if (count > 0)
                {
                    // ID exists, update the other columns
                    string updateQuery = $"UPDATE [{tableName}] SET ";

                    for (int col = 0; col < dataGridView1.Columns.Count; col++)
                    {
                        if (dataGridView1.Columns[col].HeaderText == "ID") continue; // Skip the ID column

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

                            var cellValue = dataGridView1.Rows[0].Cells[col].Value;
                            updateCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? DBNull.Value);
                        }

                        updateCmd.Parameters.AddWithValue("@id", id);
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // ID does not exist, insert a new row
                    string insertQuery = $"INSERT INTO [{tableName}] (";

                    for (int col = 0; col < dataGridView1.Columns.Count; col++)
                    {
                        string columnName = dataGridView1.Columns[col].HeaderText;
                        insertQuery += columnName;
                        if (col < dataGridView1.Columns.Count - 1)
                        {
                            insertQuery += ", ";
                        }
                    }

                    insertQuery += ") VALUES (";

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
                            var cellValue = dataGridView1.Rows[0].Cells[col].Value;
                            insertCmd.Parameters.AddWithValue($"@param{col}", cellValue ?? DBNull.Value);
                        }

                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        MessageBox.Show("Update/Insert operation completed successfully!");
    }
}
