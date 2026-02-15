using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Profiling;
using System.Diagnostics;

public class PrintingTests : MonoBehaviour
{
    private enum PrintingMethod
    {
        SingleProjector,
        ArrayProjector
    }
    
    [SerializeField] private PrintingMethod mSelectedPrintingMethod;

    public int[] indicesToPrint;
    
    [Header("Printing Method 1")]
    public Texture[] texturesToPrint;
    public DecalProjector decalProjectorMethod1;
    public string[] texturePropertyName;
    
    [Header("Printing Method 2")]
    public Material materialRef;
    public string arrayPropertyName;
    
    [Header("Profiling Settings")]
    [SerializeField] private bool enableProfiling = true;
    [SerializeField] private int benchmarkIterations = 1000;
    
    // Custom Samplers
    private static CustomSampler singleProjectorSampler = CustomSampler.Create("Print.SingleProjector");
    private static CustomSampler arrayProjectorSampler = CustomSampler.Create("Print.ArrayProjector");
    
    // Stopwatch pour mesures précises
    private Stopwatch stopwatch = new Stopwatch();
    
    private void PrintTest()
    {
        switch(mSelectedPrintingMethod)
        {
            case PrintingMethod.SingleProjector:
                if (enableProfiling)
                {
                    singleProjectorSampler.Begin();
                    DoSingleProjector();
                    singleProjectorSampler.End();
                }
                else
                {
                    DoSingleProjector();
                }
                break;
                
            case PrintingMethod.ArrayProjector:
                if (enableProfiling)
                {
                    arrayProjectorSampler.Begin();
                    DoArrayProjector();
                    arrayProjectorSampler.End();
                }
                else
                {
                    DoArrayProjector();
                }
                break;
                
            default:
                break;
        }
    }

    private void DoSingleProjector()
    {
        foreach(var i in indicesToPrint)
        {
            decalProjectorMethod1.material.SetTexture(texturePropertyName[i], texturesToPrint[i]);
        }
    }
    
    private void DoArrayProjector()
    {
        materialRef.SetVector(arrayPropertyName, new Vector4(indicesToPrint[0], indicesToPrint[1], indicesToPrint[2], 0));
    }
    
    [ContextMenu("Test Function")]
    void TestFunction()
    {
        UnityEngine.Debug.Log("Test function executed!");
        PrintTest();
    }
    
    [ContextMenu("Compare Both Methods (Single Run)")]
    void CompareMethods()
    {
        UnityEngine.Debug.Log("=== COMPARISON: Single Run ===");
        
        // Test Method 1
        stopwatch.Restart();
        DoSingleProjector();
        stopwatch.Stop();
        float method1Time = (stopwatch.ElapsedTicks * 1000000f) / Stopwatch.Frequency;
        
        // Test Method 2
        stopwatch.Restart();
        DoArrayProjector();
        stopwatch.Stop();
        float method2Time = (stopwatch.ElapsedTicks * 1000000f) / Stopwatch.Frequency;
        
        // Results
        UnityEngine.Debug.Log($"Method 1 (SingleProjector): {method1Time:F2} μs");
        UnityEngine.Debug.Log($"Method 2 (ArrayProjector): {method2Time:F2} μs");
        
        if (method1Time < method2Time)
        {
            float percent = ((method2Time - method1Time) / method2Time) * 100f;
            UnityEngine.Debug.Log($"<color=green>Method 1 is FASTER by {percent:F1}%</color>");
        }
        else
        {
            float percent = ((method1Time - method2Time) / method1Time) * 100f;
            UnityEngine.Debug.Log($"<color=green>Method 2 is FASTER by {percent:F1}%</color>");
        }
    }
    
    [ContextMenu("Benchmark Both Methods")]
    void BenchmarkMethods()
    {
        UnityEngine.Debug.Log($"=== BENCHMARK: {benchmarkIterations} iterations ===");
        
        // Warmup
        for (int i = 0; i < 10; i++)
        {
            DoSingleProjector();
            DoArrayProjector();
        }
        
        // Benchmark Method 1
        stopwatch.Restart();
        for (int i = 0; i < benchmarkIterations; i++)
        {
            DoSingleProjector();
        }
        stopwatch.Stop();
        long method1Ticks = stopwatch.ElapsedTicks;
        float method1Ms = (method1Ticks * 1000f) / Stopwatch.Frequency;
        float method1Avg = method1Ms / benchmarkIterations;
        
        // Benchmark Method 2
        stopwatch.Restart();
        for (int i = 0; i < benchmarkIterations; i++)
        {
            DoArrayProjector();
        }
        stopwatch.Stop();
        long method2Ticks = stopwatch.ElapsedTicks;
        float method2Ms = (method2Ticks * 1000f) / Stopwatch.Frequency;
        float method2Avg = method2Ms / benchmarkIterations;
        
        // Results
        UnityEngine.Debug.Log("--- Results ---");
        UnityEngine.Debug.Log($"Method 1 (SingleProjector):");
        UnityEngine.Debug.Log($"  Total: {method1Ms:F3} ms");
        UnityEngine.Debug.Log($"  Average: {method1Avg:F4} ms per call");
        
        UnityEngine.Debug.Log($"Method 2 (ArrayProjector):");
        UnityEngine.Debug.Log($"  Total: {method2Ms:F3} ms");
        UnityEngine.Debug.Log($"  Average: {method2Avg:F4} ms per call");
        
        // Comparison
        if (method1Ms < method2Ms)
        {
            float speedup = method2Ms / method1Ms;
            float percent = ((method2Ms - method1Ms) / method2Ms) * 100f;
            UnityEngine.Debug.Log($"<color=green>✓ Method 1 is FASTER</color>");
            UnityEngine.Debug.Log($"  {speedup:F2}x faster ({percent:F1}% improvement)");
        }
        else
        {
            float speedup = method1Ms / method2Ms;
            float percent = ((method1Ms - method2Ms) / method1Ms) * 100f;
            UnityEngine.Debug.Log($"<color=green>✓ Method 2 is FASTER</color>");
            UnityEngine.Debug.Log($"  {speedup:F2}x faster ({percent:F1}% improvement)");
        }
        
        UnityEngine.Debug.Log("===================");
    }
    
    [ContextMenu("Detailed Profile Comparison")]
    void DetailedProfile()
    {
        UnityEngine.Debug.Log("=== DETAILED PROFILE ===");
        
        // Method 1 detailed
        Profiler.BeginSample("Method1.Total");
        stopwatch.Restart();
        
        Profiler.BeginSample("Method1.Loop");
        foreach(var i in indicesToPrint)
        {
            Profiler.BeginSample("Method1.SetTexture");
            decalProjectorMethod1.material.SetTexture(texturePropertyName[i], texturesToPrint[i]);
            Profiler.EndSample();
        }
        Profiler.EndSample();
        
        stopwatch.Stop();
        float method1Time = (stopwatch.ElapsedTicks * 1000000f) / Stopwatch.Frequency;
        Profiler.EndSample();
        
        // Method 2 detailed
        Profiler.BeginSample("Method2.Total");
        stopwatch.Restart();
        
        Profiler.BeginSample("Method2.CreateVector");
        Vector4 vector = new Vector4(indicesToPrint[0], indicesToPrint[1], indicesToPrint[2], 0);
        Profiler.EndSample();
        
        Profiler.BeginSample("Method2.SetVector");
        materialRef.SetVector(arrayPropertyName, vector);
        Profiler.EndSample();
        
        stopwatch.Stop();
        float method2Time = (stopwatch.ElapsedTicks * 1000000f) / Stopwatch.Frequency;
        Profiler.EndSample();
        
        // Results
        UnityEngine.Debug.Log($"Method 1: {method1Time:F2} μs ({indicesToPrint.Length} SetTexture calls)");
        UnityEngine.Debug.Log($"Method 2: {method2Time:F2} μs (1 SetVector call)");
        UnityEngine.Debug.Log("Check Unity Profiler window for detailed breakdown");
    }
    
    [ContextMenu("Memory Profile")]
    void MemoryProfile()
    {
        UnityEngine.Debug.Log("=== MEMORY PROFILE ===");
        
        // Method 1 memory
        long memBefore1 = Profiler.GetTotalAllocatedMemoryLong();
        for (int i = 0; i < 100; i++)
        {
            DoSingleProjector();
        }
        long memAfter1 = Profiler.GetTotalAllocatedMemoryLong();
        long allocated1 = memAfter1 - memBefore1;
        
        // Method 2 memory
        long memBefore2 = Profiler.GetTotalAllocatedMemoryLong();
        for (int i = 0; i < 100; i++)
        {
            DoArrayProjector();
        }
        long memAfter2 = Profiler.GetTotalAllocatedMemoryLong();
        long allocated2 = memAfter2 - memBefore2;
        
        UnityEngine.Debug.Log($"Method 1 allocations (100 calls): {allocated1} bytes ({allocated1 / 100f:F2} bytes per call)");
        UnityEngine.Debug.Log($"Method 2 allocations (100 calls): {allocated2} bytes ({allocated2 / 100f:F2} bytes per call)");
        
        if (allocated1 < allocated2)
        {
            UnityEngine.Debug.Log($"<color=green>Method 1 allocates LESS memory</color>");
        }
        else if (allocated2 < allocated1)
        {
            UnityEngine.Debug.Log($"<color=green>Method 2 allocates LESS memory</color>");
        }
        else
        {
            UnityEngine.Debug.Log("Both methods have similar memory allocations");
        }
    }
    
    [ContextMenu("Full Performance Report")]
    void FullReport()
    {
        UnityEngine.Debug.Log("╔════════════════════════════════════════╗");
        UnityEngine.Debug.Log("║   FULL PERFORMANCE COMPARISON REPORT   ║");
        UnityEngine.Debug.Log("╚════════════════════════════════════════╝");
        
        CompareMethods();
        UnityEngine.Debug.Log("");
        BenchmarkMethods();
        UnityEngine.Debug.Log("");
        MemoryProfile();
        
        UnityEngine.Debug.Log("═══════════════════════════════════════════");
    }
    
    void DebugMaterialProperties(Material mat)
    {
        Shader shader = mat.shader;
        int propertyCount = shader.GetPropertyCount();
    
        UnityEngine.Debug.Log($"Material has {propertyCount} properties:");
    
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = shader.GetPropertyName(i);
            var propType = shader.GetPropertyType(i);
            UnityEngine.Debug.Log($"{i}: {propName} ({propType})");
        }
    }
}