namespace OrdenamientoBench.Algoritmos;

/// <summary>
/// Métodos de ordenamiento internos: trabajan sobre un arreglo completo en memoria principal.
/// Todos ordenan de menor a mayor y modifican el arreglo recibido.
/// </summary>
public static class OrdenamientoInterno
{
    // ------------------------------------------------------------------
    // Burbuja (Bubble Sort)
    // Mejor caso O(n) gracias a la bandera; promedio y peor O(n^2); espacio O(1); estable.
    // ------------------------------------------------------------------
    public static void Burbuja(int[] a)
    {
        int n = a.Length;
        bool huboIntercambio = true;

        for (int pasada = 0; pasada < n - 1 && huboIntercambio; pasada++)
        {
            huboIntercambio = false;
            for (int j = 0; j < n - 1 - pasada; j++)
            {
                if (a[j] > a[j + 1])
                {
                    (a[j], a[j + 1]) = (a[j + 1], a[j]);
                    huboIntercambio = true;
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Quicksort (partición de Hoare con pivote central)
    // Mejor y promedio O(n log n); peor O(n^2); espacio O(log n); no estable.
    // Se recurre sobre la mitad más pequeña y se itera sobre la otra, así la
    // profundidad de la pila nunca pasa de log2(n), incluso con 100,000 datos.
    // El pivote central evita el peor caso con datos ordenados o inversos.
    // ------------------------------------------------------------------
    public static void Quicksort(int[] a)
    {
        if (a.Length > 1)
            QuicksortRec(a, 0, a.Length - 1);
    }

    private static void QuicksortRec(int[] a, int izq, int der)
    {
        while (izq < der)
        {
            int p = Particionar(a, izq, der);

            // Subarreglos: [izq..p] y [p+1..der]
            if (p - izq + 1 < der - p)
            {
                QuicksortRec(a, izq, p);
                izq = p + 1;
            }
            else
            {
                QuicksortRec(a, p + 1, der);
                der = p;
            }
        }
    }

    private static int Particionar(int[] a, int izq, int der)
    {
        int pivote = a[izq + (der - izq) / 2];
        int i = izq - 1;
        int j = der + 1;

        while (true)
        {
            do { i++; } while (a[i] < pivote);
            do { j--; } while (a[j] > pivote);

            if (i >= j)
                return j;

            (a[i], a[j]) = (a[j], a[i]);
        }
    }

    // ------------------------------------------------------------------
    // ShellSort con la secuencia de saltos de Knuth (1, 4, 13, 40, 121, ...)
    // Aprox. O(n^(3/2)) en el peor caso con esta secuencia; mejor caso O(n log n);
    // espacio O(1); no estable.
    // ------------------------------------------------------------------
    public static void ShellSort(int[] a)
    {
        int n = a.Length;
        int salto = 1;
        while (salto < n / 3)
            salto = 3 * salto + 1;

        for (; salto >= 1; salto /= 3)
        {
            // Inserción directa con separación 'salto'
            for (int i = salto; i < n; i++)
            {
                int temp = a[i];
                int j = i;
                while (j >= salto && a[j - salto] > temp)
                {
                    a[j] = a[j - salto];
                    j -= salto;
                }
                a[j] = temp;
            }
        }
    }

    // ------------------------------------------------------------------
    // Radix Sort LSD en base 10 (para enteros no negativos)
    // O(d * (n + k)) en todos los casos, con k = 10; espacio O(n + k); estable.
    // ------------------------------------------------------------------
    public static void RadixSort(int[] a)
    {
        int n = a.Length;
        if (n < 2)
            return;

        int max = a[0];
        for (int i = 0; i < n; i++)
        {
            if (a[i] < 0)
                throw new ArgumentException("Radix Sort de esta práctica solo admite enteros no negativos.");
            if (a[i] > max)
                max = a[i];
        }

        int[] salida = new int[n];
        int[] cuenta = new int[10];

        for (long exp = 1; max / exp > 0; exp *= 10)
        {
            Array.Clear(cuenta, 0, cuenta.Length);

            for (int i = 0; i < n; i++)
                cuenta[(int)((a[i] / exp) % 10)]++;

            for (int d = 1; d < 10; d++)
                cuenta[d] += cuenta[d - 1];

            // Recorrido de derecha a izquierda para conservar la estabilidad
            for (int i = n - 1; i >= 0; i--)
            {
                int digito = (int)((a[i] / exp) % 10);
                salida[--cuenta[digito]] = a[i];
            }

            Array.Copy(salida, a, n);
        }
    }
}
