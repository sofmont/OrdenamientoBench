# Banco de pruebas de algoritmos de ordenamiento (Unidad 5 · Tarea 5.2)

Aplicación de **Windows Forms (C#, .NET 8)** que implementa y compara siete métodos de ordenamiento:

| Tipo | Algoritmos |
|---|---|
| Internos (sobre arreglos en memoria) | Burbuja, Quicksort, ShellSort, Radix |
| Externos (sobre archivos binarios) | Intercalación, Mezcla directa, Mezcla natural |

Cada algoritmo se prueba con **10,000, 50,000 y 100,000** números enteros en tres condiciones:
**orden aleatorio**, **datos ya ordenados** y **orden inverso**. Los tiempos se cronometran con `Stopwatch`
(no incluyen la preparación de los datos) y, al terminar, se verifica que el resultado esté ordenado y que
no se hayan perdido ni alterado valores.

## Cómo ejecutarlo

Requisitos: Windows, [SDK de .NET 8](https://dotnet.microsoft.com/download) y, opcionalmente, Visual Studio 2022.

```
cd OrdenamientoBench
dotnet run -c Release
```

En Visual Studio: abre la carpeta o el archivo `OrdenamientoBench.csproj`, elige la configuración **Release**
y presiona Ctrl + F5 (ejecutar sin depurar). **Mide siempre en Release y sin depurador**, o los tiempos
saldrán inflados.

## Uso de la aplicación

1. Marca los algoritmos y los tamaños que quieras probar (por defecto, todos).
2. Elige las repeticiones por prueba (el tiempo mostrado es el promedio) y la semilla del generador aleatorio.
   Con la misma semilla se generan siempre los mismos datos, así tus resultados son reproducibles.
3. Presiona **Ejecutar pruebas**. Burbuja con 100,000 registros puede tardar varios minutos.
4. Revisa las pestañas **Resultados detallados**, **Tabla comparativa** y **Gráficas**
   (con escala logarítmica activable, porque los tiempos de Burbuja y Radix difieren en órdenes de magnitud).
5. **Exportar resultados...** guarda `resultados.csv` y tres gráficas PNG (una por condición) para tu reporte.

## Estructura del proyecto

```
OrdenamientoBench/
├── Program.cs
├── Algoritmos/
│   ├── OrdenamientoInterno.cs   Burbuja, Quicksort, ShellSort, Radix
│   └── OrdenamientoExterno.cs   Intercalación, Mezcla directa, Mezcla natural
├── Banco/
│   └── BancoDePruebas.cs        generación de datos, cronometraje y verificación
└── UI/
    ├── FormPrincipal.cs         ventana, tablas y exportación
    └── GraficaTiempos.cs        gráfica dibujada con GDI+
```

## Decisiones de implementación (útiles para explicarlas en el video)

- **Quicksort** usa partición de Hoare con pivote central y recurre solo sobre la mitad más pequeña. Así no se
  degrada con datos ordenados o inversos y la pila de recursión nunca pasa de unos 17 niveles con 100,000 datos.
- **ShellSort** usa la secuencia de Knuth (1, 4, 13, 40, ...).
- **Radix** es LSD en base 10 y solo admite enteros no negativos (el generador produce valores de 0 a 999,999).
- **Los métodos externos** leen y escriben enteros de 4 bytes de forma secuencial en un archivo temporal y usan archivos
  auxiliares (`.f1/.f2`, `.n1/.n2`, `.a/.b`) que se borran al terminar.
  - *Intercalación*: divide el archivo en dos mitades, ordena cada una en memoria con Quicksort hacia un archivo
    auxiliar y las intercala sobre el original. Su tiempo incluye ordenar las dos mitades.
  - *Mezcla directa*: bloques de 1, 2, 4, 8... registros, aproximadamente log2(n) pasadas sin importar el orden inicial.
  - *Mezcla natural*: detecta las corridas ya ordenadas. Con datos ordenados hace una sola lectura.
- El tiempo medido es **tiempo de reloj** del algoritmo. En los externos incluye el acceso a disco, así que no son
  comparables 1 a 1 con los internos; compáralos entre sí y contra su teoría.

## Qué esperar (para contrastar con tus resultados)

- Burbuja crece de forma cuadrática: al multiplicar n por 10, el tiempo se multiplica por unas 100 veces.
  Con datos ya ordenados es casi instantánea (mejor caso O(n)).
- Quicksort, ShellSort y Radix terminan en milisegundos incluso con 100,000 datos.
- Mezcla directa tarda casi lo mismo con cualquier orden; Mezcla natural es mucho más rápida con datos ordenados.
- Tus números exactos dependerán de tu computadora y del disco.

## Entregables de la tarea

- **GitHub**: sube esta carpeta (el `.gitignore` ya excluye `bin/`, `obj/` y `.vs/`).
- **Reporte**: usa `docs/Plantilla_reporte.md` con las tablas y gráficas que exporta la aplicación.
- **Video en YouTube**: guion sugerido en `docs/Plantilla_reporte.md`.
