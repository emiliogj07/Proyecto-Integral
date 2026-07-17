using System;   // Importa el namespace System, que contiene funciones matemáticas como Math.Sin y Math.Cos.

namespace Proyecto_Integral
{
    public class EvaluadorFunciones
    {
        public Func<double, double> ObtenerFuncion(string expresion)
        {
            if (expresion == "x^2")
                return x => x * x;
            else if (expresion == "sin(x)")
                return x => Math.Sin(x);
            else if (expresion == "cos(x)")
                return x => Math.Cos(x);
            else
                return x => x; // función por defecto
        }
    }
}
