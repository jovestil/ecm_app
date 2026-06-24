const webpack = require('webpack');
const dotenv = require('dotenv');
const path = require('path');

module.exports = (config, options) => {
  // Load environment variables based on Angular configuration
  const environment = options.configuration || 'development';
  
  // Load environment files in order of priority
  const envFiles = [
    `.env.${environment}.local`,
    `.env.local`,
    `.env.${environment}`,
    '.env'
  ];

  // Load all environment files
  envFiles.forEach(envFile => {
    const envPath = path.resolve(__dirname, envFile);
    try {
      dotenv.config({ path: envPath });
    } catch (error) {
      // File doesn't exist, continue
    }
  });

  // Filter environment variables that start with NG_APP_
  const ngAppEnvVars = {};
  Object.keys(process.env).forEach(key => {
    if (key.startsWith('NG_APP_')) {
      ngAppEnvVars[key] = process.env[key];
    }
  });

  // Create process.env object for the browser
  const processEnv = {
    'process.env': JSON.stringify(ngAppEnvVars)
  };

  // Add DefinePlugin to inject environment variables
  config.plugins.push(
    new webpack.DefinePlugin(processEnv)
  );

  // Optional: Add some debug logging
  if (environment === 'development') {
    console.log('🔧 Webpack: Loaded environment variables:', Object.keys(ngAppEnvVars));
  }

  return config;
};