using FluentValidation;

namespace ProcessManager.Domain.Validators
{
    // Abstract class containing methods used for mocking in tests (extension methods can't be mocked)
    public abstract class Validator<T> : AbstractValidator<T>
    {
        public virtual void ValidateAndThrow(T instance)
        {
            this.ValidateAndThrow(instance, null);
        }
    }
}
