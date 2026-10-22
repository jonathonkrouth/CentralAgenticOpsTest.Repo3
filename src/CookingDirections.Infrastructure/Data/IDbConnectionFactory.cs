using System.Data.Common;

namespace CookingDirections.Infrastructure.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
