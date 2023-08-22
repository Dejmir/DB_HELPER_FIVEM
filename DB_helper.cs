using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;

using MySql.Data.MySqlClient;

using CitizenFX.Core;
using CitizenFX.Core.Native;

public class DB_helper : BaseScript
{
    public static string connect = "";

    public DB_helper() => connect = API.GetConvar("mysql_connection_string_csharp", "");


    private static bool querying = false;

    public bool CheckDatabaseConnection(string connect)
    {
        bool con = false;
        MySqlConnection sqlConnection = new MySqlConnection(connect);
        try
        {
            sqlConnection.Open();
            if (sqlConnection.State == System.Data.ConnectionState.Open) con = true;
        }
        catch(Exception e)
        {
            return false;
        }

        sqlConnection.Close();
        return con;
    }

    private static byte maxRetries = 20;
    private static bool busyQuery = false;
    public int ExecuteNonQuery(string query, MySqlConnection Connection)
    {
        int retries = 0;
        while (busyQuery && retries < maxRetries) { Delay(100); retries++; }
        if(retries == maxRetries)
        {
            Debug.WriteLine($"[ERROR] Cannot execute non query due to timeout: {query},");
            return -1;
        }
        busyQuery = true;
        bool error = false;
        try
        {
            var cmd = new MySqlCommand();
            cmd.Connection = Connection;
            cmd.CommandText = query;
            return cmd.ExecuteNonQuery();
        }
        catch(Exception e)
        {
            error = true;
            Debug.WriteLine($"[ERROR] Cannot execute non query: {query}, details:\n{e}");
            return -1;
        }
        finally { if(!error) busyQuery = false; }

    }

    public MySqlDataReader ExecuteQuery(string query, MySqlConnection Connection)
    {
        int retries = 0;
        while (busyQuery && retries < maxRetries) { Delay(100); retries++; }
        if (retries == maxRetries)
        {
            Debug.WriteLine($"[ERROR] Cannot execute query due to timeout: {query},");
            return null;
        }
        busyQuery = true;
        bool error = false;
        MySqlDataReader result = null;
        try
        {
            var cmd = new MySqlCommand();
            cmd.Connection = Connection;
            cmd.CommandText = query;
            cmd.CommandType = System.Data.CommandType.Text;
            result = cmd.ExecuteReader();
        }
        catch(Exception e) 
        {
            error = true;
            Debug.WriteLine($"[ERROR] Cannot execute query: {query}, details:\n{e}");
        }
        finally
        {
            Task.Factory.StartNew(async () => {
                while (result != null && !result.IsClosed) await Delay(100);
                busyQuery = false;
            });
        }

        return result;
    }

}


/*
OPEN SOURCE SQL DATABASE LIBRARY 
*/