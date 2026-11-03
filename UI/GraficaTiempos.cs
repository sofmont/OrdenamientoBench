using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using OrdenamientoBench.Banco;

namespace OrdenamientoBench.UI;

/// <summary>Dibuja la gráfica de líneas "tamaño de los datos vs. tiempo" para una condición.</summary>
public static class GraficaTiempos
{
    private static readonly Color[] Paleta =
    {
        Color.FromArgb(31, 119, 180),
        Color.FromArgb(214, 39, 40),
        Color.FromArgb(44, 160, 44),
        Color.FromArgb(148, 103, 189),
        Color.FromArgb(255, 127, 14),
        Color.FromArgb(23, 190, 207),
        Color.FromArgb(110, 110, 110)
    };

    public static Bitmap CrearImagen(int ancho, int alto, IReadOnlyList<Resultado> resultados, Condicion condicion, bool escalaLog)
    {
        var bmp = new Bitmap(ancho, alto);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            Dibujar(g, new Rectangle(0, 0, ancho, alto), resultados, condicion, escalaLog);
        }
        return bmp;
    }

    public static void Dibujar(Graphics g, Rectangle area, IReadOnlyList<Resultado> resultados, Condicion condicion, bool escalaLog)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.FillRectangle(Brushes.White, area);

        using var fuente = new Font("Segoe UI", 9f);
        using var fuenteTitulo = new Font("Segoe UI", 12f, FontStyle.Bold);

        string titulo = "Tiempo de ejecución con datos en orden " + condicion.ToString().ToLower();
        g.DrawString(titulo, fuenteTitulo, Brushes.Black, area.Left + 12, area.Top + 8);

        List<Resultado> datos = resultados.Where(r => r.Condicion == condicion).ToList();
        if (datos.Count == 0)
        {
            g.DrawString("Sin datos. Ejecuta el banco de pruebas.", fuente, Brushes.Gray, area.Left + 80, area.Top + 80);
            return;
        }

        var plot = new Rectangle(area.Left + 80, area.Top + 50, area.Width - 80 - 200, area.Height - 50 - 60);
        if (plot.Width < 60 || plot.Height < 60)
            return;

        List<int> tamanos = datos.Select(r => r.N).Distinct().OrderBy(n => n).ToList();
        List<string> nombres = datos.Select(r => r.Algoritmo).Distinct().ToList();

        double minMs = datos.Min(r => r.Milisegundos);
        double maxMs = datos.Max(r => r.Milisegundos);

        double yMin;
        double yMax;
        if (escalaLog)
        {
            yMin = Math.Floor(Math.Log10(Math.Max(minMs, 0.01)));
            yMax = Math.Ceiling(Math.Log10(Math.Max(maxMs, 0.01)));
            if (yMax <= yMin)
                yMax = yMin + 1;
        }
        else
        {
            yMin = 0;
            yMax = maxMs <= 0 ? 1 : maxMs * 1.05;
        }

        float PosY(double ms)
        {
            double t = escalaLog
                ? (Math.Log10(Math.Max(ms, 0.01)) - yMin) / (yMax - yMin)
                : (ms - yMin) / (yMax - yMin);
            return (float)(plot.Bottom - t * plot.Height);
        }

        float PosX(int indice) => plot.Left + (indice + 0.5f) * plot.Width / tamanos.Count;

        string unidad = escalaLog ? "Tiempo (ms, escala logarítmica)" : "Tiempo (ms)";
        g.DrawString(unidad, fuente, Brushes.Black, area.Left + 12, plot.Top - 24);

        // Líneas de cuadrícula y etiquetas del eje Y
        using var lapizCuadricula = new Pen(Color.FromArgb(225, 225, 225));
        if (escalaLog)
        {
            for (double k = yMin; k <= yMax; k++)
            {
                double valor = Math.Pow(10, k);
                DibujarMarcaY(g, fuente, lapizCuadricula, plot, PosY(valor), valor.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }
        else
        {
            for (int i = 0; i <= 5; i++)
            {
                double valor = yMax * i / 5;
                DibujarMarcaY(g, fuente, lapizCuadricula, plot, PosY(valor), valor.ToString("0.#", CultureInfo.InvariantCulture));
            }
        }

        // Eje X: un punto por tamaño
        for (int i = 0; i < tamanos.Count; i++)
        {
            float x = PosX(i);
            g.DrawLine(lapizCuadricula, x, plot.Top, x, plot.Bottom);

            string etiqueta = tamanos[i].ToString("N0", CultureInfo.InvariantCulture);
            SizeF medida = g.MeasureString(etiqueta, fuente);
            g.DrawString(etiqueta, fuente, Brushes.Black, x - medida.Width / 2, plot.Bottom + 6);
        }

        string tituloX = "Número de registros";
        SizeF medidaX = g.MeasureString(tituloX, fuente);
        g.DrawString(tituloX, fuente, Brushes.Black, plot.Left + (plot.Width - medidaX.Width) / 2, plot.Bottom + 30);

        // Ejes
        g.DrawLine(Pens.Black, plot.Left, plot.Top, plot.Left, plot.Bottom);
        g.DrawLine(Pens.Black, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        // Series y leyenda
        int leyendaX = plot.Right + 24;
        int leyendaY = plot.Top;

        for (int s = 0; s < nombres.Count; s++)
        {
            string nombre = nombres[s];
            Color color = Paleta[s % Paleta.Length];
            bool externo = datos.First(r => r.Algoritmo == nombre).Tipo == "Externo";

            var puntos = new List<PointF>();
            for (int i = 0; i < tamanos.Count; i++)
            {
                int n = tamanos[i];
                Resultado r = datos.FirstOrDefault(d => d.Algoritmo == nombre && d.N == n);
                if (r != null)
                    puntos.Add(new PointF(PosX(i), PosY(r.Milisegundos)));
            }

            using var lapiz = new Pen(color, 2f);
            lapiz.DashStyle = externo ? DashStyle.Dash : DashStyle.Solid;
            using var pincel = new SolidBrush(color);

            if (puntos.Count > 1)
                g.DrawLines(lapiz, puntos.ToArray());

            foreach (PointF p in puntos)
                g.FillEllipse(pincel, p.X - 4, p.Y - 4, 8, 8);

            int yFila = leyendaY + s * 22;
            g.DrawLine(lapiz, leyendaX, yFila + 8, leyendaX + 28, yFila + 8);
            g.FillEllipse(pincel, leyendaX + 10, yFila + 4, 8, 8);
            g.DrawString(nombre + (externo ? " (ext.)" : ""), fuente, Brushes.Black, leyendaX + 36, yFila);
        }
    }

    private static void DibujarMarcaY(Graphics g, Font fuente, Pen cuadricula, Rectangle plot, float y, string texto)
    {
        g.DrawLine(cuadricula, plot.Left, y, plot.Right, y);
        SizeF medida = g.MeasureString(texto, fuente);
        g.DrawString(texto, fuente, Brushes.Black, plot.Left - medida.Width - 4, y - medida.Height / 2);
    }
}

/// <summary>Control de Windows Forms que muestra la gráfica.</summary>
public sealed class GraficaControl : Control
{
    public IReadOnlyList<Resultado> Resultados { get; set; } = new List<Resultado>();
    public Condicion Condicion { get; set; } = Condicion.Aleatorio;
    public bool EscalaLogaritmica { get; set; } = true;

    public GraficaControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        GraficaTiempos.Dibujar(e.Graphics, ClientRectangle, Resultados, Condicion, EscalaLogaritmica);
    }
}
