namespace Shared.Contracts;

public record RegisterRequest(string Username, string Password, string PasswordConfirm);