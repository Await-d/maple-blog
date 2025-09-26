/**
 * Class name utility function for conditional styling
 * Similar to clsx but simpler implementation
 */

type ClassValue =
  | string
  | number
  | boolean
  | undefined
  | null
  | { [key: string]: boolean | undefined | null }
  | ClassValue[];

/**
 * Combines class names conditionally
 * @param classes - Array of class values to combine
 * @returns Combined class string
 */
export function cn(...classes: ClassValue[]): string {
  const result: string[] = [];

  for (const cls of classes) {
    if (!cls) continue;

    if (typeof cls === 'string' || typeof cls === 'number') {
      result.push(String(cls));
    } else if (typeof cls === 'object' && !Array.isArray(cls)) {
      for (const [key, value] of Object.entries(cls)) {
        if (value) {
          result.push(key);
        }
      }
    } else if (Array.isArray(cls)) {
      const nested = cn(...cls);
      if (nested) {
        result.push(nested);
      }
    }
  }

  return result.join(' ');
}

export default cn;