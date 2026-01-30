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
    // Program.cs에 추가
    static void TestMatchEngine()
    {
        Console.WriteLine("\n=== MatchEngine Logic Test ===");

        // 1. 데이터 생성 (Control)
        GridGenerator gen = new GridGenerator();
        StageInfo stage = gen.CreateStage(10, GameMode.Mixed); // Mixed 모드

        // 2. 엔진 초기화 (Control)
        MatchEngine engine = new MatchEngine();
        engine.Initialize(stage);

        Console.WriteLine($"Stage Loaded: {stage.Cells.Count} cells.");

        // 3. 시뮬레이션: 유저가 1, 2, 3... 순서대로 누른다고 가정
        // 정답 큐를 알 수 없으니(private), stage.Cells를 순회하며 시뮬레이션

        // 정렬해서 작은 숫자부터 눌러봅니다.
        stage.Cells.Sort((a, b) => a.Number.CompareTo(b.Number));

        foreach (var cell in stage.Cells)
        {
            Console.Write($"Touch [{cell.Number}] (Trap:{cell.IsTrap}) -> ");
            var result = engine.SubmitInput(cell);
            Console.WriteLine(result);

            if (result == MatchResult.Fail_Wrong || result == MatchResult.StageClear)
            {
                break;
            }
        }
    }
}