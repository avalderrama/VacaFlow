// SAD.md §9.3 names three rules: only-lib-api-may-fetch,
// components-do-not-import-pages, no-circular. The first is identifier-level
// (which global a file calls), not module-dependency-level, so it lives in
// eslint.config.mjs's no-restricted-globals override instead of here.
/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
  forbidden: [
    {
      name: 'no-circular',
      severity: 'error',
      comment: 'SAD.md §9.3 — no circular dependencies anywhere in the client.',
      from: {},
      to: { circular: true },
    },
    {
      name: 'components-do-not-import-pages',
      severity: 'error',
      comment: 'SAD.md §9.3 — components are leaves; pages compose them, never the reverse.',
      from: { path: '^components' },
      to: { path: '^app' },
    },
  ],
  options: {
    doNotFollow: { path: 'node_modules' },
    tsPreCompilationDeps: true,
    tsConfig: { fileName: 'tsconfig.json' },
    enhancedResolveOptions: {
      exportsFields: ['exports'],
      conditionNames: ['import', 'require', 'node', 'default', 'types'],
    },
  },
};
