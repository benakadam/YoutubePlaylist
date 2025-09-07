namespace YoutubePlaylist.Helpers;

public static class DIContainer
{
    public static T Resolve<T>()
    {
        return (T)Resolve(typeof(T));
    }

    private static object Resolve(Type type)
    {
        if (type.IsInterface)
        {
            string typeName = Helper.GetConfigValue(type.Name);
            if (string.IsNullOrEmpty(typeName))
                throw new InvalidOperationException($"{type.Name} is not configured in App.config.");

            type = Type.GetType(typeName)
                   ?? throw new InvalidOperationException($"Type not found: {typeName}");
        }

        var ctor = type.GetConstructors()
                       .OrderByDescending(c => c.GetParameters().Length)
                       .FirstOrDefault()
                       ?? throw new InvalidOperationException($"No public constructor found for {type.FullName}");

        var parameters = ctor.GetParameters()
                             .Select(p => Resolve(p.ParameterType))
                             .ToArray();

        return Activator.CreateInstance(type, parameters)
            ?? throw new InvalidOperationException($"Failed to create instance of type: {type.FullName}");
    }
}
