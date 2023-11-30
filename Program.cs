using System.Collections.Concurrent;
    using Microsoft.AspNetCore.Mvc;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
var app = builder.Build();

// Curve storage "service"
var map = new ConcurrentDictionary<int, Curve>();

string HashToId(int hash) => 
    Convert.ToHexString(BitConverter.GetBytes(hash));

int IdToHash(string id) =>
    BitConverter.ToInt32(Convert.FromHexString(id));

Curve? LoadCurve(string id)
{
    var hash = IdToHash(id);
    if (map.TryGetValue(hash, out var curve))
        return curve;
    else
        return null;
}

(List<Curve> curves, List<string> notFound) LoadCurves(string[] ids)
{
    var curves = ids
        .Where(id => map.ContainsKey(IdToHash(id)))
        .Select(id => map[IdToHash(id)])
        .ToList();

    var notFound = ids
        .Where(id => !map.ContainsKey(IdToHash(id)))
        .ToList();

    return (curves, notFound);
}

string StoreCurve(Curve curve)
{
    var hash = curve.GetStableHashCode();
    map[hash] = curve;
    var id = HashToId(hash);
    return id;
}

// nancy-http "controller"

app.MapPost("/curve", (Curve curve) =>
{
    var id = StoreCurve(curve);
    return new { id = id };
});

app.MapGet("/curve/{id}", (string id) =>
{
    var curve = LoadCurve(id);
    if (curve == null)
        return Results.NotFound();
    else
        return Results.Ok(new { curve = curve });
});

app.MapGet("/curve/{id}/valueAt", (string id, [FromBody] Rational time) =>
{
    var curve = LoadCurve(id);
    if (curve == null)
        return Results.NotFound();
    else
    {
        var sample = curve.ValueAt(time);
        return Results.Ok(sample);
    }
});

app.MapDelete("/curve/{id}", (string id) => map.Remove(IdToHash(id), out _));

app.MapGet("/curve/{id}/rightLimitAt", (string id, [FromBody] Rational time) =>
{
    var curve = LoadCurve(id);
    if (curve == null)
        return Results.NotFound();
    else
    {
        var sample = curve.RightLimitAt(time);
        return Results.Ok(sample);
    }
});

app.MapGet("/curve/{id}/leftLimitAt", (string id, [FromBody] Rational time) =>
{
    var curve = LoadCurve(id);
    if (curve == null)
        return Results.NotFound();
    else
    {
        var sample = curve.LeftLimitAt(time);
        return Results.Ok(sample);
    }
});

app.MapGet("/curve/{id}/getCsharpCodeString", (string id) =>
{
    var curve = LoadCurve(id);
    if (curve == null)
        return Results.NotFound();
    else
    {
        var codeString = curve.ToCodeString();
        return Results.Ok(codeString);
    }
});

app.MapPost("/curve/addition", ([FromBody]string[] operands) =>
{
    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);
    
    var result = Curve.Addition(curves);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/subtraction", ([FromBody]string[] operands) =>
{
    if (operands.Length != 2)
        return Results.BadRequest("Subtraction accepts only 2 operands.");

    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);
    
    var result = Curve.Subtraction(curves[0], curves[1]);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/minimum", ([FromBody]string[] operands) =>
{
    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);
    
    var result = Curve.Minimum(curves);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/maximum", ([FromBody]string[] operands) =>
{
    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);
    
    var result = Curve.Maximum(curves);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/convolution", ([FromBody]string[] operands) =>
{
    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);
    
    var result = Curve.Convolution(curves);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/maxPlusConvolution", ([FromBody]string[] operands) =>
{
    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);
    
    var result = Curve.MaxPlusConvolution(curves);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/deconvolution", ([FromBody]string[] operands) =>
{
    if (operands.Length != 2)
        return Results.BadRequest("Deconvolution accepts only 2 operands.");

    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);

    var result = Curve.Deconvolution(curves[0], curves[1]);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/maxPlusDeconvolution", ([FromBody]string[] operands) =>
{
    if (operands.Length != 2)
        return Results.BadRequest("(max,+) deconvolution accepts only 2 operands.");

    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);

    var result = Curve.Deconvolution(curves[0], curves[1]);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/composition", ([FromBody]string[] operands) =>
{
    if (operands.Length != 2)
        return Results.BadRequest("Composition accepts only 2 operands.");

    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);

    var result = Curve.Composition(curves[0], curves[1]);
    var id = StoreCurve(result);
    return Results.Ok(id);
});

app.MapPost("/curve/lowerPseudoInverse", ([FromBody]string curveId) =>
{
    var curve = LoadCurve(curveId);
    if (curve == null)
        return Results.NotFound(curveId);
    
    var result = curve.LowerPseudoInverse();
    var resultId = StoreCurve(result);
    return Results.Ok(resultId);
});

app.MapPost("/curve/upperPseudoInverse", ([FromBody]string curveId) =>
{
    var curve = LoadCurve(curveId);
    if (curve == null)
        return Results.NotFound(curveId);
    
    var result = curve.UpperPseudoInverse();
    var resultId = StoreCurve(result);
    return Results.Ok(resultId);
});

app.MapPost("/curve/subAdditiveClosure", ([FromBody]string curveId) =>
{
    var curve = LoadCurve(curveId);
    if (curve == null)
        return Results.NotFound(curveId);
    
    var result = curve.SubAdditiveClosure();
    var resultId = StoreCurve(result);
    return Results.Ok(resultId);
});

app.MapPost("/curve/superAdditiveClosure", ([FromBody]string curveId) =>
{
    var curve = LoadCurve(curveId);
    if (curve == null)
        return Results.NotFound(curveId);
    
    var result = curve.SuperAdditiveClosure();
    var resultId = StoreCurve(result);
    return Results.Ok(resultId);
});

app.MapPost("/curve/horizontalDeviation", ([FromBody]string[] operands) =>
{
    if (operands.Length != 2)
        return Results.BadRequest("Horizontal deviation accepts only 2 operands.");

    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);

    var result = Curve.HorizontalDeviation(curves[0], curves[1]);
    return Results.Ok(result);
});

app.MapPost("/curve/verticalDeviation", ([FromBody]string[] operands) =>
{
    if (operands.Length != 2)
        return Results.BadRequest("Vertical deviation accepts only 2 operands.");

    var (curves, notFound) = LoadCurves(operands); 
    if (notFound.Count > 0)
        return Results.NotFound(notFound);

    var result = Curve.VerticalDeviation(curves[0], curves[1]);
    return Results.Ok(result);
});

app.Run();