using System.Reflection;

namespace Cooking.Application;

/// <summary>Маркер сборки для регистрации MediatR (и других сканеров сборок) без привязки к случайному бизнес-типу.</summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
