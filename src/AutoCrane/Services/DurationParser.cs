// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal class DurationParser : IDurationParser
    {
        public DurationParser()
        {
        }

        public TimeSpan? Parse(string duration)
        {
            duration = duration.Trim();
            if (duration.Length < 2)
            {
                return null;
            }

            var unit = duration[duration.Length - 1];
            TimeSpan multiplier;
            switch (unit)
            {
                case 'd':
                    multiplier = TimeSpan.FromDays(1);
                    break;
                case 'h':
                    multiplier = TimeSpan.FromHours(1);
                    break;
                case 'm':
                    multiplier = TimeSpan.FromMinutes(1);
                    break;
                case 's':
                    multiplier = TimeSpan.FromSeconds(1);
                    break;
                default:
                    return null;
            }

            if (int.TryParse(duration.AsSpan(0, duration.Length - 1), out var amount))
            {
                return multiplier * amount;
            }

            return null;
        }
    }
}
