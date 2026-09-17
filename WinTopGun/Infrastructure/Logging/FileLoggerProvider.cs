using Microsoft.Extensions.Logging;

namespace WinTopGun.Infrastructure.Logging;

/// <summary>
/// Proveedor de logging que escribe en un archivo de texto plano. Sustituye a los
/// <c>Console.WriteLine</c> del prototipo, invisibles en una aplicación WinForms.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _syncLock = new();

    /// <summary>Crea el proveedor y el directorio de logs si no existe.</summary>
    /// <param name="logDirectory">Directorio donde se escribirá el archivo de log.</param>
    public FileLoggerProvider(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _filePath = Path.Combine(logDirectory, "WinTopGun.log");
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, _filePath, _syncLock);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class FileLogger(string category, string filePath, object syncLock) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {category}: {formatter(state, exception)}";

            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            lock (syncLock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }
    }
}
