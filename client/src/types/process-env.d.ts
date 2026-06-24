// TypeScript declarations for process.env in browser
declare var process: {
  env: {
    [key: string]: string | undefined;
  };
};