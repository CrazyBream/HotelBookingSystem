using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace HotelBooking.DAL.Infrastructure;

public static class DapperConfiguration
{
    private static bool _configured;

    public static void Configure()
    {
        if (_configured) return;

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        _configured = true;
    }

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value;
            if (parameter is NpgsqlParameter npgsqlParameter)
                npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Date;
        }

        public override DateOnly Parse(object value) => value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => DateOnly.Parse(value.ToString()!)
        };
    }
}