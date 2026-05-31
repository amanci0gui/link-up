namespace LinkUp.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AppError? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(AppError error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(AppError error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(AppError error) => Failure(error);
}

public record AppError(string Code, string Message, int StatusCode = 400);

public static class Errors
{
    public static class Auth
    {
        public static AppError InvalidCredentials => new("AUTH_001", "Email ou senha inválidos.", 401);
        public static AppError InvalidRefreshToken => new("AUTH_002", "Token de refresh inválido ou expirado.", 401);
        public static AppError EmailAlreadyExists => new("AUTH_003", "Este email já está cadastrado.", 409);
    }

    public static class User
    {
        public static AppError NotFound => new("USER_001", "Usuário não encontrado.", 404);
    }

    public static class Recommendation
    {
        public static AppError NotConnectedToTarget => new("REC_001", "Você precisa estar conectado a ambos os usuários para indicá-los.", 403);
        public static AppError TargetBlockedRecommender => new("REC_002", "Um dos usuários bloqueou suas indicações.", 403);
        public static AppError TargetDisabledRecommendations => new("REC_003", "Um dos usuários não aceita indicações no momento.", 403);
        public static AppError DuplicatePending => new("REC_004", "Já existe uma recomendação pendente entre estes usuários.", 409);
        public static AppError NotFound => new("REC_005", "Recomendação não encontrada.", 404);
        public static AppError NotParticipant => new("REC_006", "Você não é participante desta recomendação.", 403);
        public static AppError AlreadyResponded => new("REC_007", "Você já respondeu esta recomendação.", 409);
    }

    public static class Connection
    {
        public static AppError AlreadyExists => new("CONN_001", "Já existe uma conexão ou solicitação pendente com este usuário.", 409);
        public static AppError RequestNotFound => new("CONN_002", "Solicitação de conexão não encontrada.", 404);
        public static AppError NotAuthorized => new("CONN_003", "Você não tem permissão para esta ação.", 403);
    }
}
