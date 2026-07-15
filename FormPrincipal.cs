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

  }
}
  

