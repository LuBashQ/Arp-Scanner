using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleScanner
{
    public class Database
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Port { get; set; }
        public string SslMode { get; set; }
        private MySqlConnection _connection;

        public Database(string svname, string dbname, string username, string password, int port, string sslMode = "none")
        {
            this.ServerName = svname;
            this.DatabaseName = dbname;
            this.Username = username;
            this.Password = password;
            this.Port = port.ToString();
            this.SslMode = sslMode;

            string connectionString = $"server={ServerName};port={Port};UID={Username};password={Password};database={DatabaseName};SslMode={SslMode}";
            _connection = new MySqlConnection(connectionString);

        }

        public bool checkConnection()
        {
            bool success = false;
            try
            {
                _connection.Open();
                success = true;
            }
            catch { }
            finally
            {
                _connection.Close();
            }
            return success;
        }

        public bool ExecuteQuery(string query, List<MySqlParameter> arguments)
        {
            bool result = false;

            try
            {
                _connection.Open();

                var command = new MySqlCommand(query, _connection);
                command.Parameters.Clear();
                command.Parameters.AddRange(arguments.ToArray<MySqlParameter>());

                result = command.ExecuteReader().HasRows;
            }
            catch { }
            finally
            {
                _connection.Close();
            }
            return result;

        }


        public int GetRowCount(string tableName)
        {
            int result = 0;
            try
            {
                _connection.Open();

                string query = $"SELECT COUNT(*) as count FROM {tableName}";
                var command = new MySqlCommand(query, _connection);
                result = Convert.ToInt32(command.ExecuteScalar());
            }
            catch { }
            finally
            {
                _connection.Close();
            }
            return result;

        }


    }
}
