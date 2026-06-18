using System;
using Npgsql;

namespace PracticoIntegrador;

public class AccesoPostgres : IAccesoDatos
{
    private readonly string _csMaster;
    private readonly string _csApp = "Host=localhost;Port=5433;Username=postgres;Password=postgres;Database=practico";

    public AccesoPostgres(string connectionString) => _csMaster = connectionString;

    public void CrearEstructura()
    {
        using (var conn = new NpgsqlConnection(_csMaster))
        {
            conn.Open();
            using var cmdCheck = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'practico';", conn);
            if (cmdCheck.ExecuteScalar() == null)
            {
                using var cmdCreate = new NpgsqlCommand("CREATE DATABASE practico;", conn);
                cmdCreate.ExecuteNonQuery();
                Console.WriteLine("Base 'practico' creada.");
            }
        }

        using (var conn = new NpgsqlConnection(_csApp))
        {
            conn.Open();
            string sql = @"
                DROP TABLE IF EXISTS detalle_pedido, pedidos, productos, clientes, categorias CASCADE;
                CREATE TABLE categorias (id SERIAL PRIMARY KEY, nombre VARCHAR(100) NOT NULL);
                CREATE TABLE clientes (id SERIAL PRIMARY KEY, nombre VARCHAR(100) NOT NULL, email VARCHAR(100) NOT NULL UNIQUE);
                CREATE TABLE productos (id SERIAL PRIMARY KEY, nombre VARCHAR(100) NOT NULL, precio DECIMAL(18,2) NOT NULL, stock INT NOT NULL, categoria_id INT REFERENCES categorias(id));
                CREATE TABLE pedidos (id SERIAL PRIMARY KEY, cliente_id INT REFERENCES clientes(id), fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
                CREATE TABLE detalle_pedido (pedido_id INT REFERENCES pedidos(id), producto_id INT REFERENCES productos(id), cantidad INT NOT NULL, precio_unit DECIMAL(18,2) NOT NULL, PRIMARY KEY (pedido_id, producto_id));";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
            Console.WriteLine("Estructura (5 tablas) creada.");
        }
    }

    public void InsertarDatosPrueba()
    {
        using var conn = new NpgsqlConnection(_csApp);
        conn.Open();
        string sql = @"
            INSERT INTO categorias (nombre) VALUES ('Electrónica'), ('Libros'), ('Hogar');
            INSERT INTO clientes (nombre, email) VALUES ('Juan Perez', 'juan@mail.com'), ('Ana Gomez', 'ana@mail.com');
            INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES 
            ('Notebook 14""', 850000.00, 10, 1), ('Mouse inalámbrico', 12000.00, 50, 1), 
            ('Teclado mecánico', 35000.00, 20, 1), ('Clean Code', 28000.00, 15, 2), 
            ('Lámpara LED escritorio', 15000.00, 30, 3);";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
        Console.WriteLine("Datos de prueba insertados (commit).");
    }

    public void EjecutarOperaciones()
    {
        using var conn = new NpgsqlConnection(_csApp);
        conn.Open();

        Console.WriteLine("\n[C1] Productos con su categoría:");
        using (var cmd = new NpgsqlCommand("SELECT p.id, p.nombre, p.precio, c.nombre as cat FROM productos p JOIN categorias c ON p.categoria_id = c.id ORDER BY p.id;", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) Console.WriteLine($"#{r["id"]} {r["nombre"]} ${r["precio"]:F2} [{r["cat"]}]");

        Console.WriteLine("\n[C2] Detalle y total del pedido #1:");
        using (var tx = conn.BeginTransaction())
        {
            try
            {
                var cmdPed = new NpgsqlCommand("INSERT INTO pedidos (cliente_id) VALUES (@cId) RETURNING id;", conn, tx);
                cmdPed.Parameters.AddWithValue("@cId", 1);
                int pId = Convert.ToInt32(cmdPed.ExecuteScalar());

                var cmdDet = new NpgsqlCommand("INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unit) VALUES (@pId, @prodId, @cant, @precio);", conn, tx);
                cmdDet.Parameters.AddWithValue("@pId", pId);
                cmdDet.Parameters.AddWithValue("@prodId", 2);
                cmdDet.Parameters.AddWithValue("@cant", 2);
                cmdDet.Parameters.AddWithValue("@precio", 12000.00m);
                cmdDet.ExecuteNonQuery();
                Console.WriteLine("Mouse inalámbrico x2 @ $12000.00 = $24000.00");

                cmdDet.Parameters["@prodId"].Value = 1; cmdDet.Parameters["@cant"].Value = 1; cmdDet.Parameters["@precio"].Value = 850000.00m;
                cmdDet.ExecuteNonQuery();
                Console.WriteLine("Notebook 14\" x1 @ $850000.00 = $850000.00");

                cmdDet.Parameters["@prodId"].Value = 3; cmdDet.Parameters["@cant"].Value = 1; cmdDet.Parameters["@precio"].Value = 35000.00m;
                cmdDet.ExecuteNonQuery();
                Console.WriteLine("Teclado mecánico x1 @ $35000.00 = $35000.00");

                tx.Commit();
                Console.WriteLine($"TOTAL pedido #{pId}: $909000.00");
            }
            catch { tx.Rollback(); throw; }
        }

        using (var cmd = new NpgsqlCommand("UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = 1;", conn))
            Console.WriteLine($"\n[U1] Subí 10% precios de categoría #1 -> {cmd.ExecuteNonQuery()} filas.");

        using (var cmd = new NpgsqlCommand("DELETE FROM detalle_pedido WHERE pedido_id = 1 AND producto_id = 2;", conn))
            Console.WriteLine($"[D1] Borré línea (pedido 1, producto 2) -> {cmd.ExecuteNonQuery()} filas.");
    }

    public void DemostrarRollback()
    {
        using var conn = new NpgsqlConnection(_csApp);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = new NpgsqlCommand("UPDATE productos SET precio = 1.00 WHERE id = 1;", conn, tx);
            cmd.ExecuteNonQuery();
            Console.WriteLine("\nUPDATE aplicado (precio -> 1) dentro de la transacción.");
            throw new Exception("Fallo simulado.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            Console.WriteLine($"Excepción capturada -> ROLLBACK. (Error: {ex.Message})");
        }
    }
}