using System;
using System.Reflection;
using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    // DMCS compat (Nadi, 2026-11-17): a heel cut raises DMCS's adrenaline, killing a
    // crawling NPC with a cut tendon raises it more. DMCS owns the meter; Achilles only
    // reports the amount through DMCS.Adrenaline.AddExternal(float, string).
    public class DmcsCompatRulesTests
    {
        private static KillInput CrippledPlayerKill() => new KillInput
        {
            CompatOn = true,
            TargetIsPlayer = false,
            WasCrippled = true,
            KillIsCrippleHit = false,
            DoneByPlayer = true,
            Amount = 35f,
        };

        [Fact]
        public void Heel_cut_gives_its_amount()
        {
            Assert.Equal(20f, DmcsCompatRules.HeelCutGain(compatOn: true, amount: 20f));
        }

        [Fact]
        public void Heel_cut_gives_nothing_with_compat_off()
        {
            Assert.Equal(0f, DmcsCompatRules.HeelCutGain(compatOn: false, amount: 20f));
        }

        [Fact]
        public void Heel_cut_amount_zero_gives_nothing()
        {
            Assert.Equal(0f, DmcsCompatRules.HeelCutGain(compatOn: true, amount: 0f));
        }

        [Fact]
        public void Player_killing_a_crippled_npc_gives_the_kill_amount()
        {
            Assert.Equal(35f, DmcsCompatRules.KillGain(CrippledPlayerKill()));
        }

        [Fact]
        public void Kill_gives_nothing_with_compat_off()
        {
            var input = CrippledPlayerKill();
            input.CompatOn = false;

            Assert.Equal(0f, DmcsCompatRules.KillGain(input));
        }

        [Fact]
        public void Kill_of_an_npc_that_was_not_crippled_gives_nothing()
        {
            var input = CrippledPlayerKill();
            input.WasCrippled = false;

            Assert.Equal(0f, DmcsCompatRules.KillGain(input));
        }

        [Fact]
        public void Bleeding_out_or_any_kill_not_done_by_the_player_gives_nothing()
        {
            // The bleed-out ends with Creature.Kill(): a CollisionInstance without source,
            // caster or target, so it has no attacker.
            var input = CrippledPlayerKill();
            input.DoneByPlayer = false;

            Assert.Equal(0f, DmcsCompatRules.KillGain(input));
        }

        [Fact]
        public void A_heel_cut_that_kills_at_once_is_not_an_execution()
        {
            // The cut cripples at the hit's start, then its damage kills: the NPC never crawled.
            var input = CrippledPlayerKill();
            input.KillIsCrippleHit = true;

            Assert.Equal(0f, DmcsCompatRules.KillGain(input));
        }

        [Fact]
        public void The_player_dying_gives_nothing()
        {
            var input = CrippledPlayerKill();
            input.TargetIsPlayer = true;

            Assert.Equal(0f, DmcsCompatRules.KillGain(input));
        }

        [Fact]
        public void Kill_amount_zero_gives_nothing()
        {
            var input = CrippledPlayerKill();
            input.Amount = 0f;

            Assert.Equal(0f, DmcsCompatRules.KillGain(input));
        }

        [Fact]
        public void Defaults_make_the_execution_worth_more_than_the_cut()
        {
            Assert.True(OptionSpecs.CrippledKillAdrenaline.Default > OptionSpecs.HeelCutAdrenaline.Default);
            Assert.Equal(20f, OptionSpecs.HeelCutAdrenaline.Default);
            Assert.Equal(35f, OptionSpecs.CrippledKillAdrenaline.Default);
        }

        [Theory]
        [InlineData(0f, "0")]
        [InlineData(35f, "35")]
        public void Points_are_labelled_as_whole_numbers(float value, string label)
        {
            Assert.Equal(label, OptionSpecs.HeelCutAdrenaline.Label(value));
        }
    }

    // The bridge finds DMCS by name among the loaded assemblies (the game loads every mod
    // with Assembly.Load into one AppDomain), so Achilles has no hard dependency on it.
    public class DmcsBridgeTests
    {
        private static readonly Assembly Self = typeof(DmcsBridgeTests).Assembly;
        private static readonly string SelfName = Self.GetName().Name;

        [Fact]
        public void Finds_the_method_and_calls_it()
        {
            FakeAdrenaline.Reset(accept: true);
            var target = DmcsBridge.Resolve(new[] { Self }, SelfName, typeof(FakeAdrenaline).FullName, "AddExternal");

            Assert.NotNull(target);
            Assert.Equal(DmcsGainOutcome.Applied, DmcsBridge.Invoke(target, 20f, "AchillesHeelCut"));
            Assert.Equal(20f, FakeAdrenaline.LastAmount);
            Assert.Equal("AchillesHeelCut", FakeAdrenaline.LastSource);
        }

        [Fact]
        public void DMCS_refusing_the_gain_is_reported()
        {
            FakeAdrenaline.Reset(accept: false);
            var target = DmcsBridge.Resolve(new[] { Self }, SelfName, typeof(FakeAdrenaline).FullName, "AddExternal");

            Assert.Equal(DmcsGainOutcome.Refused, DmcsBridge.Invoke(target, 20f, "AchillesHeelCut"));
        }

        [Fact]
        public void Missing_assembly_resolves_to_nothing()
        {
            Assert.Null(DmcsBridge.Resolve(new[] { Self }, "NoSuchMod", typeof(FakeAdrenaline).FullName, "AddExternal"));
        }

        [Fact]
        public void Missing_type_resolves_to_nothing()
        {
            Assert.Null(DmcsBridge.Resolve(new[] { Self }, SelfName, "DMCS.NoSuchType", "AddExternal"));
        }

        [Fact]
        public void Missing_or_differently_shaped_method_resolves_to_nothing()
        {
            Assert.Null(DmcsBridge.Resolve(new[] { Self }, SelfName, typeof(FakeAdrenaline).FullName, "NoSuchMethod"));
            Assert.Null(DmcsBridge.Resolve(new[] { Self }, SelfName, typeof(FakeAdrenalineWrongShape).FullName, "AddExternal"));
        }

        [Fact]
        public void No_target_means_not_installed()
        {
            Assert.Equal(DmcsGainOutcome.NotInstalled, DmcsBridge.Invoke(null, 20f, "AchillesHeelCut"));
        }

        [Fact]
        public void A_throwing_target_is_reported_as_failed()
        {
            var target = DmcsBridge.Resolve(new[] { Self }, SelfName, typeof(FakeAdrenalineThrows).FullName, "AddExternal");

            Assert.Equal(DmcsGainOutcome.Failed, DmcsBridge.Invoke(target, 20f, "AchillesHeelCut"));
        }

        [Fact]
        public void The_contract_names_match_DMCS()
        {
            Assert.Equal("DMCS", DmcsBridge.AssemblyName);
            Assert.Equal("DMCS.Adrenaline", DmcsBridge.TypeName);
            Assert.Equal("AddExternal", DmcsBridge.MethodName);
        }
    }

    public static class FakeAdrenaline
    {
        private static bool accept;

        public static float LastAmount { get; private set; }
        public static string LastSource { get; private set; }

        public static void Reset(bool accept)
        {
            FakeAdrenaline.accept = accept;
            LastAmount = 0f;
            LastSource = null;
        }

        public static bool AddExternal(float amount, string source)
        {
            LastAmount = amount;
            LastSource = source;
            return accept;
        }
    }

    public static class FakeAdrenalineWrongShape
    {
        public static bool AddExternal(int amount) => amount > 0;
    }

    public static class FakeAdrenalineThrows
    {
        public static bool AddExternal(float amount, string source) => throw new InvalidOperationException("boom");
    }
}
