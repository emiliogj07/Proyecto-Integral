using System;

namespace Proyecto_Integral 
{
    public class CalculadoraIntegral
    {
        public double CalcularTrapecio(Func<double, double> funcion, double a, double b, int n)
        {
            double h = (b - a) / n;     // Calculamos el ancho de cada intervalo
            double suma = (funcion(a) + funcion(b)) / 2.0;  //aqui se inicializa la suma con los extremos de la función

            for (int i = 1; i < n; i++)  //Esto es parte de la fórmula del método del trapecio.
            {
                double xi = a + i * h;
                suma += funcion(xi);
            }

            return h * suma;    //Multiplica la suma por el ancho h
        }
    }
}
