using System;

namespace DataAcquisition.Shared
{
    [Flags]
    public enum Genre : uint
    {
        Empty = 0,
        African = 1 << 0,
        Asian = 1 << 1,
        AvantGardeOrExperimental = 1 << 2,
        Blues = 1 << 3,
        Classical = 1 << 4,
        Country = 1 << 5,
        EasyListening = 1 << 6,
        Electronic = 1 << 7,
        Folk = 1 << 8,
        HipHop = 1 << 9,
        Jazz = 1 << 10,
        LatinOrCarribean = 1 << 11,
        Metal = 1 << 12,
        MiddleEastern = 1 << 13,
        Pop = 1 << 14,
        Punk = 1 << 15,
        RnBOrSoul = 1 << 16,
        Rock = 1 << 17
    };

    public static class GenreExtensions
    {
        public static string ToSerializedString(this Genre genre)
        {
            return ((uint)genre).ToString();
        }

        public static Genre Parse(string s)
        {
            if (TryParse(s, out var genre))
                return genre;
            else
                throw new ArgumentException("Invalid value passed!");
        }

        public static bool TryParse(string s, out Genre value)
        {
            value = default;
            var successful = uint.TryParse(s, out uint intermediate);
            if (successful)
                value = (Genre)intermediate;

            return successful;
        }
    }
}
