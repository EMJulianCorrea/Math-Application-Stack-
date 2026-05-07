namespace MathAPI.Models;

public class MathCalculationRequest
{
    public decimal FirstNumber { get; set; }
    public decimal SecondNumber { get; set; }
    public int Operation { get; set; }
}