using FluentValidation;
using TonerTrack.Application.NinjaRmm.Commands;
using TonerTrack.Application.Printers.Commands;

namespace TonerTrack.Application.Common.Validators;

public sealed class AddPrinterValidator : AbstractValidator<AddPrinterCommand>
{
    public AddPrinterValidator()
    {
        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.")
            .Matches(@"^(\d{1,3}\.){3}\d{1,3}$").WithMessage("IP address format is invalid.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Printer name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Community)
            .NotEmpty().WithMessage("SNMP community string is required.")
            .MaximumLength(100);
    }
}

public sealed class UpdatePrinterValidator : AbstractValidator<UpdatePrinterCommand>
{
    public UpdatePrinterValidator()
    {
        RuleFor(x => x.IpAddress).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Community).MaximumLength(100).When(x => x.Community is not null);
    }
}

public sealed class CreateTonerTicketValidator : AbstractValidator<CreateTonerTicketCommand>
{
    public CreateTonerTicketValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.TicketFormId).GreaterThan(0);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty();
    }
}
