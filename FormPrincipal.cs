using System;
using Proyecto_Integral;

public class FormPrincipal
{
public void Iniciar()
{
    Console.WriteLine("CALCULADORA DE INTEGRALES DEFINIDAS");

    Console.Write("Ingrese la función:");
    string funcion = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(funcion))
    {
        Console.WriteLine("Error: Debe escribir una funcion.");
        return;
    }

    Console.Write("Límite inferior: ");
    string textoA = Console.ReadLine();

    Console.Write("Límite superior: ");
    string textoB = Console.ReadLine();

    if (!double.TryParse(textoA, out double a) ||
        !double.TryParse(textoB, out double b))
    {
        Console.WriteLine("Error: Los límites deben ser números.");
        return;
    }
         EvaluadorFunciones evaluador = new EvaluadorFunciones();   //Crea un objeto 'EvaluadorFunciones'
        var funcionEvaluada = evaluador.ObtenerFuncion(funcion);   //Traduce el texto que escribio el usuario ("x^2", "sin(x)", "cos(x)") en una funcion matematica real que se puede evaluar

        CalculadoraIntegral calc = new CalculadoraIntegral(); // Crea un objeto 'CalculadoraIntegral'
        double resultado = calc.CalcularTrapecio(funcionEvaluada, a, b, 100);  // Llama al metodo CalcularTrapecio para aproximar la integral de la funcion entre los limites a y b
                                                                                     //Usa 100 subdivisiones para la aproximación.
        Console.WriteLine($"Resultado aproximado: {resultado}");

  }
}
  

