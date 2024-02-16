using System.Data;

namespace GotsThorlabs.Interfaces
{
    public interface IConnectionSql
    {
        public IDbConnection CreateConnection();
    }
}
