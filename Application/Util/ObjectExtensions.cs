using Newtonsoft.Json;

namespace BOHO.Application.Util;

public static class ObjectExtensions
{
    public static string SerializeToJson(this object obj)
    {
        if (obj is null)
            return string.Empty;

        if (obj is string)
            return (string)obj;

        return JsonConvert.SerializeObject(obj);
    }
}
