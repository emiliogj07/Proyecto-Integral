using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NCalc;
using AngouriMath;
using AngouriMath.Extensions;
using ScottPlot;

using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace ProyectoIntegral
{
    public partial class MainWindow : Window
    {
        // Controla si la calculadora está trabajando en modo Definida (true) o Indefinida (false)
        private bool esIntegralDefinida = true;

        // Número de particiones para la Regla del Trapecio
        private const int PARTICIONES = 1000;

        // Configuración de la gráfica
        private const int PUNTOS_GRAFICA = 500;
        private const double GRAFICA_MIN = -10;
        private const double GRAFICA_MAX = 10;

        public MainWindow()
        {
            InitializeComponent();
            ActualizarPrevisualizacion(null, null); // Muestra la vista previa inicial al abrir la app
        }

        // =====================================================================
        // PREVISUALIZACIÓN DINÁMICA
        // =====================================================================
        private void ActualizarPrevisualizacion(object sender, TextChangedEventArgs e)
        {
            if (lblPreview == null || txtFuncion == null || txtVariable == null) return;

            string funcion = string.IsNullOrWhiteSpace(txtFuncion.Text) ? "f(x)" : txtFuncion.Text;
            string variable = string.IsNullOrWhiteSpace(txtVariable.Text) ? "x" : txtVariable.Text;

            if (esIntegralDefinida && txtLimiteA != null && txtLimiteB != null)
            {
                string a = string.IsNullOrWhiteSpace(txtLimiteA.Text) ? "a" : txtLimiteA.Text;
                string b = string.IsNullOrWhiteSpace(txtLimiteB.Text) ? "b" : txtLimiteB.Text;
                lblPreview.Text = $"∫ [{a}, {b}] {funcion} d{variable}";
            }
            else
            {
                lblPreview.Text = $"∫ {funcion} d{variable}";
            }
        }

        // =====================================================================
        // CAMBIO DE MODO: INDEFINIDA
        // =====================================================================
        private void btnIndefinida_Click(object sender, RoutedEventArgs e)
        {
            esIntegralDefinida = false;

            btnIndefinida.Background = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#2B2019"));
            btnIndefinida.Foreground = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#C08457"));
            btnDefinida.Background = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#121212"));
            btnDefinida.Foreground = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#A3A3A3"));

            txtLimiteA.IsEnabled = false;
            txtLimiteB.IsEnabled = false;
            txtLimiteA.Foreground = new SolidColorBrush(MediaColors.Gray);
            txtLimiteB.Foreground = new SolidColorBrush(MediaColors.Gray);

            LimpiarSalidas();
            ActualizarPrevisualizacion(null, null);
        }

        // =====================================================================
        // CAMBIO DE MODO: DEFINIDA
        // =====================================================================
        private void btnDefinida_Click(object sender, RoutedEventArgs e)
        {
            esIntegralDefinida = true;

            btnDefinida.Background = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#2B2019"));
            btnDefinida.Foreground = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#C08457"));
            btnIndefinida.Background = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#121212"));
            btnIndefinida.Foreground = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#A3A3A3"));

            txtLimiteA.IsEnabled = true;
            txtLimiteB.IsEnabled = true;
            txtLimiteA.Foreground = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#F5F5F5"));
            txtLimiteB.Foreground = new SolidColorBrush((MediaColor)ColorConverter.ConvertFromString("#F5F5F5"));

            LimpiarSalidas();
            ActualizarPrevisualizacion(null, null);
        }

        // =====================================================================
        // LIMPIEZA DE UI
        // =====================================================================
        private void LimpiarSalidas()
        {
            lblAntiderivada.Text = "Esperando datos...";
            lblResultado.Text = "0.000000";
            lblDetalle1.Text = "La integral definida representa el área con signo, no necesariamente el área geométrica.";
            lblDetalle2.Text = "Esperando parámetros para generar el reporte de intervalos...";
            lblH.Text = "h = (b-a)/n";

            LimpiarGrafica();
        }

        private void LimpiarGrafica()
        {
            if (GraficaIntegral == null)
                return;

            GraficaIntegral.Plot.Clear();
            GraficaIntegral.Refresh();
        }

        // =====================================================================
        // BOTÓN PRINCIPAL: CALCULAR
        // =====================================================================
        private void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            LimpiarSalidas();

            // En WPF, el Text nunca es nulo, usar Trim() directamente es 100% seguro y evita errores
            string formula = txtFuncion.Text.Trim();
            string variable = string.IsNullOrWhiteSpace(txtVariable.Text) ? "x" : txtVariable.Text.Trim();

            if (string.IsNullOrWhiteSpace(formula))
            {
                MessageBox.Show("Por favor, ingresa una función matemática.", "Campo vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(variable) || variable.Length > 1 || !char.IsLetter(variable[0]))
            {
                MessageBox.Show("La variable de integración debe ser una sola letra (ej. x, t, u).", "Variable inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (esIntegralDefinida)
            {
                CalcularIntegralDefinida(formula, variable);
            }
            else
            {
                CalcularIntegralIndefinida(formula, variable);
            }
        }

        // =====================================================================
        // MOTOR 1: INTEGRAL INDEFINIDA (AngouriMath)
        // =====================================================================
        private void CalcularIntegralIndefinida(string formula, string variable)
        {
            try
            {
                // MathS.FromString es la forma segura y sin ambigüedades de parsear la función
                Entity expr = MathS.FromString(formula);
                Entity exprPreSimplificada = expr.Simplify();

                Entity antiderivada = exprPreSimplificada.Integrate(variable);
                Entity antiderivadaLimpia = antiderivada.Simplify();

                string resultadoVisual = antiderivadaLimpia.Stringize();

                if (string.IsNullOrWhiteSpace(resultadoVisual))
                {
                    throw new InvalidOperationException("El motor simbólico no devolvió un resultado interpretable.");
                }

                int idxProvided = resultadoVisual.IndexOf(" provided ", StringComparison.OrdinalIgnoreCase);
                if (idxProvided >= 0)
                {
                    resultadoVisual = resultadoVisual.Substring(0, idxProvided);
                }

                resultadoVisual = resultadoVisual.Trim();
                resultadoVisual = resultadoVisual.Replace("+-", "-");
                resultadoVisual = resultadoVisual.Replace("*", " · ");

                lblAntiderivada.Text = "F(" + variable + ") = " + resultadoVisual + " + C";
                lblResultado.Text = "N/A";

                lblDetalle1.Text = "Integración simbólica procesada por AngouriMath.";
                lblDetalle2.Text = "Se ha calculado la familia de antiderivadas agregando la constante de integración (C), ya que existen infinitas funciones cuya derivada coincide con f(" + variable + ").";
                lblH.Text = "Modo simbólico (no aplica partición numérica)";

                GenerarGrafica(
                   formula,
                   variable,
                   GRAFICA_MIN,
                   GRAFICA_MAX,
                   false
                );

            }
            catch (Exception ex)
            {
                lblAntiderivada.Text = "Error de integración";
                MessageBox.Show("La función no pudo ser integrada simbólicamente. Verifica que la sintaxis sea válida (ej. x^2, sin(x), 1/x).\n\nDetalle técnico: " + ex.Message, "Error Algebraico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================================
        // MOTOR 2: INTEGRAL DEFINIDA (NCalc + Regla del Trapecio)
        // =====================================================================
        private void CalcularIntegralDefinida(string formula, string variable)
        {
            if (!double.TryParse(txtLimiteA.Text, out double limiteA))
            {
                MessageBox.Show("Por favor, ingresa un número válido para el límite inferior [a].", "Error de límites", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtLimiteB.Text, out double limiteB))
            {
                MessageBox.Show("Por favor, ingresa un número válido para el límite superior [b].", "Error de límites", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (limiteA == limiteB)
            {
                MessageBox.Show("Los límites [a] y [b] son iguales; el área definida en un intervalo de longitud cero es 0.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                lblResultado.Text = "0.000000";
                lblAntiderivada.Text = "Evaluación Numérica (Trapecio)";
                lblDetalle1.Text = "La integral definida representa el área con signo, no necesariamente el área geométrica.";
                lblDetalle2.Text = "No se realizó partición numérica.";
                lblH.Text = "h = 0";
                return;
            }

            if (formula.Contains("^"))
            {
                MessageBox.Show("La función contiene el símbolo '^'. En el motor NCalc, '^' es el operador bitwise XOR y NO representa una potencia.\n\nPor favor, reescribe la función usando Pow(base, exponente) — por ejemplo: Pow(x, 2) — o utiliza multiplicación directa cuando sea posible (ej. x*x).", "Operador no soportado (XOR)", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int particiones = PARTICIONES;
                double h = (limiteB - limiteA) / particiones;

                // OPTIMIZACIÓN VITAL: Instanciamos NCalc UNA SOLA VEZ fuera del bucle
                var expr = new NCalc.Expression(formula);

                expr.Parameters[variable] = limiteA;
                double fa = Convert.ToDouble(expr.Evaluate());

                expr.Parameters[variable] = limiteB;
                double fb = Convert.ToDouble(expr.Evaluate());

                double suma = fa + fb;

                // Bucle limpio que solo inyecta la variable y calcula, sin saturar la memoria
                for (int i = 1; i < particiones; i++)
                {
                    double xActual = limiteA + (i * h);
                    expr.Parameters[variable] = xActual;
                    suma += 2 * Convert.ToDouble(expr.Evaluate());
                }

                double resultadoIntegral = (h / 2) * suma;

                if (double.IsNaN(resultadoIntegral) || double.IsInfinity(resultadoIntegral))
                {
                    throw new InvalidOperationException("El resultado numérico no es un valor finito. Es posible que la función tenga una discontinuidad dentro del intervalo [a, b].");
                }

                lblResultado.Text = resultadoIntegral.ToString("F6");
                if (limiteA > limiteB)
                {
                    lblDetalle1.Text =
                        $"Los límites están invertidos: se integra desde {limiteA} hasta {limiteB}. " +
                        "Por eso el resultado de la integral es negativo.";

                    lblDetalle2.Text =
                        $"La gráfica muestra la función en el intervalo [{limiteB}, {limiteA}], " +
                        "pero la integración se realiza en dirección inversa.";
                }
                else
                {
                    lblDetalle1.Text =
                        $"Se dividió el intervalo [{limiteA}, {limiteB}] en {particiones} trapecios. " +
                        "La integral definida representa el área con signo.";

                    lblDetalle2.Text =
                        $"Cálculo de la base de cada trapecio:\nh = ({limiteB} - {limiteA}) / {particiones}";
                }

                lblAntiderivada.Text = "Evaluación Numérica (Trapecio)";

                lblDetalle1.Text = $"Se dividió el intervalo [{limiteA}, {limiteB}] en {particiones} trapecios. Recuerda que la integral definida representa el área con signo, no necesariamente el área geométrica.";
                lblDetalle2.Text = $"Cálculo de la base de cada trapecio:\nh = ({limiteB} - {limiteA}) / {particiones}";
                lblH.Text = $"h ≈ {h.ToString("F6")}";

                GenerarGrafica(
                   formula,
                   variable,
                   limiteA,
                   limiteB,
                   true
                );

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al evaluar numéricamente la función. Verifica la sintaxis de NCalc (ej. sin(x), cos(x), sqrt(x), Pow(x,2)).\n\nDetalle técnico: " + ex.Message, "Error de Evaluación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerarGrafica(
        string formula,
        string variable,
        double minimoX,
        double maximoX,
        bool sombrearArea)
        {
            try
            {
                if (GraficaIntegral == null)
                    return;

                if (minimoX == maximoX)
                    return;

                // Para calcular los puntos siempre usamos el intervalo
                // de menor a mayor.
                double inicio = Math.Min(minimoX, maximoX);
                double fin = Math.Max(minimoX, maximoX);

                // ============================================================
                // CREAR PUNTOS
                // ============================================================

                double[] xs = new double[PUNTOS_GRAFICA];
                double[] ys = new double[PUNTOS_GRAFICA];

                double paso = (fin - inicio) / (PUNTOS_GRAFICA - 1);

                var expr = new NCalc.Expression(formula);

                double minimoY = double.PositiveInfinity;
                double maximoY = double.NegativeInfinity;

                // ============================================================
                // CALCULAR LOS VALORES DE LA FUNCIÓN
                // ============================================================

                for (int i = 0; i < PUNTOS_GRAFICA; i++)
                {
                    double x = inicio + (i * paso);

                    xs[i] = x;

                    try
                    {
                        expr.Parameters[variable] = x;

                        double y = Convert.ToDouble(expr.Evaluate());

                        if (double.IsNaN(y) || double.IsInfinity(y))
                        {
                            ys[i] = double.NaN;
                        }
                        else
                        {
                            ys[i] = y;

                            minimoY = Math.Min(minimoY, y);
                            maximoY = Math.Max(maximoY, y);
                        }
                    }
                    catch
                    {
                        ys[i] = double.NaN;
                    }
                }

                // ============================================================
                // VERIFICAR VALORES
                // ============================================================

                if (double.IsInfinity(minimoY) ||
                    double.IsInfinity(maximoY))
                {
                    MessageBox.Show(
                        "No se pudieron obtener valores válidos para generar la gráfica.",
                        "Error en la gráfica",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // ============================================================
                // LIMPIAR GRÁFICA
                // ============================================================

                GraficaIntegral.Plot.Clear();

                // ============================================================
                // COLORES
                // ============================================================

                GraficaIntegral.Plot.FigureBackground.Color =
                    ScottPlot.Color.FromHex("#1B1B1B");

                GraficaIntegral.Plot.DataBackground.Color =
                    ScottPlot.Color.FromHex("#121212");

                GraficaIntegral.Plot.Axes.Color(
                    ScottPlot.Color.FromHex("#A3A3A3"));

                GraficaIntegral.Plot.Grid.MajorLineColor =
                    ScottPlot.Color.FromHex("#333333");

                // ============================================================
                // AGREGAR FUNCIÓN
                // ============================================================

                var grafica = GraficaIntegral.Plot.Add.Scatter(xs, ys);

                grafica.Color =
                    ScottPlot.Color.FromHex("#C08457");

                grafica.LineWidth = 2.5f;

                grafica.MarkerSize = 0;

                // ============================================================
                // SOMBREADO
                // ============================================================

                if (sombrearArea)
                {
                    grafica.FillY = true;

                    grafica.FillYValue = 0;

                    grafica.FillYColor =
                        ScottPlot.Color.FromHex("#C08457")
                        .WithAlpha(0.20);
                }

                // ============================================================
                // RANGO DEL EJE Y
                // ============================================================

                minimoY = Math.Min(minimoY, 0);
                maximoY = Math.Max(maximoY, 0);

                double rangoY = maximoY - minimoY;

                if (rangoY == 0)
                    rangoY = 1;

                double margenY = rangoY * 0.10;

                double limiteYInferior = minimoY - margenY;
                double limiteYSuperior = maximoY + margenY;

                // ============================================================
                // LÍMITES DEL GRÁFICO
                // ============================================================

                GraficaIntegral.Plot.Axes.SetLimits(
                    inicio,
                    fin,
                    limiteYInferior,
                    limiteYSuperior);

                // ============================================================
                // TÍTULO
                // ============================================================

                if (sombrearArea)
                {
                    GraficaIntegral.Plot.Title(
                        $"f({variable})   Integral: {minimoX} → {maximoX}");
                }
                else
                {
                    GraficaIntegral.Plot.Title(
                        $"Gráfica de f({variable})");
                }

                GraficaIntegral.Plot.XLabel(variable);

                GraficaIntegral.Plot.YLabel(
                    $"f({variable})");

                // ============================================================
                // ACTUALIZAR
                // ============================================================

                GraficaIntegral.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo generar la gráfica.\n\n" +
                    "Detalle técnico: " + ex.Message,
                    "Error en la gráfica",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}