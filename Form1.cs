using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using OfficeOpenXml;
using System.Data.OleDb;

namespace Win12
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    DataTable dt = ReadExcel(filePath);
                    dataGridView1.DataSource = dt;
                }
            }
        }

        private DataTable ReadExcel(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (extension.Equals(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return ReadExcelOleDb(filePath);
            }
            else if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return ReadExcelEPPlus(filePath);
            }
            else
            {
                throw new NotSupportedException("The file format is not supported.");
            }
        }

        private DataTable ReadExcelEPPlus(string filePath)
        {
            DataTable dt = new DataTable();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // Get the first worksheet
                foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                {
                    dt.Columns.Add(firstRowCell.Text);
                }

                for (int rowNum = 2; rowNum <= worksheet.Dimension.End.Row; rowNum++)
                {
                    var wsRow = worksheet.Cells[rowNum, 1, rowNum, worksheet.Dimension.End.Column];
                    DataRow row = dt.NewRow();
                    foreach (var cell in wsRow)
                    {
                        row[cell.Start.Column - 1] = cell.Text;
                    }
                    dt.Rows.Add(row);
                }
            }

            return dt;
        }

        private DataTable ReadExcelOleDb(string filePath)
        {
            DataTable dt = new DataTable();
            string connString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={filePath};Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"";

            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand("SELECT * FROM [Sheet1$]", conn);
                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                da.Fill(dt);
            }

            return dt;
        }
    }
}
