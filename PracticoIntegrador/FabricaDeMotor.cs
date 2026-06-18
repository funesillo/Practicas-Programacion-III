using System;

namespace PracticoIntegrador;

public static class FabricaDeMotor
{
    public static IAccesoDatos Crear(Motor motor)
    {
        return motor switch
        {
            Motor.Postgres => new AccesoPostgres("Host=localhost;Port=5433;Username=postgres;Password=postgres;Database=postgres"),
            Motor.SqlServer => new AccesoSqlServer("Server=localhost,1433;User Id=sa;Password=Curso.NET2026;Database=master;TrustServerCertificate=True"),
            Motor.MySql => new AccesoMySql("Server=localhost;Port=3306;User=root;Password=Curso.NET2026;"),
            _ => throw new ArgumentException("Motor no soportado.")
        };
    }
}