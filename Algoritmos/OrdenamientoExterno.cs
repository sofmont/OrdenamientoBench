namespace OrdenamientoBench.Algoritmos;

/// <summary>
/// Métodos de ordenamiento externos: los registros (enteros de 4 bytes) viven en un archivo
/// binario y solo se leen y escriben de forma secuencial. En memoria se mantienen unos pocos
/// valores y los buffers de los archivos. Los archivos auxiliares se crean junto al archivo
/// original (con extensión .f1, .f2, .n1, .n2, .a, .b) y se borran al terminar.
/// </summary>
public static class OrdenamientoExterno
{
    private const int TamanoBuffer = 1 << 16; // 64 KB

    // ------------------------------------------------------------------
    // Utilidades de archivos
    // ------------------------------------------------------------------
    private static BinaryReader AbrirLectura(string ruta) =>
        new BinaryReader(new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, TamanoBuffer));

    private static BinaryWriter AbrirEscritura(string ruta) =>
        new BinaryWriter(new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None, TamanoBuffer));

    public static long ContarRegistros(string ruta) => new FileInfo(ruta).Length / sizeof(int);

    public static void EscribirArchivo(string ruta, int[] datos)
    {
        using var w = AbrirEscritura(ruta);
        foreach (int x in datos)
            w.Write(x);
    }

    public static int[] LeerArchivo(string ruta)
    {
        using var r = AbrirLectura(ruta);
        int n = (int)(r.BaseStream.Length / sizeof(int));
        int[] datos = new int[n];
        for (int i = 0; i < n; i++)
            datos[i] = r.ReadInt32();
        return datos;
    }

    private static void Borrar(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
                File.Delete(ruta);
        }
        catch (IOException)
        {
            // Si el archivo temporal no se puede borrar ahora, no afecta el resultado.
        }
    }

    /// <summary>Lector secuencial con un registro de anticipación (Actual) y bandera de fin de archivo.</summary>
    private sealed class Lector : IDisposable
    {
        private readonly BinaryReader _lector;
        private readonly long _total;
        private long _leidos;

        public int Actual;
        public bool Fin;

        public Lector(string ruta)
        {
            _lector = AbrirLectura(ruta);
            _total = _lector.BaseStream.Length / sizeof(int);
            Avanzar();
        }

        public void Avanzar()
        {
            if (_leidos >= _total)
            {
                Fin = true;
                return;
            }
            Actual = _lector.ReadInt32();
            _leidos++;
        }

        public void Dispose() => _lector.Dispose();
    }

    // ------------------------------------------------------------------
    // 1) Intercalación: une dos archivos YA ORDENADOS en uno solo ordenado.
    //    Tiempo O(n + m); memoria O(1); estable (en empates sale primero el archivo A).
    // ------------------------------------------------------------------
    public static void Intercalacion(string rutaA, string rutaB, string rutaSalida)
    {
        using var a = new Lector(rutaA);
        using var b = new Lector(rutaB);
        using var w = AbrirEscritura(rutaSalida);

        while (!a.Fin && !b.Fin)
        {
            if (a.Actual <= b.Actual)
            {
                w.Write(a.Actual);
                a.Avanzar();
            }
            else
            {
                w.Write(b.Actual);
                b.Avanzar();
            }
        }

        while (!a.Fin)
        {
            w.Write(a.Actual);
            a.Avanzar();
        }

        while (!b.Fin)
        {
            w.Write(b.Actual);
            b.Avanzar();
        }
    }

    /// <summary>
    /// Ordena un archivo con intercalación: lo divide en dos mitades, ordena cada mitad por separado
    /// en memoria (Quicksort) hacia un archivo auxiliar y luego intercala ambos archivos sobre el original.
    /// En memoria solo cabe la mitad de los datos a la vez. Devuelve el número de pasadas de intercalación.
    /// </summary>
    public static int IntercalacionDosBloques(string ruta)
    {
        long n = ContarRegistros(ruta);
        if (n < 2)
            return 0;

        string archivoA = ruta + ".a";
        string archivoB = ruta + ".b";

        try
        {
            long mitad = n / 2;

            using (var r = AbrirLectura(ruta))
            {
                int[] bloque = LeerBloque(r, mitad);
                OrdenamientoInterno.Quicksort(bloque);
                EscribirArchivo(archivoA, bloque);

                bloque = LeerBloque(r, n - mitad);
                OrdenamientoInterno.Quicksort(bloque);
                EscribirArchivo(archivoB, bloque);
            }

            Intercalacion(archivoA, archivoB, ruta);
            return 1;
        }
        finally
        {
            Borrar(archivoA);
            Borrar(archivoB);
        }
    }

    private static int[] LeerBloque(BinaryReader r, long cantidad)
    {
        int[] bloque = new int[cantidad];
        for (long i = 0; i < cantidad; i++)
            bloque[i] = r.ReadInt32();
        return bloque;
    }

    // ------------------------------------------------------------------
    // 2) Mezcla directa (Straight Merge)
    //    Distribuye bloques de longitud 1, 2, 4, 8... en dos archivos y los mezcla por pares.
    //    O(n log n) en todos los casos; devuelve el número de pasadas (≈ log2 n).
    // ------------------------------------------------------------------
    public static int MezclaDirecta(string ruta)
    {
        long n = ContarRegistros(ruta);
        if (n < 2)
            return 0;

        string f1 = ruta + ".f1";
        string f2 = ruta + ".f2";
        int pasadas = 0;

        try
        {
            for (long tam = 1; tam < n; tam *= 2)
            {
                DistribuirBloques(ruta, f1, f2, tam, out long c1, out long c2);
                MezclarBloques(f1, f2, ruta, tam, c1, c2);
                pasadas++;
            }
        }
        finally
        {
            Borrar(f1);
            Borrar(f2);
        }

        return pasadas;
    }

    private static void DistribuirBloques(string origen, string f1, string f2, long tam, out long c1, out long c2)
    {
        c1 = 0;
        c2 = 0;

        using var r = AbrirLectura(origen);
        using var w1 = AbrirEscritura(f1);
        using var w2 = AbrirEscritura(f2);

        long n = r.BaseStream.Length / sizeof(int);
        long leidos = 0;
        bool haciaPrimero = true;

        while (leidos < n)
        {
            long bloque = Math.Min(tam, n - leidos);
            BinaryWriter destino = haciaPrimero ? w1 : w2;

            for (long i = 0; i < bloque; i++)
                destino.Write(r.ReadInt32());

            if (haciaPrimero)
                c1 += bloque;
            else
                c2 += bloque;

            leidos += bloque;
            haciaPrimero = !haciaPrimero;
        }
    }

    private static void MezclarBloques(string f1, string f2, string destino, long tam, long c1, long c2)
    {
        using var r1 = AbrirLectura(f1);
        using var r2 = AbrirLectura(f2);
        using var w = AbrirEscritura(destino);

        while (c1 > 0 || c2 > 0)
        {
            long n1 = Math.Min(tam, c1);
            long n2 = Math.Min(tam, c2);
            c1 -= n1;
            c2 -= n2;

            int x = 0;
            int y = 0;
            if (n1 > 0) x = r1.ReadInt32();
            if (n2 > 0) y = r2.ReadInt32();

            while (n1 > 0 && n2 > 0)
            {
                if (x <= y)
                {
                    w.Write(x);
                    if (--n1 > 0) x = r1.ReadInt32();
                }
                else
                {
                    w.Write(y);
                    if (--n2 > 0) y = r2.ReadInt32();
                }
            }

            while (n1 > 0)
            {
                w.Write(x);
                if (--n1 > 0) x = r1.ReadInt32();
            }

            while (n2 > 0)
            {
                w.Write(y);
                if (--n2 > 0) y = r2.ReadInt32();
            }
        }
    }

    // ------------------------------------------------------------------
    // 3) Mezcla natural (Natural Merge)
    //    Aprovecha las corridas (tramos ya ordenados) que existan en el archivo.
    //    Mejor caso O(n) (una sola corrida); promedio y peor O(n log n).
    //    Devuelve el número de pasadas de mezcla realizadas.
    // ------------------------------------------------------------------
    public static int MezclaNatural(string ruta)
    {
        string f1 = ruta + ".n1";
        string f2 = ruta + ".n2";
        int pasadas = 0;

        try
        {
            while (true)
            {
                long corridas = DistribuirCorridas(ruta, f1, f2);
                if (corridas <= 1)
                    break; // el archivo original ya está ordenado

                MezclarCorridas(f1, f2, ruta);
                pasadas++;
            }
        }
        finally
        {
            Borrar(f1);
            Borrar(f2);
        }

        return pasadas;
    }

    /// <summary>Reparte las corridas del origen, alternándolas entre f1 y f2. Devuelve cuántas corridas había.</summary>
    private static long DistribuirCorridas(string origen, string f1, string f2)
    {
        long corridas = 0;

        using var r = new Lector(origen);
        using var w1 = AbrirEscritura(f1);
        using var w2 = AbrirEscritura(f2);

        bool haciaPrimero = true;
        bool primero = true;
        int previo = 0;

        while (!r.Fin)
        {
            int x = r.Actual;

            if (primero)
            {
                corridas = 1;
                primero = false;
            }
            else if (x < previo)
            {
                corridas++;
                haciaPrimero = !haciaPrimero;
            }

            BinaryWriter destino = haciaPrimero ? w1 : w2;
            destino.Write(x);

            previo = x;
            r.Avanzar();
        }

        return corridas;
    }

    private static void MezclarCorridas(string f1, string f2, string destino)
    {
        using var a = new Lector(f1);
        using var b = new Lector(f2);
        using var w = AbrirEscritura(destino);

        while (!a.Fin || !b.Fin)
            MezclarUnaCorrida(a, b, w);
    }

    /// <summary>Mezcla una corrida de A con una corrida de B. Una corrida termina cuando el siguiente valor es menor.</summary>
    private static void MezclarUnaCorrida(Lector a, Lector b, BinaryWriter w)
    {
        bool enA = !a.Fin;
        bool enB = !b.Fin;

        while (enA || enB)
        {
            bool tomarA = (enA && enB) ? a.Actual <= b.Actual : enA;

            if (tomarA)
            {
                int v = a.Actual;
                w.Write(v);
                a.Avanzar();
                if (a.Fin || a.Actual < v)
                    enA = false;
            }
            else
            {
                int v = b.Actual;
                w.Write(v);
                b.Avanzar();
                if (b.Fin || b.Actual < v)
                    enB = false;
            }
        }
    }
}
