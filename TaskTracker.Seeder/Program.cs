using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Persistence;

// Usage:
//   dotnet run --project TaskTracker.Seeder -- --db ./TaskTracker/tasktracker.db --count 42536
// If --db is omitted, uses ./TaskTracker/tasktracker.db by default
// If --count is omitted, defaults to 42536

var argsDict = ParseArgs(args);
var dbPath = argsDict.TryGetValue("--db", out var p) ? p : Path.Combine(Directory.GetCurrentDirectory(), "TaskTracker", "tasktracker.db");
var count = argsDict.TryGetValue("--count", out var cStr) && int.TryParse(cStr, out var c) ? Math.Max(c, 42536) : 42536;

Console.WriteLine($"Seeding database: {dbPath} with {count} TaskItems...");

var options = new DbContextOptionsBuilder<TaskTrackerDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
using var db = new TaskTrackerDbContext(options);
db.Database.Migrate(); // ensure schema

// Ensure urgency levels exist (migrations already seed 1..3)
var urgencies = db.UrgencyLevels.ToDictionary(u => u.Id);
if (urgencies.Count == 0)
{
    db.UrgencyLevels.AddRange(
        new UrgencyLevel { Id = 1, Name = "Low", SortOrder = 1, IsActive = true },
        new UrgencyLevel { Id = 2, Name = "Medium", SortOrder = 2, IsActive = true },
        new UrgencyLevel { Id = 3, Name = "High", SortOrder = 3, IsActive = true }
    );
    db.SaveChanges();
}

var rnd = new Random(20260816); // deterministic
var now = DateTime.UtcNow;

int batchSize = 1000;
int total = 0;
var titles = new HashSet<string>(StringComparer.Ordinal);

while (total < count)
{
    var batch = new List<TaskItem>(batchSize);
    for (int i = 0; i < batchSize && total < count; i++, total++)
    {
        // Distribute stage roughly: Started(40%), InProgress(35%), Finished(25%)
        var stageRoll = rnd.NextDouble();
        var stage = stageRoll < 0.40 ? TaskStage.Started : stageRoll < 0.75 ? TaskStage.InProgress : TaskStage.Finished;

        // Distribute urgency: 1/2/3 ~ 33/33/34
        var urgency = (rnd.Next(1, 4));

        // Create unique title among active items
        string title;
        do
        {
            title = $"Task #{total + 1} - {RandomWord(rnd)} {RandomWord(rnd)}";
        } while (!titles.Add(title));

        // Optional notes ~ 20%
        string? notes = rnd.NextDouble() < 0.2 ? $"Notes {RandomWord(rnd)} {RandomWord(rnd)}" : null;

        // Optional deadline: ~60% have deadlines, spread past/near/future
        DateTime? deadline = null;
        var deadlineRoll = rnd.NextDouble();
        if (deadlineRoll < 0.6)
        {
            // Range ±90 days
            deadline = now.AddDays(rnd.Next(-90, 91));
        }

        var created = now.AddDays(-rnd.Next(0, 180)).AddMinutes(-rnd.Next(0, 1440));
        var updated = created.AddDays(rnd.Next(0, 90));

        batch.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Notes = notes,
            Stage = stage,
            UrgencyLevelId = urgency,
            Deadline = deadline,
            CreatedAtUtc = created,
            UpdatedAtUtc = updated,
            IsDeleted = false,
            DeletedAtUtc = null
        });
    }

    db.TaskItems.AddRange(batch);
    db.SaveChanges();
    Console.WriteLine($"Seeded: {total}/{count}");
}

Console.WriteLine("Seeding complete.");

static Dictionary<string, string> ParseArgs(string[] args)
{
    var dict = new Dictionary<string, string>();
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].StartsWith("--"))
        {
            var key = args[i];
            var val = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : "";
            dict[key] = val;
        }
    }
    return dict;
}

static string RandomWord(Random rnd)
{
    string[] words = new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliet", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu" };
    return words[rnd.Next(words.Length)];
}