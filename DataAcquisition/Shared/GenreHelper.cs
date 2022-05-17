using System;
using System.Linq;
using static DataAcquisition.Shared.Genres.GenreAggregate;

namespace DataAcquisition.Shared
{
    public static class GenreHelper
    {
        public static Genre GetGenreFromString(string s)
        {
            s = s.ToLowerInvariant();
            Genre genre = Genre.Empty;

            if (AfricanGenres.Contains(s))
                genre |= Genre.African;
            if (AsianGenres.Contains(s))
                genre |= Genre.Asian;
            if (AvantGardeOrExeprimentalGenres.Contains(s))
                genre |= Genre.AvantGardeOrExperimental;
            if (BluesGenres.Contains(s))
                genre |= Genre.Blues;
            if (ClassicalGenres.Contains(s))
                genre |= Genre.Classical;
            if (CountryGenres.Contains(s))
                genre |= Genre.Country;
            if (EasyListeningGenres.Contains(s))
                genre |= Genre.EasyListening;
            if (ElectronicGenres.Contains(s))
                genre |= Genre.Electronic;
            if (FolkGenres.Contains(s))
                genre |= Genre.Folk;
            if (HipHopGenres.Contains(s))
                genre |= Genre.HipHop;
            if (JazzGenres.Contains(s))
                genre |= Genre.Jazz;
            if (LatinOrCarribeanGenres.Contains(s))
                genre |= Genre.LatinOrCarribean;
            if (MetalGenres.Contains(s))
                genre |= Genre.Metal;
            if (MiddleEasternGenres.Contains(s))
                genre |= Genre.MiddleEastern;
            if (PopGenres.Contains(s))
                genre |= Genre.Pop;
            if (PunkGenres.Contains(s))
                genre |= Genre.Punk;
            if (RnBOrSoulGenres.Contains(s))
                genre |= Genre.RnBOrSoul;
            if (RockGenres.Contains(s))
                genre |= Genre.Rock;

            return genre;
        }

        public static bool IsStringInAnyGroup(string s)
        {
            return (IgnoredGenres.Contains(s)
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
            || RockGenres.Contains(s));
        }

        private static byte[] _lookupTable;
        private static byte[] LookupTable => (_lookupTable ??= GenerateLookupTable());

        private static byte[] GenerateLookupTable()
        {
            var table = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                table[i] = (byte)CountSetBits(i);
            }
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

        private const int Mask = 0b11111111;

        public static int CountSetFlags(Genre genre)
        {
            var bytes = (int)genre;
            var sum = 0;
            for (int i = 0; i < 4; i++)
            {
                sum += LookupTable[bytes & Mask];
                bytes >>= 8;
            }
            return sum;
        }

        public static string[] GetGenreNames()
        {
            return Enum.GetNames(typeof(Genre)).Skip(1).ToArray();
        }

        public static int[] GetGenresAs01s(Genre genre)
        {
            int conv(Genre g, Genre f) => g.HasFlag(f) ? 1 : 0;

            return new int[] {
                conv(genre, Genre.African),
                conv(genre, Genre.Asian),
                conv(genre, Genre.AvantGardeOrExperimental),
                conv(genre, Genre.Blues),
                conv(genre, Genre.Classical),
                conv(genre, Genre.Country),
                conv(genre, Genre.EasyListening),
                conv(genre, Genre.Electronic),
                conv(genre, Genre.Folk),
                conv(genre, Genre.HipHop),
                conv(genre, Genre.Jazz),
                conv(genre, Genre.LatinOrCarribean),
                conv(genre, Genre.Metal),
                conv(genre, Genre.MiddleEastern),
                conv(genre, Genre.Pop),
                conv(genre, Genre.Punk),
                conv(genre, Genre.RnBOrSoul),
                conv(genre, Genre.Rock)
            };
        }
    }
}
