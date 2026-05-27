using MySql.Data.MySqlClient;
using System;
using System.Data.Common;

namespace DBL
{
    public abstract class DB
    {
        // Change this to your real connection string
        // (Better: read from appsettings.json / env later)
        protected static readonly string CONNECTION_STRING =
            "server=localhost;user=root;password=999GtaS999An;database=trivia_game;";

        protected MySqlConnection conn;
        protected MySqlCommand cmd;
        protected DbDataReader? reader;

        protected DB()
        {
            conn = new MySqlConnection(CONNECTION_STRING);
            cmd = conn.CreateCommand();
        }
    }
}
