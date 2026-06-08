using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RulesEngineEditor.Server.Infrastructure.Data;

public sealed class StronglyTypedIdValueConverter<TId, TPrimitive> : ValueConverter<TId, TPrimitive>
    where TId : struct
    where TPrimitive : struct
{
    public StronglyTypedIdValueConverter(
        Expression<Func<TId, TPrimitive>> convertToProvider,
        Expression<Func<TPrimitive, TId>> convertFromProvider)
        : base(convertToProvider, convertFromProvider)
    {
    }
}
