using System;

namespace ChickenNuget.Data
{
    public class NugetVersion : IComparable
    {
        protected bool Equals(NugetVersion other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NugetVersion) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Build;
                return hashCode;
            }
        }

        public NugetVersion(int major, int minor, int build, string suffix = null)
        {
            Major = major;
            Minor = minor;
            Build = build;
            Suffix = suffix;
        }

        public string VersionString => ToString();

        public int Major { get; }
        public int Minor { get; }
        public int Build { get; }
        public string Suffix { get; }
        public bool Prerelease => !string.IsNullOrWhiteSpace(Suffix);

        public override string ToString() => $"{Major}.{Minor}.{Build}{(Prerelease ? "-"+Suffix : "")}";

        public static bool operator >(NugetVersion a,NugetVersion b)
        {
            if ((object)a == null || (object)b == null) return false;
            return a.CompareTo(b) == -1;
        }

        public static bool operator <(NugetVersion a,NugetVersion b) => b > a;

        public static bool operator ==(NugetVersion a,NugetVersion b)
        {
            if ((object)a == null && (object)b == null) return true;
            if ((object)a == null || (object)b == null) return false;
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(NugetVersion a,NugetVersion b) => !(a == b);

        public int CompareTo(NugetVersion versionB) => ((IComparable) this).CompareTo(versionB);

        int IComparable.CompareTo(object obj)
        {
            var versionB = (NugetVersion) obj;
            if (this.Major > versionB.Major)
                return -1;
            if (this.Major < versionB.Major)
                return 1;
            if (this.Minor > versionB.Minor) 
                return -1;
            if (this.Minor < versionB.Minor) 
                return 1;
            if (this.Build > versionB.Build)
                return -1;
            if (this.Build < versionB.Build)
                return 1;
            return 0;
        }

        public static NugetVersion FromString(string str)
        {
            var versionSuffix = str.Split('-');
            if (versionSuffix.Length == 0)
                throw new FormatException();
            if (versionSuffix.Length > 2)
                throw new FormatException();

            var versionNumbers = versionSuffix[0].Split('.');
            if (versionNumbers.Length != 3)
                throw new FormatException();

            var major = int.Parse(versionNumbers[0]);
            var minor = int.Parse(versionNumbers[1]);
            var build = int.Parse(versionNumbers[2]);

            string suffix = null;
            if (versionSuffix.Length == 2)
            {
                var suffixString = versionSuffix[1];
                if (suffixString[0] != '-')
                    throw new FormatException();
                suffix = suffixString.Substring(1);
            }

            return new NugetVersion(major, minor, build, suffix);
        }
    }
}