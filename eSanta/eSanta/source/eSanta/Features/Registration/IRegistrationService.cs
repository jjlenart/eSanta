namespace eSanta.Features.Registration;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(RegistrationModel model, CancellationToken cancellationToken = default);
}
