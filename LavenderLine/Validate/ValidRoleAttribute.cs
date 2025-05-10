using System.ComponentModel.DataAnnotations;

public class ValidRoleAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Check if the value is a string and not null
        if (value is string role && !string.IsNullOrWhiteSpace(role))
        {
            // Define valid roles as an array for better maintainability
            var validRoles = new[] { Roles.Manager, Roles.Customer, Roles.Admin };

            if (validRoles.Contains(role))
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult("Invalid role specified. Valid roles are: Manager, Customer, Admin.");
    }
}