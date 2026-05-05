import i18n from '@/i18n';

/**
 * Translates an error key returned by the API.
 * If the key starts with "errors." and has a translation, returns the localized string.
 * Otherwise returns the raw string as-is (fallback for Identity/system errors).
 */
export function translateError(errorKey: string): string {
  if (errorKey.startsWith('errors.') && i18n.exists(errorKey)) {
    return i18n.t(errorKey);
  }
  return errorKey;
}

/**
 * Translates an array of error keys/messages from the API.
 */
export function translateErrors(errors: string[]): string[] {
  return errors.map(translateError);
}
