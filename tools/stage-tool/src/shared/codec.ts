import { ENCODED_CELL_WIDTH } from "./stage";

const BASE36 = "0123456789abcdefghijklmnopqrstuvwxyz";
const MAX_FIXED_WIDTH_VALUE = 36 ** ENCODED_CELL_WIDTH - 1;

export function encodeFixedBase36(values: readonly number[]): string {
  return values.map(encodeCellCode).join("");
}

export function decodeFixedBase36(encoded: string, field: string): number[] {
  if (encoded.length % ENCODED_CELL_WIDTH !== 0) {
    throw new Error(`${field} length must be divisible by ${ENCODED_CELL_WIDTH}`);
  }

  const values: number[] = [];
  for (let i = 0; i < encoded.length; i += ENCODED_CELL_WIDTH) {
    const token = encoded.slice(i, i + ENCODED_CELL_WIDTH).toLowerCase();
    if (!/^[0-9a-z]{2}$/.test(token)) {
      throw new Error(`${field} contains invalid base36 token '${token}'`);
    }
    values.push(parseInt(token, 36));
  }
  return values;
}

export function normalizeMap(input: number[] | string, field: string): number[] {
  if (typeof input === "string") {
    return decodeFixedBase36(input, field);
  }
  if (Array.isArray(input)) {
    return input.slice();
  }
  throw new Error(`${field} must be an encoded string or number array`);
}

function encodeCellCode(value: number): string {
  if (!Number.isInteger(value) || value < 0 || value > MAX_FIXED_WIDTH_VALUE) {
    throw new Error(`map value ${value} is outside fixed base36 range 0..${MAX_FIXED_WIDTH_VALUE}`);
  }
  return BASE36[Math.floor(value / 36)] + BASE36[value % 36];
}

