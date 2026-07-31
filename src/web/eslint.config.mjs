import nextCoreWebVitals from 'eslint-config-next/core-web-vitals';

const eslintConfig = [
  ...nextCoreWebVitals,
  {
    // ADR-013 / SAD.md §9.3: lib/api.ts is the only module allowed to call
    // fetch. Everywhere else, importing from lib/api.ts is the only door in.
    files: ['**/*.ts', '**/*.tsx'],
    ignores: ['lib/api.ts'],
    rules: {
      'no-restricted-globals': ['error', { name: 'fetch', message: 'Call the API only through lib/api.ts (ADR-013).' }],
    },
  },
];

export default eslintConfig;
