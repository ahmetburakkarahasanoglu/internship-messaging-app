namespace ChatApi.Services;

public sealed class ChatValidationException(string message) : Exception(message);

public sealed class ResourceNotFoundException(string message) : Exception(message);
