using Microsoft.Extensions.Configuration;

namespace Fuel.Events;

public static class MySqlConnectionStrings
{
    public static string FromConfiguration(
        IConfiguration configuration,
        string connectionStringName,
        string defaultDatabase)
    {
        var configured = configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var host = configuration["MYSQLHOST"];
        var port = configuration["MYSQLPORT"];
        var user = configuration["MYSQLUSER"];
        var password = configuration["MYSQLPASSWORD"];
        var database = configuration["MYSQLDATABASE"];

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(port)
            || string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"{connectionStringName} not found. Configure ConnectionStrings:{connectionStringName} or MYSQLHOST, MYSQLPORT, MYSQLUSER, MYSQLPASSWORD.");
        }

        database = string.IsNullOrWhiteSpace(database) ? defaultDatabase : database;

        return $"Server={host};Port={port};Database={database};User={user};Password={password};";
    }
}
