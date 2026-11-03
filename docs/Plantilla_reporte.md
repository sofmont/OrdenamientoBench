# Reporte de práctica — Tarea 5.2: Banco de pruebas de algoritmos de ordenamiento

**Alumno:** ______________________  **Materia:** ______________________  **Fecha:** ____________

> Completa los espacios con TUS resultados. No copies números de ejemplo: ejecuta la aplicación en tu computadora.

## 1. Objetivo
Implementar los algoritmos de ordenamiento internos (Burbuja, Quicksort, ShellSort, Radix) y externos
(Intercalación, Mezcla directa, Mezcla natural) y comparar su tiempo de ejecución con 10,000, 50,000 y 100,000 registros
en orden aleatorio, ordenado e inverso.

## 2. Entorno de pruebas
- Procesador: ______________  Memoria RAM: ______  Disco (SSD/HDD): ______
- Sistema operativo: ______________  Versión de .NET: 8  Configuración: Release, sin depurador
- Semilla aleatoria: ______  Repeticiones por prueba: ______

## 3. Metodología
Describe en un párrafo: generación de los datos (enteros de 0 a 999,999 con semilla fija), cronometraje con `Stopwatch`,
datos preparados fuera del cronómetro, verificación del resultado y, en los externos, uso de archivos temporales.

## 4. Resultados
Pega aquí las tablas de la pestaña **Tabla comparativa** (una por condición o la tabla completa) y las
tres gráficas exportadas (`grafica_aleatorio.png`, `grafica_ordenado.png`, `grafica_inverso.png`).

| Algoritmo | Aleatorio 10,000 | Aleatorio 50,000 | Aleatorio 100,000 | Ordenado 10,000 | ... | Inverso 100,000 |
|---|---|---|---|---|---|---|
| Burbuja | | | | | | |
| Quicksort | | | | | | |
| ShellSort | | | | | | |
| Radix | | | | | | |
| Intercalación | | | | | | |
| Mezcla directa | | | | | | |
| Mezcla natural | | | | | | |

## 5. Análisis
Responde con base en tus datos:
1. ¿Cómo cambia el tiempo de Burbuja al pasar de 10,000 a 100,000 registros? ¿Coincide con O(n²)?
2. ¿Qué pasó con Burbuja y con Mezcla natural cuando los datos ya estaban ordenados? ¿Por qué?
3. ¿Qué algoritmo interno fue más rápido en cada condición y por qué?
4. ¿Cómo se comportó Quicksort con datos ordenados e inversos? ¿Qué papel juega la elección del pivote?
5. ¿Por qué Mezcla directa tarda casi lo mismo con cualquier orden inicial?
6. ¿Por qué los métodos externos son más lentos que los internos con estos tamaños si caben en memoria? ¿Cuándo sí valen la pena?
7. ¿Qué diferencias hay entre tu medición y la complejidad teórica del cuadro comparativo de la Tarea 5.1?

## 6. Conclusiones
(Un párrafo propio.)

## 7. Enlaces
- Repositorio de GitHub: ______________________
- Video en YouTube: ______________________

---

## Guion sugerido para los videos (pantalla + cámara web en la esquina inferior derecha)

**Video de la Tarea 5.1 (5–8 min)**
1. Presentación: nombre, materia y tema.
2. Diferencia entre ordenamiento interno y externo.
3. Recorre el cuadro comparativo fila por fila: mejor, promedio y peor caso, espacio y estabilidad.
4. Explica cuándo usar cada algoritmo (sección de discusión crítica).
5. Cierre con conclusiones.

**Video de la Tarea 5.2 (8–12 min)**
1. Muestra el repositorio de GitHub y la estructura del proyecto.
2. Explica brevemente el código de cada algoritmo (muestra al menos Quicksort y una mezcla externa).
3. Ejecuta la aplicación en Release y muestra el avance de las pruebas.
4. Muestra la tabla comparativa y las gráficas; comenta los resultados con tus palabras.
5. Responde las preguntas de análisis y cierra con conclusiones.

Recuerda subir ambos videos al **mismo canal** de YouTube y dejar los enlaces como públicos.
