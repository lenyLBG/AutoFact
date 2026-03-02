using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoFact
{
    internal class Bdd
    {
        string connectionString = "Server=192.168.56.200;Database=AutoFact;User Id=app;Password=Demaindeslaube;";

        public bool TestConnection()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch
                {
                    return false;
                }

            }
        }
    }
}
