using FluentValidation;
using MediatR;
using LinkUp.Application.Common.Models;

namespace LinkUp.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next(ct);

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next(ct);

        // Build validation error message
        var messages = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // If response is Result<T>, return typed failure
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var error = new AppError("VALIDATION_ERROR", messages, 400);
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(resultType)
                .GetMethod(nameof(Result<object>.Failure))!;
            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new ValidationException(failures);
    }
}
