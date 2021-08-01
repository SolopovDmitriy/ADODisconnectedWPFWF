using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientApp
{
    public partial class Form1 : Form
    {
        private string _dp;
        private string _connectionString;

        private DbProviderFactory _dbProviderFactory;
        private DbConnection _dbConn;
        private DataTable _dataTable;
        private DbDataAdapter _dataAdapter;
        private DataSet _dataSet;
        public Form1()
        {
            InitializeComponent();
            _dp = ConfigurationManager.AppSettings["MSSQLProvider"];
            _connectionString = ConfigurationManager.ConnectionStrings["MSSQLProvider"].ConnectionString;
            _dbProviderFactory = DbProviderFactories.GetFactory(_dp);
            _dbConn = _dbProviderFactory.CreateConnection();
            _dbConn.ConnectionString = _connectionString;
            _dataAdapter = _dbProviderFactory.CreateDataAdapter();
            _dataSet = new DataSet();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //_dataTable = new DataTable(); //пустая таблица
            ///*Определяю столбцы*/
            //_dataTable.Columns.Add("Id");
            //_dataTable.Columns.Add("Login");
            //_dataTable.Columns.Add("Email");
            //_dataTable.Columns.Add("Password");

            ///*создаю строку данных*/
            //DataRow dataRow = _dataTable.NewRow();
            //dataRow[0] = 1;
            //dataRow[1] = "Test user";
            //dataRow[2] = "testemail.om";
            //dataRow[3] = "qwerty";

            //_dataTable.Rows.Add(dataRow);
            ///*привязываю DataTable к DataGridView*/
            //dataGridView_Results.DataSource = _dataTable;
            EncryptConnSettings("connectionStrings");
        }

        private void EncryptConnSettings(string connectSection)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //MessageBox.Show(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            ConnectionStringsSection connectionStringsSection = (ConnectionStringsSection)config.GetSection(connectSection);
            if(connectionStringsSection !=null)
            {
                if (!connectionStringsSection.SectionInformation.IsProtected)
                {
                    /*блок шифрования ------------start */

                    connectionStringsSection.SectionInformation.ProtectSection("RsaProtectedConfigurationProvider");// указываем какой алгорит будет использован для шифровки данных
                    connectionStringsSection.SectionInformation.ForceSave = true;
                    config.Save(ConfigurationSaveMode.Full);// перезаписываем наш файл конфигурации новыми данными
                    /*блок шифрования ------------end */
                }
            }
        }

        private void button_Execute_Click(object sender, EventArgs e)
        {
            DbDataReader resultReader = null;
            try
            {
                if (textBox_Query.Text.Length >= 5)
                {
                    _dataTable = new DataTable();

                    DbCommand dbSelectAllUsersCommand = _dbProviderFactory.CreateCommand();
                    dbSelectAllUsersCommand.Connection = _dbConn;
                    dbSelectAllUsersCommand.CommandText = textBox_Query.Text;
                    _dbConn.Open();
                    resultReader = dbSelectAllUsersCommand.ExecuteReader();
                    //toolStripStatusLabel_Info.Text = resultReader.
                    int line = 0;
                    do
                    {
                        while (resultReader.Read())
                        {
                            if (line == 0)
                            {
                                for (int i = 0; i < resultReader.FieldCount; i++)
                                {
                                    _dataTable.Columns.Add(resultReader.GetName(i));
                                }
                                line++;
                            }
                                DataRow currentRow = _dataTable.NewRow();
                                for (int i = 0; i < resultReader.FieldCount; i++)
                                {
                                    currentRow[i] = resultReader[i];
                                }
                                _dataTable.Rows.Add(currentRow);
                        }
                    } while (resultReader.NextResult());
                    dataGridView_Results.DataSource = _dataTable;
                }
                else
                {
                    toolStripStatusLabel_Info.ForeColor = Color.Red;
                    toolStripStatusLabel_Info.Text = "Тело  запроса не межет быть пустым";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dbConn.Close();
                if(resultReader != null) resultReader.Close();
            }
        }

        private void button_Exexute_set_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox_Query.Text.Length >= 5)
                {
                    //_dataAdapter.SelectCommand
                    //_dataAdapter.InsertCommand
                    //_dataAdapter.UpdateCommand
                    //_dataAdapter.DeleteCommand


                    //_dataAdapter.Fill() //select
                    //_dataAdapter.Update(); //insert-delete-update

                    _dataSet.Clear();
                    DbCommand command = _dbProviderFactory.CreateCommand();
                    command.Connection = _dbConn;
                    command.CommandText = textBox_Query.Text;
                    _dataAdapter.SelectCommand = command;
                    _dataAdapter.Fill(_dataSet);
                    dataGridView_Results.DataSource = _dataSet.Tables[0];

                    Debug.WriteLine(_dataAdapter.UpdateCommand);
                    Debug.WriteLine(_dataAdapter.InsertCommand);
                    Debug.WriteLine(_dataAdapter.DeleteCommand);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dbConn.Close();
            }
        }

        private void button_ExecUpdate_Click(object sender, EventArgs e)
        {
            if (textBox_Query.Text.Length >= 5)
            {
                //Пользовательская логика синхронизации GridView <=> dataAdapter <=> DB
                //_dataAdapter.SelectCommand
                //_dataAdapter.InsertCommand
                //_dataAdapter.UpdateCommand
                //_dataAdapter.DeleteCommand

                DbCommand dbSelectCommand = _dbProviderFactory.CreateCommand();
                dbSelectCommand.Connection = _dbConn;
                dbSelectCommand.CommandText = textBox_Query.Text;
                _dataAdapter.SelectCommand = dbSelectCommand;

                _dataAdapter.Fill(_dataSet);

                DbCommandBuilder dbCommandBuilder = _dbProviderFactory.CreateCommandBuilder();
                dbCommandBuilder.DataAdapter = _dataAdapter;
                dbCommandBuilder.GetDeleteCommand();
                dbCommandBuilder.GetInsertCommand();
                dbCommandBuilder.GetUpdateCommand();

                /*Пользовательская логика - UpdateCommand ----------start*/

                /*DbCommand dbCommandUpdate = _dbProviderFactory.CreateCommand();
                dbCommandUpdate.Connection = _dbConn;
                dbCommandUpdate.CommandText = "UPDATE users SET password = @pPassword WHERE Id = @iId";

                DbParameter dbPasswordParameter = dbCommandUpdate.CreateParameter();
                dbPasswordParameter.DbType = DbType.String;
                dbPasswordParameter.ParameterName = "@pPassword";
                dbPasswordParameter.SourceVersion = DataRowVersion.Current;
                dbPasswordParameter.SourceColumn = "password";
                dbCommandUpdate.Parameters.Add(dbPasswordParameter);

                DbParameter dbIdParameter = dbCommandUpdate.CreateParameter();
                dbIdParameter.DbType = DbType.Int32;
                dbIdParameter.ParameterName = "@iId";
                dbIdParameter.SourceColumn = "Id";
                dbIdParameter.SourceVersion = DataRowVersion.Original;
                dbCommandUpdate.Parameters.Add(dbIdParameter);

                _dataAdapter.UpdateCommand = dbCommandUpdate;*/
                /*Пользовательская логика - UpdateCommand ----------end*/


                _dataAdapter.Update(_dataSet);

                _dataSet.Clear();
                _dataAdapter.Fill(_dataSet);
                dataGridView_Results.DataSource = _dataSet.Tables[0];


                for (int i = 0; i < dataGridView_Results.Columns.Count; i++)
                {
                    if (dataGridView_Results.Columns[i] is DataGridViewImageColumn)
                    {
                        ((DataGridViewImageColumn)dataGridView_Results.Columns[i]).ImageLayout = DataGridViewImageCellLayout.Stretch;
                        break;
                    }
                }
            }
        }

        private void dataGridView_Results_CellLeave(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button_FilterExec_Click(object sender, EventArgs e)
        {
            if(textBox_RowFilter.Text.Length >= 5)
            {
                DataViewManager dataViewManager = new DataViewManager(_dataSet);
                dataViewManager.DataViewSettings[0].RowFilter = textBox_RowFilter.Text;

                DataView dataViewFiltered = dataViewManager.CreateDataView(_dataSet.Tables[0]);
                dataGridView_Results.DataSource = dataViewFiltered;
            }
        }

        private void button_SortExec_Click(object sender, EventArgs e)
        {

            if (textBox_SortFilter.Text.Length >= 3)
            { 
                DataViewManager dataViewManager = new DataViewManager(_dataSet);
                dataViewManager.DataViewSettings[0].Sort = textBox_SortFilter.Text;

                DataView dataViewSorted = dataViewManager.CreateDataView(_dataSet.Tables[0]);
                dataGridView_Results.DataSource = dataViewSorted;
            }
        }

        private void button_OpenFD_Click(object sender, EventArgs e)
        {
            string imageFile = "";
            if (dataGridView_Results.SelectedRows.Count == 1) //только если есть выбранная строка gridview
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png"; 
                    imageFile = openFileDialog.FileName;
                    byte[] file;
                    using (var stream = new FileStream(imageFile, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            _dbConn.Open();
                            file = reader.ReadBytes((int)stream.Length);
                            DbCommand dbCommandInsert = _dbProviderFactory.CreateCommand();
                            dbCommandInsert.Connection = _dbConn;
                            dbCommandInsert.CommandText = "UPDATE users_info SET avatar = @avatar WHERE id = @id";

                            DbParameter idParam = dbCommandInsert.CreateParameter();
                            idParam.DbType = DbType.Int32;
                            idParam.ParameterName = "@id";
                            idParam.SourceVersion = DataRowVersion.Current;
                            idParam.SourceColumn = "id";
                            idParam.Value = dataGridView_Results.SelectedRows[0].Cells["Id"].Value;
                            dbCommandInsert.Parameters.Add(idParam);

                            DbParameter avatarParam = dbCommandInsert.CreateParameter();
                            avatarParam.DbType = DbType.Binary;
                            avatarParam.ParameterName = "@avatar";
                            avatarParam.Value = CompressImage(file);
                            avatarParam.Size = file.Length;

                            dbCommandInsert.Parameters.Add(avatarParam);

                            dbCommandInsert.ExecuteNonQuery();
                            _dbConn.Close();
                        }
                    }

                }
            }
        }
        private byte[] CompressImage(byte[] file)
        {
            using (var ms = new MemoryStream(file))
            {
                Image img = Image.FromStream(ms);
                int maxWidth = 300;
                int maxHeight = 300;

                double dX = (double)maxWidth / img.Width;
                double dY = (double)maxHeight / img.Height;

                double ratio = Math.Min(dX, dY);

                int newWidth = Convert.ToInt32(ratio * img.Width);   //сжатое по ширине
                int newHeight = Convert.ToInt32(ratio * img.Height); //сжатое по высоте

                //создаем новую картинку
                Image newImg = new Bitmap(newWidth, newHeight);
                /*используя графикс рисуем на новой картинке старое изображение*/

                Graphics graphics = Graphics.FromImage(newImg);
                graphics.DrawImage(img, 0, 0, newWidth, newHeight);

                using (var mems = new MemoryStream())
                {
                    newImg.Save(mems, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return mems.ToArray();
                }
            }
        }

        private async void button_ExecAsync_Click(object sender, EventArgs e)
        {
            // для работы в асинхронном режиме
            const string AsyncEnabled = "Asynchronous Processing=true"; // ключ включения асинхронного режима
            if (!_connectionString.Contains(AsyncEnabled))// если строка не содержит подключение в ассинхронном режиме
            {
                //_connectionString = _connectionString + "; " + AsyncEnabled;
                _connectionString = String.Format("{0}; {1}", _connectionString, AsyncEnabled);
            }
            _dbConn = _dbProviderFactory.CreateConnection();// пересоздаем строку подключения, переинициализируем строку подключения
            _dbConn.ConnectionString = _connectionString; //задаем ту строку подключения, которую с конкатенировали с ключом работы в асинхронном режиме
            DbCommand dbAsyncSelectCommand = _dbProviderFactory.CreateCommand();//асинхронная операция выбора
            dbAsyncSelectCommand.Connection = _dbConn;// указываем подключение
            dbAsyncSelectCommand.CommandText = "WAITFOR DELAY '00:00:03'; SELECT * FROM Users";// тело запроса

            try
            {
                await _dbConn.OpenAsync();
                DataTable dataTable = new DataTable();
                using (DbDataReader reader = await dbAsyncSelectCommand.ExecuteReaderAsync())
                {
                    int rowIndex = 0;
                    do
                    {
                        while (await reader.ReadAsync())
                        {
                            /*заполняю шапку таблицы---------------start*/
                            if (rowIndex==0)
                            {
                                
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    dataTable.Columns.Add(reader.GetName(i));
                                }
                                rowIndex++;
                            }
                            /*заполняю шапку таблицы---------------end*/
                            /*заполняю тело таблицы---------------start*/
                            DataRow currentRow = dataTable.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                currentRow[i] = await reader.GetFieldValueAsync<object>(i);
                            }
                            dataTable.Rows.Add(currentRow);
                            /*заполняю тело таблицы---------------end*/


                        }
                    } while (reader.NextResult());
                }

                dataGridView_Results.DataSource = null;
                dataGridView_Results.DataSource = dataTable;
            }
            catch (Exception ex)// создаем небезопасный участок
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                _dbConn.Close();
            }
        }
    }
}
