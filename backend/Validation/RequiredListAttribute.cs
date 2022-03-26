using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Validation
{
    public class RequiredListAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            IList list = value as IList;

            if (list == null || list.Count <= 0) return new ValidationResult("List cannot be null or empty.");
            else return ValidationResult.Success;
        }
    }
}
