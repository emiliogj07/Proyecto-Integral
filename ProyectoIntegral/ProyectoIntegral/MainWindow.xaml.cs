using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NCalc;
using AngouriMath;
using AngouriMath.Extensions;
using ScottPlot;

// Alias para evitar conflictos entre System.Windows.Media y ScottPlot
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace ProyectoIntegral
{
    public partial class MainWindow : Window
    {
        private bool esIntegralDefinida = true;
        private readonly List<string> historialOperaciones = new List<string>();

        // Paleta de colores cacheada para optimizar rendimiento de la UI
        private readonly SolidColorBrush colorFondoActivo = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#2B2019"));
        private readonly SolidColorBrush colorTextoActivo = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#C08457"));
        private readonly SolidColorBrush colorFondoInactivo = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#121212"));
        private readonly SolidColorBrush colorTextoInactivo = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#A3A3A3"));
        private readonly SolidColorBrush colorExito = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#7BD389"));
        private readonly SolidColorBrush colorError = new SolidColorBrush(WpfColors.IndianRed);
        private readonly SolidColorBrush colorNeutro = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#555555"));

        public MainWindow()
        {
            InitializeComponent();
            ConfigurarEstadoInicial();
        }

        private void ConfigurarEstadoInicial()
        {
            txtParticiones.Text = "1000";
            cmbMetodo.SelectedIndex = 2; // Simpson por defecto
            cmbEjemplos.SelectedIndex = 0;
            LimpiarSalidas();
            ActualizarTextosPrevisualizacion();
        }

        #region NORMALIZACIÓN DE SINTAXIS

        private static string NormalizarParaNCalc(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula)) return "0";

            formula = formula.Replace(" ", "");

            // Reemplazo iterativo para soportar potencias (ej: x^2 -> Pow(x, 2))
            string patron = @"([a-zA-Z0-9_.]+)\^([a-zA-Z0-9_.]+)";
            while (Regex.IsMatch(formula, patron))
            {
                formula = Regex.Replace(formula, patron, "Pow($1, $2)");
            }

            // Sustituir constantes comunes
            return formula.Replace("Pi", "PI", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region PREVISUALIZACIÓN Y CONTROLES UI

        private void ActualizarPrevisualizacion(object sender, TextChangedEventArgs e) => ActualizarTextosPrevisualizacion();

        private void ActualizarTextosPrevisualizacion()
        {
            if (lblPreview == null || txtFuncion == null || txtVariable == null) return;

            string funcion = string.IsNullOrWhiteSpace(txtFuncion.Text) ? "f(x)" : txtFuncion.Text;
            string variable = string.IsNullOrWhiteSpace(txtVariable.Text) ? "x" : txtVariable.Text;
            string funcionVisual = funcion.Replace("*", "·");

            if (esIntegralDefinida && txtLimiteA != null && txtLimiteB != null)
            {
                string a = string.IsNullOrWhiteSpace(txtLimiteA.Text) ? "a" : txtLimiteA.Text;
                string b = string.IsNullOrWhiteSpace(txtLimiteB.Text) ? "b" : txtLimiteB.Text;
                lblPreview.Text = $"∫ [{a}, {b}] {funcionVisual} d{variable}";
            }
            else
            {
                lblPreview.Text = $"∫ {funcionVisual} d{variable}";
            }
        }

        private void cmbEjemplos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEjemplos == null || txtFuncion == null || cmbEjemplos.SelectedIndex == 0) return;

            switch (cmbEjemplos.SelectedIndex)
            {
                case 1: CargarEjemplo("x^2", "0", "2"); break;
                case 2: CargarEjemplo("x^2 + 3*x + 2", "0", "3"); break;
                case 3: CargarEjemplo("sin(x)", "0", "Pi"); break;
                case 4: CargarEjemplo("cos(x)", "0", "Pi/2"); break;
                case 5: CargarEjemplo("sqrt(x)", "0", "4"); break;
                case 6: CargarEjemplo("1/x", "1", "4"); break;
            }

            // Desenganchar temporalmente para evitar disparar el evento de nuevo
            cmbEjemplos.SelectionChanged -= cmbEjemplos_SelectionChanged;
            cmbEjemplos.SelectedIndex = 0;
            cmbEjemplos.SelectionChanged += cmbEjemplos_SelectionChanged;
        }

        private void CargarEjemplo(string funcion, string a, string b)
        {
            txtFuncion.Text = funcion;
            txtVariable.Text = "x";
            txtLimiteA.Text = a;
            txtLimiteB.Text = b;
            btnDefinida_Click(null, null); // Forzar modo definida
        }

        private void cmbMetodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lblExplicacionMetodo == null || cmbMetodo == null) return;

            lblExplicacionMetodo.Text = cmbMetodo.SelectedIndex switch
            {
                0 => "Riemann (Punto Medio):\nAproxima el área utilizando rectángulos. Ideal para conceptos básicos.",
                1 => "Regla del Trapecio:\nAproxima el área utilizando trapecios. Mejora la precisión en curvas simples.",
                2 => "Simpson 1/3:\nAproxima el área utilizando arcos de parábola.\n💡 Recomendado: Excelente precisión para funciones suaves.",
                _ => ""
            };
        }

        private void btnIndefinida_Click(object sender, RoutedEventArgs e)
        {
            esIntegralDefinida = false;
            ActivarModoVisual(btnIndefinida, btnDefinida);
            txtLimiteA.IsEnabled = txtLimiteB.IsEnabled = false;
            txtParticiones.IsEnabled = cmbMetodo.IsEnabled = false;
            LimpiarSalidas();
            ActualizarTextosPrevisualizacion();
        }

        private void btnDefinida_Click(object sender, RoutedEventArgs e)
        {
            esIntegralDefinida = true;
            ActivarModoVisual(btnDefinida, btnIndefinida);
            txtLimiteA.IsEnabled = txtLimiteB.IsEnabled = true;
            txtParticiones.IsEnabled = cmbMetodo.IsEnabled = true;
            LimpiarSalidas();
            ActualizarTextosPrevisualizacion();
        }

        private void ActivarModoVisual(Button activo, Button inactivo)
        {
            activo.Background = colorFondoActivo;
            activo.Foreground = colorTextoActivo;
            inactivo.Background = colorFondoInactivo;
            inactivo.Foreground = colorTextoInactivo;
        }

        private void LimpiarSalidas()
        {
            lblResultado.Text = "Esperando cálculo...";
            lblResultado.Foreground = colorNeutro;
            lblAntiderivada.Text = "—";
            lblPasoAPaso.Text = "Completa los datos y presiona calcular.";
            lblEstado.Text = "● Listo para calcular";
            lblEstado.Foreground = new SolidColorBrush(WpfColors.Gray);

            if (GraficaIntegral != null)
            {
                GraficaIntegral.Plot.Clear();
                GraficaIntegral.Refresh();
            }
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            txtFuncion.Text = "";
            txtVariable.Text = "x";
            txtLimiteA.Text = "0";
            txtLimiteB.Text = "1";
            ConfigurarEstadoInicial();
        }

        private void btnCopiar_Click(object sender, RoutedEventArgs e)
        {
            if (lblResultado.Text != "Esperando cálculo..." && lblResultado.Text != "Error")
            {
                Clipboard.SetText(lblResultado.Text);
                lblEstado.Text = "✓ Resultado copiado al portapapeles";
                lblEstado.Foreground = colorExito;
            }
        }

        private void btnAyuda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "SINTAXIS RÁPIDA DE CALCULOOP\n\n" +
                "• Potencia: x^2\n" +
                "• Multiplicación: 2*x (No uses 2x)\n" +
                "• Seno / Coseno: sin(x) / cos(x)\n" +
                "• Raíz Cuadrada: sqrt(x)\n" +
                "• Fracciones: 1/x\n" +
                "• Pi: Pi",
                "¿Cómo escribir tu función?",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region MOTOR PRINCIPAL

        private async void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            string formula = txtFuncion.Text.Trim();
            string variable = string.IsNullOrWhiteSpace(txtVariable.Text) ? "x" : txtVariable.Text.Trim();

            if (string.IsNullOrWhiteSpace(formula))
            {
                MessageBox.Show("⚠ Escribe una función. Ejemplo: x^2 + 3*x", "Campo vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (variable.Length > 1 || !char.IsLetter(variable[0]))
            {
                MessageBox.Show("⚠ La variable debe ser una sola letra (ej. x, t).", "Variable inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Bloqueo de UI
            btnCalcular.IsEnabled = false;
            btnCalcular.Content = "⏳ CALCULANDO...";
            lblEstado.Text = "◌ Procesando algoritmos...";
            lblEstado.Foreground = colorTextoActivo;

            try
            {
                if (esIntegralDefinida)
                    await ProcesarIntegralDefinidaAsync(formula, variable);
                else
                    await ProcesarIntegralIndefinidaAsync(formula, variable);
            }
            catch (Exception ex)
            {
                lblResultado.Text = "Error";
                lblEstado.Text = "⚠ Error en la ejecución";
                lblEstado.Foreground = colorError;
                MessageBox.Show($"No pudimos interpretar la operación.\n\nRevisa la sintaxis. Usa '*' para multiplicar (ej. 2*x).\n\nDetalle técnico: {ex.Message}", "Error de Evaluación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnCalcular.IsEnabled = true;
                btnCalcular.Content = "CALCULAR INTEGRAL";
            }
        }

        private async Task ProcesarIntegralIndefinidaAsync(string formula, string variable)
        {
            string resultadoVisual = "";

            await Task.Run(() =>
            {
                Entity expr = MathS.FromString(formula);
                Entity antiderivada = expr.Integrate(variable).Simplify();
                resultadoVisual = antiderivada.Stringize();
            });

            lblAntiderivada.Text = "Integración Simbólica (AngouriMath)";
            lblResultado.Text = $"F({variable}) = {resultadoVisual} + C";
            lblResultado.Foreground = colorExito;

            lblPasoAPaso.Text =
                "DESARROLLO SIMBÓLICO\n" +
                "──────────────────────\n" +
                "1. Función Original:\n" +
                $"   f({variable}) = {formula}\n\n" +
                "2. Familia de Antiderivadas:\n" +
                $"   F({variable}) = {resultadoVisual} + C\n\n" +
                "Interpretación:\nExisten infinitas funciones cuya derivada es la función original. La constante 'C' representa esa familia.";

            lblEstado.Text = "✓ Cálculo simbólico completado";
            lblEstado.Foreground = colorExito;

            await GenerarGraficaAsync(NormalizarParaNCalc(formula), variable, -10, 10, false);
        }

        private async Task ProcesarIntegralDefinidaAsync(string formula, string variable)
        {
            double a = 0, b = 0;
            int particiones = 1000;

            try
            {
                var exprA = new NCalc.Expression(NormalizarParaNCalc(txtLimiteA.Text.Trim()));
                var exprB = new NCalc.Expression(NormalizarParaNCalc(txtLimiteB.Text.Trim()));

                a = Convert.ToDouble(exprA.Evaluate());
                b = Convert.ToDouble(exprB.Evaluate());

                if (!int.TryParse(txtParticiones.Text, out particiones) || particiones < 2) particiones = 1000;
            }
            catch
            {
                MessageBox.Show("⚠ Los límites deben ser números o expresiones válidas (Ej. 2, Pi, sqrt(9)).", "Límites Inválidos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool limitesInvertidos = false;
            if (a > b)
            {
                limitesInvertidos = true;
                (a, b) = (b, a); // Swap moderno (Tuplas)
            }
            else if (a == b)
            {
                lblResultado.Text = "0.000000";
                lblPasoAPaso.Text = "El límite superior e inferior son iguales. El área es matemáticamente 0.";
                lblEstado.Text = "✓ Cálculo completado";
                return;
            }

            string formulaNormalizada = NormalizarParaNCalc(formula);
            int indiceMetodo = cmbMetodo.SelectedIndex;
            double h = (b - a) / particiones;

            double resultadoFinal = await Task.Run(() =>
            {
                var expr = new NCalc.Expression(formulaNormalizada);
                return indiceMetodo switch
                {
                    0 => MetodoRiemann(expr, variable, a, b, particiones),
                    1 => MetodoTrapecio(expr, variable, a, b, particiones),
                    _ => MetodoSimpson(expr, variable, a, b, particiones)
                };
            });

            if (double.IsNaN(resultadoFinal) || double.IsInfinity(resultadoFinal))
            {
                lblEstado.Text = "⚠ Discontinuidad detectada";
                lblEstado.Foreground = colorError;
                MessageBox.Show("⚠ La función presenta una discontinuidad (ej. división por cero) dentro del intervalo evaluado.\n\nEl resultado numérico no es válido bajo este método.", "Discontinuidad Detectada", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (limitesInvertidos) resultadoFinal *= -1;

            // Actualizar Interfaz
            lblResultado.Text = resultadoFinal.ToString("F6");
            lblResultado.Foreground = colorExito;
            lblAntiderivada.Text = "Evaluación de Análisis Numérico";

            string nombreMetodo = indiceMetodo switch
            {
                0 => "Riemann (Punto Medio)",
                1 => "Regla del Trapecio",
                _ => "Simpson 1/3"
            };

            lblPasoAPaso.Text =
                "DESARROLLO NUMÉRICO\n" +
                "──────────────────────\n" +
                $"1. Intervalo Evaluado:\n   [ {a} , {b} ]\n" +
                (limitesInvertidos ? "\n⚠ Intervalo invertido detectado. Por definición, el resultado invierte su signo.\n" : "") +
                $"\n2. Configuración:\n   Método: {nombreMetodo}\n   Particiones (n): {particiones}\n" +
                $"   Tamaño de paso (h): {h:F6}\n\n" +
                $"3. Resultados:\n" +
                $"   Integral con signo: {resultadoFinal:F6}\n" +
                $"   Área Geométrica (aprox): {Math.Abs(resultadoFinal):F6}\n\n" +
                "Interpretación:\nLa integral representa la sumatoria del área bajo la curva. Los valores por debajo del eje X restan al total.";

            // Gestión del Historial
            string registro = $"∫ [{txtLimiteA.Text}, {txtLimiteB.Text}] {formula} = {resultadoFinal:F4} ({nombreMetodo})";
            historialOperaciones.Insert(0, registro);

            if (lstHistorial != null)
            {
                lstHistorial.ItemsSource = null;
                lstHistorial.ItemsSource = historialOperaciones;
            }

            lblEstado.Text = "✓ Cálculo completado con éxito";
            lblEstado.Foreground = colorExito;

            await GenerarGraficaAsync(formulaNormalizada, variable, a, b, true);
        }

        #endregion

        #region ALGORITMOS MATEMÁTICOS NUMÉRICOS

        private static double MetodoRiemann(NCalc.Expression expr, string var, double a, double b, int n)
        {
            double h = (b - a) / n;
            double suma = 0;
            for (int i = 0; i < n; i++)
            {
                expr.Parameters[var] = a + h * (i + 0.5);
                suma += Convert.ToDouble(expr.Evaluate());
            }
            return suma * h;
        }

        private static double MetodoTrapecio(NCalc.Expression expr, string var, double a, double b, int n)
        {
            double h = (b - a) / n;
            expr.Parameters[var] = a;
            double suma = Convert.ToDouble(expr.Evaluate());
            expr.Parameters[var] = b;
            suma += Convert.ToDouble(expr.Evaluate());

            for (int i = 1; i < n; i++)
            {
                expr.Parameters[var] = a + (i * h);
                suma += 2 * Convert.ToDouble(expr.Evaluate());
            }
            return (h / 2) * suma;
        }

        private static double MetodoSimpson(NCalc.Expression expr, string var, double a, double b, int n)
        {
            if (n % 2 != 0) n++; // Simpson requiere particiones pares
            double h = (b - a) / n;

            expr.Parameters[var] = a;
            double suma = Convert.ToDouble(expr.Evaluate());
            expr.Parameters[var] = b;
            suma += Convert.ToDouble(expr.Evaluate());

            for (int i = 1; i < n; i++)
            {
                expr.Parameters[var] = a + i * h;
                double y = Convert.ToDouble(expr.Evaluate());
                suma += (i % 2 == 0) ? 2 * y : 4 * y;
            }
            return (h / 3) * suma;
        }

        #endregion

        #region GENERACIÓN DE GRÁFICAS (SCOTTPLOT)

        private async Task GenerarGraficaAsync(string formulaNormalizada, string variable, double limiteA, double limiteB, bool sombrearArea)
        {
            if (GraficaIntegral == null || limiteA == limiteB) return;

            double margenX = (limiteB - limiteA) * 0.3;
            if (margenX == 0) margenX = 5;

            double vistaInicio = limiteA - margenX;
            double vistaFin = limiteB + margenX;

            int puntosTotales = 1000;
            double[] xsCurva = new double[puntosTotales];
            double[] ysCurva = new double[puntosTotales];

            // Listas instanciadas con capacidad máxima para ahorrar reasignaciones de memoria
            List<double> xsSombreado = new List<double>(puntosTotales);
            List<double> ysSombreado = new List<double>(puntosTotales);

            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;
            bool evaluarExito = true;

            await Task.Run(() =>
            {
                try
                {
                    double paso = (vistaFin - vistaInicio) / (puntosTotales - 1);
                    var expr = new NCalc.Expression(formulaNormalizada);

                    for (int i = 0; i < puntosTotales; i++)
                    {
                        double x = vistaInicio + (i * paso);
                        xsCurva[i] = x;

                        try
                        {
                            expr.Parameters[variable] = x;
                            double y = Convert.ToDouble(expr.Evaluate());
                            ysCurva[i] = (double.IsNaN(y) || double.IsInfinity(y)) ? double.NaN : y;

                            if (!double.IsNaN(ysCurva[i]) && x >= vistaInicio && x <= vistaFin)
                            {
                                minY = Math.Min(minY, ysCurva[i]);
                                maxY = Math.Max(maxY, ysCurva[i]);
                            }

                            if (sombrearArea && x >= limiteA && x <= limiteB && !double.IsNaN(ysCurva[i]))
                            {
                                xsSombreado.Add(x);
                                ysSombreado.Add(ysCurva[i]);
                            }
                        }
                        catch { ysCurva[i] = double.NaN; }
                    }
                }
                catch { evaluarExito = false; }
            });

            if (!evaluarExito || double.IsInfinity(minY)) return;

            AplicarEstiloGrafica(vistaInicio, vistaFin, minY, maxY, variable);

            // 1. Añadir la Curva Principal
            var curva = GraficaIntegral.Plot.Add.Scatter(xsCurva, ysCurva);
            curva.Color = ScottPlot.Color.FromHex("#C08457");
            curva.LineWidth = 2.5f;
            curva.MarkerSize = 0;

            // 2. Sombrear estrictamente el Área
            if (sombrearArea && xsSombreado.Count > 0)
            {
                xsSombreado.Insert(0, xsSombreado[0]); ysSombreado.Insert(0, 0);
                xsSombreado.Add(xsSombreado[xsSombreado.Count - 1]); ysSombreado.Add(0);

                var poligono = GraficaIntegral.Plot.Add.Polygon(xsSombreado.ToArray(), ysSombreado.ToArray());
                poligono.FillColor = ScottPlot.Color.FromHex("#C08457").WithAlpha(0.40);
                poligono.LineColor = ScottPlot.Colors.Transparent;

                var vLineA = GraficaIntegral.Plot.Add.VerticalLine(limiteA);
                vLineA.Color = ScottPlot.Color.FromHex("#7BD389");
                vLineA.LineWidth = 1.5f;
                vLineA.LinePattern = ScottPlot.LinePattern.Dashed;

                var vLineB = GraficaIntegral.Plot.Add.VerticalLine(limiteB);
                vLineB.Color = ScottPlot.Color.FromHex("#7BD389");
                vLineB.LineWidth = 1.5f;
                vLineB.LinePattern = ScottPlot.LinePattern.Dashed;
            }

            GraficaIntegral.Refresh();
        }

        private void AplicarEstiloGrafica(double vistaInicio, double vistaFin, double minY, double maxY, string variable)
        {
            GraficaIntegral.Plot.Clear();
            GraficaIntegral.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1B1B1B");
            GraficaIntegral.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#121212");
            GraficaIntegral.Plot.Axes.Color(ScottPlot.Color.FromHex("#A3A3A3"));
            GraficaIntegral.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#333333");

            // Ejes visuales centrales
            var hLine = GraficaIntegral.Plot.Add.HorizontalLine(0);
            hLine.Color = ScottPlot.Color.FromHex("#A3A3A3");
            hLine.LineWidth = 1f;

            // Ajuste de cámara interactivo
            minY = Math.Min(minY, 0);
            maxY = Math.Max(maxY, 0);
            double rangoY = (maxY - minY) == 0 ? 1 : maxY - minY;
            GraficaIntegral.Plot.Axes.SetLimits(vistaInicio, vistaFin, minY - (rangoY * 0.1), maxY + (rangoY * 0.1));

            GraficaIntegral.Plot.Title($"Análisis Geométrico de f({variable})");
            GraficaIntegral.Plot.XLabel($"Eje {variable}");
            GraficaIntegral.Plot.YLabel($"f({variable})");
        }

        #endregion
    }
}