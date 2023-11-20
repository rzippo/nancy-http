using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
var app = builder.Build();

var map = new ConcurrentDictionary<int, Curve>();

app.MapPost("/curve", async (HttpRequest request) =>
{
    string json = "";
    using (var reader = new StreamReader(request.Body))
    {
        json = await reader.ReadToEndAsync();
    }
    app.Logger.LogInformation($"Received curve: {json}");
    var curve = Curve.FromJson(json);
    var id = curve.GetHashCode();
    map[id] = curve;
    return id;
});

app.MapGet("/curve/{id}", (int id) => map[id].ToString());

app.MapGet("/curve/{id}/sample", (int id, [FromBody] Rational time) =>
{
    var curve = map[id];
    var sample = curve.ValueAt(time);
    return sample;
});

app.MapDelete("/curve", (int id) => map.Remove(id, out _));

app.MapPost("/curve/convolution", ([FromBody]ConvolutionOperands parameters) =>
{
    var fCurve = map[parameters.f];
    var gCurve = map[parameters.g];
    var result = Curve.Convolution(fCurve, gCurve);
    var id = result.GetHashCode();
    map[id] = result;
    return id;
});

app.MapPost("/curve/deconvolution", ([FromBody]DeconvolutionOperands parameters) =>
{
    var fCurve = map[parameters.f];
    var gCurve = map[parameters.g];
    var result = Curve.Deconvolution(fCurve, gCurve);
    var id = result.GetHashCode();
    map[id] = result;
    return id;
});

app.MapPost("/curve/subadditive-closure", ([FromBody]int f) =>
{
    var fCurve = map[f];
    var result = fCurve.SubAdditiveClosure();
    var id = result.GetHashCode();
    map[id] = result;
    return id;
});

app.Run();

record ConvolutionOperands(int f, int g);
record DeconvolutionOperands(int f, int g);