using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MathAPI.Models;
using System.Threading.Tasks;

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

        var mathCalculation = new MathCalculation
        {
            FirstNumber = request.FirstNumber,
            SecondNumber = request.SecondNumber,
            Operation = request.Operation
        };

        if (mathCalculation.FirstNumber == null || mathCalculation.SecondNumber == null || mathCalculation.Operation == 0)
        {
            return BadRequest(new Error("Math equation not complete!"));
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
            default:
                mathCalculation.Result = mathCalculation.FirstNumber / mathCalculation.SecondNumber;
                break;
        }

        try
        {
            mathCalculation = MathCalculation.Create(mathCalculation.FirstNumber, mathCalculation.SecondNumber, mathCalculation.Operation, mathCalculation.Result, userId);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        if (ModelState.IsValid)
        {
            _context.Add(mathCalculation);
            await _context.SaveChangesAsync();
        }

        return Created(mathCalculation.CalculationId.ToString(), mathCalculation);
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

        if (ModelState.IsValid)
        {
            _context.Add(mathCalculation);
            await _context.SaveChangesAsync();

        }
        if (mathCalculation.FirebaseUuid == null || mathCalculation.FirebaseUuid == "")
        {
            return Unauthorized(new Error("Token missing!"));
        }
        if (mathCalculation.FirstNumber == null || mathCalculation.SecondNumber == null || mathCalculation.Operation == 0)
        {
            return BadRequest(new Error("Math equation not complete!"));
        }
        ViewBag.Result = mathCalculation.Result;
        return View();

        // return RedirectToAction("Calculate");

    }

    [HttpGet("GetHistory")]
    #region
    [ProducesResponseType(typeof(List<MathCalculation>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [Authorize]
    #endregion
    public async Task<IActionResult> GetHistory()
    {
        var Token = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var history = await _context.MathCalculations
            .Where(m => m.FirebaseUuid == userId)
            .ToListAsync();

        if (history == null || history.Count == 0)
        {
            return Ok(new List<MathCalculation>()); // tests expect array
        }

        return Ok(history);
        List<MathCalculation> historyItems = await _context.MathCalculations.Where(m => m.FirebaseUuid.Equals(userId)).ToListAsync();
        if (historyItems.Count > 0)
        {
            return Ok(historyItems);
        }
        else
        {
            return NotFound(new Error("No history found!"));
        }
    }

    [HttpDelete("DeleteHistory")]
    #region
    [ProducesResponseType(typeof(List<MathCalculation>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [Authorize]
    #endregion
    public async Task<IActionResult> DeleteHistory()
    {
        var Token = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var history = await _context.MathCalculations
            .Where(m => m.FirebaseUuid == userId)
            .ToListAsync();

        if (history == null || history.Count == 0)
        {
            return Ok(new List<MathCalculation>()); // tests expect array
        }

        return Ok(history);

        List<MathCalculation> removableItems = await _context.MathCalculations.Where(m => m.FirebaseUuid.Equals(userId)).ToListAsync();
        if (removableItems.Count > 0)
        {
            _context.MathCalculations.RemoveRange(removableItems);
            await _context.SaveChangesAsync();
            return Ok(removableItems);
        }
        else
        {
            return NotFound(new Error("No history to delete!"));
        }
    }
}
