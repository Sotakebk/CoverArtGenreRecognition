using System.Collections.Generic;

namespace DataAcquisition.Shared.Genres
{
    public static partial class GenreAggregate
    {
        public static readonly HashSet<string> IgnoredGenres = new()
        {
            "instrumental",
            "production music",
            "western"
        };
    }
}
