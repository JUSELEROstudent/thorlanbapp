using GotsThorlabs.Interfaces;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel;
using apitest.Controllers;
using System.Linq;
using apitest.Controllers;
using Dapper;
using System.Collections;

namespace GotsThorlabs.Services

{
    public class ConnectionSqlite/* : IConnectionSql*/
    {
        public  static SqliteConnection CreateConnection()
        {
            var directorio = Directory.GetCurrentDirectory();

            string nameDbFile = Path.Combine(Directory.GetCurrentDirectory(), $"Database{Path.DirectorySeparatorChar}database{Path.DirectorySeparatorChar}ThorlabsSql.db");
            var connection = new SqliteConnection($"Data Source={nameDbFile}");
            return connection;
            
        }

        // async public IAsyncEnumerable<User> IsCorrectLogin(login sesionuser)
        //{
        //    var queryable =  CreateConnection();
        //    string loginString = "SELECT IdUser FROM user WHERE (nombre = @User OR correo =  @user) AND password =  @Password";
        //    //var rowsAffected = await queryable.QueryAsync<User>(loginString, sesionuser);
        //    yield return queryable.QueryAsync<User>(loginString, sesionuser);
        //}
    }
}
