using System;
using System.Linq;

namespace ChickenNuget.Data
{
    public class Path
    {
        private readonly char _separator;
        public string[] PathSegments { get; }
        public string FileName => PathSegments.Last();
        public bool CaseSensitive { get; }
        public string FullName => string.Join(_separator, PathSegments);

        public string FileExtension
        {
            get
            {
                var parts = FileName.Split('.');
                return parts.Length > 1 ? parts.LastOrDefault() : null;
            }
        }

        public Path(string path, bool caseSensitive) : this(path, caseSensitive, '/')
        {
        }

        public Path(string path, bool caseSensitive, char separator)
        {
            _separator = separator;
            PathSegments = path.Split(_separator);
            CaseSensitive = caseSensitive;
        }

        public bool IsInSameDirectory(Path path)
        {
            var culture = path.CaseSensitive && this.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (path.PathSegments.Length != this.PathSegments.Length)
                return false;

            for (var i = 0; i < path.PathSegments.Length - 1; i++)
            {
                var a = path.PathSegments[i];
                var b = this.PathSegments[i];

                if (!a.Equals(b, culture))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public static class PathExtensions
    {
        public static bool IsInSameDirectory(this Path path1, string strPath2, bool? caseSensitive = null)
        {
            var path2 = new Path(strPath2, caseSensitive ?? path1.CaseSensitive);
            return path1.IsInSameDirectory(path2);
        }

        public static Path AsPath(this string str, bool caseSensitive)
        {
            return new Path(str, caseSensitive);
        }
    }
}