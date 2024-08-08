using Newtonsoft.Json;

namespace BOHO.Application.Util
{
    public static class StringExtensions
    {
        public static T Deserialize<T>(this string str)
        {
            if (str is null)
                return default;

            return JsonConvert.DeserializeObject<T>(str);
        }

        public static bool ToBool(this string str)
        {
            if (str is null)
                return false;

            if (bool.TryParse(str, out bool boolValue))
                return boolValue;

            return false;
        }
    }
}
