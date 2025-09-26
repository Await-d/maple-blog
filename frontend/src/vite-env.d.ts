/// <reference types="vite/client" />

// Vite HMR types
interface ImportMeta {
  readonly hot?: {
    readonly accept: (dep?: string, cb?: () => void) => void;
    readonly dispose: (cb: () => void) => void;
    readonly decline: () => void;
    readonly invalidate: () => void;
  };
}