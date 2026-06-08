using System.Data;
using System.Globalization;
using Dapper;

namespace Outlet.Registry.Outbox.Tests.Support;

/// <summary>
/// Microsoft.Data.Sqlite stores Guid and DateTimeOffset as TEXT; these handlers let Dapper
/// round-trip them. They live in the TEST harness, never in the shipped adapter — real SQL
/// Server / PostgreSQL handle both types natively, so the adapter SQL stays provider-neutral.
/// </summary>
public static class SqliteDapperTypeHandlers
{
    private static bool _registered;
    private static readonly object Gate = new();

    public static void EnsureRegistered()
    {
        lock (Gate)
        {
            if (_registered)
                return;

            SqlMapper.AddTypeHandler(new GuidHandler());
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            _registered = true;
        }
    }

    private sealed class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.ToString();
        }
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value) =>
            DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
