using FluentValidation;
using System.Text.Json;

namespace SaaS.Application.Features.TargetGroups.Commands.Add
{
    public class AddGroupCommandValidator : AbstractValidator<AddGroupCommand>
    {
        public AddGroupCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("A valid user Id must be provided.");

            RuleFor(x => x.GroupDto.BotId)
                .GreaterThan(0)
                .WithMessage("A valid BotId must be provided.");

            RuleFor(x => x.GroupDto.GroupName)
                .NotEmpty()
                .WithMessage("GroupName is required.");

            RuleFor(x => x.GroupDto.ConfigJson)
                .Must(IsValidJson).WithMessage("InfoJson must be valid JSON.");

        }

        private bool IsValidJson(string? json)
        {
            if (json == null) return true;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
