using System.IO;
using System.Windows;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;
using static ModernDiceSmokeHelper;

internal static class KniffelSmoke
{
    internal static async Task Run(MainWindow window, string output, PlayerProfileState profile)
    {
        Require(window.Modes.Modes.Any(x => x.Id == "kniffel"), "Midnight Dice is missing from the lobby.");
        profile.OwnedCosmeticIds.Add("diceset-aurora");
        profile.EquippedDiceSetId = "diceset-aurora";
        ModernDiceView view = await Open(window);
        CoreWebView2 web = view.Browser.CoreWebView2!;
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
            "Midnight Dice opened a second native window.");
        Require(await JavaScript(web,
            "Boolean(document.querySelectorAll('.md-die').length===5"
            + "&&document.querySelectorAll('.md-category').length===13"
            + "&&document.querySelector('[data-testid=\"modern-dice-app\"]')?.dataset.mode==='casual')"),
            "The complete React casual dice sheet was not shown.");
        Require(await JavaScript(web,
            "Boolean(document.querySelector('.md-die-image')?.outerHTML.includes('#389E97'))"),
            "The equipped Aurora dice palette was not projected into the React dice.");
        Require(!await JavaScript(web,
            "Boolean(!document.querySelector('[data-testid=\"dice-commit\"]')?.disabled)"),
            "Scoring is available without a roll.");

        await Capture(web, Path.Combine(output, "kniffel-modern-initial.png"));
        window.Width = 1080; window.Height = 720;
        await Task.Delay(160);
        Require(await JavaScript(web,
            "(()=>{const dice=document.querySelectorAll('.md-die');"
            + "const paper=document.querySelector('.md-scorecard');return dice.length===5"
            + "&&Array.from(dice).every(x=>x.getBoundingClientRect().width>=45)"
            + "&&paper.getBoundingClientRect().right<=innerWidth+1})()"),
            "Dice or score sheet are clipped at 1080×720.");
        await Capture(web, Path.Combine(output, "kniffel-modern-1080x720.png"));
        window.Width = 1280; window.Height = 900;

        if (await JavaScript(web, "Boolean(document.querySelector('[data-testid=\"dice-settings-panel\"]'))"))
            await Click(web, "dice-settings", false);
        // The WPF suite leaves per-game animations disabled; ensure deterministic
        // first-roll smoke independent of the player's presentation preference.
        await Click(web, "dice-settings", false);
        bool animationEnabled = await JavaScript(web,
            "document.querySelectorAll('[data-testid=\"dice-settings-panel\"] input')[1]?.checked===true");
        await Click(web, "dice-settings", false);
        if (animationEnabled) await SetAnimations(web, false);

        await Click(web, "dice-roll");
        Require(Game(window).RollsUsed == 1, "The first native dice roll was not committed.");
        await Click(web, "dice-die-0");
        int face = Game(window).Dice[0];
        Require(Game(window).Held[0], "React did not hold die one.");
        await Click(web, "dice-roll");
        Require(Game(window).RollsUsed == 2 && Game(window).Dice[0] == face,
            "Rolling the native dice changed a held die.");
        await Select(web, "Chance");
        Require(Game(window).FilledCount == 0, "Selecting Chance prematurely recorded points.");
        await Capture(web, Path.Combine(output, "kniffel-modern-selection.png"));
        await Click(web, "dice-commit");
        Require(Game(window).FilledCount == 1 && Game(window).RollsUsed == 0,
            "Recording Chance did not advance the authoritative turn.");

        // Resume precisely the same committed faces, hold switches and roll allowance.
        await Click(web, "dice-roll");
        await Click(web, "dice-die-1");
        int[] faces = Game(window).Dice.ToArray();
        await Click(web, "dice-back", false);
        await WaitForExit(window);
        Require(Find<CasinoLobbyView>(window).Any(x => x.IsVisible),
            "The casino lobby is not visible after the React Midnight Dice exit.");

        view = await Open(window);web = view.Browser.CoreWebView2!;
        Require(Game(window).RollsUsed == 1 && Game(window).Held[1]
            && faces.SequenceEqual(Game(window).Dice), "Re-entry did not restore the saved held dice.");

        long xpBefore = profile.LifetimeExperience;
        foreach (KniffelCategory category in KniffelScoring.Categories.Where(x => x != KniffelCategory.Chance))
        {
            if (!Game(window).CanScore) await Click(web, "dice-roll");
            int preview = Game(window).Preview(category);
            await Select(web, category.ToString());
            await Click(web, preview > 0 ? "dice-commit" : "dice-scratch");
        }
        Require(Game(window).IsComplete, "The 13-category React sheet never completed.");
        long xpEarned = KniffelProgression.PlayerExperience(Game(window).Total);
        Require(profile.LifetimeExperience == xpBefore + xpEarned,
            "The casual completion XP was not awarded exactly once.");
        await WaitFor(window, () => true, "");
        for (int i = 0; i < 120; i++)
        {
            if (await JavaScript(web,
                "Boolean(document.querySelector('[data-testid=\"dice-result-panel\"]'))")) break;
            await Task.Delay(70);
        }
        Require(await JavaScript(web,
            "Boolean(document.querySelector('[data-testid=\"dice-result-panel\"]')"
            + "&&document.body.textContent.includes('XP'))"),
            "The completed React sheet did not expose its final receipt and XP.");
        await Click(web, "dice-result-close", false);
        await Capture(web, Path.Combine(output, "kniffel-modern-complete.png"));

        await Click(web, "dice-back", false);
        await WaitForExit(window);
        view = await Open(window);web = view.Browser.CoreWebView2!;
        Require(Game(window).IsComplete && profile.LifetimeExperience == xpBefore + xpEarned,
            "Reopening the completed game awarded duplicate XP.");
        await Click(web, "dice-result-close", false);
        await Click(web, "dice-setup", false);
        await Click(web, "dice-casual-new");
        Require(Game(window).FilledCount == 0 && Game(window).RollsUsed == 0,
            "Starting a fresh casual sheet did not clear the previous game.");

        // A roll is persisted before animation; closing mid-flight cannot grant a reroll.
        await SetAnimations(web, true);
        await Click(web, "dice-roll");
        Require(Game(window).RollsUsed == 1,
            "Animated roll did not settle natively before the animation.");
        faces = Game(window).Dice.ToArray();
        await Click(web, "dice-back", false);
        await WaitForExit(window);
        view = await Open(window);web = view.Browser.CoreWebView2!;
        Require(Game(window).RollsUsed == 1 && faces.SequenceEqual(Game(window).Dice),
            "Exiting during dice animation changed the committed outcome.");

        await Click(web, "dice-roll");
        Require(Game(window).RollsUsed == 2, "Reroll after re-entry was lost.");
        faces = Game(window).Dice.ToArray();
        await Task.Delay(1100);
        Require(faces.SequenceEqual(Game(window).Dice), "Cosmetic dice animation altered the RNG result.");
        await Capture(web, Path.Combine(output, "kniffel-modern-animated-settled.png"));
        await Click(web, "dice-setup", false);
        await Click(web, "dice-casual-new", false);
        Require(await JavaScript(web,
            "Boolean(document.querySelector('[data-testid=\"dice-casual-confirm\"]'))"),
            "Starting over during an unfinished casual game skipped confirmation.");
        await Click(web, "dice-setup-close", false);
        Require(Game(window).RollsUsed == 2, "Canceling a restart lost the committed dice.");

        await SetAnimations(web, false);
        await Click(web, "dice-back", false);
        await WaitForExit(window);
        File.WriteAllText(Path.Combine(output, "kniffel-result.txt"),
            "PASS: modern 5-dice/13-score React route, equipped Aurora cosmetics, hold/reroll, preview and confirm, 13 turns and XP, durable exact resume, result receipt, new-game confirmation, animation independence, 1080x720 layout.");
    }
}
