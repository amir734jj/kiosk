namespace Api.Extensions;

public static class DictionaryExtension
{
    public static bool ContainKeys<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, params TKey[] keys)
    {
        return keys.All(source.ContainsKey);
    }
}
