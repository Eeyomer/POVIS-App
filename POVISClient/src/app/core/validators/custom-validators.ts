import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// 09XXXXXXXXX — exactly 11 digits, starts with 09
export const CONTACT_NUMBER_PATTERN = /^09\d{9}$/;

// Letters, apostrophe, hyphen, period, space
const NAME_ALLOWED_CHARS = /^[A-Za-z '.-]+$/;
const NAME_LEADING_SPECIAL = /^[ '.-]/;
const NAME_CONSECUTIVE_SPECIAL = /[ '.-]{2,}/;

export function personNameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    if (!NAME_ALLOWED_CHARS.test(value) || NAME_LEADING_SPECIAL.test(value) || NAME_CONSECUTIVE_SPECIAL.test(value)) {
      return { invalidName: true };
    }
    return null;
  };
}

// Letters, numbers, and common address punctuation
const ADDRESS_ALLOWED_CHARS = /^[A-Za-z0-9 ,.#\-\/'&()]+$/;

export function addressValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    return ADDRESS_ALLOWED_CHARS.test(value) ? null : { invalidAddress: true };
  };
}

// Letters, numbers, and common punctuation used in treatment/equipment/company names
const LABEL_ALLOWED_CHARS = /^[A-Za-z0-9 ,.#\-\/&()%']+$/;

export function labelValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    return LABEL_ALLOWED_CHARS.test(value) ? null : { invalidLabel: true };
  };
}

// Letters, numbers, hyphen, underscore, period only
const USERNAME_ALLOWED_CHARS = /^[A-Za-z0-9._-]+$/;

export function usernameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    return USERNAME_ALLOWED_CHARS.test(value) ? null : { invalidUsername: true };
  };
}

// At least 1 uppercase, 1 digit, 1 special character from !@#$%^&*()_-+=
const PASSWORD_SPECIAL_CHARS = /[!@#$%^&*()_\-+=]/;

export function passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    const hasUpper = /[A-Z]/.test(value);
    const hasDigit = /\d/.test(value);
    const hasSpecial = PASSWORD_SPECIAL_CHARS.test(value);
    return hasUpper && hasDigit && hasSpecial ? null : { weakPassword: true };
  };
}
