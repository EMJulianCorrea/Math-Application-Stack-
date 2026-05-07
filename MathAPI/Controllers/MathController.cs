using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MathAPI.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace MathAPI.Controllers;

[Route("api/[controller]")]
[ApiController]

public class MathController : Controller
{
    private readonly MathDbContext _context;

    public MathController(MathDbContext context)
    {
        _context = context;
    }

    [HttpPost("PostCalculate")]
    #region 
    [ProducesResponseType(typeof(MathCalculation), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    [Authorize]
    #endregion
    public async Task<IActionResult> PostCalculate([FromBody] MathCalculationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new Error("Invalid token"));

        MathCalculation mathCalculation;

        try
        {
            mathCalculation = MathCalculation.Create(
                request.FirstNumber,
                request.SecondNumber,
                request.Operation,
                null,
                userId
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new Error(ex.Message));
        }

        switch (mathCalculation.Operation)
        {
            case 1:
                mathCalculation.Result = mathCalculation.FirstNumber + mathCalculation.SecondNumber;
                break;
            case 2:
                mathCalculation.Result = mathCalculation.FirstNumber - mathCalculation.SecondNumber;
                break;
            case 3:
                mathCalculation.Result = mathCalculation.FirstNumber * mathCalculation.SecondNumber;
                break;
            case 4:
                mathCalculation.Result = mathCalculation.SecondNumber == 0
                    ? throw new DivideByZeroException()
                    : mathCalculation.FirstNumber / mathCalculation.SecondNumber;
                break;
        }

        _context.Add(mathCalculation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(PostCalculate), mathCalculation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(decimal? FirstNumber, decimal? SecondNumber, int Operation)
    {
        var token = HttpContext.Session.GetString("currentUser");

        if (token == null)
        {
            return RedirectToAction("Login", "Auth");
        }
        decimal? Result = 0;
        MathCalculation mathCalculation;

        try
        {
            mathCalculation = MathCalculation.Create(FirstNumber, SecondNumber, Operation, Result, token);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View();
            throw;
        }


        switch (Operation)
        {
            case 1:
                mathCalculation.Result = mathCalculation.FirstNumber + mathCalculation.SecondNumber;
                break;
            case 2:
                mathCalculation.Result = mathCalculation.FirstNumber - mathCalculation.SecondNumber;
                break;
            case 3:
                mathCalculation.Result = mathCalculation.FirstNumber * mathCalculation.SecondNumber;
                break;
            case 4:
                mathCalculation.Result = mathCalculation.FirstNumber / mathCalculation.SecondNumber;
                break;
            default:
                throw new ArgumentException("Operation not present");
                break;
        }

        if (mathCalculation.FirebaseUuid == null || mathCalculation.FirebaseUuid == "")
            return Unauthorized(new Error("Token missing!"));

        if (mathCalculation.FirstNumber == null || mathCalculation.SecondNumber == null || mathCalculation.Operation == 0)
            return BadRequest(new Error("Math equation not complete!"));

        _context.Add(mathCalculation);
        await _context.SaveChangesAsync();

        ViewBag.Result = mathCalculation.Result;
        return View();

        // return RedirectToAction("Calculate");

    }

    [HttpGet("GetHistory")]
    [Authorize]
    public async Task<IActionResult> GetHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new Error("Invalid token"));

        var history = await _context.MathCalculations
            .Where(m => m.FirebaseUuid == userId)
            .ToListAsync();

        return Ok(history);
    }

    [HttpDelete("DeleteHistory")]
    [Authorize]
    public async Task<IActionResult> DeleteHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new Error("Invalid token"));

        var history = await _context.MathCalculations
            .Where(m => m.FirebaseUuid == userId)
            .ToListAsync();

        if (history.Count == 0)
            return Ok(new List<MathCalculation>()); // tests expect empty array

        _context.MathCalculations.RemoveRange(history);
        await _context.SaveChangesAsync();

        return Ok(history);
    }
}
