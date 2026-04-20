using System.Data;
using Dapper;

namespace Federation.Results.Api.Infrastructure;

public static class DapperTypeHandlers
{
    private static bool _configured;

    public static void Configure()
    {
        if (_configured) return;

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

        _configured = true;
    }

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
            => parameter.Value = value.ToDateTime(TimeOnly.MinValue);

        public override DateOnly Parse(object value)
            => value switch
            {
                DateTime dt => DateOnly.FromDateTime(dt),
                string s => DateOnly.Parse(s),
                _ => throw new DataException($"Não foi possível converter {value.GetType()} para DateOnly")
            };
    }

    private sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
            => parameter.Value = value.ToTimeSpan();

        public override TimeOnly Parse(object value)
            => value switch
            {
                TimeSpan ts => TimeOnly.FromTimeSpan(ts),
                DateTime dt => TimeOnly.FromDateTime(dt),
                string s => TimeOnly.Parse(s),
                _ => throw new DataException($"Não foi possível converter {value.GetType()} para TimeOnly")
            };
    }
}
