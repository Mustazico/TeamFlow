using FluentValidation;
using AppValidationException = TeamFlow.Application.Common.Exceptions.ValidationException;

namespace TeamFlow.Api.Common;

public static class ValidationExtensions
{
    public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);
        if (result.IsValid) return;
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new AppValidationException(errors);
    }
}
