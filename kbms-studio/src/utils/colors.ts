const DISTINCT_HUES = [
  0,   // Red
  25,  // Orange
  45,  // Gold
  70,  // Yellow-Green
  100, // Green
  140, // Emerald
  170, // Teal
  190, // Cyan
  210, // Azure
  230, // Blue
  260, // Indigo
  280, // Violet
  310, // Magenta
  340  // Pink
];

/** Returns a hue based on a sequential index assigned per-KB in the store */
export function getKbHueByIndex(index: number): number {
  return DISTINCT_HUES[index % DISTINCT_HUES.length];
}

/** Convenience: returns HSL color string using CSS vars for lightness/saturation */
export function getKbColorStyle(index: number): string {
  return `hsl(${getKbHueByIndex(index)}, var(--kb-color-saturation), var(--kb-color-lightness))`;
}

/** Legacy hash-based fallback (used when no index is assigned yet) */
export function getKbHue(kbName: string): number {
  if (!kbName) return 0;
  let hash = 0;
  for (let i = 0; i < kbName.length; i++) {
    hash = (hash << 5) - hash + kbName.charCodeAt(i);
    hash |= 0;
  }
  const index = Math.abs(hash) % DISTINCT_HUES.length;
  return DISTINCT_HUES[index];
}

