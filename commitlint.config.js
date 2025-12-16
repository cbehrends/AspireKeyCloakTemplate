module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat',      // A new feature
        'fix',       // A bug fix
        'docs',      // Documentation only changes
        'style',     // Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
        'refactor',  // A code change that neither fixes a bug nor adds a feature
        'perf',      // A code change that improves performance
        'test',      // Adding missing tests or correcting existing tests
        'chore',     // Changes to the build process, dependencies, or other tooling
        'ci',        // Changes to CI configuration files and scripts
        'build',     // Changes that affect the build system or external dependencies
      ],
    ],
    'subject-case': [2, 'never', ['start-case', 'pascal-case', 'upper-case']],
    'subject-empty': [2, 'never'],
    'subject-full-stop': [2, 'never', '.'],
    'type-case': [2, 'always', 'lowercase'],
    'type-empty': [2, 'never'],
  },
};

