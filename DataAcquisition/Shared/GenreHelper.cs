using System;
using System.Linq;
using static DataAcquisition.Shared.Genres.GenreAggregate;

namespace DataAcquisition.Shared
{
    public static class GenreHelper
    {
        private const int Mask = 0b11111111;

        private static byte[] _lookupTable;
        private static byte[] LookupTable => _lookupTable ??= GenerateLookupTable();

        public static GenreFlags GetGenreFromString(string s)
        {
            s = s.ToLowerInvariant();
            var genreFlags = GenreFlags.Empty;

            if (AfricanGenres.Contains(s))
                genreFlags |= GenreFlags.African;
            if (AsianGenres.Contains(s))
                genreFlags |= GenreFlags.Asian;
            if (AvantGardeOrExeprimentalGenres.Contains(s))
                genreFlags |= GenreFlags.AvantGardeOrExperimental;
            if (BluesGenres.Contains(s))
                genreFlags |= GenreFlags.Blues;
            if (ClassicalGenres.Contains(s))
                genreFlags |= GenreFlags.Classical;
            if (CountryGenres.Contains(s))
                genreFlags |= GenreFlags.Country;
            if (EasyListeningGenres.Contains(s))
                genreFlags |= GenreFlags.EasyListening;
            if (ElectronicGenres.Contains(s))
                genreFlags |= GenreFlags.Electronic;
            if (FolkGenres.Contains(s))
                genreFlags |= GenreFlags.Folk;
            if (HipHopGenres.Contains(s))
                genreFlags |= GenreFlags.HipHop;
            if (JazzGenres.Contains(s))
                genreFlags |= GenreFlags.Jazz;
            if (LatinOrCarribeanGenres.Contains(s))
                genreFlags |= GenreFlags.LatinOrCarribean;
            if (MetalGenres.Contains(s))
                genreFlags |= GenreFlags.Metal;
            if (MiddleEasternGenres.Contains(s))
                genreFlags |= GenreFlags.MiddleEastern;
            if (PopGenres.Contains(s))
                genreFlags |= GenreFlags.Pop;
            if (PunkGenres.Contains(s))
                genreFlags |= GenreFlags.Punk;
            if (RnBOrSoulGenres.Contains(s))
                genreFlags |= GenreFlags.RnBOrSoul;
            if (RockGenres.Contains(s))
                genreFlags |= GenreFlags.Rock;

            return genreFlags;
        }

        public static bool IsStringInAnyGroup(string s)
        {
            return IgnoredGenres.Contains(s)
                   || AfricanGenres.Contains(s)
                   || AsianGenres.Contains(s)
                   || AvantGardeOrExeprimentalGenres.Contains(s)
                   || BluesGenres.Contains(s)
                   || ClassicalGenres.Contains(s)
                   || CountryGenres.Contains(s)
                   || EasyListeningGenres.Contains(s)
                   || ElectronicGenres.Contains(s)
                   || FolkGenres.Contains(s)
                   || HipHopGenres.Contains(s)
                   || JazzGenres.Contains(s)
                   || LatinOrCarribeanGenres.Contains(s)
                   || MetalGenres.Contains(s)
                   || MiddleEasternGenres.Contains(s)
                   || PopGenres.Contains(s)
                   || PunkGenres.Contains(s)
                   || RnBOrSoulGenres.Contains(s)
                   || RockGenres.Contains(s);
        }

        private static byte[] GenerateLookupTable()
        {
            var table = new byte[256];
            for (var i = 0; i < 256; i++) table[i] = (byte)CountSetBits(i);
            return table;
        }

        private static int CountSetBits(int b)
        {
            var sum = 0;
            while (b != 0)
            {
                sum += b & 1;
                b >>= 1;
            }

            return sum;
        }

        public static int CountSetFlags(GenreFlags genreFlags)
        {
            var bytes = (int)genreFlags;
            var sum = 0;
            sum += LookupTable[bytes & Mask];
            sum += LookupTable[(bytes >> 8) & Mask];
            sum += LookupTable[(bytes >> 16) & Mask];
            sum += LookupTable[(bytes >> 24) & Mask];
            return sum;
        }

        public static string[] GetGenreNames()
        {
            return Enum.GetNames(typeof(GenreFlags)).Skip(1).ToArray();
        }

        public static int[] GetGenresAs01Array(GenreFlags genreFlags)
        {
            static int Conv(GenreFlags g, GenreFlags f)
            {
                return g.HasFlag(f) ? 1 : 0;
            }

            return new[]
            {
                Conv(genreFlags, GenreFlags.African),
                Conv(genreFlags, GenreFlags.Asian),
                Conv(genreFlags, GenreFlags.AvantGardeOrExperimental),
                Conv(genreFlags, GenreFlags.Blues),
                Conv(genreFlags, GenreFlags.Classical),
                Conv(genreFlags, GenreFlags.Country),
                Conv(genreFlags, GenreFlags.EasyListening),
                Conv(genreFlags, GenreFlags.Electronic),
                Conv(genreFlags, GenreFlags.Folk),
                Conv(genreFlags, GenreFlags.HipHop),
                Conv(genreFlags, GenreFlags.Jazz),
                Conv(genreFlags, GenreFlags.LatinOrCarribean),
                Conv(genreFlags, GenreFlags.Metal),
                Conv(genreFlags, GenreFlags.MiddleEastern),
                Conv(genreFlags, GenreFlags.Pop),
                Conv(genreFlags, GenreFlags.Punk),
                Conv(genreFlags, GenreFlags.RnBOrSoul),
                Conv(genreFlags, GenreFlags.Rock)
            };
        }
    }
}
