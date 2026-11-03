using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using OrdenamientoBench.Banco;

namespace OrdenamientoBench.UI;

public sealed class FormPrincipal : Form
{
    private static readonly int[] TamanosDisponibles = { 10000, 50000, 100000 };

    private readonly CheckedListBox _lstAlgoritmos = new CheckedListBox();
    private readonly CheckedListBox _lstTamanos = new CheckedListBox();
    private readonly NumericUpDown _numRepeticiones = new NumericUpDown();
    private readonly NumericUpDown _numSemilla = new NumericUpDown();
    private readonly Button _btnEjecutar = new Button();
    private readonly Button _btnCancelar = new Button();
    private readonly Button _btnExportar = new Button();
    private readonly ProgressBar _barra = new ProgressBar();
    private readonly Label _lblEstado = new Label();
    private readonly DataGridView _gridDetalle = new DataGridView();
    private readonly DataGridView _gridTabla = new DataGridView();
    private readonly ComboBox _cmbCondicion = new ComboBox();
    private readonly CheckBox _chkLog = new CheckBox();
    private readonly GraficaControl _grafica = new GraficaControl();

    private readonly List<Resultado> _resultados = new List<Resultado>();
    private CancellationTokenSource _cts;

    public FormPrincipal()
    {
        Text = "Banco de pruebas de algoritmos de ordenamiento — Unidad 5";
        Width = 1200;
        Height = 760;
        MinimumSize = new Size(960, 620);
        StartPosition = FormStartPosition.CenterScreen;

        ConstruirInterfaz();
        FormClosing += (s, e) => _cts?.Cancel();
    }

    // ------------------------------------------------------------------
    // Construcción de la interfaz
    // ------------------------------------------------------------------
    private void ConstruirInterfaz()
    {
        // --- Panel superior: opciones ---
        var panelSuperior = new Panel { Dock = DockStyle.Top, Height = 185 };

        var grpAlgoritmos = new GroupBox { Text = "Algoritmos", Left = 10, Top = 8, Width = 270, Height = 168 };
        _lstAlgoritmos.Dock = DockStyle.Fill;
        _lstAlgoritmos.CheckOnClick = true;
        foreach (Algoritmo a in Catalogo.Todos)
            _lstAlgoritmos.Items.Add(a.Nombre + (a.EsExterno ? "  (externo)" : "  (interno)"), true);
        grpAlgoritmos.Controls.Add(_lstAlgoritmos);

        var grpTamanos = new GroupBox { Text = "Tamaños (registros)", Left = 290, Top = 8, Width = 170, Height = 168 };
        _lstTamanos.Dock = DockStyle.Fill;
        _lstTamanos.CheckOnClick = true;
        foreach (int n in TamanosDisponibles)
            _lstTamanos.Items.Add(n.ToString("N0"), true);
        grpTamanos.Controls.Add(_lstTamanos);

        var lblRepeticiones = new Label { Text = "Repeticiones por prueba:", Left = 480, Top = 14, AutoSize = true };
        _numRepeticiones.Left = 480;
        _numRepeticiones.Top = 36;
        _numRepeticiones.Width = 80;
        _numRepeticiones.Minimum = 1;
        _numRepeticiones.Maximum = 20;
        _numRepeticiones.Value = 1;

        var lblSemilla = new Label { Text = "Semilla aleatoria:", Left = 480, Top = 70, AutoSize = true };
        _numSemilla.Left = 480;
        _numSemilla.Top = 92;
        _numSemilla.Width = 120;
        _numSemilla.Minimum = 0;
        _numSemilla.Maximum = int.MaxValue;
        _numSemilla.Value = 12345;

        _btnEjecutar.Text = "Ejecutar pruebas";
        _btnEjecutar.Left = 480;
        _btnEjecutar.Top = 130;
        _btnEjecutar.Width = 140;
        _btnEjecutar.Height = 38;
        _btnEjecutar.Click += BtnEjecutar_Click;

        _btnCancelar.Text = "Cancelar";
        _btnCancelar.Left = 630;
        _btnCancelar.Top = 130;
        _btnCancelar.Width = 100;
        _btnCancelar.Height = 38;
        _btnCancelar.Enabled = false;
        _btnCancelar.Click += (s, e) =>
        {
            _cts?.Cancel();
            _lblEstado.Text = "Cancelando al terminar la prueba actual...";
        };

        _btnExportar.Text = "Exportar resultados...";
        _btnExportar.Left = 740;
        _btnExportar.Top = 130;
        _btnExportar.Width = 160;
        _btnExportar.Height = 38;
        _btnExportar.Click += BtnExportar_Click;

        var lblNotas = new Label
        {
            Left = 630,
            Top = 14,
            Width = 540,
            Height = 100,
            Text = "Cada algoritmo se prueba con datos aleatorios, ya ordenados y en orden inverso.\r\n" +
                   "Los métodos externos escriben los datos en un archivo temporal y lo ordenan con archivos auxiliares.\r\n" +
                   "Burbuja con 100,000 registros puede tardar varios minutos (no se puede interrumpir a media prueba).\r\n" +
                   "Cierra otros programas pesados mientras mides para obtener tiempos más estables."
        };

        panelSuperior.Controls.AddRange(new Control[]
        {
            grpAlgoritmos, grpTamanos, lblRepeticiones, _numRepeticiones, lblSemilla, _numSemilla,
            _btnEjecutar, _btnCancelar, _btnExportar, lblNotas
        });

        // --- Panel inferior: progreso ---
        var panelInferior = new Panel { Dock = DockStyle.Bottom, Height = 30 };
        _barra.Dock = DockStyle.Left;
        _barra.Width = 260;
        _lblEstado.Dock = DockStyle.Fill;
        _lblEstado.TextAlign = ContentAlignment.MiddleLeft;
        _lblEstado.Padding = new Padding(8, 0, 0, 0);
        _lblEstado.Text = "Listo.";
        panelInferior.Controls.Add(_lblEstado);
        panelInferior.Controls.Add(_barra);

        // --- Pestañas ---
        var tabs = new TabControl { Dock = DockStyle.Fill };

        var tabDetalle = new TabPage("Resultados detallados");
        ConfigurarGrid(_gridDetalle);
        _gridDetalle.Columns.Add("alg", "Algoritmo");
        _gridDetalle.Columns.Add("tipo", "Tipo");
        _gridDetalle.Columns.Add("n", "Registros (n)");
        _gridDetalle.Columns.Add("cond", "Orden de los datos");
        _gridDetalle.Columns.Add("ms", "Tiempo promedio (ms)");
        _gridDetalle.Columns.Add("rep", "Repeticiones");
        _gridDetalle.Columns.Add("pas", "Pasadas (externos)");
        _gridDetalle.Columns.Add("ver", "Verificación");
        tabDetalle.Controls.Add(_gridDetalle);

        var tabTabla = new TabPage("Tabla comparativa");
        ConfigurarGrid(_gridTabla);
        _gridTabla.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _gridTabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _gridTabla.ColumnHeadersHeight = 46;
        tabTabla.Controls.Add(_gridTabla);

        var tabGraficas = new TabPage("Gráficas");
        var barraGrafica = new Panel { Dock = DockStyle.Top, Height = 40 };
        var lblCondicion = new Label { Text = "Orden de los datos:", Left = 10, Top = 12, AutoSize = true };
        _cmbCondicion.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbCondicion.Left = 140;
        _cmbCondicion.Top = 8;
        _cmbCondicion.Width = 140;
        _cmbCondicion.Items.AddRange(new object[] { "Aleatorio", "Ordenado", "Inverso" });
        _cmbCondicion.SelectedIndex = 0;
        _chkLog.Text = "Escala logarítmica";
        _chkLog.Left = 300;
        _chkLog.Top = 10;
        _chkLog.AutoSize = true;
        _chkLog.Checked = true;
        barraGrafica.Controls.AddRange(new Control[] { lblCondicion, _cmbCondicion, _chkLog });

        _grafica.Dock = DockStyle.Fill;
        _grafica.Resultados = _resultados;
        _grafica.Condicion = Condicion.Aleatorio;
        _grafica.EscalaLogaritmica = true;

        _cmbCondicion.SelectedIndexChanged += (s, e) =>
        {
            _grafica.Condicion = (Condicion)_cmbCondicion.SelectedIndex;
            _grafica.Invalidate();
        };
        _chkLog.CheckedChanged += (s, e) =>
        {
            _grafica.EscalaLogaritmica = _chkLog.Checked;
            _grafica.Invalidate();
        };

        tabGraficas.Controls.Add(_grafica);
        tabGraficas.Controls.Add(barraGrafica);

        tabs.TabPages.Add(tabDetalle);
        tabs.TabPages.Add(tabTabla);
        tabs.TabPages.Add(tabGraficas);

        // El control que llena el espacio se agrega primero.
        Controls.Add(tabs);
        Controls.Add(panelInferior);
        Controls.Add(panelSuperior);
    }

    private static void ConfigurarGrid(DataGridView g)
    {
        g.Dock = DockStyle.Fill;
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.RowHeadersVisible = false;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.BackgroundColor = SystemColors.Window;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    // ------------------------------------------------------------------
    // Ejecución del banco de pruebas
    // ------------------------------------------------------------------
    private async void BtnEjecutar_Click(object sender, EventArgs e)
    {
        var algoritmos = new List<Algoritmo>();
        for (int i = 0; i < _lstAlgoritmos.Items.Count; i++)
        {
            if (_lstAlgoritmos.GetItemChecked(i))
                algoritmos.Add(Catalogo.Todos[i]);
        }

        var tamanos = new List<int>();
        for (int i = 0; i < _lstTamanos.Items.Count; i++)
        {
            if (_lstTamanos.GetItemChecked(i))
                tamanos.Add(TamanosDisponibles[i]);
        }

        if (algoritmos.Count == 0 || tamanos.Count == 0)
        {
            MessageBox.Show("Selecciona al menos un algoritmo y un tamaño.", "Faltan datos",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (tamanos.Contains(100000) && algoritmos.Any(a => a.Nombre == "Burbuja"))
        {
            DialogResult respuesta = MessageBox.Show(
                "Burbuja con 100,000 registros puede tardar varios minutos. ¿Quieres continuar?",
                "Prueba larga", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (respuesta != DialogResult.Yes)
                return;
        }

        _resultados.Clear();
        _gridDetalle.Rows.Clear();
        RefrescarTabla();
        _grafica.Invalidate();

        int repeticiones = (int)_numRepeticiones.Value;
        int semilla = (int)_numSemilla.Value;

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        EstablecerEjecutando(true);

        var banco = new BancoDePruebas();

        try
        {
            await Task.Run(() => banco.Ejecutar(
                algoritmos, tamanos, repeticiones, semilla, token,
                r => EnInterfaz(() => AlRecibirResultado(r)),
                (actual, total, texto) => EnInterfaz(() => ActualizarProgreso(actual, total, texto))));

            _lblEstado.Text = "Terminado. Ya puedes ver la tabla y las gráficas, o exportar los resultados.";
        }
        catch (OperationCanceledException)
        {
            _lblEstado.Text = "Cancelado. Se conservan los resultados obtenidos hasta ahora.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error durante las pruebas", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblEstado.Text = "Error.";
        }
        finally
        {
            EstablecerEjecutando(false);
        }
    }

    private void EnInterfaz(Action accion)
    {
        if (!IsDisposed && IsHandleCreated)
            BeginInvoke(accion);
    }

    private void EstablecerEjecutando(bool ejecutando)
    {
        _btnEjecutar.Enabled = !ejecutando;
        _btnCancelar.Enabled = ejecutando;
        _btnExportar.Enabled = !ejecutando;
        _lstAlgoritmos.Enabled = !ejecutando;
        _lstTamanos.Enabled = !ejecutando;
        _numRepeticiones.Enabled = !ejecutando;
        _numSemilla.Enabled = !ejecutando;
    }

    private void ActualizarProgreso(int actual, int total, string texto)
    {
        _barra.Maximum = Math.Max(total, 1);
        _barra.Value = Math.Min(actual, _barra.Maximum);
        _lblEstado.Text = $"Ejecutando ({actual}/{total}): {texto}";
    }

    private void AlRecibirResultado(Resultado r)
    {
        _resultados.Add(r);
        _gridDetalle.Rows.Add(
            r.Algoritmo,
            r.Tipo,
            r.N.ToString("N0"),
            r.Condicion.ToString(),
            r.Milisegundos.ToString("N2"),
            r.Repeticiones,
            r.Pasadas > 0 ? r.Pasadas.ToString() : "-",
            r.Correcto ? "OK" : "ERROR");

        RefrescarTabla();
        _grafica.Invalidate();
    }

    // ------------------------------------------------------------------
    // Tabla comparativa: filas = algoritmos, columnas = condición × tamaño
    // ------------------------------------------------------------------
    private void RefrescarTabla()
    {
        _gridTabla.Columns.Clear();
        _gridTabla.Rows.Clear();
        if (_resultados.Count == 0)
            return;

        List<int> tamanos = _resultados.Select(x => x.N).Distinct().OrderBy(x => x).ToList();
        Condicion[] condiciones = Enum.GetValues<Condicion>();

        _gridTabla.Columns.Add("alg", "Algoritmo (tiempo en ms)");
        foreach (Condicion c in condiciones)
        {
            foreach (int n in tamanos)
                _gridTabla.Columns.Add(c + "_" + n, c + "\n" + n.ToString("N0"));
        }

        foreach (Algoritmo alg in Catalogo.Todos)
        {
            if (!_resultados.Any(x => x.Algoritmo == alg.Nombre))
                continue;

            var celdas = new List<object> { alg.Nombre };
            foreach (Condicion c in condiciones)
            {
                foreach (int n in tamanos)
                {
                    Resultado res = _resultados.FirstOrDefault(x => x.Algoritmo == alg.Nombre && x.N == n && x.Condicion == c);
                    celdas.Add(res == null ? "" : res.Milisegundos.ToString("N2") + (res.Correcto ? "" : " (!)"));
                }
            }
            _gridTabla.Rows.Add(celdas.ToArray());
        }
    }

    // ------------------------------------------------------------------
    // Exportación: CSV + una imagen PNG por cada condición
    // ------------------------------------------------------------------
    private void BtnExportar_Click(object sender, EventArgs e)
    {
        if (_resultados.Count == 0)
        {
            MessageBox.Show("Primero ejecuta las pruebas.", "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialogo = new FolderBrowserDialog
        {
            Description = "Elige la carpeta donde se guardarán el CSV y las gráficas (PNG)."
        };
        if (dialogo.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            string carpeta = dialogo.SelectedPath;
            File.WriteAllText(Path.Combine(carpeta, "resultados.csv"), ConstruirCsv(), new UTF8Encoding(true));

            foreach (Condicion c in Enum.GetValues<Condicion>())
            {
                using Bitmap imagen = GraficaTiempos.CrearImagen(1100, 650, _resultados, c, _chkLog.Checked);
                imagen.Save(Path.Combine(carpeta, "grafica_" + c.ToString().ToLower() + ".png"), ImageFormat.Png);
            }

            MessageBox.Show("Se guardaron resultados.csv y 3 gráficas PNG en:\n" + carpeta, "Exportación lista",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string ConstruirCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Algoritmo,Tipo,N,Orden,Milisegundos,Repeticiones,Pasadas,Verificacion");
        foreach (Resultado r in _resultados)
        {
            sb.AppendLine(string.Join(",", new object[]
            {
                r.Algoritmo,
                r.Tipo,
                r.N,
                r.Condicion,
                r.Milisegundos.ToString("F3", CultureInfo.InvariantCulture),
                r.Repeticiones,
                r.Pasadas,
                r.Correcto ? "OK" : "ERROR"
            }));
        }
        return sb.ToString();
    }
}
