using KBMS.Reasoning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace KBMS.Tests;

public class VectorizedInferenceTests
{
    [Fact]
    public void SIMD_Evaluation_ShouldBeCorrect_AndFast()
    {
        var engine = new InferenceEngine();
        int length = 100_000;
        
        var bulkParams = new Dictionary<string, double[]>
        {
            ["dist"] = Enumerable.Range(0, length).Select(i => (double)i * 10).ToArray(),
            ["time"] = Enumerable.Range(0, length).Select(i => (double)(i + 1)).ToArray()
        };

        // 1. Warmup and Test Scalar Performance (for baseline)
        var sw = Stopwatch.StartNew();
        double[] scalarResults = new double[length];
        for (int i = 0; i < length; i++)
        {
            var p = new Dictionary<string, object>
            {
                ["dist"] = bulkParams["dist"][i],
                ["time"] = bulkParams["time"][i]
            };
            scalarResults[i] = Convert.ToDouble(engine.EvaluateFormula("dist / time", p));
        }
        sw.Stop();
        var scalarTime = sw.ElapsedMilliseconds;

        // 2. Test SIMD Performance
        sw.Restart();
        var simdResults = engine.EvaluateFormulaSIMD("dist / time", bulkParams, length);
        sw.Stop();
        var simdTime = sw.ElapsedMilliseconds;

        // Assert correctness
        Assert.Equal(length, simdResults.Length);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(scalarResults[i], simdResults[i], 5); // tolerance of 5 decimals
        }

        // SIMD should ideally be faster than executing scalar loop with Dictionary overhead
        Console.WriteLine($"Scalar Time: {scalarTime}ms");
        Console.WriteLine($"SIMD Time: {simdTime}ms");
        
        Assert.True(simdTime < scalarTime, "SIMD must be faster than scalar evaluation loop");
    }
}
