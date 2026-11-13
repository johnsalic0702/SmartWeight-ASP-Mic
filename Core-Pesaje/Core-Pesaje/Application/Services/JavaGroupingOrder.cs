namespace CorePesaje.Application.Services;

internal static class JavaGroupingOrder
{
    public static IEnumerable<IGrouping<long, T>> ForLongKeys<T>(IEnumerable<IGrouping<long, T>> source)
    {
        var groups = source.ToList();
        var capacity = 16;
        while (groups.Count > capacity * 0.75) capacity <<= 1;

        return groups.Select((group, index) => new
            {
                Group = group,
                Index = index,
                Bucket = BucketIndex(group.Key, capacity)
            })
            .OrderBy(item => item.Bucket)
            .ThenBy(item => item.Index)
            .Select(item => item.Group);
    }

    private static uint BucketIndex(long key, int capacity)
    {
        var hashCode = unchecked((uint)(int)(key ^ (long)((ulong)key >> 32)));
        var spreadHash = hashCode ^ (hashCode >> 16);
        return spreadHash & (uint)(capacity - 1);
    }
}
