namespace ComCat.Extensions
{
    public static class DecimalExt
    {
        public static decimal Pow(this decimal dBase, int n)
        {
            var value = 1M;
            for (; n > 0; n--)
                value *= dBase;
            return value;
        }
    }
}
