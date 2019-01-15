using System.Linq;
using System.Text;

namespace ChickenNuget.Data
{
    public static class StringExtensions
    {
        public static string RemoveBOMCharacter(this string value)
        {
            var bomChar = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble()).ToCharArray();
            var valueChars = value.ToCharArray(0, bomChar.Length);
            if (valueChars.SequenceEqual(bomChar)) // StartsWith is a lie
                value = new string(value.ToCharArray().Skip(1).ToArray());
            return value;
        }
    }
}