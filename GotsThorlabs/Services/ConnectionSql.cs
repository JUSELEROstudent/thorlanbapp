using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace apitest.Services
{
    public class ConnectionSql : Controller
    {
        public IDbConnection CreateConnection()
        {
            //IDbConnection db = new SqlConnection("Server=LAPTOP-GIVPQJIV;Database=Crud;Trusted_Connection=true;Encrypt=False");
            IDbConnection db = new SqlConnection("Server=192.168.10.16;Database=leontest1;user id=sa;password=GeVr2020$;Trusted_Connection=true;Encrypt=False;Integrated Security = False");
            return db;
        }
    }
}
