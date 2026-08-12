using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NCalc;
using AngouriMath;
using AngouriMath.Extensions;

namespace ProyectoIntegral
{
    public partial class MainWindow : Window
    {
        // Controla si la calculadora está trabajando en modo Definida (true) o Indefinida (false)
        private bool esIntegralDefinida = true;

        // Número de particiones para la Regla del Trapecio
        private const int PARTICIONES = 1000;

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

            btnIndefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2019"));
            btnIndefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C08457"));
            btnDefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
            btnDefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A3A3A3"));

            txtLimiteA.IsEnabled = false;
            txtLimiteB.IsEnabled = false;
            txtLimiteA.Foreground = new SolidColorBrush(Colors.Gray);
            txtLimiteB.Foreground = new SolidColorBrush(Colors.Gray);

            LimpiarSalidas();
            ActualizarPrevisualizacion(null, null);
        }

        // =====================================================================
        // CAMBIO DE MODO: DEFINIDA
        // =====================================================================
        private void btnDefinida_Click(object sender, RoutedEventArgs e)
        {
            esIntegralDefinida = true;

            btnDefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2019"));
            btnDefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C08457"));
            btnIndefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
            btnIndefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A3A3A3"));

            txtLimiteA.IsEnabled = true;
            txtLimiteB.IsEnabled = true;
            txtLimiteA.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            txtLimiteB.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));

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
                lblAntiderivada.Text = "Evaluación Numérica (Trapecio)";

                lblDetalle1.Text = $"Se dividió el intervalo [{limiteA}, {limiteB}] en {particiones} trapecios. Recuerda que la integral definida representa el área con signo, no necesariamente el área geométrica.";
                lblDetalle2.Text = $"Cálculo de la base de cada trapecio:\nh = ({limiteB} - {limiteA}) / {particiones}";
                lblH.Text = $"h ≈ {h.ToString("F6")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al evaluar numéricamente la función. Verifica la sintaxis de NCalc (ej. sin(x), cos(x), sqrt(x), Pow(x,2)).\n\nDetalle técnico: " + ex.Message, "Error de Evaluación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}