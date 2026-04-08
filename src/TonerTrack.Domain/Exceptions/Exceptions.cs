namespace TonerTrack.Domain.Exceptions;

public class PrinterDomainException(string message, Exception? inner = null)
    : Exception(message, inner);

public sealed class PrinterNotFoundException(string ipAddress)
    : PrinterDomainException($"Printer with IP address '{ipAddress}' was not found.");
