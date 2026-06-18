using System;

namespace PracticoIntegrador;

internal class Program
{
    static void Main(string[] args)
    {
        // Puedes cambiar aquí el motor para probar los distintos casos
        // Motor.Postgres | Motor.SqlServer | Motor.MySql
        Motor motorSeleccionado = Motor.MySql;

        try
        {
            Console.Clear();
            Console.WriteLine($"MOTOR: {motorSeleccionado}\n");

            IAccesoDatos acceso = FabricaDeMotor.Crear(motorSeleccionado);

            Console.WriteLine("RF2\nCrear estructura");
            acceso.CrearEstructura();

            Console.WriteLine("\nRF3 Insertar datos de prueba");
            acceso.InsertarDatosPrueba();

            Console.WriteLine("\nRF4\nEjecutar operaciones (C1, C2, U1, D1)");
            acceso.EjecutarOperaciones();

            Console.WriteLine("\nRF5\nDemostrar rollback");
            acceso.DemostrarRollback();

            Console.WriteLine($"\n=====\nFIN ({motorSeleccionado}) =====");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR OCURRIDO]: {ex.Message}");
        }
    }
}