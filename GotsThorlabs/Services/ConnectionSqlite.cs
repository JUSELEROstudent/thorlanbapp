using GotsThorlabs.Interfaces;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace GotsThorlabs.Services

{
    public class ConnectionSqlite/* : IConnectionSql*/
    {
        public async static CreateConnection()
        {
            //await ApplicationData.Current.LocalFolder.CreateFileAsync("sqliteSample.db", CreationCollisionOption.OpenIfExists);
            //string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "sqliteSample.db");
            //using (SqliteConnection db =
            //   new SqliteConnection($"Filename={dbpath}"))
            //{
            //    db.Open();

            //    String tableCommand = "CREATE TABLE IF NOT " +
            //        "EXISTS MyTable (Primary_Key INTEGER PRIMARY KEY, " +
            //        "Text_Entry NVARCHAR(2048) NULL)";

            //    SqliteCommand createTable = new SqliteCommand(tableCommand, db);

            //    createTable.ExecuteReader();
            //}
            throw new NotImplementedException();
        }
    }
}
