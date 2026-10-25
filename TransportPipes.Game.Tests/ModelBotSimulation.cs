using System.Text;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// The model's player of 1.4 §17 on the real game (phase A: the bots learn to play like the model). It starts from
/// nothing (an HQ, a trading post and the starting gold), does at most two things a minute on the first evening and six
/// after, always the step that pays back soonest within five days, keeps research running and buys land as soon as its
/// level and gold allow (BalanceSimulation.Bot.ModelStep). Phase B's economy brings the model's numbers; until then the
/// report shows how the first hour goes on today's.
/// </summary>
[TestClass]
public class ModelBotSimulation
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// The model's days (§17's "Day by day"): the first evening two hours from 19:30 at two things a minute, then three
    /// visits a day (ten minutes at 8:00, forty-five at 19:30, ten at 22:30) at six. Slow: run it with TP_BALANCE=1
    /// (TP_BALANCE_DAYS, default 7).
    /// </summary>
    [TestMethod]
    public void TheModelsDays()
    {
        if (Environment.GetEnvironmentVariable("TP_BALANCE") != "1")
        {
            Assert.Inconclusive("Set TP_BALANCE=1 to run the model's days.");
        }
        var days = int.TryParse(Environment.GetEnvironmentVariable("TP_BALANCE_DAYS"), out var d) ? d : 7;
        var game = new TestGame(seed: 4242, tutorial: true);
        // The first evening starts at 19:30
        game.Time = game.Time.Date.AddHours(19.5);
        var company = game.NewCompany("Model", gold: game.World.StartingGold);
        var start = company.Gold;
        var bot = new BalanceSimulation.Bot(game, company);
        var report = new StringBuilder();
        report.AppendLine("| Day | Income a minute | Level | Buildings | Avg level | Parcels | Research | Spent of earned | Bank |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        bool Playing(int minute, out int perMinute)
        {
            perMinute = minute < 120 ? 2 : 6;
            if (minute < 120)
            {
                return true;
            }
            var clock = (minute + 19 * 60 + 30) % (24 * 60);
            return clock is >= 8 * 60 and < 8 * 60 + 10 or >= 19 * 60 + 30 and < 20 * 60 + 15 or >= 22 * 60 + 30 and < 22 * 60 + 40;
        }
        for (var minute = 0; minute < days * 24 * 60; minute++)
        {
            if (Playing(minute, out var perMinute))
            {
                for (var action = 0; action < perMinute && bot.ModelStep(); action++)
                {
                }
                game.Pass(1);
            }
            else
            {
                // Away: the world runs on in bigger steps
                game.Pass(5);
                minute += 4;
            }
            // Each day at midnight, as §17 counts its bank
            if ((minute + 19 * 60 + 30 + 1) % (24 * 60) == 0)
            {
                var income = game.World.GoldPerMinute().GetValueOrDefault(company.Id);
                var buildings = game.World.BuildingsOf(company.Id).Where(t => t.Type is not ("hq" or "tradingPost")).ToList();
                var earned = company.LifetimeEarnings;
                var spent = start + earned - company.Gold;
                report.AppendLine($"| {(minute + 19 * 60 + 30) / (24 * 60) + 1} | {income:N0} | {company.Level} | {buildings.Count} | {(buildings.Count == 0 ? 0 : buildings.Average(t => t.Level)):N1} | {game.EstateOf(company).UnlockedCount} | {company.Researched.Count} | {(earned <= 0 ? 0 : spent / (start + earned)):P0} | {(income <= 0 ? 0 : company.Gold / income / 60):N1} h |");
            }
        }
        foreach (var step in game.World.NextSteps(company, 12))
        {
            report.AppendLine($"- {step.Action} {step.Title}: {step.Cost:N0} g for +{step.GainPerMinute:N1}/min (wait {step.WaitMinutes:N0})");
        }
        foreach (var post in game.World.BuildingsOf(company.Id).Where(t => game.World.Content.Building(t.Type).Kind == Content.BuildingKind.TradingPost))
        {
            report.AppendLine($"- {post.Type} level {post.Level}: sells up to {game.World.TradingPostThroughput(post, game.World.Content.Building(post.Type)):N0}/min, full {game.World.TradingPostFull(post)}, upgrade {game.World.UpgradeCost(post, 1):N0} g, Max {game.World.SmartMax(company, post).Levels} levels");
        }
        var idle = game.World.BuildingsOf(company.Id).Select(t => game.World.IdleReasonOf(t).Cause).GroupBy(t => t).Select(t => $"{t.Key} ×{t.Count()}");
        report.AppendLine($"- idle: {string.Join(", ", idle)}; alerts: {string.Join(" | ", game.World.Alerts(company).Select(t => t.Text))}");
        var (parcelCost, parcelLevel) = game.World.NextParcel(company);
        report.AppendLine($"- next parcel {parcelCost:N0} g at Level {parcelLevel}; research {company.CurrentResearch ?? "none"}; points {company.ResearchPoints:N0}; grow {game.World.PlanGrowth(company).Parts.Count} parts, saving {game.World.PlanGrowth(company).Saving}");
        TestContext.WriteLine(report.ToString());
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "model-days.md"), report.ToString());
    }

    /// <summary>The first hour from nothing, minute by minute, at two things a minute</summary>
    [TestMethod]
    public void TheFirstHourFromNothing()
    {
        var game = new TestGame(seed: 4242, tutorial: true);
        var company = game.NewCompany("Model", gold: game.World.StartingGold);
        var bot = new BalanceSimulation.Bot(game, company);
        var report = new StringBuilder();
        report.AppendLine("| Minute | Income a minute | Level | Buildings | Gold | What happened |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- |");
        var incomeAt10 = 0.0;
        for (var minute = 0; minute < 60; minute++)
        {
            for (var action = 0; action < 2 && bot.ModelStep(); action++)
            {
            }
            game.Pass(1);
            var income = game.World.GoldPerMinute().GetValueOrDefault(company.Id);
            if (minute == 9)
            {
                incomeAt10 = income;
            }
            if ((minute + 1) % 5 == 0)
            {
                report.AppendLine($"| {minute + 1} | {income:N0} | {company.Level} | {game.World.BuildingsOf(company.Id).Count()} | {company.Gold:N0} | {bot.TakeNotes().TrimStart(' ', '·')} |");
            }
        }
        var final = game.World.GoldPerMinute().GetValueOrDefault(company.Id);
        report.AppendLine();
        report.AppendLine($"Buildings: {string.Join(", ", game.World.BuildingsOf(company.Id).GroupBy(t => t.Type).OrderByDescending(t => t.Count()).Select(t => $"{t.Key} ×{t.Count()}"))}.");
        report.AppendLine($"Research: {company.Researched.Count} done, {company.CurrentResearch ?? "none"} running.");
        TestContext.WriteLine(report.ToString());
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "model-first-hour.md"), report.ToString());

        Assert.IsGreaterThan(0, incomeAt10, "it earns within ten minutes");
        Assert.IsGreaterThan(incomeAt10 * 1.5, final, "and keeps growing");
        Assert.IsGreaterThanOrEqualTo(10, game.World.BuildingsOf(company.Id).Count(), "copies, not one building levelled to the sky");
        // Spending keeps up with income: at most half an hour of it in hand. (The model's Lab at minute 20 needs Company
        // Level from gold earned, phase B: today's XP brings Level 3 in this hour.)
        Assert.IsLessThanOrEqualTo(final * 30 + game.World.StartingGold, company.Gold, $"gold in hand: {company.Gold:N0} at {final:N0}/min");
    }
}
