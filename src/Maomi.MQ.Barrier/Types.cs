using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maomi.MQ.Barrier;

using System.Data.SqlClient; // Assuming SQL Server; adjust for other providers

// Interface representing a database connection
public interface Db
{
    // Executes a SQL statement and returns the result
    int ExecuteNonQuery(string query, params object[] args);

    // Executes a SQL query and returns a single row
    System.Data.IDataReader ExecuteReader(string query, params object[] args);
}

// Class representing database configuration
public class DbConfig
{
    public string Driver { get; set; }
    public string Host { get; set; }
    public long Port { get; set; } // Use long for 64-bit compatibility
    public string User { get; set; }
    public string Password { get; set; }
    public string Db { get; set; }
    public string Schema { get; set; }
}
