using System.Reflection;

namespace ValheimItemDataExtract
{
    public class CodeFactory
    {
        public string? ClassToString<T>()
        {
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && t.Name == typeof(T).Name);
            if (classes.Count() != 1)
            {
                return null;
            }
            var c = classes.First();
            var x = c
                .GetProperties()
                .Select(p => (p.Name, p.PropertyType));
            var s =
                @$"public class {c.Name} ";
            return s;
        }
    }
}
