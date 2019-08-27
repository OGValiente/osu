﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuDifficultyAttributes : DifficultyAttributes
    {
        public double TapSR;
        internal double TapDiff;
        public double StreamNoteCount;
        public double[] MashLevels;
        public double[] TapSkills;

        public double AimSR;
        public double AimDiff;
        public double[] MissTPs;
        public double[] MissCounts;
        public double CheeseNoteCount;
        public double[] CheeseLevels;
        public double[] CheeseFactors;

        public double ApproachRate;
        public double OverallDifficulty;
        public int MaxCombo;
    }
}
