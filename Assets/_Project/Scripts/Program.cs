// Program.cs (테스트용)
using System;
using ReflexPuzzle.Control;
using ReflexPuzzle.Entity;

class Program
{
    static void Main(string[] args)
    {
        GridGenerator generator = new GridGenerator();

        Console.WriteLine("=== [Mode: Classic] Level 5 Test ===");
        PrintStage(generator.CreateStage(5, GameMode.Classic));

        Console.WriteLine("\n=== [Mode: Color] Level 15 Test ===");
        PrintStage(generator.CreateStage(15, GameMode.Color));

        Console.WriteLine("\n=== [Mode: Mixed] Level 20 Test ===");
        PrintStage(generator.CreateStage(20, GameMode.Mixed));
    }

    static void PrintStage(StageInfo stage)
    {
        Console.WriteLine($"Level: {stage.Level} | Grid: {stage.GridSize}x{stage.GridSize} | Time: {stage.TimeLimit:F2}s");
        int count = 0;
        foreach (var cell in stage.Cells)
        {
            string trapMark = cell.IsTrap ? "[TRAP]" : "";
            Console.Write($"[{cell.Number}(C:{cell.ColorID}){trapMark}] \t");

            count++;
            if (count % stage.GridSize == 0) Console.WriteLine();
        }
    }
}