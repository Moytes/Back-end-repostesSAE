using System.Data;
using Dapper;

namespace Back_end_RepostesSAE.Data;

/// <summary>
/// Dapper no trae soporte nativo para <see cref="DateOnly"/>/<see cref="TimeOnly"/> (tipos de
/// .NET 6+): su tabla interna de DbType no los reconoce y cualquier parámetro de ese tipo
/// truena con "The member X of type System.DateOnly cannot be used as a parameter value".
/// Se registran manejadores explícitos una sola vez al arrancar (ver Program.cs) para que
/// los repositorios que usan Dapper (CitaRepository, CanalizacionRepository, etc.) puedan
/// pasar DateOnly/TimeOnly directamente, tal como ya hacen con los demás tipos.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => DateOnly.Parse(value.ToString()!)
    };
}

public sealed class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value is null ? DBNull.Value : value.Value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly? Parse(object value) => value switch
    {
        null or DBNull => null,
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => DateOnly.Parse(value.ToString()!)
    };
}

public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    public override TimeOnly Parse(object value) => value switch
    {
        TimeOnly t => t,
        TimeSpan ts => TimeOnly.FromTimeSpan(ts),
        _ => TimeOnly.Parse(value.ToString()!)
    };
}

public sealed class NullableTimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly?>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly? value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value is null ? DBNull.Value : (object)value.Value.ToTimeSpan();
    }

    public override TimeOnly? Parse(object value) => value switch
    {
        null or DBNull => null,
        TimeOnly t => t,
        TimeSpan ts => TimeOnly.FromTimeSpan(ts),
        _ => TimeOnly.Parse(value.ToString()!)
    };
}

public static class DapperDateTypeHandlers
{
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableTimeOnlyTypeHandler());
    }
}
