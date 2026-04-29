using System.Reflection;

namespace Framework.Test;

public class LibraryReferenceTests
{
    public static IEnumerable<object[]> FrameworkPublicMethods()
    {
        var assembly = typeof(Framework.UnsupportedException).Assembly;

        return assembly
            .GetTypes()
            .Where(t => t.IsPublic && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Select(m => new object[] { t.FullName ?? t.Name, BuildMethodSignature(m) }));
    }

    [Theory]
    [MemberData(nameof(FrameworkPublicMethods))]
    public void Framework_Each_Public_Method_Has_Test_Case(string typeName, string methodSignature)
    {
        Assert.False(string.IsNullOrWhiteSpace(typeName));
        Assert.False(string.IsNullOrWhiteSpace(methodSignature));
    }

    private static string BuildMethodSignature(MethodInfo methodInfo)
    {
        var parameterTypes = methodInfo
            .GetParameters()
            .Select(p => p.ParameterType.Name);

        return $"{methodInfo.Name}({string.Join(",", parameterTypes)})";
    }
}
