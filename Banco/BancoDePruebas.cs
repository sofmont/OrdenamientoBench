using System.Diagnostics;
using OrdenamientoBench.Algoritmos;

namespace OrdenamientoBench.Banco;

/// <summary>Condición inicial de los datos antes de ordenarlos.</summary>
public enum Condicion
{
    Aleatorio,
    Ordenado,
    Inverso
}

/// <summary>Resultado de medir un algoritmo con un tamaño y una condición.</summary>
public sealed class Resultado
{
    public string Algoritmo { get; set; }
    public string Tipo { get; set; }
    public int N { get; set; }
    public Condicion Condicion { get; set; }
    public double Milisegundos { get; set; }
    public int Repeticiones { get; set; }
    public int Pasadas { get; set; }
    public bool Correcto { get; set; }
}

/// <summary>Describe un algoritmo del catálogo (interno sobre arreglo o externo sobre archivo).</summary>
public sealed class Algoritmo
{
    public string Nombre { get; }
    public bool EsExterno { get; }
    public Action<int[]> EjecutarInterno { get; }
    public Func<string, int> EjecutarExterno { get; }

    private Algoritmo(string nombre, Action<int[]> interno, Func<string, int> externo)
    {
        Nombre = nombre;
        EjecutarInterno = interno;
        EjecutarExterno = externo;
        EsExterno = externo != null;
    }

    public static Algoritmo Interno(string nombre, Action<int[]> accion) => new Algoritmo(nombre, accion, null);

    public static Algoritmo Externo(string nombre, Func<string, int> accion) => new Algoritmo(nombre, null, accion);
}

public static class Catalogo
{
    public static readonly IReadOnlyList<Algoritmo> Todos = new List<Algoritmo>
    {
        Algoritmo.Interno("Burbuja", OrdenamientoInterno.Burbuja),
        Algoritmo.Interno("Quicksort", OrdenamientoInterno.Quicksort),
        Algoritmo.Interno("ShellSort", OrdenamientoInterno.ShellSort),
        Algoritmo.Interno("Radix", OrdenamientoInterno.RadixSort),
        Algoritmo.Externo("Intercalación", OrdenamientoExterno.IntercalacionDosBloques),
        Algoritmo.Externo("Mezcla directa", OrdenamientoExterno.MezclaDirecta),
        Algoritmo.Externo("Mezcla natural", OrdenamientoExterno.MezclaNatural)
    };
}

public sealed class BancoDePruebas
{
    /// <summary>Rango de los valores generados: 0 a 999,999 (enteros no negativos, requisito de Radix).</summary>
    private const int ValorMaximo = 1_000_000;

    public static int[] GenerarAleatorios(int n, int semilla)
    {
        var rnd = new Random(semilla);
        int[] datos = new int[n];
        for (int i = 0; i < n; i++)
            datos[i] = rnd.Next(0, ValorMaximo);
        return datos;
    }

    /// <summary>Devuelve una copia de los datos aleatorios en la condición pedida (no se cronometra).</summary>
    public static int[] Preparar(int[] aleatorio, Condicion condicion)
    {
        int[] copia = (int[])aleatorio.Clone();
        if (condicion != Condicion.Aleatorio)
        {
            Array.Sort(copia);
            if (condicion == Condicion.Inverso)
                Array.Reverse(copia);
        }
        return copia;
    }

    public static bool EstaOrdenado(int[] a)
    {
        for (int i = 1; i < a.Length; i++)
        {
            if (a[i - 1] > a[i])
                return false;
        }
        return true;
    }

    private static long Suma(int[] a)
    {
        long s = 0;
        foreach (int x in a)
            s += x;
        return s;
    }

    /// <summary>
    /// Ejecuta todas las combinaciones tamaño × algoritmo × condición. Se llama desde un hilo de fondo.
    /// </summary>
    public List<Resultado> Ejecutar(
        IList<Algoritmo> algoritmos,
        IList<int> tamanos,
        int repeticiones,
        int semilla,
        CancellationToken ct,
        Action<Resultado> alResultado,
        Action<int, int, string> alProgreso)
    {
        var lista = new List<Resultado>();
        string dir = Path.Combine(Path.GetTempPath(), "OrdenamientoBench_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            alProgreso?.Invoke(0, 1, "Calentando el compilador JIT...");
            Calentar(algoritmos, dir);

            Condicion[] condiciones = Enum.GetValues<Condicion>();
            int total = algoritmos.Count * tamanos.Count * condiciones.Length;
            int hecho = 0;

            foreach (int n in tamanos)
            {
                int[] baseAleatoria = GenerarAleatorios(n, semilla);

                foreach (Algoritmo alg in algoritmos)
                {
                    foreach (Condicion cond in condiciones)
                    {
                        ct.ThrowIfCancellationRequested();
                        alProgreso?.Invoke(hecho, total, $"{alg.Nombre} · n = {n:N0} · {cond}");

                        Resultado r = Medir(alg, baseAleatoria, cond, repeticiones, dir);
                        lista.Add(r);
                        alResultado?.Invoke(r);
                        hecho++;
                    }
                }
            }

            alProgreso?.Invoke(total, total, "Terminado");
        }
        finally
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (IOException)
            {
            }
        }

        return lista;
    }

    private static void Calentar(IEnumerable<Algoritmo> algoritmos, string dir)
    {
        int[] muestra = GenerarAleatorios(2000, 1);
        string ruta = Path.Combine(dir, "calentamiento.bin");

        foreach (Algoritmo a in algoritmos)
        {
            if (a.EsExterno)
            {
                OrdenamientoExterno.EscribirArchivo(ruta, muestra);
                a.EjecutarExterno(ruta);
            }
            else
            {
                a.EjecutarInterno((int[])muestra.Clone());
            }
        }
    }

    private static Resultado Medir(Algoritmo alg, int[] baseAleatoria, Condicion cond, int repeticiones, string dir)
    {
        double acumulado = 0;
        bool correcto = true;
        int pasadas = 0;
        string ruta = Path.Combine(dir, "datos.bin");

        for (int i = 0; i < repeticiones; i++)
        {
            int[] datos = Preparar(baseAleatoria, cond);
            long sumaEsperada = Suma(datos);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (alg.EsExterno)
            {
                OrdenamientoExterno.EscribirArchivo(ruta, datos);

                var reloj = Stopwatch.StartNew();
                pasadas = alg.EjecutarExterno(ruta);
                reloj.Stop();
                acumulado += reloj.Elapsed.TotalMilliseconds;

                int[] salida = OrdenamientoExterno.LeerArchivo(ruta);
                correcto &= salida.Length == datos.Length && EstaOrdenado(salida) && Suma(salida) == sumaEsperada;
            }
            else
            {
                var reloj = Stopwatch.StartNew();
                alg.EjecutarInterno(datos);
                reloj.Stop();
                acumulado += reloj.Elapsed.TotalMilliseconds;

                correcto &= EstaOrdenado(datos) && Suma(datos) == sumaEsperada;
            }
        }

        return new Resultado
        {
            Algoritmo = alg.Nombre,
            Tipo = alg.EsExterno ? "Externo" : "Interno",
            N = baseAleatoria.Length,
            Condicion = cond,
            Milisegundos = acumulado / repeticiones,
            Repeticiones = repeticiones,
            Pasadas = pasadas,
            Correcto = correcto
        };
    }
}
