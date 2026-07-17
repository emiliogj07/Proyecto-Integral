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
        // Variable para controlar el estado actual de la calculadora
        private bool esIntegralDefinida = true;

        public MainWindow()
        {
            InitializeComponent();
            ActualizarPrevisualizacion(null, null); // Muestra la vista previa inicial
        }

       

        // Dibuja la integral en pantalla mientras el usuario escribe
        private void ActualizarPrevisualizacion(object sender, TextChangedEventArgs e)
        {
            if (lblPreview == null || txtFuncion == null) return;

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

        // Cambia el diseño y comportamiento a "Integral Indefinida"
        private void btnIndefinida_Click(object sender, RoutedEventArgs e)
        {
            esIntegralDefinida = false;

            // Cambiar estilos visuales
            btnIndefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2019"));
            btnIndefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C08457"));
            btnDefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
            btnDefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A3A3A3"));

            // Deshabilitar la entrada de límites
            txtLimiteA.IsEnabled = false;
            txtLimiteB.IsEnabled = false;
            txtLimiteA.Foreground = new SolidColorBrush(Colors.Gray);
            txtLimiteB.Foreground = new SolidColorBrush(Colors.Gray);

            ActualizarPrevisualizacion(null, null);
        }

        // Cambia el diseño y comportamiento a "Integral Definida"
        private void btnDefinida_Click(object sender, RoutedEventArgs e)
        {
            esIntegralDefinida = true;

            // Cambiar estilos visuales
            btnDefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2019"));
            btnDefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C08457"));
            btnIndefinida.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
            btnIndefinida.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A3A3A3"));

            // Habilitar la entrada de límites
            txtLimiteA.IsEnabled = true;
            txtLimiteB.IsEnabled = true;
            txtLimiteA.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            txtLimiteB.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));

            ActualizarPrevisualizacion(null, null);
        }



        private void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            string formula = txtFuncion.Text;
            string variable = string.IsNullOrWhiteSpace(txtVariable.Text) ? "x" : txtVariable.Text;

            if (string.IsNullOrWhiteSpace(formula))
            {
                MessageBox.Show("Por favor, ingresa una función matemática.", "Campo vacío");
                return;
            }


            // CASO 1: INTEGRAL INDEFINIDA

            if (!esIntegralDefinida)
            {
                try
                {
                    // AngouriMath procesa el texto y realiza el cálculo algebraico
                    Entity expr = formula;
                    Entity antiderivada = expr.Integrate(variable);
                    Entity antiderivadaLimpia = antiderivada.Simplify();

                    lblAntiderivada.Text = "F(" + variable + ") = " + antiderivadaLimpia.Stringize();
                    lblResultado.Text = "N/A";

                    lblDetalle1.Text = "Integración simbólica procesada por AngouriMath.";
                    lblDetalle2.Text = "Se ha calculado la familia de antiderivadas agregando la constante de integración (C).";
                    lblH.Text = "Simbólico";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("La función no pudo ser integrada simbólicamente. Verifica la sintaxis.\nDetalle: " + ex.Message, "Error Algebraico");
                }
                return; // Cortamos la ejecución para no entrar a la parte definida
            }

            // Validaciones de entrada para los límites
            if (!double.TryParse(txtLimiteA.Text, out double limiteA))
            {
                MessageBox.Show("Por favor, ingresa un número válido para el límite inferior [a].", "Error"); return;
            }

            if (!double.TryParse(txtLimiteB.Text, out double limiteB))
            {
                MessageBox.Show("Por favor, ingresa un número válido para el límite superior [b].", "Error"); return;
            }

            int particiones = 1000; // Garantiza precisión

            try
            {
                // Algoritmo: Regla del Trapecio
                double h = (limiteB - limiteA) / particiones;

                NCalc.Expression exprA = new NCalc.Expression(formula);
                exprA.Parameters[variable] = limiteA;
                double fa = Convert.ToDouble(exprA.Evaluate());

                NCalc.Expression exprB = new NCalc.Expression(formula);
                exprB.Parameters[variable] = limiteB;
                double fb = Convert.ToDouble(exprB.Evaluate());

                double suma = fa + fb;

                for (int i = 1; i < particiones; i++)
                {
                    double xActual = limiteA + (i * h);
                    NCalc.Expression exprIntermedia = new NCalc.Expression(formula);
                    exprIntermedia.Parameters[variable] = xActual;
                    suma += 2 * Convert.ToDouble(exprIntermedia.Evaluate());
                }

                double resultadoIntegral = (h / 2) * suma;

                // Actualización del panel de reportes
                lblResultado.Text = resultadoIntegral.ToString("F6");
                lblAntiderivada.Text = "Evaluación Numérica (Trapecio)";

                lblDetalle1.Text = $"Se dividió el intervalo en {particiones} trapecios. Recuerda que la integral definida representa el área con signo, no necesariamente el área geométrica.";
                lblDetalle2.Text = $"Cálculo de la base de cada trapecio:\nh = ({limiteB} - {limiteA}) / {particiones}";
                lblH.Text = $"h ≈ {h.ToString("F4")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al evaluar numéricamente la función. Verifica la sintaxis de NCalc.\nDetalle: " + ex.Message, "Error de Evaluación");
            }
        }
    }
}