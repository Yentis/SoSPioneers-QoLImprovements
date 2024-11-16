namespace ExtensionMethods
{
    public static class Il2CppListExtensions
    {
        public static List<T> ToList<T>(this Il2CppSystem.Collections.Generic.List<T> list)
        {
            var regularList = new List<T>();
            foreach (var item in list) regularList.Add(item);

            return regularList;
        }
    }
}
